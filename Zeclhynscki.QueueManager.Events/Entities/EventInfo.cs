using System.Reflection;
using Zeclhynscki.QueueManager.Events.Entities.Attributes;
using Zeclhynscki.QueueManager.Events.Exceptions;

namespace Zeclhynscki.QueueManager.Events.Entities;

public class EventInfo
{
    public string EventName { get; private set; }
    public string QueueName { get; private set; }

    public static EventInfo GetFrom<T>(string queueName = null)
        where T : Event
    {
        var eventQueueAttr = typeof(T).GetCustomAttribute<EventQueueAttribute>();
        var eventAttr = typeof(T).GetCustomAttribute<EventAttribute>();

        if (eventQueueAttr == null && eventAttr == null)
            throw new MissingEventQueueAttribute(typeof(T));

        queueName ??= eventQueueAttr?.QueueName;

        if (string.IsNullOrWhiteSpace(queueName))
            throw new MissingQueueName(typeof(T));

        var eventName = eventQueueAttr?.EventName ?? eventAttr?.EventName;

        if (string.IsNullOrWhiteSpace(eventName))
            throw new MissingEventName(typeof(T));

        var eventVersion = eventQueueAttr?.EventVersion ?? eventAttr?.EventVersion ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(eventVersion))
        {
            eventName = $"{eventVersion.Replace(".", "-")}-{eventName}";
        }

        return new EventInfo
        {
            EventName = eventName,
            QueueName = queueName
        };
    }
}