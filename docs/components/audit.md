# Cephalon.Audit

`Cephalon.Audit` is the host-agnostic audit-recording companion pack for Cephalon phase 8 workloads.

## What it owns

- low-ceremony audit recording through `IAuditRecorder`
- ambient actor resolution through `IAuditActorAccessor`
- a default in-memory audit writer baseline for local and starter scenarios
- config-driven control over whether the built-in in-memory writer stays active, including `Engine:Audit:EnableInMemoryWriter`, with runtime audit-store answers that stay aligned with that choice
- stable diagnostics conventions for successful and failed audit-entry writes
- additive audit-store catalog contributions that flow into `/engine/audit-stores` and `/engine/snapshot`

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

The audit path is also designed to stay low ceremony. Consumer apps can turn the pack on, record audit events through one service, let ambient tenant and actor context fill the repetitive fields, and keep the remaining hand-written code focused on business behavior instead of boilerplate audit plumbing.

## Related docs

- [Cephalon.Abstractions](abstractions.md)
- [Cephalon.Engine](engine.md)
- [Operations](../operations.md)
