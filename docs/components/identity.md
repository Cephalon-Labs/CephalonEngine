# Cephalon.Identity

> **Maturity:** `M1` · **Ownership:** mixed: `cephalon-managed` default evaluator/runtime/catalog/diagnostics + `application-managed` identity scheme and principal flow — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.Identity` is the host-agnostic identity and authorization companion package for Cephalon.

## What it owns

- registers a default metadata-driven `IAuthorizationEvaluator` when `IdentityAccess` is active, while letting hosts opt out through `Engine:Identity:EnableDefaultEvaluator` when they want to supply their own evaluator
- keeps `RBAC`, `ABAC`, and policy-evaluation behavior out of hosts so consumer apps can stay low ceremony
- projects the active identity runtime answer through the `identity-authorization` technology surface, which can be disabled explicitly through `Engine:Identity:EnableRuntimeSurface`
- contributes a dedicated diagnostics convention so `/engine/diagnostics` can advertise stable identity event ids
- keeps ASP.NET Core `ClaimsPrincipal`, auth schemes, and endpoint-policy mapping out of the host-agnostic pack

## Main surfaces

- `Configuration/IdentityRuntimeOptions.cs`
- `Policies/IdentityPolicyMetadataKeys.cs`
- `Registration/IdentityEngineBuilderExtensions.cs`
- `Services/MetadataDrivenAuthorizationEvaluator.cs`
- `Services/IdentityAuthorizationRuntimeSurfaceContributor.cs`

## How it fits

This package is the first honest runtime slice of the phase-8 identity story. `Cephalon.Abstractions` already carries the host-agnostic authorization contracts, and `Cephalon.Engine` already exposes authorization-policy catalogs plus config-driven identity selection through the resolved app profile. `Cephalon.Identity` now turns those contracts into a reusable runtime baseline without forcing ASP.NET Core-specific principal or scheme semantics into the core.

The built-in evaluator is intentionally declarative and conservative. It understands low-ceremony metadata conventions for role checks, owner checks, tenant-boundary checks, and subject/resource/context attribute matching, but it does not pretend to be a full authentication stack or a product-specific policy engine. That keeps the current slice truthful while still giving consumer apps a meaningful ready-to-use baseline that reduces repeated authorization wiring and lets project code focus on business rules.

The next step after this package is broader adapter follow-through on top of the shipped `Cephalon.Identity.AspNetCore` baseline. That now covers minimal APIs and controller actions while keeping `ClaimsPrincipal`, auth schemes, and endpoint-policy wiring out of the host-agnostic core, and the next iterations should deepen host/runtime truth instead of pushing ASP.NET Core concerns back into `Cephalon.Abstractions`.

## Related docs

- [Cephalon.Abstractions](abstractions.md)
- [Cephalon.Identity.AspNetCore](identity-aspnetcore.md)
- [Cephalon.Engine](engine.md)
- [Cephalon.Eventing](eventing.md)
- [Operations](../operations.md)
