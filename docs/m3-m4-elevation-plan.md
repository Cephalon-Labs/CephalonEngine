# M3/M4 Elevation Plan

Plan date: `June 30, 2026`

This is the repo-owned plan for advancing Cephalon from broad runtime coverage toward repeatable operator automation (`M3`) and adoption proof (`M4`) without inflating package claims.

## Outcome

Cephalon should let modules and companion packs express domain, execution, workflow, data, integration, governance, and operational behavior through composable primitives. “Support every business logic” means the framework supplies universal extension seams and deterministic runtime contracts; it does not mean engine core contains every product-domain concept.

The program keeps these boundaries:

- `Cephalon.Abstractions` remains host-agnostic.
- `Cephalon.Engine` owns composition and combined runtime truth.
- host adapters only translate engine contracts to their host.
- package maturity follows evidence, not package count or descriptor breadth.
- `net10.0` remains the shipping floor; .NET 11 stays in a separate assessment lane until an intentional migration is approved.

## Baseline

The Gate 0 report currently reconciles `107` source projects with `107` component documents and `107` package evidence rows. The truthful distribution is `M0=1`, `M1=39`, `M2=51`, `M3=7`, and `M4=9` with zero detected drift.

The eighteen dependency-health provider packs remain `M2`. A managed probe loop, timeout handling, cached state, diagnostics, and readiness integration are real execution proof, but they are not yet reconciliation or remediation automation.

## Gates

### Gate 0 — authoritative evidence and release hygiene

Status: `complete` through `ENG-712`.

- reconcile local history against current `origin/master` before promotion work
- emit `surface-maturity-report.json` from source projects, component badges, the conformance matrix, maturity audit, backlog baseline, and dependency-health provider manifest
- fail release validation when source/document/package truth drifts
- upload the maturity and engine-completion artifacts with fail-closed missing-file behavior
- keep CI SDK setup and package-lock caching aligned with the repo's project-level lock-file layout
- keep dependency restore audit clean; pin the safe SQLite native bundle used by test harnesses and the patched Microsoft.OpenApi 2.x line used by ASP.NET OpenAPI consumers
- keep public API baselines stable across line-ending environments
- keep generated reference-doc bundles, hosted reference-doc configuration, CLI/tooling path fixtures, ASP.NET Core sample boot, and Agentics singleton/scoped service lifetimes stable across Windows, Linux, and the `.NET 11` readiness lane
- keep showcase read-model projection tests deterministic by making the background projection loop configuration-driven instead of removing services before the sample registers them
- keep behavior resilience timeout proofs deterministic under the `.NET 11` readiness SDK by avoiding near-threshold scheduler-sensitive delays
- keep local adjacent checkouts such as the engine clone and docs-site clone out of the repo contract unless an explicit submodule/milestone decision promotes them

Exit proof: report status `passed`, drift count `0`, focused Pester coverage passes, package restore has no High advisory, affected public-API projects build with deterministic baselines, generated reference-doc parity passes, focused hosting/tooling smoke tests pass, full hosting validation stays green, and release validation executes the report by default.

### Gate 1 — additive operator observation kernel

Status: `foundation shipped` through `ENG-713`; broader family adoption remains open.

- `IRuntimeIntrospectionSectionContributor` lets a package add one versioned section without expanding the top-level snapshot for every package
- entries share desired state, observed state, conditions, declared actions, and stable metadata
- section and entry ordering is deterministic; duplicate section or entry identifiers fail closed
- `RuntimeOperatorAction` advertises intent without coupling core to HTTP or claiming that execution exists
- dependency health is the first real consumer and publishes observation time, probe duration, and consecutive failure count
- ASP.NET Core and Worker read the same `snapshot.ExtensionSections` contract

Exit proof: custom package contribution, duplicate rejection, source-generated JSON, ASP.NET Core, Worker, all-provider failure, and all-provider live-success tests pass.

### Gate 2 — shared coordination kernel

Status: `planned` as `ENG-714`.

Add host-agnostic primitives for:

- reconciliation requests, plans, results, and attempt history
- leases and fencing tokens for cross-node ownership
- idempotency keys and durable journals
- retry/backoff policy plus terminal and in-doubt outcomes
- approval and authorization boundaries for operator actions
- redacted audit and diagnostics conventions

No package is promoted solely because it references these primitives. It must own and prove a real loop.

### Gate 3 — three operator-automation pilots

Status: `planned` as `ENG-715`.

Adopt the shared kernel in three different ownership families:

1. dependency health: freshness evaluation and a bounded re-probe/reconcile action
2. eventing or CDC: durable recovery/replay or drift reconciliation using the shared journal/lease model
3. edge or multi-tenancy governance: provider-observed drift plus approval-aware remediation

Each pilot must prove success, transient failure, terminal failure, duplicate command, lost lease/fencing, restart recovery, redaction, and operator readback. Only the proven package or family can move to `M3`.

### Gate 4 — adoption proof and M4 promotion

Status: `future`.

- out-of-repo sample or generated app using published packages
- upgrade and rollback proof
- operations/runbook guidance and compatibility statement
- package metadata, XML documentation, reference-doc, and source-link evidence
- performance and failure-injection baselines where the runtime path is hot or stateful
- one release artifact that ties implementation, tests, maturity, compatibility, and adoption evidence together

M4 is selective. Taxonomy-only and application-managed packages may remain at lower levels permanently when that is their correct product role.

## Promotion rules

| Move | Minimum evidence |
| --- | --- |
| `M0 → M1` | deterministic catalog/runtime truth, validation, docs, and introspection |
| `M1 → M2` | one owned execution/provisioning path plus failure-state proof |
| `M2 → M3` | reconciliation/remediation/recovery/drift loop with operator tests |
| `M3 → M4` | external adoption, upgrade/operations guidance, compatibility and release evidence |

Every promotion updates source, XML comments, tests, component docs, maturity audit, conformance matrix, runtime contract index, roadmap, backlog, milestone, GitHub issue/Project fields, and commit/PR references in the same delivery flow.

## Near-term sequence

1. Keep Gate 0 report green on every release-validation run.
2. Stabilize the Gate 1 public contract with package-authoring guidance and one additional non-health consumer.
3. Deliver Gate 2 as small primitives with contract tests before provider integration.
4. Run Gate 3 as three narrow pilots; do not perform family-wide label promotion.
5. Choose M4 candidates only from packages that have real external adoption evidence.

## Related planning

- [June 2026 architecture review](architecture-review-2026-06.md)
- [June follow-ups](architecture-review-2026-06-followups.md)
- [Engine surface maturity audit](engine-surface-maturity-audit.md)
- [Engine completion scorecard](engine-completion-scorecard.md)
- [Runtime contract index](runtime-contract-index.md)
- [Engine roadmap](engine-roadmap.md)
- [Engine backlog](engine-backlog.md)
