using OpenDCP.Core.Core.Extensions;
using OpenDCP.Core.Core.Interfaces;
using OpenDCP.Core.Models.Models.Configuration;
using OpenDCP.Core.Models.Models.ConnectionBridge;
using OpenDCP.Framework.Hosting.CustomAttributes;
using Microsoft.AspNetCore.Mvc;

namespace OpenDCP.Core.API.Controllers.v2
{
    /// <summary>
    /// API controller for managing Connection Bridge operations.
    /// Provides endpoints to search connection bridges and translate events or identifiers.
    /// </summary>
    [Route("api/v2/connection-bridge/")]
    public class ConnectionBridgeController : BaseController
    {
        private readonly IConnectionBridgeService _cbService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionBridgeController"/> class.
        /// </summary>
        /// <param name="configuration">Application configuration settings.</param>
        /// <param name="connectionBridgeService">Service for connection bridge operations.</param>
        public ConnectionBridgeController(IConfiguration configuration, IConnectionBridgeService connectionBridgeService) : base(configuration)
        {
            _cbService = connectionBridgeService;
        }

        /// <summary>
        /// Searches for connection bridges based on the specified query.
        /// </summary>
        /// <param name="model">The query model containing search parameters.</param>
        /// <param name="microserviceKey">The key added to identify as a server-to-server request</param>
        /// <returns>A list of <see cref="ConnectionBridgeConfigurationDTO"/> matching the query.</returns>
        [HttpGet]
        [Route("query")]
        [Produces(typeof(List<ConnectionBridgeConfigurationDTO>))]
        public async Task<IActionResult> SearchConnectionBridges([FromQuery] ConnectionBridgeQueryDTO model,
            [FromHeader(Name = "X-DCPMicroservice-Key")] string? microserviceKey)
        {
            ValidateAsInternalRequest(microserviceKey);

            var result = await _cbService.SearchConnectionBridgesAsync(model);

            Response.SetValidationHeader(
                _configuration.GetValue<bool>("Host:IsModuleValidated", false),
                null,
                null,
                null
            );

            return Ok(result);
        }

        /// <summary>
        /// Searches for connection bridges based on the specified query.
        /// </summary>
        /// <param name="model">The query model containing search parameters.</param>
        /// <param name="siteId">The ID of the site to filter the search results.</param>
        /// <param name="microserviceKey">The key added to identify as a server-to-server request</param>
        /// <returns>A list of <see cref="ConnectionBridgeConfigurationDTO"/> matching the query.</returns>
        [HttpGet]
        [Route("translate/{siteId}/translate/event")]
        [Produces(typeof(TranslationResponse))]
        [ActionName("GetTranslatedEventIds")]
        [ApiGateway(IsExposed = false)]
        public async Task<IActionResult> TranslateEvents([FromQuery] TranslationQueryDTO model, int siteId,
            [FromHeader(Name = "X-DCPMicroservice-Key")] string? microserviceKey)
        {
            ValidateAsInternalRequest(microserviceKey);
            var result = await _cbService.TranslateEventsAsync(model, siteId);

            Response.SetValidationHeader(
                _configuration.GetValue<bool>("Host:IsModuleValidated", false),
                result.DatasourceValidation,
                result.CalculationValidation,
                result.EntityValidation
            );

            return Ok(result.Data);
        }

        /// <summary>
        /// Translates identifiers for a given site based on the provided translation query.
        /// </summary>
        /// <param name="model">The translation query containing site and identifier information.</param>
        /// <param name="siteId">The ID of the site to filter the search results.</param>
        /// <param name="microserviceKey">The key added to identify as a server-to-server request</param>
        /// <returns>A <see cref="TranslationResponse"/> containing translated identifiers.</returns>
        [HttpGet]
        [Route("translate/{siteId}/translate/id")]
        [Produces(typeof(TranslationResponse))]
        [ActionName("GetTranslatedIdentifiers")]
        [ApiGateway(IsExposed = false)]
        public async Task<IActionResult> TranslateIdentifers([FromQuery] TranslationQueryDTO model, int siteId,
            [FromHeader(Name = "X-DCPMicroservice-Key")] string? microserviceKey)
        {
            ValidateAsInternalRequest(microserviceKey);

            var result = await _cbService.TranslateIdentifersAsync(model, siteId);

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
