using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.Modules;
using Cephalon.AspNetCore.Transports.Rest;
using Cephalon.Behaviors.Http.Abstractions;
using Cephalon.Behaviors.Http.Hosting;
using Microsoft.AspNetCore.Builder;

namespace Cephalon.Benchmarks.Support;

internal static class BenchmarkRestScenarioIds
{
    internal const string GovernanceModuleId = "bench.rest.governance";
    internal const string GeneratedGroupsModuleId = "bench.rest.generated-groups";

    internal const string ThreeWayBehaviorId = "bench.rest.threeway.lookup";
    internal const string AuthoringPolicyBehaviorId = "bench.rest.authoring.lookup";
    internal const string BindingOverrideBehaviorId = "bench.rest.bindings.lookup";
    internal const string ExplicitGovernedBehaviorId = "bench.rest.explicit.lookup";
    internal const string GroupedOrdersLookupBehaviorId = "bench.rest.grouped.orders.lookup";
    internal const string GroupedOrdersCreateBehaviorId = "bench.rest.grouped.orders.create";
    internal const string GroupedInventoryLookupBehaviorId = "bench.rest.grouped.inventory.lookup";

    internal const string GroupedOrdersPrefix = "bench.rest.grouped.orders";
    internal const string GroupedScope = "benchmark-grouped";
    internal const string ExplicitScope = "benchmark-explicit";

    internal const string BindingOverrideRuleId = "govern-benchmark-bindings";
    internal const string ExplicitScopeOverrideRuleId = "govern-benchmark-explicit-scope";
    internal const string ExplicitExactOverrideRuleId = "govern-benchmark-explicit-lookup";
    internal const string GroupedPrefixSuppressionRuleId = "hide-benchmark-grouped-orders";
    internal const string GroupedLookupSuppressionRuleId = "hide-benchmark-grouped-orders-lookup";

    internal const string ThreeWayEndpointName = "bench_rest_three_way_lookup";
    internal const string ThreeWayPublishedEndpointName = "bench_rest_governance.v6.bench_rest_threeway_lookup";
    internal const string BindingEndpointName = "bench_rest_bindings_lookup";
    internal const string GovernedBindingEndpointName = "bench_rest_bindings_lookup_governed";
    internal const string ExplicitEndpointName = "bench_rest_explicit_lookup";
    internal const string GovernedExplicitEndpointName = "bench_rest_explicit_governed_lookup";
    internal const string GroupedInventoryPublishedEndpointName = "bench_rest_generated_groups.v9.bench_rest_grouped_inventory_lookup";

    internal const string BindingSourceCapabilityKey = "benchmark.orders.bindings.source";
    internal const string BindingGovernedCapabilityKey = "benchmark.orders.bindings.governed";
    internal const string ExplicitSourceCapabilityKey = "benchmark.orders.explicit.source";
    internal const string ExplicitScopeCapabilityKey = "benchmark.orders.explicit.scope";
    internal const string ExplicitGovernedCapabilityKey = "benchmark.orders.explicit.governed";
}

internal sealed class BenchmarkRestGovernanceModule : RestBehaviorModuleBase
{
    public override ModuleDescriptor Descriptor { get; } = new(
        BenchmarkRestScenarioIds.GovernanceModuleId,
        "Benchmark REST Governance Module",
        "Exercises precedence, authoring policy, override, and opt-in governance REST projection paths.",
        version: "1.0.0");

