using Microsoft.AspNetCore.Mvc;
using OpenDCP.Core.Core.Classes;
using OpenDCP.Core.Core.Extensions;
using OpenDCP.Core.Core.Interfaces;
using OpenDCP.Core.Core.Interfaces.Data;
using OpenDCP.Core.Models.Legacy.DataHandling;
using OpenDCP.Framework.Configuration.Model.Responses.Service;
using OpenDCP.Framework.DataTypes.Equipment.Constants;
using OpenDCP.Framework.DataTypes.Equipment.Requests.AssetHierarchy;
using OpenDCP.Framework.DataTypes.Equipment.Requests.DataBundle;
using OpenDCP.Framework.DataTypes.Equipment.Requests.EventFrameContext;
using OpenDCP.Framework.DataTypes.Equipment.Requests.SensorData;
using OpenDCP.Framework.DataTypes.Equipment.Responses.SensorData;
using DataType = OpenDCP.Framework.Configuration.Model.Constants.Datasource.DataType;

#pragma warning disable CS1573
#pragma warning disable CS1572
#pragma warning disable CS1591

namespace OpenDCP.Core.API.Controllers.v1
{
    [Route("api/")]
    public class DataHandlingLegacyController : BaseController
    {
        private readonly ISensorTelemetryDataService _sensorTelemetry;
        private readonly IEventframeContextDataService _eventframeContext;
        private readonly IAssetHierarchyDataService _assetHierarchy;
        private readonly ILegacyService _legacy;
        private readonly IBundleDataService _dataBundleService;
        public DataHandlingLegacyController(IConfiguration configuration, IAssetHierarchyDataService assetHierarchy,
            IEventframeContextDataService eventframeContext, ISensorTelemetryDataService sensorTelemetry,
            ILegacyService legacyService, IBundleDataService dataBundleService) : base(configuration)
        {
            _assetHierarchy = assetHierarchy;
            _eventframeContext = eventframeContext;
            _sensorTelemetry = sensorTelemetry;
            _legacy = legacyService;
            _dataBundleService = dataBundleService;
        }

