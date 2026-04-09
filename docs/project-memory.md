# Cephalon Project Memory

Project memory in this document reflects the repository state observed on `April 9, 2026`.

This page is a repo-oriented orientation snapshot. It is meant to help contributors recover context quickly before they change code, docs, planning, or package surfaces.

Cross-references: `README.md`, `docs/README.md`, `docs/architecture.md`, `docs/architecture-inventory.md`, `docs/architecture-recommendations.md`, `docs/engine-roadmap.md`, `docs/engine-backlog.md`, `docs/compatibility.md`

## Identity

Cephalon is not being positioned as a single application shell.

The repository is aiming at a modular .NET engine/framework foundation that is growing into a modular runtime platform. The recurring design center is:

- host-agnostic contracts first
- deterministic module and package composition
- explicit app-model, transport, technology, and policy selection
- runtime introspection as a product surface
- additive companion packs instead of engine-core sprawl
- generated starter output, templates, and samples that stay aligned with the runtime contract

## Current technical baseline

- the repo is pinned to `.NET SDK 10.0.201` through `global.json`
- the shipped project baseline is `net10.0`
- notable packaging exceptions are the template pack and source generator surfaces that stay on `netstandard2.0`
- central package management is enabled through `Directory.Packages.props`
- repo-wide build defaults enable nullable reference types, implicit usings, XML doc generation, and warnings-as-errors
- the default Cephalon package version baseline is currently `0.1.0-preview`

## Repo shape at a glance

Current project counts from the workspace scan:

- `src`: 81 projects
- `tests`: 4 projects
- `samples`: 9 projects
- `templates`: 6 projects
- `playground`: 2 projects
- `benchmarks`: 1 project

These counts are useful as a scale indicator only. They will drift as new companion packs and samples are added.

## What the repository already ships

The repo is well past an early prototype. The following surfaces are already present and should be treated as part of the living product:

- `Cephalon.Abstractions` as the host-agnostic contract layer
- `Cephalon.Engine` as the composition, manifest, runtime, policy, package-loading, trust, and app-model center
- `Cephalon.AspNetCore` and `Cephalon.Worker` as the primary host adapters
- transport adapters for REST, JSON-RPC, gRPC, GraphQL, Server-Sent Events, and WebSocket
- `Cephalon.Scaffolding`, `Cephalon.Cli`, and `Cephalon.TemplatePack` as the adoption and generation surfaces
- `Cephalon.ReferenceDocs` plus the published `docs/reference/` output as the XML-doc publishing path
- `Cephalon.Observability` plus dependency-health, exporter, cloud, and provider companion packs
- `Cephalon.Behaviors` plus HTTP, messaging, pattern, and source-generator companion packages
- `Cephalon.Data` and `Cephalon.EventSourcing` plus relational and non-relational provider families
- samples that model intended blueprint shapes and reference modules that model intended package authoring

## Runtime and architecture anchors

The architecture docs and the codebase align around a few important truths:

- blueprints are not the same thing as design patterns, deployment topology, or transports
- Cephalon models app shape through multiple dimensions rather than one overloaded architecture label
- the built-in app blueprints are `modular-monolith`, `modular-vertical-slice`, and `microservice`
- the built-in suite blueprint is `microservice-suite`
- transports are first-class descriptors and are expected to stay aligned across runtime, scaffolding, CLI parsing, templates, samples, and docs
- technology profiles are additive workload hints, not excuses to explode the blueprint catalog
- the engine owns the runtime contract and generator surfaces should follow it instead of re-encoding their own semantics

Current built-in transport surface:

- `rest-api`
- `json-rpc`
- `grpc`
- `graphql`
- `server-sent-events`
- `websocket`

Current built-in technology profile surface includes:

- `agentic-workloads`
- `event-driven-integration`
- `knowledge-retrieval`
- `realtime-experience`
- `edge-native-delivery`
- `serverless-hosting`
- `identity-access`
- `multi-tenancy`
- `hybrid-cloud-runtime`
- `service-mesh-integration`

## High-value code anchors

When recovering context in code, these are good starting points:

- `src/Cephalon.Abstractions/Modules/IModule.cs`
- `src/Cephalon.Abstractions/Modules/ModuleDescriptor.cs`
- `src/Cephalon.Engine/Composition/EngineServiceCollectionExtensions.cs`
- `src/Cephalon.Engine/Runtime/IRuntime.cs`
- `src/Cephalon.Engine/Runtime/RuntimeOperationalStory.cs`
- `src/Cephalon.AspNetCore/Hosting/EngineWebApplicationBuilderExtensions.cs`
- `src/Cephalon.AspNetCore/Hosting/EngineWebApplicationExtensions.cs`
- `src/Cephalon.Worker/Hosting/WorkerHostApplicationBuilderExtensions.cs`
- `src/Cephalon.Cli/CliApplication.cs`
- `src/Cephalon.Behaviors/Services/BehaviorDispatcher.cs`
- `src/Cephalon.Abstractions/Behaviors/IBehaviorContext.cs`

