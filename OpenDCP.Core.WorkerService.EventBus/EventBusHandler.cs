using OpenDCP.Core.Models.Constants;
using OpenDCP.Core.Models.Models.AuditTrail;
using OpenDCP.Framework.Connections.Core;
using OpenDCP.Framework.Exceptions;
using OpenDCP.Framework.Hosting.Exceptions;
using OpenDCP.Framework.Messaging.Constants;
using System.Text.Json;

namespace OpenDCP.Core.WorkerService.EventBus
{
    public class EventBusHandler
    {
        private readonly ILogger _logger;
        private readonly IDCPCoreConnection _dcpCoreConnection;

        public EventBusHandler(IDCPCoreConnection dcpCoreConnection, ILoggerFactory factory)
        {
            _dcpCoreConnection = dcpCoreConnection;
            _logger = factory.CreateLogger<EventBusHandler>();
        }

        public async Task EventBusHandlerAsync(string message, string type, string routingKey)
        {
            _logger.LogInformation("Message received - routing key: {RoutingKey}", routingKey);

            try
            {
                switch (routingKey)
                {
                    case RoutingKeys.ExecuteAdminActions:
                        {
                            _logger.LogDebug("Received event payload for {RoutingKey}: {MessagePayload}", routingKey, message);
                            AuditTrailAdminActionsEntryDTO? model;
                            try
                            {
                                model = JsonSerializer.Deserialize<AuditTrailAdminActionsEntryDTO>(message);
                            }
                            catch (JsonException jsonEx)
                            {
                                _logger.LogError(jsonEx, "Failed to deserialize event payload for {RoutingKey}. Raw payload: {MessagePayload}", routingKey, message);
                                return;
                            }

                            if (model == null)
                            {
                                _logger.LogError("Deserialized model is null for {RoutingKey}. Raw payload: {MessagePayload}", routingKey, message);
                                return;
                            }

                            await ExecuteAdminActionAsync(model);
                            break;
                        }
                    default:
                        throw new DCPBadRequestException
                            ("Invalid argument");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error in EventBusHandler() {Message}, InnerException ex: {Inner}, stackTrace: {StackTrace}",
                    ex.Message,
                    ex.GetOriginalException()?.Message,
                    ex.StackTrace
                 );
            }
        }

        private async Task ExecuteAdminActionAsync(AuditTrailAdminActionsEntryDTO model)
        {
            await _dcpCoreConnection.PostAsync(DCPCoreEndpoints.CreateAdminActionAuditTrail, model);
        }
    }
}
