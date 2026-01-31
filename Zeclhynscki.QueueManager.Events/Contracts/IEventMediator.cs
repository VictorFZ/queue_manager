using Zeclhynscki.QueueManager.Events.Entities;

namespace Zeclhynscki.QueueManager.Events.Contracts;

public interface IEventMediator
{
    Task Broadcast<T>(T @event) where T : Event;
    Task Broadcast<T>(string eventName, T @event) where T : Event;
}