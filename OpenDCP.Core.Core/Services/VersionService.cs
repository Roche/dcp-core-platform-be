using Consul;
using OpenDCP.Core.Core.Factories;
using OpenDCP.Core.Core.Interfaces;
using OpenDCP.Core.DAL;
using OpenDCP.Core.Models.Models.CalculationNode;
using OpenDCP.Framework.Connections.Manager.Models;
using OpenDCP.Framework.Connections.Manager.PluginManagement;
using OpenDCP.Framework.Security.Classes;
using OpenDCP.Framework.ServiceDiscovery.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OpenDCP.Core.Core.Services
{
    public class VersionService : BaseService, IVersionService
    {
        private readonly IServiceDiscovery _serviceDiscovery;
        protected DatasourceCache _cache;
        public VersionService(DCPCoreDbContext dbContext, IConfiguration configuration,
            ILoggerFactory factory,
            IMemoryCache memoryCache,
            IServiceDiscovery serviceDiscovery,
            DatasourceCache cache) : base(dbContext, configuration, factory, memoryCache)
        {
            _serviceDiscovery = serviceDiscovery;
            _cache = cache;
        }

        public async Task<List<SiteCalculationNodeInfoDTO>> GetCalculationNodeVersionsAsync()
        {
            var key = GetEncryptionKey();
            var env = _configuration["Host:EnvironmentSlug"];

            var calculationNodeFactory = new CalculationNodeFactory(_configuration, _dbContext);

            var tags = new List<string> { "backend", "api" };
            var services = await _serviceDiscovery.QueryActiveServices(tags, env);

            //Get all configured CalculationNode
            var modules = await _dbContext.Module
                .Where(x => x.IsEnable)
                .Select(x => new
                        {
                            x.Id,
                            x.SiteId,
                            x.Name,
                            SiteName = x.Site.Name,
                            x.ConnectionString,
                            x.Type
                        })
                .Distinct()
                .ToListAsync();

            var calculationNodes = modules
                .Select(x => (id: x.Id, node: calculationNodeFactory.LoadByConnectionString(
                    x.Id,
                    x.Type,
                    EncryptionHelper.DecryptConnectionString(x.ConnectionString, key)
                    ))
                )
                .Where(x => x.node.IsConfigured())
                .GroupBy(x => x.node)
                .ToList();

            var timeout = TimeSpan.FromSeconds(3);

            var versionResults = await Task.WhenAll(
                calculationNodes.Select(async group =>
                {
                    try
                    {
                        var version = await WithTimeout(
                            group.Key.GetCalculationNodeVersionAsync(),
                            timeout);

                        return new
                        {
                            Node = group.Key,
                            ModuleIds = group.Select(x => x.id).ToHashSet(),
                            Version = version
                        };
                    }
                    catch
                    {
                        // consider logging
                        return new
                        {
                            Node = group.Key,
                            ModuleIds = group.Select(x => x.id).ToHashSet(),
                            Version = new CalculationNodeVersionResponse()
                        };
                    }
                })
            );

            var versionByModuleId = versionResults
                .SelectMany(v => v.ModuleIds.Select(id => (id, v.Version)))
                .ToDictionary(x => x.id, x => x.Version);


            var libMap = services
                .Select(s => new
                {
                    Name = s.MetaData.ModuleName,
                    Lib = s.MetaData.CalculationNodeLibraries.FirstOrDefault()
                })
                .Where(x => x.Lib != null)
                .DistinctBy(x => x.Name)
                .ToDictionary(x => x.Name, x => x.Lib);

            var result = modules
                .GroupBy(m => new { m.SiteId, m.SiteName })
                .Select(siteGroup => new SiteCalculationNodeInfoDTO
                {
                    SiteId = siteGroup.Key.SiteId,
                    SiteName = siteGroup.Key.SiteName,

                    Modules = siteGroup.Select(m =>
                    {
                        versionByModuleId.TryGetValue(m.Id, out var version);
                        libMap.TryGetValue(m.Name, out var libName);

                        return new ModuleCalculationNodeInfoDTO
                        {
                            ModuleName = m.Name,
                            Type = m.Type,

                            NodeVersion =
                                version
                                    ?.Version
                                    ?.Version
                                ?? "Unknown",

                            PackageVersion =
                                libName != null
                                    ? version
                                        ?.Version
                                        ?.Packages
                                        ?.FirstOrDefault(p => p.Name == libName)
                                        ?.Version
                                      ?? "Unknown"
                                    : "Unknown"
                        };
                    }).ToList()
                })
                .ToList();

            return result;

        }

        public Task<List<RegisteredDataInterfaceDTO>> GetRegisteredDatasourcesAsync()
        {
            var result = _cache.RegisteredInfo;

            return Task.FromResult(result);
        }

        private static async Task<T?> WithTimeout<T>(Task<T> task, TimeSpan timeout)
        {
            var completedTask = await Task.WhenAny(task, Task.Delay(timeout));
            if (completedTask == task)
                return await task; // finished in time
            else
                return default; // timeout
        }
    }
}
