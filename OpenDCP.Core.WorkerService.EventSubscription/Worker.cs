using OpenDCP.Core.DAL;
using OpenDCP.Framework.Connections.Core;
using OpenDCP.Framework.Connections.Manager.PluginManagement;
using OpenDCP.Framework.Hosting.Exceptions;
using OpenDCP.Framework.Messaging.Constants;
using OpenDCP.Framework.Messaging.Interfaces;
using OpenDCP.Framework.ServiceDiscovery.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace OpenDCP.Core.WorkerService.EventSubscription
{
    public class Worker : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly IMessageBus _messageBus;
        private readonly IServiceProvider _serviceProvider;
        private readonly IServiceDiscovery _serviceDiscovery;
        private readonly IConfiguration _configuration;
        private readonly IDCPCoreConnection _dcpCoreConnection;
        private readonly ILoggerFactory _factory;
        private readonly DatasourceCache _cache;
        public Worker(ILoggerFactory factory, IMessageBus messageBus, IServiceDiscovery serviceDiscovery,
            IConfiguration configuration, IServiceProvider serviceProvider, IDCPCoreConnection dcpCoreConnection, DatasourceCache cache)
        {
            _factory = factory;
            _logger = factory.CreateLogger<Worker>();
            _messageBus = messageBus;
            _serviceProvider = serviceProvider;
            _serviceDiscovery = serviceDiscovery;
            _configuration = configuration;
            _dcpCoreConnection = dcpCoreConnection;
            _cache = cache;
        }
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _messageBus.Connect("Event subscription WorkerService");

            //´The messaging channels to be used
            _messageBus.InitializeQueue(MessageQueues.Default);
            _messageBus.InitializeExchange(
                MessageExchangers.EventSubscription,
                "direct",
                true
              );

            //The legacy quees those should be decom as soon as possible
            _messageBus.InitializeQueue(MessageQueues.LegacyDefault);
            _messageBus.InitializeExchange(
                MessageExchangers.LegacyEventSubscription,
                "direct",
                true
              );

            try
            {
                _messageBus.BindQueue(
                    MessageQueues.LegacyDefault,
                    MessageExchangers.LegacyEventSubscription,
                    new List<string> {
                        RoutingKeys.Default
                    }
                 );

                _messageBus.BindQueue(
                    MessageQueues.Default,
                    MessageExchangers.EventSubscription,
                    new List<string> {
                        RoutingKeys.Default
                    }
                 );
            }
            catch (Exception ex)
            {

                _logger.LogError(
                    "Error while QueueBind() {message}, InnerException ex: {exception}, stackTrace: {stackTrace}",
                    ex.Message,
                    ex.GetOriginalException().Message,
                    ex.StackTrace
                 );
                throw;
            }

            _logger.LogInformation("DCP Core Event subscription worker started.");

            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<DCPCoreDbContext>();

                await dbContext.InProgressEvent.ExecuteDeleteAsync();
            }

            _logger.LogInformation("InProgress Event Cache cleared");

            await base.StartAsync(cancellationToken);
        }
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Worker Service started, start event polling...");

            string? configValue = _configuration["LoopsTime:EventChecks"];
            if (!double.TryParse(configValue, NumberStyles.Any, CultureInfo.InvariantCulture, out double loopExecution) || loopExecution <= 0)
            {
                _logger.LogError("Invalid or missing configuration for 'LoopsTime:EventChecks'. Current value: '{ConfigValue}'. Polling loop aborted.", configValue);
                return;
            }

            _logger.LogTrace("Event polling configured with interval: {IntervalSeconds} seconds.", loopExecution);

            using PeriodicTimer timer = new PeriodicTimer(TimeSpan.FromSeconds(loopExecution));

            while (!cancellationToken.IsCancellationRequested &&
                await timer.WaitForNextTickAsync(cancellationToken))
            {
                //Wrap any invocations inside this while loop into a try catch so that when one run of the while loop fails,
                //it doesn't break the entire method meaning the periodic loop continues
                try
                {
                    _logger.LogDebug("Start event-polling...");

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<DCPCoreDbContext>();

                        var checker = new SubscriptionHandler(
                                   _messageBus,
                                   _serviceDiscovery,
                                   _configuration,
                                   dbContext,
                                   _dcpCoreConnection,
                                   _factory,
                                   _serviceProvider,
                                   _cache
                        );

                        await checker.CheckSubscriptions();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error while polling for events , "
                                     + ex.Message
                                     + ", InnerException ex:"
                                     + ex.GetOriginalException()?.Message
                                     + ", stackTrace:"
                                     + ex.StackTrace);
                }
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _messageBus.Disconnect();
            _logger.LogInformation("DCP Core Event subscription worker stopped.");
            await base.StopAsync(cancellationToken);
        }

    }
}
