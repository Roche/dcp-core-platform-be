using OpenDCP.Core.Models.Models.Banner;
using OpenDCP.Framework.Hosting.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenDCP.Core.Core.Interfaces
{
    public interface IBannerService
    {
        public Task<List<BannerDTO>> GetAllBannersAsync(UserContextDTO user);
        public Task<List<BannerDisplayDTO>> GetActiveBannersAsync();
        public Task SetBannerStatusAsync(int bannerId, bool isEnabled,UserContextDTO user);
        public Task<BannerConfigurationDTO> CreateBannerAsync(BannerConfigurationDTO model, UserContextDTO user);
        public Task<BannerConfigurationDTO> UpdateBannerAsync(BannerConfigurationDTO model, UserContextDTO user);
        public Task DeleteBannerAsync(int bannerId, UserContextDTO user);
    }
}
