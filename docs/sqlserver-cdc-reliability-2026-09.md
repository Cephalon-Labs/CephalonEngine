# SQL Server CDC reliability evidence

ENG-753 / [#1446](https://github.com/Cephalon-Labs/CephalonEngine/issues/1446), September 27, 2026. Owner: Cephalon-Neza. Estimate: **6 engineering hours** including implementation, tests, docs and tracking. This is additional observed scope under ENG-729, whose non-additive rollup increases **50 -> 56 h**. September scope becomes **572 h** and Phase 15 **244 h**. ENG-752 retains its separate **13 h** workload, benchmark and statistical evidence scope.

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

## Validation and sources

Local validation on the pinned SDK 10.0.401 passes **63/63** related composition cases, including all **11 SQL Server CDC cases**. Before the runtime fix, three new cases failed because they observed two failures for one staged/checkpoint error; the other eight SQL Server cases passed. After the fix all eleven pass. Planning tests pass **8/8**, Markdown link checks **2/2**, and both live GitHub guards pass for **24 open issues / 24 Project items / five required fields**. The maturity report covers **107 packages / 107 component documents**, drift **0**, with unchanged M0=1, M1=39, M2=51, M3=7, M4=9.

Local receipts are under `artifacts/journal-validation-2026-09-27/` (`cdc-red.trx`, `cdc-green.trx`, `doc-links.trx`, `planning.xml`, `maturity.json`). Committed-source CI acceptance is pending; ENG-753 remains open. Reproduction command:

```powershell
dotnet test tests/Cephalon.Tests.Composition/Cephalon.Tests.Composition.csproj -c Release --filter FullyQualifiedName~SqlServerDataCdcPackTests
```

Official references consulted: [SQL Server CDC overview](https://learn.microsoft.com/sql/relational-databases/track-changes/about-change-data-capture-sql-server?view=sql-server-ver17) explains the source change/LSN model. [Microsoft guidance on testing asynchronous code](https://learn.microsoft.com/en-us/archive/msdn-magazine/2014/november/async-programming-unit-testing-asynchronous-code-three-solutions-for-better-tests) describes synchronization through controlled dependencies; the test decorator applies that approach at Cephalon's accepted-report boundary. These sources do not certify Cephalon's implementation.

The earlier journal benchmark failure on `69a20430` (519.8 us > 500 us) remains unresolved under ENG-752. The newer `05d25dfe` Windows run stopped at CDC tests before reaching benchmarks; it supplies no new journal measurement. No benchmark threshold, stable SLO, maturity or provider-support claim changes here.
