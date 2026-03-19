

# Slipstream

**Fast, lean CQRS dispatcher with pipeline behaviors.**

Slipstream is a lightweight alternative to MediatR focused on doing one thing well — dispatching requests through a pipeline to a single handler. No event publishing, no bloat, just clean request/response dispatching.

---

## Features

- Request/response dispatching via `IDispatcher`
- Void request support (fire-and-forget style)
- Pipeline behaviors for cross-cutting concerns (logging, validation, transactions, etc.)
- Reflection-free hot path via cached generic wrappers
- No dependencies beyond `Microsoft.Extensions.DependencyInjection.Abstractions`

---

## Installation

```bash
dotnet add package Slipstream
```

---

## Quick Start

### 1. Define a request

```csharp
// With a response
public record GetOrderById(int Id) : IRequest<Order>;

// Without a response
public record DeleteOrder(int Id) : IRequest;
```

### 2. Implement a handler

```csharp
public class GetOrderByIdHandler : IRequestHandler<GetOrderById, Order>
{
    public Task<Order> Handle(GetOrderById request, CancellationToken cancellationToken)
    {
        // your logic here
        return Task.FromResult(new Order(request.Id));
    }
}

public class DeleteOrderHandler : IRequestHandler<DeleteOrder>
{
    public Task Handle(DeleteOrder request, CancellationToken cancellationToken)
    {
        // your logic here
        return Task.CompletedTask;
    }
}
```

### 3. Register with your DI container

```csharp
// Register the dispatcher
services.AddTransient<IDispatcher, SlipstreamDispatcher>();

// Register handlers manually...
services.AddTransient<IRequestHandler<GetOrderById, Order>, GetOrderByIdHandler>();

// ...or use assembly scanning
services.RegisterHandlersAndBehaviors(typeof(GetOrderByIdHandler).Assembly);
```

### 4. Dispatch

```csharp
public class OrdersController
{
    private readonly IDispatcher _dispatcher;

    public OrdersController(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public async Task<Order> Get(int id)
    {
        return await _dispatcher.Send(new GetOrderById(id));
    }
}
```

---

## Pipeline Behaviors

Behaviors wrap handler execution and are ideal for logging, validation, exception handling, and transactions.

```csharp
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        CancellationToken cancellationToken,
        RequestHandlerDelegate<TResponse> next)
    {
        _logger.LogInformation("Handling {Request}", typeof(TRequest).Name);
        var response = await next();
        _logger.LogInformation("Handled {Request}", typeof(TRequest).Name);
        return response;
    }
}
```

Register behaviors with your DI container:

```csharp
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
```

Behaviors execute in registration order, wrapping the handler like an onion:

```
Behavior1 -> Behavior2 -> Handler -> Behavior2 -> Behavior1
```

---

## Assembly Scanning

Slipstream includes a registration helper to scan assemblies and register all handlers and behaviors automatically:

```csharp
// Using the IRegistrar abstraction (container-agnostic)
var registrar = new DelegateRegistrar(
    (service, impl) => services.AddTransient(service, impl),
    (service, impl) => services.AddTransient(service, impl)
);

registrar.RegisterHandlersAndBehaviors(typeof(MyHandler).Assembly);
```

---

## Why Not MediatR?

MediatR is excellent but carries features many projects don't need — notifications, polymorphic dispatch, and a growing surface area. Slipstream is for teams that want the request/pipeline pattern without the overhead.

|                          | Slipstream       | MediatR          |
|--------------------------|------------------|------------------|
| Request/Response         | ✅               | ✅               |
| Pipeline Behaviors       | ✅               | ✅               |
| Notifications            | ❌ intentional   | ✅               |
| Reflection-free hot path | ✅               | ✅ (v12+)        |
| Dependencies             | Abstractions only| Abstractions only|

---

## License

MIT