using System.Reflection;
using Newtonsoft.Json;
using Zeclhynscki.QueueManager.Commands.Contracts;
using Zeclhynscki.QueueManager.Commands.Entities;
using Zeclhynscki.QueueManager.Commands.Entities.Attributes;
using Zeclhynscki.QueueManager.Commands.Exceptions;
using Zeclhynscki.QueueManager.Commands.RabbitMq.Publisher.Contracts;

namespace Zeclhynscki.QueueManager.Commands.RabbitMq;

public class RabbitMqCommandMediator(IRabbitMqCommandPublisher rabbitMqCommandPublisher) : ICommandMediator
{
    private const string CommandSuffix = "command";

    public async Task Send<T>(T command, TimeSpan? delay = null) where T : Command
    {
        var commandAttr = typeof(T).GetCustomAttribute<CommandAttribute>();

        if (commandAttr == null)
        {
            throw new MissingCommandAttribute(typeof(T));
        }

        var commandName = commandAttr.CommandName;

        if (!string.IsNullOrEmpty(commandAttr.CommandVersion))
        {
            commandName = $"{commandAttr.CommandVersion.Replace(".", "-")}-{commandName}";
        }

        commandName = $"{commandName}-{CommandSuffix}";

        await rabbitMqCommandPublisher.Publish(commandName, JsonConvert.SerializeObject(command,
            new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            }), delay);
    }

    public async Task Send<T>(string commandName, T command, TimeSpan? delay = null) where T : Command
    {
        commandName = $"{commandName}-{CommandSuffix}";

        await rabbitMqCommandPublisher.Publish(commandName, JsonConvert.SerializeObject(command,
            new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            }), delay);
    }
}