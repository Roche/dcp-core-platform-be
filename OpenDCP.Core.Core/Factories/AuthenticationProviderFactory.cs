using OpenDCP.Core.Core.Interfaces;
using OpenDCP.Core.Core.Providers;
using Microsoft.Extensions.Configuration;
using System.ComponentModel;

namespace OpenDCP.Core.Core.Factories
{
    public static class AuthenticationProviderFactory
    {
        public static IAuthenticationProvider Load(string driver, IConfiguration configuration)
        {
            switch (driver.ToLower())
            {
                case "sso":
                    return new SingleSignOnProvider(configuration);
                case "ad":
                    return new ActiveDirectoryProvider(configuration);
                case "file":
                    return new ConfigFileProvider(configuration);
                default:
                    throw new InvalidEnumArgumentException($"The driver {driver} is not a known authentication procider");
            }
        }
    }
}
