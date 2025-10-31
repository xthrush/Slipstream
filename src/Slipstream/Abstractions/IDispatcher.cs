namespace Slipstream.Abstractions;

/// <summary>Dispatches requests through the configured pipeline to their handler.</summary>
public interface IDispatcher
{
    Task Send(IRequest request, CancellationToken cancellationToken = default);
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}

