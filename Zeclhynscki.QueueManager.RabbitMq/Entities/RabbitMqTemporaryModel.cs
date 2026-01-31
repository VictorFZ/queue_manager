using RabbitMQ.Client;

namespace Zeclhynscki.QueueManager.RabbitMq.Entities;

public class RabbitMqTemporaryModel
{
    public IChannel Channel { get; set; }
    public bool IsNewChannel { get; set; }
}