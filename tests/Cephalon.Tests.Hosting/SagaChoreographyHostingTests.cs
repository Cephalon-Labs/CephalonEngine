using System.Net;
using System.Net.Http.Json;
using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.Execution;
using Cephalon.Abstractions.Modules;
using Cephalon.AspNetCore.Hosting;
using Cephalon.Behaviors.Hosting;
using Cephalon.Behaviors.Modules;
using Cephalon.Behaviors.Patterns.Abstractions;
using Cephalon.Behaviors.Patterns.Hosting;
using Cephalon.Behaviors.Patterns.Runtime;
using Cephalon.Behaviors.Patterns.Strategies;
using Cephalon.Behaviors.Services;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Manifest;
using Cephalon.Engine.Runtime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Hosting;

public sealed class SagaChoreographyHostingTests
{
    [Fact]
    public async Task MapCephalonExposesSagaChoreographyCatalogAcrossRoutesAndSnapshot()
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
            engine.AddModule(new SagaChoreographyHostingModule());
        });
        RegisterHostedCatalogSlots(builder.Services);

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var sagaChoreographies = await client.GetFromJsonAsync<SagaChoreographyRuntimeDescriptor[]>("/engine/saga-choreographies");
        var byModule = await client.GetFromJsonAsync<SagaChoreographyRuntimeDescriptor[]>("/engine/saga-choreographies/modules/tests.saga-host");
        var byTransport = await client.GetFromJsonAsync<SagaChoreographyRuntimeDescriptor[]>("/engine/saga-choreographies/transports/rabbitmq");
        var reviewApproved = await client.GetFromJsonAsync<SagaChoreographyRuntimeDescriptor>("/engine/saga-choreographies/tests.sagas.hosted.review-approved");
        var capabilities = await client.GetFromJsonAsync<CapabilityManifest[]>("/engine/capabilities");
        var missing = await client.GetAsync("/engine/saga-choreographies/tests.sagas.hosted.missing");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(sagaChoreographies);
        Assert.Equal(2, sagaChoreographies.Length);
        Assert.NotNull(byModule);
        Assert.Equal(2, byModule.Length);
        Assert.NotNull(byTransport);
        Assert.Single(byTransport);
        Assert.NotNull(reviewApproved);
        Assert.NotNull(capabilities);
        Assert.NotNull(snapshot);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);

        var escalation = Assert.Single(
            sagaChoreographies,
            static descriptor => string.Equals(descriptor.Id, "tests.sagas.hosted.escalate", StringComparison.Ordinal));
        Assert.Equal("tests.saga-host", escalation.SourceModuleId);
        Assert.Equal(["in-memory"], escalation.TransportIds);
        Assert.Equal(["host.workflow-preview"], escalation.RequiredFeatureFlagIds);
        Assert.Null(escalation.LocalOutputType);
        Assert.Equal("publication-sequence", escalation.Metadata["publicationResultShape"]);
        Assert.Equal("behavior", escalation.Metadata["authoringModel"]);

        Assert.Equal("tests.saga-host", reviewApproved!.SourceModuleId);
        Assert.Equal(["in-memory", "rabbitmq"], reviewApproved.TransportIds);
        Assert.Equal(["host.workflow-preview"], reviewApproved.RequiredFeatureFlagIds);
        Assert.Equal([200, 202, 204], reviewApproved.SuccessStatusCodes);
        Assert.Equal(typeof(string).FullName, reviewApproved.LocalOutputType);
        Assert.Equal("reactor", reviewApproved.Metadata["authoringModel"]);
        Assert.Equal("typed-step-result", reviewApproved.Metadata["publicationResultShape"]);
        Assert.Equal("approval", reviewApproved.Metadata["lane"]);
        Assert.Equal("behaviors.saga-choreography", reviewApproved.Metadata["capabilityKey"]);

        var runtimeCatalogCapability = Assert.Single(
            capabilities!,
            static capability => string.Equals(capability.Key, "behaviors.saga-choreography.runtime-catalog", StringComparison.Ordinal));
        Assert.Equal("behaviors", runtimeCatalogCapability.SourceModuleId);
        Assert.Equal("runtime-catalog", runtimeCatalogCapability.Metadata["surface"]);
        Assert.Equal("/engine/saga-choreographies", runtimeCatalogCapability.Metadata["aspNetCoreRoute"]);

        var publicationStateCapability = Assert.Single(
            capabilities,
            static capability => string.Equals(capability.Key, "behaviors.saga-choreography.publication-state", StringComparison.Ordinal));
        Assert.Equal("behaviors", publicationStateCapability.SourceModuleId);
        Assert.Equal("publication-state", publicationStateCapability.Metadata["surface"]);
        Assert.Equal("/engine/saga-choreographies/runtime", publicationStateCapability.Metadata["aspNetCoreRoute"]);
        Assert.Equal("choreography-strategy", publicationStateCapability.Metadata["ownership"]);

        Assert.Equal(2, snapshot!.SagaChoreographies.Count);
        var snapshotReviewApproved = Assert.Single(
            snapshot.SagaChoreographies,
            static descriptor => string.Equals(descriptor.Id, "tests.sagas.hosted.review-approved", StringComparison.Ordinal));
        Assert.Equal(reviewApproved.BehaviorType, snapshotReviewApproved.BehaviorType);
        Assert.Equal(reviewApproved.InputType, snapshotReviewApproved.InputType);
        Assert.Equal(reviewApproved.ResultType, snapshotReviewApproved.ResultType);
        Assert.Equal(reviewApproved.LocalOutputType, snapshotReviewApproved.LocalOutputType);
    }

    [Fact]
    public async Task MapCephalonExposesSagaChoreographyPublicationRuntimeStateAcrossRoutesAndSnapshot()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<HostingObservedSagaPublisher>();
        builder.Services.AddSingleton<ISagaChoreographyPublisher>(static serviceProvider =>
            serviceProvider.GetRequiredService<HostingObservedSagaPublisher>());
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "ModularMonolith";
        builder.Configuration[$"{EngineSettings.SectionName}:Transports:0"] = "RestApi";
        builder.AddCephalon(engine =>
        {
            engine.AddBehaviors(
                configureOptions: options => options.AutoRegister = false,
                configure: behaviors => behaviors.AddBehaviorPatterns());
            engine.AddModule(new SagaChoreographyRuntimeStateHostingModule());
        });
        RegisterHostedRuntimeStateSlots(builder.Services);

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var initialStates = await client.GetFromJsonAsync<SagaChoreographyPublicationRuntimeState[]>("/engine/saga-choreographies/runtime");
        var initialCompensations = await client.GetFromJsonAsync<SagaChoreographyPublicationRuntimeState[]>("/engine/saga-choreographies/runtime/compensations");
        var initialFailures = await client.GetFromJsonAsync<SagaChoreographyPublicationRuntimeState[]>("/engine/saga-choreographies/runtime/failures");
        var missingStateResponse = await client.GetAsync("/engine/saga-choreographies/runtime/publications/tests.missing");

        Assert.NotNull(initialStates);
        Assert.Empty(initialStates);
        Assert.NotNull(initialCompensations);
        Assert.Empty(initialCompensations);
        Assert.NotNull(initialFailures);
        Assert.Empty(initialFailures);
        Assert.Equal(HttpStatusCode.NotFound, missingStateResponse.StatusCode);

        var strategy = app.Services.GetServices<IBehaviorExecutionStrategy>()
            .OfType<ChoreographySagaExecutionStrategy>()
            .Single();

        await strategy.ExecuteAsync(MakeRuntimeStateContext(
            behaviorId: "tests.sagas.hosted.runtime.progress",
            behavior: new HostedObservedApprovalSagaBehavior(),
            input: new HostedObservedApprovalSagaInput("accepted", "APR-42"),
            behaviorContext: new HostingTestBehaviorContext(
                "tests.sagas.hosted.runtime.progress",
                correlationId: "corr-hosted",
                metadata: new Dictionary<string, string>
                {
                    ["tenantId"] = "tenant-7"
                })));

        await strategy.ExecuteAsync(MakeRuntimeStateContext(
            behaviorId: "tests.sagas.hosted.runtime.progress",
            behavior: new HostedObservedApprovalSagaBehavior(),
            input: new HostedObservedApprovalSagaInput("accepted", "APR-42"),
            behaviorContext: new HostingTestBehaviorContext(
                "tests.sagas.hosted.runtime.progress",
                correlationId: "corr-hosted",
                metadata: new Dictionary<string, string>
                {
                    ["tenantId"] = "tenant-7"
                })));

        await strategy.ExecuteAsync(MakeRuntimeStateContext(
            behaviorId: "tests.sagas.hosted.runtime.progress",
            behavior: new HostedObservedApprovalSagaBehavior(),
            input: new HostedObservedApprovalSagaInput("compensate", "APR-77"),
            behaviorContext: new HostingTestBehaviorContext(
                "tests.sagas.hosted.runtime.progress",
                correlationId: "corr-compensate")));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => strategy.ExecuteAsync(MakeRuntimeStateContext(
            behaviorId: "tests.sagas.hosted.runtime.progress",
            behavior: new HostedObservedApprovalSagaBehavior(),
            input: new HostedObservedApprovalSagaInput("fail", "APR-99"),
            behaviorContext: new HostingTestBehaviorContext(
                "tests.sagas.hosted.runtime.progress",
                correlationId: "corr-failed"))));

        Assert.Contains("Simulated choreography handoff failure", exception.Message, StringComparison.Ordinal);

        var states = await client.GetFromJsonAsync<SagaChoreographyPublicationRuntimeState[]>("/engine/saga-choreographies/runtime");
        var byBehavior = await client.GetFromJsonAsync<SagaChoreographyPublicationRuntimeState[]>("/engine/saga-choreographies/runtime/behaviors/tests.sagas.hosted.runtime.progress");
        var byModule = await client.GetFromJsonAsync<SagaChoreographyPublicationRuntimeState[]>("/engine/saga-choreographies/runtime/modules/tests.saga-runtime-host");
        var byTransport = await client.GetFromJsonAsync<SagaChoreographyPublicationRuntimeState[]>("/engine/saga-choreographies/runtime/transports/in-memory");
        var byChannel = await client.GetFromJsonAsync<SagaChoreographyPublicationRuntimeState[]>("/engine/saga-choreographies/runtime/channels/approvals");
        var byCorrelation = await client.GetFromJsonAsync<SagaChoreographyPublicationRuntimeState[]>("/engine/saga-choreographies/runtime/correlations/corr-hosted");
        var compensations = await client.GetFromJsonAsync<SagaChoreographyPublicationRuntimeState[]>("/engine/saga-choreographies/runtime/compensations");
        var failures = await client.GetFromJsonAsync<SagaChoreographyPublicationRuntimeState[]>("/engine/saga-choreographies/runtime/failures");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(states);
        Assert.Equal(3, states.Length);
        Assert.Equal(3, byBehavior!.Length);
        Assert.Equal(3, byModule!.Length);
        Assert.Equal(3, byTransport!.Length);
        Assert.Equal(3, byChannel!.Length);
        Assert.Single(byCorrelation!);
        Assert.Single(compensations!);
        Assert.Single(failures!);
        Assert.NotNull(snapshot);

        var acceptedState = Assert.Single(
            states,
            static state => string.Equals(state.CorrelationId, "corr-hosted", StringComparison.Ordinal));
        var acceptedStateById = await client.GetFromJsonAsync<SagaChoreographyPublicationRuntimeState>(
            $"/engine/saga-choreographies/runtime/publications/{acceptedState.Id}");
        Assert.NotNull(acceptedStateById);
        Assert.Equal(acceptedState.Id, acceptedStateById!.Id);
        Assert.Equal("tests.sagas.hosted.runtime.progress", acceptedStateById.BehaviorId);
        Assert.Equal("tests.saga-runtime-host", acceptedStateById.SourceModuleId);
        Assert.Equal(["in-memory"], acceptedStateById.TransportIds);
        Assert.Equal("approval.progressed", acceptedStateById.PublicationId);
        Assert.Equal("approvals.progressed", acceptedStateById.EventType);
        Assert.Equal("approvals", acceptedStateById.ChannelId);
        Assert.Equal("tenant-7", acceptedStateById.TenantId);
        Assert.Equal("application/json", acceptedStateById.ContentType);
        Assert.False(acceptedStateById.IsCompensation);
        Assert.Equal("accepted", acceptedStateById.LastOutcome);
        Assert.NotNull(acceptedStateById.LastObservedAtUtc);
        Assert.Contains(nameof(HostingObservedSagaPublisher), acceptedStateById.LastPublisherType, StringComparison.Ordinal);
        Assert.Equal(2, acceptedStateById.AcceptedCount);
        Assert.Equal(0, acceptedStateById.FailedCount);
        Assert.Equal(2, acceptedStateById.TotalReports);
        Assert.True(acceptedStateById.IsAccepted);
        Assert.False(acceptedStateById.IsFailed);
        Assert.Null(acceptedStateById.LastError);
        Assert.Equal("approval", acceptedStateById.Metadata["lane"]);

        Assert.NotNull(compensations);
        var compensationState = Assert.Single(compensations!);
        Assert.Equal("corr-compensate", compensationState.CorrelationId);
        Assert.True(compensationState.IsCompensation);
        Assert.Equal("accepted", compensationState.LastOutcome);
        Assert.Equal(1, compensationState.AcceptedCount);
        Assert.Equal(0, compensationState.FailedCount);

        Assert.NotNull(failures);
        var failedState = Assert.Single(failures!);
        Assert.Equal("corr-failed", failedState.CorrelationId);
        Assert.False(failedState.IsCompensation);
        Assert.Equal("failed", failedState.LastOutcome);
        Assert.Equal(0, failedState.AcceptedCount);
        Assert.Equal(1, failedState.FailedCount);
        Assert.Equal(1, failedState.TotalReports);
        Assert.True(failedState.IsFailed);
        Assert.Contains("Simulated choreography handoff failure", failedState.LastError, StringComparison.Ordinal);
        Assert.Contains("InvalidOperationException", failedState.Metadata["exceptionType"], StringComparison.Ordinal);
        Assert.Contains(nameof(HostingObservedSagaPublisher), failedState.LastPublisherType, StringComparison.Ordinal);

        Assert.Equal(3, snapshot!.SagaChoreographyPublicationStates.Count);
        Assert.Contains(
            snapshot.SagaChoreographyPublicationStates,
            state => string.Equals(state.Id, acceptedStateById.Id, StringComparison.Ordinal));
    }

    private sealed class SagaChoreographyHostingModule : BehaviorModuleBase
    {
        private static readonly ModuleDescriptor DescriptorInstance = new(
            id: "tests.saga-host",
            displayName: "Saga Host",
            description: "Owns saga choreography behaviors for ASP.NET Core operator-route tests.",
            version: "1.0.0");

        public override ModuleDescriptor Descriptor => DescriptorInstance;

        public override void ConfigureBehaviors(IBehaviorModuleBuilder behaviors)
        {
            behaviors.Add<HostedApprovalEscalationBehavior>(topology => topology
                .AsSagaChoreography()
                .ViaInMemory()
                .RequireFeatureFlag("host.workflow-preview")
                .WithApiSurface("hosted-approvals", "escalate")
                .WithMetadata("lane", "approval"));

            behaviors.Add<HostedApprovalReviewReactor>(topology => topology
                .AsSagaChoreography()
                .ViaInMemory()
                .ViaRabbitMq()
                .RequireFeatureFlag("host.workflow-preview")
                .WithApiSurface("hosted-approvals", "review-approved")
                .WithMetadata("lane", "approval"));
        }
    }

    private sealed record HostedApprovalEscalationInput(string ApprovalId, string Reason);

    private sealed record HostedApprovalReviewEvent(string ApprovalId, string Decision);

    private sealed record HostedApprovalEscalatedPayload(string ApprovalId, string Reason);

    private sealed record HostedApprovalReviewedPayload(string ApprovalId, string Decision);

    private static void RegisterHostedCatalogSlots(IServiceCollection services)
    {
        services.AddSingleton(
            SagaChoreographyRuntimeSlot.For<HostedApprovalEscalationBehavior, HostedApprovalEscalationInput, SagaChoreographyPublication[]>(
                "behavior",
                "publication-sequence"));
        services.AddSingleton(
            SagaChoreographyRuntimeSlot.For<HostedApprovalReviewReactor, HostedApprovalReviewEvent, SagaChoreographyStepResult<string>>(
                "reactor",
                "typed-step-result",
                typeof(string).FullName));
    }

    private static BehaviorExecutionContext MakeRuntimeStateContext<TBehavior>(
        string behaviorId,
        TBehavior behavior,
        object input,
        IBehaviorContext behaviorContext)
        where TBehavior : class
    {
        var descriptor = new BehaviorTopologyDescriptor(
            id: behaviorId,
            pattern: "saga-choreography",
            transportIds: ["in-memory"],
            sourceModuleId: "tests.saga-runtime-host");
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

    [AppBehavior("tests.sagas.hosted.escalate")]
    private sealed class HostedApprovalEscalationBehavior : IAppBehavior<HostedApprovalEscalationInput, SagaChoreographyPublication[]>
    {
        public Task<SagaChoreographyPublication[]> HandleAsync(
            HostedApprovalEscalationInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            var occurredAtUtc = new DateTimeOffset(2026, 4, 19, 5, 10, 0, TimeSpan.Zero);
            var publications = new[]
            {
                SagaChoreographyPublication.CreateJson(
                    id: "approval.hosted.escalated",
                    channelId: "approvals",
                    eventType: "approvals.hosted.escalated",
                    payload: new HostedApprovalEscalatedPayload(input.ApprovalId, input.Reason),
                    occurredAtUtc: occurredAtUtc)
            };

            return Task.FromResult(publications);
        }
    }

    [AppBehavior("tests.sagas.hosted.review-approved")]
    private sealed class HostedApprovalReviewReactor : ISagaEventReactor<HostedApprovalReviewEvent, string>
    {
        public Task<SagaChoreographyStepResult<string>> ReactAsync(
            HostedApprovalReviewEvent input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            var publication = SagaChoreographyPublication.CreateJson(
                id: "approval.hosted.reviewed",
                channelId: "approvals",
                eventType: "approvals.hosted.reviewed",
                payload: new HostedApprovalReviewedPayload(input.ApprovalId, input.Decision),
                occurredAtUtc: new DateTimeOffset(2026, 4, 19, 5, 15, 0, TimeSpan.Zero));

            return Task.FromResult(
                new SagaChoreographyStepResult<string>(
                    output: "accepted",
                    publications: [publication]));
        }
    }

    private sealed class SagaChoreographyRuntimeStateHostingModule : BehaviorModuleBase
    {
        private static readonly ModuleDescriptor DescriptorInstance = new(
            id: "tests.saga-runtime-host",
            displayName: "Saga Runtime Host",
            description: "Owns one choreography behavior for ASP.NET Core runtime-state tests.",
            version: "1.0.0");

        public override ModuleDescriptor Descriptor => DescriptorInstance;

        public override void ConfigureBehaviors(IBehaviorModuleBuilder behaviors)
        {
            behaviors.Add<HostedObservedApprovalSagaBehavior>(topology => topology
                .AsSagaChoreography()
                .ViaInMemory()
                .RequireFeatureFlag("host.workflow-preview")
                .WithApiSurface("hosted-runtime-approvals", "progress")
                .WithMetadata("lane", "approval"));
        }
    }

    private sealed record HostedObservedApprovalSagaInput(string Mode, string ApprovalId);

    private sealed record HostedObservedApprovalPayload(string ApprovalId, string Mode);

    private static void RegisterHostedRuntimeStateSlots(IServiceCollection services)
    {
        services.AddSingleton(
            SagaChoreographyRuntimeSlot.For<HostedObservedApprovalSagaBehavior, HostedObservedApprovalSagaInput, SagaChoreographyStepResult<string?>>(
                "behavior",
                "typed-step-result",
                typeof(string).FullName));
    }

    [AppBehavior("tests.sagas.hosted.runtime.progress")]
    private sealed class HostedObservedApprovalSagaBehavior : IAppBehavior<HostedObservedApprovalSagaInput, SagaChoreographyStepResult<string?>>
    {
        public Task<SagaChoreographyStepResult<string?>> HandleAsync(
            HostedObservedApprovalSagaInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            var publication = input.Mode switch
            {
                "accepted" => SagaChoreographyPublication.CreateJson(
                    id: "approval.progressed",
                    channelId: "approvals",
                    eventType: "approvals.progressed",
                    payload: new HostedObservedApprovalPayload(input.ApprovalId, input.Mode),
                    occurredAtUtc: new DateTimeOffset(2026, 4, 19, 6, 30, 0, TimeSpan.Zero),
                    metadata: new Dictionary<string, string>
                    {
                        ["lane"] = "approval"
                    }),
                "compensate" => SagaChoreographyPublication.CreateCompensationJson(
                    id: "approval.compensated",
                    channelId: "approvals",
                    eventType: "approvals.compensated",
                    payload: new HostedObservedApprovalPayload(input.ApprovalId, input.Mode),
                    occurredAtUtc: new DateTimeOffset(2026, 4, 19, 6, 35, 0, TimeSpan.Zero),
                    metadata: new Dictionary<string, string>
                    {
                        ["lane"] = "approval"
                    }),
                "fail" => SagaChoreographyPublication.CreateJson(
                    id: "approval.failed",
                    channelId: "approvals",
                    eventType: "approvals.failed",
                    payload: new HostedObservedApprovalPayload(input.ApprovalId, input.Mode),
                    occurredAtUtc: new DateTimeOffset(2026, 4, 19, 6, 40, 0, TimeSpan.Zero),
                    metadata: new Dictionary<string, string>
                    {
                        ["lane"] = "approval"
                    }),
                _ => throw new InvalidOperationException($"Unknown hosted choreography runtime mode '{input.Mode}'.")
            };

            return Task.FromResult(
                new SagaChoreographyStepResult<string?>(
                    output: input.Mode,
                    publications: [publication]));
        }
    }

    private sealed class HostingObservedSagaPublisher : ISagaChoreographyPublisher
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

    private sealed class HostingTestBehaviorContext : IBehaviorContext
    {
        private readonly List<object> replies = [];

        public HostingTestBehaviorContext(
            string behaviorId,
            string? correlationId = null,
            IReadOnlyDictionary<string, string>? metadata = null)
        {
            BehaviorId = behaviorId;
            CorrelationId = correlationId;
            Metadata = metadata ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public string BehaviorId { get; }

        public string? CorrelationId { get; }

        public IReadOnlyDictionary<string, string> Metadata { get; }

        public Cephalon.Abstractions.EventSourcing.IEventStore? EventStore => null;

        public IReadOnlyList<object> Replies => replies.AsReadOnly();

        public Task ReplyAsync(object reply, CancellationToken cancellationToken = default)
        {
            replies.Add(reply);
            return Task.CompletedTask;
        }
    }
}
