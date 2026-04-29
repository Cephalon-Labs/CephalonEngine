using Cephalon.Abstractions.Technologies;
using Cephalon.AspNetCore.Hosting;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Diagnostics;
using Cephalon.MultiTenancy.Governance.Registration;
using Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore.Hosting;
using Cephalon.MultiTenancy.Governance.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Cephalon.Tests.Hosting;

public sealed class MultiTenancyGovernanceSendGridDeliveryAspNetCoreHostingTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task MapCephalonSendGridInvitationDeliveryStatusCallbacksTranslatesWebhookPayloadAndReportsRuntimeSurface()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "Microservice";
        builder.Configuration[$"{EngineSettings.SectionName}:Technologies:0"] = "MultiTenancy";
        builder.Services.AddCephalonSendGridInvitationDeliveryAspNetCore(configure: options =>
        {
            options.RequireStatusCallbackAuthorization = false;
        });
        builder.AddCephalon(engine =>
        {
            engine.UseConfiguration(builder.Configuration);
            engine.AddMultiTenancyGovernance(options =>
            {
                options.Invitations.Add(new TenantInvitationDescriptor(
                    invitationId: "invite-sendgrid-callback",
                    tenantId: "tenant-sendgrid-callback",
                    inviteeId: "owner@example.test",
                    inviteeKind: "email",
                    displayName: "SendGrid Callback Target",
                    roles: ["member"],
                    expiresAtUtc: new DateTimeOffset(2026, 05, 01, 0, 0, 0, TimeSpan.Zero),
                    metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        [TenantInvitationDeliveryMetadataKeys.LastDeliveryProviderMessageId] = "sendgrid-message-295"
                    }));
            });
        });

        await using var app = builder.Build();
        app.MapCephalonSendGridInvitationDeliveryStatusCallbacks();

        await app.StartAsync();
        var client = app.GetTestClient();
        var observedAtUtc = new DateTimeOffset(2026, 04, 29, 16, 0, 0, TimeSpan.Zero);
        var payload = JsonSerializer.Serialize(
            new object[]
            {
                new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["event"] = "delivered",
                    ["timestamp"] = observedAtUtc.ToUnixTimeSeconds(),
                    ["sg_event_id"] = "sg-event-295",
                    ["sg_message_id"] = "sendgrid-message-295.filter-001",
                    ["status"] = "2.0.0",
                    ["reason"] = "250 OK",
                    ["email"] = "owner@example.test",
                    ["cephalonTenantId"] = "tenant-sendgrid-callback",
                    ["cephalonInvitationId"] = "invite-sendgrid-callback",
                    ["cephalonDeliveryChannel"] = "email",
                    ["cephalonSenderId"] = "sendgrid-email",
                    ["cephalonCorrelationId"] = "corr-sendgrid-callback-295"
                }
            },
            SerializerOptions);
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/engine/tenant-invitations/delivery-status/sendgrid", content);
        var result = await response.Content.ReadFromJsonAsync<SendGridInvitationDeliveryStatusCallbackResult>(SerializerOptions);
        var invitation = Assert.Single(app.Services.GetRequiredService<ITenantInvitationCatalog>().Invitations);
        var observation = Assert.Single(app.Services.GetRequiredService<ITenantInvitationDeliveryStatusObservationStore>().Observations);
        var technologySurface = Assert.Single(
            app.Services.GetRequiredService<ITechnologyRuntimeCatalog>().GetByTechnology("multi-tenancy"),
            surface => surface.SurfaceId == "tenant-invitation-delivery-sendgrid-status-callbacks");
        var endpointEntry = Assert.Single(technologySurface.Entries);
        var diagnosticsConvention = Assert.Single(
            app.Services.GetRequiredService<IRuntimeDiagnosticsCatalog>().Conventions,
            convention => convention.Source == "Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalEvents);
        Assert.Equal(1, result.TranslatedEvents);
        Assert.Equal(1, result.ReconciledEvents);
        Assert.Equal(0, result.SkippedEvents);
        Assert.Equal(0, result.DeniedEvents);
        var eventResult = Assert.Single(result.Events);
        Assert.True(eventResult.Translated);
        Assert.True(eventResult.Reconciled);
        Assert.Equal("sg-event-295", eventResult.SendGridEventId);
        Assert.Equal("sendgrid-message-295.filter-001", eventResult.SendGridMessageId);
        Assert.Equal("delivered", eventResult.SendGridEventType);
        Assert.Equal(TenantInvitationDeliveryStatusReconciliationOutcomes.Reconciled, eventResult.Outcome);
        Assert.Equal(TenantInvitationDeliveryStatuses.Delivered, invitation.Metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryStatus]);
        Assert.Equal("sendgrid-message-295", invitation.Metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryStatusProviderMessageId]);
        Assert.Equal(observedAtUtc.ToString("O", CultureInfo.InvariantCulture), invitation.Metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryStatusObservedAtUtc]);
        Assert.Equal("sendgrid-event-webhook", invitation.Metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryStatusSource]);
        Assert.Equal("sendgrid", invitation.Metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryStatusActor]);
        Assert.Equal("corr-sendgrid-callback-295", invitation.Metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryStatusCorrelationId]);
        Assert.Equal("sendgrid:sg-event-295", invitation.Metadata[TenantInvitationDeliveryMetadataKeys.DeliveryStatusObservationId]);
        Assert.Equal("true", invitation.Metadata["sendGridEventWebhook"]);
        Assert.Equal("cephalon-managed", invitation.Metadata["sendGridEventWebhookTranslationOwnership"]);
        Assert.Equal("application-managed", invitation.Metadata["sendGridEventWebhookSignatureVerificationOwnership"]);
        Assert.Equal("sg_message_id-prefix", invitation.Metadata["sendGridProviderMessageIdSource"]);
        Assert.Equal("2.0.0", invitation.Metadata["sendGridStatus"]);
        Assert.Equal("sg-event-295", invitation.Metadata["sendGridEventId"]);
        Assert.Equal("sendgrid-message-295.filter-001", invitation.Metadata["sendGridMessageId"]);
        Assert.Equal(invitation.Metadata[TenantInvitationDeliveryMetadataKeys.DeliveryStatusObservationId], observation.ObservationId);
        Assert.Equal("sendgrid-event-webhook", observation.Source);
        Assert.Equal("mapped", endpointEntry.Metadata["runtimeState"]);
        Assert.Equal("true", endpointEntry.Metadata["endpointMapped"]);
        Assert.Equal("false", endpointEntry.Metadata["requireAuthorization"]);
        Assert.Equal("/engine/tenant-invitations/delivery-status/sendgrid", endpointEntry.Metadata["routePattern"]);
        Assert.Equal("cephalon-managed", endpointEntry.Metadata["sendGridEventWebhookTranslationOwnership"]);
        Assert.Equal("application-managed", endpointEntry.Metadata["sendGridEventWebhookSignatureVerificationOwnership"]);
        Assert.Equal("application-managed", endpointEntry.Metadata["sendGridEventWebhookInboxOwnership"]);
        Assert.Equal("true", endpointEntry.Metadata["normalizeProviderMessageIdFromSgMessageId"]);
        Assert.Contains(
            diagnosticsConvention.Events,
            definition => definition.Name == "SendGridInvitationDeliveryStatusCallbackAccepted");
    }

    [Fact]
    public async Task MapCephalonSendGridInvitationDeliveryStatusCallbacksSkipsUnrelatedEventsWithoutLeakingRecipientEmail()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "Microservice";
        builder.Configuration[$"{EngineSettings.SectionName}:Technologies:0"] = "MultiTenancy";
        builder.Services.AddCephalonSendGridInvitationDeliveryAspNetCore(configure: options =>
        {
            options.RequireStatusCallbackAuthorization = false;
        });
        builder.AddCephalon(engine =>
        {
            engine.UseConfiguration(builder.Configuration);
            engine.AddMultiTenancyGovernance();
        });

        await using var app = builder.Build();
        app.MapCephalonSendGridInvitationDeliveryStatusCallbacks();

        await app.StartAsync();
        var client = app.GetTestClient();
        var payload = """
            [
              {
                "event": "delivered",
                "timestamp": 1777478400,
                "sg_event_id": "sg-event-unrelated",
                "sg_message_id": "sendgrid-unrelated.filter-001",
                "email": "private-recipient@example.test"
              }
            ]
            """;
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/engine/tenant-invitations/delivery-status/sendgrid", content);
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<SendGridInvitationDeliveryStatusCallbackResult>(body, SerializerOptions);
        var observations = app.Services.GetRequiredService<ITenantInvitationDeliveryStatusObservationStore>().Observations;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalEvents);
        Assert.Equal(0, result.TranslatedEvents);
        Assert.Equal(0, result.ReconciledEvents);
        Assert.Equal(1, result.SkippedEvents);
        Assert.Equal(0, result.DeniedEvents);
        var eventResult = Assert.Single(result.Events);
        Assert.False(eventResult.Translated);
        Assert.False(eventResult.Reconciled);
        Assert.Equal("missing-cephalon-context", eventResult.Outcome);
        Assert.DoesNotContain("private-recipient@example.test", body, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(observations);
    }

    [Fact]
    public async Task MapCephalonSendGridInvitationDeliveryStatusCallbacksDeniesAnonymousByDefault()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "Microservice";
        builder.Configuration[$"{EngineSettings.SectionName}:Technologies:0"] = "MultiTenancy";
        builder.Services.AddCephalonSendGridInvitationDeliveryAspNetCore();
        builder.AddCephalon(engine =>
        {
            engine.UseConfiguration(builder.Configuration);
            engine.AddMultiTenancyGovernance();
        });

        await using var app = builder.Build();
        app.MapCephalonSendGridInvitationDeliveryStatusCallbacks();

        await app.StartAsync();
        var client = app.GetTestClient();
        using var content = new StringContent("[]", Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/engine/tenant-invitations/delivery-status/sendgrid", content);
        var technologySurface = Assert.Single(
            app.Services.GetRequiredService<ITechnologyRuntimeCatalog>().GetByTechnology("multi-tenancy"),
            surface => surface.SurfaceId == "tenant-invitation-delivery-sendgrid-status-callbacks");
        var endpointEntry = Assert.Single(technologySurface.Entries);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("true", endpointEntry.Metadata["requireAuthorization"]);
        Assert.Equal("host-managed-authorization", endpointEntry.Metadata["sendGridEventWebhookOAuthVerificationOwnership"]);
    }
}
