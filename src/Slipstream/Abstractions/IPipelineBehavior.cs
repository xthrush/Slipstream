namespace Slipstream.Abstractions;

/// <summary>Pipeline behavior invoked before/after the handler.</summary>
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(
        TRequest request,
        CancellationToken cancellationToken,
        RequestHandlerDelegate<TResponse> next);
}

/// <summary>Delegate that executes the next behavior/handler in the chain.</summary>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();

