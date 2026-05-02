using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;
using Cephalon.Diagnostics;

namespace Cephalon.Engine.Diagnostics;

/// <summary>
/// Defines the stable meter, activity source, and counter names emitted by the engine runtime.
/// </summary>
/// <remarks>
/// The <see cref="MeterName"/> and <see cref="ActivitySourceName"/> values are sourced from
/// <see cref="CephalonMeters.Engine"/> and <see cref="CephalonActivitySources.Engine"/>
/// respectively, so the engine runtime and observability companion packs share one
/// canonical name set rather than re-declaring the literal "Cephalon.Engine" string.
/// </remarks>
public static class EngineDiagnostics
{
    /// <summary>
    /// Gets the meter name emitted by the engine.
    /// </summary>
    public const string MeterName = CephalonMeters.Engine;

    /// <summary>
    /// Gets the activity-source name emitted by the engine.
    /// </summary>
    public const string ActivitySourceName = CephalonActivitySources.Engine;

    /// <summary>
    /// Gets the activity name used while building the runtime.
    /// </summary>
    public const string BuildActivityName = "engine.build";

    /// <summary>
    /// Gets the counter name for completed engine builds.
    /// </summary>
    public const string EngineBuildCounterName = "cephalon.engine.builds";

    /// <summary>
    /// Gets the counter name for runtime lifecycle transitions.
    /// </summary>
    public const string RuntimeTransitionCounterName = "cephalon.runtime.transitions";

    /// <summary>
    /// Gets the counter name for module lifecycle transitions.
    /// </summary>
    public const string ModuleTransitionCounterName = "cephalon.module.transitions";

    /// <summary>
    /// Gets the counter name for execution-graph lifecycle transitions.
    /// </summary>
    public const string ExecutionGraphTransitionCounterName = "cephalon.execution-graphs.transitions";

    /// <summary>
    /// Gets the counter name for hosted-execution lifecycle transitions.
    /// </summary>
    public const string HostedExecutionTransitionCounterName = "cephalon.hosted-executions.transitions";

    /// <summary>
    /// Gets the counter name for runtime lifecycle failures.
    /// </summary>
    public const string RuntimeFailureCounterName = "cephalon.runtime.failures";

    /// <summary>
    /// Gets the counter name for module lifecycle failures.
    /// </summary>
    public const string ModuleFailureCounterName = "cephalon.module.failures";

    /// <summary>
    /// Gets the counter name for runtime restart attempts.
    /// </summary>
    public const string RuntimeRestartCounterName = "cephalon.runtime.restarts";

    private static readonly string Version = typeof(EngineDiagnostics).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
        .InformationalVersion
        ?? typeof(EngineDiagnostics).Assembly.GetName().Version?.ToString()
        ?? "0.0.0";

    internal static readonly ActivitySource ActivitySource = new(ActivitySourceName, Version);
    internal static readonly Meter Meter = new(MeterName, Version);
    internal static readonly Counter<long> EngineBuildCounter = Meter.CreateCounter<long>(
        EngineBuildCounterName,
        unit: "builds",
        description: "Counts completed engine builds.");
    internal static readonly Counter<long> RuntimeTransitionCounter = Meter.CreateCounter<long>(
        RuntimeTransitionCounterName,
        unit: "transitions",
        description: "Counts runtime lifecycle transitions.");
    internal static readonly Counter<long> ModuleTransitionCounter = Meter.CreateCounter<long>(
        ModuleTransitionCounterName,
        unit: "transitions",
        description: "Counts module lifecycle transitions.");
    internal static readonly Counter<long> ExecutionGraphTransitionCounter = Meter.CreateCounter<long>(
        ExecutionGraphTransitionCounterName,
        unit: "transitions",
        description: "Counts execution-graph lifecycle transitions.");
    internal static readonly Counter<long> HostedExecutionTransitionCounter = Meter.CreateCounter<long>(
        HostedExecutionTransitionCounterName,
        unit: "transitions",
        description: "Counts hosted-execution lifecycle transitions.");
    internal static readonly Counter<long> RuntimeFailureCounter = Meter.CreateCounter<long>(
        RuntimeFailureCounterName,
        unit: "failures",
        description: "Counts runtime lifecycle failures.");
    internal static readonly Counter<long> ModuleFailureCounter = Meter.CreateCounter<long>(
        ModuleFailureCounterName,
        unit: "failures",
        description: "Counts module lifecycle failures.");
    internal static readonly Counter<long> RuntimeRestartCounter = Meter.CreateCounter<long>(
        RuntimeRestartCounterName,
        unit: "restarts",
        description: "Counts runtime restart attempts.");
}
