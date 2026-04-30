using Amazon.SimpleEmailV2;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Diagnostics;
using Cephalon.MultiTenancy.Governance.AmazonSesDelivery.Hosting;
using Cephalon.MultiTenancy.Governance.AmazonSesDelivery.Services;
using Cephalon.MultiTenancy.Governance.Registration;
using Cephalon.MultiTenancy.Governance.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Composition;

public sealed class MultiTenancyGovernanceAmazonSesDeliveryPackTests
{
    [Fact]
    public async Task AmazonSesInvitationDeliverySenderBuildsMessageAndRecordsSafeMetadata()
    {
        var client = new RecordingAmazonSesInvitationDeliveryClient(new AmazonSesInvitationDeliveryClientResult(
            accepted: true,
            statusCode: 200,
            providerMessageId: "ses-message-306",
            reason: "Amazon SES accepted the test message.",
            metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["amazonSesRequestId"] = "ses-request-306"
            }));
        var services = new ServiceCollection();
        services.AddSingleton<IAmazonSesInvitationDeliveryClient>(client);
        services.AddCephalonAmazonSesInvitationDelivery(options =>
        {
            options.RegionSystemName = "us-east-1";
            options.ConfigurationSetName = "cephalon-governance";
            options.FromEmail = "invites@example.test";
            options.FromName = "Cephalon Invites";
            options.ReplyToAddresses = ["support@example.test", "invalid-address"];
            options.SubjectTemplate = "Invite {displayName} to {tenantId}";
            options.TextBodyTemplate = "Invitation {invitationId} for {inviteeId}; roles={roles}; correlation={correlationId}";
            options.HtmlBodyTemplate = "<p>Invitation {invitationId} for <strong>{tenantId}</strong>.</p>";
            options.Tags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["product"] = "Cephalon",
                ["unsafe value"] = "will normalize"
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
                    invitationId: "invite-ses",
                    tenantId: "tenant-ses",
                    inviteeId: "owner@example.test",
                    inviteeKind: "email",
                    displayName: "SES Invitee",
                    roles: ["admin", "member"],
                    expiresAtUtc: new DateTimeOffset(2026, 05, 01, 0, 0, 0, TimeSpan.Zero)));
            });
        });

        await using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<ITenantInvitationDeliveryDispatcher>();
        var catalog = provider.GetRequiredService<ITenantInvitationCatalog>();
        var diagnosticsCatalog = provider.GetRequiredService<IRuntimeDiagnosticsCatalog>();

        var result = await dispatcher.DispatchAsync(new TenantInvitationDeliveryRequest(
            tenantId: "tenant-ses",
            invitationId: "invite-ses",
            channel: "email",
            senderId: "amazon-ses-email",
            source: "amazon-ses-delivery-test",
            actor: "operator-306",
            atUtc: new DateTimeOffset(2026, 04, 30, 14, 0, 0, TimeSpan.Zero),
            correlationId: "corr-ses-306"));

        var captured = Assert.Single(client.Messages);
        var invitation = Assert.Single(catalog.Invitations);

        Assert.True(result.Dispatched);
        Assert.True(result.Recorded);
        Assert.Equal(TenantInvitationDeliveryOutcomes.Dispatched, result.Outcome);
        Assert.Equal("ses-message-306", result.ProviderMessageId);
        Assert.Equal("Cephalon Invites <invites@example.test>", captured.From);
        Assert.Equal("owner@example.test", captured.ToEmail);
        Assert.Equal("Invite SES Invitee to tenant-ses", captured.Subject);
        Assert.Contains("Invitation invite-ses", captured.TextBody, StringComparison.Ordinal);
        Assert.Contains("<strong>tenant-ses</strong>", captured.HtmlBody, StringComparison.Ordinal);
        Assert.Equal("cephalon-governance", captured.ConfigurationSetName);
        Assert.Equal(["support@example.test"], captured.ReplyToAddresses);
        Assert.Equal("Cephalon", captured.Tags["product"]);
        Assert.Equal("will-normalize", captured.Tags["unsafe-value"]);
        Assert.Equal("tenant-ses", captured.Tags["cephalon-tenant-id"]);
        Assert.Equal("invite-ses", captured.Tags["cephalon-invitation-id"]);
        Assert.Equal("corr-ses-306", captured.Tags["cephalon-correlation-id"]);
        Assert.Equal("us-east-1", result.Metadata["amazonSesRegionSystemName"]);
        Assert.Equal("cephalon-governance", result.Metadata["amazonSesConfigurationSetName"]);
        Assert.Equal("owner@example.test", result.Metadata["amazonSesRecipientEmail"]);
        Assert.Equal("HTML", result.Metadata["amazonSesBodyContentType"]);
        Assert.Equal("1", result.Metadata["amazonSesReplyToAddressCount"]);
        Assert.Equal("8", result.Metadata["amazonSesTagCount"]);
        Assert.Equal("ses-request-306", result.Metadata["amazonSesRequestId"]);
        Assert.Equal(TenantInvitationDeliveryOutcomes.Dispatched, invitation.Metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryOutcome]);
        Assert.Contains(
            diagnosticsCatalog.Conventions,
            convention => convention.Source == "Cephalon.MultiTenancy.Governance.AmazonSesDelivery");
    }

    [Fact]
    public async Task AmazonSesInvitationDeliverySenderUsesRequestMetadataRecipientWhenInviteeIsNotEmail()
    {
        var client = new RecordingAmazonSesInvitationDeliveryClient(new AmazonSesInvitationDeliveryClientResult(accepted: true, statusCode: 200));
        var services = new ServiceCollection();
        services.AddSingleton<IAmazonSesInvitationDeliveryClient>(client);
        services.AddCephalonAmazonSesInvitationDelivery(options =>
        {
            options.FromEmail = "invites@example.test";
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
                    invitationId: "invite-ses-metadata",
                    tenantId: "tenant-ses",
                    inviteeId: "user-306",
                    inviteeKind: "user",
                    displayName: "Metadata Recipient",
                    expiresAtUtc: new DateTimeOffset(2026, 05, 01, 0, 0, 0, TimeSpan.Zero)));
            });
        });

        await using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<ITenantInvitationDeliveryDispatcher>();

        var result = await dispatcher.DispatchAsync(new TenantInvitationDeliveryRequest(
            tenantId: "tenant-ses",
            invitationId: "invite-ses-metadata",
            channel: "email",
            senderId: "amazon-ses-email",
            atUtc: new DateTimeOffset(2026, 04, 30, 14, 30, 0, TimeSpan.Zero),
            metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["recipientEmail"] = "metadata-recipient@example.test"
            }));

        var captured = Assert.Single(client.Messages);

        Assert.True(result.Dispatched);
        Assert.Equal("metadata-recipient@example.test", captured.ToEmail);
        Assert.Equal("recipientEmail", result.Metadata["amazonSesRecipientMetadataKey"]);
    }

    [Fact]
    public async Task AmazonSesInvitationDeliverySenderReportsProviderFailureWithoutDispatch()
    {
        var client = new RecordingAmazonSesInvitationDeliveryClient(new AmazonSesInvitationDeliveryClientResult(
            accepted: false,
            statusCode: 400,
            reason: "Amazon SES rejected the identity.",
            metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["amazonSesErrorCode"] = "MessageRejected"
            }));
        var services = new ServiceCollection();
        services.AddSingleton<IAmazonSesInvitationDeliveryClient>(client);
        services.AddCephalonAmazonSesInvitationDelivery(options =>
        {
            options.FromEmail = "invites@example.test";
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
                    invitationId: "invite-ses-failed",
                    tenantId: "tenant-ses",
                    inviteeId: "owner@example.test",
                    inviteeKind: "email",
                    expiresAtUtc: new DateTimeOffset(2026, 05, 01, 0, 0, 0, TimeSpan.Zero)));
            });
        });

        await using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<ITenantInvitationDeliveryDispatcher>();

        var result = await dispatcher.DispatchAsync(new TenantInvitationDeliveryRequest(
            tenantId: "tenant-ses",
            invitationId: "invite-ses-failed",
            channel: "email",
            senderId: "amazon-ses-email",
            atUtc: new DateTimeOffset(2026, 04, 30, 15, 0, 0, TimeSpan.Zero)));

        Assert.False(result.Dispatched);
        Assert.True(result.Recorded);
        Assert.Equal(TenantInvitationDeliveryOutcomes.SenderFailed, result.Outcome);
        Assert.Equal("400", result.Metadata["amazonSesStatusCode"]);
        Assert.Equal("MessageRejected", result.Metadata["amazonSesErrorCode"]);
        Assert.Equal("Amazon SES rejected the identity.", result.Metadata["amazonSesReason"]);
    }

    [Fact]
    public async Task AmazonSesInvitationDeliverySenderSuppressesUnsupportedChannelWithoutClientCall()
    {
        var client = new ThrowingAmazonSesInvitationDeliveryClient();
        var services = new ServiceCollection();
        services.AddSingleton<IAmazonSesInvitationDeliveryClient>(client);
        services.AddCephalonAmazonSesInvitationDelivery(options =>
        {
            options.FromEmail = "invites@example.test";
            options.SupportedChannels = ["email"];
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
                    invitationId: "invite-ses-suppressed",
                    tenantId: "tenant-ses",
                    inviteeId: "owner@example.test",
                    inviteeKind: "email",
                    expiresAtUtc: new DateTimeOffset(2026, 05, 01, 0, 0, 0, TimeSpan.Zero)));
            });
        });

        await using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<ITenantInvitationDeliveryDispatcher>();
        var catalog = provider.GetRequiredService<ITenantInvitationCatalog>();

        var result = await dispatcher.DispatchAsync(new TenantInvitationDeliveryRequest(
            tenantId: "tenant-ses",
            invitationId: "invite-ses-suppressed",
            channel: "sms",
            senderId: "amazon-ses-email",
            atUtc: new DateTimeOffset(2026, 04, 30, 15, 30, 0, TimeSpan.Zero)));

        var invitation = Assert.Single(catalog.Invitations);

        Assert.False(result.Dispatched);
        Assert.True(result.Recorded);
        Assert.Equal(TenantInvitationDeliveryOutcomes.Suppressed, result.Outcome);
        Assert.Equal(TenantInvitationDeliveryOutcomes.Suppressed, invitation.Metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryOutcome]);
        Assert.Equal(0, client.Calls);
    }

    [Fact]
    public void AmazonSesInvitationDeliveryRegistrationAddsDefaultAwsSdkClientAndDiagnostics()
    {
        var services = new ServiceCollection();
        services.AddCephalonAmazonSesInvitationDelivery(options =>
        {
            options.FromEmail = "invites@example.test";
            options.RegionSystemName = "us-east-1";
        });

        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IAmazonSimpleEmailServiceV2>());
        Assert.NotNull(provider.GetRequiredService<IAmazonSesInvitationDeliveryClient>());
        Assert.Contains(
            provider.GetServices<IDiagnosticsConventionContributor>(),
            contributor => contributor.DescribeDiagnosticsConvention().Source == "Cephalon.MultiTenancy.Governance.AmazonSesDelivery");
    }

    private sealed class RecordingAmazonSesInvitationDeliveryClient(AmazonSesInvitationDeliveryClientResult result) : IAmazonSesInvitationDeliveryClient
    {
        public List<AmazonSesInvitationDeliveryMessage> Messages { get; } = [];

        public ValueTask<AmazonSesInvitationDeliveryClientResult> SendAsync(
            AmazonSesInvitationDeliveryMessage message,
            CancellationToken cancellationToken = default)
        {
            Messages.Add(message);
            return ValueTask.FromResult(result);
        }
    }

    private sealed class ThrowingAmazonSesInvitationDeliveryClient : IAmazonSesInvitationDeliveryClient
    {
        public int Calls { get; private set; }

        public ValueTask<AmazonSesInvitationDeliveryClientResult> SendAsync(
            AmazonSesInvitationDeliveryMessage message,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            throw new InvalidOperationException("Unexpected Amazon SES dispatch.");
        }
    }
}
