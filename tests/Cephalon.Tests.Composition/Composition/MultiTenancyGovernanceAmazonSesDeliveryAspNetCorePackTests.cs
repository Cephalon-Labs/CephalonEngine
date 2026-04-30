using Cephalon.Abstractions.Technologies;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Diagnostics;
using Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore.Hosting;
using Cephalon.MultiTenancy.Governance.Registration;
using Cephalon.MultiTenancy.Governance.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Cephalon.Tests.Composition;

public sealed class MultiTenancyGovernanceAmazonSesDeliveryAspNetCorePackTests
{
    [Fact]
    public async Task AmazonSesSnsStatusCallbackReconcilesDeliveryEventAndRecordsSafeMetadata()
    {
        await using var app = await CreateAppAsync(
            configureEndpoint: options =>
            {
                options.RequireStatusCallbackAuthorization = false;
                options.MapEngagementEventsAsDelivered = true;
            });
        var client = app.GetTestClient();

        using var response = await client.PostAsync(
            "/engine/tenant-invitations/delivery-status/amazon-ses",
            CreateJsonContent(CreateSnsNotificationPayload(
                snsMessageId: "sns-message-307",
                sesMessageId: "ses-message-307",
                eventType: "Delivery",
                eventBody:
                """
                "delivery": {
                  "timestamp": "2026-04-30T04:00:00.000Z",
                  "smtpResponse": "250 2.6.0 Message received"
                }
                """)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var resultDocument = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(1, resultDocument.RootElement.GetProperty("totalEvents").GetInt32());
        Assert.Equal(1, resultDocument.RootElement.GetProperty("translatedEvents").GetInt32());
        Assert.Equal(1, resultDocument.RootElement.GetProperty("reconciledEvents").GetInt32());
        Assert.False(resultDocument.RootElement.GetProperty("snsSignatureVerificationRequired").GetBoolean());
        Assert.Equal("not-configured", resultDocument.RootElement.GetProperty("snsSignatureVerificationOutcome").GetString());

        var eventResult = Assert.Single(resultDocument.RootElement.GetProperty("events").EnumerateArray());
        Assert.Equal("sns-message-307", eventResult.GetProperty("snsMessageId").GetString());
        Assert.Equal("ses-message-307", eventResult.GetProperty("amazonSesMessageId").GetString());
        Assert.Equal(TenantInvitationDeliveryStatuses.Delivered, eventResult.GetProperty("status").GetString());
        Assert.True(eventResult.GetProperty("reconciled").GetBoolean());

        var invitation = Assert.Single(app.Services.GetRequiredService<ITenantInvitationCatalog>().Invitations);
        Assert.Equal(TenantInvitationDeliveryStatuses.Delivered, invitation.Metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryStatus]);
        Assert.Equal("ses-message-307", invitation.Metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryStatusProviderMessageId]);
        Assert.Equal("amazon-ses-sns", invitation.Metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryStatusSource]);
        Assert.Equal("amazon-ses-sns:sns-message-307", invitation.Metadata[TenantInvitationDeliveryMetadataKeys.DeliveryStatusObservationId]);
        Assert.Equal("cephalon-managed", invitation.Metadata["amazonSesSnsTranslationOwnership"]);
        Assert.Equal("not-configured", invitation.Metadata["amazonSesSnsSignatureVerification"]);
        Assert.Equal("Delivery", invitation.Metadata["amazonSesEventType"]);
        Assert.Equal("250 2.6.0 Message received", invitation.Metadata["amazonSesDeliverySmtpResponse"]);

        var observation = Assert.Single(app.Services.GetRequiredService<ITenantInvitationDeliveryStatusObservationStore>().Observations);
        Assert.Equal("amazon-ses-sns:sns-message-307", observation.ObservationId);
        Assert.Equal(TenantInvitationDeliveryStatuses.Delivered, observation.Status);

        var diagnosticsCatalog = app.Services.GetRequiredService<IRuntimeDiagnosticsCatalog>();
        Assert.Contains(
            diagnosticsCatalog.Conventions,
            convention => convention.Source == "Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore");

