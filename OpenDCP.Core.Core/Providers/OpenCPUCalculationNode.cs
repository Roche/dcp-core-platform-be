using OpenDCP.Core.Core.Interfaces;
using OpenDCP.Core.Models.Models.CalculationNode;
using OpenDCP.Framework.CalculationNode.Classes;
using OpenDCP.Framework.CalculationNode.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;


namespace OpenDCP.Core.Core.Providers
{
    public class OpenCPUCalculationNode : OpenCPUConnection, ICalculationNode, IEquatable<OpenCPUCalculationNode>
    {

        public OpenCPUCalculationNode(IConfiguration configuration) : base(configuration)
        {

        }
        public OpenCPUCalculationNode(IConfiguration configuration, int? id, JsonObject connectionString) : base(id, connectionString, configuration)
        {

        }

        protected override List<string> SecretFields => new List<string> { "rpassword" };

        public async Task<CalculationNodeVersionResponse> GetCalculationNodeVersionAsync()
        {
            var url = "/lib/acdcClient/R/getVersionInfo/json";

            var response = await PostAsync(url, new List<ACDCRequestParameters>());

            var result = new CalculationNodeVersionResponse();

            if (response.NodeErrors.Count > 0)
            {
                result.Errors = response.NodeErrors.Select(e => new Framework.Configuration.Model.Errors.GenericError
                {
                    Error = e.Error,
                    ErrorCode = e.ErrorCode,
                    Identifier = e.Identifier
                }).ToList();
                return result;
            }

            var doc = JsonDocument.Parse(response.Content).RootElement;

            var version = new CalculationNodeVersionDTO
            {
                Id = _id!.Value,
                Version = doc.GetProperty("R").GetString() ?? "Unkown",
                Packages = new List<CalculationNodePackageVersionDTO>()
            };

            foreach (JsonProperty pkg in doc.GetProperty("pkg").EnumerateObject())
            {
                var pkgVersion = new CalculationNodePackageVersionDTO
                {
                    Name = pkg.Name,
                    Version = pkg.Value.GetString() ?? "Unkown"
                };

                version.Packages.Add(pkgVersion);
            }

            result.Version = version;

            return result;
        }

        public bool Equals(OpenCPUCalculationNode? other)
        {
            if (other == null)
                return false;

            return string.Equals(
                _connectionString["RserverBaseURL"]?.ToString(),
                other._connectionString["RserverBaseURL"]?.ToString(),
                StringComparison.OrdinalIgnoreCase
             );
        }

        // Override Equals for object comparisons
        public override bool Equals(object? obj)
        {
            if (obj is OpenCPUCalculationNode otherNode)
            {
                return Equals(otherNode);
            }
            return false;
        }

        // Override GetHashCode for hash-based collections like HashSet or Dictionary
        public override int GetHashCode()
        {
            return HashCode.Combine(_connectionString["RserverBaseURL"]?.ToString().ToLowerInvariant());
        }

        public bool IsConfigured()
        {
            return !string.IsNullOrEmpty(_connectionString["RserverBaseURL"]?.ToString());
        }
    }
}
