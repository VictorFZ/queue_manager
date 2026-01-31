using System.Collections.Concurrent;
using System.Text;
using RabbitMQ.Client;
using Zeclhynscki.QueueManager.Commands.RabbitMq.Publisher.Contracts;
using Zeclhynscki.QueueManager.RabbitMq.Providers.Contracts;

namespace Zeclhynscki.QueueManager.Commands.RabbitMq.Publisher;

public class RabbitMqCommandPublisher(IRabbitMqChannelProvider channelProvider) : IRabbitMqCommandPublisher
{
    private static class CachedBind
    {
        public static readonly ConcurrentDictionary<string, string> CachedForQueue = new();
        public static readonly ConcurrentDictionary<string, string> CachedForDelayedQueue = new();
    }

    public async Task Publish(string commandName, string message, TimeSpan? delay = null)
    {
        var temporaryChannel = await channelProvider.GetChannel($"{commandName}-publisher", false);

        var channel = temporaryChannel.Channel;
            
        if (!CachedBind.CachedForQueue.ContainsKey(commandName))
        {
            await channel.ExchangeDeclareAsync(commandName, "direct", true, false);
                
            var arguments = new Dictionary<string, object> {
                { "x-dead-letter-exchange", $"{commandName}-poison" },
                { "x-dead-letter-routing-key", $"{commandName}-poison" },
            };
                
            await channel.QueueDeclareAsync(queue: commandName, durable: true, exclusive: false, autoDelete: false, arguments: arguments);
            await channel.QueueBindAsync(queue: commandName, exchange: commandName, routingKey: commandName);

            CachedBind.CachedForQueue.TryAdd(commandName, commandName);
        }

        var body = Encoding.UTF8.GetBytes(message);

        var props = new BasicProperties
        {
            Persistent = true
        };

        if (delay != null && delay.Value.TotalMilliseconds > default(long))
        {
            var delayedCommandName = $"{commandName}-delayed-{(int)delay.Value.TotalMilliseconds}";

            if (!CachedBind.CachedForDelayedQueue.ContainsKey(delayedCommandName))
            {
                await channel.ExchangeDeclareAsync(exchange: delayedCommandName, durable: true, type: "direct");
                await channel.QueueDeclareAsync(queue: delayedCommandName, durable: true, exclusive: false, autoDelete: false, arguments: new Dictionary<string, object> {
                    { "x-dead-letter-exchange", commandName },
                    { "x-dead-letter-routing-key", commandName },
                    { "x-message-ttl", (int)delay.Value.TotalMilliseconds }
                });
                await channel.QueueBindAsync(queue: delayedCommandName, exchange: delayedCommandName, routingKey: delayedCommandName);

                CachedBind.CachedForDelayedQueue.TryAdd(delayedCommandName, delayedCommandName);
            }

            await channel.BasicPublishAsync(delayedCommandName, delayedCommandName, true, props, body);
        }
        else
        {
            await channel.BasicPublishAsync(commandName, commandName, true, props, body);
        }
    }
}