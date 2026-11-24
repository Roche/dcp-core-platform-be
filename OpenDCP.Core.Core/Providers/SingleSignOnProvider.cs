using OpenDCP.Core.Core.Interfaces;
using OpenDCP.Core.Models.Constants;
using OpenDCP.Core.Models.Models.Authentication;
using OpenDCP.Framework.Exceptions;
using OpenDCP.Framework.ConfigurationProvider.Classes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Security;
using System.Globalization;


namespace OpenDCP.Core.Core.Providers
{
    public class SingleSignOnProvider : IAuthenticationProvider
    {
        private readonly string _sigAlg = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";
        private readonly string _relayState = string.Empty;

        private readonly IConfiguration _configuration;
        public SingleSignOnProvider(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public string LoginUrl(HttpRequest? request = null)
        {
            return GetRedirectUrl();
        }
        public string ExtractRequestData(HttpRequest request)
        {
            // POST binding
            if (request.HasFormContentType &&
                request.Form.TryGetValue("SAMLResponse", out var formValue))
            {
                return formValue.ToString();
            }

            // Redirect binding
            if (request.Query.TryGetValue("SAMLResponse", out var queryValue))
            {
                return queryValue.ToString();
            }

            throw new DCPInternalServerErrorException("Error in SAML flow");
        }
        public Task<IdPReponse> AuthenticateAsync(string tokenOrCredentials)
        {
            if (string.IsNullOrWhiteSpace(tokenOrCredentials))
                throw new ArgumentException("Token or credentials must be provided.", nameof(tokenOrCredentials));

            string xmlString;

            //Safe Base64 decoding
            try
            {
                var bytes = Convert.FromBase64String(tokenOrCredentials);
                xmlString = Encoding.UTF8.GetString(bytes);
            }
            catch (FormatException ex)
            {
                throw new SecurityException("Invalid Base64 encoded SAML response.", ex);
            }

            //Secure XML parsing (prevents XXE)
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                IgnoreComments = true,
                IgnoreProcessingInstructions = true
            };

            XmlDocument doc = new XmlDocument
            {
                PreserveWhitespace = true,
                XmlResolver = null
            };

            using (var stringReader = new StringReader(xmlString))
            using (var xmlReader = XmlReader.Create(stringReader, settings))
            {
                doc.Load(xmlReader);
            }

            //Domain validation
            ValidateResponse(doc);

            //Extract validated data
            var result = ExtractResponse(doc);
            return Task.FromResult(result);
        }

        private static IdPReponse ExtractResponse(XmlDocument doc)
        {
            var nsmgr = CreateNamespaceManager(doc);

            if (doc is null)
                throw new ArgumentException("No XML document provided");

            // Get required values
            var issuer = GetRequiredNodeInnerText(doc, "//saml:Issuer", nsmgr);
            var assertionId = GetAttributeValue(doc.SelectSingleNode("//saml:Assertion", nsmgr)!, "ID");
            var unixId = GetRequiredNodeInnerText(doc, "//saml:Subject/saml:NameID", nsmgr);
            var firstName = GetRequiredNodeInnerText(doc, "//saml:AttributeStatement/saml:Attribute[@Name='FirstName']/saml:AttributeValue", nsmgr);
            var lastName = GetRequiredNodeInnerText(doc, "//saml:AttributeStatement/saml:Attribute[@Name='LastName']/saml:AttributeValue", nsmgr);
            var email = GetNodeInnerText(doc, "//saml:AttributeStatement/saml:Attribute[@Name='Email Address']/saml:AttributeValue", nsmgr);

            // Groups can be empty but not null
            var groupNodes = doc.SelectSingleNode("//saml:AttributeStatement/saml:Attribute[@Name='radaGroups']", nsmgr);
            var groups = groupNodes?.ChildNodes.Cast<XmlNode>().Select(x => x.InnerText).Where(x => !string.IsNullOrWhiteSpace(x)).ToList()
                         ?? new List<string>();

            var data = new IdPReponse
            {
                StatusCode = "Success",
                Issuer = issuer,
                AssertionId = assertionId,
                UnixId = unixId,
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                Groups = groups
            };

            return data;
        }
        private void ValidateResponse(XmlDocument doc)
        {
            ArgumentNullException.ThrowIfNull(nameof(doc));

            var nsmgr = CreateNamespaceManager(doc);

            // Status code
            var statusCodeNode = doc.SelectSingleNode("//samlp:Status/samlp:StatusCode", nsmgr)
                                 ?? throw new DCPBadRequestException(ErrorMessages.SSOError);

            var statusCodeAttr = statusCodeNode.Attributes?["Value"]?.Value;
            var statusCode = statusCodeAttr?.Split(':').LastOrDefault();

            if (!string.Equals(statusCode, "Success", StringComparison.OrdinalIgnoreCase))
                throw new DCPBadRequestException(ErrorMessages.SSOError);

            // Assertion nodes
            var assertionNodes = doc.SelectNodes("/samlp:Response/saml:Assertion", nsmgr);
            if (assertionNodes == null || assertionNodes.Count != 1)
                throw new DCPBadRequestException(ErrorMessages.SSOError);

            var assertion = doc.SelectSingleNode("/samlp:Response/saml:Assertion", nsmgr) as XmlElement
                            ?? throw new DCPBadRequestException(ErrorMessages.SSOError);

            // Signature
            var sigElem = assertion.SelectSingleNode("./ds:Signature", nsmgr) as XmlElement;
            XmlElement signedElement = assertion;

            if (sigElem == null)
            {
                sigElem = doc.SelectSingleNode("/samlp:Response/ds:Signature", nsmgr) as XmlElement
                          ?? throw new DCPBadRequestException(ErrorMessages.SSOError);

                signedElement = doc.DocumentElement
                                ?? throw new DCPBadRequestException(ErrorMessages.SSOError);
            }

            // SignedXml
            var signedXml = new SignedXml(signedElement);
            signedXml.LoadXml(sigElem);

            var signingCert = ExtractCertFromSignedXml(signedXml)
                              ?? throw new DCPBadRequestException(ErrorMessages.SSOError);

            // Certificate validation
            bool allowUntrustedCertificates = Convert.ToBoolean(_configuration["IdP:Settings:AllowUntrustedCertificates"]);
            using var chain = new X509Chain();
            if (allowUntrustedCertificates)
                chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;

            if (!chain.Build(signingCert) || !signedXml.CheckSignature(signingCert, true))
                throw new DCPBadRequestException(ErrorMessages.SSOError);

            // Conditions: NotOnOrAfter
            var notOnOrAfterAttr = doc.SelectSingleNode("//saml:Conditions/@NotOnOrAfter", nsmgr)?.Value;
            if (!string.IsNullOrEmpty(notOnOrAfterAttr)
                && DateTime.TryParse(notOnOrAfterAttr, CultureInfo.InvariantCulture, out var notOnOrAfter)
                && DateTime.UtcNow >= notOnOrAfter)
            {
                throw new DCPBadRequestException(ErrorMessages.SSOError);
            }
        }


