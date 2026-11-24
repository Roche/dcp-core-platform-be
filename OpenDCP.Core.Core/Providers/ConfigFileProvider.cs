using OpenDCP.Core.Core.Interfaces;
using OpenDCP.Core.Models.Constants;
using OpenDCP.Core.Models.Models.Authentication;
using OpenDCP.Framework.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OpenDCP.Core.Core.Providers
{
    public class ConfigFileProvider : IAuthenticationProvider
    {
        private readonly IConfiguration _configuration;
        public ConfigFileProvider(IConfiguration configuration) {
            _configuration = configuration;
        }
        public string ExtractRequestData(HttpRequest request)
        {
            if (string.IsNullOrEmpty(request.Form["username"]))
            {
                throw new DCPUnauthorizedException(ErrorMessages.SSOError);
            }

            if (string.IsNullOrEmpty(request.Form["password"]))
            {
                throw new DCPUnauthorizedException(ErrorMessages.SSOError);
            }
            return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{request.Form["username"]}:{request.Form["password"]}"));
        }
        public Task<IdPReponse> AuthenticateAsync(string tokenOrCredentials)
        {
            var users = LoadUsers();
            var credentials = Encoding.UTF8.GetString(Convert.FromBase64String(tokenOrCredentials)).Split(":");

            var user = users
                .FirstOrDefault(u => u.UnixId == credentials[0]);

            if (user == null)
            {
                throw new DCPUnauthorizedException(ErrorMessages.UnkownUser);
            }

            if (user.Password != credentials[1])
            {
                throw new DCPUnauthorizedException(ErrorMessages.NoUserAccess);
            }

            var result = new IdPReponse
            {
                StatusCode = "Success",
                AssertionId = Guid.NewGuid().ToString(),
                Issuer = "ConfigFile",
                UnixId = user.UnixId,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Groups = user.Groups,
            };

            return Task.FromResult(result);
        }
        public string LoginUrl(HttpRequest? request = null)
        {
            return $"{request!.Host}/LoginForm.html";
        }

        private List<ConfigurationFileItemDTO> LoadUsers()
        {
            try
            {
                var path = _configuration["IdP:Settings:Path"];

                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                {
                    throw new DCPInternalServerErrorException(ErrorMessages.NoUserAccess);
                }

                var json = File.ReadAllText(path);

                return JsonSerializer.Deserialize<List<ConfigurationFileItemDTO>>(json)
                       ?? new List<ConfigurationFileItemDTO>();
            }
            catch (JsonException ex)
            {
                throw new DCPInternalServerErrorException($"Invalid configuration file: {ex.Message}");
            }
            catch (IOException ex)
            {
                throw new DCPInternalServerErrorException($"Failed to read users configuration file: {ex.Message}");
            }
        }

        public Task<string> LogOutAsync()
        {
            throw new NotImplementedException();
        }
    }
}
