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

## Deployment-mode boundary

`Cephalon.ReferenceDocs` is intentionally outside Cephalon's trim, Native AOT, and single-file support claims. The tool reflects arbitrary referenced assemblies at runtime because that is how it discovers the API members it publishes. Its project file and `scripts/deployment-mode-support.json` both declare `IsTrimmable=false`, `IsAotCompatible=false`, `PublishTrimmed=false`, `PublishAot=false`, and `PublishSingleFile=false`, and the deployment-mode manifest tests verify those values against the project file.

That boundary does not weaken the generated docs contract: hand-authored docs remain the primary product documentation, XML comments remain the API explanation layer, and generated reference output remains optional release/adoption evidence. It only means this repo-local generator is not itself a deploy-anywhere runtime package.

## DocFX readiness

Cephalon's XML comments should stay good enough for DocFX-style API publishing, not only for the repo-local `Cephalon.ReferenceDocs` generator.

Current enforced baseline:

- shipped `src/Cephalon.*` packages are expected to carry complete XML comments on public APIs
- reference-doc coverage tests guard the current shipped assembly set so missing XML summaries are caught in CI before docs generation regresses
- compiler-generated delegate infrastructure members such as delegate constructors and `BeginInvoke` / `EndInvoke` are not published as standalone member contracts; the delegate type XML comment remains the supported API explanation surface

Current follow-up boundary:

- `samples/`, `benchmarks/`, and reference-module packages are expected to carry the same XML-comment completeness before we include them in the supported published docs set
- test projects such as `tests/Cephalon.Tests` are intentionally outside the DocFX/reference-doc publishing boundary, and the shared test harness now keeps only framework-required xUnit classes plus a narrow reflective transport-contract exception public while helpers stay internal so test-only APIs do not drift into the published surface accidentally

## Hosted surface

`Cephalon.AspNetCore` can optionally serve the generated output directly from a running host when `ReferenceDocs` hosting is enabled.

Generated app roots now also emit `Configurations/AddReferenceDocs.json` with the disabled-by-default hosted reference-doc baseline, and `cephalon doctor --app-root <path>` validates that file together with `Configurations/AddOpenApi.json` so the generated `/scalar` plus hosted-reference-doc config shape stays explicit before teams rely on either surface.

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
- `/engine/reference-docs/runtime` for host-level introspection with payload freshness and evaluation-duration metadata
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
`docs validate-hosting` checks that the `ReferenceDocs` section exists, is enabled, resolves to a real directory, and contains the configured default document. It normalizes Windows (`\`) and POSIX (`/`) directory separators before resolving `DirectoryPath`, so generated app configs remain valid when replayed from Linux CI, containers, or Windows workstations. When you pass `--host-url`, it also prints the expected browser, manifest, and `/engine/reference-docs` URLs for that host.

## Publish flow

Build and publish the full reference set:

```powershell
pwsh ./scripts/publish-reference-docs.ps1
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
pwsh ./scripts/publish-reference-docs.ps1 -Assemblies Cephalon.Engine,Cephalon.Agentics
```

Skip the build when the solution has already been compiled:

```powershell
pwsh ./scripts/publish-reference-docs.ps1 -SkipBuild
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
- keep test projects excluded from the generated reference-doc and DocFX publishing set unless they are intentionally promoted into documentation scope
- keep shared test-harness types internal and leave only framework-required xUnit classes plus rare reflective transport-contract exceptions public while the test project remains outside the supported published docs set
- keep hand-authored guide docs in `README.md` and `docs/` focused on capability explanation and adoption guidance
- regenerate `docs/reference/` after changing public API docs
- keep the checked-in `docs/reference/` bundle aligned with the current generator output; the reference-doc test suite now treats bundle drift as a failure and expects `pwsh ./scripts/publish-reference-docs.ps1` to be the repair path
- keep `docs/reference/reference-manifest.json` aligned with the checked-in bundle: Tooling coverage validates required browser/index/manifest assets, manifest-owned assembly pages, namespace/type/member anchor ids, assembly namespace/type counts, and orphan generated Markdown pages before hosted reference docs can be considered stable
- keep `docs/reference/browse.html` aligned with its local browser bundle: Tooling coverage resolves local `href` and `src` targets inside `docs/reference` so the hosted browser shell cannot silently lose its CSS, JavaScript, manifest, or index links
- keep `artifacts/reference-docs-release/` as pipeline output, not as hand-edited source content
- keep the generator, publish script, and generated output aligned with the current solution layout
