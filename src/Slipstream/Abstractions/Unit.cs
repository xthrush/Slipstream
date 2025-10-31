namespace Slipstream.Abstractions;

/// <summary>A void-like type for request/response signatures.</summary>
public readonly struct Unit
{
    public static readonly Unit Value = new();
}

