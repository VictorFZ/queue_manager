using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Zeclhynscki.QueueManager.RabbitMq.Entities;
using Zeclhynscki.QueueManager.RabbitMq.Providers.Contracts;

namespace Zeclhynscki.QueueManager.RabbitMq.Providers;

public class RabbitMqChannelDefaultProvider(IOptions<RabbitMqDefaultProviderConnectionSettings> options)
    : IRabbitMqChannelProvider
{
    private const string DefaultChannelName = "default";

    private readonly ConcurrentDictionary<string, RabbitMqChannelInstance> _typeChannels = new();

    public async Task<RabbitMqTemporaryModel> GetChannel(bool forceNewChannel) =>
        await GetChannel(DefaultChannelName, forceNewChannel).ConfigureAwait(false);

    public async Task<RabbitMqTemporaryModel> GetChannel(string key, bool forceNewChannel)
    {
        var isNewChannel = false;

        if (!_typeChannels.ContainsKey(key) || !_typeChannels[key].Channel.IsOpen || !_typeChannels[key].Connection.IsOpen || forceNewChannel)
        {
            if (_typeChannels.ContainsKey(key))
            {
                if (_typeChannels[key].Channel.IsOpen)
                    await _typeChannels[key].Channel.CloseAsync();

                if (_typeChannels[key].Connection.IsOpen)
                    await _typeChannels[key].Connection.CloseAsync();
            }

            var factory = new ConnectionFactory
            {
                HostName = options.Value.HostName,
                UserName = options.Value.UserName,
                Password = options.Value.Password,
                Port = options.Value.Port,
                Ssl = options.Value.UseSsl ? new SslOption { Enabled = true, ServerName = options.Value.HostName } : new SslOption { Enabled = false },
                AutomaticRecoveryEnabled = true
            };

            var connection = await factory.CreateConnectionAsync();

            var newTypeChannel = await connection.CreateChannelAsync();

            _typeChannels.AddOrUpdate(key,
                s => new RabbitMqChannelInstance
                {
                    Channel = newTypeChannel,
                    Connection = connection
                },
                (s, model) =>
                    new RabbitMqChannelInstance
                    {
                        Channel = newTypeChannel,
                        Connection = connection
                    });

            isNewChannel = true;
        }

        return new RabbitMqTemporaryModel { Channel = _typeChannels[key].Channel, IsNewChannel = isNewChannel };
    }

    public async Task CloseCurrentChannel() => await CloseCurrentChannel(DefaultChannelName);

    public async Task CloseCurrentChannel(string key)
    {
        if (!_typeChannels.TryGetValue(key, out var typeChannel))
            return;

        if (typeChannel.Channel.IsOpen)
            await typeChannel.Channel.CloseAsync();

        return;
    }

    private class RabbitMqChannelInstance
    {
        public IConnection Connection { get; init; }
        public IChannel Channel { get; init; }
    }
}