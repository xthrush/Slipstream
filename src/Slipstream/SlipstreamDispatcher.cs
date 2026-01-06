using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Slipstream.Abstractions;

namespace Slipstream
{
    public class SlipstreamDispatcher : IDispatcher
    {
        private readonly IServiceProvider _provider;

        public SlipstreamDispatcher(IServiceProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public Task Send(IRequest request, CancellationToken cancellationToken = default)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));

            var requestType = request.GetType();
            var handlerType = typeof(IRequestHandler<>).MakeGenericType(requestType);
            var handler = _provider.GetService(handlerType) ?? throw new InvalidOperationException($"No handler registered for {requestType}");

            var handleMethod = handlerType.GetMethod("Handle", BindingFlags.Public | BindingFlags.Instance)
                                ?? throw new InvalidOperationException("Handler does not implement Handle method");

            var task = (Task)handleMethod.Invoke(handler, new object[] { request, cancellationToken })!;
            return task;
        }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));

            var requestType = request.GetType();

            var handlerInterface = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));
            var handler = _provider.GetService(handlerInterface) ?? throw new InvalidOperationException($"No handler registered for {requestType} -> {typeof(TResponse)}");

            RequestHandlerDelegate<TResponse> baseDelegate = () =>
            {
                var handle = handlerInterface.GetMethod("Handle")!;
                var result = handle.Invoke(handler, new object[] { request, cancellationToken });
                return (Task<TResponse>)result!;
            };

            var behaviorInterface = typeof(IPipelineBehavior<,>).MakeGenericType(requestType, typeof(TResponse));
            var enumerableInterface = typeof(IEnumerable<>).MakeGenericType(behaviorInterface);
            var behaviorsObj = _provider.GetService(enumerableInterface);

            if (behaviorsObj is IEnumerable behaviors)
            {
                var next = baseDelegate;
                var behaviorList = behaviors.Cast<object>().ToArray();

                for (int i = behaviorList.Length - 1; i >= 0; i--)
                {
                    var behavior = behaviorList[i];
                    var capturedNext = next;

                    RequestHandlerDelegate<TResponse> wrapper = () =>
                    {
                        var handle = behavior.GetType().GetMethod("Handle")!;
                        var result = handle.Invoke(behavior, new object[] { request, cancellationToken, capturedNext });
                        return (Task<TResponse>)result!;
                    };

                    next = wrapper;
                }

                return next();
            }

            return baseDelegate();
        }
    }
}
