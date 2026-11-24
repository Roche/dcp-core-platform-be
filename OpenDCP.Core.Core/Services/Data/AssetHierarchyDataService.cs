using OpenDCP.Core.Core.ConnectionStringLoader;
using OpenDCP.Core.Core.Interfaces.Data;
using OpenDCP.Core.DAL;
using OpenDCP.Framework.Configuration.Model.Constants.Datasource;
using OpenDCP.Framework.Configuration.Model.Responses.Service;
using OpenDCP.Framework.Connections.Manager.Helpers;
using OpenDCP.Framework.Connections.Manager.Models;
using OpenDCP.Framework.Connections.Manager.Pipeline;
using OpenDCP.Framework.Connections.Manager.PluginManagement;
using OpenDCP.Framework.DataTypes.Equipment.Adapters;
using OpenDCP.Framework.DataTypes.Equipment.Entities.AssetHierarchy;
using OpenDCP.Framework.DataTypes.Equipment.Interfaces;
using OpenDCP.Framework.DataTypes.Equipment.Requests.AssetHierarchy;
using OpenDCP.Framework.DataTypes.Equipment.Responses.AssetHierarchy;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OpenDCP.Core.Core.Services.Data
{
    public class AssetHierarchyDataService : BaseDataService, IAssetHierarchyDataService
    {
        private readonly DbConnectionStringLoader _loader;

        public AssetHierarchyDataService(IConfiguration configuration, DCPCoreDbContext dbContext, IServiceProvider serviceProvider, DatasourceCache cache)
            : base(configuration, dbContext, serviceProvider, cache)
        {
            _loader = new DbConnectionStringLoader(_dbContext, _configuration);
        }

        public async Task<ServiceResponse<DeviceListResponse>> SearchDevicesAsync(int siteId, string module, DeviceQueryDTO model, Framework.Hosting.Models.UserContextDTO user, CancellationToken ct = default)
        {
            _loader.SetModule(module);

            var pipeline = new PluginPipeline<DeviceQueryDTO, DeviceListResponse>(_cache, _serviceProvider, _loader)
                .WithMapOnAll(
                    siteId: siteId,
                    dataType: DataType.AssetHierarchy
                 )
                .WithDefaultTransformFunction()
                .WithDefaultReduceFunction();

            var response = await pipeline.RunMapReduceAsync<IAssetHierarchyDataSource>(
                (datasource, model, ct) => datasource.SearchDevicesAsync(model, ct),
                model
            );

            var result = new ServiceResponse<DeviceListResponse>
            {
                Data = response,
                DatasourceValidation = pipeline.GetValidationStatus()
            };

            return result;
        }

        public async Task<ServiceResponse<SensorListResponse>> GetDeviceSensorsAsync(int siteId, string module, string webId, Framework.Hosting.Models.UserContextDTO user, string? groupName, CancellationToken ct = default)
        {
            _loader.SetModule(module);

            var pipeline = new PluginPipeline<RequestContainer, SensorListResponse>(_cache, _serviceProvider, _loader)
                        .WithDefaultMapFunction()
                        .WithDefaultTransformFunction()
                        .WithDefaultReduceFunction();

#pragma warning disable CS8601 //due to non-nullable interface defintions in netstandard2.0
            var response = await pipeline.RunMapReduceAsync<IAssetHierarchyDataSource>(
                (datasource, args, ct) => datasource.GetDeviceSensorsAsync(
                    args[0] as string ?? throw new ArgumentNullException(nameof(args)),
                    args.Length > 1 ? args[1] as string : null
                ),
                mapArgIndex: 0,
                args: new object[] { webId, groupName }
            );
#pragma warning restore CS8601

            var result = new ServiceResponse<SensorListResponse>
            {
                Data = response,
                DatasourceValidation = pipeline.GetValidationStatus()
            };

            return result;
        }

        public async Task<ServiceResponse<GroupedSensorListResponse>> GetGroupedDeviceSensorsAsync(int siteId, string module, string webId, Framework.Hosting.Models.UserContextDTO user, CancellationToken ct = default)
        {
            _loader.SetModule(module);

            var pipeline = new PluginPipeline<RequestContainer, GroupedSensorListResponse>(_cache, _serviceProvider, _loader)
                        .WithDefaultMapFunction()
                        .WithDefaultTransformFunction()
                        .WithDefaultReduceFunction();

            var response = await pipeline.RunMapReduceAsync<IAssetHierarchyDataSource>(
                (datasource, args, ct) => datasource.GetGroupedDeviceSensorsAsync((string)args[0]),
                mapArgIndex: 0,
                args: new object[] { webId }
            );

            var result = new ServiceResponse<GroupedSensorListResponse>
            {
                Data = response,
                DatasourceValidation = pipeline.GetValidationStatus()
            };

            return result;
        }

        public async Task<ServiceResponse<ElementMetaDataResponse>> GetElementMetaDataAsync(int siteId, string module, string webId, Framework.Hosting.Models.UserContextDTO user, CancellationToken ct = default)
        {
            _loader.SetModule(module);

            var pipeline = new PluginPipeline<RequestContainer, ElementMetaDataResponse>(_cache, _serviceProvider, _loader)
                .WithDefaultMapFunction()
                .WithDefaultTransformFunction()
                .WithDefaultReduceFunction();

            var response = await pipeline.RunMapReduceAsync<IAssetHierarchyDataSource>(
                async (datasource, args, ct) => await datasource.GetElementMetaDataAsync((string)args[0]),
                mapArgIndex: 0,
                args: new object[] { webId }
            );

            var result = new ServiceResponse<ElementMetaDataResponse>
            {
                Data = response,
                DatasourceValidation = pipeline.GetValidationStatus()
            };

            return result;
        }

        public async Task<ServiceResponse<GlobalFilterListResponse>> GetGlobalFilterAsync(int siteId, string module, string? link, Framework.Hosting.Models.UserContextDTO user, CancellationToken ct = default)
        {
            _loader.SetModule(module);

            var result = new ServiceResponse<GlobalFilterListResponse>();


            //need to find the roots, and sort
            if (link is null)
            {
                var bridges = await _loader.LoadConnectionBridges(siteId, DataType.AssetHierarchy, DataType.AssetHierarchy);
                int datasourceId;

                if (bridges.Count > 0)
                {
                    datasourceId = bridges.Select(x => x.SConId).Except(bridges.Select(x => x.TConId)).First();
                }
                else
                {
                    var con = await _loader.Load(siteId, DataType.AssetHierarchy);
                    datasourceId = con[0].DatasourceId;
                }

                var groups = new List<ConnectionContext<RequestContainer>>
                {
                    new ConnectionContext<RequestContainer>
                    {
                        DatasourceId = datasourceId,
                        Request = new RequestContainer
                        {
                            Args = null,
                            MapArgs = null
                        }
                    }
                };

                var rootPipeline = new PluginPipeline<RequestContainer, GlobalFilterListResponse>(_cache, _serviceProvider, _loader)
                   .WithDefaultMapFunction()
                   .WithDefaultTransformFunction()
                   .WithDefaultReduceFunction();

                var rresponse = await rootPipeline.RunReduceAsync<IAssetHierarchyDataSource>(
                    async (datasource, args, ct) => await datasource.GetGlobalFilterAsync(null, ct),
                    groups
                );

                result.Data = rresponse;
                result.DatasourceValidation = rootPipeline.GetValidationStatus();

            }
            else
            {
                var pipeline = new GraphMergePipeline(_cache, _serviceProvider, _loader);

                var response = await pipeline.RunPipeline<IAssetHierarchyDataSource, GlobalFilterElementDTO, GlobalFilterListResponse>(
                    siteId: siteId,
                    dataType: DataType.AssetHierarchy,
                    mapArgIndex: 0,
                    plugin => new TreeNodeFromGlobalFilter(plugin),
                    async (plugin, args, ct) =>
                    {
                        var lnk = (string)args[0];
                        var parts = lnk.Split(new[] { '/' }, 2, StringSplitOptions.None);
                        var decoded = WebIdEncoding.Decode(parts[0]).WebId;
                        var normalizedLink = $"{decoded}/{parts[1]}";

                        return await plugin.GetGlobalFilterAsync(normalizedLink, ct);
                    },
                    args: new object[] { link }
                );

                result.Data = response;
                result.DatasourceValidation = pipeline.GetValidationStatus();
            }

            return result;
        }

        public async Task<ServiceResponse<GlobalFilterResponse>> GetGlobalFilterElementAsync(int siteId, string module, string webId, Framework.Hosting.Models.UserContextDTO user, CancellationToken ct = default)
        {
            _loader.SetModule(module);

            var pipeline = new GraphMergePipeline(_cache, _serviceProvider, _loader);

            var response = await pipeline.RunPipeline<IAssetHierarchyDataSource, GlobalFilterElementDTO, GlobalFilterResponse>(
                siteId: siteId,
                dataType: DataType.AssetHierarchy,
                mapArgIndex: 0,
                plugin => new TreeNodeFromGlobalFilter(plugin),
                async (plugin, args, ct) =>
                {
                    var decoded = WebIdEncoding.Decode((string)args[0]).WebId;

                    return await plugin.GetGlobalFilterElementAsync(decoded, ct);
                },
                args: new object[] { webId }
            );

            var result = new ServiceResponse<GlobalFilterResponse>
            {
                Data = response,
                DatasourceValidation = pipeline.GetValidationStatus()
            };

            return result;
        }

        public async Task<ServiceResponse<GlobalFilterListResponse>> GetGlobalFilterBackwardsAsync(int siteId, string module, string webId, Framework.Hosting.Models.UserContextDTO user, CancellationToken ct = default)
        {
            _loader.SetModule(module);

            var pipeline = new GraphMergePipeline(_cache, _serviceProvider, _loader);

            var response = await pipeline.RunPipeline<IAssetHierarchyDataSource, GlobalFilterElementDTO, GlobalFilterListResponse>(
                siteId: siteId,
                dataType: DataType.AssetHierarchy,
                mapArgIndex: 0,
                plugin => new TreeNodeFromGlobalFilter(plugin),
                async (plugin, args, ct) =>
                {
                    var decoded = WebIdEncoding.Decode((string)args[0]).WebId;

                    return await plugin.GetGlobalFilterBackwardsAsync(decoded, ct);
                },
                args: new object[] { webId }
            );

            var result = new ServiceResponse<GlobalFilterListResponse>
            {
                Data = response,
                DatasourceValidation = pipeline.GetValidationStatus()
            };

            return result;
        }
        public async Task<ServiceResponse<GlobalFilterResponse>> GetPreviousGlobalFilterAsync(int siteId, string module, string link, Framework.Hosting.Models.UserContextDTO user, CancellationToken ct = default)
        {
            _loader.SetModule(module);

            var pipeline = new GraphMergePipeline(_cache, _serviceProvider, _loader);

            var response = await pipeline.RunPipeline<IAssetHierarchyDataSource, GlobalFilterElementDTO, GlobalFilterResponse>(
                siteId: siteId,
                dataType: DataType.AssetHierarchy,
                mapArgIndex: 0,
                plugin => new TreeNodeFromGlobalFilter(plugin),
                async (plugin, args, ct) =>
                {
                    var lnk = (string)args[0];
                    var parts = lnk.Split(new[] { '/' }, 2, StringSplitOptions.None);
                    var decoded = WebIdEncoding.Decode(parts[0]).WebId;
                    var normalizedLink = $"{decoded}/{parts[1]}";

                    return await plugin.GetPreviousGlobalFilterAsync(normalizedLink, ct);
                },
                args: new object[] { link }
            );

            var result = new ServiceResponse<GlobalFilterResponse>
            {
                Data = response,
                DatasourceValidation = pipeline.GetValidationStatus()
            };

            return result;
        }

        public async Task<ServiceResponse<DeviceResponse>> GetSingleDeviceAsync(int siteId, string module, string webId, Framework.Hosting.Models.UserContextDTO user, CancellationToken ct = default)
        {
            _loader.SetModule(module);

            var pipeline = new PluginPipeline<RequestContainer, DeviceResponse>(_cache, _serviceProvider, _loader)
                .WithDefaultMapFunction()
                .WithDefaultTransformFunction()
                .WithDefaultReduceFunction();

            var response = await pipeline.RunMapReduceAsync<IAssetHierarchyDataSource>(
                async (datasource, args, ct) => await datasource.GetSingleDeviceAsync((string)args[0], ct),
                mapArgIndex: 0,
                args: new object[] { webId }
            );

            var result = new ServiceResponse<DeviceResponse>
            {
                Data = response,
                DatasourceValidation = pipeline.GetValidationStatus()
            };

            return result;
        }

        public async Task<ServiceResponse<SensorResponse>> GetSingleSensorAsync(int siteId, string module, string webId, Framework.Hosting.Models.UserContextDTO user, CancellationToken ct = default)
        {
            _loader.SetModule(module);

            var pipeline = new PluginPipeline<RequestContainer, SensorResponse>(_cache, _serviceProvider, _loader)
                .WithDefaultMapFunction()
                .WithDefaultTransformFunction()
                .WithDefaultReduceFunction();

            var response = await pipeline.RunMapReduceAsync<IAssetHierarchyDataSource>(
                async (datasource, args, ct) => await datasource.GetSingleSensorAsync((string)args[0], ct),
                mapArgIndex: 0,
                args: new object[] { webId }
            );

            var result = new ServiceResponse<SensorResponse>
            {
                Data = response,
                DatasourceValidation = pipeline.GetValidationStatus()
            };

            return result;
        }

        public async Task<ServiceResponse<DatasourceUnitResponse>> GetUnitsOfMeasureAsync(int siteId, string module, Framework.Hosting.Models.UserContextDTO user, CancellationToken ct = default)
        {
            _loader.SetModule(module);

            var pipeline = new PluginPipeline<NoRequest, DatasourceUnitResponse>(_cache, _serviceProvider, _loader)
                .WithMapOnAll(
                    siteId: siteId,
                    dataType: DataType.AssetHierarchy
                 )
                .WithDefaultTransformFunction()
                .WithDefaultReduceFunction();

            var response = await pipeline.RunMapReduceAsync<IAssetHierarchyDataSource>(
                (datasource, ct) => datasource.GetUnitsOfMeasureAsync()
            );

            response.Units = response.Units
                .DistinctBy(u => u.Abbreviation)
                .ToList();

            var result = new ServiceResponse<DatasourceUnitResponse>
            {
                Data = response,
                DatasourceValidation = pipeline.GetValidationStatus()
            };

            return result;
        }

        public async Task<ServiceResponse<DeviceListResponse>> GetMultipleDevicesAsync(int siteId, string module, List<string> webIds, Framework.Hosting.Models.UserContextDTO user,
            CancellationToken ct = default)
        {
            //Map
            var model = new MultiWebIdQueryDTO
            {
                WebId = webIds,
            };

            _loader.SetModule(module);

            var pipeline = new PluginPipeline<MultiWebIdQueryDTO, DeviceListResponse>(_cache, _serviceProvider, _loader)
                .WithDefaultMapFunction()
                .WithDefaultTransformFunction()
                .WithDefaultReduceFunction();

            var response = await pipeline.RunMapReduceAsync<IAssetHierarchyDataSource>(
                (datasource, model, ct) => datasource.GetMultipleDevicesAsync(model, ct),
                model
            );

            var result = new ServiceResponse<DeviceListResponse>
            {
                Data = response,
                DatasourceValidation = pipeline.GetValidationStatus()
            };

            return result;
        }

        public async Task<ServiceResponse<SensorListResponse>> GetMultipleSensorsAsync(int siteId, string module, SensorQueryDTO model, Framework.Hosting.Models.UserContextDTO user, CancellationToken ct = default)
        {
            _loader.SetModule(module);

            var pipeline = new PluginPipeline<SensorQueryDTO, SensorListResponse>(_cache, _serviceProvider, _loader)
                .WithDefaultMapFunction()
                .WithDefaultTransformFunction()
                .WithDefaultReduceFunction();

            var response = await pipeline.RunMapReduceAsync<IAssetHierarchyDataSource>(
                (datasource, model, ct) => datasource.GetMultipleSensorsAsync(model, ct),
                model
            );

            var result = new ServiceResponse<SensorListResponse>
            {
                Data = response,
                DatasourceValidation = pipeline.GetValidationStatus()
            };

            return result;
        }

        [Obsolete("This is for the legacy service implementation, and is removed after migration to core-v2")]
        public async Task<string> GetPreviousGlobalFilterWebIdAsync(int siteId, string module, string webId, Framework.Hosting.Models.UserContextDTO user, CancellationToken ct = default)
        {
            _loader.SetModule(module);

            var pipeline = new PluginPipeline<RequestContainer, string>(_cache, _serviceProvider, _loader)
                        .WithDefaultMapFunction()
                        .WithDefaultTransformFunction()
                        .WithDefaultReduceFunction();

            var response = await pipeline.RunMapReduceAsync<IAssetHierarchyDataSource>(
                (datasource, args, ct) => datasource.GetPreviousGlobalFilterWebIdAsync((string)args[0]),
                mapArgIndex: 0,
                args: new object[] { webId }
            );

            return response;
        }
    }
}
