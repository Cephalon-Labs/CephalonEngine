using Cephalon.Abstractions.Capabilities;

namespace Cephalon.Engine.Trust;

/// <summary>
/// Describes the evaluated trust decision for a single capability.
/// </summary>
/// <param name="CapabilityKey">The capability key that was evaluated.</param>
/// <param name="SourceModuleId">The module that contributed the capability.</param>
/// <param name="SourcePackageId">The package that contributed the capability when one is known.</param>
/// <param name="Access">The effective access mode resolved from policy.</param>
/// <param name="SourceTrusted">Whether the contributing source is trusted.</param>
/// <param name="IsAllowed">Whether the capability is allowed under the resolved policy.</param>
/// <param name="Reason">The human-readable reason for the decision.</param>
public sealed record CapabilityPolicyDecision(
    string CapabilityKey,
    string SourceModuleId,
    string? SourcePackageId,
    CapabilityAccess Access,
    bool SourceTrusted,
    bool IsAllowed,
    string Reason);
