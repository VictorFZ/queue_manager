namespace Zeclhynscki.QueueManager.Commands.RabbitMq.Entities;

public class RabbitMQGlobalCommandListenerSettings<T>
{
    public string CommandName { get; set; }
    public int DequeueLimit { get; set; }
    public TimeSpan RetryDelay { get; set; }
    public uint PreFetchSize { get; set; }
    public ushort PreFetchCount { get; set; }
    public TimeSpan? StartDelay { get; set; }
}