These files anchor the public or cross-cutting runtime contract more reliably than any one sample or playground host.

## Introspection is part of the product

Runtime introspection is not secondary documentation. It is an explicit operator-facing surface.

Important routes and concepts that repeatedly appear across docs and code:

- `/engine`
- `/engine/manifest`
- `/engine/snapshot`
- `/engine/runtime-story`
- `/engine/modules`
- `/engine/packages`
- `/engine/diagnostics`
- `/engine/technologies`
- `/engine/technology-catalog`
- `/engine/technology-surfaces`
- `/engine/capabilities`
- `/engine/dependencies`
- `/health`
- `/health/live`
- `/health/ready`

If a change impacts lifecycle, packages, diagnostics, technologies, executions, or health semantics, it likely also impacts one or more of these routes and the docs that describe them.

## Adoption surfaces

The repository now treats adoption as a first-class concern, not as a later packaging task.

The main adoption paths are:

- `Cephalon.Cli` as a packaged `.NET tool` with the `cephalon` command
- `Cephalon.TemplatePack` as the `dotnet new` install surface
- `Cephalon.Scaffolding` as the shared rendering layer behind generated output
- `samples/` as adoption-quality blueprint examples
- `playground/` as freeform experimentation rather than the official starter baseline

The generated app baseline already includes publishing and deployment assets for:

- published output smoke
- Windows Service
- IIS
- Azure App Service
- container image publishing
- Azure Container Apps
- Kubernetes
- Linux `systemd`

## Documentation model

The repository uses two documentation layers on purpose:

- hand-authored Markdown under `README.md` and `docs/` is the primary human-facing product and adoption documentation
- XML comments on public contracts are the API explanation layer used by IntelliSense and generated reference-doc publishing

This distinction matters. Hand-authored docs should explain ownership, usage, architecture, and adoption. XML comments should explain supported public API behavior precisely enough for reference generation.

## Planning memory

The roadmap and backlog indicate that Cephalon has already shipped large parts of its foundation, adoption hardening, operational baseline, package-loading baseline, execution/orchestration baseline, and solution-level platform baseline.

The near-term planning center has shifted away from "start the engine" and toward:

- hardening and truthfulness across shipped surfaces
- resilience and migration patterns
- keeping docs, templates, CLI behavior, scaffolding, package metadata, and runtime contracts aligned

Recent roadmap memory worth keeping in mind:

- Sprint 35 shipped benchmark expansion for hot paths
- Phase 11 is planned around resilience foundations such as circuit breaker, retry, timeout, bulkhead, rate limiting, and taxonomy additions such as Onion Architecture and Anti-Corruption Layer
- Phase 12 is planned around migration and advanced coordination, including strangler fig, saga choreography, BFF, feature flags, and durable execution foundations

## Collaboration agreements

These are explicit working agreements from the current collaboration and should be extended as new standing decisions are made.

- when the team settles on an approach, plan, recurring workflow, or repeated command pattern, record it in project memory so it does not rely on thread-local recall alone
- when deeper or version-sensitive external research is needed, especially around `.NET`, `.NET 10`, libraries, frameworks, support policy, or official guidance, use internet research instead of relying only on prior model knowledge
- when delegating research work to sub-agents, prefer primary and official sources first, then synthesize the result back into repo context for Cephalon-specific decisions
- for meaningful work, prefer using sub-agents in complementary roles when that improves the outcome, for example business/product framing, architecture, design patterns, documentation, planning cards, quality review, testing, benchmarking, or other task-shaped specialties
- let sub-agents collaborate as a working group rather than as isolated note takers; they can cross-check one another's reasoning, surface tradeoffs, and help review the same change from different perspectives before the final decision lands
- sub-agents may use internet research when it helps them validate framework guidance, technical options, benchmarks, security practices, documentation, or other external references that matter to the decision
- repo-local memory is the reliable cross-thread source of truth; agreements that matter beyond the current thread should be written down here or in another repo-owned document
- Cephalon is still in an active POC and invention phase, so decisions should not stay trapped by the current implementation alone; if a materially better approach exists for engine quality, architecture, performance, security, developer ergonomics, or future-proofing, it is acceptable to change direction deliberately
- when rethinking an area during this POC phase, optimize for making Cephalon a stronger long-term engine: reduce common developer pain, keep the platform broad enough for varied project types, and prefer durable engine primitives over short-term local convenience
- Cephalon does not yet have legacy production consumers that must be preserved at all costs; when a compatibility shim conflicts with a cleaner long-term engine contract during the POC phase, prefer removing the shim and shipping the better design deliberately

Current standing examples from this collaboration:

