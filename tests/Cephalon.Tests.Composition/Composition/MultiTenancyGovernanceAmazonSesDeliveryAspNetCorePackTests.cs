using Cephalon.Abstractions.Technologies;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Diagnostics;
using Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore.Configuration;
using Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore.Hosting;
using Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore.Services;
using Cephalon.MultiTenancy.Governance.Registration;
using Cephalon.MultiTenancy.Governance.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

namespace Cephalon.Tests.Composition;

public sealed class MultiTenancyGovernanceAmazonSesDeliveryAspNetCorePackTests
{
    private const string SnsTopicArn = "arn:aws:sns:us-east-1:123456789012:cephalon-governance";

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
        Assert.Equal("pending-record", invitation.Metadata["amazonSesSnsMessageIdIdempotency"]);
        Assert.Equal("cephalon-managed", invitation.Metadata["amazonSesSnsMessageIdIdempotencyOwnership"]);
        Assert.Equal("observation-store", invitation.Metadata["amazonSesSnsMessageIdIdempotencyScope"]);
        Assert.Equal("amazon-ses-sns:sns-message-307", invitation.Metadata["amazonSesSnsMessageIdObservationId"]);
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
        Assert.Equal("cephalon-managed", entry.Metadata["amazonSesSnsMessageIdIdempotencyOwnership"]);
        Assert.Equal("observation-store", entry.Metadata["amazonSesSnsMessageIdIdempotencyScope"]);
        Assert.Equal("application-managed", entry.Metadata["amazonSesSnsInboxOwnership"]);
        Assert.Equal("false", entry.Metadata["amazonSesSnsSubscriptionConfirmationConfigured"]);
        Assert.Equal("application-managed", entry.Metadata["amazonSesSnsSubscriptionConfirmationOwnership"]);
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
            ["TopicArn"] = SnsTopicArn,
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
    public async Task AmazonSesSnsStatusCallbackConfirmsVerifiedSubscriptionWhenEnabled()
    {
        using var rsa = RSA.Create(2048);
        using var certificate = CreateSigningCertificate(rsa);
        var confirmationClient = new CapturingSnsSubscriptionConfirmationClient();
        await using var app = await CreateAppAsync(
            configureEndpoint: options =>
            {
                options.RequireStatusCallbackAuthorization = false;
                options.RequireSnsSignatureVerification = true;
                options.EnableSnsSubscriptionConfirmation = true;
                options.AllowedSnsTopicArns = [SnsTopicArn];
                options.PinnedSnsSigningCertificatePem = certificate.ExportCertificatePem();
                options.ValidateSnsSigningCertificateChain = false;
            },
            configureServices: services => services.Replace(ServiceDescriptor.Singleton<IAmazonSesSnsSubscriptionConfirmationClient>(confirmationClient)));
        var client = app.GetTestClient();

        using var response = await client.PostAsync(
            "/engine/tenant-invitations/delivery-status/amazon-ses",
            CreateJsonContent(CreateSignedSnsSubscriptionConfirmationPayload(
                rsa,
                snsMessageId: "sns-subscribe-311",
                token: "sns-subscription-token-311",
                subscribeUrl: "https://sns.us-east-1.amazonaws.com/?Action=ConfirmSubscription&Token=sns-subscription-token-311")));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var resultDocument = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(1, resultDocument.RootElement.GetProperty("totalEvents").GetInt32());
        Assert.Equal(0, resultDocument.RootElement.GetProperty("translatedEvents").GetInt32());
        Assert.Equal(0, resultDocument.RootElement.GetProperty("reconciledEvents").GetInt32());
        Assert.Equal(1, resultDocument.RootElement.GetProperty("skippedEvents").GetInt32());
        Assert.True(resultDocument.RootElement.GetProperty("snsSignatureVerificationRequired").GetBoolean());
        Assert.True(resultDocument.RootElement.GetProperty("snsSignatureVerified").GetBoolean());
        Assert.Equal("verified", resultDocument.RootElement.GetProperty("snsSignatureVerificationOutcome").GetString());
        Assert.True(resultDocument.RootElement.GetProperty("snsReplayProtectionEnabled").GetBoolean());
        Assert.Equal("recorded", resultDocument.RootElement.GetProperty("snsReplayProtectionOutcome").GetString());
        Assert.True(resultDocument.RootElement.GetProperty("snsSubscriptionConfirmationEnabled").GetBoolean());
        Assert.Equal("confirmed", resultDocument.RootElement.GetProperty("snsSubscriptionConfirmationOutcome").GetString());
        Assert.Equal(1, resultDocument.RootElement.GetProperty("subscriptionConfirmationAttempts").GetInt32());
        Assert.Equal(1, resultDocument.RootElement.GetProperty("subscriptionConfirmationsSucceeded").GetInt32());

        var eventResult = Assert.Single(resultDocument.RootElement.GetProperty("events").EnumerateArray());
        Assert.Equal("sns-subscribe-311", eventResult.GetProperty("snsMessageId").GetString());
        Assert.Equal("SubscriptionConfirmation", eventResult.GetProperty("snsMessageType").GetString());
        Assert.Equal("subscription-confirmed", eventResult.GetProperty("outcome").GetString());
        Assert.False(eventResult.GetProperty("translated").GetBoolean());
        Assert.False(eventResult.GetProperty("reconciled").GetBoolean());

        var request = Assert.Single(confirmationClient.Requests);
        Assert.Equal(SnsTopicArn, request.TopicArn);
        Assert.Equal("sns-subscribe-311", request.MessageId);
        Assert.Equal("sns-subscription-token-311", request.Token);
        Assert.Equal("sns.us-east-1.amazonaws.com", request.SubscribeUrl.Host);
        Assert.Equal("/", request.SubscribeUrl.AbsolutePath);
        Assert.Empty(app.Services.GetRequiredService<ITenantInvitationDeliveryStatusObservationStore>().Observations);

        var technologyCatalog = app.Services.GetRequiredService<ITechnologyRuntimeCatalog>();
        var surface = Assert.Single(
            technologyCatalog.Surfaces,
            surface => surface.SurfaceId == "tenant-invitation-delivery-amazon-ses-status-callbacks");
        var entry = Assert.Single(surface.Entries);
        Assert.Equal("true", entry.Metadata["amazonSesSnsSubscriptionConfirmationConfigured"]);
        Assert.Equal("cephalon-managed", entry.Metadata["amazonSesSnsSubscriptionConfirmationOwnership"]);
        Assert.Equal("true", entry.Metadata["amazonSesSnsSubscriptionConfirmationRequiresSignature"]);
        Assert.Equal("GET", entry.Metadata["amazonSesSnsSubscriptionConfirmationHttpMethod"]);
        Assert.Equal("https-sns-confirm-subscription", entry.Metadata["amazonSesSnsSubscriptionConfirmationUrlPolicy"]);
        Assert.Equal("IAmazonSesSnsSubscriptionConfirmationClient", entry.Metadata["amazonSesSnsSubscriptionConfirmationClient"]);
    }

