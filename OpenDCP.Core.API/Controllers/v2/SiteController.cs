using OpenDCP.Core.Core.Extensions;
using OpenDCP.Core.Core.Interfaces;
using OpenDCP.Core.Models.Models.Module;
using OpenDCP.Core.Models.Models.Site;
using OpenDCP.Framework.AuditTrail.Models;
using Microsoft.AspNetCore.Authorization;
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
    [Route("api/v2/admin/")]
    public class SiteController : BaseController
    {
        private readonly ISiteService _siteService;

        /// <summary>
        /// Initializes a new instance of the <see cref="SiteController"/> class.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="siteService">The site service used for site management operations.</param>
        public SiteController(IConfiguration configuration, ISiteService siteService) : base(configuration)
        {
            _siteService = siteService;
        }

        /// <summary>
        /// Retrieves a list of all configured sites
        /// </summary>
        /// <returns>A list of site information items.</returns>
        /// <response code="200">The list of sites was successfully retrieved.</response>
        [HttpGet]
        [Route("site/list")]
        [Produces(typeof(List<SiteListItemDTO>))]
        public async Task<IActionResult> GetAllSites()
        {
            var user = GetUserContext();
            var result = await _siteService.GetAllSitesAsync(user);
            Response.SetValidationHeader(_configuration.GetValue<bool>("Host:IsModuleValidated", false), null, null, null);
            return Ok(result);
        }

        /// <summary>
        /// Get all site base on site ids
        /// </summary>
        /// <param name="model">
        /// The query parameters for searching sites, encapsulated in a <see cref="SiteQueryDTO"/> object.
        /// </param>
        /// <response code="200">The list of sites was successfully retrieved></response>
        [HttpGet]
        [Route("/api/v2/public/site/query")]
        [Produces(typeof(List<PublicSiteDTO>))]
        [AllowAnonymous]
        public async Task<IActionResult> SearchPublicSiteDetails([FromQuery] SiteQueryDTO model)
        {
            var result = await _siteService.SearchPublicSiteDetailsAsync(model);
            Response.SetValidationHeader(_configuration.GetValue<bool>("Host:IsModuleValidated", false), null, null, null);
            return Ok(result);
        }

        /// <summary>
        /// Get all site base on site ids
        /// </summary>
        /// <param name="model">
        /// The query parameters for searching sites, encapsulated in a <see cref="SiteQueryDTO"/> object.
        /// </param>
        /// <response code="200">The list of sites was successfully retrieved></response>
        [HttpGet]
        [Route("/api/v2/site/query")]
        [Produces(typeof(List<SiteDTO>))]
        public async Task<IActionResult> SearchSiteDetails([FromQuery] SiteQueryDTO model)
        {
            var user = GetUserContext();
            var result = await _siteService.SearchSiteDetailsAsync(model, user);
            Response.SetValidationHeader(_configuration.GetValue<bool>("Host:IsModuleValidated", false), null, null, null);
            return Ok(result);
        }

        /// <summary>
        /// Retrieves the details of a single site based on the provided site ID.
        /// </summary>
        /// <param name="siteId">The ID of the site to retrieve.</param>
        /// <returns>The details of the requested site.</returns>
        /// <response code="200">The site details were successfully retrieved.</response>
        [HttpGet]
        [ActionName("GetSingleSite")]
        [Route("site/{siteId}/get")]
        [Produces(typeof(SiteConfigurationFormDTO))]
        public async Task<IActionResult> GetSingleSite([BindRequired] int siteId)
        {
            var user = GetUserContext();
            var result = await _siteService.GetSingleSiteAsync(siteId, user);
            Response.SetValidationHeader(_configuration.GetValue<bool>("Host:IsModuleValidated", false), null, null, null);
            return Ok(result);
        }

        /// <summary>
        /// Retrieves the modules based on the provided site ID.
        /// </summary>
        /// <param name="siteId">The ID of the site to retrieve.</param>
        /// <returns>The details of the requested site.</returns>
        /// <response code="200">The site details were successfully retrieved.</response>
        [HttpGet]
        [ActionName("GetSiteModules")]
        [Route("/api/v2/site/{siteId}/modules")]
        [Produces(typeof(ModuleListItemDTO))]
        public async Task<IActionResult> GetSiteModules([BindRequired] int siteId)
        {
            var user = GetUserContext();
            var result = await _siteService.GetSiteModulesAsync(siteId, user);
            Response.SetValidationHeader(_configuration.GetValue<bool>("Host:IsModuleValidated", false), null, null, null);
            return Ok(result);
        }

        /// <summary>
        /// Retrieves the details of a single site based on the provided site ID.
        /// </summary>
        /// <param name="siteId">The ID of the site to retrieve.</param>
        /// <returns>The details of the requested site.</returns>
        /// <response code="200">The site details were successfully retrieved.</response>
        [HttpGet]
        [ActionName("GetSiteAuditTrail")]
        [Route("site/{siteId}/audit")]
        [Produces(typeof(List<AuditEntryDisplayDTO>))]
        public async Task<IActionResult> GetSiteAuditTrail([BindRequired] int siteId)
        {
            var user = GetUserContext();
            var result = await _siteService.GetSiteAuditTrailAsync(siteId, user);
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
        /// A <see cref="SiteUserConfigurationDTO"/> object containing the details required to create the site configuration.
        /// </param>
        /// <returns>
        /// An <see cref="IActionResult"/> containing the created <see cref="SiteConfigurationFormDTO"/> object with the details of the newly configured site.
        /// </returns>
        /// <response code="200">Returns the newly created site configuration.</response>
        [HttpPost]
        [ActionName("CreateSite")]
        [Route("site/create")]
        [Produces(typeof(SiteConfigurationFormDTO))]
        public async Task<IActionResult> CreateSite([FromBody] SiteUserConfigurationDTO model)
        {
            var user = GetUserContext();
            var result = await _siteService.CreateSiteAsync(model, user);
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
        /// <param name="siteId">
        /// The unique identifier of the site to be updated.
        /// </param>
        /// <param name="model">
        /// A <see cref="SiteUserConfigurationDTO"/> object containing the updated configuration details for the site.
        /// </param>
        [HttpPut]
        [ActionName("UpdateSite")]
        [Route("site/{siteId}/update")]
        [Produces(typeof(SiteConfigurationFormDTO))]
        public async Task<IActionResult> UpdateSite(int siteId, [FromBody] SiteUserConfigurationDTO model)
        {
            var user = GetUserContext();
            var result = await _siteService.UpdateSiteAsync(siteId, model, user);
            Response.SetValidationHeader(_configuration.GetValue<bool>("Host:IsModuleValidated", false), null, null, null);
            return Ok(result);
        }

        /// <summary>
        /// Delete an existing site
        /// </summary>
        /// <param name="siteId">Site id</param>
        /// <param name="reason">The change reason for deleting the site</param>
        /// <returns>Status code</returns>
        [HttpDelete]
        [ActionName("DeleteSite")]
        [Route("site/{siteId}/delete")]
        public async Task<IActionResult> DeleteSite(int siteId, [BindRequired] string reason)
        {
            var user = GetUserContext();
            await _siteService.DeleteSiteAsync(siteId, reason, user);
            return Ok();
        }

        /// <summary>
        /// Delete all entites related to the site
        /// </summary>
        /// <param name="siteId">Site id</param>
        /// <param name="cleanHistoryTables">Should records in the history tables should be removed as well</param>
        /// <returns>Status code</returns>
        [HttpDelete]
        [ActionName("DeleteSite")]
        [Route("site/{siteId}/relatedEntites/delete")]
        public async Task<IActionResult> DeleteSiteRelatedEntites([BindRequired] int siteId, bool cleanHistoryTables = false)
        {
            var user = GetUserContext();
            await _siteService.DeleteSiteRelatedEntitesAsync(siteId, user, cleanHistoryTables);
            return Ok();
        }
    }
}
