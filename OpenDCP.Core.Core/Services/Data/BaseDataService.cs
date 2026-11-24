using System;
using OpenDCP.Core.DAL;
using Microsoft.Extensions.Configuration;
using OpenDCP.Framework.Connections.Manager.PluginManagement;

namespace OpenDCP.Core.Core.Services.Data
{
    public class BaseDataService
    {
        protected readonly IConfiguration _configuration;
        protected readonly DCPCoreDbContext _dbContext;
        protected IServiceProvider _serviceProvider;
        protected DatasourceCache _cache;

        public BaseDataService(IConfiguration configuration, DCPCoreDbContext dbContext, IServiceProvider serviceProvider, DatasourceCache cache)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _cache = cache;
            _serviceProvider = serviceProvider;
        }
    }
}
