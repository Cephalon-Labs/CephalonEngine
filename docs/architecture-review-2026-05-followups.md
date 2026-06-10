# Architecture review (May 2026) — follow-ups tracker

This document tracks the open follow-through items raised by [`architecture-review-2026-05.md`](architecture-review-2026-05.md) plus the inconsistencies recorded at the tail of [`conformance-matrix.md`](conformance-matrix.md). It is a thin tracking artifact, not a new source of truth: each row points at the durable doc that owns the eventual resolution.

Cross-references: [`architecture-review-2026-05.md`](architecture-review-2026-05.md), [`conformance-matrix.md`](conformance-matrix.md), [`engine-surface-maturity-audit.md`](engine-surface-maturity-audit.md), [`runtime-contract-index.md`](runtime-contract-index.md), [`engine-roadmap.md`](engine-roadmap.md), [`engine-backlog.md`](engine-backlog.md), [`planning-governance.md`](planning-governance.md), [`engineering-standards.md`](engineering-standards.md), [`long-range-direction.md`](long-range-direction.md), [`dotnet11-readiness.md`](dotnet11-readiness.md), [`deployment-mode-support.md`](deployment-mode-support.md).

## Why this document exists

The May 2026 review records five "next 30 days", four "next 60 days", and three "next 90 days" recommendations. The conformance matrix records five "audit pending" inconsistencies at its tail. Without a single tracking page, those follow-throughs are easy to lose between monthly review snapshots, the durable roadmap, and the backlog.

This page is the single navigable read for "what did the May review and the conformance matrix flag as still open, and where does each item live now?". When an item closes, the matching row is updated to point at the resolving slice (`ENG-*` id, commit, or shipped-doc anchor) and stays in this page as durable history; nothing in this page is deleted just because it was resolved.

This page does not introduce new policies. Standing operating rules still live in [`AGENTS.md`](../AGENTS.md), [`planning-governance.md`](planning-governance.md), and [`engineering-standards.md`](engineering-standards.md).

## How to read each row

Every row uses the same fields:

- **Source** — the doc that flagged the item (May review, conformance matrix, audit, etc.)
- **Theme** — which engine quality dimension from [`engineering-standards.md`](engineering-standards.md) the work most directly advances (Performance / Security / Usability / Reliability / Maintainability / Scalability / Flexibility / Compatibility / Data integrity / Availability / Auditability / Compliance)
- **Risk** — `non-breaking`, `additive`, or `deliberate-break` (the latter requires the obsoletion path documented in [`compatibility.md`](compatibility.md))
- **Status** — `open`, `in-flight`, or `closed (resolution: <id-or-anchor>)`
- **Owner home** — the durable doc that should record the eventual resolution; this page only mirrors the open/closed state

## May 2026 review — open follow-throughs

### Next 30 days (May 2026)

| Item | Source anchor | Theme | Risk | Status | Owner home |
| --- | --- | --- | --- | --- | --- |
| Cross-link `long-range-direction.md` and `engineering-standards.md` into `module-authoring.md`, `learning-roadmap.md`, `compatibility.md`, `architecture/design-patterns-reference.md` | [May review § Next 30 days, item 1](architecture-review-2026-05.md) | Maintainability | non-breaking | closed (resolution: cross-links present in `module-authoring.md` line 5, `learning-roadmap.md` lines 16 and 599-600, `compatibility.md` line 5, and `architecture/design-patterns-reference.md` line 8) | `architecture-review-2026-05.md`, durable in respective doc heads |
| Decide and document architecture-review cadence in `planning-governance.md` | [May review § Next 30 days, item 2](architecture-review-2026-05.md) | Maintainability | non-breaking | closed (resolution: [`planning-governance.md` § Architecture review cadence](planning-governance.md) lines 97-117 declare monthly cadence with extends-not-replaces semantics) | `planning-governance.md` |
| Begin deployment-mode validation harness — PowerShell script, companion doc, first claim audit | [May review § Next 30 days, item 3](architecture-review-2026-05.md) | Compatibility | non-breaking | in-flight (PowerShell harness drafted per session-handoff `0438f872`, `8cb74fc9`, `0c297326`, `713c97d3`, `c949aa69`, plus Pester suites and CI wiring; first claim audit run still TBD) | `deployment-mode-support.md`, `dotnet11-readiness.md`, `scripts/validate-deployment-mode-claims.ps1` |
| Continue truthful slice cadence for governance, CDC, traffic automation, feature flags | [May review § Next 30 days, item 4](architecture-review-2026-05.md) | Reliability | additive | open (continuous; no specific gate) | `engine-roadmap.md`, `engine-backlog.md` |

