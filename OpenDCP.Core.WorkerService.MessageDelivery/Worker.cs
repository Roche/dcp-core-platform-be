using OpenDCP.Core.Core.Extensions;
using OpenDCP.Core.Core.Factories;
using OpenDCP.Core.DAL;
using OpenDCP.Core.Models.Legacy.Common;
using OpenDCP.Framework.Configuration.Model.Constants.MessageDelivery;
using OpenDCP.Framework.Messaging.Constants;
using OpenDCP.Framework.Messaging.Interfaces;
using OpenDCP.Framework.Messaging.Message.MessageDelivery;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenDCP.Core.WorkerService.MessageDelivery
{
    public class Worker : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly IMessageBus _messageBus;
        private readonly IServiceProvider _serviceProvider;

        public Worker(
            ILoggerFactory factory,
            IMessageBus messageBus,
            IServiceProvider serviceProvider)
        {
            _logger = factory.CreateLogger<Worker>();
            _messageBus = messageBus;
            _serviceProvider = serviceProvider;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _messageBus.Connect("Message Delivery Core WorkerService");

            //This is the legacy queue which is only used for downward compability
            //Deprecated will be removed as soon as possible
            _messageBus.InitializeQueue(MessageQueues.MessageDelivery);

            //This is the queue to be used
            _messageBus.InitializeQueue(MessageQueues.LegacyMessageDelivery);

            await base.StartAsync(cancellationToken);

            _logger.LogInformation("DCP Core Message Delivery worker started.");
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DCP Core Message Delivery listening for messages...");

            await _messageBus.BindConsumerAsync(
                MessageQueues.MessageDelivery,
                DeliverMessageAsync
            );

            await _messageBus.BindConsumerAsync(
               MessageQueues.LegacyMessageDelivery,
               DeliverLegacyMessageAsync
           );

            await Task.Yield();

            await Task.CompletedTask;
        }

        private async Task DeliverMessageAsync(string message, string type, string routingKey)
        {
            _logger.LogInformation("Message received");
            _logger.LogDebug(message);

            UserMessageDTO? msg;
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter() }
                };

                msg = JsonSerializer.Deserialize<UserMessageDTO>(message, options);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize message payload.");
                return;
            }

            if (msg == null)
            {
                _logger.LogError("Deserialized UserMessageDTO is null.");
                return;
            }

            _logger.LogDebug(
                "Processing message for Subject: '{Subject}', Channels: [{MessageChannels}], RecipientCount: {RecipientCount}",
                msg.Subject,
                string.Join(", ", msg.MessageChannels),
                msg.UserIds?.Count ?? 0);

            using (var scope = _serviceProvider.CreateScope())
            {
                var messageChannelFactory = scope.ServiceProvider.GetRequiredService<MessageChannelFactory>();

                foreach (var msgChannel in msg.MessageChannels)
                {
                    try
                    {
                        var channel = messageChannelFactory.Load(msgChannel);

                        var formattedMessage = await channel.FormatMessageAsync(msg);

                        formattedMessage = await channel.ApplyTemplate(formattedMessage);

                        var personalizedMessages = await channel.Personalize(formattedMessage);

                        _logger.LogDebug(
                            "Generated {PersonalizedCount} personalized message(s) for channel {MsgChannel}",
                            personalizedMessages.Count,
                            msgChannel);

                        int sentCount = 0;
                        foreach (var pMessage in personalizedMessages)
                        {
                            var targetUsers = string.Join(',', pMessage.UserIds);
                            try
                            {
                                await channel.SendMessageAsync(pMessage);
                                sentCount++;
                                _logger.LogTrace("Message sent via {MsgChannel} to UserIds: {UserIds}", msgChannel, targetUsers);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(
                                   "Message could not be send using channel {MsgChannel} to {UserIds} - see: {Exception}",
                                   msgChannel,
                                   string.Join(',', pMessage.UserIds),
                                   ex.Message
                                );
                            }
                        }

                        _logger.LogInformation(
                            "Successfully dispatched {SentCount}/{TotalCount} message(s) via channel {MsgChannel} for Subject: '{Subject}'",
                            sentCount,
                            personalizedMessages.Count,
                            msgChannel,
                            msg.Subject);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            "Message could not be send using channel {MsgChannel} - see: {Exception}",
                            msgChannel,
                            ex.Message
                         );
                    }
                }
            }
        }

        private async Task DeliverLegacyMessageAsync(string message, string type, string routingKey)
        {
            _logger.LogInformation("Legacy message received");
            _logger.LogDebug(message);

            try
            {
                var mails = JsonSerializer.Deserialize<List<LegacyEmailQueueDTO>>(message);

                if (mails == null)
                {
                    _logger.LogError("Failed to deserialize message");
                    return;
                }

                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<DCPCoreDbContext>();
                    var messageChannelFactory = scope.ServiceProvider.GetRequiredService<MessageChannelFactory>();

                    var userDetails = await dbContext.User
                        .Where(u => mails.Select(m => m.Email).Contains(u.Email))
                        .Select(u =>
                            new
                            {
                                u.Id,
                                u.Email
                            }).ToListAsync();

                    var channel = messageChannelFactory.Load(MessageDeliveryChannelType.Mail);

                    foreach (var mailDefinition in mails)
                    {
                        try
                        {
                            var msg = new UserMessageDTO
                            {
                                Subject = mailDefinition.Subject,
                                Message = mailDefinition.Body,
                                FormattedMessage = mailDefinition.Body,
                                IsGxP = mailDefinition.IsGxp,
                                Format = MessageDeliveryMessageFormat.HTML,
                                //all microservices that are using this Queues are sending 1 message contaning 1 or more emails to be send
                                UserIds = new List<int> { userDetails.Where(x => x.Email == mailDefinition.Email).Select(x => x.Id).FirstOrDefault() },
                                Attachments = mailDefinition.Attachments.ToUserMessageAttachments()
                            };

                            await channel.SendMessageAsync(msg);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(
                                "Message could not be send using channel mail - see: {Exception}",
                                ex.Message
                             );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Message could not be deserialized - exception {Exception}",
                    ex.Message
                 );
            }
        }
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _messageBus.Disconnect();
            await base.StopAsync(cancellationToken);
            _logger.LogInformation("DCP Core Message Delivery stopped.");
        }
    }
}
