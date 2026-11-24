using OpenDCP.Core.Core.Extensions;
using OpenDCP.Core.Core.Interfaces.Data;
using OpenDCP.Framework.DataTypes.Equipment.Requests.EventFrameContext;
using OpenDCP.Framework.DataTypes.Equipment.Responses.EventFrameContext;
using Microsoft.AspNetCore.Mvc;

namespace OpenDCP.Core.API.Controllers.v2.Data
{
    /// <summary>
    /// Provides read access to asset event frame context data.
    /// The data returned is based on the connections mapped to the specified site and module.
    /// </summary>
    [Route("api/v2/data/{siteId}/{module}/eventframe-context/")]
    public class EventframeContextDataController : BaseController
    {
        private readonly IEventframeContextDataService _dataService;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventframeContextDataController"/>.
        /// </summary>
        /// <param name="configuration">Application configuration instance.</param>
        /// <param name="dataService">Service used to retrieve eventframe context data.</param>
        public EventframeContextDataController(IConfiguration configuration, IEventframeContextDataService dataService) : base(configuration)
        {
            _dataService = dataService;
        }

        /// <summary>
        /// Searches for batch context information based on a specified time range and other criteria.
        /// </summary>
        /// <param name="siteId">The ID of the site to filter the search results.</param>
        /// <param name="module">The module name to narrow down the search scope.</param>
        /// <param name="model">The query parameters specifying the time range and other criteria for the search.</param>
        /// <returns>
        /// An <see cref="IActionResult"/> containing the search results wrapped in a <see cref="BatchContextListResponse"/> object.
        /// </returns>
        /// <remarks>
        /// The context does not include meta data which could either retrived seperatly or use the /eventframe route to include the
        /// meta data from the start
        /// </remarks>
        [HttpGet]
        [Route("context/query/time")]
        [Produces(typeof(BatchContextListResponse))]
        public async Task<IActionResult> SearchBatchContextByTime(int siteId, string module, [FromQuery] BatchContextTimeQueryDTO model, CancellationToken ct = default)
        {
            var user = GetUserContext();
            var result = await _dataService.SearchBatchContextByTimeAsync(siteId, module, model, user, ct);

            Response.SetValidationHeader(
                _configuration.GetValue<bool>("Host:IsModuleValidated", false),
                result.DatasourceValidation,
                result.CalculationValidation,
                result.EntityValidation
            );

            return Ok(result.Data);
        }

        /// <summary>
        /// Searches for event frames based on a specified time range and other criteria.
        /// </summary>
        /// <param name="siteId">The ID of the site used to filter the event frames.</param>
        /// <param name="module">The module name used to narrow the search scope.</param>
        /// <param name="model">The query parameters specifying the time range and additional search criteria.</param>
        [HttpGet]
        [Route("eventframe/query/time")]
        [Produces(typeof(EventframeListResponse))]
        public async Task<IActionResult> SearchEventframesByTime(int siteId, string module, [FromQuery] EventframeContextTimeQueryDTO model, CancellationToken ct = default)
        {
            var user = GetUserContext();
            var result = await _dataService.SearchEventframeContextByTimeAsync(siteId, module, model, user, ct);

            Response.SetValidationHeader(
                _configuration.GetValue<bool>("Host:IsModuleValidated", false),
                result.DatasourceValidation,
                result.CalculationValidation,
                result.EntityValidation
            );

            return Ok(result.Data);
        }

