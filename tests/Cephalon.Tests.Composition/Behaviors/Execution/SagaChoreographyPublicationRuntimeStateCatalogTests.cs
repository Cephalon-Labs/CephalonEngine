using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.Execution;
using Cephalon.Abstractions.Modules;
using Cephalon.Behaviors.Hosting;
using Cephalon.Behaviors.Modules;
using Cephalon.Behaviors.Patterns.Abstractions;
using Cephalon.Behaviors.Patterns.Hosting;
using Cephalon.Behaviors.Patterns.Strategies;
using Cephalon.Behaviors.Services;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Runtime;
using Cephalon.Tests.Behaviors;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Composition;

public sealed class SagaChoreographyPublicationRuntimeStateCatalogTests
{
    [Fact]
    public async Task RuntimeStateCatalogTracksAcceptedFailedAndCompensationPublicationPostureAcrossSnapshotAndFilters()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ObservedSagaPublisher>();
        services.AddSingleton<ISagaChoreographyPublisher>(static serviceProvider =>
            serviceProvider.GetRequiredService<ObservedSagaPublisher>());
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(blueprint: "ModularMonolith"));
            engine.AddBehaviors(
                configureOptions: options => options.AutoRegister = false,
                configure: behaviors => behaviors.AddBehaviorPatterns());
            engine.AddModule(new SagaChoreographyRuntimeStateModule());
        });

        using var provider = services.BuildServiceProvider();
        var strategy = provider.GetServices<IBehaviorExecutionStrategy>()
            .OfType<ChoreographySagaExecutionStrategy>()
            .Single();
        var catalog = provider.GetRequiredService<ISagaChoreographyPublicationRuntimeStateCatalog>();
        var snapshotProvider = provider.GetRequiredService<IRuntimeIntrospectionSnapshotProvider>();

        await strategy.ExecuteAsync(MakeContext(
            behaviorId: "tests.sagas.runtime.approvals.progress",
            behavior: new ObservedApprovalSagaBehavior(),
            input: new ObservedApprovalSagaInput("accepted", "APR-42"),
            behaviorContext: new TestBehaviorContext(
                "tests.sagas.runtime.approvals.progress",
                correlationId: "corr-accepted",
                metadata: new Dictionary<string, string>
                {
                    ["tenantId"] = "tenant-42"
                })));

        await strategy.ExecuteAsync(MakeContext(
            behaviorId: "tests.sagas.runtime.approvals.progress",
            behavior: new ObservedApprovalSagaBehavior(),
            input: new ObservedApprovalSagaInput("accepted", "APR-42"),
            behaviorContext: new TestBehaviorContext(
                "tests.sagas.runtime.approvals.progress",
                correlationId: "corr-accepted",
                metadata: new Dictionary<string, string>
                {
                    ["tenantId"] = "tenant-42"
                })));

        await strategy.ExecuteAsync(MakeContext(
            behaviorId: "tests.sagas.runtime.approvals.progress",
            behavior: new ObservedApprovalSagaBehavior(),
            input: new ObservedApprovalSagaInput("compensate", "APR-77"),
            behaviorContext: new TestBehaviorContext(
                "tests.sagas.runtime.approvals.progress",
                correlationId: "corr-compensate")));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => strategy.ExecuteAsync(MakeContext(
            behaviorId: "tests.sagas.runtime.approvals.progress",
            behavior: new ObservedApprovalSagaBehavior(),
            input: new ObservedApprovalSagaInput("fail", "APR-99"),
            behaviorContext: new TestBehaviorContext(
                "tests.sagas.runtime.approvals.progress",
                correlationId: "corr-failed"))));

        Assert.Contains("Simulated choreography handoff failure", exception.Message, StringComparison.Ordinal);

        Assert.Equal(3, catalog.States.Count);
        Assert.Equal(3, catalog.GetByBehaviorId("tests.sagas.runtime.approvals.progress").Count);
        Assert.Equal(3, catalog.GetBySourceModule("tests.saga-state-owner").Count);
        Assert.Equal(3, catalog.GetByTransportId("rabbitmq").Count);
        Assert.Equal(3, catalog.GetByChannelId("approvals").Count);
        Assert.Single(catalog.GetByCorrelationId("corr-accepted"));
        Assert.Single(catalog.GetCompensationPublications());
        Assert.Single(catalog.GetFailedPublications());

        var acceptedState = Assert.Single(
            catalog.States,
            static state => string.Equals(state.CorrelationId, "corr-accepted", StringComparison.Ordinal));
        Assert.Equal("tests.sagas.runtime.approvals.progress", acceptedState.BehaviorId);
        Assert.Equal("approval.progressed", acceptedState.PublicationId);
        Assert.Equal("approvals", acceptedState.ChannelId);
        Assert.Equal("approvals.progressed", acceptedState.EventType);
        Assert.Equal(new DateTimeOffset(2026, 4, 19, 6, 0, 0, TimeSpan.Zero), acceptedState.OccurredAtUtc);
        Assert.Equal("tests.saga-state-owner", acceptedState.SourceModuleId);
        Assert.Equal(["in-memory", "rabbitmq"], acceptedState.TransportIds);
        Assert.Equal("tenant-42", acceptedState.TenantId);
        Assert.Equal("application/json", acceptedState.ContentType);
        Assert.False(acceptedState.IsCompensation);
        Assert.Equal("accepted", acceptedState.LastOutcome);
        Assert.NotNull(acceptedState.LastObservedAtUtc);
        Assert.Contains(nameof(ObservedSagaPublisher), acceptedState.LastPublisherType, StringComparison.Ordinal);
        Assert.Equal(2, acceptedState.AcceptedCount);
        Assert.Equal(0, acceptedState.FailedCount);
        Assert.Equal(2, acceptedState.TotalReports);
        Assert.True(acceptedState.IsAccepted);
        Assert.False(acceptedState.IsFailed);
        Assert.Null(acceptedState.LastError);
        Assert.Equal("approval", acceptedState.Metadata["lane"]);

        Assert.True(catalog.TryGetById(acceptedState.Id, out var lookedUpAcceptedState));
        Assert.NotNull(lookedUpAcceptedState);
        Assert.Equal(acceptedState.Id, lookedUpAcceptedState!.Id);

        var compensationState = Assert.Single(catalog.GetCompensationPublications());
        Assert.Equal("corr-compensate", compensationState.CorrelationId);
        Assert.True(compensationState.IsCompensation);
        Assert.Equal("accepted", compensationState.LastOutcome);
        Assert.Equal(1, compensationState.AcceptedCount);
        Assert.Equal(0, compensationState.FailedCount);
        Assert.Equal("approval.compensated", compensationState.PublicationId);
        Assert.Equal("approvals.compensated", compensationState.EventType);

        var failedState = Assert.Single(catalog.GetFailedPublications());
        Assert.Equal("corr-failed", failedState.CorrelationId);
        Assert.False(failedState.IsCompensation);
        Assert.Equal("failed", failedState.LastOutcome);
        Assert.Equal(0, failedState.AcceptedCount);
        Assert.Equal(1, failedState.FailedCount);
        Assert.Equal(1, failedState.TotalReports);
        Assert.True(failedState.IsFailed);
        Assert.Contains("Simulated choreography handoff failure", failedState.LastError, StringComparison.Ordinal);
        Assert.Contains("InvalidOperationException", failedState.Metadata["exceptionType"], StringComparison.Ordinal);
        Assert.Contains(nameof(ObservedSagaPublisher), failedState.LastPublisherType, StringComparison.Ordinal);

        var snapshot = snapshotProvider.CreateSnapshot();
        Assert.Equal(3, snapshot.SagaChoreographyPublicationStates.Count);
        Assert.Contains(
            snapshot.SagaChoreographyPublicationStates,
            state => string.Equals(state.Id, acceptedState.Id, StringComparison.Ordinal));
    }

    private static BehaviorExecutionContext MakeContext<TBehavior>(
        string behaviorId,
        TBehavior behavior,
        object input,
        IBehaviorContext behaviorContext)
        where TBehavior : class
    {
        var descriptor = new BehaviorTopologyDescriptor(
            id: behaviorId,
            pattern: "saga-choreography",
            transportIds: ["in-memory", "rabbitmq"],
            sourceModuleId: "tests.saga-state-owner");
        var slot = BehaviorExecutionSlot.ForType(typeof(TBehavior));
        return new BehaviorExecutionContext
        {
            Descriptor = descriptor,
            BehaviorInstance = behavior,
            Slot = slot,
            Input = input,
            BehaviorContext = behaviorContext
        };
    }

    private sealed class SagaChoreographyRuntimeStateModule : BehaviorModuleBase
    {
        private static readonly ModuleDescriptor DescriptorInstance = new(
            id: "tests.saga-state-owner",
            displayName: "Saga State Owner",
            description: "Owns one choreography behavior for live runtime-state tests.",
            version: "1.0.0");

        public override ModuleDescriptor Descriptor => DescriptorInstance;

        public override void ConfigureBehaviors(IBehaviorModuleBuilder behaviors)
        {
            behaviors.Add<ObservedApprovalSagaBehavior>(topology => topology
                .AsSagaChoreography()
                .ViaInMemory()
                .ViaRabbitMq()
                .RequireFeatureFlag("host.approvals-live")
                .WithApiSurface("runtime-approvals", "progress")
                .WithMetadata("lane", "approval"));
        }
    }

    private sealed record ObservedApprovalSagaInput(string Mode, string ApprovalId);

    private sealed record ApprovalProgressedPayload(string ApprovalId, string Mode);

    [AppBehavior("tests.sagas.runtime.approvals.progress")]
    private sealed class ObservedApprovalSagaBehavior : IAppBehavior<ObservedApprovalSagaInput, SagaChoreographyStepResult<string?>>
    {
        public Task<SagaChoreographyStepResult<string?>> HandleAsync(
            ObservedApprovalSagaInput input,
            IBehaviorContext context,
            CancellationToken cancellationToken = default)
        {
            var publication = input.Mode switch
            {
                "accepted" => SagaChoreographyPublication.CreateJson(
                    id: "approval.progressed",
                    channelId: "approvals",
                    eventType: "approvals.progressed",
                    payload: new ApprovalProgressedPayload(input.ApprovalId, input.Mode),
                    occurredAtUtc: new DateTimeOffset(2026, 4, 19, 6, 0, 0, TimeSpan.Zero),
                    metadata: new Dictionary<string, string>
                    {
                        ["lane"] = "approval"
                    }),
                "compensate" => SagaChoreographyPublication.CreateCompensationJson(
                    id: "approval.compensated",
                    channelId: "approvals",
                    eventType: "approvals.compensated",
                    payload: new ApprovalProgressedPayload(input.ApprovalId, input.Mode),
                    occurredAtUtc: new DateTimeOffset(2026, 4, 19, 6, 5, 0, TimeSpan.Zero),
                    metadata: new Dictionary<string, string>
                    {
                        ["lane"] = "approval"
                    }),
                "fail" => SagaChoreographyPublication.CreateJson(
                    id: "approval.failed",
                    channelId: "approvals",
                    eventType: "approvals.failed",
                    payload: new ApprovalProgressedPayload(input.ApprovalId, input.Mode),
                    occurredAtUtc: new DateTimeOffset(2026, 4, 19, 6, 10, 0, TimeSpan.Zero),
                    metadata: new Dictionary<string, string>
                    {
                        ["lane"] = "approval"
                    }),
                _ => throw new InvalidOperationException($"Unknown choreography runtime mode '{input.Mode}'.")
            };

            return Task.FromResult(
                new SagaChoreographyStepResult<string?>(
                    output: input.Mode,
                    publications: [publication]));
        }
    }

    private sealed class ObservedSagaPublisher : ISagaChoreographyPublisher
    {
        public ValueTask PublishAsync(
            SagaChoreographyPublication publication,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(publication);
            cancellationToken.ThrowIfCancellationRequested();

            if (string.Equals(publication.Id, "approval.failed", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Simulated choreography handoff failure.");
            }

            return ValueTask.CompletedTask;
        }
    }
}
