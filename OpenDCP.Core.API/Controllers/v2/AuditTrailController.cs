using OpenDCP.Core.Core.Extensions;
using OpenDCP.Core.Core.Interfaces;
using OpenDCP.Core.Models.Models.AuditTrail;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Globalization;
using System.Net.Mime;

namespace OpenDCP.Core.API.Controllers.v2
{
    /// <summary>
    /// Provides endpoints for retrieving and creating audit trail records.
    /// </summary>
    /// <remarks>
    /// This controller exposes audit trail data for configuration changes,
    /// security-related events, and administrative actions.
    /// All downloadable endpoints return CSV files.
    /// </remarks>
    [Route("api/v2/audit-trail/")]
    public class AuditTrailController : BaseController
    {
        private readonly IAuditTrailService _auditTrailService;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuditTrailController"/> class.
        /// </summary>
        /// <param name="configuration">Application configuration settings.</param>
        /// <param name="auditTrailService">Service responsible for audit trail operations.</param>
        public AuditTrailController(IConfiguration configuration, IAuditTrailService auditTrailService) : base(configuration)
        {
            _auditTrailService = auditTrailService;
        }

        /// <summary>
        /// Get configuration audit trail
        /// </summary>
        /// <param name="startTime">Ex.: 2018-11-13T04:04:28.9150085Z</param>
        /// <param name="endTime">Ex.: 2018-11-14T04:04:28.9150085Z</param>
        /// <returns>CSV with all changes made</returns>
        [HttpGet]
        [Route("configuration/download")]
        [ActionName("GetConfigurationAuditTrail")]
        public async Task<IActionResult> GetConfigurationAuditTrail([BindRequired] string startTime, [BindRequired]  string endTime)
        {
            if (!DateTimeOffset.TryParse(startTime, CultureInfo.InvariantCulture, out _) 
                || !DateTimeOffset.TryParse(endTime, CultureInfo.InvariantCulture, out _))
                return BadRequest("Invalid start or end date format.");

            var user = GetUserContext();
            var result = await _auditTrailService.GetConfigurationAuditTrailAsync(
                startTime: startTime.ParseISO8601(),
                endTime: endTime.ParseISO8601(),
                user
             );

            var contentDisposition = new ContentDisposition
            {
                FileName = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "-audit-Configuration.csv",
                Inline = false
            };

            Response.Headers.Append("Content-Disposition", contentDisposition.ToString());
            Response.Headers.Append("access-control-expose-headers", "content-disposition");

            Response.SetValidationHeader(_configuration.GetValue<bool>("Host:IsModuleValidated", false), null, null, null);

            return new FileContentResult(result.ToArray(), "text/csv");
        }

        /// <summary>
        /// Get security audit, capturing all logins or token requests
        /// </summary>
        /// <param name="startTime">Ex.: 2018-11-13T04:04:28.9150085Z</param>
        /// <param name="endTime">Ex.: 2018-11-14T04:04:28.9150085Z</param>
        /// <returns>CSV with all changes made</returns>
        [HttpGet]
        [Route("security/download")]
        [ActionName("GetSecurityAuditTrail")]
        public async Task<IActionResult> GetSecurityAuditTrail([BindRequired] string startTime, [BindRequired] string endTime)
        {

            if (!DateTimeOffset.TryParse(startTime, CultureInfo.InvariantCulture, out _)
                || !DateTimeOffset.TryParse(endTime, CultureInfo.InvariantCulture, out _))
                return BadRequest("Invalid start or end date format.");

            var user = GetUserContext();
            var result = await _auditTrailService.GetSecurityAuditTrailAsync(
                startTime: startTime.ParseISO8601(), 
                endTime: endTime.ParseISO8601(), 
                user
            );

            var contentDisposition = new ContentDisposition
            {
                FileName = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "-audit-Security.csv",
                Inline = false
            };

            Response.Headers.Append("Content-Disposition", contentDisposition.ToString());
            Response.Headers.Append("access-control-expose-headers", "content-disposition");

            Response.SetValidationHeader(_configuration.GetValue<bool>("Host:IsModuleValidated", false), null, null, null);

            return new FileContentResult(result.ToArray(), "text/csv");
        }

        /// <summary>
        /// Get the admin action audit containing all the actions
        /// </summary>
        /// <param name="startTime">Ex.: 2018-11-13T04:04:28.9150085Z</param>
        /// <param name="endTime">Ex.: 2018-11-14T04:04:28.9150085Z</param>
        /// <returns>CSV with all changes made</returns>
        [HttpGet]
        [Route("admin-actions/download")]
        [ActionName("GetAdminActionAuditTrail")]
        public async Task<IActionResult> GetAdminActionAuditTrail([BindRequired] string startTime, [BindRequired] string endTime)
        {


            if (!DateTimeOffset.TryParse(startTime, CultureInfo.InvariantCulture, out _)
                || !DateTimeOffset.TryParse(endTime, CultureInfo.InvariantCulture, out _))
                return BadRequest("Invalid start or end date format.");

            var user = GetUserContext();

            var result = await _auditTrailService.GetAdminActionsAuditTrailAsync(
                startTime: startTime.ParseISO8601(),
                endTime: endTime.ParseISO8601(),
                user);

            var contentDisposition = new ContentDisposition
            {
                FileName = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "-audit-Admin.csv",
                Inline = false
            };

            Response.Headers.Append("Content-Disposition", contentDisposition.ToString());
            Response.Headers.Append("access-control-expose-headers", "content-disposition");

            Response.SetValidationHeader(_configuration.GetValue<bool>("Host:IsModuleValidated", false), null, null, null);

            return new FileContentResult(result.ToArray(), "text/csv");
        }

        /// <summary>
        /// Creates a new admin action audit trail entry.
        /// </summary>
        /// <param name="model">The model containing the details of the admin action to log.</param>
        /// <returns>A response indicating the success or failure of the audit trail entry creation.</returns>
        /// <response code="200">The audit trail entry was successfully created.</response>
        [HttpPost]
        [Route("admin-actions/entry/create")]
        [ActionName("CreateAdminActionAuditTrail")]
        public async Task<IActionResult> CreateAdminActionAuditTrail([FromBody] AuditTrailAdminActionsEntryDTO model)
        {
            var user = GetUserContext();
            await _auditTrailService.CreateAdminActionAuditTrailEntryAsync(model, user);
            return Ok();
        }
    }
}
