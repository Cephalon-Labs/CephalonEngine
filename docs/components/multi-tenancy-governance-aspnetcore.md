# Cephalon.MultiTenancy.Governance.AspNetCore

`Cephalon.MultiTenancy.Governance.AspNetCore` is the optional ASP.NET Core host adapter for tenant-domain ownership HTTP proof publication and tenant-administration workflow commands.

## What it owns

- opt-in ASP.NET Core routing for published tenant-domain ownership HTTP file proofs
- default `/.well-known/cephalon/{**proofPath}` proof-file endpoint mapping
- opt-in ASP.NET Core routing for tenant-administration workflow commands over the host-agnostic governance workflow
- default `POST /engine/tenant-administration/commands` command endpoint mapping
- fail-closed tenant-administration authorization by default, with an optional ASP.NET Core policy override
- adapter runtime truth through the `tenant-administration-http-endpoints` technology surface
- host configuration for enabling/disabling endpoints, route patterns, cache-control header, authorization posture, and endpoint-description visibility
- serving only proof files that the host-agnostic governance catalog reports as published for the current request host and path
- keeping the HTTP serving layer outside `Cephalon.MultiTenancy.Governance` so the governance core remains host-agnostic

## Main surfaces

- `Configuration/MultiTenancyGovernanceAspNetCoreOptions.cs`
- `Hosting/MultiTenancyGovernanceAspNetCoreServiceCollectionExtensions.cs`
- `Hosting/MultiTenancyGovernanceAspNetCoreWebApplicationBuilderExtensions.cs`
- `Hosting/TenantAdministrationEndpointRouteBuilderExtensions.cs`
- `Hosting/TenantDomainOwnershipHttpProofEndpointRouteBuilderExtensions.cs`

## Source structure

- `Configuration`
- `Hosting`

## How it fits

The core `Cephalon.MultiTenancy.Governance` package owns HTTP proof publication state through `ITenantDomainOwnershipHttpProofPublisher` and `ITenantDomainOwnershipHttpProofPublicationCatalog`, and it owns host-driven tenant-administration workflow commands through `ITenantAdministrationWorkflow`. It intentionally does not reference ASP.NET Core. This adapter is the thin host layer that turns those host-agnostic states into real HTTP responses for ASP.NET Core apps.

Register the adapter options beside the normal governance package, then explicitly map the endpoints the host wants:

```csharp
builder.AddCephalonMultiTenancyGovernanceAspNetCore();

var app = builder.Build();
app.MapCephalonTenantDomainOwnershipHttpProofs();
app.MapCephalonTenantAdministrationCommands();
```

By default, published proof files are served from `/.well-known/cephalon/{**proofPath}` with `Cache-Control: no-store` and are excluded from OpenAPI/endpoint descriptions. Hosts can override those defaults through `Engine:MultiTenancy:Governance:AspNetCore`.

By default, tenant-administration commands are mapped at `POST /engine/tenant-administration/commands`, are excluded from endpoint descriptions, and require authorization. The handler also performs a fail-closed authorization check so a host that accidentally omits authorization middleware does not execute membership or invitation mutations anonymously. Hosts can deliberately disable the command endpoint, change the route, disable the authorization requirement for an internal/test host, or require a named ASP.NET Core policy with `TenantAdministrationAuthorizationPolicy`.

The adapter reports its command endpoint posture through the `tenant-administration-http-endpoints` runtime surface. A mapped endpoint reports `cephalon-managed`; an enabled but unmapped endpoint reports `host-mapping-required`; a disabled endpoint reports `not-configured`. The same surface keeps public onboarding, tenant-admin UI, provider-specific invitation senders, external invitation delivery, and identity-provider synchronization marked as application-managed.

This package does not issue challenges, plan proof instructions, mutate DNS records, call domain providers, verify collected evidence, run background polling, dispatch invitation delivery, implement provider-specific invitation senders, create identity-provider users, or provide a backoffice UI. Those responsibilities stay in `Cephalon.MultiTenancy.Governance`, future provider-specific packs, or consumer applications. The adapter only serves the HTTP file content that the host-agnostic publication catalog has already accepted as published and exposes the workflow command endpoint that the host explicitly maps.

## Related docs

- [Cephalon.MultiTenancy.Governance](multi-tenancy-governance.md)
- [Cephalon.AspNetCore](aspnetcore.md)
- [Technology packs](../technology-packs.md)
- [Operations](../operations.md)
