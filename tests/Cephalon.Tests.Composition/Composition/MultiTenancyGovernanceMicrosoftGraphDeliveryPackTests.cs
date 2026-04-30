using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Diagnostics;
using Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.Hosting;
using Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.Services;
using Cephalon.MultiTenancy.Governance.Registration;
using Cephalon.MultiTenancy.Governance.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;

namespace Cephalon.Tests.Composition;

public sealed class MultiTenancyGovernanceMicrosoftGraphDeliveryPackTests
{
    [Fact]
    public async Task MicrosoftGraphInvitationDeliverySenderPostsSendMailPayloadAndRecordsSafeMetadata()
    {
        var capturedRequests = new List<CapturedRequest>();
        var handler = new CapturingHttpMessageHandler(async request =>
        {
            capturedRequests.Add(await CapturedRequest.FromAsync(request));

            var response = new HttpResponseMessage(HttpStatusCode.Accepted);
            response.Headers.Add("request-id", "graph-request-304");
            response.Headers.Add("client-request-id", "graph-client-request-304");
            return response;
        });

        var services = new ServiceCollection();
        services.AddCephalonMicrosoftGraphInvitationDelivery(options =>
        {
            options.BaseUrl = "https://graph.example.test";
            options.ApiVersion = "v1.0";
            options.SenderUserId = "sender@example.test";
            options.AccessToken = "graph-secret-token-304";
            options.SubjectTemplate = "Invite {displayName} to {tenantId}";
            options.TextBodyTemplate = "Invitation {invitationId} for {inviteeId}; roles={roles}; correlation={correlationId}";
            options.HtmlBodyTemplate = "<p>Invitation {invitationId} for <strong>{tenantId}</strong>.</p>";
            options.Categories = ["cephalon-invitation", "governance"];
            options.Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["x-product"] = "Cephalon",
                ["Subject"] = "must-not-override",
                ["x-unsafe"] = "bad\r\nheader"
            };
        });
        services.AddHttpClient(MicrosoftGraphInvitationDeliveryServiceCollectionExtensions.HttpClientName)
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
                    invitationId: "invite-graph",
                    tenantId: "tenant-graph",
                    inviteeId: "owner@example.test",
                    inviteeKind: "email",
                    displayName: "Graph Invitee",
                    roles: ["admin", "member"],
                    expiresAtUtc: new DateTimeOffset(2026, 05, 01, 0, 0, 0, TimeSpan.Zero)));
            });
        });

        await using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<ITenantInvitationDeliveryDispatcher>();
        var catalog = provider.GetRequiredService<ITenantInvitationCatalog>();
        var diagnosticsCatalog = provider.GetRequiredService<IRuntimeDiagnosticsCatalog>();

        var result = await dispatcher.DispatchAsync(new TenantInvitationDeliveryRequest(
            tenantId: "tenant-graph",
            invitationId: "invite-graph",
            channel: "email",
            senderId: "microsoft-graph-email",
            source: "microsoft-graph-delivery-test",
            actor: "operator-304",
            atUtc: new DateTimeOffset(2026, 04, 30, 12, 0, 0, TimeSpan.Zero),
            correlationId: "corr-graph-304"));

        var captured = Assert.Single(capturedRequests);
        var invitation = Assert.Single(catalog.Invitations);
        using var body = JsonDocument.Parse(captured.Body);
        var root = body.RootElement;
        var message = root.GetProperty("message");
        var graphBody = message.GetProperty("body");
        var recipient = message.GetProperty("toRecipients")[0].GetProperty("emailAddress");
        var headers = message.GetProperty("internetMessageHeaders").EnumerateArray().ToArray();

        Assert.True(result.Dispatched);
        Assert.True(result.Recorded);
        Assert.Equal(TenantInvitationDeliveryOutcomes.Dispatched, result.Outcome);
        Assert.Equal(HttpMethod.Post, captured.Method);
        Assert.Equal("https://graph.example.test/v1.0/users/sender%40example.test/sendMail", captured.RequestUri);
        Assert.Equal("Bearer graph-secret-token-304", captured.Headers["Authorization"]);
        Assert.Equal("application/json; charset=utf-8", captured.Headers["Content-Type"]);
        Assert.Equal("Invite Graph Invitee to tenant-graph", message.GetProperty("subject").GetString());
        Assert.Equal("HTML", graphBody.GetProperty("contentType").GetString());
        Assert.Contains("Invitation invite-graph", graphBody.GetProperty("content").GetString(), StringComparison.Ordinal);
        Assert.Equal("owner@example.test", recipient.GetProperty("address").GetString());
        Assert.Equal("Graph Invitee", recipient.GetProperty("name").GetString());
        Assert.False(root.GetProperty("saveToSentItems").GetBoolean());
        Assert.Contains("cephalon-invitation", message.GetProperty("categories").EnumerateArray().Select(static value => value.GetString()));
        Assert.Contains(headers, header => header.GetProperty("name").GetString() == "x-cephalon-tenant-id");
        Assert.Contains(headers, header => header.GetProperty("name").GetString() == "x-product");
        Assert.DoesNotContain(headers, header => string.Equals(header.GetProperty("name").GetString(), "Subject", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(headers, header => header.GetProperty("name").GetString() == "x-unsafe");
        Assert.Equal("202", result.Metadata["microsoftGraphStatusCode"]);
        Assert.Equal("graph.example.test", result.Metadata["microsoftGraphEndpointHost"]);
        Assert.Equal("sender@example.test", result.Metadata["microsoftGraphSenderUserId"]);
        Assert.Equal("false", result.Metadata["microsoftGraphSaveToSentItems"]);
        Assert.Equal("2", result.Metadata["microsoftGraphCategoryCount"]);
        Assert.Equal("7", result.Metadata["microsoftGraphHeaderCount"]);
        Assert.Equal("graph-request-304", result.Metadata["microsoftGraphRequestId"]);
        Assert.Equal(TenantInvitationDeliveryOutcomes.Dispatched, invitation.Metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryOutcome]);
        Assert.DoesNotContain("graph-secret-token-304", result.Metadata.Values);
        Assert.DoesNotContain("graph-secret-token-304", captured.Body, StringComparison.Ordinal);
        Assert.Contains(
            diagnosticsCatalog.Conventions,
            convention => convention.Source == "Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery");
    }

    [Fact]
    public async Task MicrosoftGraphInvitationDeliverySenderUsesRequestMetadataRecipientWhenInviteeIsNotEmail()
    {
        var client = new RecordingMicrosoftGraphInvitationDeliveryClient(new MicrosoftGraphInvitationDeliveryClientResult(accepted: true));
        var services = new ServiceCollection();
        services.AddSingleton<IMicrosoftGraphInvitationDeliveryClient>(client);
        services.AddCephalonMicrosoftGraphInvitationDelivery(options =>
        {
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
                    invitationId: "invite-graph-metadata",
                    tenantId: "tenant-graph",
                    inviteeId: "user-304",
                    inviteeKind: "user",
                    displayName: "Metadata Recipient",
                    expiresAtUtc: new DateTimeOffset(2026, 05, 01, 0, 0, 0, TimeSpan.Zero)));
            });
        });

        await using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<ITenantInvitationDeliveryDispatcher>();

        var result = await dispatcher.DispatchAsync(new TenantInvitationDeliveryRequest(
            tenantId: "tenant-graph",
            invitationId: "invite-graph-metadata",
            channel: "email",
            senderId: "microsoft-graph-email",
            atUtc: new DateTimeOffset(2026, 04, 30, 12, 30, 0, TimeSpan.Zero),
            metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["recipientEmail"] = "metadata-recipient@example.test"
            }));

        var captured = Assert.Single(client.Messages);

        Assert.True(result.Dispatched);
        Assert.Equal("metadata-recipient@example.test", captured.ToEmail);
        Assert.Equal("me", captured.SenderUserId);
        Assert.Equal("recipientEmail", result.Metadata["microsoftGraphRecipientMetadataKey"]);
    }

    [Fact]
    public async Task MicrosoftGraphInvitationDeliverySenderReportsFailureStatusWithoutLeakingBearerToken()
    {
        var handler = new CapturingHttpMessageHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                ReasonPhrase = "Unauthorized",
                Content = new StringContent("""{"error":{"code":"InvalidAuthenticationToken","message":"Access token is invalid."}}""")
            }));

        var services = new ServiceCollection();
        services.AddCephalonMicrosoftGraphInvitationDelivery(options =>
        {
            options.AccessToken = "graph-secret-token-304";
        });
        services.AddHttpClient(MicrosoftGraphInvitationDeliveryServiceCollectionExtensions.HttpClientName)
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
                    invitationId: "invite-graph-failed",
                    tenantId: "tenant-graph",
                    inviteeId: "owner@example.test",
                    inviteeKind: "email",
                    expiresAtUtc: new DateTimeOffset(2026, 05, 01, 0, 0, 0, TimeSpan.Zero)));
            });
        });

        await using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<ITenantInvitationDeliveryDispatcher>();

        var result = await dispatcher.DispatchAsync(new TenantInvitationDeliveryRequest(
            tenantId: "tenant-graph",
            invitationId: "invite-graph-failed",
            channel: "email",
            senderId: "microsoft-graph-email",
            atUtc: new DateTimeOffset(2026, 04, 30, 13, 0, 0, TimeSpan.Zero)));

        Assert.False(result.Dispatched);
        Assert.True(result.Recorded);
        Assert.Equal(TenantInvitationDeliveryOutcomes.SenderFailed, result.Outcome);
        Assert.Equal("401", result.Metadata["microsoftGraphStatusCode"]);
        Assert.Equal("Unauthorized", result.Metadata["microsoftGraphReasonPhrase"]);
        Assert.Equal("InvalidAuthenticationToken", result.Metadata["microsoftGraphErrorCode"]);
        Assert.Equal("Access token is invalid.", result.Metadata["microsoftGraphErrorMessage"]);
        Assert.DoesNotContain("graph-secret-token-304", result.Metadata.Values);
    }

    [Fact]
    public async Task MicrosoftGraphInvitationDeliverySenderSuppressesUnsupportedChannelWithoutHttpCall()
    {
        var handler = new CapturingHttpMessageHandler(_ => throw new InvalidOperationException("Unexpected Microsoft Graph dispatch."));
        var services = new ServiceCollection();
        services.AddCephalonMicrosoftGraphInvitationDelivery(options =>
        {
            options.AccessToken = "graph-secret-token-304";
            options.SupportedChannels = ["email"];
        });
        services.AddHttpClient(MicrosoftGraphInvitationDeliveryServiceCollectionExtensions.HttpClientName)
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
                    invitationId: "invite-graph-suppressed",
                    tenantId: "tenant-graph",
                    inviteeId: "owner@example.test",
                    inviteeKind: "email",
                    expiresAtUtc: new DateTimeOffset(2026, 05, 01, 0, 0, 0, TimeSpan.Zero)));
            });
        });

        await using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<ITenantInvitationDeliveryDispatcher>();
        var catalog = provider.GetRequiredService<ITenantInvitationCatalog>();

        var result = await dispatcher.DispatchAsync(new TenantInvitationDeliveryRequest(
            tenantId: "tenant-graph",
            invitationId: "invite-graph-suppressed",
            channel: "sms",
            senderId: "microsoft-graph-email",
            atUtc: new DateTimeOffset(2026, 04, 30, 13, 30, 0, TimeSpan.Zero)));

        var invitation = Assert.Single(catalog.Invitations);

        Assert.False(result.Dispatched);
        Assert.True(result.Recorded);
        Assert.Equal(TenantInvitationDeliveryOutcomes.Suppressed, result.Outcome);
        Assert.Equal(TenantInvitationDeliveryOutcomes.Suppressed, invitation.Metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryOutcome]);
        Assert.Empty(handler.Requests);
    }

    private sealed class RecordingMicrosoftGraphInvitationDeliveryClient(MicrosoftGraphInvitationDeliveryClientResult result) : IMicrosoftGraphInvitationDeliveryClient
    {
        public List<MicrosoftGraphInvitationDeliveryMessage> Messages { get; } = [];

        public ValueTask<MicrosoftGraphInvitationDeliveryClientResult> SendAsync(
            MicrosoftGraphInvitationDeliveryMessage message,
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
        Dictionary<string, string> Headers,
        string Body)
    {
        public static async Task<CapturedRequest> FromAsync(HttpRequestMessage request)
        {
            var contentHeaders = request.Content is null
                ? Enumerable.Empty<KeyValuePair<string, IEnumerable<string>>>()
                : request.Content.Headers;

            var headers = request.Headers
                .Concat(contentHeaders)
                .ToDictionary(
                    static header => header.Key,
                    static header => string.Join(",", header.Value),
                    StringComparer.OrdinalIgnoreCase);

            return new CapturedRequest(
                request.Method,
                request.RequestUri?.ToString() ?? string.Empty,
                headers,
                request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync().ConfigureAwait(false));
        }
    }
}
