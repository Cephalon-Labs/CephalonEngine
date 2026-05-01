# Cephalon Learning Roadmap

This guide turns the current Cephalon repository into a structured 30-60-90 day learning path.

It is intended for contributors who want to learn:

- `.NET 10` in a real product-style repository
- framework and engine architecture
- runtime, build-time, and documentation surfaces
- relational and non-relational data provider patterns
- cloud, container, and operator-facing deployment concerns

This roadmap assumes a start date of `April 13, 2026` and runs through `July 11, 2026`.

Starter artifacts that pair with this roadmap live under [Learning starters](learning/README.md).
The current repo-grounded architecture baselines that should be read beside this roadmap live in [Architecture review (April 2026)](architecture-review-2026-04.md) and [Architecture review (May 2026)](architecture-review-2026-05.md). The long-range planning frame and consolidated quality baseline live in [Long-range engine direction](long-range-direction.md) and [Engineering standards](engineering-standards.md); contributors should skim both before committing to the 30-60-90 day plan so the work plan stays aligned with the engine's long-term posture and quality gates. The consolidated per-package adoption truth lives in [Conformance matrix](conformance-matrix.md), and the consolidated map of `/engine/*` routes, `snapshot.*` keys, and runtime catalog interfaces lives in [Runtime contract index](runtime-contract-index.md); use both as quick reference while learning specific surfaces during the plan.

## Why this guide exists

Cephalon is not a tutorial repository and it is not a single application shell.

It is a modular runtime platform with a broad surface area:

- host-agnostic contracts
- runtime composition
- app-model and transport descriptors
- data and eventing companion packs
- observability and cloud-oriented host integration packs
- scaffolding, CLI, template-pack, and reference-doc tooling

Without a learning order, it is easy to read the codebase sideways and miss the design center.

This guide keeps the sequence deliberate:

1. `.NET 10`
2. architecture and framework design
3. database topology and provider families
4. cloud, container, operations, and build-time adoption

## Working agreements from project memory

This roadmap follows the standing agreements in [Project memory](project-memory.md).

Use these rules throughout the full 90-day path:

- treat hand-authored docs under `docs/` as the primary human-facing product documentation
- treat runtime introspection routes such as `/engine/manifest`, `/engine/snapshot`, and `/engine/runtime-story` as part of the product, not as optional debug extras
- keep repository-facing written artifacts in English
- prefer explicit registration and deterministic runtime composition over ambient magic
- keep host adapters thin and push reusable behavior into engine services, modules, or companion packs
- when behavior changes, inspect the related docs graph and update every impacted hand-authored document that references that surface
- when version-sensitive `.NET`, library, or provider guidance matters, verify it from primary or official sources instead of relying on stale memory alone
- remember that Cephalon is still in an active POC and invention phase, so a cleaner long-term engine contract is allowed to replace a weaker compatibility shim deliberately

## Outcomes by day

### By day 30

You should be able to:

- explain the Cephalon layering model
- run the showcase sample in at least two modes
- describe the difference between blueprints, patterns, transports, and technology profiles
- navigate the main runtime introspection routes
- make a small code, test, and doc change in the repo safely

### By day 60

You should be able to:

- explain the engine-owned database-topology contract
- trace write/read/history/outbox role selection through configuration, runtime, and sample behavior
- explain the current read-model sync and durable projection-job flow
- compare Cephalon's relational and non-relational provider families at a high level

### By day 90

You should be able to:

- describe Cephalon as a modular runtime platform instead of as a collection of packages
- reason about runtime, build-time, and operator-facing surfaces together
- explain the current cloud, container, observability, and tooling story
- write a credible architecture recommendation or RFC grounded in the repo

## 30-60-90 structure

### Days 1-30: `.NET 10` and runtime fundamentals

Focus:

- `.NET 10` hosting model
- DI, configuration, Minimal APIs, hosted services
- Cephalon abstractions, engine runtime, and ASP.NET Core adapter
- behavior ownership and module-owned REST

Primary reading:

- [Project memory](project-memory.md)
- [Architecture](architecture.md)
- [App models](app-models.md)
- [.NET ecosystem reference](dotnet-ecosystem-reference.md)
- [Cephalon.Abstractions](components/abstractions.md)
- [Cephalon.Engine](components/engine.md)
- [Cephalon.AspNetCore](components/aspnetcore.md)
- [Cephalon.Behaviors](components/behaviors.md)
- [Cephalon.Behaviors.Http](components/behaviors-http.md)
- [Module authoring](module-authoring.md)

Primary code anchors:

- `samples/Cephalon.Sample.Showcase/ShowcaseSampleApp.cs`
- `src/Cephalon.AspNetCore/Hosting/EngineWebApplicationExtensions.cs`
- `src/Cephalon.Behaviors/Modules/BehaviorModuleBase.cs`
- `src/Cephalon.Behaviors.Http/Hosting/RestBehaviorModuleBase.cs`
- `samples/Cephalon.Sample.Showcase/Modules/ShowcaseSystemModule.cs`

