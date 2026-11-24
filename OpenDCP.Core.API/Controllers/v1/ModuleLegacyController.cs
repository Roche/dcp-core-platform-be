using OpenDCP.Core.Core.Interfaces;
using OpenDCP.Core.Models.Models.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

#pragma warning disable CS1591
#pragma warning disable CS1572
#pragma warning disable CS1573

namespace OpenDCP.Core.API.Controllers.v1
{
    [Route("api/")]
    public class ModuleLegacyController : BaseController
    {
        private readonly IConfigurationService _configurationService;
        private readonly ILegacyService _legacy;
        public ModuleLegacyController(IConfiguration configuration, IConfigurationService configurationService, ILegacyService legacy) : base(configuration)
        {
            _configurationService = configurationService;
            _legacy = legacy;
        }

        /// <summary>
        /// Get module details
        /// </summary>
        /// <param name="siteId">Site id</param>
        /// <param name="moduleName">Module name</param>
        /// <returns>Module for given site</returns>
        [HttpGet]
        [ActionName("GetModuleDetails")]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/Module/GetModuleDetails")]
        [Route("Module/GetModuleDetails")]
        public async Task<IActionResult> GetModuleDetails(int siteId, string moduleName,
            [FromHeader(Name = "X-DCPMicroservice-Key")] string? microserviceKey)
        {
            ValidateAsInternalRequest(microserviceKey);

            var model = new ModuleQueryDTO
            {
                Module = moduleName,
                SiteId = new List<int> { siteId },
                IncludeDisabledModules = true
            };

            var user = GetUserContext();
            var result = await _configurationService.SearchModuleConfigurationAsync(model, user);

            return this.Ok(_legacy.MapModules(result).FirstOrDefault());
        }
    }
}

#pragma warning restore CS1591
#pragma warning restore CS1572
#pragma warning restore CS1573
