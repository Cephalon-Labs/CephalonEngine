# Cephalon.MultiTenancy.Governance.AspNetCore

`Cephalon.MultiTenancy.Governance.AspNetCore` is the optional ASP.NET Core host adapter for tenant-domain ownership HTTP proof publication, tenant-administration workflow commands, and normalized tenant-invitation delivery status callbacks.

## What it owns

- opt-in ASP.NET Core routing for published tenant-domain ownership HTTP file proofs
- default `/.well-known/cephalon/{**proofPath}` proof-file endpoint mapping
- opt-in ASP.NET Core routing for tenant-administration workflow commands over the host-agnostic governance workflow
- default `POST /engine/tenant-administration/commands` command endpoint mapping
- fail-closed tenant-administration authorization by default, with an optional ASP.NET Core policy override
- opt-in ASP.NET Core routing for normalized tenant-invitation delivery status callbacks over the host-agnostic invitation delivery status reconciler
- default `POST /engine/tenant-invitations/delivery-status` callback endpoint mapping
- fail-closed delivery-status callback authorization by default, with an optional ASP.NET Core policy override
- provider-message-match enforcement for callback requests by default so the adapter cannot attach a status observation to the wrong invitation silently
- opt-in provider-neutral HMAC-SHA256 callback signature verification over the exact normalized JSON request body
- bounded process-local replay protection for signed normalized delivery-status callbacks
- adapter runtime truth through the `tenant-administration-http-endpoints` technology surface
- adapter runtime truth through the `tenant-invitation-delivery-status-http-endpoints` technology surface
- host configuration for enabling/disabling endpoints, route patterns, cache-control header, authorization posture, endpoint-description visibility, callback signature headers, callback signing key id, timestamp tolerance, signed-callback replay retention, and signed-callback replay cache limits
- serving only proof files that the host-agnostic governance catalog reports as published for the current request host and path
- keeping the HTTP serving layer outside `Cephalon.MultiTenancy.Governance` so the governance core remains host-agnostic

## Main surfaces

- `Configuration/MultiTenancyGovernanceAspNetCoreOptions.cs`
- `Hosting/MultiTenancyGovernanceAspNetCoreServiceCollectionExtensions.cs`
- `Hosting/MultiTenancyGovernanceAspNetCoreWebApplicationBuilderExtensions.cs`
- `Hosting/TenantAdministrationEndpointRouteBuilderExtensions.cs`
- `Hosting/TenantDomainOwnershipHttpProofEndpointRouteBuilderExtensions.cs`
- `Hosting/TenantInvitationDeliveryStatusCallbackEndpointRouteBuilderExtensions.cs`
- `Hosting/TenantInvitationDeliveryStatusCallbackReplayGuard.cs`
- `Hosting/TenantInvitationDeliveryStatusCallbackRequest.cs`

## Source structure

- `Configuration`
- `Hosting`

## How it fits

The core `Cephalon.MultiTenancy.Governance` package owns HTTP proof publication state through `ITenantDomainOwnershipHttpProofPublisher` and `ITenantDomainOwnershipHttpProofPublicationCatalog`, host-driven tenant-administration workflow commands through `ITenantAdministrationWorkflow`, and delivery status reconciliation through `ITenantInvitationDeliveryStatusReconciler`. It intentionally does not reference ASP.NET Core. This adapter is the thin host layer that turns those host-agnostic states into real HTTP responses and normalized HTTP ingress for ASP.NET Core apps.

Register the adapter options beside the normal governance package, then explicitly map the endpoints the host wants:

```csharp
builder.AddCephalonMultiTenancyGovernanceAspNetCore();

var app = builder.Build();
app.MapCephalonTenantDomainOwnershipHttpProofs();
app.MapCephalonTenantAdministrationCommands();
app.MapCephalonTenantInvitationDeliveryStatusCallbacks();
```

By default, published proof files are served from `/.well-known/cephalon/{**proofPath}` with `Cache-Control: no-store` and are excluded from OpenAPI/endpoint descriptions. Hosts can override those defaults through `Engine:MultiTenancy:Governance:AspNetCore`.

