using Newtonsoft.Json;
using Zeclhynscki.QueueManager.Log.Contracts;

namespace Zeclhynscki.QueueManager.Log
{
    public class ConsoleRuntimeLogger : IRuntimeLogger
    {
        public void Log(Exception exception, params KeyValuePair<string, object>[] objectsOfInterest)
        {
            Console.WriteLine($"\n[Exception Log Begin ({DateTime.Now.ToUniversalTime():dd/MM/yyyy HH:mm:ss})]:\n\nMessage: {exception.Message}\nStack Trace: {exception.StackTrace}\n");

            LogObjectsOfInterest(objectsOfInterest);

            Console.WriteLine("[Exception Log End]\n");
        }

        public void Log(string message, params KeyValuePair<string, object>[] objectsOfInterest)
        {
            Console.WriteLine($"\n[Log Begin ({DateTime.Now.ToUniversalTime():dd/MM/yyyy HH:mm:ss})]:\n\nMessage: {message}\n");

            LogObjectsOfInterest(objectsOfInterest);

            Console.WriteLine("[Log End]\n");
        }

        public Task LogAsync(Exception exception, params KeyValuePair<string, object>[] objectsOfInterest)
        {
            Console.WriteLine($"\n[Exception Log Begin ({DateTime.Now.ToUniversalTime():dd/MM/yyyy HH:mm:ss})]:\n\nMessage: {exception.Message}\nStack Trace: {exception.StackTrace}\n");

            LogObjectsOfInterest(objectsOfInterest);

            Console.WriteLine("[Exception Log End]\n");

            return Task.CompletedTask;
        }

        public Task LogAsync(string message, params KeyValuePair<string, object>[] objectsOfInterest)
        {
            Console.WriteLine($"\n[Log Begin ({DateTime.Now.ToUniversalTime():dd/MM/yyyy HH:mm:ss})]:\n\nMessage: {message}\n");

            LogObjectsOfInterest(objectsOfInterest);

            Console.WriteLine("[Log End]\n");

            return Task.CompletedTask;
        }

        protected Task LogObjectsOfInterest(KeyValuePair<string, object>[] objectsOfInterest)
        {
            if(objectsOfInterest == null || !objectsOfInterest.Any())
                return Task.CompletedTask;

            Console.WriteLine($"\nObjects of interest:\n");

            var index = 0;

            foreach (var objectOfInterest in objectsOfInterest)
            {
                Console.WriteLine($"#{++index}: {objectOfInterest.Key}\n{JsonConvert.SerializeObject(objectOfInterest.Value)}\n");
            }

            return Task.CompletedTask;
        }
    }
}
