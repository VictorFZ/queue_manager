using Zeclhynscki.QueueManager.Common;

namespace Zeclhynscki.QueueManager.Commands.Entities;

public abstract class Command : QueueMessage
{
    public long DequeueCount { get; set; }

    public abstract bool IsValid();

    public virtual IDictionary<string, object> GetObjectsOfInterestForLog()
    {
        return null;
    }
}