using Cephalon.Abstractions.Features;
using Cephalon.Abstractions.Modules;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Runtime;
using Cephalon.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Composition;

public sealed class FeatureFlagRuntimeCatalogTests
{
    [Fact]
    public void BuildCollectsFeatureFlagsFromCodeConfigurationAndModulesAndProjectsThemIntoSnapshot()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                features: new FeatureSettings(
                [
                    new FeatureFlagSettings(
                        id: "host.preview-search",
                        displayName: "Preview Search",
                        description: "Enables the preview search experience for targeted contexts.",
                        enabled: true,
                        targeting: new FeatureFlagTargetingSettings(
                            includedEnvironmentNames: ["Production"],
                            includedTransportIds: ["rest-api"],
                            includedTags: ["pilot"])),
                    new FeatureFlagSettings(
                        id: "host.legacy-mode",
                        displayName: "Legacy Mode",
                        description: "Keeps the legacy fallback disabled until migration is approved.",
                        enabled: false)
                ])));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new FeatureFlagCatalogTestModule());
            engine.AddFeatureFlag(new FeatureFlagDescriptor(
                id: "host.checkout-v2",
                displayName: "Checkout V2",
                description: "Enables the v2 checkout flow for targeted tenants and subjects.",
                enabled: true,
                targeting: new FeatureFlagTargetingDescriptor(
                    includedEnvironmentNames: ["Production"],
                    includedModuleIds: ["feature-flags-tests"],
                    includedBehaviorIds: ["tests.checkout.submit"],
                    includedCapabilityKeys: ["orders.submit"],
                    includedTransportIds: ["rest-api"],
                    includedTenantIds: ["tenant-a"],
                    includedSubjectIds: ["user-42"],
                    includedTags: ["beta"],
                    excludedTags: ["blocked"]),
                metadata: new Dictionary<string, string>
                {
                    ["rollout"] = "beta"
                }));
        });

        using var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<IFeatureFlagRuntimeCatalog>();
        var featureToggle = provider.GetRequiredService<IFeatureToggle>();
        var snapshot = provider.GetRequiredService<IRuntimeIntrospectionSnapshotProvider>().CreateSnapshot();

        Assert.Equal(4, catalog.FeatureFlags.Count);
        Assert.Equal(3, catalog.GetEnabled().Count);
        Assert.Single(catalog.GetDisabled());
        Assert.Equal(4, snapshot.FeatureFlags.Count);

        var moduleFlag = catalog.GetById("module.orders-insights");
        Assert.NotNull(moduleFlag);
        Assert.Equal(FeatureFlagSourceKind.Module, moduleFlag!.SourceKind);
        Assert.Equal("feature-flags-tests", moduleFlag.SourceModuleId);
        Assert.Equal("analytics", moduleFlag.Metadata["surface"]);
        Assert.Single(catalog.GetBySourceModule("feature-flags-tests"));

        var hostFlag = catalog.GetById("host.checkout-v2");
        Assert.NotNull(hostFlag);
        Assert.Equal(FeatureFlagSourceKind.Host, hostFlag!.SourceKind);
        Assert.Null(hostFlag.SourceModuleId);
        Assert.Equal("beta", hostFlag.Metadata["rollout"]);
        Assert.Contains(snapshot.FeatureFlags, static featureFlag => featureFlag.Id == "host.checkout-v2");

        var includedContext = new FeatureFlagEvaluationContext(
            environmentName: "Production",
            moduleId: "feature-flags-tests",
            behaviorId: "tests.checkout.submit",
            capabilityKey: "orders.submit",
            transportId: "rest-api",
            tenantId: "tenant-a",
            subjectId: "user-42",
            tags: ["beta"]);
        var excludedContext = new FeatureFlagEvaluationContext(
            environmentName: "Production",
            moduleId: "feature-flags-tests",
            behaviorId: "tests.checkout.submit",
            capabilityKey: "orders.submit",
            transportId: "rest-api",
            tenantId: "tenant-a",
            subjectId: "user-42",
            tags: ["beta", "blocked"]);

        Assert.True(featureToggle.IsEnabled("host.checkout-v2", includedContext));

        var excludedResult = featureToggle.Evaluate("host.checkout-v2", excludedContext);
        Assert.True(excludedResult.IsDefined);
        Assert.False(excludedResult.IsEnabled);
        Assert.False(excludedResult.Matched);
        Assert.Equal(FeatureFlagSourceKind.Host, excludedResult.SourceKind);

        Assert.False(featureToggle.IsEnabled("host.preview-search", new FeatureFlagEvaluationContext(
            environmentName: "Development",
            transportId: "rest-api",
            tags: ["pilot"])));
        Assert.False(featureToggle.IsEnabled("host.legacy-mode"));

        var missing = featureToggle.Evaluate("missing-flag");
        Assert.False(missing.IsDefined);
        Assert.False(missing.IsEnabled);
    }

    [Fact]
    public void BuildFailsWhenModuleContributedFeatureFlagDeclaresDifferentSourceModule()
    {
        var services = new ServiceCollection();
        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddCephalon(engine =>
            {
                engine.UseSettings(new EngineSettings(blueprint: "Microservice"));
                engine.AddModule(new PlatformTestModule());
                engine.AddModule(new MismatchedFeatureFlagModule());
            }));

        Assert.Contains("declared source module", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildFailsWhenDuplicateFeatureFlagIdsExistAcrossSources()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(blueprint: "Microservice"));
            engine.AddModule(new PlatformTestModule());
            engine.AddModule(new FeatureFlagCatalogTestModule());
            engine.AddFeatureFlag(new FeatureFlagDescriptor(
                id: "module.orders-insights",
                displayName: "Duplicate Module Insights",
                description: "Conflicts with the module-owned flag id.",
                enabled: true));
        });

        using var provider = services.BuildServiceProvider();

        var exception = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IFeatureFlagRuntimeCatalog>());
        Assert.Contains("registered multiple times", exception.Message, StringComparison.Ordinal);
    }

    private sealed class FeatureFlagCatalogTestModule : ModuleBase, IFeatureFlagContributor
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            id: "feature-flags-tests",
            displayName: "Feature Flags Tests",
            description: "Provides module-owned feature flags for runtime catalog tests.",
            dependsOn: [typeof(PlatformTestModule)]);

        public void RegisterFeatureFlags(IFeatureFlagRegistry registry)
        {
            registry.Add(new FeatureFlagDescriptor(
                id: "module.orders-insights",
                displayName: "Orders Insights",
                description: "Enables the insights dashboard for order analytics.",
                enabled: true,
                sourceKind: FeatureFlagSourceKind.Module,
                sourceModuleId: "feature-flags-tests",
                targeting: new FeatureFlagTargetingDescriptor(
                    includedCapabilityKeys: ["orders.read"],
                    includedTags: ["analytics"]),
                metadata: new Dictionary<string, string>
                {
                    ["surface"] = "analytics"
                }));
        }
    }

    private sealed class MismatchedFeatureFlagModule : ModuleBase, IFeatureFlagContributor
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            id: "feature-flags-mismatch",
            displayName: "Feature Flag Mismatch Tests",
            description: "Deliberately contributes a mismatched module-owned feature flag.",
            dependsOn: [typeof(PlatformTestModule)]);

        public void RegisterFeatureFlags(IFeatureFlagRegistry registry)
        {
            registry.Add(new FeatureFlagDescriptor(
                id: "module.invalid-owner",
                displayName: "Invalid Owner",
                description: "Uses the wrong source module id.",
                enabled: true,
                sourceKind: FeatureFlagSourceKind.Module,
                sourceModuleId: "different-module"));
        }
    }
}
