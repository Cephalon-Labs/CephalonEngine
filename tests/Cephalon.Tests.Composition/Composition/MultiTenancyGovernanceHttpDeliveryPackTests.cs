using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Diagnostics;
using Cephalon.MultiTenancy.Governance.HttpDelivery.Hosting;
using Cephalon.MultiTenancy.Governance.Services;
using Cephalon.MultiTenancy.Governance.Registration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;

namespace Cephalon.Tests.Composition;

public sealed class MultiTenancyGovernanceHttpDeliveryPackTests
{
    [Fact]
    public async Task HttpInvitationDeliverySenderPostsWebhookPayloadAndRecordsProviderMessage()
    {
        var capturedRequests = new List<CapturedRequest>();
        var handler = new CapturingHttpMessageHandler(async request =>
        {
            capturedRequests.Add(await CapturedRequest.FromAsync(request));

            var response = new HttpResponseMessage(HttpStatusCode.Accepted)
            {
                Content = new StringContent("{\"accepted\":true}")
            };
            response.Headers.Add("X-Cephalon-Provider-Message-Id", "provider-message-257");

            return response;
        });

        var services = new ServiceCollection();
        services.AddCephalonHttpInvitationDelivery(options =>
        {
            options.Endpoint = "https://delivery.example.test/invitations";
            options.Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["X-Delivery-Key"] = "test-key"
            };
            options.ExpectedStatusCodes = [202];
            options.IncludeResponseBodyInMetadata = true;
        });
        services.AddHttpClient(HttpInvitationDeliveryServiceCollectionExtensions.HttpClientName)
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
                    invitationId: "invite-http",
                    tenantId: "tenant-http",
                    inviteeId: "user-http",
                    displayName: "HTTP Invitee",
                    roles: ["member"],
                    expiresAtUtc: new DateTimeOffset(2026, 05, 01, 0, 0, 0, TimeSpan.Zero),
                    metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["template"] = "welcome"
                    }));
            });
        });

        await using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<ITenantInvitationDeliveryDispatcher>();
        var catalog = provider.GetRequiredService<ITenantInvitationCatalog>();
        var diagnosticsCatalog = provider.GetRequiredService<IRuntimeDiagnosticsCatalog>();

        var result = await dispatcher.DispatchAsync(new TenantInvitationDeliveryRequest(
            tenantId: "tenant-http",
            invitationId: "invite-http",
            channel: "email",
            senderId: "http-webhook",
            source: "http-delivery-test",
            actor: "operator-257",
            atUtc: new DateTimeOffset(2026, 04, 29, 5, 0, 0, TimeSpan.Zero),
            correlationId: "corr-http-257",
            metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["locale"] = "en-US"
            }));

        var captured = Assert.Single(capturedRequests);
        using var payload = JsonDocument.Parse(captured.Body);
        var invitation = Assert.Single(catalog.Invitations);

        Assert.True(result.Dispatched);
        Assert.True(result.Recorded);
        Assert.Equal(TenantInvitationDeliveryOutcomes.Dispatched, result.Outcome);
        Assert.Equal("provider-message-257", result.ProviderMessageId);
        Assert.Equal("202", result.Metadata["httpStatusCode"]);
        Assert.Equal("delivery.example.test", result.Metadata["httpEndpointHost"]);
        Assert.Equal("{\"accepted\":true}", result.Metadata["httpResponseBody"]);
        Assert.Equal(HttpMethod.Post, captured.Method);
        Assert.Equal("https://delivery.example.test/invitations", captured.RequestUri);
        Assert.Equal("test-key", captured.Headers["X-Delivery-Key"]);
        Assert.Equal("tenant-http", payload.RootElement.GetProperty("tenantId").GetString());
        Assert.Equal("invite-http", payload.RootElement.GetProperty("invitationId").GetString());
        Assert.Equal("user-http", payload.RootElement.GetProperty("inviteeId").GetString());
        Assert.Equal("email", payload.RootElement.GetProperty("channel").GetString());
        Assert.Equal("http-webhook", payload.RootElement.GetProperty("requestedSenderId").GetString());
        Assert.Equal("welcome", payload.RootElement.GetProperty("invitationMetadata").GetProperty("template").GetString());
        Assert.Equal("en-US", payload.RootElement.GetProperty("metadata").GetProperty("locale").GetString());
        Assert.Equal("provider-message-257", invitation.Metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryProviderMessageId]);
        Assert.Contains(
            diagnosticsCatalog.Conventions,
            convention => convention.Source == "Cephalon.MultiTenancy.Governance.HttpDelivery");
    }

    [Fact]
    public async Task HttpInvitationDeliverySenderSuppressesUnsupportedChannelWithoutHttpCall()
    {
        var handler = new CapturingHttpMessageHandler(_ => throw new InvalidOperationException("Unexpected HTTP dispatch."));
        var services = new ServiceCollection();
        services.AddCephalonHttpInvitationDelivery(options =>
        {
            options.Endpoint = "https://delivery.example.test/invitations";
            options.SupportedChannels = ["webhook"];
        });
        services.AddHttpClient(HttpInvitationDeliveryServiceCollectionExtensions.HttpClientName)
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
                    invitationId: "invite-http",
                    tenantId: "tenant-http",
                    inviteeId: "user-http",
                    roles: ["member"],
                    expiresAtUtc: new DateTimeOffset(2026, 05, 01, 0, 0, 0, TimeSpan.Zero)));
            });
        });

        await using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<ITenantInvitationDeliveryDispatcher>();
        var catalog = provider.GetRequiredService<ITenantInvitationCatalog>();

        var result = await dispatcher.DispatchAsync(new TenantInvitationDeliveryRequest(
            tenantId: "tenant-http",
            invitationId: "invite-http",
            channel: "email",
            senderId: "http-webhook",
            atUtc: new DateTimeOffset(2026, 04, 29, 5, 5, 0, TimeSpan.Zero)));

        var invitation = Assert.Single(catalog.Invitations);

        Assert.False(result.Dispatched);
        Assert.True(result.Recorded);
        Assert.Equal(TenantInvitationDeliveryOutcomes.Suppressed, result.Outcome);
        Assert.Equal(TenantInvitationDeliveryOutcomes.Suppressed, invitation.Metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryOutcome]);
        Assert.Equal("provider-managed", invitation.Metadata[TenantInvitationDeliveryMetadataKeys.ExternalDeliveryOwnership]);
        Assert.Empty(handler.Requests);
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