        private string GetRedirectUrl(string relayState = "")
        {
            var samlEndpoint = _configuration["IdP:Settings:SSOSamlEndpoint"];

            if (samlEndpoint is null)
                throw new DCPInternalServerErrorException(ErrorMessages.SSOError);

            bool signRequest = Convert.ToBoolean(_configuration["IdP:Settings:SignRequest"]);

            string signature = string.Empty;

            string samlRequestXml = BuildAuthnRequestXmlAsync().GetAwaiter().GetResult();
            string samlRequest = DeflateAndBase64Encode(samlRequestXml);

            // Build query string in the correct order
            string queryString = $"SAMLRequest={Uri.EscapeDataString(samlRequest)}";

            if (!string.IsNullOrEmpty(relayState))
            {
                queryString += $"&RelayState={Uri.EscapeDataString(relayState)}";
            }

            if (signRequest)
            {
                queryString += $"&SigAlg={Uri.EscapeDataString(_sigAlg)}";

                // Calculate signature
                byte[] dataToSign = Encoding.UTF8.GetBytes(queryString);
                byte[] signatureBytes;
                using (var rsa = LoadCertificate().GetRSAPrivateKey())
                {
                    if (rsa is null)
                        throw new DCPInternalServerErrorException(ErrorMessages.SSOError);

                    signatureBytes = rsa.SignData(dataToSign, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                }
                signature = Convert.ToBase64String(signatureBytes);
            }

            // Build redirect URL
            var queryStringSeparator = samlEndpoint.Contains('?') ? "&" : "?";
            var url = $"{samlEndpoint}{queryStringSeparator}{queryString}";

            if (signRequest)
            {
                url = $"{url}&Signature={Uri.EscapeDataString(signature)}";
            }

            return url;
        }

        private async Task<string> BuildAuthnRequestXmlAsync()
        {
            string id = "_" + Guid.NewGuid().ToString("N");
            var sb = new StringBuilder();
            using (var sw = new StringWriter(sb))
            {
                var xws = new XmlWriterSettings
                {
                    OmitXmlDeclaration = true,
                    Async = true
                };
                using (var xw = XmlWriter.Create(sw, xws))
                {
                    await xw.WriteStartElementAsync("samlp", "AuthnRequest", "urn:oasis:names:tc:SAML:2.0:protocol");
                    await xw.WriteAttributeStringAsync(null, "ID", null, id);
                    await xw.WriteAttributeStringAsync(null, "Version", null, "2.0");
                    await xw.WriteAttributeStringAsync(null, "IssueInstant", null,
                        DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture));

                    await xw.WriteAttributeStringAsync(null, "Destination", null, _configuration["IdP:Settings:SSOSamlEndpoint"]);
                    await xw.WriteAttributeStringAsync(null, "ProtocolBinding", null, "urn:oasis:names:tc:SAML:2.0:bindings:HTTP-POST");
                    await xw.WriteAttributeStringAsync(null, "AssertionConsumerServiceURL", null, _configuration["IdP:Settings:AssertionConsumerServiceURL"]);

                    // Issuer
                    await xw.WriteStartElementAsync("saml", "Issuer", "urn:oasis:names:tc:SAML:2.0:assertion");
                    await xw.WriteStringAsync(_configuration["IdP:Settings:SSOIssuer"]);
                    await xw.WriteEndElementAsync();

                    // NameIDPolicy
                    await xw.WriteStartElementAsync("samlp", "NameIDPolicy", "urn:oasis:names:tc:SAML:2.0:protocol");
                    await xw.WriteAttributeStringAsync(null, "Format", null, "urn:oasis:names:tc:SAML:1.1:nameid-format:unspecified");
                    await xw.WriteAttributeStringAsync(null, "AllowCreate", null, "true");
                    await xw.WriteEndElementAsync();

                    await xw.WriteEndElementAsync(); // AuthnRequest
                }
            }
            return sb.ToString();
        }

