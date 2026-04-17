# Cephalon Architecture Review - April 2026

Review date: `April 13, 2026`

Cross-references: `docs/architecture.md`, `docs/architecture-inventory.md`, `docs/architecture-recommendations.md`, `docs/architecture/rest-endpoint-authoring-strategy.md`, `docs/database-topology.md`, `docs/learning-roadmap.md`, `docs/project-memory.md`

## Purpose

This review captures the current architectural state of Cephalon as it exists in the repository today.

It is meant to answer four questions clearly:

1. what is already strong
2. where the main architectural risks now sit
3. which gaps matter most next
4. what the recommended next moves are

## Review scope and evidence

This review is grounded in the current repo, not in a future-state pitch deck.

Primary evidence used:

- hand-authored docs under `docs/`
- core runtime and host code under `src/Cephalon.*`
- the adoption-quality `samples/Cephalon.Sample.Showcase`
- current runtime-introspection and operator-surface design
- current planning state in `docs/engine-roadmap.md` and `docs/engine-backlog.md`

High-value code anchors for this review:

- `src/Cephalon.Abstractions`
- `src/Cephalon.Engine`
- `src/Cephalon.AspNetCore`
- `src/Cephalon.Worker`
- `src/Cephalon.Data.EntityFramework`
- `samples/Cephalon.Sample.Showcase/ShowcaseSampleApp.cs`
- `samples/Cephalon.Sample.Showcase/Infrastructure/ShowcaseSystemProjectionService.cs`

## Executive summary

Cephalon has moved beyond a loose package collection and is now behaving like a real modular runtime platform.

Its strongest architectural traits are:

- a clean host-agnostic core
- explicit runtime composition and policy selection
- runtime introspection treated as product surface
- additive companion-pack growth instead of engine-core sprawl
- a showcase sample that increasingly proves the architecture through real operator flows

Its main architectural pressure now comes from breadth rather than direction.

The repository is expanding across providers, transports, observability packs, tooling, docs, and deployment guidance. The direction is strong, but the cost of keeping all of those surfaces equally truthful is now the main risk.

## Current strengths

### 1. The layering model is real

`Cephalon.Abstractions` remains the host-agnostic contract layer, `Cephalon.Engine` remains the runtime/composition center, and host adapters remain additive. That separation is visible in both the source tree and the runtime surface.

This is important because it keeps Cephalon from collapsing into an ASP.NET Core-specific framework with portability language layered on top.

### 2. Runtime introspection is treated as first-class product behavior

Cephalon does not treat manifest, runtime story, diagnostics, package policy, or topology answers as debug leftovers.

Routes such as `/engine/manifest`, `/engine/snapshot`, `/engine/runtime-story`, `/engine/database-roles`, and `/engine/database-migrations` make the system self-describing. That is one of the clearest differentiators in the current architecture.

### 3. The app model separates concerns cleanly

Blueprints, patterns, transports, and technology profiles are modeled as different dimensions instead of being collapsed into one overloaded "architecture type".

That design gives Cephalon room to support many system shapes without exploding the blueprint catalog or baking transport decisions into the wrong layer.

### 4. Companion packs are containing complexity well

The repository already spans behaviors, data, event sourcing, observability, cloud exporters, dependency-health packs, tooling, and templates.

The current architecture usually puts those concerns into additive companion packs instead of letting `Cephalon.Engine` absorb every technology choice directly. That is a healthy long-term direction for framework evolution.

### 5. The showcase sample is becoming a real proving ground

`Cephalon.Sample.Showcase` now validates more than happy-path REST endpoints. It proves runtime introspection, database-role topology, migrations, audit history, durable read-model sync, transport discovery, and operator-facing UI flows together.

That matters because Cephalon needs an adoption-quality validation surface, not just isolated unit tests and package docs.

## Main risks

### 1. Conformance debt is now the biggest scaling risk

Cephalon has many provider and companion-pack families. The architecture is good, but the repository can still drift if provider packs implement the same ideas unevenly.

The risk is not that the core architecture is wrong. The risk is that the surface area grows faster than the shared conformance story.

