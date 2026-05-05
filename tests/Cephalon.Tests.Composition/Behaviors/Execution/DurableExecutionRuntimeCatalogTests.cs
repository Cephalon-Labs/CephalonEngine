using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.EventSourcing;
using Cephalon.Abstractions.Execution;
using Cephalon.Abstractions.Modules;
using Cephalon.Behaviors.Hosting;
using Cephalon.Behaviors.Modules;
using Cephalon.Behaviors.Patterns.Abstractions;
using Cephalon.Behaviors.Patterns.Hosting;
using Cephalon.Behaviors.Patterns.Strategies;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Composition;

public sealed class DurableExecutionRuntimeCatalogTests
{
    [Fact]
    public void BuildProjectsDurableExecutionCatalogIntoSnapshot()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(blueprint: "ModularMonolith"));
            engine.AddBehaviors(
                configureOptions: options => options.AutoRegister = false,
                configure: behaviors => behaviors.AddBehaviorPatterns());
            engine.AddModule(new DurableExecutionCatalogModule());
        });
        services.AddSingleton(
            DurableExecutionSlot.For<ApprovalWorkflowBehavior, ApprovalWorkflowInput, ApprovalWorkflowState, ApprovalWorkflowOutput>());

        using var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<IDurableExecutionRuntimeCatalog>();
        var snapshot = provider.GetRequiredService<IRuntimeIntrospectionSnapshotProvider>().CreateSnapshot();

        var durableExecution = Assert.Single(catalog.DurableExecutions);
        Assert.Equal("tests.workflows.approvals.start", durableExecution.Id);
        Assert.Equal("tests.durable-owner", durableExecution.SourceModuleId);
        Assert.Equal("event-store-replay", durableExecution.ExecutionMode);
        Assert.Equal(["in-memory", "rabbitmq"], durableExecution.TransportIds);
        Assert.Equal(["host.approvals-v2"], durableExecution.RequiredFeatureFlagIds);
        Assert.True(durableExecution.EventSourcingEnabled);
        Assert.True(durableExecution.RequiresEventStore);
        Assert.Equal([200, 202, 204], durableExecution.SuccessStatusCodes);
        Assert.Contains(nameof(ApprovalWorkflowBehavior), durableExecution.BehaviorType, StringComparison.Ordinal);
        Assert.Contains(nameof(ApprovalWorkflowInput), durableExecution.InputType, StringComparison.Ordinal);
        Assert.Contains(nameof(ApprovalWorkflowState), durableExecution.StateType, StringComparison.Ordinal);
        Assert.Contains(nameof(ApprovalWorkflowOutput), durableExecution.OutputType, StringComparison.Ordinal);
        Assert.Equal("approval", durableExecution.Metadata["lane"]);
        Assert.Equal("behaviors.durable-execution", durableExecution.Metadata["capabilityKey"]);
        Assert.Equal("ABT-006", durableExecution.Metadata["compatibilityRuleId"]);
        Assert.Equal("behavior-defined", durableExecution.Metadata["streamIdentityMode"]);
        Assert.Equal("event-store-replay", durableExecution.Metadata["replayMode"]);
        Assert.Equal("optimistic-concurrency", durableExecution.Metadata["appendMode"]);
        Assert.Equal("step-result", durableExecution.Metadata["completionMode"]);
        Assert.Equal("approvals", durableExecution.Metadata["apiSurfaceGroupPath"]);
        Assert.Equal("start", durableExecution.Metadata["apiSurfaceOperationPath"]);

        var byId = catalog.GetById("tests.workflows.approvals.start");
        Assert.NotNull(byId);
        Assert.Equal(durableExecution.Id, byId!.Id);
        Assert.Single(catalog.GetBySourceModule("tests.durable-owner"));
        Assert.Single(catalog.GetByTransportId("rabbitmq"));
        Assert.Empty(catalog.GetByTransportId("grpc"));

        var snapshotDescriptor = Assert.Single(snapshot.DurableExecutions);
        Assert.Equal(durableExecution.Id, snapshotDescriptor.Id);
        Assert.Equal(durableExecution.SourceModuleId, snapshotDescriptor.SourceModuleId);
        Assert.Equal(durableExecution.TransportIds, snapshotDescriptor.TransportIds);
        Assert.Equal(durableExecution.RequiredFeatureFlagIds, snapshotDescriptor.RequiredFeatureFlagIds);
        Assert.Equal(durableExecution.InputType, snapshotDescriptor.InputType);
        Assert.Equal(durableExecution.StateType, snapshotDescriptor.StateType);
        Assert.Equal(durableExecution.OutputType, snapshotDescriptor.OutputType);
        Assert.Empty(snapshot.DurableExecutionStates);
    }

    private sealed class DurableExecutionCatalogModule : BehaviorModuleBase
    {
        private static readonly ModuleDescriptor DescriptorInstance = new(
            id: "tests.durable-owner",
            displayName: "Durable Owner",
            description: "Owns one durable execution workflow for runtime catalog tests.",
            version: "1.0.0");

        public override ModuleDescriptor Descriptor => DescriptorInstance;

        public override void ConfigureBehaviors(IBehaviorModuleBuilder behaviors)
        {
            behaviors.Add<ApprovalWorkflowBehavior>(topology => topology
                .AsDurableExecution()
                .ViaInMemory()
                .ViaRabbitMq()
                .RequireFeatureFlag("host.approvals-v2")
                .WithApiSurface("approvals", "start")
                .WithMetadata("lane", "approval")
                .WithOptions(options => options.EventSourcingEnabled = true));
        }
    }

    private sealed record ApprovalWorkflowInput(string ApprovalId);

    private sealed record ApprovalWorkflowState(int ApprovedCount);

    private sealed record ApprovalWorkflowOutput(string Status);

    [AppBehavior("tests.workflows.approvals.start")]
    private sealed class ApprovalWorkflowBehavior : IDurableExecution<ApprovalWorkflowInput, ApprovalWorkflowState, ApprovalWorkflowOutput>
    {
        public ApprovalWorkflowState CreateInitialState()
        {
            return new ApprovalWorkflowState(0);
        }

        public ApprovalWorkflowState Apply(
            ApprovalWorkflowState state,
            IDomainEvent domainEvent)
        {
            return state;
        }

        public string ResolveStreamId(string behaviorId, IBehaviorContext context)
        {
            return $"{behaviorId}:approval";
        }

        public Task<DurableExecutionStepResult<ApprovalWorkflowOutput>> ExecuteDurablyAsync(
            ApprovalWorkflowInput input,
            DurableExecutionState<ApprovalWorkflowState> execution,
            IBehaviorContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new DurableExecutionStepResult<ApprovalWorkflowOutput>(
                output: new ApprovalWorkflowOutput("queued"),
                events: [],
                isCompleted: false));
        }
    }
}
