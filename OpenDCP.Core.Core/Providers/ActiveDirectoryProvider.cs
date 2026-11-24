using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using OpenDCP.Core.Core.Interfaces;
using OpenDCP.Core.Models.Constants;
using OpenDCP.Core.Models.Models.Authentication;
using OpenDCP.Framework.Exceptions;
using System;
using System.Collections.Generic;
using System.DirectoryServices.Protocols;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace OpenDCP.Core.Core.Providers
{
    public class ActiveDirectoryProvider : IAuthenticationProvider
    {
        private readonly IConfiguration _configuration;

        public ActiveDirectoryProvider(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string ExtractRequestData(HttpRequest request)
        {
            if (string.IsNullOrEmpty(request.Form["username"]))
            {
                throw new DCPUnauthorizedException(ErrorMessages.SSOError);
            }

            if (string.IsNullOrEmpty(request.Form["password"]))
            {
                throw new DCPUnauthorizedException(ErrorMessages.SSOError);
            }
            return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{request.Form["username"]}:{request.Form["password"]}"));
        }

        private void ValidateUser(string tokenOrCredentials)
        {
            var credentials = Encoding.UTF8.GetString(Convert.FromBase64String(tokenOrCredentials)).Split(":");

            try
            {
                // Establish a connection to the LDAP server
                using (var connection = new LdapConnection(_configuration["IdP:Settings:Server"]))
                {
                    // Use NetworkCredential to provide the username and password
                    var credential = new NetworkCredential(credentials[0], credentials[1]);
                    connection.Credential = credential;
                    connection.Bind(); // Attempt to bind (triggers validation)
                }
            }
            catch (LdapException)
            {
                throw new DCPUnauthorizedException(ErrorMessages.NoUserAccess);
            }
        }

        public Task<IdPReponse> AuthenticateAsync(string tokenOrCredentials)
        {
            if (string.IsNullOrWhiteSpace(tokenOrCredentials))
                throw new ArgumentException("Token or credentials must be provided.", nameof(tokenOrCredentials));

            ValidateUser(tokenOrCredentials);

            // Safely decode Base64
            string decoded;
            try
            {
                decoded = Encoding.UTF8.GetString(Convert.FromBase64String(tokenOrCredentials));
            }
            catch (FormatException ex)
            {
                throw new DCPBadRequestException("Invalid credential format.", ex);
            }

            var parts = decoded.Split(':', 2);
            if (parts.Length == 0 || string.IsNullOrWhiteSpace(parts[0]))
                throw new DCPBadRequestException("Invalid credential content.");

            string userToSearch = parts[0];

            // Read configuration safely
            var ldapServer = _configuration["IdP:Settings:Server"]
                ?? throw new InvalidOperationException("LDAP server configuration is missing.");

            var ldapUser = _configuration["IdP:Settings:User"]
                ?? throw new InvalidOperationException("LDAP bind user is missing.");

            var ldapPassword = _configuration["IdP:Settings:Password"]
                ?? throw new InvalidOperationException("LDAP bind password is missing.");

            var searchBase = _configuration["IdP:Settings:SearchBase"]
                ?? throw new InvalidOperationException("LDAP search base is missing.");

            using var connection = new LdapConnection(ldapServer)
            {
                Credential = new NetworkCredential(ldapUser, ldapPassword),
                AuthType = AuthType.Basic
            };

            connection.Bind();

            // Escape input to prevent LDAP injection
            string escapedUser = EscapeLdapFilterValue(userToSearch);
            string searchFilter = $"(sAMAccountName={escapedUser})";

            string[] attributesToLoad =
            {
                "cn",
                "givenName",
                "sn",
                "mail",
                "memberOf"
            };

            var request = new SearchRequest(
                searchBase,
                searchFilter,
                SearchScope.Subtree,
                attributesToLoad
            );

            var response = connection.SendRequest(request) as SearchResponse;

            if (response?.Entries.Count > 0)
            {
                var entry = response.Entries[0];

                var groups = entry.Attributes["memberOf"]?
                                .GetValues(typeof(string))
                                .OfType<string>()
                                .Select(dn =>
                                {
                                    var cnPart = dn.Split(',')
                                        .FirstOrDefault(p => p.StartsWith("CN=", StringComparison.OrdinalIgnoreCase));
                                    if (cnPart == null) throw new InvalidOperationException($"Invalid DN format: {dn}");
                                    return cnPart.Substring(3);
                                })
                                .ToList() ?? new List<string>();

                var result = new IdPReponse
                {
                    StatusCode = "Success",
                    Issuer = ldapServer,
                    AssertionId = userToSearch,
                    UnixId = GetAttributeValue(entry, "cn"),
                    FirstName = GetAttributeValue(entry, "givenName"),
                    LastName = GetAttributeValue(entry, "sn"),
                    Email = GetAttributeValue(entry, "mail"),
                    Groups = groups
                };

                return Task.FromResult(result);
            }

            throw new DCPBadRequestException(ErrorMessages.UnkownUser);
        }

        public string LoginUrl(HttpRequest? request = null)
        {
            return $"{request!.Host}/LoginForm.html";
        }

        private static string EscapeLdapFilterValue(string value)
        {
            return value
                .Replace(@"\", @"\5c")
                .Replace("*", @"\2a")
                .Replace("(", @"\28")
                .Replace(")", @"\29")
                .Replace("\0", @"\00");
        }

        private static string GetAttributeValue(SearchResultEntry entry, string attributeName)
        {
            if (entry.Attributes[attributeName]?.Count > 0 && entry.Attributes[attributeName][0] != null)
                return entry.Attributes[attributeName][0].ToString()!;

            throw new InvalidOperationException($"Required LDAP attribute '{attributeName}' is missing or empty.");
        }

        public Task<string> LogOutAsync()
        {
            throw new NotImplementedException();
        }
    }
}
