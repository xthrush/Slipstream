using System;
using System.Transactions;
using Slipstream.Abstractions;

namespace Slipstream.Behaviors
{
    public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
        {
            var useTx = request.GetType().GetCustomAttributes(typeof(UseTransactionAttribute), inherit: false).FirstOrDefault() as UseTransactionAttribute;

            if (useTx is null)
            {
                return await next().ConfigureAwait(false);
            }

            var options = new TransactionOptions { IsolationLevel = useTx.IsolationLevel };
            if (useTx.Timeout.HasValue)
            {
                options.Timeout = useTx.Timeout.Value;
            }

            using var scope = new TransactionScope(TransactionScopeOption.Required, options, TransactionScopeAsyncFlowOption.Enabled);

            var response = await next().ConfigureAwait(false);

            scope.Complete();

            return response;
        }
    }
}
