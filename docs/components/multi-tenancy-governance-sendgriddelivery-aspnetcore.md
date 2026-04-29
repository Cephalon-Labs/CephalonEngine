# Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore

`Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore` is the optional ASP.NET Core SendGrid Event Webhook callback translator for tenant-invitation delivery status reconciliation.

## What it owns

- opt-in `POST /engine/tenant-invitations/delivery-status/sendgrid` endpoint mapping
- fail-closed ASP.NET Core authorization by default, with an optional policy override
- SendGrid Event Webhook JSON-array parsing with bounded request size and event count
- translation from SendGrid deliverability events into `TenantInvitationDeliveryStatusReconciliationRequest`
- Cephalon context extraction from SendGrid custom arguments such as `cephalonTenantId`, `cephalonInvitationId`, `cephalonDeliveryChannel`, `cephalonSenderId`, and `cephalonCorrelationId`
- `sg_message_id` correlation back to the stored SendGrid `X-Message-ID` provider message id by using the prefix before the first dot by default
- optional SendGrid signed Event Webhook verification with ECDSA-SHA256 over `X-Twilio-Email-Event-Webhook-Timestamp` plus the exact raw request body bytes
- bounded process-local replay protection for verified SendGrid signed Event Webhook callbacks, keyed by safe signature fingerprints
- observation-store-backed SendGrid event-id idempotency that skips duplicate translated `sg_event_id` observations before reconciliation
- safe status metadata such as SendGrid event id, message id, event type, status, bounce type, reason, event timestamp, and translation ownership
- safe signed-webhook and idempotency metadata such as verification outcome, replay outcome, event-id idempotency outcome, algorithm, timestamp, age, signature fingerprint, and observation id without storing the public key, raw signature, raw payload, or recipient email
- observation-id seeding from `sg_event_id` through the normalized delivery-status observation store
- optional engagement-event mapping when a host deliberately sets `MapEngagementEventsAsDelivered`
- runtime truth through the `tenant-invitation-delivery-sendgrid-status-callbacks` technology surface
- stable diagnostics for accepted SendGrid callback payloads, rejected signed-webhook verification attempts, rejected signed-callback replays, and duplicate event-id skips

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
            "NormalizeProviderMessageIdFromSgMessageId": true,
            "RequireSignedEventWebhook": true,
            "SignedEventWebhookPublicKey": "${SENDGRID_EVENT_WEBHOOK_PUBLIC_KEY}",
            "SignedEventWebhookSignatureHeaderName": "X-Twilio-Email-Event-Webhook-Signature",
            "SignedEventWebhookTimestampHeaderName": "X-Twilio-Email-Event-Webhook-Timestamp",
            "SignedEventWebhookSignatureToleranceSeconds": 300,
            "EnableSignedEventWebhookReplayProtection": true,
            "SignedEventWebhookReplayRetentionSeconds": 300,
            "SignedEventWebhookReplayCacheLimit": 4096,
            "EnableEventWebhookEventIdIdempotency": true
          }
        }
      }
    }
  }
}
```

Mapped statuses are intentionally narrow. `processed` becomes `accepted`, `delivered` becomes `delivered`, `deferred` becomes `deferred`, `bounce` becomes `bounced`, and `dropped`, `spamreport`, `unsubscribe`, and `group_unsubscribe` become `suppressed`. `open` and `click` are skipped by default because they are engagement events rather than delivery status events; set `MapEngagementEventsAsDelivered` only when that is an explicit product decision.

The endpoint returns `SendGridInvitationDeliveryStatusCallbackResult` with aggregate counts and per-event translation results. Events without Cephalon custom arguments are skipped without leaking recipient email addresses in the response. Translated events still go through the host-agnostic reconciler, so invitation existence, provider-message matching, status recording, and observation storage keep using the same governance rules as normalized callbacks.

When `RequireSignedEventWebhook` is enabled, the endpoint verifies the SendGrid ECDSA-SHA256 signature before JSON parsing or reconciliation. Verification uses `X-Twilio-Email-Event-Webhook-Signature`, `X-Twilio-Email-Event-Webhook-Timestamp`, and the exact raw request body bytes because re-serialized JSON can change the signed bytes. Missing public keys are treated as host misconfiguration, while missing, malformed, stale, or invalid request signatures fail closed with `401`. Runtime metadata reports signature verification as `cephalon-managed` only when this option is enabled and keeps the public key out of runtime output.

When signed verification is required, `EnableSignedEventWebhookReplayProtection` is enabled by default. The endpoint records the verified signature fingerprint in a bounded process-local cache, returns `409` for a duplicate signed request inside `SignedEventWebhookReplayRetentionSeconds`, and reports replay policy/key/scope/durability/retention/cache posture through callback results, reconciliation metadata, diagnostics `4564`, and the runtime surface. The cache is intentionally non-durable and per process; it reduces accidental or malicious local replays without claiming cross-node replay protection or exactly-once delivery.

When `EnableEventWebhookEventIdIdempotency` is enabled and the governance delivery-status observation store is enabled, each translated event with `sg_event_id` uses its stable `sendgrid:{sg_event_id}` observation id as an idempotency key against `ITenantInvitationDeliveryStatusObservationStore`. A duplicate event id is skipped before the reconciler is invoked, returns `200` with `DuplicateEvents` plus a per-event `duplicate-skipped` outcome, emits diagnostic `4565`, and reports `cephalon-managed` event-id idempotency posture through reconciliation metadata and the runtime surface. Durability follows the configured observation store: in-memory stores are process-local, while the built-in file store can persist observed ids locally. If the observation store is disabled, this idempotency lane reports `not-configured`.

ASP.NET Core authorization is still enabled by default and can be combined with SendGrid OAuth, gateway policy, or other host controls. Durable callback inboxes, distributed replay protection, distributed event-id ledgers, provider polling, bounce orchestration beyond status translation, dynamic-template lifecycle management, Mailgun, SES, Microsoft Graph, SMS, chat, CRM, identity-provider onboarding, public onboarding, tenant-admin UI, and distributed/provider-backed governance stores remain later provider-pack or application-owned work.

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
