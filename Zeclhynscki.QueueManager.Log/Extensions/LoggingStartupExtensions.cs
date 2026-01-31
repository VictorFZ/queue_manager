using Microsoft.Extensions.DependencyInjection;
using Zeclhynscki.QueueManager.Log.Contracts;

namespace Zeclhynscki.QueueManager.Log.Extensions
{
    public static class LoggingStartupExtensions
    {
        public static IServiceCollection AddConsoleRuntimeLoggerOptions(this IServiceCollection services)
        {
            services.AddSingleton<IRuntimeLogger, ConsoleRuntimeLogger>();

            return services;
        }
        public static IServiceCollection AddMicrosoftRuntimeLoggerOptions(this IServiceCollection services)
        {
            services.AddSingleton<IRuntimeLogger, MicrosoftRuntimeLogger>();

            return services;
        }
    }
}
