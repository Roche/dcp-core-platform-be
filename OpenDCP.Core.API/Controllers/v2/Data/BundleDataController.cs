using OpenDCP.Core.Core.Extensions;
using OpenDCP.Core.Core.Interfaces.Data;
using OpenDCP.Framework.DataTypes.Equipment.Requests.DataBundle;
using OpenDCP.Framework.DataTypes.Equipment.Responses.SensorData;
using Microsoft.AspNetCore.Mvc;

namespace OpenDCP.Core.API.Controllers.v2.Data
{
    /// <summary>
    /// Provides endpoints for retrieving bundled sensor data with contextual batch information.
    /// </summary>
    /// <remarks>
    /// These endpoints are used to retrieve time-series sensor data enriched with batch context
    /// for a specific site and module.
    /// </remarks>
    [Route("api/v2/data/{siteId}/{module}/bundle/")]
    public class BundleDataController : BaseController
    {
        private readonly IBundleDataService _dataService;

        /// <summary>
        /// Initializes a new instance of the <see cref="BundleDataController"/> class.
        /// </summary>
        /// <param name="configuration">Application configuration.</param>
        /// <param name="dataService">Service used to retrieve bundled sensor data.</param>
        public BundleDataController(IConfiguration configuration, IBundleDataService dataService)
            : base(configuration)
        {
            _dataService = dataService;
        }

        /// <summary>
        /// Retrieves plot-ready sensor data enriched with batch context information.
        /// </summary>
        /// <param name="siteId">The identifier of the site for which data is requested.</param>
        /// <param name="module">The module name associated with the requested data.</param>
        /// <param name="model">
        /// Query parameters defining the time range, sensors, and batch context
        /// for the plot data request.
        /// </param>
        /// <returns>
        /// An <see cref="IActionResult"/> containing a <see cref="SensorDataWithBatchContextResponse"/>
        /// object with plot-ready sensor data and batch context.
        /// </returns>
        /// <response code="200">Returns the sensor data with batch context.</response>
        /// <response code="400">If the query parameters are invalid.</response>
        /// <response code="401">If the request is unauthorized.</response>
        [HttpGet]
        [Route("plot-with-context")]
        [Produces(typeof(SensorDataWithBatchContextResponse))]
        public async Task<IActionResult> GetPlotDataWithBatchContext(
            int siteId,
            string module,
            [FromQuery] PlotDataWithBatchContextQueryDTO model,
            CancellationToken ct = default)
        {
            var user = GetUserContext();
            var result = await _dataService.GetPlotDataWithContextAsync(siteId, module, model, user, ct);

            Response.SetValidationHeader(
                _configuration.GetValue<bool>("Host:IsModuleValidated", false),
                result.DatasourceValidation,
                result.CalculationValidation,
                result.EntityValidation
            );

            return Ok(result.Data);
        }

        /// <summary>
        /// Retrieves interpolated sensor data enriched with batch context information.
        /// </summary>
        /// <param name="siteId">The identifier of the site for which data is requested.</param>
        /// <param name="module">The module name associated with the requested data.</param>
        /// <param name="model">
        /// Query parameters defining the interpolation rules, time range, sensors,
        /// and batch context for the data request.
        /// </param>
        /// <returns>
        /// An <see cref="IActionResult"/> containing a <see cref="SensorDataWithBatchContextResponse"/>
        /// object with interpolated sensor data and batch context.
        /// </returns>
        /// <response code="200">Returns the interpolated sensor data with batch context.</response>
        /// <response code="400">If the query parameters are invalid.</response>
        /// <response code="401">If the request is unauthorized.</response>
        [HttpGet]
        [Route("interpolated-with-context")]
        [Produces(typeof(SensorDataWithBatchContextResponse))]
        public async Task<IActionResult> GetInterpoaltedDataWithBatchContext(
            int siteId,
            string module,
            [FromQuery] InterpolatedDataWithBatchContextQueryDTO model,
            CancellationToken ct = default)
        {
            var user = GetUserContext();
            var result = await _dataService.GetInterpolatedDataWithContextAsync(siteId, module, model, user, ct);

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
