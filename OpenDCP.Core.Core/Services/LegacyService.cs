using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenDCP.Core.Core.Classes;
using OpenDCP.Core.Core.ConnectionStringLoader;
using OpenDCP.Core.Core.Extensions;
using OpenDCP.Core.Core.Interfaces;
using OpenDCP.Core.DAL;
using OpenDCP.Core.Models.Constants;
using OpenDCP.Core.Models.Legacy.Common;
using OpenDCP.Core.Models.Legacy.DataHandling;
using OpenDCP.Core.Models.Legacy.Site;
using OpenDCP.Core.Models.Models.Configuration;
using OpenDCP.Core.Models.Models.EventSubscription;
using OpenDCP.Core.Models.Models.Site;
using OpenDCP.Framework.Configuration.Model.Attributes;
using OpenDCP.Framework.Configuration.Model.Constants.Datasource;
using OpenDCP.Framework.Configuration.Model.Entities.Site;
using OpenDCP.Framework.Configuration.Model.Responses.Service;
using OpenDCP.Framework.Connections.Manager.Helpers;
using OpenDCP.Framework.Connections.Manager.Models;
using OpenDCP.Framework.Connections.Manager.Pipeline;
using OpenDCP.Framework.Connections.Manager.PluginManagement;
using OpenDCP.Framework.DataTypes.Equipment.Constants;
using OpenDCP.Framework.DataTypes.Equipment.Entities.AssetHierarchy;
using OpenDCP.Framework.DataTypes.Equipment.Entities.EventframeContext;
using OpenDCP.Framework.DataTypes.Equipment.Entities.SensorData;
using OpenDCP.Framework.DataTypes.Equipment.Interfaces;
using OpenDCP.Framework.DataTypes.Equipment.Requests.AssetHierarchy;
using OpenDCP.Framework.DataTypes.Equipment.Requests.DataBundle;
using OpenDCP.Framework.DataTypes.Equipment.Requests.EventFrameContext;
using OpenDCP.Framework.DataTypes.Equipment.Requests.SensorData;
using OpenDCP.Framework.DataTypes.Equipment.Responses.AssetHierarchy;
using OpenDCP.Framework.DataTypes.Equipment.Responses.EventFrameContext;
using OpenDCP.Framework.DataTypes.Equipment.Responses.SensorData;
using OpenDCP.Framework.Exceptions;
using OpenDCP.Framework.Security.Classes;
using OpenDCP.Framework.Security.Constants;
using OpenDCP.Framework.Security.Models;
using OpenDCP.Framework.ServiceDiscovery.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DataType = OpenDCP.Framework.Configuration.Model.Constants.Datasource.DataType;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace OpenDCP.Core.Core.Services
{
    public class LegacyService : BaseService, ILegacyService
    {
        private readonly IServiceDiscovery _serviceDiscovery;
        private readonly DbConnectionStringLoader _loader;
        protected IServiceProvider _serviceProvider;
        protected DatasourceCache _cache;

        public LegacyService(DCPCoreDbContext dbContext, IConfiguration configuration, ILoggerFactory factory, IMemoryCache memoryCache,
            IServiceDiscovery serviceDiscovery, IServiceProvider serviceProvider, DatasourceCache cache) : base(dbContext, configuration, factory, memoryCache)
        {
            _serviceDiscovery = serviceDiscovery;
            _loader = new DbConnectionStringLoader(_dbContext, _configuration);
            _serviceProvider = serviceProvider;
            _cache = cache;
        }

        public async Task<(int siteId, string module)> ReverseMapSiteconfigurationDTO(LegacySiteConfigurationDTO_v2 siteConfiguration, DataType dataType)
        {
            var moduleName = await _dbContext.DataType
                .Where(x => x.DataSourceId == siteConfiguration.DataSourceId
                            && x.DataSource!.SiteId == siteConfiguration.ID
                            && x.Type == dataType)
                .Select(x => x.ModuleName)
                .FirstOrDefaultAsync();

            if (moduleName is null)
                throw new DCPBadRequestException("Reverse mapping failed");

            var result =
                (
                    siteId: siteConfiguration.ID,
                    module: moduleName
                );

            return result;
        }

        public async Task<int> GetDatasourceId(int siteId, string moduleName, DataType dataType)
        {
            var datasourceId = await _dbContext.DataType
                .Where(x => x.ModuleName == moduleName
                            && x.DataSource!.SiteId == siteId
                            && x.Type == dataType)
                .Select(x => x.DataSourceId)
                .FirstOrDefaultAsync();

            return datasourceId;
        }

        public JsonObject MapRecordedData(SensorDataResponse model)
        {
            var errors = model.Errors.FirstOrDefault();

            var item = new JsonArray();

            var response = new JsonObject();

            if (errors == null)
            {
                response["Items"] = item;
            }
            else
            {
                response["WebId"] = errors.Identifier;
            }

            var result = new JsonObject
            {
                { "IsSuccessStatusCode", errors == null },
                 //in AVEVAPIwebAPI package there is method called ExecuteWithCachingNotFoundWebIdsAsync which is used here 
                 //and its return ErrorCode.DeleteSensor which in logical and business term is correct but in microservices which are using this legacy end point 
                 //it`s checking for (byte)WarningStatusCodes.DeletedWebOnPI which has value of 2 that why we are using WarningStatusCodes.DeletedAttributeWebId here 
                { "WarningCode", errors != null && errors.ErrorCode ==  (byte)ErrorCode.DeleteSensor ? (byte)WarningCode.DeletedAttributeWebId : (byte)WarningCode.Success},
                { "Response", response},
            };

            foreach (var data in model.Data)
            {
                var itemSection = new JsonArray();

                var objectToAdd = new JsonObject
                {
                     { "WebId", data.WebId },
                     { "Items", itemSection },
                };

                foreach (var dataPoints in data.DataPoints)
                {
                    var dataPoint = new JsonObject
                    {
                        { "Timestamp" , dataPoints.ObsId},
                        { "Good" , dataPoints.Good},
                        { "Value" , JsonValue.Create(dataPoints.Value)},
                    };

                    itemSection.Add(dataPoint);
                }

                item.Add(objectToAdd);
            }

            return result;
        }
        public async Task<JsonObject> MapSensorDetails(SensorResponse model, LegacySiteConfigurationDTO_v2 siteConfiguration)
        {
            if (model.Errors != null && model.Errors.Count() > 0)
            {
                var errorMessage = new JsonObject
                {
                    { "Errors", new JsonArray
                         {
                            model.Errors[0].Error
                         }
                    }
                };

                throw new DCPRawBadRequestException(errorMessage.ToJsonString());
            }

            var dataSourceDetails = await _dbContext.DataSource
            .Where(x => x.Id == siteConfiguration.DataSourceId && x.SiteId == siteConfiguration.ID)
            .Select(x => new { x.ConnectionString, x.Type })
            .FirstOrDefaultAsync();

            var element = new JsonObject();

            if (dataSourceDetails != null)
            {
                var key = GetEncryptionKey();

                var decryptedConnectionString = EncryptionHelper.DecryptConnectionString(dataSourceDetails.ConnectionString, key);

                if (ParseConnectionType(dataSourceDetails.Type) == ConnectionType.OSISoftPI)
                {
                    element.Add("Element", @$"{decryptedConnectionString["BaseURL"]}elements/{model.Sensor.DeviceWebId}");
                }
                else if (ParseConnectionType(dataSourceDetails.Type) == ConnectionType.PTDC || ParseConnectionType(dataSourceDetails.Type) == ConnectionType.Mock)
                {
                    element.Add("Element", null);
                }
            }

            var result = new JsonObject
            {
                { "WebId", model.Sensor.WebId },
                { "Name", model.Sensor.Name},
                { "Description", model.Sensor.Description},
                { "Type", model.Sensor.ValueType},
                { "DefaultUnitsNameAbbreviation", model.Sensor.UoM},
                { "CategoryNames",  JsonSerializer.SerializeToNode(model.Sensor.CategoryNames)},
                { "Step", model.Sensor.IsStep},
                { "Links", element},
            };

            return result;
        }
        public async Task<JsonObject> MapDeviceDetails(DeviceResponse model, string parentWebId, LegacySiteConfigurationDTO_v2 siteConfiguration)
        {
            if (model.Errors != null && model.Errors.Count() > 0)
            {
                var errorMessage = new JsonObject
                {
                    { "Errors", new JsonArray
                         {
                            model.Errors[0].Error
                         }
                    }
                };

                throw new DCPRawBadRequestException(errorMessage.ToJsonString());
            }

            var dataSourceDetails = await _dbContext.DataSource
                .Where(x => x.Id == siteConfiguration.DataSourceId && x.SiteId == siteConfiguration.ID)
                .Select(x => new { x.ConnectionString, x.Type })
                .FirstOrDefaultAsync();

            var hasChildren = false;
            var links = new JsonObject();

            if (dataSourceDetails != null)
            {
                var key = GetEncryptionKey();

                var decryptedConnectionString = EncryptionHelper.DecryptConnectionString(dataSourceDetails.ConnectionString, key);

                if (dataSourceDetails.Type == ConnectionType.OSISoftPI.ToString())
                {
                    var sensorFilterType = siteConfiguration.ConnectionString?
                        .ElementAtOrDefault(0).Value?["SensorFilterType"]?
                        .GetValue<string>();

                    //ToDo there is a case >(invalid configuration) where this logic is not correct check how to resolve this
                    if (string.Equals(sensorFilterType, "ElementFilter", StringComparison.OrdinalIgnoreCase))
                    {
                        hasChildren = true;
                    }

                    links.Add("Attributes", @$"{decryptedConnectionString["BaseURL"]}elements/{model.Device.WebId}/attributes");
                    links.Add("Elements", @$"{decryptedConnectionString["BaseURL"]}elements/{model.Device.WebId}/elements");
                    links.Add("Parent", @$"{decryptedConnectionString["BaseURL"]}elements/{parentWebId}");
                }
            }

            var result = new JsonObject
            {
                { "WebId", model.Device.WebId },
                { "Name", model.Device.Name},
                { "HasChildren", hasChildren},
                { "Description", model.Device.Description},
                { "CategoryNames",  JsonSerializer.SerializeToNode(model.Device.CategoryNames)},
                { "Links", links }
            };


            return result;
        }
        public async Task<JsonObject> MapMultipleSensorsDetailsAsync(SensorListResponse model, LegacySiteConfigurationDTO_v2 siteConfiguration)
        {
            var errors = model.Errors.FirstOrDefault();
            var items = new JsonArray();

            var result = new JsonObject
            {
                 { "IsSuccessStatusCode", errors is null },
                 { "WarningCode", errors != null ? errors.ErrorCode : (byte)WarningCode.Success},
                 { "Response", new JsonObject {
                         { "Items", items },
             }
                },
            };

            var dataSourceDetails = await _dbContext.DataSource
                .Where(x => x.Id == siteConfiguration.DataSourceId && x.SiteId == siteConfiguration.ID)
            .Select(x => new { x.ConnectionString, x.Type })
                .FirstOrDefaultAsync();

            foreach (var item in model.Errors.Where(x => x.ErrorCode == (byte)ErrorCode.DeleteSensor))
            {
                var element = new JsonObject
                {
                    { "Exception", new JsonObject
                        {
                            { "Errors", new JsonArray
                                 {
                                     $"WebID '{item.Identifier}' is not found."
                                 }
                            }
                        }
                    }
                };

                items.Add(element);
            }

            foreach (var sensorDetails in model.Sensors)
            {
                var element = new JsonObject();

                if (dataSourceDetails != null)
                {
                    var key = GetEncryptionKey();

                    var decryptedConnectionString = EncryptionHelper.DecryptConnectionString(dataSourceDetails.ConnectionString, key);

                    if (ParseConnectionType(dataSourceDetails.Type) == ConnectionType.OSISoftPI)
                    {
                        element.Add("Element", @$"{decryptedConnectionString["BaseURL"]}elements/{sensorDetails.DeviceWebId}");
                    }
                    else if (ParseConnectionType(dataSourceDetails.Type) == ConnectionType.PTDC || ParseConnectionType(dataSourceDetails.Type) == ConnectionType.Mock)
                    {
                        element.Add("Element", null);
                    }
                }

                var sensorDetailsForAdd = new JsonObject
                {
                    { "WebId", sensorDetails.WebId },
                    { "Name", sensorDetails.Name},
                    { "Description", sensorDetails.Description},
                    { "Type", sensorDetails.ValueType},
                    { "DefaultUnitsNameAbbreviation", sensorDetails.UoM},
                    { "CategoryNames",  JsonSerializer.SerializeToNode(sensorDetails.CategoryNames)},
                    { "Step", sensorDetails.IsStep},
                    { "Links", element},
                };

                var objectToAdd = new JsonObject
                {
                    { "Object", sensorDetailsForAdd }
                };

                items.Add(objectToAdd);
            }

            return result;
        }
        public JsonArray MapDeviceEventFrameAttribute(EventframeAttributesResponse model)
        {
            if (model.Errors != null && model.Errors.Count > 0)
            {
                throw new DCPRawBadRequestException("The following ParentIds did not complete successfully: 1.");
            }

            var result = new JsonArray();

            foreach (var item in model.Attributes)
            {
                var itemToAdd = new JsonObject
                {
                     { "Name", item.Name },
                     { "Description", item.Description},
                     { "Type", item.ValueType},
                };

                result.Add(itemToAdd);
            }

            return result;
        }
        public JsonObject MapGlobalFilterBackwards(GlobalFilterListResponse model, LegacySiteConfigurationDTO_v2 siteConfiguration)
        {
            //TODO From my perspective the response objects are switched - GlobalFilterListResponse needs to be returned from GlobalFilterFromWebId and GlobalFilterBackwards needs to return GlobalFilterResponse
            //Check the old Core for details 
            //In the old Core always one object is return by the end point 
            //Check GetGlobalFilterBackwardsAsync there is a if case where one more element is added?
            //If attribute filter is used a special filter level for the sensor group needs to be added
            var firstElement = model.Elements.FirstOrDefault();

            var result = new JsonObject();

            if (firstElement != null)
            {
                var element = new JsonObject()
                {
                    { "Elements", $"{siteConfiguration.ConnectionString![0]!["BaseURL"]}elements/{firstElement.WebId}/elements" },
                    { "Attributes", $"{siteConfiguration.ConnectionString![0]!["BaseURL"]}elements/{firstElement.WebId}/attributes" },
                };

                result.Add("WebId", firstElement.WebId);
                result.Add("Name", firstElement.Name);
                result.Add("Description", firstElement.Description);
                result.Add("CategoryNames", JsonSerializer.SerializeToNode(firstElement.CategoryNames));
                result.Add("Links", element);
            }

            return result;
        }
        public JsonArray MapGlobalFilterFromWebId(GlobalFilterListResponse model)
        {
            var result = new JsonArray();

            foreach (var item in model.Elements)
            {
                var itemToAdd = new JsonObject
                {
                    { "Name" , item.Name },
                    { "Description" , item.Description },
                    { "WebId" , item.WebId },
                    { "UoM" , item.UoM },
                    { "IsDevice" , item.IsDevice },
                    { "IsSensor" , !(item.ValueType is null)  },
                    { "ValueType" , item.ValueType },
                    { "IsChromUnit" , item.IsChromUnit },
                    { "CategoryNames" , JsonSerializer.SerializeToNode(item.CategoryNames) },
                    { "Step" , item.Step },
                    { "Link" , item.Link },
                };

                result.Add(itemToAdd);
            }

            return result;
        }

        public JsonObject MapInterpolatedData(SensorDataResponse model, LegacyShowSignalsRequest legacyModel, DateTime startTime)
        {
            var errors = model.Errors.FirstOrDefault();

            var dataPoints = new JsonArray();

            var obsId = model.Data?.FirstOrDefault()?.DataPoints?.FirstOrDefault()?.ObsId;

            var result = new JsonObject
            {
                { "UnitOfMeasureXAxis", null},
                { "UnitOfMeasureYAxis", null},
                { "FirstItemTotalSeconds", obsId.HasValue && errors == null ? ((DateTimeOffset)obsId.Value).ToUnixTimeSeconds() : 0},
                 //in AVEVAPIwebAPI package there is method called ExecuteWithCachingNotFoundWebIdsAsync which is used here 
                 //and its return ErrorCode.DeleteSensor which in logical and business term is correct but in microservices which are using this legacy end point 
                 //it`s checking for (byte)WarningStatusCodes.DeletedAttributeWebId which has value of 2 that why we are using WarningStatusCodes.DeletedAttributeWebId here 
                { "WarningCode", errors != null && errors.ErrorCode ==  (byte)ErrorCode.DeleteSensor ? (byte)WarningCode.DeletedAttributeWebId : (byte)WarningCode.Success},
                { "DataPoints", errors != null ? null : dataPoints},
            };

            if (errors != null)
            {
                return result;
            }

            if (!string.IsNullOrEmpty(legacyModel.WebIdOfMaturity))
            {
                var sensorDetails = model.Data!
                    .FirstOrDefault(x => x.WebId == legacyModel.WebIdOfMaturity);

                if (sensorDetails != null)
                {
                    result["UnitOfMeasureXAxis"] = sensorDetails.UoM;
                }
            }
            else
            {
                var lastSensorDetails = model.Data!.LastOrDefault();

                if (lastSensorDetails != null)
                {
                    result["UnitOfMeasureYAxis"] = lastSensorDetails.UoM;
                }
            }

            int? firstItemTotalSeconds = null;

            foreach (var item in model.Data!)
            {
                if (!string.IsNullOrEmpty(legacyModel.WebIdOfMaturity) && item.WebId == legacyModel.WebIdOfMaturity)
                {
                    continue;
                }

                foreach (var dataPointsPerSensor in item.DataPoints)
                {
                    if (firstItemTotalSeconds == null)
                    {
                        firstItemTotalSeconds = (int)(dataPointsPerSensor.ObsId.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                    }

                    var objectToAdd = new JsonObject
                    {
                        { "Value", dataPointsPerSensor.Good ? Convert.ToString(dataPointsPerSensor.Value) : "NA"},
                        { "Interval", Math.Round(Math.Abs((dataPointsPerSensor.ObsId - startTime).TotalHours), 6)},
                        { "ObsID", dataPointsPerSensor.ObsId},
                        { "BadValue", !dataPointsPerSensor.Good},
                        { "ValueSpecialValueType", dataPointsPerSensor.Value is double && double.IsNaN(dataPointsPerSensor.Value)? (byte)SpecialValueType.NaN : null},
                        { "IntervalSpecialValueType", null},
                    };

                    if (!string.IsNullOrEmpty(legacyModel.WebIdOfMaturity))
                    {
                        var sensorDetails = model.Data.FirstOrDefault(x => x.WebId == legacyModel.WebIdOfMaturity);

                        if (sensorDetails != null)
                        {
                            var currentValueIndex = item.DataPoints.IndexOf(dataPointsPerSensor);

                            var maturityDatapoint = sensorDetails.DataPoints[currentValueIndex];

                            objectToAdd["Interval"] = maturityDatapoint.Value;

                        }
                    }

                    dataPoints.Add(objectToAdd);
                }
            }

            result["FirstItemTotalSeconds"] = firstItemTotalSeconds;

            return result;
        }
        public async Task<JsonObject> MapPlotData(SensorDataResponse model, PlotDataRequest legacyModel, string module, List<string> webIds)
        {
            var errors = model.Errors.FirstOrDefault();

            var sensorPlotData = new JsonArray();

            var result = new JsonObject
            {
                  //in AVEVAPIwebAPI package there is method called ExecuteWithCachingNotFoundWebIdsAsync which is used here 
                 //and its return ErrorCode.DeleteSensor which in logical and business term is correct but in microservices which are using this legacy end point 
                 //it`s checking for (byte)WarningStatusCodes.DeletedWebOnPI which has value of 2 that why we are using WarningStatusCodes.DeletedAttributeWebId here 
                 { "WarningCode", errors != null && errors.ErrorCode ==  (byte)ErrorCode.DeleteSensor ? (byte)WarningCode.DeletedAttributeWebId  : (byte)WarningCode.Success},
                 { "SensorPlotData", sensorPlotData},
            };

            if (errors != null)
            {
                var missingItem = new JsonObject
                {
                    { "SensorName", null},
                    { "SensorDescription", null},
                    { "DeviceName", null},
                    { "WebId", errors.Identifier},
                    { "UoM", null},
                    { "DataPoints", null},
                    { "TotalCount", 0},
                    { "BadCount", 0},
                };

                sensorPlotData.Add(missingItem);

                return result;
            }

            var sensorsInfo = await GetSensorsDataAsync(module, webIds);
            var devicesInfo = await GetSensorsDeviceAsync(module, webIds);

            foreach (var item in model.Data)
            {
                var sensorInfo = sensorsInfo.Sensors
                    .FirstOrDefault(x => x.WebId == item.WebId);
                var deviceInfo = devicesInfo.SensorDeviceInfo
                    .FirstOrDefault(x => x.WebId == item.WebId);
                var deviceName = deviceInfo?.Device.Name;

                var dataPoints = new JsonArray();

                var itemToAdd = new JsonObject
                {
                    { "SensorName", item.Name},
                    { "SensorDescription", sensorInfo != null ? sensorInfo.Description : null},
                    { "DeviceName", deviceName != null ? deviceName : null},
                    { "WebId", item.WebId},
                    { "UoM", string.IsNullOrEmpty(item.UoM) ? "dim" : item.UoM},
                    { "DataPoints", dataPoints},
                    { "TotalCount", item.DataPoints.Count},
                };

                var badCount = 0;

                foreach (var dataPoint in item.DataPoints)
                {
                    var dataPointForAdd = new JsonObject
                    {
                        { "Value", dataPoint.Good ? Convert.ToString(dataPoint.Value) : "NA"},
                        { "Interval", Math.Round((dataPoint.ObsId - TimeParser.Parse(legacyModel.StartTime)).TotalHours, 6)},
                        { "ObsID", dataPoint.ObsId},
                        { "BadValue", !dataPoint.Good},
                        { "ValueSpecialValueType", null},
                        { "IntervalSpecialValueType", null},
                    };

                    if (!dataPoint.Good)
                    {
                        badCount = badCount + 1;
                    }

                    dataPoints.Add(dataPointForAdd);
                }

                itemToAdd.Add("BadCount", badCount);

                sensorPlotData.Add(itemToAdd);
            }

            return result;
        }
        public JsonObject MapInterpolatedAtTimeData(SensorDataResponse model)
        {
            var errors = model.Errors.FirstOrDefault();

            var item = new JsonArray();

            var response = new JsonObject();

            if (errors != null)
            {
                response["WebId"] = errors.Identifier;
            }
            else
            {
                response["Items"] = item;
            }

            var result = new JsonObject
            {
                 { "IsSuccessStatusCode", errors is null },
                  //in AVEVAPIwebAPI package there is method called ExecuteWithCachingNotFoundWebIdsAsync which is used here 
                 //and its return ErrorCode.DeleteSensor which in logical and business term is correct but in microservices which are using this legacy end point 
                 //it`s checking for (byte)WarningStatusCodes.DeletedWebOnPI which has value of 2 that why we are using WarningStatusCodes.DeletedAttributeWebId here 
                 { "WarningCode", errors != null && errors.ErrorCode ==  (byte)ErrorCode.DeleteSensor ? (byte)WarningCode.DeletedAttributeWebId : (byte)WarningCode.Success},
                 { "Response", response},
            };

            if (errors != null)
            {
                return result;
            }

            foreach (var sensorDetails in model.Data)
            {
                var itemDetailsForAdd = new JsonArray();

                var objectToAdd = new JsonObject
                {
                    { "WebId" , sensorDetails.WebId},
                    { "Items" , itemDetailsForAdd}
                };

                foreach (var dataPoint in sensorDetails.DataPoints)
                {
                    var dataPointForAdd = new JsonObject
                    {
                        { "Timestamp", dataPoint.ObsId},
                        { "Value", JsonValue.Create(dataPoint.Value)},
                        { "Good", dataPoint.Good},
                    };

                    itemDetailsForAdd.Add(dataPointForAdd);
                }

                item.Add(objectToAdd);
            }

            return result;

        }
        public JsonObject InterpolatedDataAtTimes(SensorDataResponse model, LegacyShowSignalsRequest legacyModel)
        {
            var errors = model.Errors.FirstOrDefault();

            var dataPoints = new JsonArray();

            var result = new JsonObject
            {
                  //in AVEVAPIwebAPI package there is method called ExecuteWithCachingNotFoundWebIdsAsync which is used here 
                 //and its return ErrorCode.DeleteSensor which in logical and business term is correct but in microservices which are using this legacy end point 
                 //it`s checking for (byte)WarningStatusCodes.DeletedWebOnPI which has value of 2 that why we are using WarningStatusCodes.DeletedAttributeWebId here 
                  { "WarningCode", errors != null && errors.ErrorCode ==  (byte)ErrorCode.DeleteSensor ? (byte)WarningCode.DeletedAttributeWebId : (byte)WarningCode.Success},
                  { "DataPoints", dataPoints},
            };

            int firstItemTotalSeconds = 0;
            var startTime = legacyModel.StartTime.ParseISO8601();

            foreach (var sensorDetails in model.Data)
            {
                if (!string.IsNullOrEmpty(legacyModel.WebIdOfMaturity)
                    && string.Equals(sensorDetails.WebId, legacyModel.WebIdOfMaturity, StringComparison.CurrentCultureIgnoreCase))
                {
                    if (!result.ContainsKey("UnitOfMeasureXAxis"))
                        result.Add("UnitOfMeasureXAxis", sensorDetails.UoM);
                }
                else
                {
                    if (!result.ContainsKey("UnitOfMeasureYAxis"))
                        result.Add("UnitOfMeasureYAxis", sensorDetails.UoM);
                }

                foreach (var item in sensorDetails.DataPoints)
                {
                    var objectToAdd = new JsonObject
                    {
                        { "BadValue", !item.Good},
                        //ToDo
                        { "IntervalSpecialValueType", null},
                        { "ValueSpecialValueType", item.Good ? null: (byte)SpecialValueType.NaN},
                        { "Value", JsonValue.Create(item.Value)},
                        { "ObsID", item.ObsId },
                        { "Interval", Math.Round((item.ObsId - startTime).TotalSeconds / 3600.0, 6, MidpointRounding.AwayFromZero) }
                    };

                    dataPoints.Add(objectToAdd);
                }
            }

            result.Add("FirstItemTotalSeconds", firstItemTotalSeconds);

            return result;
        }
        public JsonObject MapDeviceEventFrames(EventframeListResponse model, DeviceListResponse model2, bool isMultipleDevice)
        {
            var errors = model.Errors.FirstOrDefault();

            if (model.Errors != null && model.Errors.Count > 0)
            {
                throw new DCPRawBadRequestException($"The following ParentIds did not complete successfully: {(isMultipleDevice ? "2" : "0")}.");
            }

            var data = new JsonArray();

            var dataTypes = new JsonObject();

            var result = new JsonObject
            {
                  { "WarningCode", errors != null ? errors.ErrorCode : (byte)WarningCode.Success},
                  { "Data", data},
                  { "DataTypes", dataTypes},
            };

            foreach (var item in model.Attributes)
            {
                dataTypes.Add(item.Name, item.ValueType);
            }

            var dateTimeNow = DateTime.UtcNow;

            foreach (var item in model.Eventframes)
            {
                var objectToAdd = new JsonObject
                {
                    {"Id", item.Id },
                    {"StartTime", item.StartTime },
                    {"EndTime", item.EndTime },
                    {"DeviceWebId", item.RefWebId },
                    {"DeviceName", model2.Devices?.FirstOrDefault(d => d.WebId == item.RefWebId)?.Name },
                };

                var batchStartTime = item.StartTime.ParseISO8601();
                var batchEndTime = item.EndTime.ParseISO8601();

                if (batchEndTime.Date == DateTime.MaxValue.Date)
                {
                    batchEndTime = dateTimeNow;
                }

                var timeDifference = (batchEndTime - batchStartTime);

                objectToAdd.Add("Interval", timeDifference.ToString());
                objectToAdd.Add("IntervalInSeconds", timeDifference.TotalSeconds);

                var values = new JsonObject();

                foreach (var value in item.Values)
                {
                    values.Add(value.Name, JsonValue.Create(value.Value));
                }

                objectToAdd.Add("Values", values);

                data.Add(objectToAdd);
            }


            return result;
        }
        public JsonObject MapDeviceInProgressEventFrames(EventframeListResponse model)
        {
            var errors = model.Errors.FirstOrDefault();

            if (model.Errors != null && model.Errors.Count > 0)
            {
                throw new DCPRawBadRequestException("The following ParentIds did not complete successfully: 0.");
            }

            var data = new JsonArray();

            var dataTypes = new JsonObject();

            var result = new JsonObject
            {
                  { "WarningCode", errors != null ? errors.ErrorCode : (byte)WarningCode.Success},
                  { "Data", data},
                  { "DataTypes", dataTypes},
            };

            foreach (var item in model.Attributes)
            {
                dataTypes.Add(item.Name, item.ValueType);
            }

            var dateTimeNow = DateTime.UtcNow;

            foreach (var item in model.Eventframes)
            {
                var objectToAdd = new JsonObject
                {
                    {"Id", item.Id },
                    {"StartTime", item.StartTime },
                    {"EndTime", item.EndTime },
                    {"DeviceWebId", item.RefWebId },
                };

                var batchStartTime = DateTime.Parse(item.StartTime, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind).ToUniversalTime();
                var batchEndTime = DateTime.Parse(item.EndTime, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind).ToUniversalTime();

                if (batchEndTime.Date == DateTime.MaxValue.Date)
                {
                    batchEndTime = dateTimeNow;
                }

                var timeDifference = (batchEndTime - batchStartTime);

                objectToAdd.Add("Interval", timeDifference.ToString());
                objectToAdd.Add("IntervalInSeconds", timeDifference.TotalSeconds);

                var values = new JsonObject();

                foreach (var value in item.Values)
                {
                    values.Add(value.Name, JsonValue.Create(value.Value));
                }

                objectToAdd.Add("Values", values);

                data.Add(objectToAdd);
            }


            return result;
        }
        public JsonArray MapActiveBatchesPerDevice(BatchContextListResponse model)
        {
            var result = new JsonArray();

            foreach (var item in model.Eventframes)
            {
                var itemToAdd = new JsonObject
                {
                    { "Id", item.Id }
                };

                result.Add(itemToAdd);
            }

            return result;
        }
        public JsonObject MapEventFrameAttributesValues(EventframeValuesResponse model)
        {
            var errors = model.Errors.FirstOrDefault();

            var items = new JsonArray();

            foreach (var val in model.Values)
            {
                var dataPointForAdd = new JsonObject
                    {
                        { "Name", val.Name},
                        { "Value", new JsonObject {
                            { "Value", val.Good ? val.Value : null },
                            { "Good", val.Good},
                        } }
                    };

                items.Add(dataPointForAdd);
            }

            var result = new JsonObject
            {
                 { "IsSuccessStatusCode", errors == null },
                 //return always 0 for core v1 compatability, incorrect logic
                 { "WarningCode", (byte)WarningCode.Success},
                 { "Response", items.Count != 0 ?
                                new JsonObject
                                {
                                    { "Items", items}
                                }
                                : null
                 }
            };

            return result;
        }
        public JsonObject MapEventFrameEndTime(EventFrameResponse model)
        {
            var errors = model.Errors.FirstOrDefault();

            JsonObject? response = null;

            if (model.Eventframe != null)
            {
                response = new JsonObject
                {
                    { "Id", model.Eventframe.Id},
                    { "EndTime", model.Eventframe.EndTime},
                };
            }

            var result = new JsonObject
            {
                 { "IsSuccessStatusCode", errors == null },
                 //return always 0 for core v1 compatability, incorrect logic
                 { "WarningCode", (byte)WarningCode.Success},
                 { "Response", response},
            };

            return result;
        }
        public JsonObject MapEventFramesInProgress(EventframeListResponse model, EventFramesInProgressRequest legacyModel)
        {
            if (model.Errors != null && model.Errors.Count > 0)
            {
                var errorResponse = new JsonObject
                {
                     { "IsSuccessStatusCode", true },
                     { "Response", model.Errors.FirstOrDefault()?.Error},
                };

                return errorResponse;
            }

            var response = new JsonObject();
            var indexMap = new JsonObject();

            foreach (var item in legacyModel.DeviceWebIds)
            {
                var currentItem = legacyModel.DeviceWebIds.IndexOf(item).ToString();

                indexMap[currentItem] = item;

                var statusCode = (int)HttpStatusCode.OK;

                var itemsArray = new JsonArray();
                var content = new JsonObject
                {
                    { "Items", itemsArray }
                };

                var errorDetails = model.Errors.Where(x => x.Identifier == item).FirstOrDefault();

                if (errorDetails != null)
                {
                    statusCode = errorDetails.ErrorCode;

                    var element = new JsonObject
                    {
                        { "Errors", new JsonArray
                            {
                               $"WebID '{errorDetails.Identifier}' is not found."
                            }
                        }


                    };

                    itemsArray.Add(element);
                }

                foreach (var ef in model.Eventframes.Where(x => x.RefWebId == item))
                {
                    itemsArray.Add(new JsonObject
                    {
                        {"Id",ef.Id },
                        {"StartTime",ef.StartTime },
                        {"RefElementWebIds", new JsonArray{ ef.RefWebId } },
                    });
                }

                var responseDetails = new JsonObject
                {
                    { "Status", statusCode},
                    { "Headers", new JsonObject
                        {
                            {
                                "Content-Type","application/json; charset=utf-8"
                            }
                        }
                    },
                    { "Content", content}
                };

                response.Add(currentItem, responseDetails);

            }

            var errors = model.Errors.FirstOrDefault();

            var result = new JsonObject
            {
                 { "IsSuccessStatusCode", errors is null },
                 { "IndexesToDeviceWebIds", indexMap },
                 { "Response", response},
            };

            return result;
        }
        public JsonArray MapDataSourceUnits(DatasourceUnitResponse model)
        {
            var result = new JsonArray();

            foreach (var unit in model.Units)
            {
                var objectToAdd = new JsonObject
                {
                    { "UoMClass", unit.Class },
                    { "UoMName", unit.Name },
                    { "UoMAbbreviation", unit.Abbreviation },
                };

                result.Add(objectToAdd);
            }

            return result;
        }
        public JsonObject MapRecordedAtTimesData(SensorDataResponse model)
        {
            var errors = model.Errors.FirstOrDefault();

            var item = new JsonArray();

            var response = new JsonObject();

            if (errors != null)
            {
                response["WebId"] = errors.Identifier;
            }
            else
            {
                response["Items"] = item;
            }

            var result = new JsonObject
            {
                 { "IsSuccessStatusCode", errors is null},
                 //in AVEVAPIwebAPI package there is method called ExecuteWithCachingNotFoundWebIdsAsync which is used here 
                 //and its return ErrorCode.DeleteSensor which in logical and business term is correct but in microservices which are using this legacy end point 
                 //it`s checking for (byte)WarningStatusCodes.DeletedWebOnPI which has value of 2 that why we are using WarningStatusCodes.DeletedAttributeWebId here 
                 { "WarningCode", errors != null && errors.ErrorCode ==  (byte)ErrorCode.DeleteSensor ? (byte)WarningCode.DeletedAttributeWebId : (byte)WarningCode.Success},
                 { "Response", response},
            };

            if (errors != null)
            {
                return result;
            }

            foreach (var sensorDetails in model.Data)
            {
                var itemDetailsForAdd = new JsonArray();

                var objectToAdd = new JsonObject
                {
                    { "WebId" , sensorDetails.WebId},
                    { "Items" , itemDetailsForAdd}
                };

                foreach (var dataPoint in sensorDetails.DataPoints)
                {
                    var dataPointForAdd = new JsonObject
                    {
                        { "Timestamp", dataPoint.ObsId},
                        { "Value", JsonValue.Create(dataPoint.Value)},
                        { "Good", dataPoint.Good},
                    };

                    itemDetailsForAdd.Add(dataPointForAdd);
                }

                item.Add(objectToAdd);
            }

            return result;
        }
        public JsonObject MapSingleBatchIntermediate(List<InProgressEventDTO> model)
        {
            var options = new JsonSerializerOptions
            {
                NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
            };

            var result = new JsonObject
            {
                ["ID"] = 0,
                ["SiteId"] = 0,
                ["BatchID"] = null,
                ["BatchStartTime"] = null,
                ["JBatchValues"] = null,
                ["EventFrameId"] = null,
                ["DeviceWebId"] = null,
                ["BadValue"] = false,
                ["UpdateAttems"] = 0,
                ["IsNotifiedSend"] = false,
                ["IsNewSignUpCreated"] = false
            };

            if (model.Count > 0)
            {
                var item = model[0];

                result["ID"] = item.Id;
                result["SiteId"] = item.SiteId;
                result["EventFrameId"] = WebIdEncoding.Decode(item.EventId).WebId;
                result["BatchStartTime"] = item.StartTime;
                result["DeviceWebId"] = WebIdEncoding.Decode(item.DeviceWebId).WebId;
                result["JBatchValues"] = JsonSerializer.Serialize(
                    item.JEventValues.Select(v => new
                    {
                        v.Name,
                        Value = new
                        {
                            Value = v.Value,
                            Good = v.Good
                        }
                    }),
                options)
;
            }

            return result;
        }
        public JsonArray MapBatchIntermediate(List<InProgressEventDTO> model)
        {
            var result = new JsonArray();

            var options = new JsonSerializerOptions
            {
                NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
            };


            foreach (var item in model)
            {
                var itemToAdd = new JsonObject
                {
                    { "Id", item.Id},
                    { "EventFrameId", WebIdEncoding.Decode(item.EventId).WebId },
                    { "BatchStartTime", item.StartTime},
                    { "DeviceWebId", WebIdEncoding.Decode(item.DeviceWebId).WebId},
                    { "SiteId", item.SiteId},
                    { "JBatchValues", JsonSerializer.Serialize(
                    item.JEventValues.Select(v => new
                    {
                        v.Name,
                        Value = new
                        {
                            Value = v.Value,
                            Good = v.Good
                        }
                    }),
                options)},
                };
                result.Add(itemToAdd);
            }

            return result;
        }
        public JsonObject MapGlobalFilter(GlobalFilterListResponse model)
        {
            var result = new JsonObject
            {
                { "PIFilterDTO", JsonSerializer.SerializeToNode(model.Elements)},
                { "HasChildren", !model.Elements.Any(e => e.ValueType != null)}
            };

            return result;
        }
        public JsonArray MapElementMetaData(ElementMetaDataResponse model)
        {
            var result = new JsonArray();

            foreach (var item in model.MetaData)
            {
                var objectToAdd = new JsonObject
                {
                    { "Name" , item.Name},
                    { "Description", item.Description},
                    { "Value", item.Value}
                };

                result.Add(objectToAdd);
            }

            return result;
        }
        public JsonArray MapDataSource(List<DatasourceConfigurationDTO> model, bool includeConnectionString = true)
        {
            var result = new JsonArray();

            foreach (var item in model)
            {
                var objectToAdd = new JsonObject
                {
                     { "ID", item.Id},
                     { "Name" , item.Name},
                     { "Type", (byte)ParseConnectionType(item.ConnectionType)},
                     { "ConnectionString", includeConnectionString ? new JsonObject {
                         { item.ConnectionType.Replace(" ", ""), item.ConnectionString}
                     }: null},
                     { "IsValidated",item.IsValidated},
                     { "SupportedDataTypes",null},
                     {"SiteID" , item.SiteId}
                };

                result.Add(objectToAdd);
            }

            return result;
        }
        public JsonArray MapAllDevicesPerSite(DeviceListResponse model)
        {
            var result = new JsonArray();

            foreach (var item in model.Devices)
            {
                var objectToAdd = new JsonObject
                {
                    {"Name" , item.Name },
                    {"WebId" , item.WebId },
                    {"Description" , item.Description },
                    { "Path",  string.Join('-',item.Path.Select(p => p.Name))},
                };

                result.Add(objectToAdd);
            }

            return result;
        }

        public JsonArray MapModules(List<ModuleConfigurationDTO> model)
        {
            var result = new JsonArray();

            foreach (var item in model)
            {
                var rConnectionWrapper = new JsonObject
                {
                    ["RConnection"] = item.ConnectionString
                };

                var objectToAdd = new JsonObject
                {
                    ["ID"] = item.Id,
                    ["ModuleName"] = item.Name,
                    ["RConnection"] = rConnectionWrapper,
                    ["SiteID"] = item.SiteId,
                    ["IsEnable"] = item.IsEnabled,
                    ["IsValidated"] = item.IsValidated,
                    ["DataType"] = null
                };

                result.Add(objectToAdd);
            }

            return result;
        }

        public async Task<List<UserInfoDTO>> MapUsersDetails(List<Models.Models.User.UserListItemDTO> model, bool includeRoles = false)
        {
            var result = new List<UserInfoDTO>();

            var userIds = model.Select(x => x.Id);

            var usersDetails = await _dbContext.User
                .Where(x => userIds.Contains(x.Id))
                .Select(x => new
                {
                    x.Id,
                    x.FirstName,
                    x.LastName,
                    x.DateFormat,
                    x.TimeFormat,
                    x.Permissions
                })
                .AsNoTracking()
                .ToListAsync();

            foreach (var item in model)
            {
                var itemToAdd = new UserInfoDTO
                {

                    UserID = item.Id,
                    DisplayName = item.DisplayName,
                    Email = item.Email,
                    UserName = item.UnixId,

                };

                var userDetails = usersDetails.FirstOrDefault(x => x.Id == item.Id);
                if (userDetails != null)
                {
                    itemToAdd.FirstName = userDetails.FirstName;
                    itemToAdd.LastName = userDetails.LastName;
                    itemToAdd.DateFormat = userDetails.DateFormat;
                    itemToAdd.TimeFormat = userDetails.TimeFormat;

                    if (includeRoles)
                    {
                        itemToAdd.Roles = LegacyPermissionsToGroups(userDetails.Permissions);
                    }
                }

                result.Add(itemToAdd);
            }

            return result;
        }
        public Models.Legacy.Site.LegacySiteConfigurationDTO MapSiteDetails(SiteConfigurationFormDTO siteModel, List<DatasourceConfigurationDTO> datasourceModel, List<ModuleConfigurationDTO> moduleModel)
        {
            var result = new Models.Legacy.Site.LegacySiteConfigurationDTO
            {
                ID = siteModel.Id,
                Name = siteModel.Name,
                TimeZone = siteModel.TimeZone,
                ShortName = siteModel.ShortName,
                IsEnable = siteModel.IsEnabled,
            };

            var dataSourceDetails = datasourceModel.FirstOrDefault();

            if (dataSourceDetails != null)
            {
                result.DataSourceId = dataSourceDetails.Id;
                result.IsDataSourceValidated = dataSourceDetails.IsValidated;
                result.ConnectionType = (byte)ParseConnectionType(dataSourceDetails.ConnectionType);
                var oldConnectionStringFormat = AddLeadingKey(dataSourceDetails.ConnectionString, dataSourceDetails.ConnectionType);

                result.ConnectionString = oldConnectionStringFormat;
            }

            var moduleDetails = moduleModel.FirstOrDefault();
            if (moduleDetails != null)
            {
                result.IsModuleValidated = moduleDetails.IsValidated;
                result.IsModuleEnabled = moduleDetails.IsEnabled;


                var rConnectionWrapper = new JsonObject
                {
                    ["RConnection"] = moduleDetails.ConnectionString
                };

                result.RConnection = rConnectionWrapper;
            }

            return result;
        }
        public List<Models.Legacy.Site.LegacySiteConfigurationDTO> MapSitesDetails(List<SiteListItemDTO> sitesModel, List<DatasourceConfigurationDTO> datasourceModel, List<ModuleConfigurationDTO> moduleModel)
        {
            var result = new List<Models.Legacy.Site.LegacySiteConfigurationDTO>();

            foreach (var item in sitesModel)
            {
                var itemToAdd = new Models.Legacy.Site.LegacySiteConfigurationDTO
                {
                    ID = item.Id,
                    Name = item.Name,
                    TimeZone = item.TimeZone,
                    ShortName = item.ShortName,
                    IsEnable = item.IsEnabled,
                };

                var datasourceDetails = datasourceModel.FirstOrDefault(x => x.SiteId == item.Id);

                if (datasourceDetails != null)
                {
                    itemToAdd.DataSourceId = datasourceDetails.Id;
                    itemToAdd.ConnectionType = (byte)ParseConnectionType(datasourceDetails.ConnectionType);
                    itemToAdd.IsDataSourceValidated = datasourceDetails.IsValidated;

                    var oldConnectionStringFormat = AddLeadingKey(datasourceDetails.ConnectionString, datasourceDetails.ConnectionType);

                    itemToAdd.ConnectionString = oldConnectionStringFormat;
                }

                var moduleDetails = moduleModel.FirstOrDefault(x => x.SiteId == item.Id);

                if (moduleDetails != null)
                {
                    itemToAdd.IsModuleValidated = moduleDetails.IsValidated;
                    itemToAdd.IsModuleEnabled = moduleDetails.IsEnabled;

                    var rConnectionWrapper = new JsonObject
                    {
                        ["RConnection"] = moduleDetails.ConnectionString
                    };

                    itemToAdd.RConnection = rConnectionWrapper;
                }

                result.Add(itemToAdd);
            }

            return result;
        }
        public List<LegacyValueTextViewModel> MapAllSiteWithActiveModule(List<SiteListItemDTO> sitesModel)
        {
            var result = new List<LegacyValueTextViewModel>();

            foreach (var site in sitesModel)
            {
                var itemToAdd = new LegacyValueTextViewModel
                {
                    Value = site.Id,
                    Text = site.Name
                };

                result.Add(itemToAdd);
            }

            return result;
        }
        public JsonObject MapGetSingleSite(SiteConfigurationFormDTO siteModel, List<DatasourceConfigurationDTO> datasourceModel, List<ModuleConfigurationDTO> moduleModel)
        {
            var modules = new JsonArray();
            var datasources = new JsonArray();


            var result = new JsonObject
            {
                { "ID", siteModel.Id},
                { "Name", siteModel.Name},
                { "ShortName", siteModel.ShortName},
                { "TimeZone", siteModel.TimeZone},
                { "IsEnable", siteModel.IsEnabled},
                { "SiteType", (byte)siteModel.Type},
                { "Modules", modules},
                { "DataSources", datasources}
            };

            foreach (var module in moduleModel)
            {
                var rConnectionWrapper = new JsonObject
                {
                    ["RConnection"] = module.ConnectionString
                };

                var moduleForAdd = new JsonObject
                {
                    {"ID", module.Id },
                    {"ModuleName", module.Name },
                    {"IsEnable", module.IsEnabled },
                    {"IsValidated", module.IsValidated },
                    {"SiteID", siteModel.Id },
                    {"RConnection",  rConnectionWrapper },
                    {"DataType",  null }

                };

                modules.Add(moduleForAdd);
            }


            foreach (var datasource in datasourceModel)
            {
                var oldConnectionStringFormat = AddLeadingKey(datasource.ConnectionString, datasource.ConnectionType);

                var objectToAdd = new JsonObject
                    {
                         { "ID", datasource.Id},
                         { "Name" , datasource.Name},
                         { "Type", (byte)ParseConnectionType(datasource.ConnectionType)},
                         { "ConnectionString", oldConnectionStringFormat},
                         { "IsValidated",datasource.IsValidated},
                         { "SupportedDataTypes",null},
                         {"SiteID" , siteModel.Id}
                    };

                datasources.Add(objectToAdd);
            }

            return result;
        }

        public JsonNode MapGroupedDeviceSensors(GroupedSensorListResponse model)
        {
            if (model.Errors != null && model.Errors.Count() > 0)
            {
                var errorMessage = new JsonObject
                {
                    { "Errors", new JsonArray
                         {
                            model.Errors[0].Error
                         }
                    }
                };

                throw new DCPRawBadRequestException(errorMessage.ToJsonString());
            }

            var result = new JsonArray();

            foreach (var group in model.Groups)
            {
                var groupSensors = new JsonArray();
                var groupArray = new JsonObject
                {
                    { "CategoryName", group.GroupName },
                    { "SensorIdentifiers", groupSensors },
                };


                foreach (var item in group.Sensors)
                {
                    var itemToAdd = new JsonObject
                    {
                        { "Name" , item.Name },
                        { "Description" , item.Description },
                        { "WebId" , item.WebId },
                        { "ValueType" , item.ValueType },
                        { "IsStep" , item.IsStep },
                    };

                    groupSensors.Add(itemToAdd);
                }

                result.Add(groupArray);
            }

            return result;
        }

        public async Task<List<InProgressEventDTO>> GetCachedBatchEventsAsync(EventSubscriptionQuery model, Framework.Hosting.Models.UserContextDTO user)
        {
            HasPermission(new List<string>
            {
                Permissions.ModuleIndependent.ADMIN,
                Permissions.ModuleIndependent.VIEWER
            }, user, 0, true);

            var query = _dbContext.InProgressEvent.AsQueryable();

            if (model.DeviceWebId.Count > 0)
            {
                query = query.Where(x => model.DeviceWebId.Any(id => x.DeviceWebId.EndsWith(id)));
            }

            if (model.EventframeId.Count > 0)
            {
                query = query.Where(x => model.EventframeId.Any(id => x.EventId.EndsWith(id)));
            }

            if (model.SiteId.Count > 0)
            {
                query = query.Where(x => model.SiteId.Contains(x.Datasource!.SiteId));
            }

            var result = await query
                .Select(x => new InProgressEventDTO
                {
                    DeviceWebId = x.DeviceWebId,
                    SiteId = x.Datasource!.SiteId,
                    EventId = x.EventId,
                    Id = x.Id,
                    StartTime = x.EventStartTime,
                    JEventValues = x.JEventValues.Select(v => new EventFrameValueDTO
                    {
                        Good = v.Good,
                        Name = v.Name,
                        //Check if value type conversion can be and should be done
                        Value = v.Value,
                    }).ToList(),
                })
                .AsNoTracking()
                .ToListAsync();

            return result;
        }

        public JsonArray MapDeviceSensorsByGroup(SensorListResponse model)
        {
            if (model.Errors != null && model.Errors.Count > 0)
            {
                throw new DCPRawBadRequestException("The following ParentIds did not complete successfully: 0.");
            }


            var result = new JsonArray();

            foreach (var item in model.Sensors)
            {
                var itemToAdd = new JsonObject
                    {
                        { "Name" , item.Name },
                        { "Description" , item.Description },
                        { "WebId" , item.WebId },
                        { "ValueType" , item.ValueType },
                        { "IsStep" , item.IsStep },
                    };

                result.Add(itemToAdd);
            }

            return result;
        }

        public JsonArray MapInterpolatedDataWithBatchContext(SensorDataWithBatchContextResponse model, LegacyRequestSensorData legacyModel, string module, List<string> webIds)
        {
            //TODO Check if in case of missing device/sensor this ifnromation is returned - the microservices are using it 
            var result = new JsonArray();

            var errors = model.Errors.FirstOrDefault();

            var sensorsInfo = GetSensorsDataAsync(module, webIds);

            foreach (var item in legacyModel.DeviceWebIds)
            {
                var batchContext = new JsonArray();
                var sensorDataArray = new JsonArray();
                var sensorWebIds = legacyModel.SensorsWebIdsPerDeviceWebid
                    .Where(kvp => string.Equals(kvp.Value, item, StringComparison.Ordinal))
                    .Select(kvp => kvp.Key);

                var error = model.Errors.Where(x => sensorWebIds.Contains(x.Identifier)).FirstOrDefault();

                var objectToAdd = new JsonObject
                {
                     //in AVEVAPIwebAPI package there is method called ExecuteWithCachingNotFoundWebIdsAsync which is used here 
                     //and its return ErrorCode.DeleteSensor which in logical and business term is correct but in microservices which are using this legacy end point 
                     //it`s checking for (byte)WarningStatusCodes.DeletedWebOnPI which has value of 2 that why we are using WarningStatusCodes.DeletedAttributeWebId here 
                     { "WarningCode", error != null && error.ErrorCode ==  (byte)ErrorCode.DeleteSensor ? (byte)WarningCode.DeletedAttributeWebId : (byte)WarningCode.Success},
                     { "DeviceWebId", item},
                     { "BatchContext", errors != null ? null : batchContext},
                     {"SensorData", sensorDataArray }
                };

                var batchDetails = model.BatchData.FirstOrDefault(x => x.DeviceWebId == item);

                if (batchDetails != null)
                {
                    foreach (var eventFrame in batchDetails.EventFrames)
                    {
                        var eventFrameForAdd = new JsonObject
                        {
                            { "WebId", eventFrame.Id },
                            { "StartTime", eventFrame.StartTime },
                            { "EndTime", eventFrame.EndTime },
                        };

                        batchContext.Add(eventFrameForAdd);
                    }
                }

                result.Add(objectToAdd);

                foreach (var sensorWebId in sensorWebIds)
                {
                    if (error != null && error.Identifier == sensorWebId)
                    {
                        var deletedItem = new JsonObject
                        {
                            { "Name" , null},
                            { "WebId" , error.Identifier},
                            { "UoM" , null},
                            { "ValueType" , null},
                            { "XValues" , null },
                            { "YValues" , null },
                            { "FormulaSensors" , null },
                            { "BadCount" , 0 },
                            { "TotalCount" , 0 },
                        };

                        sensorDataArray.Add(deletedItem);
                        continue;
                    }

                    if (errors != null)
                    {
                        continue;
                    }

                    var sensorDetailsFromPI = model.SensorData
                    .FirstOrDefault(x => x.WebId == sensorWebId);

                    var sensorInfo = sensorsInfo.Result.Sensors
                        .FirstOrDefault(x => x.WebId == sensorWebId);

                    if (sensorInfo != null)
                    {
                        var deviceWebId = legacyModel.SensorsWebIdsPerDeviceWebid[sensorWebId];

                        var xValue = new JsonArray();
                        var yValue = new JsonArray();

                        var sensorData = new JsonObject
                        {
                            { "Name" , sensorInfo.Name},
                            { "WebId" , sensorWebId},
                            { "UoM" , sensorInfo == null ? null : sensorInfo.UoM},
                            { "ValueType" , sensorInfo == null ? null : sensorInfo.ValueType},
                            { "XValues" , xValue },
                            { "YValues" , yValue },
                            //MVTODO: check from where to get value
                            { "FormulaSensors" , null },
                        };

                        sensorDataArray.Add(sensorData);

                        int badCount = 0;
                        int totalCount = 0;

                        if (sensorDetailsFromPI != null)
                        {
                            foreach (var dataPoints in sensorDetailsFromPI.DataPoints)
                            {
                                totalCount += 1;

                                if (!dataPoints.Good)
                                {
                                    badCount += 1;
                                    xValue.Add(dataPoints.ObsId);
                                    yValue.Add("NA");

                                    continue;
                                }

                                xValue.Add(dataPoints.ObsId);
                                object? yVal = dataPoints.Value;

                                if (yVal is EnumValueDTO enumValue)
                                {
                                    yValue.Add(enumValue.Value.ToString());
                                }
                                else
                                {
                                    yValue.Add(yVal);
                                }
                            }
                        }

                        sensorData.Add("BadCount", badCount);
                        sensorData.Add("TotalCount", totalCount);
                    }
                }
            }

            return result;
        }

        public JsonArray MapPlotDataWithContext(SensorDataWithBatchContextResponse model, LegacyPlotDataWithBatchContextRequest legacyModel, string module, List<string> webIds)
        {
            //TODO Check if in case of missing device/sensor this ifnromation is returned - the microservices are using it 
            var result = new JsonArray();

            var errors = model.Errors.FirstOrDefault();
            var sensorsInfo = GetSensorsDataAsync(module, webIds);

            foreach (var item in legacyModel.DeviceWebIds.Distinct())
            {
                var batchContext = new JsonArray();
                var sensorDataArray = new JsonArray();

                var sensorWebIds = legacyModel.SensorsWebIdsPerDeviceWebid
                    .Where(kvp => string.Equals(kvp.Value, item, StringComparison.Ordinal))
                    .Select(kvp => kvp.Key);

                var error = model.Errors.Where(x => sensorWebIds.Contains(x.Identifier)).FirstOrDefault();

                var objectToAdd = new JsonObject
                {
                     //in AVEVAPIwebAPI package there is method called ExecuteWithCachingNotFoundWebIdsAsync which is used here 
                     //and its return ErrorCode.DeleteSensor which in logical and business term is correct but in microservices which are using this legacy end point 
                     //it`s checking for (byte)WarningStatusCodes.DeletedWebOnPI which has value of 2 that why we are using WarningStatusCodes.DeletedAttributeWebId here 
                     { "WarningCode", error != null && error.ErrorCode ==  (byte)ErrorCode.DeleteSensor ? (byte)WarningCode.DeletedAttributeWebId : (byte)WarningCode.Success},
                     { "DeviceWebId", item},
                     { "BatchContext", errors != null ? null : batchContext},
                     {"SensorData", sensorDataArray }
                };

                result.Add(objectToAdd);

                var batchDetails = model.BatchData
                    .FirstOrDefault(x => x.DeviceWebId == item);

                if (batchDetails != null)
                {
                    foreach (var eventFrame in batchDetails.EventFrames)
                    {
                        var eventFrameForAdd = new JsonObject
                        {
                            { "WebId", eventFrame.Id },
                            { "StartTime", eventFrame.StartTime },
                            { "EndTime", eventFrame.EndTime },
                        };

                        batchContext.Add(eventFrameForAdd);
                    }
                }

                foreach (var sensorWebId in sensorWebIds)
                {
                    if (error != null && error.Identifier == sensorWebId)
                    {
                        var deletedItem = new JsonObject
                        {
                            { "Name" , null},
                            { "WebId" , error.Identifier},
                            { "UoM" , null},
                            { "ValueType" , null},
                            { "XValues" , null },
                            { "YValues" , null },
                            { "FormulaSensors" , null },
                            { "BadCount" , 0 },
                            { "TotalCount" , 0 },
                        };

                        sensorDataArray.Add(deletedItem);
                        continue;
                    }

                    if (errors != null)
                    {
                        continue;
                    }

                    var sensorDetailsFromPI = model.SensorData
                    .FirstOrDefault(x => x.WebId == sensorWebId);

                    var sensorInfo = sensorsInfo.Result.Sensors
                        .FirstOrDefault(x => x.WebId == sensorWebId);

                    if (sensorInfo != null)
                    {
                        var deviceWebId = legacyModel.SensorsWebIdsPerDeviceWebid[sensorWebId];

                        var xValue = new JsonArray();
                        var yValue = new JsonArray();

                        var sensorData = new JsonObject
                        {
                            { "Name" , sensorInfo.Name},
                            { "WebId" , sensorWebId},
                            { "UoM" , sensorInfo == null ? null : sensorInfo.UoM},
                            { "ValueType" , sensorInfo == null ? null : sensorInfo.ValueType},
                            { "XValues" , xValue },
                            { "YValues" , yValue },
                            //MVTODO: check from where to get value
                            { "FormulaSensors" , null },
                        };

                        sensorDataArray.Add(sensorData);

                        int badCount = 0;
                        int totalCount = 0;

                        if (sensorDetailsFromPI != null)
                        {
                            foreach (var dataPoints in sensorDetailsFromPI.DataPoints)
                            {
                                totalCount += 1;

                                if (!dataPoints.Good)
                                {
                                    //This is legacy logic from Core V1: If its last element we are not adding NA values and subtracting one from TotalCount 
                                    //In that way arrays and total counts will be equal
                                    if (sensorDetailsFromPI.DataPoints.LastOrDefault() == dataPoints)
                                    {
                                        totalCount -= 1;

                                        continue;
                                    }

                                    badCount += 1;
                                    xValue.Add(dataPoints.ObsId);
                                    yValue.Add("NA");

                                    continue;
                                }

                                xValue.Add(dataPoints.ObsId);
                                object? yVal = dataPoints.Value;

                                if (yVal is EnumValueDTO enumValue)
                                {
                                    yValue.Add(enumValue.Value.ToString());
                                }
                                else
                                {
                                    yValue.Add(yVal);
                                }
                            }
                        }

                        sensorData.Add("BadCount", badCount);
                        sensorData.Add("TotalCount", totalCount);
                    }
                }
            }

            return result;
        }

        public async Task<ServiceResponse<SensorDataWithBatchContextResponse>> GetDataWithContextWithRefreshAsync(string module, PlotDataWithBatchContextQueryDTO? plotModel = null, InterpolatedDataWithBatchContextQueryDTO? interpolatedModel = null, CancellationToken ct = default)
        {
            _loader.SetModule(module);

            var telemetryModel = new InterpolatedAtTimeDataQueryDTO();
            var eventframeModel = new BatchContextTimeQueryDTO();

            if (plotModel != null)
            {
                telemetryModel = new InterpolatedAtTimeDataQueryDTO
                {
                    Time = new List<DateTime> { DateTime.UtcNow },
                    WebId = plotModel.WebId
                };

                eventframeModel = new BatchContextTimeQueryDTO
                {
                    StartTime = plotModel.StartTime,
                    EndTime = plotModel.EndTime,
                    DeviceWebId = plotModel.DeviceWebId
                };
            }
            else if (interpolatedModel != null)
            {
                telemetryModel = new InterpolatedAtTimeDataQueryDTO
                {
                    Time = new List<DateTime> { DateTime.UtcNow },
                    WebId = interpolatedModel.WebId
                };

                eventframeModel = new BatchContextTimeQueryDTO
                {
                    StartTime = interpolatedModel.StartTime,
                    EndTime = interpolatedModel.EndTime,
                    DeviceWebId = interpolatedModel.DeviceWebId
                };
            }

            var telemetryPipeline = new PluginPipeline<InterpolatedAtTimeDataQueryDTO, SensorDataResponse>(_cache, _serviceProvider, _loader)
                .WithDefaultMapFunction()
                .WithDefaultTransformFunction()
                .WithDefaultReduceFunction();

            var telemetryReponse = await telemetryPipeline.RunMapReduceAsync<ISensorTelemetryDataSource>(
                (datasource, telemetryModel, ct) => datasource.GetInterpolatedDataAtTimesAsync(telemetryModel, ct),
                telemetryModel
            );

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

        public async Task<LegacySiteInfoDTO> GetUserCurrentSiteAsync(Framework.Hosting.Models.UserContextDTO userContext, bool includeModules = true,
            string? unixId = null)
        {

            if (userContext.PersistedGrantType == (byte)PersistedGrantsType.APIGateWay)
            {
                unixId = await _dbContext.UserClient
                    .Where(x => x.ClientId == userContext.UnixId)
                    .Select(x => x.Owner.UnixId)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();
            }

            unixId = string.IsNullOrEmpty(unixId) ? userContext.UnixId : unixId;

            var currentUserDetails = await _dbContext.User
                .FirstOrDefaultAsync(x => x.UnixId == unixId);

            if (currentUserDetails == null)
            {
                throw new DCPNotFoundException("User not found");
            }

            int? currentSiteID = null;

            if (!await _dbContext.Site.AnyAsync())
            {
                return new LegacySiteInfoDTO
                {
                    Modules = new List<LegacyModuleDTO>()
                };
            }

            if (currentUserDetails.CurrentSiteId.HasValue
                && await _dbContext.Site.AnyAsync(x => x.Id == currentUserDetails.CurrentSiteId.Value))
            {
                currentSiteID = currentUserDetails.CurrentSiteId.Value;
            }
            else
            {
                //On user login it's possible
                // - first login
                // - latest used site to be not available anymore, because of removed access
                // - site is deleted
                //In such case get first of available sites
                currentSiteID = currentUserDetails.Permissions
                    .Where(x => !currentUserDetails.CurrentSiteId.HasValue ||
                                (x.SiteId != currentUserDetails.CurrentSiteId.Value && x.SiteId > 0))
                    .Select(x => x.SiteId)
                    .FirstOrDefault();
            }

            var result = await _dbContext.Site
                .Where(x => x.Id == currentSiteID)
                .Select(x => new LegacySiteInfoDTO
                {
                    SiteId = x.Id,
                    Name = x.Name,
                    ShortName = x.ShortName,
                    TimeZone = x.TimeZone,
                    IsEnable = x.IsEnable,
                    SiteType = (byte)x.SiteType,
                })
                .FirstOrDefaultAsync();

            //If a user have access only to one site and this site is deleted
            if (result == null)
            {
                throw new DCPBadRequestException("Current logged user do not have access to the selected site");
            }

            if (includeModules)
            {
                var modulesInfo = await _dbContext.Module.Where(x => x.SiteId == currentSiteID)
                                                          .Select(x => new
                                                          {
                                                              DataSourceIds = x.Site.Datasources.Select(ds => ds.Id),
                                                              ID = x.Id,
                                                              IsEnable = x.IsEnable,
                                                              ModuleName = x.Name,
                                                              SiteID = x.SiteId,
                                                          })
                                                          .ToListAsync();

                var allDataSourceIds = modulesInfo.SelectMany(x => x.DataSourceIds)
                                                  .Distinct()
                                                  .ToList();

                var dataTypes = await _dbContext.DataType.Where(x => allDataSourceIds.Contains(x.DataSourceId))
                                                          .Select(x => new
                                                          {
                                                              DataSourceId = x.DataSourceId,
                                                              DataType = x.Type,
                                                              DataTypeId = x.Id,
                                                              ModuleName = x.ModuleName,
                                                          })
                                                          .ToListAsync();

                result.Modules = modulesInfo.Select(x => new LegacyModuleDTO
                {
                    ID = x.ID,
                    IsEnable = x.IsEnable,
                    ModuleName = x.ModuleName,
                    SiteID = x.SiteID,
                    DataType = dataTypes.Where(dt => x.DataSourceIds.Contains(dt.DataSourceId) && x.ModuleName.Equals(dt.ModuleName, StringComparison.InvariantCultureIgnoreCase))
                                        .Select(dt => new LegacyDataTypeDTO
                                        {
                                            DataTypeId = dt.DataTypeId,
                                            DataSourceId = dt.DataSourceId,
                                            DataType = (byte)dt.DataType,
                                        })
                                        .ToList(),
                })
                .ToList();
            }

            return result;
        }

        public JsonArray MapAllSites(List<SiteListItemDTO> siteModel, List<DatasourcePublicConfigurationDTO> datasourceModel, List<ModuleConfigurationDTO> moduleModel)
        {
            var result = new JsonArray();

            foreach (var item in siteModel)
            {
                var itemToAdd = new JsonObject
                {
                    { "SiteId", item.Id},
                    { "Name", item.Name},
                    { "ShortName", item.ShortName},
                    { "TimeZone", item.TimeZone},
                    { "IsEnable", item.IsEnabled},
                    { "SiteType", (byte)item.SiteType},
                };

                var modulesForAdd = new JsonArray();

                itemToAdd.Add("Modules", modulesForAdd);

                var modulesPerSite = moduleModel.Where(x => x.SiteId == item.Id);

                foreach (var module in modulesPerSite)
                {
                    var moduleForAdd = new JsonObject
                    {
                        {"ID", module.Id },
                        {"ModuleName", module.Name },
                        {"IsEnable", module.IsEnabled },
                        {"IsValidated", module.IsValidated },

                    };

                    modulesForAdd.Add(moduleForAdd);
                }

                var datasourcePerSite = datasourceModel.Where(x => x.SiteId == item.Id).Select(x => x.ConnectionType);

                var dataTypesForAdd = new JsonArray();

                itemToAdd.Add("DataSources", dataTypesForAdd);

                foreach (var datasourceType in datasourcePerSite)
                {
                    var datasourceForAdd = new JsonObject
                    {
                        { "Type", (byte)ParseConnectionType(datasourceType)}
                    };

                    dataTypesForAdd.Add(datasourceForAdd);
                }

                result.Add(itemToAdd);
            }

            return result;
        }
        public async Task<JsonDocument> GetModuleConfigurationItems()
        {
            var env = _configuration["Host:EnvironmentSlug"];

            var services = await _serviceDiscovery.QueryActiveServices(new List<string> { "backend", "api" }, env);

            var result = JsonSerializer.Serialize(services.Select(x => new
            {
                Name = x.MetaData.ModuleName,
                Host = x.Host,
                IsValidated = x.Tags.Contains("gxp"),
                Port = x.Port.ToString(),
                DataTypes = x.MetaData.DataTypes.Select(z => new
                {
                    Type = (byte)z.Type,
                    Mandatory = z.Mandatory,
                    AllowMultiselection = z.AllowMultiselection,
                }),
                Resources = x.MetaData.Resources.Select(y => new
                {
                    ResourceURL = NormalizeResourceUrl(y.ResourceURL),
                    Type = y.Type
                }),
                RPackages = x.MetaData.CalculationNodeLibraries.Select(x => x)
            }));

            return JsonDocument.Parse(result);
        }

        private string NormalizeResourceUrl(string resourceUrl)
        {
            if (string.IsNullOrWhiteSpace(resourceUrl))
                return resourceUrl;

            if (resourceUrl.StartsWith("/"))
                return resourceUrl;

            var slashIndex = resourceUrl.IndexOf('/');

            if (slashIndex == -1)
                return "api";

            return "api" + resourceUrl.Substring(slashIndex);
        }

        private static JsonObject AddLeadingKey(JsonObject json, string connectionType)
        {
            var result = new JsonObject();

            var pConnectionType = ParseConnectionType(connectionType);

            if (pConnectionType == ConnectionType.OSISoftPI)
            {
                //MVDA inside old Connection.AvevaPI is using DisplayID
                json["DisplayID"] = json["IdentifierKey"]?.GetValue<string>();

                result.Add("OSISoftPI", json);
            }
            else if (pConnectionType == ConnectionType.GCP)
            {
                result.Add("GCP", json);
            }
            else if (pConnectionType == ConnectionType.SynTQ)
            {
                result.Add("SynTQ", json);
            }
            else if (pConnectionType == ConnectionType.ReMIX)
            {
                result.Add("SQL", json);
            }
            else if (pConnectionType == ConnectionType.Condor)
            {
                result.Add("Condor", json);
            }
            else if (pConnectionType == ConnectionType.PTDC)
            {
                result.Add("SQL", json);
            }
            else if (pConnectionType == ConnectionType.Mock)
            {
                if (json["Settings"] is JsonObject settings)
                {
                    foreach (var prop in settings)
                    {
                        json[prop.Key] = prop.Value?.DeepClone();
                    }

                    json.Remove("Settings");
                }

                result.Add("MockConfig", json);
            }
            else if (pConnectionType == ConnectionType.Snowflake)
            {
                result.Add("Snowflake", json);
            }
            else if (pConnectionType == ConnectionType.Basecamp20)
            {
                result.Add("Snowflake", json);
            }
            else if (pConnectionType == ConnectionType.SchemalessSQL)
            {
                result.Add("SQL", json);
            }
            else if (pConnectionType == ConnectionType.DataRefinery)
            {
                result.Add("GCP", json);
            }
            else if (pConnectionType == ConnectionType.GRED)
            {
                result.Add("GCP", json);
            }
            else if (pConnectionType == ConnectionType.Prozell)
            {
                result.Add("Prozell", json);
            }
            else if (pConnectionType == ConnectionType.RAW2)
            {
                result.Add("RAW2", json);
            }
            else if (pConnectionType == ConnectionType.Tetralake)
            {
                result.Add("Tetralake", json);
            }
            else if (pConnectionType == ConnectionType.ProGraf)
            {
                result.Add("ProGraf", json);
            }
            else if (pConnectionType == ConnectionType.KRIS)
            {
                result.Add("KRIS", json);
            }

            return result;
        }
        private string? LegacyPermissionToGroup(PermissionDTO model)
        {
            if (model == null)
                return null;

            var globalRegex = _configuration["IdP:RoleRegex:Global"];

            if (globalRegex is null)
                throw new DCPInternalServerErrorException("Missing Regex configuration");

            var prefix = globalRegex.Split(new[] { "_GLOBAL_" }, StringSplitOptions.None)[0];

            // Handle global roles
            if (model.SiteName.Equals("global", StringComparison.OrdinalIgnoreCase) &&
                model.Module.Equals("agnostic", StringComparison.OrdinalIgnoreCase) &&
                model.Permission.StartsWith("GLOBAL_", StringComparison.OrdinalIgnoreCase))
            {
                string permission = model.Permission.Substring("GLOBAL_".Length);
                return $"{prefix}_GLOBAL_{permission.ToUpper()}";
            }

            // Handle site-only roles (module-agnostic)
            if (!model.SiteName.Equals("global", StringComparison.OrdinalIgnoreCase) &&
                model.Module.Equals("agnostic", StringComparison.OrdinalIgnoreCase))
            {
                return $"{prefix}_{model.SiteName}_{model.Permission.ToUpper()}";
            }

            // Handle site + module roles
            if (!model.SiteName.Equals("global", StringComparison.OrdinalIgnoreCase) &&
                !model.Module.Equals("agnostic", StringComparison.OrdinalIgnoreCase))
            {
                var permission = $"{prefix}_{model.SiteName}_{model.Module}_{model.Permission.ToUpper()}";
                return NormalizePermission(permission, model.Module);
            }

            return null;
        }

        string NormalizePermission(string permission, string module)
        {
            var expected = $"{module}_{module}";

            return permission.Contains(expected, StringComparison.OrdinalIgnoreCase)
                ? Regex.Replace(
                    permission,
                    Regex.Escape(expected),
                    $"{module}_",
                    RegexOptions.IgnoreCase
                )
                : permission;
        }

        private List<string> LegacyPermissionsToGroups(List<PermissionDTO> permissions)
        {
            if (permissions == null)
                return new List<string>();

            return permissions
                .Select(p => LegacyPermissionToGroup(p))
                .Where(group => !string.IsNullOrEmpty(group))
                .Distinct()
                .ToList()!;
        }

        public void TransformObjectDecode(object? obj, HashSet<object> visited)
        {
            if (obj == null) return;
            if (visited.Contains(obj)) return; // cycle protection
            visited.Add(obj);

            var type = obj.GetType();

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.GetIndexParameters().Length > 0) continue; // skip indexers
                if (!property.CanRead || !property.CanWrite) continue;

                var value = property.GetValue(obj);
                if (value == null) continue;

                var attr = property.GetCustomAttributes<ConnectionManagerAttribute>()
                                   .FirstOrDefault(a => a.Action == RequestPipelineAction.Transform
                                   || a.Action == RequestPipelineAction.Translate);

                // --- Case 1: string property marked for transform ---
                if (attr != null && property.PropertyType == typeof(string))
                {
                    var decoded = WebIdEncoding.Decode((string)value);

                    property.SetValue(obj, decoded.WebId);
                }

                // --- Case 2: IEnumerable<string> property marked for transform ---
                else if (attr != null && typeof(IEnumerable<string>).IsAssignableFrom(property.PropertyType))
                {
                    var list = ((IEnumerable<string>)value)
                        .Select(val => WebIdEncoding.Decode(val).WebId)
                        .ToList();

                    property.SetValue(obj, list);
                }
                // --- Case 4: IEnumerable of complex objects (List<T>, etc.) ---
                else if (typeof(IEnumerable).IsAssignableFrom(property.PropertyType)
                         && property.PropertyType != typeof(string))
                {
                    var collection = value as IEnumerable;
                    if (collection == null) continue;

                    foreach (var item in (IEnumerable)value)
                    {
                        if (item == null) continue;

                        var itemType = item.GetType();
                        if (itemType.IsClass && itemType != typeof(string))
                        {
                            TransformObjectDecode(item, visited);
                        }
                    }
                }
                // --- Case 3: Complex object (recurse) ---
                else if (property.PropertyType.IsClass && property.PropertyType != typeof(string))
                {
                    TransformObjectDecode(value, visited);
                }
            }
        }

        public string TransformSingleWebIdEncode(string id, int datasourceId)
        {
            id = WebIdEncoding.Encode(
                        webId: id,
                        datasourceId: datasourceId,
                        version: 1,
                        format: Framework.Connections.Manager.Constants.WebIdType.DCP
                    );

            return id;
        }

        public void TransformObjectEncode(object? obj, int datasourceId, HashSet<object> visited)
        {
            if (obj == null) return;
            if (visited.Contains(obj)) return; // cycle protection
            visited.Add(obj);

            var type = obj.GetType();

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.GetIndexParameters().Length > 0) continue; // skip indexers
                if (!property.CanRead || !property.CanWrite) continue;

                var value = property.GetValue(obj);
                if (value == null) continue;

                var attr = property.GetCustomAttributes<ConnectionManagerAttribute>()
                                   .FirstOrDefault(a => a.Action == RequestPipelineAction.Map);

                // --- Case 1: string property marked for transform ---
                if (attr != null && property.PropertyType == typeof(string))
                {
                    var encoded = WebIdEncoding.Encode(
                        webId: (string)value,
                        datasourceId: datasourceId,
                        version: 1,
                        format: Framework.Connections.Manager.Constants.WebIdType.DCP
                    );
                    property.SetValue(obj, encoded);
                }

                // --- Case 2: IEnumerable<string> property marked for transform ---
                else if (attr != null && typeof(IEnumerable<string>).IsAssignableFrom(property.PropertyType))
                {
                    var list = ((IEnumerable<string>)value)
                        .Select(val => WebIdEncoding.Encode(
                            webId: val,
                            datasourceId: datasourceId,
                            version: 1,
                            format: Framework.Connections.Manager.Constants.WebIdType.DCP))
                        .ToList();

                    property.SetValue(obj, list);
                }
                // --- Case 4: IEnumerable of complex objects (List<T>, etc.) ---
                else if (typeof(IEnumerable).IsAssignableFrom(property.PropertyType)
                         && property.PropertyType != typeof(string))
                {
                    var collection = value as IEnumerable;
                    if (collection == null) continue;

                    foreach (var item in (IEnumerable)value)
                    {
                        if (item == null) continue;

                        var itemType = item.GetType();
                        if (itemType.IsClass && itemType != typeof(string))
                        {
                            TransformObjectEncode(item, datasourceId, visited);
                        }
                    }
                }
                // --- Case 3: Complex object (recurse) ---
                else if (property.PropertyType.IsClass && property.PropertyType != typeof(string))
                {
                    TransformObjectEncode(value, datasourceId, visited);
                }
            }
        }

        public async Task<string> TransformLinkEncode(string module, string link, int datasourceId, string filterType, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(link))
                return link;

            var uriParts = link.Split('?', 2);
            var path = uriParts[0];
            var query = uriParts.Length > 1 ? uriParts[1] : string.Empty;

            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length < 2)
                return link;

            var webId = segments[segments.Length - 2];
            var lastSegment = segments.Last();

            var encodedWebId = WebIdEncoding.Encode(
                webId: webId,
                datasourceId: datasourceId,
                version: 1,
                format: Framework.Connections.Manager.Constants.WebIdType.DCP
            );

            string type = string.Empty;

            if (lastSegment.Equals("elements", StringComparison.OrdinalIgnoreCase) && (link.Contains("Items.Links") || string.IsNullOrEmpty(filterType)))
                type = "level";
            else if (((lastSegment.Equals("elements", StringComparison.OrdinalIgnoreCase) && !link.Contains("Items.Links"))
                || (lastSegment.Equals("attributes", StringComparison.OrdinalIgnoreCase) && link.Contains("Items.CategoryNames"))) && !string.IsNullOrEmpty(filterType))
                type = "group";
            else if (lastSegment.Equals("attributes", StringComparison.OrdinalIgnoreCase))
                type = "sensor";

            if (filterType == "ElementFilter")
            {
                _loader.SetModule(module);

                var pipeline = new PluginPipeline<RequestContainer, DeviceResponse>(_cache, _serviceProvider, _loader)
                    .WithDefaultMapFunction()
                    .WithDefaultTransformFunction()
                    .WithDefaultReduceFunction();

                var response = await pipeline.RunMapReduceAsync<IAssetHierarchyDataSource>(
                    async (datasource, args, ct) => await datasource.GetSingleDeviceAsync((string)args[0], ct),
                    mapArgIndex: 0,
                    args: new object[] { encodedWebId }
                );

                if (response.Device.CategoryNames.Contains("DC_Asset"))
                {
                    type = "group";
                }
            }

            var transformedLink = $"{encodedWebId}/{type}";

            if (!string.IsNullOrEmpty(query))
            {
                Match match = Regex.Match(query, @"categoryName=([^;,]+)", RegexOptions.None, TimeSpan.FromMilliseconds(100));

                if (match.Success)
                    transformedLink += $"?groupName={match.Groups[1].Value}";
            }

            return transformedLink;
        }

        public async Task TransformElementBack(List<GlobalFilterElementDTO> elements, LegacySiteConfigurationDTO_v2 siteConfiguration, string? link)
        {
            foreach (var element in elements)
            {
                var transformedLink = await TransformLinkBack(element.Link, siteConfiguration, link);

                element.WebId = WebIdEncoding.Decode(element.WebId).WebId;
                element.Link = transformedLink;
            }
        }

        private async Task<string> TransformLinkBack(string link, LegacySiteConfigurationDTO_v2 siteConfiguration, string? linkFromRequest)
        {
            if (string.IsNullOrEmpty(link)) return link;

            var parts = link.Split('/');

            var decodedWebId = WebIdEncoding.Decode(parts[0]).WebId;

            if (siteConfiguration.ConnectionType == (byte)ConnectionType.OSISoftPI)
            {
                var connectionString = await _dbContext.DataSource
                    .Where(x => x.Id == siteConfiguration.DataSourceId && x.SiteId == siteConfiguration.ID)
                    .Select(x => x.ConnectionString)
                    .FirstOrDefaultAsync();

                var key = GetEncryptionKey();

                var decryptedConnectionString = EncryptionHelper.DecryptConnectionString(connectionString, key);
                var baseURL = decryptedConnectionString["BaseURL"];
                var gFilterType = decryptedConnectionString["SensorFilterType"]?.ToString();


                if (link.Contains("/level"))
                {
                    link = $"{baseURL}elements/{decodedWebId}/elements" +
                      "?selectedFields=Items.WebId;Items.Name;Items.Description;Items.Links.Elements;Items.Links.Attributes;Items.HasChildren;Items.CategoryNames" +
                      "&webIdType=IDOnly";
                }
                else if (link.Contains("/group"))
                {

                    if (gFilterType == "AttributeFilter")
                    {
                        link = $"{baseURL}elements/{decodedWebId}/attributes" +
                          "?selectedFields=Items.IsConfigurationItem;Items.CategoryNames" +
                          "&webIdType=IDOnly";
                    }
                    else if (gFilterType == "ElementFilter")
                    {
                        link = $"{baseURL}elements/{decodedWebId}/elements" +
                              "?selectedFields=Items.WebId;Items.Name;Items.Description;" +
                              "&webIdType=IDOnly";
                    }
                }
                else if (link.Contains("/sensor"))
                {
                    Match match = (gFilterType == "ElementFilter" || gFilterType == "AttributeFilter")
                                ? Regex.Match(link, @"groupName=([^;,]+)", RegexOptions.None, TimeSpan.FromMilliseconds(100))
                                : Match.Empty;

                    string? groupName = match.Success ? match.Groups[1].Value : null;
                    string elementId = (gFilterType == "ElementFilter" && groupName != null)
                        ? groupName
                        : decodedWebId;

                    link = $"{baseURL}elements/{elementId}/attributes?webIdType=IDOnly";

                    if (gFilterType == "AttributeFilter" && groupName != null)
                        link += $"&categoryName={groupName}";
                }
            }
            else
            {
                var uriParts = link.Split('?', 2);
                var path = uriParts[0];
                var query = uriParts.Length > 1 ? uriParts[1] : string.Empty;

                var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

                var lastSegment = segments[segments.Count() - 1];

                string type = string.Empty;

                if (lastSegment.Equals("level", StringComparison.OrdinalIgnoreCase))
                    type = "elements";
                else if (lastSegment.Equals("sensor", StringComparison.OrdinalIgnoreCase))
                    type = "attributes";

                link = $"{decodedWebId}/{type}";

                if (!string.IsNullOrEmpty(query))
                {
                    Match match = Regex.Match(query, @"groupName=([^;,\s]+)", RegexOptions.None, TimeSpan.FromMilliseconds(100));

                    if (match.Success)
                        link += $"?categoryName={match.Groups[1].Value}";
                }
            }

            return link;
        }

        public void TranslateObjectEncode(object? obj, int datasourceId, HashSet<object> visited)
        {
            if (obj == null) return;
            if (visited.Contains(obj)) return; // cycle protection
            visited.Add(obj);

            var type = obj.GetType();

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.GetIndexParameters().Length > 0) continue; // skip indexers
                if (!property.CanRead || !property.CanWrite) continue;

                var value = property.GetValue(obj);
                if (value == null) continue;

                var attr = property.GetCustomAttributes<ConnectionManagerAttribute>()
                                   .FirstOrDefault(a => a.Action == RequestPipelineAction.Translate);

                // --- Case 1: string property marked for transform ---
                if (attr != null && property.PropertyType == typeof(string))
                {
                    var encoded = WebIdEncoding.Encode(
                        webId: (string)value,
                        datasourceId: datasourceId,
                        version: 1,
                        format: Framework.Connections.Manager.Constants.WebIdType.DCP
                    );
                    property.SetValue(obj, encoded);
                }

                // --- Case 2: IEnumerable<string> property marked for transform ---
                else if (attr != null && typeof(IEnumerable<string>).IsAssignableFrom(property.PropertyType))
                {
                    var list = ((IEnumerable<string>)value)
                        .Select(val => WebIdEncoding.Encode(
                            webId: val,
                            datasourceId: datasourceId,
                            version: 1,
                            format: Framework.Connections.Manager.Constants.WebIdType.DCP))
                        .ToList();

                    property.SetValue(obj, list);
                }
                // --- Case 4: IEnumerable of complex objects (List<T>, etc.) ---
                else if (typeof(IEnumerable).IsAssignableFrom(property.PropertyType)
                         && property.PropertyType != typeof(string))
                {
                    var collection = value as IEnumerable;
                    if (collection == null) continue;

                    foreach (var item in (IEnumerable)value)
                    {
                        if (item == null) continue;

                        var itemType = item.GetType();
                        if (itemType.IsClass && itemType != typeof(string))
                        {
                            TranslateObjectEncode(item, datasourceId, visited);
                        }
                    }
                }
                // --- Case 3: Complex object (recurse) ---
                else if (property.PropertyType.IsClass && property.PropertyType != typeof(string))
                {
                    TranslateObjectEncode(value, datasourceId, visited);
                }
            }
        }

        public async Task<ServiceResponse<SensorDataWithBatchContextResponse>> EventFrameIdToWebIdInBatchResponses(ServiceResponse<SensorDataWithBatchContextResponse> response)
        {
            var pipeline = new PluginPipeline<RequestContainer, List<string>>(_cache, _serviceProvider, _loader)
                .WithDefaultMapFunction()
                .WithDefaultTransformFunction()
                .WithDefaultReduceFunction();

            if (response != null && response.Data.BatchData != null && response.Data.BatchData.Count() > 0)
            {
                var eventframeIds = response.Data.BatchData
                    .SelectMany(x => x.EventFrames.Select(ef => ef.Id))
                    .Distinct()
                    .ToList();

                if (eventframeIds.Count > 0)
                {
                    var webIds = await pipeline.RunMapReduceAsync<IEventframeContextDataSource>(
                                               (datasource, args, ct) => Task.FromResult(datasource.EventFrameId2WebId((List<string>)args[0])),
                                               mapArgIndex: 0,
                                               args: new object[] { eventframeIds }
                                           );

                    var dict = eventframeIds.Zip(webIds, (key, value) => new { key, value })
                                    .ToDictionary(x => x.key, x => x.value);

                    foreach (var data in response.Data.BatchData)
                    {
                        foreach (var eventFrame in data.EventFrames)
                        {
                            eventFrame.Id = dict[eventFrame.Id];
                        }
                    }
                }
            }

            return response ?? new ServiceResponse<SensorDataWithBatchContextResponse>();
        }

        private async Task<SensorListResponse> GetSensorsDataAsync(string module, List<string> webIds, CancellationToken ct = default)
        {
            var sensorRequest = new SensorQueryDTO
            {
                WebId = webIds
            };

            var visitedSensors = new HashSet<object>();

            _loader.SetModule(module);

            var pipeline = new PluginPipeline<SensorQueryDTO, SensorListResponse>(_cache, _serviceProvider, _loader)
                .WithDefaultMapFunction()
                .WithDefaultTransformFunction()
                .WithDefaultReduceFunction();

            var response = await pipeline.RunMapReduceAsync<IAssetHierarchyDataSource>(
                (datasource, sensorRequest, ct) => datasource.GetMultipleSensorsAsync(sensorRequest, ct),
                sensorRequest
            );

            TransformObjectDecode(response, visitedSensors);

            return response;
        }

        private async Task<SensorDeviceInfoResponse> GetSensorsDeviceAsync(string module, List<string> webIds, CancellationToken ct = default)
        {
            var visitedDevices = new HashSet<object>();

            _loader.SetModule(module);

            var pipeline = new PluginPipeline<RequestContainer, SensorDeviceInfoResponse>(_cache, _serviceProvider, _loader)
                .WithDefaultMapFunction()
                .WithDefaultTransformFunction()
                .WithDefaultReduceFunction();

            var response = await pipeline.RunMapReduceAsync<IAssetHierarchyDataSource>(
                (datasource, args, ct) => datasource.GetSensorsDeviceAsync((List<string>)args[0], ct),
                mapArgIndex: 0,
                args: new object[] { webIds }
            );

            TransformObjectDecode(response, visitedDevices);

            return response;
        }

        public async Task<List<string>> WebId2EventFrameId(List<string> webId)
        {
            var pipeline = new PluginPipeline<RequestContainer, List<string>>(_cache, _serviceProvider, _loader)
               .WithDefaultMapFunction()
               .WithDefaultTransformFunction()
               .WithDefaultReduceFunction();

            var ids = await pipeline.RunMapReduceAsync<IEventframeContextDataSource>(
                            (datasource, args, ct) => Task.FromResult(datasource.WebId2EventFrameId((List<string>)args[0])),
                            mapArgIndex: 0,
                            args: new object[] { webId }
                        );
            return ids;
        }

        public async Task<List<LegacyActiveModule>> SetUserSite(int siteId, Framework.Hosting.Models.UserContextDTO user)
        {
            var userInfo = await _dbContext.User.FirstOrDefaultAsync(u => u.Id == user.UserId);

            if (userInfo is null)
                throw new DCPBadRequestException(ErrorMessages.UnkownUser);

            userInfo.CurrentSiteId = siteId;
            await _dbContext.SaveChangesAsync();

            var result = await _dbContext.Module
                .Where(x => x.SiteId == siteId)
                .Select(x => new LegacyActiveModule
                {
                    ModuleName = x.Name,
                    SiteID = x.SiteId,
                    ID = x.Id,
                    IsEnable = x.IsEnable,
                    IsValidated = x.IsValidated
                })
                .AsNoTracking()
                .ToListAsync();

            return result;
        }

        private static ConnectionType ParseConnectionType(string s) =>
        s switch
        {
            "OSISoftPI" => ConnectionType.OSISoftPI,
            "GCP" => ConnectionType.GCP,
            "SynTQ" => ConnectionType.SynTQ,
            "reMIX" => ConnectionType.ReMIX,
            "ConDoR" => ConnectionType.Condor,
            "PTDC" => ConnectionType.PTDC,
            "ProZell" => ConnectionType.Prozell,
            "Mock" => ConnectionType.Mock,
            "SfAdaptSchema" => ConnectionType.Snowflake,
            "SchemalessSQL" => ConnectionType.SchemalessSQL,
            "DataRefinery" => ConnectionType.DataRefinery,
            "GRED" => ConnectionType.GRED,
            "Basecamp2.0" => ConnectionType.Basecamp20,
            "RAW2" => ConnectionType.RAW2,
            "Tetralake" => ConnectionType.Tetralake,
            "ProGraf" => ConnectionType.ProGraf,
            "KRIS" => ConnectionType.KRIS,
            "SfProcessMonitoring" => ConnectionType.Snowflake,
            _ => throw new ArgumentOutOfRangeException(nameof(s), s, "Unknown connection type")
        };
    }
}
