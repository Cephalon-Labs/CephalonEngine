# Planning Governance

This guide defines the standing planning and execution workflow for Cephalon.

It exists to keep repository docs, GitHub Project tracking, sprint planning, and implementation history aligned as one durable project record.

## Why this guide exists

Cephalon has grown past the point where planning can live only in thread memory, ad hoc issue titles, or code comments.

The repository now has:

- a roadmap
- a backlog
- sprint history
- planning sync automation
- a GitHub Project
- a large and growing package, sample, and companion-pack surface

That means meaningful work must stay synchronized across both repo-owned documents and the GitHub Project workflow.

## Scope

Apply this guide to every meaningful task that changes one or more of the following:

- shipped behavior
- architecture direction
- planning status
- roadmap or backlog state
- sprint allocation
- docs or adoption guidance
- validation expectations
- benchmark expectations
- package or runtime contracts

Small local experiments can remain temporary, but once work becomes intentional project progress it should be represented in both docs and GitHub Project tracking.

## Required sources of truth

Cephalon planning now has two complementary durable records.

### 1. Repo-owned docs

Use repo docs for the durable planning and product record:

- [Engine roadmap](engine-roadmap.md) for phase-level and sprint-level sequencing
- [Engine backlog](engine-backlog.md) for task-level status, estimates, and acceptance framing
- [Project memory](project-memory.md) for standing collaboration rules and settled decisions
- feature, component, and operations docs for shipped behavior and adoption guidance

### 2. GitHub Project tracking

Use the GitHub Project as the execution and coordination surface:

