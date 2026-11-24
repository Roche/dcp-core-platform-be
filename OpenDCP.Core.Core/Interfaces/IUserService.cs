using OpenDCP.Core.Models.Models.Messages;
using OpenDCP.Core.Models.Models.User;
using OpenDCP.Framework.Hosting.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenDCP.Core.Core.Interfaces
{
    public interface IUserService
    {
        public Task<List<UserListItemDTO>> QueryUsersAsync(UserQueryDTO model, UserContextDTO user);
        public Task<List<UserListItemDTO>> GetAllUsersPerSiteAsync(int siteId, UserContextDTO user);
        public Task<UserDTO> GetUserInfoAsync(UserContextDTO user);
        public Task SetUserSiteAsync(int siteId, UserContextDTO user);
        public Task UpdateUserSettingsAsync(UserSettingsDTO model, UserContextDTO user);
        public Task SendUserFeedbackAsync(UserFeedbackDTO model, UserContextDTO user);

    }
}
