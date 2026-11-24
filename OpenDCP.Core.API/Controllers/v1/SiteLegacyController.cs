using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenDCP.Core.Core.Interfaces;
using OpenDCP.Core.Models.Legacy.Site;
using OpenDCP.Core.Models.Models.Configuration;
using OpenDCP.Framework.Exceptions;
using DataType = OpenDCP.Framework.Configuration.Model.Constants.Datasource.DataType;

#pragma warning disable CS1591
#pragma warning disable CS1572
#pragma warning disable CS1573

namespace OpenDCP.Core.API.Controllers.v1
{
    [Route("api/")]
    public class ConfigurationLegacyController : BaseController
    {
        private readonly IConfigurationService _configurationService;
        private readonly ISiteService _siteService;
        private readonly ILegacyService _legacy;
        public ConfigurationLegacyController(IConfiguration configuration, IConfigurationService configurationService, ISiteService siteService, ILegacyService legacy) : base(configuration)
        {
            _siteService = siteService;
            _configurationService = configurationService;
            _legacy = legacy;
        }

        /// <summary>
        /// Get all sites
        /// </summary>
        /// <returns>List ValueTextViewModel></returns>
        [HttpGet]
        [ActionName("GetAllSites")]
        [Produces(typeof(List<LegacySiteInfoDTO>))]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/Site/GetAllSites")]
        [Route("Site/GetAllSites")]
        public async Task<IActionResult> GetAllSites()
        {
            var user = GetUserContext();

            var siteResult = await _siteService.GetAllSitesAsync(user);

            var siteIds = siteResult.Select(x => x.Id).ToList();

            var dataSourceSearchModel = new Models.Models.Configuration.DatasourceQueryDTO
            {
                SiteId = siteIds,
            };

            var dataSourceDetails = await _configurationService.PublicSearchDatasourceConfigurationAsync(dataSourceSearchModel, user);

            var moduleSearchModel = new ModuleQueryDTO
            {
                SiteId = siteIds,
                IncludeDisabledModules = true,
            };

            var moduleDetails = await _configurationService.SearchModuleConfigurationAsync(moduleSearchModel, user);


            return this.Ok(_legacy.MapAllSites(siteResult, dataSourceDetails, moduleDetails));
        }

        /// <summary>
        /// Get all module configuration items
        /// </summary>
        /// <returns>All configuration items as JSON</returns>
        [HttpGet]
        [ActionName("GetModuleConfigurationItems")]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/Module/GetModuleConfigurationItems")]
        [Route("Module/GetModuleConfigurationItems")]
        public async Task<IActionResult> GetModuleConfigurationItems()
        {
            return Ok(await _legacy.GetModuleConfigurationItems());
        }
        /// <summary>
        /// Get site configuration for current logged user - currently used by Micro services
        /// </summary>
        /// <param name="legacyModel">All except Module parameters are required</param>
        /// <returns>Site connection details></returns>
        [HttpPost]
        [ActionName("SiteForCurrentUserConfigurations")]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/Site/SiteForCurrentUserConfigurations")]
        [Route("Site/SiteForCurrentUserConfigurations")]

