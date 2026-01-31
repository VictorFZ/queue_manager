using Zeclhynscki.QueueManager.Commands.Entities;

namespace Zeclhynscki.QueueManager.Commands.Contracts;

public interface ICommandMediator
{
    Task Send<T>(T command, TimeSpan? delay = null) where T : Command;
    Task Send<T>(string commandName, T command, TimeSpan? delay = null) where T : Command;
}