    public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
    {
        behaviors.Group("/bench/runtime/three-way")
            .ApiVersion(6)
            .WithTagName("Benchmark Three Way API")
            .MapGeneratedProfiles("bench.rest.threeway")
            .MapProfile<BenchmarkThreeWayBehavior>(
                configureEndpoint: (Action<RouteHandlerBuilder>)(static endpoint => endpoint
                    .WithName("bench_rest_three_way_lookup_profile")))
            .MapGet<BenchmarkThreeWayBehavior>(
                "/explicit/{orderId}",
                configureEndpoint: (Action<RouteHandlerBuilder>)(static endpoint => endpoint
                    .WithName(BenchmarkRestScenarioIds.ThreeWayEndpointName)));

        behaviors.Group("/bench/runtime/authoring")
            .ApiVersion(7)
            .WithTagName("Benchmark Authoring Policy API")
            .MapGeneratedProfiles("bench.rest.authoring")
            .MapProfile<BenchmarkAuthoringPolicyBehavior>(
                configureEndpoint: (Action<RouteHandlerBuilder>)(static endpoint => endpoint
                    .WithName("bench_rest_authoring_lookup_profile")))
            .MapGet<BenchmarkAuthoringPolicyBehavior>(
                "/explicit/{orderId}",
                configureEndpoint: (Action<RouteHandlerBuilder>)(static endpoint => endpoint
                    .WithName("bench_rest_authoring_lookup_dsl")));

        behaviors.Group("/bench/runtime/bindings")
            .WithTagName("Benchmark Binding API")
            .MapProfile<BenchmarkBindingOverrideBehavior>(
                configureEndpoint: (Action<RouteHandlerBuilder>)(static endpoint => endpoint
                    .WithName(BenchmarkRestScenarioIds.BindingEndpointName)
                    .RequireCapability(BenchmarkRestScenarioIds.BindingSourceCapabilityKey)));

        behaviors.Group("/bench/runtime/explicit")
            .ApiVersion(11)
            .WithTagName("Benchmark Explicit API")
            .WithHostGovernanceScope(BenchmarkRestScenarioIds.ExplicitScope)
            .AllowHostGovernance()
            .MapGet<BenchmarkExplicitGovernedBehavior>(
                "/{orderId}",
                configureEndpoint: (Action<RouteHandlerBuilder>)(static endpoint => endpoint
                    .WithName(BenchmarkRestScenarioIds.ExplicitEndpointName)
                    .RequireCapability(BenchmarkRestScenarioIds.ExplicitSourceCapabilityKey)));
    }
}

internal sealed class BenchmarkRestGeneratedGroupsModule : RestBehaviorModuleBase
{
    public override ModuleDescriptor Descriptor { get; } = new(
        BenchmarkRestScenarioIds.GeneratedGroupsModuleId,
        "Benchmark REST Generated Groups Module",
        "Exercises low-code generated REST groups with preserved module ownership.",
        version: "1.0.0");

    public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
    {
        behaviors.MapGeneratedProfileGroups(
            "bench.rest.grouped",
            group => group
                .WithTagName("Benchmark Grouped API")
                .WithHostGovernanceScope(BenchmarkRestScenarioIds.GroupedScope)
                .AllowHostGovernance());
    }
}

[AppBehavior(BenchmarkRestScenarioIds.ThreeWayBehaviorId)]
[BehaviorRestProfile(BehaviorRestMethod.Get, "/{orderId}", ApiVersionMajor = 6)]
internal sealed class BenchmarkThreeWayBehavior : IAppBehavior<BenchmarkOrderLookupInput, BenchmarkOrderLookupOutput>
{
    public Task<BenchmarkOrderLookupOutput> HandleAsync(
        BenchmarkOrderLookupInput input,
        IBehaviorContext context,
        CancellationToken ct = default)
    {
        return Task.FromResult(new BenchmarkOrderLookupOutput(input.OrderId));
    }
}

[AppBehavior(BenchmarkRestScenarioIds.AuthoringPolicyBehaviorId)]
[BehaviorRestProfile(BehaviorRestMethod.Get, "/{orderId}", ApiVersionMajor = 7)]
internal sealed class BenchmarkAuthoringPolicyBehavior : IAppBehavior<BenchmarkOrderLookupInput, BenchmarkOrderLookupOutput>
{
    public Task<BenchmarkOrderLookupOutput> HandleAsync(
        BenchmarkOrderLookupInput input,
        IBehaviorContext context,
        CancellationToken ct = default)
    {
        return Task.FromResult(new BenchmarkOrderLookupOutput(input.OrderId));
    }
}

