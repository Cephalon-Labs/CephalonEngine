namespace Cephalon.Behaviors.Patterns.Abstractions;

/// <summary>The result of a pattern-executed behavior invocation.</summary>
public sealed class BehaviorExecutionResult
{
    /// <summary>Gets the output value (may be null for fire-and-forget patterns).</summary>
    public object? Output { get; init; }

    /// <summary>Gets the HTTP status code hint for HTTP transport bindings.</summary>
    public int HttpStatusCode { get; init; } = 200;

    /// <summary>Gets a value indicating whether this was a fire-and-forget invocation.</summary>
    public bool IsFireAndForget { get; init; }
}
