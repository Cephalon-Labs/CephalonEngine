using Cephalon.Abstractions.Capabilities;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Manifest;

namespace Cephalon.Engine.Trust;

public sealed class CapabilityPolicyEvaluator
{
    private readonly TrustSnapshot snapshot;

    public CapabilityPolicyEvaluator(TrustSnapshot snapshot)
    {
        this.snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
    }

    public TrustSnapshot Snapshot => snapshot;

    public bool IsAllowed(string capabilityKey)
    {
        return TryGetDecision(capabilityKey, out var decision) && decision.IsAllowed;
    }

    public bool TryGetDecision(string capabilityKey, out CapabilityPolicyDecision decision)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(capabilityKey);

        decision = snapshot.Capabilities.FirstOrDefault(item =>
            string.Equals(item.CapabilityKey, capabilityKey.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? CreateFallbackDecision(capabilityKey.Trim());

        return decision is not null;
    }

    public static TrustSnapshot CreateSnapshot(
        TrustPolicy policy,
        IReadOnlyList<PackageManifest> packages,
        IReadOnlyList<ModuleManifest> modules,
        IReadOnlyList<CapabilityManifest> capabilities)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(packages);
        ArgumentNullException.ThrowIfNull(modules);
        ArgumentNullException.ThrowIfNull(capabilities);

        var moduleLookup = modules.ToDictionary(static module => module.Id, StringComparer.OrdinalIgnoreCase);
        var packageLookup = packages.ToDictionary(static package => package.Id, StringComparer.OrdinalIgnoreCase);

        var capabilityDecisions = capabilities
            .Select(capability =>
            {
                var module = moduleLookup[capability.SourceModuleId];
                var access = policy.ResolveCapabilityAccess(capability.Key);
                var sourceTrusted = module.IsTrusted;
                var isAllowed = access switch
                {
                    CapabilityAccess.Allowed => true,
                    CapabilityAccess.TrustedOnly => sourceTrusted,
                    _ => false
                };
                var reason = access switch
                {
                    CapabilityAccess.Allowed => "Capability is allowed by trust policy.",
                    CapabilityAccess.TrustedOnly when sourceTrusted => "Capability requires a trusted source and the source is trusted.",
                    CapabilityAccess.TrustedOnly => "Capability requires a trusted source, but the source is not trusted.",
                    _ => "Capability is denied by trust policy."
                };

                return new CapabilityPolicyDecision(
                    CapabilityKey: capability.Key,
                    SourceModuleId: capability.SourceModuleId,
                    SourcePackageId: module.PackageId,
                    Access: access,
                    SourceTrusted: sourceTrusted,
                    IsAllowed: isAllowed,
                    Reason: reason);
            })
            .OrderBy(static decision => decision.CapabilityKey, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var packageDecisions = packages
            .Select(package => new PackageTrustDecision(
                PackageId: package.Id,
                AssemblyName: package.AssemblyName,
                Path: package.Path,
                PublisherId: package.PublisherId,
                SignatureKeyId: package.SignatureKeyId,
                SignatureFingerprint: package.SignatureFingerprint,
                Signatures: package.Signatures
                    .Select(static signature => new PackageSignatureTrustDecision(
                        Signer: signature.Signer,
                        KeyId: signature.KeyId,
                        Fingerprint: signature.Fingerprint,
                        IsVerified: signature.IsVerified,
                        Reason: signature.VerificationReason))
                    .ToArray(),
                IsSignatureVerified: package.IsSignatureVerified,
                SignatureVerificationReason: package.SignatureVerificationReason,
                IsTrusted: package.IsTrusted,
                Reason: package.TrustReason))
            .OrderBy(static decision => decision.PackageId, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new TrustSnapshot(policy, packageDecisions, capabilityDecisions);
    }

    private CapabilityPolicyDecision CreateFallbackDecision(string capabilityKey)
    {
        var access = snapshot.Policy.ResolveCapabilityAccess(capabilityKey);
        var isAllowed = access == CapabilityAccess.Allowed;
        var reason = isAllowed
            ? "Capability is allowed by default trust policy."
            : "Capability is not registered and is not allowed by the default trust policy.";

        return new CapabilityPolicyDecision(
            CapabilityKey: capabilityKey,
            SourceModuleId: "unknown",
            SourcePackageId: null,
            Access: access,
            SourceTrusted: false,
            IsAllowed: isAllowed,
            Reason: reason);
    }
}
