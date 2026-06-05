# Test coverage roadmap

This document records the deliberate testing shape of the Cephalon repository as of `May 5, 2026` and lists the next slices that would advance test coverage truthfully without forcing a 1:1 test-project-per-source-project shape that does not match how a framework gets tested.

Cross-references: [`engineering-standards.md`](engineering-standards.md), [`benchmarking.md`](benchmarking.md), [`engine-surface-maturity-audit.md`](engine-surface-maturity-audit.md), [`conformance-matrix.md`](conformance-matrix.md), [`architecture-review-2026-05-followups.md`](architecture-review-2026-05-followups.md), [`planning-governance.md`](planning-governance.md).

## Why this document exists

Cephalon ships ~102 first-class `Cephalon.*` source projects. A naive count would say only 6 of them have a matching `tests/Cephalon.*` test project, which sounds like a 6% coverage rate. That count is misleading. The repository does not test packages by mirroring them 1:1 — it tests them through layered framework-level test projects that exercise composition, host integration, runtime behavior, scripts, and tooling against real surfaces.

This document records what the layered shape covers, where the genuine gaps are, and what kind of work would advance coverage in a way that respects the layered model rather than fighting it.

## Current test layering (May 2026)

The repository ships six dedicated test lanes under `tests/`. Together they hold roughly 204 test files. Each layer has a deliberate role:

| Test project | Role | File count (approx) | What it exercises |
| --- | --- | --- | --- |
| `tests/Cephalon.Tests` | Legacy top-level harness workspace folder | no active `.csproj`; retained for historical artifacts and local run outputs | historical residue from pre-split test layout; active test execution lives in split projects below |
| `tests/Cephalon.Tests.Composition` | Composition-time tests | ~81 | engine composition, module discovery and ordering, capability evaluation, manifest generation, behavior topology composition, agentics/eventing/retrieval composition, multi-tenancy composition, package loading, technology selection |
| `tests/Cephalon.Tests.Hosting` | Host-runtime integration tests | ~72 | ASP.NET Core hosting, runtime introspection routes, REST/JSON-RPC/gRPC/WebSockets/SSE wiring, behavior REST projection, cell traffic automation hosting, cloud-platform observability hosting, audit hosting, identity hosting, dependency-health probe hosting |
| `tests/Cephalon.Tests.Scripts` | PowerShell harness Pester suites | (Pester scripts under `scripts/`) | release validation harness, deployment-mode claim harness, planning-sync, reference-doc publishing flow |
| `tests/Cephalon.Tests.Support` | Shared test infrastructure | ~26 | test fixtures, builders, in-memory stores, deterministic clocks, fake host primitives that other test projects consume |
| `tests/Cephalon.Tests.Tooling` | CLI and scaffolding tests | ~25 | `Cephalon.Cli` doctor checks, scaffolding generation, package staging, blueprint emission |

Plus benchmarks under `benchmarks/Cephalon.Benchmarks` participate in release validation through the guardrail catalog (see [`benchmarking.md`](benchmarking.md)).

The intended posture: layered tests cover composition + host + tooling + scripts + benchmarks. A new dedicated test project under `tests/` is added only when one source pack ships enough independent execution surface to justify a separate harness, not because every src/ folder should have a sibling tests/ folder.

## Gap principle

A genuine test coverage gap exists when one of the following is true for a `Cephalon.*` source pack:

1. **No layered test exercises any of its surfaces.** No composition test loads it; no hosting test routes through it; no tooling test calls into it; no benchmark touches it. This is the highest-priority gap.
2. **Layered tests exercise only the composition seam, not the runtime path.** The pack composes correctly into the engine but its actual managed-execution behavior is uncovered. This is the next-priority gap.
3. **One or more of the pack's `M2`-or-higher claims have no test that proves the runtime contract.** A pack labeled `M2 cephalon-managed` should have at least one test that exercises its managed execution path beyond happy composition. This is the maturity-floor gap.
4. **A regression-prone hot path is not covered by a guardrail benchmark.** This is a benchmark-coverage gap, distinct from unit/integration test coverage.

Adoption-quality test gaps for `M3` and `M4` packs are tracked through `Cephalon.Tests.Hosting`, `Cephalon.Tests.Composition`, and benchmark coverage rather than separate per-pack test projects.

## Gap inventory by family

The list below records, per family, the current layered coverage and what kind of follow-through would advance it. Detailed test-file inventories live in the test projects themselves; this page only names the directions.

### Core runtime — covered

`Cephalon.Abstractions`, `Cephalon.Engine` are exercised heavily by `Cephalon.Tests.Composition` (composition, manifest, runtime introspection, technology selection, package loading, trust policy, failure policy). No new dedicated test project needed; deepen scenarios within the composition project as new contracts ship.

