using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Diagnostics;
using Cephalon.MultiTenancy.Governance.HttpDelivery.Hosting;
using Cephalon.MultiTenancy.Governance.Services;
using Cephalon.MultiTenancy.Governance.Registration;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
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
    public async Task HttpInvitationDeliverySenderRetriesTransientStatusBeforeAcceptingDispatch()
    {
        var capturedRequests = new List<CapturedRequest>();
        var attempts = 0;
        var handler = new CapturingHttpMessageHandler(async request =>
        {
            capturedRequests.Add(await CapturedRequest.FromAsync(request));
            attempts++;

            if (attempts == 1)
            {
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }

            var response = new HttpResponseMessage(HttpStatusCode.Accepted);
            response.Headers.Add("X-Cephalon-Provider-Message-Id", "provider-message-259");

            return response;
        });

        var services = new ServiceCollection();
        services.AddCephalonHttpInvitationDelivery(options =>
        {
            options.Endpoint = "https://delivery.example.test/invitations";
            options.ExpectedStatusCodes = [202];
            options.MaxAttempts = 2;
            options.RetryDelayMilliseconds = 0;
            options.RetryStatusCodes = [503];
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
                    invitationId: "invite-http-retry",
                    tenantId: "tenant-http",
                    inviteeId: "user-http",
                    roles: ["member"],
                    expiresAtUtc: new DateTimeOffset(2026, 05, 01, 0, 0, 0, TimeSpan.Zero)));
            });
        });

        await using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<ITenantInvitationDeliveryDispatcher>();

        var result = await dispatcher.DispatchAsync(new TenantInvitationDeliveryRequest(
            tenantId: "tenant-http",
            invitationId: "invite-http-retry",
            channel: "webhook",
            senderId: "http-webhook",
            atUtc: new DateTimeOffset(2026, 04, 29, 6, 30, 0, TimeSpan.Zero)));

        Assert.True(result.Dispatched);
        Assert.Equal(TenantInvitationDeliveryOutcomes.Dispatched, result.Outcome);
        Assert.Equal("provider-message-259", result.ProviderMessageId);
        Assert.Equal(2, capturedRequests.Count);
        Assert.Equal("202", result.Metadata["httpStatusCode"]);
        Assert.Equal("2", result.Metadata["httpAttemptCount"]);
        Assert.Equal("2", result.Metadata["httpMaxAttempts"]);
        Assert.Equal("true", result.Metadata["httpRetried"]);
        Assert.Equal("HTTP status code 503.", result.Metadata["httpRetryReason"]);
        Assert.Equal("0", result.Metadata["httpRetryDelayMilliseconds"]);
        Assert.Equal("503", result.Metadata["httpRetryStatusCodes"]);
        Assert.Equal("true", result.Metadata["httpRetryTransportFailures"]);
        Assert.Equal("X-Cephalon-Idempotency-Key", result.Metadata["httpIdempotencyHeaderName"]);
        Assert.Equal("derived", result.Metadata["httpIdempotencyKeySource"]);
        Assert.StartsWith("cephalon-invitation-", result.Metadata["httpIdempotencyKey"], StringComparison.Ordinal);
        Assert.All(capturedRequests, captured =>
        {
            Assert.Equal(HttpMethod.Post, captured.Method);
            Assert.Equal("https://delivery.example.test/invitations", captured.RequestUri);
            Assert.Equal(result.Metadata["httpIdempotencyKey"], captured.Headers["X-Cephalon-Idempotency-Key"]);
        });
    }

    [Fact]
    public async Task HttpInvitationDeliverySenderUsesMetadataIdempotencyKeyWhenConfigured()
    {
        var capturedRequests = new List<CapturedRequest>();
        var handler = new CapturingHttpMessageHandler(async request =>
        {
            capturedRequests.Add(await CapturedRequest.FromAsync(request));
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        });

        var services = new ServiceCollection();
        services.AddCephalonHttpInvitationDelivery(options =>
        {
            options.Endpoint = "https://delivery.example.test/invitations";
            options.IdempotencyHeaderName = "Idempotency-Key";
            options.IdempotencyMetadataKey = "deliveryIdempotencyKey";
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
                    invitationId: "invite-http-idempotency",
                    tenantId: "tenant-http",
                    inviteeId: "user-http",
                    roles: ["member"],
                    expiresAtUtc: new DateTimeOffset(2026, 05, 01, 0, 0, 0, TimeSpan.Zero)));
            });
        });

        await using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<ITenantInvitationDeliveryDispatcher>();

        var result = await dispatcher.DispatchAsync(new TenantInvitationDeliveryRequest(
            tenantId: "tenant-http",
            invitationId: "invite-http-idempotency",
            channel: "webhook",
            senderId: "http-webhook",
            atUtc: new DateTimeOffset(2026, 04, 29, 7, 0, 0, TimeSpan.Zero),
            metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["deliveryIdempotencyKey"] = "external-dispatch-260"
            }));

        var captured = Assert.Single(capturedRequests);

        Assert.True(result.Dispatched);
        Assert.Equal("Idempotency-Key", result.Metadata["httpIdempotencyHeaderName"]);
        Assert.Equal("external-dispatch-260", result.Metadata["httpIdempotencyKey"]);
        Assert.Equal("metadata", result.Metadata["httpIdempotencyKeySource"]);
        Assert.Equal("external-dispatch-260", captured.Headers["Idempotency-Key"]);
    }

    [Fact]
    public async Task HttpInvitationDeliverySenderHashesUnsafeMetadataIdempotencyKeyBeforeSendingHeader()
    {
        var capturedRequests = new List<CapturedRequest>();
        var handler = new CapturingHttpMessageHandler(async request =>
        {
            capturedRequests.Add(await CapturedRequest.FromAsync(request));
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        });

        var services = new ServiceCollection();
        services.AddCephalonHttpInvitationDelivery(options =>
        {
            options.Endpoint = "https://delivery.example.test/invitations";
            options.IdempotencyMetadataKey = "deliveryIdempotencyKey";
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
                    invitationId: "invite-http-idempotency-safe",
                    tenantId: "tenant-http",
                    inviteeId: "user-http",
                    roles: ["member"],
                    expiresAtUtc: new DateTimeOffset(2026, 05, 01, 0, 0, 0, TimeSpan.Zero)));
            });
        });

        await using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<ITenantInvitationDeliveryDispatcher>();

        var result = await dispatcher.DispatchAsync(new TenantInvitationDeliveryRequest(
            tenantId: "tenant-http",
            invitationId: "invite-http-idempotency-safe",
            channel: "webhook",
            senderId: "http-webhook",
            atUtc: new DateTimeOffset(2026, 04, 29, 7, 0, 0, TimeSpan.Zero),
            metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["deliveryIdempotencyKey"] = "external-dispatch-260\r\nunsafe"
            }));

        var captured = Assert.Single(capturedRequests);

        Assert.True(result.Dispatched);
        Assert.Equal("metadata", result.Metadata["httpIdempotencyKeySource"]);
        Assert.StartsWith("cephalon-custom-", result.Metadata["httpIdempotencyKey"], StringComparison.Ordinal);
        Assert.Equal(result.Metadata["httpIdempotencyKey"], captured.Headers["X-Cephalon-Idempotency-Key"]);
        Assert.DoesNotContain("\r", captured.Headers["X-Cephalon-Idempotency-Key"], StringComparison.Ordinal);
        Assert.DoesNotContain("\n", captured.Headers["X-Cephalon-Idempotency-Key"], StringComparison.Ordinal);
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

    [Fact]
    public async Task HttpInvitationDeliverySenderSignsWebhookPayloadWhenSecretConfigured()
    {
        var capturedRequests = new List<CapturedRequest>();
        var handler = new CapturingHttpMessageHandler(async request =>
        {
            capturedRequests.Add(await CapturedRequest.FromAsync(request));
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var services = new ServiceCollection();
        services.AddCephalonHttpInvitationDelivery(options =>
        {
            options.Endpoint = "https://delivery.example.test/invitations";
            options.SigningSecret = "delivery-secret-258";
            options.SigningKeyId = "key-258";
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
                    invitationId: "invite-http-signed",
                    tenantId: "tenant-http",
                    inviteeId: "user-http",
                    roles: ["member"],
                    expiresAtUtc: new DateTimeOffset(2026, 05, 01, 0, 0, 0, TimeSpan.Zero)));
            });
        });

        await using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<ITenantInvitationDeliveryDispatcher>();

        var dispatchedAtUtc = new DateTimeOffset(2026, 04, 29, 6, 0, 0, TimeSpan.Zero);
        var result = await dispatcher.DispatchAsync(new TenantInvitationDeliveryRequest(
            tenantId: "tenant-http",
            invitationId: "invite-http-signed",
            channel: "webhook",
            senderId: "http-webhook",
            atUtc: dispatchedAtUtc));

        var captured = Assert.Single(capturedRequests);
        var timestamp = dispatchedAtUtc.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        var expectedSignature = CreateExpectedSignature("delivery-secret-258", timestamp, captured.Body);

        Assert.True(result.Dispatched);
        Assert.Equal("true", result.Metadata["httpSigned"]);
        Assert.Equal("key-258", result.Metadata["httpSigningKeyId"]);
        Assert.Equal(timestamp, captured.Headers["X-Cephalon-Webhook-Signature-Timestamp"]);
        Assert.Equal(expectedSignature, captured.Headers["X-Cephalon-Webhook-Signature"]);
        Assert.Equal("key-258", captured.Headers["X-Cephalon-Webhook-Key-Id"]);
        Assert.DoesNotContain("delivery-secret-258", captured.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("delivery-secret-258", result.Metadata.Values);
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

    private static string CreateExpectedSignature(string secret, string timestamp, string requestBody)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{timestamp}.{requestBody}"));
        return "v1=" + Convert.ToHexString(hash).ToLowerInvariant();
    }
}
