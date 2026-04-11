using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.Resilience;
using Cephalon.Behaviors.Hosting;
using Cephalon.Behaviors.Services;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Polly.CircuitBreaker;
using Polly.RateLimiting;
using Polly.Timeout;

namespace Cephalon.Tests.Behaviors;

public sealed class BehaviorResilienceTests
{
    [Fact]
    public void AddBehaviorsUsesCodeFirstResilienceSettingsForCatalogAndSnapshot()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularMonolith",
                resilience: new ResilienceSettings(
                    retry: new RetrySettings(
                        enabled: true,
                        maxAttempts: 3,
                        backoff: "Exponential",
                        baseDelayMilliseconds: 100,
                        maxDelayMilliseconds: 300,
                        useJitter: true),
                    timeout: new TimeoutSettings(
                        enabled: true,
                        totalTimeoutSeconds: 12,
                        attemptTimeoutSeconds: 4),
                    circuitBreaker: new CircuitBreakerSettings(
                        enabled: true,
                        failureRatio: 0.5m,
                        minimumThroughput: 8,
                        samplingDurationSeconds: 30,
                        breakDurationSeconds: 20),
                    bulkhead: new BulkheadSettings(
                        enabled: true,
                        maxConcurrentExecutions: 2,
                        maxQueuedActions: 1))));
            engine.AddBehaviors(
                configureOptions: options => options.AutoRegister = false,
                configure: behaviors => behaviors.Register<FastGreetingBehavior>(topology => topology
                    .AsDirect()
                    .ViaInMemory()));
        });

        using var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<IBehaviorResilienceRuntimeCatalog>();
        var snapshot = provider.GetRequiredService<IRuntimeIntrospectionSnapshotProvider>().CreateSnapshot();

        var policy = Assert.Single(catalog.Policies);

        Assert.Equal("cephalon-behavior-execution", policy.Id);
        Assert.Equal("behavior-dispatch-middleware", policy.ExecutionMode);
        Assert.True(policy.Requested.Retry.Enabled);
        Assert.True(policy.Requested.CircuitBreaker.Enabled);
        Assert.True(policy.Effective.CircuitBreaker.Enabled);
        Assert.Equal(0.5m, policy.Effective.CircuitBreaker.FailureRatio);
        Assert.Equal(8, policy.Effective.CircuitBreaker.MinimumThroughput);
        Assert.Equal(30, policy.Effective.CircuitBreaker.SamplingDurationSeconds);
        Assert.Equal(20, policy.Effective.CircuitBreaker.BreakDurationSeconds);
        Assert.True(policy.Effective.Timeout.Enabled);
        Assert.Equal(12, policy.Effective.Timeout.TotalTimeoutSeconds);
        Assert.Null(policy.Effective.Timeout.AttemptTimeoutSeconds);
        Assert.True(policy.Effective.Bulkhead.Enabled);
        Assert.Equal(2, policy.Effective.Bulkhead.MaxConcurrentExecutions);
        Assert.Equal(1, policy.Effective.Bulkhead.MaxQueuedActions);
        Assert.Equal("contract-only", policy.Metadata["retryMode"]);
        Assert.Equal("enforced", policy.Metadata["circuitBreakerMode"]);
        Assert.Equal("enforced", policy.Metadata["timeoutMode"]);
        Assert.Equal("enforced", policy.Metadata["bulkheadMode"]);
        Assert.Equal("retry,timeout,circuit-breaker,bulkhead", policy.Metadata["requestedStrategies"]);
        Assert.Equal("timeout,circuit-breaker,bulkhead", policy.Metadata["effectiveStrategies"]);

        var snapshotPolicy = Assert.Single(snapshot.BehaviorResiliencePolicies);
        Assert.Equal(policy.Id, snapshotPolicy.Id);
        Assert.Equal(policy.ExecutionMode, snapshotPolicy.ExecutionMode);
        Assert.Equal(policy.Effective.Timeout.TotalTimeoutSeconds, snapshotPolicy.Effective.Timeout.TotalTimeoutSeconds);
        Assert.Equal(policy.Effective.Bulkhead.MaxConcurrentExecutions, snapshotPolicy.Effective.Bulkhead.MaxConcurrentExecutions);
    }

    [Fact]
    public async Task BehaviorDispatcherAppliesConfiguredTimeout()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularMonolith",
                resilience: new ResilienceSettings(
                    timeout: new TimeoutSettings(
                        enabled: true,
                        totalTimeoutSeconds: 1))));
            engine.AddBehaviors(
                configureOptions: options => options.AutoRegister = false,
                configure: behaviors => behaviors.Register<SlowBehavior>(topology => topology
                    .AsDirect()
                    .ViaInMemory()));
        });

        using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<BehaviorDispatcher>();

        var exception = await Assert.ThrowsAsync<TimeoutRejectedException>(() =>
            dispatcher.DispatchAsync(
                "tests.resilience.slow",
                new SlowInput(1500),
                new TestBehaviorContext("tests.resilience.slow", isDirect: true)));

        Assert.Contains("timeout", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BehaviorDispatcherOpensCircuitBreakerAfterHandledFailuresAndPublishesRuntimeState()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularMonolith",
                resilience: new ResilienceSettings(
                    timeout: new TimeoutSettings(
                        enabled: true,
                        totalTimeoutSeconds: 1),
                    circuitBreaker: new CircuitBreakerSettings(
                        enabled: true,
                        failureRatio: 0.5m,
                        minimumThroughput: 2,
                        samplingDurationSeconds: 30,
                        breakDurationSeconds: 15))));
            engine.AddBehaviors(
                configureOptions: options => options.AutoRegister = false,
                configure: behaviors => behaviors.Register<SlowBehavior>(topology => topology
                    .AsDirect()
                    .ViaInMemory()));
        });

        using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<BehaviorDispatcher>();
        var catalog = provider.GetRequiredService<IBehaviorResilienceRuntimeCatalog>();

        await Assert.ThrowsAsync<TimeoutRejectedException>(() =>
            dispatcher.DispatchAsync(
                "tests.resilience.slow",
                new SlowInput(1500),
                new TestBehaviorContext("tests.resilience.slow", isDirect: true)));

        await Assert.ThrowsAsync<TimeoutRejectedException>(() =>
            dispatcher.DispatchAsync(
                "tests.resilience.slow",
                new SlowInput(1500),
                new TestBehaviorContext("tests.resilience.slow", isDirect: true)));

        var openCircuitException = await Assert.ThrowsAsync<BrokenCircuitException>(() =>
            dispatcher.DispatchAsync(
                "tests.resilience.slow",
                new SlowInput(1500),
                new TestBehaviorContext("tests.resilience.slow", isDirect: true)));

        var policy = catalog.Resolve("tests.resilience.slow");

        Assert.NotNull(openCircuitException);
        Assert.NotNull(policy);
        Assert.Equal("open", policy!.Metadata["circuitState"]);
        Assert.True(policy.Metadata.ContainsKey("circuitStateChangedAtUtc"));
        Assert.True(policy.Metadata.ContainsKey("circuitRetryAfterSeconds"));
        Assert.True(policy.Metadata.ContainsKey("circuitLastOpenedExceptionType"));
    }

    [Fact]
    public async Task BehaviorDispatcherAppliesConfiguredBulkhead()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularMonolith",
                resilience: new ResilienceSettings(
                    bulkhead: new BulkheadSettings(
                        enabled: true,
                        maxConcurrentExecutions: 1,
                        maxQueuedActions: 0))));
            engine.AddBehaviors(
                configureOptions: options => options.AutoRegister = false,
                configure: behaviors => behaviors.Register<BlockingBehavior>(topology => topology
                    .AsDirect()
                    .ViaInMemory()));
        });

        using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<BehaviorDispatcher>();
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstInput = new BlockingInput(
            new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously),
            release);

        var firstDispatch = dispatcher.DispatchAsync(
            "tests.resilience.bulkhead",
            firstInput,
            new TestBehaviorContext("tests.resilience.bulkhead", isDirect: true));

        await firstInput.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        var secondException = await Assert.ThrowsAsync<RateLimiterRejectedException>(() =>
            dispatcher.DispatchAsync(
                "tests.resilience.bulkhead",
                new BlockingInput(
                    new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously),
                    release),
                new TestBehaviorContext("tests.resilience.bulkhead", isDirect: true)));

        release.TrySetResult(true);
        var firstResult = await firstDispatch;

        Assert.NotNull(secondException);
        Assert.Equal("released", firstResult);
    }

    [Fact]
    public void AddBehaviorsDoesNotPublishBehaviorResiliencePolicyWhenStrategiesAreExplicitlyDisabled()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularMonolith",
                resilience: new ResilienceSettings(
                    retry: new RetrySettings(enabled: false),
                    timeout: new TimeoutSettings(enabled: false),
                    circuitBreaker: new CircuitBreakerSettings(enabled: false),
                    bulkhead: new BulkheadSettings(enabled: false))));
            engine.AddBehaviors(
                configureOptions: options => options.AutoRegister = false,
                configure: behaviors => behaviors.Register<FastGreetingBehavior>(topology => topology
                    .AsDirect()
                    .ViaInMemory()));
        });

        using var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<IBehaviorResilienceRuntimeCatalog>();
        var snapshot = provider.GetRequiredService<IRuntimeIntrospectionSnapshotProvider>().CreateSnapshot();

        Assert.Empty(catalog.Policies);
        Assert.Empty(snapshot.BehaviorResiliencePolicies);
    }

    [Fact]
    public void AddBehaviorsPublishesBehaviorExecutionOverridePoliciesAndResolvePrefersMostSpecificMatch()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularMonolith",
                resilience: new ResilienceSettings(
                    timeout: new TimeoutSettings(
                        enabled: true,
                        totalTimeoutSeconds: 5),
                    behaviorExecutionOverrides:
                    new[]
                    {
                        new BehaviorExecutionResilienceOverrideSettings(
                            id: "rest-timeout",
                            transportIds: ["rest-api"],
                            timeout: new TimeoutSettings(totalTimeoutSeconds: 9)),
                        new BehaviorExecutionResilienceOverrideSettings(
                            id: "slow-disabled",
                            behaviorIds: ["tests.resilience.slow"],
                            timeout: new TimeoutSettings(enabled: false)),
                        new BehaviorExecutionResilienceOverrideSettings(
                            id: "slow-rest-timeout",
                            behaviorIds: ["tests.resilience.slow"],
                            transportIds: ["rest-api"],
                            timeout: new TimeoutSettings(totalTimeoutSeconds: 12))
                    })));
            engine.AddBehaviors(
                configureOptions: options => options.AutoRegister = false,
                configure: behaviors =>
                {
                    behaviors.Register<FastGreetingBehavior>(topology => topology
                        .AsDirect()
                        .ViaInMemory());
                    behaviors.Register<SlowBehavior>(topology => topology
                        .AsDirect()
                        .ViaInMemory());
                });
        });

        using var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<IBehaviorResilienceRuntimeCatalog>();
        var snapshot = provider.GetRequiredService<IRuntimeIntrospectionSnapshotProvider>().CreateSnapshot();

        Assert.Equal(4, catalog.Policies.Count);
        Assert.Equal(4, snapshot.BehaviorResiliencePolicies.Count);

        var slowRestPolicy = catalog.Resolve("tests.resilience.slow", "rest-api");
        Assert.NotNull(slowRestPolicy);
        Assert.Equal("slow-rest-timeout", slowRestPolicy!.Metadata["overrideId"]);
        Assert.Equal("behavior-executions-by-behavior-and-transport", slowRestPolicy.Scope);
        Assert.Equal(12, slowRestPolicy.Effective.Timeout.TotalTimeoutSeconds);
        Assert.Equal(["tests.resilience.slow"], slowRestPolicy.BehaviorIds);
        Assert.Equal(["rest-api"], slowRestPolicy.TransportIds);

        var slowKafkaPolicy = catalog.Resolve("tests.resilience.slow", "kafka");
        Assert.NotNull(slowKafkaPolicy);
        Assert.Equal("disabled", slowKafkaPolicy!.ExecutionMode);
        Assert.False(slowKafkaPolicy.Effective.Timeout.HasValues);
        Assert.Equal("slow-disabled", slowKafkaPolicy.Metadata["overrideId"]);
        Assert.Equal("timeout", slowKafkaPolicy.Metadata["explicitStrategies"]);
        Assert.Equal("none", slowKafkaPolicy.Metadata["requestedStrategies"]);
        Assert.Equal("disabled-by-override", slowKafkaPolicy.Metadata["reason"]);

        var fastRestPolicy = catalog.Resolve("tests.resilience.fast", "rest-api");
        Assert.NotNull(fastRestPolicy);
        Assert.Equal("rest-timeout", fastRestPolicy!.Metadata["overrideId"]);
        Assert.Equal(9, fastRestPolicy.Effective.Timeout.TotalTimeoutSeconds);

        var fastInMemoryPolicy = catalog.Resolve("tests.resilience.fast", "in-memory");
        Assert.NotNull(fastInMemoryPolicy);
        Assert.Equal("cephalon-behavior-execution", fastInMemoryPolicy!.Id);
        Assert.Equal(5, fastInMemoryPolicy.Effective.Timeout.TotalTimeoutSeconds);
    }

    [Fact]
    public async Task BehaviorDispatcherSkipsTimeoutWhenBehaviorSpecificOverrideDisablesDefaultTimeout()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularMonolith",
                resilience: new ResilienceSettings(
                    timeout: new TimeoutSettings(
                        enabled: true,
                        totalTimeoutSeconds: 1),
                    behaviorExecutionOverrides:
                    new[]
                    {
                        new BehaviorExecutionResilienceOverrideSettings(
                            id: "slow-timeout-disabled",
                            behaviorIds: ["tests.resilience.slow"],
                            timeout: new TimeoutSettings(enabled: false))
                    })));
            engine.AddBehaviors(
                configureOptions: options => options.AutoRegister = false,
                configure: behaviors => behaviors.Register<SlowBehavior>(topology => topology
                    .AsDirect()
                    .ViaInMemory()));
        });

        using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<BehaviorDispatcher>();

        var result = await dispatcher.DispatchAsync(
            "tests.resilience.slow",
            new SlowInput(1500),
            new TestBehaviorContext("tests.resilience.slow", isDirect: true));

        Assert.Equal("completed", result);
    }

    [Fact]
    public async Task BehaviorDispatcherSkipsTimeoutWhenTransportSpecificOverrideDisablesDefaultTimeout()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "ModularMonolith",
                resilience: new ResilienceSettings(
                    timeout: new TimeoutSettings(
                        enabled: true,
                        totalTimeoutSeconds: 1),
                    behaviorExecutionOverrides:
                    new[]
                    {
                        new BehaviorExecutionResilienceOverrideSettings(
                            id: "rest-timeout-disabled",
                            transportIds: ["rest-api"],
                            timeout: new TimeoutSettings(enabled: false))
                    })));
            engine.AddBehaviors(
                configureOptions: options => options.AutoRegister = false,
                configure: behaviors => behaviors.Register<SlowBehavior>(topology => topology
                    .AsDirect()
                    .ViaInMemory()));
        });

        using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<BehaviorDispatcher>();

        await Assert.ThrowsAsync<TimeoutRejectedException>(() =>
            dispatcher.DispatchAsync(
                "tests.resilience.slow",
                new SlowInput(1500),
                new TestBehaviorContext("tests.resilience.slow", isDirect: true)));

        var result = await dispatcher.DispatchAsync(
            "tests.resilience.slow",
            new SlowInput(1500),
            new TestBehaviorContext(
                "tests.resilience.slow",
                isDirect: true,
                metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["TransportId"] = "rest-api"
                }));

        Assert.Equal("completed", result);
    }

    [AppBehavior("tests.resilience.fast")]
    private sealed class FastGreetingBehavior : IAppBehavior<string, string>
    {
        public Task<string> HandleAsync(
            string input,
            IBehaviorContext context,
            CancellationToken cancellationToken = default)
            => Task.FromResult($"Hello, {input}!");
    }

    private sealed record SlowInput(int DelayMilliseconds);

    [AppBehavior("tests.resilience.slow")]
    private sealed class SlowBehavior : IAppBehavior<SlowInput, string>
    {
        public async Task<string> HandleAsync(
            SlowInput input,
            IBehaviorContext context,
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(input.DelayMilliseconds, cancellationToken);
            return "completed";
        }
    }

    private sealed record BlockingInput(
        TaskCompletionSource<bool> Started,
        TaskCompletionSource<bool> Release);

    [AppBehavior("tests.resilience.bulkhead")]
    private sealed class BlockingBehavior : IAppBehavior<BlockingInput, string>
    {
        public async Task<string> HandleAsync(
            BlockingInput input,
            IBehaviorContext context,
            CancellationToken cancellationToken = default)
        {
            input.Started.TrySetResult(true);
            await input.Release.Task.WaitAsync(cancellationToken);
            return "released";
        }
    }
}
