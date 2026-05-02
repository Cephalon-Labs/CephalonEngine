# Cephalon supply-chain uplift plan

This document is the consolidated multi-sprint plan for raising Cephalon's package, build, and release supply-chain posture toward the contract levels named by the May 2026 Learning Knowledge Pack delta in [`project-memory.md`](project-memory.md): Microsoft library author guidance, NuGet trusted publishing, SLSA v1.1 L3, Sigstore (Cosign + Fulcio + Rekor), CycloneDX SBOM, public-API contract lock-in via `Microsoft.CodeAnalysis.PublicApiAnalyzers`, and a curated `Cephalon.Analyzers` meta-package.

It is *not* a runtime-contract change. None of the slices below alter the public engine surface, the runtime catalog, or behavior dispatch. They harden the way Cephalon ships, signs, and validates packages so consumers can depend on it across `.NET 10` LTS, the `.NET 11` readiness lane, and the `.NET 12+` migration lanes that follow.

Cross-references: [`engineering-standards.md`](engineering-standards.md), [`package-publishing.md`](package-publishing.md), [`external-package-lifecycle.md`](external-package-lifecycle.md), [`compatibility.md`](compatibility.md), [`engine-backlog.md`](engine-backlog.md), [`project-memory.md`](project-memory.md), [`dotnet11-readiness.md`](dotnet11-readiness.md), [`deployment-mode-support.md`](deployment-mode-support.md).

## Why a consolidated plan

The supply-chain recommendations in the Learning Knowledge Pack are intentionally separable on paper: lock files do not depend on Sigstore signing, SLSA provenance does not depend on `PublicApiAnalyzers`, and the analyzer meta-package does not depend on SBOM emission. In practice, however, they share three constraints that make a single coherent plan more honest than five overlapping ones:

- **Build determinism is a prerequisite.** SLSA L3 builds require hermetic, reproducible inputs; without committed `packages.lock.json` files and `RestoreLockedMode=true` in CI, every release validation pass implicitly mutates the dependency graph
- **Public-API discipline is a prerequisite for binary-stable releases.** `Microsoft.DotNet.PackageValidation` baselines need a stable reference; without `PublicApiAnalyzers` (or a comparable diff gate) reviewers cannot tell whether a public-surface change is intentional
- **Signed releases need a signed authoring authority.** Sigstore keyless signing ties release provenance to the GitHub OIDC identity that produced it; that identity in turn depends on NuGet trusted publishing being configured before the first signed release lands

The plan below sequences the slices so each one removes a prerequisite for the next without forcing the engine into a long branch-rebase cycle.

## Sprint sequence

The slices sequence across **Sprint 116 through Sprint 120**, one slice per sprint, consistent with the existing one-card-per-sprint cadence visible in [`engine-backlog.md`](engine-backlog.md). Identifiers are reserved here so concurrent agents and contributors pick them up in order.

### `ENG-321` — NuGet lock files plus `RestoreLockedMode` in CI baseline (Sprint 116)

Quality dimensions advanced: *Compatibility*, *Auditability*, *Security*.

Why:

- the engine restores against `nuget.org` only (see [`nuget.config`](../nuget.config)), but the dependency graph is not reproducible across builds because `packages.lock.json` files are not committed and `RestoreLockedMode` is not enforced in CI
- without a committed lock graph, every release validation pass implicitly accepts the latest in-range dependency versions; that is incompatible with SLSA L3, with EU CRA conformity evidence, and with deterministic reference-doc generation

Delivered (target):

- enable `<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>` for `Cephalon.*` projects in `Directory.Build.props` so `dotnet restore` writes a `packages.lock.json` per project
- run a one-time restore across the workspace and commit the generated lock files
- update `nuget.config` with explicit package source mapping so each restored id can resolve only from its expected feed (the engine currently has one feed, `nuget.org`, but adding the mapping makes the contract explicit and pre-empts later supply-chain feed splits)
- update `scripts/validate-release.ps1` to invoke `dotnet restore --locked-mode` so CI fails fast on unintended dependency drift while leaving local developer restores unconstrained
- update [`engineering-standards.md`](engineering-standards.md) packaging section to declare lock-file commitment as a baseline
- update [`package-publishing.md`](package-publishing.md) and [`external-package-lifecycle.md`](external-package-lifecycle.md) to reflect the new restore contract

Follow-up later:

- distributed package source mapping when the engine adds an internal feed (out of scope until that feed exists)
- automated lock-file refresh PRs from a Renovate / dependabot equivalent (out of scope; this slice only enables determinism)

### `ENG-322` — Public-API contract lock-in proof on `Cephalon.Abstractions` (Sprint 117)

Quality dimensions advanced: *Compatibility*, *Maintainability*, *Auditability*.

