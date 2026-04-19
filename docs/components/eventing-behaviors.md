# Cephalon.Eventing.Behaviors

`Cephalon.Eventing.Behaviors` is the explicit bridge package that routes behavior-authored saga choreography publications into the shared `Cephalon.Eventing` publish path.

## What it owns

- keeps `ISagaChoreographyPublisher` ownership in `Cephalon.Behaviors.Patterns` while still allowing choreography steps to stage durable publications through the eventing stack
- provides `AddBehaviorEventingBridge()` so hosts can opt into the bridge deliberately instead of receiving it through ambient registration
- contributes module `eventing-behaviors` only when `EventDrivenIntegration` is selected
- preserves explicit `ISagaChoreographyPublisher` registrations instead of overwriting consumer-owned choreography publishers
- replaces the in-memory fallback choreography publisher only when the shared eventing publish path is actually available
- projects capability `eventing.behaviors.saga-choreography` so operators can see when the engine-managed bridge is active
- leaves `behaviors.saga-choreography.runtime-catalog` and `behaviors.saga-choreography.publication-state` capability ownership with `Cephalon.Behaviors` when `AddBehaviorPatterns()` activates those shared pattern surfaces
- contributes the `saga-choreography-bridges` runtime surface under `event-driven-integration`
- enriches bridged `EventPublication` metadata with stable saga-choreography provenance keys instead of inventing a second publish contract

## Main surfaces

- `Modules/BehaviorEventingModule.cs`
- `Registration/BehaviorEventingEngineBuilderExtensions.cs`
- `Services/EventingSagaChoreographyPublisher.cs`
- `Services/BehaviorEventingRuntimeSurfaceContributor.cs`

## How it fits

This package exists to keep both sides honest. `Cephalon.Behaviors.Patterns` still owns
choreography authoring, the transport-neutral `ISagaChoreographyPublisher` contract, higher-level
authoring helpers such as `ISagaEventReactor<TEvent>`, and typed output/publication helpers such as
`ISagaChoreographyStepResult`, `SagaChoreographyStepResult<TOutput>`, and
`SagaChoreographyPublication.CreateJson(...)`. `Cephalon.Eventing` still owns channels, staged
publication, outbox handoff, and event-driven runtime introspection. `Cephalon.Eventing.Behaviors`
is the explicit handoff between those two worlds when a host wants saga choreography steps to
stage durable publications through the same outbox-backed eventing path used elsewhere in the app.

That separation matters because Cephalon does not want the behavior-pattern layer to hard-depend on an eventing technology pack, and it also does not want `Cephalon.Eventing` to claim ownership of behavior choreography semantics that originate elsewhere. The bridge therefore stays opt-in through `AddBehaviorEventingBridge()`, activates only when `EventDrivenIntegration` is selected, and validates that the shared eventing publish path is real before it accepts choreography publications. If a host or consumer has already registered its own non-default `ISagaChoreographyPublisher`, the bridge leaves that choice alone and does not publish an engine-managed bridge capability or runtime-surface entry.

When the bridge is active, `EventingSagaChoreographyPublisher` creates a scope per publication, resolves `IEventPublisher`, maps `SagaChoreographyPublication` into the shared `EventPublication` contract, and annotates the staged metadata with `cephalon.pattern = saga-choreography`, `cephalon.publisherBridge = eventing.behaviors`, and `cephalon.isCompensation = true|false`. Runtime truth stays explicit through the `eventing.behaviors.saga-choreography` capability plus the `saga-choreography-bridges` surface, which tell operators that choreography handoff is engine-managed, explicitly activated, and durability-dependent on the same outbox-backed eventing publish path. That bridge truth is additive over the static choreography runtime catalog exposed through `ISagaChoreographyRuntimeCatalog`, `snapshot.SagaChoreographies`, and `/engine/saga-choreographies`, over the live choreography publication-state surface exposed through `ISagaChoreographyPublicationRuntimeStateCatalog`, `snapshot.SagaChoreographyPublicationStates`, and `/engine/saga-choreographies/runtime*`, and over the module-backed `behaviors.saga-choreography.runtime-catalog` plus `behaviors.saga-choreography.publication-state` capabilities surfaced through `/engine/capabilities`: the static choreography catalog answers ownership and contract shape, the choreography publication-state surface answers the latest shared-strategy handoff observations, the module-backed capabilities answer whether those shared pattern surfaces are active at all, this bridge surface answers whether eventing owns the effective durable handoff, and downstream event-dispatch runtime surfaces answer later broker or dispatcher progress.

## Related docs

- [Cephalon.Behaviors.Patterns](behaviors-patterns.md)
- [Cephalon.Eventing](eventing.md)
- [Cephalon.Eventing.Wolverine](eventing-wolverine.md)
- [Technology packs](../technology-packs.md)
