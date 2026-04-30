# Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery

`Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery` is the optional Microsoft Graph `sendMail` sender companion for tenant-invitation delivery.

## What it owns

- one provider-managed `ITenantInvitationDeliverySender` implementation for Microsoft Graph `sendMail` handoff
- configuration-driven setup through `Engine:MultiTenancy:Governance:MicrosoftGraphInvitationDelivery`
- code-first setup through `AddCephalonMicrosoftGraphInvitationDelivery(...)`
- a replaceable `IMicrosoftGraphInvitationDeliveryClient` seam so hosts can test, wrap, or replace the default HTTP client
- a replaceable `IMicrosoftGraphInvitationDeliveryAccessTokenProvider` seam so hosts can plug in Azure.Identity, managed identity, workload identity, token caches, or gateway-issued tokens
- first-party Azure.Identity token-provider integration through the optional `Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.AzureIdentity` companion
- recipient email resolution from dispatch metadata, invitation metadata, or `InviteeKind = email`
- plain-text and optional HTML message templates with bounded Cephalon placeholders
- Microsoft Graph JSON `sendMail` payload construction for subject, body, recipients, optional categories, optional custom `x-*` internet message headers, and `saveToSentItems`
- deterministic Cephalon message ids carried through `client-request-id` and safe custom message headers
- safe sender metadata such as endpoint host, API version, sender mailbox scope, Graph status code, request id, Cephalon message id, sender id, recipient email, body content type, category count, header count, token-provider posture, and client outcome reason
- stable diagnostics for accepted and failed Microsoft Graph invitation dispatch attempts

## Main surfaces

- `Configuration/MicrosoftGraphInvitationDeliveryOptions.cs`
- `Hosting/MicrosoftGraphInvitationDeliveryServiceCollectionExtensions.cs`
- `Services/IMicrosoftGraphInvitationDeliveryAccessTokenProvider.cs`
- `Services/IMicrosoftGraphInvitationDeliveryClient.cs`
- `Services/MicrosoftGraphInvitationDeliveryClientResult.cs`
- `Services/MicrosoftGraphInvitationDeliveryMessage.cs`
- `Services/MicrosoftGraphInvitationDeliverySender.cs`
- `Services/MicrosoftGraphInvitationDeliveryDiagnosticsConventionContributor.cs`

## Source structure

- `Configuration`
- `Hosting`
- `Services`

## How it fits

The core `Cephalon.MultiTenancy.Governance` package owns the host-agnostic invitation delivery dispatcher, run catalog, outcome recording, retry queue, and `ITenantInvitationDeliverySender` extension point. This companion package supplies a real Microsoft Graph `sendMail` sender for teams that want Cephalon to hand a prepared invitation email to Microsoft Graph without writing a custom sender first.

Register the governance pack, then register the Microsoft Graph sender:

```csharp
builder.Services.AddCephalonMicrosoftGraphInvitationDelivery(builder.Configuration);

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
        "MicrosoftGraphInvitationDelivery": {
          "Enabled": true,
          "SenderId": "microsoft-graph-email",
          "BaseUrl": "https://graph.microsoft.com",
          "ApiVersion": "v1.0",
          "SenderUserId": "invitations@example.com",
          "RecipientEmailMetadataKey": "email",
          "SupportedChannels": ["email"],
          "SubjectTemplate": "Invitation for {tenantId}",
          "TextBodyTemplate": "You have been invited to tenant {tenantId}. Invitation: {invitationId}. Roles: {roles}.",
          "HtmlBodyTemplate": "<p>You have been invited to tenant <strong>{tenantId}</strong>.</p>",
          "Categories": ["cephalon-invitation"],
          "Headers": {
            "x-product": "Example SaaS"
          },
          "SaveToSentItems": false,
          "AcceptedStatusCodes": [202]
        }
      }
    }
  }
}
```

Production hosts should usually register `IMicrosoftGraphInvitationDeliveryAccessTokenProvider` instead of storing a short-lived bearer token in configuration. The default `AccessToken` option exists for tests, local proofs, and controlled host-owned token handoff scenarios.

For Azure-hosted or Microsoft Entra-integrated apps, install `Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.AzureIdentity` and call:

```csharp
builder.Services.AddCephalonMicrosoftGraphInvitationDeliveryAzureIdentity(builder.Configuration);
```

That companion replaces the default static-token provider with an `Azure.Identity` provider that requests `https://graph.microsoft.com/.default` by default and can use managed identity, workload identity, environment credentials, Azure CLI, Azure PowerShell, or other `DefaultAzureCredential` chain entries according to Azure SDK behavior.

The sender posts JSON to `/v1.0/users/{SenderUserId}/sendMail` when `SenderUserId` is configured, or `/v1.0/me/sendMail` otherwise. It treats `202 Accepted` as dispatch handoff only: Microsoft Graph accepted the request, but downstream Exchange Online processing and recipient delivery remain provider-managed. The package records Graph request ids as safe metadata rather than pretending Graph returned a durable message id.

Unsupported channels are reported as `suppressed`; invalid recipient resolution, missing tokens, HTTP errors, non-accepted status codes, and timeouts are reported as `sender-failed`. The governance dispatcher persists those outcomes through the invitation store, queues retryable sender failures when the retry queue is enabled, and keeps `externalDeliveryOwnership = provider-managed` when this sender handled the attempt.

This package intentionally owns Microsoft Graph `sendMail` handoff only. Microsoft Entra app registration, permission consent, mailbox provisioning/access policy, Graph throttling policy beyond the bounded request timeout, delivery completion, provider polling, Graph change notifications, callback inboxes, public onboarding, tenant-admin UI/backoffice, identity-provider sync, and distributed/provider-backed governance stores remain host-owned or future companion work until a package owns those paths explicitly. Token acquisition through Azure.Identity lives in `Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.AzureIdentity`. Amazon SES v2 handoff lives in `Cephalon.MultiTenancy.Governance.AmazonSesDelivery`; SendGrid Mail Send handoff lives in `Cephalon.MultiTenancy.Governance.SendGridDelivery`; Mailgun Messages API handoff lives in `Cephalon.MultiTenancy.Governance.MailgunDelivery`; SMTP relay handoff lives in `Cephalon.MultiTenancy.Governance.SmtpDelivery`.

## Related docs

- [Cephalon.MultiTenancy](multi-tenancy.md)
- [Cephalon.MultiTenancy.Governance](multi-tenancy-governance.md)
- [Cephalon.MultiTenancy.Governance.AmazonSesDelivery](multi-tenancy-governance-amazonsesdelivery.md)
- [Cephalon.MultiTenancy.Governance.AspNetCore](multi-tenancy-governance-aspnetcore.md)
- [Cephalon.MultiTenancy.Governance.HttpDelivery](multi-tenancy-governance-httpdelivery.md)
- [Cephalon.MultiTenancy.Governance.MailgunDelivery](multi-tenancy-governance-mailgundelivery.md)
- [Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore](multi-tenancy-governance-mailgundelivery-aspnetcore.md)
- [Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.AzureIdentity](multi-tenancy-governance-microsoftgraphdelivery-azureidentity.md)
- [Cephalon.MultiTenancy.Governance.SendGridDelivery](multi-tenancy-governance-sendgriddelivery.md)
- [Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore](multi-tenancy-governance-sendgriddelivery-aspnetcore.md)
- [Cephalon.MultiTenancy.Governance.SmtpDelivery](multi-tenancy-governance-smtpdelivery.md)
- [Technology packs](../technology-packs.md)
- [Operations](../operations.md)
