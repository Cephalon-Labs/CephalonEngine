# Cephalon.Analyzers

Curated meta-package that gives Cephalon-engine consumers a single `<PackageReference>` for the engine's analyzer baseline:

- **Microsoft .NET analyzers** (already in the .NET 10 SDK; the meta-package only tunes severities through the bundled `.editorconfig` snippet)
- [Roslynator.Analyzers](https://github.com/dotnet/roslynator) — broad C# style and design rules
- [Meziantou.Analyzer](https://github.com/meziantou/Meziantou.Analyzer) — pragmatic modern-C# rules (async, perf, nullability)
- [Microsoft.CodeAnalysis.BannedApiAnalyzers](https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.BannedApiAnalyzers/Microsoft.CodeAnalysis.BannedApiAnalyzers.md) — paired with the curated `BannedSymbols.txt` shipped in this package
- [Microsoft.VisualStudio.Threading.Analyzers](https://github.com/Microsoft/vs-threading) — async / threading correctness for engine and module code
- [Microsoft.CodeAnalysis.PublicApiAnalyzers](https://github.com/dotnet/roslyn-analyzers/blob/main/src/PublicApiAnalyzers/Microsoft.CodeAnalysis.PublicApiAnalyzers.md) — pair with `PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt` to track public-API drift as a reviewable artefact

## Usage

Add a reference:

```xml
<PackageReference Include="Cephalon.Analyzers">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```

The bundled `BannedSymbols.txt` is wired up automatically through the package's
`buildTransitive/Cephalon.Analyzers.props` file. To opt out:

```xml
<PropertyGroup>
  <CephalonAnalyzersUseBannedSymbols>false</CephalonAnalyzersUseBannedSymbols>
</PropertyGroup>
```

The shipped `cephalon-analyzers.editorconfig` is in the `content/` folder of the package — copy
its contents into your repo's `.editorconfig` (or include the file directly) to inherit the
engine's analyzer severity baseline. The snippet enables strict severities for `ConfigureAwait`,
cancellation-token forwarding, sync-over-async detection, banned-API enforcement, and a few
Roslynator / Meziantou rules that complement the SDK defaults.

## Cephalon-engine context

The meta-package is part of the engine's supply-chain uplift plan. See
[`docs/components/analyzers.md`](https://github.com/Cephalon-Labs/CephalonEngine/blob/master/docs/components/analyzers.md)
for the component overview and
[`docs/supply-chain-uplift-plan.md`](https://github.com/Cephalon-Labs/CephalonEngine/blob/master/docs/supply-chain-uplift-plan.md)
for the multi-sprint context.
