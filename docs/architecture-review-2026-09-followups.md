# September 2026 architecture review follow-ups

Live follow-through for the [September architecture review](architecture-review-2026-09.md), aligned with [engineering qualities](engineering-standards.md), [long-range direction](long-range-direction.md) and [SRE posture](sre-posture.md).

| Follow-up | Status | Planning anchor | Required evidence |
| --- | --- | --- | --- |
| Remove timeout timer races and isolate Showcase exporter waits | shipped | ENG-745, 8 h within ENG-729 | Controlled timeout/override/cancellation assertions, retained collector integration coverage and Windows/Linux release receipts |
| Preserve provider CDC failure and bootstrap evidence | shipped | ENG-747, 10 h within revised ENG-729 50 h | MongoDB owned bootstrap lifecycle and deadline/caller-cancellation diagnostics, MySQL/PostgreSQL one-shot failure observation, real MongoDB CDC and full CI proof; historical startup-delay cause remains unproven |
| Add host-clock publication scheduling and deterministic due-time acceptance | shipped | ENG-748, 8 h within ENG-729 50 h | System default/host override, due boundaries, rearming, disposal, failure timestamps and full CI |
| Investigate `engine.validate-release.wall-time` overrun | investigate | ENG-729 / ENG-746 | Windows `c4f8d288` measured 31 m 22 s against 30 minutes; repeat workload/runner-aware timing after the fixture repair before renewing the SLO |
| Project journal cursor reads and retain release timing | shipped | ENG-749, 8 h within ENG-746 | Relational selector checks, unchanged benchmark gate, success/failure/reduced run receipts and CI artifacts |
| Complete load/recovery and telemetry-cost evidence | planned | ENG-750, 16 h within ENG-746 | Reproducible workload baselines, failure drills, cardinality budgets and CI flake-rate assessment |

ENG-746 is now active with 8/16 h children, preserving its 24 h rollup. The later docs-only `340c408e` release passed tests but failed the Windows journal mean guardrail. [Current investigation and evidence](sre-evidence-2026-09-27.md) retain that failure separately from the earlier accepted run below.

[Implementation and receipts](sre-validation-2026-09.md) distinguish passing compatibility checkpoints from the later Windows test failure. No test is skipped and no benchmark/SLO threshold is relaxed. These evidence repairs do not change package maturity or production runtime ownership.

[Release CI](https://github.com/Cephalon-Labs/CephalonEngine/actions/runs/36298293286) passed Windows/Linux shipping (2,101 core tests plus 263 Pester each) and SDK 11 readiness (2,092 selected net10.0 tests) on `201e92a8`. All 41 Windows benchmark guardrails passed. [Host/deployment](https://github.com/Cephalon-Labs/CephalonEngine/actions/runs/36298293252) and [contract compatibility](https://github.com/Cephalon-Labs/CephalonEngine/actions/runs/36298293274) passed both OS. Windows release-step duration was **25 m 00 s**, from GitHub step timestamps. This single run does not renew a stable SLO or identify the historical MongoDB startup cause. [Detailed evidence](sre-validation-2026-09.md).

ENG-749 is accepted on `e67697ae`; all three CI workflows passed and both OS timing artifacts were verified. ENG-750 retains 16 h of workload/SLO/telemetry evidence. [Receipts and limits](sre-evidence-2026-09-27.md#committed-source-acceptance).