[AppBehavior(BenchmarkRestScenarioIds.BindingOverrideBehaviorId)]
[BehaviorRestProfile(BehaviorRestMethod.Get, "/lookup/{orderId}", ApiVersionMajor = 10, PreserveImplicitQueryFallback = true)]
[BehaviorRestBinding(nameof(BenchmarkBoundLookupInput.OrderId), BehaviorRestBindingSource.Route, Name = "orderId")]
[BehaviorRestBinding(nameof(BenchmarkBoundLookupInput.Quantity), BehaviorRestBindingSource.Query, Name = "quantity")]
internal sealed class BenchmarkBindingOverrideBehavior : IAppBehavior<BenchmarkBoundLookupInput, BenchmarkBoundLookupOutput>
{
    public Task<BenchmarkBoundLookupOutput> HandleAsync(
        BenchmarkBoundLookupInput input,
        IBehaviorContext context,
        CancellationToken ct = default)
    {
        return Task.FromResult(new BenchmarkBoundLookupOutput(
            input.OrderId,
            input.Quantity,
            input.Status));
    }
}

[AppBehavior(BenchmarkRestScenarioIds.ExplicitGovernedBehaviorId)]
internal sealed class BenchmarkExplicitGovernedBehavior : IAppBehavior<BenchmarkOrderLookupInput, BenchmarkOrderLookupOutput>
{
    public Task<BenchmarkOrderLookupOutput> HandleAsync(
        BenchmarkOrderLookupInput input,
        IBehaviorContext context,
        CancellationToken ct = default)
    {
        return Task.FromResult(new BenchmarkOrderLookupOutput(input.OrderId));
    }
}

[AppBehavior(BenchmarkRestScenarioIds.GroupedOrdersLookupBehaviorId)]
[BehaviorRestProfile(BehaviorRestMethod.Get, "/{orderId}", ApiVersionMajor = 8)]
internal sealed class BenchmarkGroupedOrdersLookupBehavior : IAppBehavior<BenchmarkOrderLookupInput, BenchmarkOrderLookupOutput>
{
    public Task<BenchmarkOrderLookupOutput> HandleAsync(
        BenchmarkOrderLookupInput input,
        IBehaviorContext context,
        CancellationToken ct = default)
    {
        return Task.FromResult(new BenchmarkOrderLookupOutput(input.OrderId));
    }
}

[AppBehavior(BenchmarkRestScenarioIds.GroupedOrdersCreateBehaviorId)]
[BehaviorRestProfile(BehaviorRestMethod.Post, "/{orderId}/items", ApiVersionMajor = 8)]
internal sealed class BenchmarkGroupedOrdersCreateBehavior : IAppBehavior<BenchmarkCreateOrderItemInput, BenchmarkOrderLookupOutput>
{
    public Task<BenchmarkOrderLookupOutput> HandleAsync(
        BenchmarkCreateOrderItemInput input,
        IBehaviorContext context,
        CancellationToken ct = default)
    {
        return Task.FromResult(new BenchmarkOrderLookupOutput(input.OrderId));
    }
}

[AppBehavior(BenchmarkRestScenarioIds.GroupedInventoryLookupBehaviorId)]
[BehaviorRestProfile(BehaviorRestMethod.Get, "/{sku}", ApiVersionMajor = 9)]
internal sealed class BenchmarkGroupedInventoryLookupBehavior : IAppBehavior<BenchmarkInventoryLookupInput, BenchmarkInventoryLookupOutput>
{
    public Task<BenchmarkInventoryLookupOutput> HandleAsync(
        BenchmarkInventoryLookupInput input,
        IBehaviorContext context,
        CancellationToken ct = default)
    {
        return Task.FromResult(new BenchmarkInventoryLookupOutput(input.Sku));
    }
}

internal sealed record BenchmarkOrderLookupInput(string OrderId);

internal sealed record BenchmarkOrderLookupOutput(string OrderId);

internal sealed record BenchmarkBoundLookupInput(
    string OrderId,
    int Quantity,
    string? Status = null);

internal sealed record BenchmarkBoundLookupOutput(
    string OrderId,
    int Quantity,
    string? Status);

internal sealed record BenchmarkCreateOrderItemInput(
    string OrderId,
    string? ProductId = null);

internal sealed record BenchmarkInventoryLookupInput(string Sku);

internal sealed record BenchmarkInventoryLookupOutput(string Sku);
