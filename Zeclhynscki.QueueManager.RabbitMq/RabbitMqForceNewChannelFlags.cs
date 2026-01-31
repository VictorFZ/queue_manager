using System.Collections.Concurrent;

namespace Zeclhynscki.QueueManager.RabbitMq;

public static class RabbitMqForceNewChannelFlags
{
    public static IDictionary<string, bool> Values = new ConcurrentDictionary<string, bool>();

    public static void SetAllFlagToTrue()
    {
        foreach (var key in Values.Keys)
        {
            Values[key] = true;
        }
    }
}