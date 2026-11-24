using OpenDCP.Core.Core.ConnectionStringLoader;
using OpenDCP.Core.Core.Interfaces.Data;
using OpenDCP.Core.DAL;
using OpenDCP.Framework.Configuration.Model.Responses.Service;
using OpenDCP.Framework.Connections.Manager.Models;
using OpenDCP.Framework.Connections.Manager.Pipeline;
using OpenDCP.Framework.Connections.Manager.PluginManagement;
using OpenDCP.Framework.DataTypes.Equipment.Interfaces;
using OpenDCP.Framework.DataTypes.Equipment.Requests.EventFrameContext;
using OpenDCP.Framework.DataTypes.Equipment.Responses.EventFrameContext;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OpenDCP.Core.Core.Services.Data
{
    public class EventframeContextDataService : BaseDataService, IEventframeContextDataService
    {
        private readonly DbConnectionStringLoader _loader;

        public EventframeContextDataService(IConfiguration configuration, DCPCoreDbContext dbContext, IServiceProvider serviceProvider, DatasourceCache cache)
            : base(configuration, dbContext, serviceProvider, cache)
        {
            _loader = new DbConnectionStringLoader(_dbContext, _configuration);
        }

        public async Task<ServiceResponse<EventFrameResponse>> GetEventframeAsync(int siteId, string module, string id, Framework.Hosting.Models.UserContextDTO user, CancellationToken ct = default)
        {
            _loader.SetModule(module);

            var pipeline = new PluginPipeline<RequestContainer, EventFrameResponse>(_cache, _serviceProvider, _loader)
                .WithDefaultMapFunction()
                .WithDefaultTransformFunction()
                .WithDefaultReduceFunction();

            var response = await pipeline.RunMapReduceAsync<IEventframeContextDataSource>(
                async (datasource, args, ct) => await datasource.GetEventframeAsync((string)args[0], ct),
                mapArgIndex: 0,
                args: new object[] { id }
            );

            var result = new ServiceResponse<EventFrameResponse>
            {
                Data = response,
                DatasourceValidation = pipeline.GetValidationStatus()
            };

            return result;
        }

        public async Task<ServiceResponse<EventframeAttributesResponse>> GetEventframeAttributesAsync(int siteId, string module, string deviceWebId, Framework.Hosting.Models.UserContextDTO user, CancellationToken ct = default)
        {
            _loader.SetModule(module);

            var pipeline = new PluginPipeline<RequestContainer, EventframeAttributesResponse>(_cache, _serviceProvider, _loader)
                .WithDefaultMapFunction()
                .WithDefaultTransformFunction()
                .WithDefaultReduceFunction();

            var response = await pipeline.RunMapReduceAsync<IEventframeContextDataSource>(
                async (datasource, args, ct) => await datasource.GetEventframeAttributesAsync((string)args[0], ct),
                mapArgIndex: 0,
                args: new object[] { deviceWebId }
            );

            var result = new ServiceResponse<EventframeAttributesResponse>
            {
                Data = response,
                DatasourceValidation = pipeline.GetValidationStatus()
            };

            return result;
        }

        public async Task<ServiceResponse<EventframeValuesResponse>> GetEventframeValuesAsync(int siteId, string module, string id, Framework.Hosting.Models.UserContextDTO user, CancellationToken ct = default)
        {
            _loader.SetModule(module);

            var pipeline = new PluginPipeline<RequestContainer, EventframeValuesResponse>(_cache, _serviceProvider, _loader)
                .WithDefaultMapFunction()
                .WithDefaultTransformFunction()
                .WithDefaultReduceFunction();

            var response = await pipeline.RunMapReduceAsync<IEventframeContextDataSource>(
                async (datasource, args, ct) => await datasource.GetEventframeValuesAsync((string)args[0], ct),
                mapArgIndex: 0,
                args: new object[] { id }
            );

            var result = new ServiceResponse<EventframeValuesResponse>
            {
                Data = response,
                DatasourceValidation = pipeline.GetValidationStatus()
            };

            return result;
        }

        public async Task<ServiceResponse<BatchContextListResponse>> SearchBatchContextByTimeAsync(int siteId, string module, BatchContextTimeQueryDTO model, Framework.Hosting.Models.UserContextDTO user, CancellationToken ct = default)
        {
            _loader.SetModule(module);

            var pipeline = new PluginPipeline<BatchContextTimeQueryDTO, BatchContextListResponse>(_cache, _serviceProvider, _loader)
                .WithDefaultMapFunction()
                .WithDefaultTransformFunction()
                .WithDefaultReduceFunction();

            var response = await pipeline.RunMapReduceAsync<IEventframeContextDataSource>(
                (datasource, model, ct) => datasource.SearchBatchContextByTimeAsync(model, ct),
                model
            );

            var result = new ServiceResponse<BatchContextListResponse>
            {
                Data = response,
                DatasourceValidation = pipeline.GetValidationStatus()
            };

            return result;
        }

        public async Task<ServiceResponse<EventframeListResponse>> SearchEventframeContextByIdAsync(int siteId, string module, EventframeContextIdQueryDTO model, Framework.Hosting.Models.UserContextDTO user, CancellationToken ct = default)
        {
            _loader.SetModule(module);

            var pipeline = new PluginPipeline<EventframeContextIdQueryDTO, EventframeListResponse>(_cache, _serviceProvider, _loader)
                .WithDefaultMapFunction()
                .WithDefaultTransformFunction()
                .WithDefaultReduceFunction();

            var response = await pipeline.RunMapReduceAsync<IEventframeContextDataSource>(
                (datasource, model, ct) => datasource.SearchEventframeContextByIdAsync(model, ct),
                model
            );

            var result = new ServiceResponse<EventframeListResponse>
            {
                Data = response,
                DatasourceValidation = pipeline.GetValidationStatus()
            };

            return result;
        }

        public async Task<ServiceResponse<EventframeListResponse>> SearchEventframeContextByTimeAsync(int siteId, string module, EventframeContextTimeQueryDTO model, Framework.Hosting.Models.UserContextDTO user, CancellationToken ct = default)
        {
            _loader.SetModule(module);

            var pipeline = new PluginPipeline<EventframeContextTimeQueryDTO, EventframeListResponse>(_cache, _serviceProvider, _loader)
                .WithDefaultMapFunction()
                .WithDefaultTransformFunction()
                .WithDefaultReduceFunction();

            var response = await pipeline.RunMapReduceAsync<IEventframeContextDataSource>(
                (datasource, model, ct) => datasource.SearchEventframeContextByTimeAsync(model, ct),
                model
            );

            var result = new ServiceResponse<EventframeListResponse>
            {
                Data = response,
                DatasourceValidation = pipeline.GetValidationStatus()
            };

            return result;
        }
    }
}
