using System.Text;
using Autofac;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Zeclhynscki.QueueManager.Events.Entities;
using Zeclhynscki.QueueManager.Events.RabbitMq.Entities;
using Zeclhynscki.QueueManager.Log.Contracts;
using Zeclhynscki.QueueManager.RabbitMq;
using Zeclhynscki.QueueManager.RabbitMq.Providers.Contracts;

namespace Zeclhynscki.QueueManager.Events.RabbitMq;

public class RabbitMqEventBackgroundService<T>(
    ILifetimeScope lifetimeScope,
    IRabbitMqChannelProvider rabbitMqChannelProvider,
    IOptions<RabbitMqEventListenerSettings<T>> eventListenerSettings,
    IOptions<RabbitMqEventHandlerSettings<T>> registeredHandlerSettings,
    IQueueLogger logger)
    : BackgroundService
    where T : Event
{
    private readonly SemaphoreSlim _channelLock = new(1, 1);


    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        if (registeredHandlerSettings.Value.Type?.GetMethod("Handle", new Type[] { typeof(T), typeof(CancellationToken) }) == null)
            return;

        if (eventListenerSettings.Value.StartDelay != null)
            await Task.Delay(eventListenerSettings.Value.StartDelay.Value, cancellationToken);

        var key = $"global-event-{typeof(T).FullName}";

        RabbitMqForceNewChannelFlags.Values.TryAdd(key, false);

        while (true)
        {
            try
            {
                var temporaryChannel = await rabbitMqChannelProvider.GetChannel(key, RabbitMqForceNewChannelFlags.Values[key]);

                if (temporaryChannel.IsNewChannel)
                {
                    logger.LogInformation($"{typeof(T).Name} RabbitMQ Global Event Listener: creating new connection {eventListenerSettings.Value.EventName}");

                    IChannel channel;

                    await _channelLock.WaitAsync(cancellationToken);

                    try
                    {
                        channel = temporaryChannel.Channel;
                    }
                    finally
                    {
                        _channelLock.Release();
                    }

                    await channel.ExchangeDeclareAsync(exchange: eventListenerSettings.Value.EventName, durable: true, type: "fanout", cancellationToken: cancellationToken);
                        
                    await channel.QueueDeclareAsync(queue: eventListenerSettings.Value.ListenerRouteName, durable: true, exclusive: false, autoDelete: false, arguments: new Dictionary<string, object> {
                        { "x-dead-letter-exchange", $"{eventListenerSettings.Value.ListenerRouteName}-poison" },
                        { "x-dead-letter-routing-key", $"{eventListenerSettings.Value.ListenerRouteName}-poison" },
                    }, cancellationToken: cancellationToken);

                    await channel.QueueBindAsync(queue: eventListenerSettings.Value.ListenerRouteName, exchange: eventListenerSettings.Value.EventName, routingKey: "", cancellationToken: cancellationToken);

                    await channel.ExchangeDeclareAsync(exchange: $"{eventListenerSettings.Value.ListenerRouteName}-retry", durable: true, type: "direct", cancellationToken: cancellationToken);
                        
                    await channel.QueueBindAsync(queue: eventListenerSettings.Value.ListenerRouteName, exchange: $"{eventListenerSettings.Value.ListenerRouteName}-retry", routingKey: eventListenerSettings.Value.ListenerRouteName, cancellationToken: cancellationToken);

                    //retry queue
                    await channel.ExchangeDeclareAsync(exchange: $"{eventListenerSettings.Value.ListenerRouteName}-delayed-{(int)eventListenerSettings.Value.RetryDelay.TotalMilliseconds}", durable: true, type: "direct", cancellationToken: cancellationToken);
                        
                    await channel.QueueDeclareAsync(
                        queue:
                        $"{eventListenerSettings.Value.ListenerRouteName}-delayed-{(int)eventListenerSettings.Value.RetryDelay.TotalMilliseconds}",
                        durable: true, exclusive: false, autoDelete: false,
                        arguments: new Dictionary<string, object>
                        {
                            {"x-dead-letter-exchange", $"{eventListenerSettings.Value.ListenerRouteName}-retry"},
                            {"x-dead-letter-routing-key", eventListenerSettings.Value.ListenerRouteName},
                            {"x-message-ttl", (int) eventListenerSettings.Value.RetryDelay.TotalMilliseconds}
                        }, cancellationToken: cancellationToken);

                    await channel.QueueBindAsync(queue: $"{eventListenerSettings.Value.ListenerRouteName}-delayed-{(int)eventListenerSettings.Value.RetryDelay.TotalMilliseconds}", exchange: $"{eventListenerSettings.Value.ListenerRouteName}-delayed-{(int)eventListenerSettings.Value.RetryDelay.TotalMilliseconds}", routingKey: $"{eventListenerSettings.Value.ListenerRouteName}-delayed-{(int)eventListenerSettings.Value.RetryDelay.TotalMilliseconds}", cancellationToken: cancellationToken);

                    //poison queue
                    await channel.ExchangeDeclareAsync(exchange: $"{eventListenerSettings.Value.ListenerRouteName}-poison", durable: true, type: "direct", cancellationToken: cancellationToken);

                    await channel.QueueDeclareAsync(queue: $"{eventListenerSettings.Value.ListenerRouteName}-poison",
                        durable: true, exclusive: false, autoDelete: false,
                        arguments: new Dictionary<string, object>
                        {
                            {"x-message-ttl", (int) TimeSpan.FromDays(7).TotalMilliseconds}
                        }, cancellationToken: cancellationToken);

                    await channel.QueueBindAsync(queue: $"{eventListenerSettings.Value.ListenerRouteName}-poison", exchange: $"{eventListenerSettings.Value.ListenerRouteName}-poison", routingKey: $"{eventListenerSettings.Value.ListenerRouteName}-poison", cancellationToken: cancellationToken);

                    await channel.BasicQosAsync(eventListenerSettings.Value.PreFetchSize, eventListenerSettings.Value.PreFetchCount, false);

                    var consumer = new AsyncEventingBasicConsumer(channel);

                    consumer.UnregisteredAsync += (sender, args) =>
                    {
                        logger.LogInformation($"{typeof(T).Name} RabbitMQ Global Event Listener: Unregistered Event Hit for {key}");

                        if (!RabbitMqForceNewChannelFlags.Values[key])
                            RabbitMqForceNewChannelFlags.SetAllFlagToTrue();

                        return Task.CompletedTask;
                    };

                    consumer.ShutdownAsync += (sender, args) =>
                    {
                        logger.LogInformation($"{typeof(T).Name} RabbitMQ Global Event Listener: Shutdown Event Hit for {key}");

                        if (!RabbitMqForceNewChannelFlags.Values[key])
                            RabbitMqForceNewChannelFlags.SetAllFlagToTrue();

                        return Task.CompletedTask;
                    };

                    consumer.ReceivedAsync += async (model, ea) =>
                    {
                        try
                        {
                            var body = ea.Body.ToArray();
                            var message = Encoding.UTF8.GetString(body);
                            bool result;

                            var @event = JsonConvert.DeserializeObject<T>(message);

                            await using (var container = lifetimeScope.BeginLifetimeScope())
                            {
                                try
                                {
                                    var method = registeredHandlerSettings.Value.Type?.GetMethod("Handle", new Type[] { typeof(T), typeof(CancellationToken) });

                                    result = await ((Task<bool>)method.Invoke(container.Resolve(registeredHandlerSettings.Value.Type), new object[] { @event, CancellationToken.None }));
                                }
                                catch (Exception e)
                                {
                                    logger.LogError(e, $"{typeof(T).Name} RabbitMQ Global Event Listener: finished work with exception");

                                    result = false;
                                }
                            }

                            await _channelLock.WaitAsync(cancellationToken);

                            try
                            {
                                if (!result)
                                {
                                    if (@event != null)
                                    {
                                        if (@event.DequeueCount < eventListenerSettings.Value.DequeueLimit)
                                        {
                                            await channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken);

                                            @event.DequeueCount++;

                                            await channel.BasicPublishAsync
                                            (
                                                $"{eventListenerSettings.Value.ListenerRouteName}-delayed-{(int)eventListenerSettings.Value.RetryDelay.TotalMilliseconds}",
                                                $"{eventListenerSettings.Value.ListenerRouteName}-delayed-{(int)eventListenerSettings.Value.RetryDelay.TotalMilliseconds}",
                                                true,
                                                Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(@event)), cancellationToken: cancellationToken);
                                        }
                                        else
                                        {
                                            await channel.BasicNackAsync(ea.DeliveryTag, false, false, cancellationToken);
                                        }
                                    }
                                    else
                                    {
                                        await channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken);
                                    }
                                }
                                else
                                {
                                    await channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken);
                                }
                            }
                            finally
                            {
                                _channelLock.Release();
                            }
                        }
                        catch (Exception e)
                        {
                            logger.LogError(e, $"{typeof(T).Name} RabbitMQ Global Event Listener: could not start work due to exception. Nacking message");

                            await channel.BasicNackAsync(ea.DeliveryTag, false, false, cancellationToken);
                        }
                    };

                    await channel.BasicConsumeAsync(queue: eventListenerSettings.Value.ListenerRouteName, consumer: consumer, autoAck: false, cancellationToken: cancellationToken);
                }

                if (RabbitMqForceNewChannelFlags.Values[key])
                {
                    logger.LogInformation($"{typeof(T).Name} RabbitMQ Global Event Listener: Forced new channel creation {eventListenerSettings.Value.EventName}. Variable: {RabbitMqForceNewChannelFlags.Values[key]}");

                    RabbitMqForceNewChannelFlags.Values[key] = false;

                    logger.LogInformation($"{typeof(T).Name} RabbitMQ Global Event Listener: Reset forceNewChannel value {eventListenerSettings.Value.EventName}. Variable: {RabbitMqForceNewChannelFlags.Values[key]}");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
            }
            catch (Exception e)
            {
                logger.LogError(e, $"{typeof(T).Name} RabbitMQ Global Event Listener: critical exception (entering 1 min sleep mode and invalidating channel)");

                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
            }
        }
    }
}