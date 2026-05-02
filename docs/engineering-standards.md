# Cephalon engineering standards

This document is the consolidated standards baseline for code, packaging, documentation, testing, and quality gates that apply to the Cephalon engine and every shipped `Cephalon.*` package, host adapter, companion pack, and tooling surface.

Cross-references: [`project-memory.md`](project-memory.md), [`architecture.md`](architecture.md), [`architecture-patterns-research.md`](architecture-patterns-research.md), [`architecture/design-patterns-reference.md`](architecture/design-patterns-reference.md), [`compatibility.md`](compatibility.md), [`package-publishing.md`](package-publishing.md), [`engine-surface-maturity-audit.md`](engine-surface-maturity-audit.md), [`conformance-matrix.md`](conformance-matrix.md), [`reference-docs.md`](reference-docs.md), [`benchmarking.md`](benchmarking.md), [`dotnet11-readiness.md`](dotnet11-readiness.md), [`long-range-direction.md`](long-range-direction.md).

## How to use this document

This page does not replace any of the more detailed docs above. It is the *one place a contributor or AI agent can read first* to understand what "high quality" means in this repository before they ship code, docs, packaging, or planning changes. Each section ends with the doc that owns the deeper truth so this page stays a navigable index, not a duplicate source.

## First-class engine qualities

Every shipped surface must consider the following qualities deliberately, not as later hardening chores. They are listed once here so future ADRs, planning cards, and reviews can reference a stable set of dimensions.

- **Performance.** Hot paths have benchmarks; benchmark drift is part of release validation; allocations and JIT shape are checked when claims justify it.
- **Security.** Supply-chain trust, secret hygiene, transport security, and least-privilege defaults are part of the contract, not appendix material.
- **Usability (developer experience).** Consumer code shrinks as the engine grows; APIs feel obvious; docs and IntelliSense agree.
- **Reliability.** Failure modes are explicit; retries, timeouts, circuit breakers, bulkheads, and rate limits are first-class settings.
- **Maintainability.** Source, docs, and planning truth move together; engine-internal abstractions stay small and additive.
- **Scalability.** Cell-based, edge-aware, and distributed-by-default placement is supported by the same primitives whether the deployment is single-instance or multi-region.
- **Flexibility.** Architecture, transport, host, and policy choices live in engine composition, not in consumer-app rewrites.
- **Compatibility.** Public packages stay binary-stable across minor versions; breaking changes go through deliberate, versioned obsoletion.
- **Data integrity.** Outbox, durable execution, CDC, event-sourcing, and saga choreography contracts keep replay-safe semantics first.
- **Availability.** Health, readiness, dependency-health probes, and degradation modes are observable through the runtime catalog surface.
- **Auditability.** Behavior, governance, tenancy, agent-tool, and rollout decisions emit truthful runtime metadata so audits do not require log archaeology.
- **Compliance.** Multi-tenancy, governance, sovereignty routing, retention, and data-handling decisions stay declarative and inspectable.

These qualities are listed in the same order in [`project-memory.md`](project-memory.md). When a planning card claims to advance one of them, name the dimension explicitly in the card so reviewers can check the claim.

## Quality framework mapping

Each first-class engine quality maps onto one or more external frameworks. Planning cards that claim to advance a quality should name both the dimension and, when relevant, the external framework requirement they advance, so audits and reviewers have a single matrix to read.

The matrix below is the durable mapping. Refresh it in place when ISO/IEC 25010, OWASP ASVS, NIST SSDF, SLSA, OpenTelemetry semantic conventions, or the EU regulatory framework publishes a new revision; do not append a dated change log here.

