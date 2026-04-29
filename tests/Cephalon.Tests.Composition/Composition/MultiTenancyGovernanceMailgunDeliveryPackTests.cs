using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Diagnostics;
using Cephalon.MultiTenancy.Governance.MailgunDelivery.Hosting;
using Cephalon.MultiTenancy.Governance.MailgunDelivery.Services;
using Cephalon.MultiTenancy.Governance.Registration;
using Cephalon.MultiTenancy.Governance.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace Cephalon.Tests.Composition;

public sealed class MultiTenancyGovernanceMailgunDeliveryPackTests
{
    [Fact]
    public async Task MailgunInvitationDeliverySenderPostsMessagesPayloadAndRecordsSafeMetadata()
    {
        var capturedRequests = new List<CapturedRequest>();
        var handler = new CapturingHttpMessageHandler(async request =>
        {
            capturedRequests.Add(await CapturedRequest.FromAsync(request));

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"id":"<mailgun-message-299@example.test>","message":"Queued. Thank you."}""")
            };
        });

        var services = new ServiceCollection();
        services.AddCephalonMailgunInvitationDelivery(options =>
        {
            options.BaseUrl = "https://api.mailgun.example.test";
            options.DomainName = "mg.example.test";
            options.ApiKey = "mailgun-secret-299";
            options.FromEmail = "noreply@example.test";
            options.FromName = "Cephalon Mailgun";
            options.SubjectTemplate = "Invite {displayName} to {tenantId}";
            options.TextBodyTemplate = "Invitation {invitationId} for {inviteeId}; roles={roles}; correlation={correlationId}";
            options.HtmlBodyTemplate = "<p>Invitation {invitationId} for <strong>{tenantId}</strong>.</p>";
            options.Tags = ["cephalon-invitation", "governance"];
            options.Variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["product"] = "cephalon-test"
            };
            options.Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["X-Product"] = "Cephalon",
                ["Subject"] = "must-not-override",
                ["X-Unsafe"] = "bad\r\nheader"
            };
            options.EnableTestMode = true;
        });
        services.AddHttpClient(MailgunInvitationDeliveryServiceCollectionExtensions.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => handler);
        services.AddCephalon(engine =>
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
                    invitationId: "invite-mailgun",
                    tenantId: "tenant-mailgun",
                    inviteeId: "owner@example.test",
                    inviteeKind: "email",
                    displayName: "Mailgun Invitee",
                    roles: ["admin", "member"],
                    expiresAtUtc: new DateTimeOffset(2026, 05, 01, 0, 0, 0, TimeSpan.Zero)));
            });
        });

        await using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<ITenantInvitationDeliveryDispatcher>();
        var catalog = provider.GetRequiredService<ITenantInvitationCatalog>();
        var diagnosticsCatalog = provider.GetRequiredService<IRuntimeDiagnosticsCatalog>();

        var result = await dispatcher.DispatchAsync(new TenantInvitationDeliveryRequest(
            tenantId: "tenant-mailgun",
            invitationId: "invite-mailgun",
            channel: "email",
            senderId: "mailgun-email",
            source: "mailgun-delivery-test",
            actor: "operator-299",
            atUtc: new DateTimeOffset(2026, 04, 30, 10, 0, 0, TimeSpan.Zero),
            correlationId: "corr-mailgun-299"));

        var captured = Assert.Single(capturedRequests);
        var invitation = Assert.Single(catalog.Invitations);

        Assert.True(result.Dispatched);
        Assert.True(result.Recorded);
        Assert.Equal(TenantInvitationDeliveryOutcomes.Dispatched, result.Outcome);
        Assert.Equal("<mailgun-message-299@example.test>", result.ProviderMessageId);
        Assert.Equal(HttpMethod.Post, captured.Method);
        Assert.Equal("https://api.mailgun.example.test/v3/mg.example.test/messages", captured.RequestUri);
        Assert.Equal(
            "Basic " + Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes("api:mailgun-secret-299")),
            captured.Headers["Authorization"]);
        Assert.Contains("multipart/form-data", captured.Headers["Content-Type"], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Cephalon Mailgun <noreply@example.test>", captured.Body, StringComparison.Ordinal);
        Assert.Contains("Mailgun Invitee <owner@example.test>", captured.Body, StringComparison.Ordinal);
        Assert.Contains("Invite Mailgun Invitee to tenant-mailgun", captured.Body, StringComparison.Ordinal);
        Assert.Contains("Invitation invite-mailgun for owner@example.test", captured.Body, StringComparison.Ordinal);
        Assert.Contains("<p>Invitation invite-mailgun for <strong>tenant-mailgun</strong>.</p>", captured.Body, StringComparison.Ordinal);
        Assert.Contains("cephalon-invitation", captured.Body, StringComparison.Ordinal);
        Assert.Contains("governance", captured.Body, StringComparison.Ordinal);
        Assert.Contains("o:testmode", captured.Body, StringComparison.Ordinal);
        Assert.Contains("v:cephalonTenantId", captured.Body, StringComparison.Ordinal);
        Assert.Contains("v:product", captured.Body, StringComparison.Ordinal);
        Assert.Contains("h:X-Cephalon-Tenant-Id", captured.Body, StringComparison.Ordinal);
        Assert.Contains("h:X-Product", captured.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("h:Subject", captured.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("h:X-Unsafe", captured.Body, StringComparison.Ordinal);
        Assert.Equal("200", result.Metadata["mailgunStatusCode"]);
        Assert.Equal("api.mailgun.example.test", result.Metadata["mailgunEndpointHost"]);
        Assert.Equal("mg.example.test", result.Metadata["mailgunDomain"]);
        Assert.Equal("true", result.Metadata["mailgunTestMode"]);
        Assert.Equal("2", result.Metadata["mailgunTagCount"]);
        Assert.Equal("<mailgun-message-299@example.test>", result.Metadata["mailgunMessageId"]);
        Assert.Equal("<mailgun-message-299@example.test>", invitation.Metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryProviderMessageId]);
        Assert.DoesNotContain("mailgun-secret-299", result.Metadata.Values);
        Assert.DoesNotContain("mailgun-secret-299", captured.Body, StringComparison.Ordinal);
        Assert.Contains(
            diagnosticsCatalog.Conventions,
            convention => convention.Source == "Cephalon.MultiTenancy.Governance.MailgunDelivery");
    }

    [Fact]
    public async Task MailgunInvitationDeliverySenderUsesRequestMetadataRecipientWhenInviteeIsNotEmail()
    {
        var client = new RecordingMailgunInvitationDeliveryClient(new MailgunInvitationDeliveryClientResult(accepted: true));
        var services = new ServiceCollection();
        services.AddSingleton<IMailgunInvitationDeliveryClient>(client);
        services.AddCephalonMailgunInvitationDelivery(options =>
        {
            options.DomainName = "mg.example.test";
            options.ApiKey = "mailgun-secret-299";
            options.FromEmail = "noreply@example.test";
            options.RecipientEmailMetadataKey = "recipientEmail";
        });
        services.AddCephalon(engine =>
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
                    invitationId: "invite-mailgun-metadata",
                    tenantId: "tenant-mailgun",
                    inviteeId: "user-299",
                    inviteeKind: "user",
                    displayName: "Metadata Recipient",
                    expiresAtUtc: new DateTimeOffset(2026, 05, 01, 0, 0, 0, TimeSpan.Zero)));
            });
        });

        await using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<ITenantInvitationDeliveryDispatcher>();

        var result = await dispatcher.DispatchAsync(new TenantInvitationDeliveryRequest(
            tenantId: "tenant-mailgun",
            invitationId: "invite-mailgun-metadata",
            channel: "email",
            senderId: "mailgun-email",
            atUtc: new DateTimeOffset(2026, 04, 30, 10, 30, 0, TimeSpan.Zero),
            metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["recipientEmail"] = "metadata-recipient@example.test"
            }));

        var captured = Assert.Single(client.Messages);

        Assert.True(result.Dispatched);
        Assert.Equal("metadata-recipient@example.test", captured.ToEmail);
        Assert.Equal("recipientEmail", result.Metadata["mailgunRecipientMetadataKey"]);
    }

    [Fact]
    public async Task MailgunInvitationDeliverySenderReportsFailureStatusWithoutLeakingApiKey()
    {
        var handler = new CapturingHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                ReasonPhrase = "Unauthorized",
                Content = new StringContent("""{"message":"Forbidden"}""")
            }));

        var services = new ServiceCollection();
        services.AddCephalonMailgunInvitationDelivery(options =>
        {
            options.DomainName = "mg.example.test";
            options.ApiKey = "mailgun-secret-299";
            options.FromEmail = "noreply@example.test";
        });
        services.AddHttpClient(MailgunInvitationDeliveryServiceCollectionExtensions.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => handler);
        services.AddCephalon(engine =>
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
                    invitationId: "invite-mailgun-failed",
                    tenantId: "tenant-mailgun",
                    inviteeId: "owner@example.test",
                    inviteeKind: "email",
                    expiresAtUtc: new DateTimeOffset(2026, 05, 01, 0, 0, 0, TimeSpan.Zero)));
            });
        });

        await using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<ITenantInvitationDeliveryDispatcher>();

        var result = await dispatcher.DispatchAsync(new TenantInvitationDeliveryRequest(
            tenantId: "tenant-mailgun",
            invitationId: "invite-mailgun-failed",
            channel: "email",
            senderId: "mailgun-email",
            atUtc: new DateTimeOffset(2026, 04, 30, 11, 30, 0, TimeSpan.Zero)));

        Assert.False(result.Dispatched);
        Assert.True(result.Recorded);
        Assert.Equal(TenantInvitationDeliveryOutcomes.SenderFailed, result.Outcome);
        Assert.Equal("401", result.Metadata["mailgunStatusCode"]);
        Assert.Equal("Unauthorized", result.Metadata["mailgunReasonPhrase"]);
        Assert.Equal("Forbidden", result.Metadata["mailgunResponseMessage"]);
        Assert.DoesNotContain("mailgun-secret-299", result.Metadata.Values);
    }

    [Fact]
    public async Task MailgunInvitationDeliverySenderSuppressesUnsupportedChannelWithoutHttpCall()
    {
        var handler = new CapturingHttpMessageHandler(_ => throw new InvalidOperationException("Unexpected Mailgun dispatch."));
        var services = new ServiceCollection();
        services.AddCephalonMailgunInvitationDelivery(options =>
        {
            options.DomainName = "mg.example.test";
            options.ApiKey = "mailgun-secret-299";
            options.FromEmail = "noreply@example.test";
            options.SupportedChannels = ["email"];
        });
        services.AddHttpClient(MailgunInvitationDeliveryServiceCollectionExtensions.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => handler);
        services.AddCephalon(engine =>
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
                    invitationId: "invite-mailgun-suppressed",
                    tenantId: "tenant-mailgun",
                    inviteeId: "owner@example.test",
                    inviteeKind: "email",
                    expiresAtUtc: new DateTimeOffset(2026, 05, 01, 0, 0, 0, TimeSpan.Zero)));
            });
        });

        await using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<ITenantInvitationDeliveryDispatcher>();
        var catalog = provider.GetRequiredService<ITenantInvitationCatalog>();

        var result = await dispatcher.DispatchAsync(new TenantInvitationDeliveryRequest(
            tenantId: "tenant-mailgun",
            invitationId: "invite-mailgun-suppressed",
            channel: "sms",
            senderId: "mailgun-email",
            atUtc: new DateTimeOffset(2026, 04, 30, 11, 45, 0, TimeSpan.Zero)));

        var invitation = Assert.Single(catalog.Invitations);

        Assert.False(result.Dispatched);
        Assert.True(result.Recorded);
        Assert.Equal(TenantInvitationDeliveryOutcomes.Suppressed, result.Outcome);
        Assert.Equal(TenantInvitationDeliveryOutcomes.Suppressed, invitation.Metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryOutcome]);
        Assert.Empty(handler.Requests);
    }

    private sealed class RecordingMailgunInvitationDeliveryClient(MailgunInvitationDeliveryClientResult result) : IMailgunInvitationDeliveryClient
    {
        public List<MailgunInvitationDeliveryMessage> Messages { get; } = [];

        public ValueTask<MailgunInvitationDeliveryClientResult> SendAsync(
            MailgunInvitationDeliveryMessage message,
            CancellationToken cancellationToken = default)
        {
            Messages.Add(message);
            return ValueTask.FromResult(result);
        }
    }

    private sealed class CapturingHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return await handler(request).ConfigureAwait(false);
        }
    }

    private sealed record CapturedRequest(
        HttpMethod Method,
        string RequestUri,
        string Body,
        IReadOnlyDictionary<string, string> Headers)
    {
        public static async Task<CapturedRequest> FromAsync(HttpRequestMessage request)
        {
            var contentHeaders = request.Content is null
                ? Enumerable.Empty<KeyValuePair<string, IEnumerable<string>>>()
                : request.Content.Headers;

            var headers = request.Headers
                .Concat(contentHeaders)
                .ToDictionary(
                    static pair => pair.Key,
                    static pair => string.Join(",", pair.Value),
                    StringComparer.OrdinalIgnoreCase);

            return new CapturedRequest(
                request.Method,
                request.RequestUri?.ToString() ?? string.Empty,
                request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync().ConfigureAwait(false),
                headers);
        }
    }
}
