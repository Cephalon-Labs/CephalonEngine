using System.Net.Http.Json;
using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.EventSourcing;
using Cephalon.Abstractions.Execution;
using Cephalon.Abstractions.Modules;
using Cephalon.AspNetCore.Hosting;
using Cephalon.Behaviors.Hosting;
using Cephalon.Behaviors.Modules;
using Cephalon.Behaviors.Patterns.Abstractions;
using Cephalon.Behaviors.Patterns.Hosting;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Runtime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;

namespace Cephalon.Tests.Hosting;

public sealed class DurableExecutionHostingTests
{
    [Fact]
    public async Task MapCephalonExposesDurableExecutionCatalogAcrossRoutesAndSnapshot()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.AddCephalon(engine =>
        {
            engine.AddBehaviors(
                configureOptions: options => options.AutoRegister = false,
                configure: behaviors => behaviors.AddBehaviorPatterns());
            engine.AddModule(new DurableExecutionHostingModule());
        });

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var durableExecutions = await client.GetFromJsonAsync<DurableExecutionRuntimeDescriptor[]>("/engine/durable-executions");
        var byModule = await client.GetFromJsonAsync<DurableExecutionRuntimeDescriptor[]>("/engine/durable-executions/modules/tests.durable-host");
        var byTransport = await client.GetFromJsonAsync<DurableExecutionRuntimeDescriptor[]>("/engine/durable-executions/transports/in-memory");
        var durableExecution = await client.GetFromJsonAsync<DurableExecutionRuntimeDescriptor>("/engine/durable-executions/tests.workflows.hosted.approvals.start");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        var listedDescriptor = Assert.Single(durableExecutions!);
        Assert.Single(byModule!);
        Assert.Single(byTransport!);
        Assert.NotNull(durableExecution);
        Assert.NotNull(snapshot);

        Assert.Equal(listedDescriptor.Id, durableExecution!.Id);
        Assert.Equal("tests.durable-host", durableExecution.SourceModuleId);
        Assert.Equal(["host.workflow-preview"], durableExecution.RequiredFeatureFlagIds);
        Assert.Equal(["in-memory"], durableExecution.TransportIds);
        Assert.Equal("event-store-replay", durableExecution.ExecutionMode);
        Assert.True(durableExecution.EventSourcingEnabled);
        Assert.True(durableExecution.RequiresEventStore);
        Assert.Equal([200, 202, 204], durableExecution.SuccessStatusCodes);
        Assert.Equal("approval", durableExecution.Metadata["lane"]);
        Assert.Equal("behaviors.durable-execution", durableExecution.Metadata["capabilityKey"]);

        var snapshotDescriptor = Assert.Single(snapshot!.DurableExecutions);
        Assert.Equal(durableExecution.Id, snapshotDescriptor.Id);
        Assert.Equal(durableExecution.SourceModuleId, snapshotDescriptor.SourceModuleId);
        Assert.Equal(durableExecution.BehaviorType, snapshotDescriptor.BehaviorType);
        Assert.Equal(durableExecution.InputType, snapshotDescriptor.InputType);
        Assert.Equal(durableExecution.StateType, snapshotDescriptor.StateType);
        Assert.Equal(durableExecution.OutputType, snapshotDescriptor.OutputType);
    }

    private sealed class DurableExecutionHostingModule : BehaviorModuleBase
    {
        private static readonly ModuleDescriptor DescriptorInstance = new(
            id: "tests.durable-host",
            displayName: "Durable Host",
            description: "Owns one durable workflow for ASP.NET Core operator-route tests.",
            version: "1.0.0");

        public override ModuleDescriptor Descriptor => DescriptorInstance;

        public override void ConfigureBehaviors(IBehaviorModuleBuilder behaviors)
        {
            behaviors.Add<HostedApprovalWorkflowBehavior>(topology => topology
                .AsDurableExecution()
                .ViaInMemory()
                .RequireFeatureFlag("host.workflow-preview")
                .WithApiSurface("hosted-approvals", "start")
                .WithMetadata("lane", "approval")
                .WithOptions(options => options.EventSourcingEnabled = true));
        }
    }

    private sealed record HostedApprovalWorkflowInput(string ApprovalId);

    private sealed record HostedApprovalWorkflowState(int ApprovedCount);

    private sealed record HostedApprovalWorkflowOutput(string Status);

    [AppBehavior("tests.workflows.hosted.approvals.start")]
    private sealed class HostedApprovalWorkflowBehavior : IDurableExecution<HostedApprovalWorkflowInput, HostedApprovalWorkflowState, HostedApprovalWorkflowOutput>
    {
        public HostedApprovalWorkflowState CreateInitialState()
        {
            return new HostedApprovalWorkflowState(0);
        }

        public HostedApprovalWorkflowState Apply(
            HostedApprovalWorkflowState state,
            IDomainEvent domainEvent)
        {
            return state;
        }

        public string ResolveStreamId(string behaviorId, IBehaviorContext context)
        {
            return $"{behaviorId}:hosted";
        }

        public Task<DurableExecutionStepResult<HostedApprovalWorkflowOutput>> ExecuteDurablyAsync(
            HostedApprovalWorkflowInput input,
            DurableExecutionState<HostedApprovalWorkflowState> execution,
            IBehaviorContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new DurableExecutionStepResult<HostedApprovalWorkflowOutput>(
                output: new HostedApprovalWorkflowOutput("accepted"),
                events: [],
                isCompleted: false));
        }
    }
}