        /// <summary>
        /// Get single notifications values - currently used by Basic Micro service 
        /// </summary>
        /// <param name="model">SensorsWebIds and SiteConfiguration are required</param>
        /// <returns>Sensors data</returns>
        [HttpPost]
        [ActionName("RecordedData")]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/DataHandling/RecordedData")]
        [Route("DataHandling/RecordedData")]
        public async Task<IActionResult> RecordedData([FromBody] RecordedDataRequest legacyModel)
        {
            var user = GetUserContext();

            //Map to new model(s)
            var info = await _legacy.ReverseMapSiteconfigurationDTO(legacyModel.SiteConfiguration, DataType.SensorTelemetry);
            var model = new RecordedDataQueryDTO
            {
                EndTime = string.IsNullOrEmpty(legacyModel.EndTime) ? DateTime.UtcNow : TimeParser.Parse(legacyModel.EndTime),
                StartTime = string.IsNullOrEmpty(legacyModel.StartTime) ? DateTime.UtcNow.AddSeconds(-60) : TimeParser.Parse(legacyModel.StartTime),
                WebId = legacyModel.SensorsWebIds,
                MaxCount = string.IsNullOrEmpty(legacyModel.MaxCount) ? 1000 : int.Parse(legacyModel.MaxCount),
                FilterExpression = legacyModel.FilterExpression,
                IncludeFilteredValues = legacyModel.IncludeFilteredValues ?? true,
                SynchronizeTimestamps = false
            };

            var visitedEncode = new HashSet<object>();
            _legacy.TransformObjectEncode(model, legacyModel.SiteConfiguration.DataSourceId, visitedEncode);

            //Use new implementation
            var result = await _sensorTelemetry.GetRecordedDataAsync(info.siteId, info.module, model, user);

            var visited = new HashSet<object>();
            _legacy.TransformObjectDecode(result, visited);

            Response.SetValidationHeader(
              _configuration.GetValue<bool>("Host:IsModuleValidated", false),
              result.DatasourceValidation,
              result.CalculationValidation,
              result.EntityValidation
            );

            return this.Ok(_legacy.MapRecordedData(result.Data));
        }
        /// <summary>
        /// Get equation notifications values - currently used by Basic Micro service 
        /// </summary>
        /// <param name="model">SensorsWebIds and SiteConfiguration are required</param>
        /// <returns>Sensors data</returns>
        [HttpPost]
        [ActionName("RecordedDataSynchronizedTimeStamps")]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/DataHandling/RecordedDataSynchronizedTimeStamps")]
        [Route("DataHandling/RecordedDataSynchronizedTimeStamps")]
        public async Task<IActionResult> RecordedDataSynchronizedTimeStamps([FromBody] RecordedDataRequest legacyModel)
        {
            var user = GetUserContext();

            //Map to new model(s)
            var info = await _legacy.ReverseMapSiteconfigurationDTO(legacyModel.SiteConfiguration, DataType.SensorTelemetry);
            var model = new RecordedDataQueryDTO
            {
                EndTime = string.IsNullOrEmpty(legacyModel.EndTime) ? DateTime.UtcNow : TimeParser.Parse(legacyModel.EndTime),
                StartTime = string.IsNullOrEmpty(legacyModel.StartTime) ? DateTime.UtcNow.AddSeconds(-60) : TimeParser.Parse(legacyModel.StartTime),
                WebId = legacyModel.SensorsWebIds,
                MaxCount = string.IsNullOrEmpty(legacyModel.MaxCount) ? 1000 : int.Parse(legacyModel.MaxCount),
                FilterExpression = legacyModel.FilterExpression,
                IncludeFilteredValues = legacyModel.IncludeFilteredValues ?? true,
                SynchronizeTimestamps = true
            };

            var visitedEncode = new HashSet<object>();
            _legacy.TransformObjectEncode(model, legacyModel.SiteConfiguration.DataSourceId, visitedEncode);

            //Use new implementation
            var result = await _sensorTelemetry.GetRecordedDataAsync(info.siteId, info.module, model, user);

            var visited = new HashSet<object>();
            _legacy.TransformObjectDecode(result, visited);

            Response.SetValidationHeader(
              _configuration.GetValue<bool>("Host:IsModuleValidated", false),
              result.DatasourceValidation,
              result.CalculationValidation,
              result.EntityValidation
            );

            return this.Ok(_legacy.MapRecordedData(result.Data));
        }
        /// <summary>
        /// Get single sensor details - currently used by Basic Micro service
        /// </summary>
        /// <param name="model">All parameters are required</param>
        /// <returns>Details for given sensor</returns>
        [HttpPost]
        [ActionName("SensorDetails")]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/DataHandling/SensorDetails")]
        [Route("DataHandling/SensorDetails")]
        public async Task<IActionResult> SensorDetails([FromBody] SensorDetailsRequest legacyModel)
        {
            var user = GetUserContext();

            var info = await _legacy.ReverseMapSiteconfigurationDTO(legacyModel.SiteConfiguration, DataType.AssetHierarchy);

            var webId = _legacy.TransformSingleWebIdEncode(legacyModel.SensorWebId, legacyModel.SiteConfiguration.DataSourceId);

            //Use new implementation
            var result = await _assetHierarchy.GetSingleSensorAsync(info.siteId, info.module, webId, user);

            var visited = new HashSet<object>();
            _legacy.TransformObjectDecode(result, visited);

            Response.SetValidationHeader(
              _configuration.GetValue<bool>("Host:IsModuleValidated", false),
              result.DatasourceValidation,
              result.CalculationValidation,
              result.EntityValidation
            );

            return this.Ok(await _legacy.MapSensorDetails(result.Data, legacyModel.SiteConfiguration));
        }
        /// <summary>
        /// Get single device details - currently used by Basic Micro service
        /// </summary>
        /// <param name="model">All parameters are required</param>
        /// <returns>Details for given sensor</returns>
        [HttpPost]
        [ActionName("DeviceDetails")]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/DataHandling/DeviceDetails")]
        [Route("DataHandling/DeviceDetails")]
        public async Task<IActionResult> DeviceDetails([FromBody] DeviceDetailsRequest legacyModel)
        {
            var user = GetUserContext();

            var info = await _legacy.ReverseMapSiteconfigurationDTO(legacyModel.SiteConfiguration, DataType.AssetHierarchy);

            var webId = _legacy.TransformSingleWebIdEncode(legacyModel.DeviceWebId, legacyModel.SiteConfiguration.DataSourceId);

            //Use new implementation
            var result = await _assetHierarchy.GetSingleDeviceAsync(info.siteId, info.module, webId, user);

            var parentWebId = string.Empty;

            if (!result.Data.Errors.Any())
            {
                parentWebId = await _assetHierarchy.GetPreviousGlobalFilterWebIdAsync(info.siteId, info.module, webId, user);
            }

            var visited = new HashSet<object>();
            _legacy.TransformObjectDecode(result, visited);

            Response.SetValidationHeader(
              _configuration.GetValue<bool>("Host:IsModuleValidated", false),
              result.DatasourceValidation,
              result.CalculationValidation,
              result.EntityValidation
            );

            return this.Ok(await _legacy.MapDeviceDetails(result.Data, parentWebId, legacyModel.SiteConfiguration));
        }
        /// <summary>
        /// Get multiple sensors details - currently used by Basic Micro service
        /// </summary>
        /// <param name="model">All parameters are required</param>
        /// <returns>Details for set of sensors</returns>
        [HttpPost]
        [ActionName("MultipleSensorsDetails")]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/DataHandling/MultipleSensorsDetails")]
        [Route("DataHandling/MultipleSensorsDetails")]
        public async Task<IActionResult> MultipleSensorsDetails([FromBody] MultipleSensorDetailsRequest legacyModel)
        {
            var user = GetUserContext();

            var info = await _legacy.ReverseMapSiteconfigurationDTO(legacyModel.SiteConfiguration, DataType.AssetHierarchy);
            var model = new SensorQueryDTO
            {
                WebId = legacyModel.SensorsWebIds
            };

            var visitedEncode = new HashSet<object>();
            _legacy.TransformObjectEncode(model, legacyModel.SiteConfiguration.DataSourceId, visitedEncode);

            //Use new implementation
            var result = await _assetHierarchy.GetMultipleSensorsAsync(info.siteId, info.module, model, user);

            var visited = new HashSet<object>();
            _legacy.TransformObjectDecode(result, visited);

            Response.SetValidationHeader(
              _configuration.GetValue<bool>("Host:IsModuleValidated", false),
              result.DatasourceValidation,
              result.CalculationValidation,
              result.EntityValidation
            );

            return this.Ok(await _legacy.MapMultipleSensorsDetailsAsync(result.Data, legacyModel.SiteConfiguration));
        }
        /// <summary>
        /// Get the EventFrame Attributes by device webId - currently used by Basic Micro service
        /// </summary>
        /// <param name="model">All parameters are required</param>
        /// <returns>Details for given batch</returns>
        [HttpPost]
        [ActionName("DeviceEventFrameAttribute")]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/DataHandling/DeviceEventFrameAttribute")]
        [Route("DataHandling/DeviceEventFrameAttribute")]
        public async Task<IActionResult> DeviceEventFrameAttribute([FromBody] DeviceEventFrameAttributeRequest legacyModel)
        {
            var user = GetUserContext();

            //Map to new models
            var info = await _legacy.ReverseMapSiteconfigurationDTO(legacyModel.SiteConfiguration, DataType.EventframeContext);

            var webId = _legacy.TransformSingleWebIdEncode(legacyModel.DeviceWebId, legacyModel.SiteConfiguration.DataSourceId);

            //Use new implementation
            var result = await _eventframeContext.GetEventframeAttributesAsync(info.siteId, info.module, webId, user);

            var visited = new HashSet<object>();
            _legacy.TransformObjectDecode(result, visited);

            Response.SetValidationHeader(
              _configuration.GetValue<bool>("Host:IsModuleValidated", false),
              result.DatasourceValidation,
              result.CalculationValidation,
              result.EntityValidation
            );

            return this.Ok(_legacy.MapDeviceEventFrameAttribute(result.Data));
        }
        //move this to global filter controller!!backwards
        /// <summary>
        /// Get global filter element details - currently used by Basic Micro service
        /// </summary>
        /// <param name="model">All parameters are required</param>
        /// <returns>Details for given batch</returns>
        [HttpPost]
        [ActionName("GlobalFilterBackwards")]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/DataHandling/GlobalFilterBackwards")]
        [Route("DataHandling/GlobalFilterBackwards")]
        public async Task<IActionResult> GlobalFilterBackwards([FromBody] GlobalFilterRequest legacyModel)
        {
            var user = GetUserContext();

            var info = await _legacy.ReverseMapSiteconfigurationDTO(legacyModel.SiteConfiguration, DataType.AssetHierarchy);

            var webId = _legacy.TransformSingleWebIdEncode(legacyModel.WebId, legacyModel.SiteConfiguration.DataSourceId);

            //Use new implementation
            var result = await _assetHierarchy.GetGlobalFilterBackwardsAsync(info.siteId, info.module, webId, user);

            await _legacy.TransformElementBack(result.Data.Elements, legacyModel.SiteConfiguration);

            Response.SetValidationHeader(
              _configuration.GetValue<bool>("Host:IsModuleValidated", false),
              result.DatasourceValidation,
              result.CalculationValidation,
              result.EntityValidation
            );

            return this.Ok(_legacy.MapGlobalFilterBackwards(result.Data, legacyModel.SiteConfiguration));
        }