    [Fact]
    public async Task AmazonSesSnsStatusCallbackObservesVerifiedUnsubscribeWithoutRestoringSubscription()
    {
        using var rsa = RSA.Create(2048);
        using var certificate = CreateSigningCertificate(rsa);
        var confirmationClient = new CapturingSnsSubscriptionConfirmationClient();
        await using var app = await CreateAppAsync(
            configureEndpoint: options =>
            {
                options.RequireStatusCallbackAuthorization = false;
                options.RequireSnsSignatureVerification = true;
                options.EnableSnsUnsubscribeConfirmationObservation = true;
                options.AllowedSnsTopicArns = [SnsTopicArn];
                options.PinnedSnsSigningCertificatePem = certificate.ExportCertificatePem();
                options.ValidateSnsSigningCertificateChain = false;
            },
            configureServices: services => services.Replace(ServiceDescriptor.Singleton<IAmazonSesSnsSubscriptionConfirmationClient>(confirmationClient)));
        var client = app.GetTestClient();

        using var response = await client.PostAsync(
            "/engine/tenant-invitations/delivery-status/amazon-ses",
            CreateJsonContent(CreateSignedSnsUnsubscribeConfirmationPayload(
                rsa,
                snsMessageId: "sns-unsubscribe-312",
                token: "sns-restore-token-312",
                subscribeUrl: "https://sns.us-east-1.amazonaws.com/?Action=ConfirmSubscription&Token=sns-restore-token-312")));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var resultDocument = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(1, resultDocument.RootElement.GetProperty("totalEvents").GetInt32());
        Assert.Equal(0, resultDocument.RootElement.GetProperty("translatedEvents").GetInt32());
        Assert.Equal(0, resultDocument.RootElement.GetProperty("reconciledEvents").GetInt32());
        Assert.Equal(1, resultDocument.RootElement.GetProperty("skippedEvents").GetInt32());
        Assert.True(resultDocument.RootElement.GetProperty("snsSignatureVerificationRequired").GetBoolean());
        Assert.True(resultDocument.RootElement.GetProperty("snsSignatureVerified").GetBoolean());
        Assert.Equal("verified", resultDocument.RootElement.GetProperty("snsSignatureVerificationOutcome").GetString());
        Assert.True(resultDocument.RootElement.GetProperty("snsReplayProtectionEnabled").GetBoolean());
        Assert.Equal("recorded", resultDocument.RootElement.GetProperty("snsReplayProtectionOutcome").GetString());
        Assert.True(resultDocument.RootElement.GetProperty("snsUnsubscribeConfirmationObservationEnabled").GetBoolean());
        Assert.Equal("observed", resultDocument.RootElement.GetProperty("snsUnsubscribeConfirmationOutcome").GetString());
        Assert.Equal(1, resultDocument.RootElement.GetProperty("unsubscribeConfirmationsObserved").GetInt32());

        var eventResult = Assert.Single(resultDocument.RootElement.GetProperty("events").EnumerateArray());
        Assert.Equal("sns-unsubscribe-312", eventResult.GetProperty("snsMessageId").GetString());
        Assert.Equal("UnsubscribeConfirmation", eventResult.GetProperty("snsMessageType").GetString());
        Assert.Equal("unsubscribe-confirmation-observed", eventResult.GetProperty("outcome").GetString());
        Assert.False(eventResult.GetProperty("translated").GetBoolean());
        Assert.False(eventResult.GetProperty("reconciled").GetBoolean());
        Assert.Empty(confirmationClient.Requests);
        Assert.Empty(app.Services.GetRequiredService<ITenantInvitationDeliveryStatusObservationStore>().Observations);

        var technologyCatalog = app.Services.GetRequiredService<ITechnologyRuntimeCatalog>();
        var surface = Assert.Single(
            technologyCatalog.Surfaces,
            surface => surface.SurfaceId == "tenant-invitation-delivery-amazon-ses-status-callbacks");
        var entry = Assert.Single(surface.Entries);
        Assert.Equal("true", entry.Metadata["amazonSesSnsUnsubscribeConfirmationObservationConfigured"]);
        Assert.Equal("cephalon-managed", entry.Metadata["amazonSesSnsUnsubscribeConfirmationObservationOwnership"]);
        Assert.Equal("observe-only", entry.Metadata["amazonSesSnsUnsubscribeConfirmationAction"]);
        Assert.Equal("validated-never-invoked", entry.Metadata["amazonSesSnsUnsubscribeConfirmationSubscribeUrlPolicy"]);
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

    [Fact]
    public async Task AmazonSesSnsStatusCallbackVerifiesSnsSignatureBeforeReconciling()
    {
        using var rsa = RSA.Create(2048);
        using var certificate = CreateSigningCertificate(rsa);
        await using var app = await CreateAppAsync(
            configureEndpoint: options =>
            {
                options.RequireStatusCallbackAuthorization = false;
                options.RequireSnsSignatureVerification = true;
                options.AllowedSnsTopicArns = [SnsTopicArn];
                options.PinnedSnsSigningCertificatePem = certificate.ExportCertificatePem();
                options.ValidateSnsSigningCertificateChain = false;
            });
        var client = app.GetTestClient();

        using var response = await client.PostAsync(
            "/engine/tenant-invitations/delivery-status/amazon-ses",
            CreateJsonContent(CreateSignedSnsNotificationPayload(
                rsa,
                snsMessageId: "sns-message-308",
                sesMessageId: "ses-message-307",
                eventType: "Delivery",
                eventBody:
                """
                "delivery": {
                  "timestamp": "2026-04-30T04:15:00.000Z",
                  "smtpResponse": "250 2.6.0 Message received"
                }
                """)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var resultDocument = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(resultDocument.RootElement.GetProperty("snsSignatureVerificationRequired").GetBoolean());
        Assert.True(resultDocument.RootElement.GetProperty("snsSignatureVerified").GetBoolean());
        Assert.Equal("verified", resultDocument.RootElement.GetProperty("snsSignatureVerificationOutcome").GetString());
        Assert.True(resultDocument.RootElement.GetProperty("snsReplayProtectionEnabled").GetBoolean());
        Assert.Equal("recorded", resultDocument.RootElement.GetProperty("snsReplayProtectionOutcome").GetString());

        var invitation = Assert.Single(app.Services.GetRequiredService<ITenantInvitationCatalog>().Invitations);
        Assert.Equal(TenantInvitationDeliveryStatuses.Delivered, invitation.Metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryStatus]);
        Assert.Equal("verified", invitation.Metadata["amazonSesSnsSignatureVerification"]);
        Assert.Equal("cephalon-managed", invitation.Metadata["amazonSesSnsSignatureVerificationOwnership"]);
        Assert.Equal("2", invitation.Metadata["amazonSesSnsSignatureVersion"]);
        Assert.Equal("rsa-sha256", invitation.Metadata["amazonSesSnsSignatureAlgorithm"]);
        Assert.Equal(SnsTopicArn, invitation.Metadata["amazonSesSnsSignatureTopicArn"]);
        Assert.Equal("sns-message-308", invitation.Metadata["amazonSesSnsSignatureMessageId"]);
        Assert.StartsWith("sha256:", invitation.Metadata["amazonSesSnsSignatureFingerprint"], StringComparison.Ordinal);
        Assert.Equal("recorded", invitation.Metadata["amazonSesSnsReplayProtection"]);
        Assert.Equal("cephalon-managed", invitation.Metadata["amazonSesSnsReplayProtectionOwnership"]);
        Assert.Equal("process-local", invitation.Metadata["amazonSesSnsReplayProtectionScope"]);
        Assert.Equal("topic-arn+message-id", invitation.Metadata["amazonSesSnsReplayProtectionKey"]);
        Assert.StartsWith("sha256:", invitation.Metadata["amazonSesSnsReplayProtectionFingerprint"], StringComparison.Ordinal);
        Assert.Equal("pending-record", invitation.Metadata["amazonSesSnsMessageIdIdempotency"]);
        Assert.Equal("cephalon-managed", invitation.Metadata["amazonSesSnsMessageIdIdempotencyOwnership"]);
        Assert.Equal("MessageId", invitation.Metadata["amazonSesSnsMessageIdIdempotencyKey"]);
        Assert.Equal("amazon-ses-sns:sns-message-308", invitation.Metadata["amazonSesSnsMessageIdObservationId"]);

        var technologyCatalog = app.Services.GetRequiredService<ITechnologyRuntimeCatalog>();
        var surface = Assert.Single(
            technologyCatalog.Surfaces,
            surface => surface.SurfaceId == "tenant-invitation-delivery-amazon-ses-status-callbacks");
        var entry = Assert.Single(surface.Entries);
        Assert.Equal("cephalon-managed", entry.Metadata["amazonSesSnsSignatureVerificationOwnership"]);
        Assert.Equal("true", entry.Metadata["amazonSesSnsSignatureVerificationRequired"]);
        Assert.Equal("true", entry.Metadata["amazonSesSnsSignatureVersion2Required"]);
        Assert.Equal("1", entry.Metadata["amazonSesSnsAllowedTopicArnCount"]);
        Assert.Equal("cephalon-managed", entry.Metadata["amazonSesSnsReplayProtectionOwnership"]);
        Assert.Equal("true", entry.Metadata["amazonSesSnsReplayProtectionConfigured"]);
        Assert.Equal("process-local", entry.Metadata["amazonSesSnsReplayProtectionScope"]);
        Assert.Equal("topic-arn+message-id", entry.Metadata["amazonSesSnsReplayProtectionKey"]);
        Assert.Equal("cephalon-managed", entry.Metadata["amazonSesSnsMessageIdIdempotencyOwnership"]);
        Assert.Equal("true", entry.Metadata["amazonSesSnsMessageIdIdempotencyConfigured"]);
        Assert.Equal("MessageId", entry.Metadata["amazonSesSnsMessageIdIdempotencyKey"]);
    }

    [Fact]
    public async Task AmazonSesSnsStatusCallbackRejectsDuplicateVerifiedSnsMessageInsideReplayWindow()
    {
        using var rsa = RSA.Create(2048);
        using var certificate = CreateSigningCertificate(rsa);
        await using var app = await CreateAppAsync(
            configureEndpoint: options =>
            {
                options.RequireStatusCallbackAuthorization = false;
                options.RequireSnsSignatureVerification = true;
                options.AllowedSnsTopicArns = [SnsTopicArn];
                options.PinnedSnsSigningCertificatePem = certificate.ExportCertificatePem();
                options.ValidateSnsSigningCertificateChain = false;
            });
        var client = app.GetTestClient();
        var payload = CreateSignedSnsNotificationPayload(
            rsa,
            snsMessageId: "sns-message-309-replay",
            sesMessageId: "ses-message-307",
            eventType: "Delivery",
            eventBody:
            """
            "delivery": {
              "timestamp": "2026-04-30T04:18:00.000Z",
              "smtpResponse": "250 2.6.0 Message received"
            }
            """);

        using var firstResponse = await client.PostAsync(
            "/engine/tenant-invitations/delivery-status/amazon-ses",
            CreateJsonContent(payload));
        using var secondResponse = await client.PostAsync(
            "/engine/tenant-invitations/delivery-status/amazon-ses",
            CreateJsonContent(payload));

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
        using var secondResultDocument = JsonDocument.Parse(await secondResponse.Content.ReadAsStringAsync());
        Assert.Equal("Amazon SES SNS callback replay was rejected.", secondResultDocument.RootElement.GetProperty("title").GetString());

        var invitation = Assert.Single(app.Services.GetRequiredService<ITenantInvitationCatalog>().Invitations);
        Assert.Equal(TenantInvitationDeliveryStatuses.Delivered, invitation.Metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryStatus]);
        Assert.Equal("recorded", invitation.Metadata["amazonSesSnsReplayProtection"]);
        Assert.Single(app.Services.GetRequiredService<ITenantInvitationDeliveryStatusObservationStore>().Observations);
    }

    [Fact]
    public async Task AmazonSesSnsStatusCallbackSkipsDuplicateMessageIdAlreadyObservedByStore()
    {
        using var rsa = RSA.Create(2048);
        using var certificate = CreateSigningCertificate(rsa);
        await using var app = await CreateAppAsync(
            configureEndpoint: options =>
            {
                options.RequireStatusCallbackAuthorization = false;
                options.RequireSnsSignatureVerification = true;
                options.EnableSnsReplayProtection = false;
                options.AllowedSnsTopicArns = [SnsTopicArn];
                options.PinnedSnsSigningCertificatePem = certificate.ExportCertificatePem();
                options.ValidateSnsSigningCertificateChain = false;
            });
        var client = app.GetTestClient();
        var payload = CreateSignedSnsNotificationPayload(
            rsa,
            snsMessageId: "sns-message-310-idempotency",
            sesMessageId: "ses-message-307",
            eventType: "Delivery",
            eventBody:
            """
            "delivery": {
              "timestamp": "2026-04-30T04:19:00.000Z",
              "smtpResponse": "250 2.6.0 Message received"
            }
            """);

        using var firstResponse = await client.PostAsync(
            "/engine/tenant-invitations/delivery-status/amazon-ses",
            CreateJsonContent(payload));
        using var secondResponse = await client.PostAsync(
            "/engine/tenant-invitations/delivery-status/amazon-ses",
            CreateJsonContent(payload));

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        using var secondResultDocument = JsonDocument.Parse(await secondResponse.Content.ReadAsStringAsync());
        Assert.Equal(1, secondResultDocument.RootElement.GetProperty("translatedEvents").GetInt32());
        Assert.Equal(0, secondResultDocument.RootElement.GetProperty("reconciledEvents").GetInt32());
        Assert.Equal(1, secondResultDocument.RootElement.GetProperty("duplicateEvents").GetInt32());

        var eventResult = Assert.Single(secondResultDocument.RootElement.GetProperty("events").EnumerateArray());
        Assert.Equal("duplicate-skipped", eventResult.GetProperty("outcome").GetString());
        Assert.True(eventResult.GetProperty("translated").GetBoolean());
        Assert.False(eventResult.GetProperty("reconciled").GetBoolean());

        var invitation = Assert.Single(app.Services.GetRequiredService<ITenantInvitationCatalog>().Invitations);
        Assert.Equal(TenantInvitationDeliveryStatuses.Delivered, invitation.Metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryStatus]);
        Assert.Equal("pending-record", invitation.Metadata["amazonSesSnsMessageIdIdempotency"]);
        Assert.Equal("cephalon-managed", invitation.Metadata["amazonSesSnsMessageIdIdempotencyOwnership"]);
        Assert.Equal("observation-store", invitation.Metadata["amazonSesSnsMessageIdIdempotencyScope"]);
        Assert.Equal("amazon-ses-sns:sns-message-310-idempotency", invitation.Metadata["amazonSesSnsMessageIdObservationId"]);
        Assert.Single(app.Services.GetRequiredService<ITenantInvitationDeliveryStatusObservationStore>().Observations);
    }

