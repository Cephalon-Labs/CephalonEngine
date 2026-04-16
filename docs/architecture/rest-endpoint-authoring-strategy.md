# Cephalon REST Endpoint Authoring Strategy

Decision baseline date: `April 14, 2026`

Related issues: `ENG-058-T55` / GitHub issue `#313`, `ENG-058-T56` / GitHub issue `#314`, `ENG-058-T57` / GitHub issue `#318`, `ENG-058-T58` / GitHub issue `#320`, `ENG-058-T61` / GitHub issue `#324`, `ENG-058-T62` / GitHub issue `#325`, `ENG-058-T63` / GitHub issue `#326`, `ENG-058-T64` / GitHub issue `#327`, `ENG-058-T65` / GitHub issue `#329`, `ENG-058-T66` / GitHub issue `#331`, `ENG-058-T67` / GitHub issue `#332`, `ENG-058-T68` / GitHub issue `#333`, `ENG-058-T69` / GitHub issue `#334`, `ENG-058-T70` / GitHub issue `#335`, `ENG-058-T71` / GitHub issue `#336`, `ENG-058-T72` / GitHub issue `#337`, `ENG-058-T73` / GitHub issue `#338`, `ENG-058-T74` / GitHub issue `#339`, `ENG-058-T75` / GitHub issue `#340`, `ENG-058-T76` / GitHub issue `#341`, `ENG-058-T77` / GitHub issue `#342`, `ENG-058-T78` / GitHub issue `#343`, `ENG-058-T79` / GitHub issue `#344`, `ENG-058-T80` / GitHub issue `#345`, `ENG-058-T81` / GitHub issue `#346`, `ENG-058-T82` / GitHub issue `#347`, `ENG-058-T83` / GitHub issue `#348`, `ENG-058-T84` / GitHub issue `#349`

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
- the current module-owned REST DSL now compiles into one normalized internal projection contract
  before ASP.NET Core materializes route groups and endpoints

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

Status update:

- Step 1 of this direction is now shipped internally through the current `RestBehaviorModuleBase`
  DSL, which compiles into a normalized projection contract plus a dedicated materializer
- Step 2 of this direction is now shipped through `IRestEndpointRuntimeCatalog`,
  `/engine/rest-endpoints`, `/engine/rest-endpoints/{restEndpointId}`, `snapshot.RestEndpoints`,
  and fail-fast collision validation on the resolved public `HTTP method + route pattern`
- Step 3 is now shipped through `BehaviorRestProfileAttribute`, `BehaviorRestMethod`,
  `BehaviorRestProfileDescriptor`, source-generated `GetRestProfiles()` hints, and
  `ABT0015` through `ABT0025`, while still keeping those behavior-authored REST profiles
  metadata-only rather than publishing public REST directly from behaviors
- Step 4 is now partially shipped through `IRestBehaviorEndpointGroupBuilder.MapProfile<TBehavior>()`,
  which consumes those generated hints through the same explicit module-owned DSL, prefers
  source-generated `GetRestProfiles()` material, falls back only to the explicitly targeted
  behavior type when generated hints are unavailable, and keeps runtime publication on the existing
  `module-dsl` path with additive `authoringStyle = behavior-module-profile` metadata
- the next high-value follow-through was tightening the normalized projection model with explicit
  binding descriptors before any broader convention-backed shorthand publication was considered
- that explicit-binding slice is now shipped through `ENG-058-T63`: profile metadata can carry
  explicit route/query/header/body binding descriptors, module-owned `MapProfile<TBehavior>()`
  consumes them through the same normalized projection pipeline, request composition now treats
  those descriptors as overrides instead of an exclusive mode, and the runtime catalog initially
  surfaced the resolved plan through additive `bindingDescriptors` metadata
- the next hardening slice is now also shipped through `ENG-058-T64`: `Cephalon.Behaviors.SourceGen`
  rejects invalid explicit binding metadata at build time through `ABT0019` through `ABT0025`,
  generated `GetRestProfiles()` output no longer depends on hard-coded enum ordinals, and
  `BehaviorRestProfileResolver` now fails fast when an explicit route binding names a placeholder
  that the declared profile route pattern does not contain
- the next runtime-contract follow-through is now shipped through `ENG-058-T65`: the engine-owned
  transport contract now publishes explicit binding plans through
  `RestEndpointRuntimeDescriptor.BindingDescriptors`, `RestEndpointBindingDescriptor`, and
  `RestEndpointBindingSource`, while the ASP.NET Core host no longer duplicates that plan inside
  `metadata.bindingDescriptors`
- the next precedence-visibility follow-through is now shipped through `ENG-058-T66`: the runtime
  now exposes `IRestEndpointCandidateRuntimeCatalog`, `GET /engine/rest-endpoint-candidates`,
  `GET /engine/rest-endpoint-candidates/{candidateId}`, and `snapshot.RestEndpointCandidates`, so
  operators can see both published and suppressed module-owned REST candidates, while explicit
  module DSL mappings now suppress lower-precedence profile shorthand for the same behavior by
  default and surface the winning candidate id plus suppression reason explicitly
