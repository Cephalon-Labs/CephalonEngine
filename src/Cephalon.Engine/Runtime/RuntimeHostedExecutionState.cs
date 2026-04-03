namespace Cephalon.Engine.Runtime;

/// <summary>
/// Describes the current operator-facing lifecycle state for one hosted execution visible to the runtime.
/// </summary>
/// <param name="HostedExecutionId">The stable hosted-execution identifier.</param>
/// <param name="DisplayName">The operator-facing hosted-execution display name.</param>
/// <param name="Description">The operator-facing hosted-execution description when one was published.</param>
/// <param name="SourceModuleId">The module that contributed the hosted execution.</param>
/// <param name="SourceModuleVersion">The effective version of the source module when available.</param>
/// <param name="Kind">The operator-facing hosted-execution kind.</param>
/// <param name="ExecutionGraphId">The related execution-graph identifier when one was declared.</param>
/// <param name="StartsWithHost">A value indicating whether the hosted execution is expected to become active when the runtime host starts.</param>
/// <param name="LoadedAtUtc">The UTC timestamp when the hosted execution became visible to the built runtime story.</param>
/// <param name="ActivatedAtUtc">The UTC timestamp when the hosted execution most recently became active with the runtime.</param>
/// <param name="DeactivatedAtUtc">The UTC timestamp when the hosted execution most recently became inactive because the runtime stopped.</param>
/// <param name="LastObservedPhase">The last lifecycle phase recorded for the hosted execution.</param>
/// <param name="LastObservedAtUtc">The UTC timestamp when the last lifecycle phase was recorded.</param>
public sealed record RuntimeHostedExecutionState(
    string HostedExecutionId,
    string DisplayName,
    string? Description,
    string SourceModuleId,
    string? SourceModuleVersion,
    string Kind,
    string? ExecutionGraphId,
    bool StartsWithHost,
    DateTimeOffset? LoadedAtUtc,
    DateTimeOffset? ActivatedAtUtc,
    DateTimeOffset? DeactivatedAtUtc,
    string? LastObservedPhase,
    DateTimeOffset? LastObservedAtUtc)
{
    /// <summary>
    /// Gets a value indicating whether the hosted execution is visible to the runtime story.
    /// </summary>
    public bool IsLoaded => LoadedAtUtc.HasValue;

    /// <summary>
    /// Gets a value indicating whether the hosted execution is currently active with the runtime host.
    /// </summary>
    public bool IsActive => ActivatedAtUtc.HasValue &&
        (!DeactivatedAtUtc.HasValue || ActivatedAtUtc.Value > DeactivatedAtUtc.Value);

    /// <summary>
    /// Gets a value indicating whether the hosted execution most recently observed a deactivation event.
    /// </summary>
    public bool IsDeactivated => DeactivatedAtUtc.HasValue &&
        (!ActivatedAtUtc.HasValue || DeactivatedAtUtc.Value >= ActivatedAtUtc.Value);
}
