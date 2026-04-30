using Azure.Core;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Diagnostics;
using Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.AzureIdentity.Hosting;
using Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.Hosting;
using Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.Services;
using Cephalon.MultiTenancy.Governance.Registration;
using Cephalon.MultiTenancy.Governance.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace Cephalon.Tests.Composition;

public sealed class MultiTenancyGovernanceMicrosoftGraphDeliveryAzureIdentityPackTests
{
    [Fact]
    public async Task AzureIdentityTokenProviderUsesConfiguredCredentialAndScopes()
    {
        var credential = new RecordingTokenCredential("azure-identity-token-305");
        var services = new ServiceCollection();
        services.AddCephalonMicrosoftGraphInvitationDeliveryAzureIdentity(
            credential,
            options =>
            {
                options.Scopes =
                [
                    "https://graph.microsoft.com/.default",
                    "https://graph.microsoft.com/Mail.Send"
                ];
            });
        services.AddCephalon(engine =>
        {
            engine.UseSettings(new EngineSettings(
                blueprint: "Microservice",
                technologies: ["MultiTenancy"],
                tenancy: new TenancySettings(
                    enabled: true,
                    mode: "SharedDatabase")));
        });

        await using var provider = services.BuildServiceProvider();
        var tokenProvider = provider.GetRequiredService<IMicrosoftGraphInvitationDeliveryAccessTokenProvider>();
        var diagnosticsCatalog = provider.GetRequiredService<IRuntimeDiagnosticsCatalog>();

        var token = await tokenProvider.GetAccessTokenAsync();
        var request = Assert.Single(credential.Requests);

        Assert.Equal("azure-identity-token-305", token);
        Assert.Equal(
            ["https://graph.microsoft.com/.default", "https://graph.microsoft.com/Mail.Send"],
            request.Scopes);
        Assert.Contains(
            diagnosticsCatalog.Conventions,
            convention => convention.Source == "Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.AzureIdentity");
    }

    [Fact]
    public async Task AzureIdentityRegistrationOverridesStaticTokenProviderWhenDispatchingGraphInvitation()
    {
        var capturedRequests = new List<CapturedRequest>();
        var credential = new RecordingTokenCredential("azure-identity-token-305");
        var handler = new CapturingHttpMessageHandler(async request =>
        {
            capturedRequests.Add(await CapturedRequest.FromAsync(request));
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        });

        var services = new ServiceCollection();
        services.AddCephalonMicrosoftGraphInvitationDelivery(options =>
        {
            options.BaseUrl = "https://graph.example.test";
            options.AccessToken = "static-token-should-not-win";
        });
        services.AddCephalonMicrosoftGraphInvitationDeliveryAzureIdentity(credential);
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
                    invitationId: "invite-graph-azure-identity",
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
            invitationId: "invite-graph-azure-identity",
            channel: "email",
            senderId: "microsoft-graph-email",
            atUtc: new DateTimeOffset(2026, 04, 30, 14, 0, 0, TimeSpan.Zero)));

        var captured = Assert.Single(capturedRequests);

        Assert.True(result.Dispatched);
        Assert.Equal("Bearer azure-identity-token-305", captured.Headers["Authorization"]);
        Assert.Equal("MicrosoftGraphInvitationDeliveryAzureIdentityAccessTokenProvider", result.Metadata["microsoftGraphTokenProvider"]);
        Assert.DoesNotContain("static-token-should-not-win", result.Metadata.Values);
        Assert.DoesNotContain("static-token-should-not-win", captured.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AzureIdentityTokenFailureReportsMissingTokenWithoutCallingGraph()
    {
        var credential = new RecordingTokenCredential("unused-token", new InvalidOperationException("credential unavailable"));
        var handler = new CapturingHttpMessageHandler(_ => throw new InvalidOperationException("Unexpected Microsoft Graph dispatch."));
        var services = new ServiceCollection();
        services.AddCephalonMicrosoftGraphInvitationDelivery();
        services.AddCephalonMicrosoftGraphInvitationDeliveryAzureIdentity(credential);
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
                    invitationId: "invite-graph-azure-identity-failure",
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
            invitationId: "invite-graph-azure-identity-failure",
            channel: "email",
            senderId: "microsoft-graph-email",
            atUtc: new DateTimeOffset(2026, 04, 30, 14, 30, 0, TimeSpan.Zero)));

        Assert.False(result.Dispatched);
        Assert.True(result.Recorded);
        Assert.Equal(TenantInvitationDeliveryOutcomes.SenderFailed, result.Outcome);
        Assert.Equal("false", result.Metadata["microsoftGraphTokenAvailable"]);
        Assert.Equal("MicrosoftGraphInvitationDeliveryAzureIdentityAccessTokenProvider", result.Metadata["microsoftGraphTokenProvider"]);
        Assert.Empty(handler.Requests);
    }

    private sealed class RecordingTokenCredential(string token, Exception? exception = null) : TokenCredential
    {
        public List<TokenRequestContext> Requests { get; } = [];

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            throw new NotSupportedException("Tests should use async token acquisition.");
        }

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            Requests.Add(requestContext);

            if (exception is not null)
            {
                throw exception;
            }

            return ValueTask.FromResult(new AccessToken(token, new DateTimeOffset(2026, 05, 01, 0, 0, 0, TimeSpan.Zero)));
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
