namespace Zeclhynscki.QueueManager.Events.RabbitMq.Publisher.Contracts;

public interface IRabbitMqEventPublisher
{
    Task Publish(string eventName, string message);
}