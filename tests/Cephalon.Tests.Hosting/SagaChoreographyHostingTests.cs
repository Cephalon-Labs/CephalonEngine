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
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Runtime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;

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

        await using var app = builder.Build();
        app.MapCephalon();

        await app.StartAsync();
        var client = app.GetTestClient();

        var sagaChoreographies = await client.GetFromJsonAsync<SagaChoreographyRuntimeDescriptor[]>("/engine/saga-choreographies");
        var byModule = await client.GetFromJsonAsync<SagaChoreographyRuntimeDescriptor[]>("/engine/saga-choreographies/modules/tests.saga-host");
        var byTransport = await client.GetFromJsonAsync<SagaChoreographyRuntimeDescriptor[]>("/engine/saga-choreographies/transports/rabbitmq");
        var reviewApproved = await client.GetFromJsonAsync<SagaChoreographyRuntimeDescriptor>("/engine/saga-choreographies/tests.sagas.hosted.review-approved");
        var missing = await client.GetAsync("/engine/saga-choreographies/tests.sagas.hosted.missing");
        var snapshot = await client.GetFromJsonAsync<RuntimeIntrospectionSnapshot>("/engine/snapshot");

        Assert.NotNull(sagaChoreographies);
        Assert.Equal(2, sagaChoreographies.Length);
        Assert.NotNull(byModule);
        Assert.Equal(2, byModule.Length);
        Assert.NotNull(byTransport);
        Assert.Single(byTransport);
        Assert.NotNull(reviewApproved);
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

        Assert.Equal(2, snapshot!.SagaChoreographies.Count);
        var snapshotReviewApproved = Assert.Single(
            snapshot.SagaChoreographies,
            static descriptor => string.Equals(descriptor.Id, "tests.sagas.hosted.review-approved", StringComparison.Ordinal));
        Assert.Equal(reviewApproved.BehaviorType, snapshotReviewApproved.BehaviorType);
        Assert.Equal(reviewApproved.InputType, snapshotReviewApproved.InputType);
        Assert.Equal(reviewApproved.ResultType, snapshotReviewApproved.ResultType);
        Assert.Equal(reviewApproved.LocalOutputType, snapshotReviewApproved.LocalOutputType);
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
}