- the next original-versus-effective shorthand visibility follow-through is now shipped through
  `ENG-058-T78`: the runtime now also exposes
  `RestEndpointCandidateProjectionDescriptor` plus
  `RestEndpointCandidateRuntimeDescriptor.OriginalProjection` so operators can compare the original
  shorthand route, method, version, route-group prefix, relative pattern, and binding plan against
  the final effective `ProjectedEndpoint` shape without inferring that source truth back out of the
  mapped endpoint
- the next shorthand binding-governance follow-through is now shipped through `ENG-058-T79`:
  `Cephalon.Abstractions` now also exposes typed
  `RestEndpointOverrideBindingMode` through the override runtime contract, `RestApi:Overrides`
  can now declare `BindingMode = MergeExplicit` when a host wants to upsert only selected explicit
  bindings by property name instead of restating the whole explicit plan, the existing
  `ReplaceExplicit` behavior remains the default when `BindingMode` is omitted, and the runtime now
  keeps that merge-versus-replace governance truth visible through `/engine/rest-endpoint-overrides`
  plus `snapshot.RestEndpointOverrides`
- the next grouped publication-visibility follow-through is now shipped through `ENG-058-T80`:
  `Cephalon.Abstractions` now also exposes `IRestEndpointPublicationGroupRuntimeCatalog` plus
  `RestEndpointPublicationGroupDescriptor`, `Cephalon.AspNetCore` now publishes
  `/engine/rest-endpoint-publication-groups`,
  `/engine/rest-endpoint-publication-groups/{behaviorId}`, and
  `snapshot.RestEndpointPublicationGroups`, and the host now groups the existing candidate-level
  truth per behavior so operators can inspect published candidates, precedence-suppressed
  candidates, governance-suppressed candidates, the winning precedence rank when one exists, and
  the ordered candidate set without manually joining several runtime surfaces
- the next low-code inline module-owned authoring follow-through is now shipped through
  `ENG-058-T81`: `Cephalon.Behaviors.Http` now exposes
  `RestBehaviorEngineBuilderExtensions.AddRestBehaviorModule<TMarker>()`, which lets a host
  register a real module-owned REST surface without a dedicated `RestBehaviorModuleBase`
  subclass, still drives the same normalized projection/materialization/candidate/governance
  pipeline as the class-based DSL, uses `TMarker` both as the reusable helper's distinct module
  type identity and as the source assembly marker for `MapGeneratedProfiles(...)`, and still never
  publishes public REST from `[AppBehavior]` alone
- the next low-code generated module-path follow-through is now shipped through `ENG-058-T84`:
  `IRestBehaviorModuleBuilder.GroupFromBehaviorIdPrefix(...)` now derives a deterministic route
  group such as `/showcase/cart` from `showcase.cart`, and
  `RestBehaviorEngineBuilderExtensions.AddGeneratedRestBehaviorModule<TMarker>()` now wraps that
  same derivation for the common inline generated-profile case so the engine no longer requires
  authors to repeat both the route path and the behavior-id prefix manually while still using the
  same module-owned projection/materialization/candidate/governance pipeline and still never
  publishing public REST from `[AppBehavior]` alone
- the next stable shorthand candidate-id governance follow-through is now shipped through
  `ENG-058-T85`: shorthand candidate ids now resolve from the original shorthand projection before
  host-level overrides are applied, `RestApi:Suppressions` and `RestApi:Overrides` now also accept
  exact `CandidateIds`, runtime suppression/override catalogs plus the snapshot now surface those
  configured candidate ids directly, and selector specificity now prefers candidate-targeted rules
  before broader behavior/module selector matches while `ProjectedEndpoint` continues to answer the
  final effective mapped route shape
- the next shorthand projected-endpoint metadata alignment follow-through is now shipped through
  `ENG-058-T86`: shorthand candidates now project the same endpoint name, summary, and description
  metadata that the final behavior-backed runtime endpoint exposes, using the same
  operation-name/documentation conventions during candidate resolution so
  `/engine/rest-endpoint-candidates` and `/engine/rest-endpoints` stay aligned for operator-facing
  metadata truth
- the next shorthand governance-match visibility follow-through is now shipped through
  `ENG-058-T87`: shorthand candidates now also keep the full ordered suppression-rule and
  override-rule match sets visible through `MatchedSuppressionIds` and `MatchedOverrideIds` before
  `SuppressedBySuppressionId` or `AppliedOverrideId` identifies the selected winner, so
  overlapping host governance stays operator-visible without changing the existing specificity
  model
