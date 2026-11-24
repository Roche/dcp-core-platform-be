using OpenDCP.Core.Core.ConnectionStringLoader;
using OpenDCP.Core.Core.Interfaces.Data;
using OpenDCP.Core.DAL;
using OpenDCP.Framework.Configuration.Model.Responses.Service;
using OpenDCP.Framework.Connections.Manager.Pipeline;
using OpenDCP.Framework.Connections.Manager.PluginManagement;
using OpenDCP.Framework.DataTypes.Equipment.Interfaces;
using OpenDCP.Framework.DataTypes.Equipment.Requests.SensorData;
using OpenDCP.Framework.DataTypes.Equipment.Responses.SensorData;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OpenDCP.Core.Core.Services.Data
{
    public class SensorTelemetryDataService : BaseDataService, ISensorTelemetryDataService
    {
        private readonly DbConnectionStringLoader _loader;

        public SensorTelemetryDataService(IConfiguration configuration, DCPCoreDbContext dbContext, IServiceProvider serviceProvider, DatasourceCache cache)
            : base(configuration, dbContext, serviceProvider, cache)
        {
            _loader = new DbConnectionStringLoader(_dbContext, _configuration);
        }

        public async Task<ServiceResponse<SensorDataResponse>> GetInterpolatedDataAsync(int siteId, string module, InterpolatedDataQueryDTO model, Framework.Hosting.Models.UserContextDTO user, CancellationToken ct = default)
        {
            _loader.SetModule(module);

            var pipeline = new PluginPipeline<InterpolatedDataQueryDTO, SensorDataResponse>(_cache, _serviceProvider, _loader)
                .WithDefaultMapFunction()
                .WithDefaultTransformFunction()
                .WithDefaultReduceFunction();

            var response = await pipeline.RunMapReduceAsync<ISensorTelemetryDataSource>(
                (datasource, model, ct) => datasource.GetInterpolatedDataAsync(model, ct),
                model
            );

            var result = new ServiceResponse<SensorDataResponse>
            {
                Data = response,
                DatasourceValidation = pipeline.GetValidationStatus()
            };

            return result;
        }

        public async Task<ServiceResponse<SensorDataResponse>> GetInterpolatedDataAtTimesAsync(int siteId, string module, InterpolatedAtTimeDataQueryDTO model, Framework.Hosting.Models.UserContextDTO user, CancellationToken ct = default)
        {
            _loader.SetModule(module);

            var pipeline = new PluginPipeline<InterpolatedAtTimeDataQueryDTO, SensorDataResponse>(_cache, _serviceProvider, _loader)
                .WithDefaultMapFunction()
                .WithDefaultTransformFunction()
                .WithDefaultReduceFunction();

            var response = await pipeline.RunMapReduceAsync<ISensorTelemetryDataSource>(
                (datasource, model, ct) => datasource.GetInterpolatedDataAtTimesAsync(model, ct),
                model
            );

            var result = new ServiceResponse<SensorDataResponse>
            {
                Data = response,
                DatasourceValidation = pipeline.GetValidationStatus()
            };

            return result;
        }

        public async Task<ServiceResponse<SensorDataResponse>> GetPlotDataAsync(int siteId, string module, PlotDataQueryDTO model, Framework.Hosting.Models.UserContextDTO user, CancellationToken ct = default)
        {
            //Validations
            if (model.BatchStartTime == null)
            {
                model.BatchStartTime = model.StartTime;
            }

            _loader.SetModule(module);

            var pipeline = new PluginPipeline<PlotDataQueryDTO, SensorDataResponse>(_cache, _serviceProvider, _loader)
                .WithDefaultMapFunction()
                .WithDefaultTransformFunction()
                .WithDefaultReduceFunction();

            var response = await pipeline.RunMapReduceAsync<ISensorTelemetryDataSource>(
                (datasource, model, ct) => datasource.GetPlotDataAsync(model, ct),
                model
            );

            var result = new ServiceResponse<SensorDataResponse>
            {
                Data = response,
                DatasourceValidation = pipeline.GetValidationStatus()
            };

            return result;
        }

        public async Task<ServiceResponse<SensorDataResponse>> GetRecordedDataAsync(int siteId, string module, RecordedDataQueryDTO model, Framework.Hosting.Models.UserContextDTO user, CancellationToken ct = default)
        {
            _loader.SetModule(module);

            var pipeline = new PluginPipeline<RecordedDataQueryDTO, SensorDataResponse>(_cache, _serviceProvider, _loader)
                .WithDefaultMapFunction()
                .WithDefaultTransformFunction()
                .WithDefaultReduceFunction();

            var response = await pipeline.RunMapReduceAsync<ISensorTelemetryDataSource>(
                (datasource, model, ct) => datasource.GetRecordedDataAsync(model, ct),
                model
            );

            var result = new ServiceResponse<SensorDataResponse>
            {
                Data = response,
                DatasourceValidation = pipeline.GetValidationStatus()
            };

            return result;
        }

        public async Task<ServiceResponse<SensorDataResponse>> GetRecordedDataAtTimesAsync(int siteId, string module, RecordedAtTimeDataQueryDTO model, Framework.Hosting.Models.UserContextDTO user, CancellationToken ct = default)
        {
            _loader.SetModule(module);

            var pipeline = new PluginPipeline<RecordedAtTimeDataQueryDTO, SensorDataResponse>(_cache, _serviceProvider, _loader)
                .WithDefaultMapFunction()
                .WithDefaultTransformFunction()
                .WithDefaultReduceFunction();

            var response = await pipeline.RunMapReduceAsync<ISensorTelemetryDataSource>(
                (datasource, model, ct) => datasource.GetRecordedDataAtTimesAsync(model, ct),
                model
            );

            var result = new ServiceResponse<SensorDataResponse>
            {
                Data = response,
                DatasourceValidation = pipeline.GetValidationStatus()
            };

            return result;
        }
    }
}
