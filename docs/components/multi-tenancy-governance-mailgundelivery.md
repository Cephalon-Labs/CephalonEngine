# Cephalon.MultiTenancy.Governance.MailgunDelivery

`Cephalon.MultiTenancy.Governance.MailgunDelivery` is the optional Mailgun Messages API sender companion for tenant-invitation delivery.

## What it owns

- one provider-managed `ITenantInvitationDeliverySender` implementation for Mailgun Messages API handoff
- configuration-driven setup through `Engine:MultiTenancy:Governance:MailgunInvitationDelivery`
- code-first setup through `AddCephalonMailgunInvitationDelivery(...)`
- a replaceable `IMailgunInvitationDeliveryClient` seam so hosts can test, wrap, or replace the default HTTP client
- recipient email resolution from dispatch metadata, invitation metadata, or `InviteeKind = email`
- plain-text and optional HTML message templates with bounded Cephalon placeholders
- Mailgun multipart Messages API payload construction for `from`, `to`, `subject`, `text`, optional `html`, optional `o:tag`, optional `o:testmode`, optional `h:*` headers, and optional `v:*` user variables
- deterministic Cephalon message ids carried through Mailgun user variables and safe context headers
- provider message id capture from the Mailgun JSON `id` response property by default
- safe sender metadata such as endpoint host, domain, Mailgun status code, test-mode posture, Cephalon message id, sender id, recipient email, tag count, variable count, header count, and client outcome reason
- stable diagnostics for accepted and failed Mailgun invitation dispatch attempts

## Main surfaces

- `Configuration/MailgunInvitationDeliveryOptions.cs`
- `Hosting/MailgunInvitationDeliveryServiceCollectionExtensions.cs`
- `Services/IMailgunInvitationDeliveryClient.cs`
- `Services/MailgunInvitationDeliveryClientResult.cs`
- `Services/MailgunInvitationDeliveryMessage.cs`
- `Services/MailgunInvitationDeliverySender.cs`
- `Services/MailgunInvitationDeliveryDiagnosticsConventionContributor.cs`

## Source structure

- `Configuration`
- `Hosting`
- `Services`

## How it fits

The core `Cephalon.MultiTenancy.Governance` package owns the host-agnostic invitation delivery dispatcher, run catalog, outcome recording, retry queue, and `ITenantInvitationDeliverySender` extension point. This companion package supplies a real Mailgun Messages API sender for teams that want Cephalon to hand a prepared invitation email to Mailgun without writing a custom sender first.

Register the governance pack, then register the Mailgun sender:

```csharp
builder.Services.AddCephalonMailgunInvitationDelivery(builder.Configuration);

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
        "MailgunInvitationDelivery": {
          "Enabled": true,
          "SenderId": "mailgun-email",
          "BaseUrl": "https://api.mailgun.net",
          "DomainName": "mg.example.com",
          "ApiKey": "${MAILGUN_API_KEY}",
          "FromEmail": "noreply@example.com",
          "FromName": "Example SaaS",
          "RecipientEmailMetadataKey": "email",
          "SupportedChannels": ["email"],
          "SubjectTemplate": "Invitation for {tenantId}",
          "TextBodyTemplate": "You have been invited to tenant {tenantId}. Invitation: {invitationId}. Roles: {roles}.",
          "HtmlBodyTemplate": "<p>You have been invited to tenant <strong>{tenantId}</strong>.</p>",
          "Tags": ["cephalon-invitation"],
          "Variables": {
            "product": "example-saas"
          },
          "Headers": {
            "X-Product": "Example SaaS"
          },
          "EnableTestMode": false,
          "ProviderMessageIdJsonPropertyName": "id",
          "AcceptedStatusCodes": [200]
        }
      }
    }
  }
}
```

Set `BaseUrl` to `https://api.eu.mailgun.net` for Mailgun EU regional sending. The sender always posts to `/v3/{DomainName}/messages` with multipart form data and Mailgun basic authentication.

The sender returns `dispatched` only when the Mailgun API accepts the Messages API request. Unsupported channels are reported as `suppressed`; invalid recipient resolution, HTTP errors, non-accepted status codes, and timeouts are reported as `sender-failed`. The governance dispatcher persists those outcomes through the invitation store, queues retryable sender failures when the retry queue is enabled, and keeps `externalDeliveryOwnership = provider-managed` when this sender handled the attempt.

This package intentionally owns Mailgun Messages API handoff only. Mailgun webhook payload translation, optional HMAC-SHA256 signed-webhook verification, bounded process-local replay-token rejection, and observation-store-backed event-id idempotency live in `Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore`; provider polling, durable callback inboxes, distributed replay protection, distributed event-id ledgers, SES or other provider-specific email API senders, SMS, chat, CRM, identity-provider onboarding, distributed retry queues, cross-node leases, public onboarding, and tenant-admin UI remain application-managed or future provider-specific companion work until a package owns them explicitly. SendGrid Mail Send handoff lives in `Cephalon.MultiTenancy.Governance.SendGridDelivery`; SMTP relay handoff lives in `Cephalon.MultiTenancy.Governance.SmtpDelivery`; Microsoft Graph `sendMail` handoff lives in `Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery`.

## Related docs

- [Cephalon.MultiTenancy](multi-tenancy.md)
- [Cephalon.MultiTenancy.Governance](multi-tenancy-governance.md)
- [Cephalon.MultiTenancy.Governance.AspNetCore](multi-tenancy-governance-aspnetcore.md)
- [Cephalon.MultiTenancy.Governance.HttpDelivery](multi-tenancy-governance-httpdelivery.md)
- [Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery](multi-tenancy-governance-microsoftgraphdelivery.md)
- [Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore](multi-tenancy-governance-mailgundelivery-aspnetcore.md)
- [Cephalon.MultiTenancy.Governance.SendGridDelivery](multi-tenancy-governance-sendgriddelivery.md)
- [Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore](multi-tenancy-governance-sendgriddelivery-aspnetcore.md)
- [Cephalon.MultiTenancy.Governance.SmtpDelivery](multi-tenancy-governance-smtpdelivery.md)
- [Technology packs](../technology-packs.md)
- [Operations](../operations.md)
