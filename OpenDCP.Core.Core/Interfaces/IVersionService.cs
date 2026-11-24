using OpenDCP.Core.Models.Models.CalculationNode;
using OpenDCP.Framework.Connections.Manager.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenDCP.Core.Core.Interfaces
{
    public interface IVersionService
    {
        Task<List<SiteCalculationNodeInfoDTO>> GetCalculationNodeVersionsAsync();
        Task<List<RegisteredDataInterfaceDTO>> GetRegisteredDatasourcesAsync();
    }
}
