# Reference Docs

This document describes the optional repo-local tooling for publishing API reference output from Cephalon's XML comments.

The documentation model for this repository is:

- hand-authored `.md` files explain the engine, its shipped capabilities, and how teams should adopt it
- XML comments on public contracts explain API behavior in code and feed external documentation generators
- `Cephalon.ReferenceDocs` is convenience tooling for publishing a browsable reference bundle inside this repo when we want one

## Source of truth

- hand-authored product and adoption guides in `README.md` and `docs/*.md`
- public XML comments in `src/*` for API/reference explanations
- compiled assemblies in `src/*/bin/<Configuration>/<TargetFramework>/`
- optional generated reference output in `docs/reference/`

## Optional repo-local tooling

- generator project: `src/Cephalon.ReferenceDocs`
- publish script: `scripts/publish-reference-docs.ps1`
- CLI command: `dotnet run --project src/Cephalon.Cli -- docs publish --root .`
- hosting helper: `dotnet run --project src/Cephalon.Cli -- docs enable-hosting --appsettings src/MyApp/appsettings.json --root .`
- chained flow: `dotnet run --project src/Cephalon.Cli -- docs publish --root . --enable-hosting --appsettings src/MyApp/appsettings.json`
- chained validation flow: `dotnet run --project src/Cephalon.Cli -- docs publish --root . --enable-hosting --validate-hosting --appsettings src/MyApp/appsettings.json --host-url https://localhost:7235`
- open generated docs: `dotnet run --project src/Cephalon.Cli -- docs publish --root . --open`
- open hosted docs URL: `dotnet run --project src/Cephalon.Cli -- docs publish --root . --enable-hosting --appsettings src/MyApp/appsettings.json --open --host-url https://localhost:7235`
- validate hosted docs config: `dotnet run --project src/Cephalon.Cli -- docs validate-hosting --appsettings src/MyApp/appsettings.json --host-url https://localhost:7235`

The generator reads built assemblies plus their adjacent XML documentation files, reflects the public API surface, and writes assembly-level Markdown pages together with shared navigation assets for assemblies, namespaces, types, and members.

## DocFX readiness

Cephalon's XML comments should stay good enough for DocFX-style API publishing, not only for the repo-local `Cephalon.ReferenceDocs` generator.

Current enforced baseline:

- shipped `src/Cephalon.*` packages are expected to carry complete XML comments on public APIs
- reference-doc coverage tests guard the current shipped assembly set so missing XML summaries are caught in CI before docs generation regresses

Current follow-up boundary:

- `samples/`, `benchmarks/`, and other non-shipped code paths are not yet held to the same repo-wide XML-comment standard
- if DocFX input expands beyond shipped packages, bring those assemblies up to the same XML-comment completeness before adding them to the published docs set

## Hosted surface

`Cephalon.AspNetCore` can optionally serve the generated output directly from a running host when `ReferenceDocs` hosting is enabled.

```json
{
  "ReferenceDocs": {
    "Enabled": true,
    "RoutePrefix": "/reference",
    "DirectoryPath": "..\\..\\docs\\reference",
    "DefaultDocument": "browse.html"
  }
}
```

When this is enabled, `MapCephalon()` exposes:

- `/engine/reference-docs` for host-level introspection
- `/reference/browse.html` for the browser UI
- `/reference/README.md`, `/reference/namespaces.md`, `/reference/types.md`, and `/reference/members.md`
- `/reference/reference-manifest.json` for tooling

You can flip an existing host to the enabled state through the CLI instead of editing JSON by hand:

```powershell
dotnet run --project src/Cephalon.Cli -- docs enable-hosting `
  --appsettings src/Acme.Store.Service/appsettings.json `
  --root .
```

If the host already contains a `ReferenceDocs` section, the command preserves its current route prefix, directory path, and default document unless you explicitly override them with `--route-prefix`, `--directory`, or `--default-document`.
The chained `docs publish --enable-hosting` flow follows the same override rules, but writes `DirectoryPath` relative to the publish output directory so custom docs destinations stay aligned with the host config.
`docs validate-hosting` checks that the `ReferenceDocs` section exists, is enabled, resolves to a real directory, and contains the configured default document. When you pass `--host-url`, it also prints the expected browser, manifest, and `/engine/reference-docs` URLs for that host.

## Publish flow

Build and publish the full reference set:

```powershell
.\scripts\publish-reference-docs.ps1
```

Publish through the main Cephalon CLI:

```powershell
dotnet run --project src/Cephalon.Cli -- docs publish --root .
```

Open the generated browser UI right after publishing:

```powershell
dotnet run --project src/Cephalon.Cli -- docs publish `
  --root . `
  --open
```

Publish and enable hosted reference docs in one step:

```powershell
dotnet run --project src/Cephalon.Cli -- docs publish `
  --root . `
  --enable-hosting `
  --appsettings src/Acme.Store.Service/appsettings.json
```

Publish, enable hosting, and validate the host wiring in one step:

```powershell
dotnet run --project src/Cephalon.Cli -- docs publish `
  --root . `
  --enable-hosting `
  --validate-hosting `
  --appsettings src/Acme.Store.Service/appsettings.json `
  --host-url https://localhost:7235
```

Publish, enable hosting, and open the hosted route:

```powershell
dotnet run --project src/Cephalon.Cli -- docs publish `
  --root . `
  --enable-hosting `
  --appsettings src/Acme.Store.Service/appsettings.json `
  --open `
  --host-url https://localhost:7235
```

Validate an app host before you run or deploy it:

```powershell
dotnet run --project src/Cephalon.Cli -- docs validate-hosting `
  --appsettings src/Acme.Store.Service/appsettings.json `
  --host-url https://localhost:7235
```

Publish a narrowed set of assemblies:

```powershell
.\scripts\publish-reference-docs.ps1 -Assemblies Cephalon.Engine,Cephalon.Agentics
```

Skip the build when the solution has already been compiled:

```powershell
.\scripts\publish-reference-docs.ps1 -SkipBuild
```

## Output

- `docs/reference/README.md`
- `docs/reference/browse.html`
- `docs/reference/index.md`
- `docs/reference/namespaces.md`
- `docs/reference/types.md`
- `docs/reference/members.md`
- `docs/reference/reference-manifest.json`
- one assembly page per documented package, for example `docs/reference/cephalon-engine.md`
- `artifacts/reference-docs-release/` for release-validation and CI artifact publishing

The browser UI can switch between type search and member search, while the JSON manifest now includes both type-level and member-level metadata for future docs-site tooling.

## Maintenance rules

- keep XML comments meaningful on all public contracts so external doc tools and IntelliSense remain accurate
- keep the DocFX input set aligned with the assemblies covered by XML-comment completeness checks
- keep hand-authored guide docs in `README.md` and `docs/` focused on capability explanation and adoption guidance
- regenerate `docs/reference/` after changing public API docs
- keep `artifacts/reference-docs-release/` as pipeline output, not as hand-edited source content
- keep the generator, publish script, and generated output aligned with the current solution layout
