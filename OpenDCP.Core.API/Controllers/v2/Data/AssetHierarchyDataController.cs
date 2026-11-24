using OpenDCP.Core.Core.Extensions;
using OpenDCP.Core.Core.Interfaces.Data;
using OpenDCP.Framework.DataTypes.Equipment.Entities.AssetHierarchy;
using OpenDCP.Framework.DataTypes.Equipment.Requests.AssetHierarchy;
using OpenDCP.Framework.DataTypes.Equipment.Responses.AssetHierarchy;
using Microsoft.AspNetCore.Mvc;

namespace OpenDCP.Core.API.Controllers.v2.Data
{
    /// <summary>
    /// Provides read access to asset hierarchy–related data.
    /// The data returned is based on the connections mapped to the specified site and module.
    /// </summary>
    [Route("api/v2/data/{siteId}/{module}/asset-hierachy/")]
    public class AssetHierarchyDataController : BaseController
    {
        private readonly IAssetHierarchyDataService _dataService;

        /// <summary>
        /// Initializes a new instance of the <see cref="AssetHierarchyDataController"/>.
        /// </summary>
        /// <param name="configuration">Application configuration instance.</param>
        /// <param name="dataService">Service used to retrieve asset hierarchy data.</param>
        public AssetHierarchyDataController(
            IConfiguration configuration,
            IAssetHierarchyDataService dataService)
            : base(configuration)
        {
            _dataService = dataService;
        }


        /// <summary>
        /// Retrieves the next global filter level based on the provided link.
        /// </summary>
        /// <param name="siteId">The identifier of the site.</param>
        /// <param name="module">The name of the module.</param>
        /// <param name="link">The link associated with the module.</param>
        /// <returns>An <see cref="IActionResult"/> containing the global filter response.</returns>
        [HttpGet]
        [Route("hierachy/next")]
        [Produces(typeof(GlobalFilterListResponse))]
        public async Task<IActionResult> GetGlobalFilter(int siteId, string module, [FromQuery] string? link, CancellationToken ct = default)
        {
            var user = GetUserContext();
            var result = await _dataService.GetGlobalFilterAsync(siteId, module, link, user, ct);

            Response.SetValidationHeader(
                _configuration.GetValue<bool>("Host:IsModuleValidated", false),
                result.DatasourceValidation,
                result.CalculationValidation,
                result.EntityValidation
            );

            return Ok(result.Data);
        }
        /// <summary>
        /// Retrieves the previous global filter level based on the provided link.
        /// </summary>
        /// <param name="siteId">The identifier of the site.</param>
        /// <param name="module">The name of the module.</param>
        /// <param name="link">The link associated with the module.</param>
        /// <returns>An <see cref="IActionResult"/> containing the global filter response.</returns>
        [HttpGet]
        [Route("hierachy/previous")]
        [Produces(typeof(GlobalFilterResponse))]
        public async Task<IActionResult> GetPreviousGlobalFilter(int siteId, string module, string link, CancellationToken ct = default)
        {
            var user = GetUserContext();
            var result = await _dataService.GetPreviousGlobalFilterAsync(siteId, module, link, user, ct);

            Response.SetValidationHeader(
                _configuration.GetValue<bool>("Host:IsModuleValidated", false),
                result.DatasourceValidation,
                result.CalculationValidation,
                result.EntityValidation
            );

            return Ok(result.Data);
        }

        /// <summary>
        /// Retrieves the global filter in a backwards hierarchy from the element with the webId to the configured root
        /// </summary>
        /// <param name="siteId">The identifier of the site.</param>
        /// <param name="module">The name of the module.</param>
        /// <param name="webId">The web ID used to query the backwards hierarchy.</param>
        [HttpGet]
        [Route("hierachy/{webId}/backwards")]
        [Produces(typeof(GlobalFilterListResponse))]
        public async Task<IActionResult> GetGlobalFilterBackwards(int siteId, string module, string webId, CancellationToken ct = default)
        {
            var user = GetUserContext();
            var result = await _dataService.GetGlobalFilterBackwardsAsync(siteId, module, webId, user, ct);

            Response.SetValidationHeader(
                _configuration.GetValue<bool>("Host:IsModuleValidated", false),
                result.DatasourceValidation,
                result.CalculationValidation,
                result.EntityValidation
            );

            return Ok(result.Data);
        }

