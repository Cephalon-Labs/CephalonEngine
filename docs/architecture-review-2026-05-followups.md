---
maturity: M2 — engine-managed follow-through tracker
ownership: cephalon-managed (Cephalon-Neza maintains this doc inline with every architecture-review-driven slice)
---

# May 2026 architecture-review follow-ups

> See also: [`architecture-review-2026-05.md`](architecture-review-2026-05.md), [`planning-governance.md`](planning-governance.md), [`engine-roadmap.md`](engine-roadmap.md), [`engine-backlog.md`](engine-backlog.md), [`long-range-direction.md`](long-range-direction.md), [`engineering-standards.md`](engineering-standards.md), [`engine-completion-scorecard.md`](engine-completion-scorecard.md), [`project-memory.md`](project-memory.md).

This doc tracks the live status of every recommendation, gap, and risk from [`architecture-review-2026-05.md`](architecture-review-2026-05.md) so contributors and autonomous scheduled-task runs can see at a glance which follow-throughs are shipped vs. pending without re-reading the full review.

It supplements — does not replace — the dated review document. The May review is the durable historical snapshot; this doc is the live tracker that gets refreshed in the same slice that closes any item below.

## Status conventions

| Status | Meaning |
| --- | --- |
| `shipped` | Closed and recorded in `engine-backlog.md` plus the relevant component / cross-link surface. |
| `partial` | At least one slice has shipped that meaningfully advances this item; listed sub-items remain. |
| `pending` | Not yet started or not yet substantively advanced. |
| `superseded` | Replaced by a later, more specific item; `supersededBy` field names the replacement. |
| `closed-no-action` | Resolved through external context (e.g., upstream Microsoft cadence change) without a Cephalon slice. |

Each entry should name the closing slice (`ENG-NNN` reference) when status moves to `shipped` or `partial`, and should keep the closing date so historical traversal stays possible.

> Current deployment-mode checkpoint (`ENG-494`, May 7, 2026): add `ENG-471` through `ENG-480` plus `ENG-494` to the partial deployment-mode follow-through rows below. The latest manifest-backed inventory reports 6 package entries, 2 packages with known hazards, 14 known hazard entries, 2 `high` + 0 `medium` + 3 `excluded-by-design` + 1 `clean-baseline`, zero active `low` entries, 1 scoped `singleFile` claim, a `knownTransitiveHazardAudit` status of `matched` across 7 audited entries, 116 lock files, and 0 missing entries, plus `publishProbePolicy.releaseValidationMode=audit-only` / `nonOptOutGate=false`. `ENG-471` and `ENG-472` retired the remaining `Cephalon.Behaviors` execution-slot and type-registry runtime fallbacks, returning that package to clean-baseline absence from the active table; `ENG-473` retired `Cephalon.Behaviors.Http` `MapProfile<TBehavior>()` attribute/profile fallback by requiring generated or explicitly registered descriptors; `ENG-474` retired the profile input-shape fallback by making explicit binding validation consume descriptor-backed input contracts; `ENG-475` retired the manual/profile route-contract fallback by requiring generated or explicitly registered `BehaviorContractDescriptor` metadata; `ENG-476` retired the broader `Cephalon.Engine` module-discovery blocker through generated `ModuleDiscoveryDescriptor` metadata; `ENG-477` moved the MySQL SciSharp reflective transport path out of core `Cephalon.Data.MySql` into optional `Cephalon.Data.MySql.SciSharpReplication`; `ENG-478` made that optional adapter's permanent `not-claimed` posture machine-checkable through matching project properties, manifest `requiredProjectProperties`, and manifest Pester coverage; `ENG-479` made the transitive-hazard subset lock-file-audited without widening support; `ENG-480` made the release-validation publish-probe gate posture manifest-backed and reportable without turning it into a non-opt-out gate; and `ENG-494` confirmed `Cephalon.Data.MySql.SciSharpReplication` plus `Cephalon.ReferenceDocs` as deliberate package-level support boundaries rather than open global remediation blockers. Remaining global support-promotion work is now a later deliberate publish-probe gate promotion and any scoped package claims that explicitly enter the promoted package set.