Why:

- `Cephalon.Abstractions` is the host-agnostic contract layer (`IModule`, `ModuleDescriptor`, `IBehaviorContext`, runtime catalog interfaces); accidental binary-breaking changes there propagate across every Cephalon-dependent project
- the engine has analyzer baseline (`AnalysisLevel=latest-recommended`, `TreatWarningsAsErrors=true`) but does not yet track public API as a reviewable artefact

Delivered (target):

- add `Microsoft.CodeAnalysis.PublicApiAnalyzers` to `Cephalon.Abstractions` as a `PrivateAssets=all` analyzer reference
- generate `PublicAPI.Shipped.txt` from the current public surface and commit it; commit an empty `PublicAPI.Unshipped.txt`
- ship a single proof package; do not roll the change out across all 84 packable projects in the same slice — `Cephalon.Abstractions` is the right scope because contract churn there is most expensive
- update [`engineering-standards.md`](engineering-standards.md) library/API design section to declare the analyzer as the public-surface diff gate; declare the rollout to additional packages as follow-up sprints
- update [`compatibility.md`](compatibility.md) to declare `PublicAPI.Shipped.txt` as part of the public contract artefact set

Follow-up later:

- roll the analyzer out to `Cephalon.Engine`, the host adapters, and the transport / behavior / data / event-sourcing / observability / multi-tenancy / agentics / retrieval / edge package families across subsequent sprints
- enable `<EnablePackageValidation>true</EnablePackageValidation>` with a `<PackageValidationBaselineVersion>` once a stable GA exists; until then, `PublicApiAnalyzers` is the diff gate and `PackageValidation` runs in cross-target mode only

### `ENG-323` — `Cephalon.Diagnostics` OpenTelemetry semantic-convention adapter package skeleton (Sprint 118)

Quality dimensions advanced: *Auditability*, *Reliability*, *Maintainability*.

Why:

- engine telemetry already uses `System.Diagnostics.ActivitySource`, `System.Diagnostics.Metrics.Meter`, and `Microsoft.Extensions.Logging.ILogger`, but each emitting site re-decides attribute names; without a centralised semantic-convention adapter the engine drifts away from OpenTelemetry semconv as semconv stabilises through 2026
- the engine SRE posture in [`sre-posture.md`](sre-posture.md) names attribute cardinality and OTel semconv discipline as part of the contract; a single package centralises that discipline so consumers do not re-invent attribute names

Delivered (target):

- new `src/Cephalon.Diagnostics/` package with `IActivitySourceFactory`, `IMeterFactory`-style helpers that emit semconv-correct attributes for HTTP, DB, messaging, and runtime spans / metrics
- preconfigured `LoggerMessage`-source-generated logging factories aligned with the per-package diagnostic-id range discipline
- redaction filter at the engine boundary so secrets / PII / authentication tokens never reach exporters; consumers register additional filters additively
- ship `M0` taxonomy-only at first; promote to `M1` when the engine itself emits at least one span / metric through this package; promote to `M2` when at least one host adapter (`Cephalon.AspNetCore`) routes its telemetry through this package
- update [`engineering-standards.md`](engineering-standards.md) and [`engine-surface-maturity-audit.md`](engine-surface-maturity-audit.md) to reflect the new package
- emit reference-doc coverage through `Cephalon.ReferenceDocs`

Follow-up later:

- per-companion-pack OTel adapter rollout (CDC, eventing, agentics, retrieval, multi-tenancy governance) once the base `Cephalon.Diagnostics` shape is proven
- vendor-specific exporter packs remain out of scope; this package ships exporter-agnostic surface only

### `ENG-324` — Release pipeline supply-chain hardening (SLSA L3 + Sigstore + CycloneDX SBOM + NuGet trusted publishing) (Sprint 119)

Quality dimensions advanced: *Security*, *Compliance*, *Auditability*.

Why:

- the EU Cyber Resilience Act reporting obligation kicks in `September 11, 2026`; main provisions are binding `December 11, 2027`; CRA conformity evidence requires SLSA-style provenance, SBOMs, and a deterministic build pipeline before that date arrives
- NuGet API keys are long-lived; trusted publishing through GitHub OIDC issues short-lived (~1 hour) tokens and ties package provenance to the GitHub identity that signed the release

Delivered (target):

