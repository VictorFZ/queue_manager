namespace Zeclhynscki.QueueManager.Log.Contracts
{
    public interface IRuntimeLogger
    {
        void Log(Exception exception, params KeyValuePair<string, object>[] objectsOfInterest);
        void Log(string message, params KeyValuePair<string, object>[] objectsOfInterest);
        Task LogAsync(Exception exception, params KeyValuePair<string, object>[] objectsOfInterest);
        Task LogAsync(string message, params KeyValuePair<string, object>[] objectsOfInterest);
    }
}
