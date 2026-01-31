using Microsoft.Extensions.Logging;
using Zeclhynscki.QueueManager.Log.Contracts;

namespace Zeclhynscki.QueueManager.Log
{
    public class MicrosoftRuntimeLogger : IRuntimeLogger
    {
        private readonly ILogger<MicrosoftRuntimeLogger> _logger;

        public MicrosoftRuntimeLogger(ILogger<MicrosoftRuntimeLogger> logger)
        {
            _logger = logger;
        }
        public void Log(Exception exception, params KeyValuePair<string, object>[] objectsOfInterest)
        {
            _logger.LogError(exception, exception.Message, objectsOfInterest);
           
        }

        public void Log(string message, params KeyValuePair<string, object>[] objectsOfInterest)
        {
            _logger.LogInformation(message, objectsOfInterest);
        }

        public Task LogAsync(Exception exception, params KeyValuePair<string, object>[] objectsOfInterest)
        {
            _logger.LogError(exception, exception.Message, objectsOfInterest);

            return Task.CompletedTask;
        }

        public Task LogAsync(string message, params KeyValuePair<string, object>[] objectsOfInterest)
        {
            _logger.LogInformation(message, objectsOfInterest);

            return Task.CompletedTask;
        }
    }
}