- the next bounded shorthand route-group-prefix override follow-through is now shipped through
  `ENG-058-T82`: the typed override/runtime contracts plus ASP.NET Core config binding now support
  shorthand-only `RouteGroupPrefix`, that remap must stay beneath the active REST root, cannot
  declare route placeholders, cannot silently change effective API-version truth, and now causes
  the ASP.NET Core materializer to split effective shorthand route groups when only some
  candidates in one authored group are remapped so actual HTTP routes, runtime catalogs, and
  snapshots all report the same published answer
- the next shorthand explicit-binding withdrawal follow-through is now shipped through
  `ENG-058-T83`: the typed override/runtime contracts plus ASP.NET Core config binding now also
  support `RemovedBindingProperties`, `MergeExplicit` now covers both property-by-property explicit
  binding upserts and explicit-binding withdrawals, removal-only rules normalize to merge mode
  automatically, `ReplaceExplicit` cannot pair with removals, a merge rule cannot both remove and
  override the same property, removal targets must already exist in the source shorthand explicit
  binding plan, and `/engine/rest-endpoint-overrides` plus `snapshot.RestEndpointOverrides` now
  keep both the typed binding mode and removed-property list visible
- the next low-code generated module-owned shorthand is now shipped through `ENG-058-T67`:
  `IRestBehaviorEndpointGroupBuilder.MapGeneratedProfiles()` and
  `MapGeneratedProfiles(string behaviorIdPrefix)` let an owning module opt into profile-backed
  generated publication for one owned route group, prefer source-generated `GetRestProfiles()` plus
  `GetRestProfileBehaviorTypes()` hints, fall back only to a bounded scan of the explicit owning
  module assembly when generated type hints are unavailable, and keep runtime publication on the
  same normalized projection and candidate-catalog path with
  `metadata.authoringStyle = behavior-module-generated`
- the first host-governance slice is now shipped through `ENG-058-T68`: ASP.NET Core hosts can
  suppress descriptor-backed shorthand candidates through `RestApi:Suppressions`, the runtime now
  exposes those configured rules through `IRestEndpointSuppressionRuntimeCatalog`,
  `/engine/rest-endpoint-suppressions`, and `snapshot.RestEndpointSuppressions`, and suppressed
  candidates now distinguish governance suppression through
  `RestEndpointCandidateRuntimeDescriptor.SuppressedBySuppressionId` while
  `RestEndpointCandidateRuntimeDescriptor.MatchedSuppressionIds` keeps every matching rule visible
  in specificity order
- the first constrained shorthand-override slices are now shipped through `ENG-058-T69`,
  `ENG-058-T70`, `ENG-058-T71`, `ENG-058-T72`, `ENG-058-T73`, `ENG-058-T74`, `ENG-058-T75`,
  `ENG-058-T76`, `ENG-058-T82`, and `ENG-058-T83`:
  ASP.NET Core hosts can retarget
  descriptor-backed shorthand candidates through `RestApi:Overrides` when they need a different
  effective `ApiVersionMajor`, HTTP `Method`, bounded published `RouteGroupPrefix`, constrained
  relative `Pattern`, and/or explicit binding plan; the runtime now exposes those configured rules
  through
  `IRestEndpointOverrideRuntimeCatalog`, `/engine/rest-endpoint-overrides`, and
  `snapshot.RestEndpointOverrides`; candidates now surface the governing rule through
  `RestEndpointCandidateRuntimeDescriptor.AppliedOverrideId` while
  `RestEndpointCandidateRuntimeDescriptor.MatchedOverrideIds` keeps every matching rule visible in
  specificity order; the normalized materializer now maps the same effective projection shape that
  the runtime catalogs report; explicit binding-plan
  overrides now default to replacing the shorthand candidate's explicit descriptors, but can also
  merge explicit binding upserts and withdrawals by property name through
  `BindingMode = MergeExplicit` plus `RemovedBindingProperties` while still leaving unbound route
  placeholders and remaining request-body fields available for deterministic fallback; placeholder
  renames can now also apply when the effective explicit route-binding plan
  covers the renamed placeholder set exactly; placeholder removals can now also apply when the
  original projection already exposes explicit route-binding coverage for the original placeholder
  set and the effective explicit binding plan keeps every affected original route-bound property
  explicitly bound; placeholder additions can now also apply when the effective explicit
  route-binding plan covers the full final placeholder set and every newly route-bound property was
  either already explicitly bound in the original projection or, for `POST`/`PUT`/`PATCH`, already
  part of the original deterministic remaining-body fallback surface; and invalid effective
  method-plus-binding plans, rename attempts that still rely on inference, removal attempts that
  rely on inferred original route coverage or drop explicit binding coverage, or addition attempts
  that would promote any other implicit property into the public route now fail fast
- the selector-expansion follow-through is now shipped through `ENG-058-T77`: both
  `RestApi:Suppressions` and `RestApi:Overrides` can refine `Behaviors`/`Modules` targeting with
  `ApiVersionMajors`, `Methods`, `RelativePatterns`, and `RouteGroupPrefixes`; those selectors
  match the original shorthand candidate shape before override actions are applied, suppression now
  preserves that same original-shape contract even when an override later rewrites the final
  published endpoint, rule specificity now also considers populated selector dimensions plus
  narrower selector sets, and the runtime suppression/override catalogs now expose the selector
  arrays directly