        /// <summary>
        /// Build global filter all elements base on the specific web id - currently used by Micro service
        /// </summary>
        /// <param name="model">All parameters are required</param>
        /// <returns>Complete Global filter array</returns>
        [HttpPost]
        [ActionName("BuildGlobalFilterFromWebId")]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/DataHandling/BuildGlobalFilterFromWebId")]
        [Route("DataHandling/BuildGlobalFilterFromWebId")]
        public async Task<IActionResult> BuildGlobalFilterFromWebId([FromBody] GlobalFilterRequest legacyModel)
        {
            var user = GetUserContext();

            var info = await _legacy.ReverseMapSiteconfigurationDTO(legacyModel.SiteConfiguration, DataType.AssetHierarchy);

            var webId = _legacy.TransformSingleWebIdEncode(legacyModel.WebId, legacyModel.SiteConfiguration.DataSourceId);

            //Use new implementation
            var result = await _assetHierarchy.GetGlobalFilterBackwardsAsync(info.siteId, info.module, webId, user);

            await _legacy.TransformElementBack(result.Data.Elements, legacyModel.SiteConfiguration);

            Response.SetValidationHeader(
              _configuration.GetValue<bool>("Host:IsModuleValidated", false),
              result.DatasourceValidation,
              result.CalculationValidation,
              result.EntityValidation
            );

            return this.Ok(_legacy.MapGlobalFilterFromWebId(result.Data));
        }