    [Fact]
    public async Task AmazonSesSnsStatusCallbackRejectsInvalidSnsSignature()
    {
        using var rsa = RSA.Create(2048);
        using var certificate = CreateSigningCertificate(rsa);
        await using var app = await CreateAppAsync(
            configureEndpoint: options =>
            {
                options.RequireStatusCallbackAuthorization = false;
                options.RequireSnsSignatureVerification = true;
                options.AllowedSnsTopicArns = [SnsTopicArn];
                options.PinnedSnsSigningCertificatePem = certificate.ExportCertificatePem();
                options.ValidateSnsSigningCertificateChain = false;
            });
        var client = app.GetTestClient();

        using var response = await client.PostAsync(
            "/engine/tenant-invitations/delivery-status/amazon-ses",
            CreateJsonContent(CreateSignedSnsNotificationPayload(
                rsa,
                snsMessageId: "sns-message-308-invalid",
                sesMessageId: "ses-message-307",
                eventType: "Delivery",
                eventBody:
                """
                "delivery": {
                  "timestamp": "2026-04-30T04:16:00.000Z",
                  "smtpResponse": "250 2.6.0 Message received"
                }
                """,
                signatureOverride: Convert.ToBase64String(new byte[256]))));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        using var resultDocument = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Amazon SES SNS signature verification failed.", resultDocument.RootElement.GetProperty("title").GetString());

        var invitation = Assert.Single(app.Services.GetRequiredService<ITenantInvitationCatalog>().Invitations);
        Assert.False(invitation.Metadata.ContainsKey(TenantInvitationDeliveryMetadataKeys.LastDeliveryStatus));
        Assert.Empty(app.Services.GetRequiredService<ITenantInvitationDeliveryStatusObservationStore>().Observations);
    }