> Current scorecard checkpoint (`ENG-486` + `ENG-495`, May 7, 2026): `scripts/publish-engine-completion-scorecard.ps1` schema `1.6.0` validates `scripts/deployment-mode-support.json`, emits `DeploymentModeEvidence` for global deployment-mode claim rows, package-scoped claims, known hazards, transitive audit entries, representative publish targets, and publish-probe policy, validates `scripts/adoption-smoke-support.json`, emits `AdoptionSmokeEvidence` for the out-of-repo generated-app package-stage replay, validates `scripts/sre-posture-support.json`, emits `SrePostureEvidence` for target-declared versus pending/stable SLI baselines, validates `scripts/supply-chain-release-support.json` plus `.github/workflows/publish-release.yml`, emits `SupplyChainEvidence` for workflow-ready versus external-policy-pending release provenance, and emits `PublicApiCompatibilityEvidence` from `src/Cephalon.*/PublicAPI.Unshipped.txt` plus matching shipped baselines. `scripts/validate-release.ps1` now prints generated deployment-mode, SRE, supply-chain, and public API count summaries from the scorecard artifact, runs `scripts/summarise-public-api-deltas.ps1 -FailOnRemovals` so pending `*REMOVED*` entries write a delta artifact and fail the release gate, and `cephalon doctor --scorecard <path>` reads the same schema `1.6.0` artifact and checks deployment-mode, SRE, supply-chain, and public API summary counts against their detailed evidence nodes. The scorecard remains a read model over owning docs and scripts; it fails fast if deployment-mode manifest paths/counts drift from source truth, if adoption probes drift away from `scripts/validate-out-of-tree-package-adoption.ps1`, if SRE SLI ids drift away from `docs/sre-posture.md`, if supply-chain workflow tokens drift away from `.github/workflows/publish-release.yml`, if public API delta files lose their shipped baseline/project pairing, or if the generated summary drifts from detailed deployment-mode/SRE/supply-chain/public API evidence.

## Recommendations: Next 30 days (≈ May 2026)

| Recommendation | Status | Closing slice(s) | Closed date | Notes |
| --- | --- | --- | --- | --- |
| Ship the cross-link pass that wires `long-range-direction.md` and `engineering-standards.md` into `module-authoring.md`, `learning-roadmap.md`, `compatibility.md`, and `architecture/design-patterns-reference.md` | `shipped` | (cross-link pass) | 2026-05-03 | Closure recorded in May review *Updated gap* #1. All four target docs now reference both anchor docs in their *See also* prefaces. |
| Decide and document architecture-review cadence (monthly snapshot vs. reactive); update `planning-governance.md` accordingly | `shipped` | (`planning-governance.md` lines 97-117) | 2026-05-03 | Closure recorded in May review *Updated gap* #2. Monthly cadence with no-change-month posture is now declared. |
| Begin the deployment-mode validation harness work (PowerShell script + companion doc + first claim audit) so the trim/AOT/single-file claim story has a path to truthful proof before the .NET 11 final release window | `partial` | `ENG-426`, `ENG-427`, `ENG-431`..`ENG-439`, `ENG-449`, `ENG-454`, `ENG-455`, `ENG-456`, `ENG-457`, `ENG-458`, `ENG-459`, `ENG-460`, `ENG-461`, `ENG-462`, `ENG-463`, `ENG-464`, `ENG-465`, `ENG-466`, `ENG-467`, `ENG-468`, `ENG-469`, `ENG-470`, `ENG-471`, `ENG-472`, `ENG-473`, `ENG-474`, `ENG-475`, `ENG-476`, `ENG-477`, `ENG-478`, `ENG-479`, `ENG-480`, `ENG-494` | 2026-05-07 | The harness PowerShell + Pester baseline shipped earlier (`ENG-331`); `ENG-426` added the per-package hazard inventory and `ENG-427` ported it into machine-readable manifest entries. The current wave through `ENG-494` retires the behavior/durable/saga/REST/Data/EventSourcing/CDC/Engine structural hazards, isolates the optional SciSharp adapter, makes permanent package-level non-claims machine-checkable and clearly documented, makes the transitive-hazard subset lock-file-audited, and emits `PublishProbePolicy` so release-validation audit-only posture is explicit. Still pending: a later deliberate `publishProbePolicy.nonOptOutGate=true` promotion if Cephalon chooses to widen global support rows. |
| Continue the truthful slice cadence for governance, CDC, traffic automation, and feature flags; do not slow code velocity to wait for docs | `shipped` (continuing) | (48+ slices) | ongoing | The May 4-6 wave shipped across the redaction adoption, conformance-matrix drift-fix, Resilience extraction, trim/AOT inventory, release-validation, scorecard, and adoption-smoke evidence arcs without slowing on doc cross-links — the discipline is working. |

## Recommendations: Next 60 days (≈ June 2026)

