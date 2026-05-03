# Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.AzureIdentity

> **Maturity:** `M2` · **Ownership:** `cephalon-managed` — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.AzureIdentity` is the optional Azure Identity token-provider companion for the Microsoft Graph invitation sender.

## What it owns

- one `IMicrosoftGraphInvitationDeliveryAccessTokenProvider` implementation backed by `Azure.Core.TokenCredential`
- configuration-driven setup through `Engine:MultiTenancy:Governance:MicrosoftGraphInvitationDelivery:AzureIdentity`
- code-first setup through `AddCephalonMicrosoftGraphInvitationDeliveryAzureIdentity(...)`
- `DefaultAzureCredential` creation for configuration-first hosts
- explicit `TokenCredential` injection for tests, shared host credential factories, or custom credential selection
- Microsoft Graph scope selection with `https://graph.microsoft.com/.default` as the default
- optional tenant id, user-assigned managed identity client id, authority-host alias, and browser/managed-identity exclusion settings
- order-insensitive replacement of the Microsoft Graph sender's default static-token provider
- safe token-acquisition diagnostics that record credential type, scope count, expiry, and failure reason without logging token values

## Main surfaces

- `Configuration/MicrosoftGraphInvitationDeliveryAzureIdentityOptions.cs`
- `Hosting/MicrosoftGraphInvitationDeliveryAzureIdentityServiceCollectionExtensions.cs`
- `Services/MicrosoftGraphInvitationDeliveryAzureIdentityAccessTokenProvider.cs`
- `Services/MicrosoftGraphInvitationDeliveryAzureIdentityDiagnosticsConventionContributor.cs`

## Source structure

- `Configuration`
- `Hosting`
- `Services`

## How it fits

The `Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery` package owns the actual Graph `sendMail` handoff. This companion supplies the production-friendly token provider so consumer apps do not need to write the common Azure.Identity bridge themselves.

Register the Graph sender, then register the Azure Identity provider. The Azure Identity registration intentionally replaces the sender's static-token provider, so it also works when called before the Graph sender.

```csharp
builder.Services.AddCephalonMicrosoftGraphInvitationDelivery(builder.Configuration);
builder.Services.AddCephalonMicrosoftGraphInvitationDeliveryAzureIdentity(builder.Configuration);

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
          "SenderUserId": "invites@example.com",
          "AzureIdentity": {
            "Enabled": true,
            "Scopes": ["https://graph.microsoft.com/.default"],
            "TenantId": "00000000-0000-0000-0000-000000000000",
            "ManagedIdentityClientId": "11111111-1111-1111-1111-111111111111",
            "AuthorityHost": "AzurePublicCloud",
            "ExcludeInteractiveBrowserCredential": true,
            "ExcludeManagedIdentityCredential": false
          }
        }
      }
    }
  }
}
```

Production Microsoft Graph `sendMail` with application permissions normally uses a Microsoft Entra app or managed identity with Microsoft Graph `Mail.Send` granted and admin-consented. `SenderUserId` should identify the mailbox used by `/users/{id | userPrincipalName}/sendMail` for service-style application-permission hosts. The token provider requests scopes only; mailbox existence, Exchange Online access policy, Graph throttling, and downstream mail delivery remain Microsoft/host-owned operational responsibilities.

For tests or hosts with a shared credential factory, pass a credential explicitly:

```csharp
builder.Services.AddCephalonMicrosoftGraphInvitationDeliveryAzureIdentity(
    credential,
    options => options.Scopes = ["https://graph.microsoft.com/.default"]);
```

This package intentionally owns token acquisition only. It does not own Microsoft Entra application registration, permission consent, mailbox provisioning, Exchange application access policies, Graph `sendMail` payload construction, delivery completion after Graph accepts the request, Graph change notifications, provider polling, callback inboxes, public onboarding, tenant-admin UI/backoffice, identity-provider synchronization, or distributed retry infrastructure.

## Related docs

- [Cephalon.MultiTenancy](multi-tenancy.md)
- [Cephalon.MultiTenancy.Governance](multi-tenancy-governance.md)
- [Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery](multi-tenancy-governance-microsoftgraphdelivery.md)
- [Technology packs](../technology-packs.md)
- [Operations](../operations.md)
