using OpenDCP.Core.Core.ConnectionStringLoader;
using OpenDCP.Core.Core.Interfaces;
using OpenDCP.Core.DAL;
using OpenDCP.Core.Models.Models.Configuration;
using OpenDCP.Core.Models.Models.ConnectionBridge;
using OpenDCP.Framework.Configuration.Model.Constants.Datasource;
using OpenDCP.Framework.Configuration.Model.Responses.Service;
using OpenDCP.Framework.Connections.Manager.Models;
using OpenDCP.Framework.Connections.Manager.Pipeline;
using OpenDCP.Framework.Connections.Manager.PluginManagement;
using OpenDCP.Framework.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace OpenDCP.Core.Core.Services
{
    public class ConnectionBridgeService : IConnectionBridgeService
    {
        protected readonly IConfiguration _configuration;
        protected readonly DCPCoreDbContext _dbContext;
        protected IServiceProvider _serviceProvider;
        protected DatasourceCache _cache;
        private readonly DbConnectionStringLoader _loader;

        public ConnectionBridgeService(IConfiguration configuration, DCPCoreDbContext dbContext, DatasourceCache cache,
            IServiceProvider serviceProvider)
        {
            _configuration = configuration;
            _serviceProvider = serviceProvider;
            _cache = cache;
            _dbContext = dbContext;
            _loader = new DbConnectionStringLoader(_dbContext, _configuration);
        }

        public async Task<List<ConnectionBridgeConfigurationDTO>> SearchConnectionBridgesAsync(ConnectionBridgeQueryDTO model)
        {
            var serializeOptions = new JsonSerializerOptions
            {
                IncludeFields = true
            };

            var bridges = await QueryConnectionBridges(
                model.SiteId,
                model.SConnectionId, model.SDataType,
                model.TConnectionId, model.TDataType
            );

            return bridges.Select(x => new ConnectionBridgeConfigurationDTO
            {
                SConId = x.SConId,
                TConId = x.TConId,
                Strategy = (byte)x.Strategy,
                Definition = JsonSerializer.SerializeToNode(
                    x.Definition,
                    x.Definition!.GetType(),
                    serializeOptions
                )!.AsObject(),
                IsGxP = x.IsGxP
            }).ToList();
        }

        public async Task<ServiceResponse<TranslationResponse>> TranslateEventsAsync(TranslationQueryDTO model, int siteId)
        {
            var bridges = await QueryConnectionBridges(
                siteId,
                model.SConnectionId, model.SDataType,
                model.TConnectionId, model.TDataType
            );

            var translatePipeline = new EventTranslationPipeline(_cache, _serviceProvider, _loader);

            var dict = await translatePipeline.TranslateForward<TranslationQueryDTO>(
               siteId: siteId,
               sDataType: model.SDataType.Value,
               tDataType: model.TDataType.Value,
               model
            );

            var result = new ServiceResponse<TranslationResponse>
            {
                Data = new TranslationResponse
                {
                    TranslationDictonary = dict.Item2
                        .SelectMany(kvp => kvp.Value.Select(innerVal => new { NewKey = innerVal, NewValue = kvp.Key }))
                        .GroupBy(x => x.NewKey)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(x => x.NewValue).ToList()
                        )
                },
                DatasourceValidation = translatePipeline.GetValidationStatus()
            };

            return result;
        }

        public async Task<ServiceResponse<TranslationResponse>> TranslateIdentifersAsync(TranslationQueryDTO model, int siteId)
        {
            var bridges = await QueryConnectionBridges(
               siteId,
               model.SConnectionId, model.SDataType,
               model.TConnectionId, model.TDataType
           );

            var translatePipeline = new EventTranslationPipeline(_cache, _serviceProvider, _loader);
            //int siteId, DataType sDataType, DataType tDataType, TRequest mode
            var dict = await translatePipeline.TranslateForward<TranslationQueryDTO>(
               siteId: siteId,
               sDataType: model.SDataType.Value,
               tDataType: model.TDataType.Value,
               model
           );

            var result = new ServiceResponse<TranslationResponse>
            {
                Data = new TranslationResponse
                {
                    TranslationDictonary = dict.Item2
                        .SelectMany(kvp => kvp.Value.Select(innerVal => new { NewKey = innerVal, NewValue = kvp.Key }))
                        .GroupBy(x => x.NewKey)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(x => x.NewValue).ToList()
                        )
                },
                DatasourceValidation = translatePipeline.GetValidationStatus()
            };

            return result;
        }

        private async Task<List<ConnectionBridgeDTO>> QueryConnectionBridges(int siteId,
            int? sConnectionId, DataType? sDataType, int? tConnectionId, DataType? tDataType)
        {
            var query = _dbContext.ConnectionBridge.Where(x => x.SCon.SiteId == siteId);

            if (sConnectionId is null && sDataType is null)
            {
                throw new DCPBadRequestException("No source domain specification");
            }
            else if (sConnectionId.HasValue)
            {
                query = query.Where(x => x.SConId == sConnectionId.Value);
            }
            else
            {
                query = query.Where(x => x.SConDataType == (byte)sDataType.Value!);
            }

            if (tConnectionId is null && tDataType is null)
            {
                throw new DCPBadRequestException("No target domain specification");
            }
            else if (tConnectionId.HasValue)
            {
                query = query.Where(x => x.TConId == tConnectionId.Value);
            }
            else
            {
                query = query.Where(x => x.TConDataType == (byte)tDataType.Value!);
            }

            var bridges = await query
               .AsNoTracking()
               .Select(x => new ConnectionBridgeDTO
               {
                   Strategy = x.Strategy,
                   SConId = x.SConId,
                   Definition = x.Definition,
                   IsGxP = x.SCon.IsValidated && x.TCon.IsValidated,
                   TConId = x.TConId,
               })
               .ToListAsync();

            return bridges;
        }
    }
}
