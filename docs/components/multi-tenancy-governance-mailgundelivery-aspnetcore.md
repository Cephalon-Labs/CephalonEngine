# Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore

> **Maturity:** `M2` · **Ownership:** cephalon-managed · **Family:** `multi-tenancy` · See [audit](../engine-surface-maturity-audit.md), [matrix](../conformance-matrix.md).

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
- optional Mailgun HMAC-SHA256 webhook signature verification over `timestamp + token` when `RequireSignedWebhook` and `WebhookSigningKey` are configured
- Mailgun `parent-signature` verification for subaccount events when `AcceptParentSignature` remains enabled
- safe signed-webhook metadata such as verification outcome, algorithm, timestamp, age, signature field, signature fingerprint, and parent-signature posture without storing the signing key, raw signature, raw payload, or recipient email
- bounded process-local replay protection for verified signed webhook tokens when `EnableSignedWebhookReplayProtection` remains enabled
- safe replay metadata such as replay outcome, policy, key type, scope, durability, retention, cache limit, and token fingerprint without storing the raw token
- observation-store-backed Mailgun event-id idempotency when `EnableWebhookEventIdIdempotency` remains enabled and the governance observation store is enabled
- safe event-id idempotency metadata such as outcome, ownership, policy, key, scope, store kind, durability, and normalized observation id without storing raw webhook payloads or recipient email
- runtime truth through the `tenant-invitation-delivery-mailgun-status-callbacks` technology surface
- stable diagnostics for accepted Mailgun callback payloads, rejected signed-webhook verification attempts, rejected signed-webhook token replays, and duplicate Mailgun event-id skips

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
            "NormalizeProviderMessageIdWithAngleBrackets": true,
            "RequireSignedWebhook": true,
            "WebhookSigningKey": "${MAILGUN_WEBHOOK_SIGNING_KEY}",
            "SignedWebhookSignatureToleranceSeconds": 300,
            "AcceptParentSignature": true,
            "EnableSignedWebhookReplayProtection": true,
            "SignedWebhookReplayRetentionSeconds": 300,
            "SignedWebhookReplayCacheLimit": 4096,
            "EnableWebhookEventIdIdempotency": true
          }
        }
      }
    }
  }
}
```

Mapped statuses are intentionally narrow. `accepted` becomes `accepted`, `delivered` becomes `delivered`, `failed` with temporary severity becomes `deferred`, `failed` with permanent severity becomes `bounced`, other `failed` events become `failed`, and `complained` plus `unsubscribed` become `suppressed`. `opened` and `clicked` are skipped by default because they are engagement events rather than delivery status events; set `MapEngagementEventsAsDelivered` only when that is an explicit product decision.

The endpoint returns `MailgunInvitationDeliveryStatusCallbackResult` with aggregate counts and per-event translation results. Events without Cephalon tenant and invitation user variables are skipped without leaking recipient email addresses in the response. Duplicate Mailgun event ids increment `DuplicateEvents` and return per-event `duplicate-skipped` outcomes. Translated non-duplicate events still go through the host-agnostic reconciler, so invitation existence, provider-message matching, status recording, and observation storage keep using the same governance rules as normalized callbacks.

When `RequireSignedWebhook` is enabled, the endpoint verifies Mailgun's HMAC-SHA256 signature before translation or reconciliation. Verification uses the `signature.token`, `signature.timestamp`, and `signature.signature` values from Mailgun's signed JSON envelope, computes the lowercase hex digest over `timestamp + token` with `WebhookSigningKey`, enforces `SignedWebhookSignatureToleranceSeconds`, and fails closed with `401` for missing, malformed, stale, or invalid signatures. Mailgun subaccount events can also include `signature.parent-signature`; Cephalon accepts that field by default so hosts can validate subaccount events with the parent account signing key. Runtime metadata reports signature verification as `cephalon-managed` only when this option is enabled and keeps the signing key and raw signature out of runtime output.

When `RequireSignedWebhook` and `EnableSignedWebhookReplayProtection` are both enabled, verified Mailgun tokens are recorded as SHA-256 token fingerprints in a bounded process-local cache. A duplicate token inside `SignedWebhookReplayRetentionSeconds` is rejected with `409` before reconciliation and emits diagnostic `4570`. Runtime and reconciliation metadata report `mailgunWebhookReplayProtectionOwnership = cephalon-managed`, `mailgunWebhookReplayProtectionPolicy = signed-webhook-token`, `mailgunWebhookReplayProtectionKey = token-fingerprint`, `mailgunWebhookReplayProtectionScope = process-local`, and `mailgunWebhookReplayProtectionDurability = none` so operators can distinguish this from a durable or distributed inbox.

When `EnableWebhookEventIdIdempotency` is enabled and the governance observation store is enabled, the endpoint checks the normalized `mailgun:{event-data.id}` observation id before invoking the reconciler. Already observed ids are skipped with diagnostic `4571`, `DuplicateEvents`, and a per-event `duplicate-skipped` outcome. Runtime and reconciliation metadata report `mailgunWebhookEventIdIdempotencyOwnership = cephalon-managed`, `mailgunWebhookEventIdIdempotencyPolicy = mailgun-event-id`, `mailgunWebhookEventIdIdempotencyKey = event-data.id`, `mailgunWebhookEventIdIdempotencyScope = observation-store`, and store-dependent durability so operators can distinguish this guard from a durable callback inbox or distributed event ledger.

ASP.NET Core authorization is still enabled by default and can be combined with gateway policy, Mailgun TLS client-certificate checks, or other host controls. This baseline owns process-local Mailgun replay-token rejection plus observation-store-backed Mailgun event-id idempotency only; durable callback inboxes, distributed replay ledgers, distributed event-id ledgers, provider polling, and exactly-once delivery remain future provider-pack or application-owned work.

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
