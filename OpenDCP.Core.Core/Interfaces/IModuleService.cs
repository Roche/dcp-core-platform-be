using OpenDCP.Core.Models.Models.Module;
using OpenDCP.Framework.Hosting.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenDCP.Core.Core.Interfaces
{
    public interface IModuleService
    {
        public Task DeleteModuleAsync(int moduleId, string reason, UserContextDTO user);
        public Task<List<ModuleListItemDTO>> SearchModuleDetailsAsync(ModuleQueryDTO model, UserContextDTO user);
        public Task<ModuleConfigurationForm> GetSingleModuleAsync(int moduleId, UserContextDTO user);
        public Task<ModuleConfigurationForm> CreateModuleAsync(ModuleUserConfigurationDTO model, UserContextDTO user);
        public Task<ModuleConfigurationForm> UpdateModuleAsync(int moduleId, ModuleUserConfigurationDTO model, UserContextDTO user);
    }
}
