# Cephalon.MultiTenancy.Governance.AmazonSesDelivery

`Cephalon.MultiTenancy.Governance.AmazonSesDelivery` is the optional Amazon SES v2 sender companion for tenant-invitation delivery.

## What it owns

- one provider-managed `ITenantInvitationDeliverySender` implementation for Amazon SES v2 `SendEmail` handoff
- configuration-driven setup through `Engine:MultiTenancy:Governance:AmazonSesInvitationDelivery`
- code-first setup through `AddCephalonAmazonSesInvitationDelivery(...)`
- a replaceable `IAmazonSesInvitationDeliveryClient` seam so hosts can test, wrap, or replace the default AWS SDK client
- default `IAmazonSimpleEmailServiceV2` registration using the AWS SDK region/credential resolution chain, with optional `RegionSystemName`
- recipient email resolution from dispatch metadata, invitation metadata, or `InviteeKind = email`
- plain-text and optional HTML message templates with bounded Cephalon placeholders
- Amazon SES simple message payload construction for sender, recipient, subject, text body, optional HTML body, reply-to addresses, configuration set, and message tags
- deterministic Cephalon message ids carried through safe Amazon SES message tags
- safe sender metadata such as region, configuration set, SES status code, SES message id, request id, Cephalon message id, sender id, recipient email, body content type, reply-to count, tag count, and client outcome reason
- safe Cephalon context tags that `Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore` can later translate from SNS-wrapped SES event publishing payloads
- stable diagnostics for accepted and failed Amazon SES invitation dispatch attempts

## Main surfaces

- `Configuration/AmazonSesInvitationDeliveryOptions.cs`
- `Hosting/AmazonSesInvitationDeliveryServiceCollectionExtensions.cs`
- `Services/IAmazonSesInvitationDeliveryClient.cs`
- `Services/AmazonSesInvitationDeliveryClientResult.cs`
- `Services/AmazonSesInvitationDeliveryMessage.cs`
- `Services/AmazonSesInvitationDeliverySender.cs`
- `Services/AmazonSesInvitationDeliveryDiagnosticsConventionContributor.cs`

## Source structure

- `Configuration`
- `Hosting`
- `Services`

## How it fits

The core `Cephalon.MultiTenancy.Governance` package owns the host-agnostic invitation delivery dispatcher, run catalog, outcome recording, retry queue, and `ITenantInvitationDeliverySender` extension point. This companion package supplies a real Amazon SES v2 sender for teams that want Cephalon to hand a prepared invitation email to SES without writing a custom sender first.

Register the governance pack, then register the Amazon SES sender:

```csharp
builder.Services.AddCephalonAmazonSesInvitationDelivery(builder.Configuration);

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
        "AmazonSesInvitationDelivery": {
          "Enabled": true,
          "SenderId": "amazon-ses-email",
          "RegionSystemName": "us-east-1",
          "ConfigurationSetName": "cephalon-invitations",
          "FromEmail": "noreply@example.com",
          "FromName": "Example SaaS",
          "ReplyToAddresses": ["support@example.com"],
          "RecipientEmailMetadataKey": "email",
          "SupportedChannels": ["email"],
          "SubjectTemplate": "Invitation for {tenantId}",
          "TextBodyTemplate": "You have been invited to tenant {tenantId}. Invitation: {invitationId}. Roles: {roles}.",
          "HtmlBodyTemplate": "<p>You have been invited to tenant <strong>{tenantId}</strong>.</p>",
          "Tags": {
            "product": "example-saas"
          },
          "IncludeContextTags": true,
          "AcceptedStatusCodes": [200]
        }
      }
    }
  }
}
```

When `RegionSystemName` is omitted, the AWS SDK default region resolution chain remains authoritative. Credentials are owned by the host and the AWS SDK credential chain; Cephalon does not record AWS credentials or message bodies in metadata.

The sender returns `dispatched` only when the AWS SDK reports that SES accepted the `SendEmail` request and returned a real SES message id. Unsupported channels are reported as `suppressed`; invalid recipient resolution, AWS service errors, non-accepted status codes, and timeouts are reported as `sender-failed`. The governance dispatcher persists those outcomes through the invitation store, queues retryable sender failures when the retry queue is enabled, and keeps `externalDeliveryOwnership = provider-managed` when this sender handled the attempt.

This package intentionally owns Amazon SES v2 `SendEmail` handoff only. SNS-wrapped SES delivery-status callback translation, SNS signature verification, process-local SNS replay protection, observation-store-backed SNS message-id idempotency, opt-in verified SNS subscription confirmation, and opt-in verified SNS unsubscribe-confirmation observation live in `Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore`. AWS account setup, identity/domain verification, IAM policy, DKIM/SPF/DMARC posture, SES sandbox movement, SES configuration-set event destination setup, SNS topic/subscription creation, automatic resubscribe/restore, subscription lifecycle governance, provider polling, durable callback inboxes, public onboarding, tenant-admin UI/backoffice, identity-provider sync, SMS, chat, and CRM delivery remain host-owned or future companion work until a package owns those paths explicitly. SendGrid Mail Send handoff lives in `Cephalon.MultiTenancy.Governance.SendGridDelivery`; Mailgun Messages API handoff lives in `Cephalon.MultiTenancy.Governance.MailgunDelivery`; Microsoft Graph `sendMail` handoff lives in `Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery`; SMTP relay handoff lives in `Cephalon.MultiTenancy.Governance.SmtpDelivery`.

## Related docs

- [Cephalon.MultiTenancy](multi-tenancy.md)
- [Cephalon.MultiTenancy.Governance](multi-tenancy-governance.md)
- [Cephalon.MultiTenancy.Governance.AspNetCore](multi-tenancy-governance-aspnetcore.md)
- [Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore](multi-tenancy-governance-amazonsesdelivery-aspnetcore.md)
- [Cephalon.MultiTenancy.Governance.HttpDelivery](multi-tenancy-governance-httpdelivery.md)
- [Cephalon.MultiTenancy.Governance.MailgunDelivery](multi-tenancy-governance-mailgundelivery.md)
- [Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery](multi-tenancy-governance-microsoftgraphdelivery.md)
- [Cephalon.MultiTenancy.Governance.SendGridDelivery](multi-tenancy-governance-sendgriddelivery.md)
- [Cephalon.MultiTenancy.Governance.SmtpDelivery](multi-tenancy-governance-smtpdelivery.md)
- [Technology packs](../technology-packs.md)
- [Operations](../operations.md)
