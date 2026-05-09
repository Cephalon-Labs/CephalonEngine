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

*Shipped.* `<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>` is set in [`Directory.Build.props`](../Directory.Build.props), [`nuget.config`](../nuget.config) declares an explicit `packageSourceMapping` pinning every restored id to `nuget.org`, the per-project `packages.lock.json` files are committed, and [`scripts/validate-release.ps1`](../scripts/validate-release.ps1) runs `dotnet restore --locked-mode` as the first non-skippable step before build / test / readiness / deployment-mode / benchmarks / reference-docs / packages. See `ENG-321` in [`engine-backlog.md`](engine-backlog.md). Follow-up: distributed package source mapping when the engine adds an internal feed; automated lock-file refresh PRs from a Renovate / dependabot equivalent.

### `ENG-322` — Public-API contract lock-in proof on `Cephalon.Abstractions` (Sprint 117)

*Shipped.* `Microsoft.CodeAnalysis.PublicApiAnalyzers` is wired into [`src/Cephalon.Abstractions/Cephalon.Abstractions.csproj`](../src/Cephalon.Abstractions/Cephalon.Abstractions.csproj) as a `PrivateAssets=all` analyzer reference; [`src/Cephalon.Abstractions/PublicAPI.Shipped.txt`](../src/Cephalon.Abstractions/PublicAPI.Shipped.txt) carries the 8,207-entry baseline (every public type / member of the host-agnostic contract layer); [`src/Cephalon.Abstractions/PublicAPI.Unshipped.txt`](../src/Cephalon.Abstractions/PublicAPI.Unshipped.txt) is the empty next-release diff sheet. RS0026 / RS0027 are suppressed inside `Cephalon.Abstractions` only (existing optional-overload patterns; cleanup is follow-up). [`docs/compatibility.md`](compatibility.md) carries the new *Public-API contract artefacts* section. See `ENG-322` in [`engine-backlog.md`](engine-backlog.md). Follow-up: roll out to `Cephalon.Engine`, host adapters, transport / behavior / data / event-sourcing / observability / multi-tenancy / agentics / retrieval / edge package families; enable `<EnablePackageValidation>true</EnablePackageValidation>` with a `<PackageValidationBaselineVersion>` once a stable GA exists; clean up the RS0026 / RS0027 patterns in `Cephalon.Abstractions` so the suppressions can be removed.

### `ENG-323` — `Cephalon.Diagnostics` OpenTelemetry semantic-convention adapter package skeleton (Sprint 118)

*Shipped at `M0` taxonomy-only.* New [`src/Cephalon.Diagnostics/`](../src/Cephalon.Diagnostics/) package ships three stable static classes — `CephalonActivitySources` (engine / ASP.NET Core / worker), `CephalonMeters` (engine / ASP.NET Core / worker), and `CephalonDiagnosticsAttributeKeys` (`cephalon.module.id`, `cephalon.behavior.id`, `cephalon.cell.id`, `cephalon.app.blueprint`, `cephalon.tenant.id`) — that publish the names engine and host-adapter emission sites will use. `Microsoft.CodeAnalysis.PublicApiAnalyzers` is wired in from day one with `PublicAPI.Shipped.txt` (empty) and `PublicAPI.Unshipped.txt` (the new types). Where OpenTelemetry semantic conventions exist for a concept, engine emission uses the OTel attribute name directly; the `cephalon.*` keys are scoped to engine concepts that have no semconv equivalent. See [`docs/components/diagnostics.md`](components/diagnostics.md) and `ENG-323` in [`engine-backlog.md`](engine-backlog.md). Promote to `M1` when the engine itself emits at least one span / metric through `CephalonActivitySources.Engine` or `CephalonMeters.Engine`; promote to `M2` when at least one host adapter routes its telemetry through this package's name set. Follow-up: redaction filter at the engine boundary, `LoggerMessage` factories, per-companion-pack OTel adapter rollout (CDC, eventing, agentics, retrieval, multi-tenancy governance).

### `ENG-324` — Release pipeline supply-chain hardening (SLSA L3 + Sigstore + CycloneDX SBOM + NuGet trusted publishing) (Sprint 119)

