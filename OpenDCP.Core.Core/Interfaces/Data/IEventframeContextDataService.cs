using OpenDCP.Framework.Configuration.Model.Responses.Service;
using OpenDCP.Framework.DataTypes.Equipment.Requests.EventFrameContext;
using OpenDCP.Framework.DataTypes.Equipment.Responses.EventFrameContext;
using System.Threading;
using System.Threading.Tasks;

namespace OpenDCP.Core.Core.Interfaces.Data
{
    public interface IEventframeContextDataService
    {
        public Task<ServiceResponse<BatchContextListResponse>> SearchBatchContextByTimeAsync(int siteId, string module, BatchContextTimeQueryDTO model, Framework.Hosting.Models.UserContextDTO user, CancellationToken ct = default);
        public Task<ServiceResponse<EventframeListResponse>> SearchEventframeContextByTimeAsync(int siteId, string module, EventframeContextTimeQueryDTO model, Framework.Hosting.Models.UserContextDTO user, CancellationToken ct = default);
        public Task<ServiceResponse<EventframeListResponse>> SearchEventframeContextByIdAsync(int siteId, string module, EventframeContextIdQueryDTO model, Framework.Hosting.Models.UserContextDTO user, CancellationToken ct = default);
        public Task<ServiceResponse<EventFrameResponse>> GetEventframeAsync(int siteId, string module, string id, Framework.Hosting.Models.UserContextDTO user, CancellationToken ct = default);
        public Task<ServiceResponse<EventframeValuesResponse>> GetEventframeValuesAsync(int siteId, string module, string id, Framework.Hosting.Models.UserContextDTO user, CancellationToken ct = default);
        public Task<ServiceResponse<EventframeAttributesResponse>> GetEventframeAttributesAsync(int siteId, string module, string deviceWebId, Framework.Hosting.Models.UserContextDTO user, CancellationToken ct = default);
    }
}
