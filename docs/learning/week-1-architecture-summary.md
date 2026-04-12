# Week 1 Architecture Summary Starter

Use this starter during Week 1 of the [Learning roadmap](../learning-roadmap.md).

The goal is not to write perfect documentation on the first pass.

The goal is to prove that you can explain Cephalon's design center in your own words while staying aligned with:

- [Project memory](../project-memory.md)
- [Architecture](../architecture.md)
- [App models](../app-models.md)
- [Cephalon.Engine](../components/engine.md)
- [Cephalon.Abstractions](../components/abstractions.md)
- [Cephalon.AspNetCore](../components/aspnetcore.md)

## What Cephalon is

Write 3-5 sentences here.

Prompt:

- Cephalon is a modular `.NET` engine/framework foundation that is growing into a modular runtime platform.
- It is designed around host-agnostic contracts, explicit app-model and transport choices, deterministic composition, and runtime introspection as a product surface.
- It includes runtime, tooling, scaffolding, templates, samples, and reference-doc publication paths.

Your draft:

> Replace this block with your own summary.

## What Cephalon is not

Write 3-5 sentences here.

Prompt:

- It is not a single app shell.
- It is not only an ASP.NET Core host.
- It is not only a code generator.
- It is not a pile of unrelated packages without a runtime contract.

Your draft:

> Replace this block with your own summary.

## Recurring design center

Convert the recurring design center from project memory into your own language.

- host-agnostic contracts first
- deterministic module and package composition
- explicit app-model, transport, technology, and policy selection
- runtime introspection as a product surface
- additive companion packs instead of engine-core sprawl
- generated starter output, templates, and samples aligned to the runtime contract

Your draft:

- <point>
- <point>
- <point>

## Layering model

Summarize the main layers.

### 1. `Cephalon.Abstractions`

Prompt:

- stable host-agnostic contract layer
- modules, capabilities, app-model, health, localization, patterns, technologies, and transports

Your notes:

- <note>

### 2. `Cephalon.Engine`

Prompt:

- composition, manifest generation, runtime behavior, package loading, policy, trust, diagnostics, and introspection center

Your notes:

- <note>

### 3. Host adapters

Prompt:

- ASP.NET Core and Worker are the primary shipped host adapters
- adapters should stay thin

Your notes:

- <note>

### 4. Companion packs

Prompt:

- behaviors
- data
- event sourcing
- observability
- cloud/platform follow-through packs

Your notes:

- <note>

## App-model dimensions

Explain the difference between these dimensions.

| Dimension | What it answers | Example |
| --- | --- | --- |
| Blueprint | <fill in> | `modular-monolith` |
| Pattern | <fill in> | `cqrs` |
| Transport | <fill in> | `rest-api` |
| Technology profile | <fill in> | `serverless-hosting` |

## Runtime introspection routes I inspected

Fill this table after running the showcase sample.

| Route | What it answered | Why it matters |
| --- | --- | --- |
| `/engine/manifest` | <fill in> | <fill in> |
| `/engine/snapshot` | <fill in> | <fill in> |
| `/engine/runtime-story` | <fill in> | <fill in> |
| `/engine/modules` | <fill in> | <fill in> |
| `/engine/capabilities` | <fill in> | <fill in> |
| `/health/ready` | <fill in> | <fill in> |

## Cephalon decisions I already agree with

- <decision>
- <decision>
- <decision>

## Cephalon decisions I want to inspect more deeply

- <question>
- <question>
- <question>

## Evidence I used

- docs:
  - [Project memory](../project-memory.md)
  - [Architecture](../architecture.md)
  - [App models](../app-models.md)
- code:
  - `samples/Cephalon.Sample.Showcase/ShowcaseSampleApp.cs`
  - `src/Cephalon.AspNetCore/Hosting/EngineWebApplicationExtensions.cs`
  - `src/Cephalon.Abstractions/Modules/IModule.cs`
  - `src/Cephalon.Engine/Composition/EngineServiceCollectionExtensions.cs`
- runtime:
  - `/engine/manifest`
  - `/engine/snapshot`
  - `/engine/runtime-story`

## End-of-week check

At the end of Week 1, you should be able to explain:

- why Cephalon is an engine/framework instead of an app shell
- why runtime introspection is treated as product surface
- why blueprints, patterns, transports, and technologies are different dimensions
- why host adapters should stay thin
