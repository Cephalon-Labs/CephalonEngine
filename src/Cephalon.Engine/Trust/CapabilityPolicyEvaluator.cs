using Cephalon.Abstractions.Capabilities;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Manifest;

namespace Cephalon.Engine.Trust;

/// <summary>
/// Evaluates capability and package trust decisions against the current trust policy snapshot.
/// </summary>
public sealed class CapabilityPolicyEvaluator
{
    private readonly TrustSnapshot snapshot;

    /// <summary>
    /// Initializes a new instance of the <see cref="CapabilityPolicyEvaluator" /> class.
    /// </summary>
    /// <param name="snapshot">The trust snapshot to evaluate against.</param>
    public CapabilityPolicyEvaluator(TrustSnapshot snapshot)
    {
        this.snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
    }

    /// <summary>
    /// Gets the trust snapshot being evaluated.
    /// </summary>
    public TrustSnapshot Snapshot => snapshot;

    /// <summary>
    /// Determines whether a capability is allowed under the current trust policy.
    /// </summary>
    /// <param name="capabilityKey">The capability key to evaluate.</param>
    /// <returns><see langword="true" /> when the capability is allowed; otherwise, <see langword="false" />.</returns>
    public bool IsAllowed(string capabilityKey)
    {
        return TryGetDecision(capabilityKey, out var decision) && decision.IsAllowed;
    }

    /// <summary>
    /// Attempts to resolve the trust decision for a capability.
    /// </summary>
    /// <param name="capabilityKey">The capability key to evaluate.</param>
    /// <param name="decision">The resolved trust decision.</param>
    /// <returns><see langword="true" /> when a decision was produced.</returns>
    public bool TryGetDecision(string capabilityKey, out CapabilityPolicyDecision decision)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(capabilityKey);

        decision = snapshot.Capabilities.FirstOrDefault(item =>
            string.Equals(item.CapabilityKey, capabilityKey.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? CreateFallbackDecision(capabilityKey.Trim());

        return decision is not null;
    }

    /// <summary>
    /// Creates a trust snapshot from the supplied policy, packages, modules, and capabilities.
    /// Populates operator-facing metadata for freshness, performance visibility, and drift detection.
    /// </summary>
    /// <param name="policy">The trust policy to apply.</param>
    /// <param name="packages">The package manifests visible to the runtime.</param>
    /// <param name="modules">The module manifests visible to the runtime.</param>
    /// <param name="capabilities">The capability manifests visible to the runtime.</param>
    /// <returns>A computed trust snapshot with evaluation timestamps and performance metrics.</returns>
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

        var evaluatedAtUtc = DateTimeOffset.UtcNow;
        var evaluationStopwatch = System.Diagnostics.Stopwatch.StartNew();

        var moduleLookup = modules.ToDictionary(static module => module.Id, StringComparer.OrdinalIgnoreCase);

        var capabilityDecisions = capabilities
            .Select(capability =>
            {
                var sourceModuleIds = capability.Metadata.TryGetValue("sourceModuleIds", out var rawSourceModuleIds)
                    ? rawSourceModuleIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    : [capability.SourceModuleId];
                if (sourceModuleIds.Length == 0 || sourceModuleIds.Any(id => !moduleLookup.ContainsKey(id)))
                {
                    return new CapabilityPolicyDecision(
                        CapabilityKey: capability.Key,
                        SourceModuleId: capability.SourceModuleId,
                        SourcePackageId: null,
                        Access: policy.ResolveCapabilityAccess(capability.Key),
                        SourceTrusted: false,
                        IsAllowed: false,
                        Reason: "Capability source module is not registered in the runtime manifest.")
                    {
                        EvaluatedAtUtc = evaluatedAtUtc
                    };
                }

                var sourceModules = ResolveSourceModules(capability, moduleLookup);
                var access = policy.ResolveCapabilityAccess(capability.Key);
                var sourceTrusted = sourceModules.All(static module => module.IsTrusted);
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

                var sourcePackageIds = sourceModules
                    .Select(static module => module.PackageId)
                    .Where(static packageId => !string.IsNullOrWhiteSpace(packageId))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(static packageId => packageId, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                return new CapabilityPolicyDecision(
                    CapabilityKey: capability.Key,
                    SourceModuleId: string.Join(",", sourceModules.Select(static module => module.Id)),
                    SourcePackageId: sourcePackageIds.Length == 0
                        ? null
                        : string.Join(",", sourcePackageIds),
                    Access: access,
                    SourceTrusted: sourceTrusted,
                    IsAllowed: isAllowed,
                    Reason: reason)
                {
                    EvaluatedAtUtc = evaluatedAtUtc
                };
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
                SignatureCertificateThumbprint: package.SignatureCertificateThumbprint,
                Signatures: package.Signatures
                    .Select(static signature => new PackageSignatureTrustDecision(
                        Signer: signature.Signer,
                        KeyId: signature.KeyId,
                        Fingerprint: signature.Fingerprint,
                        VerificationSource: signature.VerificationSource,
                        CertificateThumbprint: signature.CertificateThumbprint,
                        IsVerified: signature.IsVerified,
                        Reason: signature.VerificationReason))
                    .ToArray(),
                IsSignatureVerified: package.IsSignatureVerified,
                SignatureVerificationReason: package.SignatureVerificationReason,
                IsTrusted: package.IsTrusted,
                Reason: package.TrustReason)
            {
                VerifiedAtUtc = evaluatedAtUtc,
                VerificationDurationMilliseconds = (int)Math.Min(evaluationStopwatch.ElapsedMilliseconds, int.MaxValue)
            })
            .OrderBy(static decision => decision.PackageId, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        evaluationStopwatch.Stop();
        return new TrustSnapshot(policy, packageDecisions, capabilityDecisions)
        {
            EvaluatedAtUtc = evaluatedAtUtc
        };
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

    private static ModuleManifest[] ResolveSourceModules(
        CapabilityManifest capability,
        Dictionary<string, ModuleManifest> moduleLookup)
    {
        var sourceModuleIds = capability.Metadata.TryGetValue("sourceModuleIds", out var rawSourceModuleIds)
            ? rawSourceModuleIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : [capability.SourceModuleId];

        return sourceModuleIds
            .Select(sourceModuleId => moduleLookup.TryGetValue(sourceModuleId, out var module)
                ? module
                : throw new InvalidOperationException(
                    $"Capability '{capability.Key}' references source module '{sourceModuleId}', but that module is not registered in the runtime manifest."))
            .OrderBy(static module => module.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