Expected deliverables:

- one glossary page for core Cephalon terms
- one short architecture summary written in your own words
- one small validated repo change

### Days 31-60: database topology and provider learning

Focus:

- engine-owned database topology
- EF Core role-aware runtime behavior
- write/read/history split
- durable read-model sync and projection jobs
- audit-history follow-through
- provider-family taxonomy across relational and non-relational packs

Primary reading:

- [Database topology](database-topology.md)
- [Cephalon.Data](components/data.md)
- [Cephalon.Data.EntityFramework](components/data-entityframework.md)
- [Cephalon.Audit.EntityFramework](components/audit-entityframework.md)
- `docs/components/data-*.md`
- `docs/components/event-sourcing-*.md`
- [Technology packs](technology-packs.md)

Primary code anchors:

- `samples/Cephalon.Sample.Showcase/Infrastructure/ShowcaseDbContext.cs`
- `samples/Cephalon.Sample.Showcase/Infrastructure/ShowcaseReadModelSyncService.cs`
- `samples/Cephalon.Sample.Showcase/Infrastructure/ShowcaseReadModelProjector.cs`
- `samples/Cephalon.Sample.Showcase/Infrastructure/ShowcaseResetService.cs`
- `samples/Cephalon.Sample.Showcase/Infrastructure/ShowcaseSystemProjectionService.cs`
- `tests/Cephalon.Tests.Hosting/ShowcaseSampleHostingTests.cs`

Expected deliverables:

- one provider-family comparison table
- one database-topology note explaining requested vs resolved roles
- one change or test improvement in the showcase data/runtime path

### Days 61-90: cloud, operations, and build-time adoption

Focus:

- operator-facing runtime and diagnostics
- deployment and container surfaces
- observability packs
- CLI, scaffolding, template pack, and reference-doc tooling
- planning and architecture recommendations

Primary reading:

- [Operations](operations.md)
- [Container runtime](container-runtime.md)
- [Azure App Service deployment](azure-app-service-deployment.md)
- [Azure Container Apps deployment](azure-container-apps-deployment.md)
- [Kubernetes deployment](kubernetes-deployment.md)
- [Reference docs](reference-docs.md)
- [Compatibility](compatibility.md)
- [Engine roadmap](engine-roadmap.md)
- [Engine backlog](engine-backlog.md)
- `docs/components/observability-*.md`
- [Cephalon.Cli](components/cli.md)
- [Cephalon.Scaffolding](components/scaffolding.md)
- [Cephalon.ReferenceDocs](components/reference-docs.md)

Primary code anchors:

- `src/Cephalon.Cli`
- `src/Cephalon.Scaffolding`
- `src/Cephalon.ReferenceDocs`
- `src/Cephalon.Observability*`

Expected deliverables:

- one operations map that ties docs to runtime routes
- one architecture review grounded in current code and docs
- one RFC-sized recommendation for a next Cephalon investment area

## 12-week plan

### Week 1: April 13-19, 2026

Goal:

- recover the design center of the repo and learn the main runtime surfaces

Read:

- [Project memory](project-memory.md)
- [Architecture](architecture.md)
- [App models](app-models.md)
- [Docs hub](README.md)

Run:

- `dotnet run --project samples/Cephalon.Sample.Showcase/Cephalon.Sample.Showcase.csproj`

Inspect:

- `/engine/manifest`
- `/engine/snapshot`
- `/engine/runtime-story`
- `/api/v1/showcase/system/summary`

Produce:

- a one-page summary of Cephalon in your own words
- a glossary of 15-20 core terms

Starter artifacts:

- [Week 1 architecture summary starter](learning/week-1-architecture-summary.md)
- [Cephalon glossary starter](learning/cephalon-glossary-starter.md)

### Week 2: April 20-26, 2026

Goal:

- understand `.NET 10` hosting and adapter wiring in the Cephalon style

Read:

- [.NET ecosystem reference](dotnet-ecosystem-reference.md)
- [Cephalon.Engine](components/engine.md)
- [Cephalon.AspNetCore](components/aspnetcore.md)

Study code:

- `samples/Cephalon.Sample.Showcase/ShowcaseSampleApp.cs`
- `src/Cephalon.AspNetCore/Hosting/EngineWebApplicationExtensions.cs`
- `src/Cephalon.AspNetCore/Hosting/EngineWebApplicationBuilderExtensions.cs`

### Week 3: April 27-May 3, 2026

Goal:

- understand module ownership and behavior transport exposure

Read:

- [Cephalon.Behaviors](components/behaviors.md)
- [Cephalon.Behaviors.Http](components/behaviors-http.md)
- [Module authoring](module-authoring.md)

