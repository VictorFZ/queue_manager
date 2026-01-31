using Zeclhynscki.QueueManager.Commands.Entities;

namespace Zeclhynscki.QueueManager.Commands.Contracts;

public interface ICommandHandler<TCommand> where TCommand : Command
{
    Task<bool> Handle(TCommand command, CancellationToken cancellationToken = default);
}