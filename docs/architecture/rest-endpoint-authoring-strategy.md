# Cephalon REST Endpoint Authoring Strategy

Decision baseline date: `April 13, 2026`

Related issue: `ENG-058-T55` / GitHub issue `#313`

Cross-references: `docs/components/behaviors-http.md`, `docs/module-authoring.md`, `docs/architecture.md`, `docs/architecture-review-2026-04.md`, `docs/project-memory.md`

## Purpose

This document captures the recommended long-term model for REST endpoint authoring in Cephalon.

It exists because Cephalon now has a correct explicit public REST path through
`RestBehaviorModuleBase.ConfigureRestBehaviors(...)`, but the next design question is not whether
that path works. The next question is how to reduce ceremony for developers without undoing the
architectural gains that came from making public REST module-owned.

The target outcome is:

- low-code authoring for simple projects
- deterministic ownership for serious systems
- clean separation between behavior logic and public HTTP concerns
- clear override rules
- a path to evolve authoring models without rewriting existing behavior code

## Ground truth from the current repo

The current shipped model is already opinionated:

- public REST is module-owned
- behavior topology is for pattern plus non-REST transports
- `http.rest` is intentionally rejected in behavior transport allowlists and topology
- `RestBehaviorModuleBase` is the canonical authoring path for behavior-backed public REST
- `BehaviorModuleBase` is the canonical path for behavior ownership without public REST
- `Engine:Behaviors:AutoRegister` is an opt-in fallback, not the default behavior-ownership model

That means Cephalon should not go back to a model where `[AppBehavior]` silently publishes a public
REST boundary by default.

If Cephalon adds a lower-ceremony REST path later, it should still require explicit REST opt-in
metadata such as a projection profile or generated module contract. `[AppBehavior]` plus
auto-registration alone must never be enough to publish a public REST surface.

## The three proposed modes

### Mode 1: explicit module-owned REST DSL

Example:

```csharp
public override void ConfigureRestBehaviors(IRestBehaviorModuleBuilder behaviors)
{
    var group = behaviors.Group("/showcase/cart");
    group.MapGet<GetCartBehavior>("/{cartId}");
    group.MapPost<AddToCartBehavior>("/{cartId}/items");
    group.MapDelete<RemoveFromCartBehavior>("/{cartId}/items/{productId}");
    group.MapPost<CheckoutCartBehavior>("/{cartId}/checkout");
}
```

Assessment:

- strongest for deterministic ownership
- strongest for bounded-context clarity
- strongest for operator truth and future introspection
- easy to understand in large systems
- slightly more ceremony than a behavior-only authoring flow

Decision:

- keep this as the canonical Cephalon public REST path
- do not demote it to legacy or escape-hatch status

### Mode 2: behavior-only REST shorthand

Example intent:

```csharp
[AppBehavior("cart.xgetx")]
[BehaviorAllowedPatterns("cqrs")]
[BehaviorAllowedTransports("http.rest")]
public sealed class XxxxBehavior : IAppBehavior<XxInput, XxOutput>
```

Assessment:

- good for low-code DX
- bad if implemented as direct behavior-owned REST activation
- conflicts with the current module-owned public-boundary model
- weakens ownership, route governance, and explicit public-surface review
- risks accidental route publication from auto-registered behaviors

Decision:

- reject direct behavior-owned REST activation as the long-term model
- accept the underlying DX goal, but satisfy it through a lower-level projection system instead

### Mode 3: explicit module mapping overrides behavior shorthand

Assessment:

- this direction is correct
- explicit public-boundary declarations must beat implicit or convention-based projections

Decision:

- keep this rule
- strengthen it into a formal precedence model

## Additional mode Cephalon should add

The repo needs one more mode beyond the three above.

### Mode 4: normalized REST projection descriptors

Cephalon should introduce one normalized descriptor layer for public REST projections.

That layer should be the internal source of truth for:

- HTTP method
- route pattern
- route group or public boundary
- input binding plan
- OpenAPI tag and candidate document version
- authorization or capability hooks that belong to the REST projection

Different authoring styles should compile into the same normalized projection model:

- explicit module DSL
- behavior-authored HTTP profile metadata
- configuration-driven projection overrides
- generated or convention-based module projections