        /// <summary>
        /// Retrieves a specific global filter element for the given webId. Can be used as a translation service.
        /// </summary>
        /// <param name="siteId">The identifier of the site.</param>
        /// <param name="module">The name of the module.</param>
        /// <param name="webId">The web ID used to identify the filter element.</param>
        [HttpGet]
        [Route("hierachy/{webId}/get")]
        [Produces(typeof(GlobalFilterResponse))]
        public async Task<IActionResult> GetGlobalFilterElement(int siteId, string module, string webId, CancellationToken ct = default)
        {
            var user = GetUserContext();
            var result = await _dataService.GetGlobalFilterElementAsync(siteId, module, webId, user, ct);

            Response.SetValidationHeader(
                _configuration.GetValue<bool>("Host:IsModuleValidated", false),
                result.DatasourceValidation,
                result.CalculationValidation,
                result.EntityValidation
            );

            return Ok(result.Data);
        }

        /// <summary>
        /// Retrieves metadata for a specific element identified by the webId.
        /// </summary>
        /// <param name="siteId">The identifier of the site.</param>
        /// <param name="module">The name of the module.</param>
        /// <param name="webId">The web ID used to identify the element.</param>
        /// <returns>An <see cref="IActionResult"/> containing the element metadata response.</returns>
        [HttpGet]
        [Route("hierachy/{webId}/meta-data")]
        [Produces(typeof(ElementMetaDataResponse))]
        public async Task<IActionResult> GetElementMetaData(int siteId, string module, string webId, CancellationToken ct = default)
        {
            var user = GetUserContext();
            var result = await _dataService.GetElementMetaDataAsync(siteId, module, webId, user, ct);

            Response.SetValidationHeader(
                _configuration.GetValue<bool>("Host:IsModuleValidated", false),
                result.DatasourceValidation,
                result.CalculationValidation,
                result.EntityValidation
            );

            return Ok(result.Data);
        }

        /// <summary>
        /// Retrieves a list of all units of measure for the specified site and module.
        /// </summary>
        /// <param name="siteId">The identifier of the site.</param>
        /// <param name="module">The name of the module.</param>
        /// <returns>An <see cref="IActionResult"/> containing a list of units of measure.</returns>

        [HttpGet]
        [Route("uom/list")]
        [Produces(typeof(DatasourceUnitResponse))]
        public async Task<IActionResult> GetAllUnitsOfMeasure(int siteId, string module, CancellationToken ct = default)
        {
            var user = GetUserContext();
            var result = await _dataService.GetUnitsOfMeasureAsync(siteId, module, user, ct);

            Response.SetValidationHeader(
                _configuration.GetValue<bool>("Host:IsModuleValidated", false),
                result.DatasourceValidation,
                result.CalculationValidation,
                result.EntityValidation
            );

            return Ok(result.Data);
        }

        /// <summary>
        /// Searches for devices based on the specified query parameters.
        /// </summary>
        /// <param name="siteId">The identifier of the site.</param>
        /// <param name="module">The name of the module.</param>
        /// <param name="model">The query parameters for filtering devices, provided as a <see cref="DeviceQueryDTO"/>.</param>
        /// <returns>An <see cref="IActionResult"/> containing the list of devices that match the query.</returns>
        [HttpGet]
        [Route("device/query")]
        [Produces(typeof(DeviceListResponse))]
        public async Task<IActionResult> SearchDevices(int siteId, string module, [FromQuery] DeviceQueryDTO model, CancellationToken ct = default)
        {
            var user = GetUserContext();
            var result = await _dataService.SearchDevicesAsync(siteId, module, model, user, ct);

            Response.SetValidationHeader(
                _configuration.GetValue<bool>("Host:IsModuleValidated", false),
                result.DatasourceValidation,
                result.CalculationValidation,
                result.EntityValidation
            );

            return Ok(result.Data);
        }

        /// <summary>
        /// Retrieves details for multiple devices identified by their web IDs.
        /// </summary>
        /// <param name="siteId">The identifier of the site.</param>
        /// <param name="module">The name of the module.</param>
        /// <param name="webId">A list of web IDs identifying the devices to retrieve.</param>
        /// <returns>An <see cref="IActionResult"/> containing the list of device details.</returns>
        [HttpGet]
        [Route("device/multiple")]
        [Produces(typeof(DeviceListResponse))]
        public async Task<IActionResult> GetMultipleDevices(int siteId, string module, [FromQuery] List<string> webId, CancellationToken ct = default)
        {
            var user = GetUserContext();
            var result = await _dataService.GetMultipleDevicesAsync(siteId, module, webId, user, ct);

            Response.SetValidationHeader(
                _configuration.GetValue<bool>("Host:IsModuleValidated", false),
                result.DatasourceValidation,
                result.CalculationValidation,
                result.EntityValidation
            );

            return Ok(result.Data);
        }