| Recommendation | Status | Closing slice(s) | Closed date | Notes |
| --- | --- | --- | --- | --- |
| Close the conformance matrix gap with a per-provider/per-companion matrix that consumes `engine-surface-maturity-audit.md` truth and projects it for adopters | `shipped` | (matrix authoring + drift-fix arc) | 2026-05-03..05 | `docs/conformance-matrix.md` shipped with per-package rows + family summary on May 3; the drift-fix arc (`ENG-388` / `ENG-389` / `ENG-391` / `ENG-392` / `ENG-393` / `ENG-411` / `ENG-416` / `ENG-417` / `ENG-418` / `ENG-425`) closed every per-row drift through May 5. Per-provider extension of the existing matrix remains future work but is not blocking. |
| Promote maturity labels into the human-facing component docs and getting-started flows so adopters see `M0`/`M1`/`M2`/`M3`/`M4` and the four ownership modes without having to read the audit page | `shipped` | `ENG-382`..`ENG-387`, `ENG-393` | 2026-05-04..05 | Per-page maturity-badge frontmatter convention rolled out across all 100+ component docs in 7 batches. `getting-started.md` lines 14-30 also surface the labels explicitly. |
| Run a `.NET 11` analyzer-drift pass against Preview 4 (expected on or near May 12, 2026) and Preview 5 (June) so analyzer-only readiness signals stay current | `pending` | — | — | Microsoft has not yet shipped Preview 4 (still on the second-Tuesday cadence expectation). The next scheduled-task pass after Preview 4 publishes should refresh `dotnet11-readiness.md` in place per the no-third-dated-layer discipline. |
| Consider drafting a `docs/runtime-contract-index.md` that consolidates `/engine/*` routes and `snapshot.*` data into one machine-readable contract index for AI consumers and operators | `shipped` | (initial authoring), `ENG-411` (drift fix) | 2026-05-03..04 | Shipped on May 3; ENG-411 closed a 27-route drift on May 4. |

## Recommendations: Next 90 days (≈ July 2026)

| Recommendation | Status | Closing slice(s) | Closed date | Notes |
| --- | --- | --- | --- | --- |
| Close the truthful trim/AOT/single-file claim story end-to-end: project properties, manifest, validation script, workflow, docs, project memory, package-publishing guidance, and any roadmap/backlog cards updated together in one slice | `partial` | `ENG-426`, `ENG-427`, `ENG-431`..`ENG-439`, `ENG-449`, `ENG-454`, `ENG-455`, `ENG-456`, `ENG-457`, `ENG-458`, `ENG-459`, `ENG-460`, `ENG-461`, `ENG-462`, `ENG-463`, `ENG-464`, `ENG-465`, `ENG-466`, `ENG-467`, `ENG-468`, `ENG-469`, `ENG-470`, `ENG-471`, `ENG-472`, `ENG-473`, `ENG-474`, `ENG-475`, `ENG-476`, `ENG-477`, `ENG-478`, `ENG-479`, `ENG-480`, `ENG-494` | 2026-05-07 | Inventory + manifest seed + path-truth Pester layer shipped, and the active wave through `ENG-494` now records structural remediation, deliberate package-level permanent non-claims, lock-file-backed transitive hazard audit, and manifest-backed `PublishProbePolicy` readback. The current aggregate posture is 2 `high` + 0 `medium` + 0 `low` + 3 `excluded-by-design` = 5 active structural entries, plus 1 clean-baseline scoped `singleFile` claim, and the emitted inventory summarizes 6 package entries, 2 packages with known hazards, 14 known hazard entries, transitive-hazard hints for all three modes, `knownTransitiveHazardAudit=matched`, and release-validation publish probes still audit-only. Remaining: a later deliberate non-opt-out publish-probe promotion if a global support claim is intentionally widened. |
| Review whether the "May 2026" architecture review cadence assumption holds; if July is going to be the third dated review, decide before the cadence becomes accidentally permanent | `pending` | — | — | The June architecture review is the natural anchor for this decision. The monthly cadence declaration in `planning-governance.md` already supports continuing it; the open question is whether July warrants a full review or only a no-change-month snapshot. |
| Run a planning-governance review against the new long-range-direction horizons to confirm `engine-roadmap.md` phase plans still align with the planning frame; rewrite affected horizon sections in place rather than appending | `pending` | — | — | The long-range direction doc shipped in April. `ENG-450` adds the completion-scorecard roll-up that the review can use as an input, and `ENG-481` adds adoption-smoke evidence to that read model; the broader phase-plan alignment review remains pending before the Sprint 126 planning round. |

