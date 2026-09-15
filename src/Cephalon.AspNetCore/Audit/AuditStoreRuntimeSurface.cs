using Cephalon.Abstractions.Audit;

namespace Cephalon.AspNetCore.Audit;

/// <summary>
/// Describes the operator-facing audit-store runtime surface exposed by a Cephalon ASP.NET Core host.
/// </summary>
/// <param name="AuditStores">The active audit stores visible to the current runtime.</param>
/// <param name="EvaluatedAtUtc">The UTC timestamp when the audit-store payload was evaluated.</param>
/// <param name="EvaluationDurationMilliseconds">The time in milliseconds spent resolving the audit-store payload.</param>
public sealed record AuditStoreRuntimeSurface(
    IReadOnlyList<AuditStoreDescriptor> AuditStores,
    DateTimeOffset EvaluatedAtUtc = default,
    int EvaluationDurationMilliseconds = 0);