### 2. Documentation language can drift faster than code

The repo now uses important terms such as `shipped`, `baseline`, `follow-through`, `planned`, and `later`.

Those distinctions matter, but new contributors can still misread them. Without stronger maturity labeling, it is easy to overstate what is production-ready versus what is directionally correct but still thin.

### 3. Sample-level operator answers can become de facto contracts too early

The showcase now exposes strong operator projections such as `/api/v1/showcase/system/database-topology`.

That is useful, but it creates a recurring design tension: a good sample projection can look like an engine contract before the underlying abstraction is ready to be generalized. Cephalon needs to keep proving ideas in the showcase without prematurely freezing them in the engine.

### 4. Operational breadth is ahead of orchestration depth

Cephalon now documents many provider, cloud, deployment, and observability paths.

The platform explains a lot, but some deeper operational workflows are still guidance-oriented rather than engine-owned workflows. Migration command templates are a good example: the truth is visible, but deploy-time orchestration is still deliberately external.

### 5. Default-path dilution is becoming a real product risk

Cephalon supports many choices. That is powerful, but it can also weaken the learning and adoption story if the repository does not keep one or two golden paths especially crisp.

The more optionality the engine adds, the more important it becomes to preserve a very clear "recommended first path" for adopters.

### 6. Future REST shorthand must stay on the shipped runtime-catalog and collision-validation baseline

The current module-owned REST DSL is the right direction, and Cephalon now also ships a first-class
resolved REST runtime catalog plus fail-fast route-collision validation over the normalized
projection layer. The remaining risk is future shorthand bypassing that baseline instead of
compiling into it. If a later authoring path publishes routes outside the shared projection,
runtime-catalog, and collision-validation pipeline, Cephalon could reintroduce duplicate routes,
weaker ownership semantics, and operator ambiguity.

## Main gaps

### 1. A stronger conformance matrix is still needed

Cephalon needs broader, repeatable validation across data providers, event-sourcing packs, observability packs, and deployment surfaces.

The architecture strongly suggests this need already; the repo now needs the same discipline in automation and documentation.

### 2. Provider-neutral operational orchestration is still thinner than the operator surface

The engine now exposes topology, roles, migration targets, and command guidance well.

What it does not yet own at the same level is a richer deployment-orchestration story around migration execution, replay flows, or broader operational automation.

### 3. Feature maturity is not yet labeled sharply enough

The repo has many shipped surfaces, but not all shipped surfaces are equally mature.

Cephalon would benefit from explicit maturity labeling so readers can distinguish:

- core stable foundation
- adoption-quality validated baseline
- showcase-proven pattern
- future planned direction

### 4. Serverless and hybrid-cloud remain more architectural intent than runtime path

The technology-profile story is strong, but the runtime-host story is still strongest on ASP.NET Core and Worker.

That is a reasonable current choice, but it remains a gap between platform intent and platform execution.

### 5. Planning traceability needed stronger written governance

The repository had substantial planning content, but a durable rule set for keeping docs, GitHub Project cards, sprint placement, and commit history aligned needed to be made explicit.

`docs/planning-governance.md` now improves that, but the long-term value will come from applying it consistently.

### 6. Public REST needed suppression visibility beyond the first explicit binding-plan slice

The explicit DSL is good, and Cephalon now has the normalized internal public REST projection
layer, the resolved runtime catalog, fail-fast route-collision validation, manual module-owned
REST follow-through beneath it, and the first explicit profile-binding-plan slice for
module-owned `MapProfile<TBehavior>()` shorthand.

The newest hardening pass now also rejects invalid explicit binding metadata at build time and
re-checks route-placeholder truth during runtime profile normalization, so the remaining gap is no
longer basic authoring safety.

