using OpenDCP.Core.Models.Models.EventSubscription;
using OpenDCP.Framework.Hosting.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenDCP.Core.Core.Interfaces
{
    public interface IEventSubscriptionService
    {
        Task<List<InProgressEventDTO>> GetCachedBatchEventsAsync(EventSubscriptionQuery model, UserContextDTO user);
    }
}
