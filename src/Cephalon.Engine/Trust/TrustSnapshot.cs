using Cephalon.Engine.Configuration;

namespace Cephalon.Engine.Trust;

/// <summary>
/// Captures the effective trust policy together with evaluated package and capability decisions.
/// </summary>
/// <param name="Policy">The policy that produced the trust decisions.</param>
/// <param name="Packages">The evaluated package trust decisions.</param>
/// <param name="Capabilities">The evaluated capability trust decisions.</param>
/// <param name="EvaluatedAtUtc">The UTC timestamp when this trust snapshot was created, enabling operator freshness checks and re-evaluation scheduling.</param>
public sealed record TrustSnapshot(
    TrustPolicy Policy,
    IReadOnlyList<PackageTrustDecision> Packages,
    IReadOnlyList<CapabilityPolicyDecision> Capabilities,
    DateTimeOffset EvaluatedAtUtc = default);
