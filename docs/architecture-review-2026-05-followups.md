---
maturity: M2 — engine-managed follow-through tracker
ownership: cephalon-managed (Cephalon-Neza maintains this doc inline with every architecture-review-driven slice)
---

# May 2026 architecture-review follow-ups

> See also: [`architecture-review-2026-05.md`](architecture-review-2026-05.md), [`planning-governance.md`](planning-governance.md), [`engine-roadmap.md`](engine-roadmap.md), [`engine-backlog.md`](engine-backlog.md), [`long-range-direction.md`](long-range-direction.md), [`engineering-standards.md`](engineering-standards.md), [`project-memory.md`](project-memory.md).

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

## Recommendations: Next 30 days (≈ May 2026)

| Recommendation | Status | Closing slice(s) | Closed date | Notes |
| --- | --- | --- | --- | --- |
| Ship the cross-link pass that wires `long-range-direction.md` and `engineering-standards.md` into `module-authoring.md`, `learning-roadmap.md`, `compatibility.md`, and `architecture/design-patterns-reference.md` | `shipped` | (cross-link pass) | 2026-05-03 | Closure recorded in May review *Updated gap* #1. All four target docs now reference both anchor docs in their *See also* prefaces. |
| Decide and document architecture-review cadence (monthly snapshot vs. reactive); update `planning-governance.md` accordingly | `shipped` | (`planning-governance.md` lines 97-117) | 2026-05-03 | Closure recorded in May review *Updated gap* #2. Monthly cadence with no-change-month posture is now declared. |
| Begin the deployment-mode validation harness work (PowerShell script + companion doc + first claim audit) so the trim/AOT/single-file claim story has a path to truthful proof before the .NET 11 final release window | `partial` | `ENG-426`, `ENG-427`, `ENG-431`, `ENG-432`, `ENG-433`, `ENG-434`, `ENG-435`, `ENG-436`, `ENG-437`, `ENG-438`, `ENG-439` | 2026-05-05 | The harness PowerShell + Pester baseline shipped earlier (`ENG-331`); `ENG-426` added the per-package hazard inventory and `ENG-427` ported it into machine-readable manifest entries. The `ENG-431` → `ENG-439` extension wave then strengthened the path-truth Pester layer (`ENG-431`), reclassified `Cephalon.Behaviors.Http` `low → medium` (`ENG-432`), broadened the inventory pattern set and promoted `Cephalon.Data` to `medium` (`ENG-433`), classified `Cephalon.Data.MySql` `high` + `Cephalon.Data.Postgres` / `Cephalon.Data.Oracle` `medium` (`ENG-434`), classified `Cephalon.Abstractions` `low` plus added two `Cephalon.Behaviors.Http` enum-extension hazards (`ENG-435`), and closed every remaining `ENG-433` not-yet-classified item across `Cephalon.Behaviors` (`ENG-436`), `Cephalon.Behaviors.Http` (`ENG-437`), and `Cephalon.ReferenceDocs` (`ENG-438` classification + `ENG-439` permanent `not-claimed` posture declared in csproj plus `requiredProjectProperties` seeded in the manifest entry). After the wave the inventory carries 5 `high` + 14 `medium` + 1 `low` + 2 `excluded-by-design` = 22 entries, and `Cephalon.ReferenceDocs` is the first package with a machine-checkable permanent `not-claimed` posture. Still pending: project-property declarations on the first *clean-baseline* packages to claim `claim-truthful` for any of the three modes; the harness itself does not yet emit the inventory programmatically; structural remediation slices for the remaining 4 `high`-tier packages (source-gen-emitted module manifest + adapter table + behavior-type registry; for `Cephalon.Data.MySql`, an upstream `SciSharp.MySQL.Replication` API surface change or a fully public re-implementation of the binlog transport). |
| Continue the truthful slice cadence for governance, CDC, traffic automation, and feature flags; do not slow code velocity to wait for docs | `shipped` (continuing) | (47+ slices) | ongoing | The May 4-5 wave alone shipped over 25 PRs across the redaction adoption, conformance-matrix drift-fix, Resilience extraction, and trim/AOT inventory arcs without slowing on doc cross-links — the discipline is working. |

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
| Close the truthful trim/AOT/single-file claim story end-to-end: project properties, manifest, validation script, workflow, docs, project memory, package-publishing guidance, and any roadmap/backlog cards updated together in one slice | `partial` | `ENG-426`, `ENG-427`, `ENG-431`..`ENG-439` | 2026-05-05 | Inventory + manifest seed + path-truth Pester layer shipped, and the `ENG-432`..`ENG-439` deeper-read wave classified every previously-unclassified hazard surface so the inventory aggregate posture is now 5 `high` + 14 `medium` + 1 `low` + 2 `excluded-by-design` = 22 entries with no remaining `not-yet-classified` callouts. Remaining: structural remediation for the 4 `high` packages whose remediation is still a source-gen rewrite (`Cephalon.Engine`, `Cephalon.Behaviors`, `Cephalon.Behaviors.Patterns`, `Cephalon.Data.MySql`); the 5th `high`-tier package `Cephalon.ReferenceDocs` already shipped its permanent `not-claimed` posture machine-checkably via `ENG-438`/`ENG-439` so it does not need structural remediation. Also remaining: uniform remediation for the 10 `medium` `Cephalon.EventSourcing.*` packs (compile-time event-type registry), uniform remediation for the 3 `medium`-tier CDC capture services in `Cephalon.Data.Postgres` / `Cephalon.Data.Oracle` (small `ICaptureFailureMetadata` interface plus typed-exception registry), the harness publish-phase emitting the inventory programmatically, and the first project-property declarations that intentionally claim a mode for a clean-baseline reference package. |
| Review whether the "May 2026" architecture review cadence assumption holds; if July is going to be the third dated review, decide before the cadence becomes accidentally permanent | `pending` | — | — | The June architecture review is the natural anchor for this decision. The monthly cadence declaration in `planning-governance.md` already supports continuing it; the open question is whether July warrants a full review or only a no-change-month snapshot. |
| Run a planning-governance review against the new long-range-direction horizons to confirm `engine-roadmap.md` phase plans still align with the planning frame; rewrite affected horizon sections in place rather than appending | `pending` | — | — | The long-range direction doc shipped in April. A targeted alignment review in late May or June is the right window so any phase-plan rewrite happens before the Sprint 126 planning round. |