## Risks tracked from the May review

| Risk | Status | Closing / mitigation slice(s) | Notes |
| --- | --- | --- | --- |
| #1 — Documentation cross-link debt grows faster than code | `closed for May` | (cross-link pass shipped May 3) | Carry forward to June only if a future arc ships without a parallel doc-pass. |
| #2 — Maturity-label communication still asymmetric across surface families | `closed` | `ENG-382`..`ENG-387`, `ENG-393` | Per-page badge convention adopted everywhere. |
| #3 — Trim, AOT, and single-file claim story is still not closed | `partial` | `ENG-426`, `ENG-427`, `ENG-431`..`ENG-439`, `ENG-449`, `ENG-454`, `ENG-455`, `ENG-456`, `ENG-457`, `ENG-458`, `ENG-459`, `ENG-460`, `ENG-461`, `ENG-462`, `ENG-463`, `ENG-464`, `ENG-465`, `ENG-466`, `ENG-467`, `ENG-468`, `ENG-469`, `ENG-470`, `ENG-471`, `ENG-472`, `ENG-473`, `ENG-474`, `ENG-475`, `ENG-476`, `ENG-477`, `ENG-478`, `ENG-479`, `ENG-480`, `ENG-494` | See the Next-90-days recommendation above. The active remediation wave now retires or isolates the known structural hazards, makes permanent package-level non-claims and transitive-hazard inventory machine-checkable, and emits the publish-probe policy while keeping global support `not-claimed`. |
| #4 — Default-path dilution risk has not changed | `pending — re-check quarterly` | — | Latent risk; intentionally tracked rather than actioned. |
| #5 — Sub-agent and AI tooling assumptions need to remain reversible | `pending — re-check quarterly` | — | Project-memory pins specific tooling decisions but preserves reversibility. |

## Updated gaps tracked from the May review

| Gap | Status | Closing slice(s) | Notes |
| --- | --- | --- | --- |
| #1 — May 2026 cross-link pass for long-range-direction and engineering-standards | `closed` | (May 3 cross-link pass) | Recorded in the review doc itself. |
| #2 — Architecture review month-to-month cadence is now de facto monthly | `closed` | (`planning-governance.md` lines 97-117) | Recorded in the review doc itself. |
| #3 — Deployment-mode claim validation needs a real harness | `partial` | `ENG-331`, `ENG-426`, `ENG-427`, `ENG-431`..`ENG-439`, `ENG-449`, `ENG-454`, `ENG-455`, `ENG-456`, `ENG-457`, `ENG-458`, `ENG-459`, `ENG-460`, `ENG-461`, `ENG-462`, `ENG-463`, `ENG-464`, `ENG-465`, `ENG-466`, `ENG-467`, `ENG-468`, `ENG-469`, `ENG-470`, `ENG-471`, `ENG-472`, `ENG-473`, `ENG-474`, `ENG-475`, `ENG-476`, `ENG-477`, `ENG-478`, `ENG-479`, `ENG-480`, `ENG-494` | The harness exists and runs in CI, the inventory feeding it is repo-owned, every package is classified, the default representative publish target set now covers five sample hosts, `Cephalon.Diagnostics` is the first package-scoped single-file claim, the manifest-backed inventory now emits as `HazardInventory` plus `hazard-inventory.json`, and `PublishProbePolicy` now explains why release validation is still audit-only. Remaining gating work is intentionally removing `-SkipPublish` only in a later support-promotion slice. |
| #4 — AI/agent-facing readability of the runtime contract | `closed` | (`runtime-contract-index.md` shipped May 3, drift-fix `ENG-411` May 4) | Recorded in the review doc itself. |
| #5 — Conformance matrix stayed open since April | `closed` | (matrix shipped + drift-fix arc) | Recorded in the review doc itself. |

## How this doc gets updated

This doc is refreshed in any slice that:

- closes one of the items above (move `pending` → `partial` / `shipped`, fill the closing slice + date)
- adds a new item to the May review (which by convention should not happen — items are added to the *next* monthly review instead, and this doc supersedes its old form via `supersededBy: 2026-06-followups.md`)
- supersedes an item via a later, more specific replacement

A slice that does not touch any of the items above does not need to update this doc.

## Refresh discipline

When `architecture-review-2026-06.md` ships, this doc is forked into `architecture-review-2026-06-followups.md` to track June-specific items. The May version stays as durable history and stops updating; closures landing after the June review are recorded against the June version instead.