- broader configuration-driven projection overrides that promote implicit properties into route
  placeholders beyond that constrained body-fallback path, or rewrite binding shape beyond that
  constrained explicit-binding upsert-plus-withdraw model, remain later work

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

Current shipped follow-through:

- `RestBehaviorModuleBase` remains the class-based path for dedicated module types
- `AddRestBehaviorModule<TMarker>()` is now the lowest-ceremony explicit module-registration path
  for straightforward hosts, but it still lands on this same layer instead of inventing a new
  publication source
- `GroupFromBehaviorIdPrefix(...)` and `AddGeneratedRestBehaviorModule<TMarker>()` now cover the
  common generated-profile case where route-group path and behavior-id prefix should mirror one
  another, again without inventing a new publication source

### Layer 4: host- or app-level projection overrides

Cephalon should support configuration-driven projection overrides, but not by silently rewriting
every explicit module DSL route by default.

The safe long-term rule is:

- explicit manual module mapping remains authoritative by default
- descriptor-backed convention or generated projections may be overridden from configuration
- explicit code must opt in if it wants host configuration to replace route shape or binding rules

That preserves deterministic ownership and avoids operator-side route surprises.

Current shipped baseline:

- `RestApi:Suppressions` and `RestApi:Overrides` are the first ASP.NET Core host-governance
  surfaces
- they apply only to descriptor-backed shorthand candidates such as `MapProfile<TBehavior>()` and
  `MapGeneratedProfiles(...)`
- suppression runs before precedence resolution rather than silently rewriting candidates
- override currently supports `ApiVersionMajor`, `Method`, bounded published `RouteGroupPrefix`,
  relative `Pattern`, explicit `Bindings`, `RemovedBindingProperties`, and typed `BindingMode`
- both rule families fail fast when a rule omits both `Behaviors` and `Modules`
- override rules also fail fast when they omit all override actions, use a non-positive
  `ApiVersionMajor`, declare an unsupported HTTP method, declare an invalid relative route
  pattern, or declare an invalid `RouteGroupPrefix`
- both rule families can refine `Behaviors`/`Modules` targeting with `ApiVersionMajors`,
  `Methods`, `RelativePatterns`, and `RouteGroupPrefixes`, and those selector refiners match the
  original shorthand candidate shape before override actions are applied
- when more than one rule matches, the host prefers the more specific rule deterministically by
  populated target dimensions first, then by behavior-targeted scope, narrower authoring-style
  scope, fewer total selector values, and finally stable rule-id ordering
- neither surface overrides explicit module DSL or manual module-owned REST endpoints
- shorthand groups that declare `.ApiVersion(...)` explicitly stay authoritative over host version
  rewrites, while shorthand method and constrained pattern overrides can still apply to those same
  groups
- the current override slice can rewrite the effective API major version, HTTP method, bounded
  published route-group prefix, constrained relative route pattern, and/or explicit binding plan
  while keeping the `/api/v{major}` route segment, published group boundary, OpenAPI document
  name, mapped endpoint, and runtime catalogs aligned to the same effective projection
- `RouteGroupPrefix` rewrites stay beneath the active REST root, cannot declare placeholders,
  cannot silently change effective API-version truth, and now make ASP.NET Core split effective
  shorthand groups when only some candidates in one authored group are remapped
- `OpenApi:EnabledVersions` and legacy document config still decide which documents are actually
  published
- pattern rewrites preserve the placeholder set by default and can now also rename placeholders
  when the effective explicit route-binding plan covers the renamed placeholder set exactly
- placeholder removals can now also apply when the original projection already exposes explicit
  route-binding coverage for the original placeholder set and the effective explicit binding plan
  keeps every affected original route-bound property explicitly bound
- placeholder additions can now also apply when the effective explicit route-binding plan covers
  the full final placeholder set and every newly route-bound property was either already explicitly
  bound in the original projection or, for `POST`/`PUT`/`PATCH`, already part of the original
  deterministic remaining-body fallback surface
- `BindingMode = MergeExplicit` can now upsert changed explicit bindings and withdraw selected
  original explicit bindings through `RemovedBindingProperties`, while failing fast if a removal
  targets a property the source shorthand never bound explicitly or if one merge rule both removes
  and overrides the same property
- broader implicit-property promotion beyond that constrained body-fallback path plus broader
  input-binding rewrites beyond the current replace-plus-merge-explicit upsert-plus-withdraw model
  remain later work

## Precedence and suppression rules

Cephalon should formalize the following precedence order.

### Ownership precedence

