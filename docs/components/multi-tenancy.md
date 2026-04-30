# Cephalon.MultiTenancy

`Cephalon.MultiTenancy` is the host-agnostic multi-tenancy companion package for Cephalon.

## What it owns

- registers a default configuration-driven `ITenantResolver` when `MultiTenancy` is active
- lets hosts disable the built-in configuration-driven resolver without removing the package
- keeps tenant resolution, default-tenant fallback, and ambient tenant-context management out of hosts
- projects the active tenant-resolution answer through the `tenant-resolution` runtime surface under `multi-tenancy`
- projects the base-package versus companion boundary through the `tenant-governance-boundaries` runtime surface under `multi-tenancy`
- contributes a dedicated diagnostics convention so `/engine/diagnostics` can advertise stable tenant-resolution event ids
- keeps request-host, route, and ASP.NET Core-specific tenant extraction out of the host-agnostic pack

## Main surfaces

- `Configuration/MultiTenancyRuntimeOptions.cs`
- `Registration/MultiTenancyEngineBuilderExtensions.cs`
- `Services/ConfiguredTenantResolver.cs`
- `Services/AmbientTenantContextAccessor.cs`
- `Services/MultiTenancyRuntimeSurfaceContributor.cs`
- `Services/MultiTenancyGovernanceBoundaryRuntimeSurfaceContributor.cs`
- `Services/MultiTenancyDiagnosticsConventionContributor.cs`

## How it fits

This package is the first truthful runtime slice of Cephalon multi-tenancy. `Cephalon.Abstractions` already carries the host-agnostic tenant contracts, and `Cephalon.Engine` already exposes configuration-driven tenancy selection through the resolved app profile. `Cephalon.MultiTenancy` now turns those contracts into a reusable runtime baseline without hardwiring ASP.NET Core or any single hosting model into the core.

The built-in resolver is intentionally configuration-driven and conservative. It can resolve tenants from explicit tenant id hints, tenant keys, host names, configured default tenants, and the single-tenant fallback case, and it seeds an ambient tenant context for the current async flow. Explicit tenant-id, tenant-key, or host-name misses stay misses instead of silently defaulting into a different tenant, which keeps the runtime answer safer and more truthful. Hosts can also disable the built-in configuration-driven resolver when they want to supply a different resolution strategy on top of the same shared contracts and runtime surface.

The base package still does not execute tenant membership workflows, invite flows, invitation delivery dispatch, invitation delivery status reconciliation, declared domain ownership validation, domain ownership verification workflows, domain proof challenge issuance, domain proof publication planning, HTTP proof publication, domain proof evaluation, HTTP proof collection, DNS TXT proof collection, domain proof verification runner orchestration, domain proof polling, approval/remediation action decisions, approval/remediation action workflow transitions, durable governance storage, tenant-administration workflow commands, or product-specific backoffice/public-site orchestration. Those broader workflows are visible as boundary entries in the `tenant-governance-boundaries` surface so the current runtime stays honest: `Cephalon.MultiTenancy` owns resolution and ambient context, while `Cephalon.MultiTenancy.Governance` owns concrete companion proofs for membership, invitations, delivery dispatch/retry/status reconciliation, local durable observation storage, tenant administration, declared domain ownership, proof collection/polling, and governance actions.

Optional companions keep provider concerns additive. `Cephalon.MultiTenancy.Governance.AspNetCore` can add ASP.NET Core routes for published HTTP proofs, tenant-administration commands, invitation-delivery dispatch actions, normalized delivery-status callbacks, provider-neutral signed callback replay protection, and bounded observation reads. HTTP, SMTP, SendGrid, Mailgun, Amazon SES, and Microsoft Graph sender packages add outbound sender implementations. `Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore` adds SendGrid Event Webhook translation and SendGrid-specific webhook hardening, `Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore` adds Mailgun webhook payload translation plus Mailgun-specific signed-webhook verification, bounded process-local replay-token rejection, and observation-store-backed event-id idempotency, and `Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore` adds Amazon SES over SNS callback translation plus opt-in SNS signature verification, bounded process-local SNS replay protection, and observation-store-backed SNS message-id idempotency. Actual DNS proof publication, provider-backed proof publication or mutation, remediation execution beyond state transitions, distributed or provider-backed governance storage, additional provider-specific email API senders beyond the shipped SMTP/SendGrid/Mailgun/Amazon SES/Microsoft Graph set, SMS/chat/CRM/identity-provider invitation senders, durable retry queues, provider-specific or distributed callback inboxes, cross-node callback replay protection, distributed event-id ledgers, provider-specific callback translation beyond the shipped SendGrid/Mailgun/Amazon SES translators, provider-specific callback signature verification beyond shipped SendGrid/Mailgun/Amazon SNS hardening, provider polling, tenant-admin UI, public onboarding, and identity-provider synchronization remain future governance work until a package owns those paths explicitly.

That gives consumer apps a low-ceremony starting point: hosts and future adapters can feed host-neutral resolution hints into `ITenantResolver`, while the shared Cephalon runtime surfaces continue to tell operators what tenant-resolution behavior is actually active.

## Related docs

- [Cephalon.Abstractions](abstractions.md)
- [Cephalon.Engine](engine.md)
- [Cephalon.Identity](identity.md)
- [Cephalon.MultiTenancy.Governance](multi-tenancy-governance.md)
- [Cephalon.MultiTenancy.Governance.AspNetCore](multi-tenancy-governance-aspnetcore.md)
- [Cephalon.MultiTenancy.Governance.AmazonSesDelivery](multi-tenancy-governance-amazonsesdelivery.md)
- [Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore](multi-tenancy-governance-amazonsesdelivery-aspnetcore.md)
- [Cephalon.MultiTenancy.Governance.HttpDelivery](multi-tenancy-governance-httpdelivery.md)
- [Cephalon.MultiTenancy.Governance.MailgunDelivery](multi-tenancy-governance-mailgundelivery.md)
- [Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore](multi-tenancy-governance-mailgundelivery-aspnetcore.md)
- [Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery](multi-tenancy-governance-microsoftgraphdelivery.md)
- [Cephalon.MultiTenancy.Governance.SendGridDelivery](multi-tenancy-governance-sendgriddelivery.md)
- [Cephalon.MultiTenancy.Governance.SmtpDelivery](multi-tenancy-governance-smtpdelivery.md)
- [Operations](../operations.md)