### Host adapters — covered

`Cephalon.AspNetCore` and `Cephalon.Worker` are exercised by `Cephalon.Tests.Hosting` (ASP.NET Core), `Cephalon.Tests.Composition` (worker host composition), and `Cephalon.Tests.Hosting` (runtime introspection routes). No new dedicated test project needed.

### Transport adapters — partial

`Cephalon.AspNetCore.Grpc`, `Cephalon.AspNetCore.JsonRpc`, `Cephalon.AspNetCore.GraphQL` are at `M2`. `Cephalon.Tests.Hosting` exercises the route mapping and basic happy paths. `Cephalon.AspNetCore.JsonRpc` now has direct error-mode coverage shipped via `ENG-405` (`tests/Cephalon.Tests.Hosting/JsonRpcErrorResponseHostingTests.cs`, covering method-not-found, parse-error, invalid-request, and internal-error responses). Open work:

- Add streaming behavior coverage for gRPC bi-directional and server-streaming patterns to `Cephalon.Tests.Hosting`.
- Add a small set of GraphQL transport mapping tests to `Cephalon.Tests.Hosting` once the GraphQL runtime claim widens beyond route mapping.

### Behaviors — covered

`Cephalon.Behaviors` and `Cephalon.Behaviors.Http` are heavily exercised by both composition and hosting test projects (behavior topology, REST projection, resilience policies, durable execution, saga choreography, feature-flag-aware behavior). `Cephalon.Behaviors.Messaging`, `Cephalon.Behaviors.Patterns`, `Cephalon.Behaviors.SourceGen` are descriptor/utility surfaces with composition-time coverage. Adequate.

### Eventing — covered

`Cephalon.Eventing` core and `Cephalon.Eventing.Wolverine` are exercised by composition + hosting (publication runtime, subscription readiness, dispatch state, terminal failures, in-process retry/idempotency). `Cephalon.Eventing.Behaviors` bridge is descriptor-level and composition-tested. Adequate at current maturity.

### Agentics and retrieval — covered

`Cephalon.Agentics` and `Cephalon.Retrieval` have both composition and hosting coverage for descriptors, dispatch, run state, operator routes, and reindex/query. Adequate at `M3`.

### Data and CDC — partial

`Cephalon.Data` core is exercised through composition + hosting (CDC capture state, runtime catalog, data product descriptors, outbox/inbox descriptors, database topology, role/migration descriptors). Open work:

- `Cephalon.Data.SqlServer` provider-native CDC pump path (`M2 provider-managed`) deserves a dedicated provider integration scenario that runs against a containerized SQL Server, even if it lives inside `Cephalon.Tests.Hosting` rather than its own test project. Same shape for `Cephalon.Data.Postgres` (logical replication) and `Cephalon.Data.MongoDB` (change stream).
- `Cephalon.Data.{Redis,Neo4j,Cassandra,ClickHouse,Elasticsearch,OpenSearch,Qdrant,Nats,Debezium}` are `M1` catalog-only; composition coverage is sufficient at current maturity. Promote test scope only when one of these promotes to `M2`.

### Event sourcing — narrow

`Cephalon.EventSourcing` core has composition-level coverage. The 10 provider packs are `M1` catalog-only. No additional test work is appropriate until a provider pack ships managed-execution proof; at that point the test slice should land in the same `ENG-*` card that ships the runtime.

### Multi-tenancy — covered

`Cephalon.MultiTenancy` core, `Cephalon.MultiTenancy.Governance`, and `Cephalon.MultiTenancy.Governance.AspNetCore` are heavily exercised through composition + hosting (resolver, membership, invitation, domain ownership, governance actions, durable stores, callback translation, signed-webhook verification, replay protection, idempotency). The six invitation-delivery sender packs and their AspNetCore companion packs share the family-floor `M2` and are exercised through hosting integration. Adequate at the family floor.

### Audit and identity — covered

`Cephalon.Audit` and `Cephalon.Audit.EntityFramework` have hosting coverage (audit history endpoints, export) plus direct fallback-chain coverage for `IAuditActorAccessor` and `DefaultAuditRecorder` shipped via `ENG-404` (`tests/Cephalon.Tests.Composition/Composition/AuditActorAndRecorderFallbackTests.cs`, exercising the no-principal, principal-without-claims, and tenant-scoped-principal cases). `Cephalon.Identity` and `Cephalon.Identity.AspNetCore` have hosting coverage for the metadata-driven evaluator and policy mapping plus direct decision-matrix coverage shipped via `ENG-403` (`tests/Cephalon.Tests.Composition/Composition/MetadataDrivenAuthorizationDecisionMatrixTests.cs`, exercising RBAC, ABAC, and tenant-boundary edge cases). Adequate at the family floor.

### Edge — covered