1. explicit `MapAdditionalEndpoints(...)` manual routes
2. explicit `RestBehaviorModuleBase.ConfigureRestBehaviors(...)` DSL routes such as `MapGet/MapPost/...`
3. explicit profile-consumption shorthand through `MapProfile<TBehavior>()`
4. explicit generated module shorthand through `MapGeneratedProfiles(...)`
5. pure convention defaults derived from behavior id and input shape

The shipped runtime-catalog baseline now covers both the explicit module DSL and explicit manual
module-owned REST paths. That means precedence affects publication, but the winning manual or DSL
route still flows through the same operator-facing catalog and duplicate-route guard once ASP.NET
Core materializes the final endpoints.

The shipped precedence-visibility baseline now also makes that suppression decision observable for
module-owned normalized behavior projections. Within that behavior-projection path, the explicit
module DSL wins over both `MapProfile<TBehavior>()` and `MapGeneratedProfiles(...)`, and
`MapProfile<TBehavior>()` in turn wins over generated shorthand for the same behavior. The runtime
keeps all three candidates visible through the candidate catalog instead of hiding the winning
decision as silent startup behavior.

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

The current repo already prevents duplicate behavior ownership across modules, and it now also has
a first-class runtime contract for resolved public REST truth plus fail-fast route-collision
validation.

That shipped baseline matters because future generated, shorthand, or convention-backed REST
projections now have one operator-facing surface and one collision-policy pipeline to extend instead
of inventing their own route publication rules.

Recommended runtime surface:

- `IRestEndpointRuntimeCatalog`
- `/engine/rest-endpoints`
- `/engine/rest-endpoints/{restEndpointId}`
- `snapshot.RestEndpoints`
- `IRestEndpointCandidateRuntimeCatalog`
- `/engine/rest-endpoint-candidates`
- `/engine/rest-endpoint-candidates/{candidateId}`
- `snapshot.RestEndpointCandidates`
- `IRestEndpointPublicationGroupRuntimeCatalog`
- `/engine/rest-endpoint-publication-groups`
- `/engine/rest-endpoint-publication-groups/{behaviorId}`
- `snapshot.RestEndpointPublicationGroups`
- fail-fast startup validation on duplicate resolved public `HTTP method + route pattern`

The shipped baseline now answers at least:

- source kind such as `manual` or `module-dsl`
- module id when a real module owns the projection
- behavior id when the endpoint dispatches through `BehaviorDispatcher`
- HTTP method
- final route pattern
- candidate OpenAPI document or API version
- additive metadata such as the route-group prefix, relative pattern, and authoring style
- for shorthand candidate entries, both the original source projection and the final effective
  mapped endpoint shape

That answer now covers both projection-backed module DSL routes and explicit manual module-owned
REST routes published through `IRestModule`, legacy `IEndpointModule`, or
`RestBehaviorModuleBase.MapAdditionalEndpoints(...)`.

The same shipped baseline now also answers precedence visibility for module-owned shorthand
projections:

- published versus suppressed candidate status
- candidate authoring style and precedence rank
- the winning candidate id when suppression occurs
- an operator-facing suppression reason
- the projected endpoint shape each candidate would publish if it won
- the full ordered suppression-rule and override-rule match sets when overlapping governance rules
  target the same shorthand candidate
- the grouped published-versus-suppressed answer per behavior, including published candidate ids,
  precedence-suppressed candidate ids, governance-suppressed candidate ids, the winning precedence
  rank when one exists, and the ordered candidate set

Broader generated or convention-backed low-code projections should extend that same candidate
surface plus grouped publication answer instead of inventing a second precedence-answer model.

This keeps shorthand authoring compatible with Cephalon's broader requirement that runtime policy,
composition, and public surface decisions stay introspectable.

## Input binding direction

The current REST helper composes route values, query-string values, and JSON bodies into one JSON
payload by name.

That is a reasonable baseline, but it is too implicit for the long-term engine contract because
collisions can be surprising and the source of each input field is not explicit enough.

The long-term projection model now has a first shipped explicit-binding baseline through repeated
`BehaviorRestBindingAttribute` declarations on a behavior profile. That baseline feeds
`BehaviorRestProfileDescriptor.Bindings`, source-generated `GetRestProfiles()` hints, explicit
module-owned `MapProfile<TBehavior>()` consumption, and the engine-owned
`RestEndpointRuntimeDescriptor.BindingDescriptors` runtime contract.

The current binding-descriptor baseline supports:

- route
- query
- header
- body

Current rule:

- explicit binding metadata beats inference
- explicit bindings currently require object inputs; scalar inputs still use the existing scalar
  REST binder path
- route placeholders can still infer unbound route-bound properties when names match
- explicit route bindings must name placeholders that actually exist in the declared
  `BehaviorRestProfileAttribute` pattern
- for `GET` and `DELETE`, explicit body bindings are rejected
- for `POST`, `PUT`, and `PATCH`, explicit route/query/header/body bindings resolve first, and the
  JSON body can still fill remaining unbound object properties
- body values that target a property already reserved by an explicit non-body binding fail fast
  instead of silently overwriting the explicit source
