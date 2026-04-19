using System.Net;
using System.Net.Http.Json;
using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.Features;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Technologies;
using Cephalon.Abstractions.Transports;
using Cephalon.AspNetCore.Hosting;
using Cephalon.Behaviors.Http.Hosting;
using Cephalon.Behaviors.Hosting;
using Cephalon.Engine.Runtime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;

namespace Cephalon.Tests.Hosting;

public sealed class BehaviorFeatureFlagHostingTests
{
    [Fact]
    public async Task MapCephalonPropagatesBehaviorFeatureRequirementsToRestRuntimeTruth()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Production"
        });
        builder.WebHost.UseTestServer();
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["Engine:Features:Flags:0:Id"] = "host.behavior-preview";
        builder.Configuration["Engine:Features:Flags:0:DisplayName"] = "Behavior Preview";
        builder.Configuration["Engine:Features:Flags:0:Description"] = "Enables the preview behavior over REST.";
        builder.Configuration["Engine:Features:Flags:0:Enabled"] = "true";
        builder.Configuration["Engine:Features:Flags:0:Targeting:IncludedEnvironmentNames:0"] = "Production";
        builder.Configuration["Engine:Features:Flags:0:Targeting:IncludedModuleIds:0"] = "tests.behavior-feature-flags";
        builder.Configuration["Engine:Features:Flags:0:Targeting:IncludedBehaviorIds:0"] = "tests.behavior-feature-flags.get-order";
        builder.Configuration["Engine:Features:Flags:0:Targeting:IncludedTransportIds:0"] = "rest-api";
        builder.AddCephalon(engine =>
        {
            engine.AddBehaviors(options => options.AutoRegister = false);
            engine.AddModule(new FeatureFlaggedBehaviorModule());
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/api/behavior-feature-flags/orders/ord-42");
        var payload = await response.Content.ReadAsStringAsync();
        var endpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.True(
            response.StatusCode == HttpStatusCode.OK,
            $"Expected feature-gated behavior route to succeed but received {(int)response.StatusCode} {response.StatusCode}. Body: {payload}");
        Assert.NotNull(endpoints);
        Assert.NotNull(snapshot);

        var endpoint = Assert.Single(endpoints, static candidate =>
            string.Equals(candidate.RoutePattern, "/api/behavior-feature-flags/orders/{orderId}", StringComparison.Ordinal));
        Assert.Equal(["host.behavior-preview"], endpoint.RequiredFeatureFlagIds);

        Assert.Contains(snapshot.RestEndpoints, static candidate =>
            string.Equals(candidate.RoutePattern, "/api/behavior-feature-flags/orders/{orderId}", StringComparison.Ordinal) &&
            candidate.RequiredFeatureFlagIds.SequenceEqual(["host.behavior-preview"], StringComparer.Ordinal));

        var behaviorSurface = Assert.Single(snapshot.TechnologySurfaces, static surface =>
            string.Equals(surface.SurfaceId, "behaviors", StringComparison.Ordinal));
        var behaviorEntry = Assert.Single(behaviorSurface.Entries, static entry =>
            string.Equals(entry.Id, "tests.behavior-feature-flags.get-order", StringComparison.Ordinal));
        Assert.Equal("tests.behavior-feature-flags", behaviorEntry.Metadata["sourceModuleId"]);
        Assert.Equal("host.behavior-preview", behaviorEntry.Metadata["requiredFeatureFlagIds"]);
    }

    [Fact]
    public async Task MapCephalonReturnsNotFoundWhenBehaviorFeatureRequirementDoesNotMatchEnvironment()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Staging"
        });
        builder.WebHost.UseTestServer();
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.Configuration["Engine:Features:Flags:0:Id"] = "host.behavior-preview";
        builder.Configuration["Engine:Features:Flags:0:DisplayName"] = "Behavior Preview";
        builder.Configuration["Engine:Features:Flags:0:Description"] = "Enables the preview behavior only in production.";
        builder.Configuration["Engine:Features:Flags:0:Enabled"] = "true";
        builder.Configuration["Engine:Features:Flags:0:Targeting:IncludedEnvironmentNames:0"] = "Production";
        builder.Configuration["Engine:Features:Flags:0:Targeting:IncludedModuleIds:0"] = "tests.behavior-feature-flags";
        builder.Configuration["Engine:Features:Flags:0:Targeting:IncludedBehaviorIds:0"] = "tests.behavior-feature-flags.get-order";
        builder.Configuration["Engine:Features:Flags:0:Targeting:IncludedTransportIds:0"] = "rest-api";
        builder.AddCephalon(engine =>
        {
            engine.AddBehaviors(options => options.AutoRegister = false);
            engine.AddModule(new FeatureFlaggedBehaviorModule());
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/api/behavior-feature-flags/orders/ord-42");
        var payload = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("Feature not available", payload, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("environment", payload, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class FeatureFlaggedBehaviorModule : RestBehaviorModuleBase
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            id: "tests.behavior-feature-flags",
            displayName: "Behavior Feature Flags",
            description: "Publishes a REST behavior that is gated by a behavior-level feature flag.");

        public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
        {
            behaviors.Group("/behavior-feature-flags/orders")
                .MapGet<GetFeatureFlaggedOrderBehavior>(
                    "/{orderId}",
                    topology => topology
                        .AsDirect()
                        .RequireFeatureFlag("host.behavior-preview"));
        }
    }

    [AppBehavior("tests.behavior-feature-flags.get-order")]
    private sealed class GetFeatureFlaggedOrderBehavior : IAppBehavior<string, FeatureFlaggedOrderOutput>
    {
        public Task<FeatureFlaggedOrderOutput> HandleAsync(
            string input,
            IBehaviorContext context,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new FeatureFlaggedOrderOutput(input, "preview"));
    }

    private sealed record FeatureFlaggedOrderOutput(string OrderId, string Mode);
}
