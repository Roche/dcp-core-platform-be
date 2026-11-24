using OpenDCP.Core.Core.Interfaces;
using OpenDCP.Core.Models.Legacy.Site;
using OpenDCP.Core.Models.Models.Configuration;
using Microsoft.AspNetCore.Mvc;

#pragma warning disable CS1591
#pragma warning disable CS1572
#pragma warning disable CS1573

namespace OpenDCP.Core.API.Controllers.v1
{
    [Route("api/")]
    public class DataSourceLegacyController : BaseController
    {
        private readonly IConfigurationService _configurationService;
        private readonly ILegacyService _legacy;
        public DataSourceLegacyController(IConfiguration configuration, IConfigurationService configurationService, ILegacyService legacy) : base(configuration)
        {
            _configurationService = configurationService;
            _legacy = legacy;
        }
        /// <summary>
        /// Get all datasources per site - currently used by Micro services
        /// </summary>
        /// <param name="siteId">Site id</param>
        /// <returns>Datasources details per site</returns>
        [HttpGet]
        [ActionName("GetAllDataSourcesPerSite")]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/DataSource/GetAllDataSourcesPerSite")]
        [Route("DataSource/GetAllDataSourcesPerSite")]
        public async Task<IActionResult> GetAllDataSourcesPerSite(int siteId, [FromHeader(Name = "X-DCPMicroservice-Key")] string? microserviceKey)
        {
            ValidateAsInternalRequest(microserviceKey);

            var user = GetUserContext();

            var model = new DatasourceQueryDTO
            {
                SiteId = new List<int> { siteId },
            };

            var result = await _configurationService.SearchDatasourceConfigurationAsync(model, user);

            return this.Ok(_legacy.MapDataSource(result, false));
        }
        /// <summary>
        /// Get all datasources per ids and site - currently used by Micro services
        /// </summary>
        /// <param name="siteId">Site id</param>
        /// <returns>Datasources details per site</returns>
        [HttpPost]
        [ActionName("DataSourcesDetails")]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/DataSource/DataSourcesDetails")]
        [Route("DataSource/DataSourcesDetails")]
        public async Task<IActionResult> DataSourcesDetails([FromBody] LegacyDataSourcesDetailsRequest legacyModel, 
            [FromHeader(Name = "X-DCPMicroservice-Key")] string? microserviceKey)
        {
            ValidateAsInternalRequest(microserviceKey);

            var user = GetUserContext();

            var model = new DatasourceQueryDTO
            {
                SiteId = new List<int> { legacyModel.SiteId },
            };

            var result = await _configurationService.SearchDatasourceConfigurationAsync(model, user);

            return this.Ok(_legacy.MapDataSource(result.Where(x => legacyModel.DatasourceIds.Contains(x.Id)).ToList(), true));
        }
    }
}

#pragma warning restore CS1591
#pragma warning restore CS1572
#pragma warning restore CS1573