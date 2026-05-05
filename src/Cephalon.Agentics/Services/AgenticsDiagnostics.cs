using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;
using Cephalon.Diagnostics;

namespace Cephalon.Agentics.Services;

/// <summary>
/// Defines the stable activity source, meter, activity, counter, and tag names emitted by the
/// agentics companion runtime. Names are sourced from <see cref="CephalonActivitySources.Agentics"/>
/// and <see cref="CephalonMeters.Agentics"/> so the agentics pack and observability companion
/// packs share one canonical name set with the rest of the engine.
/// </summary>
public static class AgenticsDiagnostics
{
    /// <summary>
    /// Gets the stable activity-source name emitted by the agentics runtime.
    /// </summary>
    public const string ActivitySourceName = CephalonActivitySources.Agentics;

    /// <summary>
    /// Gets the stable meter name emitted by the agentics runtime.
    /// </summary>
    public const string MeterName = CephalonMeters.Agentics;

    /// <summary>
    /// Gets the stable activity name emitted around one managed agent-tool dispatch.
    /// </summary>
    public const string ToolDispatchActivityName = "agentics.tool.dispatch";

    /// <summary>
    /// Gets the stable counter name for completed agent-tool dispatches.
    /// </summary>
    public const string ToolDispatchCounterName = "cephalon.agentics.tool_executions";

    /// <summary>
    /// Stable Cephalon-prefix tag carrying the dispatcher identifier responsible for the run.
    /// </summary>
    public const string DispatcherIdTag = "cephalon.agentics.dispatcher.id";

    /// <summary>
    /// Stable Cephalon-prefix tag carrying the agent-tool identifier emitted on the activity.
    /// </summary>
    public const string ToolIdTag = "cephalon.agentics.tool.id";

    /// <summary>
    /// Stable Cephalon-prefix tag carrying the agent-tool run identifier emitted on the activity.
    /// </summary>
    public const string RunIdTag = "cephalon.agentics.run.id";

    /// <summary>
    /// Stable Cephalon-prefix tag carrying the optional actor identifier emitted on the activity.
    /// </summary>
    public const string ActorIdTag = "cephalon.agentics.actor.id";

    /// <summary>
    /// Stable Cephalon-prefix tag carrying the optional correlation identifier emitted on the
    /// activity.
    /// </summary>
    public const string CorrelationIdTag = "cephalon.agentics.correlation.id";

    /// <summary>
    /// Stable Cephalon-prefix tag carrying the requested execution attempt number emitted on the
    /// activity.
    /// </summary>
    public const string AttemptTag = "cephalon.agentics.attempt";

    /// <summary>
    /// Stable Cephalon-prefix tag carrying the terminal execution outcome emitted on the activity
    /// (succeeded, failed, skipped, approval-required, or denied).
    /// </summary>
    public const string ExecutionOutcomeTag = "cephalon.agentics.execution.outcome";

    private static readonly string Version = typeof(AgenticsDiagnostics).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
        .InformationalVersion
        ?? typeof(AgenticsDiagnostics).Assembly.GetName().Version?.ToString()
        ?? "0.0.0";

    internal static readonly ActivitySource ActivitySource = new(ActivitySourceName, Version);
    internal static readonly Meter Meter = new(MeterName, Version);
    internal static readonly Counter<long> ToolDispatchCounter = Meter.CreateCounter<long>(
        ToolDispatchCounterName,
        unit: "executions",
        description: "Counts managed agent-tool dispatches by terminal outcome.");
}
