# Cephalon architecture review — September 2026

Review date: **September 16, 2026**. Delivery: **ENG-718**. Extends the [June review](architecture-review-2026-06.md) and [June follow-ups](architecture-review-2026-06-followups.md). [Research](framework-research-2026-09.md), [execution plan](framework-completion-plan.md), [engineering standards](engineering-standards.md), [long-range direction](long-range-direction.md).

## Conclusion

Cephalon has broad, internally consistent package coverage. The next investment should complete shared coordination, prove owned operator loops and external adoption, and close cross-cutting release gaps. More descriptors, provider names or maturity labels do not resolve those gaps. Scope-specific acceptance replaces an unbounded claim to be complete or best forever.

## Evidence inspected

- `398b2b3a` master/origin parity; 107 source projects, component docs and package evidence rows; M0=1, M1=39, M2=51, M3=7, M4=9; live maturity report passed with zero drift.
- Gate 1 ExtensionSections is shipped; shared-kernel and pilot issues #1405/#1406 remain open. Existing CDC lease contracts, Eventing journals and governance retry coordination justify an inventory/ADR before a new kernel.
- global.json pins 10.0.303 while orientation/readiness docs still named 10.0.201. Microsoft sources now show .NET 11 RC1 and .NET 10.0.12; these are upstream facts, not a Cephalon SDK upgrade or RC support claim.
- GitHub had three open issues before this wave, including #1180 signed-release dispatch blocker. The blocker is retained; this research pass did not rerun a release dispatch.
- Project iteration dates extend into 2030 and an unscheduled placeholder has a past date. A September 29 Phase 14 due date had no matching capacity plan. It is removed; future tasks remain explicitly unscheduled until calendar reconciliation.
- Planning parser ignored explicit phase/iteration and treated dated shipped status as open. This delivery fixes the bounded parsing defects; broad historical issue/date reconciliation is ENG-737.

## Decisions and open risks

1. Keep core host-agnostic; reuse existing effect/store/identity seams. ENG-719 inventories duplication before public APIs grow.
2. M3 must demonstrate authorized, bounded effects, durability and recovery; provider metadata cannot stand in for a loop. ENG-720–726.
3. Retain all current package levels and add evidence-expiry/renewal rules. Existing M4 tooling/contracts use role-appropriate adoption criteria. ENG-726/734.
4. Treat tenant isolation, privacy/data recovery, supply chain, performance/SLOs, compatibility, accessibility and maintenance as release work with estimates. ENG-727–738.
5. Keep net10.0 shipping; evaluate RC1/servicing and selective AOT separately. No upstream date or support statement silently changes Cephalon's contract.
6. Preserve historical monthly reviews. July/August snapshots are absent; do not backdate invented reviews. ENG-737 reconciles the gap from evidence.
7. Prioritize owned workloads and integration alternatives over core feature breadth. Competitive claims need same-workload evidence; future technology work is time-boxed and reversible.

## Quality and horizon alignment

The [completion coverage matrix](framework-completion-plan.md#coverage-and-package-family-routing) maps all twelve engineering qualities to work. Security/Compliance/Auditability use explicit controls and artifacts; Performance/Availability/Scalability use measured SLOs; Reliability/Data integrity use recovery proofs; Usability/Flexibility/Maintainability/Compatibility use external journeys, stable contracts and lifecycle ownership.

Near term: trusted releases and adoption. Mid term: shared coordination and provider interchange. Far term: stable machine-readable contracts and replacement seams. Very far term: maintained evidence and stewardship. This follows all four horizons in the long-range direction without pretending to know future vendors or technologies.

## Follow-ups and delivery validation

The [backlog](engine-backlog.md#framework-completion-program-september-2026) is the live follow-up tracker; it avoids a second manually maintained status list. Parent ENG-714/715 remain open. At the ENG-718 planning cut, ENG-719–738 totaled 536 h of planned leaf work; ENG-532 added 1 h. The later coordination delivery below updates execution status and remaining estimates. No runtime implementation or maturity promotion is completed by this review.

The initial local scorecard publication rejected a pre-existing deployment-mode report whose publish-policy booleans disagreed with the manifest. A planning-only scorecard was then generated with an explicitly absent, dedicated report path; its deployment readback is `not-found`. This proves document/read-model consistency only, not deployment or release readiness. ENG-731 owns fresh deployment proof before any new claim. No artifact was edited to manufacture a pass.

Validation performed for this delivery:

| Check | Result / scope |
| --- | --- |
| Planning parser and live-guard Pester suites | 15 passed; phase, iteration, shipped status, explicit test/benchmark evidence, child tracking IDs, estimate total and legacy compatibility. |
| DocumentationCoverageTests | 24 passed, including hand-authored links/anchors and generated-reference links; a subsequent focused documentation pass also passed 20 tests. Existing Release test binaries were used because no C# implementation changed. |
| Surface maturity report | Passed; 107 packages, M0=1/M1=39/M2=51/M3=7/M4=9, drift=0; audit/backlog dates aligned. |
| Planning-only scorecard | Generated successfully with deployment proof explicitly not-found. API readback: 104 baselines, 3 pending packages, 205 additions, 0 removals; the stale prose was corrected. This is not full release validation. |
| Live GitHub fields and relationships | 24 relevant issues verified, including the planning delivery and existing blocker; exact estimates, assignee/type, five required fields, 8 native child links and 3 undated gate milestones. |
| Open-issue uniqueness guard | Passed; the temporary ENG-718 closeout warning is resolved when its pushed commit is recorded and the planning issue is closed. Runtime tasks remain open. |
| Whitespace / source scope | `git diff --check` clean; only documentation, planning parser and parser tests changed. No runtime behavior, package version, SDK or maturity label changed. |

GitHub publication:

- Planning delivery: [ENG-718 / #1411](https://github.com/Cephalon-Labs/CephalonEngine/issues/1411).
- Existing parents: [ENG-714 / #1405](https://github.com/Cephalon-Labs/CephalonEngine/issues/1405), [ENG-715 / #1406](https://github.com/Cephalon-Labs/CephalonEngine/issues/1406).
- Twenty planned tasks: ENG-719–738 / #1412–1431; individual links are in the [task map](framework-completion-plan.md#task-map).
- Gate milestones: [Phase 14](https://github.com/Cephalon-Labs/CephalonEngine/milestone/15), [Phase 15](https://github.com/Cephalon-Labs/CephalonEngine/milestone/16), [Phase 16](https://github.com/Cephalon-Labs/CephalonEngine/milestone/17).
- [Project 2](https://github.com/orgs/Cephalon-Labs/projects/2) now includes the plan, unit/rollup rules and scheduling limits in its README. [ENG-532 / #1180](https://github.com/Cephalon-Labs/CephalonEngine/issues/1180) retains its historical failed proof and 1 h next-action estimate under Phase 15.

The delivery commit is linked in the issue histories after push; local JSON/TRX/Pester receipts are under `artifacts/planning-2026-09/`. Future implementation, provider tests, RC assessment and external adoption remain open and are not included in these passed planning checks.

## Coordination implementation follow-up (September 16, 2026)

ENG-719/720 add immutable host-agnostic coordination contracts and opt-in instance-local execution. Existing snapshots and family services are preserved; `coordination` is a versioned non-health extension section. See the [decision](architecture/coordination-kernel.md) and [delivery evidence](coordination-kernel-delivery-2026-09.md). Durable recovery and shared authorization remain open. No package maturity or deployment support claim changes.
