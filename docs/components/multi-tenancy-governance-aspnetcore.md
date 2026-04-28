# Cephalon.MultiTenancy.Governance.AspNetCore

`Cephalon.MultiTenancy.Governance.AspNetCore` is the optional ASP.NET Core host adapter for tenant-domain ownership HTTP proof publication.

## What it owns

- opt-in ASP.NET Core routing for published tenant-domain ownership HTTP file proofs
- default `/.well-known/cephalon/{**proofPath}` proof-file endpoint mapping
- host configuration for enabling/disabling the endpoint, route pattern, cache-control header, and endpoint-description visibility
- serving only proof files that the host-agnostic governance catalog reports as published for the current request host and path
- keeping the HTTP serving layer outside `Cephalon.MultiTenancy.Governance` so the governance core remains host-agnostic

## Main surfaces

- `Configuration/MultiTenancyGovernanceAspNetCoreOptions.cs`
- `Hosting/MultiTenancyGovernanceAspNetCoreServiceCollectionExtensions.cs`
- `Hosting/MultiTenancyGovernanceAspNetCoreWebApplicationBuilderExtensions.cs`
- `Hosting/TenantDomainOwnershipHttpProofEndpointRouteBuilderExtensions.cs`

## Source structure

- `Configuration`
- `Hosting`

## How it fits

The core `Cephalon.MultiTenancy.Governance` package now owns HTTP proof publication state through `ITenantDomainOwnershipHttpProofPublisher` and `ITenantDomainOwnershipHttpProofPublicationCatalog`, but it intentionally does not reference ASP.NET Core. This adapter is the thin host layer that turns that state into real HTTP responses for ASP.NET Core apps.

Register the adapter options beside the normal governance package, then explicitly map the endpoint:

```csharp
builder.AddCephalonMultiTenancyGovernanceAspNetCore();

var app = builder.Build();
app.MapCephalonTenantDomainOwnershipHttpProofs();
```

By default, published proof files are served from `/.well-known/cephalon/{**proofPath}` with `Cache-Control: no-store` and are excluded from OpenAPI/endpoint descriptions. Hosts can override those defaults through `Engine:MultiTenancy:Governance:AspNetCore`.

This package does not issue challenges, plan proof instructions, mutate DNS records, call domain providers, verify collected evidence, or run background polling. Those responsibilities stay in `Cephalon.MultiTenancy.Governance` or future provider-specific packs. The adapter only serves the HTTP file content that the host-agnostic publication catalog has already accepted as published.

## Related docs

- [Cephalon.MultiTenancy.Governance](multi-tenancy-governance.md)
- [Cephalon.AspNetCore](aspnetcore.md)
- [Technology packs](../technology-packs.md)
- [Operations](../operations.md)
