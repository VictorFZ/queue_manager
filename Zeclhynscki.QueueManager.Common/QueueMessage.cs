namespace Zeclhynscki.QueueManager.Common;

public class QueueMessage
{
    public DateTime Timestamp { get; set; }
    public string MessageType { get; set; }
    public Guid AggregateId { get; set; }

    protected QueueMessage()
    {
        Timestamp = DateTime.Now.ToUniversalTime();
        MessageType = GetType().Name;
        AggregateId = Guid.NewGuid();
    }
}