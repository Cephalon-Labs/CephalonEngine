# Cephalon.Tests.Scripts

This directory contains Pester tests for the PowerShell scripts under `scripts/`.

## Why this exists

The `scripts/` directory ships PowerShell entry points that participate in release validation, framework readiness, package publishing, claim auditing, and operational checks. Until this slice the repo had no automated coverage of those scripts beyond ad-hoc smoke runs through `scripts/validate-release.ps1`.

Pester tests live here so the script logic can be exercised in isolation, with deterministic inputs, against temporary fixtures rather than against the real repo state.

## Requirements

- PowerShell 7.0 or later
- Pester 5.0 or later

Install Pester (Windows / Linux / macOS):

```powershell
Install-Module -Name Pester -MinimumVersion 5.0.0 -Scope CurrentUser -Force -SkipPublisherCheck
```

## Running the tests

From the repo root:

```powershell
Invoke-Pester -Path tests/Cephalon.Tests.Scripts -Output Detailed
```

For a single file:

```powershell
Invoke-Pester -Path tests/Cephalon.Tests.Scripts/validate-deployment-mode-claims.Tests.ps1 -Output Detailed
```

For CI integration:

```powershell
$config = New-PesterConfiguration
$config.Run.Path = 'tests/Cephalon.Tests.Scripts'
$config.Output.Verbosity = 'Detailed'
$config.TestResult.Enabled = $true
$config.TestResult.OutputFormat = 'NUnitXml'
$config.TestResult.OutputPath = 'artifacts/script-test-results/results.xml'
Invoke-Pester -Configuration $config
```

## Conventions

The PowerShell scripts under `scripts/` follow these patterns so they are testable:

- functions are defined at the top of the file with no top-level side effects
- the main entry block at the bottom is guarded by an environment variable
  (for example `$env:CEPHALON_VALIDATE_DEPLOYMENT_MODE_NO_RUN` or
  `$env:CEPHALON_VALIDATE_RELEASE_NO_RUN`) so tests can dot-source
  the script without triggering the entry block
- external commands (such as `dotnet`) are accepted through an injectable parameter
  (for example `-DotnetCommand`) so tests can substitute deterministic stubs
- functions return structured `[pscustomobject]` results rather than printing free-form text,
  so assertions are precise

Each test file should:

- live next to the script it covers, named `{script-name}.Tests.ps1`
- set the matching `NO_RUN` env var in `BeforeAll`
- clear it in `AfterAll`
- create temp directories under `[System.IO.Path]::GetTempPath()` and clean them up in `AfterAll`
- prefer Pester 5 `Describe` / `Context` / `It` syntax with `BeforeAll` / `BeforeEach` for setup

## Current coverage

| Script | Test file | Cases |
| --- | --- | --- |
| `scripts/generated-app-runtime-contract.ps1` | `generated-app-runtime-contract.Tests.ps1` | 13 cases: schema/configuration/module/capability/startup drift, canonical foundation ID, snapshot consistency, additive fields and equivalent JSON property ordering |
| `scripts/validate-contract-compatibility.ps1` | `validate-contract-compatibility.Tests.ps1` | 9 cases: package identity/hashes, declared framework, traversal/ambiguous metadata/DTD rejection, subprocess exits and timeout; AST extraction loads only the two guard functions without running the harness |
| `scripts/run-provider-live-testcontainers.ps1` | `run-provider-live-testcontainers.Tests.ps1` | provider Testcontainers matrix + filter tokens + locked restore + workflow dispatch/schedule/matrix wiring |
| `scripts/measure-ci-flake-rate.ps1` | `measure-ci-flake-rate.Tests.ps1` | GitHub Actions fixture parsing + no-history pending report + rerun flake detection + clean-window promotion candidate + fail-closed promotion gate |
| `scripts/publish-engine-completion-scorecard.ps1` | `publish-engine-completion-scorecard.Tests.ps1` | scorecard JSON/Markdown artifact shape + evidence-source reference validation + per-package GA readiness rows + provider integration evidence manifest validation + Eventing operational-superiority manifest/readback/runtime-concordance validation + SRE guardrail-reference validation + unsupported status guard + release-validation wiring |
| `scripts/validate-supply-chain-external-policy-preflight.ps1` | `validate-supply-chain-external-policy-preflight.Tests.ps1` | pending/pass/fail-closed external policy checks + release-manager confirmation parsing |
| `scripts/validate-nuget-vulnerability-audit.ps1` | `validate-nuget-vulnerability-audit.Tests.ps1` | captured `dotnet list package --vulnerable` JSON parsing + fail-closed vulnerability report + report-only override |
| `scripts/validate-package-metadata.ps1` | `validate-package-metadata.Tests.ps1` | NuGet package metadata/readme/tag/repository checks + symbol package pairing |
| `scripts/validate-release.ps1` | `validate-release.Tests.ps1` | scorecard evidence readback behavior + dependency-health provider manifest readback + Eventing operational-superiority missing-node/blocked-promotion/runtime-concordance failure + missing manifest failure |
| `scripts/summarise-public-api-deltas.ps1` | `summarise-public-api-deltas.Tests.ps1` | markdown/JSON report shape + optional removal gate + release-validation removal-gate wiring |
| `scripts/validate-planning-github-issues.ps1` | `validate-planning-github-issues.Tests.ps1` | duplicate open ENG issue detection + stale open issue detection for done backlog rows + closeout warning behavior |
| `scripts/validate-planning-project-fields.ps1` | `validate-planning-project-fields.Tests.ps1` | Project 2 required-field validation + missing Project item detection + offline GraphQL-shaped fixture import |
| `scripts/validate-deployment-mode-claims.ps1` | `validate-deployment-mode-claims.Tests.ps1` | 57 cases across 12 describe groups |
| `scripts/deployment-mode-support.json` | `deployment-mode-support-manifest.Tests.ps1` | manifest schema 1.1.0 shape + per-mode field assertions |

Add new test files to this table when they ship.

## Cross-references

- [`docs/engineering-standards.md`](../../docs/engineering-standards.md) — testing standards
- [`docs/deployment-mode-support.md`](../../docs/deployment-mode-support.md) — deployment-mode validation harness
- [`docs/dotnet11-readiness.md`](../../docs/dotnet11-readiness.md) — framework readiness flow
- [`docs/contract-compatibility.md`](../../docs/contract-compatibility.md) — seven executable cross-version consumer scenarios and Windows/Linux CI
