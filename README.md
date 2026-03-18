# \# Slipstream

# 

# \*\*Fast, lean CQRS dispatcher with pipeline behaviors.\*\*

# 

# Slipstream is a lightweight alternative to MediatR focused on doing one thing well — dispatching requests through a pipeline to a single handler. No event publishing, no bloat, just clean request/response dispatching.

# 

# \---

# 

# \## Features

# 

# \- Request/response dispatching via `IDispatcher`

# \- Void request support (fire-and-forget style)

# \- Pipeline behaviors for cross-cutting concerns (logging, validation, transactions, etc.)

# \- Reflection-free hot path via cached generic wrappers

# \- No dependencies beyond `Microsoft.Extensions.DependencyInjection.Abstractions`

# 

# \---

# 

# \## Installation

# 

# ```bash

# dotnet add package Slipstream

# ```

# 

# \---

# 

# \## Quick Start

# 

# \### 1. Define a request

# 

# ```csharp

# // With a response

# public record GetOrderById(int Id) : IRequest<Order>;

# 

# // Without a response

# public record DeleteOrder(int Id) : IRequest;

# ```

# 

# \### 2. Implement a handler

# 

# ```csharp

# public class GetOrderByIdHandler : IRequestHandler<GetOrderById, Order>

# {

# &#x20;   public Task<Order> Handle(GetOrderById request, CancellationToken cancellationToken)

# &#x20;   {

# &#x20;       // your logic here

# &#x20;       return Task.FromResult(new Order(request.Id));

# &#x20;   }

# }

# 

# public class DeleteOrderHandler : IRequestHandler<DeleteOrder>

# {

# &#x20;   public Task Handle(DeleteOrder request, CancellationToken cancellationToken)

# &#x20;   {

# &#x20;       // your logic here

# &#x20;       return Task.CompletedTask;

# &#x20;   }

# }

# ```

# 

# \### 3. Register with your DI container

# 

# ```csharp

# // Register the dispatcher

# services.AddTransient<IDispatcher, SlipstreamDispatcher>();

# 

# // Register handlers manually...

# services.AddTransient<IRequestHandler<GetOrderById, Order>, GetOrderByIdHandler>();

# 

# // ...or use assembly scanning

# services.RegisterHandlersAndBehaviors(typeof(GetOrderByIdHandler).Assembly);

# ```

# 

# \### 4. Dispatch

# 

# ```csharp

# public class OrdersController

# {

# &#x20;   private readonly IDispatcher \_dispatcher;

# 

# &#x20;   public OrdersController(IDispatcher dispatcher)

# &#x20;   {

# &#x20;       \_dispatcher = dispatcher;

# &#x20;   }

# 

# &#x20;   public async Task<Order> Get(int id)

# &#x20;   {

# &#x20;       return await \_dispatcher.Send(new GetOrderById(id));

# &#x20;   }

# }

# ```

# 

# \---

# 

# \## Pipeline Behaviors

# 

# Behaviors wrap handler execution and are ideal for logging, validation, exception handling, and transactions.

# 

# ```csharp

# public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>

# &#x20;   where TRequest : IRequest<TResponse>

# {

# &#x20;   private readonly ILogger<LoggingBehavior<TRequest, TResponse>> \_logger;

# 

# &#x20;   public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)

# &#x20;   {

# &#x20;       \_logger = logger;

# &#x20;   }

# 

# &#x20;   public async Task<TResponse> Handle(

# &#x20;       TRequest request,

# &#x20;       CancellationToken cancellationToken,

# &#x20;       RequestHandlerDelegate<TResponse> next)

# &#x20;   {

# &#x20;       \_logger.LogInformation("Handling {Request}", typeof(TRequest).Name);

# &#x20;       var response = await next();

# &#x20;       \_logger.LogInformation("Handled {Request}", typeof(TRequest).Name);

# &#x20;       return response;

# &#x20;   }

# }

# ```

# 

# Register behaviors with your DI container:

# 

# ```csharp

# services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

# ```

# 

# Behaviors execute in registration order, wrapping the handler like an onion:

# 

# ```

# Behavior1 -> Behavior2 -> Handler -> Behavior2 -> Behavior1

# ```

# 

# \---

# 

# \## Assembly Scanning

# 

# Slipstream includes a registration helper to scan assemblies and register all handlers and behaviors automatically:

# 

# ```csharp

# // Using the IRegistrar abstraction (container-agnostic)

# var registrar = new DelegateRegistrar(

# &#x20;   (service, impl) => services.AddTransient(service, impl),

# &#x20;   (service, impl) => services.AddTransient(service, impl)

# );

# 

# registrar.RegisterHandlersAndBehaviors(typeof(MyHandler).Assembly);

# ```

# 

# \---

# 

# \## Why Not MediatR?

# 

# MediatR is excellent but carries features many projects don't need — notifications, polymorphic dispatch, and a growing surface area. Slipstream is for teams that want the request/pipeline pattern without the overhead.

# 

# | | Slipstream | MediatR |

# |---|---|---|

# | Request/Response | ✅ | ✅ |

# | Pipeline Behaviors | ✅ | ✅ |

# | Notifications | ❌ intentional | ✅ |

# | Reflection-free hot path | ✅ | ✅ (v12+) |

# | Dependencies | Abstractions only | Abstractions only |

# 

# \---

# 

# \## License

# 

# MIT

