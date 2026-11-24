using OpenDCP.Core.Core.Extensions;
using OpenDCP.Core.Core.Interfaces.Data;
using OpenDCP.Framework.DataTypes.Analytical.Requests.AnalyticalData;
using OpenDCP.Framework.DataTypes.Analytical.Responses.AnalyticalData;
using Microsoft.AspNetCore.Mvc;

namespace OpenDCP.Core.API.Controllers.v2.Data
{
    /// <summary>
    /// Controller for accessing analytical data attributes and their data points.
    /// Provides endpoints for retrieving attribute metadata and associated data.
    /// </summary>
    [Route("api/v2/data/{siteId}/{module}/analytical/")]
    public class AnalyticalDataController : BaseController
    {
        private readonly IAnalyticalDataService _dataService;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnalyticalDataController"/> class.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="dataService">The analytical data service for retrieving attributes and data points.</param>
        public AnalyticalDataController(IConfiguration configuration, IAnalyticalDataService dataService) : base(configuration)
        {
            _dataService = dataService;
        }

        /// <summary>
        /// Retrieves a list of analytical attributes for the specified site, module, and entity.
        /// </summary>
        /// <param name="siteId">The identifier of the site.</param>
        /// <param name="module">The name of the module.</param>
        /// <param name="webId">The web ID of the entity whose attributes are requested.</param>
        /// <returns>An <see cref="IActionResult"/> containing an <see cref="AttributeListResponse"/> with the list of attributes.</returns>
        [HttpGet]
        [Route("attribute/list")]
        [Produces(typeof(AttributeListResponse))]
        public async Task<IActionResult> GetAnalyticalDataAttributes(int siteId, string module, string webId, CancellationToken ct = default)
        {
            var user = GetUserContext();
            var result = await _dataService.GetAttributesAsync(siteId, module, webId, user, ct);

            Response.SetValidationHeader(
                _configuration.GetValue<bool>("Host:IsModuleValidated", false),
                result.DatasourceValidation,
                result.CalculationValidation,
                result.EntityValidation
            );

            return Ok(result.Data);
        }

        /// <summary>
        /// Retrieves analytical attribute data points based on the provided query.
        /// </summary>
        /// <param name="siteId">The identifier of the site.</param>
        /// <param name="module">The name of the module.</param>
        /// <param name="model">An <see cref="AttributeDataQueryDTO"/> object containing the query parameters for filtering attribute data points.</param>
        /// <returns>An <see cref="IActionResult"/> containing an <see cref="AttributeDataResponse"/> with the requested data points.</returns>
        [HttpGet]
        [Route("attribute/data")]
        [Produces(typeof(AttributeDataResponse))]
        public async Task<IActionResult> GetAnalyticalAttributesDataPoints(int siteId, string module, [FromQuery] AttributeDataQueryDTO model, CancellationToken ct = default)
        {
            var user = GetUserContext();
            var result = await _dataService.GetAttributeDataAsync(siteId, module, model, user, ct);

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