### Next 60 days (June 2026)

| Item | Source anchor | Theme | Risk | Status | Owner home |
| --- | --- | --- | --- | --- | --- |
| Close conformance matrix gap — per-provider/per-companion matrix consuming `engine-surface-maturity-audit.md` truth | [May review § Next 60 days, item 1](architecture-review-2026-05.md) | Auditability | non-breaking | closed (resolution: [`conformance-matrix.md`](conformance-matrix.md) ships the per-package matrix across all 12 families with maturity, ownership, engine routes, snapshot keys, and catalog interfaces) | `conformance-matrix.md` |
| Promote maturity labels into human-facing component docs and getting-started flows | [May review § Next 60 days, item 2](architecture-review-2026-05.md) | Usability | non-breaking | closed (resolution: a standardized maturity/ownership/family banner is now injected directly under the H1 of every shipped `docs/components/*.md` (101 files), sourcing maturity and ownership truth from the conformance matrix and pointing back at the audit + matrix; `getting-started.md` already carries the maturity-labels explainer at its top § "Understanding maturity and ownership labels") | `docs/components/*.md`, `getting-started.md` |
| Run `.NET 11` analyzer-drift pass against Preview 4 (≈ May 12) and Preview 5 (June) | [May review § Next 60 days, item 3](architecture-review-2026-05.md) | Compatibility | non-breaking | open (Preview 4/5 not yet incorporated into `dotnet11-readiness.md` analyzer-signal table at the time this tracker was filed) | `dotnet11-readiness.md`, `scripts/validate-dotnet-readiness.ps1` |
| Draft `docs/runtime-contract-index.md` consolidating `/engine/*` routes and `snapshot.*` data into one machine-readable contract index | [May review § Next 60 days, item 4](architecture-review-2026-05.md) | Auditability | non-breaking | closed (resolution: [`runtime-contract-index.md`](runtime-contract-index.md) ships with route catalog, snapshot data key catalog, runtime catalog interface catalog, and AI-consumer discovery patterns) | `runtime-contract-index.md` |

### Next 90 days (July 2026)

| Item | Source anchor | Theme | Risk | Status | Owner home |
| --- | --- | --- | --- | --- | --- |
| Close trim/AOT/single-file claim story end-to-end — project properties, manifest, validation script, workflow, docs, project memory, package-publishing guidance, roadmap/backlog cards in one slice | [May review § Next 90 days, item 1](architecture-review-2026-05.md) | Compatibility | non-breaking until a claim flips from `not-claimed` to `claimed`; flipping a claim is a deliberate, manifest-driven contract change | open (harness in flight; no claim has been flipped to `claimed` yet; the `claim-truthful` verdict gate from `planning-governance.md` is the authoritative promotion criterion) | `deployment-mode-support.md`, `scripts/validate-deployment-mode-claims.ps1`, `dotnet11-readiness.md`, `package-publishing.md`, `project-memory.md`, `engine-roadmap.md` |
| Decide whether the dated architecture-review cadence remains monthly when July rolls in | [May review § Next 90 days, item 2](architecture-review-2026-05.md) | Maintainability | non-breaking | open (monthly cadence is documented; the explicit July check is a calendar event, not a code change) | `planning-governance.md` |
| Run a planning-governance review against the long-range-direction horizons to confirm `engine-roadmap.md` phase plans still align | [May review § Next 90 days, item 3](architecture-review-2026-05.md) | Maintainability | non-breaking | open (no planned slice yet) | `planning-governance.md`, `long-range-direction.md`, `engine-roadmap.md` |

## Conformance matrix — audit-pending rows

These rows surfaced during the conformance matrix build and are recorded at the tail of [`conformance-matrix.md`](conformance-matrix.md) as candidate `ENG-*` cards. Each is `non-breaking` and `additive`: the resolution is alignment, not API change.

