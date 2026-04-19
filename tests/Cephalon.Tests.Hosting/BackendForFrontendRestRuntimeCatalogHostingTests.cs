using System.Net.Http.Json;
using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.Modules;
using Cephalon.Abstractions.Patterns;
using Cephalon.Abstractions.Transports;
using Cephalon.AspNetCore.Hosting;
using Cephalon.AspNetCore.Transports.Rest;
using Cephalon.Behaviors.Hosting;
using Cephalon.Behaviors.Http.Abstractions;
using Cephalon.Behaviors.Http.Hosting;
using Cephalon.Engine.Runtime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;

namespace Cephalon.Tests.Hosting;

public sealed class BackendForFrontendRestRuntimeCatalogHostingTests
{
    [Fact]
    public async Task MapCephalonExposesClientAwareRestEndpointsWithoutInventingSeparateRuntimeTruth()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new BackendForFrontendRestCatalogModule());
            engine.AddBackendForFrontendClientBinding(new BackendForFrontendClientBindingDescriptor(
                id: "orders-read-rest",
                clientId: "orders-app",
                sourceModuleId: "tests.bff.rest.host.orders",
                displayName: "Orders Read REST",
                description: "Projects capability-scoped order lookup endpoints for the orders app.",
                transportId: "rest-api",
                entryPoint: "/api/v1/tests/bff-runtime/storefront/orders",
                behaviorFilter: new BackendForFrontendBehaviorFilterDescriptor(
                    includedCapabilityKeys: ["orders.read"])));
            engine.AddBackendForFrontendClientBinding(new BackendForFrontendClientBindingDescriptor(
                id: "public-rest",
                clientId: "public",
                sourceModuleId: "tests.bff.rest.host.public",
                displayName: "Public REST",
                description: "Projects the public REST surface while hiding admin routes.",
                transportId: "rest-api",
                entryPoint: "/api/v1/tests/bff-runtime",
                behaviorFilter: new BackendForFrontendBehaviorFilterDescriptor(
                    excludedTags: ["admin"])));
            engine.AddBackendForFrontendClientBinding(new BackendForFrontendClientBindingDescriptor(
                id: "full-rest",
                clientId: "full",
                sourceModuleId: "tests.bff.rest.host.full",
                displayName: "Full REST",
                description: "Projects every published REST endpoint without additional filtering.",
                transportId: "rest-api",
                entryPoint: "/api/v1/tests/bff-runtime"));
            engine.AddBackendForFrontendClientBinding(new BackendForFrontendClientBindingDescriptor(
                id: "graphql-storefront",
                clientId: "storefront-graphql",
                sourceModuleId: "tests.bff.rest.host.graphql",
                displayName: "Storefront GraphQL",
                description: "Demonstrates that non-REST client bindings stay out of the REST runtime catalog.",
                transportId: "graphql",
                entryPoint: "/graphql/storefront",
                behaviorFilter: new BackendForFrontendBehaviorFilterDescriptor(
                    includedBehaviorIds: ["tests.bff.storefront.lookup"])));
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var runtimeEndpoints = await client.GetFromJsonAsync<BackendForFrontendRestEndpointRuntimeDescriptor[]>("/engine/backend-for-frontend/rest-endpoints");
        var restEndpoints = await client.GetFromJsonAsync<RestEndpointRuntimeDescriptor[]>("/engine/rest-endpoints");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(runtimeEndpoints);
        Assert.NotNull(restEndpoints);
        Assert.NotNull(snapshot);
        Assert.Equal(7, runtimeEndpoints.Length);
        Assert.Equal(7, snapshot.BackendForFrontendRestEndpoints.Count);

        var mobileEntry = Assert.Single(runtimeEndpoints, static item =>
            string.Equals(item.BindingId, "mobile-rest", StringComparison.Ordinal));
        Assert.Equal("mobile", mobileEntry.ClientId);
        Assert.Equal("tests.bff.rest.runtime", mobileEntry.SourceModuleId);
        Assert.False(mobileEntry.MatchedByDefault);
        Assert.Equal(["tests.bff.mobile.lookup"], mobileEntry.MatchedBehaviorIds);
        Assert.Empty(mobileEntry.MatchedCapabilityKeys);
        Assert.Equal(["mobile"], mobileEntry.MatchedTags);
        Assert.Equal("tests.bff.mobile.lookup", mobileEntry.Endpoint.BehaviorId);
        Assert.Equal(mobileEntry.Endpoint.Id, mobileEntry.RestEndpointId);

        var capabilityEntry = Assert.Single(runtimeEndpoints, static item =>
            string.Equals(item.BindingId, "orders-read-rest", StringComparison.Ordinal));
        Assert.False(capabilityEntry.MatchedByDefault);
        Assert.Empty(capabilityEntry.MatchedBehaviorIds);
        Assert.Equal(["orders.read"], capabilityEntry.MatchedCapabilityKeys);
        Assert.Empty(capabilityEntry.MatchedTags);
        Assert.Equal("tests.bff.storefront.lookup", capabilityEntry.Endpoint.BehaviorId);

        var publicEntries = await client.GetFromJsonAsync<BackendForFrontendRestEndpointRuntimeDescriptor[]>("/engine/backend-for-frontend/rest-endpoints/bindings/public-rest");
        Assert.NotNull(publicEntries);
        Assert.Equal(2, publicEntries.Length);
        Assert.All(publicEntries, static item =>
        {
            Assert.True(item.MatchedByDefault);
            Assert.Empty(item.MatchedBehaviorIds);
            Assert.Empty(item.MatchedCapabilityKeys);
            Assert.Empty(item.MatchedTags);
            Assert.DoesNotContain("admin", item.Endpoint.Tags, StringComparer.OrdinalIgnoreCase);
        });

        var fullEntries = await client.GetFromJsonAsync<BackendForFrontendRestEndpointRuntimeDescriptor[]>("/engine/backend-for-frontend/rest-endpoints/clients/full");
        Assert.NotNull(fullEntries);
        Assert.Equal(3, fullEntries.Length);
        Assert.All(fullEntries, static item => Assert.True(item.MatchedByDefault));

        var moduleEntries = await client.GetFromJsonAsync<BackendForFrontendRestEndpointRuntimeDescriptor[]>("/engine/backend-for-frontend/rest-endpoints/modules/tests.bff.rest.runtime");
        Assert.NotNull(moduleEntries);
        Assert.Single(moduleEntries);
        Assert.Equal(mobileEntry.Id, moduleEntries[0].Id);

        var storefrontEndpoint = Assert.Single(restEndpoints, static endpoint =>
            string.Equals(endpoint.BehaviorId, "tests.bff.storefront.lookup", StringComparison.Ordinal));
        var storefrontRuntimeEntries = await client.GetFromJsonAsync<BackendForFrontendRestEndpointRuntimeDescriptor[]>(
            $"/engine/backend-for-frontend/rest-endpoints/published/{Uri.EscapeDataString(storefrontEndpoint.Id)}");
        Assert.NotNull(storefrontRuntimeEntries);
        Assert.Equal(3, storefrontRuntimeEntries.Length);
        Assert.Contains(storefrontRuntimeEntries, static item => string.Equals(item.BindingId, "orders-read-rest", StringComparison.Ordinal));
        Assert.Contains(storefrontRuntimeEntries, static item => string.Equals(item.BindingId, "public-rest", StringComparison.Ordinal));
        Assert.Contains(storefrontRuntimeEntries, static item => string.Equals(item.BindingId, "full-rest", StringComparison.Ordinal));

        var runtimeEndpointById = await client.GetFromJsonAsync<BackendForFrontendRestEndpointRuntimeDescriptor>(
            $"/engine/backend-for-frontend/rest-endpoints/{Uri.EscapeDataString(mobileEntry.Id)}");
        Assert.NotNull(runtimeEndpointById);
        Assert.Equal(mobileEntry.Id, runtimeEndpointById.Id);
        Assert.Equal(mobileEntry.Endpoint.Id, runtimeEndpointById.Endpoint.Id);

        Assert.DoesNotContain(runtimeEndpoints, static item =>
            string.Equals(item.BindingId, "graphql-storefront", StringComparison.Ordinal));
        Assert.Contains(snapshot.BackendForFrontendRestEndpoints, item => item.Id == mobileEntry.Id);
        Assert.Contains(snapshot.BackendForFrontendRestEndpoints, item =>
            string.Equals(item.BindingId, "full-rest", StringComparison.Ordinal) &&
            string.Equals(item.Endpoint.BehaviorId, "tests.bff.admin.lookup", StringComparison.Ordinal));
        Assert.DoesNotContain(snapshot.BackendForFrontendRestEndpoints, item =>
            string.Equals(item.BindingId, "public-rest", StringComparison.Ordinal) &&
            string.Equals(item.Endpoint.BehaviorId, "tests.bff.admin.lookup", StringComparison.Ordinal));
    }

    private sealed class BackendForFrontendRestCatalogModule : RestBehaviorModuleBase, IBackendForFrontendClientBindingContributor
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            id: "tests.bff.rest.runtime",
            displayName: "Backend for Frontend REST Runtime Tests",
            description: "Publishes behavior-backed REST endpoints and one module-owned BFF binding for runtime catalog coverage.");

        public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
        {
            behaviors.Group("/tests/bff-runtime/mobile/orders")
                .ApiVersion(1)
                .WithTagName("mobile")
                .MapProfile<GetBffMobileOrderBehavior>();

            behaviors.Group("/tests/bff-runtime/storefront/orders")
                .ApiVersion(1)
                .WithTagName("storefront")
                .MapProfile<GetBffStorefrontOrderBehavior>(builder =>
                    builder.RequireCapability("orders.read"));

            behaviors.Group("/tests/bff-runtime/admin/orders")
                .ApiVersion(1)
                .WithTagName("admin")
                .MapProfile<GetBffAdminOrderBehavior>(builder =>
                    builder.RequireCapability("admin.read"));
        }

        public void RegisterClientBindings(IBackendForFrontendClientBindingRegistry bindings)
        {
            bindings.Add(new BackendForFrontendClientBindingDescriptor(
                id: "mobile-rest",
                clientId: "mobile",
                sourceModuleId: Descriptor.Id,
                displayName: "Mobile REST",
                description: "Projects the mobile order lookup surface through the owning module.",
                transportId: "rest-api",
                entryPoint: "/api/v1/tests/bff-runtime/mobile/orders",
                behaviorFilter: new BackendForFrontendBehaviorFilterDescriptor(
                    includedBehaviorIds: ["tests.bff.mobile.lookup"],
                    includedTags: ["mobile"])));
        }
    }

    [AppBehavior("tests.bff.mobile.lookup")]
    [BehaviorRestProfile(BehaviorRestMethod.Get, "/{orderId}", ApiVersionMajor = 1)]
    private sealed class GetBffMobileOrderBehavior : IAppBehavior<BackendForFrontendRuntimeOrderInput, BackendForFrontendRuntimeOrderOutput>
    {
        public Task<BackendForFrontendRuntimeOrderOutput> HandleAsync(
            BackendForFrontendRuntimeOrderInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(new BackendForFrontendRuntimeOrderOutput(input.OrderId));
        }
    }

    [AppBehavior("tests.bff.storefront.lookup")]
    [BehaviorRestProfile(BehaviorRestMethod.Get, "/{orderId}", ApiVersionMajor = 1)]
    private sealed class GetBffStorefrontOrderBehavior : IAppBehavior<BackendForFrontendRuntimeOrderInput, BackendForFrontendRuntimeOrderOutput>
    {
        public Task<BackendForFrontendRuntimeOrderOutput> HandleAsync(
            BackendForFrontendRuntimeOrderInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(new BackendForFrontendRuntimeOrderOutput(input.OrderId));
        }
    }

    [AppBehavior("tests.bff.admin.lookup")]
    [BehaviorRestProfile(BehaviorRestMethod.Get, "/{orderId}", ApiVersionMajor = 1)]
    private sealed class GetBffAdminOrderBehavior : IAppBehavior<BackendForFrontendRuntimeOrderInput, BackendForFrontendRuntimeOrderOutput>
    {
        public Task<BackendForFrontendRuntimeOrderOutput> HandleAsync(
            BackendForFrontendRuntimeOrderInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            return Task.FromResult(new BackendForFrontendRuntimeOrderOutput(input.OrderId));
        }
    }

    private sealed record BackendForFrontendRuntimeOrderInput(string OrderId);

    private sealed record BackendForFrontendRuntimeOrderOutput(string OrderId);
}
