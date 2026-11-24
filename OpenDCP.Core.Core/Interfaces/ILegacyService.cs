using OpenDCP.Core.Models.Legacy.Common;
using OpenDCP.Core.Models.Legacy.DataHandling;
using OpenDCP.Core.Models.Legacy.Site;
using OpenDCP.Core.Models.Models.Configuration;
using OpenDCP.Core.Models.Models.EventSubscription;
using OpenDCP.Core.Models.Models.Site;
using OpenDCP.Framework.Configuration.Model.Constants.Datasource;
using OpenDCP.Framework.Configuration.Model.Entities.Site;
using OpenDCP.Framework.Configuration.Model.Responses.Service;
using OpenDCP.Framework.DataTypes.Equipment.Entities.AssetHierarchy;
using OpenDCP.Framework.DataTypes.Equipment.Requests.DataBundle;
using OpenDCP.Framework.DataTypes.Equipment.Requests.EventFrameContext;
using OpenDCP.Framework.DataTypes.Equipment.Requests.SensorData;
using OpenDCP.Framework.DataTypes.Equipment.Responses.AssetHierarchy;
using OpenDCP.Framework.DataTypes.Equipment.Responses.EventFrameContext;
using OpenDCP.Framework.DataTypes.Equipment.Responses.SensorData;
using OpenDCP.Framework.Security.Models;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace OpenDCP.Core.Core.Interfaces
{
    public interface ILegacyService
    {
        public Task<(int siteId, string module)> ReverseMapSiteconfigurationDTO(LegacySiteConfigurationDTO_v2 siteConfiguration, DataType dataType);
        public Task<int> GetDatasourceId(int siteId, string moduleName, DataType dataType);
        public JsonObject MapRecordedData(SensorDataResponse model);
        public Task<JsonObject> MapSensorDetails(SensorResponse model, LegacySiteConfigurationDTO_v2 siteConfiguration);
        public Task<JsonObject> MapDeviceDetails(DeviceResponse model, string parentWebId, LegacySiteConfigurationDTO_v2 siteConfiguration);
        public Task<JsonObject> MapMultipleSensorsDetailsAsync(SensorListResponse model, LegacySiteConfigurationDTO_v2 siteConfiguration);
        public JsonArray MapDeviceEventFrameAttribute(EventframeAttributesResponse model);
        public JsonObject MapGlobalFilterBackwards(GlobalFilterListResponse model, LegacySiteConfigurationDTO_v2 siteConfiguration);
        public JsonArray MapGlobalFilterFromWebId(GlobalFilterListResponse model);
        public JsonObject MapInterpolatedData(SensorDataResponse model, LegacyShowSignalsRequest legacyModel, DateTime startTime);
        public Task<JsonObject> MapPlotData(SensorDataResponse model, PlotDataRequest legacyModel, string module, List<string> webIds);
        public JsonObject MapInterpolatedAtTimeData(SensorDataResponse model);
        public JsonObject InterpolatedDataAtTimes(SensorDataResponse model, LegacyShowSignalsRequest legacyModel);
        public JsonObject MapDeviceEventFrames(EventframeListResponse model, DeviceListResponse model2, bool isMultipleDevice);
        public JsonObject MapDeviceInProgressEventFrames(EventframeListResponse model);
        public JsonArray MapActiveBatchesPerDevice(BatchContextListResponse model);
        public JsonObject MapEventFrameAttributesValues(EventframeValuesResponse model);
        public JsonObject MapEventFrameEndTime(EventFrameResponse model);
        public JsonObject MapEventFramesInProgress(EventframeListResponse model, EventFramesInProgressRequest legacyModel);
        public JsonArray MapDataSourceUnits(DatasourceUnitResponse model);
        public JsonObject MapRecordedAtTimesData(SensorDataResponse model);
        public JsonObject MapSingleBatchIntermediate(List<InProgressEventDTO> model);
        public JsonArray MapBatchIntermediate(List<InProgressEventDTO> model);
        public JsonObject MapGlobalFilter(GlobalFilterListResponse model);
        public JsonArray MapElementMetaData(ElementMetaDataResponse model);
        public JsonArray MapDataSource(List<DatasourceConfigurationDTO> model, bool includeConnectionString = true);
        public JsonArray MapAllDevicesPerSite(DeviceListResponse model);
        public JsonArray MapModules(List<ModuleConfigurationDTO> model);
        public Task<List<UserInfoDTO>> MapUsersDetails(List<Models.Models.User.UserListItemDTO> model, bool includeRoles = false);
        public Models.Legacy.Site.LegacySiteConfigurationDTO MapSiteDetails(SiteConfigurationFormDTO siteModel, List<DatasourceConfigurationDTO> datasourceModel, List<ModuleConfigurationDTO> moduleModel);
        public List<Models.Legacy.Site.LegacySiteConfigurationDTO> MapSitesDetails(List<SiteListItemDTO> sitesModel, List<DatasourceConfigurationDTO> datasourceModel, List<ModuleConfigurationDTO> moduleModel);
        public List<LegacyValueTextViewModel> MapAllSiteWithActiveModule(List<SiteListItemDTO> sitesModel);
        public JsonNode MapGroupedDeviceSensors(GroupedSensorListResponse model);
        public JsonArray MapDeviceSensorsByGroup(SensorListResponse model);
        public JsonArray MapInterpolatedDataWithBatchContext(SensorDataWithBatchContextResponse model, LegacyRequestSensorData legacyModel, string module, List<string> webIds);
        public JsonArray MapPlotDataWithContext(SensorDataWithBatchContextResponse model, LegacyPlotDataWithBatchContextRequest legacyModel, string module, List<string> webIds);
        public Task<LegacySiteInfoDTO> GetUserCurrentSiteAsync(Framework.Hosting.Models.UserContextDTO userContext, bool includeModules = true, string? unixId = null);
        public JsonArray MapAllSites(List<SiteListItemDTO> siteModel, List<DatasourcePublicConfigurationDTO> datasourceModel, List<ModuleConfigurationDTO> moduleModel);
        public Task<JsonDocument> GetModuleConfigurationItems();
        public string TransformSingleWebIdEncode(string id, int datasourceId);
        public void TransformObjectDecode(object? obj, HashSet<object> visited);
        public void TransformObjectEncode(object? obj, int datasourceId, HashSet<object> visited);
        public void TranslateObjectEncode(object? obj, int datasourceId, HashSet<object> visited);
        public Task<string> TransformLinkEncode(string module, string link, int datasourceId, string filterType, CancellationToken ct = default);
        public Task TransformElementBack(List<GlobalFilterElementDTO> elements, LegacySiteConfigurationDTO_v2 siteConfiguration, string? link = null);
        public Task<ServiceResponse<SensorDataWithBatchContextResponse>> EventFrameIdToWebIdInBatchResponses(ServiceResponse<SensorDataWithBatchContextResponse> response);
        public Task<ServiceResponse<SensorDataWithBatchContextResponse>> GetDataWithContextWithRefreshAsync(string module, PlotDataWithBatchContextQueryDTO? plotModel = null, InterpolatedDataWithBatchContextQueryDTO? interpolatedModel = null, CancellationToken ct = default);
        public Task<List<string>> WebId2EventFrameId(List<string> webId);
        public Task<List<LegacyActiveModule>> SetUserSite(int siteId, Framework.Hosting.Models.UserContextDTO user);

        Task<List<InProgressEventDTO>> GetCachedBatchEventsAsync(EventSubscriptionQuery model, Framework.Hosting.Models.UserContextDTO user);
        public JsonObject MapGetSingleSite(SiteConfigurationFormDTO siteModel, List<DatasourceConfigurationDTO> datasourceModel, List<ModuleConfigurationDTO> moduleModel);
    }
}
