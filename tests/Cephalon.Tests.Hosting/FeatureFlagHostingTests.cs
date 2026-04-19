using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Cephalon.Abstractions.Features;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Transports;
using Cephalon.AspNetCore.Hosting;
using Cephalon.AspNetCore.Modules;
using Cephalon.AspNetCore.Transports.Rest;
using Cephalon.Engine.Runtime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
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

    [Fact]
    public async Task MapCephalonEnforcesRestFeatureRequirementsWhileKeepingPublishedEndpointTruthVisible()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["Engine:Features:Flags:0:Id"] = "host.orders-preview";
        builder.Configuration["Engine:Features:Flags:0:DisplayName"] = "Orders Preview";
        builder.Configuration["Engine:Features:Flags:0:Description"] = "Enables the pilot orders endpoint.";
        builder.Configuration["Engine:Features:Flags:0:Enabled"] = "true";
        builder.Configuration["Engine:Features:Flags:0:Targeting:IncludedEnvironmentNames:0"] = "Production";
        builder.Configuration["Engine:Features:Flags:0:Targeting:IncludedTransportIds:0"] = "rest-api";
        builder.Configuration["Engine:Features:Flags:1:Id"] = "host.legacy-orders";
        builder.Configuration["Engine:Features:Flags:1:DisplayName"] = "Legacy Orders";
        builder.Configuration["Engine:Features:Flags:1:Description"] = "Keeps the legacy orders endpoint disabled.";
        builder.Configuration["Engine:Features:Flags:1:Enabled"] = "false";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new FeatureFlagProtectedEndpointModule());
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var previewResponse = await client.GetAsync("/api/feature-flags/orders/ord-42");
        var legacyResponse = await client.GetAsync("/api/feature-flags/orders/legacy/ord-42");
        var previewPayload = await previewResponse.Content.ReadAsStringAsync();
        var legacyPayload = await legacyResponse.Content.ReadAsStringAsync();
        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.True(
            previewResponse.StatusCode == HttpStatusCode.OK,
            $"Expected preview route to succeed but received {(int)previewResponse.StatusCode} {previewResponse.StatusCode}. Body: {previewPayload}");
        Assert.Equal(HttpStatusCode.NotFound, legacyResponse.StatusCode);
        Assert.Contains("Feature not available", legacyPayload, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(endpoints);
        Assert.NotNull(snapshot);

        var previewEndpoint = Assert.Single(endpoints, static endpoint =>
            string.Equals(endpoint.RoutePattern, "/api/feature-flags/orders/{orderId}", StringComparison.Ordinal));
        Assert.Equal(["host.orders-preview"], previewEndpoint.RequiredFeatureFlagIds);
        Assert.Equal(["host.orders-preview"], previewEndpoint.OriginalRequiredFeatureFlagIds);

        var legacyEndpoint = Assert.Single(endpoints, static endpoint =>
            string.Equals(endpoint.RoutePattern, "/api/feature-flags/orders/legacy/{orderId}", StringComparison.Ordinal));
        Assert.Equal(["host.legacy-orders"], legacyEndpoint.RequiredFeatureFlagIds);
        Assert.Equal(["host.legacy-orders"], legacyEndpoint.OriginalRequiredFeatureFlagIds);

        Assert.Contains(snapshot.RestEndpoints, static endpoint =>
            string.Equals(endpoint.RoutePattern, "/api/feature-flags/orders/{orderId}", StringComparison.Ordinal) &&
            endpoint.RequiredFeatureFlagIds.SequenceEqual(["host.orders-preview"], StringComparer.Ordinal));
        Assert.Contains(snapshot.RestEndpoints, static endpoint =>
            string.Equals(endpoint.RoutePattern, "/api/feature-flags/orders/legacy/{orderId}", StringComparison.Ordinal) &&
            endpoint.RequiredFeatureFlagIds.SequenceEqual(["host.legacy-orders"], StringComparer.Ordinal));
    }

    [Fact]
    public async Task MapCephalonEnforcesProviderBackedFeatureRequirementsForRestEndpoints()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.AddCephalon(engine =>
        {
            engine.AddFeatureFlag(new FeatureFlagDescriptor(
                id: "host.provider-preview",
                displayName: "Provider Preview",
                description: "Enables the preview endpoint only for the provider-approved subject.",
                enabled: true,
                providerBindings:
                [
                    new FeatureFlagProviderBindingDescriptor(
                        providerId: "subject-rollout",
                        providerFeatureId: "orders-preview")
                ]));
            engine.AddFeatureFlagProvider(new SubjectScopedFeatureFlagProvider("subject-rollout", "user-42"));
            engine.AddModule(new ProviderFeatureProtectedEndpointModule());
        });

        await using var app = builder.Build();
        app.Use(async (context, next) =>
        {
            if (context.Request.Headers.TryGetValue("X-Subject-Id", out var subjectId) &&
                !string.IsNullOrWhiteSpace(subjectId))
            {
                context.User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, subjectId.ToString())
                ], "test"));
            }

            await next(context);
        });
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var allowedRequest = new HttpRequestMessage(HttpMethod.Get, "/api/provider-feature-flags/orders/ord-42");
        allowedRequest.Headers.Add("X-Subject-Id", "user-42");
        var blockedRequest = new HttpRequestMessage(HttpMethod.Get, "/api/provider-feature-flags/orders/ord-42");
        blockedRequest.Headers.Add("X-Subject-Id", "user-404");

        var allowedResponse = await client.SendAsync(allowedRequest);
        var blockedResponse = await client.SendAsync(blockedRequest);
        var blockedPayload = await blockedResponse.Content.ReadAsStringAsync();
        var evaluation = await client.GetFromJsonAsync<FeatureFlagEvaluationResult>(
            "/engine/features/host.provider-preview/evaluate?subjectId=user-42");
        var descriptor = await client.GetFromJsonAsync<FeatureFlagDescriptor>("/engine/features/host.provider-preview");

        Assert.Equal(HttpStatusCode.OK, allowedResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, blockedResponse.StatusCode);
        Assert.Contains("Feature not available", blockedPayload, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(evaluation);
        Assert.NotNull(descriptor);
        Assert.Single(descriptor!.ProviderBindings);
        Assert.Equal("subject-rollout", descriptor.ProviderBindings[0].ProviderId);
        Assert.True(evaluation!.IsEnabled);
        var providerResult = Assert.Single(evaluation.ProviderResults);
        Assert.True(providerResult.IsDefined);
        Assert.True(providerResult.IsEnabled);
        Assert.Equal("subject-rollout", providerResult.ProviderId);
        Assert.Equal("orders-preview", providerResult.ProviderFeatureId);
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

    private sealed class FeatureFlagProtectedEndpointModule : ModuleBase, IEndpointModule
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            id: "feature-flags-rest-tests",
            displayName: "Feature Flags REST Tests",
            description: "Publishes REST endpoints protected by Cephalon feature requirements.");

        public void MapEndpoints(IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGet(
                    "/feature-flags/orders/{orderId}",
                    static (string orderId) => TypedResults.Ok(new FeatureFlagProtectedOrderOutput(orderId, "preview")))
                .WithTags("Pilot Orders API")
                .RequireFeatureFlag("host.orders-preview");

            endpoints.MapGet(
                    "/feature-flags/orders/legacy/{orderId}",
                    static (string orderId) => TypedResults.Ok(new FeatureFlagProtectedOrderOutput(orderId, "legacy")))
                .WithTags("Legacy Orders API")
                .RequireFeatureFlag("host.legacy-orders");
        }
    }

    private sealed class ProviderFeatureProtectedEndpointModule : ModuleBase, IEndpointModule
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            id: "feature-flags-provider-rest-tests",
            displayName: "Feature Flags Provider REST Tests",
            description: "Publishes REST endpoints protected by provider-backed Cephalon feature requirements.");

        public void MapEndpoints(IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGet(
                    "/provider-feature-flags/orders/{orderId}",
                    static (string orderId) => TypedResults.Ok(new FeatureFlagProtectedOrderOutput(orderId, "provider-preview")))
                .WithTags("Provider Orders API")
                .RequireFeatureFlag("host.provider-preview");
        }
    }

    private sealed class SubjectScopedFeatureFlagProvider(
        string providerId,
        string allowedSubjectId) : IFeatureFlagProvider
    {
        public string ProviderId { get; } = providerId;

        public FeatureFlagProviderEvaluationResult Evaluate(
            FeatureFlagProviderBindingDescriptor binding,
            FeatureFlagDescriptor featureFlag,
            FeatureFlagEvaluationContext? context = null)
        {
            var providerFeatureId = binding.ResolveProviderFeatureId(featureFlag.Id);
            if (string.Equals(context?.SubjectId, allowedSubjectId, StringComparison.OrdinalIgnoreCase))
            {
                return new FeatureFlagProviderEvaluationResult(
                    ProviderId,
                    providerFeatureId,
                    IsDefined: true,
                    IsEnabled: true,
                    Reason: "Provider allowed the supplied subject.");
            }

            return new FeatureFlagProviderEvaluationResult(
                ProviderId,
                providerFeatureId,
                IsDefined: true,
                IsEnabled: false,
                Reason: "Provider rejected the supplied subject.");
        }
    }

    private sealed record FeatureFlagProtectedOrderOutput(string OrderId, string Mode);
}