        /// <summary>
        /// Retrieves details for a single device identified by its web ID.
        /// </summary>
        /// <param name="siteId">The identifier of the site.</param>
        /// <param name="module">The name of the module.</param>
        /// <param name="webId">The web ID of the device to retrieve.</param>
        /// <returns>An <see cref="IActionResult"/> containing the details of the specified device.</returns>
        [HttpGet]
        [Route("device/{webId}/get")]
        [Produces(typeof(DeviceResponse))]
        public async Task<IActionResult> GetSingleDevice(int siteId, string module, string webId, CancellationToken ct = default)
        {
            var user = GetUserContext();
            var result = await _dataService.GetSingleDeviceAsync(siteId, module, webId, user, ct);

            Response.SetValidationHeader(
                _configuration.GetValue<bool>("Host:IsModuleValidated", false),
                result.DatasourceValidation,
                result.CalculationValidation,
                result.EntityValidation
            );

            return Ok(result.Data);
        }

        /// <summary>
        /// Retrieves a list of sensors associated with a specific device identified by its web ID.
        /// </summary>
        /// <param name="siteId">The identifier of the site.</param>
        /// <param name="module">The name of the module.</param>
        /// <param name="webId">The web ID of the device whose sensors are to be retrieved.</param>
        /// <param name="groupName">
        /// An optional parameter to filter sensors by a specific group name.
        /// If not provided, sensors from all groups will be returned.
        /// </param>
        /// <returns>An <see cref="IActionResult"/> containing the list of sensors associated with the specified device.</returns>
        [HttpGet]
        [Route("device/{webId}/sensor/list")]
        [Produces(typeof(SensorListResponse))]
        public async Task<IActionResult> GetDeviceSensors(int siteId, string module, string webId, string? groupName, CancellationToken ct = default)
        {
            var user = GetUserContext();
            var result = await _dataService.GetDeviceSensorsAsync(siteId, module, webId, user, groupName, ct);

            Response.SetValidationHeader(
                _configuration.GetValue<bool>("Host:IsModuleValidated", false),
                result.DatasourceValidation,
                result.CalculationValidation,
                result.EntityValidation
            );

            return Ok(result.Data);
        }

        /// <summary>
        /// Retrieves a list of sensors based on the specified query parameters.
        /// </summary>
        /// <param name="siteId">The identifier of the site.</param>
        /// <param name="module">The name of the module.</param>
        /// <param name="model">
        /// A <see cref="SensorQueryDTO"/> object containing the query parameters to filter sensors.
        /// </param>
        /// <returns>An <see cref="IActionResult"/> containing the list of sensors that match the query.</returns>
        [HttpGet]
        [Route("sensor/multiple")]
        [Produces(typeof(SensorListResponse))]
        public async Task<IActionResult> GetMultipleSensors(int siteId, string module, [FromQuery] SensorQueryDTO model, CancellationToken ct = default)
        {
            var user = GetUserContext();
            var result = await _dataService.GetMultipleSensorsAsync(siteId, module, model, user, ct);

            Response.SetValidationHeader(
                _configuration.GetValue<bool>("Host:IsModuleValidated", false),
                result.DatasourceValidation,
                result.CalculationValidation,
                result.EntityValidation
            );

            return Ok(result.Data);
        }

        /// <summary>
        /// Retrieves details for a single sensor identified by its web ID.
        /// </summary>
        /// <param name="siteId">The identifier of the site.</param>
        /// <param name="module">The name of the module.</param>
        /// <param name="webId">The web ID of the sensor to retrieve.</param>
        /// <returns>An <see cref="IActionResult"/> containing the details of the specified sensor.</returns>
        [HttpGet]
        [Route("sensor/{webId}/get")]
        [Produces(typeof(SensorDTO))]
        public async Task<IActionResult> GetSingleSensor(int siteId, string module, string webId, CancellationToken ct = default)
        {
            var user = GetUserContext();
            var result = await _dataService.GetSingleSensorAsync(siteId, module, webId, user, ct);

            Response.SetValidationHeader(
                _configuration.GetValue<bool>("Host:IsModuleValidated", false),
                result.DatasourceValidation,
                result.CalculationValidation,
                result.EntityValidation
            );

            return Ok(result.Data);
        }
    }
}
