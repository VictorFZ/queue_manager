using Microsoft.Extensions.DependencyInjection;
using Zeclhynscki.QueueManager.RabbitMq.Providers;
using Zeclhynscki.QueueManager.RabbitMq.Providers.Contracts;

namespace Zeclhynscki.QueueManager.RabbitMq.Extensions;

public static class RabbitMqStartupExtensions
{
    public static IServiceCollection AddRabbitMqChannelProvider(this IServiceCollection services, RabbitMqDefaultProviderConnectionSettings settings)
    {
        services.Configure<RabbitMqDefaultProviderConnectionSettings>(s =>
        {
            s.HostName = settings.HostName;
            s.Port = settings.Port;
            s.Password = settings.Password;
            s.UserName = settings.UserName;
            s.UseSsl = settings.UseSsl;
        });

        services.AddSingleton<IRabbitMqChannelProvider, RabbitMqChannelDefaultProvider>();

        return services;
    }
}