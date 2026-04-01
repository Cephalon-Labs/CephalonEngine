using Cephalon.Engine.Configuration;

namespace Cephalon.Engine.Trust;

public sealed record TrustSnapshot(
    TrustPolicy Policy,
    IReadOnlyList<PackageTrustDecision> Packages,
    IReadOnlyList<CapabilityPolicyDecision> Capabilities);
