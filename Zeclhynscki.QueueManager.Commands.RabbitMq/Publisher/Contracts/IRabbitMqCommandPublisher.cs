namespace Zeclhynscki.QueueManager.Commands.RabbitMq.Publisher.Contracts;

public interface IRabbitMqCommandPublisher
{
    Task Publish(string commandName, string message, TimeSpan? delay = null);
}