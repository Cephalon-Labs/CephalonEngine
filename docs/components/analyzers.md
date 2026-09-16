# Cephalon.Analyzers

Repository API authoring uses a direct `Microsoft.CodeAnalysis.PublicApiAnalyzers` reference on Abstractions. A project reference to the analyzer meta-package does not propagate its NuGet analyzer dependencies to the compiler. Packaged consumer adoption remains a separate test path; see [compatibility repair](../compatibility-repair-2026-09.md).

> **Maturity:** `M2` · **Ownership:** `cephalon-managed` — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.Analyzers` is the curated meta-package that gives Cephalon-engine consumers a single `<PackageReference>` for the engine's analyzer baseline plus a curated `BannedSymbols.txt` and `.editorconfig` snippet aligned with Cephalon's quality posture.

## What it owns

- a single dependency-only NuGet package that bundles Cephalon's chosen third-party analyzers (Roslynator, Meziantou, BannedApiAnalyzers, Microsoft.VisualStudio.Threading.Analyzers, PublicApiAnalyzers) so consumers do not have to enumerate each one by hand
- the curated `BannedSymbols.txt` shipped under `buildTransitive/` and wired into the consumer's build automatically through the package's `buildTransitive/Cephalon.Analyzers.props` MSBuild props file
- the curated `cephalon-analyzers.editorconfig` shipped under `content/` so consumers can copy or include the engine's analyzer severity baseline without re-deriving it
- public-API contract tracking as an explicit package-author opt-in through `Microsoft.CodeAnalysis.PublicApiAnalyzers` plus `PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt`; consumer apps keep those warnings suppressed until they set `CephalonAnalyzersEnablePublicApiTracking=true`

## Main surfaces

- `Cephalon.Analyzers.csproj` — the dependency-only meta-package project file
- `BannedSymbols.txt` — the curated banned-symbols set
- `buildTransitive/Cephalon.Analyzers.props` — the MSBuild props file that wires `BannedSymbols.txt` as an `AdditionalFiles` entry in the consumer's build
- `cephalon-analyzers.editorconfig` — the curated `.editorconfig` snippet shipped under `content/`

## How it fits

`Cephalon.Analyzers` is a *consumer-facing* meta-package. The Cephalon engine itself does not depend on it; engine projects continue to inherit analyzer settings through `Directory.Build.props` and the central package management in `Directory.Packages.props`. The generated `cephalon new` scaffold and shipped `dotnet new` template-pack starters now reference the meta-package by default as `PrivateAssets="all"`, so consumers writing modules, behavior implementations, host adapters, or applications on top of Cephalon inherit the engine's quality posture without enumerating analyzer dependencies by hand. Consumers can still adopt it manually by adding one `<PackageReference>` and (optionally) including the bundled `.editorconfig`.

The package keeps the generated-app adoption path warning-clean by default: `buildTransitive/Cephalon.Analyzers.props` wires the curated banned-symbols file automatically, but suppresses PublicApiAnalyzers drift warnings until a package author sets `<CephalonAnalyzersEnablePublicApiTracking>true</CephalonAnalyzersEnablePublicApiTracking>` and commits matching `PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt` files. Consumers can still opt out of the bundled banned-symbols set with `<CephalonAnalyzersUseBannedSymbols>false</CephalonAnalyzersUseBannedSymbols>`.

A compiler-only project reference does not propagate meta-package dependency analyzers. Repository compiler references remove host publish-mode globals through `CephalonCompilerOnlyProjectReferenceGlobalPropertiesToRemove`; consumer apps use the packaged analyzer assets. The analyzer project also declares `TreatAsLocalProperty` for `PublishTrimmed`, `PublishAot`, `PublishSingleFile`, `SelfContained`, `RuntimeIdentifier`, and `RuntimeIdentifiers` so representative trim/AOT/single-file publish probes do not accidentally apply app publish settings to this `netstandard2.0` compiler-only project.

The bundled analyzer severities tune a small set of high-leverage rules:

- `CA2007` / `RCS1090` / `MA0004` (`ConfigureAwait(false)` discipline) → error
- `CA2016` / `MA0040` (forward `CancellationToken` parameters) → error
- `CA2100` (raw SQL via string concat) → error
- `VSTHRD002` / `VSTHRD100` / `VSTHRD110` (sync-over-async, async void, observed task return values) → error
- `RS0030` (banned-API enforcement against the bundled `BannedSymbols.txt`) → error
- `CA1848` / `CA2254` (`LoggerMessage` source generator on hot paths, compile-time-constant log templates) → warning

The curated `BannedSymbols.txt` bans wall-clock time without an injectable abstraction (`DateTime.Now`, `DateTime.UtcNow`, `DateTimeOffset.Now`), synchronous waits on async code (`Task.Wait`, `Task<T>.Result`), `Thread.Sleep` in async contexts, and `Environment.Exit` in hosted scenarios. Consumers can opt out of the bundled banned-symbols set by setting `<CephalonAnalyzersUseBannedSymbols>false</CephalonAnalyzersUseBannedSymbols>` in their MSBuild file, or extend it by including their own `BannedSymbols.txt` as an `AdditionalFile`.

## Maturity and ownership

- maturity today: `M2` — `cephalon-managed`; repository Abstractions enforces public API tracking through a direct private analyzer package reference; `cephalon new` emits `Cephalon.Analyzers` into generated non-test projects through `Directory.Packages.props`, and the template-pack app/module starters reference it by default as a private analyzer package. New consumer apps receive the curated analyzer baseline (`PublicApiAnalyzers`, `BannedApiAnalyzers`, `Roslynator`, `Meziantou`, `Microsoft.VisualStudio.Threading.Analyzers`) and the curated `BannedSymbols.txt` is wired in automatically through the package's `buildTransitive/Cephalon.Analyzers.props`; PublicApiAnalyzers public-surface enforcement remains an explicit package-author opt-in so a fresh generated app can restore, build, run, and probe `/engine/*` without starter-owned analyzer noise.
- promote beyond `M2` only when analyzer enforcement is wired into release scorecard/operator adoption gates with stable remediation guidance, not just package references
- ownership: `cephalon-managed` (the engine owns the curated analyzer baseline, generated adoption wiring, and package-level buildTransitive behavior)

## Cross-references

- [Engineering standards](../engineering-standards.md) — code-quality gates, library / API design, packaging
- [Compatibility](../compatibility.md) — public-API contract artefacts (`PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt`)
- [Engine surface maturity audit](../engine-surface-maturity-audit.md) — `M0`–`M4` plus `taxonomy-only` / `application-managed` / `cephalon-managed` / `provider-managed` truth
- [Supply-chain uplift plan](../supply-chain-uplift-plan.md) — multi-sprint plan that includes this pack as `ENG-325`
- [Roslynator analyzers](https://github.com/dotnet/roslynator) · [Meziantou.Analyzer](https://github.com/meziantou/Meziantou.Analyzer) · [BannedApiAnalyzers](https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.BannedApiAnalyzers/Microsoft.CodeAnalysis.BannedApiAnalyzers.md) · [VS Threading Analyzers](https://github.com/Microsoft/vs-threading) · [PublicApiAnalyzers](https://github.com/dotnet/roslyn-analyzers/blob/main/src/PublicApiAnalyzers/Microsoft.CodeAnalysis.PublicApiAnalyzers.md)
