namespace Zeclhynscki.QueueManager.RabbitMq.Providers;

public class RabbitMqDefaultProviderConnectionSettings
{
    public string HostName { get; set; }
    public int Port { get; set; }
    public string Password { get; set; }
    public string UserName { get; set; }
    public bool UseSsl { get; set; }
}