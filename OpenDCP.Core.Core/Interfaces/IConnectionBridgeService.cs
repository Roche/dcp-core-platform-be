using OpenDCP.Core.Models.Models.Configuration;
using OpenDCP.Core.Models.Models.ConnectionBridge;
using OpenDCP.Framework.Configuration.Model.Responses.Service;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace OpenDCP.Core.Core.Interfaces
{
    public interface IConnectionBridgeService
    {
        Task<ServiceResponse<TranslationResponse>> TranslateIdentifersAsync(TranslationQueryDTO model, int siteId);
        Task<ServiceResponse<TranslationResponse>> TranslateEventsAsync(TranslationQueryDTO model, int siteId);
        Task<List<ConnectionBridgeConfigurationDTO>> SearchConnectionBridgesAsync(ConnectionBridgeQueryDTO model);
    }
}
