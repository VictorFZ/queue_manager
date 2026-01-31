using Microsoft.Extensions.Logging;
using Zeclhynscki.QueueManager.Log.Contracts;

namespace Zeclhynscki.QueueManager.Log;

public class QueueLogger : IQueueLogger
{
    public void LogInformation(string message)
    {
        Log(LogLevel.Information, message);
    }

    public void LogError(Exception exception, string message)
    {
        Log(exception, message);
    }

    public void Log(LogLevel level, string message)
    {
        Console.WriteLine($"[{level.ToString()}] {message}");
    }

    public void Log(Exception exception, string message)
    {
        Console.WriteLine($"[{LogLevel.Error}] {message}\nException: {exception}");
    }
}