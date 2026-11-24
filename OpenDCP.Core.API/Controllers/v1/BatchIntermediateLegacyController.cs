using OpenDCP.Core.Core.Interfaces;
using OpenDCP.Core.Models.Legacy.BatchIntermediate;
using OpenDCP.Core.Models.Models.EventSubscription;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

#pragma warning disable CS1591
#pragma warning disable CS1572
#pragma warning disable CS1573

namespace OpenDCP.Core.API.Controllers.v1
{
    [Route("api/")]
    public class BatchIntermediateLegacyController : BaseController
    {
        private readonly ILegacyService _legacy;
        public BatchIntermediateLegacyController(IConfiguration configuration, ILegacyService legacyService) : base(configuration)
        {
            _legacy = legacyService;
        }


        /// <summary>
        /// Return details for single BatchIntermediate entity matching conditions - currently used by Microservices
        /// </summary>
        /// <param name="model">Model that hold settings</param>
        /// DeviceWebId, EventFrameId, BatchStartTime and BatchID can be used as where clauses
        /// <returns>Status code></returns>
        [HttpPost]
        [ActionName("BatchIntermediateDetails")]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/BatchIntermediate/BatchIntermediateDetails")]
        [Route("BatchIntermediate/BatchIntermediateDetails")]
        public async Task<IActionResult> BatchIntermediateDetails([FromBody] LegacyBatchIntermediateDTO requestModel)
        {
            var user = GetUserContext();

            var model = new EventSubscriptionQuery
            {
                SiteId = new List<int> { requestModel.SiteId }
            };

            if (requestModel.DeviceWebId != null)
            {
                model.DeviceWebId.Add(requestModel.DeviceWebId);
            }

            if (requestModel.EventFrameId != null)
            {
                model.EventframeId.Add(requestModel.EventFrameId);
            }

            //Use new implementation
            var result = await _legacy.GetCachedBatchEventsAsync(model, user);

            return this.Ok(_legacy.MapSingleBatchIntermediate(result));
        }
        /// <summary>
        /// Return details for all BatchIntermediate entity matching conditions - currently used by Microservices
        /// </summary>
        /// <param name="model">Model that hold settings</param>
        /// DeviceWebId, EventFrameId, BatchStartTime and BatchID can be used as where clauses
        /// <returns>Status code></returns>
        [HttpPost]
        [ActionName("BatchIntermediatesDetails")]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/BatchIntermediate/BatchIntermediatesDetails")]
        [Route("BatchIntermediate/BatchIntermediatesDetails")]
        public async Task<IActionResult> BatchIntermediatesDetails([FromBody] List<LegacyBatchIntermediateDTO> requestModel)
        {
            var user = GetUserContext();

            var model = new EventSubscriptionQuery
            {
                SiteId = requestModel.Select(x => x.SiteId).ToList()
            };

            var deviceWebIds = requestModel
                .Where(x => x.DeviceWebId != null)
                .Select(x => x.DeviceWebId!)
                .Distinct()
                .ToList();

            if(deviceWebIds.Count > 0)
            {
                model.DeviceWebId = deviceWebIds;
            }

            var eventIds = requestModel
                .Where(x => x.EventFrameId != null)
                .Select(x => x.EventFrameId!)
                .Distinct()
                .ToList();

            if (eventIds.Count > 0)
            {
                model.DeviceWebId = eventIds;
            }

            //Use new implementation
            var result = await _legacy.GetCachedBatchEventsAsync(model, user);

            return this.Ok(_legacy.MapBatchIntermediate(result));
        }
        /// <summary>
        /// Update all existing BatchIntermediate entities per device web id - currently used by Microservices
        /// </summary>
        /// <param name="model">Model that hold settings</param>
        /// <returns>Status code></returns>
        [HttpPut]
        [ActionName("UpdateNewSignUpCreatedColumnByDeviceWebIds")]
        [Obsolete("Migrate to the endpoints in the core-v2 scope")]
        [Route("v1/BatchIntermediate/UpdateNewSignUpCreatedColumnByDeviceWebIds")]
        [Route("BatchIntermediate/UpdateNewSignUpCreatedColumnByDeviceWebIds")]
        public IActionResult UpdateNewSignUpCreatedColumnByDeviceWebId([FromBody] List<LegacyBatchIntermediateDTO> model)
        {
            return this.Ok();
        }
    }
}

#pragma warning restore CS1591
#pragma warning restore CS1572
#pragma warning restore CS1573