That first operator-facing visibility gap is now addressed through `ENG-058-T113`, the next
runtime-contract truth gap is now addressed through `ENG-058-T114`, and the next diagnostics-parity
follow-through is now addressed through `ENG-058-T115`: the active
`Cephalon.Behaviors.Http` pack publishes stable `/engine/diagnostics` event ids plus startup log
events for suppression, precedence suppression, applied overrides, no-op override matches, and
preserved fallback answers, while the typed REST runtime contract now also keeps deterministic
remaining request-body fallback visible as
`RestEndpointBindingFallbackMode.PreserveRemainingBodyFallback` on shorthand candidates and
published endpoints instead of leaving that answer in compatibility metadata alone, and emitted
event `5204` logging now follows whichever typed preserved-fallback mode the runtime actually
resolved instead of stopping at the earlier implicit-query-only case. The next contract-hardening
follow-through is now addressed through `ENG-058-T116`: `Cephalon.Abstractions` exposes canonical
wire-name helpers for `RestEndpointBindingFallbackMode`, ASP.NET Core compatibility metadata now
derives `bindingFallbackMode` values from that one public source of truth, and the public XML/docs
wording now describes both preserved implicit-query and remaining-body fallback semantics instead
of the earlier narrower phrasing. That diagnostics family has since also been extended through
`ENG-058-T124`, `ENG-058-T131`, and `ENG-058-T133`, so startup logging now distinguishes
authoring-policy suppression (`5205`), governance-skipped explicit module-DSL ownership (`5206`),
and selected-versus-applied override action dimensions on `5202` / `5203` instead of leaving
those operator answers folded into generic suppression or silence. The next
authoring-safety follow-through is now addressed
through `ENG-058-T117`: malformed `BehaviorRestProfileAttribute.RelativePattern` placeholder syntax
now fails at build time through `ABT0026`, while `BehaviorRestProfileResolver` also parses the
route pattern during runtime normalization even when no explicit binding plan is present so
attribute fallback or stale hints still fail fast before shorthand publication can map an invalid
route. The next governance-targeting follow-through is now addressed through `ENG-058-T118`: host
suppression and override rules can now target the original shorthand `BindingFallbackMode`
identity directly through `BindingFallbackModes`, using the same stable wire names that the typed
runtime contract already publishes, so original-shape selector targeting stays truthful even after
later override actions rewrite the published endpoint.

That next declared-versus-effective override-dimension visibility gap is now addressed through
`ENG-058-T132`: the typed override contract now exposes configured `ActionKinds`, while shorthand
candidate and published endpoint runtime answers now expose `SelectedOverrideActionKinds` plus
`AppliedOverrideActionKinds`, so no-op override winners preserve the override dimensions they
declared without falsely claiming a material runtime rewrite.

That richer authoring/build-time-diagnostics gap is now partially addressed through `ENG-058-T119`:
metadata-only REST profiles can now declare `PreserveImplicitQueryFallback` alongside explicit
bindings, `Cephalon.Behaviors.SourceGen` now rejects missing-binding cases through `ABT0027`, and
`BehaviorRestProfileResolver` re-checks the same rule during runtime fallback so explicit profile
authoring can preserve source implicit query fallback without losing fail-fast truth.

The next governance-targeting follow-through is now addressed through `ENG-058-T120`: host
suppression and override rules can now target the original shorthand explicit binding-plan identity
directly through `TargetBindings`, so route-only and richer explicitly bound candidates can be
governed separately without depending on later rewritten published route shape.

That broader shorthand-source competition visibility gap is now addressed through `ENG-058-T121`:
publication-group runtime answers now also summarize the grouped candidate truth by authoring
style, so operators can see which styles participated, published, or were precedence-suppressed or
governance-suppressed without re-reading the entire candidate set.

That next authoring-policy contract gap is now addressed through `ENG-058-T122`: grouped
publication answers now also expose typed `AuthoringPolicy` data through
`RestEndpointPublicationGroupAuthoringPolicyDescriptor`, `RestApi:AuthoringPolicies:{behaviorId}`
now binds explicit default-versus-configured authoring-policy intent for
`AllowMultiplePublishedCandidates` plus preferred/allowed/disallowed authoring styles, and
`/engine/rest-endpoint-publication-groups` plus `snapshot.RestEndpointPublicationGroups` now
round-trip that policy truth.

