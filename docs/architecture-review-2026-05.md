# Cephalon Architecture Review - May 2026

Review date: `May 2, 2026` (last extended `May 5, 2026` with the redaction-adoption + cleanup-discipline arc, the gap-closure sweep, the per-package OTel emission baselines for Cephalon.Agentics + Cephalon.Retrieval + Cephalon.Worker, the conformance-matrix drift-closure cluster, the Cephalon.Resilience extraction, the doc-cross-link refresh wave, and the per-package trim/AOT/single-file hazard inventory `ENG-426`)

Cross-references: [`architecture-review-2026-04.md`](architecture-review-2026-04.md), [`architecture-review-2026-05-followups.md`](architecture-review-2026-05-followups.md) (live status tracker for every recommendation/risk/gap below), [`architecture.md`](architecture.md), [`architecture-inventory.md`](architecture-inventory.md), [`architecture-recommendations.md`](architecture-recommendations.md), [`architecture/rest-endpoint-authoring-strategy.md`](architecture/rest-endpoint-authoring-strategy.md), [`architecture/design-patterns-reference.md`](architecture/design-patterns-reference.md), [`database-topology.md`](database-topology.md), [`learning-roadmap.md`](learning-roadmap.md), [`project-memory.md`](project-memory.md), [`long-range-direction.md`](long-range-direction.md), [`engineering-standards.md`](engineering-standards.md), [`engine-surface-maturity-audit.md`](engine-surface-maturity-audit.md), [`dotnet11-readiness.md`](dotnet11-readiness.md).

## Purpose

This review extends the [April 2026 architecture review](architecture-review-2026-04.md) with a May 2026 dated snapshot. It does not replace the April review; the April review still stands as the previous month's truth.

This review answers the same four questions for May:

1. what is now stronger than it was at the April review
2. where the main architectural risks have shifted
3. which gaps still matter most
4. what the recommended next moves are for May–July 2026

## What changed since April 13, 2026

Between the April review and this May review, the repository shipped a substantial governance, multi-tenancy, CDC runtime, cell-based-architecture, and traffic-automation slice run, plus two new long-range planning anchors.

Notable shipped slices (compressed):

