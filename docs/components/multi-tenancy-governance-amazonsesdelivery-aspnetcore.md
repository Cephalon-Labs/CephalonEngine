# Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore

> **Maturity:** `M2` · **Ownership:** cephalon-managed · **Family:** `multi-tenancy` · See [audit](../engine-surface-maturity-audit.md), [matrix](../conformance-matrix.md).

`Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore` is the optional ASP.NET Core Amazon SES over SNS callback translator, SNS signature verifier, process-local SNS replay guard, observation-store-backed SNS message-id idempotency guard, verified SNS subscription-confirmation seam, and verified SNS unsubscribe-confirmation observation seam for tenant-invitation delivery status reconciliation.

## What it owns

- opt-in `POST /engine/tenant-invitations/delivery-status/amazon-ses` endpoint mapping
- fail-closed ASP.NET Core authorization by default, with an optional policy override
- Amazon SNS HTTP notification JSON parsing with bounded request size
- SNS `Notification` payload unwrapping where `Message` contains an Amazon SES event publishing record
- raw SES event object or array parsing for controlled replay and test harness scenarios when `AcceptRawSesEventPayloads` remains enabled
- opt-in SNS signature verification before translation through `RequireSnsSignatureVerification`
- SNS canonical string-to-sign construction for `Notification`, `SubscriptionConfirmation`, and `UnsubscribeConfirmation` envelopes
- SNS `SignatureVersion` 2 enforcement by default, with explicit legacy opt-out for version 1
- SNS `TopicArn` allow-list enforcement by default through `AllowedSnsTopicArns`
- SNS signing-certificate URL validation for HTTPS Amazon SNS PEM URLs, optional pinned PEM certificate loading for controlled tests, and certificate validity/chain validation by default
- bounded process-local replay protection for verified SNS callbacks through `EnableSnsReplayProtection`, `SnsReplayRetentionSeconds`, and `SnsReplayCacheLimit`
- observation-store-backed duplicate skipping for translated SNS `MessageId` values through `EnableSnsMessageIdIdempotency`
- opt-in verified SNS `SubscriptionConfirmation` handling through `EnableSnsSubscriptionConfirmation` and replaceable `IAmazonSesSnsSubscriptionConfirmationClient`
- opt-in verified SNS `UnsubscribeConfirmation` observation through `EnableSnsUnsubscribeConfirmationObservation` without invoking `SubscribeURL`
- translation from Amazon SES `eventType` or legacy `notificationType` values into `TenantInvitationDeliveryStatusReconciliationRequest`
- Cephalon context extraction from SES `mail.tags`, including `cephalon-tenant-id`, `cephalon-invitation-id`, `cephalon-delivery-channel`, `cephalon-sender-id`, and `cephalon-correlation-id`
- provider message-id correlation from `mail.messageId`, matching the SES `MessageId` captured by `Cephalon.MultiTenancy.Governance.AmazonSesDelivery`
- safe status metadata such as SNS message id/type/topic, signature verification outcome, signature algorithm/version/fingerprint, signing certificate host/thumbprint, replay posture, message-id idempotency posture, subscription-confirmation posture, unsubscribe-confirmation observation posture, SES message id, event type, bounce type/subtype, complaint feedback type, delivery SMTP response, reject reason, rendering failure message, delivery delay type, tag count, and observed timestamp
- observation-id seeding from SNS message ids through the normalized delivery-status observation path
- optional engagement-event mapping when a host deliberately sets `MapEngagementEventsAsDelivered`
- runtime truth through the `tenant-invitation-delivery-amazon-ses-status-callbacks` technology surface
- stable diagnostics for accepted Amazon SES over SNS callback payloads, signature rejections, replay rejections, duplicate SNS message-id skips, subscription-confirmation success/failure, and unsubscribe-confirmation observation

## Main Surfaces

- `Configuration/AmazonSesInvitationDeliveryAspNetCoreOptions.cs`
- `Hosting/AmazonSesInvitationDeliveryAspNetCoreServiceCollectionExtensions.cs`
- `Hosting/AmazonSesInvitationDeliveryStatusEndpointRouteBuilderExtensions.cs`
- `Hosting/AmazonSesInvitationDeliveryStatusCallbackResult.cs`
- `Hosting/AmazonSesInvitationDeliveryStatusCallbackEventResult.cs`
- `Hosting/AmazonSesInvitationDeliveryStatusCallbackReplayGuard.cs`
- `Services/AmazonSesSnsSignatureVerifier.cs`
- `Services/IAmazonSesSnsSubscriptionConfirmationClient.cs`
- `Services/AmazonSesSnsSubscriptionConfirmationRequest.cs`
- `Services/AmazonSesSnsSubscriptionConfirmationResult.cs`

## Source Structure

