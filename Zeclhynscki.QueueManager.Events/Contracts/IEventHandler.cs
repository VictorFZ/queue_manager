using Zeclhynscki.QueueManager.Events.Entities;

namespace Zeclhynscki.QueueManager.Events.Contracts;

public interface IEventHandler<TEvent> where TEvent : Event
{
    Task<bool> Handle(TEvent @event, CancellationToken cancellationToken = default);
}