using System.Text;
using Autofac;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Zeclhynscki.QueueManager.Commands.Entities;
using Zeclhynscki.QueueManager.Commands.RabbitMq.Entities;
using Zeclhynscki.QueueManager.Log.Contracts;
using Zeclhynscki.QueueManager.RabbitMq;
using Zeclhynscki.QueueManager.RabbitMq.Providers.Contracts;

namespace Zeclhynscki.QueueManager.Commands.RabbitMq;

public class RabbitMqCommandBackgroundService<T>(
    ILifetimeScope lifetimeScope,
    IRabbitMqChannelProvider rabbitMqChannelProvider,
    IOptions<RabbitMQGlobalCommandListenerSettings<T>> commandListenerSettings,
    IOptions<RabbitMQGlobalCommandHandlerSettings<T>> registeredHandlerSettings,
    IQueueLogger logger)
    : BackgroundService
    where T : Command
{
    private readonly SemaphoreSlim _channelLock = new(1, 1);


    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        if (registeredHandlerSettings.Value.Type?.GetMethod("Handle", new Type[] { typeof(T), typeof(CancellationToken) }) == null)
            return;

        if (commandListenerSettings.Value.StartDelay != null)
            await Task.Delay(commandListenerSettings.Value.StartDelay.Value, cancellationToken);

        var key = $"global-command-{typeof(T).FullName}";

        RabbitMqForceNewChannelFlags.Values.TryAdd(key, false);

        while (true)
        {
            try
            {

                var temporaryChannel = await rabbitMqChannelProvider.GetChannel(key, RabbitMqForceNewChannelFlags.Values[key]);

                if (temporaryChannel.IsNewChannel)
                {
                    logger.LogInformation($"{typeof(T).Name} RabbitMQ Global Command Listener: creating new connection {commandListenerSettings.Value.CommandName}");

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
                        
                    await channel.ExchangeDeclareAsync(exchange: commandListenerSettings.Value.CommandName, durable: true, type: "direct", cancellationToken: cancellationToken);
                        
                    var arguments = new Dictionary<string, object> {
                        { "x-dead-letter-exchange", $"{commandListenerSettings.Value.CommandName}-poison" },
                        { "x-dead-letter-routing-key", $"{commandListenerSettings.Value.CommandName}-poison" }
                    };

                    await channel.QueueDeclareAsync(queue: commandListenerSettings.Value.CommandName, durable: true, exclusive: false, autoDelete: false, arguments: arguments, cancellationToken: cancellationToken);
                        
                    await channel.QueueBindAsync(queue: commandListenerSettings.Value.CommandName, exchange: commandListenerSettings.Value.CommandName, routingKey: commandListenerSettings.Value.CommandName, cancellationToken: cancellationToken);

                    //retry queue
                    await channel.ExchangeDeclareAsync(exchange: $"{commandListenerSettings.Value.CommandName}-delayed-{(int)commandListenerSettings.Value.RetryDelay.TotalMilliseconds}", durable: true, type: "direct", cancellationToken: cancellationToken);

                    await channel.QueueDeclareAsync(
                        queue:
                        $"{commandListenerSettings.Value.CommandName}-delayed-{(int)commandListenerSettings.Value.RetryDelay.TotalMilliseconds}",
                        durable: true, exclusive: false, autoDelete: false,
                        arguments: new Dictionary<string, object>
                        {
                            { "x-dead-letter-exchange", commandListenerSettings.Value.CommandName },
                            { "x-dead-letter-routing-key", commandListenerSettings.Value.CommandName },
                            { "x-message-ttl", (int)commandListenerSettings.Value.RetryDelay.TotalMilliseconds }
                        }, cancellationToken: cancellationToken);

                    await channel.QueueBindAsync(queue: $"{commandListenerSettings.Value.CommandName}-delayed-{(int)commandListenerSettings.Value.RetryDelay.TotalMilliseconds}", exchange: $"{commandListenerSettings.Value.CommandName}-delayed-{(int)commandListenerSettings.Value.RetryDelay.TotalMilliseconds}", routingKey: $"{commandListenerSettings.Value.CommandName}-delayed-{(int)commandListenerSettings.Value.RetryDelay.TotalMilliseconds}", cancellationToken: cancellationToken);

                    //poison queue
                    await channel.ExchangeDeclareAsync(exchange: $"{commandListenerSettings.Value.CommandName}-poison", durable: true, type: "direct", cancellationToken: cancellationToken);
                        
                    await channel.QueueDeclareAsync(queue: $"{commandListenerSettings.Value.CommandName}-poison", durable: true, exclusive: false, autoDelete: false, arguments: new Dictionary<string, object> {
                        { "x-message-ttl", (int)TimeSpan.FromDays(7).TotalMilliseconds }
                    }, cancellationToken: cancellationToken);
                        
                    await channel.QueueBindAsync(queue: $"{commandListenerSettings.Value.CommandName}-poison", exchange: $"{commandListenerSettings.Value.CommandName}-poison", routingKey: $"{commandListenerSettings.Value.CommandName}-poison", cancellationToken: cancellationToken);

                    await channel.BasicQosAsync(commandListenerSettings.Value.PreFetchSize, commandListenerSettings.Value.PreFetchCount, false, cancellationToken);

                    var consumer = new AsyncEventingBasicConsumer(channel);

                    consumer.UnregisteredAsync += (sender, args) =>
                    {
                        logger.LogInformation($"{typeof(T).Name} RabbitMQ Global Command Listener: Unregistered Event Hit for {key}");

                        if (!RabbitMqForceNewChannelFlags.Values[key])
                            RabbitMqForceNewChannelFlags.SetAllFlagToTrue();

                        return Task.CompletedTask;
                    };

                    consumer.ShutdownAsync += (sender, args) =>
                    {
                        logger.LogInformation($"{typeof(T).Name} RabbitMQ Global Command Listener: Shutdown Event Hit for {key}");

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

                            var command = JsonConvert.DeserializeObject<T>(message);

                            await using (var container = lifetimeScope.BeginLifetimeScope())
                            {
                                try
                                {
                                    var method = registeredHandlerSettings.Value.Type?.GetMethod("Handle", new Type[] { typeof(T), typeof(CancellationToken) });

                                    result = await ((Task<bool>)method.Invoke(container.Resolve(registeredHandlerSettings.Value.Type), new object[] { command, CancellationToken.None }));
                                }
                                catch (Exception e)
                                {
                                    logger.LogError(e, $"{typeof(T).Name} RabbitMQ Global Command Listener: finished work with exception");

                                    result = false;
                                }
                            }


                            await _channelLock.WaitAsync(cancellationToken);

                            try
                            {
                                if (!result)
                                {
                                    if (command != null)
                                    {
                                        if (command.DequeueCount < commandListenerSettings.Value.DequeueLimit)
                                        {
                                            await channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken);

                                            command.DequeueCount++;

                                            await channel.BasicPublishAsync
                                            (
                                                $"{commandListenerSettings.Value.CommandName}-delayed-{(int)commandListenerSettings.Value.RetryDelay.TotalMilliseconds}", 
                                                $"{commandListenerSettings.Value.CommandName}-delayed-{(int)commandListenerSettings.Value.RetryDelay.TotalMilliseconds}", 
                                                true, 
                                                Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(command)), cancellationToken: cancellationToken);
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
                            logger.LogError(e, $"{typeof(T).Name} RabbitMQ Global Command Listener: could not start work due to exception. Nacking message");

                            await channel.BasicNackAsync(ea.DeliveryTag, false, false, cancellationToken);
                        }
                    };

                    await channel.BasicConsumeAsync(queue: commandListenerSettings.Value.CommandName, consumer: consumer, autoAck: false, cancellationToken: cancellationToken);
                }

                if (RabbitMqForceNewChannelFlags.Values[key])
                {
                    logger.LogInformation($"{typeof(T).Name} RabbitMQ Global Command Listener: Forced new channel creation {commandListenerSettings.Value.CommandName}. Variable: {RabbitMqForceNewChannelFlags.Values[key]}");

                    RabbitMqForceNewChannelFlags.Values[key] = false;

                    logger.LogInformation($"{typeof(T).Name} RabbitMQ Global Command Listener: Reset forceNewChannel value {commandListenerSettings.Value.CommandName}. Variable: {RabbitMqForceNewChannelFlags.Values[key]}");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
            }
            catch (Exception e)
            {
                logger.LogError(e, $"{typeof(T).Name} RabbitMQ Global Command Listener: critical exception (entering 1 min sleep mode and invalidating channel)");

                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
            }
        }
    }
}