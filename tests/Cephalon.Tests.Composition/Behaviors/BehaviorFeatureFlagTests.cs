using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.Features;
using Cephalon.Behaviors.Builders;
using Cephalon.Behaviors.Hosting;
using Cephalon.Behaviors.Services;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Behaviors;

public sealed class BehaviorFeatureFlagTests
{
    [Fact]
    public void BehaviorTopologyBuilderNormalizesRequiredFeatureFlags()
    {
        var descriptor = new BehaviorTopologyBuilder()
            .RequireFeatureFlags(" host.preview ", "host.preview", "module.rollout")
            .Build("tests.feature-flags.normalized");

        Assert.Equal(["host.preview", "module.rollout"], descriptor.RequiredFeatureFlagIds);
    }

    [Fact]
    public async Task DispatcherAllowsExecutionWhenRequiredFeatureFlagMatchesRuntimeContext()
    {
        var services = new ServiceCollection();
        var builder = new EngineBuilder(services);
        builder.UseSettings(new EngineSettings(blueprint: "ModularMonolith"));
        builder.AddFeatureFlag(new FeatureFlagDescriptor(
            id: "host.behavior-preview",
            displayName: "Behavior Preview",
            description: "Enables the preview behavior for in-memory traffic.",
            enabled: true,
            targeting: new FeatureFlagTargetingDescriptor(
                includedBehaviorIds: ["tests.behavior.feature-flags"],
                includedTransportIds: ["in-memory"])));
        builder.AddBehaviors(
            configureOptions: options => options.AutoRegister = false,
            configure: behaviors => behaviors.Register<FeatureFlaggedBehavior>(topology => topology
                .AsDirect()
                .ViaInMemory()
                .RequireFeatureFlag("host.behavior-preview")));

        builder.Build();

        using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<BehaviorDispatcher>();

        var context = new TestBehaviorContext(
            "tests.behavior.feature-flags",
            isDirect: true,
            metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["TransportId"] = "in-memory"
            });

        var result = await dispatcher.DispatchAsync(
            "tests.behavior.feature-flags",
            "Cephalon",
            context);

        Assert.Equal("Feature gate passed for Cephalon.", result);
    }

    [Fact]
    public async Task DispatcherThrowsWhenRequiredFeatureFlagDoesNotMatchTransport()
    {
        var services = new ServiceCollection();
        var builder = new EngineBuilder(services);
        builder.UseSettings(new EngineSettings(blueprint: "ModularMonolith"));
        builder.AddFeatureFlag(new FeatureFlagDescriptor(
            id: "host.rest-only-preview",
            displayName: "REST Only Preview",
            description: "Enables the behavior only for REST traffic.",
            enabled: true,
            targeting: new FeatureFlagTargetingDescriptor(
                includedBehaviorIds: ["tests.behavior.feature-flags"],
                includedTransportIds: ["rest-api"])));
        builder.AddBehaviors(
            configureOptions: options => options.AutoRegister = false,
            configure: behaviors => behaviors.Register<FeatureFlaggedBehavior>(topology => topology
                .AsDirect()
                .ViaInMemory()
                .RequireFeatureFlag("host.rest-only-preview")));

        builder.Build();

        using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<BehaviorDispatcher>();

        var context = new TestBehaviorContext(
            "tests.behavior.feature-flags",
            isDirect: true,
            metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["TransportId"] = "in-memory"
            });

        var exception = await Assert.ThrowsAsync<BehaviorFeatureDisabledException>(() =>
            dispatcher.DispatchAsync(
                "tests.behavior.feature-flags",
                "Cephalon",
                context));

        Assert.Equal("tests.behavior.feature-flags", exception.BehaviorId);
        Assert.Equal("host.rest-only-preview", exception.FeatureFlagId);
        Assert.Equal(["host.rest-only-preview"], exception.RequiredFeatureFlagIds);
        Assert.Contains("transport", exception.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [AppBehavior("tests.behavior.feature-flags")]
    private sealed class FeatureFlaggedBehavior : IAppBehavior<string, string>
    {
        public Task<string> HandleAsync(
            string input,
            IBehaviorContext context,
            CancellationToken cancellationToken = default)
            => Task.FromResult($"Feature gate passed for {input}.");
    }
}
