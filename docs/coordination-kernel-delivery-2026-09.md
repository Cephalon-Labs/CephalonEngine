# Bounded coordination delivery — September 16, 2026

Delivery scope: ENG-719 (16 h estimate) and ENG-720 (24 h estimate), Phase 14 / Sprint 16. These are retained engineering estimates, not measured elapsed hours. [Decision and adoption](architecture/coordination-kernel.md) records the four-family inventory, provider boundaries and transition contract.

## Implemented behavior

- Immutable host-agnostic operation intent, deterministic expiring plans, canonical fingerprints, effect outcomes, copied attempt history and results.
- Opt-in instance-local execution: concurrent duplicate reservation, conflicting-intent rejection, tenant key separation, bounded non-evicting capacity, retry only after a confirmed no-effect result, deterministic capped jitter, injectable clock/timers and total deadline.
- Explicit `InDoubt` after uncertain effects; no blind retry after an exception, timeout or cancellation during an outstanding effect. A late completion cannot rewrite the retained uncertainty.
- Real `coordination` schema `1.0` introspection through the existing extension envelope, deterministic order and redacted aggregate output. Existing family services and typed snapshot fields remain intact.

## Validation record

The following checks were run against this change; full release status remains separate below. Local receipts are under `artifacts/coordination-2026-09/` and are not source-controlled release artifacts.

| Check | Evidence |
| --- | --- |
| Reconciliation transition coverage | 26 new cases passed within the composition suite, including atomic provider preconditions and 200 generated intent/replay/conflict cases. |
| Full composition suite | 885 passed, zero failures/skips; includes 26 reconciliation cases and 859 existing composition/benchmark cases. A stale guardrail count assertion was updated from 40 to 41 and the complete suite rerun. |
| Public API | 77 additive signatures: Abstractions 65, Engine 12. Repository readback: 104 baselines, 3 pending packages, 282 additions, zero removals. This change leaves shipped baseline files unchanged; historical analyzer drift is described below. |
| Benchmark | BenchmarkDotNet in-process ShortRun: 686.6 ns mean, 2.67 ns standard deviation, 896 B allocated; passes initial 10,000 ns / 4,096 B smoke guardrail. Windows 11 x64, i5-13500, SDK 10.0.303 / runtime 10.0.11, 3 warmups + 3 measurements. Local smoke only, no comparative performance or provider-throughput claim. |
| Script contracts | 44 Pester tests passed: scorecard, canonical release filters and planning parser. |
| Documentation | 20 focused guide/contract tests and 3 generated-reference link/manifest/browser tests passed; hand-authored repository link/anchor scan pending. Regenerated the reference bundle; 9 files changed. Edited-guide local-link target check: 456 targets, none missing. |
| Maturity | Passed: 107 packages, M0=1 / M1=39 / M2=51 / M3=7 / M4=9, drift=0. |
| GitHub planning | Final live uniqueness and required-field guards run after issue publication. |

## Release limitations and follow-up ownership

The upstream [Release Validation run 35081865397](https://github.com/Cephalon-Labs/CephalonEngine/actions/runs/35081865397) on preceding commit `11488f13` failed before this implementation. Windows/Linux locked restore selected ILLink 10.0.12 while three lock files declare 10.0.11; the .NET 11 lane failed `MapCephalonSchedulesCoreInProcessEventPublicationWithoutWolverine`. They are tracked under [ENG-731](https://github.com/Cephalon-Labs/CephalonEngine/issues/1424), not counted as passing release evidence. This slice does not upgrade the shipping SDK or weaken locked restore.

An explicitly injected public-API analyzer on Abstractions also exposed 121 existing non-coordination baseline diagnostics (118 undeclared signatures and 3 stale declarations); the normal project-reference analyzer wiring did not report it. ENG-731 owns restoring authoritative analyzer wiring and reconciling those existing declarations. A fresh explicit analyzer run confirms zero diagnostics for the new coordination namespace, with the same 121 historical diagnostics remaining. The 65 new coordination declarations are captured from analyzer output; zero removal entries in the delta report is a baseline-file check, not proof that historical drift is absent.

ENG-721 (32 h) and ENG-722 (24 h) remain necessary for durable provider reservation, fenced writes, crash/restart recovery, shared authorization/approval and durable audit. Family pilots ENG-723–725 and maturity dossiers ENG-726 remain open. The new executor intentionally does not retain state across process/provider reconstruction, export tenant-scoped actions, or claim exactly-once remote effects. No M0–M4 package declarations change; full framework completion and GA remain open gates.

The historical API diagnostics are concentrated in Eventing remediation contracts, two behavior-builder declarations and the module discovery registry. The three stale registry declarations concern `Register`, `GetDescriptors` and `TryGetDescriptors`; reconcile static/signature truth against a released consumer baseline before editing them. Merely accepting all analyzer output as a new shipped baseline would not prove binary compatibility.

## Tracking

[ENG-719 / #1412](https://github.com/Cephalon-Labs/CephalonEngine/issues/1412), [ENG-720 / #1413](https://github.com/Cephalon-Labs/CephalonEngine/issues/1413), [ENG-714 parent / #1405](https://github.com/Cephalon-Labs/CephalonEngine/issues/1405), [Phase 14 milestone](https://github.com/Cephalon-Labs/CephalonEngine/milestone/15), and [Project 2](https://github.com/orgs/Cephalon-Labs/projects/2) carry matching scope, estimates, iteration and validation state. The delivery commit is recorded in issue history after push. The parent stays open until all four children meet acceptance.
