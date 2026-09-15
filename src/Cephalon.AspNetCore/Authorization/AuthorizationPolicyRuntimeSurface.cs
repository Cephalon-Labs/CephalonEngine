using Cephalon.Abstractions.Authorization;

namespace Cephalon.AspNetCore.Authorization;

/// <summary>
/// Describes the operator-facing authorization policy runtime surface exposed by a Cephalon ASP.NET Core host.
/// </summary>
/// <param name="Policies">The active authorization policies visible to the current runtime.</param>
/// <param name="EvaluatedAtUtc">The UTC timestamp when the authorization policy payload was evaluated.</param>
/// <param name="EvaluationDurationMilliseconds">The time in milliseconds spent resolving the authorization policy payload.</param>
public sealed record AuthorizationPolicyRuntimeSurface(
    IReadOnlyList<AuthorizationPolicyDescriptor> Policies,
    DateTimeOffset EvaluatedAtUtc = default,
    int EvaluationDurationMilliseconds = 0);
