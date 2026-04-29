using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Diagnostics;
using Cephalon.MultiTenancy.Governance.Registration;
using Cephalon.MultiTenancy.Governance.Services;
using Cephalon.MultiTenancy.Governance.SmtpDelivery.Hosting;
using Cephalon.MultiTenancy.Governance.SmtpDelivery.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Tests.Composition;

public sealed class MultiTenancyGovernanceSmtpDeliveryPackTests
{
    [Fact]
    public async Task SmtpInvitationDeliverySenderSendsMessageAndRecordsSafeMetadata()
    {
        var client = new RecordingSmtpInvitationDeliveryClient(new SmtpInvitationDeliveryClientResult(
            accepted: true,
            providerMessageId: "smtp-provider-293",
            reason: "relay accepted",
            metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["smtpRelaySession"] = "relay-session-293"
            }));

        var services = new ServiceCollection();
        services.AddSingleton<ISmtpInvitationDeliveryClient>(client);
        services.AddCephalonSmtpInvitationDelivery(options =>
        {
            options.Host = "smtp.example.test";
            options.Port = 2525;
            options.UseSsl = true;
            options.UserName = "smtp-user";
            options.Password = "smtp-secret";
            options.FromAddress = "noreply@example.test";
            options.FromDisplayName = "Cephalon Test";
            options.MessageIdDomain = "mail.example.test";
            options.SubjectTemplate = "Invite {displayName} to {tenantId}";
            options.TextBodyTemplate = "Invitation {invitationId} for {inviteeId}; roles={roles}; correlation={correlationId}";
            options.Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["X-Test-Delivery"] = "enabled",
                ["X-Unsafe"] = "bad\r\nheader"
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
                    invitationId: "invite-smtp",
                    tenantId: "tenant-smtp",
                    inviteeId: "owner@example.test",
                    inviteeKind: "email",
                    displayName: "SMTP Invitee",
                    roles: ["admin", "member"],
                    expiresAtUtc: new DateTimeOffset(2026, 05, 01, 0, 0, 0, TimeSpan.Zero)));
            });
        });

        await using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<ITenantInvitationDeliveryDispatcher>();
        var catalog = provider.GetRequiredService<ITenantInvitationCatalog>();
        var diagnosticsCatalog = provider.GetRequiredService<IRuntimeDiagnosticsCatalog>();

        var result = await dispatcher.DispatchAsync(new TenantInvitationDeliveryRequest(
            tenantId: "tenant-smtp",
            invitationId: "invite-smtp",
            channel: "email",
            senderId: "smtp-email",
            source: "smtp-delivery-test",
            actor: "operator-293",
            atUtc: new DateTimeOffset(2026, 04, 29, 9, 0, 0, TimeSpan.Zero),
            correlationId: "corr-smtp-293"));

        var captured = Assert.Single(client.Messages);
        var invitation = Assert.Single(catalog.Invitations);

        Assert.True(result.Dispatched);
        Assert.True(result.Recorded);
        Assert.Equal(TenantInvitationDeliveryOutcomes.Dispatched, result.Outcome);
        Assert.Equal("smtp-provider-293", result.ProviderMessageId);
        Assert.Equal("owner@example.test", captured.ToAddress);
        Assert.Equal("noreply@example.test", captured.FromAddress);
        Assert.Equal("Invite SMTP Invitee to tenant-smtp", captured.Subject);
        Assert.Contains("Invitation invite-smtp for owner@example.test", captured.TextBody, StringComparison.Ordinal);
        Assert.Contains("roles=admin, member", captured.TextBody, StringComparison.Ordinal);
        Assert.StartsWith("<cephalon-invitation-", captured.MessageId, StringComparison.Ordinal);
        Assert.EndsWith("@mail.example.test>", captured.MessageId, StringComparison.Ordinal);
        Assert.Equal("tenant-smtp", captured.Headers["X-Cephalon-Tenant-Id"]);
        Assert.Equal("invite-smtp", captured.Headers["X-Cephalon-Invitation-Id"]);
        Assert.Equal("corr-smtp-293", captured.Headers["X-Cephalon-Correlation-Id"]);
        Assert.Equal("enabled", captured.Headers["X-Test-Delivery"]);
        Assert.False(captured.Headers.ContainsKey("X-Unsafe"));
        Assert.Equal("smtp.example.test", result.Metadata["smtpRelayHost"]);
        Assert.Equal("2525", result.Metadata["smtpRelayPort"]);
        Assert.Equal("true", result.Metadata["smtpUseSsl"]);
        Assert.Equal("relay-session-293", result.Metadata["smtpRelaySession"]);
        Assert.Equal(captured.MessageId, result.Metadata["smtpMessageId"]);
        Assert.Equal("smtp-provider-293", invitation.Metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryProviderMessageId]);
        Assert.DoesNotContain("smtp-secret", result.Metadata.Values);
        Assert.DoesNotContain("smtp-secret", captured.TextBody, StringComparison.Ordinal);
        Assert.Contains(
            diagnosticsCatalog.Conventions,
            convention => convention.Source == "Cephalon.MultiTenancy.Governance.SmtpDelivery");
    }

    [Fact]
    public async Task SmtpInvitationDeliverySenderUsesRequestMetadataRecipientWhenInviteeIsNotEmail()
    {
        var client = new RecordingSmtpInvitationDeliveryClient(new SmtpInvitationDeliveryClientResult(accepted: true));
        var services = new ServiceCollection();
        services.AddSingleton<ISmtpInvitationDeliveryClient>(client);
        services.AddCephalonSmtpInvitationDelivery(options =>
        {
            options.Host = "smtp.example.test";
            options.FromAddress = "noreply@example.test";
            options.RecipientAddressMetadataKey = "recipientEmail";
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
                    invitationId: "invite-smtp-metadata",
                    tenantId: "tenant-smtp",
                    inviteeId: "user-293",
                    inviteeKind: "user",
                    displayName: "Metadata Recipient",
                    expiresAtUtc: new DateTimeOffset(2026, 05, 01, 0, 0, 0, TimeSpan.Zero)));
            });
        });

        await using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<ITenantInvitationDeliveryDispatcher>();

        var result = await dispatcher.DispatchAsync(new TenantInvitationDeliveryRequest(
            tenantId: "tenant-smtp",
            invitationId: "invite-smtp-metadata",
            channel: "email",
            senderId: "smtp-email",
            atUtc: new DateTimeOffset(2026, 04, 29, 9, 30, 0, TimeSpan.Zero),
            metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["recipientEmail"] = "metadata-recipient@example.test"
            }));

        var captured = Assert.Single(client.Messages);

        Assert.True(result.Dispatched);
        Assert.Equal("metadata-recipient@example.test", captured.ToAddress);
        Assert.Equal("recipientEmail", result.Metadata["smtpRecipientMetadataKey"]);
        Assert.Equal(captured.MessageId, result.ProviderMessageId);
    }

    [Fact]
    public async Task SmtpInvitationDeliverySenderSuppressesUnsupportedChannelWithoutClientCall()
    {
        var client = new RecordingSmtpInvitationDeliveryClient(new SmtpInvitationDeliveryClientResult(accepted: true));
        var services = new ServiceCollection();
        services.AddSingleton<ISmtpInvitationDeliveryClient>(client);
        services.AddCephalonSmtpInvitationDelivery(options =>
        {
            options.Host = "smtp.example.test";
            options.FromAddress = "noreply@example.test";
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
                    invitationId: "invite-smtp-suppressed",
                    tenantId: "tenant-smtp",
                    inviteeId: "owner@example.test",
                    inviteeKind: "email",
                    expiresAtUtc: new DateTimeOffset(2026, 05, 01, 0, 0, 0, TimeSpan.Zero)));
            });
        });

        await using var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<ITenantInvitationDeliveryDispatcher>();
        var catalog = provider.GetRequiredService<ITenantInvitationCatalog>();

        var result = await dispatcher.DispatchAsync(new TenantInvitationDeliveryRequest(
            tenantId: "tenant-smtp",
            invitationId: "invite-smtp-suppressed",
            channel: "sms",
            senderId: "smtp-email",
            atUtc: new DateTimeOffset(2026, 04, 29, 9, 45, 0, TimeSpan.Zero)));

        var invitation = Assert.Single(catalog.Invitations);

        Assert.False(result.Dispatched);
        Assert.True(result.Recorded);
        Assert.Equal(TenantInvitationDeliveryOutcomes.Suppressed, result.Outcome);
        Assert.Equal(TenantInvitationDeliveryOutcomes.Suppressed, invitation.Metadata[TenantInvitationDeliveryMetadataKeys.LastDeliveryOutcome]);
        Assert.Empty(client.Messages);
    }

    private sealed class RecordingSmtpInvitationDeliveryClient(SmtpInvitationDeliveryClientResult result) : ISmtpInvitationDeliveryClient
    {
        public List<SmtpInvitationDeliveryMessage> Messages { get; } = [];

        public ValueTask<SmtpInvitationDeliveryClientResult> SendAsync(
            SmtpInvitationDeliveryMessage message,
            CancellationToken cancellationToken = default)
        {
            Messages.Add(message);
            return ValueTask.FromResult(result);
        }
    }
}
