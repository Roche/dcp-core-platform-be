using OpenDCP.Framework.Configuration.Model.Responses.Service;
using OpenDCP.Framework.DataTypes.Equipment.Requests.SensorData;
using OpenDCP.Framework.DataTypes.Equipment.Responses.SensorData;
using System.Threading;
using System.Threading.Tasks;

namespace OpenDCP.Core.Core.Interfaces.Data
{
    public interface ISensorTelemetryDataService
    {
        public Task<ServiceResponse<SensorDataResponse>> GetInterpolatedDataAsync(int siteId, string module, InterpolatedDataQueryDTO model, Framework.Hosting.Models.UserContextDTO user, CancellationToken ct = default);
        public Task<ServiceResponse<SensorDataResponse>> GetRecordedDataAsync(int siteId, string module, RecordedDataQueryDTO model, Framework.Hosting.Models.UserContextDTO user, CancellationToken ct = default);
        public Task<ServiceResponse<SensorDataResponse>> GetPlotDataAsync(int siteId, string module, PlotDataQueryDTO model, Framework.Hosting.Models.UserContextDTO user, CancellationToken ct = default);
        public Task<ServiceResponse<SensorDataResponse>> GetInterpolatedDataAtTimesAsync(int siteId, string module, InterpolatedAtTimeDataQueryDTO model, Framework.Hosting.Models.UserContextDTO user, CancellationToken ct = default);
        public Task<ServiceResponse<SensorDataResponse>> GetRecordedDataAtTimesAsync(int siteId, string module, RecordedAtTimeDataQueryDTO model, Framework.Hosting.Models.UserContextDTO user, CancellationToken ct = default);
    }
}
