using System.Collections.Concurrent;
using System.Text;
using RabbitMQ.Client;
using Zeclhynscki.QueueManager.Events.RabbitMq.Publisher.Contracts;
using Zeclhynscki.QueueManager.RabbitMq.Providers.Contracts;

namespace Zeclhynscki.QueueManager.Events.RabbitMq.Publisher;

public class RabbitMqEventPublisher(IRabbitMqChannelProvider channelProvider) : IRabbitMqEventPublisher
{
    private static class CachedBind
    {
        public static readonly ConcurrentDictionary<string, string> CachedForExchange = new ConcurrentDictionary<string, string>();
    }

    public async Task Publish(string eventName, string message)
    {
        var temporaryChannel = await channelProvider.GetChannel($"{eventName}-publisher", false);

        var channel = temporaryChannel.Channel;

        if (!CachedBind.CachedForExchange.ContainsKey(eventName))
        {
            await channel.ExchangeDeclareAsync(exchange: eventName, durable: true, type: "fanout");

            CachedBind.CachedForExchange.TryAdd(eventName, eventName);
        }

        var body = Encoding.UTF8.GetBytes(message);

        var props = new BasicProperties
        {
            Persistent = true
        };

        await channel.BasicPublishAsync(eventName, "", false, props, body);
    }
}