- build-time diagnostics now reject invalid binding sources, missing or duplicate input-property
  targets, scalar-input binding misuse, body bindings on non-body verbs, and route-placeholder
  mismatches before `GetRestProfiles()` is generated
- shorthand REST publication still requires an explicit HTTP method selection and does not infer a
  public verb only from behavior-id naming conventions
- the shipped constrained host-level binding override baseline now supports both full explicit-plan
  replacement and typed `MergeExplicit` property-by-property binding patches plus withdrawals,
  still leaves unbound route placeholders and remaining request-body fields available for
  deterministic fallback, and still fails fast when the effective method-plus-binding plan is
  invalid
- broader configuration-driven binding overrides beyond that explicit-binding upsert-plus-withdraw
  model remain later work

## OpenAPI version direction

Cephalon should keep two concerns separate.

### Concern 1: endpoint candidate version

The REST projection selects the candidate API version or document membership for an endpoint.

That can come from:

- explicit module `.ApiVersion(...)`
- future behavior-authored HTTP profile defaults
- configuration-driven projection overrides

### Concern 2: host published documents

The host still decides which documents are published through:

- `OpenApi:EnabledVersions`
- `OpenApi:DefaultVersion`
- legacy `OpenApi:Documents` / `OpenApi:DefaultDocument`

That allow-list remains authoritative and must stay separate from endpoint authoring metadata.

The shipped configuration-driven override surface is still intentionally narrow: `RestApi:Overrides`
can target the original shorthand candidate shape through `ApiVersionMajors`, `Methods`,
`RelativePatterns`, and `RouteGroupPrefixes`, then change the effective shorthand candidate
`ApiVersionMajor`, HTTP `Method`, bounded published `RouteGroupPrefix`, relative `Pattern`,
and/or explicit `Bindings` plus `RemovedBindingProperties`, but the current route-pattern slice is
still constrained enough to keep binding semantics truthful. Cephalon therefore keeps the
route-version segment and document name
together for version rewrites, allows route-pattern rewrites when they preserve the same
placeholder set or when the effective explicit route-binding plan covers a renamed placeholder set
exactly, lets binding overrides either replace the shorthand candidate's explicit binding plan or
merge explicit binding upserts and withdrawals into it by property name while preserving
deterministic fallback for unbound route placeholders and remaining request-body fields, allows
route-pattern rewrites that remove placeholders when the original projection already
exposes explicit route-binding coverage for the original placeholder set and the effective explicit
binding plan keeps every affected original route-bound property explicitly bound, allows
route-pattern rewrites that add placeholders when the effective explicit route-binding plan covers
the full final placeholder set and every newly route-bound property was either already explicitly
bound in the original projection or, for `POST`/`PUT`/`PATCH`, already part of the original
deterministic remaining-body fallback surface, allows `RouteGroupPrefix` rewrites only when the
published group stays beneath the active REST root, contains no placeholders, and does not
silently change effective API-version truth, now makes ASP.NET Core materialize split effective
route groups when one authored shorthand group fans out to more than one published group, and
does not yet support route rewrites that promote other implicit properties into placeholders or
broader host-level binding rewrites beyond the current replace-plus-merge-explicit
upsert-plus-withdraw model that would silently change how one shorthand endpoint reads its input.

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

Status:

- shipped through `ENG-058-T56`; the current module DSL now compiles into a normalized internal
  projection model before route materialization

### Step 2: add resolved REST endpoint catalog and collision validation

Add a runtime catalog and fail-fast duplicate-route validation over the normalized projection model
so future shorthand or generated projections cannot create silent public-route ambiguity.

Status:

- shipped through `ENG-058-T57` plus `ENG-058-T58`; `Cephalon.Abstractions` now exposes
  `IRestEndpointRuntimeCatalog`, `IRestEndpointRuntimeRegistry`, and
  `RestEndpointRuntimeDescriptor`, `Cephalon.AspNetCore` now publishes
  `/engine/rest-endpoints`, `/engine/rest-endpoints/{restEndpointId}`, and
  `snapshot.RestEndpoints`, the public REST host now fails fast when two resolved public REST
  endpoints collide on the same `HTTP method + route pattern`, and that runtime answer now covers
  both projection-backed module DSL routes and explicit manual module-owned REST routes

### Step 3: add build-time diagnostics and source-generated profile support

If behavior-authored HTTP profiles are added, validate them at build time and emit normalized
descriptor data alongside existing behavior registration hints.

Status:

- shipped through `ENG-058-T61`; `Cephalon.Behaviors.Http` now exposes the metadata-only
  `BehaviorRestProfileAttribute`, `BehaviorRestMethod`, and `BehaviorRestProfileDescriptor`
  contract, while `Cephalon.Behaviors.SourceGen` now validates that profile metadata at build time
  and emits source-generated `GetRestProfiles()` hints without activating public REST routes from
  `[AppBehavior]`

### Step 4: ship explicit module-owned profile shorthand, then generated module projections

