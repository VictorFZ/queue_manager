namespace Zeclhynscki.QueueManager.Events.Entities.Attributes;

public class EventAttribute(string eventName, string eventVersion) : Attribute
{
    public string EventName { get; } = eventName;
    public string EventVersion { get; set; } = eventVersion;
}