        /// <summary>
        /// Get device`sensors - currently used by Basic Micro service
        /// </summary>
        /// <param name="model">Device web id and site information are required</param>
        /// <returns>Sensors by category</returns>
        [HttpPost]
        [ActionName("GroupedDeviceSensors")]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/DataHandling/GroupedDeviceSensors")]
        [Route("DataHandling/GroupedDeviceSensors")]
        public async Task<IActionResult> GroupedDeviceSensors([FromBody] DeviceSensorsRequest legacyModel)
        {
            var user = GetUserContext();

            var info = await _legacy.ReverseMapSiteconfigurationDTO(legacyModel.SiteConfiguration, DataType.AssetHierarchy);

            var webId = _legacy.TransformSingleWebIdEncode(legacyModel.DeviceWebId, legacyModel.SiteConfiguration.DataSourceId);

            var result = await _assetHierarchy.GetGroupedDeviceSensorsAsync(info.siteId, info.module, webId, user);

            var visited = new HashSet<object>();
            _legacy.TransformObjectDecode(result, visited);

            Response.SetValidationHeader(
              _configuration.GetValue<bool>("Host:IsModuleValidated", false),
              result.DatasourceValidation,
              result.CalculationValidation,
              result.EntityValidation
            );

            return this.Ok(_legacy.MapGroupedDeviceSensors(result.Data));
        }
        /// <summary>
        /// Batch Data Studio,ChromTA - Master data management, Model development step 2  , get sensors by device`group type - in before the naming was - GetDeviceSensorsByGroupType
        /// </summary>
        /// <param name="model">All parameters are required</param>
        /// <returns></returns>
        [HttpPost]
        [ActionName("DeviceSensorsByGroup")]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/DataHandling/DeviceSensorsByGroup")]
        [Route("DataHandling/DeviceSensorsByGroup")]
        public async Task<IActionResult> DeviceSensorsByGroup([FromBody] DeviceSensorsRequest legacyModel)
        {
            var user = GetUserContext();

            var info = await _legacy.ReverseMapSiteconfigurationDTO(legacyModel.SiteConfiguration, DataType.AssetHierarchy);

            var webId = _legacy.TransformSingleWebIdEncode(legacyModel.DeviceWebId, legacyModel.SiteConfiguration.DataSourceId);

            var result = await _assetHierarchy.GetDeviceSensorsAsync(info.siteId, info.module, webId, user, legacyModel.GroupType);

            var visited = new HashSet<object>();
            _legacy.TransformObjectDecode(result, visited);

            Response.SetValidationHeader(
              _configuration.GetValue<bool>("Host:IsModuleValidated", false),
              result.DatasourceValidation,
              result.CalculationValidation,
              result.EntityValidation
            );

            return this.Ok(_legacy.MapDeviceSensorsByGroup(result.Data));
        }
        /// <summary>
        /// Return interpolated data from OsiSoft PI - currently used by Micro services
        /// </summary>
        /// <param name="model">WebIDs for each sensor. Refresh if only last item is required. Start,End,Interval - optional</param>
        /// "StartTime": "2018-10-15T04:04:28.9150085Z",
        ///	"EndTime": "2018-10-15T04:24:28.8870086Z"
        ///	Interval : "60s"
        /// <returns>Sensor values by x(timestamp) and y(value) plus bad count</returns>
        [HttpPost]
        [ActionName("InterpolatedDataWithBatchContext")]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/DataHandling/InterpolatedDataWithBatchContext")]
        [Route("DataHandling/InterpolatedDataWithBatchContext")]
        public async Task<IActionResult> InterpolatedDataWithBatchContext([FromBody] LegacyRequestSensorData legacyModel)
        {
            var user = GetUserContext();

            //Map to new model(s)
            var info = await _legacy.ReverseMapSiteconfigurationDTO(legacyModel.SiteConfiguration, DataType.SensorTelemetry);

            ServiceResponse<SensorDataWithBatchContextResponse> result;

            var model = new InterpolatedDataWithBatchContextQueryDTO
            {
                DeviceWebId = legacyModel.DeviceWebIds,
                Interval = legacyModel.Interval,
                WebId = legacyModel.WebIds
            };

            var visitedEncode = new HashSet<object>();
            _legacy.TransformObjectEncode(model, legacyModel.SiteConfiguration.DataSourceId, visitedEncode);

            if (legacyModel.Refresh)
            {
                model.StartTime = DateTime.UtcNow;
                model.EndTime = DateTime.UtcNow;

                result = await _legacy.GetDataWithContextWithRefreshAsync(info.module, null, model);
            }
            else
            {
                model.StartTime = TimeParser.Parse(legacyModel.StartTime);
                model.EndTime = TimeParser.Parse(legacyModel.EndTime);

                result = await _dataBundleService.GetInterpolatedDataWithContextAsync(info.siteId, info.module, model, user);
            }

            var visited = new HashSet<object>();

            result = await _legacy.EventFrameIdToWebIdInBatchResponses(result);
            _legacy.TransformObjectDecode(result, visited);

            Response.SetValidationHeader(
              _configuration.GetValue<bool>("Host:IsModuleValidated", false),
              result.DatasourceValidation,
              result.CalculationValidation,
              result.EntityValidation
            );

            return Ok(_legacy.MapInterpolatedDataWithBatchContext(result.Data, legacyModel, info.module, model.WebId));

        }
        /// <summary>
        /// Return data optimised from OsiSoft PI for plotting and reducing the data traffic used for data visualisation - currently used by Micro services
        /// </summary>
        /// <param name="model">WebIDs for each sensor. Refresh if only last item is required. Start, End, Intervals - optional</param>
        /// "StartTime": "2018-10-15T04:04:28.9150085Z",
        ///	"EndTime": "2018-10-15T04:24:28.8870086Z"
        ///	Intervals : 20
        /// <returns>Sensor values by x(timestamp) and y(value) plus bad count</returns>
        [HttpPost]
        [ActionName("PlotDataWithBatchContext")]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/DataHandling/PlotDataWithBatchContext")]
        [Route("DataHandling/PlotDataWithBatchContext")]
        public async Task<IActionResult> PlotDataWithBatchContext([FromBody] LegacyPlotDataWithBatchContextRequest legacyModel)
        {
            var user = GetUserContext();

            //Map to new model(s)
            var info = await _legacy.ReverseMapSiteconfigurationDTO(legacyModel.SiteConfiguration, DataType.SensorTelemetry);

            ServiceResponse<SensorDataWithBatchContextResponse> result;

            var model = new PlotDataWithBatchContextQueryDTO
            {
                DeviceWebId = legacyModel.DeviceWebIds,
                Intervals = legacyModel.Intervals,
                WebId = legacyModel.SensorsWebIds,
            };

            var visitedEncode = new HashSet<object>();
            _legacy.TransformObjectEncode(model, legacyModel.SiteConfiguration.DataSourceId, visitedEncode);

            if (legacyModel.Refresh)
            {
                model.StartTime = DateTime.UtcNow;
                model.EndTime = DateTime.UtcNow;

                result = await _legacy.GetDataWithContextWithRefreshAsync(info.module, model);
            }
            else
            {
                model.StartTime = TimeParser.Parse(legacyModel.StartTime);
                model.EndTime = TimeParser.Parse(legacyModel.EndTime);

                result = await _dataBundleService.GetPlotDataWithContextAsync(info.siteId, info.module, model, user);
            }

            var visited = new HashSet<object>();

            result = await _legacy.EventFrameIdToWebIdInBatchResponses(result);
            _legacy.TransformObjectDecode(result, visited);

            return this.Ok(_legacy.MapPlotDataWithContext(result.Data, legacyModel, info.module, model.WebId));
        }
        /// <summary>
        /// Batch Data Monitoring, show signals
        /// Expecting only sensorsIDs, because one sensors can be used in only one batch at same time
        /// Using start/end (batch) filter (instead of batchIDs) + sensors to get sensors values for batches
        /// </summary>
        /// <param name="model">SensorsWebIds</param>
        /// <returns>Sensors signals`values as list order by timestamp</returns>
        [HttpPost]
        [ActionName("InterpolatedData")]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/DataHandling/InterpolatedData")]
        [Route("DataHandling/InterpolatedData")]
        public async Task<IActionResult> InterpolatedData([FromBody] LegacyShowSignalsRequest legacyModel)
        {
            var user = GetUserContext();

            //Map to new models
            var info = await _legacy.ReverseMapSiteconfigurationDTO(legacyModel.SiteConfiguration, DataType.SensorTelemetry);
            var model = new InterpolatedDataQueryDTO
            {
                Interval = legacyModel.Interval,
                EndTime = TimeParser.Parse(legacyModel.EndTime),
                StartTime = TimeParser.Parse(legacyModel.StartTime),
                WebId = legacyModel.SensorsWebIds
            };

            var visitedEncode = new HashSet<object>();
            _legacy.TransformObjectEncode(model, legacyModel.SiteConfiguration.DataSourceId, visitedEncode);

            //Use new implementation
            var result = await _sensorTelemetry.GetInterpolatedDataAsync(info.siteId, info.module, model, user);

            var visited = new HashSet<object>();
            _legacy.TransformObjectDecode(result.Data, visited);

            Response.SetValidationHeader(
              _configuration.GetValue<bool>("Host:IsModuleValidated", false),
              result.DatasourceValidation,
              result.CalculationValidation,
              result.EntityValidation
            );


            return this.Ok(_legacy.MapInterpolatedData(result.Data, legacyModel, model.StartTime));
        }
        /// <summary>
        /// Return data(sensors signals) optimised from OsiSoft PI for plotting and reducing the data traffic used for data visualisation - currently used by Micro services
        /// Expecting only sensorsIDs, because one sensors can be used in only one batch at same time
        /// Using start/end (batch) filter (instead of batchIDs) + sensors to get sensors values for batches
        /// </summary>
        /// <param name="model">SensorsWebIds</param>
        /// <returns>Sensors signals`values as list order by timestamp</returns>
        [HttpPost]
        [ActionName("PlotData")]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/DataHandling/PlotData")]
        [Route("DataHandling/PlotData")]
        public async Task<IActionResult> PlotData([FromBody] PlotDataRequest legacyModel)
        {
            var user = GetUserContext();

            //Map to new models
            var info = await _legacy.ReverseMapSiteconfigurationDTO(legacyModel.SiteConfiguration, DataType.SensorTelemetry);
            var model = new PlotDataQueryDTO
            {
                Intervals = legacyModel.Intervals,
                EndTime = TimeParser.Parse(legacyModel.EndTime),
                StartTime = TimeParser.Parse(legacyModel.StartTime),
                WebId = legacyModel.SensorsWebIds
            };

            var visitedEncode = new HashSet<object>();
            _legacy.TransformObjectEncode(model, legacyModel.SiteConfiguration.DataSourceId, visitedEncode);

            //Use new implementation
            var result = await _sensorTelemetry.GetPlotDataAsync(info.siteId, info.module, model, user);

            var visited = new HashSet<object>();
            _legacy.TransformObjectDecode(result, visited);

            Response.SetValidationHeader(
              _configuration.GetValue<bool>("Host:IsModuleValidated", false),
              result.DatasourceValidation,
              result.CalculationValidation,
              result.EntityValidation
            );

            return this.Ok(await _legacy.MapPlotData(result.Data, legacyModel, info.module, model.WebId));
        }
        /// <summary>
        /// Interpolating data for given set of sensors for giving time period - currently used by Micro services
        /// </summary>
        /// <param name="model"></param>
        /// <returns>Sensors signals`values with their timestamps</returns>
        [HttpPost]
        [ActionName("InterpolatedAtTimeData")]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/DataHandling/InterpolatedAtTimeData")]
        [Route("DataHandling/InterpolatedAtTimeData")]
        public async Task<IActionResult> InterpolatedAtTimeData([FromBody] InterpolatedAtTimeRequest legacyModel)
        {
            var user = GetUserContext();

            //Map to new models
            var info = await _legacy.ReverseMapSiteconfigurationDTO(legacyModel.SiteConfiguration, DataType.SensorTelemetry);
            var model = new InterpolatedAtTimeDataQueryDTO
            {
                Time = legacyModel.Times.Select(x => TimeParser.Parse(x)).ToList(),
                WebId = legacyModel.SensorsWebIds
            };

            var visitedEncode = new HashSet<object>();
            _legacy.TransformObjectEncode(model, legacyModel.SiteConfiguration.DataSourceId, visitedEncode);

            //Use new implementation
            var result = await _sensorTelemetry.GetInterpolatedDataAtTimesAsync(info.siteId, info.module, model, user);

            var visited = new HashSet<object>();
            _legacy.TransformObjectDecode(result, visited);

            Response.SetValidationHeader(
              _configuration.GetValue<bool>("Host:IsModuleValidated", false),
              result.DatasourceValidation,
              result.CalculationValidation,
              result.EntityValidation
            );

            return this.Ok(_legacy.MapInterpolatedAtTimeData(result.Data));
        }