By default, tenant-administration commands are mapped at `POST /engine/tenant-administration/commands`, are excluded from endpoint descriptions, and require authorization. The handler also performs a fail-closed authorization check so a host that accidentally omits authorization middleware does not execute membership or invitation mutations anonymously. Hosts can deliberately disable the command endpoint, change the route, disable the authorization requirement for an internal/test host, or require a named ASP.NET Core policy with `TenantAdministrationAuthorizationPolicy`.

By default, delivery-status callbacks are mapped at `POST /engine/tenant-invitations/delivery-status`, are excluded from endpoint descriptions, require authorization, and enforce provider-message matching even if the callback body attempts to disable that safety check. The request body is `TenantInvitationDeliveryStatusCallbackRequest`, a provider-neutral normalized shape with tenant id, invitation id, status, provider message id, sender id, channel, reason, observed timestamp, source, actor, correlation id, and safe metadata. Provider-specific webhook bodies should be translated into that shape by the host or a later provider companion before reaching this endpoint.

When `TenantInvitationDeliveryStatusCallbackSigningSecret` is configured, the endpoint also requires a provider-neutral Cephalon callback signature before it parses or reconciles the request. The signature header defaults to `X-Cephalon-Callback-Signature`, the timestamp header defaults to `X-Cephalon-Callback-Signature-Timestamp`, the optional key-id header defaults to `X-Cephalon-Callback-Key-Id`, and the signature value uses `v1=<hex-hmac-sha256>` over `{unixTimestamp}.{exactUtf8JsonBody}`. The default timestamp tolerance is five minutes and can be changed with `TenantInvitationDeliveryStatusCallbackSignatureToleranceSeconds`. Signature failures return `401` and do not call the reconciler or mutate invitation state.

When signed callback replay protection is enabled, which is the default, the endpoint records a bounded in-memory fingerprint of each accepted signed callback and rejects the same signed request with `409` inside `TenantInvitationDeliveryStatusCallbackReplayRetentionSeconds`. The replay key is a SHA-256 fingerprint of the verified signature header value, never the raw signature or signing secret. `TenantInvitationDeliveryStatusCallbackReplayCacheLimit` bounds the process-local cache and evicts the oldest accepted fingerprint when the cache is full. This is intentionally process-local and non-durable: it reduces duplicate signed callback processing inside one host process, but it does not claim distributed replay protection, durable inbox ownership, or cross-node exactly-once delivery.

The adapter reports its command endpoint posture through the `tenant-administration-http-endpoints` runtime surface and its callback endpoint posture through `tenant-invitation-delivery-status-http-endpoints`. A mapped endpoint reports `cephalon-managed`; an enabled but unmapped endpoint reports `host-mapping-required`; a disabled endpoint reports `not-configured`. The callback surface also reports route, method, authorization posture, endpoint-description posture, provider-message-match enforcement, provider-neutral callback signature verification posture, signed-callback replay posture, and explicit `application-managed` boundaries for provider-specific payload translation, provider-specific signature verification, and provider polling.

The callback surface also reports whether provider-neutral callback signature verification is configured, which safe header names are expected, whether a signing key id is configured, the effective timestamp tolerance, whether signed replay protection is active, the replay policy, replay key shape, process-local scope, non-durable posture, retention seconds, and cache limit. It never exposes the configured signing secret or received signature value; reconciled observations may store only a safe `sha256:` replay fingerprint for correlation. Provider-specific payload translation and provider-specific signature verification remain separate application/provider-pack responsibilities.

This package does not issue challenges, plan proof instructions, mutate DNS records, call domain providers, verify collected evidence, run background polling, dispatch invitation delivery, implement provider-specific invitation senders, translate provider-specific callback payloads, verify provider-specific callback signatures, poll delivery providers, create identity-provider users, or provide a backoffice UI. Those responsibilities stay in `Cephalon.MultiTenancy.Governance`, future provider-specific packs, or consumer applications. The adapter only serves the HTTP file content that the host-agnostic publication catalog has already accepted as published, exposes the workflow command endpoint that the host explicitly maps, and accepts normalized status callback requests for the reconciler the governance core already owns, optionally verifying the normalized callback body's Cephalon HMAC signature and rejecting process-local signed replays before reconciliation.

## Related docs

- [Cephalon.MultiTenancy.Governance](multi-tenancy-governance.md)
- [Cephalon.AspNetCore](aspnetcore.md)
- [Technology packs](../technology-packs.md)
- [Operations](../operations.md)
