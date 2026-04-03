using Cephalon.Engine.Manifest;

namespace Cephalon.Engine.Runtime;

/// <summary>
/// Combines the operator-facing runtime story into one payload that explains what loaded, started, failed, and why.
/// </summary>
/// <param name="GeneratedAtUtc">The UTC timestamp when the story snapshot was created.</param>
/// <param name="Status">The current runtime lifecycle status snapshot.</param>
/// <param name="LoadedPackages">The packages currently visible to the runtime story.</param>
/// <param name="Modules">The current lifecycle state for each loaded module.</param>
/// <param name="Timeline">The ordered lifecycle narrative for package load, execution-graph transitions, module transitions, runtime transitions, and failures.</param>
public sealed record RuntimeOperationalStory(
    DateTimeOffset GeneratedAtUtc,
    RuntimeStatusSnapshot Status,
    IReadOnlyList<PackageManifest> LoadedPackages,
    IReadOnlyList<RuntimeModuleLifecycleState> Modules,
    IReadOnlyList<RuntimeLifecycleEvent> Timeline)
{
    /// <summary>
    /// Gets the current lifecycle state for each execution graph visible to the runtime story.
    /// </summary>
    public IReadOnlyList<RuntimeExecutionGraphState> ExecutionGraphs { get; init; } = [];
}
