# Cephalon Glossary Starter

Use this glossary starter during Weeks 1-2 of the [Learning roadmap](../learning-roadmap.md).

The purpose is to normalize the vocabulary you use when reading docs, code, tests, and runtime routes.

Do not try to make this exhaustive on day one.

Start with the core terms below, then extend the glossary as you learn more.

## Core terms

### Engine

Working definition:

> The composition, runtime, manifest, policy, package-loading, trust, and introspection center of Cephalon.

Where to verify:

- [Cephalon.Engine](../components/engine.md)
- `src/Cephalon.Engine`

Your notes:

- <note>

### Abstractions

Working definition:

> The host-agnostic public contract layer that modules, adapters, and companion packs build on.

Where to verify:

- [Cephalon.Abstractions](../components/abstractions.md)
- `src/Cephalon.Abstractions`

Your notes:

- <note>

### Module

Working definition:

> A deterministic unit of runtime composition that publishes services, capabilities, metadata, and optional lifecycle behavior.

Where to verify:

- [Module authoring](../module-authoring.md)
- `src/Cephalon.Abstractions/Modules/IModule.cs`

Your notes:

- <note>

### Capability

Working definition:

> A named runtime contract or permission-like unit that can be published, traced to a source module, and evaluated by policy.

Where to verify:

- `src/Cephalon.Abstractions/Capabilities`
- `/engine/capabilities`

Your notes:

- <note>

### Blueprint

Working definition:

> The primary app-shape descriptor that defines the broad project form without trying to encode every pattern, transport, or deployment choice.

Examples:

- `modular-monolith`
- `modular-vertical-slice`
- `microservice`
- `microservice-suite`

Where to verify:

- [App models](../app-models.md)

Your notes:

- <note>

### Pattern

Working definition:

> A design or runtime behavior choice that can be composed on top of a blueprint rather than replacing the blueprint itself.

Examples:

- `cqrs`
- `outbox`
- `strategy`
- `pipeline`

Where to verify:

- [App models](../app-models.md)
- [Architecture](../architecture.md)

Your notes:

- <note>

### Transport

Working definition:

> A public or runtime communication surface such as REST, gRPC, GraphQL, SSE, WebSocket, or another adapter-facing protocol.

Where to verify:

- [App models](../app-models.md)
- `/engine/manifest`

Your notes:

- <note>

### Technology profile

Working definition:

> An additive workload or platform hint that influences runtime surfaces without exploding the blueprint catalog.

Examples:

- `agentic-workloads`
- `serverless-hosting`
- `hybrid-cloud-runtime`

Where to verify:

- [Project memory](../project-memory.md)
- [Technology packs](../technology-packs.md)

Your notes:

- <note>

### Runtime introspection

Working definition:

> The operator-facing runtime truth surfaced through routes such as `/engine/manifest`, `/engine/snapshot`, and `/engine/runtime-story`.

Where to verify:

- [Project memory](../project-memory.md)
- `/engine`
- `/engine/snapshot`

Your notes:

- <note>

### Companion pack

Working definition:

> An additive package that extends Cephalon with focused runtime, provider, transport, or operational behavior without bloating the engine core.

Examples:

- `Cephalon.Data.EntityFramework`
- `Cephalon.Observability.OpenTelemetry`
- `Cephalon.Behaviors.Http`

Where to verify:

- [Component catalog](../components/README.md)

Your notes:

- <note>

### Host adapter

Working definition:

> A thin integration layer that hosts the engine in a runtime environment such as ASP.NET Core or Worker without redefining the engine contract.

Where to verify:

- [Cephalon.AspNetCore](../components/aspnetcore.md)
- [Cephalon.Worker](../components/worker.md)

Your notes:

- <note>

### Runtime manifest

Working definition:

> The stable runtime description of what loaded and what the engine selected, including modules, capabilities, app profile, and packages.

Where to verify:

- `/engine/manifest`
- `/engine/snapshot`

Your notes:

- <note>

### Runtime story

Working definition:

> The ordered operator-facing answer for what loaded, started, failed, and why.

Where to verify:

- `/engine/runtime-story`
- `/engine/snapshot`

Your notes:

- <note>

### Database role

Working definition:

> A logical engine-owned database slot such as `write`, `read`, `outbox`, or `history` that resolves to an effective provider and physical target.

Where to verify:

- [Database topology](../database-topology.md)
- `/engine/database-roles`

Your notes:

- <note>

### Database migration target

Working definition:

> The operator-facing migration unit tied to a logical role and current execution status.

Where to verify:

- [Database topology](../database-topology.md)
- `/engine/database-migrations`

Your notes:

- <note>

### Read-model sync

Working definition:

> The process that reconciles write-side state into a read-side projection, including projection lag, retry state, and durable job tracking where implemented.

Where to verify:

- `/api/v1/showcase/system/database-topology`
- `/api/v1/showcase/system/database-topology/brief`
- `/api/v1/showcase/system/database-topology/handoff` (self-describing zip with `README.md`, `handoff-manifest.json`, brief, and raw projection)
- `samples/Cephalon.Sample.Showcase/Infrastructure/ShowcaseReadModelSyncService.cs`

Your notes:

- <note>

## Terms to add later

Add to this section as you move through the roadmap.

- trust policy
- package policy
- outbox
- inbox
- execution graph
- hosted execution
- diagnostics convention
- technology surface
- projection
- event-dispatch runtime
- audit history
- reference docs
- scaffolding
- template pack

## Cross-check rule

Before you treat a glossary definition as stable, verify it in at least one of these places:

- a hand-authored doc
- a runtime route
- a public contract in `Cephalon.Abstractions`
- a concrete engine or sample implementation

If those disagree, keep the term marked as provisional until you resolve the mismatch.

## Related docs

- [Learning roadmap](../learning-roadmap.md)
- [Project memory](../project-memory.md)
- [Architecture](../architecture.md)
- [App models](../app-models.md)
- [Database topology](../database-topology.md)
- [Component catalog](../components/README.md)
