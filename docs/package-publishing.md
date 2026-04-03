# Package Publishing

This document describes the repo-native flow for producing the NuGet and template artifacts Cephalon intentionally ships.

## Scope

The release package-artifact baseline currently includes:

- shipped `src/Cephalon.*` packages, except `Cephalon.Cli`
- `templates/Cephalon.TemplatePack`
- `samples/Cephalon.ReferenceModule.Operations`

The baseline intentionally excludes:

- benchmarks and playground hosts
- sample application hosts
- sample-only shared libraries such as the `MicroserviceSuite` governance and shared-foundation projects
- `Cephalon.Cli` until we decide and ship dedicated tool packaging explicitly

## Shared package metadata

Packable Cephalon packages now inherit shared NuGet metadata from `Directory.Build.props`:

- `Authors`
- `PackageLicenseExpression`
- `PackageProjectUrl`
- `RepositoryUrl`
- `RepositoryType`
- `PublishRepositoryUrl`
- a shared `PACKAGE.md` readme when a project does not provide a package-specific readme already

`Cephalon.TemplatePack` keeps its own package-specific `PACKAGE.md`.

## Publish flow

Publish the intended release package set:

```powershell
.\scripts\publish-package-artifacts.ps1
```

Skip the build when the repository has already been compiled:

```powershell
.\scripts\publish-package-artifacts.ps1 -SkipBuild
```

Choose a custom output directory:

```powershell
.\scripts\publish-package-artifacts.ps1 -OutputPath artifacts\packages-preview
```

## Output

The publish script writes package artifacts to `artifacts/packages-release/` by default:

- `.nupkg` package files for the intended release-pack surface
- `package-artifacts-manifest.json` with the packed project list and produced artifacts

## Release validation

`.\scripts\validate-release.ps1` now includes package-artifact publishing by default alongside:

- solution build
- test execution
- operational convention validation
- benchmark smoke coverage and guardrails
- reference-doc publishing

The GitHub Actions release-validation workflow uploads `artifacts/packages-release/` as the `package-artifacts` workflow artifact.

## Maintenance rules

- keep the intended packable surface explicit; do not rely on solution-wide `dotnet pack` defaults
- keep benchmarks, playgrounds, and sample-only libraries out of the release package set unless they are deliberately promoted
- keep shared package metadata aligned with the actual release surface
- keep package-publishing docs, the publish script, and release-validation automation aligned when the package boundary changes
