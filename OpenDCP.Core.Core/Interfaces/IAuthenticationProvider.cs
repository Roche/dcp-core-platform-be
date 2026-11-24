using OpenDCP.Core.Models.Models.Authentication;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace OpenDCP.Core.Core.Interfaces
{
    public interface IAuthenticationProvider
    {
        Task<IdPReponse> AuthenticateAsync(string tokenOrCredentials);
        string ExtractRequestData(HttpRequest request);
        string LoginUrl(HttpRequest? request = null);
        Task<string> LogOutAsync();
    }
}