Status:

- partially shipped through `ENG-058-T62`; `IRestBehaviorEndpointGroupBuilder.MapProfile<TBehavior>()`
  now lets an owning module consume `BehaviorRestProfileAttribute` hints without restating the HTTP
  method or relative pattern in module code, while still keeping public REST explicit and
  module-owned
- now shipped through `ENG-058-T67`; `IRestBehaviorEndpointGroupBuilder.MapGeneratedProfiles()` and
  `MapGeneratedProfiles(string behaviorIdPrefix)` let an owning module publish all matching
  profile-backed behaviors beneath one owned route group without restating each behavior
  individually, while still keeping public REST explicit, module-owned, and visible through the
  same normalized projection plus candidate-catalog runtime surfaces

Follow-through later:

- allow low-code projects to opt into broader convention-backed module projection only when it can
  preserve the same module-owned public-boundary model and operator truth

### Step 5: add explicit input-binding descriptors, then controlled configuration overrides

Finish the model by making binding plans explicit first, then evaluate whether descriptor-backed
routes should opt into configuration override behavior where that flexibility is worth the
complexity.

Status:

- the explicit input-binding descriptor slice is now shipped through `ENG-058-T63`
- the first compile-time and runtime hardening follow-through is now shipped through `ENG-058-T64`,
  so invalid binding metadata is rejected at build time and runtime fallback still re-checks
  route-placeholder truth before endpoint materialization
- suppression visibility plus explicit-DSL-over-profile precedence is now shipped through
  `ENG-058-T66`
- generated module shorthand plus explicit `DSL > MapProfile<TBehavior>() > MapGeneratedProfiles(...)`
  precedence is now shipped through `ENG-058-T67`
- the first controlled-governance follow-through is now shipped through `ENG-058-T68`, so
  ASP.NET Core hosts can suppress descriptor-backed shorthand candidates through
  `RestApi:Suppressions` while the runtime keeps both the configured suppression-rule catalog and
  the candidate-level `SuppressedBySuppressionId` plus `MatchedSuppressionIds` truth visible
- the next controlled-governance follow-through is now shipped through `ENG-058-T69`,
  `ENG-058-T70`, `ENG-058-T71`, `ENG-058-T72`, `ENG-058-T73`, `ENG-058-T74`, `ENG-058-T75`, and
  `ENG-058-T82`, so ASP.NET Core hosts can
  retarget descriptor-backed shorthand candidates through `RestApi:Overrides` when they need a
  different effective `ApiVersionMajor`, HTTP `Method`, bounded published `RouteGroupPrefix`,
  constrained relative `Pattern`, or explicit binding plan, while the runtime keeps both the
  configured override-rule catalog and the
  candidate-level `AppliedOverrideId` plus `MatchedOverrideIds` truth visible
- the next governance-overlap visibility follow-through is now shipped through `ENG-058-T87`, so
  overlapping shorthand suppression/override matches now stay visible in runtime truth through the
  ordered `MatchedSuppressionIds` and `MatchedOverrideIds` lists before one rule wins by the
  existing specificity model
- the next selector-targeting follow-through is now shipped through `ENG-058-T77`, so both
  `RestApi:Suppressions` and `RestApi:Overrides` can refine that same descriptor-backed shorthand
  scope with `ApiVersionMajors`, `Methods`, `RelativePatterns`, and `RouteGroupPrefixes` while the
  runtime keeps both the configured rule catalogs and the original-shape targeting truth visible
- controlled configuration overrides that promote implicit properties into route placeholders
  beyond the shipped constrained remaining-body-fallback path, or rewrite input binding beyond
  constrained explicit-binding replacement, remain later work now that the
  version-plus-method-plus-pattern-plus-binding-plus-placeholder-rename-plus-placeholder-removal-plus-placeholder-addition-plus-implicit-body-fallback-promotion-plus-selector-targeting
  override baseline is shipped

## What should be stored as project memory

The following points are durable enough to keep outside thread-local context.

- public REST stays module-owned even if Cephalon later adds shorthand REST authoring
- future shorthand REST authoring should be metadata or projection driven, not direct behavior-owned
  route activation
- explicit module-owned REST mappings suppress lower-precedence implicit or convention projections
  for the same behavior by default
- low-code REST authoring should stay opt-in, should prefer source-generated descriptor material
  first, and should allow only bounded owner-assembly fallback when a module explicitly opts into
  generated publication
- `[AppBehavior]` plus auto-registration alone must still not publish a public REST boundary
- the shipped public REST baseline now exposes resolved route truth through
  `IRestEndpointRuntimeCatalog`, `/engine/rest-endpoints`, `/engine/rest-endpoints/{restEndpointId}`,
  and `snapshot.RestEndpoints`
- the shipped precedence-visibility baseline now also exposes candidate publication truth through
  `IRestEndpointCandidateRuntimeCatalog`, `/engine/rest-endpoint-candidates`,
  `/engine/rest-endpoint-candidates/{candidateId}`, and `snapshot.RestEndpointCandidates`
