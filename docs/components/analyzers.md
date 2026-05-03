# Cephalon.Analyzers

> **Maturity:** `M1` · **Ownership:** `cephalon-managed` — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.Analyzers` is the curated meta-package that gives Cephalon-engine consumers a single `<PackageReference>` for the engine's analyzer baseline plus a curated `BannedSymbols.txt` and `.editorconfig` snippet aligned with Cephalon's quality posture.

## What it owns

- a single dependency-only NuGet package that bundles Cephalon's chosen third-party analyzers (Roslynator, Meziantou, BannedApiAnalyzers, Microsoft.VisualStudio.Threading.Analyzers, PublicApiAnalyzers) so consumers do not have to enumerate each one by hand
- the curated `BannedSymbols.txt` shipped under `buildTransitive/` and wired into the consumer's build automatically through the package's `buildTransitive/Cephalon.Analyzers.props` MSBuild props file
- the curated `cephalon-analyzers.editorconfig` shipped under `content/` so consumers can copy or include the engine's analyzer severity baseline without re-deriving it
- public-API contract lock-in from day one through `Microsoft.CodeAnalysis.PublicApiAnalyzers`, `PublicAPI.Shipped.txt` (header-only on first ship), and `PublicAPI.Unshipped.txt` (empty)

## Main surfaces

- `Cephalon.Analyzers.csproj` — the dependency-only meta-package project file
- `BannedSymbols.txt` — the curated banned-symbols set
- `buildTransitive/Cephalon.Analyzers.props` — the MSBuild props file that wires `BannedSymbols.txt` as an `AdditionalFiles` entry in the consumer's build
- `cephalon-analyzers.editorconfig` — the curated `.editorconfig` snippet shipped under `content/`

## How it fits

`Cephalon.Analyzers` is a *consumer-facing* meta-package. The Cephalon engine itself does not depend on it; engine projects continue to inherit analyzer settings through `Directory.Build.props` and the central package management in `Directory.Packages.props`. The point of the meta-package is that consumers writing modules, behavior implementations, host adapters, or applications on top of Cephalon can adopt the engine's quality posture by adding one `<PackageReference>` and (optionally) including the bundled `.editorconfig`.

The bundled analyzer severities tune a small set of high-leverage rules:

- `CA2007` / `RCS1090` / `MA0004` (`ConfigureAwait(false)` discipline) → error
- `CA2016` / `MA0040` (forward `CancellationToken` parameters) → error
- `CA2100` (raw SQL via string concat) → error
- `VSTHRD002` / `VSTHRD100` / `VSTHRD110` (sync-over-async, async void, observed task return values) → error
- `RS0030` (banned-API enforcement against the bundled `BannedSymbols.txt`) → error
- `CA1848` / `CA2254` (`LoggerMessage` source generator on hot paths, compile-time-constant log templates) → warning

The curated `BannedSymbols.txt` bans wall-clock time without an injectable abstraction (`DateTime.Now`, `DateTime.UtcNow`, `DateTimeOffset.Now`), synchronous waits on async code (`Task.Wait`, `Task<T>.Result`), `Thread.Sleep` in async contexts, and `Environment.Exit` in hosted scenarios. Consumers can opt out of the bundled banned-symbols set by setting `<CephalonAnalyzersUseBannedSymbols>false</CephalonAnalyzersUseBannedSymbols>` in their MSBuild file, or extend it by including their own `BannedSymbols.txt` as an `AdditionalFile`.

## Maturity and ownership

- maturity today: `M1` — `cephalon-managed`; `Cephalon.Abstractions` consumes the meta-package via a `PrivateAssets=all` `ProjectReference` (replacing its previous individual `Microsoft.CodeAnalysis.PublicApiAnalyzers` reference), so the host-agnostic contract layer now inherits the engine's curated analyzer baseline (`PublicApiAnalyzers`, `BannedApiAnalyzers`, `Roslynator`, `Meziantou`, `Microsoft.VisualStudio.Threading.Analyzers`) and the curated `BannedSymbols.txt` is wired in automatically through the package's `buildTransitive/Cephalon.Analyzers.props`
- promote to `M2` when the meta-package is the documented adoption path in `getting-started.md` and the template-pack starter projects reference it by default
- ownership: `cephalon-managed` (since `Cephalon.Abstractions` now consumes the meta-package as the engine's analyzer baseline)

## Cross-references

- [Engineering standards](../engineering-standards.md) — code-quality gates, library / API design, packaging
- [Compatibility](../compatibility.md) — public-API contract artefacts (`PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt`)
- [Engine surface maturity audit](../engine-surface-maturity-audit.md) — `M0`–`M4` plus `taxonomy-only` / `application-managed` / `cephalon-managed` / `provider-managed` truth
- [Supply-chain uplift plan](../supply-chain-uplift-plan.md) — multi-sprint plan that includes this pack as `ENG-325`
- [Roslynator analyzers](https://github.com/dotnet/roslynator) · [Meziantou.Analyzer](https://github.com/meziantou/Meziantou.Analyzer) · [BannedApiAnalyzers](https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.BannedApiAnalyzers/Microsoft.CodeAnalysis.BannedApiAnalyzers.md) · [VS Threading Analyzers](https://github.com/Microsoft/vs-threading) · [PublicApiAnalyzers](https://github.com/dotnet/roslyn-analyzers/blob/main/src/PublicApiAnalyzers/Microsoft.CodeAnalysis.PublicApiAnalyzers.md)
