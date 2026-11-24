using OpenDCP.Core.DAL;
using OpenDCP.Core.Models.Constants;
using OpenDCP.Framework.Configuration.Model.Constants.Datasource;
using OpenDCP.Framework.Connections.Manager.Interfaces;
using OpenDCP.Framework.Connections.Manager.Models;
using OpenDCP.Framework.Exceptions;
using OpenDCP.Framework.Security.Classes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenDCP.Core.Core.ConnectionStringLoader
{
    public class DbConnectionStringLoader : IConnectionStringLoader
    {
        protected readonly DCPCoreDbContext _dbContext;
        protected readonly IConfiguration _configuration;
        protected string? _module;
        public DbConnectionStringLoader(DCPCoreDbContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _module = null;
        }

        public async Task<List<ConnectionStringTransferDTO>> Load(List<int> datasourceIds)
        {
            var dbStrings = await _dbContext.DataSource
                .Where(ds => datasourceIds.Contains(ds.Id))
                .Select(x => new
                 {
                     x.ConnectionString,
                     x.Type,
                     x.Id,
                     x.IsValidated
                 })
                 .AsNoTracking()
                 .ToListAsync();

            if (dbStrings == null)
            {
                throw new DCPBadRequestException("Combination not configured");
            }

            var key = _configuration["Secrets:EncryptionKey"];

            if (string.IsNullOrEmpty(key))
            {
                throw new DCPInternalServerErrorException(ErrorMessages.EncryptionKeyInvalid);
            }

            var result = dbStrings.Select(x => new ConnectionStringTransferDTO
            {
                ConnectionString = EncryptionHelper.DecryptConnectionString(x.ConnectionString, key),
                ConnectionType = x.Type,
                DatasourceId = x.Id,
                IsGxP = x.IsValidated
            }).ToList();

            return result;
        }

        public async Task<List<ConnectionStringTransferDTO>> Load(int siteId, DataType dataType)
        {
            if (string.IsNullOrEmpty(_module))
            {
                throw new DCPBadRequestException("Module name is missing");
            }

            var dbStrings = await _dbContext.DataSource
                .Where(ds => ds.SiteId == siteId
                            && ds.DataTypes.Any(dt => dt.Type == dataType && dt.ModuleName == _module)
                 ).Select(x => new 
                 {
                     x.ConnectionString,
                     x.Type,
                     x.Id,
                     x.IsValidated
                 })
                 .AsNoTracking()
                 .ToListAsync();

            if (dbStrings == null)
            {
                throw new DCPBadRequestException("Combination not configured");
            }

            var key = _configuration["Secrets:EncryptionKey"];

            if (string.IsNullOrEmpty(key))
            {
                throw new DCPInternalServerErrorException(ErrorMessages.EncryptionKeyInvalid);
            }

            var result = dbStrings.Select(x => new ConnectionStringTransferDTO
            {
                ConnectionString = EncryptionHelper.DecryptConnectionString(x.ConnectionString, key),
                ConnectionType = x.Type,
                DatasourceId= x.Id,
                IsGxP = x.IsValidated
            }).ToList();

            return result;
        }

        public async Task<List<ConnectionBridgeDTO>> LoadConnectionBridges(int siteId, DataType sConDataType, DataType tConDataType)
        {
            var dbStrings = await _dbContext.ConnectionBridge
                .Where(x => x.TConDataType == (byte)tConDataType
                            && x.SConDataType == (byte)sConDataType
                            && x.SCon.SiteId == siteId
                     )
                .Select(x => new 
                {
                    Defintion = x.Definition,
                    SConId = x.SConId,
                    Strategy = x.Strategy,
                    TConId = x.TConId,
                    IsGxP = x.SCon.IsValidated && x.TCon.IsValidated
                })
                .AsNoTracking()
                .ToListAsync();

            return dbStrings
                .Where(x => x.Defintion != null)
                .Select(x => new ConnectionBridgeDTO { 
                    Definition = x.Defintion!,
                    SConId = x.SConId,
                    Strategy = x.Strategy,
                    TConId = x.TConId,
                    IsGxP = x.IsGxP
                })
                .ToList();

        }

        public void SetModule(string module)
        {
            _module = module;
        }
    }
}
