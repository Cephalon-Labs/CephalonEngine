# Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore

`Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore` is the optional ASP.NET Core SendGrid Event Webhook callback translator for tenant-invitation delivery status reconciliation.

## What it owns

- opt-in `POST /engine/tenant-invitations/delivery-status/sendgrid` endpoint mapping
- fail-closed ASP.NET Core authorization by default, with an optional policy override
- SendGrid Event Webhook JSON-array parsing with bounded request size and event count
- translation from SendGrid deliverability events into `TenantInvitationDeliveryStatusReconciliationRequest`
- Cephalon context extraction from SendGrid custom arguments such as `cephalonTenantId`, `cephalonInvitationId`, `cephalonDeliveryChannel`, `cephalonSenderId`, and `cephalonCorrelationId`
- `sg_message_id` correlation back to the stored SendGrid `X-Message-ID` provider message id by using the prefix before the first dot by default
- safe status metadata such as SendGrid event id, message id, event type, status, bounce type, reason, event timestamp, and translation ownership
- observation-id seeding from `sg_event_id` through the normalized delivery-status observation store
- optional engagement-event mapping when a host deliberately sets `MapEngagementEventsAsDelivered`
- runtime truth through the `tenant-invitation-delivery-sendgrid-status-callbacks` technology surface
- stable diagnostics for accepted SendGrid callback payloads

## Main surfaces

- `Configuration/SendGridInvitationDeliveryAspNetCoreOptions.cs`
- `Hosting/SendGridInvitationDeliveryAspNetCoreServiceCollectionExtensions.cs`
- `Hosting/SendGridInvitationDeliveryStatusEndpointRouteBuilderExtensions.cs`
- `Hosting/SendGridInvitationDeliveryStatusCallbackResult.cs`
- `Hosting/SendGridInvitationDeliveryStatusCallbackEventResult.cs`

## Source structure

- `Configuration`
- `Hosting`
- `Services`

## How it fits

`Cephalon.MultiTenancy.Governance.SendGridDelivery` owns outbound Mail Send API handoff. It sends Cephalon context through SendGrid `custom_args` and captures SendGrid's `X-Message-ID` response header as the dispatch provider message id. Twilio SendGrid's Event Webhook later posts event arrays with fields such as `sg_event_id`, `sg_message_id`, `event`, `timestamp`, and the same custom arguments. This package bridges those provider payloads back into Cephalon's existing `ITenantInvitationDeliveryStatusReconciler` without putting SendGrid-specific HTTP routes into the host-agnostic governance core.

Register the package beside governance and map the endpoint explicitly:

```csharp
builder.Services.AddCephalonSendGridInvitationDeliveryAspNetCore(builder.Configuration);

builder.AddCephalon(engine =>
{
    engine.AddMultiTenancyGovernance();
});

var app = builder.Build();
app.MapCephalonSendGridInvitationDeliveryStatusCallbacks();
```

Configuration example:

```json
{
  "Engine": {
    "MultiTenancy": {
      "Governance": {
        "SendGridInvitationDelivery": {
          "AspNetCore": {
            "EnableStatusCallbackEndpoint": true,
            "StatusCallbackRoutePattern": "/engine/tenant-invitations/delivery-status/sendgrid",
            "RequireStatusCallbackAuthorization": true,
            "StatusCallbackAuthorizationPolicy": "sendgrid-event-webhook",
            "ExcludeStatusCallbackEndpointFromDescription": true,
            "RequireProviderMessageMatch": true,
            "RecordStatus": true,
            "Source": "sendgrid-event-webhook",
            "Actor": "sendgrid",
            "MaxRequestBodyBytes": 262144,
            "MaxEventsPerRequest": 1000,
            "MapEngagementEventsAsDelivered": false,
            "NormalizeProviderMessageIdFromSgMessageId": true
          }
        }
      }
    }
  }
}
```

Mapped statuses are intentionally narrow. `processed` becomes `accepted`, `delivered` becomes `delivered`, `deferred` becomes `deferred`, `bounce` becomes `bounced`, and `dropped`, `spamreport`, `unsubscribe`, and `group_unsubscribe` become `suppressed`. `open` and `click` are skipped by default because they are engagement events rather than delivery status events; set `MapEngagementEventsAsDelivered` only when that is an explicit product decision.

The endpoint returns `SendGridInvitationDeliveryStatusCallbackResult` with aggregate counts and per-event translation results. Events without Cephalon custom arguments are skipped without leaking recipient email addresses in the response. Translated events still go through the host-agnostic reconciler, so invitation existence, provider-message matching, status recording, and observation storage keep using the same governance rules as normalized callbacks.

This package does not verify SendGrid's signed Event Webhook signature yet. SendGrid-specific cryptographic verification depends on raw request bytes plus `X-Twilio-Email-Event-Webhook-Signature` and `X-Twilio-Email-Event-Webhook-Timestamp`, so it remains an explicit follow-up surface instead of being implied by this translator. Hosts can protect the endpoint today with ASP.NET Core authorization, gateway verification, or SendGrid OAuth token validation. Durable callback inboxes, distributed replay protection, provider polling, bounce orchestration beyond status translation, dynamic-template lifecycle management, Mailgun, SES, Microsoft Graph, SMS, chat, CRM, identity-provider onboarding, public onboarding, tenant-admin UI, and distributed/provider-backed governance stores remain later provider-pack or application-owned work.

## Provider references

- [Twilio SendGrid Event Webhook overview](https://www.twilio.com/docs/sendgrid/for-developers/tracking-events/twilio-sendgrid-event-webhook-overview)
- [Twilio SendGrid Event Webhook reference](https://www.twilio.com/docs/sendgrid/for-developers/tracking-events/event)
- [Twilio SendGrid X-Message-ID](https://www.twilio.com/docs/sendgrid/glossary/x-message-id)
- [Twilio SendGrid Event Webhook security features](https://www.twilio.com/docs/sendgrid/for-developers/tracking-events/getting-started-event-webhook-security-features)

## Related docs

- [Cephalon.MultiTenancy.Governance](multi-tenancy-governance.md)
- [Cephalon.MultiTenancy.Governance.AspNetCore](multi-tenancy-governance-aspnetcore.md)
- [Cephalon.MultiTenancy.Governance.SendGridDelivery](multi-tenancy-governance-sendgriddelivery.md)
- [Technology packs](../technology-packs.md)
- [Operations](../operations.md)
