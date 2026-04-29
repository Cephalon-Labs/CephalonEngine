# Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore

`Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore` is the optional ASP.NET Core Mailgun webhook callback translator for tenant-invitation delivery status reconciliation.

## What it owns

- opt-in `POST /engine/tenant-invitations/delivery-status/mailgun` endpoint mapping
- fail-closed ASP.NET Core authorization by default, with an optional policy override
- Mailgun webhook JSON-object parsing with bounded request size
- JSON-array parsing for controlled replay and test harness scenarios, bounded by event count
- translation from Mailgun delivery events into `TenantInvitationDeliveryStatusReconciliationRequest`
- Cephalon context extraction from top-level fields or Mailgun `user-variables`, including `cephalonTenantId`, `cephalonInvitationId`, `cephalonDeliveryChannel`, `cephalonSenderId`, and `cephalonCorrelationId`
- provider message-id correlation from `message.headers.message-id`, with angle-bracket normalization enabled by default to match the Mailgun Messages API sender's captured response id shape
- safe status metadata such as Mailgun event id, message id, event type, severity, reason, delivery-status details, timestamp, and test-mode posture
- observation-id seeding from Mailgun event ids through the normalized delivery-status observation path
- optional engagement-event mapping when a host deliberately sets `MapEngagementEventsAsDelivered`
- runtime truth through the `tenant-invitation-delivery-mailgun-status-callbacks` technology surface
- stable diagnostics for accepted Mailgun callback payloads

## Main surfaces

- `Configuration/MailgunInvitationDeliveryAspNetCoreOptions.cs`
- `Hosting/MailgunInvitationDeliveryAspNetCoreServiceCollectionExtensions.cs`
- `Hosting/MailgunInvitationDeliveryStatusEndpointRouteBuilderExtensions.cs`
- `Hosting/MailgunInvitationDeliveryStatusCallbackResult.cs`
- `Hosting/MailgunInvitationDeliveryStatusCallbackEventResult.cs`

## Source structure

- `Configuration`
- `Hosting`
- `Services`

## How it fits

`Cephalon.MultiTenancy.Governance.MailgunDelivery` owns outbound Messages API handoff. It sends Cephalon context through Mailgun `v:*` user variables and captures Mailgun's JSON `id` response property as the dispatch provider message id. Mailgun later posts webhook event objects with fields such as `event`, `id`, `timestamp`, `severity`, `message.headers.message-id`, `delivery-status`, and `user-variables`. This package bridges those provider payloads back into Cephalon's existing `ITenantInvitationDeliveryStatusReconciler` without putting Mailgun-specific HTTP routes into the host-agnostic governance core.

Register the package beside governance and map the endpoint explicitly:

```csharp
builder.Services.AddCephalonMailgunInvitationDeliveryAspNetCore(builder.Configuration);

builder.AddCephalon(engine =>
{
    engine.AddMultiTenancyGovernance();
});

var app = builder.Build();
app.MapCephalonMailgunInvitationDeliveryStatusCallbacks();
```

Configuration example:

```json
{
  "Engine": {
    "MultiTenancy": {
      "Governance": {
        "MailgunInvitationDelivery": {
          "AspNetCore": {
            "EnableStatusCallbackEndpoint": true,
            "StatusCallbackRoutePattern": "/engine/tenant-invitations/delivery-status/mailgun",
            "RequireStatusCallbackAuthorization": true,
            "StatusCallbackAuthorizationPolicy": "mailgun-webhook",
            "ExcludeStatusCallbackEndpointFromDescription": true,
            "RequireProviderMessageMatch": true,
            "RecordStatus": true,
            "Source": "mailgun-webhook",
            "Actor": "mailgun",
            "MaxRequestBodyBytes": 262144,
            "MaxEventsPerRequest": 1000,
            "MapEngagementEventsAsDelivered": false,
            "NormalizeProviderMessageIdWithAngleBrackets": true
          }
        }
      }
    }
  }
}
```

Mapped statuses are intentionally narrow. `accepted` becomes `accepted`, `delivered` becomes `delivered`, `failed` with temporary severity becomes `deferred`, `failed` with permanent severity becomes `bounced`, other `failed` events become `failed`, and `complained` plus `unsubscribed` become `suppressed`. `opened` and `clicked` are skipped by default because they are engagement events rather than delivery status events; set `MapEngagementEventsAsDelivered` only when that is an explicit product decision.

The endpoint returns `MailgunInvitationDeliveryStatusCallbackResult` with aggregate counts and per-event translation results. Events without Cephalon tenant and invitation user variables are skipped without leaking recipient email addresses in the response. Translated events still go through the host-agnostic reconciler, so invitation existence, provider-message matching, status recording, and observation storage keep using the same governance rules as normalized callbacks.

ASP.NET Core authorization is enabled by default and can be combined with gateway policy, Mailgun webhook signing at the edge, or other host controls. This baseline does not verify Mailgun's HMAC-SHA256 webhook signature, does not protect Mailgun replay tokens, and does not own durable callback inboxes or provider polling. Runtime metadata reports signature verification and replay protection as `not-configured` so operators do not confuse payload translation with a complete provider-authenticity boundary.

## Provider references

- [Mailgun webhooks](https://documentation.mailgun.com/docs/mailgun/user-manual/webhooks/webhooks)
- [Securing Mailgun webhooks](https://documentation.mailgun.com/docs/mailgun/user-manual/webhooks/securing-webhooks)

## Related docs

- [Cephalon.MultiTenancy.Governance](multi-tenancy-governance.md)
- [Cephalon.MultiTenancy.Governance.AspNetCore](multi-tenancy-governance-aspnetcore.md)
- [Cephalon.MultiTenancy.Governance.MailgunDelivery](multi-tenancy-governance-mailgundelivery.md)
- [Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore](multi-tenancy-governance-sendgriddelivery-aspnetcore.md)
- [Cephalon.MultiTenancy.Governance.SendGridDelivery](multi-tenancy-governance-sendgriddelivery.md)
- [Technology packs](../technology-packs.md)
- [Operations](../operations.md)
