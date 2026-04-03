namespace Cephalon.Engine.Runtime;

/// <summary>
/// Describes the current operator-facing lifecycle state for one execution graph visible to the runtime.
/// </summary>
/// <param name="GraphId">The stable execution-graph identifier.</param>
/// <param name="DisplayName">The operator-facing execution-graph display name.</param>
/// <param name="Description">The operator-facing execution-graph description when one was published.</param>
/// <param name="SourceModuleId">The module that contributed the execution graph.</param>
/// <param name="SourceModuleVersion">The effective version of the source module when available.</param>
/// <param name="EntryNodeId">The entry node used when the graph begins execution.</param>
/// <param name="LoadedAtUtc">The UTC timestamp when the graph became visible to the built runtime story.</param>
/// <param name="ActivatedAtUtc">The UTC timestamp when the graph most recently became active with the runtime.</param>
/// <param name="DeactivatedAtUtc">The UTC timestamp when the graph most recently became inactive because the runtime stopped.</param>
/// <param name="LastObservedPhase">The last lifecycle phase recorded for the execution graph.</param>
/// <param name="LastObservedAtUtc">The UTC timestamp when the last lifecycle phase was recorded.</param>
public sealed record RuntimeExecutionGraphState(
    string GraphId,
    string DisplayName,
    string? Description,
    string SourceModuleId,
    string? SourceModuleVersion,
    string EntryNodeId,
    DateTimeOffset? LoadedAtUtc,
    DateTimeOffset? ActivatedAtUtc,
    DateTimeOffset? DeactivatedAtUtc,
    string? LastObservedPhase,
    DateTimeOffset? LastObservedAtUtc)
{
    /// <summary>
    /// Gets a value indicating whether the execution graph is visible to the runtime story.
    /// </summary>
    public bool IsLoaded => LoadedAtUtc.HasValue;

    /// <summary>
    /// Gets a value indicating whether the execution graph is currently active with the runtime.
    /// </summary>
    public bool IsActive => ActivatedAtUtc.HasValue &&
        (!DeactivatedAtUtc.HasValue || ActivatedAtUtc.Value > DeactivatedAtUtc.Value);

    /// <summary>
    /// Gets a value indicating whether the execution graph most recently observed a deactivation event.
    /// </summary>
    public bool IsDeactivated => DeactivatedAtUtc.HasValue &&
        (!ActivatedAtUtc.HasValue || DeactivatedAtUtc.Value >= ActivatedAtUtc.Value);
}