*Shipped (workflow-side).* New tag-triggered [`.github/workflows/publish-release.yml`](../.github/workflows/publish-release.yml) wraps the validated package set with: fail-closed external-policy preflight via [`scripts/validate-supply-chain-external-policy-preflight.ps1`](../scripts/validate-supply-chain-external-policy-preflight.ps1), SLSA v1.1 build provenance via `actions/attest-build-provenance`, Sigstore Cosign keyless signature plus Rekor transparency log entry per `.nupkg`, CycloneDX 1.6 SBOM per `Cephalon.*` project, NuGet trusted-publishing login (GitHub OIDC federation) followed by `dotnet nuget push` of `.nupkg` and `.snupkg`. The workflow runs on `v*.*.*` tag push (and `workflow_dispatch` for dry-runs); the per-PR `release-validation.yml` gate is unchanged. [`docs/package-publishing.md`](package-publishing.md) carries the new *Signed release pipeline (tag-triggered)* section; [`docs/engineering-standards.md`](engineering-standards.md) security/supply-chain section carries the new *Signed release pipeline* subsection. See `ENG-324` in [`engine-backlog.md`](engine-backlog.md). The later `ENG-485` follow-through adds [`scripts/supply-chain-release-support.json`](../scripts/supply-chain-release-support.json) so the engine-completion scorecard emits `SupplyChainEvidence` from the workflow and keeps the nuget.org-side trusted-publishing, prefix-reservation, `NUGET_USER`, and preflight prerequisites visible. `ENG-531` adds archive-level package metadata validation through [`scripts/validate-package-metadata.ps1`](../scripts/validate-package-metadata.ps1), so the package set now proves readme/tag/license/repository/source/package-type/symbol-pairing truth before signing or publishing. `ENG-533` adds a fail-closed NuGet vulnerability audit through [`scripts/validate-nuget-vulnerability-audit.ps1`](../scripts/validate-nuget-vulnerability-audit.ps1), so release validation writes `artifacts/nuget-vulnerability-audit-release/nuget-vulnerability-audit.json` and the signed release bundle includes that advisory scan beside the package, SBOM, signature, and external-policy artefacts. Per-package nuget.org-side configuration (trusted-publishing policy pointing at this repo / this workflow / the tag pattern, plus `Cephalon.*` prefix reservation, plus `NUGET_USER` repository secret, plus the protected `CEPHALON_NUGET_TRUSTED_PUBLISHING_POLICY_CONFIRMED` and `CEPHALON_NUGET_PREFIX_RESERVATION_CONFIRMED` release-manager confirmation variables) must be in place before the first real tag push lands; the workflow itself ships ready to run but does not claim it can independently inspect nuget.org policy state. Follow-up: post-quantum hybrid signing (NIST ML-KEM / ML-DSA) once the .NET cryptography stack ships hybrid signing primitives in the `.NET 12` LTS lane; container base image signing propagation; integration testing the full flow once the first `v*.*.*` tag is cut.

### `ENG-325` — `Cephalon.Analyzers` curated meta-package (Sprint 120)

*Shipped at `M0` taxonomy-only.* New [`src/Cephalon.Analyzers/`](../src/Cephalon.Analyzers/) dependency-only meta-package bundles `Microsoft.CodeAnalysis.BannedApiAnalyzers`, `Microsoft.CodeAnalysis.PublicApiAnalyzers`, `Roslynator.Analyzers`, `Meziantou.Analyzer`, and `Microsoft.VisualStudio.Threading.Analyzers` (the SDK's `Microsoft.CodeAnalysis.NetAnalyzers` are inherited from the .NET 10 SDK and do not need a `PackageReference`). Each bundled analyzer flows transitively to consumers through `IncludeAssets="analyzers; build; buildtransitive"`. The curated [`BannedSymbols.txt`](../src/Cephalon.Analyzers/BannedSymbols.txt) ships under `buildTransitive/` and is wired into the consumer's build automatically through [`Cephalon.Analyzers.props`](../src/Cephalon.Analyzers/buildTransitive/Cephalon.Analyzers.props); consumers opt out via `<CephalonAnalyzersUseBannedSymbols>false</CephalonAnalyzersUseBannedSymbols>`. The curated [`cephalon-analyzers.editorconfig`](../src/Cephalon.Analyzers/cephalon-analyzers.editorconfig) ships under `content/` for consumers to copy or include. See [`docs/components/analyzers.md`](components/analyzers.md) and `ENG-325` in [`engine-backlog.md`](engine-backlog.md). Promote to `M1` when at least one engine project (`Cephalon.Abstractions` is the natural starter) replaces its individual analyzer references with `<PackageReference Include="Cephalon.Analyzers" />`; promote to `M2` when the meta-package is the documented adoption path in `getting-started.md` and the template-pack starter projects reference it by default.

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
