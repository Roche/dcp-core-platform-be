using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

#pragma warning disable CS1591

namespace OpenDCP.Core.API.Controllers.v1
{
    [Route("api/")]
    public class AuditTrailLegacyController : BaseController
    {
        public AuditTrailLegacyController(IConfiguration configuration) : base(configuration)
        {
        }

        /// <summary>
        /// Get configuration audit
        /// </summary>
        /// <param name="startTime">Ex.: 2018-11-13T04:04:28.9150085Z</param>
        /// <param name="endTime">Ex.: 2018-11-14T04:04:28.9150085Z</param>
        /// <returns>CSV with all changes made</returns>
        [HttpGet]
        [Route("AuditTrail/GetConfigurationAudit")]
        [Route("v1/AuditTrail/GetConfigurationAudit")]
        [ActionName("GetConfigurationAudit")]
        [Obsolete("This route has been moved use: /api/v2/auditTrail/configuration/download directly")]
        public IActionResult GetConfigurationAuditTrail([BindRequired] string startTime, [BindRequired] string endTime)
        {
            var queryString = HttpContext.Request.QueryString.Value;
            return Redirect($"/api/v2/audit-trail/configuration/download{queryString}");
        }

        /// <summary>
        /// Get configuration audit
        /// </summary>
        /// <param name="startTime">Ex.: 2018-11-13T04:04:28.9150085Z</param>
        /// <param name="endTime">Ex.: 2018-11-14T04:04:28.9150085Z</param>
        /// <returns>CSV with all changes made</returns>
        [HttpGet]
        [Route("AuditTrail/GetSecurityAudit")]
        [Route("v1/AuditTrail/GetSecurityAudit")]
        [ActionName("GetSecurityAudit")]
        [Obsolete("This route has been moved use: /api/v2/auditTrail/configuration/download directly")]
        public IActionResult GetSecurityAuditTrail([BindRequired] string startTime, [BindRequired] string endTime)
        {
            var queryString = HttpContext.Request.QueryString.Value;
            return Redirect($"/api/v2/audit-trail/security/download{queryString}");
        }

        /// <summary>
        /// Get configuration audit
        /// </summary>
        /// <param name="startTime">Ex.: 2018-11-13T04:04:28.9150085Z</param>
        /// <param name="endTime">Ex.: 2018-11-14T04:04:28.9150085Z</param>
        /// <returns>CSV with all changes made</returns>
        [HttpGet]
        [Route("AuditTrail/GetAdminActionsAudit")]
        [Route("v1/AuditTrail/GetAdminActionsAudit")]
        [ActionName("GetAdminActionsAudit")]
        [Obsolete("This route has been moved use: /api/v2/auditTrail/configuration/download directly")]
        public IActionResult GetAdminActionAuditTrail(string startTime, string endTime)
        {
            var queryString = HttpContext.Request.QueryString.Value;
            return Redirect($"/api/v2/audit-trail/configuration/download{queryString}");
        }
    }
}

#pragma warning restore CS1591