    [Fact]
    public async Task AmazonSesSnsStatusCallbackRejectsUntrustedSigningCertificateUrl()
    {
        using var rsa = RSA.Create(2048);
        using var certificate = CreateSigningCertificate(rsa);
        await using var app = await CreateAppAsync(
            configureEndpoint: options =>
            {
                options.RequireStatusCallbackAuthorization = false;
                options.RequireSnsSignatureVerification = true;
                options.AllowedSnsTopicArns = [SnsTopicArn];
                options.PinnedSnsSigningCertificatePem = certificate.ExportCertificatePem();
                options.ValidateSnsSigningCertificateChain = false;
            });
        var client = app.GetTestClient();

        using var response = await client.PostAsync(
            "/engine/tenant-invitations/delivery-status/amazon-ses",
            CreateJsonContent(CreateSignedSnsNotificationPayload(
                rsa,
                snsMessageId: "sns-message-308-untrusted-cert",
                sesMessageId: "ses-message-307",
                eventType: "Delivery",
                eventBody:
                """
                "delivery": {
                  "timestamp": "2026-04-30T04:17:00.000Z",
                  "smtpResponse": "250 2.6.0 Message received"
                }
                """,
                signingCertUrl: "https://sns.evil.amazonaws.com/SimpleNotificationService-test.pem")));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        using var resultDocument = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Amazon SES SNS signature verification failed.", resultDocument.RootElement.GetProperty("title").GetString());

        var invitation = Assert.Single(app.Services.GetRequiredService<ITenantInvitationCatalog>().Invitations);
        Assert.False(invitation.Metadata.ContainsKey(TenantInvitationDeliveryMetadataKeys.LastDeliveryStatus));
        Assert.Empty(app.Services.GetRequiredService<ITenantInvitationDeliveryStatusObservationStore>().Observations);
    }

