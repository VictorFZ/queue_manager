using Zeclhynscki.QueueManager.Events.Entities.Attributes;

namespace Zeclhynscki.QueueManager.Events.Exceptions;

public class MissingEventQueueAttribute(Type classType) : Exception(
    $"Missing {nameof(EventQueueAttribute)} or {nameof(EventAttribute)} for type class {classType.Name}");