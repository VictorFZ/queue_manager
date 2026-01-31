using Zeclhynscki.QueueManager.Common;

namespace Zeclhynscki.QueueManager.Events.Entities;

public abstract class Event : QueueMessage
{
    public long DequeueCount { get; set; }
}