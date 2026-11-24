using OpenDCP.Core.Core.Extensions;
using OpenDCP.Core.Core.Interfaces;
using OpenDCP.Framework.Dashboard.Models;
using OpenDCP.Framework.Dashboard.Requests;
using OpenDCP.Framework.Hosting.CustomAttributes;
using Microsoft.AspNetCore.Mvc;

namespace OpenDCP.Core.API.Controllers.v2
{
    /// <summary>
    /// Provides operations for managing dashboard tabs and configuration data, including import and export functionality.
    /// </summary>
    /// <remarks>
    /// This controller handles the CRUD lifecycle for individual dashboard tabs and provides 
    /// endpoints for migrating dashboard state via exports and imports.
    /// </remarks>
    [Route("api/v2/dashboard/")]
    public class DashboardController : BaseController
    {
        private readonly IDashboardService _dashboardService;

        /// <summary>
        /// Initializes a new instance of the <see cref="DashboardController"/> class.
        /// </summary>
        /// <param name="configuration">The application configuration provided by the host.</param>
        /// <param name="dashboardService">The service logic for dashboard tab management and data portability.</param>
        public DashboardController(IConfiguration configuration, IDashboardService dashboardService) : base(configuration)
        {
            _dashboardService = dashboardService;
        }

        /// <summary>
        /// Creates a new dashboard tab based on the provided configuration.
        /// </summary>
        /// <param name="model">The data transfer object containing the dashboard tab settings.</param>
        /// <returns>
        /// An <see cref="IActionResult"/> containing the newly created <see cref="DashboardTabDTO"/>.
        /// </returns>
        /// <remarks>
        /// This endpoint persists a new tab configuration to the database. 
        /// It also sets the GXP validation header based on the system's "IsModuleValidated" configuration.
        /// </remarks>
        [HttpPost]
        [ActionName("CreateDashboardTab")]
        [Route("tab/create")]
        [Produces(typeof(DashboardTabDTO))]
        public async Task<IActionResult> CreateDashboardTab([FromBody] DashboardTabConfigurationDTO model)
        {
            var user = GetUserContext();
            var result = await _dashboardService.CreateDashboardTabAsync(model, user);
            Response.SetValidationHeader(_configuration.GetValue<bool>("Host:IsModuleValidated", false), null, null, null);
            return Ok(result);
        }

        /// <summary>
        /// Updates an existing dashboard tab configuration.
        /// </summary>
        /// <param name="dashboardTabId">The dashboard tab Id</param>
        /// <param name="model">The data transfer object containing the updated dashboard tab settings.</param>
        /// <returns>
        /// An <see cref="IActionResult"/> containing the updated <see cref="DashboardTabDTO"/>.
        /// </returns>
        /// <remarks>
        /// This endpoint updates the existing tab record in the database. 
        /// It also sets the GXP validation header based on the system's "IsModuleValidated" configuration.
        /// </remarks>
        [HttpPut]
        [ActionName("UpdateDashboardTab")]
        [Route("tab/{dashboardTabId}/update")]
        [Produces(typeof(DashboardTabDTO))]
        public async Task<IActionResult> UpdateDashboardTab(Guid dashboardTabId, [FromBody] DashboardTabConfigurationDTO model)
        {
            var user = GetUserContext();
            var result = await _dashboardService.UpdateDashboardTabAsync(dashboardTabId, model, user);
            Response.SetValidationHeader(_configuration.GetValue<bool>("Host:IsModuleValidated", false), null, null, null);
            return Ok(result);
        }

        /// <summary>
        /// Retrieves a specific dashboard tab based on the provided query criteria for the current user.
        /// </summary>
        /// <param name="model">The query model containing the filter criteria for the dashboard tab.</param>
        /// <returns>
        /// An <see cref="IActionResult"/> containing the first <see cref="DashboardTabDTO"/> that matches the query.
        /// </returns>
        /// <remarks>
        /// This endpoint filters dashboard tabs based on the query parameters. 
        /// It returns a single result and sets the GXP validation header.
        /// </remarks>
        [HttpGet]
        [ActionName("GetDashboardTab")]
        [Route("tab/get")]
        [Produces(typeof(DashboardTabDTO))]
        [ApiGateway(IsExposed = true)]
        public async Task<IActionResult> GetDashboardTab([FromQuery] DashboardTabQueryDTO model)
        {
            var user = GetUserContext();
            var result = await _dashboardService.SearchDashboardTabsAsync(user, model);
            Response.SetValidationHeader(_configuration.GetValue<bool>("Host:IsModuleValidated", false), null, null, null);
            return this.Ok(result.FirstOrDefault());
        }

        /// <summary>
        /// Retrieves a list of all dashboard tabs associated with the current user.
        /// </summary>
        /// <returns>
        /// An <see cref="IActionResult"/> containing a <see cref="List{DashboardTabDTO}"/>.
        /// </returns>
        /// <remarks>
        /// This endpoint fetches the complete set of dashboard tabs for the authenticated user.
        /// It also sets the GXP validation header based on the system's "IsModuleValidated" configuration.
        /// </remarks>
        [HttpGet]
        [ActionName("GetUserDashboardTabs")]
        [Route("tab/user/query")]
        [Produces(typeof(List<DashboardTabDTO>))]
        [ApiGateway(IsExposed = true)]
        public async Task<IActionResult> GetUserDashboardTabs()
        {
            var user = GetUserContext();
            var result = await _dashboardService.SearchDashboardTabsAsync(user);
            Response.SetValidationHeader(_configuration.GetValue<bool>("Host:IsModuleValidated", false), null, null, null);
            return this.Ok(result);
        }