| # | Surface | Inconsistency | Resolution path | Status |
| --- | --- | --- | --- | --- |
| 1 | `Cephalon.Behaviors.Http` REST publication ownership | Audit labels M2 / mixed; component doc says module-owned activation + Cephalon-managed materialization. Matrix should split authoring vs. materialization labels explicitly. | Update [`engine-surface-maturity-audit.md`](engine-surface-maturity-audit.md) row plus the matrix row in one slice. | closed (resolution: matrix row now records `application-managed` profile/publication activation + `cephalon-managed` materialization, governance, and runtime catalogs explicitly; audit doc row already captured the split) |
| 2 | Event-sourcing family audit coverage | Eleven `Cephalon.EventSourcing.*` packs are M1 in the matrix but the audit does not enumerate them individually. | Add an event-sourcing family section to [`engine-surface-maturity-audit.md`](engine-surface-maturity-audit.md) confirming the catalog-only stance per pack, and update the matrix rows to reference that audit row. | closed (resolution: audit doc now carries a consolidated event-sourcing family row that confirms M1 catalog-only stance for `Cephalon.EventSourcing` and all 10 provider packs; per-row Notes-field tags on those 11 matrix rows now read "(family-covered by maturity audit)" instead of the legacy "(audit pending)" placeholder, completing the matrix-side mirror of the audit-doc truth) |
| 3 | Observability provider audit closure | Cloud platform packs (AWS, GCP, Azure Monitor, etc.) ship as M1 configuration-binding packs, but the audit doc does not enumerate each one. | Add an observability-provider section to the audit doc that confirms maturity per cloud, or schedule an audit refresh during the next architecture-review cycle. | closed (resolution: audit doc now carries a consolidated observability core + exporter + dependency-health row that confirms `Cephalon.Observability` core at M2, cloud configuration packs and `DependencyHealth.Core` at M1, per-provider dependency-health probe packs at M0 taxonomy-only; per-row Notes-field tags on those 15 matrix rows now read "(family-covered by maturity audit)" instead of the legacy "(audit pending)" placeholder, completing the matrix-side mirror of the audit-doc truth) |
| 4 | Multi-tenancy delivery sender maturity granularity | Six invitation-delivery sender packs share a single M2 label, but adoption-quality proof requirements differ (HTTP webhook vs. SMTP relay vs. SaaS API vs. Microsoft Graph). | If any sender needs to advance independently, split the M2 label per sender in the audit doc plus the matrix row plus the sender's own component doc, in one slice. Otherwise, document the shared-M2 stance explicitly so future readers do not retry the question. | closed (resolution: audit doc now carries a consolidated multi-tenancy invitation delivery sender family row that records the deliberate shared-M2 stance for HttpDelivery, SmtpDelivery, SendGridDelivery, MailgunDelivery, AmazonSesDelivery, MicrosoftGraphDelivery plus AspNetCore and AzureIdentity companions, and names the criteria for per-sender promotion above the family floor) |
| 5 | `Cephalon.Audit` and `Cephalon.Identity` route projection | Both packs are M1 (taxonomy-only descriptors) yet expose `/engine/audit-*` and `/engine/authorization-policies` routes through `Cephalon.AspNetCore`. | Clarify in the audit doc whether the routes are M1 projections of M1 contracts or whether some runtime truth has shipped, and update the matrix row plus the component doc together. | closed (resolution: audit doc now carries explicit `Cephalon.Audit`, `Cephalon.Audit.EntityFramework`, `Cephalon.Identity`, `Cephalon.Identity.AspNetCore`, `Cephalon.Ids.Sfid` rows; matrix row Notes now identify each `/engine/*` route as an M1 catalog projection of the matching catalog interface, with `identity-authorization` named explicitly as the runtime surface) |

Resolved rows stay in this table as durable history; only `Status` changes. The matrix tail's "Inconsistencies observed" section mirrors this state — all five items have been moved into a "Resolved alignment items" subsection in [`conformance-matrix.md`](conformance-matrix.md). The matrix tail now opens with `No open inconsistencies remain at this snapshot` and the resolution-history subsection preserves the slice-by-slice trail.

