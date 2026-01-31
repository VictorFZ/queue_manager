# Queue Manager

[![Ask DeepWiki](https://devin.ai/assets/askdeepwiki.png)](https://deepwiki.com/VictorFZ/queue_manager)

Queue Manager is a .NET library designed to simplify the implementation of Command and Event patterns over message queues. It provides a clean abstraction layer and a ready-to-use RabbitMQ implementation, enabling robust, decoupled, and scalable microservice architectures.

## Core Concepts

The library is built around two main messaging patterns: Commands and Events.

*   **Commands**: Used for imperative actions, where a sender dispatches a command to a specific handler.
*   **Events**: Used for notifications, where a publisher broadcasts an event to multiple subscribers without knowledge of them.

This project provides core interfaces and base classes for these patterns, along with a full RabbitMQ implementation that includes connection management, retry policies, and poison queue handling.

## Features

*   **Command & Event Abstractions**: Simple interfaces (`ICommandHandler`, `IEventHandler`) and base classes (`Command`, `Event`) to build your messages.
*   **Mediator Pattern**: Decouple senders from receivers using `ICommandMediator` and `IEventMediator`.
*   **RabbitMQ Implementation**:
    *   **Connection/Channel Management**: Handles RabbitMQ connections and channels efficiently.
    *   **Automatic Retry and Poison Queues**: Automatically retries failed messages with a configurable delay and count before moving them to a poison queue.
    *   **Delayed Commands**: Schedule commands to be executed after a specific delay.
    *   **Direct & Fanout Exchanges**: Uses direct exchanges for commands and fanout exchanges for events, ensuring correct message routing.
*   **Dependency Injection**: Easy to set up in any .NET application using `IServiceCollection` extension methods.
*   **Structured Logging**: Integrated logging for queue operations.

## Project Structure

The solution is divided into several projects, each with a specific responsibility:

| Project                                     | Description                                                                          |
| ------------------------------------------- | ------------------------------------------------------------------------------------ |
| `Zeclhynscki.QueueManager.Common`           | Contains common base classes like `QueueMessage`.                                    |
| `Zeclhynscki.QueueManager.Commands`         | Defines the core abstractions for the Command pattern (`Command`, `ICommandHandler`).  |
| `Zeclhynscki.QueueManager.Events`           | Defines the core abstractions for the Event pattern (`Event`, `IEventHandler`).        |
| `Zeclhynscki.QueueManager.RabbitMq`         | Provides common RabbitMQ utilities, including the `IRabbitMqChannelProvider`.        |
| `Zeclhynscki.QueueManager.Commands.RabbitMq`| Implements the Command pattern using RabbitMQ, including publishers and listeners.   |
| `Zeclhynscki.QueueManager.Events.RabbitMq`  | Implements the Event pattern using RabbitMQ, including publishers and listeners.     |
| `Zeclhynscki.QueueManager.Log`              | Provides logging interfaces and implementations for queue-related activities.        |

## Getting Started

Follow these steps to integrate Queue Manager into your .NET application.

### 1. Configuration

First, configure the RabbitMQ connection and register the channel provider in your `Program.cs` or `Startup.cs`.

```csharp
// using Zeclhynscki.QueueManager.RabbitMq.Extensions;
// using Zeclhynscki.QueueManager.RabbitMq.Providers;

var rabbitSettings = new RabbitMqDefaultProviderConnectionSettings
{
    HostName = "your-rabbitmq-host",
    UserName = "user",
    Password = "password",
    Port = 5672,
    UseSsl = false
};

builder.Services.AddRabbitMqChannelProvider(rabbitSettings);
```

### 2. Register Core Services

Register the logger and mediators. The mediators are the entry points for sending commands and broadcasting events.

```csharp
// using Zeclhynscki.QueueManager.Log.Extensions;
// using Zeclhynscki.QueueManager.Commands.RabbitMq.Extensions;
// using Zeclhynscki.QueueManager.Events.RabbitMq.Extensions;

builder.Services.RegisterQueueLogger();
builder.Services.AddRabbitMqCommandMediator();
builder.Services.AddRabbitMqGlobalEventMediator();
```

### 3. Using Commands

**a. Define a Command**

Create a class that inherits from `Command` and decorate it with the `CommandAttribute`.

```csharp
// using Zeclhynscki.QueueManager.Commands.Entities;
// using Zeclhynscki.QueueManager.Commands.Entities.Attributes;

[Command("process-data", "1.0")]
public class ProcessDataCommand : Command
{
    public string Data { get; set; }

    public override bool IsValid() => !string.IsNullOrWhiteSpace(Data);
}
```

**b. Create a Command Handler**

Implement `ICommandHandler<T>` to process the command.

```csharp
// using Zeclhynscki.QueueManager.Commands.Contracts;

public class ProcessDataCommandHandler : ICommandHandler<ProcessDataCommand>
{
    public Task<bool> Handle(ProcessDataCommand command, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Processing data: {command.Data}");
        // Return true on success, false on failure (to trigger a retry)
        return Task.FromResult(true);
    }
}
```

**c. Register the Command Listener**

In your DI setup, register the command handler. This will automatically set up a background service to listen for and process incoming commands.

```csharp
// using Zeclhynscki.QueueManager.Commands.RabbitMq.Extensions;

builder.Services.RegisterRabbitMqCommandListener<ProcessDataCommand, ProcessDataCommandHandler>(
    dequeueLimit: 5,
    retryDelay: TimeSpan.FromMinutes(2),
    preFetchCount: 1
);
```

**d. Send a Command**

Inject `ICommandMediator` and use the `Send` method to dispatch your command. You can optionally specify a delay.

```csharp
// using Zeclhynscki.QueueManager.Commands.Contracts;

public class MyService(ICommandMediator commandMediator)
{
    public async Task DoWork()
    {
        var command = new ProcessDataCommand { Data = "Important information" };
        
        // Send immediately
        await commandMediator.Send(command);

        // Send with a 30-second delay
        await commandMediator.Send(command, delay: TimeSpan.FromSeconds(30));
    }
}
```

### 4. Using Events

**a. Define an Event**

Create a class that inherits from `Event` and decorate it with `EventAttribute` and `EventQueueAttribute`. `EventQueue` links the event to a specific consumer queue.

```csharp
// using Zeclhynscki.QueueManager.Events.Entities;
// using Zeclhynscki.QueueManager.Events.Entities.Attributes;

[Event("user-created", "1.0")]
[EventQueue("user-created", "1.0", "analytics-service-queue")]
public class UserCreatedEvent : Event
{
    public Guid UserId { get; set; }
    public string Email { get; set; }
}
```

**b. Create an Event Handler**

Implement `IEventHandler<T>` to react to the event.

```csharp
// using Zeclhynscki.QueueManager.Events.Contracts;

public class UserCreatedEventHandler : IEventHandler<UserCreatedEvent>
{
    public Task<bool> Handle(UserCreatedEvent @event, CancellationToken cancellationToken)
    {
        Console.WriteLine($"New user created: {@event.UserId}, email: {@event.Email}");
        // Return true on success, false on failure (to trigger a retry)
        return Task.FromResult(true);
    }
}
```

**c. Register the Event Listener**

In your DI setup, register the event handler. Pass the same `queueName` used in the `EventQueueAttribute`. The background service will listen for events on the fanout exchange and process them from the dedicated queue.

```csharp
// using Zeclhynscki.QueueManager.Events.RabbitMq.Extensions;

builder.Services.RegisterRabbitMQGlobalEventListener<UserCreatedEvent, UserCreatedEventHandler>(
    queueName: "analytics-service-queue",
    dequeueLimit: 3,
    retryDelay: TimeSpan.FromMinutes(5)
);
```

**d. Broadcast an Event**

Inject `IEventMediator` and use the `Broadcast` method to publish your event to all subscribers.

```csharp
// using Zeclhynscki.QueueManager.Events.Contracts;

public class MyUserService(IEventMediator eventMediator)
{
    public async Task CreateUser(string email)
    {
        // ... user creation logic ...

        var @event = new UserCreatedEvent 
        { 
            UserId = Guid.NewGuid(), 
            Email = email 
        };

        await eventMediator.Broadcast(@event);
    }
}