Study code:

- `src/Cephalon.Behaviors/Modules/BehaviorModuleBase.cs`
- `src/Cephalon.Behaviors.Http/Hosting/RestBehaviorModuleBase.cs`
- showcase bounded-context modules

### Week 4: May 4-10, 2026

Goal:

- connect Cephalon's current implementation to broader architecture patterns

Read:

- [Architecture patterns research](architecture-patterns-research.md)
- [Architecture recommendations](architecture-recommendations.md)
- [Design patterns reference](architecture/design-patterns-reference.md)

Produce:

- a short note that maps Cephalon to Clean Architecture, Hexagonal Architecture, Vertical Slice, Modular Monolith, CQRS, and Plugin Architecture

### Week 5: May 11-17, 2026

Goal:

- understand the engine-owned database-topology contract

Read:

- [Database topology](database-topology.md)
- [Cephalon.Data](components/data.md)
- [Cephalon.Data.EntityFramework](components/data-entityframework.md)

Study code:

- `samples/Cephalon.Sample.Showcase/Infrastructure/ShowcaseDbContext.cs`
- `src/Cephalon.Abstractions/Data/DatabaseRoleDescriptor.cs`
- `src/Cephalon.Abstractions/Data/DatabaseMigrationDescriptor.cs`

### Week 6: May 18-24, 2026

Goal:

- understand durable read-model synchronization and projection jobs

Study code:

- `samples/Cephalon.Sample.Showcase/Infrastructure/ShowcaseReadModelSyncService.cs`
- `samples/Cephalon.Sample.Showcase/Infrastructure/ShowcaseReadModelProjector.cs`
- `samples/Cephalon.Sample.Showcase/Infrastructure/ShowcaseReadModelProjectionHostedService.cs`
- `samples/Cephalon.Sample.Showcase/Infrastructure/ShowcaseSystemProjectionService.cs`

Inspect:

- `/api/v1/showcase/system/database-topology`
- `/api/v1/showcase/system/database-topology/brief`
- `/api/v1/showcase/system/database-topology/handoff` (self-describing zip with `README.md`, `handoff-manifest.json`, brief, and raw projection)
- `/engine/database-roles`
- `/engine/database-migrations`

### Week 7: May 25-31, 2026

Goal:

- understand audit history, reset flows, and operator-facing business/runtime summaries

Read:

- [Cephalon.Audit.EntityFramework](components/audit-entityframework.md)
- [Operations](operations.md)

Study code:

- `samples/Cephalon.Sample.Showcase/Infrastructure/ShowcaseResetService.cs`
- audit-history routes and related services

### Week 8: June 1-7, 2026

Goal:

- map the provider-family landscape

Read:

- all relevant `docs/components/data-*.md`
- all relevant `docs/components/event-sourcing-*.md`

Produce:

- one comparison table covering relational, document, key-value, search, vector, graph, and event-store styles in Cephalon

### Week 9: June 8-14, 2026

Goal:

- understand operator and deployment surfaces in local and Docker-backed runs

Read:

- [Operations](operations.md)
- [Container runtime](container-runtime.md)

Run:

- the showcase sample against Docker-backed dependencies

### Week 10: June 15-21, 2026

Goal:

- understand current cloud and platform integration guidance

Read:

- [Azure App Service deployment](azure-app-service-deployment.md)
- [Azure Container Apps deployment](azure-container-apps-deployment.md)
- [Kubernetes deployment](kubernetes-deployment.md)
- `docs/components/observability-*.md`

### Week 11: June 22-28, 2026

Goal:

- understand build-time and adoption surfaces

Read:

- [Getting started](getting-started.md)
- [Reference docs](reference-docs.md)
- [Compatibility](compatibility.md)
- [Cephalon.Cli](components/cli.md)
- [Cephalon.Scaffolding](components/scaffolding.md)
- [Cephalon.ReferenceDocs](components/reference-docs.md)

Study code:

- `src/Cephalon.Cli`
- `src/Cephalon.Scaffolding`
- `src/Cephalon.ReferenceDocs`

### Week 12: June 29-July 5, 2026

Goal:

- consolidate your own view of Cephalon and propose a next-step improvement

Read:

- [Engine roadmap](engine-roadmap.md)
- [Engine backlog](engine-backlog.md)

Produce:

- a strengths/risks/gaps summary
- one recommended next investment area
- one RFC-sized architecture note

## Week 1 daily plan

### Day 1: April 13, 2026

- read [Project memory](project-memory.md) end to end
- write down the recurring design center in your own words
- scan `global.json`, `Directory.Packages.props`, and the docs hub

Output:

- one short note: "What Cephalon is and what it is not"

### Day 2: April 14, 2026

- read [Architecture](architecture.md)
- read [App models](app-models.md)
- list the built-in blueprints, transports, and technology profiles

