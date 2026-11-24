using OpenDCP.Framework.Configuration.Model.Responses.Service;
using OpenDCP.Framework.DataTypes.Equipment.Requests.AssetHierarchy;
using OpenDCP.Framework.DataTypes.Equipment.Responses.AssetHierarchy;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OpenDCP.Core.Core.Interfaces.Data
{
    public interface IAssetHierarchyDataService
    {
        public Task<ServiceResponse<SensorListResponse>> GetMultipleSensorsAsync(int siteId, string module, SensorQueryDTO model, Framework.Hosting.Models.UserContextDTO user, CancellationToken ct = default);
        public Task<ServiceResponse<SensorResponse>> GetSingleSensorAsync(int siteId, string module, string webId, Framework.Hosting.Models.UserContextDTO user, CancellationToken ct = default);
        public Task<ServiceResponse<SensorListResponse>> GetDeviceSensorsAsync(int siteId, string module, string webId, Framework.Hosting.Models.UserContextDTO user, string? groupName, CancellationToken ct = default);
        public Task<ServiceResponse<GroupedSensorListResponse>> GetGroupedDeviceSensorsAsync(int siteId, string module, string webId, Framework.Hosting.Models.UserContextDTO user, CancellationToken ct = default);
        public Task<ServiceResponse<DeviceResponse>> GetSingleDeviceAsync(int siteId, string module, string webId, Framework.Hosting.Models.UserContextDTO user, CancellationToken ct = default);
        [Obsolete("This is present for the legacy implementation (core-v1) and should not be consumed directly")]
        public Task<string> GetPreviousGlobalFilterWebIdAsync(int siteId, string module, string webId, Framework.Hosting.Models.UserContextDTO user, CancellationToken ct = default);
        public Task<ServiceResponse<DeviceListResponse>> SearchDevicesAsync(int siteId, string module, DeviceQueryDTO model, Framework.Hosting.Models.UserContextDTO user, CancellationToken ct = default);
        public Task<ServiceResponse<DeviceListResponse>> GetMultipleDevicesAsync(int siteId, string module, List<string> webIds, Framework.Hosting.Models.UserContextDTO user, CancellationToken ct = default);
        public Task<ServiceResponse<DatasourceUnitResponse>> GetUnitsOfMeasureAsync(int siteId, string module, Framework.Hosting.Models.UserContextDTO user, CancellationToken ct = default);
        public Task<ServiceResponse<ElementMetaDataResponse>> GetElementMetaDataAsync(int siteId, string module, string webId, Framework.Hosting.Models.UserContextDTO user, CancellationToken ct = default);
        public Task<ServiceResponse<GlobalFilterListResponse>> GetGlobalFilterAsync(int siteId, string module, string? link, Framework.Hosting.Models.UserContextDTO user, CancellationToken ct = default);
        public Task<ServiceResponse<GlobalFilterResponse>> GetPreviousGlobalFilterAsync(int siteId, string module, string link, Framework.Hosting.Models.UserContextDTO user, CancellationToken ct = default);
        public Task<ServiceResponse<GlobalFilterResponse>> GetGlobalFilterElementAsync(int siteId, string module, string webId, Framework.Hosting.Models.UserContextDTO user, CancellationToken ct = default);
        public Task<ServiceResponse<GlobalFilterListResponse>> GetGlobalFilterBackwardsAsync(int siteId, string module, string webId, Framework.Hosting.Models.UserContextDTO user, CancellationToken ct = default);
    }
}
