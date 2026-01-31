using Microsoft.Extensions.DependencyInjection;
using Zeclhynscki.QueueManager.Events.Contracts;
using Zeclhynscki.QueueManager.Events.Entities;
using Zeclhynscki.QueueManager.Events.RabbitMq.Entities;
using Zeclhynscki.QueueManager.Events.RabbitMq.Publisher;
using Zeclhynscki.QueueManager.Events.RabbitMq.Publisher.Contracts;

namespace Zeclhynscki.QueueManager.Events.RabbitMq.Extensions;

public static class RabbitMqEventStartupExtensions
{
    private const int DefaultDequeueLimit = 5;
    private const int DefaultRetryDelayInMinutes = 5;

    public static IServiceCollection AddRabbitMqEventPublisher(this IServiceCollection services)
    {
        services.AddSingleton<IRabbitMqEventPublisher, RabbitMqEventPublisher>();

        return services;
    }

    public static IServiceCollection AddRabbitMqGlobalEventMediator(this IServiceCollection services)
    {
        services.AddSingleton<IEventMediator, RabbitMqEventMediator>();

        return services;
    }

    public static IServiceCollection RegisterRabbitMQGlobalEventListener<T, THandler>(
        this IServiceCollection services,
        int? dequeueLimit = null,
        TimeSpan? retryDelay = null,
        ushort preFetchCount = 1,
        uint preFetchSize = 0,
        TimeSpan? startDelay = null,
        string queueName = null)
        where T : Event
        where THandler : IEventHandler<T>
    {
        var eventInfo = EventInfo.GetFrom<T>(queueName);

        services.Configure<RabbitMqEventListenerSettings<T>>(settings =>
        {
            settings.EventName = $"{eventInfo.EventName}-event";
            settings.ListenerRouteName = $"{eventInfo.EventName}-event-{eventInfo.QueueName}";
            settings.DequeueLimit = dequeueLimit ?? DefaultDequeueLimit;
            settings.RetryDelay = retryDelay == null || retryDelay.Value.TotalMilliseconds <= default(int)
                ? TimeSpan.FromMinutes(DefaultRetryDelayInMinutes)
                : retryDelay.Value;
            settings.PreFetchCount = preFetchCount;
            settings.PreFetchSize = preFetchSize;
            settings.StartDelay = startDelay;
        });

        services
            .AddTransient(typeof(THandler))
            .Configure<RabbitMqEventHandlerSettings<T>>(settings => settings.Type = typeof(THandler))
            .AddHostedService<RabbitMqEventBackgroundService<T>>();

        return services;
    }
}