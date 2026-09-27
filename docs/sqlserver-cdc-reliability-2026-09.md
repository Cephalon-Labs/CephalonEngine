# SQL Server CDC reliability evidence

ENG-753 / [#1446](https://github.com/Cephalon-Labs/CephalonEngine/issues/1446), September 27, 2026. Owner: Cephalon-Neza. Estimate: **6 engineering hours** including implementation, tests, docs and tracking. This is additional observed scope under ENG-729, whose non-additive rollup increases **50 -> 56 h**. September scope becomes **572 h** and Phase 15 **244 h**. ENG-752 retains its separate **13 h** workload, benchmark and statistical evidence scope.

Subsequent scope review adds ENG-754 / [#1447](https://github.com/Cephalon-Labs/CephalonEngine/issues/1447), **12 h**, for four remaining native providers. Current ENG-729 rollup is **68 h**, September scope **584 h**, and Phase 15 **256 h**. The first paragraph records the SQL Server-only estimate checkpoint; these parent totals are not additional leaf hours.

## Trigger and behavior

[Windows release run 36318940272](https://github.com/Cephalon-Labs/CephalonEngine/actions/runs/36318940272) on `05d25dfe` failed the SQL Server CDC test at `lastOperationType`. Linux passed. The test allowed either Captured or Idle and then required metadata emitted only by Captured. The public state contract defines metadata as the **latest report**, so the next idle poll can legitimately replace it while preserving cumulative counts and the last checkpoint. Waiting for outbox staging is also earlier than checkpoint commit and report acceptance.

Inspection found a separate runtime defect: staging/checkpoint errors were reported by the iteration and then rethrown to the outer loop, which emitted another generic `capture` failure. That inflated the failure count and replaced `failureKind`, `pendingChangeId` and `pendingCheckpoint`. The repair ends a handled failed iteration after reporting and logging its specific failure. The normal polling delay and retry loop continue. Read failures still use the outer capture failure path; requested cancellation propagates without a fabricated success or failure report.

## Declared proof matrix

The tests run the real hosted service, DI composition, shared reporter/state catalog and runtime summary. A controlled transport replaces SQL Server I/O; a reporter decorator forwards each observation to the real catalog before retaining its public readback. Assertions consume accepted snapshots even after the loop advances to Idle. No sleep duration decides when a report is considered accepted.

| Scenario | Required proof |
| --- | --- |
| Delete, insert, update-before, update-after | Message operation/header, accepted checkpoint, one captured change, pending publication and correct runtime ownership |
| Captured -> Idle | Last checkpoint/change id and cumulative totals retained; per-report counts reset; operation metadata absent on Idle |
| Source read failure | One capture failure; no staged progress; retry succeeds |
| First/second outbox write failure | One outbox-stage failure; partial staged count/pending cursor preserved; no checkpoint before complete staging |
| Checkpoint write failure | One checkpoint failure with staged count and pending cursor; no capture success until retry commits |
| Retry after failure | One failure followed by a successful capture and idle; original failure snapshot remains inspectable; latest error metadata clears |
| Cancellation during read/stage/checkpoint | Pending I/O observes cancellation; no checkpoint or success/failure observation invented after stop |

## Adoption boundary

SQL Server CDC remains **M2 / provider-managed** in the package maturity matrix; the hosted pump owns host-managed execution. These tests strengthen the existing bounded execution proof. They do not demonstrate a live SQL Server deployment, cross-process restart, physical checkpoint durability, multi-node fencing or atomicity across the linked outbox and SQL checkpoint store. Those broader proofs stay in ENG-721/724/730/733.

Outbox staging and checkpoint persistence are separate calls. A partially staged batch or failed checkpoint can be replayed, and cancellation after staging can leave messages staged without a committed checkpoint. The tests deliberately retain repeated message IDs across retry. Applications must choose an outbox/consumer with suitable deduplication and transactional semantics; this runner does not claim exactly-once delivery or rollback of accepted outbox messages. Report counts describe observations, not unique business transactions.

Order ingestion, inventory synchronization, billing projections and audit integrations can reuse the same module/CDC/outbox seams, but each still needs domain-specific transaction, idempotency, tenant-isolation and recovery acceptance. [Framework completion plan](framework-completion-plan.md#coverage-and-package-family-routing) maps those remaining responsibilities across package families.

## Remaining provider gap

Static review on `b071e0f3` found the same nested report/rethrow/outer-report pattern in `MySqlBinlogCaptureHostedService`, `PostgresLogicalReplicationCaptureHostedService`, `OracleLogMinerCaptureHostedService` and `MongoDbChangeStreamCaptureHostedService`. ENG-754 retains **12 h** for provider-specific reproduction and fixes, including PostgreSQL abandonment/acknowledgement and MongoDB cursor cleanup/restart. The generic `Cephalon.Data` CDC pump already returns after handled failures. SQL Server's dynamic proof does not certify the four other implementations; their component guides now expose this diagnostic limitation. ENG-754 is open and unscheduled, with no maturity promotion.

## Validation and sources

Local validation on the pinned SDK 10.0.401 passes **63/63** related composition cases, including all **11 SQL Server CDC cases**. Before the runtime fix, three new cases failed because they observed two failures for one staged/checkpoint error; the other eight SQL Server cases passed. After the fix all eleven pass. Planning tests pass **8/8**, Markdown link checks **2/2**, and both live GitHub guards pass for **24 open issues / 24 Project items / five required fields**. The maturity report covers **107 packages / 107 component documents**, drift **0**, with unchanged M0=1, M1=39, M2=51, M3=7, M4=9.

Local receipts are under `artifacts/journal-validation-2026-09-27/` (`cdc-red.trx`, `cdc-green.trx`, `doc-links.trx`, `planning.xml`, `maturity.json`). Corrected-source CI is accepted below; ENG-753 is complete. Reproduction command:

```powershell
dotnet test tests/Cephalon.Tests.Composition/Cephalon.Tests.Composition.csproj -c Release --filter FullyQualifiedName~SqlServerDataCdcPackTests
```

The first implementation push `b071e0f3` passed Contract Compatibility. Release run [36324025161](https://github.com/Cephalon-Labs/CephalonEngine/actions/runs/36324025161) exposed generated-reference drift after the XML clarification, and a separate Linux WebSocket rate-limit test timed out while awaiting its first response. The reference bundle is regenerated (four changed files); bundle consistency plus Markdown links now pass **3/3** locally. The isolated WebSocket test passes **1/1** locally, which does not explain or resolve the Linux timeout. ENG-752 retains that investigation and the earlier journal benchmark failure. Corrected-source release proof is recorded below; no deadline or benchmark threshold was relaxed.

Official references consulted: [SQL Server CDC overview](https://learn.microsoft.com/sql/relational-databases/track-changes/about-change-data-capture-sql-server?view=sql-server-ver17) explains the source change/LSN model. [Microsoft guidance on testing asynchronous code](https://learn.microsoft.com/en-us/archive/msdn-magazine/2014/november/async-programming-unit-testing-asynchronous-code-three-solutions-for-better-tests) describes synchronization through controlled dependencies; the test decorator applies that approach at Cephalon's accepted-report boundary. These sources do not certify Cephalon's implementation.

The earlier journal benchmark failure on `69a20430` (519.8 us > 500 us) remains unresolved under ENG-752. The newer `05d25dfe` Windows run stopped at CDC tests before reaching benchmarks; it supplies no new journal measurement. No benchmark threshold, stable SLO, maturity or provider-support claim changes here.

## Journal runner comparison retained for ENG-752

The existing BenchmarkDotNet Markdown artifacts identify different hosted CPUs despite the same Windows label, OS build, SDK, runtime and benchmark job settings:

| Source / run | Reported CPU and JIT target | Journal mean / allocation |
| --- | --- | --- |
| `e67697ae` / [36313203784](https://github.com/Cephalon-Labs/CephalonEngine/actions/runs/36313203784) | AMD EPYC 9V45 2.60 GHz, 4 logical / 2 physical cores; x86-64-v4 | 147.7 us / 176.65 KB |
| `69a20430` / [36317329975](https://github.com/Cephalon-Labs/CephalonEngine/actions/runs/36317329975) | AMD EPYC 7763 2.44 GHz, 4 logical / 2 physical cores; x86-64-v3 | 519.8 us / 176.87 KB |
| Earlier local projection check | Intel Core i5-13500, 20 logical / 14 physical cores; x86-64-v3 | 152.4 us / 176.40 KB |

Both hosted reports use BenchmarkDotNet 0.15.8, SDK 10.0.401, runtime 10.0.12, Windows build 26100.33438, and InProcessShortRun with one launch, three warmups and three measurements. Their source/benchmark files were unchanged between those two hosted commits. Hardware/JIT target differences are an observed confounder, **not a proven cause** of the latency difference. Local numbers are not hosted-runner proof. Retain the 500 us guardrail and all failed samples; ENG-752 must compare repeated measurements on declared matching hardware/runtime conditions before drawing a code-regression or stable-SLO conclusion.

## Windows fixture failure on the corrected source

On `1d1b4640`, Windows [job 108634899161](https://github.com/Cephalon-Labs/CephalonEngine/actions/runs/36324579835/job/108634899161) passes 909/909 composition cases, then fails the MongoDB CDC hosting fixture during `InitializeAsync` before its test body (819/820 hosting cases pass). The retained diagnostics show MongoDB 7.0.18 listening at 14:12:38.172 UTC, about 1.2 seconds after process startup, while driver observations report a three-second connection timeout and then Connected/ReplicaSetGhost at elapsed 20.18 seconds. Bootstrap fails at 20.34 seconds against its existing deadline. This narrows the observed failure to fixture connection/bootstrap timing; it does not prove CPU starvation, slow database startup or a production CDC defect. The full log is retained as `artifacts/journal-validation-2026-09-27/accepted-windows.log`.

ENG-752 retains investigation of this failure, the prior Linux WebSocket receive timeout and the historical journal benchmark exceedance. Windows did not reach tooling, benchmark or full-release acceptance in this run. Do not infer a new benchmark result, raise a deadline, or report the full release gate as green from the passing SQL Server scope.

## Committed-source acceptance

Runtime implementation is `b071e0f3`; generated-reference and remaining-provider planning correction is `1d1b4640`. On `1d1b4640`, [Host Compatibility](https://github.com/Cephalon-Labs/CephalonEngine/actions/runs/36324579915) and [Contract Compatibility](https://github.com/Cephalon-Labs/CephalonEngine/actions/runs/36324579821) pass. [Release Validation](https://github.com/Cephalon-Labs/CephalonEngine/actions/runs/36324579835) passes Linux (**909 composition + 820 hosting + 383 tooling = 2,112 core tests**) and SDK 11 readiness. Both shipping OS pass **909 composition tests** including the SQL Server CDC cases, and **290 Pester tests** each. Linux tooling proves generated-reference consistency. Windows release remains **failed** at MongoDB fixture bootstrap (819/820 hosting); Windows tooling and benchmarks are not reached.

The earlier `b071e0f3` run remains failed/superseded evidence: Windows composition 909/909 and hosting 820/820 passed before reference drift failed and the job was cancelled by the corrective push; Linux composition 909/909 passed, hosting was 819/820, and the separate readiness lane detected reference drift. ENG-752 retains the latest MongoDB fixture failure, earlier WebSocket timeout, historical journal exceedance and statistical evidence. ENG-754 retains provider repairs. ENG-753 closes only the SQL Server scope, not the full release gate. No live SQL Server, exactly-once, maturity or stable-SLO promotion.
