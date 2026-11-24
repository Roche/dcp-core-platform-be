using OpenDCP.Framework.Configuration.Model.Responses.Service;
using OpenDCP.Framework.DataTypes.Analytical.Requests.AnalyticalData;
using OpenDCP.Framework.DataTypes.Analytical.Responses.AnalyticalData;
using System.Threading;
using System.Threading.Tasks;

namespace OpenDCP.Core.Core.Interfaces.Data
{
    public interface IAnalyticalDataService
    {
        public Task<ServiceResponse<AttributeListResponse>> GetAttributesAsync(int siteId, string module, string webId, Framework.Hosting.Models.UserContextDTO user, CancellationToken ct = default);
        public Task<ServiceResponse<AttributeDataResponse>> GetAttributeDataAsync(int siteId, string module, AttributeDataQueryDTO model, Framework.Hosting.Models.UserContextDTO user, CancellationToken ct = default);
    }
}