        /// <summary>
        /// Retrieves a specific dashboard tab based on the provided query criteria for the current user.
        /// </summary>
        /// <param name="model">The query model containing the filter criteria for the dashboard tab.</param>
        /// <returns>
        /// An <see cref="IActionResult"/> containing the first <see cref="DashboardTabDTO"/> that matches the query.
        /// </returns>
        /// <remarks>
        /// This endpoint filters dashboard tabs based on the query parameters. 
        /// It returns a single result and sets the GXP validation header.
        /// </remarks>
        [HttpGet]
        [ActionName("GetDashboardTabs")]
        [Route("tab/query")]
        [Produces(typeof(List<DashboardTabDTO>))]
        [ApiGateway(IsExposed = true)]
        public async Task<IActionResult> GetDashboardTabs([FromQuery] DashboardTabQueryDTO model)
        {
            var user = GetUserContext();
            var result = await _dashboardService.SearchDashboardTabsAsync(user, model);
            Response.SetValidationHeader(_configuration.GetValue<bool>("Host:IsModuleValidated", false), null, null, null);
            return this.Ok(result);
        }

        /// <summary>
        /// Deletes a dashboard tab associated with the specified unique identifier.
        /// </summary>
        /// <param name="guid">The unique identifier (GUID) of the dashboard tab to delete.</param>
        /// <returns>An <see cref="IActionResult"/> indicating the result of the deletion operation.</returns>
        /// <remarks>
        /// This endpoint permanently removes an existing dashboard tab from the system.
        /// Access is restricted based on the user's context and permissions.
        /// </remarks>>
        [HttpDelete]
        [ActionName("DeleteDashboardTab")]
        [Route("tab/{guid}/delete")]
        public async Task<IActionResult> DeleteDashboardTab(Guid guid)
        {
            var user = GetUserContext();
            await _dashboardService.DeleteDashboardTabAsync(guid, user);
            return this.Ok();
        }

        /// <summary>
        /// Export current logged user dashboard
        /// </summary>
        /// <param name="dashboardTabId">Dashboard tab id</param>
        /// <returns>Unique identifier for given user dashboard snapshot</returns>
        [HttpGet]
        [ActionName("ExportDashboard")]
        [Route("export")]
        [Produces(typeof(Guid))]
        public async Task<IActionResult> ExportDashboard(Guid dashboardTabId)
        {
            var user = GetUserContext();
            var result = await _dashboardService.ExportDashboardAsync(dashboardTabId, user);
            return this.Ok(result);
        }

        /// <summary>
        /// Import current logged user dashboard
        /// </summary>
        /// <returns>Newly created dashboard tab id</returns>
        [HttpPost]
        [ActionName("ImportDashboard")]
        [Route("import")]
        [Produces(typeof(Guid))]
        public async Task<IActionResult> ImportDashboard([FromBody] DashboardImportDTO model)
        {
            var user = GetUserContext();
            var result = await _dashboardService.ImportDashboardAsync(model, user);
            return this.Ok(result);
        }

        /// <summary>
        /// Delete dashboard export item
        /// </summary>
        /// <param name="guid">Dashboard export id</param>
        /// <returns>Status code</returns>
        [HttpDelete]
        [ActionName("DeleteDashboardExport")]
        [Route("export/{guid}/delete")]
        public async Task<IActionResult> DeleteDashboardExport(Guid guid)
        {
            var user = GetUserContext();
            await _dashboardService.DeleteDashboardExportAsync(guid, user);
            return Ok();
        }


        /// <summary>
        /// Retrieves a specific dashboard export based on the provided query criteria for the current user.
        /// </summary>
        /// <param name="model">The query model containing the filter criteria for the dashboard export.</param>
        /// <returns>
        /// An <see cref="IActionResult"/> containing the first <see cref="DashboardTabDTO"/> that matches the query.
        /// </returns>
        /// <remarks>
        /// This endpoint filters dashboard export based on the query parameters. 
        /// </remarks>
        [HttpGet]
        [ActionName("GetExportedDashboardTabs")]
        [Route("tab/export/query")]
        [Produces(typeof(List<DashboardExportDTO>))]
        [ApiGateway(IsExposed = true)]
        public async Task<IActionResult> GetExportedDashboardTabs([FromQuery] DashboardExportQueryDTO model)
        {
            var user = GetUserContext();
            var result = await _dashboardService.SearchDashboardExportAsync(user, model);
            Response.SetValidationHeader(_configuration.GetValue<bool>("Host:IsModuleValidated", false), null, null, null);
            return this.Ok(result);
        }

        /// <summary>
        /// Update dashboard tab layout
        /// </summary>
        /// <returns>Status code</returns>
        [HttpPut]
        [ActionName("UpdateDashboardTabLayout")]
        [Route("tab/layout/update")]
        public async Task<IActionResult> UpdateDashboardTabLayout(int siteId, [FromBody] DashboardTabLayoutConfigurationDTO model)
        {
            var user = GetUserContext();
            await _dashboardService.UpdateDashboardTabLayoutAsync(siteId, model, user);
            return this.Ok();
        }
    }
}
