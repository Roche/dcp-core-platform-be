using OpenDCP.Core.Core.Interfaces;
using OpenDCP.Core.Core.Interfaces.Data;
using OpenDCP.Core.Models.Legacy.DataHandling;
using OpenDCP.Framework.Configuration.Model.Constants.Datasource;
using OpenDCP.Framework.DataTypes.Analytical.Requests.AnalyticalData;
using OpenDCP.Core.Core.Extensions;
using Microsoft.AspNetCore.Mvc;

#pragma warning disable CS1573
#pragma warning disable CS1572
#pragma warning disable CS1591

namespace OpenDCP.Core.API.Controllers.v1
{
    [Route("api/")]
    public class AnalyticalDataLegacyController : BaseController
    {
        private readonly ILegacyService _legacy;
        private readonly IAnalyticalDataService _analyticalData;
        public AnalyticalDataLegacyController(IConfiguration configuration, ILegacyService legacyService,
            IAnalyticalDataService analyticalDataService) : base(configuration)
        {
            _legacy = legacyService;
            _analyticalData = analyticalDataService;
        }

        [HttpPost]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("data/analytical/v1/GetAnalyticalDataByEventFrameIds")]
        public async Task<IActionResult> GetAnalyticalDataByEventFrameIds([FromBody] LegacyAnalyticalDataByIdsRequest legacyModel)
        {
            var user = GetUserContext();

            //Map to new model(s)
            var info = await _legacy.ReverseMapSiteconfigurationDTO(legacyModel.SiteConfiguration, DataType.AnalyticalData);
            var eventFrameDatasourceId = await _legacy.GetDatasourceId(info.siteId, info.module, DataType.EventframeContext);

            var model = new AttributeDataQueryDTO
            {
                EventId = legacyModel.EventFrameIds,
                AttributeId = legacyModel.AttributeIds
            };

            var visitedEncode = new HashSet<object>();
            _legacy.TransformObjectEncode(model, legacyModel.SiteConfiguration.DataSourceId, visitedEncode);
            visitedEncode = new HashSet<object>();
            _legacy.TranslateObjectEncode(model, eventFrameDatasourceId, visitedEncode);

            //Use new implementation
            var result = await _analyticalData.GetAttributeDataAsync(info.siteId, info.module, model, user);

            var visited = new HashSet<object>();
            _legacy.TransformObjectDecode(result, visited);

            Response.SetValidationHeader(
              _configuration.GetValue<bool>("Host:IsModuleValidated", false),
              result.DatasourceValidation,
              result.CalculationValidation,
              result.EntityValidation
            );

            return this.Ok(result.Data);
        }


        [HttpPost]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("data/analytical/v1/GetAvailableAnalyticalDataAttributes")]
        public async Task<IActionResult> GetAvailableAnalyticalDataAttributes([FromBody] LegacyAvailableAnalyticalDataAttributesRequest legacyModel)
        {
            var user = GetUserContext();

            //Map to new model(s)
            var info = await _legacy.ReverseMapSiteconfigurationDTO(legacyModel.SiteConfiguration, DataType.AnalyticalData);
            var webId = _legacy.TransformSingleWebIdEncode(legacyModel.DeviceWebId, legacyModel.SiteConfiguration.DataSourceId);

            //Use new implementation
            var result = await _analyticalData.GetAttributesAsync(info.siteId, info.module, webId, user);

            var visited = new HashSet<object>();
            _legacy.TransformObjectDecode(result, visited);

            Response.SetValidationHeader(
              _configuration.GetValue<bool>("Host:IsModuleValidated", false),
              result.DatasourceValidation,
              result.CalculationValidation,
              result.EntityValidation
            );

            return this.Ok(result.Data);
        }
    }
}
#pragma warning restore CS1591
#pragma warning restore CS1573
#pragma warning restore CS1572