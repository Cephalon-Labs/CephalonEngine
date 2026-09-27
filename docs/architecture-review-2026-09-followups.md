# September 2026 architecture review follow-ups

September 27 CDC continuation: ENG-753 (6 h, Sprint 16) has local 63/63 proof and awaits committed-source CI. ENG-754 (12 h, Later) tracks the same statically identified failure-report pattern in four other native providers. ENG-729 is **68 h** (37 h delivered / 31 h remaining); ENG-752 retains 13 h through ENG-750/746. September scope is **584 h**; **19 unfinished leaves / 463 h**, plus ENG-532 **1 h** = **464 h**. Phase 15 is **256 h total / 175 h remaining**, plus ENG-532. [CDC evidence and remaining gaps](sqlserver-cdc-reliability-2026-09.md). No SLO or maturity promotion.

Earlier September 27 collector acceptance: ENG-751 (3 h) is complete on `69a20430`. ENG-752 is active in Sprint 16 with 13 h for workload/recovery/telemetry, journal benchmark investigation and test-level/statistical evidence. Non-additive parents remain open: ENG-750 16 h (3 h delivered), ENG-746 24 h (11 h delivered), ENG-729 50 h (37 h delivered). September scope remains **566 h**; **17 unfinished leaves / 445 h**, plus ENG-532 **1 h** = **446 h**. Phase 15 remains 238 h total / 157 h remaining plus ENG-532. [Collector contract and accepted evidence](ci-flake-collector.md) record collector acceptance; Windows release remains failed at the journal benchmark (519.8 us > 500 us), tracked by active ENG-752. No SLO or maturity promotion.

Live follow-through for the [September architecture review](architecture-review-2026-09.md), aligned with [engineering qualities](engineering-standards.md), [long-range direction](long-range-direction.md) and [SRE posture](sre-posture.md).

| Follow-up | Status | Planning anchor | Required evidence |
| --- | --- | --- | --- |
| Remove timeout timer races and isolate Showcase exporter waits | shipped | ENG-745, 8 h within ENG-729 | Controlled timeout/override/cancellation assertions, retained collector integration coverage and Windows/Linux release receipts |
| Preserve provider CDC failure and bootstrap evidence | shipped | ENG-747, 10 h within revised ENG-729 50 h | MongoDB owned bootstrap lifecycle and deadline/caller-cancellation diagnostics, MySQL/PostgreSQL one-shot failure observation, real MongoDB CDC and full CI proof; historical startup-delay cause remains unproven |
| Add host-clock publication scheduling and deterministic due-time acceptance | shipped | ENG-748, 8 h within ENG-729 50 h | System default/host override, due boundaries, rearming, disposal, failure timestamps and full CI |
| Investigate `engine.validate-release.wall-time` overrun | investigate | ENG-729 / ENG-746 | Windows `c4f8d288` measured 31 m 22 s against 30 minutes; repeat workload/runner-aware timing after the fixture repair before renewing the SLO |
| Project journal cursor reads and retain release timing | shipped | ENG-749, 8 h within ENG-746 | Relational selector checks, unchanged benchmark gate, success/failure/reduced run receipts and CI artifacts |
| Correct CI flake collection and promotion semantics | shipped | ENG-751, 3 h within ENG-750 | Paginated and locale-safe queries, eligible outcomes, attribution/coverage blockers and no automatic SLO promotion |
| Complete load/recovery and telemetry-cost evidence | in-progress / benchmark regressed | ENG-752, 13 h within ENG-750 | Reproducible workload baselines, failure drills, cardinality budgets and test-level/statistical assessment |

ENG-746 is now active with 8/16 h children, preserving its 24 h rollup. The later docs-only `340c408e` release passed tests but failed the Windows journal mean guardrail. [Current investigation and evidence](sre-evidence-2026-09-27.md) retain that failure separately from the earlier accepted run below.

[Implementation and receipts](sre-validation-2026-09.md) distinguish passing compatibility checkpoints from the later Windows test failure. No test is skipped and no benchmark/SLO threshold is relaxed. These evidence repairs do not change package maturity or production runtime ownership.

[Release CI](https://github.com/Cephalon-Labs/CephalonEngine/actions/runs/36298293286) passed Windows/Linux shipping (2,101 core tests plus 263 Pester each) and SDK 11 readiness (2,092 selected net10.0 tests) on `201e92a8`. All 41 Windows benchmark guardrails passed. [Host/deployment](https://github.com/Cephalon-Labs/CephalonEngine/actions/runs/36298293252) and [contract compatibility](https://github.com/Cephalon-Labs/CephalonEngine/actions/runs/36298293274) passed both OS. Windows release-step duration was **25 m 00 s**, from GitHub step timestamps. This single run does not renew a stable SLO or identify the historical MongoDB startup cause. [Detailed evidence](sre-validation-2026-09.md).

ENG-749 is accepted on `e67697ae`; all three CI workflows passed and both OS timing artifacts were verified. ENG-750 now has 3 h delivered under ENG-751 and 13 h remaining under ENG-752. [Receipts and limits](sre-evidence-2026-09-27.md#committed-source-acceptance).
