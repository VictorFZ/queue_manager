using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Zeclhynscki.QueueManager.Commands.Contracts;
using Zeclhynscki.QueueManager.Commands.Entities;
using Zeclhynscki.QueueManager.Commands.Entities.Attributes;
using Zeclhynscki.QueueManager.Commands.Exceptions;
using Zeclhynscki.QueueManager.Commands.RabbitMq.Entities;
using Zeclhynscki.QueueManager.Commands.RabbitMq.Publisher;
using Zeclhynscki.QueueManager.Commands.RabbitMq.Publisher.Contracts;

namespace Zeclhynscki.QueueManager.Commands.RabbitMq.Extensions;

public static class RabbitMqCommandStartupExtensions
{
    private const int DefaultDequeueLimit = 5;
    private const int DefaultRetryDelayInMinutes = 5;


    public static IServiceCollection AddRabbitMqCommandPublisher(this IServiceCollection services)
    {
        services.AddSingleton<IRabbitMqCommandPublisher, RabbitMqCommandPublisher>();

        return services;
    }
        
    public static IServiceCollection AddRabbitMqCommandMediator(this IServiceCollection services)
    {
        services.AddSingleton<ICommandMediator, RabbitMqCommandMediator>();

        return services;
    }

    public static IServiceCollection RegisterRabbitMqCommandListener<T, THandler>(this IServiceCollection services, int? dequeueLimit = null, TimeSpan? retryDelay = null, ushort preFetchCount = 1, uint preFetchSize = 0, TimeSpan? startDelay = null)
        where T : Command
        where THandler : ICommandHandler<T>
    {
        var commandAttr = typeof(T).GetCustomAttribute<CommandAttribute>();

        if (commandAttr == null)
        {
            throw new MissingCommandAttribute(typeof(T));
        }

        var commandName = commandAttr.CommandName;

        if (!string.IsNullOrEmpty(commandAttr.CommandVersion))
        {
            commandName = $"{commandAttr.CommandVersion.Replace(".", "-")}-{commandName}";
        }

        services.Configure<RabbitMQGlobalCommandListenerSettings<T>>(settings =>
        {
            settings.CommandName = $"{commandName}-command";
            settings.DequeueLimit = dequeueLimit ?? DefaultDequeueLimit;
            settings.RetryDelay = retryDelay == null || retryDelay.Value.TotalMilliseconds <= default(int) ? TimeSpan.FromMinutes(DefaultRetryDelayInMinutes) : retryDelay.Value;
            settings.PreFetchCount = preFetchCount;
            settings.PreFetchSize = preFetchSize;
            settings.StartDelay = startDelay;
        });

        services.AddTransient(typeof(THandler));

        services.Configure<RabbitMQGlobalCommandHandlerSettings<T>>(settings =>
        {
            settings.Type = typeof(THandler);
        });

        services.AddHostedService<RabbitMqCommandBackgroundService<T>>();

        return services;
    }
}