- **multi-tenancy governance proofs**: `ENG-235` through `ENG-256` promoted twenty-one governance proofs on top of `Cephalon.MultiTenancy.Governance`, covering membership, invitations, domain ownership, governance actions, durable governance state, action workflows, in-process workflow transitions, runtime stores, in-process domain-ownership verification workflows, proof evaluation, proof challenge issuance, proof publication planning, on-demand HTTP file proof collection, proof verification runner orchestration, configured DNS TXT proof collection, bounded on-demand proof polling, automatic background proof polling, HTTP file proof publication, host-driven tenant administration workflow commands, the first ASP.NET Core tenant-administration endpoint, and host-agnostic tenant-invitation delivery dispatch
- **CDC runtime**: `ENG-124` through `ENG-156` promoted the data-product runtime, the CDC capture runtime baseline, live runtime-state, typed operational posture, the shared hosted-execution substrate, acknowledgement and checkpoint-commit follow-through, the execution-runtime catalog, execution-ownership binding, external execution-runtime declaration, the first MongoDB change-stream provider runner, external CDC runtime live-reporting with retry/ordering/freshness/edge-aware reporter coordination, the first SQL Server CDC provider runner, and the first PostgreSQL logical-replication provider runner with publication and slot lifecycle truth, plus richer multi-reporter reconciliation
- **cell-based architecture and traffic automation**: `ENG-120` through `ENG-150` shipped the cell boundary baseline, governed cell-route runtime, cell health-isolation runtime, configuration-driven traffic automation, provider-aware and edge-aware targeting, the provider-managed traffic materializer baseline, the first edge-runtime traffic materializer, multi-provider reconciliation hardening, the first concrete `Cephalon.Edge.KubernetesGateway` and `Cephalon.Edge.Traefik` provider materializers with live observation overlays plus apply-and-reconcile loops, shared control-plane ownership lifecycle truth, provider-native lifecycle execution, provider-native cleanup sweeps, and richer provider-native condition semantics
- **two new planning anchors**: this session shipped [`long-range-direction.md`](long-range-direction.md) (multi-horizon engine direction across 3/5/8/10/15/20/30+ year horizons) and [`engineering-standards.md`](engineering-standards.md) (consolidated standards baseline naming code quality gates, library design, packaging, testing, security, and documentation discipline as the single index for "what high quality means here")
- **`.NET 11` readiness anchor refresh**: [`dotnet11-readiness.md`](dotnet11-readiness.md) now reflects May 2, 2026 truth with full Preview 1/2/3 history and a structured Preview 4 expectation
- **memory contract additions**: `project-memory.md` now pins the OpenAI Codex CLI 5.5 as the primary code-management seat and the `Cephalon-Neza` Git/GitHub account as the default commit/push attribution
- **redaction adoption arc end-to-end (`ENG-357` through `ENG-377`)**: ship the `IRedactionFilter` + `RedactionContext` contract types under `Cephalon.Diagnostics.Redaction`, the `KeyMatchRedactionFilter` + `RegexRedactionFilter` starters with 18 unit tests, the `RedactionPipeline` orchestration helper with 8 tests, the `IServiceCollection.AddRedactionPipeline()` DI extension with 7 tests, M1 wiring at three real engine emission sites (`Cephalon.AspNetCore`'s `HttpRequestResponseLoggingMiddleware`, `Cephalon.Engine`'s `EngineRuntime` module-phase activity tags, `Cephalon.Eventing.Wolverine`'s `WolverineEventDispatchHostedService` dispatch-time tags) with 6 integration tests covering all three sites, the canonical recipe documented in `docs/components/diagnostics.md` and adopted by all 5 samples, the `EngineBuilder` build-time activity tags declared as a deliberate scope boundary (build runs before DI is wired so the pipeline isn't yet resolvable; the three values are non-sensitive by construction), and the entry-point onboarding doc (`getting-started.md`) updated to surface the recipe + the diagnostic-id registry — 39/39 redaction tests pass; the surface is now genuinely production-ready end-to-end
- **cleanup discipline arc (`ENG-370` through `ENG-373`)**: ship `docs/diagnostic-id-registry.md` as the authoritative central allocation table for per-package `EventId` ranges (16 ranges allocated, reserved-gap discipline declared, no-new-allocations-below-2000 collision guard, convention block for future maintainers); strip stale `RS0026`/`RS0027` `<NoWarn>` suppressions from 100 csproj files (every suppression was already dead — `dotnet build CephalonEngine.slnx -c Release` hits 0 violations); audit the surviving non-RS0026/RS0027 suppressions and remove a fully-stale 5-rule Meziantou + Roslynator block on `Cephalon.Abstractions` while preserving + documenting the genuinely-load-bearing `RS0041` on `Cephalon.AspNetCore.Grpc` (Grpc.Tools-generated nullable-oblivious code); document every surviving genuine suppression with an inline rationale comment so a future maintainer can tell intentional from stale at a glance
- **maturity audit + ops-hardening doc refresh**: bump `engine-surface-maturity-audit.md` to name the redaction surface as part of the `Cephalon.Diagnostics` row (5 sources/meters + redaction surface + 3 M1 emission sites + recipe doc + 5-sample adoption); refresh `operational-hardening-gap-inventory.md` *Shipped baseline* + *Evidence in code* + gap `#34` (structured diagnostics + event ids) to point at the new central registry as the authoritative allocation table

The April review already noted that "the next challenge is not inventing a new center; the next challenge is hardening and aligning the broad surface area that now exists." May's slices have continued exactly along that line: depth, ownership truth, runtime-catalog parity, and lifecycle posture rather than fresh categories.

## Updated strengths

The strengths the April review identified all still hold. Four are now visibly stronger.

### 1. Runtime ownership truth is now provable across multi-provider control planes

