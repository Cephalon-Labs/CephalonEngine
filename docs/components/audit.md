# Cephalon.Audit

> **Maturity:** `M1` · **Ownership:** mixed: `cephalon-managed` in-memory writer + catalog projection + `application-managed` consumer-supplied actor accessors and durable storage — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.Audit` is the host-agnostic audit-recording companion pack for Cephalon phase 8 workloads.

## What it owns

- low-ceremony audit recording through `IAuditRecorder`
- ambient actor resolution through `IAuditActorAccessor`
- a default in-memory audit writer baseline for local and starter scenarios
- additive durable audit-store catalog contributions so provider packs can publish real audit-history stores without replacing the narrow host-agnostic baseline
- config-driven control over whether the built-in in-memory writer stays active, including `Engine:Audit:EnableInMemoryWriter`, with runtime audit-store answers that stay aligned with that choice
- stable diagnostics conventions for successful and failed audit-entry writes
- additive audit-store catalog contributions that flow into `/engine/audit-stores` and `/engine/snapshot`, so consumer-contributed stores remain visible when `AddAudit()` is active and the built-in memory store only disappears when it is explicitly disabled
- low-ceremony follow-through from `Cephalon.Identity.AspNetCore` when an authenticated ASP.NET Core principal can be projected into the ambient audit actor contract without requiring the consumer host to write a custom actor accessor
- additive query/read contracts through `IAuditHistoryReader`, which durable provider packs may implement without widening `Cephalon.Audit` into a mandatory storage or API stack
- additive export contracts through `IAuditHistoryExporter`, so durable provider packs can stream export entries without forcing one host or report shape into the baseline audit pack

## Main surfaces

- `Configuration/AuditRuntimeOptions.cs`
- `Conventions/AuditMetadataKeys.cs`
- `Registration/AuditEngineBuilderExtensions.cs`
- `Services/AuditRecordRequest.cs`
- `Services/IAuditActorAccessor.cs`
- `Services/IAuditRecorder.cs`

## Source structure

- `Configuration`
- `Conventions`
- `Modules`
- `Registration`
- `Services`

## How it fits

This pack stays intentionally narrow. It gives consumer apps a ready-to-use audit recording path without forcing one durable storage model, one observability backend, or one query/history UI. The default writer is in-memory and application-managed on purpose, which keeps the baseline truthful while the engine still exposes audit-store answers through runtime introspection.

The audit path is also designed to stay low ceremony. Consumer apps can turn the pack on, record audit events through one service, let ambient tenant and actor context fill the repetitive fields, and keep the remaining hand-written code focused on business behavior instead of boilerplate audit plumbing. In ASP.NET Core hosts that also enable `Cephalon.Identity.AspNetCore`, the authenticated principal can now flow into the ambient audit actor contract automatically unless the consumer has already registered its own actor accessor. Just as importantly, the pack now layers on top of consumer audit-store contributions instead of replacing them, so teams can add durable stores or custom query surfaces without losing truthful `/engine/audit-stores` answers when the built-in writer is enabled or disabled.

`scripts/validate-saas-tenant-governance-audit-adoption.ps1` now proves the generated consumer-app path for this pack. The script packages `Cephalon.Audit` with the SaaS governance, JSON-RPC, gRPC, and HTTP dependency-health companions, scaffolds a modular-monolith tenant host outside the repository, wires `engine.AddAudit()`, records a business event through `IAuditRecorder` at `/api/tenants/{tenantId}/audit-proof`, and checks `/engine/audit-stores` plus `/engine/snapshot` so the audit-store claim is tied to a real running generated app.

That provider-aware follow-through now starts with [Cephalon.Audit.EntityFramework](audit-entityframework.md). Durable history remains additive and configuration-driven through `Engine:Audit:History` plus the engine-owned `Engine:Databases` role contract, so `Cephalon.Audit` still does not become a mandatory storage opinion by itself. The same provider pack now also proves the first queryable audit-history path through `IAuditHistoryReader` plus the first bounded export path through `IAuditHistoryExporter`, while the core audit pack still stays host-agnostic and storage-agnostic. See [Database topology](../database-topology.md).

## Related docs

- [Cephalon.Abstractions](abstractions.md)
- [Cephalon.Audit.EntityFramework](audit-entityframework.md)
- [Cephalon.Engine](engine.md)
- [Database topology](../database-topology.md)
- [Operations](../operations.md)
