# May 2026 Learning Pass

This document records the output of an autonomous May 2026 learning pass against
the Cephalon docs graph, the current `.NET 11` preview wave, and 2025-2026
industry signal on architecture, design patterns, code quality, and packaging.

It is a thin tracking artifact, not a new source of truth. Each finding points
back at the durable doc that should record any eventual resolution.

Cross-references: [`project-memory.md`](../project-memory.md),
[`long-range-direction.md`](../long-range-direction.md),
[`dotnet11-readiness.md`](../dotnet11-readiness.md),
[`dotnet-ecosystem-reference.md`](../dotnet-ecosystem-reference.md),
[`architecture-patterns-research.md`](../architecture-patterns-research.md),
[`engineering-standards.md`](../engineering-standards.md),
[`package-publishing.md`](../package-publishing.md),
[`learning-roadmap.md`](../learning-roadmap.md),
[`runtime-contract-index.md`](../runtime-contract-index.md),
[`architecture-review-2026-05.md`](../architecture-review-2026-05.md),
[`architecture-review-2026-05-followups.md`](../architecture-review-2026-05-followups.md),
[`compatibility.md`](../compatibility.md).

## Why this document exists

The user-facing scheduled engine task asks Cephalon to keep learning `.NET`
through `.NET 11` and into a multi-decade horizon, while keeping engine quality,
design patterns, architecture, packaging, and standards aligned. The learning
content itself already lives in the durable docs above. What was missing was a
dated record of the *checks* performed during this pass, the *signal* observed,
and the *gaps* worth folding into the next architecture-review or readiness
slice. This page is that record.

This page does not introduce new policy. Standing operating rules still live
in [`AGENTS.md`](../../AGENTS.md), [`planning-governance.md`](../planning-governance.md),
and [`engineering-standards.md`](../engineering-standards.md).

## Pass scope

Performed on `May 2, 2026`. The pass covered:

- a `.NET 11` preview-state web survey (devblogs.microsoft.com/dotnet,
  github.com/dotnet/core, learn.microsoft.com)
- a 2025-2026 industry-signal survey across modular monolith / vertical-slice
  / cell-based architecture, AI-assisted authoring, WebAssembly as a server
  substrate, durable execution, supply-chain trust, and resilience / policy-as-code
- a docs-graph audit for whether the surveyed signal was already named in
  the planning-anchor docs
- a sanity check of the current `.session-handoff.md` and uncommitted git
  state so this pass would not bury earlier work

## `.NET 11` preview state on May 2, 2026

The current shipping floor stays on `net10.0` and the readiness lane stays
in [`dotnet11-readiness.md`](../dotnet11-readiness.md). The pass confirmed the
following dated facts and updated them where the doc only referenced a date
stamp:

- `.NET 11 Preview 3` is still the latest public build (released April 14,
  2026). `Preview 4` has **not** shipped as of May 2, 2026; `dotnet/core` has
  no `release-notes/11.0/preview/preview4/` folder yet, and devblogs has no
  Preview 4 announcement post.
- The expected next-preview window remains the second-Tuesday cadence (≈ May
  12, 2026) per Microsoft's published schedule, but treat that as expectation,
  not commitment, until the official post and folder both publish.
- `.NET 10` LTS support window stays through `November 14, 2028`. `.NET 11`
  final-release target stays `November 2026`.

Preview 3 carried several deltas worth flagging on the readiness lane the next
time [`dotnet11-readiness.md`](../dotnet11-readiness.md) is refreshed. None of
these change Cephalon's shipping floor; they are assessment-only signal:

- **System.IO.Compression** gained a Zstandard (zstd) implementation;
  ASP.NET Core enables zstd by default for response compression and request
  decompression. Cephalon transports and observability packs that already
  rely on default-on Brotli/Gzip should re-evaluate which encoding the host
  actually negotiates under `.NET 11`.
- **Microsoft.AspNetCore.OpenApi** moved to `Microsoft.OpenApi 3.3.1` and now
  targets the OpenAPI 3.2.0 specification. The bump carries breaking changes
  in the underlying library; Cephalon's REST documentation and BFF document
  catalog (`/engine/backend-for-frontend/rest-documents`) consume the host
  OpenAPI surface and should be re-validated against `.NET 11` specifically
  rather than assuming `.NET 10` compatibility.
- **System.Text.Json** added `JsonNamingPolicy.PascalCase` plus per-member
  naming and ignore overrides. This is purely additive but worth noting
  because Cephalon's REST result-envelope policy and behavior-authored REST
  serialization choices are part of the public contract.