    private static async Task<WebApplication> CreateAppAsync(
        Action<AmazonSesInvitationDeliveryAspNetCoreOptions> configureEndpoint,
        Action<IServiceCollection>? configureServices = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddCephalonAmazonSesInvitationDeliveryAspNetCore(configure: configureEndpoint);
        configureServices?.Invoke(builder.Services);
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
            ["TopicArn"] = SnsTopicArn,
            ["Subject"] = "Amazon SES Email Event",
            ["Message"] = sesEvent,
            ["Timestamp"] = "2026-04-30T04:00:01.000Z",
            ["SignatureVersion"] = "2",
            ["Signature"] = "not-verified-in-this-slice",
            ["SigningCertURL"] = "https://sns.us-east-1.amazonaws.com/SimpleNotificationService.pem"
        });
    }

    private static X509Certificate2 CreateSigningCertificate(RSA rsa)
    {
        var request = new CertificateRequest(
            "CN=SimpleNotificationService-Test",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        return request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddMinutes(-5),
            DateTimeOffset.UtcNow.AddDays(1));
    }

    private static string CreateSignedSnsNotificationPayload(
        RSA rsa,
        string snsMessageId,
        string sesMessageId,
        string eventType,
        string eventBody,
        string? signatureOverride = null,
        string signingCertUrl = "https://sns.us-east-1.amazonaws.com/SimpleNotificationService-test.pem")
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
        var values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Type"] = "Notification",
            ["MessageId"] = snsMessageId,
            ["TopicArn"] = SnsTopicArn,
            ["Subject"] = "Amazon SES Email Event",
            ["Message"] = sesEvent,
            ["Timestamp"] = "2026-04-30T04:00:01.000Z",
            ["SignatureVersion"] = "2",
            ["SigningCertURL"] = signingCertUrl
        };
        values["Signature"] = signatureOverride ?? Convert.ToBase64String(rsa.SignData(
            Encoding.UTF8.GetBytes(CreateSnsNotificationStringToSign(values)),
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1));
        return JsonSerializer.Serialize(values);
    }

    private static string CreateSignedSnsSubscriptionConfirmationPayload(
        RSA rsa,
        string snsMessageId,
        string token,
        string subscribeUrl,
        string? signatureOverride = null,
        string signingCertUrl = "https://sns.us-east-1.amazonaws.com/SimpleNotificationService-test.pem")
    {
        return CreateSignedSnsLifecycleConfirmationPayload(
            rsa,
            messageType: "SubscriptionConfirmation",
            message: "You have chosen to subscribe to the topic.",
            snsMessageId: snsMessageId,
            token: token,
            subscribeUrl: subscribeUrl,
            signatureOverride: signatureOverride,
            signingCertUrl: signingCertUrl);
    }

    private static string CreateSignedSnsUnsubscribeConfirmationPayload(
        RSA rsa,
        string snsMessageId,
        string token,
        string subscribeUrl,
        string? signatureOverride = null,
        string signingCertUrl = "https://sns.us-east-1.amazonaws.com/SimpleNotificationService-test.pem")
    {
        return CreateSignedSnsLifecycleConfirmationPayload(
            rsa,
            messageType: "UnsubscribeConfirmation",
            message: "You have chosen to deactivate a subscription. To restore the subscription, visit the SubscribeURL included in this message.",
            snsMessageId: snsMessageId,
            token: token,
            subscribeUrl: subscribeUrl,
            signatureOverride: signatureOverride,
            signingCertUrl: signingCertUrl);
    }

    private static string CreateSignedSnsLifecycleConfirmationPayload(
        RSA rsa,
        string messageType,
        string message,
        string snsMessageId,
        string token,
        string subscribeUrl,
        string? signatureOverride,
        string signingCertUrl)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Type"] = messageType,
            ["MessageId"] = snsMessageId,
            ["TopicArn"] = SnsTopicArn,
            ["Message"] = message,
            ["SubscribeURL"] = subscribeUrl,
            ["Timestamp"] = "2026-04-30T04:20:00.000Z",
            ["Token"] = token,
            ["SignatureVersion"] = "2",
            ["SigningCertURL"] = signingCertUrl
        };
        values["Signature"] = signatureOverride ?? Convert.ToBase64String(rsa.SignData(
            Encoding.UTF8.GetBytes(CreateSnsLifecycleConfirmationStringToSign(values)),
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1));
        return JsonSerializer.Serialize(values);
    }

    private static string CreateSnsNotificationStringToSign(Dictionary<string, string> values) =>
        string.Join(
            "\n",
            "Message",
            values["Message"],
            "MessageId",
            values["MessageId"],
            "Subject",
            values["Subject"],
            "Timestamp",
            values["Timestamp"],
            "TopicArn",
            values["TopicArn"],
            "Type",
            values["Type"]);

    private static string CreateSnsLifecycleConfirmationStringToSign(Dictionary<string, string> values) =>
        string.Join(
            "\n",
            "Message",
            values["Message"],
            "MessageId",
            values["MessageId"],
            "SubscribeURL",
            values["SubscribeURL"],
            "Timestamp",
            values["Timestamp"],
            "Token",
            values["Token"],
            "TopicArn",
            values["TopicArn"],
            "Type",
            values["Type"]);

    private sealed class CapturingSnsSubscriptionConfirmationClient : IAmazonSesSnsSubscriptionConfirmationClient
    {
        public List<AmazonSesSnsSubscriptionConfirmationRequest> Requests { get; } = [];

        public ValueTask<AmazonSesSnsSubscriptionConfirmationResult> ConfirmAsync(
            AmazonSesSnsSubscriptionConfirmationRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Requests.Add(request);
            return ValueTask.FromResult(AmazonSesSnsSubscriptionConfirmationResult.Confirmed(200));
        }
    }
}
