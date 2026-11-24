using OpenDCP.Core.Models.Constants;
using OpenDCP.Core.Models.Models.Configuration;
using OpenDCP.Framework.Exceptions;
using OpenDCP.Framework.HttpClient;
using OpenDCP.Framework.ServiceDiscovery.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OpenDCP.Core.Core.Classes
{
    public class ExternalGatewayManagement
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceDiscovery _serviceDiscovery;


        public ExternalGatewayManagement(IConfiguration configuration, IServiceDiscovery serviceDiscovery)
        {
            _configuration = configuration;
            _serviceDiscovery = serviceDiscovery;
        }

        public async Task<List<ApiServiceConfigurationDTO>> GetStatusAsync()
        {
            var env = _configuration["Host:EnvironmentSlug"];
            var queryTags = new List<string> { "backend", "api" };
            var services = await _serviceDiscovery.QueryActiveServices(queryTags, env);

            var result = services.Select(x => new ApiServiceConfigurationDTO
            {
                Name = x.ServiceName.Split('-')[1],
                Label = x.MetaData.ModuleName,
                Status = ApiGatewayStatus.NotAvailable
            }).ToList();

            var servicesActiveOnExternalGateway = await GetActiveServices();
            var servicesAvailableOnExternalGateway = await GetAvailableServices();

            foreach (var service in result)
            {
                if (servicesActiveOnExternalGateway.Contains(service.Name, StringComparer.InvariantCultureIgnoreCase))
                {
                    service.Status = ApiGatewayStatus.Active;
                }
                else if (servicesAvailableOnExternalGateway.Contains(service.Name, StringComparer.InvariantCultureIgnoreCase))
                {
                    service.Status = ApiGatewayStatus.Disabled;
                }
            }

            return result;
        }

        public async Task<List<ApiServiceConfigurationDTO>> SetStatusAsync(List<ApiServiceConfigurationDTO> model)
        {
            var servicesActiveOnExternalGateway = await GetActiveServices();
            var servicesAvailableOnExternalGateway = await GetAvailableServices();

            var activeRootPath = _configuration["ExternalGateway:Configuration:Active"];
            var configurationRootPath = _configuration["ExternalGateway:Configuration:Available"];

            if (activeRootPath is null)
                throw new DCPInternalServerErrorException(ErrorMessages.MissingConfiguration);

            if (configurationRootPath is null)
                throw new DCPInternalServerErrorException(ErrorMessages.MissingConfiguration);

            foreach (var service in model)
            {
                var activeFile = Path.Combine(activeRootPath, $"ocelot.{service.Name}.json");
                var configuredFile = Path.Combine(configurationRootPath, $"ocelot.{service.Name}.json");

                if (service.Status == ApiGatewayStatus.Active
                    && !servicesAvailableOnExternalGateway.Contains(service.Name, StringComparer.InvariantCultureIgnoreCase))
                {
                    throw new DCPBadRequestException($"The {service.Name} is not available");
                }

                if (servicesActiveOnExternalGateway.Contains(service.Name, StringComparer.InvariantCultureIgnoreCase)
                    && service.Status != ApiGatewayStatus.Active)
                {
                    //Service needs to be disabled
                    File.Delete(activeFile);
                }

                if (!servicesActiveOnExternalGateway.Contains(service.Name)
                    && service.Status == ApiGatewayStatus.Active)
                {
                    //Service needs to be activated
                    File.Copy(configuredFile, activeFile, true);
                }
            }

            await Restart();

            return model;

        }


        public async Task<List<ApiServiceConfigurationDTO>> ReloadStatusAsync(List<ApiServiceConfigurationDTO> model)
        {
            var servicesActiveOnExternalGateway = await GetActiveServices();
            var servicesAvailableOnExternalGateway = await GetAvailableServices();

            var activeRootPath = _configuration["ExternalGateway:Configuration:Active"];
            var configurationRootPath = _configuration["ExternalGateway:Configuration:Available"];

            if (activeRootPath is null)
                throw new DCPInternalServerErrorException(ErrorMessages.MissingConfiguration);

            if (configurationRootPath is null)
                throw new DCPInternalServerErrorException(ErrorMessages.MissingConfiguration);

            foreach (var service in model)
            {
                var activeFile = Path.Combine(activeRootPath, $"ocelot.{service.Name}.json");
                var configuredFile = Path.Combine(configurationRootPath, $"ocelot.{service.Name}.json");

                if (service.Status == ApiGatewayStatus.Active && !servicesAvailableOnExternalGateway.Contains(service.Name, StringComparer.InvariantCultureIgnoreCase))
                {
                    throw new DCPBadRequestException($"The {service.Name} is not available");
                }

                //Sanitize ServiceNames to avoid Path Traversal
                if (string.IsNullOrWhiteSpace(service.Name) || service.Name.Any(c => Path.GetInvalidFileNameChars().Contains(c)))
                {
                    throw new DCPBadRequestException($"Invalid service name: {service.Name}");
                }

                if (servicesActiveOnExternalGateway.Contains(service.Name, StringComparer.InvariantCultureIgnoreCase)
                    && service.Status != ApiGatewayStatus.Active)
                {
                    //Service needs to be disabled
                    File.Delete(activeFile);
                }

                if (!servicesActiveOnExternalGateway.Contains(service.Name, StringComparer.InvariantCultureIgnoreCase)
                    && service.Status == ApiGatewayStatus.Active)
                {
                    //Service needs to be activated
                    File.Copy(configuredFile, activeFile, true);
                }
            }

            await Restart();

            return model;

        }

        private async Task Restart()
        {
            var httpClient = new DCPHttpClient(_configuration);

            var baseUrl = await GetGatewayBaseUrl();

            var token = await GetToken(httpClient);

            var response = await httpClient.PostAsync(
                uri: $"{baseUrl}/api/Administration/RestartGateway",
                (h) =>
                {
                    h.Authorization = new AuthenticationHeaderValue("Bearer", token);

                }
            );

            if (!response.IsSuccessStatusCode)
            {
                throw new DCPInternalServerErrorException("External Gateway could not be restarted");
            }
        }

        private async Task<string> GetGatewayBaseUrl()
        {
            var env = _configuration["Host:EnvironmentSlug"];
            var queryTags = new List<string> { "backend", "gateway", "external" };

            var gatewayDetails = (await _serviceDiscovery.QueryActiveServices(queryTags, env)).FirstOrDefault();

            if (gatewayDetails == null)
            {
                throw new DCPInternalServerErrorException("No external Gateway registered/configured");
            }

            return $"https://{gatewayDetails.Host}:{gatewayDetails.Port}";
        }

        private async Task<string> GetBaseUrl()
        {
            var env = _configuration["Host:EnvironmentSlug"];
            var queryTags = new List<string> { "backend", "api", "core" };

            var gatewayDetails = (await _serviceDiscovery.QueryActiveServices(queryTags, env)).FirstOrDefault();

            if (gatewayDetails == null)
            {
                throw new DCPInternalServerErrorException("No external Gateway registered/configured");
            }

            return $"https://{gatewayDetails.Host}:{gatewayDetails.Port}";
        }

        private async Task<string> GetToken(DCPHttpClient httpClient)
        {
            var url = $"{await GetBaseUrl()}/api/v2/token" +
                $"?clientId={_configuration["IdP:Settings:Client:Id"]}" +
                $"&clientSecret={_configuration["IdP:Settings:Client:Secret"]}" +
                $"&grant_type=client_credentials";

            var response = await httpClient.PostAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                throw new DCPInternalServerErrorException("Could not get token for API Gateway administration");
            }

            var content = await response.Content.ReadAsStringAsync();

            var token = JsonDocument.Parse(content)
                .RootElement
                .GetProperty("access_token")
                .GetString();

            if (token == null)
            {
                throw new DCPInternalServerErrorException("Could not get token for API Gateway administration");
            }

            return token;
        }

        private static async Task<List<string>> GetApiServicesFromDirectory(string path)
        {
            var result = new List<string>();

            if (!Directory.Exists(path))
            {
                throw new DCPInternalServerErrorException($"The {path} does not exists!");
            }

            DirectoryInfo dirInfo = new DirectoryInfo(path);

            foreach (FileInfo file in dirInfo.GetFiles().Where(x => x.Extension == ".json"))
            {
                Match match = Regex.Match(file.Name, @"(?<=ocelot\.).*?(?=\.json)", RegexOptions.None, TimeSpan.FromMilliseconds(100));

                //Check if there are routes
                using (StreamReader r = new StreamReader(file.FullName))
                {
                    var stringContent = await r.ReadToEndAsync();

                    if (JsonDocument.Parse(stringContent)
                        .RootElement
                        .TryGetProperty("Routes", out JsonElement routes))
                    {
                        if (routes.ValueKind == JsonValueKind.Array && routes.GetArrayLength() > 0 && match.Success)
                        {
                            result.Add(match.Value);
                        }
                    }
                }
            }

            return result;
        }

        private async Task<List<string>> GetActiveServices()
        {
            var path = _configuration["ExternalGateway:Configuration:Active"];

            if (path is null)
                throw new DCPInternalServerErrorException(ErrorMessages.MissingConfiguration);

            return await GetApiServicesFromDirectory(path);
        }

        private async Task<List<string>> GetAvailableServices()
        {
            var path = _configuration["ExternalGateway:Configuration:Available"];

            if (path is null)
                throw new DCPInternalServerErrorException(ErrorMessages.MissingConfiguration);

            return await GetApiServicesFromDirectory(path);
        }
    }
}
