using OpenDCP.Core.Models.Legacy.Site;
using OpenDCP.Framework.Hosting.Models;
using OpenDCP.Framework.ShareLink.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenDCP.Core.Core.Interfaces
{
    public interface IShareLinkService
    {
        Task<ShareLinkDTO> CreateUserShareLinkAsync(ShareLinkConfigurationDTO model, UserContextDTO user);
        Task<ShareLinkDTO> UpdateUserShareLinkAsync(ShareLinkConfigurationUpdateDTO model, Guid guid, UserContextDTO user);
        Task<List<ShareLinkDTO>> CreateSystemShareLinkAsync(List<ShareLinkConfigurationDTO> model, UserContextDTO user);
        Task<ShareLinkAliasDTO> CreateShareLinkAliasAsync(ShareLinkAliasConfigurationDTO model, UserContextDTO user);
        Task<ShareLinkAliasDTO> UpdateShareLinkAliasAsync(ShareLinkAliasConfigurationDTO model, int id, UserContextDTO user);
        Task<List<ShareLinkListItemDTO>> GetUserShareLinksAsync(int siteId, UserContextDTO user);
        Task<ShareLinkListItemDTO> SearchAliasAsync(string alias, UserContextDTO user, int? siteId = null);
        Task<List<ShareLinkDTO>> SearchShareLinkAsync(ShareLinkQueryDTO model, UserContextDTO user);
        Task<List<ShareLinkAliasDTO>> SearchLinkAliasAsync(List<int?>? siteIds, List<int?>? ownerIds, UserContextDTO user);
        Task DeleteShareLinkAsync(Guid guid, UserContextDTO user);
        Task DeleteShareLinkAliasAsync(int id, Guid guid, UserContextDTO user);
        [Obsolete]
        Task<List<LegacyShareLinkListItemDTO>> GetLegacyUserShareLinksAsync(int siteId, UserContextDTO user);
    }
}
