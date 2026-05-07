using Cephalon.Abstractions.Capabilities;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Manifest;
using Cephalon.Engine.Trust;

namespace Cephalon.Tests.Composition;

public sealed class CapabilityPolicyEvaluatorTests
{
    [Fact]
    public void CreateSnapshotDeniesTrustedOnlyCapabilitiesFromUntrustedModules()
    {
        var policy = new TrustPolicy(defaultCapabilityAccess: CapabilityAccess.TrustedOnly);
        var modules = new[]
        {
            new ModuleManifest(
                id: "restricted",
                displayName: "Restricted",
                description: "Untrusted module.",
                version: "1.0.0",
                assemblyName: "Untrusted.Modules",
                typeName: "Untrusted.Modules.RestrictedModule",
                dependsOn: [],
                tags: ["security"],
                isTrusted: false)
        };
        var capabilities = new[]
        {
            new CapabilityManifest(
                key: "restricted.secret",
                displayName: "Restricted Secret",
                description: "Security-sensitive capability.",
                sourceModuleId: "restricted")
        };

        var snapshot = CapabilityPolicyEvaluator.CreateSnapshot(
            policy,
            packages: [],
            modules,
            capabilities);

        var decision = Assert.Single(snapshot.Capabilities);

        Assert.Equal("restricted.secret", decision.CapabilityKey);
        Assert.Equal(CapabilityAccess.TrustedOnly, decision.Access);
        Assert.False(decision.SourceTrusted);
        Assert.False(decision.IsAllowed);
        Assert.Contains("requires a trusted source", decision.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryGetDecisionMatchesRegisteredCapabilityKeysCaseInsensitively()
    {
        var evaluator = new CapabilityPolicyEvaluator(new TrustSnapshot(
            new TrustPolicy(),
            Packages: [],
            Capabilities:
            [
                new CapabilityPolicyDecision(
                    CapabilityKey: "Restricted.Secret",
                    SourceModuleId: "restricted",
                    SourcePackageId: null,
                    Access: CapabilityAccess.Allowed,
                    SourceTrusted: true,
                    IsAllowed: true,
                    Reason: "Capability is allowed by trust policy.")
            ]));

        var found = evaluator.TryGetDecision("  restricted.secret  ", out var decision);

        Assert.True(found);
        Assert.Equal("Restricted.Secret", decision.CapabilityKey);
        Assert.Equal("restricted", decision.SourceModuleId);
        Assert.True(decision.IsAllowed);
    }

    [Fact]
    public void CreateSnapshotEvaluatesAggregatedCapabilitySourceModulesTogether()
    {
        var policy = new TrustPolicy(defaultCapabilityAccess: CapabilityAccess.TrustedOnly);
        var modules = new[]
        {
            new ModuleManifest(
                id: "alpha-provider",
                displayName: "Alpha Provider",
                description: "Trusted provider.",
                version: "1.0.0",
                assemblyName: "Trusted.Modules",
                typeName: "Trusted.Modules.AlphaProvider",
                dependsOn: [],
                tags: ["data"],
                packageId: "pkg.alpha",
                isTrusted: true),
            new ModuleManifest(
                id: "beta-provider",
                displayName: "Beta Provider",
                description: "Untrusted provider.",
                version: "1.0.0",
                assemblyName: "Untrusted.Modules",
                typeName: "Untrusted.Modules.BetaProvider",
                dependsOn: [],
                tags: ["data"],
                packageId: "pkg.beta",
                isTrusted: false)
        };
        var capabilities = new[]
        {
            new CapabilityManifest(
                key: "data.shared-family",
                displayName: "Shared Data Family",
                description: "Aggregated provider capability.",
                sourceModuleId: "alpha-provider",
                metadata: new Dictionary<string, string>
                {
                    ["sourceModuleIds"] = "alpha-provider,beta-provider"
                })
        };

        var snapshot = CapabilityPolicyEvaluator.CreateSnapshot(
            policy,
            packages: [],
            modules,
            capabilities);

        var decision = Assert.Single(snapshot.Capabilities);

        Assert.Equal("data.shared-family", decision.CapabilityKey);
        Assert.Equal("alpha-provider,beta-provider", decision.SourceModuleId);
        Assert.Equal("pkg.alpha,pkg.beta", decision.SourcePackageId);
        Assert.False(decision.SourceTrusted);
        Assert.False(decision.IsAllowed);
    }

    [Fact]
    public void TryGetDecisionReturnsFallbackDeniedDecisionForUnknownCapability()
    {
        var evaluator = new CapabilityPolicyEvaluator(new TrustSnapshot(
            new TrustPolicy(defaultCapabilityAccess: CapabilityAccess.TrustedOnly),
            Packages: [],
            Capabilities: []));

        var found = evaluator.TryGetDecision("  unknown.capability  ", out var decision);

        Assert.True(found);
        Assert.Equal("unknown.capability", decision.CapabilityKey);
        Assert.Equal("unknown", decision.SourceModuleId);
        Assert.Equal(CapabilityAccess.TrustedOnly, decision.Access);
        Assert.False(decision.IsAllowed);
        Assert.Contains("not registered", decision.Reason, StringComparison.OrdinalIgnoreCase);
    }
}
