# September 2026 architecture review follow-ups

Live follow-through for the [September architecture review](architecture-review-2026-09.md), aligned with [engineering qualities](engineering-standards.md), [long-range direction](long-range-direction.md) and [SRE posture](sre-posture.md).

| Follow-up | Status | Planning anchor | Required evidence |
| --- | --- | --- | --- |
| Remove timeout timer races and isolate Showcase exporter waits | shipped | ENG-745, 8 h within ENG-729 | Controlled timeout/override/cancellation assertions, retained collector integration coverage and Windows/Linux release receipts |
| Preserve provider CDC failure and bootstrap evidence | shipped | ENG-747, 10 h within revised ENG-729 50 h | MongoDB owned bootstrap lifecycle and deadline/caller-cancellation diagnostics, MySQL/PostgreSQL one-shot failure observation, real MongoDB CDC and full CI proof; historical startup-delay cause remains unproven |
| Add host-clock publication scheduling and deterministic due-time acceptance | shipped | ENG-748, 8 h within ENG-729 50 h | System default/host override, due boundaries, rearming, disposal, failure timestamps and full CI |
| Investigate `engine.validate-release.wall-time` overrun | investigate | ENG-729 / ENG-746 | Windows `c4f8d288` measured 31 m 22 s against 30 minutes; repeat workload/runner-aware timing after the fixture repair before renewing the SLO |
| Complete load/recovery and telemetry-cost evidence | planned | ENG-746, 24 h within ENG-729 | Reproducible workload baselines, failure drills, cardinality budgets and CI flake-rate assessment |

[Implementation and receipts](sre-validation-2026-09.md) distinguish passing compatibility checkpoints from the later Windows test failure. No test is skipped and no benchmark/SLO threshold is relaxed. These evidence repairs do not change package maturity or production runtime ownership.

[Release CI](https://github.com/Cephalon-Labs/CephalonEngine/actions/runs/36298293286) passed Windows/Linux shipping (2,101 core tests plus 263 Pester each) and SDK 11 readiness (2,092 selected net10.0 tests) on `201e92a8`. All 41 Windows benchmark guardrails passed. [Host/deployment](https://github.com/Cephalon-Labs/CephalonEngine/actions/runs/36298293252) and [contract compatibility](https://github.com/Cephalon-Labs/CephalonEngine/actions/runs/36298293274) passed both OS. Windows release-step duration was **25 m 00 s**, from GitHub step timestamps. This single run does not renew a stable SLO or identify the historical MongoDB startup cause. [Detailed evidence](sre-validation-2026-09.md).
