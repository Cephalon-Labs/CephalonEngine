namespace Cephalon.Engine.Runtime;

/// <summary>
/// Captures the current runtime lifecycle state in a serialization-friendly form.
/// </summary>
/// <param name="Status">The current lifecycle status.</param>
/// <param name="InitializedAtUtc">The UTC timestamp when initialization completed, if it has completed.</param>
/// <param name="StartedAtUtc">The UTC timestamp when startup completed, if it has completed.</param>
/// <param name="StoppedAtUtc">The UTC timestamp when the runtime last transitioned to a stopped state, if any.</param>
/// <param name="RestartCount">The number of completed manual restarts.</param>
/// <param name="LastFailure">The last captured failure, if the runtime has faulted.</param>
public sealed record RuntimeStatusSnapshot(
    RuntimeStatus Status,
    DateTimeOffset? InitializedAtUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? StoppedAtUtc,
    int RestartCount,
    RuntimeFailureInfo? LastFailure);
