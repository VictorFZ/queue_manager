using Zeclhynscki.QueueManager.Commands.Entities.Attributes;

namespace Zeclhynscki.QueueManager.Commands.Exceptions;

public class MissingCommandAttribute(Type classType)
    : Exception($"Missing Attribute {nameof(CommandAttribute)} for type class {classType.Name}");