- `Configuration`
- `Hosting`
- `Services`

## How It Fits

`Cephalon.MultiTenancy.Governance.AmazonSesDelivery` owns outbound SES v2 `SendEmail` handoff. It sends safe Cephalon context through SES message tags and captures the SES `MessageId` as the dispatch provider message id. Amazon SES can later publish sending events to SNS. This package bridges those SNS-wrapped SES event payloads back into Cephalon's existing `ITenantInvitationDeliveryStatusReconciler` without putting AWS-specific HTTP routes into the host-agnostic governance core. When signature verification is required, the endpoint rejects unverified SNS envelopes before payload mapping or reconciliation. When message-id idempotency is enabled, it checks the normalized `amazon-ses-sns:{MessageId}` observation id before reconciliation and skips already observed SNS notifications. When subscription confirmation is deliberately enabled, it confirms only verified SNS `SubscriptionConfirmation` envelopes through a replaceable confirmation client. When unsubscribe-confirmation observation is deliberately enabled, it observes verified SNS `UnsubscribeConfirmation` envelopes, validates the trusted `SubscribeURL`, and never invokes that URL because doing so would re-confirm or restore the subscription.

Register the package beside governance and map the endpoint explicitly:

```csharp
builder.Services.AddCephalonAmazonSesInvitationDelivery(builder.Configuration);
builder.Services.AddCephalonAmazonSesInvitationDeliveryAspNetCore(builder.Configuration);

builder.AddCephalon(engine =>
{
    engine.AddMultiTenancyGovernance();
});

var app = builder.Build();
app.MapCephalonAmazonSesInvitationDeliveryStatusCallbacks();
```

Configuration example:

```json
{
  "Engine": {
    "MultiTenancy": {
      "Governance": {
        "AmazonSesInvitationDelivery": {
          "AspNetCore": {
            "EnableStatusCallbackEndpoint": true,
            "StatusCallbackRoutePattern": "/engine/tenant-invitations/delivery-status/amazon-ses",
            "RequireStatusCallbackAuthorization": true,
            "StatusCallbackAuthorizationPolicy": "amazon-ses-sns",
            "ExcludeStatusCallbackEndpointFromDescription": true,
            "RequireProviderMessageMatch": true,
            "RecordStatus": true,
            "Source": "amazon-ses-sns",
            "Actor": "amazon-ses",
            "MaxRequestBodyBytes": 262144,
            "MaxEventsPerRequest": 1000,
            "MapEngagementEventsAsDelivered": false,
            "AcceptRawSesEventPayloads": true,
            "RequireSnsSignatureVerification": true,
            "RequireSnsSignatureVersion2": true,
            "RequireAllowedSnsTopicArn": true,
            "AllowedSnsTopicArns": [
              "arn:aws:sns:us-east-1:123456789012:cephalon-governance"
            ],
            "ValidateSnsSigningCertificateChain": true,
            "EnableSnsReplayProtection": true,
            "SnsReplayRetentionSeconds": 300,
            "SnsReplayCacheLimit": 4096,
            "EnableSnsMessageIdIdempotency": true,
            "EnableSnsSubscriptionConfirmation": false,
            "SnsSubscriptionConfirmationTimeoutSeconds": 10,
            "EnableSnsUnsubscribeConfirmationObservation": false
          }
        }
      }
    }
  }
}
```

Mapped statuses are intentionally narrow. `Send` becomes `accepted`, `Delivery` becomes `delivered`, transient `Bounce` becomes `deferred`, other `Bounce` events become `bounced`, `Complaint` and `Reject` become `suppressed`, `Rendering Failure` becomes `failed`, and `DeliveryDelay` becomes `deferred`. `Open`, `Click`, and `Subscription` are skipped by default because they are engagement or preference events rather than delivery status events; set `MapEngagementEventsAsDelivered` only when that is an explicit product decision.

The endpoint returns `AmazonSesInvitationDeliveryStatusCallbackResult` with aggregate counts and per-event translation results. Events without Cephalon tenant and invitation tags are skipped without leaking recipient email addresses or raw payloads in the response. Translated events still go through the host-agnostic reconciler, so invitation existence, provider-message matching, status recording, and observation storage keep using the same governance rules as normalized callbacks.

Set `RequireSnsSignatureVerification` to require SNS envelope verification before translation. The verifier rejects raw SES replay payloads, enforces an allowed `TopicArn` list by default, requires `SignatureVersion` 2 by default, validates HTTPS Amazon SNS signing-certificate URLs, downloads and validates the signing certificate unless `PinnedSnsSigningCertificatePem` is configured, and verifies the RSA signature over the SNS canonical string-to-sign. `PinnedSnsSigningCertificatePem` is useful for controlled tests, certificate-pinning experiments, or replay harnesses; production hosts usually leave it unset so the endpoint retrieves the AWS SNS signing certificate from the validated `SigningCertURL`.

