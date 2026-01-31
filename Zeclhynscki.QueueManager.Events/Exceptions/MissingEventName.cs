using System.Reflection;

namespace Zeclhynscki.QueueManager.Events.Exceptions;

public class MissingEventName(MemberInfo classType) : Exception($"Missing EventName for {classType.Name}.");