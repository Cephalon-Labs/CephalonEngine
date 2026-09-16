# September 2026 architecture review follow-ups

Live follow-through for the [September architecture review](architecture-review-2026-09.md), aligned with [engineering qualities](engineering-standards.md), [long-range direction](long-range-direction.md) and [SRE posture](sre-posture.md).

| Follow-up | Status | Planning anchor | Required evidence |
| --- | --- | --- | --- |
| Remove timeout timer races and isolate Showcase exporter waits | in-progress | ENG-745, 8 h within ENG-729 | Controlled timeout/override/cancellation assertions, retained collector integration coverage and Windows/Linux release receipts |
| Investigate `engine.validate-release.wall-time` overrun | investigate | ENG-729 / ENG-746 | Windows `c4f8d288` measured 31 m 22 s against 30 minutes; repeat workload/runner-aware timing after the fixture repair before renewing the SLO |
| Complete load/recovery and telemetry-cost evidence | planned | ENG-746, 24 h within ENG-729 | Reproducible workload baselines, failure drills, cardinality budgets and CI flake-rate assessment |

[Implementation and receipts](sre-validation-2026-09.md) distinguish passing compatibility checkpoints from the later Windows test failure. No test is skipped and no benchmark/SLO threshold is relaxed. These evidence repairs do not change package maturity or production runtime ownership.
