namespace Slipstream.Abstractions;

/// <summary>Handles exceptions thrown while processing a request.</summary>
public interface IRequestExceptionHandler<in TRequest, TResponse, in TException>
    where TRequest : IRequest<TResponse>
    where TException : Exception
{
    Task Handle(
        TRequest request,
        TException exception,
        RequestExceptionHandlerState<TResponse> state,
        CancellationToken cancellationToken);
}

/// <summary>Mutable state used by exception handlers to mark a request as handled.</summary>
public sealed class RequestExceptionHandlerState<TResponse>
{
    private bool _handled;
    private TResponse? _response;

    public bool IsHandled => _handled;
    public TResponse? Response => _response;

    public void SetHandled(TResponse response)
    {
        _handled = true;
        _response = response;
    }
}

