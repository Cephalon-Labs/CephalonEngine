using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Cephalon.Eventing.Wolverine.Services;

internal static class WolverineDispatchInstrumentation
{
    public const string ActivitySourceName = "Cephalon.Eventing.Wolverine.Dispatch";
    public const string MeterName = "Cephalon.Eventing.Wolverine.Dispatch";

    public static readonly ActivitySource Source = new(ActivitySourceName, "1.0.0");
    public static readonly Meter DispatchMeter = new(MeterName, "1.0.0");

    public static readonly Counter<long> DispatchAttempts = DispatchMeter.CreateCounter<long>(
        "cephalon.wolverine.dispatch.attempts",
        description: "Total number of event dispatch attempts.");

    public static readonly Counter<long> DispatchSuccesses = DispatchMeter.CreateCounter<long>(
        "cephalon.wolverine.dispatch.successes",
        description: "Total number of successful event dispatches.");

    public static readonly Counter<long> DispatchFailures = DispatchMeter.CreateCounter<long>(
        "cephalon.wolverine.dispatch.failures",
        description: "Total number of failed event dispatch attempts.");

    public static readonly Counter<long> DispatchRetries = DispatchMeter.CreateCounter<long>(
        "cephalon.wolverine.dispatch.retries",
        description: "Total number of dispatch retry scheduling events.");

    public static readonly Histogram<double> DispatchDuration = DispatchMeter.CreateHistogram<double>(
        "cephalon.wolverine.dispatch.duration",
        unit: "ms",
        description: "Duration of individual event dispatch operations in milliseconds.");
}