- the shipped precedence-visibility baseline now also exposes grouped publication truth through
  `IRestEndpointPublicationGroupRuntimeCatalog`, `/engine/rest-endpoint-publication-groups`,
  `/engine/rest-endpoint-publication-groups/{behaviorId}`, and
  `snapshot.RestEndpointPublicationGroups`
- those candidate entries now keep the original shorthand source shape visible through
  `RestEndpointCandidateRuntimeDescriptor.OriginalProjection` while `ProjectedEndpoint` continues
  to answer the final effective mapped route, version, method, binding, endpoint-name, summary,
  and description shape
- those candidate entries now also keep overlapping host-governance matches visible through
  `RestEndpointCandidateRuntimeDescriptor.MatchedSuppressionIds` and
  `RestEndpointCandidateRuntimeDescriptor.MatchedOverrideIds` before one winning rule is selected
- those grouped publication entries now keep the behavior-level published-versus-suppressed story
  visible through published candidate ids, precedence-suppressed candidate ids,
  governance-suppressed candidate ids, the winning precedence rank when one exists, and the
  ordered candidate set
- the shipped governance baseline now also exposes configured suppression-rule truth through
  `IRestEndpointSuppressionRuntimeCatalog`, `/engine/rest-endpoint-suppressions`,
  `/engine/rest-endpoint-suppressions/{suppressionId}`, and `snapshot.RestEndpointSuppressions`
- the shipped governance baseline now also exposes configured override-rule truth through
  `IRestEndpointOverrideRuntimeCatalog`, `/engine/rest-endpoint-overrides`,
  `/engine/rest-endpoint-overrides/{overrideId}`, and `snapshot.RestEndpointOverrides`
- the shipped `RestApi:Suppressions` baseline is intentionally limited to suppression of
  descriptor-backed shorthand candidates, and the shipped `RestApi:Overrides` baseline is
  intentionally limited to shorthand `ApiVersionMajor`, `Method`, bounded `RouteGroupPrefix`,
  constrained relative `Pattern`, and constrained explicit `Bindings` rewrites with either default
  full replacement or typed `MergeExplicit` property upserts; neither surface rewrites explicit
  module DSL or manual routes
- both rule families can now target exact original-shape shorthand candidates through
  `CandidateIds`, can also refine `Behaviors`/`Modules` targeting with `ApiVersionMajors`,
  `Methods`, `RelativePatterns`, and `RouteGroupPrefixes`, and all of those selectors match the
  original shorthand candidate identity before override actions are applied so suppression and
  override decisions do not depend on already-rewritten final route shape
- `/engine/rest-endpoint-candidates` now publishes that same original-shape candidate identity
  through `RestEndpointCandidateRuntimeDescriptor.Id`, while `ProjectedEndpoint.Id` remains the
  effective mapped endpoint identity after override actions are applied
- shorthand candidate projected endpoints now also keep endpoint names plus summary/description
  metadata aligned with the final behavior-backed runtime endpoint conventions, including XML-
  derived summaries/descriptions when those docs exist and module-description fallback when they do
  not
- within that constrained pattern slice, placeholder-preserving rewrites stay the default and
  placeholder renames now also work when the effective explicit route-binding plan covers the
  renamed placeholder set exactly, placeholder removals now also work when the original
  projection already exposes explicit route-binding coverage for the original placeholder set and
  the effective explicit binding plan keeps every affected original route-bound property
  explicitly bound, placeholder additions now also work when the effective explicit route-binding
  plan covers the full final placeholder set and every newly route-bound property was either
  already explicitly bound in the original projection or, for `POST`/`PUT`/`PATCH`, already part
  of the original deterministic remaining-body fallback surface, bounded `RouteGroupPrefix`
  rewrites now also stay below the active REST root with no placeholders and no silent
  API-version drift, ASP.NET Core now materializes split effective groups when only some shorthand
  candidates in one authored group are remapped, and broader implicit-property promotion remains
  later work
- future shorthand or convention REST publication must compose through the shared projection,
  runtime-catalog, and collision-validation pipeline instead of bypassing it
- future agentic, AI, or multi-platform expansion should not outrun core engine contract quality,
  performance, security, and maintainability

## Near-term follow-through candidates

Recommended implementation sequence after the shipped normalization, runtime-catalog,
precedence-visibility, and generated-module follow-through slices:

1. extend the shipped suppression-plus-override governance baseline from the current
   candidate-id-plus-selector-targeting-plus-version-plus-method-plus-pattern-plus-binding model
   toward broader configuration-override modeling only if runtime truth, ownership, precedence,
   and candidate visibility stay explicit and introspectable
2. only then evaluate whether any additional convention-backed publication sources are worth the
   added complexity beyond the shipped `MapProfile<TBehavior>()` and `MapGeneratedProfiles(...)`
   surfaces