        private static XmlNamespaceManager CreateNamespaceManager(XmlDocument doc)
        {
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("saml", "urn:oasis:names:tc:SAML:2.0:assertion");
            nsmgr.AddNamespace("samlp", "urn:oasis:names:tc:SAML:2.0:protocol");
            nsmgr.AddNamespace("ds", "http://www.w3.org/2000/09/xmldsig#"); //NOSONAR
            return nsmgr;
        }

        /// <summary>
        /// Extracts the first X.509 certificate from the <see cref="SignedXml"/> KeyInfo section.
        /// </summary>
        /// <param name="signedXml">The signed XML containing KeyInfo data.</param>
        /// <returns>
        /// The extracted <see cref="X509Certificate2"/> if present; otherwise, <c>null</c>.
        /// </returns>
        private static X509Certificate2? ExtractCertFromSignedXml(SignedXml signedXml)
        {
            if (signedXml?.KeyInfo == null)
                return null;

            foreach (KeyInfoX509Data x509Data in signedXml.KeyInfo.OfType<KeyInfoX509Data>())
            {
                // Prefer X509Certificate2 directly
                var cert2 = x509Data.Certificates?
                    .OfType<X509Certificate2>()
                    .FirstOrDefault();

                if (cert2 != null)
                    return cert2;

                // Fallback: convert X509Certificate → X509Certificate2
                var cert = x509Data.Certificates?
                    .OfType<X509Certificate>()
                    .FirstOrDefault();

                if (cert != null)
                    return new X509Certificate2(cert);
            }

            return null;
        }

        private X509Certificate2 LoadCertificate()
        {
            var machineName = _configuration["MachineName"];
            var machineNameNonGxp = _configuration["MachineNameNonGxp"];
            var certReader = new CertificateReader();

            using (var store = new X509Store(StoreLocation.LocalMachine))
            {
                store.Open(OpenFlags.ReadOnly);

                foreach (var cert in store.Certificates)
                {
                    List<string> sans = certReader.GetSubjectAlternativeNames(cert);
                    if ((!string.IsNullOrEmpty(machineName) && sans.Contains(machineName) || (!string.IsNullOrEmpty(machineNameNonGxp) && sans.Contains(machineNameNonGxp)))
                         && cert.HasPrivateKey)
                    {
                        return cert;
                    }
                }
            }
            throw new InvalidOperationException("No suitable certificate with private key found.");
        }

        private static string DeflateAndBase64Encode(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);

            using (var output = new MemoryStream())
            {
                // Raw DEFLATE (kein ZLib Header)
                using (var deflate = new DeflateStream(output, CompressionMode.Compress, true))
                {
                    deflate.Write(bytes, 0, bytes.Length);
                }

                return Convert.ToBase64String(output.ToArray());
            }
        }

        private static string GetRequiredNodeInnerText(XmlDocument doc, string xpath, XmlNamespaceManager nsmgr)
        {
            var node = doc.SelectSingleNode(xpath, nsmgr)
                       ?? throw new InvalidOperationException($"Required XML node '{xpath}' not found.");
            return node.InnerText ?? throw new InvalidOperationException($"XML node '{xpath}' has no value.");
        }

        private static string? GetNodeInnerText(XmlDocument doc, string xpath, XmlNamespaceManager nsmgr)
        {
            var node = doc.SelectSingleNode(xpath, nsmgr);
            return node?.InnerText;
        }

        private static string GetAttributeValue(XmlNode node, string attributeName)
        {
            var attr = node.Attributes?[attributeName]?.Value
                       ?? throw new InvalidOperationException($"Required attribute '{attributeName}' not found in node '{node.Name}'.");
            return attr;
        }

        public Task<string> LogOutAsync()
        {
            var url = _configuration["IdP:Settings:SLOEndpoint"]
                ?? throw new DCPInternalServerErrorException(ErrorMessages.MissingConfiguration);

            return Task.FromResult(url);
        }
    }
}
