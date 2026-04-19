using System.Net.Http.Json;
using Cephalon.Abstractions.Features;
using Cephalon.Abstractions.Modules;
using Cephalon.AspNetCore.Hosting;
using Cephalon.Engine.Runtime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;

namespace Cephalon.Tests.Hosting;

public sealed class FeatureFlagHostingTests
{
    [Fact]
    public async Task MapCephalonExposesFeatureFlagsAndEvaluationFromSharedRuntimeTruth()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["Engine:Features:Flags:0:Id"] = "config.preview-search";
        builder.Configuration["Engine:Features:Flags:0:DisplayName"] = "Preview Search";
        builder.Configuration["Engine:Features:Flags:0:Description"] = "Enables preview search for pilot REST traffic.";
        builder.Configuration["Engine:Features:Flags:0:Enabled"] = "true";
        builder.Configuration["Engine:Features:Flags:0:Targeting:IncludedEnvironmentNames:0"] = "Production";
        builder.Configuration["Engine:Features:Flags:0:Targeting:IncludedTransportIds:0"] = "rest-api";
        builder.Configuration["Engine:Features:Flags:0:Targeting:IncludedTags:0"] = "pilot";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new FeatureFlagHostingModule());
            engine.AddFeatureFlag(new FeatureFlagDescriptor(
                id: "host.checkout-v2",
                displayName: "Checkout V2",
                description: "Enables the v2 checkout flow for a targeted rollout.",
                enabled: true,
                targeting: new FeatureFlagTargetingDescriptor(
                    includedEnvironmentNames: ["Production"],
                    includedModuleIds: ["feature-flags-hosting-tests"],
                    includedBehaviorIds: ["tests.checkout.submit"],
                    includedCapabilityKeys: ["orders.submit"],
                    includedTransportIds: ["rest-api"],
                    includedTenantIds: ["tenant-a"],
                    includedSubjectIds: ["user-42"],
                    includedTags: ["beta"],
                    excludedTags: ["blocked"])));
            engine.AddFeatureFlag(new FeatureFlagDescriptor(
                id: "host.legacy-mode",
                displayName: "Legacy Mode",
                description: "Keeps the legacy fallback disabled.",
                enabled: false));
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var featureFlags = await client.GetFromJsonAsync<FeatureFlagDescriptor[]>("/engine/features");
        var enabledFeatureFlags = await client.GetFromJsonAsync<FeatureFlagDescriptor[]>("/engine/features/enabled");
        var disabledFeatureFlags = await client.GetFromJsonAsync<FeatureFlagDescriptor[]>("/engine/features/disabled");
        var moduleFeatureFlags = await client.GetFromJsonAsync<FeatureFlagDescriptor[]>("/engine/features/modules/feature-flags-hosting-tests");
        var hostFeatureFlag = await client.GetFromJsonAsync<FeatureFlagDescriptor>("/engine/features/host.checkout-v2");
        var evaluation = await client.GetFromJsonAsync<FeatureFlagEvaluationResult>(
            "/engine/features/host.checkout-v2/evaluate?environmentName=Production&moduleId=feature-flags-hosting-tests&behaviorId=tests.checkout.submit&capabilityKey=orders.submit&transportId=rest-api&tenantId=tenant-a&subjectId=user-42&tag=beta");
        var blockedEvaluation = await client.GetFromJsonAsync<FeatureFlagEvaluationResult>(
            "/engine/features/host.checkout-v2/evaluate?environmentName=Production&moduleId=feature-flags-hosting-tests&behaviorId=tests.checkout.submit&capabilityKey=orders.submit&transportId=rest-api&tenantId=tenant-a&subjectId=user-42&tag=beta&tag=blocked");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(featureFlags);
        Assert.NotNull(enabledFeatureFlags);
        Assert.NotNull(disabledFeatureFlags);
        Assert.NotNull(moduleFeatureFlags);
        Assert.NotNull(hostFeatureFlag);
        Assert.NotNull(evaluation);
        Assert.NotNull(blockedEvaluation);
        Assert.NotNull(snapshot);

        Assert.Equal(4, featureFlags.Length);
        Assert.Equal(3, enabledFeatureFlags.Length);
        Assert.Single(disabledFeatureFlags);
        Assert.Single(moduleFeatureFlags);
        Assert.Equal("module.orders-insights", moduleFeatureFlags[0].Id);
        Assert.Equal(FeatureFlagSourceKind.Module, moduleFeatureFlags[0].SourceKind);
        Assert.Equal("feature-flags-hosting-tests", moduleFeatureFlags[0].SourceModuleId);
        Assert.Equal(4, snapshot.FeatureFlags.Count);
        Assert.Contains(snapshot.FeatureFlags, static featureFlag => featureFlag.Id == "host.checkout-v2");

        Assert.Equal("host.checkout-v2", hostFeatureFlag.Id);
        Assert.Equal(FeatureFlagSourceKind.Host, hostFeatureFlag.SourceKind);
        Assert.True(evaluation.IsDefined);
        Assert.True(evaluation.IsEnabled);
        Assert.True(evaluation.Matched);
        Assert.False(blockedEvaluation.IsEnabled);
        Assert.False(blockedEvaluation.Matched);
    }

    private sealed class FeatureFlagHostingModule : ModuleBase, IFeatureFlagContributor
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            id: "feature-flags-hosting-tests",
            displayName: "Feature Flags Hosting Tests",
            description: "Provides module-owned feature flags for hosting tests.");

        public void RegisterFeatureFlags(IFeatureFlagRegistry registry)
        {
            registry.Add(new FeatureFlagDescriptor(
                id: "module.orders-insights",
                displayName: "Orders Insights",
                description: "Enables the insights dashboard for analytics traffic.",
                enabled: true,
                sourceKind: FeatureFlagSourceKind.Module,
                sourceModuleId: "feature-flags-hosting-tests",
                targeting: new FeatureFlagTargetingDescriptor(
                    includedCapabilityKeys: ["orders.read"],
                    includedTags: ["analytics"])));
        }
    }
}
