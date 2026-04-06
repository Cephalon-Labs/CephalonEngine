namespace Cephalon.Abstractions.Behaviors;

/// <summary>Transport-neutral ambient API available inside a behavior handler. Provides correlation, publishing, and saga state without coupling to a specific transport.</summary>
public interface IBehaviorContext
{
    /// <summary>Gets the behavior identifier being executed.</summary>
    string BehaviorId { get; }

    /// <summary>Gets the correlation identifier for the current request.</summary>
    string? CorrelationId { get; }

    /// <summary>Gets the tenant identifier for the current request.</summary>
    string? TenantId { get; }

    /// <summary>Gets the user identifier for the current request.</summary>
    string? UserId { get; }

    /// <summary>Gets the trace identifier for the current request.</summary>
    string? TraceId { get; }

    /// <summary>Gets the cancellation token for the current request.</summary>
    CancellationToken CancellationToken { get; }

    /// <summary>Gets the fault set by the execution strategy on error, or <see langword="null"/> if no fault occurred.</summary>
    BehaviorFault? Fault { get; }

    /// <summary>Gets additional metadata associated with the current request.</summary>
    IReadOnlyDictionary<string, string> Metadata { get; }

    /// <summary>Publishes a domain event to all configured transport bindings for this behavior.</summary>
    Task PublishAsync<TEvent>(TEvent evt, CancellationToken ct = default);

    /// <summary>Sends a command to another behavior.</summary>
    Task SendAsync<TCommand>(TCommand command, CancellationToken ct = default);

    /// <summary>Replies with a result. Only valid in CQRS and saga patterns.</summary>
    Task ReplyAsync<TResult>(TResult result, CancellationToken ct = default);

    /// <summary>Gets saga state of type <typeparamref name="T"/>, or <see langword="null"/> if not set.</summary>
    T? GetSagaState<T>();

    /// <summary>Sets saga state of type <typeparamref name="T"/>.</summary>
    void SetSagaState<T>(T state);
}