| Engine quality | ISO/IEC 25010:2023 anchor | OWASP ASVS 5.0 anchor | NIST SSDF v1.2 anchor | SLSA v1.1 / supply-chain anchor | Other / regulatory anchor |
| --- | --- | --- | --- | --- | --- |
| Performance | *Performance Efficiency* (time behaviour, resource utilisation, capacity) | — | — | — | BenchmarkDotNet guardrails; `Microsoft.DotNet.PackageValidation` for binary-perf-affecting changes |
| Security | *Security* | ASVS V1–V17 (architecture, authn, session, access control, validation, crypto, errors, data protection, comm, malicious code, business logic, files/resources, API/web, config, supply chain, web client, OAuth/OIDC) | PS, PW, PO, RV practice groups | SLSA L3 build target; Sigstore Cosign + Fulcio + Rekor; CycloneDX SBOM | OWASP AISVS for AI-touching surfaces |
| Usability | *Interaction Capability* (renamed from Usability) | — | — | — | `cephalon doctor`, scaffolding, low-code shorthand |
| Reliability | *Reliability* (maturity, availability, fault-tolerance, recoverability) | — | — | — | SRE SLI/SLO posture (see `docs/sre-posture.md`); resilience pipeline |
| Maintainability | *Maintainability* (modularity, reusability, analysability, modifiability, testability) | — | PS.1, PS.2, PW.1 | — | `Microsoft.CodeAnalysis.PublicApiAnalyzers`; deterministic builds; central package management |
| Scalability | *Performance Efficiency:Capacity* + *Flexibility:Scalability* | — | — | — | Cell-based architecture; behavior topology decoupling |
| Flexibility | *Flexibility* (Adaptability, Scalability, Installability, Replaceability — absorbs former Portability) | — | — | — | Host-agnostic abstractions; transport-agnostic behaviors; swappable providers |
| Compatibility | *Compatibility* (co-existence, interoperability) | — | — | — | SemVer 2.0 + .NET binary compatibility rules; `<PackageValidationBaselineVersion>`; `[Obsolete]` with `DiagnosticId` and `UrlFormat` |
| Data integrity | *Reliability:Maturity* + *Security:Integrity* | ASVS V8 (Data protection), V13 (API), V14 (Config) | PW.4 (validation) | — | Outbox / inbox / idempotency contracts; CDC pump correctness; event-sourcing snapshot semantics |
| Availability | *Reliability:Availability* | — | — | — | SRE SLO posture; `/health/live` / `/health/ready` semantics; cell health isolation; deployment-mode survivability claims |
| Auditability | *Maintainability:Analysability* + *Security:Accountability* | ASVS V7 (Errors and logging), V15 (Supply chain) | RV (Respond to Vulnerabilities) practice group | SLSA provenance + Rekor transparency entries | EU CRA Article 13 vulnerability-handling logs; CRA Annex II conformity evidence; runtime introspection (`/engine/*`, `snapshot.*`) |
| Compliance | *Functional Suitability:Functional Appropriateness* (cross-cutting) | ASVS V8 (Data protection), V11 (Business logic), V14 (Config) | PO (Prepare the Organization) practice group | SLSA L3 + Sigstore + SBOM as machine-readable conformity artefacts | EU CRA enforcement (`December 11, 2027`; reporting from `September 11, 2026`); EU AI Act high-risk waves (`August 2, 2026` and `August 2, 2027`); GPAI compliance (`August 2, 2027`); W3C DID v1.1 / EUDI Wallet (regulated EU sectors from `2027`); NIST CSF; GDPR/CCPA; HIPAA where applicable |

Authoritative external sources for the mapping:

