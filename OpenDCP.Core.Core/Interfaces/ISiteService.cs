using OpenDCP.Core.Models.Models.Module;
using OpenDCP.Core.Models.Models.Site;
using OpenDCP.Framework.AuditTrail.Models;
using OpenDCP.Framework.Hosting.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenDCP.Core.Core.Interfaces
{
    public interface ISiteService
    {
        public Task DeleteSiteAsync(int siteId, string reason, UserContextDTO user);
        public Task DeleteSiteRelatedEntitesAsync(int siteId, UserContextDTO user, bool cleanHistoryTables = false);
        public Task<List<SiteListItemDTO>> GetAllSitesAsync(UserContextDTO user);
        public Task<List<ModuleListItemDTO>> GetSiteModulesAsync(int siteId, UserContextDTO user);
        public Task<List<PublicSiteDTO>> SearchPublicSiteDetailsAsync(SiteQueryDTO model);
        public Task<List<SiteDTO>> SearchSiteDetailsAsync(SiteQueryDTO model, UserContextDTO user);
        public Task<SiteConfigurationFormDTO> GetSingleSiteAsync(int siteId, UserContextDTO user);
        public Task<List<AuditEntryDisplayDTO>> GetSiteAuditTrailAsync(int siteId, UserContextDTO user);
        public Task<SiteConfigurationFormDTO> CreateSiteAsync(SiteUserConfigurationDTO model, UserContextDTO user);
        public Task<SiteConfigurationFormDTO> UpdateSiteAsync(int siteId, SiteUserConfigurationDTO model, UserContextDTO user);
        [Obsolete]
        public Task<SiteConfigurationFormDTO> GetSingleSiteLegacyAsync(int siteId);
    }
}
