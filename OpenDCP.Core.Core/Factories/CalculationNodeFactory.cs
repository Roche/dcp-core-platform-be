using OpenDCP.Core.Core.Interfaces;
using OpenDCP.Core.Core.Providers;
using OpenDCP.Core.DAL;
using OpenDCP.Core.Models.Constants;
using OpenDCP.Framework.Configuration.Model.Constants.CalculationNode;
using OpenDCP.Framework.Exceptions;
using OpenDCP.Framework.Security.Classes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;


namespace OpenDCP.Core.Core.Factories
{
    public class CalculationNodeFactory
    {
        private readonly IConfiguration _configuration;
        private readonly DCPCoreDbContext _dbContext;

        public CalculationNodeFactory(IConfiguration configuration, DCPCoreDbContext dbContext)
        {
            _configuration = configuration;
            _dbContext = dbContext;
        }
        public ICalculationNode LoadByConnectionString(int? id, CalculationNodeType type, JsonObject connectionString)
        {
            switch (type)
            {
                case CalculationNodeType.OpenCPU:
                    return new OpenCPUCalculationNode(_configuration, id, connectionString);
                case CalculationNodeType.Dataiku:
                    return new DataikuCalculationNode(_configuration, id, connectionString);
                default:
                    throw new ArgumentException($"The {nameof(type)} is not known as a calculation Node");
            }
        }

        public ICalculationNode LoadByType(CalculationNodeType type)
        {
            switch (type)
            {
                case CalculationNodeType.OpenCPU:
                    return new OpenCPUCalculationNode(_configuration);
                case CalculationNodeType.Dataiku:
                    return new DataikuCalculationNode(_configuration);
                default:
                    throw new ArgumentException($"The {nameof(type)} is not known as a calculation Node");
            }
        }

        public async Task<ICalculationNode> LoadByIdAsync(int moduleId, CalculationNodeType type)
        {
            var connectionInfo = await _dbContext.Module
                .Where(x => x.Id == moduleId)
                .Select(x => new
                {
                    x.Id,
                    x.Type,
                    x.ConnectionString
                })
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (connectionInfo == null)
            {
                throw new DCPBadRequestException(ErrorMessages.UnkownModule);
            }

            if (connectionInfo.Type != type)
                return LoadByType(type);

            var key = GetEncryptionKey();
            var decryptedConnectionString = EncryptionHelper.DecryptConnectionString(connectionInfo.ConnectionString, key);

            switch (connectionInfo.Type)
            {
                case CalculationNodeType.OpenCPU:
                    return new OpenCPUCalculationNode(_configuration, connectionInfo.Id, decryptedConnectionString);
                case CalculationNodeType.Dataiku:
                    return new DataikuCalculationNode(_configuration, connectionInfo.Id, decryptedConnectionString);
                default:
                    throw new DCPBadRequestException(ErrorMessages.UnkownConnectionType);
            }
        }

        private string GetEncryptionKey()
        {
            var key = _configuration["Secrets:EncryptionKey"];

            if (string.IsNullOrEmpty(key))
            {
                throw new DCPInternalServerErrorException(ErrorMessages.EncryptionKeyInvalid);
            }

            return key;
        }
    }
}