- [ISO/IEC 25010:2023 — Systems and software Quality Requirements and Evaluation (SQuaRE)](https://www.iso.org/standard/78176.html)
- [arc42 — ISO/IEC 25010:2023 update summary](https://quality.arc42.org/articles/iso-25010-update-2023)
- [OWASP Application Security Verification Standard 5.0](https://owasp.org/www-project-application-security-verification-standard/)
- [OWASP AI Security Verification Standard (AISVS)](https://owasp.org/www-project-artificial-intelligence-security-verification-standard/)
- [NIST SSDF (SP 800-218 v1.2 draft, Dec 2025)](https://csrc.nist.gov/projects/ssdf)
- [NIST SP 800-218A — SSDF Practices for Generative AI](https://csrc.nist.gov/publications/detail/sp/800-218a/final)
- [SLSA framework v1.1](https://slsa.dev/spec/v1.1/)
- [Sigstore (Cosign + Fulcio + Rekor)](https://www.sigstore.dev/)
- [CycloneDX SBOM specification](https://cyclonedx.org/)
- [OpenTelemetry semantic conventions](https://opentelemetry.io/docs/specs/semconv/)
- [Google SRE — Implementing SLOs](https://sre.google/workbook/implementing-slos/)
- [EU Cyber Resilience Act — official summary](https://digital-strategy.ec.europa.eu/en/policies/cra-summary)
- [EU AI Act — implementation timeline](https://digital-strategy.ec.europa.eu/en/policies/regulatory-framework-ai)

When a planning card lands a feature that closes a row in this matrix (e.g. `Cephalon.Diagnostics` filling the OpenTelemetry semantic-conventions adapter, or a release pipeline filling the SLSA L3 row), the card description should reference the row directly and the matrix should be updated in the same slice rather than after the fact.

## Library and API design

Cephalon ships reusable libraries that other teams take dependencies on. That changes the design constraints from "what works for our app" to "what other teams can rely on for years." The dominant external reference is Microsoft's *Framework Design Guidelines*; Cephalon applies those guidelines consistently across the `Cephalon.*` family.

Authoritative external sources:

- [Framework Design Guidelines (Microsoft Learn)](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/)
- [Open-source library guidance (Microsoft Learn)](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/)
- [.NET API design considerations](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/breaking-changes)
- [Source Link](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/sourcelink)
- [`PackageValidation` and the validator NuGet package](https://learn.microsoft.com/en-us/dotnet/fundamentals/apicompat/package-validation/overview)

Cephalon-specific application:

- public types that are part of the engine contract live under `Cephalon.Abstractions` or one of the explicitly named contract packages; implementation-only types stay `internal` unless the contract actually needs them
- prefer constructor injection over property injection; constructors stay lightweight; expensive work happens in explicit `Initialize`/`Start` methods or in hosted lifetime callbacks
- prefer stable `Add{Group}` registration entry points (e.g., `AddCephalonEngine`, `AddCephalonAspNetCore`, `AddBehaviorEventingBridge`) over implicit discovery; explicit ordering beats ambient registration magic
- prefer extension methods on stable surfaces (`IServiceCollection`, `IBehaviorTopologyBuilder`, `IRestBehaviorEndpointGroupBuilder`, etc.) for module/host-facing API ergonomics
- avoid public mutable state on shared services; expose immutable descriptors and registry contracts instead
- avoid leaking `System.Text.Json`, `Microsoft.Extensions.*`, or other implementation libraries through public contracts when they are not part of the intended contract; if the implementation library is the contract, document that explicitly
- naming follows the [Microsoft naming guidelines](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/naming-guidelines): PascalCase for public members, async methods end in `Async`, interfaces start with `I`, contributors/registries/catalogs use those exact suffixes consistently
- new public surface uses XML doc comments; doc generation is on by default in `Directory.Build.props`

Deeper truth:

- runtime contract anchors → [`architecture.md`](architecture.md)
- design pattern catalogue → [`architecture/design-patterns-reference.md`](architecture/design-patterns-reference.md)
- maturity labels and execution-ownership truth → [`engine-surface-maturity-audit.md`](engine-surface-maturity-audit.md)

## Code quality gates

The repo enforces baseline quality gates through MSBuild defaults so contributors and AI seats cannot accidentally drop them.

`Directory.Build.props` (current shipped baseline):

- `<ImplicitUsings>enable</ImplicitUsings>`
- `<Nullable>enable</Nullable>`
- `<AnalysisLevel>latest-recommended</AnalysisLevel>`
- `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`
- `<GenerateDocumentationFile>true</GenerateDocumentationFile>`
- `<NoWarn>$(NoWarn);CS1591</NoWarn>` (missing XML doc comment is not a hard error, but XML docs are still expected on public contracts)

Additional standards:

- `Directory.Packages.props` enables Central Package Management; do not put `<PackageReference Version="..." />` inside individual project files
- analyzer suppressions live next to the code being suppressed and include a `// reason: ...` comment so the suppression is reviewable
- nullable annotations on public surface are part of the contract; weakening a nullable annotation on a public member is a binary break
- `async` methods accept `CancellationToken` whenever they perform IO, host work, or could plausibly need cancellation later; cancellation tokens default to `default` only when the surface is intentionally non-cancelable

Performance and allocation discipline:

- hot paths use `Memory<T>` / `Span<T>` / `ReadOnlySpan<T>` instead of array allocations where the API allows
- avoid `ToList()` / `ToArray()` materialization in framework code unless the caller asked for materialization
- avoid LINQ allocation traps inside per-request paths
- benchmarks for known hot paths live under `benchmarks/` and are wired into release validation; see [`benchmarking.md`](benchmarking.md)

Diagnostics standards:

- engine-emitted diagnostics use stable diagnostic ids (`Cephalon.*` numbered ids); ids are assigned once and never reused
- `Cephalon.Behaviors`, `Cephalon.Engine`, governance, tenancy, CDC, eventing, and resilience surfaces all share the same numbered-diagnostic discipline

## Compatibility discipline

Cephalon is a framework, not a single application. That changes the compatibility expectations.

- public packages stay binary-stable across minor versions; binary breaks are deliberate, versioned, and documented
- prefer additive changes plus opt-in settings over silent contract churn
- treat package dependencies as part of the public contract; tightening a transitive minimum version can be a breaking change
- use `[Obsolete]` with a clear migration message before removing a public member; obsoletion is the floor for known breaking changes
- run [`Microsoft.DotNet.PackageValidation`](https://learn.microsoft.com/en-us/dotnet/fundamentals/apicompat/package-validation/overview) baselines so accidental binary breaks are caught at packaging time, not at consumer adoption time

The deeper compatibility-and-alignment contract lives in [`compatibility.md`](compatibility.md). Do not duplicate that contract here — read it before changing public surface, package metadata, or framework baselines.

## Packaging standards

Cephalon ships under a coherent, layered `Cephalon.*` package family. Package taxonomy is part of the framework's discoverability and upgrade safety, not a release afterthought.

Shared metadata (already inherited via `Directory.Build.props`):

- `Authors = Cephalon`
- `PackageLicenseExpression = MIT`
- `PackageProjectUrl`, `RepositoryUrl`, `RepositoryType = git`, `PublishRepositoryUrl = true`
- shared `PACKAGE.md` readme by default; package-specific `PACKAGE.md` allowed when the package needs its own
- baseline package version `0.1.0-preview` until the family enters its first stable cut

Required publishing hygiene:

- Source Link is enabled on all packable `Cephalon.*` projects
- symbol packages (`.snupkg`) ship alongside primary `.nupkg`
- `PackageValidationBaselineVersion` is configured for stable packages so accidental binary breaks fail packaging
- vulnerability scans (`dotnet list package --vulnerable --include-transitive`) run as part of release validation
- prefer trusted publishing (NuGet trusted-publishing flow + GitHub OIDC) over long-lived API keys when the release flow is formalized

Required restore determinism:

- `<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>` is set inside the `Cephalon.*` `PropertyGroup` of [`Directory.Build.props`](../Directory.Build.props), so every `dotnet restore` writes a `packages.lock.json` per project
- the generated `packages.lock.json` files are committed to the repository so the dependency graph is part of the public, reviewable contract; they belong in git the same way `Directory.Packages.props` does
- [`nuget.config`](../nuget.config) declares an explicit `packageSourceMapping` so each restored package id resolves only from its expected feed (`nuget.org` today; future feed splits land additively)
- CI runs `dotnet restore --locked-mode` so unintended dependency drift fails the build immediately rather than silently shifting the resolved graph
- local developer restores stay unconstrained; lock files are regenerated when a contributor intentionally updates a `Directory.Packages.props` version, and the resulting diff goes through normal PR review

Splitting policy:

- split a package only when its assembly is independently useful to consumers
- avoid one-megapackage that bundles unrelated functionality; avoid micro-fragmenting a coherent surface across many trivially-small packages
- when a package is split, the split is documented in [`compatibility.md`](compatibility.md) and the family taxonomy stays consistent with the shipped names listed in [`docs/components/README.md`](components/README.md)

Authoritative external sources:

- [Open-source library guidance — packaging](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/publish-nuget-package)
- [NuGet trusted publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing)
- [Source Link configuration](https://github.com/dotnet/sourcelink)

Deeper truth:

- shipped publishing flow → [`package-publishing.md`](package-publishing.md)
- generated app artefacts → [`generated-app-publishing.md`](generated-app-publishing.md)
- external package lifecycle (Microsoft and ecosystem dependencies) → [`external-package-lifecycle.md`](external-package-lifecycle.md)

## Documentation standards

Cephalon uses two documentation layers on purpose. Neither layer replaces the other.

- **hand-authored Markdown** under `README.md` and `docs/` is the primary human-facing product and adoption documentation; it explains ownership, usage, architecture, and adoption. Component pages under `docs/components/*` are the per-package map.
- **XML doc comments** on public contracts are the API explanation layer used by IntelliSense and the `Cephalon.ReferenceDocs` publishing path. They explain supported public API behavior precisely enough for reference generation.

Rules that apply to every change:

- when source changes, the related hand-authored docs change in the same slice; no source patches that leave docs drifting
- when a public/runtime/package surface changes, review source + component docs + compatibility docs + readiness/validation docs + planning docs together as one change surface
- when in doubt about the engine's runtime intent, treat hand-authored docs as the primary contract and `docs/reference/*` as the generated API layer underneath them
- repository-facing docs, commit messages, and planning artefacts stay in English so the durable record reads consistently

Deeper truth:

- documentation hub → [`docs/README.md`](README.md)
- reference-doc publishing flow → [`reference-docs.md`](reference-docs.md)
- planning record discipline → [`planning-governance.md`](planning-governance.md)

## Testing standards

The repo distinguishes deterministic engine tests, host integration tests, sample/runtime tests, and benchmark guardrails on purpose.

- engine-core and abstraction tests are deterministic and isolated; they do not rely on filesystem ordering, configuration leakage from samples, or environment variables
- host integration tests for ASP.NET Core and Worker hosts use real host configurations and content roots; do not silently rely on whatever config files happen to be copied into a test output folder
- sample and showcase tests target real sample content roots explicitly so the harness keeps testing the actual sample, not whatever transient output state existed at run time
- benchmarks live under `benchmarks/` and use `BenchmarkInProcessShortRunConfig` for the smoke path so release validation can run them quickly without losing meaningful signal
- guardrail benchmarks for hot paths participate in `scripts/validate-release.ps1`

Test-style conventions:

- prefer xUnit; assertion style stays consistent inside a test project
- prefer one assertion concept per test; use `Assert.Multiple` or fluent assertions only when the test really expresses one combined concept
- name tests after the *behavior under test* and the *expected outcome*; avoid `Test_Method_Test1`-style names

Authoritative external reference:

- [.NET unit testing best practices](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)

## Security and supply-chain standards

Security is a framework concern, not an application concern. Cephalon's standards therefore stay strict by default.

- secrets, tokens, and signing keys never live in repo-tracked files; secrets are injected through environment, host config, or trusted-publishing flows
- transport security defaults assume TLS; HTTP-only modes are explicit opt-in for development scenarios
- vulnerability scans run on every release validation pass; transitive vulnerabilities are part of the report
- analyzer-only signals are readiness, not support; trim/AOT/single-file claims still need the manifest, project settings, validation coverage, workflow automation, and docs to all agree (see [`dotnet11-readiness.md`](dotnet11-readiness.md))
- when the planned `scripts/validate-deployment-mode-claims.ps1` validation harness ships, its `claim-truthful` verdict is the authoritative gate for promoting trim, Native AOT, or single-file support from `not-claimed` to `claimed`; analyzer-only signals or local publish experiments do not widen the contract by themselves (see [`deployment-mode-support.md`](deployment-mode-support.md))
- governance, tenancy, multi-tenancy, identity, and policy decisions stay declarative and inspectable through the engine's runtime catalogs so audits do not require log archaeology

Authoritative external sources:

- [.NET secure coding guidelines](https://learn.microsoft.com/en-us/dotnet/standard/security/secure-coding-guidelines)
- [.NET application security best practices](https://learn.microsoft.com/en-us/aspnet/core/security/?view=aspnetcore-10.0)
- [GitHub Actions trusted publishing for NuGet](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing)

## Planning and governance standards

The standards above apply to every change. They are kept in sync with planning artefacts, GitHub Project tracking, and the durable record through:

- [`planning-governance.md`](planning-governance.md) — durable planning workflow
- [`engine-roadmap.md`](engine-roadmap.md) and [`engine-backlog.md`](engine-backlog.md) — phased and immutable planning record
- [`engine-surface-maturity-audit.md`](engine-surface-maturity-audit.md) — `M0`–`M4` plus `taxonomy-only` / `application-managed` / `cephalon-managed` / `provider-managed` truth

When a change advances one of the engine quality dimensions listed at the top of this page, name the dimension in the matching planning card and in the commit message so reviewers can check the claim against this standard.

## Maintenance posture

This document is meant to be the durable index of Cephalon engineering standards. Update it when:

- the shipping framework or readiness lane changes (refresh the relevant cross-link, especially [`dotnet11-readiness.md`](dotnet11-readiness.md))
- a new quality gate is introduced (analyzer pack, validation lane, vulnerability scan tier, package-validation baseline tier)
- a new Microsoft library design guideline materially changes recommended practice
- the package family taxonomy or splitting policy changes
- the testing or benchmarking discipline materially evolves

Prefer rewriting the affected section in place over appending. Long-range standards rarely benefit from change history living inside the standards page itself; meaningful history belongs in commits, planning cards, and architecture review snapshots.
