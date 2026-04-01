using Cephalon.Abstractions.Capabilities;

namespace Cephalon.Engine.Trust;

public sealed record CapabilityPolicyDecision(
    string CapabilityKey,
    string SourceModuleId,
    string? SourcePackageId,
    CapabilityAccess Access,
    bool SourceTrusted,
    bool IsAllowed,
    string Reason);
