using System.Net.Http.Json;
using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.Resilience;
using Cephalon.AspNetCore.Hosting;
using Cephalon.Behaviors.Hosting;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Runtime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;

namespace Cephalon.Tests.Hosting;

public sealed class BehaviorResilienceHostingTests
{
    [Fact]
    public async Task MapCephalonExposesBehaviorResiliencePoliciesAcrossEndpointAndSnapshot()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:Retry:Enabled"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:Retry:MaxAttempts"] = "3";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:Timeout:Enabled"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:Timeout:TotalTimeoutSeconds"] = "9";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:Timeout:AttemptTimeoutSeconds"] = "3";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:CircuitBreaker:Enabled"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:CircuitBreaker:FailureRatio"] = "0.25";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:CircuitBreaker:MinimumThroughput"] = "5";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:CircuitBreaker:SamplingDurationSeconds"] = "15";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:CircuitBreaker:BreakDurationSeconds"] = "20";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:Bulkhead:Enabled"] = "true";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:Bulkhead:MaxConcurrentExecutions"] = "4";
        builder.Configuration[$"{EngineSettings.SectionName}:Resilience:Bulkhead:MaxQueuedActions"] = "2";
        builder.AddCephalon(engine =>
        {
            engine.AddBehaviors(
                configureOptions: options => options.AutoRegister = false,
                configure: behaviors => behaviors.Register<ProbeBehavior>(topology => topology
                    .AsDirect()
                    .ViaInMemory()));
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var policies = await client.GetFromJsonAsync<BehaviorResilienceRuntimeDescriptor[]>("/engine/behavior-resilience");
        var policy = await client.GetFromJsonAsync<BehaviorResilienceRuntimeDescriptor>("/engine/behavior-resilience/cephalon-behavior-execution");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(policies);
        var listedPolicy = Assert.Single(policies);
        Assert.NotNull(policy);
        Assert.Equal(listedPolicy.Id, policy!.Id);
        Assert.Equal("behavior-dispatch-middleware", policy.ExecutionMode);
        Assert.True(policy.Requested.Retry.Enabled);
        Assert.True(policy.Requested.CircuitBreaker.Enabled);
        Assert.True(policy.Effective.Timeout.Enabled);
        Assert.Equal(9, policy.Effective.Timeout.TotalTimeoutSeconds);
        Assert.Null(policy.Effective.Timeout.AttemptTimeoutSeconds);
        Assert.True(policy.Effective.Bulkhead.Enabled);
        Assert.Equal(4, policy.Effective.Bulkhead.MaxConcurrentExecutions);
        Assert.Equal(2, policy.Effective.Bulkhead.MaxQueuedActions);
        Assert.Equal("contract-only", policy.Metadata["retryMode"]);
        Assert.Equal("contract-only", policy.Metadata["circuitBreakerMode"]);
        Assert.Equal("timeout,bulkhead", policy.Metadata["effectiveStrategies"]);

        Assert.NotNull(snapshot);
        var snapshotPolicy = Assert.Single(snapshot!.BehaviorResiliencePolicies);
        Assert.Equal(policy.Id, snapshotPolicy.Id);
        Assert.Equal(policy.ExecutionMode, snapshotPolicy.ExecutionMode);
        Assert.Equal(policy.Effective.Timeout.TotalTimeoutSeconds, snapshotPolicy.Effective.Timeout.TotalTimeoutSeconds);
    }

    [AppBehavior("tests.resilience.probe")]
    private sealed class ProbeBehavior : IAppBehavior<string, string>
    {
        public Task<string> HandleAsync(
            string input,
            IBehaviorContext context,
            CancellationToken cancellationToken = default)
            => Task.FromResult(input);
    }
}
