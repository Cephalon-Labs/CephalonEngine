using Cephalon.Abstractions.Technologies;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.MultiTenancy.Governance.Registration;
using Cephalon.MultiTenancy.Governance.Services;
using Cephalon.MultiTenancy.Governance.SmtpDelivery.Hosting;
using Cephalon.Tests.ProviderIntegration.ExternalServices;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using System.Net;
using System.Text.Json;

namespace Cephalon.Tests.ProviderIntegration;

public sealed class SmtpDeliveryProviderIntegrationTests : IAsyncLifetime
{
    private const string MailHogImage = "mailhog/mailhog:v1.0.1";
    private const int MailHogSmtpPort = 1025;
    private const int MailHogApiPort = 8025;
    private IContainer? _container;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync().ConfigureAwait(false);
        }
    }

    [ExternalProviderServiceFact(ExternalProviderServiceProvider.Smtp)]
    public async Task SmtpDelivery_DispatchesInvitationThroughLiveRelay()
    {
        var gate = ExternalProviderServiceGate.FromEnvironment();
        var service = await ResolveSmtpServiceAsync(gate).ConfigureAwait(false);
        var uniqueId = Guid.NewGuid().ToString("N");
        var tenantId = $"tenant-smtp-{uniqueId}";
        var invitationId = $"invite-smtp-{uniqueId}";
        var recipient = $"recipient-{uniqueId}@example.test";
        var subjectToken = $"smtp-live-proof-{uniqueId}";
        var fromAddress = $"noreply-{uniqueId}@example.test";

        var services = new ServiceCollection();
        services.AddCephalonSmtpInvitationDelivery(options =>
        {
            options.Host = service.Host;
            options.Port = service.SmtpPort;
            options.UseSsl = false;
            options.FromAddress = fromAddress;
            options.FromDisplayName = "Cephalon Live Proof";
            options.MessageIdDomain = "mail.example.test";
            options.SubjectTemplate = $"Cephalon SMTP live proof {subjectToken} for {{tenantId}}";
            options.TextBodyTemplate = $"SMTP live proof {uniqueId}: invitation {{invitationId}} for {{inviteeId}} correlation {{correlationId}}.";
            options.Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["X-Cephalon-Live-Proof"] = uniqueId
            };
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
                    invitationId: invitationId,
                    tenantId: tenantId,
                    inviteeId: recipient,
                    inviteeKind: "email",
                    displayName: "SMTP Live Recipient",
                    roles: ["owner"],
                    expiresAtUtc: new DateTimeOffset(2026, 05, 10, 0, 0, 0, TimeSpan.Zero)));
            });
        });

        await using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<ITenantInvitationDeliveryDispatcher>();
        var catalog = provider.GetRequiredService<ITenantInvitationCatalog>();

        var result = await dispatcher.DispatchAsync(new TenantInvitationDeliveryRequest(
                tenantId: tenantId,
                invitationId: invitationId,
                channel: "email",
                senderId: "smtp-email",
                source: "smtp-live-provider-test",
                actor: "provider-test",
                atUtc: new DateTimeOffset(2026, 05, 09, 1, 0, 0, TimeSpan.Zero),
                correlationId: $"corr-{uniqueId}"))
            .ConfigureAwait(false);

        var invitation = Assert.Single(catalog.Invitations);

        Assert.True(result.Dispatched, result.Reason);
        Assert.True(result.Recorded, result.Reason);
        Assert.Equal(TenantInvitationDeliveryOutcomes.Dispatched, result.Outcome);
        Assert.Equal(result.Metadata["smtpMessageId"], result.ProviderMessageId);
        Assert.Equal(service.Host, result.Metadata["smtpRelayHost"]);
        Assert.Equal(service.SmtpPort.ToString(CultureInfo.InvariantCulture), result.Metadata["smtpRelayPort"]);
        Assert.Equal("false", result.Metadata["smtpUseSsl"]);
        Assert.Equal(result.ProviderMessageId, invitation.Metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryProviderMessageId]);

        var relayMessage = await ReadAcceptedMessageAsync(service.ApiBaseUri, subjectToken).ConfigureAwait(false);
        Assert.Equal(fromAddress, ReadAddress(relayMessage.GetProperty("From")));
        var to = Assert.Single(relayMessage.GetProperty("To").EnumerateArray());
        Assert.Equal(recipient, ReadAddress(to));
        Assert.Equal($"Cephalon SMTP live proof {subjectToken} for {tenantId}", ReadHeader(relayMessage, "Subject"));
        Assert.Equal(result.ProviderMessageId, ReadHeader(relayMessage, "Message-Id"));
        Assert.Equal(uniqueId, ReadHeader(relayMessage, "X-Cephalon-Live-Proof"));
        Assert.Equal(tenantId, ReadHeader(relayMessage, "X-Cephalon-Tenant-Id"));
        Assert.Equal(invitationId, ReadHeader(relayMessage, "X-Cephalon-Invitation-Id"));
        Assert.Contains($"SMTP live proof {uniqueId}: invitation {invitationId}", ReadBody(relayMessage), StringComparison.Ordinal);

        var smtpSurface = Assert.Single(
            provider.GetRequiredService<ITechnologyRuntimeCatalog>().GetByTechnology("multi-tenancy"),
            surface => string.Equals(surface.SurfaceId, "tenant-invitation-delivery-smtp", StringComparison.OrdinalIgnoreCase));
        var smtpEntry = Assert.Single(smtpSurface.Entries);
        Assert.Equal("provider-managed", smtpEntry.Metadata["ownership"]);
        Assert.Equal(service.Host, smtpEntry.Metadata["host"]);
        Assert.Equal(service.SmtpPort.ToString(CultureInfo.InvariantCulture), smtpEntry.Metadata["port"]);
        Assert.Equal("redacted", smtpEntry.Metadata["secretProjection"]);
        Assert.DoesNotContain(smtpEntry.Metadata.Values, value => value.Contains(uniqueId, StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(smtpEntry.Metadata.Values, value => value.Contains("SMTP live proof", StringComparison.OrdinalIgnoreCase));
    }

    private async Task<SmtpProviderService> ResolveSmtpServiceAsync(ExternalProviderServiceGate gate)
    {
        return gate.ResolveSmtpMode() switch
        {
            ExternalProviderServiceMode.PreProvisionedConnectionString => new SmtpProviderService(
                gate.SmtpHost!,
                gate.SmtpPortOrDefault,
                new Uri(gate.SmtpApiUri!, UriKind.Absolute)),
            ExternalProviderServiceMode.Testcontainers => await StartMailHogAsync().ConfigureAwait(false),
            _ => throw new InvalidOperationException(ExternalProviderServiceGate.SkipReason)
        };
    }

    private async Task<SmtpProviderService> StartMailHogAsync()
    {
        _container = new ContainerBuilder(MailHogImage)
            .WithPortBinding(MailHogSmtpPort, true)
            .WithPortBinding(MailHogApiPort, true)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilInternalTcpPortIsAvailable(MailHogSmtpPort)
                .UntilInternalTcpPortIsAvailable(MailHogApiPort)
                .UntilExternalTcpPortIsAvailable(MailHogSmtpPort)
                .UntilExternalTcpPortIsAvailable(MailHogApiPort))
            .Build();

        await _container.StartAsync().ConfigureAwait(false);
        var service = new SmtpProviderService(
            _container.Hostname,
            _container.GetMappedPublicPort(MailHogSmtpPort),
            new Uri($"http://{_container.Hostname}:{_container.GetMappedPublicPort(MailHogApiPort)}", UriKind.Absolute));
        await WaitForMailHogApiReadyAsync(service.ApiBaseUri).ConfigureAwait(false);
        return service;
    }

    private static async Task WaitForMailHogApiReadyAsync(Uri apiBaseUri)
    {
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var messagesUri = new Uri(apiBaseUri, "/api/v2/messages");
        for (var attempt = 0; attempt < 120; attempt++)
        {
            try
            {
                using var response = await httpClient.GetAsync(messagesUri).ConfigureAwait(false);
                if (response.StatusCode != HttpStatusCode.ServiceUnavailable &&
                    response.StatusCode != HttpStatusCode.BadGateway &&
                    response.StatusCode != HttpStatusCode.GatewayTimeout)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
                // The API port can be open before MailHog finishes binding handlers.
            }
            catch (TaskCanceledException)
            {
                // Keep polling until the bounded readiness window expires.
            }

            await Task.Delay(250).ConfigureAwait(false);
        }

        throw new TimeoutException($"MailHog API at '{apiBaseUri}' did not become ready.");
    }

    private static async Task<JsonElement> ReadAcceptedMessageAsync(Uri apiBaseUri, string subjectToken)
    {
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var messagesUri = new Uri(apiBaseUri, "/api/v2/messages");
        for (var attempt = 0; attempt < 120; attempt++)
        {
            using var response = await httpClient.GetAsync(messagesUri).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            using var document = await JsonDocument.ParseAsync(stream).ConfigureAwait(false);

            if (document.RootElement.TryGetProperty("items", out var items))
            {
                foreach (var item in items.EnumerateArray())
                {
                    var subject = ReadHeader(item, "Subject");
                    if (subject?.Contains(subjectToken, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        return item.Clone();
                    }
                }
            }

            await Task.Delay(250).ConfigureAwait(false);
        }

        throw new TimeoutException($"SMTP relay did not expose a message with subject token '{subjectToken}'.");
    }

    private static string? ReadHeader(JsonElement message, string headerName)
    {
        var headers = message.GetProperty("Content").GetProperty("Headers");
        foreach (var header in headers.EnumerateObject())
        {
            if (!string.Equals(header.Name, headerName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (header.Value.ValueKind == JsonValueKind.Array)
            {
                return header.Value.EnumerateArray().FirstOrDefault().GetString();
            }

            return header.Value.GetString();
        }

        return null;
    }

    private static string ReadBody(JsonElement message)
    {
        return (message.GetProperty("Content").GetProperty("Body").GetString() ?? string.Empty)
            .Replace("=\r\n", string.Empty, StringComparison.Ordinal)
            .Replace("=\n", string.Empty, StringComparison.Ordinal);
    }

    private static string ReadAddress(JsonElement address)
    {
        return $"{address.GetProperty("Mailbox").GetString()}@{address.GetProperty("Domain").GetString()}";
    }

    private sealed record SmtpProviderService(string Host, int SmtpPort, Uri ApiBaseUri);
}