This is the missing abstraction that lets Cephalon support low-code authoring without leaking REST
concerns directly into `IAppBehavior<TIn, TOut>`.

## Recommended long-term engine model

Cephalon should use a four-layer model.

### Layer 1: behavior core

`IAppBehavior<TIn, TOut>` stays transport-neutral.

It should continue to describe:

- behavior identity
- input/output contract
- pattern defaults
- non-REST transport defaults
- generic route-shaped API surface for non-REST HTTP adapters

It should not become the primary place for public REST route ownership.

### Layer 2: optional behavior-authored HTTP profile defaults

If Cephalon adds shorthand REST metadata, it should live in an HTTP or REST companion abstraction,
not in `Cephalon.Abstractions.Behaviors`.

Good candidates:

- a dedicated attribute in an HTTP-facing package
- a static `ConfigureHttp(...)`-style profile method in an HTTP-facing interface
- a source-generator-readable profile contract that emits descriptor data at build time

This layer may declare defaults such as:

- preferred HTTP method
- preferred relative route
- preferred parameter-source hints
- preferred candidate API version

But this layer should still be metadata only. It should not directly publish public REST routes by
itself.

### Layer 3: module-owned public REST projection

`RestBehaviorModuleBase` remains the default public-boundary owner.

For serious systems, this remains the recommended path because it makes the bounded context, route
shape, and public contract explicit in one place.

For lower-ceremony systems, Cephalon can later add a generated or convention-backed module path
that still materializes into module-owned projections at runtime.

### Layer 4: host- or app-level projection overrides

Cephalon should support configuration-driven projection overrides, but not by silently rewriting
every explicit module DSL route by default.

The safe long-term rule is:

- explicit manual module mapping remains authoritative by default
- descriptor-backed convention or generated projections may be overridden from configuration
- explicit code must opt in if it wants host configuration to replace route shape or binding rules

That preserves deterministic ownership and avoids operator-side route surprises.

## Precedence and suppression rules

Cephalon should formalize the following precedence order.

### Ownership precedence

1. explicit `MapAdditionalEndpoints(...)` manual routes
2. explicit `RestBehaviorModuleBase.ConfigureRestBehaviors(...)` routes
3. generated or convention-backed module projections
4. behavior-authored HTTP profile defaults
5. pure convention defaults derived from behavior id and input shape

### Suppression rule

If any higher-precedence layer maps a behavior into public REST, lower-precedence public REST
projections for that behavior should be suppressed automatically unless the higher-precedence layer
explicitly opts into multiple projections.

That means:

- explicit module mapping should suppress implicit behavior-only REST projection for the same behavior
- Cephalon should not run both side by side by default

### Registration precedence

1. explicit module-owned behavior registration
2. fluent explicit registration
3. auto-registration / assembly scanning

This already matches the shipped model and should stay stable.

## Route-collision and runtime-catalog direction

The current repo already prevents duplicate behavior ownership across modules, but it does not yet
have a first-class engine contract for public REST route collisions by resolved `HTTP method + route
pattern`.

That gap becomes more important if Cephalon later adds generated, shorthand, or convention-backed
REST projections.

The long-term model should therefore add two things before broad shorthand publication is enabled:

- a normalized runtime catalog of resolved REST projections
- fail-fast collision validation for public REST projections unless a higher-precedence mapping
  explicitly opts into a deliberate alias

Recommended runtime surface:

- `IRestEndpointRuntimeCatalog`
- `/engine/rest-endpoints`
- `snapshot.RestEndpoints`

Each resolved projection should be able to answer at least:

- source kind such as `manual`, `module-dsl`, `generated-module`, `behavior-profile`, or
  `convention`
- source id or owner id
- module id when a real module owns the projection
- behavior id when the endpoint dispatches through `BehaviorDispatcher`
- HTTP method
- final route pattern
- candidate OpenAPI document or API version
- whether the projection suppressed lower-precedence candidates

This keeps shorthand authoring compatible with Cephalon's broader requirement that runtime policy,
composition, and public surface decisions stay introspectable.

## Input binding direction

The current REST helper composes route values, query-string values, and JSON bodies into one JSON
payload by name.

That is a reasonable baseline, but it is too implicit for the long-term engine contract because
collisions can be surprising and the source of each input field is not explicit enough.

The long-term projection model should support explicit binding descriptors for:

- route
- query
- header
- body

