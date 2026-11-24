using OpenDCP.Core.Core.Interfaces;
using OpenDCP.Core.DAL;
using OpenDCP.Core.Models.Models.EventSubscription;
using OpenDCP.Framework.DataTypes.Equipment.Entities.EventframeContext;
using OpenDCP.Framework.Hosting.Models;
using OpenDCP.Framework.Security.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenDCP.Core.Core.Services
{
    public class EventSubscriptionService : BaseService, IEventSubscriptionService
    {
        public EventSubscriptionService(DCPCoreDbContext dbContext, IConfiguration configuration, ILoggerFactory factory, IMemoryCache memoryCache) : base(dbContext, configuration, factory, memoryCache)
        {
        }

        public async Task<List<InProgressEventDTO>> GetCachedBatchEventsAsync(EventSubscriptionQuery model, UserContextDTO user)
        {
            HasPermission(new List<string>
            {
                Permissions.ModuleIndependent.ADMIN,
                Permissions.ModuleIndependent.VIEWER
            }, user, 0, true);

            var query = _dbContext.InProgressEvent.AsQueryable();

            if(model.DeviceWebId.Count > 0)
            {
                query = query.Where(x => model.DeviceWebId.Contains(x.DeviceWebId));
            }

            if (model.EventframeId.Count > 0)
            {
                query = query.Where(x => model.EventframeId.Contains(x.EventId));
            }

            if (model.SiteId.Count  > 0)
            {
                query = query.Where(x => model.SiteId.Contains(x.Datasource!.SiteId));
            }

            var result = await query
                .Select(x => new InProgressEventDTO
                {
                    DeviceWebId = x.DeviceWebId,
                    SiteId = x.Datasource!.SiteId,
                    EventId = x.EventId,
                    Id = x.Id,
                    StartTime = x.EventStartTime,
                    JEventValues = x.JEventValues.Select(v => new EventFrameValueDTO
                    {
                        Good = v.Good,
                        Name = v.Name,
                        //Check if value type conversion can be and should be done
                        Value = v.Value,
                    }).ToList(),
                })
                .AsNoTracking()
                .ToListAsync();

            return result;
        }
    }
}
