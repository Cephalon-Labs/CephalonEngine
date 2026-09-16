# Cephalon framework completion plan

Planning date: **September 16, 2026**. Owner: **Cephalon-Neza**. Planning delivery: **ENG-718**. [GitHub Project](https://github.com/orgs/Cephalon-Labs/projects/2). [Research and sources](framework-research-2026-09.md).

## Objective and verified baseline

Make Cephalon a dependable modular engine with clear runtime ownership, a small host-agnostic core, reusable primitives, usable adoption paths and defensible support claims. Completeness is evaluated for a declared release/workload scope across all twelve [engineering qualities](engineering-standards.md), not against an unbounded promise to support every technology or be universally best.

The baseline was checked at commit `398b2b3a`: local master and origin/master had no divergence; the maturity report passed for **107** source projects and **107** component documents with **M0=1, M1=39, M2=51, M3=7, M4=9**, drift **0**. This proves declaration consistency, not a fresh behavioral audit of every package. The complete package inventory remains in the [conformance matrix](conformance-matrix.md) and generated `artifacts/surface-maturity-report/surface-maturity-report.json`.

Existing open work is retained: ENG-714 / #1405 (kernel), ENG-715 / #1406 (pilots), ENG-532 / #1180 (signed-release dry-run blocker). No package maturity changes in this planning delivery. The observed SDK is **10.0.303**; the shipping floor remains **net10.0**. Official RC1/servicing findings are recorded in [.NET readiness](dotnet11-readiness.md).

## M0–M4 acceptance and maintenance

| Level | Meaning / required evidence | Next gate | Intentionally valid stopping point |
| --- | --- | --- | --- |
| M0 | Named vocabulary/taxonomy, schema validation, owner and explicit non-execution boundary. | M1 requires deterministic catalog/selection, invalid-input tests, documentation and introspection. | Taxonomy or implementation helper; do not manufacture an executor. |
| M1 | Truthful configuration/catalog/runtime projection with stable identifiers, ordering and serialization. | M2 requires an owned execution/provisioning path, cancellation, errors and observed state. | Application-managed/provider-described capabilities can remain here. |
| M2 | A real owned path with failure semantics, resource bounds, security boundary and provider support limits. | M3 requires reconciliation/remediation/recovery with tested effects, duplicate handling, restart and ownership safety. | Simple adapters/probes need not acquire autonomous remediation. |
| M3 | A bounded operator loop with plan/apply authorization, journal, retry limits, drift and recovery proof. | M4 requires independent adoption, versioned artifacts, upgrade/recovery, runbooks and supported-scope evidence. | A proven operator primitive may stay M3 until an adopter exists. |
| M4 | A declared surface is adopted and operable outside the repository with reproducible evidence and a maintainer. | Renew proof for material changes; GA additionally requires the release scorecard gates. | Tooling/contracts use role-appropriate adoption proof; do not invent an operator loop for a CLI or contract package. |

M0–M4 measure capability maturity, while ownership, security, performance, compatibility and release readiness are separate dimensions. Existing tooling M4 declarations remain; the nine current M4 rows get evidence renewal in ENG-726/734, not automatic downgrades or automatic reaffirmation. An unsupported target remains `not-claimed`. Failed or stale proof blocks a new promotion/release claim and opens a remediation issue; changing a current badge requires an explicit reviewed decision.

Each dossier records package/surface, ownership, current/target maturity, source commit, artifact digest, test command and run URL, runtime/provider versions, workload, outcome, limitations, accountable reviewer and review/expiry trigger. Revalidate before release and after relevant code/provider/contract changes; review quarterly even if unchanged. Redact secrets and personal data. Retain prior decisions so an auditor can reconstruct both promotion and withdrawal.

## Coverage and package-family routing

| Domain | Existing authority / scope | Planned completion evidence |
| --- | --- | --- |
| Composition, lifecycle, modules, configuration and localization | Abstractions, Engine, manifest v2; deterministic discovery and ExtensionSections | Contract/transition invariants, compatibility and source-generated/static path where applicable: ENG-719/720/731. |
| Hosts and transports | ASP.NET Core, Worker, REST/JSON-RPC/gRPC/GraphQL/SSE/WebSocket adapters | Correct startup/shutdown, auth/resilience boundaries, streaming cancellation, OpenAPI scope and conformance: ENG-727/731/733. |
| Behaviors, patterns, scheduling and workflow | Behaviors and pattern companions | Reuse coordination instead of a parallel workflow engine; bounded execution/recovery and migration: ENG-720/721/724. |
| Data, cache, CDC, EventSourcing and messaging | Data/Eventing/provider companions | Atomicity boundaries, schema evolution, ordering/replay, durable recovery and RPO/RTO: ENG-721/724/730/733. |
| Identity, tenancy, governance and delivery | Identity, MultiTenancy/Governance and provider callbacks | Tenant isolation, approvals, revocation, verified callbacks and privacy lifecycle: ENG-722/725/727/730. |
| Dependency health and observability | Observability family; eighteen provider probes stay M2 unless proven otherwise | One bounded re-probe pilot, telemetry stability/cardinality, meaningful SLOs: ENG-723/729/733. |
| Edge, infrastructure and external execution | Edge/provider-specific controllers | Owned drift loop, stale observation, partition/rejoin, fencing and escalation: ENG-725/733. |
| Agentics, retrieval and AI integrations | Agentics/Retrieval and optional providers | Least privilege, injection resistance, tenant-scoped retrieval, evaluations, budgets and fallback: ENG-735. |
| Packages, API contracts and supply chain | Package trust/policy, compatibility, release tooling | Provenance verification, SBOM/licenses, patch response and upgrade path: ENG-727/728/731. |
| CLI, scaffolding, templates, docs and samples | CLI/Scaffolding/TemplatePack/ReferenceDocs | Independent clean-environment journeys, XML completeness, localized/readable errors and accessible docs: ENG-732/734. |
| Platform, OS/architecture and deployment modes | Compatibility and deployment-mode manifests | Only validated package/RID claims; .NET RC assessment, AOT/static alternatives and clear exclusions: ENG-731. |
| Maintenance, cost and ecosystem longevity | Planning governance and long-range direction | Named maintainers, lifecycle policy, affordable provider matrix, reversible experiments and GA decision: ENG-736/737/738. |

All 107 current packages are covered by an existing declaration row and the routing above. This table is an action map, not a second package inventory or a claim to have validated each provider today. New packages must enter the conformance/maturity inventory and name their support owner before release.

## Delivery sequence and effort

Estimates are **engineering hours**, matching the existing GitHub Estimate field (`h`); **8 h = 1 person-day**. They include implementation, review, focused tests, documentation and planning updates. They exclude external waiting, infrastructure/license costs, unrelated defects and repeated future maintenance. These are initial planning estimates, not hours already spent or a delivery promise.

| Phase / gate | Scope | Base hours | Planning range | Dependency |
| --- | --- | ---: | ---: | --- |
| Phase 14 / Gate 2 | ENG-714 parent: ENG-719–722 | 96 | 72–144 | Gate 1 foundation shipped |
| Phase 14 / Gate 3 | ENG-715 parent: ENG-723–726 | 120 | 90–180 | Shared kernel |
| Phase 15 | ENG-727–733, release completeness | 208 | 156–312 | Security/kernel and support-matrix dependencies per task |
| Phase 16 | ENG-734–738, adoption and sustainable evolution | 112 | 84–168 | Relevant release proofs and an external adopter |
| **Original new/re-estimated leaf baseline** | **20 tasks, excluding parent rollups** | **536** | **402–804** | External blockers can delay without consuming engineering hours |

ENG-532 retains its separate **1 h** next-action estimate, so the original tracked baseline is **537 h** (**67.125 person-days**), with a **403–805 h** indicative range. ENG-718 is a separate **8 h planning estimate**, not part of remaining implementation work. ENG-714 changes **32 → 96 h** and ENG-715 **64 → 120 h** because cross-process durability/security proof and three real provider loops were not sufficiently decomposed. Parent estimates are rollups only; never sum parents and children together.

Ranges are 0.75–1.5 times base, rounded only in display, and are not statistical confidence intervals. Kernel estimates have medium confidence; provider/security/adoption work has low confidence until ADRs and environment access exist. Re-estimate at ENG-719 and after the first pilot. Example capacity scenarios: at 24 focused hours per engineer-week, 537 h is about 22.4 engineer-weeks before external waiting; adding people does not eliminate the kernel/pilot/adoption critical path.

The Phase 14 September 29 milestone date is withdrawn because no capacity-backed commitment supports it. Phases 14–16 are **gate milestones with no due date**. Work ordering is committed; calendar dates are not. Current planning delivery uses the existing September 16–29 iteration (`Sprint 16`). Future tasks use `Later / not scheduled yet`; its historical Project date is a legacy placeholder, not a schedule. ENG-737 owns evidence-based calendar reconciliation. Do not rewrite completed sprint history or auto-append dates years into the future.

Recommended next four capacity windows, assigned dates only after capacity is confirmed:

1. Inventory/ADR, servicing assessment and release blocker follow-up: ENG-719, start ENG-731, ENG-532.
2. Bounded kernel and transactional proof: ENG-720/721; start security mapping.
3. Approval boundary and first health pilot: ENG-722/723, with ENG-727/729 support.
4. Eventing/governance pilots and promotion review: ENG-724/725/726; then external adoption gates.

Keep at most two implementation slices active per primary engineer and reserve explicit time for reviews/provider incidents. Security/release/DX work may run alongside kernel work where its dependency permits; this is scheduling guidance, not authorization for unbounded agent delegation.

## Task map

The [backlog](engine-backlog.md) contains each task's scope, acceptance tests, dependencies, ownership and validation fields. Native GitHub parent/sub-issue relationships attach ENG-719–722 to ENG-714 and ENG-723–726 to ENG-715.

| ID | Task | Phase | Hours | Depends on |
| --- | --- | ---: | ---: | --- |
| [ENG-719](https://github.com/Cephalon-Labs/CephalonEngine/issues/1412) | Coordination ADR and existing-family contract inventory | 14 | 16 | ENG-713 |
| [ENG-720](https://github.com/Cephalon-Labs/CephalonEngine/issues/1413) | Deterministic reconciliation planning and bounded execution | 14 | 24 | [ENG-719](https://github.com/Cephalon-Labs/CephalonEngine/issues/1412) |
| [ENG-721](https://github.com/Cephalon-Labs/CephalonEngine/issues/1414) | Durable journal idempotency and lease-fencing provider proof | 14 | 32 | ENG-719, ENG-720 |
| [ENG-722](https://github.com/Cephalon-Labs/CephalonEngine/issues/1415) | Operator authorization approvals and redacted audit boundary | 14 | 24 | ENG-720, ENG-721 |
| [ENG-723](https://github.com/Cephalon-Labs/CephalonEngine/issues/1416) | Dependency-health freshness and bounded reprobe M3 pilot | 14 | 24 | [ENG-714](https://github.com/Cephalon-Labs/CephalonEngine/issues/1405) |
| [ENG-724](https://github.com/Cephalon-Labs/CephalonEngine/issues/1417) | Eventing or CDC durable recovery M3 integration pilot | 14 | 40 | [ENG-714](https://github.com/Cephalon-Labs/CephalonEngine/issues/1405) |
| [ENG-725](https://github.com/Cephalon-Labs/CephalonEngine/issues/1418) | Governance or Edge approval-aware drift M3 pilot | 14 | 40 | [ENG-714](https://github.com/Cephalon-Labs/CephalonEngine/issues/1405) |
| [ENG-726](https://github.com/Cephalon-Labs/CephalonEngine/issues/1419) | Package-specific promotion dossiers and evidence expiry | 14 | 16 | ENG-723, ENG-724, ENG-725 |
| [ENG-727](https://github.com/Cephalon-Labs/CephalonEngine/issues/1420) | Security and tenant-isolation verification baseline | 15 | 32 | [ENG-722](https://github.com/Cephalon-Labs/CephalonEngine/issues/1415) |
| [ENG-728](https://github.com/Cephalon-Labs/CephalonEngine/issues/1421) | Verifiable package provenance and release recovery | 15 | 24 | ENG-532 for hosted proof; local policy work can start independently |
| [ENG-729](https://github.com/Cephalon-Labs/CephalonEngine/issues/1422) | SLO load resilience and telemetry-cost evidence | 15 | 32 | ENG-720; ENG-723 through ENG-725 for pilot load scenarios |
| [ENG-730](https://github.com/Cephalon-Labs/CephalonEngine/issues/1423) | Data evolution disaster recovery and privacy proofs | 15 | 40 | ENG-721, ENG-727 |
| [ENG-731](https://github.com/Cephalon-Labs/CephalonEngine/issues/1424) | Framework API and deployment compatibility matrix | 15 | 32 | ENG-719; independent .NET assessment may start immediately |
| [ENG-732](https://github.com/Cephalon-Labs/CephalonEngine/issues/1425) | External developer journey documentation and accessibility | 15 | 24 | [ENG-731](https://github.com/Cephalon-Labs/CephalonEngine/issues/1424) |
| [ENG-733](https://github.com/Cephalon-Labs/CephalonEngine/issues/1426) | Provider and host conformance support tiers | 15 | 24 | [ENG-731](https://github.com/Cephalon-Labs/CephalonEngine/issues/1424) |
| [ENG-734](https://github.com/Cephalon-Labs/CephalonEngine/issues/1427) | Independent published-package M4 adoption and upgrade | 16 | 40 | ENG-726, ENG-728, ENG-730, ENG-731, ENG-732, ENG-733 |
| [ENG-735](https://github.com/Cephalon-Labs/CephalonEngine/issues/1428) | Agentics and retrieval trust evaluation and budgets | 16 | 24 | ENG-722, ENG-727 |
| [ENG-736](https://github.com/Cephalon-Labs/CephalonEngine/issues/1429) | Time-boxed interoperability and future-runtime experiments | 16 | 16 | ENG-719, ENG-731 |
| [ENG-737](https://github.com/Cephalon-Labs/CephalonEngine/issues/1430) | Support lifecycle stewardship and planning sustainability | 16 | 16 | ENG-718; can proceed independently of runtime pilots |
| [ENG-738](https://github.com/Cephalon-Labs/CephalonEngine/issues/1431) | Release-scope GA decision and maintenance handoff | 16 | 16 | ENG-727 through ENG-734, ENG-737; ENG-735/ENG-736 only if selected for release |

## Cross-cutting exit rules

- Reuse current primitive/provider seams first. No provider, cloud, transport, model vendor or business-domain dependency enters core just to satisfy a roadmap checkbox.
- Every runtime slice proves success and failure, including cancellation, resource bounds and relevant duplicate/restart/race/tenant scenarios. Live-provider tests identify environment/version; skipped tests cannot pass an integration gate.
- Public XML, API baselines, package/readme metadata, runtime contracts, templates, operator docs and compatibility move together. Source-only introspection changes cannot stand in for effects or external adoption.
- Releases run the existing release validation and scoped security/performance/provider/adoption evidence. Missing artifact or unresolved hard blocker fails the decision. This planning pass does not publish packages or change the runtime.
- Best-in-class claims require a named comparator, identical workload/hardware, reproducible method and published limitations. Otherwise state measured improvement against Cephalon's baseline.

## Current and future horizons

Near term (3–5 years): release scope, compatibility, security response, adoption and operating cost. Mid term (5–10): reusable coordination, provider interchangeability and migration seams. Far term (10–20): stable schemas, language-neutral interchange and replaceable hosting. Very far term (20–30+): reconstructable decisions, stewardship and reversible dependencies. These extend [long-range direction](long-range-direction.md); exact technologies are experiments, not predictions.

Monthly: refresh security/SDK sources, blockers, estimates and stale evidence. Quarterly: review provider support, twelve quality dimensions, adoption outcomes and two bounded technology experiments. Annually: reassess maintainership, retirement and compatibility windows. Review sooner after a relevant incident or upstream breaking change. ENG-737 records the process; this document does not create a scheduled automation.

## Planning delivery validation

ENG-718 completes when research is linked, documents agree, the parser honors phase/iteration/shipped status, docs/maturity/planning checks pass, Project fields and issue relationships match the plan, milestone dates are truthful, and the commit is pushed and referenced from the issues. ENG-719/720 now deliver the [bounded coordination slice](coordination-kernel-delivery-2026-09.md). ENG-721–738 remain open, as do the parent epics and release/adoption gates.

## Execution update — September 16, 2026

ENG-719 (16 h) and ENG-720 (24 h) deliver the inventory/decision and bounded instance-local kernel in Sprint 16; [delivery evidence](coordination-kernel-delivery-2026-09.md) separates actual validation from release gaps. Their estimates remain on completed cards for historical truth. ENG-714 retains its 96 h original rollup, with 40 h of estimated scope delivered and 56 h remaining in ENG-721/722. No double-counting of parent cards.

Remaining baseline after this slice: 18 new tasks / **496 h**, plus ENG-532's 1 h next action = **497 h** (**62.125 person-days**), indicative range **373–745 h**. This is estimated unfinished scope, not a delivery date or actual effort spent. Phases 15/16 remain unchanged.

ENG-731's existing 32 h scope now explicitly includes the observed SDK/ILLink locked-restore mismatch, .NET 11 scheduling test failure and pre-existing Abstractions analyzer/baseline drift. Initial breakdown: servicing/lock reproducibility 8 h; analyzer/baseline repair 8 h; readiness scheduling proof 4 h; remaining consumer/deployment matrix 12 h. ENG-731 is now active in Sprint 16, decomposed into ENG-739 (8 h), ENG-740 (8 h), ENG-741 (4 h), and ENG-742 (12 h). The first three children implement the repair; the remaining matrix stays unscheduled. Discovery alone does not pass its Test or Benchmark fields. Re-estimate if the matrix or baseline repair exceeds these bounds.

## Compatibility repair checkpoint — September 16, 2026

ENG-739/740/741 complete the SDK/lock, analyzer/API baseline and scheduling-test repairs (8 + 8 + 4 = **20 h** of estimated scope) in Sprint 16. [Evidence](compatibility-repair-2026-09.md) records local validation and leaves broader release support unclaimed. ENG-731 keeps its **32 h** original non-additive rollup; ENG-742 holds the **12 h** remaining consumer/OS/deployment matrix.

At the SDK repair checkpoint, unfinished leaf scope was **18 September tasks / 476 h**, plus ENG-532's **1 h** next action = **477 h (59.625 person-days)**. Completed estimates are retained for historical accounting, not reported as time spent. Phase 15 then had 188 h remaining of its original 208 h, plus the independent ENG-532 blocker. Phase 14/16 scope and all maturity declarations are unchanged. No capacity-backed milestone due dates are introduced.

## Contract consumer validation — September 16, 2026

ENG-742 is active in Sprint 16 and decomposes its existing 12 h into ENG-743 (8 h, selected cross-version contract consumers) and ENG-744 (4 h, remaining host/snapshot/generated/deployment matrix). The original ENG-731 32 h rollup is unchanged. ENG-743 completed after both seven-scenario CI jobs passed on [implementation `5f67f61c`](https://github.com/Cephalon-Labs/CephalonEngine/commit/5f67f61c9519a553ad1faf0ce80d40cc763d338b) and [Windows/Linux CI](https://github.com/Cephalon-Labs/CephalonEngine/actions/runs/35090093557). Current unfinished leaf scope is **18 September tasks / 468 h**, plus ENG-532's **1 h** = **469 h (58.625 person-days)**. Phase 15 remaining scope is **180 h** plus ENG-532; the 8 h completed estimate is retained as historical scope, not elapsed time. The extra card was decomposition, not added effort. [Consumer evidence and exclusions](contract-compatibility.md) define the exact contract subset; no package maturity or deployment claims change.

## Host compatibility scope revision — September 16, 2026

[ENG-744 evidence](host-compatibility-2026-09.md) reproduced an unbounded process-output wait and repeated reference-anchor parsing. Its estimate increases **4 -> 12 h** (+8 h). Parent rollups become ENG-742 **20 h** and ENG-731 **40 h**, without double counting. The original September baseline was 536 h; revised implementation scope is **544 h**. Current unfinished leaves: **18 tasks / 476 h**, plus ENG-532 **1 h** = **477 h (59.625 person-days)**. Phase 15 increases from 208 to **216 h** (indicative range 162–324), with **188 h** remaining plus ENG-532. ENG-744 is active in Sprint 16 until host/deployment and release evidence passes; no capacity-backed due date or maturity promotion is implied.
