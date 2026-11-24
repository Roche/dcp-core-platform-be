using OpenDCP.Core.Core.ConnectionStringLoader;
using OpenDCP.Core.Core.Interfaces.Data;
using OpenDCP.Core.DAL;
using OpenDCP.Framework.Configuration.Model.Responses.Service;
using OpenDCP.Framework.Connections.Manager.Pipeline;
using OpenDCP.Framework.Connections.Manager.PluginManagement;
using OpenDCP.Framework.DataTypes.Equipment.Entities.EventframeContext;
using OpenDCP.Framework.DataTypes.Equipment.Interfaces;
using OpenDCP.Framework.DataTypes.Equipment.Requests.DataBundle;
using OpenDCP.Framework.DataTypes.Equipment.Requests.EventFrameContext;
using OpenDCP.Framework.DataTypes.Equipment.Requests.SensorData;
using OpenDCP.Framework.DataTypes.Equipment.Responses.EventFrameContext;
using OpenDCP.Framework.DataTypes.Equipment.Responses.SensorData;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace OpenDCP.Core.Core.Services.Data
{
    public class BundleDataService : BaseDataService, IBundleDataService
    {
        private readonly DbConnectionStringLoader _loader;

        public BundleDataService(IConfiguration configuration, DCPCoreDbContext dbContext, IServiceProvider serviceProvider, DatasourceCache cache)
            : base(configuration, dbContext, serviceProvider, cache)
        {
            _loader = new DbConnectionStringLoader(_dbContext, _configuration);
        }

        public async Task<ServiceResponse<SensorDataWithBatchContextResponse>> GetInterpolatedDataWithContextAsync(int siteId, string module, InterpolatedDataWithBatchContextQueryDTO model, Framework.Hosting.Models.UserContextDTO user, CancellationToken ct = default)
        {
            _loader.SetModule(module);

            var telemetryModel = new InterpolatedDataQueryDTO
            {
                StartTime = model.StartTime,
                EndTime = model.EndTime,
                Interval = model.Interval,
                WebId = model.WebId
            };

            var telemetryPipeline = new PluginPipeline<InterpolatedDataQueryDTO, SensorDataResponse>(_cache, _serviceProvider, _loader)
                .WithDefaultMapFunction()
                .WithDefaultTransformFunction()
                .WithDefaultReduceFunction();

            var telemetryReponse = await telemetryPipeline.RunMapReduceAsync<ISensorTelemetryDataSource>(
                (datasource, telemetryModel, ct) => datasource.GetInterpolatedDataAsync(telemetryModel, ct),
                telemetryModel
            );

            var eventframeModel = new BatchContextTimeQueryDTO
            {
                StartTime = model.StartTime,
                EndTime = model.EndTime,
                DeviceWebId = model.DeviceWebId
            };

            var eventframePipeline = new PluginPipeline<BatchContextTimeQueryDTO, BatchContextListResponse>(_cache, _serviceProvider, _loader)
                .WithDefaultMapFunction()
                .WithDefaultTransformFunction()
                .WithDefaultReduceFunction();

            var isRegistered = await eventframePipeline.ValidateDatasourceRegistration<IEventframeContextDataSource>(eventframeModel);

            BatchContextListResponse eventframeReponse = new BatchContextListResponse();

            if (isRegistered)
            {
                eventframeReponse = await eventframePipeline.RunMapReduceAsync<IEventframeContextDataSource>(
                    (datasource, eventframeModel, ct) => datasource.SearchBatchContextByTimeAsync(eventframeModel, ct),
                    eventframeModel
                );
            }
            else
            {
                eventframeReponse.Eventframes = new List<BatchContextDTO>();
            }

            var result = new ServiceResponse<SensorDataWithBatchContextResponse>
            {
                Data = new SensorDataWithBatchContextResponse
                {
                    SensorData = telemetryReponse.Data,
                    Errors = telemetryReponse.Errors,
                    Warnings = telemetryReponse.Warnings,
                    BatchData = eventframeReponse.Eventframes
                .GroupBy(x => x.DeviceWebId)
                .Select(x => new BatchDataDTO
                {
                    DeviceWebId = x.Key,
                    EventFrames = x.Select(ef => new BatchContextDTO
                    {
                        Id = ef.Id,
                        DeviceWebId = x.Key,
                        StartTime = ef.StartTime,
                        EndTime = ef.EndTime,
                    }).ToList()
                }).ToList()
                },
                DatasourceValidation = eventframePipeline.GetValidationStatus() && telemetryPipeline.GetValidationStatus()
            };

            return result;
        }

        public async Task<ServiceResponse<SensorDataWithBatchContextResponse>> GetPlotDataWithContextAsync(int siteId, string module, PlotDataWithBatchContextQueryDTO model, Framework.Hosting.Models.UserContextDTO user, CancellationToken ct = default)
        {
            _loader.SetModule(module);

            var telemetryModel = new PlotDataQueryDTO
            {
                StartTime = model.StartTime,
                EndTime = model.EndTime,
                Intervals = model.Intervals,
                WebId = model.WebId
            };

            var telemetryPipeline = new PluginPipeline<PlotDataQueryDTO, SensorDataResponse>(_cache, _serviceProvider, _loader)
                .WithDefaultMapFunction()
                .WithDefaultTransformFunction()
                .WithDefaultReduceFunction();

            var telemetryReponse = await telemetryPipeline.RunMapReduceAsync<ISensorTelemetryDataSource>(
                (datasource, telemetryModel, ct) => datasource.GetPlotDataAsync(telemetryModel, ct),
                telemetryModel
            );

            var eventframeModel = new BatchContextTimeQueryDTO
            {
                StartTime = model.StartTime,
                EndTime = model.EndTime,
                DeviceWebId = model.DeviceWebId
            };

            var eventframePipeline = new PluginPipeline<BatchContextTimeQueryDTO, BatchContextListResponse>(_cache, _serviceProvider, _loader)
                .WithDefaultMapFunction()
                .WithDefaultTransformFunction()
                .WithDefaultReduceFunction();

            var isRegistered = await eventframePipeline.ValidateDatasourceRegistration<IEventframeContextDataSource>(eventframeModel);

            BatchContextListResponse eventframeReponse = new BatchContextListResponse();

            if (isRegistered)
            {
                eventframeReponse = await eventframePipeline.RunMapReduceAsync<IEventframeContextDataSource>(
                    (datasource, eventframeModel, ct) => datasource.SearchBatchContextByTimeAsync(eventframeModel, ct),
                    eventframeModel
                );
            }
            else
            {
                eventframeReponse.Eventframes = new List<BatchContextDTO>();
            }

            var result = new ServiceResponse<SensorDataWithBatchContextResponse>
            {
                Data = new SensorDataWithBatchContextResponse
                {
                    SensorData = telemetryReponse.Data,
                    Errors = telemetryReponse.Errors,
                    Warnings = telemetryReponse.Warnings,
                    BatchData = eventframeReponse.Eventframes
                                .GroupBy(x => x.DeviceWebId)
                                .Select(x => new BatchDataDTO
                                {
                                    DeviceWebId = x.Key,
                                    EventFrames = x.Select(ef => new BatchContextDTO
                                    {
                                        Id = ef.Id,
                                        DeviceWebId = x.Key,
                                        StartTime = ef.StartTime,
                                        EndTime = ef.EndTime,
                                    }).ToList()
                                }).ToList()
                },
                DatasourceValidation = eventframePipeline.GetValidationStatus() && telemetryPipeline.GetValidationStatus()
            };

            return result;
        }
    }
}