        /// <summary>
        /// Batch selection  per single device (Model Development, Batch Data Monitoring). Full dynamic event frame, ex. program, temperature, etc. 
        /// </summary>
        /// <param name="model">All parameters are required</param>
        /// <returns></returns>
        [HttpPost]
        [ActionName("DeviceEventFrames")]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/DataHandling/DeviceEventFrames")]
        [Route("DataHandling/DeviceEventFrames")]
        public async Task<IActionResult> DeviceEventFrames([FromBody] LegacyDeviceEventFramesRequest legacyModel)
        {
            var user = GetUserContext();

            //Map to new models
            var info = await _legacy.ReverseMapSiteconfigurationDTO(legacyModel.SiteConfiguration, DataType.EventframeContext);
            var model = new EventframeContextTimeQueryDTO
            {
                DeviceWebId = new List<string> { legacyModel.DeviceWebId },
                EndTime = legacyModel.EndTime is null ? DateTime.UtcNow : TimeParser.Parse(legacyModel.EndTime),
                StartTime = legacyModel.StartTime is null ? DateTime.UtcNow.AddHours(-8) : TimeParser.Parse(legacyModel.StartTime),
                IncludeAttributeSettings = true,
            };

            if (legacyModel.IsActive)
            {
                model.SearchMode = EventframeSearchMode.InProgress;
                model.StartTime = legacyModel.StartTime is null ? DateTime.UtcNow.AddMonths(-6) : TimeParser.Parse(legacyModel.StartTime);
            }

            var visitedEncode = new HashSet<object>();
            _legacy.TransformObjectEncode(model, legacyModel.SiteConfiguration.DataSourceId, visitedEncode);

            //Use new implementation
            var result = await _eventframeContext.SearchEventframeContextByTimeAsync(info.siteId, info.module, model, user);

            var visited = new HashSet<object>();
            _legacy.TransformObjectDecode(result, visited);

            Response.SetValidationHeader(
              _configuration.GetValue<bool>("Host:IsModuleValidated", false),
              result.DatasourceValidation,
              result.CalculationValidation,
              result.EntityValidation
            );

            return this.Ok(_legacy.MapDeviceEventFrames(result.Data, new Framework.DataTypes.Equipment.Responses.AssetHierarchy.DeviceListResponse(), isMultipleDevice: false));
        }
        /// <summary>
        /// Batch selection per multiple devices (Model Development, Batch Data Monitoring). Full dynamic event frame, ex. program, temperature, etc. 
        /// </summary>
        /// <param name="model">All parameters are required</param>
        /// <returns></returns>
        [HttpPost]
        [ActionName("DevicesEventFrames")]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/DataHandling/DevicesEventFrames")]
        [Route("DataHandling/DevicesEventFrames")]
        public async Task<IActionResult> DevicesEventFrames([FromBody] DevicesEventFramesRequest legacyModel)
        {
            var user = GetUserContext();

            //Map to new models - Eventframe
            var info = await _legacy.ReverseMapSiteconfigurationDTO(legacyModel.SiteConfiguration, DataType.EventframeContext);

            var model = new EventframeContextTimeQueryDTO
            {
                DeviceWebId = legacyModel.DeviceWebIds,
                EndTime = TimeParser.Parse(legacyModel.EndTime),
                StartTime = TimeParser.Parse(legacyModel.StartTime),
                IncludeAttributeSettings = true
            };

            var visitedEncode = new HashSet<object>();
            _legacy.TransformObjectEncode(model, legacyModel.SiteConfiguration.DataSourceId, visitedEncode);

            //Map to new models - Asset
            var info2 = await _legacy.ReverseMapSiteconfigurationDTO(legacyModel.SiteConfiguration, DataType.AssetHierarchy);

            //Use new implementation
            var result = await _eventframeContext.SearchEventframeContextByTimeAsync(info.siteId, info.module, model, user);
            var result2 = await _assetHierarchy.GetMultipleDevicesAsync(info2.siteId, info2.module, model.DeviceWebId, user);

            var visited = new HashSet<object>();
            var visited2 = new HashSet<object>();

            _legacy.TransformObjectDecode(result, visited);
            _legacy.TransformObjectDecode(result2, visited2);

            Response.SetValidationHeader(
              _configuration.GetValue<bool>("Host:IsModuleValidated", false),
              result.DatasourceValidation,
              result.CalculationValidation,
              result.EntityValidation
            );

            return this.Ok(_legacy.MapDeviceEventFrames(result.Data, result2.Data, isMultipleDevice: true));
        }
        /// <summary>
        /// Get active batches 
        /// </summary>
        /// <param name="model">All parameters are required</param>
        /// <returns></returns>
        [HttpPost]
        [ActionName("DeviceInProgressEventFrames")]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/DataHandling/DeviceInProgressEventFrames")]
        [Route("DataHandling/DeviceInProgressEventFrames")]
        public async Task<IActionResult> DeviceInProgressEventFrames([FromBody] DeviceEventFramesRequest legacyModel)
        {
            var user = GetUserContext();

            //Map to new models
            var info = await _legacy.ReverseMapSiteconfigurationDTO(legacyModel.SiteConfiguration, DataType.EventframeContext);
            var model = new EventframeContextTimeQueryDTO
            {
                DeviceWebId = new List<string> { legacyModel.DeviceWebId },
                EndTime = TimeParser.Parse(legacyModel.EndTime),
                StartTime = TimeParser.Parse(legacyModel.StartTime),
                SearchMode = EventframeSearchMode.InProgress,
                IncludeAttributeSettings = false,
                Filter = new AttributeFilterGroupDTO()
            };

            var visitedEncode = new HashSet<object>();
            _legacy.TransformObjectEncode(model, legacyModel.SiteConfiguration.DataSourceId, visitedEncode);

            //Use new implementation
            var result = await _eventframeContext.SearchEventframeContextByTimeAsync(info.siteId, info.module, model, user);

            var visited = new HashSet<object>();
            _legacy.TransformObjectDecode(result, visited);

            Response.SetValidationHeader(
              _configuration.GetValue<bool>("Host:IsModuleValidated", false),
              result.DatasourceValidation,
              result.CalculationValidation,
              result.EntityValidation
            );

            return this.Ok(_legacy.MapDeviceInProgressEventFrames(result.Data));
        }
        /// <summary>
        /// Check if device has active batches in OsiSoftPI - currently used by Micro services
        /// </summary>
        /// <param name="model">Device web id and Site details</param>
        /// <returns>Array with event frame ids for each active batch per this device</returns>
        [HttpPost]
        [ActionName("ActiveBatchesPerDevice")]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/DataHandling/ActiveBatchesPerDevice")]
        [Route("DataHandling/ActiveBatchesPerDevice")]
        public async Task<IActionResult> ActiveBatchesPerDevice([FromBody] ActiveBatchRequest legacyModel)
        {
            var user = GetUserContext();

            //Map to new models
            var info = await _legacy.ReverseMapSiteconfigurationDTO(legacyModel.SiteConfiguration, DataType.EventframeContext);
            var model = new BatchContextTimeQueryDTO
            {
                DeviceWebId = legacyModel.DeviceWebIds,
                StartTime = DateTime.UtcNow.AddDays(-300),
                EndTime = DateTime.UtcNow.AddDays(1),
                SearchMode = EventframeSearchMode.InProgress
            };

            var visitedEncode = new HashSet<object>();
            _legacy.TransformObjectEncode(model, legacyModel.SiteConfiguration.DataSourceId, visitedEncode);

            //Use new implementation
            var result = await _eventframeContext.SearchBatchContextByTimeAsync(info.siteId, info.module, model, user);

            var visited = new HashSet<object>();
            _legacy.TransformObjectDecode(result, visited);

            Response.SetValidationHeader(
              _configuration.GetValue<bool>("Host:IsModuleValidated", false),
              result.DatasourceValidation,
              result.CalculationValidation,
              result.EntityValidation
            );

            return this.Ok(_legacy.MapActiveBatchesPerDevice(result.Data));
        }
        /// <summary>
        /// Get event frame attributes values - currently used by Micro services
        /// </summary>
        /// <param name="model">All parameters are required</param>
        /// <returns>Event frame attributes values</returns>
        [HttpPost]
        [ActionName("EventFrameAttributesValues")]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/DataHandling/EventFrameAttributesValues")]
        [Route("DataHandling/EventFrameAttributesValues")]
        public async Task<IActionResult> EventFrameAttributesValues([FromBody] EventFrameAttributesValuesRequest legacyModel)
        {
            var user = GetUserContext();

            //Map to new models
            var info = await _legacy.ReverseMapSiteconfigurationDTO(legacyModel.SiteConfiguration, DataType.EventframeContext);
            var webId = _legacy.TransformSingleWebIdEncode(legacyModel.EventFrameWebId, legacyModel.SiteConfiguration.DataSourceId);

            var id = (await _legacy.WebId2EventFrameId(new List<string> { webId })).First();
            id = _legacy.TransformSingleWebIdEncode(id, legacyModel.SiteConfiguration.DataSourceId);

            //Use new implementation
            var result = await _eventframeContext.GetEventframeValuesAsync(info.siteId, info.module, id, user);

            var visited = new HashSet<object>();
            _legacy.TransformObjectDecode(result, visited);

            Response.SetValidationHeader(
              _configuration.GetValue<bool>("Host:IsModuleValidated", false),
              result.DatasourceValidation,
              result.CalculationValidation,
              result.EntityValidation
            );

            return this.Ok(_legacy.MapEventFrameAttributesValues(result.Data));
        }
        /// <summary>
        /// Get event frame end time - currently used by Micro services
        /// </summary>
        /// <param name="model">All parameters are required</param>
        /// <returns>Event frame end time</returns>
        [HttpPost]
        [ActionName("EventFrameEndTime")]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/DataHandling/EventFrameEndTime")]
        [Route("DataHandling/EventFrameEndTime")]
        public async Task<IActionResult> EventFrameEndTime([FromBody] EventFrameEndTimeRequest legacyModel)
        {
            var user = GetUserContext();

            //Map to new models
            var info = await _legacy.ReverseMapSiteconfigurationDTO(legacyModel.SiteConfiguration, DataType.EventframeContext);

            var webId = _legacy.TransformSingleWebIdEncode(legacyModel.EventFrameId, legacyModel.SiteConfiguration.DataSourceId);

            //Use new implementation
            var result = await _eventframeContext.GetEventframeAsync(info.siteId, info.module, webId, user);

            var visited = new HashSet<object>();
            _legacy.TransformObjectDecode(result, visited);

            Response.SetValidationHeader(
              _configuration.GetValue<bool>("Host:IsModuleValidated", false),
              result.DatasourceValidation,
              result.CalculationValidation,
              result.EntityValidation
            );

            return this.Ok(_legacy.MapEventFrameEndTime(result.Data));
        }
        /// <summary>
        /// Get event frames in progress - currently used by Micro services
        /// </summary>
        /// <param name="model">All parameters are required</param>
        /// <returns>Event frame details for running batches</returns>
        [HttpPost]
        [ActionName("EventFramesInProgress")]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/DataHandling/EventFramesInProgress")]
        [Route("DataHandling/EventFramesInProgress")]
        public async Task<IActionResult> EventFramesInProgress([FromBody] EventFramesInProgressRequest legacyModel)
        {
            var user = GetUserContext();

            //Map to new models
            var info = await _legacy.ReverseMapSiteconfigurationDTO(legacyModel.SiteConfiguration, DataType.EventframeContext);
            var model = new EventframeContextTimeQueryDTO
            {
                DeviceWebId = legacyModel.DeviceWebIds,
                SearchMode = EventframeSearchMode.InProgress,
                IncludeAttributeSettings = false,
                StartTime = DateTime.UtcNow.AddDays(-300),
                EndTime = DateTime.UtcNow.AddDays(1),
            };

            var visitedEncode = new HashSet<object>();
            _legacy.TransformObjectEncode(model, legacyModel.SiteConfiguration.DataSourceId, visitedEncode);

            //Use new implementation
            var result = await _eventframeContext.SearchEventframeContextByTimeAsync(info.siteId, info.module, model, user);

            var visited = new HashSet<object>();
            _legacy.TransformObjectDecode(result, visited);

            Response.SetValidationHeader(
              _configuration.GetValue<bool>("Host:IsModuleValidated", false),
              result.DatasourceValidation,
              result.CalculationValidation,
              result.EntityValidation
            );

            return this.Ok(_legacy.MapEventFramesInProgress(result.Data, legacyModel));
        }
        /// <summary>
        /// Get all datasource unit per asset database - currently used by Micro services
        /// </summary>
        /// <param name="model">All parameters are required</param>
        /// <returns>List of objects contains unit of measurement and classes per asset database</returns>
        [HttpPost]
        [ActionName("DataSourceUnits")]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/DataHandling/DataSourceUnits")]
        [Route("DataHandling/DataSourceUnits")]
        public async Task<IActionResult> DataSourceUnits([FromBody] DatasourceUnitsRequest legacyModel)
        {
            var user = GetUserContext();

            //Map to new models
            var info = await _legacy.ReverseMapSiteconfigurationDTO(legacyModel.SiteConfiguration, DataType.AssetHierarchy);

            //Use new implementation
            var result = await _assetHierarchy.GetUnitsOfMeasureAsync(info.siteId, info.module, user);

            Response.SetValidationHeader(
              _configuration.GetValue<bool>("Host:IsModuleValidated", false),
              result.DatasourceValidation,
              result.CalculationValidation,
              result.EntityValidation
            );

            return this.Ok(_legacy.MapDataSourceUnits(result.Data));
        }

