using OpenDCP.Core.DAL;
using OpenDCP.Core.WorkerService.EventSubscription;
using OpenDCP.Framework.ConfigurationProvider.Constants;
using OpenDCP.Framework.ConfigurationProvider.Factories;
using OpenDCP.Framework.ConfigurationProvider.Interfaces;
using OpenDCP.Framework.ConfigurationProvider.Models;
using OpenDCP.Framework.Connections.Core;
using OpenDCP.Framework.Connections.Manager.PluginManagement;
using OpenDCP.Framework.DataTypes.Equipment.Interfaces;
using OpenDCP.Framework.Exceptions;
using OpenDCP.Framework.Messaging.Factories;
using OpenDCP.Framework.Messaging.Interfaces;
using OpenDCP.Framework.ServiceDiscovery.Factories;
using OpenDCP.Framework.ServiceDiscovery.Interfaces;
using OpenDCP.Framework.ServiceDiscovery.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;
using Serilog;

IHost host = Host.CreateDefaultBuilder(args)
       .UseWindowsService(options =>
       {
           options.ServiceName = "DCP Core In Progress Event Subscription Service";
       })
        .UseSystemd()
        .ConfigureAppConfiguration((hostingContext, config) =>
        {
            config
                 .SetBasePath(hostingContext.HostingEnvironment.ContentRootPath)
                      .AddJsonFile("appsettings.json", true, true)
                .AddEnvironmentVariables();

        })
       .ConfigureServices((hostContext, services) =>
       {
           var configuration = hostContext.Configuration;

           services.AddMemoryCache();

           if (OperatingSystem.IsWindows())
           {
               LoggerProviderOptions.RegisterProviderOptions<EventLogSettings, EventLogLoggerProvider>(services);
           }

           var logger = new LoggerConfiguration()
            .ReadFrom.Configuration(hostContext.Configuration)
            .Enrich.FromLogContext()
            .CreateLogger();

           services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(logger));

           services.AddSingleton<IServiceDiscovery>(provider =>
           {
               var serviceDiscoveryFactory = new ServiceDiscoveryFactory();
               return serviceDiscoveryFactory.Load(configuration["ServiceDiscoveryProvider:Driver"], provider);
           });

           //Shared configuration
           services.AddSingleton<IServiceDiscoveryConfiguration>(provider =>
           {
               var serviceDiscoveryFactory = new ServiceDiscoveryConfigurationFactory();
               return serviceDiscoveryFactory.Load(configuration["ServiceDiscoveryProvider:Driver"]);
           });

           // Build the provider to resolve an instance
           var serviceProvider = services.BuildServiceProvider();
           var sharedConfigurationProvider = serviceProvider.GetRequiredService<IServiceDiscoveryConfiguration>();

           configuration = sharedConfigurationProvider.AddSharedConfiguration(
               services,
               configuration,
               new SharedConfigurationOptions
               {
                   AddHeaders = true,
                   AddMemoryCache = true,
                   AddMessageBus = true,
                   AddSecretProvider = true,
                   AddIdP = false,
               }
           );

           //SecretConfiguration
           services.AddSingleton<ISecretConfigurationProvider>(provider =>
           {
               var secretConfigurationFactory = new SecretConfigurationProviderFactory();
               return secretConfigurationFactory.Load(
                   configuration["SecretConfigurationProvider:Driver"],
                   null,
                   configuration
               );
           });

           //MessageBus
           services.AddSingleton<IMessageBus>(provider =>
           {
               var messageBusFactory = new MessageBusFactory();
               return messageBusFactory.Load(configuration["MessageBusProvider:Driver"], configuration);
           });

           serviceProvider = services.BuildServiceProvider();
           var secretConfigurationProvider = serviceProvider.GetRequiredService<ISecretConfigurationProvider>();

           configuration = secretConfigurationProvider.AddSecretsToConfiguration(
               configuration,
               new SecretConfigurationProviderOptions
               {
                   AddHeaders = true,
                   AddEncryptionKey = true,
                   AddModuleClient = true,
                   MessageBusType = MessageBusType.RabbitMQ,
                   AddAudience = false,
                   AddLegacyHeaders = false,
                   AddMessageBusConfiguration = true,
               }
           );

           services.AddSingleton<IConfiguration>(configuration);

           //DbContext
           services.AddDbContext<DCPCoreDbContext>(options =>
               options.UseNpgsql(secretConfigurationProvider.GetConnectionString(databaseType: "PostgreSQL"),
               sqlOptions => sqlOptions.CommandTimeout(60)),
               ServiceLifetime.Scoped
           );

           services.AddSingleton<IDCPCoreConnection, DCPCoreConnection>();

           //Register datasources
           var pluginPath = configuration["Host:PluginsPath"];
           if (pluginPath is null || !Directory.Exists(pluginPath))
           {
               throw new DCPInternalServerErrorException("Plugin diretory not existing");
           }

           services.AddSingleton(sp =>
           {
               return new DatasourceCache(
                   pluginPath,
                   configuration.GetValue("Debug:Enabled", false),
                   typeof(IEventframeContextDataSource)
               );
           });

           services.AddHostedService<Worker>();

       }).Build();

await host.RunAsync();