## Risks tracked from the May review

| Risk | Status | Closing / mitigation slice(s) | Notes |
| --- | --- | --- | --- |
| #1 — Documentation cross-link debt grows faster than code | `closed for May` | (cross-link pass shipped May 3) | Carry forward to June only if a future arc ships without a parallel doc-pass. |
| #2 — Maturity-label communication still asymmetric across surface families | `closed` | `ENG-382`..`ENG-387`, `ENG-393` | Per-page badge convention adopted everywhere. |
| #3 — Trim, AOT, and single-file claim story is still not closed | `partial` | `ENG-426`, `ENG-427`, `ENG-431`..`ENG-439` | See the Next-90-days recommendation above. The `ENG-431`..`ENG-439` extension wave closed every previously-unclassified hazard surface and shipped the first machine-checkable permanent `not-claimed` posture (`Cephalon.ReferenceDocs` via `ENG-439`). |
| #4 — Default-path dilution risk has not changed | `pending — re-check quarterly` | — | Latent risk; intentionally tracked rather than actioned. |
| #5 — Sub-agent and AI tooling assumptions need to remain reversible | `pending — re-check quarterly` | — | Project-memory pins specific tooling decisions but preserves reversibility. |

## Updated gaps tracked from the May review

| Gap | Status | Closing slice(s) | Notes |
| --- | --- | --- | --- |
| #1 — May 2026 cross-link pass for long-range-direction and engineering-standards | `closed` | (May 3 cross-link pass) | Recorded in the review doc itself. |
| #2 — Architecture review month-to-month cadence is now de facto monthly | `closed` | (`planning-governance.md` lines 97-117) | Recorded in the review doc itself. |
| #3 — Deployment-mode claim validation needs a real harness | `partial` | `ENG-331`, `ENG-426`, `ENG-427`, `ENG-431`..`ENG-439` | The harness exists and runs in CI; the inventory feeding it is now repo-owned and every package classified. The gating work is the first claim-truthful flip plus the harness publish-phase emitting the inventory programmatically. |
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
