# September 27 SRE evidence continuation

Tracking: [ENG-746 / #1439](https://github.com/Cephalon-Labs/CephalonEngine/issues/1439) is an active 24 h rollup within ENG-729 (50 h). [ENG-749 / #1442](https://github.com/Cephalon-Labs/CephalonEngine/issues/1442), 8 h, is complete for the cursor query and release evidence retention. [ENG-750 / #1443](https://github.com/Cephalon-Labs/CephalonEngine/issues/1443), 16 h, retains workload, recovery, telemetry and repeated-run SLO assessment. These are engineering estimates, not recorded hours or CI waiting time. No M0–M4, support, percentile or stable-baseline promotion is included.

## Observed failure and measurement boundary

[Release validation on `340c408e`](https://github.com/Cephalon-Labs/CephalonEngine/actions/runs/36299755712) passed Linux and SDK 11; Windows passed its tests but failed the journal benchmark mean guardrail: **507.5 us > 500 us**. The same runtime and benchmark source passed on `201e92a8` at **409.9 us**. The difference between those commits is documentation only. Allocation was 174.25 KB in both hosted runs. ShortRun reported Error/StdDev of 1347.0/73.83 us for the failing run and 1068.0/58.54 us for the earlier run. Neither comparison establishes a code regression, a root cause, stable percentiles, or zero flakiness. The previous [clock and fixture acceptance](sre-validation-2026-09.md#committed-source-acceptance---september-27) remains source-specific historical evidence.

`EventDispatchDurableJournalBenchmarks.RecordAndReadDurableJournal` uses EF Core **InMemory**, 128 seeded command rows, one retained scope/DbContext, and 256 operations per invocation. Each operation updates an existing result, reads up to four replay entries and reads the latest cursor. It exercises the EF journal API but does not measure database I/O, physical persistence, cross-process durability, production data volume or contention. Setup includes a warm invocation. Workload, ShortRun configuration and the 500 us / 262,144-byte guardrails are unchanged.

## Cursor query change

The EF journal now projects only `ObservedAtUtc` and `CommandId` for `LatestReplayCursor`, preserving descending timestamp/ID ordering, empty results, fresh reads and no tracking. It does not materialize the full journal entity or fetch metadata for this selector. Replay pages and journal writes keep their existing behavior. This follows EF Core's [project only required properties guidance](https://learn.microsoft.com/en-us/ef/core/performance/efficient-querying#project-only-properties-you-need).

The relational regression uses SQLite with an explicit **test-host UTC-ticks conversion** for the journal timestamp. It checks empty reads, timestamp precedence, tied IDs, commits from separate scopes, no tracked journal entities and a two-column result even with 64 KB metadata payloads. This proves the selector with that mapping; it does not add a default SQLite DateTimeOffset mapping or a new provider-support claim. Existing InMemory provider-rebuild journal coverage remains.

## Release timing evidence contract

`scripts/validate-release.ps1` writes `artifacts/sre-release-validation/run.json` at entry with `outcome=running`, then finalizes it as `passed` or `failed` when normal PowerShell cleanup executes. It records a unique run ID, source commit, dirty-worktree flag, canonical/reduced classification, skipped switches, benchmark filters, OS/architecture, available GitHub run/attempt/job and runner-image identity, last entered step, elapsed time and completed timing filenames.

Step timing JSON carries the same `runId`. **Only files listed in `completedTimingReports` with that matching ID belong to the current invocation.** Old timing files may remain in a reused checkout; their presence alone is not current evidence. A failed or reduced run never publishes a canonical `validate-release-wall-time.json`. Successful reduced execution is not full-release acceptance. `outcome=passed` means invoked checks completed; an individual timing `status=investigate` still records a target overrun. Hard termination may leave `running` or no receipt; it must not be read as success.

The workflow uploads this directory as `sre-release-validation-windows` and `sre-release-validation-linux` with `if: always()`. Windows is the canonical benchmark lane; Linux skips benchmarks and remains a reduced invocation. Upload follows GitHub's [workflow artifact guidance](https://docs.github.com/en/actions/concepts/workflows-and-actions/workflow-artifacts). The existing 30-minute full-release target and stable-baseline manifest remain unchanged. These receipts establish traceability, not an automatic SLO renewal.

## Validation checkpoint

Local baseline before the change: **140.8 us**, Error 168.7 us, StdDev 9.24 us, allocation **174.32 KB**, on this Windows development machine with SDK 10.0.401. BenchmarkDotNet warned that the minimum iteration was 99.945 ms. This runner is separate from hosted Windows; absolute cross-runner comparisons are not optimization evidence. Before/after reports are retained under `artifacts/sre-continuation-2026-09-27/`.

The same-machine after run measured **152.4 us**, Error 51.18 us, StdDev 2.81 us, allocation **176.40 KB**. It remains below the unchanged gate but is not an InMemory latency or allocation improvement over the local baseline. The justified benefit is the relational two-column read, independently verified by the regression; resolving hosted-runner variance remains ENG-750. The benchmark is not used to claim a production speedup.

Focused cursor/provider-rebuild tests passed **2/2**. Release-script and planning tests passed **27/27**, including failed partial execution, canonical early failure, successful reduced execution with stale timing present, and the existing exclusion of phase-8-skipped runs from canonical classification. Live planning guards matched **23** open issues and Project items with all five required fields. Full-suite and committed-source CI acceptance are pending at this checkpoint. ENG-749 remains open until its evidence is recorded; ENG-746/729 stay open for ENG-750 even after the bounded implementation is accepted.

Local full composition passed **899/899 in 1 m 6 s**, documentation links **3/3 in 14 s**, and the unchanged focused journal guardrail passed. These local durations are separate from release-step wall time and hosted-runner evidence.

The first committed-source CI attempt on `a329a9ad` rejected a changed backlog-baseline sentence: the maturity-report parser requires its established wording and agreement with the frozen September 16 audit baseline. That sentence is restored, with a separate September 27 task-status note. Local full Pester reported 265/267: the same documentation failure plus an old ignored `.build/t49-verify` project discovered by the recursive analyzer-reference fixture. Clean Linux CI reported 266/267 with only the documentation failure. The final clean CI run remains the authority for full-suite acceptance; the ignored local copy is not a shipped source defect and has not been deleted or included in this delivery.

## Committed-source acceptance

[Release Validation](https://github.com/Cephalon-Labs/CephalonEngine/actions/runs/36313203784), [Host Compatibility](https://github.com/Cephalon-Labs/CephalonEngine/actions/runs/36313203805) and [Contract Compatibility](https://github.com/Cephalon-Labs/CephalonEngine/actions/runs/36313203800) passed on **`e67697ae`**. Windows/Linux release each passed **899 composition + 820 hosting + 383 tooling = 2,102 core tests**, plus **267 Pester**. SDK 11 readiness passed **899 + 820 + 374 = 2,093 selected tests**. All **41** Windows benchmark guardrails passed without changing their thresholds.

Downloaded `sre-release-validation-windows` and `sre-release-validation-linux` both identify the accepted commit, GitHub run **36313203784**, a clean source checkout and `outcome=passed`. Windows records `canonical=true` with restore, reference-doc and full-release timing files; Linux records `canonical=false`, skips benchmarks and lists only restore/reference-doc timings. Every listed file has the matching `runId`. Windows full-release elapsed time is **999.912 s** against **1,800 s**, timing status **`passed`**. This is one retained observation, not a renewed stable baseline. Runs that stop before invoking the release script (for example a Pester failure) have no release receipt.

Local verification additionally includes the 22/22 maturity/report and release-script checks after restoring the parsed baseline wording. The local full-suite limitation involving an ignored historical `.build` copy remains explicit above; clean CI passed all 267 tests.

ENG-749 is complete at 8 h. ENG-746 and ENG-729 remain open for ENG-750's 16 h workload/recovery/telemetry and collector assessment. The InMemory before/after run showed no speedup; the accepted production change is the relational column projection. Package API signatures, provider support declarations, shipping SDK/net10.0 and M0–M4 levels are unchanged.

The accepted Windows journal benchmark measured **147.7 μs**, Error **436.9 μs**, StdDev **23.95 μs**, allocation **176.65 KB**. This passes the unchanged gate; runner differences and short-run variation prevent treating the change from historical hosted values as a proven production speedup.

## ENG-750 proof sequence (planned, not executed)

The retained 16 h estimate covers an initial declared workload set, including review, validation, documentation and tracking. Additional provider/host permutations require explicit scope and estimate changes before making broader claims. These work packages subdivide ENG-750; they are not additive tasks or completed evidence.

| Work package | Estimate | Required output |
| --- | ---: | --- |
| Collector eligibility and coverage | 3 h | Paginated or explicitly incomplete run/attempt coverage; canceled and unresolved outcomes separated; missing inspection blocks promotion; failure attribution distinguishes tests from build/benchmark/publish |
| Workload latency and throughput | 4 h | Source/configuration/hardware declaration, raw observations and sample counts; separate cold-process, warm, steady and burst measurements; p95/p99 are measured from the declared observation population |
| Fault, cancellation and recovery | 3 h | Bounded resource pressure, slow/unavailable dependency and cancellation cases; observed backlog/recovery and retained command outcomes; provider and persistence boundary stated |
| Telemetry budget | 2 h | Attribute allow-list, measured series counts, overflow monitoring, redaction fixtures, exporter outage behavior and retained-byte/export-rate observations |
| Repeated hosted-runner and release timing | 2 h | Comparable runner/source groups, all attempted runs retained, unchanged benchmark gates, matching release run/timing IDs; explain failures and variance |
| Analysis and publication | 2 h | Dashboard/query definitions and evidence index; distinguish descriptive observations from supported SLO windows; update docs, Project and issue state only for accepted scope |

Existing benchmark means remain regression signals. `ColdStartBenchmarks` constructs, starts, probes (ASP.NET Core), stops and disposes hosts repeatedly **inside a warm benchmark process**; its elapsed mean includes shutdown/disposal. It does not isolate build-to-first-response or fresh-process p95. The behavior p95/p99 baseline rows also explicitly declare `benchmark-mean-baseline-proxy`. ENG-750 must preserve these distinctions when adding real percentile evidence.

The Linux `sre-ci-flake-rate-linux` artifact from run **36313203784** illustrates the current collector limitation: six completed matching runs comprise one success, three failures and two cancellations; it reports zero observed rerun events and `PromotionAllowed=true`. Attempt inspection was available for all three inspected failure attempts. This is a descriptive rerun-event result, not proof of zero failing tests or a statistically supported flake SLO. The artifact was captured while the current release workflow was still running. No baseline promotion is accepted from this flag.

Each workload receipt should name SDK/runtime, OS/CPU, source and configuration hashes, process/cache state, provider, concurrency, offered/completed load, warmup and observation windows, errors/cancellations/timeouts, raw samples or histogram boundaries and the aggregation method. Keep request latency, allocation, lifecycle completion, release wall time and provider I/O as separate measurements. Controlled clocks remain useful for deterministic functional tests; elapsed performance evidence uses real elapsed time.

Telemetry review follows the OpenTelemetry [metrics cardinality contract](https://opentelemetry.io/docs/specs/otel/metrics/sdk/#cardinality-limits): filter unnecessary dimensions, set deliberate limits and observe overflow. A bounded aggregate can preserve totals while losing per-attribute distinctions, so dashboards and SLO queries must account for that limitation. This is a review plan, not a claim that every current Cephalon exporter already enforces the proposed budget.
