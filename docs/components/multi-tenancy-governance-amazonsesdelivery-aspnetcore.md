# Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore

`Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore` is the optional ASP.NET Core Amazon SES over SNS callback translator for tenant-invitation delivery status reconciliation.

## What it owns

- opt-in `POST /engine/tenant-invitations/delivery-status/amazon-ses` endpoint mapping
- fail-closed ASP.NET Core authorization by default, with an optional policy override
- Amazon SNS HTTP notification JSON parsing with bounded request size
- SNS `Notification` payload unwrapping where `Message` contains an Amazon SES event publishing record
- raw SES event object or array parsing for controlled replay and test harness scenarios when `AcceptRawSesEventPayloads` remains enabled
- translation from Amazon SES `eventType` or legacy `notificationType` values into `TenantInvitationDeliveryStatusReconciliationRequest`
- Cephalon context extraction from SES `mail.tags`, including `cephalon-tenant-id`, `cephalon-invitation-id`, `cephalon-delivery-channel`, `cephalon-sender-id`, and `cephalon-correlation-id`
- provider message-id correlation from `mail.messageId`, matching the SES `MessageId` captured by `Cephalon.MultiTenancy.Governance.AmazonSesDelivery`
- safe status metadata such as SNS message id/type/topic, SES message id, event type, bounce type/subtype, complaint feedback type, delivery SMTP response, reject reason, rendering failure message, delivery delay type, tag count, and observed timestamp
- observation-id seeding from SNS message ids through the normalized delivery-status observation path
- optional engagement-event mapping when a host deliberately sets `MapEngagementEventsAsDelivered`
- runtime truth through the `tenant-invitation-delivery-amazon-ses-status-callbacks` technology surface
- stable diagnostics for accepted Amazon SES over SNS callback payloads

## Main Surfaces

- `Configuration/AmazonSesInvitationDeliveryAspNetCoreOptions.cs`
- `Hosting/AmazonSesInvitationDeliveryAspNetCoreServiceCollectionExtensions.cs`
- `Hosting/AmazonSesInvitationDeliveryStatusEndpointRouteBuilderExtensions.cs`
- `Hosting/AmazonSesInvitationDeliveryStatusCallbackResult.cs`
- `Hosting/AmazonSesInvitationDeliveryStatusCallbackEventResult.cs`

## Source Structure

- `Configuration`
- `Hosting`
- `Services`

## How It Fits

`Cephalon.MultiTenancy.Governance.AmazonSesDelivery` owns outbound SES v2 `SendEmail` handoff. It sends safe Cephalon context through SES message tags and captures the SES `MessageId` as the dispatch provider message id. Amazon SES can later publish sending events to SNS. This package bridges those SNS-wrapped SES event payloads back into Cephalon's existing `ITenantInvitationDeliveryStatusReconciler` without putting AWS-specific HTTP routes into the host-agnostic governance core.

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
            "AcceptRawSesEventPayloads": true
          }
        }
      }
    }
  }
}
```

Mapped statuses are intentionally narrow. `Send` becomes `accepted`, `Delivery` becomes `delivered`, transient `Bounce` becomes `deferred`, other `Bounce` events become `bounced`, `Complaint` and `Reject` become `suppressed`, `Rendering Failure` becomes `failed`, and `DeliveryDelay` becomes `deferred`. `Open`, `Click`, and `Subscription` are skipped by default because they are engagement or preference events rather than delivery status events; set `MapEngagementEventsAsDelivered` only when that is an explicit product decision.

The endpoint returns `AmazonSesInvitationDeliveryStatusCallbackResult` with aggregate counts and per-event translation results. Events without Cephalon tenant and invitation tags are skipped without leaking recipient email addresses or raw payloads in the response. Translated events still go through the host-agnostic reconciler, so invitation existence, provider-message matching, status recording, and observation storage keep using the same governance rules as normalized callbacks.

SNS `SubscriptionConfirmation` and `UnsubscribeConfirmation` messages are reported as skipped and are not auto-confirmed. Hosts or infrastructure-as-code should own SNS subscription confirmation and topic policy posture deliberately. This baseline also reports SNS signature verification as `not-configured`; future slices can add SNS signature verification, process-local replay protection, or observation-store-backed SNS message id duplicate skipping without turning this translation baseline into a durable callback inbox.

ASP.NET Core authorization is still enabled by default and can be combined with gateway policy, SNS topic policy, AWS WAF, private networking, or other host-owned controls. This package owns provider payload translation only; SNS topic/subscription creation, SES configuration-set event destination setup, SNS signature verification, durable callback inboxes, distributed replay ledgers, distributed event-id ledgers, provider polling, and exactly-once delivery remain future provider-pack or application-owned work.

## Provider References

- [Amazon SES event publishing SNS contents](https://docs.aws.amazon.com/ses/latest/dg/event-publishing-retrieving-sns-contents.html)
- [Amazon SNS HTTP notification JSON format](https://docs.aws.amazon.com/sns/latest/dg/http-notification-json.html)

## Related Docs

- [Cephalon.MultiTenancy.Governance](multi-tenancy-governance.md)
- [Cephalon.MultiTenancy.Governance.AspNetCore](multi-tenancy-governance-aspnetcore.md)
- [Cephalon.MultiTenancy.Governance.AmazonSesDelivery](multi-tenancy-governance-amazonsesdelivery.md)
- [Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore](multi-tenancy-governance-mailgundelivery-aspnetcore.md)
- [Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore](multi-tenancy-governance-sendgriddelivery-aspnetcore.md)
- [Technology packs](../technology-packs.md)
- [Operations](../operations.md)
