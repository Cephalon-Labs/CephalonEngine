using Cephalon.Abstractions.Technologies;
using Cephalon.AspNetCore.Hosting;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Diagnostics;
using Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore.Hosting;
using Cephalon.MultiTenancy.Governance.Registration;
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

public sealed class MultiTenancyGovernanceMailgunDeliveryAspNetCoreHostingTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task MapCephalonMailgunInvitationDeliveryStatusCallbacksTranslatesWebhookPayloadAndReportsRuntimeSurface()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "Microservice";
        builder.Configuration[$"{EngineSettings.SectionName}:Technologies:0"] = "MultiTenancy";
        builder.Services.AddCephalonMailgunInvitationDeliveryAspNetCore(configure: options =>
        {
            options.RequireStatusCallbackAuthorization = false;
        });
        builder.AddCephalon(engine =>
        {
            engine.UseConfiguration(builder.Configuration);
            engine.AddMultiTenancyGovernance(options =>
            {
                options.Invitations.Add(new TenantInvitationDescriptor(
                    invitationId: "invite-mailgun-callback",
                    tenantId: "tenant-mailgun-callback",
                    inviteeId: "owner@example.test",
                    inviteeKind: "email",
                    displayName: "Mailgun Callback Target",
                    roles: ["member"],
                    expiresAtUtc: new DateTimeOffset(2026, 05, 01, 0, 0, 0, TimeSpan.Zero),
                    metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        [TenantInvitationDeliveryMetadataKeys.LastDeliveryProviderMessageId] = "<mailgun-message-300@example.test>"
                    }));
            });
        });

        await using var app = builder.Build();
        app.MapCephalonMailgunInvitationDeliveryStatusCallbacks();

        await app.StartAsync();
        var client = app.GetTestClient();
        var observedAtUtc = new DateTimeOffset(2026, 04, 30, 18, 0, 0, TimeSpan.Zero);
        var payload = JsonSerializer.Serialize(
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["event"] = "delivered",
                ["id"] = "mailgun-event-300",
                ["timestamp"] = observedAtUtc.ToUnixTimeSeconds(),
                ["log-level"] = "info",
                ["recipient"] = "owner@example.test",
                ["domain"] = new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["name"] = "mg.example.test"
                },
                ["flags"] = new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["is-test-mode"] = true
                },
                ["message"] = new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["headers"] = new Dictionary<string, object?>(StringComparer.Ordinal)
                    {
                        ["message-id"] = "mailgun-message-300@example.test",
                        ["subject"] = "Sample webhook payload"
                    }
                },
                ["delivery-status"] = new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["code"] = 250,
                    ["message"] = "OK",
                    ["description"] = "Delivered"
                },
                ["user-variables"] = new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["cephalonTenantId"] = "tenant-mailgun-callback",
                    ["cephalonInvitationId"] = "invite-mailgun-callback",
                    ["cephalonDeliveryChannel"] = "email",
                    ["cephalonSenderId"] = "mailgun-email",
                    ["cephalonCorrelationId"] = "corr-mailgun-callback-300"
                }
            },
            SerializerOptions);
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/engine/tenant-invitations/delivery-status/mailgun", content);
        var result = await response.Content.ReadFromJsonAsync<MailgunInvitationDeliveryStatusCallbackResult>(SerializerOptions);
        var invitation = Assert.Single(app.Services.GetRequiredService<ITenantInvitationCatalog>().Invitations);
        var observation = Assert.Single(app.Services.GetRequiredService<ITenantInvitationDeliveryStatusObservationStore>().Observations);
        var technologySurface = Assert.Single(
            app.Services.GetRequiredService<ITechnologyRuntimeCatalog>().GetByTechnology("multi-tenancy"),
            surface => surface.SurfaceId == "tenant-invitation-delivery-mailgun-status-callbacks");
        var endpointEntry = Assert.Single(technologySurface.Entries);
        var diagnosticsConvention = Assert.Single(
            app.Services.GetRequiredService<IRuntimeDiagnosticsCatalog>().Conventions,
            convention => convention.Source == "Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore");

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
        Assert.Equal("mailgun-event-300", eventResult.MailgunEventId);
        Assert.Equal("mailgun-message-300@example.test", eventResult.MailgunMessageId);
        Assert.Equal("delivered", eventResult.MailgunEventType);
        Assert.Equal(TenantInvitationDeliveryStatusReconciliationOutcomes.Reconciled, eventResult.Outcome);
        Assert.Equal(TenantInvitationDeliveryStatuses.Delivered, invitation.Metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryStatus]);
        Assert.Equal("<mailgun-message-300@example.test>", invitation.Metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryStatusProviderMessageId]);
        Assert.Equal(observedAtUtc.ToString("O", CultureInfo.InvariantCulture), invitation.Metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryStatusObservedAtUtc]);
        Assert.Equal("mailgun-webhook", invitation.Metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryStatusSource]);
        Assert.Equal("mailgun", invitation.Metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryStatusActor]);
        Assert.Equal("corr-mailgun-callback-300", invitation.Metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryStatusCorrelationId]);
        Assert.Equal("mailgun:mailgun-event-300", invitation.Metadata[TenantInvitationDeliveryMetadataKeys.DeliveryStatusObservationId]);
        Assert.Equal("true", invitation.Metadata["mailgunWebhook"]);
        Assert.Equal("cephalon-managed", invitation.Metadata["mailgunWebhookTranslationOwnership"]);
        Assert.Equal("not-configured", invitation.Metadata["mailgunWebhookSignatureVerificationOwnership"]);
        Assert.Equal("not-configured", invitation.Metadata["mailgunWebhookReplayProtectionOwnership"]);
        Assert.Equal("message.headers.message-id", invitation.Metadata["mailgunProviderMessageIdSource"]);
        Assert.Equal("angle-brackets", invitation.Metadata["mailgunProviderMessageIdNormalization"]);
        Assert.Equal("mailgun-event-300", invitation.Metadata["mailgunEventId"]);
        Assert.Equal("mailgun-message-300@example.test", invitation.Metadata["mailgunMessageId"]);
        Assert.Equal("<mailgun-message-300@example.test>", invitation.Metadata["mailgunProviderMessageId"]);
        Assert.Equal("250", invitation.Metadata["mailgunDeliveryStatusCode"]);
        Assert.Equal("true", invitation.Metadata["mailgunTestMode"]);
        Assert.Equal(invitation.Metadata[TenantInvitationDeliveryMetadataKeys.DeliveryStatusObservationId], observation.ObservationId);
        Assert.Equal("mailgun-webhook", observation.Source);
        Assert.Equal("mapped", endpointEntry.Metadata["runtimeState"]);
        Assert.Equal("true", endpointEntry.Metadata["endpointMapped"]);
        Assert.Equal("false", endpointEntry.Metadata["requireAuthorization"]);
        Assert.Equal("/engine/tenant-invitations/delivery-status/mailgun", endpointEntry.Metadata["routePattern"]);
        Assert.Equal("cephalon-managed", endpointEntry.Metadata["mailgunWebhookTranslationOwnership"]);
        Assert.Equal("application-managed", endpointEntry.Metadata["mailgunWebhookInboxOwnership"]);
        Assert.Equal("not-configured", endpointEntry.Metadata["mailgunWebhookSignatureVerificationOwnership"]);
        Assert.Equal("not-configured", endpointEntry.Metadata["mailgunWebhookReplayProtectionOwnership"]);
        Assert.Equal("false", endpointEntry.Metadata["mailgunWebhookSignatureVerificationRequired"]);
        Assert.Equal("timestamp+token", endpointEntry.Metadata["mailgunWebhookSignaturePayload"]);
        Assert.Equal("true", endpointEntry.Metadata["normalizeProviderMessageIdWithAngleBrackets"]);
        Assert.Contains(
            diagnosticsConvention.Events,
            definition => definition.Name == "MailgunInvitationDeliveryStatusCallbackAccepted");
    }

    [Fact]
    public async Task MapCephalonMailgunInvitationDeliveryStatusCallbacksSkipsEngagementEventsByDefault()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "Microservice";
        builder.Configuration[$"{EngineSettings.SectionName}:Technologies:0"] = "MultiTenancy";
        builder.Services.AddCephalonMailgunInvitationDeliveryAspNetCore(configure: options =>
        {
            options.RequireStatusCallbackAuthorization = false;
        });
        builder.AddCephalon(engine =>
        {
            engine.UseConfiguration(builder.Configuration);
            engine.AddMultiTenancyGovernance();
        });

        await using var app = builder.Build();
        app.MapCephalonMailgunInvitationDeliveryStatusCallbacks();

        await app.StartAsync();
        var client = app.GetTestClient();
        var payload = JsonSerializer.Serialize(
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["event"] = "opened",
                ["id"] = "mailgun-event-opened-300",
                ["timestamp"] = 1777478400,
                ["message"] = new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["headers"] = new Dictionary<string, object?>(StringComparer.Ordinal)
                    {
                        ["message-id"] = "mailgun-message-opened@example.test"
                    }
                },
                ["user-variables"] = new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["cephalonTenantId"] = "tenant-mailgun-opened",
                    ["cephalonInvitationId"] = "invite-mailgun-opened"
                }
            },
            SerializerOptions);
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/engine/tenant-invitations/delivery-status/mailgun", content);
        var result = await response.Content.ReadFromJsonAsync<MailgunInvitationDeliveryStatusCallbackResult>(SerializerOptions);
        var observations = app.Services.GetRequiredService<ITenantInvitationDeliveryStatusObservationStore>().Observations;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalEvents);
        Assert.Equal(0, result.TranslatedEvents);
        Assert.Equal(0, result.ReconciledEvents);
        Assert.Equal(1, result.SkippedEvents);
        var eventResult = Assert.Single(result.Events);
        Assert.False(eventResult.Translated);
        Assert.False(eventResult.Reconciled);
        Assert.Equal("unsupported-event-type", eventResult.Outcome);
        Assert.Equal("opened", eventResult.MailgunEventType);
        Assert.Empty(observations);
    }
}
