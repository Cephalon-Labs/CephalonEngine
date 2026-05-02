using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;
using Cephalon.Diagnostics;

namespace Cephalon.Worker.Hosting;

/// <summary>
/// Defines the stable activity source and meter names emitted by the worker host adapter.
/// Names are sourced from <see cref="CephalonActivitySources.Worker"/> and
/// <see cref="CephalonMeters.Worker"/> so the worker host adapter and observability companion
/// packs share one canonical name set with the rest of the engine.
/// </summary>
internal static class WorkerDiagnostics
{
    public const string ActivitySourceName = CephalonActivitySources.Worker;

    public const string MeterName = CephalonMeters.Worker;

    public const string LifecycleStartActivityName = "worker.lifecycle.start";

    public const string LifecycleStopActivityName = "worker.lifecycle.stop";

    private static readonly string Version = typeof(WorkerDiagnostics).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
        .InformationalVersion
        ?? typeof(WorkerDiagnostics).Assembly.GetName().Version?.ToString()
        ?? "0.0.0";

    internal static readonly ActivitySource ActivitySource = new(ActivitySourceName, Version);

    internal static readonly Meter Meter = new(MeterName, Version);
}