`ENG-138` through `ENG-150` proved the cell-traffic-automation seam across two real control planes (Kubernetes Gateway and Traefik) with the same shared ownership/dependency/drift/lifecycle vocabulary. The same `/engine/cell-traffic-automations*` route, the same `snapshot.CellTrafficAutomations` data, and the same provider-specific surfaces all answer the same lifecycle truth. That is the kind of shared truth a mature framework needs to claim "host-agnostic" honestly.

### 2. Multi-tenancy governance is now end-to-end provable

`ENG-235` through `ENG-256` carried the governance story from a single `tenant-resolution` proof all the way through tenant administration workflow commands and a live ASP.NET Core endpoint, plus host-agnostic invitation delivery dispatch. The base package, the governance companion, and the ASP.NET Core companion all stay on one ownership boundary. That is the kind of multi-package proof other companion families (eventing, observability, behaviors) had to fight for, and it is now reproducible.

### 3. Long-range planning is now repo-owned

The April review observed that "documentation language can drift faster than code." The May addition of `long-range-direction.md` and `engineering-standards.md` puts that drift on a leash by giving contributors and AI agents a single place to read forecast assumptions and quality expectations. Memory now cross-references both.

### 4. Engine-boundary redaction is now provable end-to-end across six emission sites

`ENG-357` through `ENG-377` carried the redaction story from a single contract type (`IRedactionFilter` + `RedactionContext`) through starter implementations, an orchestration helper, a DI extension, M1 wiring at three real engine emission sites (HTTP middleware, engine runtime module-phase tags, Wolverine dispatch tags) with 6 integration tests, recipe documentation, adoption in all five samples, and a deliberate scope-boundary declaration for `EngineBuilder` build-time tags. **Updated May 5, 2026**: the May 4-5 wave extended the surface to **six** real M1 emission sites — `ENG-401` added Cephalon.Agentics tool-dispatch tags (`agentics.tool.dispatch`), `ENG-402` added Cephalon.Retrieval knowledge-indexing + knowledge-query tags (`retrieval.knowledge.index` + `retrieval.knowledge.query`), `ENG-412` added Cephalon.Worker lifecycle tags (`worker.lifecycle.start` + `worker.lifecycle.stop`), each routing through the same `Redact(activity, key, value)` helper pattern resolved lazily from `IServiceProvider`. `ENG-413` added the Worker integration test, `ENG-415..ENG-425` closed the cross-doc drift cluster (Evidence-in-code list, runtime-contract-index `/engine/*` route catalog, conformance-matrix per-package M1-site annotations, Family-summary M2 line, Resolved-alignment-items historical record). The complementary cleanup discipline (`ENG-370` through `ENG-373`) stripped 100+ stale `RS0026`/`RS0027` suppressions across the package family and shipped `docs/diagnostic-id-registry.md` as the authoritative `EventId` allocation table. Together this turns "the engine handles secrets / PII / authentication tokens correctly before they leave the boundary" from a discipline-only principle into a contract-typed surface a security reviewer can verify by reading the recipe doc and the 39+ integration tests, and the static-analysis surface is now actively enforced rather than masked.

## Updated risks

### 1. Slice cadence is faster than docs cross-link cadence (materially closed on May 3, 2026)

In May, twenty-plus governance, CDC, and traffic-automation slices shipped while the docs surface that consumes them (architecture review, learning roadmap, module authoring, design patterns reference, deployment guides) only got partial cross-link updates. This is the same risk April flagged as "documentation language can drift faster than code"; in May it is more concretely about *cross-references not being woven*. The May fix is to schedule cross-link passes deliberately, not to slow down code work. **Update May 3, 2026:** the cross-link pass landed — `module-authoring.md`, `learning-roadmap.md`, `compatibility.md`, and `architecture/design-patterns-reference.md` now all reference `long-range-direction.md` and `engineering-standards.md` in their *See also* prefaces; the redaction-adoption arc (`ENG-357..377`) shipped alongside its own seven-doc cross-link refresh (engine-backlog, components/diagnostics, engine-surface-maturity-audit, operational-hardening-gap-inventory, getting-started, architecture-review-2026-05, conformance-matrix). The risk is therefore materially closed for May; carry it forward to June only if a future arc ships without the parallel doc-pass.

