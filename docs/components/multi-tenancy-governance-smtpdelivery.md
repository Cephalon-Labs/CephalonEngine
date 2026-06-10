# Cephalon.MultiTenancy.Governance.SmtpDelivery

> **Maturity:** `M2` · **Ownership:** cephalon-managed · **Family:** `multi-tenancy` · See [audit](../engine-surface-maturity-audit.md), [matrix](../conformance-matrix.md).

`Cephalon.MultiTenancy.Governance.SmtpDelivery` is the optional SMTP relay sender companion for tenant-invitation delivery.

## What it owns

- one provider-managed `ITenantInvitationDeliverySender` implementation for SMTP relay delivery
- configuration-driven setup through `Engine:MultiTenancy:Governance:SmtpInvitationDelivery`
- code-first setup through `AddCephalonSmtpInvitationDelivery(...)`
- a replaceable `ISmtpInvitationDeliveryClient` seam so hosts can test, wrap, or replace the default SMTP relay client
- deterministic SMTP `Message-Id` generation based on tenant, invitation, channel, and sender boundaries
- recipient email resolution from dispatch metadata, invitation metadata, or `InviteeKind = email`
- plain-text and optional HTML message templates with bounded Cephalon placeholders
- safe SMTP headers for tenant, invitation, channel, sender, and correlation context
- safe sender metadata such as relay host, port, TLS posture, message id, sender id, recipient address, and client outcome reason
- stable diagnostics for accepted and failed SMTP invitation dispatch attempts

## Main surfaces

- `Configuration/SmtpInvitationDeliveryOptions.cs`
- `Hosting/SmtpInvitationDeliveryServiceCollectionExtensions.cs`
- `Services/ISmtpInvitationDeliveryClient.cs`
- `Services/SmtpInvitationDeliveryClientResult.cs`
- `Services/SmtpInvitationDeliveryMessage.cs`
- `Services/SmtpInvitationDeliverySender.cs`
- `Services/SmtpInvitationDeliveryDiagnosticsConventionContributor.cs`

## Source structure

- `Configuration`
- `Hosting`
- `Services`

## How it fits

The core `Cephalon.MultiTenancy.Governance` package owns the host-agnostic invitation delivery dispatcher, run catalog, outcome recording, retry queue, and `ITenantInvitationDeliverySender` extension point. This companion package supplies a real SMTP relay sender for teams that want Cephalon to hand a prepared invitation email to an SMTP server without writing a custom sender first.

Register the governance pack, then register the SMTP sender:

```csharp
builder.Services.AddCephalonSmtpInvitationDelivery(builder.Configuration);

builder.AddCephalon(engine =>
{
    engine.AddMultiTenancyGovernance();
});
```

Configuration example:

```json
{
  "Engine": {
    "MultiTenancy": {
      "Governance": {
        "SmtpInvitationDelivery": {
          "Enabled": true,
          "SenderId": "smtp-email",
          "Host": "smtp.internal.example",
          "Port": 587,
          "UseSsl": true,
          "UserName": "${SMTP_USER}",
          "Password": "${SMTP_PASSWORD}",
          "FromAddress": "noreply@example.com",
          "FromDisplayName": "Example SaaS",
          "RecipientAddressMetadataKey": "email",
          "SupportedChannels": ["email"],
          "MessageIdDomain": "mail.example.com",
          "SubjectTemplate": "Invitation for {tenantId}",
          "TextBodyTemplate": "You have been invited to tenant {tenantId}. Invitation: {invitationId}. Roles: {roles}.",
          "HtmlBodyTemplate": "<p>You have been invited to tenant <strong>{tenantId}</strong>.</p>",
          "Headers": {
            "X-Product": "Example SaaS"
          }
        }
      }
    }
  }
}
```

The sender returns `dispatched` only when the SMTP client reports that the relay accepted the message. Unsupported channels are reported as `suppressed`; invalid recipient resolution, relay errors, and timeouts are reported as `sender-failed`. The governance dispatcher persists those outcomes through the invitation store, queues retryable sender failures when the retry queue is enabled, and keeps `externalDeliveryOwnership = provider-managed` when this sender handled the attempt.

This package intentionally owns SMTP relay handoff only. It does not own SendGrid Mail Send API handoff, Mailgun Messages API handoff, Amazon SES v2 handoff, Microsoft Graph `sendMail` handoff, SMS, chat, CRM, identity-provider onboarding, bounce/webhook translation, provider polling, distributed retry queues, cross-node leases, public onboarding, or tenant-admin UI. Mailgun Messages API handoff lives in `Cephalon.MultiTenancy.Governance.MailgunDelivery`, SendGrid Mail Send handoff lives in `Cephalon.MultiTenancy.Governance.SendGridDelivery`, Amazon SES v2 handoff lives in `Cephalon.MultiTenancy.Governance.AmazonSesDelivery`, and Microsoft Graph `sendMail` handoff lives in `Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery`; additional provider-specific email API senders beyond the shipped SMTP/SendGrid/Mailgun/Amazon SES/Microsoft Graph set should remain application-managed or future provider-specific companion packs until a package owns them explicitly.

## Related docs

- [Cephalon.MultiTenancy](multi-tenancy.md)
- [Cephalon.MultiTenancy.Governance](multi-tenancy-governance.md)
- [Cephalon.MultiTenancy.Governance.AspNetCore](multi-tenancy-governance-aspnetcore.md)
- [Cephalon.MultiTenancy.Governance.HttpDelivery](multi-tenancy-governance-httpdelivery.md)
- [Cephalon.MultiTenancy.Governance.MailgunDelivery](multi-tenancy-governance-mailgundelivery.md)
- [Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery](multi-tenancy-governance-microsoftgraphdelivery.md)
- [Cephalon.MultiTenancy.Governance.SendGridDelivery](multi-tenancy-governance-sendgriddelivery.md)
- [Technology packs](../technology-packs.md)
- [Operations](../operations.md)
