namespace Cephalon.Engine.Runtime;

/// <summary>
/// Describes the current operator-facing lifecycle state for one loaded module.
/// </summary>
/// <param name="ModuleId">The stable module identifier.</param>
/// <param name="DisplayName">The operator-facing module display name.</param>
/// <param name="Version">The effective module version.</param>
/// <param name="AssemblyName">The assembly that contains the module implementation.</param>
/// <param name="PackageId">The package that supplied the module when package loading was used.</param>
/// <param name="LoadedAtUtc">The UTC timestamp when the module became part of the built runtime story.</param>
/// <param name="InitializedAtUtc">The UTC timestamp when module initialization last completed successfully.</param>
/// <param name="StartedAtUtc">The UTC timestamp when module startup last completed successfully.</param>
/// <param name="StoppedAtUtc">The UTC timestamp when module shutdown last completed successfully.</param>
/// <param name="LastObservedPhase">The last lifecycle phase recorded for the module.</param>
/// <param name="LastObservedAtUtc">The UTC timestamp when the last lifecycle phase was recorded for the module.</param>
/// <param name="LastFailure">The last failure recorded for the module when one is still relevant to the current runtime story.</param>
public sealed record RuntimeModuleLifecycleState(
    string ModuleId,
    string DisplayName,
    string Version,
    string AssemblyName,
    string? PackageId,
    DateTimeOffset? LoadedAtUtc,
    DateTimeOffset? InitializedAtUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? StoppedAtUtc,
    string? LastObservedPhase,
    DateTimeOffset? LastObservedAtUtc,
    RuntimeFailureInfo? LastFailure)
{
    /// <summary>
    /// Gets a value indicating whether the module is present in the built runtime.
    /// </summary>
    public bool IsLoaded => LoadedAtUtc.HasValue;

    /// <summary>
    /// Gets a value indicating whether the module completed initialization successfully.
    /// </summary>
    public bool IsInitialized => InitializedAtUtc.HasValue;

    /// <summary>
    /// Gets a value indicating whether the module most recently completed startup without a later successful stop.
    /// </summary>
    public bool IsStarted => StartedAtUtc.HasValue &&
        (!StoppedAtUtc.HasValue || StartedAtUtc.Value > StoppedAtUtc.Value);

    /// <summary>
    /// Gets a value indicating whether the module most recently completed shutdown.
    /// </summary>
    public bool IsStopped => StoppedAtUtc.HasValue &&
        (!StartedAtUtc.HasValue || StoppedAtUtc.Value >= StartedAtUtc.Value);
}
