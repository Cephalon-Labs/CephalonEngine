using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;

namespace Cephalon.Engine.Diagnostics;

public static class EngineDiagnostics
{
    public const string MeterName = "Cephalon.Engine";
    public const string ActivitySourceName = "Cephalon.Engine";
    public const string BuildActivityName = "engine.build";
    public const string EngineBuildCounterName = "cephalon.engine.builds";
    public const string RuntimeTransitionCounterName = "cephalon.runtime.transitions";
    public const string ModuleTransitionCounterName = "cephalon.module.transitions";
    public const string RuntimeFailureCounterName = "cephalon.runtime.failures";
    public const string ModuleFailureCounterName = "cephalon.module.failures";
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