`Cephalon.Edge`, `Cephalon.Edge.KubernetesGateway`, and `Cephalon.Edge.Traefik` are exercised through composition + hosting (cell boundaries, cell routes, health isolation, traffic automation, provider-native materialization with apply-and-reconcile loops). Adequate at `M3`.

### Observability — partial

`Cephalon.Observability` core and `Cephalon.Observability.OpenTelemetry` are exercised through hosting (startup summary, OTLP wiring, ASP.NET Core server tracing). The 14 cloud configuration packs (AWS / Azure Monitor / GCP / Alibaba / Huawei / Oracle Cloud / DigitalOcean / OpenShift / Tanzu / Grafana Cloud / New Relic / Kubernetes / Serilog / OpenTelemetry) have hosting coverage for option binding and host wiring. The 18 dependency-health probe packs are `M0` taxonomy-only; their probe runtime in `Cephalon.Observability.DependencyHealth.Core` is composition-tested. Adequate at current maturity.

### Scaffolding and tooling — covered

`Cephalon.Cli`, `Cephalon.Scaffolding`, `Cephalon.ReferenceDocs` are exercised by `Cephalon.Tests.Tooling` (doctor checks, scaffolding generation, package staging) and `Cephalon.Tests.Scripts` (PowerShell harness). Adequate.

## Recommended next slices

These are the concrete cards a planning seat should consider allocating against this roadmap. Each is `non-breaking` and either lands a new test scenario inside an existing test project (preferred) or, in rare cases, a new dedicated test project.

### High priority — proves maturity floor

| # | Slice | Lands in | Theme | Rationale |
| --- | --- | --- | --- | --- |
| 1 | gRPC streaming and error-mode coverage for `Cephalon.AspNetCore.Grpc` | `Cephalon.Tests.Hosting` | Reliability, Compatibility | M2 transport adapter; current happy-path coverage does not exercise streaming or error frames |

The three high-priority slices originally listed alongside `#1` (JSON-RPC error-response coverage, `MetadataDrivenAuthorizationEvaluator` decision-matrix coverage, `IAuditActorAccessor` ambient-context fallback coverage) shipped before this roadmap was authored. Their resolution is recorded in [`architecture-review-2026-05-followups.md` § Test coverage roadmap](architecture-review-2026-05-followups.md#test-coverage-roadmap) per the maintenance rule below; the family-section prose above already reflects the new coverage.

### Medium priority — provider-native scenarios

| # | Slice | Lands in | Theme | Rationale |
| --- | --- | --- | --- | --- |
| 2 | SQL Server CDC pump containerized scenario | `Cephalon.Tests.Hosting` | Data integrity | M2 provider-managed; provider-native CDC truth deserves at least one end-to-end scenario per relational provider |
| 3 | Postgres logical-replication CDC scenario | `Cephalon.Tests.Hosting` | Data integrity | M2 provider-managed; same shape as #2 |
| 4 | MongoDB change-stream CDC scenario | `Cephalon.Tests.Hosting` | Data integrity | M2 provider-managed; same shape as #2 |

### As-needed — promotion-aligned

When a `Cephalon.*` package promotes from `M1` to `M2`, the slice that ships the runtime should also land at least one execution-path test in `Cephalon.Tests.Hosting` or `Cephalon.Tests.Composition`. New dedicated test projects (e.g., `tests/Cephalon.Tests.<Family>`) are appropriate only when a family ships enough independent execution surface to justify a separate harness.

## Standing rules

- Layered framework-level tests are the default home for new coverage. Add new dedicated test projects only when a family ships enough independent execution surface to justify a separate harness.
- Tests follow the standards baseline in [`engineering-standards.md`](engineering-standards.md) § Testing standards (xUnit, deterministic, isolated, no filesystem-ordering reliance, name tests by behavior + expected outcome).
- New tests are wired into `scripts/validate-release.ps1` and the `release-validation` workflow in the same slice that adds them, so coverage participates in the release gate.
- Hot-path additions trigger benchmark-coverage review per [`benchmarking.md`](benchmarking.md).
- This roadmap is descriptive: each implemented slice should still receive an explicit `ENG-*` card so the planning record stays truthful per [`planning-governance.md`](planning-governance.md).

## Maintenance posture

Update this document when:

- a `Cephalon.*` package promotes maturity (the matching family section's recommended slices may change)
- a new dedicated test project lands under `tests/` (add a row to the layering table)
- the recommended-next-slices list changes — close shipped items by removing them from the high-priority list and recording the resolution in the relevant `architecture-review-YYYY-MM.md` snapshot rather than leaving them in this roadmap
- a new family enters the repository (add a per-family gap section)

This roadmap is a planning artifact, not a source of truth. The audit + matrix + component docs remain authoritative; this page consolidates the testing implications.
