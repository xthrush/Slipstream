using Slipstream.Abstractions;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Slipstream
{
    public class SlipstreamDispatcher : IDispatcher
    {
        private readonly IServiceProvider _provider;
        private static readonly ConcurrentDictionary<Type, IRequestHandlerWrapper> _handlerCache = new();

        public SlipstreamDispatcher(IServiceProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public Task Send(IRequest request, CancellationToken cancellationToken = default)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));

            var requestType = request.GetType();
            var wrapper = _handlerCache.GetOrAdd(requestType, t =>
            {
                var wrapperType = typeof(VoidRequestHandlerWrapper<>).MakeGenericType(t);
                return (IRequestHandlerWrapper)Activator.CreateInstance(wrapperType)!;
            });

            return ((IVoidRequestHandlerWrapper)wrapper).Handle(request, _provider, cancellationToken);
        }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));

            var requestType = request.GetType();
            var wrapper = _handlerCache.GetOrAdd(requestType, t =>
            {
                var wrapperType = typeof(RequestHandlerWrapper<,>).MakeGenericType(t, typeof(TResponse));
                return (IRequestHandlerWrapper)Activator.CreateInstance(wrapperType)!;
            });

            return ((IRequestHandlerWrapper<TResponse>)wrapper).Handle(request, _provider, cancellationToken);
        }
    }
}