Recommended future rule:

- explicit binding metadata beats inference
- route placeholders can infer route-bound properties when names match
- for `GET` and `DELETE`, complex-body binding should stay off by default
- for `POST`, `PUT`, and `PATCH`, route and explicit query/header bindings should be resolved first,
  with remaining complex fields coming from body
- field-source conflicts should fail fast instead of silently merging one source over another
- shorthand REST publication should require an explicit HTTP method selection and should not infer a
  public verb only from behavior-id naming conventions

## OpenAPI version direction

Cephalon should keep two concerns separate.

### Concern 1: endpoint candidate version

The REST projection selects the candidate API version or document membership for an endpoint.

That can come from:

- explicit module `.ApiVersion(...)`
- future behavior-authored HTTP profile defaults
- future configuration-driven projection overrides

### Concern 2: host published documents

The host still decides which documents are published through:

- `OpenApi:EnabledVersions`
- `OpenApi:DefaultVersion`
- legacy `OpenApi:Documents` / `OpenApi:DefaultDocument`

That allow-list remains authoritative and must stay separate from endpoint authoring metadata.

If the same behavior needs multiple public API versions simultaneously, model that as multiple
explicit projections or versioned modules. Do not hide multi-version public contracts behind one
implicit behavior-level flag until behavior identity and versioned transport-surface semantics are
reworked deliberately.

## Why this model is better than direct REST on `IAppBehavior`

It preserves the current architectural wins:

- public REST remains a public-boundary concern
- behavior logic stays portable across REST, GraphQL, JSON-RPC, SSE, WebSocket, gRPC, and messaging
- non-REST route-shaped adapters keep using `BehaviorApiSurfaceDescriptor`
- low-code authoring can exist without making every behavior an accidental public API
- future source generation can emit descriptor data and binder plans without runtime reflection

It also gives Cephalon a better portability story:

- teams can keep behavior code stable
- teams can switch route shape, grouping, or public-boundary layout through descriptor-backed
  authoring instead of editing handler logic
- future architecture or app-shape changes can happen at the projection layer rather than inside
  behavior implementations

## Recommended migration path

Cephalon should implement this in five steps.

### Step 1: normalize the public REST projection contract

Add a normalized descriptor model and make the current module DSL compile into it internally.

### Step 2: add resolved REST endpoint catalog and collision validation

Add a runtime catalog and fail-fast duplicate-route validation over the normalized projection model
so future shorthand or generated projections cannot create silent public-route ambiguity.

### Step 3: add build-time diagnostics and source-generated profile support

If behavior-authored HTTP profiles are added, validate them at build time and emit normalized
descriptor data alongside existing behavior registration hints.

### Step 4: add generated or convention-backed module projections

Allow low-code projects to opt into convention REST mapping without abandoning module-owned public
boundaries.

### Step 5: add explicit input-binding descriptors and controlled configuration overrides

Finish the model by making binding plans explicit and letting descriptor-backed routes opt into
configuration override behavior where that flexibility is worth the complexity.

## What should be stored as project memory

The following points are durable enough to keep outside thread-local context.

- public REST stays module-owned even if Cephalon later adds shorthand REST authoring
- future shorthand REST authoring should be metadata or projection driven, not direct behavior-owned
  route activation
- explicit module-owned REST mappings suppress lower-precedence implicit or convention projections
  for the same behavior by default
- low-code REST authoring should stay opt-in and should prefer source-generated descriptor material
  over broad runtime reflection
- `[AppBehavior]` plus auto-registration alone must still not publish a public REST boundary
- before broad shorthand or convention REST publication is enabled, Cephalon should add a normalized
  runtime catalog plus fail-fast route-collision validation for resolved public REST projections
- future agentic, AI, or multi-platform expansion should not outrun core engine contract quality,
  performance, security, and maintainability

## Near-term follow-through candidates

Recommended implementation sequence after this design slice:

1. introduce the normalized REST projection descriptor contract and refactor the current DSL to use it
2. add a resolved REST endpoint runtime catalog plus fail-fast route-collision validation
3. add diagnostics and source-generator support for future HTTP profile metadata
4. add explicit input-binding descriptors and conflict validation
5. add a generated or convention-backed low-code module path
6. only then evaluate whether richer configuration-driven public-boundary overrides are worth the added complexity
