namespace Zeclhynscki.QueueManager.Events.RabbitMq.Entities;

public class RabbitMqEventListenerSettings<T>
{
    public string EventName { get; set; }
    public string ListenerRouteName { get; set; }
    public int DequeueLimit { get; set; }
    public TimeSpan RetryDelay { get; set; }
    public uint PreFetchSize { get; set; }
    public ushort PreFetchCount { get; set; }
    public TimeSpan? StartDelay { get; set; }
}