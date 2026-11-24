using OpenDCP.Framework.Connections.Core;
using OpenDCP.Framework.Hosting.Exceptions;
using OpenDCP.Framework.Messaging.Constants;
using OpenDCP.Framework.Messaging.Interfaces;

namespace OpenDCP.Core.WorkerService.EventBus
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IMessageBus _messageBus;
        private readonly EventBusHandler _handler;

        public Worker(ILoggerFactory factory, IMessageBus messageBus, IDCPCoreConnection dcpCoreConnection)
        {
            _messageBus = messageBus;
            _handler = new EventBusHandler(dcpCoreConnection, factory);
            _logger = factory.CreateLogger<Worker>();
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _messageBus.Connect("Eventbus Core WorkerService");

            _messageBus.InitializeExchange(MessageExchangers.EventBus,"topic",true);
            _messageBus.InitializeQueue(MessageQueues.EventBus);
            //Legacy Core logic
            _messageBus.InitializeQueue(MessageQueues.LegacyEventBus);

            try
            {
                _messageBus.BindQueue(
                    MessageQueues.EventBus,
                    MessageExchangers.EventBus,
                    new List<string> {
                        RoutingKeys.ExecuteAdminActions
                    }
                 );
                //Legacy Core logic
                _messageBus.BindQueue(
                    MessageQueues.LegacyEventBus,
                    MessageExchangers.LegacyEventBus,
                    new List<string> {
                        RoutingKeys.ExecuteAdminActions
                    }
                 );
            }
            catch (Exception ex)
            {

                _logger.LogError(
                    ex,
                    "Error while QueueBind() ,{Message}, InnerException ex: {InnerException}, stackTrace: {StackTrace}",
                    ex.Message, ex.GetOriginalException().Message, ex.StackTrace);
                throw;
            }

            _logger.LogInformation("DCP Core Eventbus started.");

            await base.StartAsync(cancellationToken);
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker Service started, listening for messages...");

            await _messageBus.BindConsumerAsync(
                MessageQueues.EventBus,
                _handler.EventBusHandlerAsync
            );
            //Legacy Core logic
            await _messageBus.BindConsumerAsync(
                MessageQueues.LegacyEventBus,
                _handler.EventBusHandlerAsync
            );

            await Task.Yield();

            await Task.CompletedTask;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _messageBus.Disconnect();

            await base.StopAsync(cancellationToken);

            _logger.LogInformation("DCP Core Eventbus stopped.");
        }
        
    }
}
