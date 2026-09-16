# Local branch consolidation — September 15–16, 2026

Tracking: [ENG-717 / GitHub #1410](https://github.com/Cephalon-Labs/CephalonEngine/issues/1410), Phase 14, Operational Sprint 0 (September 2–15), estimate 16.

Status: shipped to master; local/GitHub branch consolidation complete on September 16, 2026.

## Scope and retained history

The initial local repository contained 139 branches at 51 distinct tips. Local `master` was 29 commits ahead of and 521 commits behind GitHub `master` (`77229295`). All original branch tips are retained as ancestors of the integration result. The 138 non-master local branch names were removed after the validated result reached local and GitHub `master`.

The two remaining GitHub feature branches had already merged through PRs #1408 and #1409 and were deleted after ancestry verification. A verified local Git bundle and a branch-to-commit inventory preserve the pre-consolidation state. All 130 non-master branch-linked worktrees were detached at their original commits, with identical before/after status and staged/unstaged binary diffs. Existing files in two dirty worktrees remain intact. The pending merge in `frosty-shtern-500fcd` was preserved, including `MERGE_HEAD` and merge metadata, by detaching its HEAD reference without aborting or resetting it. No existing worktree directory or nested checkout was removed.

The integrated code and validation baseline were published as [a16280cf](https://github.com/Cephalon-Labs/CephalonEngine/commit/a16280cf0ab1d8d7b36662ad07848957c52e03bb). Local and remote branch inventories contain only `master`; final closeout commits and CI results are linked from ENG-717.

## Resulting behavior

- gRPC, JSON-RPC, SSE, and WebSocket direct-module resilience surfaces expose process-local timeout occurrence, circuit-open transition, and open-circuit rejection counters with last-occurrence timestamps.
- Selected JSON-RPC endpoints use a JSON-RPC error envelope for endpoint rate-limit rejection, including HTTP 429 and retry guidance.
- Nine catalog readback endpoints add payload evaluation time and duration. These are observation metadata (`M1`), not reconciliation or remediation automation.
- Diagnostics readback reports liveness/readiness evaluation durations. Trust readback reports policy-projection timestamps without changing published positional constructors or deconstruction signatures.
- A missing capability source module is denied, including a missing source in a shared capability's source-module set.
- Local transport, identity, dependency-health, and runtime-readback regression tests are integrated with the current upstream tests.
- Additional framework/AI architecture research and module adoption cross-links are retained as dated reference material.

The current `net10.0` shipping floor, current provider ownership, package maturity distribution, source-generated JSON contracts, deployment-mode claim gates, and Windows benchmark guardrails remain authoritative. The build uses installed SDK `10.0.303`; the three SDK-provided ILLink lock entries are refreshed to its `10.0.11` tool version.

## Validation-discovered release hygiene

The full solution restore found the test harness transitively using `SSH.NET` 2025.1.0. The centrally pinned 2026.0.0 version is the patched version identified by [GHSA-q939-rpr3-3284](https://github.com/advisories/GHSA-q939-rpr3-3284); refreshed lock files keep the dependency correction scoped to the provider/CDC integration test projects.

`global.json` now selects SDK 10.0.303 to match the SDK-provided ILLink lock entries. The provider Testcontainers workflow now gives setup-dotnet the repository's `**/packages.lock.json` layout. The September 13 run failed before Docker execution because setup-dotnet looked for a root lock file; the [failed run](https://github.com/Cephalon-Labs/CephalonEngine/actions/runs/34783348093) provides the original annotation. Release-validation's existing cache configuration remains the baseline.

The CatalogService sample retains its existing unwrapped JSON response contract after the imported result-envelope configuration caused two sample assertions to fail. The sample suite passes after this resolution.

The provider pack generator now delimits provider names before a literal colon, fixing two PowerShell parser errors in its implementation checklist. Both imported pack generators target the current split test projects and pass syntax validation. Obsolete session handoff and local push-watcher files are retired; their contents remain in the retained commit history.

The first post-push Release Validation run exposed stale scorecard test expectations for pending public APIs. The JSON and Markdown assertions now match the measured catalog: 3 packages with 205 additions, 101 header-only packages, and zero removals. The gate remains strict; only its repository snapshot expectations changed.

## Superseded drafts

The following source drafts are retained in merge ancestry while their already-shipped successors remain the implementation. This avoids replacing newer provider contracts, moving tests back to obsolete fixtures, or reverting benchmark calibrations.

| Original tip | Retained implementation and reason |
| --- | --- |
| `0efb8a5e` | `4e7815ae` and later Windows guardrails: durable command-journal benchmarks supersede the older event-stream benchmark draft. |
| `159222c5` | `dd9c463b`: downstream delivery proof uses the current provider execution contract. |
| `51492c98` | `fcfebc83`: choreography proof shipped and the integration tests moved to `EntityFrameworkDataPackTests`. |
| `8366914b` / `3ca2a3a3` | `e2074f0a` / `8ed5a4f8`: broker topology and inbound consumption use the current catalog. |
| `e2975f56` | `d5184dec`: broker DLQ replay uses the current provider execution contract. |
| `2805a8be` | `627da66e` / `f5f2e31c`: current wire-contract and serialization proofs replace the earlier metadata vocabulary. |
| `74062d94` | `34f24b97`: the identical resilience source extraction shipped as ENG-390; newer lock files remain authoritative. |

Patch-equivalent planning changes are joined without reapplying them. Overlapping historical dates and status edits retain the current statement; the original versions remain available in commit history. In particular, observation timestamps do not promote dependency-health providers from `M2` to `M3`.

## Validation

Validation on Windows with SDK 10.0.303:

| Check | Result |
| --- | --- |
| Full solution Release build | Passed, 0 warnings and 0 errors. |
| Composition suite | 859/859 passed. |
| Hosting suite | 816/818 initially passed; the two CatalogService failures were fixed, and all 6 sample-suite tests passed on rerun. The whole hosting suite was not rerun after that configuration-only fix. |
| Tooling, public surface, reference documentation, and documentation coverage | 287/287 passed after updating the public-type allow-list and generated bundle. |
| PowerShell suites | 233/233 passed across all 14 files, including the scorecard API-count correction. |
| Provider/observability pack generators | Both scripts pass PowerShell syntax validation. |
| API delta report | 205 pending additions, 0 removals across 3 packages; published positional record signatures retained. |
| Generated reference bundle | 88 files for 79 supported assemblies; no test assemblies in the catalog. |
| Surface maturity | 107 packages, zero drift: M0=1, M1=39, M2=51, M3=7, M4=9. |
| Focused composition benchmarks | Existing 130 us / 70,000 B guardrails pass for both measured scenarios: baseline 71.21 us / 60.41 KB; strict trust 75.41 us / 60.02 KB. |
| Planning issue and Project guards | All open planning issues have backlog entries and the required Status, Estimate, Iteration, Test, and Benchmark fields. |

The benchmark result uses the shipped in-process short-run configuration and unchanged thresholds. It covers the two affected manifest-composition scenarios, not the entire release benchmark catalog. An earlier run overlapped a solution build and was discarded after causing Windows output-file contention; the full build and measured benchmark were then run separately.

Local evidence is retained under `.git/branch-consolidation-20260915/`: the verified pre-consolidation bundle, branch inventory, integration decisions, build logs, TRX/Pester results, reference output, and benchmark CSVs. The branch cleanup records capture each original tip and before/after worktree status and patch hashes.

The signed-release rehearsal, provider live infrastructure suite, and a fresh cross-platform GitHub release-validation result are separate evidence. This consolidation does not claim those checks passed locally or advance the open signed-release blocker or M3/M4 operator-loop follow-ups.

## Related guides

- [Runtime contract index](runtime-contract-index.md)
- [Operations](operations.md)
- [ASP.NET Core adapter](components/aspnetcore.md)
- [M3/M4 elevation plan](m3-m4-elevation-plan.md)
- [Planning governance](planning-governance.md)