That next policy-enforcement gap is now addressed across `ENG-058-T123` and `ENG-058-T124`:
`Cephalon.Behaviors.Http` now honors
`RestApi:AuthoringPolicies:{behaviorId}:AllowMultiplePublishedCandidates` during candidate
resolution, so lower-precedence shorthand candidates can remain published when the grouped
behavior explicitly opts into that outcome and no governance rule suppresses them. The same area
now also enforces `PreferredAuthoringStyle`, `AllowedAuthoringStyles`, and
`DisallowedAuthoringStyles` for shorthand candidates only, keeps explicit module DSL publication
authoritative, and records authoring-policy suppression as a distinct runtime outcome instead of
pretending config governance or candidate precedence caused the same result. Effective endpoint
names are also disambiguated deterministically for co-published shorthand candidates while
`OriginalEndpointName` still preserves the source shorthand lineage.

The remaining gap is now narrower: keep low-code shorthand growth, broader governance, and future
projection sources on the same explicit ownership and runtime-truth model without reintroducing
hidden rule layers.

That next grouped-governance visibility gap is now addressed through `ENG-058-T130`:
`/engine/rest-endpoint-publication-groups` plus `snapshot.RestEndpointPublicationGroups` now also
surface grouped `HostGovernanceEligibleCandidateIds`, `HostGovernanceIneligibleCandidateIds`,
`SkippedSuppressionIds`, and `SkippedOverrideIds` at both the behavior-group level and inside each
authoring-style summary, so operators can confirm that explicit module-owned REST stayed outside
host governance without drilling into the raw candidate catalog first.

That next inline generated-module ergonomics gap is now addressed through `ENG-058-T127`:
`RestBehaviorEngineBuilderExtensions.AddGeneratedRestBehaviorModule<TMarker>(descriptor,
configureGroup?)` now derives the generated behavior-id prefix from `ModuleDescriptor.Id` for the
common inline module case, reuses the same fail-fast deterministic route-group validation as
`GroupFromBehaviorIdPrefix(...)`, and keeps the explicit prefix overload for the cases where module
identity and generated ownership intentionally differ. That keeps lower-ceremony inline module
growth on the same explicit ownership model instead of inventing a new silent projection layer.

## Architecture recommendations

### Next 30 days

- add maturity labels to high-traffic docs so readers can tell stable, adoption-quality, and planned surfaces apart quickly
- keep the showcase proving the database-topology and operator story, but promote only the reusable parts into engine contracts
- keep docs, roadmap, backlog, and GitHub Project history synchronized for meaningful work
- strengthen the "recommended first path" across ASP.NET Core, Entity Framework, Wolverine, and the showcase sample
- keep the new normalized REST endpoint projection layer as the internal source of truth
- keep every future REST authoring path on the shared projection, runtime-catalog, and
  collision-validation pipeline before any new behavior-only shorthand is considered

### Next 60 days

- build cross-pack conformance suites for data, event sourcing, observability, and operator surfaces
- deepen the provider-neutral story around migration, replay, and runtime recovery workflows
- define clearer capability and maturity expectations for any new provider or cloud companion pack

### Next 90 days

- use Phase 12 work to close the migration and coordination story deliberately: strangler fig, feature flags, choreography, and durable execution
- decide how much of the current serverless, edge, and hybrid-cloud story should become first-class runtime-host support versus remaining technology-profile guidance
- keep expanding only where Cephalon can preserve truthful runtime answers, stable docs, and repeatable validation

## What this means for the learning roadmap

The current learning order remains correct:

1. `.NET 10` and runtime fundamentals first
2. architecture and framework design second
3. database topology and provider families third
4. cloud, container, and operations last

That order matches the actual architecture.

Cephalon's center of gravity is not cloud deployment first and it is not provider breadth first. It is explicit runtime design first. The learning path should keep reflecting that truth.

## Bottom line

Cephalon's architectural direction is strong.

The repository already shows the right long-term instincts:

- protect host-agnostic contracts
- make runtime truth introspectable
- keep adapters thin
- grow through additive packs
- prove design decisions in samples, tests, and docs together

The next challenge is not inventing a new center. The next challenge is hardening and aligning the broad surface area that now exists.
