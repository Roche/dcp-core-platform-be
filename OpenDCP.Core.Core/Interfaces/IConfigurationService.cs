using OpenDCP.Core.Models.Models.Configuration;
using OpenDCP.Core.Models.Models.ConnectionBridge;
using OpenDCP.Core.Models.Models.Site;
using OpenDCP.Framework.Configuration.Model.Constants.CalculationNode;
using OpenDCP.Framework.Connections.Manager.Constants;
using OpenDCP.Framework.FormBuilder.Models;
using OpenDCP.Framework.Hosting.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using ConnectionBridgeDTO = OpenDCP.Framework.Connections.Manager.Models.ConnectionBridgeDTO;

namespace OpenDCP.Core.Core.Interfaces
{
    public interface IConfigurationService
    {
        public Task<List<DatasourceConfigurationDTO>> SearchDatasourceConfigurationAsync(DatasourceQueryDTO model, UserContextDTO user);
        public Task<List<DatasourcePublicConfigurationDTO>> PublicSearchDatasourceConfigurationAsync(DatasourceQueryDTO model, UserContextDTO user);
        public Task<DatasourceConfigurationDTO> GetDatasourceConfigurationByIdAsync(int datasourceId, UserContextDTO user);
        public Task<List<ModuleConfigurationDTO>> SearchModuleConfigurationAsync(ModuleQueryDTO model, UserContextDTO user);
        public Task<List<ConnectionBridgeDTO>> SearchConnectionBridgeAsync(ConnectionBridgeQueryDTO model, UserContextDTO user);
        public Task<ModuleConfigurationDTO> GetModuleConfigurationByIdAsync(int moduleId, UserContextDTO user);
        public Task<SystemConfigurationDTO> GetActiveSystemConfigurationAsync(UserContextDTO user);
        public Task<List<InputField>> GetModuleConnectionStringConfigurationFormAsync(CalculationNodeType type, int? moduleId, UserContextDTO user);
        public Task<List<InputField>> GetDatasourceConnectionStringConfigurationFormAsync(string type, int? datasourceId, UserContextDTO user);
        public Task<List<InputField>> GetConnectionBridgeConfigurationFormAsync(ConnectionBridgeStrategyType type, int? connectionBridgeId, UserContextDTO user);
        public Task<List<ApiServiceConfigurationDTO>> GetApiGatewayStatusAsync(UserContextDTO user);
        public Task<List<ApiServiceConfigurationDTO>> SetApiGatewayStatusAsync(List<ApiServiceConfigurationDTO> model, UserContextDTO user, string reason);
        public Task<List<ApiServiceConfigurationDTO>> ReloadApiGatewayStatusAsync(List<ApiServiceConfigurationDTO> model, UserContextDTO user, string reason);

    }
}