        /// <summary>
        /// Searches for event frames based on specified IDs and other criteria.
        /// </summary>
        /// <param name="siteId">The ID of the site used to filter the event frames.</param>
        /// <param name="module">The module name used to narrow the search scope.</param>
        /// <param name="model">The query parameters containing the event frame IDs and additional search criteria.</param>
        /// <returns>
        /// An <see cref="IActionResult"/> containing the search results wrapped in an <see cref="EventframeListResponse"/> object.
        /// </returns>
        [HttpGet]
        [Route("eventframe/query/id")]
        [Produces(typeof(EventframeListResponse))]
        public async Task<IActionResult> SearchEventframesById(int siteId, string module, [FromQuery] EventframeContextIdQueryDTO model, CancellationToken ct = default)
        {
            var user = GetUserContext();
            var result = await _dataService.SearchEventframeContextByIdAsync(siteId, module, model, user, ct);

            Response.SetValidationHeader(
                _configuration.GetValue<bool>("Host:IsModuleValidated", false),
                result.DatasourceValidation,
                result.CalculationValidation,
                result.EntityValidation
            );

            return Ok(result.Data);
        }

        /// <summary>
        /// Retrieves a specific event frame by its ID.
        /// </summary>
        /// <param name="siteId">The ID of the site where the event frame is located.</param>
        /// <param name="module">The module name to narrow the search scope for the event frame.</param>
        /// <param name="id">The unique identifier of the event frame to retrieve.</param>
        /// <returns>
        /// An <see cref="IActionResult"/> containing the details of the requested event frame wrapped in an <see cref="EventframeListValuesResponse"/> object.
        /// </returns>
        [HttpGet]
        [Route("eventframe/{id}/get")]
        [Produces(typeof(EventframeListValuesResponse))]
        public async Task<IActionResult> GetEventframe(int siteId, string module, string id, CancellationToken ct = default)
        {
            var user = GetUserContext();
            var result = await _dataService.GetEventframeAsync(siteId, module, id, user, ct);

            Response.SetValidationHeader(
                _configuration.GetValue<bool>("Host:IsModuleValidated", false),
                result.DatasourceValidation,
                result.CalculationValidation,
                result.EntityValidation
            );

            return Ok(result.Data);
        }

        /// <summary>
        /// Retrieves the values associated with a specific event frame by its ID.
        /// </summary>
        /// <param name="siteId">The ID of the site where the event frame is located.</param>
        /// <param name="module">The module name to narrow the search scope for the event frame.</param>
        /// <param name="id">The unique identifier of the event frame whose values are to be retrieved.</param>
        /// <returns>
        /// An <see cref="IActionResult"/> containing the values of the specified event frame wrapped in an <see cref="EventframeValuesResponse"/> object.
        /// </returns>
        [HttpGet]
        [Route("eventframe/{id}/values/get")]
        [Produces(typeof(EventframeValuesResponse))]
        public async Task<IActionResult> GetEventframeValues(int siteId, string module, string id, CancellationToken ct = default)
        {
            var user = GetUserContext();
            var result = await _dataService.GetEventframeValuesAsync(siteId, module, id, user, ct);

            Response.SetValidationHeader(
                _configuration.GetValue<bool>("Host:IsModuleValidated", false),
                result.DatasourceValidation,
                result.CalculationValidation,
                result.EntityValidation
            );

            return Ok(result.Data);
        }

        /// <summary>
        /// Retrieves the attributes of an event frame associated with a specific device.
        /// </summary>
        /// <param name="siteId">The ID of the site where the event frame is located.</param>
        /// <param name="module">The module name to narrow the search scope for the event frame attributes.</param>
        /// <param name="deviceWebId">The unique Web ID of the device associated with the event frame.</param>
        /// <returns>
        /// An <see cref="IActionResult"/> containing the attributes of the specified event frame wrapped in an <see cref="EventframeAttributesResponse"/> object.
        /// </returns>
        [HttpGet]
        [Route("eventframe/attributes/get")]
        [Produces(typeof(EventframeAttributesResponse))]
        public async Task<IActionResult> GetEventframeAttributes(int siteId, string module, string deviceWebId, CancellationToken ct = default)
        {
            var user = GetUserContext();
            var result = await _dataService.GetEventframeAttributesAsync(siteId, module, deviceWebId, user, ct);

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