        var technologyCatalog = app.Services.GetRequiredService<ITechnologyRuntimeCatalog>();
        var surface = Assert.Single(
            technologyCatalog.Surfaces,
            surface => surface.SurfaceId == "tenant-invitation-delivery-amazon-ses-status-callbacks");
        var entry = Assert.Single(surface.Entries);
        Assert.Equal("mapped", entry.Metadata["runtimeState"]);
        Assert.Equal("cephalon-managed", entry.Metadata["amazonSesSnsTranslationOwnership"]);
        Assert.Equal("not-configured", entry.Metadata["amazonSesSnsSignatureVerificationOwnership"]);
        Assert.Equal("application-managed", entry.Metadata["amazonSesSnsInboxOwnership"]);
    }

    [Fact]
    public async Task AmazonSesSnsStatusCallbackSkipsSubscriptionConfirmationWithoutAutoConfirming()
    {
        await using var app = await CreateAppAsync(
            configureEndpoint: options => options.RequireStatusCallbackAuthorization = false);
        var client = app.GetTestClient();
        var payload = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["Type"] = "SubscriptionConfirmation",
            ["MessageId"] = "sns-subscribe-307",
            ["TopicArn"] = "arn:aws:sns:us-east-1:123456789012:cephalon-governance",
            ["SubscribeURL"] = "https://sns.us-east-1.amazonaws.com/?Action=ConfirmSubscription",
            ["Timestamp"] = "2026-04-30T04:05:00.000Z"
        });

        using var response = await client.PostAsync(
            "/engine/tenant-invitations/delivery-status/amazon-ses",
            CreateJsonContent(payload));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var resultDocument = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(1, resultDocument.RootElement.GetProperty("totalEvents").GetInt32());
        Assert.Equal(0, resultDocument.RootElement.GetProperty("translatedEvents").GetInt32());
        Assert.Equal(1, resultDocument.RootElement.GetProperty("skippedEvents").GetInt32());

        var eventResult = Assert.Single(resultDocument.RootElement.GetProperty("events").EnumerateArray());
        Assert.Equal("sns-subscribe-307", eventResult.GetProperty("snsMessageId").GetString());
        Assert.Equal("SubscriptionConfirmation", eventResult.GetProperty("snsMessageType").GetString());
        Assert.Equal("sns-message-type-not-translated", eventResult.GetProperty("outcome").GetString());
        Assert.False(eventResult.GetProperty("translated").GetBoolean());
        Assert.Empty(app.Services.GetRequiredService<ITenantInvitationDeliveryStatusObservationStore>().Observations);
    }

    [Fact]
    public async Task AmazonSesSnsStatusCallbackMapsTransientBounceToDeferred()
    {
        await using var app = await CreateAppAsync(
            configureEndpoint: options => options.RequireStatusCallbackAuthorization = false);
        var client = app.GetTestClient();

        using var response = await client.PostAsync(
            "/engine/tenant-invitations/delivery-status/amazon-ses",
            CreateJsonContent(CreateSnsNotificationPayload(
                snsMessageId: "sns-bounce-307",
                sesMessageId: "ses-message-307",
                eventType: "Bounce",
                eventBody:
                """
                "bounce": {
                  "bounceType": "Transient",
                  "bounceSubType": "General",
                  "timestamp": "2026-04-30T04:10:00.000Z"
                }
                """)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var resultDocument = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(1, resultDocument.RootElement.GetProperty("reconciledEvents").GetInt32());

        var invitation = Assert.Single(app.Services.GetRequiredService<ITenantInvitationCatalog>().Invitations);
        Assert.Equal(TenantInvitationDeliveryStatuses.Deferred, invitation.Metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryStatus]);
        Assert.Equal("Transient", invitation.Metadata["amazonSesBounceType"]);
        Assert.Equal("General", invitation.Metadata["amazonSesBounceSubType"]);
    }

    private static async Task<WebApplication> CreateAppAsync(
        Action<Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore.Configuration.AmazonSesInvitationDeliveryAspNetCoreOptions> configureEndpoint)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddCephalonAmazonSesInvitationDeliveryAspNetCore(configure: configureEndpoint);
        builder.Services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                technologies: ["MultiTenancy"],
                tenancy: new TenancySettings(
                    enabled: true,
                    mode: "SharedDatabase")));
            engine.AddMultiTenancyGovernance(options =>
            {
                options.Invitations.Add(new TenantInvitationDescriptor(
                    invitationId: "invite-ses-callback",
                    tenantId: "tenant-ses",
                    inviteeId: "owner@example.test",
                    inviteeKind: "email",
                    displayName: "SES Callback Invitee",
                    roles: ["admin"],
                    expiresAtUtc: new DateTimeOffset(2026, 05, 01, 0, 0, 0, TimeSpan.Zero),
                    metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        [TenantInvitationDeliveryMetadataKeys.LastDeliveryProviderMessageId] = "ses-message-307",
                        [TenantInvitationDeliveryMetadataKeys.LastDeliveryOutcome] = TenantInvitationDeliveryOutcomes.Dispatched
                    }));
            });
        });

        var app = builder.Build();
        app.MapCephalonAmazonSesInvitationDeliveryStatusCallbacks();
        await app.StartAsync();
        return app;
    }

    private static StringContent CreateJsonContent(string payload) =>
        new(payload, Encoding.UTF8, "application/json");

    private static string CreateSnsNotificationPayload(
        string snsMessageId,
        string sesMessageId,
        string eventType,
        string eventBody)
    {
        var sesEvent = $$"""
        {
          "eventType": "{{eventType}}",
          "mail": {
            "timestamp": "2026-04-30T03:59:00.000Z",
            "messageId": "{{sesMessageId}}",
            "tags": {
              "cephalon-tenant-id": ["tenant-ses"],
              "cephalon-invitation-id": ["invite-ses-callback"],
              "cephalon-delivery-channel": ["email"],
              "cephalon-sender-id": ["amazon-ses-email"],
              "cephalon-correlation-id": ["corr-ses-callback-307"]
            }
          },
          {{eventBody}}
        }
        """;

        return JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["Type"] = "Notification",
            ["MessageId"] = snsMessageId,
            ["TopicArn"] = "arn:aws:sns:us-east-1:123456789012:cephalon-governance",
            ["Subject"] = "Amazon SES Email Event",
            ["Message"] = sesEvent,
            ["Timestamp"] = "2026-04-30T04:00:01.000Z",
            ["SignatureVersion"] = "2",
            ["Signature"] = "not-verified-in-this-slice",
            ["SigningCertURL"] = "https://sns.us-east-1.amazonaws.com/SimpleNotificationService.pem"
        });
    }
}
