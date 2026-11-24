using OpenDCP.Core.Models.Models.CalculationNode;
using OpenDCP.Framework.CalculationNode.Interfaces;
using System.Threading.Tasks;

namespace OpenDCP.Core.Core.Interfaces
{
    public interface ICalculationNode: IBaseCalculationNode
    {
        Task<CalculationNodeVersionResponse> GetCalculationNodeVersionAsync();
        bool IsConfigured();
    }
}
