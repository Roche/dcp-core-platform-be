using OpenDCP.Framework.Configuration.Model.Responses.Service;
using OpenDCP.Framework.DataTypes.Equipment.Requests.DataBundle;
using OpenDCP.Framework.DataTypes.Equipment.Responses.SensorData;
using System.Threading;
using System.Threading.Tasks;

namespace OpenDCP.Core.Core.Interfaces.Data
{
    public interface IBundleDataService
    {
        public Task<ServiceResponse<SensorDataWithBatchContextResponse>> GetPlotDataWithContextAsync(int siteId, string module, PlotDataWithBatchContextQueryDTO model, Framework.Hosting.Models.UserContextDTO user, CancellationToken ct = default);
        public Task<ServiceResponse<SensorDataWithBatchContextResponse>> GetInterpolatedDataWithContextAsync(int siteId, string module, InterpolatedDataWithBatchContextQueryDTO model, Framework.Hosting.Models.UserContextDTO user, CancellationToken ct = default);
    }
}
