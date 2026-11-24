using OpenDCP.Core.Models.Constants;
using OpenDCP.Core.Models.Models.UserClient;
using OpenDCP.Framework.Hosting.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenDCP.Core.Core.Interfaces
{
    public interface IUserClientService
    {
        public Task<List<UserClientConfigurationDTO>> GetAllUserClientsAsync(UserContextDTO user);
        public Task<UserClientConfigurationDTO> GetUserClientAsync(int userClientId, UserContextDTO user);
        public Task SetUserClientStatusAsync(int userClientId, UserClientStatus status, UserContextDTO user);
        public Task<UserClientConfigurationDTO> CreateUserClientAsync(UserClientUserConfigurationDTO model, UserContextDTO user);
        public Task<UserClientConfigurationDTO> UpdateUserClientAsync(UserClientUserConfigurationDTO model, UserContextDTO user);
        public Task DeleteUserClientAsync(int userClientId, UserContextDTO user);
    }
}
