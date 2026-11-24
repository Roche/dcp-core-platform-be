using OpenDCP.Core.Core.Interfaces;
using OpenDCP.Core.Models.Models.CalculationNode;
using OpenDCP.Framework.CalculationNode.Classes;
using Microsoft.Extensions.Configuration;
using System;
using System.Text.Json.Nodes;
using System.Threading.Tasks;


namespace OpenDCP.Core.Core.Providers
{
    public class DataikuCalculationNode : DataikuConnection, ICalculationNode, IEquatable<DataikuCalculationNode>
    {

        public DataikuCalculationNode(IConfiguration configuration): base(configuration)
        {

        }
        public DataikuCalculationNode(IConfiguration configuration, int? id, JsonObject connectionString): base(id, connectionString, configuration) {

        }

        public Task<CalculationNodeVersionResponse> GetCalculationNodeVersionAsync()
        {

            var result = new CalculationNodeVersionResponse
            {
                Version = new CalculationNodeVersionDTO
                {
                    Id = 1,
                    Version = "0.1.0"
                }
            };

            return Task.FromResult(result);
        }

        public bool Equals(DataikuCalculationNode? other)
        {
            if (other == null)
                return false;

            return string.Equals(
                _connectionString["BaseUrl"]?.ToString(),
                other._connectionString["BaseUrl"]?.ToString(),
                StringComparison.OrdinalIgnoreCase
            );
        }

        // Override Equals for object comparisons
        public override bool Equals(object? obj)
        {
            if (obj is DataikuCalculationNode otherNode)
            {
                return Equals(otherNode);
            }
            return false;
        }

        // Override GetHashCode for hash-based collections like HashSet or Dictionary
        public override int GetHashCode()
        {
            return HashCode.Combine(_connectionString["BaseUrl"]?.ToString().ToLowerInvariant());
        }

        public bool IsConfigured()
        {
            return !string.IsNullOrEmpty(_connectionString["BaseUrl"]?.ToString());
        }
    }
}
