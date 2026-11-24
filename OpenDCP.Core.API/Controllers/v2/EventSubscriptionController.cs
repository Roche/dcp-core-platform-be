using OpenDCP.Core.Core.Interfaces;
using OpenDCP.Core.Models.Models.EventSubscription;
using Microsoft.AspNetCore.Mvc;

namespace OpenDCP.Core.API.Controllers.v2
{
    /// <summary>
    /// Provides endpoints for querying event subscription data related to batch event processing.
    /// </summary>
    /// <remarks>
    /// This controller exposes internal endpoints intended for server-to-server communication only
    /// and is not designed for direct consumption by frontend clients.
    /// </remarks>
    [Route("api/v2/event-subscription/")]
    public class EventSubscriptionController : BaseController
    {
        private readonly IEventSubscriptionService _eventSubscriptionService;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventSubscriptionController"/>.
        /// </summary>
        /// <param name="configuration">Application configuration instance.</param>
        /// <param name="eventSubscriptionService">
        /// Service responsible for retrieving event subscription and batch processing data.
        /// </param>
        public EventSubscriptionController(
            IConfiguration configuration,
            IEventSubscriptionService eventSubscriptionService)
            : base(configuration)
        {
            _eventSubscriptionService = eventSubscriptionService;
        }

        /// <summary>
        /// Retrieves cached in-progress batch events based on the specified query criteria.
        /// </summary>
        /// <param name="model">
        /// An <see cref="EventSubscriptionQuery"/> instance defining the filtering criteria
        /// for in-progress batch events.
        /// </param>
        /// <returns>
        /// An <see cref="IActionResult"/> containing a collection of
        /// <see cref="InProgressEventDTO"/> objects.
        /// </returns>
        /// <response code="200">
        /// Returns the list of in-progress batch events matching the query criteria.
        /// </response>
        /// <response code="400">
        /// The provided query parameters are invalid.
        /// </response>
        /// <response code="500">
        /// An unexpected server error occurred.
        /// </response>
        /// <remarks>
        /// This endpoint retrieves cached data for in-progress batch events and avoids direct
        /// database access in order to support separation of services and improve performance.
        /// <para>
        /// The endpoint is intended exclusively for server-to-server communication and should
        /// not be called directly by frontend applications.
        /// </para>
        /// </remarks>
        [HttpGet]
        [Route("query")]
        [Produces(typeof(List<InProgressEventDTO>))]
        public async Task<IActionResult> GetCachedBatchEvents(EventSubscriptionQuery model)
        {
            var user = GetUserContext();
            var result = await _eventSubscriptionService.GetCachedBatchEventsAsync(model, user);
            return Ok(result);
        }
    }
}
