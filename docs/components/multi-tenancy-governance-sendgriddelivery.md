# Cephalon.MultiTenancy.Governance.SendGridDelivery

`Cephalon.MultiTenancy.Governance.SendGridDelivery` is the optional SendGrid Mail Send API sender companion for tenant-invitation delivery.

## What it owns

- one provider-managed `ITenantInvitationDeliverySender` implementation for SendGrid Mail Send API handoff
- configuration-driven setup through `Engine:MultiTenancy:Governance:SendGridInvitationDelivery`
- code-first setup through `AddCephalonSendGridInvitationDelivery(...)`
- a replaceable `ISendGridInvitationDeliveryClient` seam so hosts can test, wrap, or replace the default HTTP client
- recipient email resolution from dispatch metadata, invitation metadata, or `InviteeKind = email`
- plain-text and optional HTML message templates with bounded Cephalon placeholders
- SendGrid Mail Send payload construction for `personalizations`, `from`, `subject`, `content`, optional `headers`, optional `categories`, optional `custom_args`, and optional sandbox mode
- deterministic Cephalon message ids carried through SendGrid custom arguments and safe context headers
- provider message id capture from the SendGrid `X-Message-ID` response header by default
- safe sender metadata such as endpoint host, SendGrid status code, sandbox posture, Cephalon message id, sender id, recipient email, category count, custom-argument count, and client outcome reason
- stable diagnostics for accepted and failed SendGrid invitation dispatch attempts

## Main surfaces

- `Configuration/SendGridInvitationDeliveryOptions.cs`
- `Hosting/SendGridInvitationDeliveryServiceCollectionExtensions.cs`
- `Services/ISendGridInvitationDeliveryClient.cs`
- `Services/SendGridInvitationDeliveryClientResult.cs`
- `Services/SendGridInvitationDeliveryMessage.cs`
- `Services/SendGridInvitationDeliverySender.cs`
- `Services/SendGridInvitationDeliveryDiagnosticsConventionContributor.cs`

## Source structure

- `Configuration`
- `Hosting`
- `Services`

## How it fits

The core `Cephalon.MultiTenancy.Governance` package owns the host-agnostic invitation delivery dispatcher, run catalog, outcome recording, retry queue, and `ITenantInvitationDeliverySender` extension point. This companion package supplies a real SendGrid Mail Send sender for teams that want Cephalon to hand a prepared invitation email to SendGrid without writing a custom sender first.

Register the governance pack, then register the SendGrid sender:

```csharp
builder.Services.AddCephalonSendGridInvitationDelivery(builder.Configuration);

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
        "SendGridInvitationDelivery": {
          "Enabled": true,
          "SenderId": "sendgrid-email",
          "BaseUrl": "https://api.sendgrid.com",
          "ApiKey": "${SENDGRID_API_KEY}",
          "FromEmail": "noreply@example.com",
          "FromName": "Example SaaS",
          "RecipientEmailMetadataKey": "email",
          "SupportedChannels": ["email"],
          "SubjectTemplate": "Invitation for {tenantId}",
          "TextBodyTemplate": "You have been invited to tenant {tenantId}. Invitation: {invitationId}. Roles: {roles}.",
          "HtmlBodyTemplate": "<p>You have been invited to tenant <strong>{tenantId}</strong>.</p>",
          "Categories": ["cephalon-invitation"],
          "CustomArgs": {
            "product": "example-saas"
          },
          "Headers": {
            "X-Product": "Example SaaS"
          },
          "EnableSandboxMode": false,
          "ProviderMessageIdHeaderName": "X-Message-ID",
          "AcceptedStatusCodes": [202]
        }
      }
    }
  }
}
```

Set `BaseUrl` to `https://api.eu.sendgrid.com` for SendGrid EU regional sending. When `EnableSandboxMode` is enabled, the sender adds `mail_settings.sandbox_mode.enable = true` and treats SendGrid's validation `200 OK` response as accepted in addition to the normal `202 Accepted` response.

The sender returns `dispatched` only when the SendGrid API accepts the Mail Send request. Unsupported channels are reported as `suppressed`; invalid recipient resolution, HTTP errors, non-accepted status codes, and timeouts are reported as `sender-failed`. The governance dispatcher persists those outcomes through the invitation store, queues retryable sender failures when the retry queue is enabled, and keeps `externalDeliveryOwnership = provider-managed` when this sender handled the attempt.

This package intentionally owns SendGrid Mail Send API handoff only. It does not own SendGrid Event Webhook callback translation, SendGrid webhook signature verification, bounce handling, provider polling, dynamic-template lifecycle management, Mailgun, SES, Microsoft Graph, SMS, chat, CRM, identity-provider onboarding, distributed retry queues, cross-node leases, public onboarding, or tenant-admin UI. Those should remain application-managed or future provider-specific companion packs until a package owns them explicitly.

## Related docs

- [Cephalon.MultiTenancy](multi-tenancy.md)
- [Cephalon.MultiTenancy.Governance](multi-tenancy-governance.md)
- [Cephalon.MultiTenancy.Governance.AspNetCore](multi-tenancy-governance-aspnetcore.md)
- [Cephalon.MultiTenancy.Governance.HttpDelivery](multi-tenancy-governance-httpdelivery.md)
- [Cephalon.MultiTenancy.Governance.SmtpDelivery](multi-tenancy-governance-smtpdelivery.md)
- [Technology packs](../technology-packs.md)
- [Operations](../operations.md)
