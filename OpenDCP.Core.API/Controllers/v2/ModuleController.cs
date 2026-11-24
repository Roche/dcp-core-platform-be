using OpenDCP.Core.Core.Extensions;
using OpenDCP.Core.Core.Interfaces;
using OpenDCP.Core.Models.Models.Datasource;
using OpenDCP.Core.Models.Models.Module;
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
    [Route("api/v2/admin/module/")]
    public class ModuleController : BaseController
    {
        private readonly IModuleService _moduleService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModuleController"/> class.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="moduleService">The module service used for module management operations.</param>
        public ModuleController(IConfiguration configuration, IModuleService moduleService) : base(configuration)
        {
            _moduleService = moduleService;
        }

        /// <summary>
        /// Get all modules base on site ids
        /// </summary>
        /// <param name="model">
        /// The query parameters for searching sites, encapsulated in a <see cref="DatasourceQueryDTO"/> object.
        /// </param>
        /// <response code="200">The list of sites was successfully retrieved></response>
        [HttpGet]
        [Route("query")]
        [Produces(typeof(List<ModuleListItemDTO>))]
        public async Task<IActionResult> SearchDatasourceDetails([FromQuery] ModuleQueryDTO model)
        {
            var user = GetUserContext();
            var result = await _moduleService.SearchModuleDetailsAsync(model, user);
            Response.SetValidationHeader(_configuration.GetValue<bool>("Host:IsModuleValidated", false), null, null, null);
            return Ok(result);
        }

        /// <summary>
        /// Retrieves the details of a single datasource based on the provided site ID.
        /// </summary>
        /// <param name="moduleId">The ID of the site to retrieve.</param>
        /// <returns>The details of the requested site.</returns>
        /// <response code="200">The site details were successfully retrieved.</response>
        [HttpGet]
        [ActionName("GetSingleModule")]
        [Route("{moduleId}/get")]
        [Produces(typeof(ModuleConfigurationForm))]
        public async Task<IActionResult> GetSingleSite([BindRequired] int moduleId)
        {
            var user = GetUserContext();
            var result = await _moduleService.GetSingleModuleAsync(moduleId, user);
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
        [ActionName("CreateModule")]
        [Route("create")]
        [Produces(typeof(ModuleConfigurationForm))]
        public async Task<IActionResult> CreateModule([FromBody] ModuleUserConfigurationDTO model)
        {
            var user = GetUserContext();
            var result = await _moduleService.CreateModuleAsync(model, user);
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
        /// <param name="moduleId">
        /// The unique identifier of the datasource to be updated.
        /// </param>
        /// <param name="model">
        /// A <see cref="ModuleUserConfigurationDTO"/> object containing the updated configuration details for the site.
        /// </param>
        [HttpPut]
        [ActionName("UpdateModule")]
        [Route("{moduleId}/update")]
        [Produces(typeof(ModuleConfigurationForm))]
        public async Task<IActionResult> UpdateModule(int moduleId, [FromBody] ModuleUserConfigurationDTO model)
        {
            var user = GetUserContext();
            var result = await _moduleService.UpdateModuleAsync(moduleId, model, user);
            Response.SetValidationHeader(_configuration.GetValue<bool>("Host:IsModuleValidated", false), null, null, null);
            return Ok(result);
        }

        /// <summary>
        /// Delete an existing module
        /// </summary>
        /// <param name="moduleId">Module id</param>
        /// <param name="reason">The change reason for deleting the module</param>
        /// <returns>Status code</returns>
        [HttpDelete]
        [ActionName("DeleteModule")]
        [Route("{moduleId}/delete")]
        public async Task<IActionResult> DeleteModule(int moduleId, [BindRequired] string reason)
        {
            var user = GetUserContext();
            await _moduleService.DeleteModuleAsync(moduleId, reason, user);
            return Ok();
        }
    }
}