- project: [`@Cephalon-Engine`](https://github.com/orgs/Cephalon-Labs/projects/2)
- repo: [`Cephalon-Labs/CephalonEngine`](https://github.com/Cephalon-Labs/CephalonEngine)
- planning sync automation: `scripts/sync-planning-github.ps1`
- planning sync workflow: `.github/workflows/planning-sync.yml`

The docs and the GitHub Project must agree. If they drift, update both.

## Minimum planning requirements for every meaningful work item

Each meaningful work item must have:

- a repo-doc footprint
- a GitHub Project card or issue-backed project item
- a clear sprint or iteration placement
- a clear validation expectation
- a traceable implementation history

In practice, that means:

- roadmap and backlog entries stay truthful
- the task appears in GitHub Project tracking
- the card body contains enough detail to stand on its own
- project fields stay populated
- commits that materially advance the work are referenced from the card

## Surface maturity declaration rule

Every meaningful package, runtime surface, or planning slice should declare:

- its primary product role
- its ownership mode
- its current target maturity
- the proof required before it can claim the next maturity level

Use [Engine surface maturity audit](engine-surface-maturity-audit.md) as the repo-owned vocabulary for that declaration.

Intentional `M0` taxonomy work and `M1` catalog/runtime-truth work are allowed.

They still need to be labeled explicitly.

Do not use execution-oriented language for a surface unless the package actually owns an execution or provisioning path.

## Architecture review cadence

Cephalon publishes a dated architecture-review snapshot once per month.

The cadence is intentional: monthly snapshots are short enough to capture meaningful slice progress without becoming a quarterly report, and they are long enough that the reviewer can synthesize multiple `ENG-*` slices into one coherent picture.

Anchor points so far:

- [Architecture review (April 2026)](architecture-review-2026-04.md), dated `April 13, 2026`
- [Architecture review (May 2026)](architecture-review-2026-05.md), dated `May 2, 2026`

Cadence rules:

- one new dated review per month, named `architecture-review-YYYY-MM.md`
- the new review extends, rather than replaces, the previous month's review; older reviews stay in `docs/` as the durable monthly history
- each review explicitly cross-references [`long-range-direction.md`](long-range-direction.md) horizon framing and [`engineering-standards.md`](engineering-standards.md) quality dimensions so the planning frame stays load-bearing across reviews
- if a month genuinely had no meaningful architectural change, the review for that month should still ship as a short `nothing material changed; previous review still stands` snapshot rather than be skipped, so the dated history stays unbroken
- the review month is the *publication month*, not necessarily the analysis window; a review dated May 2026 may report on slices from April–May, but it is filed under May
- both [`docs/README.md`](README.md) Start here and Research references sections must list every dated review so the docs hub stays the canonical index

When the review month rolls over, the new review's recommendations supersede the previous month's recommendations only where they are explicitly addressed; otherwise, treat the previous recommendations as still open follow-through.

## Planning identifier rule

Once an `ENG-*` id is assigned in repo docs or GitHub Project tracking, treat it as immutable.

Do not reuse historical ids for a new planning wave, even if the earlier slice is already shipped or the later plan feels conceptually cleaner.

If sequencing changes, allocate a fresh id and update roadmap, backlog, issue titles, iteration placement, and card metadata together.

## Required GitHub Project card fields

When a work item is tracked in GitHub Project, populate these fields when they apply:

- `Assignee`
- `Label`
- `Type`
- `Project`
- `Estimate`
- `Iteration`
- `Test`
- `Benchmark`
- `Milestone`
- `Relationship`

If a field does not apply, leave an explicit note in the card body instead of silently omitting context.

## Required card content

Every meaningful GitHub Project card should let a reader understand the work without thread context.

Use this minimum narrative structure:

1. problem or purpose
2. scope
3. expected outcome
4. validation or proof
5. related docs or runtime surfaces
6. linked dependencies or parent/child relationships

Recommended content:

- why the work exists
- what is changing
- what is intentionally not changing
- impacted runtime or docs surfaces
- tests and benchmarks expected
- related roadmap/backlog references

## Required repo-doc updates

When work changes the project record, update the relevant docs in the same flow.

Typical examples:

- change in scope or direction: update [Project memory](project-memory.md)
- change in task state, estimate, or sprint placement: update [Engine backlog](engine-backlog.md)
- change in milestone or implementation order: update [Engine roadmap](engine-roadmap.md)
- change in surface maturity or runtime-ownership claims: update [Engine surface maturity audit](engine-surface-maturity-audit.md) and the affected component docs
- change in framework baseline or support-claim posture: update [Project memory](project-memory.md), [.NET 11 readiness](dotnet11-readiness.md), [Compatibility](compatibility.md), and [Package publishing](package-publishing.md)
- change in shipped behavior: update affected feature or component docs
- change in ops or adoption contract: update operations and getting-started docs

Do not patch only the nearest document if multiple docs reference the same surface.

## Commit reference rule

If a commit materially advances a GitHub Project card:

1. keep the commit intentional and readable
2. add a card comment that references the commit id
3. summarize what changed in one short paragraph or a few short bullets
4. mention validation status if it was part of that commit

The purpose is simple: the card should tell the implementation story without requiring a reader to search the full git history manually.

If one task lands across multiple commits, add multiple card comments.

## Definition of done for tracked work

A meaningful tracked task is not done when code exists only locally.

It is done when all relevant parts are aligned:

- the code is in place
- validation is recorded
- docs are truthful
- roadmap and backlog status are truthful
- the GitHub Project card is updated
- commit references are visible from the card history

## Scope-change rule

If scope changes while work is in progress:

- update the card description
- update the affected planning docs
- update iteration, estimate, milestone, or relationships if needed
- do not leave the old plan in place once it is no longer truthful

Cephalon is still in an active POC phase, so changing direction is allowed. Leaving stale planning artifacts behind is not.

## Suggested workflow

### 1. Before starting meaningful work

- confirm the task exists in planning docs or create the needed doc footprint
- confirm the task exists in GitHub Project tracking
- make sure sprint or iteration placement is explicit
- make sure validation expectations are written down

### 2. While implementing

- keep docs in sync when behavior changes
- keep card scope and field values truthful if the task grows or narrows
- record notable tradeoffs or design changes in the card or docs instead of leaving them thread-local

### 3. When validation passes

- update docs that describe the shipped behavior
- update backlog or roadmap state if the task materially advanced
- create or update the commit
- add a GitHub Project card comment that references the commit id and what landed

### 4. When closing the task

- make sure the card body still matches the final shipped outcome
- make sure linked docs are truthful
- make sure related dependencies or follow-ups are visible

## Automation and human responsibility

Cephalon already has planning automation:

- `scripts/sync-planning-github.ps1`
- `.github/workflows/planning-sync.yml`

That automation helps keep issues, milestones, and project metadata aligned.

It does not replace human responsibility for:

- writing good card descriptions
- keeping scope truthful
- updating feature docs
- adding commit-reference comments
- recording validation and implementation notes

## Quick checklist

Before you consider a task ready, confirm:

- docs updated
- roadmap truthful
- backlog truthful
- surface maturity and ownership truth captured
- sprint or iteration truthful
- framework and support claims truthful
- GitHub Project card updated
- required fields populated
- tests and benchmarks noted
- commit id referenced in card comments

## Related docs

- [Docs hub](README.md)
- [Engine surface maturity audit](engine-surface-maturity-audit.md)
- [Project memory](project-memory.md)
- [Engine roadmap](engine-roadmap.md)
- [Engine backlog](engine-backlog.md)
