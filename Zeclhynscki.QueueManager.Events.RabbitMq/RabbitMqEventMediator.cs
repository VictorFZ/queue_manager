using System.Reflection;
using Newtonsoft.Json;
using Zeclhynscki.QueueManager.Events.Contracts;
using Zeclhynscki.QueueManager.Events.Entities;
using Zeclhynscki.QueueManager.Events.Entities.Attributes;
using Zeclhynscki.QueueManager.Events.Exceptions;
using Zeclhynscki.QueueManager.Events.RabbitMq.Publisher.Contracts;

namespace Zeclhynscki.QueueManager.Events.RabbitMq;

//singleton
public class RabbitMqEventMediator(IRabbitMqEventPublisher rabbitMqEventPublisher) : IEventMediator
{
    private const string EventSuffix = "event";

    public virtual async Task Broadcast<T>(T @event) where T : Event
    {
        var eventAttr = typeof(T).GetCustomAttribute<EventAttribute>();

        if (eventAttr == null)
        {
            throw new MissingEventAttribute(typeof(T));
        }

        var eventName = eventAttr.EventName;

        if (!string.IsNullOrEmpty(eventAttr.EventVersion))
        {
            eventName = $"{eventAttr.EventVersion.Replace(".", "-")}-{eventName}";
        }

        eventName = $"{eventName}-{EventSuffix}";

        await rabbitMqEventPublisher.Publish(eventName, JsonConvert.SerializeObject(@event,
            new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            }));
    }

    public async Task Broadcast<T>(string eventName, T @event) where T : Event
    {
        eventName = $"{eventName}-{EventSuffix}";

        await rabbitMqEventPublisher.Publish(eventName, JsonConvert.SerializeObject(@event,
            new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            }));
    }
}