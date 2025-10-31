namespace Slipstream.Abstractions;

/// <summary>Marker interface for a request without a return type.</summary>
public interface IRequest { }

/// <summary>Marker interface for a request with a return type.</summary>
public interface IRequest<out TResponse> { }

