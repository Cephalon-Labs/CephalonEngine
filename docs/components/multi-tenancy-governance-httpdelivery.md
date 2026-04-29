# Cephalon.MultiTenancy.Governance.HttpDelivery

`Cephalon.MultiTenancy.Governance.HttpDelivery` is the optional HTTP webhook sender companion for tenant-invitation delivery.

## What it owns

- one provider-managed `ITenantInvitationDeliverySender` implementation for HTTP or HTTPS webhook endpoints
- configuration-driven setup through `Engine:MultiTenancy:Governance:HttpInvitationDelivery`
- code-first setup through `AddCephalonHttpInvitationDelivery(...)`
- bounded HTTP dispatch with configurable method, timeout, headers, accepted status codes, supported channels, and in-process retry/backoff for transient outcomes
- JSON invitation delivery payload shaping through `HttpInvitationDeliveryPayload`
- provider-neutral idempotency headers for receiver-side duplicate suppression across retry attempts
- optional HMAC-SHA256 webhook signing over the exact JSON body plus dispatch timestamp
- provider-message id capture from a configurable response header
- safe sender metadata such as HTTP endpoint host, status code, reason, idempotency posture/key source, signing enablement/key id, attempt count, retry posture/reason, optional bounded response body excerpt, and exception type
- stable diagnostics for accepted and failed HTTP invitation dispatch attempts

## Main surfaces

- `Configuration/HttpInvitationDeliveryOptions.cs`
- `Hosting/HttpInvitationDeliveryServiceCollectionExtensions.cs`
- `Services/HttpInvitationDeliveryPayload.cs`
- `Services/HttpInvitationDeliverySender.cs`
- `Services/HttpInvitationDeliveryDiagnosticsConventionContributor.cs`

## Source structure

- `Configuration`
- `Hosting`
- `Services`

## How it fits

The core `Cephalon.MultiTenancy.Governance` package owns the host-agnostic invitation delivery dispatcher, run catalog, outcome recording, and `ITenantInvitationDeliverySender` extension point. This companion package supplies a real sender implementation for teams that want the engine to POST invitation delivery requests into their own notification, workflow, CRM, or identity-onboarding webhook without writing a custom sender first. ASP.NET Core hosts can expose that same dispatcher through `MapCephalonTenantInvitationDeliveryDispatches()` when they want a fail-closed HTTP operator action instead of custom dispatch glue.

Register the governance pack, then register the HTTP sender:

```csharp
builder.Services.AddCephalonHttpInvitationDelivery(builder.Configuration);

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
        "HttpInvitationDelivery": {
          "Enabled": true,
          "SenderId": "http-webhook",
          "Endpoint": "https://notifications.internal.example/invitations",
          "Method": "POST",
          "TimeoutSeconds": 10,
          "MaxAttempts": 3,
          "RetryDelayMilliseconds": 250,
          "RetryStatusCodes": [408, 429, 500, 502, 503, 504],
          "RetryTransportFailures": true,
          "EnableIdempotencyHeader": true,
          "IdempotencyHeaderName": "X-Cephalon-Idempotency-Key",
          "IdempotencyMetadataKey": "idempotencyKey",
          "ExpectedStatusCodes": [202],
          "SupportedChannels": ["email", "webhook"],
          "SigningSecret": "${INVITATION_DELIVERY_SIGNING_SECRET}",
          "SigningKeyId": "primary-2026-04",
          "SignatureHeaderName": "X-Cephalon-Webhook-Signature",
          "SignatureTimestampHeaderName": "X-Cephalon-Webhook-Signature-Timestamp",
          "SignatureKeyIdHeaderName": "X-Cephalon-Webhook-Key-Id",
          "ProviderMessageIdHeaderName": "X-Cephalon-Provider-Message-Id",
          "Headers": {
            "X-Delivery-Key": "${INVITATION_DELIVERY_KEY}"
          }
        }
      }
    }
  }
}
```

When `SigningSecret` is configured, the sender serializes the payload once, computes `HMACSHA256(secret, "{unixTimestamp}.{jsonBody}")`, and sends the signature as `v1=<lowercase hex>` in `SignatureHeaderName`. The timestamp and optional key id are sent in their configured headers. Runtime metadata records only `httpSigned` and the optional `httpSigningKeyId`; it never records the shared secret or generated signature.

When `EnableIdempotencyHeader` is enabled, the sender sends `IdempotencyHeaderName` on every webhook attempt. If dispatch metadata contains `IdempotencyMetadataKey`, that header-safe value becomes the idempotency key; overlong or header-unsafe metadata values are deterministically hashed before being sent. When metadata is absent, the sender derives a stable hashed key from tenant id, invitation id, channel, and sender id. The derived key is safe to record and stays identical across retry attempts, while intentional provider-specific duplicate-handling semantics remain on the receiver.

When `MaxAttempts` is greater than 1, the sender retries non-accepted responses whose status code appears in `RetryStatusCodes`, and it retries transient `HttpRequestException` failures when `RetryTransportFailures` is enabled. Each attempt uses a fresh `HttpRequestMessage` over the same serialized payload, the fixed `RetryDelayMilliseconds` delay runs inside the configured timeout budget, and sender metadata records `httpAttemptCount`, `httpMaxAttempts`, `httpRetried`, `httpRetryReason`, and the active retry policy. This is an in-process resilience baseline for a single dispatch call, not a durable delivery queue.

The sender returns `dispatched` only when the webhook returns an accepted response according to `ExpectedStatusCodes`, or any successful 2xx response when no explicit status list is configured. Unsupported channels are reported as `suppressed`; transport errors, non-accepted responses, timeouts, and endpoint failures are reported as `sender-failed`. The governance dispatcher persists those outcomes through the invitation store and keeps `externalDeliveryOwnership = provider-managed` when this sender handled the attempt.

This is intentionally not a provider-specific email, SMS, chat, CRM, or identity-provider connector. Provider-specific authentication models, message templates, user provisioning, durable retry queues, provider-specific delivery-status callback translation, provider-specific callback signature verification, provider polling, human inboxes, and external provider reconciliation remain future companion or application-managed work until a package owns those paths explicitly. ASP.NET Core hosts that need an operator dispatch action can install `Cephalon.MultiTenancy.Governance.AspNetCore` and map `MapCephalonTenantInvitationDeliveryDispatches()` over the core dispatcher; hosts that need a generic normalized callback ingress can map `MapCephalonTenantInvitationDeliveryStatusCallbacks()` over the core reconciler. That adapter can verify provider-neutral HMAC signatures and reject bounded process-local signed replays for normalized callbacks, while the core governance pack can retain the resulting normalized observations in memory or an opt-in local JSON file. This sender package remains outbound-only.

## Related docs

- [Cephalon.MultiTenancy](multi-tenancy.md)
- [Cephalon.MultiTenancy.Governance](multi-tenancy-governance.md)
- [Cephalon.MultiTenancy.Governance.AspNetCore](multi-tenancy-governance-aspnetcore.md)
- [Technology packs](../technology-packs.md)
- [Operations](../operations.md)