Output:

- one glossary draft with at least 10 terms

### Day 3: April 15, 2026

- run the showcase sample
- inspect `/engine`, `/engine/manifest`, `/engine/snapshot`
- capture what each route answers that the others do not

Output:

- one table of runtime introspection routes

### Day 4: April 16, 2026

- read [Cephalon.Engine](components/engine.md)
- read [Cephalon.Abstractions](components/abstractions.md)
- open `src/Cephalon.Abstractions/Modules/IModule.cs`
- open `src/Cephalon.Engine/Composition/EngineServiceCollectionExtensions.cs`

Output:

- one note explaining the contract boundary between abstractions and engine

### Day 5: April 17, 2026

- read [Cephalon.AspNetCore](components/aspnetcore.md)
- open `samples/Cephalon.Sample.Showcase/ShowcaseSampleApp.cs`
- trace startup from builder creation to route mapping

Output:

- one startup flow diagram or bullet list

### Day 6: April 18, 2026

- read [Cephalon.Behaviors](components/behaviors.md)
- read [Cephalon.Behaviors.Http](components/behaviors-http.md)
- compare `BehaviorModuleBase` and `RestBehaviorModuleBase`

Output:

- one note on module-owned REST versus generic behavior transport paths

### Day 7: April 19, 2026

- reread your notes from the week
- write a one-page summary of Cephalon's architecture in English
- identify one very small repo change that would be safe to make in week 2

Output:

- one page titled `Week 1 Architecture Summary`

## Weekly note template

Use the following template for each week.

```md
# Week <n> Notes

Date range: <start date> - <end date>

## Main focus

- <what this week was supposed to teach>

## What I read

- <doc or code path>
- <doc or code path>

## What I ran

- <command>
- <route inspected>

## What I now understand better

- <concept>
- <concept>

## Where I was still confused

- <confusion>
- <confusion>

## Repo truths I verified

- <runtime, doc, or test truth>
- <runtime, doc, or test truth>

## Small changes or experiments

- <change>
- <validation>

## Risks or design tensions I noticed

- <risk>
- <tradeoff>

## Next week's starting point

- <first doc or code path to open>
- <first command to run>
```

## Architecture lens to carry through the roadmap

### Current strengths

- layering is clear and consistent with the repo mission
- runtime introspection is a real product surface rather than an afterthought
- docs, tests, samples, and runtime routes increasingly reinforce the same truths
- companion packs keep the engine core from absorbing every technology concern directly

### Current risks

- the package and provider surface is expanding faster than a unified conformance story
- newer contributors can confuse `shipped`, `baseline`, and `planned` language across docs
- some operator-friendly truths are still sample-level patterns before they become engine-level primitives
- maintenance overhead will continue to grow across analyzers, docs, tests, providers, and deployment guidance

### Current gaps

- `Engine:Databases` is strongest on the Entity Framework path and is not yet equally unified across all provider families
- migration orchestration is still more startup-apply and command-template oriented than deploy-orchestration oriented
- serverless is visible as a technology profile and planning concept but is not yet a first-class host path comparable to ASP.NET Core or Worker
- a broad provider and observability conformance matrix is still needed

### Current architecture recommendations

- add maturity labeling in docs so readers can distinguish proven surfaces from baseline or planned slices quickly
- build conformance suites for data, event-sourcing, and observability companion packs
- promote proven sample-level operator answers into optional engine-level runtime catalogs when the contract is ready
- keep protecting the core design rule: host-agnostic contracts, explicit runtime truth, and thin host adapters

## Evidence checkpoints

By the end of the roadmap, make sure you can point to evidence in all of these places:

- docs
- runtime introspection routes
- tests
- sample behavior
- source contracts in `Cephalon.Abstractions`
- engine behavior in `Cephalon.Engine`
- host wiring in `Cephalon.AspNetCore` or `Cephalon.Worker`

If one claim about Cephalon cannot be traced to at least one of those, treat that claim as provisional until you verify it.

## Related docs

- [Project memory](project-memory.md)
- [Learning starters](learning/README.md)
- [Architecture review (April 2026)](architecture-review-2026-04.md)
- [Architecture review (May 2026)](architecture-review-2026-05.md)
- [Long-range engine direction](long-range-direction.md)
- [Engineering standards](engineering-standards.md)
- [Runtime contract index](runtime-contract-index.md)
- [Conformance matrix](conformance-matrix.md)
- [Engine surface maturity audit](engine-surface-maturity-audit.md)
- [Architecture](architecture.md)
- [App models](app-models.md)
- [Database topology](database-topology.md)
- [Operations](operations.md)
- [.NET ecosystem reference](dotnet-ecosystem-reference.md)
- [Engine roadmap](engine-roadmap.md)
- [Engine backlog](engine-backlog.md)
