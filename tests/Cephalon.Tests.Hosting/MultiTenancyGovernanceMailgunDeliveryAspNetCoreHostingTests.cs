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
using System.Security.Cryptography;
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
        Assert.False(result.SignedWebhookVerificationRequired);
        Assert.False(result.SignedWebhookVerified);
        Assert.Equal("not-configured", result.SignedWebhookVerificationOutcome);
        Assert.Null(result.SignedWebhookSignatureField);
        Assert.False(result.SignedWebhookReplayProtectionEnabled);
        Assert.Equal("not-configured", result.SignedWebhookReplayProtectionOutcome);
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
        Assert.Equal("false", endpointEntry.Metadata["mailgunWebhookReplayProtectionConfigured"]);
        Assert.Equal("none", endpointEntry.Metadata["mailgunWebhookReplayProtectionPolicy"]);
        Assert.Equal("none", endpointEntry.Metadata["mailgunWebhookReplayProtectionScope"]);
        Assert.Equal("false", endpointEntry.Metadata["mailgunWebhookSignatureVerificationRequired"]);
        Assert.Equal("false", endpointEntry.Metadata["mailgunWebhookSigningKeyConfigured"]);
        Assert.Equal("timestamp+token", endpointEntry.Metadata["mailgunWebhookSignaturePayload"]);
        Assert.Equal("signature.signature", endpointEntry.Metadata["mailgunWebhookSignatureField"]);
        Assert.Equal("signature.parent-signature", endpointEntry.Metadata["mailgunWebhookParentSignatureField"]);
        Assert.Equal("true", endpointEntry.Metadata["mailgunWebhookParentSignatureAccepted"]);
        Assert.Equal("300", endpointEntry.Metadata["mailgunWebhookSignatureToleranceSeconds"]);
        Assert.Equal("true", endpointEntry.Metadata["normalizeProviderMessageIdWithAngleBrackets"]);
        Assert.Contains(
            diagnosticsConvention.Events,
            definition => definition.Name == "MailgunInvitationDeliveryStatusCallbackAccepted");
        Assert.Contains(
            diagnosticsConvention.Events,
            definition => definition.Name == "MailgunInvitationDeliveryStatusCallbackSignatureRejected");
        Assert.Contains(
            diagnosticsConvention.Events,
            definition => definition.Name == "MailgunInvitationDeliveryStatusCallbackReplayRejected");
    }

    [Fact]
    public async Task MapCephalonMailgunInvitationDeliveryStatusCallbacksVerifiesSignedWebhookBeforeReconciliation()
    {
        const string signingKey = "mailgun-signing-key-301";
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "Microservice";
        builder.Configuration[$"{EngineSettings.SectionName}:Technologies:0"] = "MultiTenancy";
        builder.Services.AddCephalonMailgunInvitationDeliveryAspNetCore(configure: options =>
        {
            options.RequireStatusCallbackAuthorization = false;
            options.RequireSignedWebhook = true;
            options.WebhookSigningKey = signingKey;
        });
        builder.AddCephalon(engine =>
        {
            engine.UseConfiguration(builder.Configuration);
            engine.AddMultiTenancyGovernance(options =>
            {
                options.Invitations.Add(new TenantInvitationDescriptor(
                    invitationId: "invite-mailgun-signed",
                    tenantId: "tenant-mailgun-signed",
                    inviteeId: "signed@example.test",
                    inviteeKind: "email",
                    displayName: "Mailgun Signed Callback Target",
                    roles: ["member"],
                    expiresAtUtc: new DateTimeOffset(2026, 05, 01, 0, 0, 0, TimeSpan.Zero),
                    metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        [TenantInvitationDeliveryMetadataKeys.LastDeliveryProviderMessageId] = "<mailgun-message-signed@example.test>"
                    }));
            });
        });

        await using var app = builder.Build();
        app.MapCephalonMailgunInvitationDeliveryStatusCallbacks();

        await app.StartAsync();
        var client = app.GetTestClient();
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        var token = "mailgun-token-301-abcdefghijklmnopqrstuvwxyz012345";
        var signature = CreateMailgunSignature(signingKey, timestamp, token);
        var payload = CreateSignedMailgunEnvelopePayload(
            CreateMailgunDeliveredEvent(
                "mailgun-event-signed-301",
                "mailgun-message-signed@example.test",
                "tenant-mailgun-signed",
                "invite-mailgun-signed",
                "corr-mailgun-signed-301"),
            timestamp,
            token,
            signature: "0000000000000000000000000000000000000000000000000000000000000000",
            parentSignature: signature);
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/engine/tenant-invitations/delivery-status/mailgun", content);
        var result = await response.Content.ReadFromJsonAsync<MailgunInvitationDeliveryStatusCallbackResult>(SerializerOptions);
        var invitation = Assert.Single(app.Services.GetRequiredService<ITenantInvitationCatalog>().Invitations);
        var observation = Assert.Single(app.Services.GetRequiredService<ITenantInvitationDeliveryStatusObservationStore>().Observations);
        var technologySurface = Assert.Single(
            app.Services.GetRequiredService<ITechnologyRuntimeCatalog>().GetByTechnology("multi-tenancy"),
            surface => surface.SurfaceId == "tenant-invitation-delivery-mailgun-status-callbacks");
        var endpointEntry = Assert.Single(technologySurface.Entries);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.True(result.SignedWebhookVerificationRequired);
        Assert.True(result.SignedWebhookVerified);
        Assert.Equal("verified", result.SignedWebhookVerificationOutcome);
        Assert.Equal("parent-signature", result.SignedWebhookSignatureField);
        Assert.True(result.SignedWebhookReplayProtectionEnabled);
        Assert.Equal("recorded", result.SignedWebhookReplayProtectionOutcome);
        Assert.Equal(1, result.ReconciledEvents);
        Assert.Equal(TenantInvitationDeliveryStatuses.Delivered, invitation.Metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryStatus]);
        Assert.Equal("verified", invitation.Metadata["mailgunWebhookSignatureVerification"]);
        Assert.Equal("cephalon-managed", invitation.Metadata["mailgunWebhookSignatureVerificationOwnership"]);
        Assert.Equal("hmac-sha256", invitation.Metadata["mailgunWebhookSignatureAlgorithm"]);
        Assert.Equal("timestamp+token", invitation.Metadata["mailgunWebhookSignaturePayload"]);
        Assert.Equal(timestamp, invitation.Metadata["mailgunWebhookSignatureTimestamp"]);
        Assert.Equal("parent-signature", invitation.Metadata["mailgunWebhookSignatureField"]);
        Assert.Equal("true", invitation.Metadata["mailgunWebhookParentSignatureAccepted"]);
        Assert.Equal(
            "sha256:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(signature))).ToLowerInvariant(),
            invitation.Metadata["mailgunWebhookSignatureFingerprint"]);
        Assert.Equal("recorded", invitation.Metadata["mailgunWebhookReplayProtection"]);
        Assert.Equal("cephalon-managed", invitation.Metadata["mailgunWebhookReplayProtectionOwnership"]);
        Assert.Equal("signed-webhook-token", invitation.Metadata["mailgunWebhookReplayProtectionPolicy"]);
        Assert.Equal("token-fingerprint", invitation.Metadata["mailgunWebhookReplayProtectionKey"]);
        Assert.Equal("process-local", invitation.Metadata["mailgunWebhookReplayProtectionScope"]);
        Assert.Equal("none", invitation.Metadata["mailgunWebhookReplayProtectionDurability"]);
        Assert.Equal("300", invitation.Metadata["mailgunWebhookReplayProtectionRetentionSeconds"]);
        Assert.Equal("4096", invitation.Metadata["mailgunWebhookReplayProtectionCacheLimit"]);
        Assert.Equal(CreateSha256Fingerprint(token), invitation.Metadata["mailgunWebhookReplayProtectionFingerprint"]);
        Assert.Equal(invitation.Metadata[TenantInvitationDeliveryMetadataKeys.DeliveryStatusObservationId], observation.ObservationId);
        Assert.Equal("cephalon-managed", endpointEntry.Metadata["mailgunWebhookSignatureVerificationOwnership"]);
        Assert.Equal("true", endpointEntry.Metadata["mailgunWebhookSignatureVerificationRequired"]);
        Assert.Equal("true", endpointEntry.Metadata["mailgunWebhookSigningKeyConfigured"]);
        Assert.Equal("true", endpointEntry.Metadata["mailgunWebhookParentSignatureAccepted"]);
        Assert.Equal("true", endpointEntry.Metadata["mailgunWebhookReplayProtectionConfigured"]);
        Assert.Equal("cephalon-managed", endpointEntry.Metadata["mailgunWebhookReplayProtectionOwnership"]);
        Assert.Equal("signed-webhook-token", endpointEntry.Metadata["mailgunWebhookReplayProtectionPolicy"]);
        Assert.Equal("token-fingerprint", endpointEntry.Metadata["mailgunWebhookReplayProtectionKey"]);
        Assert.Equal("process-local", endpointEntry.Metadata["mailgunWebhookReplayProtectionScope"]);
        Assert.Equal("none", endpointEntry.Metadata["mailgunWebhookReplayProtectionDurability"]);
        Assert.Equal("300", endpointEntry.Metadata["mailgunWebhookReplayProtectionRetentionSeconds"]);
        Assert.Equal("4096", endpointEntry.Metadata["mailgunWebhookReplayProtectionCacheLimit"]);
    }

    [Fact]
    public async Task MapCephalonMailgunInvitationDeliveryStatusCallbacksRejectsSignedWebhookReplayBeforeReconciliation()
    {
        const string signingKey = "mailgun-signing-key-replay-302";
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "Microservice";
        builder.Configuration[$"{EngineSettings.SectionName}:Technologies:0"] = "MultiTenancy";
        builder.Services.AddCephalonMailgunInvitationDeliveryAspNetCore(configure: options =>
        {
            options.RequireStatusCallbackAuthorization = false;
            options.RequireSignedWebhook = true;
            options.WebhookSigningKey = signingKey;
        });
        builder.AddCephalon(engine =>
        {
            engine.UseConfiguration(builder.Configuration);
            engine.AddMultiTenancyGovernance(options =>
            {
                options.Invitations.Add(new TenantInvitationDescriptor(
                    invitationId: "invite-mailgun-replay",
                    tenantId: "tenant-mailgun-replay",
                    inviteeId: "replay@example.test",
                    inviteeKind: "email",
                    displayName: "Mailgun Replay Callback Target",
                    roles: ["member"],
                    expiresAtUtc: new DateTimeOffset(2026, 05, 01, 0, 0, 0, TimeSpan.Zero),
                    metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        [TenantInvitationDeliveryMetadataKeys.LastDeliveryProviderMessageId] = "<mailgun-message-replay@example.test>"
                    }));
            });
        });

        await using var app = builder.Build();
        app.MapCephalonMailgunInvitationDeliveryStatusCallbacks();

        await app.StartAsync();
        var client = app.GetTestClient();
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        var token = "mailgun-token-replay-302-abcdefghijklmnopqrstuvwxyz";
        var signature = CreateMailgunSignature(signingKey, timestamp, token);
        var payload = CreateSignedMailgunEnvelopePayload(
            CreateMailgunDeliveredEvent(
                "mailgun-event-replay-302",
                "mailgun-message-replay@example.test",
                "tenant-mailgun-replay",
                "invite-mailgun-replay",
                "corr-mailgun-replay-302"),
            timestamp,
            token,
            signature);

        var firstResponse = await client.PostAsync(
            "/engine/tenant-invitations/delivery-status/mailgun",
            new StringContent(payload, Encoding.UTF8, "application/json"));
        var secondResponse = await client.PostAsync(
            "/engine/tenant-invitations/delivery-status/mailgun",
            new StringContent(payload, Encoding.UTF8, "application/json"));

        var firstResult = await firstResponse.Content.ReadFromJsonAsync<MailgunInvitationDeliveryStatusCallbackResult>(SerializerOptions);
        var invitation = Assert.Single(app.Services.GetRequiredService<ITenantInvitationCatalog>().Invitations);
        var observations = app.Services.GetRequiredService<ITenantInvitationDeliveryStatusObservationStore>().Observations;
        var technologySurface = Assert.Single(
            app.Services.GetRequiredService<ITechnologyRuntimeCatalog>().GetByTechnology("multi-tenancy"),
            surface => surface.SurfaceId == "tenant-invitation-delivery-mailgun-status-callbacks");
        var endpointEntry = Assert.Single(technologySurface.Entries);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.NotNull(firstResult);
        Assert.True(firstResult.SignedWebhookReplayProtectionEnabled);
        Assert.Equal("recorded", firstResult.SignedWebhookReplayProtectionOutcome);
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
        Assert.Single(observations);
        Assert.Equal("recorded", invitation.Metadata["mailgunWebhookReplayProtection"]);
        Assert.Equal("cephalon-managed", invitation.Metadata["mailgunWebhookReplayProtectionOwnership"]);
        Assert.Equal(CreateSha256Fingerprint(token), invitation.Metadata["mailgunWebhookReplayProtectionFingerprint"]);
        Assert.Equal("true", endpointEntry.Metadata["mailgunWebhookReplayProtectionConfigured"]);
        Assert.Equal("cephalon-managed", endpointEntry.Metadata["mailgunWebhookReplayProtectionOwnership"]);
        Assert.Equal("process-local", endpointEntry.Metadata["mailgunWebhookReplayProtectionScope"]);
        Assert.Equal("none", endpointEntry.Metadata["mailgunWebhookReplayProtectionDurability"]);
    }

    [Fact]
    public async Task MapCephalonMailgunInvitationDeliveryStatusCallbacksRejectsInvalidSignedWebhookBeforeReconciliation()
    {
        const string signingKey = "mailgun-signing-key-invalid-301";
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{EngineSettings.SectionName}:Blueprint"] = "Microservice";
        builder.Configuration[$"{EngineSettings.SectionName}:Technologies:0"] = "MultiTenancy";
        builder.Services.AddCephalonMailgunInvitationDeliveryAspNetCore(configure: options =>
        {
            options.RequireStatusCallbackAuthorization = false;
            options.RequireSignedWebhook = true;
            options.WebhookSigningKey = signingKey;
        });
        builder.AddCephalon(engine =>
        {
            engine.UseConfiguration(builder.Configuration);
            engine.AddMultiTenancyGovernance(options =>
            {
                options.Invitations.Add(new TenantInvitationDescriptor(
                    invitationId: "invite-mailgun-invalid-signature",
                    tenantId: "tenant-mailgun-invalid-signature",
                    inviteeId: "invalid@example.test",
                    inviteeKind: "email",
                    displayName: "Mailgun Invalid Signature Target",
                    roles: ["member"],
                    expiresAtUtc: new DateTimeOffset(2026, 05, 01, 0, 0, 0, TimeSpan.Zero),
                    metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        [TenantInvitationDeliveryMetadataKeys.LastDeliveryProviderMessageId] = "<mailgun-message-invalid-signature@example.test>"
                    }));
            });
        });

        await using var app = builder.Build();
        app.MapCephalonMailgunInvitationDeliveryStatusCallbacks();

        await app.StartAsync();
        var client = app.GetTestClient();
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        var token = "mailgun-token-invalid-301-abcdefghijklmnopqrstuv";
        var payload = CreateSignedMailgunEnvelopePayload(
            CreateMailgunDeliveredEvent(
                "mailgun-event-invalid-signature-301",
                "mailgun-message-invalid-signature@example.test",
                "tenant-mailgun-invalid-signature",
                "invite-mailgun-invalid-signature",
                "corr-mailgun-invalid-signature-301"),
            timestamp,
            token,
            signature: "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff");
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/engine/tenant-invitations/delivery-status/mailgun", content);
        var invitation = Assert.Single(app.Services.GetRequiredService<ITenantInvitationCatalog>().Invitations);
        var observations = app.Services.GetRequiredService<ITenantInvitationDeliveryStatusObservationStore>().Observations;

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(observations);
        Assert.False(invitation.Metadata.ContainsKey(TenantInvitationDeliveryMetadataKeys.LastDeliveryStatus));
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

    private static Dictionary<string, object?> CreateMailgunDeliveredEvent(
        string eventId,
        string messageId,
        string tenantId,
        string invitationId,
        string correlationId)
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["event"] = "delivered",
            ["id"] = eventId,
            ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ["message"] = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["headers"] = new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["message-id"] = messageId
                }
            },
            ["delivery-status"] = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["code"] = 250,
                ["message"] = "OK"
            },
            ["user-variables"] = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["cephalonTenantId"] = tenantId,
                ["cephalonInvitationId"] = invitationId,
                ["cephalonDeliveryChannel"] = "email",
                ["cephalonSenderId"] = "mailgun-email",
                ["cephalonCorrelationId"] = correlationId
            }
        };
    }

    private static string CreateSignedMailgunEnvelopePayload(
        Dictionary<string, object?> eventData,
        string timestamp,
        string token,
        string signature,
        string? parentSignature = null)
    {
        var signaturePayload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["token"] = token,
            ["timestamp"] = timestamp,
            ["signature"] = signature
        };
        if (!string.IsNullOrWhiteSpace(parentSignature))
        {
            signaturePayload["parent-signature"] = parentSignature;
        }

        return JsonSerializer.Serialize(
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["signature"] = signaturePayload,
                ["event-data"] = eventData
            },
            SerializerOptions);
    }

    private static string CreateMailgunSignature(string signingKey, string timestamp, string token)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(signingKey));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(timestamp + token))).ToLowerInvariant();
    }

    private static string CreateSha256Fingerprint(string value) =>
        "sha256:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim()))).ToLowerInvariant();
}