## Test coverage roadmap

The full test coverage roadmap now lives in [`test-coverage-roadmap.md`](test-coverage-roadmap.md). It records:

- the layered test-project shape across `Cephalon.Tests`, `Cephalon.Tests.Composition`, `Cephalon.Tests.Hosting`, `Cephalon.Tests.Scripts`, `Cephalon.Tests.Support`, `Cephalon.Tests.Tooling` (~204 test files total)
- the per-family gap inventory keyed against current maturity
- a prioritized list of recommended next slices
- the standing rule that new dedicated test projects are added only when a family ships enough independent execution surface to justify a separate harness, and that layered framework-level tests remain the default home for new coverage

The durable backlog should still receive an explicit `ENG-*` card per shipped slice so the planning record stays truthful.

### Recommended-slice status (May 5, 2026)

The roadmap originally listed seven recommended slices (four high-priority, three medium-priority). Three of the high-priority slices shipped before the roadmap itself was authored on `May 2, 2026`; their resolution anchors are recorded here per the roadmap's own maintenance rule.

| # | Slice | Lands in | Status | Resolution anchor |
| --- | --- | --- | --- | --- |
| 1 | gRPC streaming and error-mode coverage for `Cephalon.AspNetCore.Grpc` | `Cephalon.Tests.Hosting` | closed | `tests/Cephalon.Tests.Hosting/AspNetCoreHostingTests.cs` (`MapCephalonExposesRuntimeAndModuleRoutes` streaming paths and `MapCephalonGrpcReturnsUnimplementedForUnknownMethod` error-mode path) |
| 2 (was #2) | JSON-RPC error-response coverage for `Cephalon.AspNetCore.JsonRpc` | `Cephalon.Tests.Hosting` | closed | `ENG-405` — `tests/Cephalon.Tests.Hosting/JsonRpcErrorResponseHostingTests.cs` |
| 3 (was #3) | Direct unit coverage of `MetadataDrivenAuthorizationEvaluator` decision matrix | `Cephalon.Tests.Composition` | closed | `ENG-403` — `tests/Cephalon.Tests.Composition/Composition/MetadataDrivenAuthorizationDecisionMatrixTests.cs` plus `tests/Cephalon.Tests.Support/IdentityDecisionMatrixTestModule.cs` |
| 4 (was #4) | `IAuditActorAccessor` ambient-context fallback coverage | `Cephalon.Tests.Composition` | closed | `ENG-404` — `tests/Cephalon.Tests.Composition/Composition/AuditActorAndRecorderFallbackTests.cs` |
| 2 (was #5) | SQL Server CDC pump containerized scenario | `Cephalon.Tests.Hosting` | open (medium priority) | n/a — still in [`test-coverage-roadmap.md` § Recommended next slices](test-coverage-roadmap.md#recommended-next-slices) |
| 3 (was #6) | Postgres logical-replication CDC scenario | `Cephalon.Tests.Hosting` | open (medium priority) | n/a — still in [`test-coverage-roadmap.md` § Recommended next slices](test-coverage-roadmap.md#recommended-next-slices) |
| 4 (was #7) | MongoDB change-stream CDC scenario | `Cephalon.Tests.Hosting` | open (medium priority) | n/a — still in [`test-coverage-roadmap.md` § Recommended next slices](test-coverage-roadmap.md#recommended-next-slices) |

Resolved rows stay in this table as durable history; only `Status` changes. New high-priority slices added to the roadmap should be mirrored here with `Status: open` so this tracker stays the single read for "what was recommended and where did it land?".

## Maintenance posture

This document is meant to evolve continuously across architecture-review cycles. Update it when:

- a row in this page resolves — change `Status` to `closed (resolution: <anchor>)` in the same slice that ships the resolution, and keep the row in place so the historical follow-through stays visible
- a new architecture-review snapshot dates a new month — add a top-level section for that month's open follow-throughs and link to the new dated review
- the conformance matrix tail grows a new "Inconsistencies observed" row — mirror it here in the same slice
- the durable owner doc for a row changes — update the `Owner home` field; do not let this page diverge from where the resolution actually lives

Do not put primary truth into this page. If a row's detail grows, move that detail into the `Owner home` doc and link from this page.
