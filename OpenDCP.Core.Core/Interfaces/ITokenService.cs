using OpenDCP.Core.Models.Constants;
using OpenDCP.Core.Models.Models.Authentication;
using OpenDCP.Framework.Security.Constants;
using System.Threading.Tasks;

namespace OpenDCP.Core.Core.Interfaces
{
    public interface ITokenService
    {
        public Task<TokenResponseDTO> GenerateToken(string tokenOrCredentials);
        public Task<TokenResponseDTO> GenerateToken(string clientId, string clientSecret, PersistedGrantsType type);
        public Task<TokenResponseDTO> RefreshToken(string refreshToken, TokenFormat format = TokenFormat.V2);
        public Task<TokenResponseDTO> ExchangeCollect4AccessToken(string token);
        public Task<string> GenerateCollectToken(string tokenOrCredentials);
        public Task<bool> IsAllowedAsync(string unixId);
        public Task RevokeUserTokensAsync(string unixId);
        public Task LogUserActionAsync(string unixId, SecurityAction action);
        public Task<TokenResponseDTO> GenerateToken(string certificateThumbprint, PersistedGrantsType type);
    }
}
