using OpenDCP.Core.Core.Extensions;
using OpenDCP.Core.Core.Interfaces;
using OpenDCP.Core.Models.Models.Datasource;
using OpenDCP.Core.Models.Models.Site;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace OpenDCP.Core.API.Controllers.v2
{
    /// <summary>
    /// Provides administrative operations for managing sites.
    /// </summary>
    /// <remarks>
    /// This controller allows administrators to create, update, retrieve, and delete site configurations.
    /// It also provides endpoints to query audit trails and public site information.
    /// </remarks>
    [Route("api/v2/admin/datasource/")]
    public class DatasourceController : BaseController
    {
        private readonly IDatasourceService _datasourceService;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatasourceController"/> class.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="datasourceService">The datasource service used for datasource management operations.</param>
        public DatasourceController(IConfiguration configuration, IDatasourceService datasourceService) : base(configuration)
        {
            _datasourceService = datasourceService;
        }

        /// <summary>
        /// Get all datasources base on site ids
        /// </summary>
        /// <param name="model">
        /// The query parameters for searching sites, encapsulated in a <see cref="DatasourceQueryDTO"/> object.
        /// </param>
        /// <response code="200">The list of sites was successfully retrieved></response>
        [HttpGet]
        [Route("query")]
        [Produces(typeof(List<DatasourceListItemDTO>))]
        public async Task<IActionResult> SearchDatasourceDetails([FromQuery] DatasourceQueryDTO model)
        {
            var user = GetUserContext();
            var result = await _datasourceService.SearchDatasourceDetailsAsync(model, user);
            Response.SetValidationHeader(_configuration.GetValue<bool>("Host:IsModuleValidated", false), null, null, null);
            return Ok(result);
        }

        /// <summary>
        /// Retrieves the details of a single datasource based on the provided site ID.
        /// </summary>
        /// <param name="datasourceId">The ID of the site to retrieve.</param>
        /// <returns>The details of the requested site.</returns>
        /// <response code="200">The site details were successfully retrieved.</response>
        [HttpGet]
        [ActionName("GetSingleDatasource")]
        [Route("{datasourceId}/get")]
        [Produces(typeof(DatasourceConfigurationForm))]
        public async Task<IActionResult> GetSingleSite([BindRequired] int datasourceId)
        {
            var user = GetUserContext();
            var result = await _datasourceService.GetSingleDatasourceAsync(datasourceId, user);
            Response.SetValidationHeader(_configuration.GetValue<bool>("Host:IsModuleValidated", false), null, null, null);
            return Ok(result);
        }

        /// <summary>
        /// Creates a new site configuration based on the provided user input.
        /// </summary>
        /// <remarks>
        /// This endpoint accepts a site user configuration and creates a new site configuration in the system.
        /// The operation is performed in the context of the current user.
        /// </remarks>
        /// <param name="model">
        /// A <see cref="DatasourceUserConfigurationDTO"/> object containing the details required to create the site configuration.
        /// </param>
        /// <returns>
        /// An <see cref="IActionResult"/> containing the created <see cref="SiteConfigurationFormDTO"/> object with the details of the newly configured site.
        /// </returns>
        /// <response code="200">Returns the newly created site configuration.</response>
        [HttpPost]
        [ActionName("CreateDatasource")]
        [Route("create")]
        [Produces(typeof(DatasourceConfigurationForm))]
        public async Task<IActionResult> CreateDatasource([FromBody] DatasourceUserConfigurationDTO model)
        {
            var user = GetUserContext();
            var result = await _datasourceService.CreateDatasourceAsync(model, user);
            Response.SetValidationHeader(_configuration.GetValue<bool>("Host:IsModuleValidated", false), null, null, null);
            return Ok(result);
        }

        /// <summary>
        /// Updates an existing site configuration with the specified details.
        /// </summary>
        /// <remarks>
        /// This endpoint allows updating the configuration of an existing site identified by its <paramref name="siteId"/>. 
        /// The operation considers the context of the current user.
        /// </remarks>
        /// <param name="datasourceId">
        /// The unique identifier of the datasource to be updated.
        /// </param>
        /// <param name="model">
        /// A <see cref="SiteUserConfigurationDTO"/> object containing the updated configuration details for the site.
        /// </param>
        [HttpPut]
        [ActionName("UpdateDatasource")]
        [Route("{datasourceId}/update")]
        [Produces(typeof(DatasourceConfigurationForm))]
        public async Task<IActionResult> UpdateDatasource(int datasourceId, [FromBody] DatasourceUserConfigurationDTO model)
        {
            var user = GetUserContext();
            var result = await _datasourceService.UpdateDatasourceAsync(datasourceId, model, user);
            Response.SetValidationHeader(_configuration.GetValue<bool>("Host:IsModuleValidated", false), null, null, null);
            return Ok(result);
        }

        /// <summary>
        /// Delete an existing datasource
        /// </summary>
        /// <param name="datasourceId">Datasource id</param>
        /// <param name="reason">The change reason for deleting the datasource</param>
        /// <returns>Status code</returns>
        [HttpDelete]
        [ActionName("DeleteDatasource")]
        [Route("{datasourceId}/delete")]
        public async Task<IActionResult> DeleteDatasource(int datasourceId, [BindRequired] string reason)
        {
            var user = GetUserContext();
            await _datasourceService.DeleteDatasourceAsync(datasourceId, reason, user);
            return Ok();
        }
    }
}
