using Microsoft.Extensions.Logging;

namespace Zeclhynscki.QueueManager.Log.Contracts;

public interface IQueueLogger
{
    void LogInformation(string message);
    void LogError(Exception exception, string message);
    void Log(LogLevel level, string message);
    void Log(Exception exception, string message);
}