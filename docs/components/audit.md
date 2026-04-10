# Cephalon.Audit

`Cephalon.Audit` is the host-agnostic audit-recording companion pack for Cephalon phase 8 workloads.

## What it owns

- low-ceremony audit recording through `IAuditRecorder`
- ambient actor resolution through `IAuditActorAccessor`
- a default in-memory audit writer baseline for local and starter scenarios
- config-driven control over whether the built-in in-memory writer stays active, including `Engine:Audit:EnableInMemoryWriter`, with runtime audit-store answers that stay aligned with that choice
- stable diagnostics conventions for successful and failed audit-entry writes
- additive audit-store catalog contributions that flow into `/engine/audit-stores` and `/engine/snapshot`, so consumer-contributed stores remain visible when `AddAudit()` is active and the built-in memory store only disappears when it is explicitly disabled
- low-ceremony follow-through from `Cephalon.Identity.AspNetCore` when an authenticated ASP.NET Core principal can be projected into the ambient audit actor contract without requiring the consumer host to write a custom actor accessor

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

The planned next step is durable audit history through an additive provider-aware companion path rather than by inflating this narrow baseline pack. That future path should align with engine-owned database roles and migration policy instead of creating another isolated storage contract. See [Database topology direction](../database-topology.md).

The next recommended slice is to keep this pack narrow while adding durable history as an additive provider-pack follow-through on top of an engine-owned database-topology contract. That means `Cephalon.Audit` should not grow into a mandatory storage opinion by itself; a future durable path should plug into named database roles and explicit `Engine:Audit:History` policy instead. See [Database topology direction](../database-topology.md).

## Related docs

- [Cephalon.Abstractions](abstractions.md)
- [Cephalon.Engine](engine.md)
- [Database topology direction](../database-topology.md)
- [Operations](../operations.md)
