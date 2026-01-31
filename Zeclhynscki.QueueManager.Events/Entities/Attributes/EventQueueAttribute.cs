namespace Zeclhynscki.QueueManager.Events.Entities.Attributes;

public class EventQueueAttribute(string eventName, string eventVersion, string queueName) : Attribute
{
    public string EventName { get; } = eventName;
    public string EventVersion { get; set; } = eventVersion;
    public string QueueName { get; } = queueName;
}