- **Entity Framework Core** added `ChangeTracker.GetEntriesForState` and
  `RemoveDbContext` / `RemoveExtension` helpers. The latter pair is
  interesting for Cephalon's database-topology role swapping in
  [`database-topology.md`](../database-topology.md): swapping a provider
  without rebuilding the entire `IServiceProvider` becomes more idiomatic.
- **Kestrel** now starts processing HTTP/3 requests without waiting for the
  control stream and SETTINGS frame first, reducing first-request latency.
  Cephalon claim language around HTTP/3 in component docs should not lag this
  improvement when the readiness lane is exercised.
- **Blazor `Virtualize<TItem>`** behavior changed: `OverscanCount` default
  moved from 3 to 15, and items no longer assume uniform height. Cephalon
  does not own a Blazor host today, but reference samples or future host
  packs that consume `Virtualize<TItem>` would inherit the change.
- **Code analyzers** received bug fixes for `CA1515`, `CA1034`, `CA1859` and
  reduced noise on `CA1873`. Cephalon enables `TreatWarningsAsErrors`
  repo-wide; the dedicated `.NET 11` readiness CI job is the natural place
  to surface any new diagnostics from this change.

Owner home for any durable change: [`dotnet11-readiness.md`](../dotnet11-readiness.md)
plus [`dotnet-ecosystem-reference.md`](../dotnet-ecosystem-reference.md). No
package or runtime contract change is requested by this pass.

## 2025-2026 industry-signal summary

The external signal survey reached six topic anchors. The summary below
records what was observed and where it lands against current Cephalon
direction. Detailed citation is in the per-topic entries.

### Modular monolith / vertical slice / cell-based

- **Signal**: practitioners (Sam Newman, InfoQ cell-based architecture
  pattern catalog, AWS Well-Architected on cell-based architecture, Milan
  Jovanović on modular monolith + vertical slices) have converged on three
  named shapes that solve different problems: modular monolith for code-modular
  but deployment-monolithic shapes, vertical-slice for feature-locality, and
  cell-based for blast-radius containment. Cell-based has stuck as a discrete,
  named industry pattern in 2026, distinct from modular monolith.
- **Cephalon implication**: this validates the existing app-model split
  recorded in [`app-models.md`](../app-models.md) and the cell-based runtime
  baseline recorded in [`project-memory.md`](../project-memory.md) under
  ENG-120 through ENG-150. No engine direction change is requested.
- **Owner home**: [`architecture-patterns-research.md`](../architecture-patterns-research.md),
  [`app-models.md`](../app-models.md).

### AI-assisted authoring as the default seat

- **Signal**: three open standards converged in 2025-2026 around making
  frameworks discoverable to AI agents and tools — Anthropic's Model Context
  Protocol (MCP), Google's Agent2Agent (A2A), and OpenAI's `AGENTS.md`
  convention. The first two define machine-readable surface and inter-agent
  protocols; the third names a repo-local instructions file format. Frameworks
  that already publish typed runtime catalogs are well positioned, but the
  *vocabulary* of these specs is now what tooling expects.
- **Cephalon implication**: Cephalon already ships the substrate. Typed
  runtime catalogs (`/engine/manifest`, `/engine/snapshot`, `/engine/runtime-story`,
  every `/engine/*` route enumerated in [`runtime-contract-index.md`](../runtime-contract-index.md)),
  the maturity-and-ownership labels recorded in
  [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md),
  and the conformance matrix in [`conformance-matrix.md`](../conformance-matrix.md)
  already give an agent enough to reason about Cephalon's contract surface
  without scraping the source. The repo also ships [`AGENTS.md`](../../AGENTS.md)
  in the modern sense (standing operating rules for AI contributors) rather
  than only as a doc index.
  What is *not* yet in the planning anchors is name-level alignment with
  these specs. None of `long-range-direction.md`, `engineering-standards.md`,
  `runtime-contract-index.md`, or `dotnet-ecosystem-reference.md` mentions
  MCP, A2A, or the `AGENTS.md` convention by name today. That is an additive
  doc gap, not a code gap. A future architecture-review slice could name the
  alignment explicitly without changing any runtime contract.
- **Owner home**: [`long-range-direction.md`](../long-range-direction.md)
  (Horizon 1 forecast theme on AI-assisted authoring),
  [`runtime-contract-index.md`](../runtime-contract-index.md) (AI-consumer
  discovery patterns section), [`AGENTS.md`](../../AGENTS.md) (standing
  operating rules).

