# Cephalon Architecture Review — June 2026

Review date: `June 30, 2026`

This review extends the [May 2026 review](architecture-review-2026-05.md). The main June conclusion is that Cephalon does not need more maturity labels; it needs a smaller set of shared operator primitives and machine-checkable evidence that makes each label defensible.

## What changed

- authoritative history was reconciled against current `origin/master` before new work
- the repository now emits a fail-closed surface-maturity artifact across all `107` source projects and component documents
- the first run found and closed a real `Cephalon.ReferenceDocs` matrix drift (`M2` versus authoritative `M4`)
- `/engine/snapshot` now has a versioned additive `ExtensionSections` envelope so companion packages can contribute operator truth without adding another top-level property
- dependency health is the first consumer, projecting desired/observed health, conditions, freshness, probe duration, and consecutive-failure evidence
- the test harness now pins `SQLitePCLRaw.bundle_e_sqlite3 3.0.3`, replacing the vulnerable transitive native bundle identified by `GHSA-2m69-gcr7-jv3q`
- Cassandra and ClickHouse event-sourcing constant baselines no longer depend on CRLF-specific raw-string values

## Strengths

1. Runtime composition remains host-agnostic: ASP.NET Core and Worker consume the same snapshot projection.
2. Extension sections are deterministic and fail closed on duplicate identifiers.
3. Maturity is now executable evidence, not only prose.
4. The first consumer is a real family with eighteen provider implementations, so the abstraction is not descriptor-only design.

## Risks

1. A declared operator action is not an executable remediation. The action contract must not be read as M3 proof until authorization, idempotency, journaling, execution, and audit exist.
2. Existing families have overlapping lease, retry, journal, approval, and reconciliation concepts. Copying them again would increase surface area while reducing interoperability.
3. `RuntimeIntrospectionSnapshotProvider` still contains many legacy typed projections and optional service lookups. Extension sections prevent further growth but do not remove that existing coupling.
4. The current maturity report validates package/document agreement, not behavior depth. Promotion tests must still prove the runtime loop.

## Decisions

- keep existing typed snapshot fields for compatibility; new package-specific operator projections prefer `ExtensionSections`
- keep dependency-health providers at `M2` until a real re-probe/reconcile/remediate loop ships
- build coordination primitives before launching more family-specific operator endpoints
- treat .NET 11 as assessment-only while `net10.0` remains stable shipping truth
- promote packages individually from evidence; do not force the full repository toward M4

## 30/60/90-day direction

- 30 days: document section authoring, stabilize Gate 1 contracts, and prototype shared reconciliation/journal/lease records
- 60 days: land the coordination kernel with deterministic and restart-safe contract tests
- 90 days: complete three operator-automation pilots across different ownership families and select only proven candidates for M3

Detailed gates and acceptance criteria live in the [M3/M4 elevation plan](m3-m4-elevation-plan.md); live status is tracked in [June follow-ups](architecture-review-2026-06-followups.md).