When signature verification is required, `EnableSnsReplayProtection` defaults to `true`. The replay guard derives a safe fingerprint from the verified SNS `TopicArn` plus `MessageId`, keeps it in a bounded process-local cache, rejects duplicate verified callbacks with `409 Conflict`, and records replay posture in callback responses and reconciliation metadata. This is intentionally process-local protection, not a durable callback inbox, distributed replay ledger, or exactly-once delivery guarantee.

`EnableSnsMessageIdIdempotency` defaults to `true`. When the governance observation store is enabled and a translated event has an SNS `MessageId`, the endpoint checks the normalized `amazon-ses-sns:{MessageId}` observation id before reconciliation. Duplicate observations are skipped before the reconciler runs, return `200 OK` with `duplicateEvents`, emit diagnostic `4581`, and keep the same safe per-event `duplicate-skipped` response shape used by the other provider callbacks. The guard inherits the configured observation store's durability: the default in-memory store is process-local, while the built-in file-backed store can preserve observed message ids locally across process restarts. It is still not a distributed callback inbox, cross-node idempotency ledger, or exactly-once delivery guarantee.

`EnableSnsSubscriptionConfirmation` defaults to `false`. When a host deliberately enables it together with `RequireSnsSignatureVerification`, the endpoint confirms only verified SNS `SubscriptionConfirmation` envelopes from allowed topics. The built-in `IAmazonSesSnsSubscriptionConfirmationClient` performs a bounded `GET` to the trusted HTTPS Amazon SNS `SubscribeURL` with redirects disabled, records only safe confirmation metadata, returns confirmation aggregate fields on `AmazonSesInvitationDeliveryStatusCallbackResult`, and emits diagnostics `4582` or `4583`. Hosts can replace the client to use platform HTTP policy, approval workflows, AWS SDK seams, or tests. This does not create SNS topics/subscriptions, configure SES event destinations, store confirmation tokens, or own subscription lifecycle governance.

`EnableSnsUnsubscribeConfirmationObservation` also defaults to `false`. When a host deliberately enables it together with `RequireSnsSignatureVerification`, the endpoint observes only verified SNS `UnsubscribeConfirmation` envelopes from allowed topics, requires and validates the trusted HTTPS Amazon SNS `SubscribeURL`, records safe observation metadata, returns unsubscribe-confirmation aggregate fields on `AmazonSesInvitationDeliveryStatusCallbackResult`, and emits diagnostic `4584`. It does not call `SubscribeURL`, use the token with `ConfirmSubscription`, automatically restore subscriptions, store confirmation tokens, or own subscription lifecycle governance.

ASP.NET Core authorization is still enabled by default and can be combined with gateway policy, SNS topic policy, AWS WAF, private networking, or other host-owned controls. This package owns provider payload translation, opt-in SNS signature verification, bounded process-local SNS replay rejection, observation-store-backed SNS message-id duplicate skipping, opt-in verified SNS subscription confirmation, and opt-in verified SNS unsubscribe-confirmation observation only; SNS topic/subscription creation, SES configuration-set event destination setup, automatic resubscribe/restore, subscription lifecycle governance, durable callback inboxes, distributed replay ledgers, distributed event-id ledgers, provider polling, and exactly-once delivery remain future provider-pack or application-owned work.

## Provider References

- [Amazon SES event publishing SNS contents](https://docs.aws.amazon.com/ses/latest/dg/event-publishing-retrieving-sns-contents.html)
- [Amazon SNS HTTP notification JSON format](https://docs.aws.amazon.com/sns/latest/dg/http-notification-json.html)
- [Amazon SNS message signature verification](https://docs.aws.amazon.com/sns/latest/dg/sns-verify-signature-of-message.html)
- [Amazon SNS HTTP subscription confirmation JSON format](https://docs.aws.amazon.com/sns/latest/dg/http-subscription-confirmation-json.html)
- [Amazon SNS HTTP unsubscribe confirmation JSON format](https://docs.aws.amazon.com/sns/latest/dg/http-unsubscribe-confirmation-json.html)

## Related Docs

- [Cephalon.MultiTenancy.Governance](multi-tenancy-governance.md)
- [Cephalon.MultiTenancy.Governance.AspNetCore](multi-tenancy-governance-aspnetcore.md)
- [Cephalon.MultiTenancy.Governance.AmazonSesDelivery](multi-tenancy-governance-amazonsesdelivery.md)
- [Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore](multi-tenancy-governance-mailgundelivery-aspnetcore.md)
- [Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore](multi-tenancy-governance-sendgriddelivery-aspnetcore.md)
- [Technology packs](../technology-packs.md)
- [Operations](../operations.md)
