using System;
using Slipstream.Abstractions;

namespace Slipstream.Behaviors
{
    public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public LoggingBehavior()
        {
        }

        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
        {
            Console.WriteLine($"Handling {typeof(TRequest).Name}");
            var response = await next().ConfigureAwait(false);
            Console.WriteLine($"Handled {typeof(TRequest).Name}");
            return response;
        }
    }
}