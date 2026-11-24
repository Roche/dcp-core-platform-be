using OpenDCP.Core.Core.Interfaces;
using OpenDCP.Core.Core.Interfaces.Data;
using OpenDCP.Framework.Configuration.Model.Constants;
using OpenDCP.Framework.Configuration.Model.Constants.Datasource;
using OpenDCP.Framework.DataTypes.Equipment.Requests.AssetHierarchy;
using Microsoft.AspNetCore.Mvc;
using OpenDCP.Core.Core.Extensions;

#pragma warning disable CS1591
#pragma warning disable CS1572
#pragma warning disable CS1573

namespace OpenDCP.Core.API.Controllers.v1
{
    [Route("api/")]
    public class GlobalFilterLegacyController : BaseController
    {
        private readonly IAssetHierarchyDataService _dataService;
        private readonly ILegacyService _legacy;
        public GlobalFilterLegacyController(IConfiguration configuration, IAssetHierarchyDataService dataService, ILegacyService legacyService) : base(configuration)
        {
            _dataService = dataService;
            _legacy = legacyService;
        }

        /// <summary>
        /// Build dynamic structure - site, module, device, sensor , etc.
        /// </summary>
        /// <param name="model">if null or empty - return parent, to get child - provide value from link fields (from response).If site is selected from context, pass WebId of the site</param>
        /// <returns></returns>
        [HttpPost]
        [ActionName("GlobalFilter")]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/GlobalFilter/GlobalFilter")]
        [Route("GlobalFilter/GlobalFilter")]
        public async Task<IActionResult> GlobalFilter([FromBody] GlobalFilterRequest legacyModel)
        {
            var user = GetUserContext();

            //Map to new model(s)
            var info = await _legacy.ReverseMapSiteconfigurationDTO(legacyModel.SiteConfiguration, DataType.AssetHierarchy);

            string filterType = string.Empty;
            if(legacyModel.SiteConfiguration.ConnectionType == (byte)ConnectionType.OSISoftPI)
            {
                if(legacyModel.SiteConfiguration.ConnectionString["OSISoftPI"]?.ToString() != null)
                {
                    filterType = legacyModel.SiteConfiguration.ConnectionString["OSISoftPI"]!["SensorFilterType"]!.ToString();
                }
                else
                {
                    filterType = legacyModel.SiteConfiguration.ConnectionString["SensorFilterType"]!.ToString();
                }
            }

            var link = await _legacy.TransformLinkEncode(info.module, legacyModel.Link, legacyModel.SiteConfiguration.DataSourceId, filterType);

            var result = await _dataService.GetGlobalFilterAsync(info.siteId, info.module, link, user);

            await _legacy.TransformElementBack(result.Data.Elements, legacyModel.SiteConfiguration, link);

            Response.SetValidationHeader(
              _configuration.GetValue<bool>("Host:IsModuleValidated", false),
              result.DatasourceValidation,
              result.CalculationValidation,
              result.EntityValidation
            );

            return this.Ok(_legacy.MapGlobalFilter(result.Data));

        }
        /// <summary>
        /// Returns configuration and meta data for the specific element
        /// </summary>
        /// <param name="model">Element web id is required</param>
        /// <returns>Json array containing Name, Description and Value for specific element</returns>
        [HttpPost]
        [ActionName("ElementMetaData")]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/GlobalFilter/ElementMetaData")]
        [Route("GlobalFilter/ElementMetaData")]
        public async Task<IActionResult> ElementMetaData([FromBody] ElementMetaDataRequest legacyModel)
        {

            var user = GetUserContext();

            //Map to new model(s)
            var info = await _legacy.ReverseMapSiteconfigurationDTO(legacyModel.SiteConfiguration, DataType.AssetHierarchy);

            var webId = _legacy.TransformSingleWebIdEncode(legacyModel.ElementWebId, legacyModel.SiteConfiguration.DataSourceId);

            var result = await _dataService.GetElementMetaDataAsync(info.siteId, info.module, webId, user);

            Response.SetValidationHeader(
              _configuration.GetValue<bool>("Host:IsModuleValidated", false),
              result.DatasourceValidation,
              result.CalculationValidation,
              result.EntityValidation
            );

            return this.Ok(_legacy.MapElementMetaData(result.Data));
        }
    }
}

#pragma warning restore CS1591
#pragma warning restore CS1572
#pragma warning restore CS1573
