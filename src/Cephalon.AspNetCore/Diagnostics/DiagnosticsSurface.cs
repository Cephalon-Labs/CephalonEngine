using Cephalon.Engine.Diagnostics;
using Cephalon.Engine.Runtime;

namespace Cephalon.AspNetCore.Diagnostics;

/// <summary>
/// Describes the operator-facing diagnostics surface exposed by a Cephalon ASP.NET Core host.
/// </summary>
/// <param name="MeterName">The meter name used for engine metrics.</param>
/// <param name="ActivitySourceName">The activity source name used for engine tracing.</param>
/// <param name="Counters">The built-in counter names exposed by the engine.</param>
/// <param name="Conventions">The published diagnostics conventions and event-id catalogs visible to the current host.</param>
/// <param name="Liveness">The current liveness report.</param>
/// <param name="Readiness">The current readiness report.</param>
/// <param name="SummaryPath">The aggregate health endpoint path.</param>
/// <param name="LivenessPath">The liveness endpoint path.</param>
/// <param name="ReadinessPath">The readiness endpoint path.</param>
/// <param name="GeneratedAtUtc">The UTC timestamp when this diagnostics surface payload was generated.</param>
/// <param name="LivenessEvaluationDurationMilliseconds">The time in milliseconds spent evaluating liveness for this payload.</param>
/// <param name="ReadinessEvaluationDurationMilliseconds">The time in milliseconds spent evaluating readiness for this payload.</param>
public sealed record DiagnosticsSurface(
    string MeterName,
    string ActivitySourceName,
    IReadOnlyList<string> Counters,
    IReadOnlyList<DiagnosticsConvention> Conventions,
    RuntimeHealthReport Liveness,
    RuntimeHealthReport Readiness,
    string SummaryPath,
    string LivenessPath,
    string ReadinessPath,
    DateTimeOffset GeneratedAtUtc = default,
    int LivenessEvaluationDurationMilliseconds = 0,
    int ReadinessEvaluationDurationMilliseconds = 0);