- keep important collaboration memory in `docs/project-memory.md`
- treat internet-backed research as the default follow-through for sub-agent learning tasks when accuracy or freshness matters
- when a coding task is complete and the change is validated, stage only the intended files, create a commit, keep `master` updated as the integration branch, push the finished work to GitHub, and leave project docs aligned with the shipped behavior
- when a code change affects documented behavior, inspect the related `docs/*` graph and update every impacted hand-authored document that references that surface instead of patching only one nearby page
- GitHub Project cards should contain enough narrative detail for a reader to understand the work without extra thread context, and they should populate the standard fields `Assignee`, `Label`, `Type`, `Project`, `Estimate`, `Iteration`, `Test`, `Benchmark`, `Milestone`, and `Relationship` when cross-card references exist
- when a commit materially advances a GitHub Project card, add a card comment that records what changed and references the commit id so the card keeps a readable implementation history
- repository-facing written artifacts should be in English, including hand-authored documentation, commit messages, and planning or tracking content that becomes part of the project record
- use sub-agents proactively for review, consultation, and parallel investigation when a task benefits from multiple perspectives instead of treating delegation as a last resort
- keep future-facing engine quality in view during POC work, including architecture strength, design-pattern fit, performance, security, support for broad project shapes, and overall developer experience
- when a host exposes both module-owned REST helpers and generic behavior HTTP routes, treat the module-owned REST groups as the public REST/OpenAPI/Scalar surface and keep the generic behavior routes focused on non-REST adapter transports
- public REST is module-owned only: do not declare `http.rest` in `[BehaviorAllowedTransports(...)]` or `ConfigureTopology(...)`; author REST through `RestBehaviorModuleBase.ConfigureRestBehaviors(...)` by default and reserve low-level `MapBehaviorRestGroup(...)` work for advanced/manual cases
- `Engine:Behaviors` is no longer a per-behavior REST topology contract; keep it focused on behavior discovery and auto-registration
- when a module explicitly owns behaviors, prefer `BehaviorModuleBase` for process-only modules and `RestBehaviorModuleBase` when the same module also exposes some of those behaviors over REST; keep raw `IRestModule` implementations for REST modules that do not dispatch through behaviors
- prefer `BehaviorModuleBase` when a module explicitly owns behaviors but does not expose a public REST surface
- prefer `RestBehaviorModuleBase` when the same module owns behaviors and exposes some of them over REST
- prefer the single-surface REST DSL on `RestBehaviorModuleBase`: public `behaviors.Group(...).MapGet/MapPost/...` routes imply ownership automatically, while `behaviors.Internal<TBehavior>()` is the explicit path for internal-only or custom/manual-route behaviors in the same module
- keep one bounded context in one module when possible; do not split modules only to separate internal behaviors from REST-exposed behaviors
- `Engine:Behaviors:AutoRegister` is now an opt-in fallback rather than the default behavior-ownership path; prefer explicit module ownership and only turn scanning back on when a host intentionally wants convention-based discovery
- keep behavior return contracts transport-neutral by default: prefer raw `TOut` for simple success paths, prefer `BehaviorResult<T>` for expected non-success branches, and treat `ResultModel<T>` / `ResultModelError` as an optional REST wire-format policy rather than the universal engine contract

## Working assumptions for contributors

Unless the code clearly proves otherwise, contributors should assume:

- host adapters should stay thin
- reusable behavior belongs in engine services, modules, or companion packs rather than in app hosts
- deterministic ordering and explicit registration beat ambient discovery magic
- any public runtime, package, or diagnostics surface may also have docs, sample, scaffold, template, and validation-script follow-through
- hand-authored docs are part of the product and should be kept truthful when shipped behavior changes
- documents under `docs/*` often cross-reference one another, so code changes may require multi-file doc updates across hubs, component pages, operations guides, compatibility guidance, roadmap/backlog notes, and related adoption docs
- GitHub Project tracking should stay readable on its own, with complete card descriptions and the expected planning fields filled in so another contributor can understand scope, validation, timing, and dependencies quickly
- GitHub Project history should remain traceable from the card itself, including comment-level references to relevant commit ids as work lands
- the durable written project record should stay in English so docs, commits, cards, and related planning artifacts read consistently across the repository
- XML comments on public contracts are not optional polish; they are part of the supported documentation surface

## Current next-look areas

If you need to resume deeper analysis later, the most likely next focus areas are:

- `Cephalon.Engine` runtime, package, trust, and introspection internals
- `Cephalon.Behaviors` and the ABT pipeline, strategies, and transport bindings
- `Cephalon.Data` plus provider families and event-sourcing follow-through
- `Cephalon.Observability` plus dependency-health and exporter/provider conventions
- CLI, scaffolding, template, and sample alignment whenever app-model or package contracts change

## Local workspace notes

This snapshot was created from the current local workspace, not from a clean published release artifact.

Observed local notes during the scan:

- the git worktree contains local untracked directories such as `.build/`, `.claude/`, `.dotnet/`, `.dotnet-cli/`, `.dotnet-appdata/`, and `.nuget-appdata/`
- direct `git status` required a safe-directory override because the repository ownership on disk differs from the current user context

Those notes may be local-environment artifacts rather than repository-intended state.
