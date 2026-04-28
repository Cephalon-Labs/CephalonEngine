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

The base package still does not execute tenant membership workflows, invite flows, declared domain ownership validation, or product-specific backoffice/public-site orchestration. Those broader workflows are visible as boundary entries in the `tenant-governance-boundaries` surface so the current runtime stays honest: `Cephalon.MultiTenancy` owns resolution and ambient context, while `Cephalon.MultiTenancy.Governance` now owns concrete companion proofs for tenant membership cataloging/evaluation, tenant invitation cataloging/validation, and declared domain-ownership cataloging/validation. DNS/HTTP verification execution, approval/remediation workflows, durable governance storage, identity-provider synchronization, and tenant administration remain future governance work until a package owns those paths explicitly.

That gives consumer apps a low-ceremony starting point: hosts and future adapters can feed host-neutral resolution hints into `ITenantResolver`, while the shared Cephalon runtime surfaces continue to tell operators what tenant-resolution behavior is actually active.

## Related docs

- [Cephalon.Abstractions](abstractions.md)
- [Cephalon.Engine](engine.md)
- [Cephalon.Identity](identity.md)
- [Cephalon.MultiTenancy.Governance](multi-tenancy-governance.md)
- [Operations](../operations.md)