        /// <summary>
        /// Get values of datapoints by their existing datetimes
        /// </summary>
        /// <param name="model">times, webIds, retrieval mode and SiteConfiguration are required</param>
        /// <returns>Sensors data</returns>
        [HttpPost]
        [ActionName("RecordedAtTimesData")]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/DataHandling/RecordedAtTimesData")]
        [Route("DataHandling/RecordedAtTimesData")]
        public async Task<IActionResult> RecordedAtTimesData([FromBody] RecordedAtTimesRequest legacyModel)
        {
            var user = GetUserContext();

            var info = await _legacy.ReverseMapSiteconfigurationDTO(legacyModel.SiteConfiguration, DataType.SensorTelemetry);
            var model = new RecordedAtTimeDataQueryDTO
            {
                WebId = legacyModel.SensorsWebIds,
                Time = legacyModel.Times.Select(x => TimeParser.Parse(x)).ToList(),
                //RetrievalMode = (RetrievalMode)legacyModel.RetrievalMode
            };

            var visitedEncode = new HashSet<object>();
            _legacy.TransformObjectEncode(model, legacyModel.SiteConfiguration.DataSourceId, visitedEncode);

            //Use new implementation
            var result = await _sensorTelemetry.GetRecordedDataAtTimesAsync(info.siteId, info.module, model, user);

            var visited = new HashSet<object>();
            _legacy.TransformObjectDecode(result, visited);

            Response.SetValidationHeader(
              _configuration.GetValue<bool>("Host:IsModuleValidated", false),
              result.DatasourceValidation,
              result.CalculationValidation,
              result.EntityValidation
            );

            return this.Ok(_legacy.MapRecordedAtTimesData(result.Data));
        }
        /// <summary>
        /// Get values of datapoints by their existing datetimes
        /// </summary>
        /// <param name="model">times, webIds, retrieval mode and SiteConfiguration are required</param>
        /// <returns>Sensors data</returns>
        [HttpPost]
        [ActionName("GetAllDevicesPerSite")]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/DataHandling/GetAllDevicesPerSite")]
        [Route("DataHandling/GetAllDevicesPerSite")]
        public async Task<IActionResult> GetAllDevicesPerSite([FromBody] LegacyDevicesPerSiteRequest legacyModel)
        {
            var user = GetUserContext();

            var info = await _legacy.ReverseMapSiteconfigurationDTO(legacyModel.SiteConfiguration, DataType.AssetHierarchy);
            var model = new DeviceQueryDTO
            {
                NameQuery = legacyModel.Query,
            };

            //Use new implementation
            var result = await _assetHierarchy.SearchDevicesAsync(info.siteId, info.module, model, user);

            var visited = new HashSet<object>();
            _legacy.TransformObjectDecode(result, visited);

            Response.SetValidationHeader(
              _configuration.GetValue<bool>("Host:IsModuleValidated", false),
              result.DatasourceValidation,
              result.CalculationValidation,
              result.EntityValidation
            );

            return this.Ok(_legacy.MapAllDevicesPerSite(result.Data));
        }
    }
}

#pragma warning restore CS1591
#pragma warning restore CS1573
#pragma warning restore CS1572