using System.Reflection;

namespace Zeclhynscki.QueueManager.Events.Exceptions;

public class MissingQueueName(MemberInfo classType) : Exception($"Missing QueueName for {classType.Name}.");