### 2. Maturity-label communication still asymmetric across surface families

`engine-surface-maturity-audit.md` is current and honest, but the April risk that "feature maturity is not yet labeled sharply enough" is only partially closed. New CDC, governance, and traffic-automation surfaces all carry maturity labels in roadmap/backlog cards, but the human-facing docs (component pages, README sections, getting-started doc) still do not surface them prominently. Adopters who never read the maturity audit may still treat `M1` or `M2` like fully-claimed runtime ownership.

### 3. Trim, AOT, and single-file claim story is still not closed

April flagged this as a "next 60 days" item. As of May 2, 2026, the explicit `not-claimed` posture in `scripts/deployment-mode-support.json` and `cephalon doctor` is still the truth. The new `engineering-standards.md` baseline restates this as a baseline quality gate. With `.NET 11` final release expected November 2026, May–July is the right window to close the claim story before adopters start asking for AOT/single-file by default. **Update May 5, 2026:** the per-package hazard inventory shipped as [`docs/trim-aot-hazard-inventory.md`](trim-aot-hazard-inventory.md) (`ENG-426`); it enumerates the 14 reflection / dynamic-code call sites in shipped Cephalon runtime code, classifies them by severity (`high`: 3 packages — `Cephalon.Engine` `ModuleDiscovery`, `Cephalon.Behaviors` type registry, `Cephalon.Behaviors.Patterns` durable adapter; `medium`: 10 packages — uniform `Type.GetType(persistedString)` across the `Cephalon.EventSourcing.*` provider family; `low`: 1 package — `Cephalon.Behaviors.Http` REST result-model wrapper), and maps each tier to the `deploymentModeEligibility` / `knownHazards` / `representativePublishTargets` manifest fields. The claim story was still open at that point, but the per-package surface that the harness needs to validate became repo-owned and dated; the later `ENG-449` update below narrows the remaining gap to support-claim promotion rather than harness/schema existence.

**Update May 5, 2026 (`ENG-449`):** the harness and manifest schema are no longer pending; the remaining gap is support-claim promotion. `representativePublishTargets.projects` now covers five sample hosts (`ModularMonolith`, `ModularVerticalSlice`, `Microservice`, `MicroserviceSuite` CatalogService, and `Showcase`), and the expanded set publishes cleanly under `singleFile` after retiring the first `ManifestModule.Name` / `Assembly.Location` analyzer blockers. The support contract still stays `not-claimed` until the project-property declarations, inventory-emitting harness output, and release-validation `-SkipPublish` decision all move deliberately.

### 4. Default-path dilution risk has not changed

April flagged a real product risk that lower-ceremony shorthand could dilute the canonical authoring story. May added more shorthand and authoring-policy surfaces (REST `RestApi:AuthoringPolicies`, generated profile groups, host-governance scope refinements), all on the canonical runtime catalogs. The shorthand is staying truthful; the dilution risk is still latent and worth re-checking each quarter.

### 5. Sub-agent and AI tooling assumptions need to remain reversible

`project-memory.md` now pins specific tooling decisions (Codex CLI 5.5, Cephalon-Neza account). That is operationally useful, but it must stay reversible: as AI tooling evolves quickly through 2026–2027, the engine's own contracts must not assume any specific external authoring tool. The new long-range-direction `Horizon 1` section already names this concern; the standards baseline should make it explicit that Codex CLI is a *contributor* tool, not part of the engine's runtime contract.

## Updated gaps

### 1. A May 2026 cross-link pass for long-range-direction and engineering-standards (closed)

**Closed May 3, 2026.** All four target docs now cross-link `long-range-direction.md` and `engineering-standards.md` in their *See also* prefaces: `module-authoring.md` line 5, `learning-roadmap.md` line 16 (plus a *Cross-references* section at the bottom), `compatibility.md` line 5, and `architecture/design-patterns-reference.md` line 8. Contributors and AI agents now traverse to the new anchors from any of the four entry points. Original gap statement (preserved for historical traceability): the new anchor docs were not referenced from these four entry points; without those cross-links, contributors and AI agents may not traverse to the new anchors.

