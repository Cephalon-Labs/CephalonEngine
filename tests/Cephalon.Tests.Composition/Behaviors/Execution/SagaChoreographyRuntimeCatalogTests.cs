using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.Execution;
using Cephalon.Abstractions.Modules;
using Cephalon.Behaviors.Hosting;
using Cephalon.Behaviors.Modules;
using Cephalon.Behaviors.Patterns.Abstractions;
using Cephalon.Behaviors.Patterns.Hosting;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Composition;

public sealed class SagaChoreographyRuntimeCatalogTests
{
    [Fact]
    public void BuildProjectsSagaChoreographyCatalogIntoSnapshot()
    {
        var services = new ServiceCollection();
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(blueprint: "ModularMonolith"));
            engine.AddBehaviors(
                configureOptions: options => options.AutoRegister = false,
                configure: behaviors => behaviors.AddBehaviorPatterns());
            engine.AddModule(new SagaChoreographyCatalogModule());
        });

        using var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<ISagaChoreographyRuntimeCatalog>();
        var snapshot = provider.GetRequiredService<IRuntimeIntrospectionSnapshotProvider>().CreateSnapshot();

        Assert.Equal(2, catalog.SagaChoreographies.Count);

        var escalation = catalog.GetById("tests.sagas.approvals.escalate");
        Assert.NotNull(escalation);
        Assert.Equal("tests.saga-owner", escalation.SourceModuleId);
        Assert.Equal(["in-memory"], escalation.TransportIds);
        Assert.Equal(["host.approvals-v2"], escalation.RequiredFeatureFlagIds);
        Assert.Equal([200, 202, 204], escalation.SuccessStatusCodes);
        Assert.Contains(nameof(ApprovalEscalationBehavior), escalation.BehaviorType, StringComparison.Ordinal);
        Assert.Contains(nameof(ApprovalEscalationInput), escalation.InputType, StringComparison.Ordinal);
        Assert.Contains(nameof(SagaChoreographyPublication), escalation.ResultType, StringComparison.Ordinal);
        Assert.Null(escalation.LocalOutputType);
        Assert.Equal("approval", escalation.Metadata["lane"]);
        Assert.Equal("behaviors.saga-choreography", escalation.Metadata["capabilityKey"]);
        Assert.Equal("ABT-005", escalation.Metadata["compatibilityRuleId"]);
        Assert.Equal("behavior", escalation.Metadata["authoringModel"]);
        Assert.Equal("publication-sequence", escalation.Metadata["publicationResultShape"]);
        Assert.Equal("Cephalon.Behaviors.Patterns.Abstractions.ISagaChoreographyPublisher", escalation.Metadata["publisherContract"]);
        Assert.Equal("choreography-publications", escalation.Metadata["publicationMode"]);
        Assert.Equal("approvals", escalation.Metadata["apiSurfaceGroupPath"]);
        Assert.Equal("escalate", escalation.Metadata["apiSurfaceOperationPath"]);

        var reviewApproved = catalog.GetById("tests.sagas.approvals.review-approved");
        Assert.NotNull(reviewApproved);
        Assert.Equal("tests.saga-owner", reviewApproved.SourceModuleId);
        Assert.Equal(["in-memory", "rabbitmq"], reviewApproved.TransportIds);
        Assert.Equal(["host.approvals-v2"], reviewApproved.RequiredFeatureFlagIds);
        Assert.Equal([200, 202, 204], reviewApproved.SuccessStatusCodes);
        Assert.Contains(nameof(ApprovalReviewReactor), reviewApproved.BehaviorType, StringComparison.Ordinal);
        Assert.Contains(nameof(ApprovalReviewEvent), reviewApproved.InputType, StringComparison.Ordinal);
        Assert.Contains(nameof(SagaChoreographyStepResult<string>), reviewApproved.ResultType, StringComparison.Ordinal);
        Assert.Equal(typeof(string).FullName, reviewApproved.LocalOutputType);
        Assert.Equal("approval", reviewApproved.Metadata["lane"]);
        Assert.Equal("reactor", reviewApproved.Metadata["authoringModel"]);
        Assert.Equal("typed-step-result", reviewApproved.Metadata["publicationResultShape"]);
        Assert.Equal("approvals", reviewApproved.Metadata["apiSurfaceGroupPath"]);
        Assert.Equal("review-approved", reviewApproved.Metadata["apiSurfaceOperationPath"]);

        Assert.Equal(2, catalog.GetBySourceModule("tests.saga-owner").Count);
        Assert.Single(catalog.GetByTransportId("rabbitmq"));
        Assert.Equal(2, catalog.GetByTransportId("in-memory").Count);
        Assert.Empty(catalog.GetByTransportId("grpc"));

        Assert.Equal(2, snapshot.SagaChoreographies.Count);
        var snapshotEscalation = Assert.Single(
            snapshot.SagaChoreographies,
            static descriptor => string.Equals(descriptor.Id, "tests.sagas.approvals.escalate", StringComparison.Ordinal));
        Assert.Equal(escalation.SourceModuleId, snapshotEscalation.SourceModuleId);
        Assert.Equal(escalation.TransportIds, snapshotEscalation.TransportIds);
        Assert.Equal(escalation.RequiredFeatureFlagIds, snapshotEscalation.RequiredFeatureFlagIds);
        Assert.Equal(escalation.ResultType, snapshotEscalation.ResultType);
        Assert.Equal(escalation.Metadata["publicationResultShape"], snapshotEscalation.Metadata["publicationResultShape"]);

        var snapshotReviewApproved = Assert.Single(
            snapshot.SagaChoreographies,
            static descriptor => string.Equals(descriptor.Id, "tests.sagas.approvals.review-approved", StringComparison.Ordinal));
        Assert.Equal(reviewApproved.BehaviorType, snapshotReviewApproved.BehaviorType);
        Assert.Equal(reviewApproved.InputType, snapshotReviewApproved.InputType);
        Assert.Equal(reviewApproved.ResultType, snapshotReviewApproved.ResultType);
        Assert.Equal(reviewApproved.LocalOutputType, snapshotReviewApproved.LocalOutputType);
        Assert.Equal(reviewApproved.Metadata["authoringModel"], snapshotReviewApproved.Metadata["authoringModel"]);
    }

    private sealed class SagaChoreographyCatalogModule : BehaviorModuleBase
    {
        private static readonly ModuleDescriptor DescriptorInstance = new(
            id: "tests.saga-owner",
            displayName: "Saga Owner",
            description: "Owns saga choreography behaviors for runtime catalog tests.",
            version: "1.0.0");

        public override ModuleDescriptor Descriptor => DescriptorInstance;

        public override void ConfigureBehaviors(IBehaviorModuleBuilder behaviors)
        {
            behaviors.Add<ApprovalEscalationBehavior>(topology => topology
                .AsSagaChoreography()
                .ViaInMemory()
                .RequireFeatureFlag("host.approvals-v2")
                .WithApiSurface("approvals", "escalate")
                .WithMetadata("lane", "approval"));

            behaviors.Add<ApprovalReviewReactor>(topology => topology
                .AsSagaChoreography()
                .ViaInMemory()
                .ViaRabbitMq()
                .RequireFeatureFlag("host.approvals-v2")
                .WithApiSurface("approvals", "review-approved")
                .WithMetadata("lane", "approval"));
        }
    }

    private sealed record ApprovalEscalationInput(string ApprovalId, string Reason);

    private sealed record ApprovalReviewEvent(string ApprovalId, string Decision);

    private sealed record ApprovalEscalatedPayload(string ApprovalId, string Reason);

    private sealed record ApprovalReviewedPayload(string ApprovalId, string Decision);

    [AppBehavior("tests.sagas.approvals.escalate")]
    private sealed class ApprovalEscalationBehavior : IAppBehavior<ApprovalEscalationInput, SagaChoreographyPublication[]>
    {
        public Task<SagaChoreographyPublication[]> HandleAsync(
            ApprovalEscalationInput input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            var occurredAtUtc = new DateTimeOffset(2026, 4, 19, 5, 0, 0, TimeSpan.Zero);
            var publications = new[]
            {
                SagaChoreographyPublication.CreateJson(
                    id: "approval.escalated",
                    channelId: "approvals",
                    eventType: "approvals.escalated",
                    payload: new ApprovalEscalatedPayload(input.ApprovalId, input.Reason),
                    occurredAtUtc: occurredAtUtc),
                SagaChoreographyPublication.CreateCompensationJson(
                    id: "approval.escalation.revert",
                    channelId: "approvals",
                    eventType: "approvals.escalation.revert",
                    payload: new ApprovalEscalatedPayload(input.ApprovalId, input.Reason),
                    occurredAtUtc: occurredAtUtc.AddMinutes(1))
            };

            return Task.FromResult(publications);
        }
    }

    [AppBehavior("tests.sagas.approvals.review-approved")]
    private sealed class ApprovalReviewReactor : ISagaEventReactor<ApprovalReviewEvent, string>
    {
        public Task<SagaChoreographyStepResult<string>> ReactAsync(
            ApprovalReviewEvent input,
            IBehaviorContext context,
            CancellationToken ct = default)
        {
            var publication = SagaChoreographyPublication.CreateJson(
                id: "approval.reviewed",
                channelId: "approvals",
                eventType: "approvals.reviewed",
                payload: new ApprovalReviewedPayload(input.ApprovalId, input.Decision),
                occurredAtUtc: new DateTimeOffset(2026, 4, 19, 5, 5, 0, TimeSpan.Zero));

            return Task.FromResult(
                new SagaChoreographyStepResult<string>(
                    output: "accepted",
                    publications: [publication]));
        }
    }
}
