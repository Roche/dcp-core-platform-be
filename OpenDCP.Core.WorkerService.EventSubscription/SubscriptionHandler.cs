using OpenDCP.Core.Core.ConnectionStringLoader;
using OpenDCP.Core.Core.Extensions;
using OpenDCP.Core.DAL;
using OpenDCP.Core.DAL.Entities;
using OpenDCP.Core.Models.Models.EventSubscription;
using OpenDCP.Framework.Connections.Core;
using OpenDCP.Framework.Connections.Manager.Helpers;
using OpenDCP.Framework.Connections.Manager.Models;
using OpenDCP.Framework.Connections.Manager.Pipeline;
using OpenDCP.Framework.Connections.Manager.PluginManagement;
using OpenDCP.Framework.DataTypes.Equipment.Constants;
using OpenDCP.Framework.DataTypes.Equipment.Entities.EventframeContext;
using OpenDCP.Framework.DataTypes.Equipment.Interfaces;
using OpenDCP.Framework.DataTypes.Equipment.Requests.EventFrameContext;
using OpenDCP.Framework.DataTypes.Equipment.Responses.EventFrameContext;
using OpenDCP.Framework.Exceptions;
using OpenDCP.Framework.HttpClient;
using OpenDCP.Framework.Messaging.Classes;
using OpenDCP.Framework.Messaging.Constants;
using OpenDCP.Framework.Messaging.Interfaces;
using OpenDCP.Framework.Messaging.Message.EventBus;
using OpenDCP.Framework.Messaging.Message.EventSubscription;
using OpenDCP.Framework.Messaging.Models;
using OpenDCP.Framework.ServiceDiscovery.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenDCP.Core.WorkerService.EventSubscription
{
    public class SubscriptionHandler
    {
        private readonly IMessageBus _messageBus;
        private readonly IServiceDiscovery _serviceDiscovery;
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly DCPCoreDbContext _dbContext;
        private readonly IDCPCoreConnection _dcpCoreConnection;
        private readonly IServiceProvider _serviceProvider;
        private readonly DatasourceCache _cache;
        private readonly DbConnectionStringLoader _loader;

        public SubscriptionHandler(
            IMessageBus messageBus,
            IServiceDiscovery serviceDiscovery,
            IConfiguration configuration,
            DCPCoreDbContext dbContext,
            IDCPCoreConnection dcpCoreConnection,
            ILoggerFactory factory,
            IServiceProvider serviceProvider,
            DatasourceCache cache)
        {
            _messageBus = messageBus;
            _serviceDiscovery = serviceDiscovery;
            _dbContext = dbContext;
            _configuration = configuration;
            _dcpCoreConnection = dcpCoreConnection;
            _logger = factory.CreateLogger<SubscriptionHandler>();
            _serviceProvider = serviceProvider;
            _cache = cache;
            _loader = new DbConnectionStringLoader(_dbContext, _configuration);
        }

        public async Task CheckSubscriptions()
        {
            var registeredDevices = await GetRegisteredDevices();
            _logger.LogTrace("Discovered {DeviceCount} registered devices across all modules.", registeredDevices.Count);

            var inProgressEvents = await GetInProgressEventsFromDatasource(registeredDevices);
            _logger.LogInformation("Queried {EventCount} in-progress event(s) from external datasources.", inProgressEvents.Count);

            var cachedEvents = await _dbContext.InProgressEvent
                .Select(x => new CachedEventDTO
                {
                    Id = x.Id,
                    ConnectionId = x.DatasourceId,
                    SiteId = x.Datasource!.SiteId,
                    DeviceWebId = x.DeviceWebId,
                    EventDisplayId = x.EventDisplayId,
                    EventId = x.EventId,
                    EventStartTime = x.EventStartTime,
                    Values = x.JEventValues.Select(v => new EventFrameValueDTO
                    {
                        Good = v.Good,
                        Name = v.Name,
                        Value = v.Value,
                    }).ToList(),
                    UpdateAttempts = x.UpdateAttempts,
                })
                .AsNoTracking()
                .ToListAsync();


            var startingEvents = inProgressEvents
                .Where(se => !cachedEvents.Any(ce => ce.EventId == se.EventId &&
                                                     se.EventSubscribers.Any(sub => sub.SiteId == ce.SiteId)))
                .ToList();

            var endingEvents = cachedEvents
                .Where(ce => !inProgressEvents.Any(se => se.EventId == ce.EventId &&
                                                         se.EventSubscribers.Any(sub => sub.SiteId == ce.SiteId)))
                .ToList();

            var eventsToUpdate = cachedEvents
                .Where(ce => inProgressEvents.Any(se => se.EventId == ce.EventId &&
                                                        se.EventSubscribers.Any(sub => sub.SiteId == ce.SiteId))
                          && ce.Values.Any(v => !v.Good)
                          && ce.UpdateAttempts < 10)
                .ToList();

            _logger.LogInformation(
                "Subscription evaluation summary: {StartingCount} new, {EndingCount} ending, {UpdateCount} bad-value events to evaluate.",
                startingEvents.Count,
                endingEvents.Count,
                eventsToUpdate.Count);

            if (endingEvents.Count > 0)
            {
                await HandleEndedEvents(endingEvents);
            }

            if (startingEvents.Count > 0)
            {
                await HandleNewEvents(startingEvents);
            }

            if (eventsToUpdate.Count > 0)
            {
                await HandleBadValueEvents(eventsToUpdate, inProgressEvents);
            }
        }

        private async Task HandleEndedEvents(List<CachedEventDTO> events)
        {
            var model = new EventframeContextIdQueryDTO
            {
                Id = events.Select(x => x.EventId).ToList()
            };

            //Get data
            var eventsByConnection = events.GroupBy(x => x.ConnectionId);
            var eventIdsSkippingRelease = new List<string>();

            var moduleDetails = await _dbContext.Module
                .Where(x => events.Select(x => x.SiteId).Contains(x.SiteId))
                .Select(x => new
                {
                    x.Name,
                    x.IsEnable,
                    x.SiteId
                })
                .AsNoTracking()
                .ToListAsync();


            var modulesPerDatasources = await _dbContext.DataType
                .Where(x => eventsByConnection.Select(x => x.Key).Contains(x.DataSourceId))
                .Select(x => new
                {
                    x.DataSourceId,
                    Name = x.ModuleName,
                    x.DataSource!.SiteId
                })
                .Distinct()
                .AsNoTracking()
                .ToListAsync();

            var relasedTags = modulesPerDatasources.GroupJoin(moduleDetails,
                x => new { x.Name, x.SiteId },
                z => new { z.Name, z.SiteId }, (x, z) =>
                new
                {
                    x.DataSourceId,
                    x.Name,
                    IsEnable = z.Select(y => y.IsEnable).FirstOrDefault()

                }).Where(x => x.IsEnable);

            var pipeline = new PluginPipeline<EventframeContextIdQueryDTO, EventframeListResponse>(_cache, _serviceProvider, _loader)
                    .WithDefaultMapFunction()
                    .WithDefaultTransformFunction()
                    .WithDefaultReduceFunction();

            var eventframeData = await pipeline.RunMapReduceAsync<IEventframeContextDataSource>(
                    (datasource, model, ct) => datasource.SearchEventframeContextByIdAsync(model),
                    model
                );

            foreach (var event2update in events)
            {
                var eventDetails = eventframeData.Eventframes.FirstOrDefault(x => x.Id == event2update.EventId);

                if (eventDetails != null)
                {
                    if (DateTimeOffset.TryParse(
                    eventDetails.EndTime,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var dto)
                && dto.UtcDateTime < DateTime.MaxValue.AddMinutes(-1))
                    {
                        event2update.EventEndTime = dto.UtcDateTime;
                        _logger.LogTrace("Event {EventId} determined end time: {EndTime}", event2update.EventId, event2update.EventEndTime);
                    }
                    else
                    {
                        //The event is still returned as active this can happen if some device is in error
                        //state, the event will not be considered as ended until a finite end time is returned
                        _logger.LogWarning("Event {EventId} still active or returned non-finite end time '{EndTime}'. Skipping release.", event2update.EventId, eventDetails.EndTime);
                        eventIdsSkippingRelease.Add(event2update.EventId);
                    }
                    event2update.Values = eventDetails.Values;
                }
                else
                {
                    event2update.EventEndTime = DateTime.UtcNow;
                    _logger.LogWarning("EventFrame meta data could not be found");
                }
            }

            //Send messages to the exchanger
            foreach (var endingEvent in events.Where(e => !eventIdsSkippingRelease.Contains(e.EventId)))
            {
                var modulesPerDatasource = relasedTags
                    .Where(x => x.DataSourceId == endingEvent.ConnectionId).Select(x => x.Name)
                    .ToList();

                var message = new EventEndMessage
                {
                    SiteId = endingEvent.SiteId,
                    EventEndTime = endingEvent.EventEndTime,
                    EventId = endingEvent.EventId,
                    IsGxP = _configuration.GetValue("Host:IsModuleValidated", false),
                    EventStartTime = endingEvent.EventStartTime,
                    DeviceWebId = endingEvent.DeviceWebId,
                    EventDisplayId = endingEvent.EventDisplayId,
                    Values = endingEvent.Values
                                        .Select(v => new EventFrameValueDTO
                                        {
                                            Name = v.Name,
                                            Value = v.Value,
                                            Good = v.Good,
                                        })
                                        .ToList()
                };

                var messageMetaData = new BasicPropertiesDTO
                {
                    Headers = new Dictionary<string, object>
                     {
                         { "CC", modulesPerDatasource}
                     },
                    Type = BatchSubscriptionEventType.BatchEnd.ToString()
                };

                var options = new JsonSerializerOptions
                {
                    NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
                };

                _messageBus.Publish(
                    message: JsonSerializer.SerializeToElement(message, options),
                    exchangerName: MessageExchangers.EventSubscription,
                    keys: new List<string> { "notUsedKey" },
                    basicProperties: messageMetaData
                );

                //TODO Remove - when all services have been migrated to core-v2
                message.DeviceWebId = DecodeWebId(message.DeviceWebId);
                message.EventId = DecodeWebId(message.EventId);

                //TODO Remove - when all services have been migrated to core-v2. Old services are casting Type to byte. where BatchSubscriptionEventType.BatchEnd.ToString() will send it as "BatchEnd" as string 
                messageMetaData.Type = ((byte)BatchSubscriptionEventType.BatchEnd).ToString();

                var legacyEventMessage = MessageFormatConverter.Convert(message);

                //Legacy to be removed when all service consumers have switched
                _messageBus.Publish(
                    message: JsonSerializer.SerializeToElement(legacyEventMessage, options),
                    exchangerName: MessageExchangers.LegacyEventSubscription,
                    keys: new List<string> { "notUsedKey" },
                    basicProperties: messageMetaData
                );

                _logger.LogInformation(
                    "Batch End for Event: {EventId} ending at: {EndTime} detected on site {SiteId}",
                    endingEvent.EventId, endingEvent.EventEndTime, endingEvent.SiteId
                 );

                _logger.LogDebug(JsonSerializer.SerializeToElement(message, options).ToString());
            }

            var eventIds = events
                .Where(e => !eventIdsSkippingRelease.Contains(e.EventId))
                .Select(x => x.Id)
                .ToList();

            var eventsToDelete = await _dbContext.InProgressEvent
                .Where(ef => eventIds.Contains(ef.Id))
                .ToListAsync();

            if (eventsToDelete.Any())
            {
                _dbContext.InProgressEvent.RemoveRange(eventsToDelete);
                await _dbContext.SaveChangesAsync();
            }
        }

        private async Task HandleNewEvents(List<SubscribedEventDTO> events)
        {
            var model = new EventframeContextIdQueryDTO
            {
                Id = events.Select(x => x.EventId).ToList()
            };

            var pipeline = new PluginPipeline<EventframeContextIdQueryDTO, EventframeListResponse>(_cache, _serviceProvider, _loader)
                    .WithDefaultMapFunction()
                    .WithDefaultTransformFunction()
                    .WithDefaultReduceFunction();

            var eventframeData = await pipeline.RunMapReduceAsync<IEventframeContextDataSource>(
                    (datasource, model, ct) => datasource.SearchEventframeContextByIdAsync(model),
                    model
                );

            var displayEventIdPipeline = new PluginPipeline<RequestContainer, string>(_cache, _serviceProvider, _loader)
                    .WithDefaultMapFunction()
                    .WithDefaultTransformFunction()
                    .WithDefaultReduceFunction();

            // Track missing events to safely remove after enumeration completes
            var eventsToRemove = new List<SubscribedEventDTO>();

            foreach (var event2update in events)
            {
                var eventDetails = eventframeData.Eventframes.FirstOrDefault(x => x.Id == event2update.EventId);

                if (eventDetails != null)
                {
                    var displayEventId = await displayEventIdPipeline.RunMapReduceAsync<IEventframeContextDataSource>(
                        (datasource, args, ct) => Task.FromResult(datasource.ExtractDisplayEventId((List<EventFrameValueDTO>)args[0], (string)args[1])),
                        mapArgIndex: 1,
                        args: new object[] { eventDetails.Values, event2update.EventId }
                    );

                    event2update.EventDisplayId = displayEventId;
                    event2update.Values = eventDetails.Values;
                    _logger.LogTrace("Extracted DisplayEventId '{DisplayEventId}' for EventId {EventId}", displayEventId, event2update.EventId);
                }
                else
                {
                    eventsToRemove.Add(event2update);
                    _logger.LogWarning("EventFrame metadata not found for starting EventId {EventId}. Skipping event processing.", event2update.EventId);
                }
            }

            // Safely remove missing events outside the enumeration loop
            foreach (var missingEvent in eventsToRemove)
            {
                events.Remove(missingEvent);
            }

            foreach (var startingEvent in events)
            {
                foreach (var siteId in startingEvent.EventSubscribers.Select(x => x.SiteId).Distinct())
                {
                    // Create a fresh message instance per site iteration to avoid cross-iteration state corruption
                    var message = new EventStartMessage
                    {
                        EventStartTime = startingEvent.EventStartTime,
                        DeviceWebId = startingEvent.DeviceWebId,
                        EventDisplayId = startingEvent.EventDisplayId,
                        EventId = startingEvent.EventId,
                        IsGxP = _configuration.GetValue("Host:IsModuleValidated", false),
                        Values = startingEvent.Values,
                        SiteId = siteId
                    };

                    // Instantiate a new InProgressEvent entity per site to prevent DbContext tracking conflicts
                    var cachedStartingEvent = new InProgressEvent
                    {
                        DatasourceId = startingEvent.ConnectionId,
                        EventDisplayId = startingEvent.EventDisplayId,
                        DeviceWebId = startingEvent.DeviceWebId,
                        EventId = startingEvent.EventId,
                        UpdateAttempts = 0,
                        EventStartTime = startingEvent.EventStartTime,
                        JEventValues = startingEvent.Values.Select(v => new JEventValue
                        {
                            Name = v.Name,
                            Value = Convert.ToString(v.Value),
                            Good = v.Good
                        }).ToList()
                    };

                    message.SiteId = siteId;

                    _dbContext.InProgressEvent.Add(cachedStartingEvent);

                    var messageMetaData = new BasicPropertiesDTO
                    {
                        Headers = new Dictionary<string, object>
                        {
                            { "CC",
                                startingEvent.EventSubscribers
                                .Where(x => x.SiteId == siteId)
                                .Select(x => x.moduleName)
                                .ToList()
                            }
                        },
                        Type = BatchSubscriptionEventType.BatchStart.ToString()
                    };

                    var options = new JsonSerializerOptions
                    {
                        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
                    };

                    _messageBus.Publish(
                        message: JsonSerializer.SerializeToElement(message, options),
                        exchangerName: MessageExchangers.EventSubscription,
                        keys: new List<string> { "notUsedKey" },
                        basicProperties: messageMetaData
                    );

                    //TODO Remove - when all services have been migrated to core-v2
                    message.DeviceWebId = DecodeWebId(message.DeviceWebId);
                    message.EventId = DecodeWebId(message.EventId);
                    //TODO Remove - when all services have been migrated to core-v2. Old services are casting Type to byte. where BatchSubscriptionEventType.BatchStart.ToString() will send it as "BatchStart" as string 
                    messageMetaData.Type = ((byte)BatchSubscriptionEventType.BatchStart).ToString();

                    var legacyEventMessage = MessageFormatConverter.Convert(message);

                    _messageBus.Publish(
                        message: JsonSerializer.SerializeToElement(legacyEventMessage, options),
                        exchangerName: MessageExchangers.LegacyEventSubscription,
                        keys: new List<string> { "notUsedKey" },
                        basicProperties: messageMetaData
                    );


                    _logger.LogInformation(
                        "Batch Start for Event: {EventId} starting at: {StartTime} detected on site {SiteId}",
                        startingEvent.EventId, startingEvent.EventStartTime, siteId
                    );

                    _logger.LogDebug(JsonSerializer.SerializeToElement(message, options).ToString());
                }
            }

            await _dbContext.SaveChangesAsync();
        }

        private async Task HandleBadValueEvents(List<CachedEventDTO> eventsToUpdate, List<SubscribedEventDTO> inProgressEvents)
        {
            if (!eventsToUpdate.Any())
                return;

            var model = new EventframeContextIdQueryDTO
            {
                Id = eventsToUpdate.Select(x => x.EventId).ToList()
            };

            var pipeline = new PluginPipeline<EventframeContextIdQueryDTO, EventframeListResponse>(_cache, _serviceProvider, _loader)
                .WithDefaultMapFunction()
                .WithDefaultTransformFunction()
                .WithDefaultReduceFunction();

            var eventframeData = await pipeline.RunMapReduceAsync<IEventframeContextDataSource>(
                (datasource, model, ct) => datasource.SearchEventframeContextByIdAsync(model),
                model
            );

            foreach (var cachedEvent in eventsToUpdate)
            {
                var eventDetails = eventframeData.Eventframes.FirstOrDefault(x => x.Id == cachedEvent.EventId);

                if (eventDetails == null)
                {
                    _logger.LogWarning(
                        "Eventframe meta data could not be found for EventId {EventId}",
                        cachedEvent.EventId);
                    continue;
                }

                var dbEvent = await _dbContext.InProgressEvent
                    .FirstOrDefaultAsync(x => x.EventId == cachedEvent.EventId
                                           && x.DatasourceId == cachedEvent.ConnectionId);

                if (dbEvent == null)
                {
                    _logger.LogWarning("Cached entity not found in database for EventId {EventId} on DatasourceId {DatasourceId}", cachedEvent.EventId, cachedEvent.ConnectionId);
                    continue;
                }

                // Increment attempt count
                dbEvent.UpdateAttempts += 1;

                // Only send update message if we have change from bad to good value
                var cachedValuesLookup = cachedEvent.Values
                .GroupBy(v => v.Name)
                .ToDictionary(g => g.Key, g => g.First());

                var hasTransitionedToGood = eventDetails.Values.Any(newVal =>
                    cachedValuesLookup.TryGetValue(newVal.Name, out var oldVal)
                    && !oldVal.Good
                    && newVal.Good);

                if (!hasTransitionedToGood)
                {
                    _logger.LogTrace(
                        "Event {EventId} still has bad values on attempt {Attempt}, skipping update message.",
                        cachedEvent.EventId,
                        dbEvent.UpdateAttempts);

                    await _dbContext.SaveChangesAsync();
                    continue;
                }

                // Update stored values
                dbEvent.JEventValues = eventDetails.Values.Select(v => new JEventValue
                {
                    Name = v.Name,
                    Value = Convert.ToString(v.Value),
                    Good = v.Good
                }).ToList();

                var liveEvent = inProgressEvents.First(se => se.EventId == cachedEvent.EventId);

                var message = new EventMetaDataChangeMessage
                {
                    EventStartTime = cachedEvent.EventStartTime,
                    DeviceWebId = cachedEvent.DeviceWebId,
                    EventDisplayId = cachedEvent.EventDisplayId,
                    EventId = cachedEvent.EventId,
                    IsGxP = _configuration.GetValue("Host:IsModuleValidated", false),
                    Values = eventDetails.Values
                };

                foreach (var siteId in liveEvent.EventSubscribers.Select(x => x.SiteId).Distinct())
                {
                    message.SiteId = siteId;

                    var messageMetaData = new BasicPropertiesDTO
                    {
                        Headers = new Dictionary<string, object>
                        {
                            { "CC",
                                liveEvent.EventSubscribers
                                    .Where(x => x.SiteId == siteId)
                                    .Select(x => x.moduleName)
                                    .ToList()
                            }
                        },
                        Type = BatchSubscriptionEventType.BatchValueChange.ToString()
                    };

                    var options = new JsonSerializerOptions
                    {
                        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
                    };

                    _messageBus.Publish(
                        message: JsonSerializer.SerializeToElement(message, options),
                        exchangerName: MessageExchangers.EventSubscription,
                        keys: new List<string> { "notUsedKey" },
                        basicProperties: messageMetaData
                    );

                    //TODO Remove - when all services have been migrated to core-v2
                    message.DeviceWebId = DecodeWebId(message.DeviceWebId);
                    message.EventId = DecodeWebId(message.EventId);
                    //TODO Remove - when all services have been migrated to core-v2. Old services are casting Type to byte. where BatchSubscriptionEventType.BatchValueChange.ToString() will send it as "BatchValueChange" as string 
                    messageMetaData.Type = ((byte)BatchSubscriptionEventType.BatchValueChange).ToString();

                    var legacyEventMessage = MessageFormatConverter.Convert(message);

                    _messageBus.Publish(
                        message: JsonSerializer.SerializeToElement(legacyEventMessage, options),
                        exchangerName: MessageExchangers.LegacyEventSubscription,
                        keys: new List<string> { "notUsedKey" },
                        basicProperties: messageMetaData
                    );

                    _logger.LogInformation(
                        "Bad value update for Event: {EventId} on attempt {Attempt} for site {SiteId}",
                        cachedEvent.EventId,
                        dbEvent.UpdateAttempts,
                        siteId
                    );

                    _logger.LogDebug("BatchValueChange payload published: {Payload}", JsonSerializer.Serialize(message, options));
                }

                await _dbContext.SaveChangesAsync();
            }
        }

        private async Task<List<SubscribedEventDTO>> GetInProgressEventsFromDatasource(List<DeviceRegistered4EventSubscriptionDTO> registeredDevices)
        {
            var result = new List<SubscribedEventDTO>();

            var connectionInfos = await _dbContext.DataSource.
                Where(x => x.Site.IsEnable
                            && x.DataTypes.Any(dt => dt.Type == Framework.Configuration.Model.Constants.Datasource.DataType.EventframeContext))
                .Select(x => new
                {
                    ConnectionType = x.Type,
                    Id = x.Id,
                    SiteId = x.SiteId,
                    x.ConnectionString,
                    Modules = x.DataTypes.Select(dt => dt.ModuleName).ToList()
                })
                .AsNoTracking()
                .ToListAsync();

            _logger.LogDebug("Found {DatasourceCount} active EventFrameContext datasource(s) across enabled sites.", connectionInfos.Count);

            var pipeline = new PluginPipeline<BatchContextTimeQueryDTO, BatchContextListResponse>(_cache, _serviceProvider, _loader)
                .WithDefaultMapFunction()
                .WithDefaultTransformFunction()
                .WithDefaultReduceFunction();

            var connections = connectionInfos
                .Select(x => new
                {
                    Id = x.Id,
                    SiteId = x.SiteId,
                    Modules = x.Modules
                }
                ).ToList();

            //Perform groupig - registration is happening on a Module/Site context we are requesting in
            //connection context
            foreach (var connection in connections)
            {
                _logger.LogTrace("Polling connection {Id}", connection.Id);

                try
                {
                    var deviceWebIds = registeredDevices
                        .Where(d => d.SiteIds.Contains(connection.SiteId)
                                    && connection.Modules.Contains(d.ModuleName))
                        .Select(d => d.DeviceWebId)
                        .ToList();

                    if (deviceWebIds.Count < 1)
                    {
                        _logger.LogTrace("No registered devices skipping {Connection}", connection.Id);
                        continue;
                    }

                    var model = new BatchContextTimeQueryDTO
                    {
                        SearchMode = EventframeSearchMode.InProgress,
                        StartTime = DateTime.UtcNow.AddYears(-10),
                        EndTime = DateTime.UtcNow,
                        DeviceWebId = deviceWebIds
                    };

                    BatchContextListResponse context;

                    try
                    {
                        context = await pipeline.RunMapReduceWithTimeoutAsync<IEventframeContextDataSource>(
                            (datasource, model, ct) => datasource.SearchBatchContextByTimeAsync(model),
                            model,
                            TimeSpan.FromSeconds(5)
                        );
                    }
                    catch (TimeoutException ex)
                    {
                        _logger.LogWarning(ex, "Timeout for connection {Id}", connection.Id);
                        continue;
                    }

                    if (context.Errors.Any())
                    {
                        foreach (var error in context.Errors)
                        {
                            _logger.LogError(
                                "EventFrame search from datasource for connection: {ConnectionId} contained errors",
                                connection.Id
                             );

                            _logger.LogDebug(
                                "Error Code: {ErrorCode} for {ErrorIdentifier} - Details {ErrorMessage}",
                                error.ErrorCode,
                                error.Identifier,
                                error.Error
                             );

                            if (error.ErrorCode == (byte)ErrorCode.DeleteDevice)
                            {
                                SendDeleteDeviceEvent(error.Identifier, connection.SiteId);
                            }

                        }
                    }

                    var events = context.Eventframes
                        .Select(ef => new SubscribedEventDTO
                        {
                            ConnectionId = connection.Id,
                            DeviceWebId = ef.DeviceWebId,
                            EventId = ef.Id,
                            EventStartTime = ef.StartTime.ParseISO8601(),
                            EventSubscribers = registeredDevices
                                                .Where(x => x.DeviceWebId == ef.DeviceWebId)
                                                .SelectMany(s => s.SiteIds.Select(z => (moduleName: s.ModuleName, siteId: z)))
                                                .ToList()
                        }).ToList();

                    _logger.LogDebug("Fetched {Count} in-progress event frame(s) from ConnectionId {ConnectionId}", events.Count, connection.Id);
                    result.AddRange(events);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Error while fetching events for connection {ConnectionId}",
                        connection.Id
                     );
                }
            }

            return result;
        }
        private async Task<List<DeviceRegistered4EventSubscriptionDTO>> GetRegisteredDevices()
        {
            var urls = await GetSubscriptionRequestUrlsAsync();
            _logger.LogDebug("Discovered {UrlCount} subscription endpoint URL(s).", urls.Count);

            var httpClient = new DCPHttpClient(_configuration);
            TimeSpan requestTimeout = TimeSpan.FromSeconds(5);

            var token = await _dcpCoreConnection.GetToken();

            var connectionMap = await _dbContext.DataType
                .Where(x => x.Type == Framework.Configuration.Model.Constants.Datasource.DataType.EventframeContext && x.DataSource != null)
                .AsNoTracking()
                .Select(x => new
                {
                    x.ModuleName,
                    x.DataSource!.SiteId,
                    x.DataSourceId
                })
                .ToDictionaryAsync(
                    x => (x.ModuleName, x.SiteId),
                    x => x.DataSourceId
                );

            var tasks = urls.Select(async url =>
            {
                try
                {
                    // Create a task with a timeout wrapper
                    var getRequestTask = httpClient.GetAsync(
                        url.url,
                        (h) =>
                        {
                            h.Authorization = new AuthenticationHeaderValue("Bearer", token);
                        }
                    );
                    var timeoutTask = Task.Delay(requestTimeout);

                    // Wait for either the request to complete or timeout
                    var completedTask = await Task.WhenAny(getRequestTask, timeoutTask);

                    // Check if the original request completed in time
                    if (completedTask == getRequestTask && getRequestTask.IsCompleted)
                    {
                        var response = await getRequestTask;
                        return response;
                    }
                    else
                    {
                        _logger.LogWarning("Request for module {Module} to {Url} timed out.", url.module, url.url);
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Request for module {Module} to {Url} failed: {Message}",
                        url.module,
                        url.url,
                        ex.Message
                    );

                    return null;
                }
            });

            var results = await Task.WhenAll(tasks);
            var successfulResults = results.Where(result => result != null && result.IsSuccessStatusCode);

            _logger.LogDebug("Successfully received HTTP 200 responses from {SuccessCount}/{TotalCount} subscription endpoints.", successfulResults.Count(), urls.Count);

            var registeredDevices = new List<DeviceRegistered4EventSubscriptionDTO>();


            foreach (var res in successfulResults)
            {
                var content = await res!.Content.ReadAsStringAsync();

                JsonElement.ArrayEnumerator elements;
                try
                {
                    elements = JsonDocument.Parse(content).RootElement.EnumerateArray();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to parse JSON response for request {Url}",
                        res.RequestMessage?.RequestUri);
                    continue;
                }

                var module = urls.FirstOrDefault(x =>
                    x.url == res.RequestMessage?.RequestUri?.ToString()).module;

                if (string.IsNullOrWhiteSpace(module))
                {
                    _logger.LogWarning(
                        "No matching module found for request URI {Url}",
                        res.RequestMessage?.RequestUri);
                    continue;
                }

                var devices = elements
                    .Select(el =>
                    {
                        // DeviceWebId
                        if (!el.TryGetProperty("DeviceWebId", out var deviceWebIdProp) ||
                         deviceWebIdProp.ValueKind != JsonValueKind.String ||
                         string.IsNullOrWhiteSpace(deviceWebIdProp.GetString()))
                        {
                            _logger.LogWarning(
                                "DeviceWebId is missing, not a string, or empty in response for request {Url}",
                                res.RequestMessage?.RequestUri);
                            return null;
                        }

                        var deviceWebId = deviceWebIdProp.GetString()!;

                        // SiteIds
                        if (!el.TryGetProperty("SiteIds", out var siteIdsProp) ||
                            siteIdsProp.ValueKind != JsonValueKind.Array)
                        {
                            _logger.LogWarning(
                                "SiteIds property is missing or not an array for DeviceWebId {DeviceWebId}",
                                deviceWebId);
                            return null;
                        }

                        List<int>? siteIds = null;

                        // First try normal int deserialization
                        try
                        {
                            siteIds = siteIdsProp.Deserialize<List<int>>();
                        }
                        catch
                        {
                            // Fallback: deserialize as strings, then parse
                            try
                            {
                                var stringIds = siteIdsProp.Deserialize<List<string>>();
                                siteIds = stringIds?
                                    .Select(s => int.TryParse(s, out var i) ? i : (int?)null)
                                    .Where(i => i.HasValue)
                                    .Select(i => i!.Value)
                                    .ToList();
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(
                                    ex,
                                    "Failed to deserialize SiteIds for DeviceWebId {DeviceWebId}",
                                    deviceWebId);
                                return null;
                            }
                        }

                        if (siteIds is not { Count: > 0 })
                        {
                            _logger.LogWarning(
                                "SiteIds is empty or invalid for DeviceWebId {DeviceWebId}",
                                deviceWebId);
                            return null;
                        }

                        return new DeviceRegistered4EventSubscriptionDTO
                        {
                            DeviceWebId = deviceWebId,
                            ModuleName = module,
                            SiteIds = siteIds.Distinct().ToList()
                        };
                    })
                    .Where(dto => dto != null)!.OfType<DeviceRegistered4EventSubscriptionDTO>();

                //TODO Remove - when all services have been migrated to core-v2

                devices = devices
                  .SelectMany(d =>
                     d.SiteIds.Select(s =>
                     {
                         if (connectionMap.TryGetValue((d.ModuleName, s), out var datasourceId))
                         {
                             var encodedWebId = EncodeWebIds(
                                 new List<string> { d.DeviceWebId },
                                 datasourceId
                             ).First();

                             return new
                             {
                                 DeviceWebId = encodedWebId,
                                 d.ModuleName,
                                 SiteId = s
                             };
                         }

                         _logger.LogTrace("No matching datasource found in connectionMap for Module '{ModuleName}' and SiteId {SiteId}", d.ModuleName, s);
                         return null;
                     })
                  )
                  .Where(x => x != null)
                  .Select(x => x!)
                  .GroupBy(x => x.DeviceWebId)
                  .Select(x => new DeviceRegistered4EventSubscriptionDTO
                  {
                      DeviceWebId = x.Key,
                      ModuleName = x.First().ModuleName,
                      SiteIds = x.Select(v => v.SiteId).Distinct().ToList()
                  })
                  .ToList();

                registeredDevices.AddRange(devices);
            }

            return registeredDevices;
        }
        private async Task<List<(string module, string url)>> GetSubscriptionRequestUrlsAsync()
        {
            var env = _configuration["Host:EnvironmentSlug"];
            var gateWayServices = await _serviceDiscovery.QueryActiveServices(new List<string> { "gateway", "internal" }, env);
            var services = await _serviceDiscovery.QueryActiveServices(new List<string> { "backend", "api" }, env);

            if (gateWayServices.Count == 0)
            {
                _logger.LogError("Service discovery query returned no active gateway internal services for environment '{EnvironmentSlug}'.", env);
                throw new DCPInternalServerErrorException("Base url cannot be found");
            }

            var urls = services
                .Where(x => x.MetaData.Resources.Any(r => r.Type == 1))
                .Select(x =>
                (
                    module: x.MetaData.ModuleName,
                    urls: $"https://{gateWayServices[0].Host}:{gateWayServices[0].Port}/{x.MetaData.Resources.First(r => r.Type == 1).ResourceURL}"
                ))
                .ToList();

            _logger.LogDebug("Resolved {Count} target subscription URL(s) from active services.", urls.Count);
            return urls;
        }
        private void SendDeleteDeviceEvent(string webId, int siteId)
        {
            _logger.LogInformation("Publishing DeviceDelete event for WebId: '{DeviceWebId}', SiteId: {SiteId}", webId, siteId);
            var requestModel = new DeleteDeviceMessage
            {
                DeviceWebId = webId,
                SiteId = siteId,
                IsGxp = _configuration.GetValue<bool>("Host:IsModuleValidated", false)
            };

            var options = new JsonSerializerOptions
            {
                NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
            };

            _messageBus.Publish(
                message: JsonSerializer.SerializeToElement(requestModel, options),
                exchangerName: MessageExchangers.EventBus,
                keys: new List<string> { RoutingKeys.DeviceDelete }
                );

            //TODO remove this when all microservices are migrated to new event bus exchanger
            requestModel.DeviceWebId = DecodeWebId(webId);

            _messageBus.Publish(
                message: JsonSerializer.SerializeToElement(requestModel, options),
                exchangerName: MessageExchangers.LegacyEventBus,
                keys: new List<string> { RoutingKeys.DeviceDelete }
                );


        }
        [Obsolete]
        private static string DecodeWebId(string deviceWebId)
        {
            if (WebIdEncoding.Format(deviceWebId) == Framework.Connections.Manager.Constants.WebIdType.DCP)
            {
                return WebIdEncoding.Decode(
                    encoded: deviceWebId).WebId;
            }
            else
            {
                return deviceWebId;
            }
        }
        [Obsolete]
        private static List<string> EncodeWebIds(List<string> deviceWebIds, int datasourceId)
        {
            var deviceWebIdsForSend = new List<string>();

            foreach (var deviceWebId in deviceWebIds)
            {
                if (WebIdEncoding.Format(deviceWebId) == Framework.Connections.Manager.Constants.WebIdType.External)
                {
                    deviceWebIdsForSend.Add(WebIdEncoding.Encode(
                    webId: deviceWebId,
                    datasourceId: datasourceId,
                    version: 1,
                    format: Framework.Connections.Manager.Constants.WebIdType.DCP));
                }
                else
                {
                    deviceWebIdsForSend.Add(deviceWebId);
                }
            }

            return deviceWebIdsForSend;
        }
    }
}