### 2. Architecture review month-to-month cadence is now de facto monthly (closed)

**Closed May 3, 2026.** [`docs/planning-governance.md`](planning-governance.md) lines 97-117 document the architecture-review cadence as monthly snapshots: "Cephalon publishes a dated architecture-review snapshot once per month. The cadence is intentional: monthly snapshots are short enough to capture meaningful slice progress without becoming a quarterly report." The cadence rules also declare that a no-change month still ships a short `nothing material changed; previous review still stands` snapshot so the dated history stays unbroken, and that supersession is explicit — new recommendations supersede old only where they explicitly address them. Original gap statement (preserved for historical traceability): April was the first dated architecture review. May is now the second. There is no written commitment that a monthly review snapshot is the cadence. If the cadence is monthly, the planning governance doc should record that. If the cadence is reactive (only when scope changes warrant), April and May should explicitly say so. Pick a posture and document it.

### 3. Deployment-mode claim validation needs a real harness

The April recommendation to "finish the truthful trim/AOT/single-file claim story" still needs a validation harness that runs alongside `scripts/validate-dotnet-readiness.ps1` and emits a machine-readable claim-validation report. This harness is the gate that lets future trim/AOT/single-file claims become real support statements.

### 4. AI/agent-facing readability of the runtime contract (closed)

**Closed May 3, 2026.** `docs/runtime-contract-index.md` shipped as the consolidated map of every `/engine/*` HTTP route, every `snapshot.*` data key, and every runtime catalog interface that an operator, an AI agent, or external tooling can read to know what the engine is actually doing. Original gap statement (preserved for historical traceability): Long-range Horizon 3 raises this for ten-to-twenty-year planning, but it is also a near-term concern: `/engine/*` routes and `snapshot.*` data are already machine-readable, but there is no consolidated "agent-readable contract index" doc that lets an autonomous agent (or a non-Cephalon AI consumer) discover the full runtime catalogue without scraping each individual route. A future `docs/runtime-contract-index.md` could solve this.

### 5. Conformance matrix stayed open since April (closed)

**Closed May 3, 2026.** `docs/conformance-matrix.md` shipped with per-package rows (maturity, ownership mode, engine routes, snapshot keys, catalog interfaces, notes) plus *Family summary at a glance* groupings; the May 3 redaction-arc PR refreshed the `Cephalon.Diagnostics` row to include the new redaction surface and the central diagnostic-id registry. Original gap statement (preserved for historical traceability): The April gap "a stronger conformance matrix is still needed" is still open. The April recommendation tied conformance to the maturity audit; May has not yet shipped the matrix itself. This remains a high-leverage piece of work because it is the operator-facing truth that lets adopters compare provider packs apples-to-apples.

## Architecture recommendations

The recommendations below assume `engine-roadmap.md` and `engine-backlog.md` continue to be the durable planning record; this section names the *direction*, not the cards.

### Next 30 days (≈ May 2026)

- ~~ship the cross-link pass that wires `long-range-direction.md` and `engineering-standards.md` into `module-authoring.md`, `learning-roadmap.md`, `compatibility.md`, and `architecture/design-patterns-reference.md`~~ — **shipped on May 3, 2026; see Updated gap #1 closure note**
- ~~decide and document architecture-review cadence (monthly snapshot vs. reactive); update `planning-governance.md` accordingly~~ — **shipped on May 3, 2026 (`planning-governance.md` lines 97-117 declare monthly cadence with no-change-month posture); see Updated gap #2 closure note**
- begin the deployment-mode validation harness work (PowerShell script + companion doc + first claim audit) so the trim/AOT/single-file claim story has a path to truthful proof before the .NET 11 final release window
- continue the truthful slice cadence for governance, CDC, traffic automation, and feature flags; do not slow code velocity to wait for docs

### Next 60 days (≈ June 2026)