        public async Task<IActionResult> SiteForCurrentUserConfigurations([FromBody] LegacySiteConfigurationsRequest legacyModel)
        {
            var user = GetUserContext();

            var dataSourceSearchModel = new Models.Models.Configuration.DatasourceQueryDTO
            {
                DataType = new List<DataType> { (DataType)legacyModel.DataType! },
                Module = legacyModel.Module
            };

            //EquipmentData is split
            dataSourceSearchModel.DataType = dataSourceSearchModel.DataType
                .Select(x => x == DataType.EquipmentData ? DataType.AssetHierarchy : x)
                .ToList();

            if (legacyModel.SiteId.HasValue && legacyModel.SiteId > 0)
            {
                dataSourceSearchModel.SiteId.Add(legacyModel.SiteId.Value);
            }
            else
            {
                var site = await _legacy.GetUserCurrentSiteAsync(user, false, legacyModel.CurrentUser);

                if (site is null)
                    throw new DCPBadRequestException("Session Expired");

                dataSourceSearchModel.SiteId.Add(site.SiteId);
            }

            var dataSourceDetails = await _configurationService.SearchDatasourceConfigurationAsync(dataSourceSearchModel, user);

            var siteIds = dataSourceDetails.Count > 0
                ? dataSourceDetails.Select(d => d.SiteId).Distinct().ToList()
                : dataSourceSearchModel.SiteId ?? new List<int>();

            var moduleSearchModel = new ModuleQueryDTO
            {
                Module = legacyModel.Module,
                SiteId = siteIds
            };

            var moduleDetails = await _configurationService.SearchModuleConfigurationAsync(moduleSearchModel, user);

            var siteDetails = siteIds.Count > 0
                ? await _siteService.GetSingleSiteLegacyAsync(siteIds[0])
                : throw new Exception("Missing site Id");

            return Ok(_legacy.MapSiteDetails(siteDetails, dataSourceDetails, moduleDetails));
        }
        /// <summary>
        /// Get site configuration per site id - currently used by Micro services
        /// </summary>
        /// <param name="model">All except Module parameters are required</param>
        /// <returns>Site connection details></returns>
        [HttpPost]
        [ActionName("SiteConfigurations")]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/Site/SiteConfigurations")]
        [Route("Site/SiteConfigurations")]
        public async Task<IActionResult> SiteConfigurations([FromBody] LegacySiteConfigurationsRequest legacyModel)
        {
            var user = GetUserContext();

            var dataSourceSearchModel = new Models.Models.Configuration.DatasourceQueryDTO
            {
                DataType = new List<DataType> { (DataType)legacyModel.DataType! },
                Module = legacyModel.Module
            };

            //EquipmentData is split
            dataSourceSearchModel.DataType = dataSourceSearchModel.DataType
                .Select(x => x == DataType.EquipmentData ? DataType.AssetHierarchy : x)
                .ToList();

            if (legacyModel.SiteId.HasValue)
            {
                dataSourceSearchModel.SiteId.Add(legacyModel.SiteId.Value);
            }

            var dataSourceDetails = await _configurationService.SearchDatasourceConfigurationAsync(dataSourceSearchModel, user);

            var siteIds = dataSourceDetails.Count > 0
                ? dataSourceDetails.Select(d => d.SiteId).Distinct().ToList()
                : dataSourceSearchModel.SiteId ?? new List<int>();

            var moduleSearchModel = new ModuleQueryDTO
            {
                Module = legacyModel.Module,
                SiteId = siteIds
            };

            var moduleDetails = await _configurationService.SearchModuleConfigurationAsync(moduleSearchModel, user);

            var siteDetails = siteIds.Count > 0
                ? await _siteService.GetSingleSiteLegacyAsync(siteIds[0])
                : throw new Exception("Missing site Id");

            return Ok(_legacy.MapSiteDetails(siteDetails, dataSourceDetails, moduleDetails));
        }
        /// <summary>
        /// Get site configuration per site id - currently used by Micro services
        /// </summary>
        /// <param name="model">All except Module parameters are required</param>
        /// <returns>Site connection details></returns>
        [HttpPost]
        [ActionName("AllSiteConfigurations")]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/Site/AllSiteConfigurations")]
        [Route("Site/AllSiteConfigurations")]
        public async Task<IActionResult> AllSiteConfigurations([FromBody] LegacySiteConfigurationsRequest legacyModel)
        {
            var user = GetUserContext();

            var dataSourceSearchModel = new Models.Models.Configuration.DatasourceQueryDTO
            {
                SiteId = legacyModel.SiteIds,
                DataType = new List<DataType> { (DataType)legacyModel.DataType! },
                Module = legacyModel.Module
            };

            //EquipmentData is split
            dataSourceSearchModel.DataType = dataSourceSearchModel.DataType
                .Select(x => x == DataType.EquipmentData ? DataType.AssetHierarchy : x)
                .ToList();

            var dataSourceDetails = await _configurationService.SearchDatasourceConfigurationAsync(dataSourceSearchModel, user);

            var moduleSearchModel = new ModuleQueryDTO
            {
                Module = legacyModel.Module,
                SiteId = legacyModel.SiteIds
            };

            var moduleDetails = await _configurationService.SearchModuleConfigurationAsync(moduleSearchModel, user);

            var sitesDetails = await _siteService.GetAllSitesAsync(user);

            return this.Ok(_legacy.MapSitesDetails(sitesDetails.Where(x => legacyModel.SiteIds.Contains(x.Id)).ToList(), dataSourceDetails, moduleDetails));
        }
        /// <summary>
        /// Get all site with active module
        /// </summary>
        /// <param name="moduleName">Module name</param>
        /// <returns>Site details></returns>
        [HttpGet]
        [ActionName("GetAllSiteWithActiveModule")]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/Site/GetAllSiteWithActiveModule")]
        [Route("Site/GetAllSiteWithActiveModule")]
        public async Task<IActionResult> GetAllSiteWithActiveModule(string moduleName)
        {
            var user = GetUserContext();

            var moduleSearchModel = new ModuleQueryDTO
            {
                Module = moduleName,
            };

            var moduleDetails = await _configurationService.SearchModuleConfigurationAsync(moduleSearchModel, user);

            var sitesDetails = await _siteService.GetAllSitesAsync(user);

            return this.Ok(_legacy.MapAllSiteWithActiveModule(sitesDetails.Where(x => moduleDetails.Select(x => x.SiteId).Contains(x.Id)).ToList()));
        }
        /// <summary>
        /// Get all site base on site ids
        /// </summary>
        /// <param name="siteIds">Site ids</param>
        /// <response code="200">The list of sites was successfully retrieved></response>
        [HttpGet]
        [ActionName("SearchPublicSiteDetails")]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/Site/public/query")]
        [Route("Site/public/query")]
        [AllowAnonymous]
        public async Task<IActionResult> SearchPublicSiteDetails([FromQuery] List<int> siteIds)
        {
            var model = new Models.Models.Site.SiteQueryDTO
            {
                SiteId = siteIds
            };

            var result = await _siteService.SearchPublicSiteDetailsAsync(model);
            return Ok(result);
        }

        /// <summary>
        /// Get single site
        /// </summary>
        /// <param name="id">Site id</param>
        /// <response code="200">Site details>></response>
        [HttpGet]
        [ActionName("GetSingleSite")]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/Site/GetSingleSite")]
        [Route("Site/GetSingleSite")]
        public async Task<IActionResult> GetSingleSite([FromQuery] int id)
        {
            var user = GetUserContext();

            var dataSourceSearchModel = new Models.Models.Configuration.DatasourceQueryDTO
            {
                SiteId = new List<int> { id }
            };

            var dataSourceDetails = await _configurationService.SearchDatasourceConfigurationAsync(dataSourceSearchModel, user);

            var moduleSearchModel = new ModuleQueryDTO
            {
                SiteId = new List<int> { id }
            };

            var moduleDetails = await _configurationService.SearchModuleConfigurationAsync(moduleSearchModel, user);

            var siteDetails = await _siteService.GetSingleSiteLegacyAsync(dataSourceDetails.Select(d => d.SiteId).First());

            return this.Ok(_legacy.MapGetSingleSite(siteDetails, dataSourceDetails, moduleDetails));
        }
    }
}

#pragma warning restore CS1591
#pragma warning restore CS1572
#pragma warning restore CS1573