using OpenDCP.Core.Core.Extensions;
using OpenDCP.Core.Core.Interfaces.Data;
using OpenDCP.Framework.DataTypes.Equipment.Entities.SensorData;
using OpenDCP.Framework.DataTypes.Equipment.Requests.SensorData;
using OpenDCP.Framework.DataTypes.Equipment.Responses.SensorData;
using Microsoft.AspNetCore.Mvc;

namespace OpenDCP.Core.API.Controllers.v2.Data
{
    /// <summary>
    /// Provides read access to sensor telemetry data.
    /// The data returned is based on the connections mapped to the specified site and module.
    /// </summary>
    [Route("api/v2/data/{siteId}/{module}/sensor-telemetry/")]
    public class SensorTelemetryDataController : BaseController
    {
        private readonly ISensorTelemetryDataService _dataService;

        /// <summary>
        /// Initializes a new instance of the <see cref="SensorTelemetryDataController"/>.
        /// </summary>
        /// <param name="configuration">Application configuration instance.</param>
        /// <param name="dataService">Service used to retrieve sensor telemetry data.</param>
        public SensorTelemetryDataController(IConfiguration configuration, ISensorTelemetryDataService dataService) : base(configuration)
        {
            _dataService = dataService;
        }

        /// <summary>
        /// Retrieves interpolated sensor data based on specified query parameters.
        /// </summary>
        /// <param name="siteId">The ID of the site where the data is located.</param>
        /// <param name="module">The module name to narrow the scope of the data retrieval.</param>
        /// <param name="model">The query parameters defining the interpolation criteria and additional filters.</param>
        /// <returns>
        /// An <see cref="IActionResult"/> containing the interpolated sensor data wrapped in a <see cref="SensorDataResponse"/> object.
        /// </returns>
        [HttpGet]
        [Route("interpolated")]
        [Produces(typeof(SensorDataResponse))]
        public async Task<IActionResult> GetInterpolatedData(int siteId, string module, [FromQuery] InterpolatedDataQueryDTO model, CancellationToken ct = default)
        {
            var user = GetUserContext();
            var result = await _dataService.GetInterpolatedDataAsync(siteId, module, model, user, ct);

            Response.SetValidationHeader(
                _configuration.GetValue<bool>("Host:IsModuleValidated", false),
                result.DatasourceValidation,
                result.CalculationValidation,
                result.EntityValidation
            );

            return Ok(result.Data);
        }

        /// <summary>
        /// Retrieves recorded sensor data based on specified query parameters.
        /// </summary>
        /// <param name="siteId">The ID of the site where the data is located.</param>
        /// <param name="module">The module name to narrow the scope of the data retrieval.</param>
        /// <param name="model">The query parameters defining the recorded data criteria and filters.</param>
        /// <returns>
        /// An <see cref="IActionResult"/> containing the recorded sensor data wrapped in a <see cref="SensorDataResponse"/> object.
        /// </returns>
        [HttpGet]
        [Route("recorded")]
        [Produces(typeof(SensorDataResponse))]
        public async Task<IActionResult> GetRecordedData(int siteId, string module, [FromQuery] RecordedDataQueryDTO model, CancellationToken ct = default)
        {
            var user = GetUserContext();
            var result = await _dataService.GetRecordedDataAsync(siteId, module, model, user, ct);

            Response.SetValidationHeader(
                _configuration.GetValue<bool>("Host:IsModuleValidated", false),
                result.DatasourceValidation,
                result.CalculationValidation,
                result.EntityValidation
            );

            return Ok(result.Data);
        }

        /// <summary>
        /// Retrieves sensor data for plotting based on specified query parameters.
        /// </summary>
        /// <param name="siteId">The ID of the site where the data is located.</param>
        /// <param name="module">The module name to narrow the scope of the data retrieval.</param>
        /// <param name="model">The query parameters defining the data to be plotted, including filters and criteria.</param>
        /// <returns>
        /// An <see cref="IActionResult"/> containing the plot data wrapped in a <see cref="SensorDataResponse"/> object.
        /// </returns>
        [HttpGet]
        [Route("plot")]
        [Produces(typeof(SensorDataResponse))]
        public async Task<IActionResult> GetPlotData(int siteId, string module, [FromQuery] PlotDataQueryDTO model, CancellationToken ct = default)
        {
            var user = GetUserContext();
            var result = await _dataService.GetPlotDataAsync(siteId, module, model, user, ct);

            Response.SetValidationHeader(
                _configuration.GetValue<bool>("Host:IsModuleValidated", false),
                result.DatasourceValidation,
                result.CalculationValidation,
                result.EntityValidation
            );

            return Ok(result.Data);
        }

        /// <summary>
        /// Retrieves interpolated sensor data at specific times based on the provided query parameters.
        /// </summary>
        /// <param name="siteId">The ID of the site where the data is located.</param>
        /// <param name="module">The module name to narrow the scope of the data retrieval.</param>
        /// <param name="model">The query parameters defining the times at which data is interpolated and additional filters.</param>
        /// <returns>
        /// An <see cref="IActionResult"/> containing the interpolated sensor data at specific times, wrapped in a <see cref="SensorDataResponse"/> object.
        /// </returns>
        [HttpGet]
        [Route("interpolatedAtTimes")]
        [Produces(typeof(SensorDataResponse))]
        public async Task<IActionResult> GetInterpolatedAtTimesData(int siteId, string module, [FromQuery] InterpolatedAtTimeDataQueryDTO model, CancellationToken ct = default)
        {
            var user = GetUserContext();
            var result = await _dataService.GetInterpolatedDataAtTimesAsync(siteId, module, model, user, ct);

            Response.SetValidationHeader(
                _configuration.GetValue<bool>("Host:IsModuleValidated", false),
                result.DatasourceValidation,
                result.CalculationValidation,
                result.EntityValidation
            );

            return Ok(result.Data);
        }

        /// <summary>
        /// Retrieves recorded sensor data at specific times based on the provided query parameters.
        /// </summary>
        /// <param name="siteId">The ID of the site where the data is located.</param>
        /// <param name="module">The module name to narrow the scope of the data retrieval.</param>
        /// <param name="model">The query parameters defining the times at which data is recorded and additional filters.</param>
        [HttpGet]
        [Route("recordedAtTimes")]
        [Produces(typeof(List<SensorDataDTO>))]
        public async Task<IActionResult> GetRecordedAtTimesData(int siteId, string module, [FromQuery] RecordedAtTimeDataQueryDTO model, CancellationToken ct = default)
        {
            var user = GetUserContext();
            var result = await _dataService.GetRecordedDataAtTimesAsync(siteId, module, model, user, ct);

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