- ~~close the conformance matrix gap with a per-provider/per-companion matrix that consumes `engine-surface-maturity-audit.md` truth and projects it for adopters~~ — **shipped on May 3, 2026 (`docs/conformance-matrix.md` exists with per-package rows + family summary; the redaction-arc PR refreshed the `Cephalon.Diagnostics` row); see Updated gap #5 closure note. Per-provider/per-companion *extension* of the existing matrix remains future work**
- promote maturity labels into the human-facing component docs and getting-started flows so adopters see `M0`/`M1`/`M2`/`M3`/`M4` and the four ownership modes without having to read the audit page
- run a `.NET 11` analyzer-drift pass against Preview 4 (expected on or near May 12, 2026) and Preview 5 (June) so analyzer-only readiness signals stay current
- ~~consider drafting a `docs/runtime-contract-index.md` that consolidates `/engine/*` routes and `snapshot.*` data into one machine-readable contract index for AI consumers and operators~~ — **shipped on May 3, 2026; see Updated gap #4 closure note**

### Next 90 days (≈ July 2026)

- close the truthful trim/AOT/single-file claim story end-to-end: project properties, manifest, validation script, workflow, docs, project memory, package-publishing guidance, and any roadmap/backlog cards updated together in one slice
- review whether the "May 2026" architecture review cadence assumption holds; if July is going to be the third dated review, decide before the cadence becomes accidentally permanent
- run a planning-governance review against the new long-range-direction horizons to confirm `engine-roadmap.md` phase plans still align with the planning frame; rewrite affected horizon sections in place rather than appending

## Alignment with long-range direction

This review references each long-range horizon explicitly so future reviewers can audit alignment without rediscovering the frame.

- **Horizon 1 (Near, 3–5 y)**: `.NET 11` readiness lane stays the right shape; Codex CLI 5.5 as primary contributor seat is recorded in memory; AI seats are treated as default authoring path. May's recommendations primarily live in this horizon.
- **Horizon 2 (Mid, 5–10 y)**: governance, multi-tenancy, edge traffic automation, CDC, and feature-flag work continue to expand the additive primitives Horizon 2 expects (policy-as-code, sovereignty, AI/provider routing seeds). No mid-horizon commitment was destabilized in May.
- **Horizon 3 (Far, 10–20 y)**: the new "agent-readable runtime contract index" gap is a Horizon 3 concern. The engine's commitment to keep declarative behavior topology, resilience, idempotency, and durable execution stays intact.
- **Horizon 4 (Very far, 20–30+ y)**: nothing shipped in May threatens the "small, additive, introspectable, well-documented core" stance.

## Alignment with engineering standards baseline

The new `engineering-standards.md` document names twelve first-class engine qualities (performance, security, usability, reliability, maintainability, scalability, flexibility, compatibility, data integrity, availability, auditability, compliance). Each May slice can be mapped to one or more of those qualities; the recommendations above are concentrated on **maintainability**, **compatibility**, **auditability**, and **availability**. Future architecture reviews should preserve this naming so commit messages, PR descriptions, and review notes stay aligned with the baseline.

## Bottom line

April's bottom line still holds: Cephalon's architectural direction is strong; the next challenge is not inventing a new center but hardening and aligning the broad surface area.

May's addition is that the *planning frame itself* is now repo-owned through `long-range-direction.md` and `engineering-standards.md`. That changes the work from "build everything" to "build deliberately within a known frame." It also raises the documentation cross-link debt because two new anchor docs need to be woven through the existing contributor surface; that weaving is the most important May–June docs work and is already on the recommendations list.

The architectural posture for May 2026 is therefore:

1. keep shipping depth and ownership-truth slices on the existing surface families
2. weave the new planning anchors into the contributor surface
3. close the trim/AOT/single-file claim story before `.NET 11` final release
4. promote maturity labels into adopter-facing surfaces
5. decide and document the architecture-review cadence

That order respects April's recommendation that "the next challenge is hardening and aligning" and adds the May-specific concern that the planning frame itself now needs to be load-bearing across the contributor surface.
