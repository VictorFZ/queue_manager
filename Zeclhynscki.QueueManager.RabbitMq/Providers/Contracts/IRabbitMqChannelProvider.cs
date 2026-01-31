using Zeclhynscki.QueueManager.RabbitMq.Entities;

namespace Zeclhynscki.QueueManager.RabbitMq.Providers.Contracts;

public interface IRabbitMqChannelProvider
{
    Task<RabbitMqTemporaryModel> GetChannel(bool forceNewChannel);
    Task<RabbitMqTemporaryModel> GetChannel(string key, bool forceNewChannel); 
    Task CloseCurrentChannel();
    Task CloseCurrentChannel(string key);
}