- author a reusable GitHub Actions workflow for Cephalon releases producing: SLSA v1.1 L3 build provenance, Sigstore Cosign keyless signing tied to OIDC, Rekor transparency log entry, CycloneDX SBOM per package, NuGet trusted publishing instead of long-lived API keys
- depend on `ENG-321` (committed lock files + `RestoreLockedMode=true`) so the build is hermetic and reproducible
- depend on `ENG-322` (public-API diff gate on `Cephalon.Abstractions`) so a binary-breaking change cannot accidentally ship inside a signed release
- update [`package-publishing.md`](package-publishing.md), [`engineering-standards.md`](engineering-standards.md) security/supply-chain section, and the release validation workflow to declare the new release contract
- emit machine-readable conformity evidence (vulnerability-handling logs, SBOM artefacts, provenance attestations) in the release artefact bundle so CRA reporting can ingest it without log archaeology

Follow-up later:

- post-quantum hybrid signing (NIST ML-KEM / ML-DSA) once the .NET cryptography stack ships hybrid signing primitives in the `.NET 12` LTS lane
- container base image signing propagation (signed `.NET 11` base images already; consumers should be able to verify the signature chain end-to-end)

### `ENG-325` — `Cephalon.Analyzers` curated meta-package (Sprint 120)

Quality dimensions advanced: *Maintainability*, *Security*, *Compatibility*.

Why:

- the analyzer baseline (`Microsoft.CodeAnalysis.NetAnalyzers`, `Roslynator`, `Meziantou.Analyzer`, `BannedApiAnalyzers`, `PublicApiAnalyzers`) plus the existing `Cephalon.Behaviors.SourceGen` analyzers is currently composed package-by-package; consumers that adopt Cephalon do not inherit the engine's quality posture without hand-tuning every `.editorconfig`
- a single meta-package gives consumers a `<PackageReference Include="Cephalon.Analyzers" />` that pulls in curated severities and a `BannedSymbols.txt` aligned with the engine's discipline (banning `DateTime.Now`, `Thread.Sleep`, `Task.Wait`, `Task.Run` in module bodies, etc.)

Delivered (target):

- new `src/Cephalon.Analyzers/` meta-package referencing the analyzer baseline as `PrivateAssets=all` so consumers do not re-export the analyzers transitively
- ship a curated `.editorconfig` snippet that consumers can include via `<EditorConfigFile>` or copy into their own `.editorconfig`
- ship a `BannedSymbols.txt` that bans the time / threading / synchronisation primitives the engine has already discouraged in module bodies
- ship `M0` taxonomy-only at first; promote to `M1` when the engine itself routes one project through `Cephalon.Analyzers` instead of duplicate analyzer references
- update [`engineering-standards.md`](engineering-standards.md) code-quality gates section to declare the meta-package as the canonical consumer adoption path

Follow-up later:

- per-Cephalon-package analyzer-rule severity tuning (e.g. eventing-specific banned symbols) remains future per-package work; this slice ships the curated baseline only

## Cross-slice dependencies

```
ENG-321 (lock files)  -+-> ENG-324 (release hardening)
                       |
ENG-322 (public-API)  -+
                       |
ENG-323 (diagnostics) ---> (independent; can ship in parallel)
                       |
ENG-325 (analyzers)   -+-> consumer adoption surface
```

`ENG-321` and `ENG-322` are the only hard prerequisites for `ENG-324`. `ENG-323` and `ENG-325` are independent and can ship in any order, but landing them after `ENG-324` makes the first signed release carry the diagnostics adapter and analyzer meta-package as part of the conformity evidence bundle.

## Out-of-scope items

The following are intentionally not part of this plan and remain owned elsewhere:

- runtime-contract changes (none of the slices alter `/engine/*` routes, `snapshot.*` keys, or `I*Catalog` interfaces)
- per-companion-pack execution-ownership work (`M2`-or-higher promotion of existing surfaces remains owned by the matching `ENG-*` cards in [`engine-backlog.md`](engine-backlog.md))
- multi-region or distributed-execution `M3` promotion (companion packs that cross into `M3` declare their own SLI/SLO additively in their component doc; not absorbed here)
- post-quantum signing primitives (deferred to the `.NET 12` LTS readiness lane)
- consumer-application supply-chain posture (consumer teams own their own SLSA / SBOM / signing posture against their own deployment artefacts; this plan only covers Cephalon's own packages)

## Refresh cadence

Refresh this plan in place when:

- a slice lands; collapse the matching section into a back-pointer to the shipped `ENG-*` card and remove the "(target)" marker from the *Delivered* paragraph
- the EU regulatory timeline shifts (CRA enforcement date, AI Act high-risk wave dates, GPAI compliance date)
- SLSA, Sigstore, CycloneDX, or NuGet trusted publishing publishes a new revision that materially affects the contract
- a new prerequisite is discovered between slices (e.g. the diagnostics package needs a runtime-contract surface that has not shipped yet)

Do not append a dated change log inside this document. Long-range plans rarely benefit from change history living inside the plan page; the durable history belongs in commits, planning cards, and architecture-review snapshots.
