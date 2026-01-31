namespace Zeclhynscki.QueueManager.Events.Exceptions;

public class MissingEventAttribute(Type classType)
    : Exception($"Missing Attribute EventAttribute for type class {classType.Name}");