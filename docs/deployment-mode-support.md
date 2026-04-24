# Deployment-mode support

This guide records the current Cephalon support contract for trimming, Native AOT, and single-file publishing.

## Current contract

| Concern | Current contract | Notes |
| --- | --- | --- |
| Stable shipping floor | `net10.0` | Cephalon still ships stable packages, templates, and samples on the current LTS baseline. |
| Higher-SDK readiness lane | `net11.0` assessment-only | `.NET 11` remains a readiness lane, not a supported default-target migration. |
| Trim | `not-claimed` | Trimming is not part of the current Cephalon support contract. |
| Native AOT | `not-claimed` | Native AOT is not part of the current Cephalon support contract. |
| Single-file | `not-claimed` | Single-file publishing is not part of the current Cephalon support contract. |

## Source of truth

Cephalon now keeps this contract explicit in two layers:

- machine-readable manifest: `scripts/deployment-mode-support.json`
- repo-native validation and reporting: `scripts/validate-dotnet-readiness.ps1`

The broader framework-readiness story stays aligned through:

- [.NET 11 readiness](dotnet11-readiness.md)
- [Compatibility](compatibility.md)
- [Package publishing](package-publishing.md)

## What the current statuses mean

- `not-claimed` means Cephalon does not currently publish trim, Native AOT, or single-file as supported deployment modes for external adopters.
- `not-claimed` does **not** block targeted local experiments, but those experiments do not become repo truth by themselves.
- analyzer-only signals remain useful readiness input, but they do not widen the support contract without matching validation, docs, and planning updates.

The reserved future `claimed` state should only be used once Cephalon intentionally enables and validates that deployment mode as part of the shipped framework story.

## How a support claim changes

For trim, Native AOT, or single-file support to become real Cephalon support statements, all of the following must move together:

- project properties that intentionally enable or declare the deployment mode
- `scripts/validate-dotnet-readiness.ps1`
- release-validation workflow coverage
- `docs/deployment-mode-support.md`
- `docs/compatibility.md`
- `docs/package-publishing.md`
- `docs/project-memory.md`
- `docs/engine-roadmap.md`
- `docs/engine-backlog.md`

## What this guide does not mean

- it does not move Cephalon's shipping floor from `net10.0`
- it does not turn `.NET 11` readiness into a default-target migration
- it does not claim trim, Native AOT, or single-file compatibility today
- it does not let support statements outrun what the readiness report and release-validation flow actually prove
