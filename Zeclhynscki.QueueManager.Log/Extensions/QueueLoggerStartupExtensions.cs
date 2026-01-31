using Microsoft.Extensions.DependencyInjection;
using Zeclhynscki.QueueManager.Log.Contracts;

namespace Zeclhynscki.QueueManager.Log.Extensions;

public static class QueueLoggerStartupExtensions
{
    public static IServiceCollection RegisterQueueLogger(this IServiceCollection services)
    {
        services.AddSingleton<IQueueLogger, QueueLogger>();

        return services;
    }
}