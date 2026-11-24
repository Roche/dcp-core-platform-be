using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;
using Serilog;
using OpenDCP.Framework.ConfigurationProvider.Interfaces;
using OpenDCP.Framework.ConfigurationProvider.Factories;
using OpenDCP.Framework.ConfigurationProvider.Models;
using OpenDCP.Core.DAL;
using Microsoft.EntityFrameworkCore;
using OpenDCP.Framework.ServiceDiscovery.Factories;
using OpenDCP.Framework.ServiceDiscovery.Interfaces;
using OpenDCP.Framework.ServiceDiscovery.Models;

IHost host = Host.CreateDefaultBuilder(args)
       .UseWindowsService(options =>
       {
           options.ServiceName = "DCP Core CleanUp Service";
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
               return secretConfigurationFactory.Load(configuration["SecretConfigurationProvider:Driver"], null, configuration);
           });

           serviceProvider = services.BuildServiceProvider();
           var secretConfigurationProvider = serviceProvider.GetRequiredService<ISecretConfigurationProvider>();

           configuration = secretConfigurationProvider.AddSecretsToConfiguration(
               configuration,
               new SecretConfigurationProviderOptions
               {
                   AddHeaders = false,
                   AddModuleClient = false,
                   AddAudience = false,
                   AddLegacyHeaders = false,
                   AddMessageBusConfiguration = false,
               }
           );
           services.AddSingleton<IConfiguration>(configuration);

           services.AddDbContext<DCPCoreDbContext>(options =>
                options.UseNpgsql(secretConfigurationProvider.GetConnectionString("PostgreSQL"),
                sqlOptions => sqlOptions.CommandTimeout(60)),
                ServiceLifetime.Scoped
            );

       }).Build();

await host.RunAsync();