### WebAssembly as a portable runtime substrate

- **Signal**: `.NET`-on-WASM has matured for client-side workloads
  (Blazor WASM, WasmGC, AOT for compute hot paths), but server-side WASM with
  full WASI support (component model, cryptographic primitives, IO) is still
  speculative as the dominant deployment shape in 2026.
- **Cephalon implication**: Cephalon's host-agnostic-core stance is validated.
  The current decision boundary in [`long-range-direction.md`](../long-range-direction.md)
  Horizon 2 ("keep the runtime contract host-agnostic enough to run inside
  WebAssembly hosts") is the right shape; there is no `M2`-grade WASM host
  pack to ship yet. Watch WASI Component Model maturation as the gate.
- **Owner home**: [`long-range-direction.md`](../long-range-direction.md).

### Durable execution

- **Signal**: Temporal, Dapr Workflows, Azure Durable Functions, and
  DTFx-Azure have converged on the same primitives — workflows as resumable
  code, activities as non-deterministic work, deterministic state replay
  for restart safety. There is no single winner; the primitives are stable.
- **Cephalon implication**: this confirms the shape of the existing
  `IDurableExecution<TState>` baseline (project memory entry under
  Phase 12 follow-through). The durable timer/signal coordination, durable
  compensation helpers, and waiting-state runtime catalogs already match
  the industry primitive set. No engine direction change is requested.
- **Owner home**: project memory durable-execution entries,
  [`engine-roadmap.md`](../engine-roadmap.md) phase 12 records.

### Supply-chain trust + SBOM + signing

- **Signal**: SLSA v1.1 is the stable release (v1.2 in development).
  Build Level 2 attestations via GitHub Actions OIDC + Sigstore (Fulcio
  for short-lived cert issuance, Rekor for transparency-log recording)
  is the table-stakes floor for serious .NET libraries in 2026; Build
  Level 3 requires reusable, out-of-repo build definitions. NuGet
  trusted publishing and `dotnet/setup-nuget`-style flows are the
  delivery path.
- **Cephalon implication**: Cephalon already records the *principles*
  ("Source Link, symbols, package validation, vulnerability scanning,
  trusted publishing, deterministic builds, and SBOM-aligned metadata"
  in [`long-range-direction.md`](../long-range-direction.md) Horizon 1)
  and uses some of them today (Source Link, symbols, central package
  management, package validation baseline). What is *not* yet in the
  durable docs is the explicit SLSA / Sigstore / Fulcio / Rekor
  vocabulary, even though that is now the language operators and
  external auditors use. That is an additive doc gap, not a code gap.
  A future package-publishing slice could name the target SLSA level
  explicitly and capture the attestation contract at the same level
  of detail Cephalon already uses for deployment-mode claims.
- **Owner home**: [`long-range-direction.md`](../long-range-direction.md)
  (Horizon 1 supply-chain forecast row),
  [`engineering-standards.md`](../engineering-standards.md) (framework
  quality gates), [`package-publishing.md`](../package-publishing.md)
  (release pipeline, trusted publishing, attestation contract).

### Rate limiting, resilience, policy-as-code

- **Signal**: `Polly v8` plus the built-in `System.Threading.RateLimiter`
  remains the right .NET-side resilience frame in 2026. Cedar and OpenFGA
  solve fine-grained authorization decisions, OPA-WASM solves policy
  evaluation at the edge; these are orthogonal to resilience orchestration.
- **Cephalon implication**: Cephalon's separation of concerns is already
  correct. The `Engine:Resilience` baseline plus `IBehaviorResilienceRuntimeCatalog`
  consume the right primitives, and ASP.NET Core endpoint rate limiting
  composes deterministically with the behavior-dispatch limiter as recorded
  in project memory. No engine direction change is requested. Cedar /
  OpenFGA / OPA-WASM remain in scope only as additive companion-pack
  candidates if a consumer needs policy-as-code authorization, not as
  engine-core seams.
- **Owner home**: existing resilience entries in project memory,
  [`engine-roadmap.md`](../engine-roadmap.md) phase 11.

## Docs-graph audit

The pass cross-checked the planning anchors and identified where the surveyed
signal is *not* yet named in the durable docs. The two real audit findings
are:

1. **MCP / Agent2Agent / `AGENTS.md` naming gap.** The substrate is shipped
   (`/engine/*`, runtime-contract-index, conformance matrix, repo-root
   `AGENTS.md`). The names are absent from `long-range-direction.md`,
   `runtime-contract-index.md` "AI-consumer discovery patterns" section,
   and `dotnet-ecosystem-reference.md`. A future slice could name them
   explicitly without changing any runtime contract.
2. **SLSA / Sigstore vocabulary gap.** The principles are recorded; the
   industry-standard names (SLSA Build Levels 1/2/3, Sigstore, Fulcio,
   Rekor, attestation, provenance) are absent from
   `long-range-direction.md`, `engineering-standards.md`, and
   `package-publishing.md`. A future package-publishing slice could state
   the target SLSA level and the attestation contract explicitly.

Both findings are non-breaking and additive. They do not warrant a code
change in this pass; they are tracked here so a later slice can pick them
up against the existing owner-home docs.

The pass also noticed that the Preview 3 *feature inventory* is not yet
captured in [`dotnet11-readiness.md`](../dotnet11-readiness.md) beyond the
date stamp. The list above (zstd, OpenAPI 3.2 / `Microsoft.OpenApi 3.3.1`,
EF Core helpers, Kestrel HTTP/3 first-request latency, Blazor `Virtualize`
defaults, analyzer fixes) belongs in the next refresh of that doc, in line
with the standing rule that the readiness anchor stays useful only when
the dated truth is recent.

## What this pass intentionally does not do

- it does not move Cephalon's shipping floor off `net10.0`
- it does not introduce a new ENG-* card; the [`engine-backlog.md`](../engine-backlog.md)
  / [`engine-roadmap.md`](../engine-roadmap.md) maintainers should decide
  whether the two doc-graph gaps above warrant cards
- it does not rewrite [`long-range-direction.md`](../long-range-direction.md);
  the horizons are intact and validated by this pass
- it does not add SLSA, Sigstore, MCP, A2A, or `AGENTS.md` naming into the
  durable planning anchors — that is owner-home doc work, not learning-pass
  work
- it does not flip any deployment-mode claim; the trim/AOT/single-file
  story stays gated by the manifest harness recorded in
  [`deployment-mode-support.md`](../deployment-mode-support.md)
- it does not push to `origin`; the `.session-handoff.md` push gate is
  unchanged

## Maintenance posture

This document is a dated learning-pass record. Future passes should:

- add a new dated section instead of rewriting this one in place; the
  value is the trail
- keep findings pointing at owner-home docs rather than restating their
  content
- close findings here only when the owner-home doc has been updated, and
  link the resolving slice (`ENG-*` id, commit, or shipped-doc anchor)

## Sources

- [.NET 11 Preview 3 announcement (devblogs.microsoft.com)](https://devblogs.microsoft.com/dotnet/dotnet-11-preview-3/)
- [.NET runtime in .NET 11 Preview 3 — release notes (`dotnet/core`)](https://github.com/dotnet/core/blob/main/release-notes/11.0/preview/preview3/runtime.md)
- [`.NET 11` preview folder root (`dotnet/core`)](https://github.com/dotnet/core/tree/main/release-notes/11.0/preview)
- [What's new in .NET 11 (Microsoft Learn)](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/overview)
- [What's new in ASP.NET Core 11 (Microsoft Learn)](https://learn.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-11)
- [InfoQ — .NET 11 Preview 3 coverage](https://www.infoq.com/news/2026/04/dotnet-11-preview-3/)
- [InfoQ — Cell-Based Architecture pattern catalog](https://www.infoq.com/cell-based-architecture/)
- [AWS Well-Architected — What is a cell-based architecture?](https://docs.aws.amazon.com/wellarchitected/latest/reducing-scope-of-impact-with-cell-based-architecture/what-is-a-cell-based-architecture/)
- [Sam Newman on monolith to microservices (InfoQ podcast)](https://www.infoq.com/podcasts/monolith-microservices/)
- [Anthropic — Model Context Protocol overview](https://modelcontextprotocol.io/)
- [Andrew Lock — Provenance attestations for NuGet packages in GitHub Actions](https://andrewlock.net/creating-provenance-attestations-for-nuget-packages-in-github-actions/)
- [SLSA framework site](https://slsa.dev/)
- [Sigstore documentation](https://docs.sigstore.dev/)
- [Polly v8 documentation](https://www.pollydocs.org/)
- [Temporal — durable execution overview](https://temporal.io/blog/durable-execution-in-distributed-systems-increasing-observability)
- [Dapr Workflows — Diagrid coverage](https://www.diagrid.io/blog/code-like-a-monolith-scale-like-a-microservice-durable-workflows-for-cloud-native-apps)
