namespace Zeclhynscki.QueueManager.Commands.Entities.Attributes;

public class CommandAttribute(string commandName, string commandVersion) : Attribute
{
    public string CommandName { get; } = commandName;
    public string CommandVersion { get; set; } = commandVersion;
}