using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Diagnostics;
using Cephalon.MultiTenancy.Governance.Registration;
using Cephalon.MultiTenancy.Governance.SendGridDelivery.Hosting;
using Cephalon.MultiTenancy.Governance.SendGridDelivery.Services;
using Cephalon.MultiTenancy.Governance.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;

namespace Cephalon.Tests.Composition;

public sealed class MultiTenancyGovernanceSendGridDeliveryPackTests
{
    [Fact]
    public async Task SendGridInvitationDeliverySenderPostsMailSendPayloadAndRecordsSafeMetadata()
    {
        var capturedRequests = new List<CapturedRequest>();
        var handler = new CapturingHttpMessageHandler(async request =>
        {
            capturedRequests.Add(await CapturedRequest.FromAsync(request));

            var response = new HttpResponseMessage(HttpStatusCode.Accepted);
            response.Headers.Add("X-Message-ID", "sendgrid-message-294");

            return response;
        });

        var services = new ServiceCollection();
        services.AddCephalonSendGridInvitationDelivery(options =>
        {
            options.BaseUrl = "https://api.sendgrid.example.test";
            options.ApiKey = "SG.secret-294";
            options.FromEmail = "noreply@example.test";
            options.FromName = "Cephalon SendGrid";
            options.SubjectTemplate = "Invite {displayName} to {tenantId}";
            options.TextBodyTemplate = "Invitation {invitationId} for {inviteeId}; roles={roles}; correlation={correlationId}";
            options.HtmlBodyTemplate = "<p>Invitation {invitationId} for <strong>{tenantId}</strong>.</p>";
            options.Categories = ["cephalon-invitation", "governance"];
            options.CustomArgs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["product"] = "cephalon-test"
            };
            options.Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["X-Product"] = "Cephalon",
                ["Subject"] = "must-not-override",
                ["X-Unsafe"] = "bad\r\nheader"
            };
        });
        services.AddHttpClient(SendGridInvitationDeliveryServiceCollectionExtensions.HttpClientName)
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
                    invitationId: "invite-sendgrid",
                    tenantId: "tenant-sendgrid",
                    inviteeId: "owner@example.test",
                    inviteeKind: "email",
                    displayName: "SendGrid Invitee",
                    roles: ["admin", "member"],
                    expiresAtUtc: new DateTimeOffset(2026, 05, 01, 0, 0, 0, TimeSpan.Zero)));
            });
        });

        await using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<ITenantInvitationDeliveryDispatcher>();
        var catalog = provider.GetRequiredService<ITenantInvitationCatalog>();
        var diagnosticsCatalog = provider.GetRequiredService<IRuntimeDiagnosticsCatalog>();

        var result = await dispatcher.DispatchAsync(new TenantInvitationDeliveryRequest(
            tenantId: "tenant-sendgrid",
            invitationId: "invite-sendgrid",
            channel: "email",
            senderId: "sendgrid-email",
            source: "sendgrid-delivery-test",
            actor: "operator-294",
            atUtc: new DateTimeOffset(2026, 04, 29, 10, 0, 0, TimeSpan.Zero),
            correlationId: "corr-sendgrid-294"));

        var captured = Assert.Single(capturedRequests);
        using var payload = JsonDocument.Parse(captured.Body);
        var root = payload.RootElement;
        var personalization = root.GetProperty("personalizations")[0];
        var customArgs = personalization.GetProperty("custom_args");
        var headers = root.GetProperty("headers");
        var invitation = Assert.Single(catalog.Invitations);

        Assert.True(result.Dispatched);
        Assert.True(result.Recorded);
        Assert.Equal(TenantInvitationDeliveryOutcomes.Dispatched, result.Outcome);
        Assert.Equal("sendgrid-message-294", result.ProviderMessageId);
        Assert.Equal(HttpMethod.Post, captured.Method);
        Assert.Equal("https://api.sendgrid.example.test/v3/mail/send", captured.RequestUri);
        Assert.Equal("Bearer SG.secret-294", captured.Headers["Authorization"]);
        Assert.Equal("owner@example.test", personalization.GetProperty("to")[0].GetProperty("email").GetString());
        Assert.Equal("SendGrid Invitee", personalization.GetProperty("to")[0].GetProperty("name").GetString());
        Assert.Equal("noreply@example.test", root.GetProperty("from").GetProperty("email").GetString());
        Assert.Equal("Cephalon SendGrid", root.GetProperty("from").GetProperty("name").GetString());
        Assert.Equal("Invite SendGrid Invitee to tenant-sendgrid", root.GetProperty("subject").GetString());
        Assert.Equal("text/plain", root.GetProperty("content")[0].GetProperty("type").GetString());
        Assert.Contains("Invitation invite-sendgrid for owner@example.test", root.GetProperty("content")[0].GetProperty("value").GetString(), StringComparison.Ordinal);
        Assert.Equal("text/html", root.GetProperty("content")[1].GetProperty("type").GetString());
        Assert.Equal("cephalon-invitation", root.GetProperty("categories")[0].GetString());
        Assert.Equal("tenant-sendgrid", customArgs.GetProperty("cephalonTenantId").GetString());
        Assert.Equal("invite-sendgrid", customArgs.GetProperty("cephalonInvitationId").GetString());
        Assert.Equal("corr-sendgrid-294", customArgs.GetProperty("cephalonCorrelationId").GetString());
        Assert.Equal("cephalon-test", customArgs.GetProperty("product").GetString());
        Assert.StartsWith("cephalon-invitation-", customArgs.GetProperty("cephalonMessageId").GetString(), StringComparison.Ordinal);
        Assert.Equal("tenant-sendgrid", headers.GetProperty("X-Cephalon-Tenant-Id").GetString());
        Assert.Equal("invite-sendgrid", headers.GetProperty("X-Cephalon-Invitation-Id").GetString());
        Assert.Equal("Cephalon", headers.GetProperty("X-Product").GetString());
        Assert.False(headers.TryGetProperty("Subject", out _));
        Assert.False(headers.TryGetProperty("X-Unsafe", out _));
        Assert.Equal("202", result.Metadata["sendGridStatusCode"]);
        Assert.Equal("api.sendgrid.example.test", result.Metadata["sendGridEndpointHost"]);
        Assert.Equal("false", result.Metadata["sendGridSandboxMode"]);
        Assert.Equal("2", result.Metadata["sendGridCategoryCount"]);
        Assert.Equal("sendgrid-message-294", result.Metadata["sendGridMessageId"]);
        Assert.Equal("sendgrid-message-294", invitation.Metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryProviderMessageId]);
        Assert.DoesNotContain("SG.secret-294", result.Metadata.Values);
        Assert.DoesNotContain("SG.secret-294", captured.Body, StringComparison.Ordinal);
        Assert.Contains(
            diagnosticsCatalog.Conventions,
            convention => convention.Source == "Cephalon.MultiTenancy.Governance.SendGridDelivery");
    }

    [Fact]
    public async Task SendGridInvitationDeliverySenderUsesRequestMetadataRecipientWhenInviteeIsNotEmail()
    {
        var client = new RecordingSendGridInvitationDeliveryClient(new SendGridInvitationDeliveryClientResult(accepted: true));
        var services = new ServiceCollection();
        services.AddSingleton<ISendGridInvitationDeliveryClient>(client);
        services.AddCephalonSendGridInvitationDelivery(options =>
        {
            options.ApiKey = "SG.secret-294";
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
                    invitationId: "invite-sendgrid-metadata",
                    tenantId: "tenant-sendgrid",
                    inviteeId: "user-294",
                    inviteeKind: "user",
                    displayName: "Metadata Recipient",
                    expiresAtUtc: new DateTimeOffset(2026, 05, 01, 0, 0, 0, TimeSpan.Zero)));
            });
        });

        await using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<ITenantInvitationDeliveryDispatcher>();

        var result = await dispatcher.DispatchAsync(new TenantInvitationDeliveryRequest(
            tenantId: "tenant-sendgrid",
            invitationId: "invite-sendgrid-metadata",
            channel: "email",
            senderId: "sendgrid-email",
            atUtc: new DateTimeOffset(2026, 04, 29, 10, 30, 0, TimeSpan.Zero),
            metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["recipientEmail"] = "metadata-recipient@example.test"
            }));

        var captured = Assert.Single(client.Messages);

        Assert.True(result.Dispatched);
        Assert.Equal("metadata-recipient@example.test", captured.ToEmail);
        Assert.Equal("recipientEmail", result.Metadata["sendGridRecipientMetadataKey"]);
    }

    [Fact]
    public async Task SendGridInvitationDeliverySenderAcceptsSandboxValidationResponse()
    {
        var capturedRequests = new List<CapturedRequest>();
        var handler = new CapturingHttpMessageHandler(async request =>
        {
            capturedRequests.Add(await CapturedRequest.FromAsync(request));
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var services = new ServiceCollection();
        services.AddCephalonSendGridInvitationDelivery(options =>
        {
            options.ApiKey = "SG.secret-294";
            options.FromEmail = "noreply@example.test";
            options.EnableSandboxMode = true;
        });
        services.AddHttpClient(SendGridInvitationDeliveryServiceCollectionExtensions.HttpClientName)
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
                    invitationId: "invite-sendgrid-sandbox",
                    tenantId: "tenant-sendgrid",
                    inviteeId: "owner@example.test",
                    inviteeKind: "email",
                    expiresAtUtc: new DateTimeOffset(2026, 05, 01, 0, 0, 0, TimeSpan.Zero)));
            });
        });

        await using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<ITenantInvitationDeliveryDispatcher>();

        var result = await dispatcher.DispatchAsync(new TenantInvitationDeliveryRequest(
            tenantId: "tenant-sendgrid",
            invitationId: "invite-sendgrid-sandbox",
            channel: "email",
            senderId: "sendgrid-email",
            atUtc: new DateTimeOffset(2026, 04, 29, 11, 0, 0, TimeSpan.Zero)));

        var captured = Assert.Single(capturedRequests);
        using var payload = JsonDocument.Parse(captured.Body);

        Assert.True(result.Dispatched);
        Assert.Equal("200", result.Metadata["sendGridStatusCode"]);
        Assert.Equal("true", result.Metadata["sendGridSandboxMode"]);
        Assert.True(payload.RootElement
            .GetProperty("mail_settings")
            .GetProperty("sandbox_mode")
            .GetProperty("enable")
            .GetBoolean());
    }

    [Fact]
    public async Task SendGridInvitationDeliverySenderReportsFailureStatusWithoutLeakingApiKey()
    {
        var handler = new CapturingHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                ReasonPhrase = "Bad Request"
            }));

        var services = new ServiceCollection();
        services.AddCephalonSendGridInvitationDelivery(options =>
        {
            options.ApiKey = "SG.secret-294";
            options.FromEmail = "noreply@example.test";
        });
        services.AddHttpClient(SendGridInvitationDeliveryServiceCollectionExtensions.HttpClientName)
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
                    invitationId: "invite-sendgrid-failed",
                    tenantId: "tenant-sendgrid",
                    inviteeId: "owner@example.test",
                    inviteeKind: "email",
                    expiresAtUtc: new DateTimeOffset(2026, 05, 01, 0, 0, 0, TimeSpan.Zero)));
            });
        });

        await using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<ITenantInvitationDeliveryDispatcher>();

        var result = await dispatcher.DispatchAsync(new TenantInvitationDeliveryRequest(
            tenantId: "tenant-sendgrid",
            invitationId: "invite-sendgrid-failed",
            channel: "email",
            senderId: "sendgrid-email",
            atUtc: new DateTimeOffset(2026, 04, 29, 11, 30, 0, TimeSpan.Zero)));

        Assert.False(result.Dispatched);
        Assert.True(result.Recorded);
        Assert.Equal(TenantInvitationDeliveryOutcomes.SenderFailed, result.Outcome);
        Assert.Equal("400", result.Metadata["sendGridStatusCode"]);
        Assert.Equal("Bad Request", result.Metadata["sendGridReasonPhrase"]);
        Assert.DoesNotContain("SG.secret-294", result.Metadata.Values);
    }

    [Fact]
    public async Task SendGridInvitationDeliverySenderSuppressesUnsupportedChannelWithoutHttpCall()
    {
        var handler = new CapturingHttpMessageHandler(_ => throw new InvalidOperationException("Unexpected SendGrid dispatch."));
        var services = new ServiceCollection();
        services.AddCephalonSendGridInvitationDelivery(options =>
        {
            options.ApiKey = "SG.secret-294";
            options.FromEmail = "noreply@example.test";
            options.SupportedChannels = ["email"];
        });
        services.AddHttpClient(SendGridInvitationDeliveryServiceCollectionExtensions.HttpClientName)
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
                    invitationId: "invite-sendgrid-suppressed",
                    tenantId: "tenant-sendgrid",
                    inviteeId: "owner@example.test",
                    inviteeKind: "email",
                    expiresAtUtc: new DateTimeOffset(2026, 05, 01, 0, 0, 0, TimeSpan.Zero)));
            });
        });

        await using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<ITenantInvitationDeliveryDispatcher>();
        var catalog = provider.GetRequiredService<ITenantInvitationCatalog>();

        var result = await dispatcher.DispatchAsync(new TenantInvitationDeliveryRequest(
            tenantId: "tenant-sendgrid",
            invitationId: "invite-sendgrid-suppressed",
            channel: "sms",
            senderId: "sendgrid-email",
            atUtc: new DateTimeOffset(2026, 04, 29, 11, 45, 0, TimeSpan.Zero)));

        var invitation = Assert.Single(catalog.Invitations);

        Assert.False(result.Dispatched);
        Assert.True(result.Recorded);
        Assert.Equal(TenantInvitationDeliveryOutcomes.Suppressed, result.Outcome);
        Assert.Equal(TenantInvitationDeliveryOutcomes.Suppressed, invitation.Metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryOutcome]);
        Assert.Empty(handler.Requests);
    }

    private sealed class RecordingSendGridInvitationDeliveryClient(SendGridInvitationDeliveryClientResult result) : ISendGridInvitationDeliveryClient
    {
        public List<SendGridInvitationDeliveryMessage> Messages { get; } = [];

        public ValueTask<SendGridInvitationDeliveryClientResult> SendAsync(
            SendGridInvitationDeliveryMessage message,
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
