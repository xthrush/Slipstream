using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Slipstream.Abstractions
{
    // Marker interface for the cache dictionary
    internal interface IRequestHandlerWrapper { }

    internal interface IVoidRequestHandlerWrapper : IRequestHandlerWrapper
    {
        Task Handle(IRequest request, IServiceProvider provider, CancellationToken cancellationToken);
    }

    internal interface IRequestHandlerWrapper<TResponse> : IVoidRequestHandlerWrapper
    {
        Task<TResponse> Handle(IRequest<TResponse> request, IServiceProvider provider, CancellationToken cancellationToken);
    }

    // Handles IRequest (no response) - now includes pipeline support
    internal class VoidRequestHandlerWrapper<TRequest> : IRequestHandlerWrapper
        where TRequest : IRequest
    {
        public Task Handle(IRequest request, IServiceProvider provider, CancellationToken cancellationToken)
        {
            var handler = provider.GetService<IRequestHandler<TRequest>>()
                ?? throw new InvalidOperationException($"No handler registered for {typeof(TRequest)}");

            var typedRequest = (TRequest)request;

            // Pipeline support for void requests
            RequestHandlerDelegate<Unit> baseDelegate = async () =>
            {
                await handler.Handle(typedRequest, cancellationToken);
                return Unit.Value;
            };

            IEnumerable<IPipelineBehavior<TRequest, Unit>> behaviors = provider.GetService<IEnumerable<IPipelineBehavior<TRequest, Unit>>>()
                            ?? Enumerable.Empty<IPipelineBehavior<TRequest, Unit>>();

            var next = behaviors
                .Reverse()
                .Aggregate(baseDelegate, (nextDelegate, behavior) => () =>
                    behavior.Handle(typedRequest, cancellationToken, nextDelegate));

            return next();
        }
    }

    // Handles IRequest<TResponse>
    internal class RequestHandlerWrapper<TRequest, TResponse> : IRequestHandlerWrapper<TResponse>
        where TRequest : IRequest<TResponse>
    {
        // Satisfies IRequestHandlerWrapper base interface - never called directly
        public Task Handle(IRequest request, IServiceProvider provider, CancellationToken cancellationToken)
            => throw new InvalidOperationException("Use the generic Send<TResponse> overload.");

        public Task<TResponse> Handle(IRequest<TResponse> request, IServiceProvider provider, CancellationToken cancellationToken)
        {
            var handler = provider.GetService<IRequestHandler<TRequest, TResponse>>()
                ?? throw new InvalidOperationException($"No handler registered for {typeof(TRequest)} -> {typeof(TResponse)}");

            var typedRequest = (TRequest)request;

            RequestHandlerDelegate<TResponse> baseDelegate = () =>
                handler.Handle(typedRequest, cancellationToken);

            var behaviors = provider.GetService<IEnumerable<IPipelineBehavior<TRequest, TResponse>>>()
                            ?? Enumerable.Empty<IPipelineBehavior<TRequest, TResponse>>();

            var next = behaviors
                .Reverse()
                .Aggregate(baseDelegate, (nextDelegate, behavior) => () =>
                    behavior.Handle(typedRequest, cancellationToken, nextDelegate));

            return next();
        }
    }
}
