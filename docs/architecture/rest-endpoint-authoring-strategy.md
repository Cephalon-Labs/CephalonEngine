# Cephalon REST Endpoint Authoring Strategy

Decision baseline date: `April 14, 2026`

Related issues: `ENG-058-T55` / GitHub issue `#313`, `ENG-058-T56` / GitHub issue `#314`, `ENG-058-T57` / GitHub issue `#318`, `ENG-058-T58` / GitHub issue `#320`, `ENG-058-T61` / GitHub issue `#324`, `ENG-058-T62` / GitHub issue `#325`, `ENG-058-T63` / GitHub issue `#326`, `ENG-058-T64` / GitHub issue `#327`, `ENG-058-T65` / GitHub issue `#329`, `ENG-058-T66` / GitHub issue `#331`, `ENG-058-T67` / GitHub issue `#332`, `ENG-058-T68` / GitHub issue `#333`, `ENG-058-T69` / GitHub issue `#334`, `ENG-058-T70` / GitHub issue `#335`, `ENG-058-T71` / GitHub issue `#336`, `ENG-058-T72` / GitHub issue `#337`, `ENG-058-T73` / GitHub issue `#338`, `ENG-058-T74` / GitHub issue `#339`, `ENG-058-T75` / GitHub issue `#340`, `ENG-058-T76` / GitHub issue `#341`, `ENG-058-T77` / GitHub issue `#342`, `ENG-058-T78` / GitHub issue `#343`, `ENG-058-T79` / GitHub issue `#344`, `ENG-058-T80` / GitHub issue `#345`, `ENG-058-T81` / GitHub issue `#346`, `ENG-058-T82` / GitHub issue `#347`, `ENG-058-T83` / GitHub issue `#348`, `ENG-058-T84` / GitHub issue `#349`, `ENG-058-T85` / GitHub issue `#350`, `ENG-058-T86` / GitHub issue `#351`, `ENG-058-T87` / GitHub issue `#352`, `ENG-058-T88` / GitHub issue `#353`, `ENG-058-T89` / GitHub issue `#354`, `ENG-058-T90` / GitHub issue `#355`, `ENG-058-T91` / GitHub issue `#356`, `ENG-058-T92` / GitHub issue `#357`, `ENG-058-T93` / GitHub issue `#358`, `ENG-058-T94` / GitHub issue `#359`, `ENG-058-T95` / GitHub issue `#360`, `ENG-058-T96` / GitHub issue `#361`, `ENG-058-T97` / GitHub issue `#362`, `ENG-058-T98` / GitHub issue `#363`, `ENG-058-T99` / GitHub issue `#364`, `ENG-058-T100` / GitHub issue `#365`, `ENG-058-T101` / GitHub issue `#367`, `ENG-058-T102` / GitHub issue `#368`, `ENG-058-T103` / GitHub issue `#369`, `ENG-058-T104` / GitHub issue `#370`, `ENG-058-T105` / GitHub issue `#371`, `ENG-058-T106` / GitHub issue `#372`, `ENG-058-T107` / GitHub issue `#373`, `ENG-058-T108` / GitHub issue `#374`, `ENG-058-T109` / GitHub issue `#375`, `ENG-058-T110` / GitHub issue `#376`, `ENG-058-T111` / GitHub issue `#377`, `ENG-058-T112` / GitHub issue `#378`, `ENG-058-T113` / GitHub issue `#379`, `ENG-058-T114` / GitHub issue `#380`, `ENG-058-T115` / GitHub issue `#381`, `ENG-058-T116` / GitHub issue `#382`, `ENG-058-T117` / GitHub issue `#383`, `ENG-058-T118` / GitHub issue `#384`, `ENG-058-T119` / GitHub issue `#385`, `ENG-058-T120` / GitHub issue `#386`, `ENG-058-T121` / GitHub issue `#387`, `ENG-058-T122` / GitHub issue `#388`, `ENG-058-T123` / GitHub issue `#389`, `ENG-058-T124` / GitHub issue `#390`, `ENG-058-T125` / GitHub issue `#391`, `ENG-058-T126` / GitHub issue `#392`, `ENG-058-T127` / GitHub issue `#393`, `ENG-058-T128` / GitHub issue `#394`, `ENG-058-T129` / GitHub issue `#395`, `ENG-058-T130` / GitHub issue `#396`, `ENG-058-T131` / GitHub issue `#397`, `ENG-058-T132` / GitHub issue `#398`, `ENG-058-T133` / GitHub issue `#399`, `ENG-058-T134` / GitHub issue `#401`

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
- host governance is still shorthand-first by default; explicit module-DSL route groups only
  participate when the owning group explicitly allows host governance and the matching host rule
  deliberately targets authoring style `behavior-module-dsl`

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
- keep explicit module DSL authoritative by default, but allow a route group to opt into
  host-level publication governance explicitly when a host needs to suppress or rewrite that
  authored boundary without changing the owning module model

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
  `ABT0015` through `ABT0027`, while still keeping those behavior-authored REST profiles
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
- the next contract-hardening follow-through is now also shipped through `ENG-058-T141` and
  `ENG-058-T142`: `BehaviorRestBindingSource` plus `BehaviorRestMethod` now carry stable wire-name
  vocabularies, and `Cephalon.Behaviors.SourceGen` now validates both profile methods and explicit
  binding metadata against those canonical wire-name contracts while still emitting resolved enum
  member names into generated `GetRestProfiles()` output
- the next runtime-guidance parity follow-through is now also shipped through `ENG-058-T143`:
  runtime attribute-fallback, explicit binding-plan normalization, and last-mile profile-method
  conversion now echo those same canonical `get` / `post` / `put` / `patch` / `delete` and
  `route` / `query` / `header` / `body` wire names in their exception guidance, so operator and
  developer troubleshooting no longer diverges from the JSON/source-generation contract
- the next method-guidance completion follow-through is now also shipped through `ENG-058-T144`:
  non-body method body-binding rejections and unsupported REST method parser failures now also
  echo the canonical `get` / `post` / `put` / `patch` / `delete` vocabulary, so the remaining
  runtime method-validation paths no longer drift back to enum/member-name wording
- the next profile-route authoring-safety follow-through is now also shipped through
  `ENG-058-T117`: `Cephalon.Behaviors.SourceGen` now rejects malformed
  `BehaviorRestProfileAttribute.RelativePattern` placeholder syntax earlier through `ABT0026`, and
  `BehaviorRestProfileResolver` now also parses the declared route pattern even when no explicit
  bindings are present so direct attribute fallback or stale generated hints still fail fast before
  shorthand publication can materialize an invalid route shape
- the next explicit-binding authoring-parity follow-through is now also shipped through
  `ENG-058-T119`: metadata-only REST profiles can now declare
  `BehaviorRestProfile(PreserveImplicitQueryFallback = true)` when they already carry explicit
  bindings, so profile authors can keep explicit route aliases or other explicit bindings without
  losing the remaining implicit query-fallback surface; `Cephalon.Behaviors.SourceGen` rejects
  missing-binding cases through `ABT0027`, and `BehaviorRestProfileResolver` re-checks the same
  rule during runtime fallback
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
  can now declare wire-name-only `BindingMode = merge-explicit` when a host wants to upsert only selected explicit
  bindings by property name instead of restating the whole explicit plan, the existing
  `replace-explicit` behavior remains the default when `BindingMode` is omitted, and the runtime now
  keeps that merge-versus-replace governance truth visible through `/engine/rest-endpoint-overrides`
  plus `snapshot.RestEndpointOverrides`; `bindingMode = unspecified` is preserved there only when a
  `ClearBindings` rule omitted an explicit mode, while other omitted modes still normalize to
  `replace-explicit` or `merge-explicit`, and host config still accepts only those two explicit
  wire names
- the next grouped publication-visibility follow-through is now shipped through `ENG-058-T80`:
  `Cephalon.Abstractions` now also exposes `IRestEndpointPublicationGroupRuntimeCatalog` plus
  `RestEndpointPublicationGroupDescriptor`, `Cephalon.AspNetCore` now publishes
  `/engine/rest-endpoint-publication-groups`,
  `/engine/rest-endpoint-publication-groups/{behaviorId}`, and
  `snapshot.RestEndpointPublicationGroups`, and the host now groups the existing candidate-level
  truth per behavior so operators can inspect published candidates, precedence-suppressed
  candidates, governance-suppressed candidates, the winning precedence rank when one exists, and
  the ordered candidate set without manually joining several runtime surfaces
- the next publication-group source-competition visibility follow-through is now shipped through
  `ENG-058-T121`: `Cephalon.Abstractions` now also exposes
  `RestEndpointPublicationGroupAuthoringStyleDescriptor`, and
  `RestEndpointPublicationGroupDescriptor.AuthoringStyleSummaries` now derives a first-class
  per-style summary directly from the grouped candidate truth so
  `/engine/rest-endpoint-publication-groups` and `snapshot.RestEndpointPublicationGroups` also
  show which authoring styles participated, which remained published, and which were
  precedence-suppressed or governance-suppressed without re-partitioning the ordered candidate set
  manually
- the next publication-group authoring-policy contract follow-through is now shipped through
  `ENG-058-T122`: `Cephalon.Abstractions` now also exposes
  `RestEndpointPublicationGroupAuthoringPolicyDescriptor`,
  `RestEndpointPublicationGroupDescriptor.AuthoringPolicy` now carries the effective default-
  versus-configured authoring-policy intent for each grouped behavior answer, and
  `RestApi:AuthoringPolicies:{behaviorId}` now binds
  `AllowMultiplePublishedCandidates` plus preferred/allowed/disallowed authoring-style intent so
  `/engine/rest-endpoint-publication-groups` and `snapshot.RestEndpointPublicationGroups` can
  round-trip that policy truth
- the next bounded authoring-policy enforcement follow-through is now shipped through
  `ENG-058-T123`: `Cephalon.Behaviors.Http` now honors
  `RestApi:AuthoringPolicies:{behaviorId}:AllowMultiplePublishedCandidates` during candidate
  resolution so lower-precedence shorthand candidates can remain published when governance does not
  suppress them, while route-collision validation still stays authoritative and effective endpoint
  names are now disambiguated deterministically by authoring style, route shape, and candidate
  identity when co-published shorthand candidates would otherwise reuse the same endpoint name
- the next shorthand authoring-policy truth follow-through is now shipped through `ENG-058-T124`:
  `PreferredAuthoringStyle`, `AllowedAuthoringStyles`, and `DisallowedAuthoringStyles` now also
  enforce shorthand publication policy for `behavior-module-profile` and
  `behavior-module-generated` candidates, while explicit module DSL publication remains
  authoritative; grouped publication and candidate runtime truth now keep authoring-policy
  suppression distinct from config-governance suppression and candidate-precedence suppression
  through dedicated suppression kinds, policy-suppressed candidate buckets, and startup
  diagnostics/logging
- the next authoring-policy runtime-contract conformance follow-through is now shipped through
  `ENG-058-T145`: `/engine/rest-endpoint-candidates`,
  `/engine/rest-endpoint-candidates/{candidateId}`, and `snapshot.RestEndpointCandidates` now have
  targeted hosting/runtime coverage that locks `SuppressedByAuthoringPolicyKind` onto the
  canonical `disallowed-authoring-style`, `not-allowed-authoring-style`, and
  `preferred-authoring-style-selected` wire names instead of leaving operator JSON truth implicit
  in enum serialization behavior alone
- the next publication-group authoring-policy suppression-breakdown follow-through is now shipped
  through `ENG-058-T146`: grouped publication answers now also expose typed
  `AuthoringPolicySuppressionSummaries` at both the behavior-group level and inside each
  `AuthoringStyleSummaries` entry, so `/engine/rest-endpoint-publication-groups`,
  `/engine/rest-endpoint-publication-groups/{behaviorId}`, and
  `snapshot.RestEndpointPublicationGroups` can distinguish
  `disallowed-authoring-style`, `not-allowed-authoring-style`, and
  `preferred-authoring-style-selected` suppression outcomes without forcing operators to re-join
  the candidate catalog manually
- the next rule-centric authoring-policy runtime-answer follow-through is now shipped through
  `ENG-058-T152` plus `ENG-058-T153`: `Cephalon.Abstractions` now also exposes
  `IRestEndpointAuthoringPolicyRuntimeCatalog`, `RestEndpointAuthoringPolicyDescriptor`,
  `RestEndpointAuthoringPolicyAuthoringStyleDescriptor`, and
  `RestEndpointAuthoringPolicySuppressionSummaryDescriptor`; `Cephalon.AspNetCore` now publishes
  `/engine/rest-endpoint-authoring-policies`,
  `/engine/rest-endpoint-authoring-policies/{behaviorId}`, and
  `snapshot.RestEndpointAuthoringPolicies`; and that top-level behavior answer keeps explicit
  default-versus-configured policy intent, explicitly configured-but-unmatched policies, separate
  retained-versus-published-versus-precedence/governance-suppressed buckets, and per-style
  `AuthoringStyleSummaries` visible without reopening grouped publication answers
- the next publication-group host-governance rule-summary follow-through is now shipped through
  `ENG-058-T147`: grouped publication answers now also expose typed
  `GovernanceSuppressionSummaries` and `GovernanceOverrideSummaries` at both the behavior-group
  level and inside each `AuthoringStyleSummaries` entry, so
  `/engine/rest-endpoint-publication-groups`,
  `/engine/rest-endpoint-publication-groups/{behaviorId}`, and
  `snapshot.RestEndpointPublicationGroups` can show matched-versus-suppressed host suppression
  outcomes plus matched/selected/applied override outcomes, including no-op winning overrides
  whose applied bucket remains empty, without re-reading `/engine/rest-endpoint-candidates`
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
  support `RemovedBindingProperties`, `merge-explicit` now covers both property-by-property explicit
  binding upserts and explicit-binding withdrawals, removal-only rules normalize to merge mode
  automatically, `replace-explicit` cannot pair with removals, a merge rule cannot both remove and
  override the same property, removal targets must already exist in the source shorthand explicit
  binding plan, and `/engine/rest-endpoint-overrides` plus `snapshot.RestEndpointOverrides` now
  keep both the typed binding mode and removed-property list visible
- the next shorthand explicit-binding reset follow-through is now shipped through
  `ENG-058-T110`: the typed override/runtime contracts plus ASP.NET Core config binding now also
  support shorthand-only `ClearBindings`, that reset can discard the source explicit binding plan
  so request composition returns to the implicit route/query/body baseline, it cannot pair with
  `Bindings`, `RemovedBindingProperties`, or an explicit `BindingMode`, and it fails fast when the
  effective route would only remain satisfiable through removed explicit route-binding aliases;
  equivalent reorder-only override plans now also stay visible as matches while preserving source
  explicit binding order and leaving `AppliedOverrideId = null` when the normalized binding set
  does not change
- the next low-code generated module-owned shorthand is now shipped through `ENG-058-T67`:
  `IRestBehaviorEndpointGroupBuilder.MapGeneratedProfiles()` and
  `MapGeneratedProfiles(string behaviorIdPrefix)` let an owning module opt into profile-backed
  generated publication for one owned route group, prefer source-generated `GetRestProfiles()` plus
  `GetRestProfileBehaviorTypes()` hints, fall back only to a bounded scan of the explicit owning
  module assembly when generated type hints are unavailable, and keep runtime publication on the
  same normalized projection and candidate-catalog path with
  `metadata.authoringStyle = behavior-module-generated`
- the next explicit module-DSL host-governance boundary follow-through is now shipped through
  `ENG-058-T128`: `IRestBehaviorEndpointGroupBuilder.AllowHostGovernance()` now lets an explicit
  module-owned route group opt into ASP.NET Core `RestApi:Suppressions` and `RestApi:Overrides`
  without changing its authoring style, `RestEndpointCandidateProjectionDescriptor` plus
  published `OriginalProjection` now keep that opt-in visible through
  `AllowsHostGovernance`, shorthand candidates still participate by default, and host rules still
  leave explicit module DSL out of scope unless they explicitly target authoring style
  `behavior-module-dsl`
- the next explicit module-DSL governance-visibility follow-through is now shipped through
  `ENG-058-T129`: when host suppression or override rules target that same explicit module-DSL
  surface before the route group opts in, runtime candidates plus published behavior-backed
  endpoints now keep the ordered skipped rule ids visible through `SkippedSuppressionIds` and
  `SkippedOverrideIds` while `Matched*` stays empty, so operators can distinguish
  governance-ineligible explicit ownership from a simple selector miss
- the next explicit module-DSL grouped-governance visibility follow-through is now shipped through
  `ENG-058-T130`: `/engine/rest-endpoint-publication-groups` plus
  `snapshot.RestEndpointPublicationGroups` now also keep grouped
  `HostGovernanceEligibleCandidateIds`, `HostGovernanceIneligibleCandidateIds`,
  `SkippedSuppressionIds`, and `SkippedOverrideIds` visible at both the top-level behavior group
  and per-authoring-style summary, so operators can inspect grouped publication truth without
  drilling into each candidate first
- the next explicit module-DSL grouped skipped-governance follow-through is now shipped through
  `ENG-058-T148`: those same grouped publication answers now also keep per-rule skipped
  suppression/override candidate buckets visible through `SkippedSuppressionSummaries` and
  `SkippedOverrideSummaries` at both the top-level behavior group and per-authoring-style summary,
  so operators can see which ineligible candidates each skipped host rule targeted without
  re-reading `/engine/rest-endpoint-candidates`
- the next explicit module-DSL diagnostics follow-through is now shipped through `ENG-058-T131`:
  `/engine/diagnostics` plus information-level startup logging now also emit
  `RestEndpointGovernanceSkipped` event `5206` when host suppression or override rules target an
  explicit module-DSL candidate that did not opt into host governance, so skipped-rule visibility
  stays aligned across candidate, endpoint, grouped-publication, and diagnostics surfaces
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
  effective `ApiVersionMajor`, `OpenApiDocumentName`, HTTP `Method`, bounded published
  `RouteGroupPrefix`, constrained relative `Pattern`, and/or explicit binding plan; later
  follow-through also layers capability, endpoint-metadata, and tag/document governance on the
  same contract. The runtime now exposes those configured rules through
  `IRestEndpointOverrideRuntimeCatalog`, `/engine/rest-endpoint-overrides`, and
  `snapshot.RestEndpointOverrides`; candidates now surface the governing rule through
  `RestEndpointCandidateRuntimeDescriptor.AppliedOverrideId` when the winning rule materially
  changes the effective answer, and now also surface the selected winning no-op rule through
  `RestEndpointCandidateRuntimeDescriptor.SelectedOverrideId` plus
  `RestEndpointCandidateRuntimeDescriptor.OverrideSelectionBasis` while
  `RestEndpointCandidateRuntimeDescriptor.MatchedOverrideIds` keeps every matching rule visible in
  specificity order, and published candidate registration now reconciles capability-only no-op
  matches against the actual mapped endpoint so no-op clears or same-key capability rewrites leave
  `AppliedOverrideId = null` while `SelectedOverrideId`, `OverrideSelectionBasis`, and
  `MatchedOverrideIds` still show which rule won, why it beat the runner-up, and which rules
  matched. The next declared-versus-effective override-dimension visibility follow-through is now
  shipped through `ENG-058-T132`: the typed override contract now exposes
  `RestEndpointOverrideActionKind` plus `RestEndpointOverrideDescriptor.ActionKinds`, while
  shorthand candidates and published endpoints now expose `SelectedOverrideActionKinds` and
  `AppliedOverrideActionKinds` so the runtime can say which override dimensions the winning rule
  declared versus which ones materially changed the effective published answer; no-op winners keep
  the selected action kinds visible while leaving applied action kinds empty. The next
  diagnostics-story follow-through is now shipped through `ENG-058-T133` and `ENG-058-T134`:
  startup diagnostics event `5200` now also echoes the decisive suppression selection-basis wire
  name, while events `5202` and `5203` now also echo the decisive override selection-basis wire
  name plus the selected and applied override action-kind wire names, so the operator log story
  matches the runtime catalog story without requiring a manual diff against the final route shape.
  The
  normalized materializer now maps the same effective projection shape that
  the runtime catalogs report; explicit binding-plan
  overrides now default to replacing the shorthand candidate's explicit descriptors, but can also
  merge explicit binding upserts and withdrawals by property name through
  `BindingMode = merge-explicit` plus `RemovedBindingProperties` while still leaving unbound route
  placeholders and remaining request-body fields available for deterministic fallback, or clear the
  explicit plan entirely through shorthand-only `ClearBindings` when the effective route still
  binds truthfully through the implicit baseline; placeholder
  renames can now also apply when the effective explicit route-binding plan
  covers the renamed placeholder set exactly; placeholder removals can now also apply when the
  original projection already exposes explicit route-binding coverage for the original placeholder
  set and the effective explicit binding plan keeps every affected original route-bound property
  explicitly bound; placeholder additions can now also apply when the effective explicit
  route-binding plan covers the full final placeholder set and every newly route-bound property was
  either already explicitly bound in the original projection, for `POST`/`PUT`/`PATCH` already
  part of the original deterministic remaining-body fallback surface, or for shorthand candidates
  with no explicit binding plan already part of the original implicit query-fallback surface; and
  invalid effective method-plus-binding plans, rename attempts that still rely on inference,
  removal attempts that rely on inferred original route coverage or drop explicit binding coverage,
  or addition attempts that would promote any other implicit property into the public route now
  fail fast
- the selector-expansion follow-through is now shipped through `ENG-058-T77`,
  `ENG-058-T109`, and `ENG-058-T118`: both
  `RestApi:Suppressions` and `RestApi:Overrides` can refine `Behaviors`/`Modules` targeting with
  `ApiVersionMajors`, `Methods`, `RelativePatterns`, `RouteGroupPrefixes`,
  `OpenApiDocumentNames`, `TagNames`, and `BindingFallbackModes`; those selectors match the
  original shorthand candidate shape before override actions are applied, suppression now preserves
  that same original-shape contract even when an override later rewrites the final published
  endpoint, rule specificity now also considers populated selector dimensions plus narrower
  selector sets, and the runtime suppression/override catalogs now expose the selector arrays
  directly
- broader configuration-driven projection overrides that promote implicit properties into route
  placeholders beyond that constrained body-fallback-plus-bounded-query-fallback path, or rewrite
  binding shape beyond that constrained explicit-binding upsert-plus-withdraw model, remain later
  work

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
- `GroupFromBehaviorIdPrefix(...)` plus `AddGeneratedRestBehaviorModule<TMarker>(...)` now cover
  the common generated-profile cases where route-group path, generated behavior-id prefix, and
  inline module id intentionally mirror one another, while the explicit
  `AddGeneratedRestBehaviorModule<TMarker>(descriptor, "prefix", ...)` overload remains available
  when the inline module id and generated ownership prefix should differ, again without inventing a
  new publication source

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
- override currently supports `ApiVersionMajor`, `OpenApiDocumentName`, `Method`, bounded
  published `RouteGroupPrefix`, relative `Pattern`, `RequiredCapabilityKey`,
  `ClearRequiredCapability`, `EndpointName`, `Summary`, `Description`, `TagName`,
  `ClearEndpointName`, `ClearSummary`, `ClearDescription`, explicit `Bindings`,
  `RemovedBindingProperties`, shorthand binding resets through `ClearBindings`, and typed
  `BindingMode`
- both rule families fail fast when a rule omits both `Behaviors` and `Modules`
- override rules also fail fast when they omit all override actions, use a non-positive
  `ApiVersionMajor`, declare an unsupported HTTP method, declare an invalid relative route
  pattern, or declare an invalid `RouteGroupPrefix`
- both rule families can refine `Behaviors`/`Modules` targeting with `ApiVersionMajors`,
  `Methods`, `RelativePatterns`, `RouteGroupPrefixes`, `OpenApiDocumentNames`, `TagNames`, and
  `BindingFallbackModes`, and those selector refiners match the original shorthand candidate shape
  before override actions are applied
- when more than one rule matches, the host prefers the more specific rule deterministically by
  populated target dimensions first, then by behavior-targeted scope, narrower authoring-style
  scope, fewer total selector values, and finally stable rule-id ordering
- neither surface overrides explicit module DSL or manual module-owned REST endpoints
- shorthand groups that declare `.ApiVersion(...)` explicitly stay authoritative over host version
  rewrites, while shorthand method and constrained pattern overrides can still apply to those same
  groups
- the current override slice can rewrite the effective API major version, OpenAPI document name,
  HTTP method, bounded published route-group prefix, constrained relative route pattern, required
  capability boundary, shorthand endpoint metadata, shorthand tag, and/or explicit binding plan
  while keeping the `/api/v{major}` route segment, published group boundary, OpenAPI document
  name, mapped endpoint, endpoint-group metadata, and runtime catalogs aligned to the same
  effective projection; `ApiVersionMajor` only re-derives the document name when the authored
  shorthand group did not pin one explicitly
- `RouteGroupPrefix` rewrites stay beneath the active REST root, cannot declare placeholders,
  cannot silently change effective API-version truth, and now make ASP.NET Core split effective
  shorthand groups when only some candidates in one authored group are remapped, moved to another
  document, or retagged
- `OpenApi:EnabledVersions` and legacy document config still decide which documents are actually
  published
- pattern rewrites preserve the placeholder set by default and can now also rename placeholders
  when the effective explicit route-binding plan covers the renamed placeholder set exactly
- placeholder removals can now also apply when the original projection already exposes explicit
  route-binding coverage for the original placeholder set and the effective explicit binding plan
  keeps every affected original route-bound property explicitly bound
- placeholder additions can now also apply when the effective explicit route-binding plan covers
  the full final placeholder set and every newly route-bound property was either already explicitly
  bound in the original projection, for `POST`/`PUT`/`PATCH` already part of the original
  deterministic remaining-body fallback surface, or for shorthand candidates with no explicit
  binding plan already part of the original implicit query-fallback surface
- `BindingMode = merge-explicit` can now upsert changed explicit bindings and withdraw selected
  original explicit bindings through `RemovedBindingProperties`, while failing fast if a removal
  targets a property the source shorthand never bound explicitly or if one merge rule both removes
  and overrides the same property
- `ClearBindings = true` can now discard the source shorthand explicit binding plan entirely and
  return the candidate to the implicit request-binding baseline, while failing fast if the
  effective route would only stay valid through explicit placeholder aliases that the clear
  removed
- broader implicit-property promotion beyond that constrained body-fallback-plus-bounded-query-
  fallback path plus broader input-binding rewrites beyond the current replace-plus-merge-explicit
  upsert-plus-withdraw model remain later work

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

The current shipped place for that intent is
`RestApi:AuthoringPolicies:{behaviorId}:AllowMultiplePublishedCandidates`, surfaced back out
through `RestEndpointPublicationGroupDescriptor.AuthoringPolicy`. When that flag is omitted or
`false`, the default single-winner precedence answer still applies. When it is `true`,
lower-precedence shorthand candidates can remain published side by side as long as governance does
not suppress them and the final `HTTP method + route pattern` answers still stay distinct; if
co-published candidates would otherwise reuse the same effective endpoint name, the resolver now
disambiguates that endpoint name deterministically while preserving `OriginalEndpointName` as the
source shorthand lineage.

That means:

- explicit module mapping should suppress implicit behavior-only REST projection for the same behavior
- Cephalon should not run both side by side by default
- Cephalon may run more than one shorthand candidate side by side only when the grouped behavior
  explicitly opts into that outcome and the final mapped public surface remains deterministic

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
- first-class published-endpoint `RouteGroupPrefix`, `RelativePattern`, `AuthoringStyle`,
  nullable `BehaviorType`, nullable `SourceId`, and nullable `CandidateId`, while additive
  metadata remains compatibility-only
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
  replacement and typed `merge-explicit` property-by-property binding patches plus withdrawals,
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
`RelativePatterns`, `RouteGroupPrefixes`, `OpenApiDocumentNames`, `TagNames`, and
`BindingFallbackModes`, then change the effective shorthand candidate `ApiVersionMajor`,
`OpenApiDocumentName`, HTTP `Method`, bounded published `RouteGroupPrefix`, relative `Pattern`,
`RequiredCapabilityKey`, `ClearRequiredCapability`, `EndpointName`, `Summary`, `Description`,
`TagName`, `ClearEndpointName`, `ClearSummary`, `ClearDescription`, and/or explicit `Bindings`
plus `RemovedBindingProperties`, and can now also reset the authored explicit binding plan through
shorthand-only `ClearBindings`, but the current route-pattern slice
is still constrained enough to keep binding semantics truthful and `ApiVersionMajor` only
re-derives the effective document name when the authored shorthand group did not pin one
explicitly through `.WithOpenApiDocumentName(...)`. Cephalon therefore keeps the
route-version segment and document name
together for version rewrites, allows route-pattern rewrites when they preserve the same
placeholder set or when the effective explicit route-binding plan covers a renamed placeholder set
exactly, lets binding overrides either replace the shorthand candidate's explicit binding plan or
merge explicit binding upserts and withdrawals into it by property name while preserving
deterministic fallback for unbound route placeholders and remaining request-body fields, now also
lets hosts clear that explicit plan entirely when the effective route can still bind truthfully
through the implicit baseline, allows
route-pattern rewrites that remove placeholders when the original projection already
exposes explicit route-binding coverage for the original placeholder set and the effective explicit
binding plan keeps every affected original route-bound property explicitly bound, allows
route-pattern rewrites that add placeholders when the effective explicit route-binding plan covers
the full final placeholder set and every newly route-bound property was either already explicitly
bound in the original projection, for `POST`/`PUT`/`PATCH` already part of the original
deterministic remaining-body fallback surface, or for shorthand candidates with no explicit
binding plan already part of the original implicit query-fallback surface, allows
`RouteGroupPrefix` rewrites only when the published group stays beneath the active REST root,
contains no placeholders, and does not silently change effective API-version truth, now makes
ASP.NET Core materialize split effective route groups when one authored shorthand group fans out
to more than one published group, rejects any one rule that tries to both set and clear the same
endpoint-metadata field, and does not yet support route rewrites that promote other
implicit properties into placeholders or broader host-level binding rewrites beyond the current
replace-plus-merge-explicit
upsert-plus-withdraw model that would silently change how one shorthand endpoint reads its input.

Within that bounded capability-governance slice, `ClearRequiredCapability = true` is the explicit
host answer for removing an inherited shorthand capability boundary. It is mutually exclusive with
`RequiredCapabilityKey`, remains visible through `/engine/rest-endpoint-overrides`, and must drive
the same `null` effective capability boundary across the shorthand candidate projection, the
materialized ASP.NET Core endpoint metadata, `/engine/rest-endpoints`, and `snapshot.RestEndpoints`
rather than leaving the earlier shorthand guard in place invisibly.

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
  `ENG-058-T70`, `ENG-058-T71`, `ENG-058-T72`, `ENG-058-T73`, `ENG-058-T74`, `ENG-058-T75`,
  `ENG-058-T82`, `ENG-058-T97`, `ENG-058-T98`, `ENG-058-T99`, `ENG-058-T105`, `ENG-058-T107`,
  and `ENG-058-T108`, so ASP.NET Core hosts can retarget descriptor-backed shorthand candidates
  through `RestApi:Overrides` when they need a different effective `ApiVersionMajor`,
  `OpenApiDocumentName`, HTTP `Method`, bounded published `RouteGroupPrefix`, constrained
  relative `Pattern`, required capability boundary, endpoint metadata, tag, or explicit binding
  plan, while the runtime keeps both the configured override-rule catalog and the candidate-level
  `AppliedOverrideId`, `SelectedOverrideId`, and `MatchedOverrideIds` truth visible
- the next governance-overlap visibility follow-through is now shipped through `ENG-058-T87`, so
  overlapping shorthand suppression/override matches now stay visible in runtime truth through the
  ordered `MatchedSuppressionIds` and `MatchedOverrideIds` lists before one rule wins by the
  existing specificity model
- the candidate-runtime status contract now also uses stable `published` / `suppressed` wire names
  through `RestEndpointCandidateStatus`, so `/engine/rest-endpoint-candidates`,
  `/engine/rest-endpoint-candidates/{candidateId}`, and `snapshot.RestEndpointCandidates` no
  longer rely on raw enum-number serialization for operator-facing publication state
- the next selector-targeting follow-through is now shipped through `ENG-058-T77`,
  `ENG-058-T109`, `ENG-058-T118`, and `ENG-058-T120`, so both
  `RestApi:Suppressions` and `RestApi:Overrides` can refine that same descriptor-backed shorthand
  scope with `ApiVersionMajors`, `Methods`, `RelativePatterns`, `RouteGroupPrefixes`,
  `OpenApiDocumentNames`, `TagNames`, `BindingFallbackModes`, and exact original explicit
  `TargetBindings` selector sets while the runtime keeps both the configured rule catalogs and the
  original-shape targeting truth visible
- the next constrained shorthand REST implicit query-fallback route promotion is now shipped
  through `ENG-058-T88`, so shorthand candidates with no explicit binding plan can promote
  placeholders from the original implicit query-fallback surface while explicit-binding candidates
  stay on the stricter explicit-binding path
- the next partial-explicit-override follow-through is now shipped through `ENG-058-T89`, so when
  a shorthand candidate originally had no explicit binding plan, hosts can add partial explicit
  bindings without losing the remaining implicit query-fallback surface for unbound properties, and
  runtime truth now keeps that preserved mode visible through
  `metadata.bindingFallbackMode = preserve-source-implicit-fallback`
- the next typed binding-fallback runtime-contract follow-through is now shipped through
  `ENG-058-T90`, so that preserved shorthand fallback answer no longer lives only in additive
  metadata: `Cephalon.Abstractions` now exposes `RestEndpointBindingFallbackMode` plus typed
  `BindingFallbackMode` properties on both `RestEndpointCandidateProjectionDescriptor` and
  `RestEndpointRuntimeDescriptor`, runtime JSON now serializes that typed answer directly on the
  original shorthand projection and the effective published endpoint, and
  `metadata.bindingFallbackMode = preserve-source-implicit-fallback` remains compatibility-only
  metadata
- the next explicit-profile authoring parity follow-through is now shipped through `ENG-058-T119`,
  so explicit metadata-only REST profiles can now preserve that same source implicit query fallback
  intentionally by setting `BehaviorRestProfile(PreserveImplicitQueryFallback = true)` when at
  least one explicit binding is present; build-time `ABT0027` and runtime profile normalization now
  both fail fast when that flag is declared without explicit bindings
- the next explicit-binding remaining-body-fallback truth follow-through is now shipped through
  `ENG-058-T114`, so body-capable shorthand candidates with explicit bindings now also publish
  typed `BindingFallbackMode = PreserveRemainingBodyFallback` when unbound properties still remain
  on the deterministic request-body surface, and compatibility
  `metadata.bindingFallbackMode = preserve-remaining-body-fallback` remains additive-only metadata
- the next published-endpoint provenance follow-through is now shipped through `ENG-058-T91`, so
  the final `/engine/rest-endpoints` surface no longer needs route-text guessing to reconnect with
  the shorthand-candidate story: `RestEndpointRuntimeDescriptor` now exposes typed nullable
  `CandidateId`, behavior-backed published endpoints now point back to the original candidate id
  even when host overrides rewrite the final route shape, and manual endpoints stay `null`
- the next published-endpoint authorship follow-through is now shipped through `ENG-058-T92`, so
  published endpoints no longer need `metadata.authoringStyle` as the canonical origin answer:
  `RestEndpointRuntimeDescriptor` now exposes first-class `AuthoringStyle`, runtime JSON now
  serializes that property directly on `/engine/rest-endpoints` plus `snapshot.RestEndpoints`, and
  additive `metadata.authoringStyle` remains compatibility-only metadata
- the next published route-boundary follow-through is now shipped through `ENG-058-T93`, so
  published endpoints no longer need `metadata.routeGroupPrefix` or
  `metadata.relativePattern` as the canonical grouped-route answer:
  `RestEndpointRuntimeDescriptor` now exposes first-class `RouteGroupPrefix` and
  `RelativePattern`, runtime JSON now serializes those properties directly on
  `/engine/rest-endpoints` plus `snapshot.RestEndpoints`, and additive
  `metadata.routeGroupPrefix` plus `metadata.relativePattern` remain compatibility-only metadata
- the next published behavior-implementation follow-through is now shipped through `ENG-058-T94`,
  so published endpoints no longer need `metadata.behaviorType` as the canonical behavior-backed
  implementation answer: `RestEndpointRuntimeDescriptor` now exposes first-class nullable
  `BehaviorType`, runtime JSON now serializes that property directly on `/engine/rest-endpoints`
  plus `snapshot.RestEndpoints`, manual non-behavior endpoints stay `null`, and additive
  `metadata.behaviorType` remains compatibility-only metadata
- the next published source-identity follow-through is now shipped through `ENG-058-T95`, so
  published endpoints no longer need `metadata.sourceId` as the canonical source-identity answer:
  `RestEndpointRuntimeDescriptor` now exposes first-class nullable `SourceId`, runtime JSON now
  serializes that property directly on `/engine/rest-endpoints` plus `snapshot.RestEndpoints`, and
  additive `metadata.sourceId` remains compatibility-only metadata
- the next route-boundary materialization follow-through is now shipped through `ENG-058-T96`, so
  shorthand ASP.NET Core materialization now reads `ProjectedEndpoint.RouteGroupPrefix` directly
  when it publishes effective route groups instead of parsing `metadata.routeGroupPrefix`, and
  additive `metadata.routeGroupPrefix` now stays compatibility-only even for internal route mapping
- the next shorthand endpoint-metadata governance follow-through is now shipped through
  `ENG-058-T97`, so `RestApi:Overrides` can now also rewrite shorthand `EndpointName`, `Summary`,
  and `Description`, those effective metadata values now flow through shorthand candidate truth,
  actual ASP.NET Core endpoint metadata, `/engine/rest-endpoints`, and `snapshot.RestEndpoints`,
  and metadata-only no-op rules do not pretend they changed the effective published answer
- the next shorthand capability-boundary governance follow-through is now shipped through
  `ENG-058-T98`, so `RestApi:Overrides` can now also rewrite shorthand
  `RequiredCapabilityKey`, that effective value now flows through shorthand candidate truth,
  actual ASP.NET Core endpoint metadata, `/engine/rest-endpoints`, and `snapshot.RestEndpoints`,
  and `RequireCapability(...)` now treats the last declared capability boundary as authoritative so
  a later host override can supersede an earlier shorthand `configureEndpoint` guard instead of
  stacking both boundaries
- the next shorthand capability-boundary clear follow-through is now shipped through
  `ENG-058-T99`, so `RestApi:Overrides` can now also declare
  `ClearRequiredCapability = true` for descriptor-backed shorthand candidates, that action stays
  visible through `/engine/rest-endpoint-overrides` plus `snapshot.RestEndpointOverrides`,
  shorthand candidate projections plus actual ASP.NET Core endpoint metadata plus
  `/engine/rest-endpoints` now all agree on `RequiredCapabilityKey = null` when that clear wins,
  and hosts now fail fast if one override rule tries to both set `RequiredCapabilityKey` and clear
  it in the same action
- the next published capability-provenance follow-through is now shipped through `ENG-058-T100`,
  so `RestEndpointRuntimeDescriptor` now also exposes nullable
  `OriginalRequiredCapabilityKey` plus `AppliedOverrideId`, shorthand materialization captures the
  source capability boundary before later host capability overrides run, `/engine/rest-endpoints`
  plus `snapshot.RestEndpoints` now show both the original and effective capability answers
  directly, and capability-only no-op clear rules no longer claim an endpoint-level applied
  override when the published capability answer stays unchanged
- the next published override-match visibility follow-through is now shipped through
  `ENG-058-T102`, so `RestEndpointRuntimeDescriptor` now also exposes ordered
  `MatchedOverrideIds`, shorthand materialization carries that same ordered match set onto the
  final published endpoint surface, `/engine/rest-endpoints` plus `snapshot.RestEndpoints` no
  longer need a candidate join to explain which shorthand override rules matched a live published
  endpoint, and capability-only no-op matches now keep `MatchedOverrideIds` visible there even
  when `AppliedOverrideId` stays `null`
- the next published original-shorthand-visibility follow-through is now shipped through
  `ENG-058-T103`, so `RestEndpointRuntimeDescriptor` now also exposes nullable
  `OriginalProjection`, shorthand materialization carries the pre-override shorthand method, route,
  document-version, and binding-plan truth onto the final published endpoint surface, and
  `/engine/rest-endpoints` plus `snapshot.RestEndpoints` can now compare original-versus-effective
  publication directly without joining back to the candidate catalog while manual and
  behavior-helper endpoints stay `null`
- the next published original-shorthand endpoint-metadata follow-through is now shipped through
  `ENG-058-T104`, so `RestEndpointRuntimeDescriptor` now also exposes nullable
  `OriginalEndpointName`, `OriginalSummary`, and `OriginalDescription`, shorthand materialization
  carries that pre-override endpoint-metadata truth onto the final published endpoint surface, and
  `/engine/rest-endpoints` plus `snapshot.RestEndpoints` can now compare original-versus-effective
  endpoint metadata directly while manual and behavior-helper endpoints keep that trio `null`
- the next shorthand endpoint-metadata clear-governance follow-through is now shipped through
  `ENG-058-T105`, so `RestEndpointOverrideDescriptor` plus `RestEndpointOverrideOptions` now also
  expose `ClearEndpointName`, `ClearSummary`, and `ClearDescription`, hosts now fail fast when one
  rule tries to both set and clear the same endpoint-metadata field, shorthand candidate
  projections plus actual ASP.NET Core endpoint metadata plus `/engine/rest-endpoints` now all
  agree on `null` when an endpoint-metadata clear wins, and the original shorthand endpoint-
  metadata lineage still stays visible through `OriginalEndpointName`, `OriginalSummary`, and
  `OriginalDescription`
- the next shorthand metadata-and-document truthfulness follow-through is now shipped through
  `ENG-058-T106`, `ENG-058-T107`, `ENG-058-T108`, `ENG-058-T109`, `ENG-058-T110`,
  `ENG-058-T111`, `ENG-058-T112`, `ENG-058-T113`, `ENG-058-T114`, `ENG-058-T115`, and
  `ENG-058-T116`: same-value endpoint-metadata, tag, and document-name rewrites plus reorder-only
  equivalent binding-set rewrites now keep `MatchedOverrideIds` visible while leaving
  `AppliedOverrideId = null`, tag rewrites preserve `OriginalProjection.TagName` while
  splitting effective materialized groups, document-name governance now adds
  `OpenApiDocumentName` plus `.WithOpenApiDocumentName(...)`, explicit `.ApiVersion(...)`
  precedence remains authoritative for version selection even when later host governance rewrites
  the effective published document name, and selector targeting can now distinguish candidates by
  original shorthand document name plus primary tag name without depending on rewritten routes,
  while `ClearBindings` now resets shorthand explicit binding plans back to the implicit
  route/query/body baseline, keeps that reset truthful through the override, candidate, endpoint,
  and snapshot catalogs, fails fast when the effective route would only remain satisfiable through
  removed explicit placeholder aliases, and keeps source explicit binding order authoritative when
  a matched override only restates the same normalized binding set in another order; that same
  semantic binding-set comparison now also drives projection reuse and post-materialization
  structural override reconciliation so future shorthand paths cannot regress into order-sensitive
  endpoint provenance, while `Cephalon.Behaviors.Http` now also publishes stable startup
  diagnostics for suppression, precedence suppression, applied overrides, no-op override matches,
  and preserved binding fallback through `/engine/diagnostics` source `Cephalon.Behaviors.Http`
  and information-level logs that reconcile published answers against post-materialization truth
  whenever the runtime candidate registry is active, while explicit body-capable shorthand binding
  plans that still preserve deterministic remaining request-body fallback now also keep that typed
  `BindingFallbackMode = PreserveRemainingBodyFallback` answer aligned across
  `OriginalProjection`, `ProjectedEndpoint`, `/engine/rest-endpoints`, and
  `snapshot.RestEndpoints`, `ENG-058-T115` now keeps event `5204`
  `RestEndpointBindingFallbackPreserved` aligned with that same broader runtime truth so startup
  logging emits whichever typed fallback mode was preserved, including both
  `PreserveSourceImplicitFallback` and `PreserveRemainingBodyFallback`, `ENG-058-T135` now
  also keeps that startup log payload on the same stable
  `preserve-source-implicit-fallback` / `preserve-remaining-body-fallback` wire names the runtime
  contract and compatibility metadata already use, and
  `ENG-058-T116` now centralizes the stable compatibility wire names behind
  `RestEndpointBindingFallbackModeExtensions.GetWireName()` /
  `TryParseWireName(...)` so ASP.NET Core metadata plus tests no longer maintain independent
  hardcoded fallback-mode strings
- the next binding-fallback selector-targeting follow-through is now shipped through
  `ENG-058-T118`, so both `RestApi:Suppressions` and `RestApi:Overrides` can now refine original-
  shape shorthand governance with `BindingFallbackModes`, the runtime suppression/override
  catalogs now publish those configured selectors directly, specificity now counts that extra
  dimension consistently, and hosts can target the original shorthand fallback identity through the
  stable wire names `preserve-source-implicit-fallback` and
  `preserve-remaining-body-fallback` without depending on later rewritten published shape;
  `ENG-058-T136` now also makes those stable wire names the only accepted host-config input for
  `BindingFallbackModes`, so runtime truth, diagnostics, and config no longer mix PascalCase enum
  aliases into the operator contract
- the next original-binding-plan selector-targeting follow-through is now shipped through
  `ENG-058-T120`, so both `RestApi:Suppressions` and `RestApi:Overrides` can now refine original-
  shape shorthand governance with exact `TargetBindings` selector sets, the runtime
  suppression/override catalogs now publish those configured selectors directly, specificity now
  counts that extra dimension consistently, and hosts can target route-only versus richer explicit-
  binding shorthand candidates by their authored binding plan without depending on later rewritten
  published shape
- the next metadata-authoring parity follow-through is now also shipped through `ENG-058-T119`, so
  that same `preserve-source-implicit-fallback` story is no longer limited to no-explicit-plan
  shorthand candidates plus later host overrides; explicit metadata-only profiles can now opt into
  it directly through `BehaviorRestProfile(PreserveImplicitQueryFallback = true)` when they also
  declare explicit bindings, and both build-time `ABT0027` plus runtime profile normalization keep
  invalid no-binding cases from leaking into shorthand publication
- controlled configuration overrides that promote implicit properties into route placeholders
  beyond the shipped constrained remaining-body-fallback-plus-bounded-query-fallback path, or
  rewrite input binding beyond constrained explicit-binding replacement, remain later work now that
  the current override baseline covers original-shape candidate ids plus selector targeting across
  version, method, route-group prefix, document name, tag name, binding fallback, exact original
  explicit binding plans, capability, bindings, clear-bindings resets, placeholder reshaping, and
  bounded fallback promotion

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
- those grouped publication entries now also expose `AuthoringStyleSummaries`, derived from the
  same candidate set, so operators can inspect per-style participation, candidate ids, precedence
  ranks, published candidates, and suppression buckets without manually splitting the ordered
  behavior-level candidate set
- those grouped publication entries now also expose `AuthoringPolicy` as typed
  `RestEndpointPublicationGroupAuthoringPolicyDescriptor` data, so operators can distinguish the
  implicit default single-winner boundary from an explicit
  `RestApi:AuthoringPolicies:{behaviorId}` configuration and see preferred/allowed/disallowed
  authoring-style policy plus authoring-policy-suppressed candidate outcomes without inferring
  either answer from roadmap text alone
- those grouped publication entries now also expose typed
  `AuthoringPolicySuppressionSummaries` at both the grouped behavior level and inside each
  `AuthoringStyleSummaries` entry, so operators can read the per-kind
  `disallowed-authoring-style`, `not-allowed-authoring-style`, and
  `preferred-authoring-style-selected` suppression breakdown directly from publication-group and
  snapshot payloads instead of reconstructing that answer candidate by candidate
- the shipped REST authoring-policy runtime baseline now also exposes
  `IRestEndpointAuthoringPolicyRuntimeCatalog`, `/engine/rest-endpoint-authoring-policies`,
  `/engine/rest-endpoint-authoring-policies/{behaviorId}`, and
  `snapshot.RestEndpointAuthoringPolicies`, so one behavior-level policy answer can keep explicit
  default-versus-configured policy intent, explicitly configured-but-unmatched policies, and
  separate `CandidateIds`, `RetainedCandidateIds`, `PublishedCandidateIds`,
  `PrecedenceSuppressedCandidateIds`, `GovernanceSuppressedCandidateIds`, and grouped
  `SuppressionSummaries` visible without reopening grouped publication answers
- that same rule-centric authoring-policy answer now also exposes typed
  `AuthoringStyleSummaries` entries through
  `RestEndpointAuthoringPolicyAuthoringStyleDescriptor`, so operators can inspect per-style
  `CandidateIds`, `RetainedCandidateIds`, `PublishedCandidateIds`,
  `PrecedenceSuppressedCandidateIds`, `GovernanceSuppressedCandidateIds`,
  `SuppressedCandidateIds`, and grouped suppression summaries without falling back to
  publication-group-only answers
- those grouped publication entries now also expose typed `GovernanceSuppressionSummaries` and
  `GovernanceOverrideSummaries` at both the grouped behavior level and inside each
  `AuthoringStyleSummaries` entry, so publication-group and snapshot payloads can answer which
  host rules matched, which candidates they actually suppressed, and which candidates they only
  selected versus materially changed without re-reading the candidate catalog
- those same grouped governance summaries now also expose typed selection-basis and override-action
  buckets, so grouped publication answers can say why a winning host rule beat the runner-up and
  which override dimensions only stayed declared versus materially applied without reopening the
  candidate catalog
- those candidate entries now keep the original shorthand source shape visible through
  `RestEndpointCandidateRuntimeDescriptor.OriginalProjection` while `ProjectedEndpoint` continues
  to answer the final effective mapped route, version, method, binding, endpoint-name, summary,
  and description shape and now also keeps original shorthand endpoint metadata visible through
  `ProjectedEndpoint.OriginalEndpointName`, `ProjectedEndpoint.OriginalSummary`, and
  `ProjectedEndpoint.OriginalDescription`
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
- those same rule catalogs now also derive rule-centric runtime-effect buckets directly from
  candidate truth: suppression descriptors surface `MatchedCandidateIds`,
  `SuppressedCandidateIds`, `SkippedCandidateIds`, `SelectionBases`, and grouped
  `SelectionBasisSummaries`, while override descriptors surface `MatchedCandidateIds`,
  `SelectedCandidateIds`, `AppliedCandidateIds`, `SkippedCandidateIds`, `SelectionBases`,
  grouped `SelectionBasisSummaries`, `SelectedActionKinds`, grouped
  `SelectedActionKindSummaries`, `AppliedActionKinds`, and grouped
  `AppliedActionKindSummaries`, so operators can answer per-rule runtime effect and per-bucket
  provenance without reopening `/engine/rest-endpoint-candidates` or
  `/engine/rest-endpoint-publication-groups`
- the shipped `RestApi:Suppressions` baseline is intentionally limited to suppression of
  descriptor-backed shorthand candidates, and the shipped `RestApi:Overrides` baseline is
  intentionally limited to shorthand `ApiVersionMajor`, `OpenApiDocumentName`, `Method`, bounded
  `RouteGroupPrefix`, constrained relative `Pattern`, `RequiredCapabilityKey`,
  `ClearRequiredCapability`, `EndpointName`, `Summary`, `Description`, `TagName`,
  `ClearEndpointName`, `ClearSummary`, `ClearDescription`, constrained explicit `Bindings`
  rewrites with either default full replacement or typed `merge-explicit` property upserts plus
  `RemovedBindingProperties`, and shorthand-only `ClearBindings` resets back to the implicit
  request-binding baseline; neither surface rewrites explicit module DSL or manual routes
- both rule families can now target exact original-shape shorthand candidates through
  `CandidateIds`, can also refine `Behaviors`/`Modules` targeting with `ApiVersionMajors`,
  `Methods`, `RelativePatterns`, `RouteGroupPrefixes`, `OpenApiDocumentNames`, `TagNames`, and
  `BindingFallbackModes`, and all of those selectors match the original shorthand candidate
  identity before override actions are applied so suppression and override decisions do not depend
  on already-rewritten final route shape; `BindingFallbackModes` uses the stable wire names
  `preserve-source-implicit-fallback` and `preserve-remaining-body-fallback`
- `/engine/rest-endpoint-candidates` now publishes that same original-shape candidate identity
  through `RestEndpointCandidateRuntimeDescriptor.Id`, while `ProjectedEndpoint.Id` remains the
  effective mapped endpoint identity after override actions are applied
- shorthand candidate projected endpoints now also keep endpoint names plus summary/description
  metadata aligned with the final behavior-backed runtime endpoint conventions, including XML-
  derived summaries/descriptions when those docs exist and module-description fallback when they do
  not
- when REST governance rewrites or clears shorthand `EndpointName`, `Summary`, or `Description`,
  the same effective answer must drive `ProjectedEndpoint`, actual ASP.NET Core endpoint metadata,
  `/engine/rest-endpoints`, and `snapshot.RestEndpoints`; metadata-only no-op matches should not
  pretend they changed the published answer, and `OriginalEndpointName`,
  `OriginalSummary`, and `OriginalDescription` should remain available so operators can still see
  the source shorthand metadata after a clear wins
- when REST governance rewrites shorthand `TagName`, the same effective answer must drive
  `ProjectedEndpoint.Tags`, actual ASP.NET Core endpoint tag metadata, `/engine/rest-endpoints`,
  and `snapshot.RestEndpoints` while `OriginalProjection.TagName` continues to preserve the source
  shorthand tag lineage, and same-value tag rewrites should keep `MatchedOverrideIds` visible while
  leaving `AppliedOverrideId = null`
- when REST governance rewrites shorthand `OpenApiDocumentName`, the same effective answer must
  drive `ProjectedEndpoint.OpenApiDocumentName`, actual ASP.NET Core endpoint-group metadata,
  `/engine/rest-endpoints`, and `snapshot.RestEndpoints` while
  `OriginalProjection.OpenApiDocumentName` continues to preserve the source shorthand document
  lineage, and same-value document rewrites should keep `MatchedOverrideIds` visible while leaving
  `AppliedOverrideId = null`
- when REST governance clears shorthand explicit bindings through `ClearBindings`, the same
  effective answer must drive `ProjectedEndpoint.BindingDescriptors`, actual ASP.NET Core request
  composition, `/engine/rest-endpoints`, and `snapshot.RestEndpoints`, and that clear must fail
  fast when the effective route would only remain valid through explicit placeholder aliases that
  the source shorthand binding plan had been supplying
- when published candidate or endpoint runtime truth is reconciled after ASP.NET Core
  materialization, metadata-only same-value rewrites, same-value document rewrites, same-value tag
  rewrites, and metadata-clear matches against source metadata the module already set or cleared should keep
  `MatchedOverrideIds` visible while leaving `AppliedOverrideId = null`; those no-op wins should
  still surface the winning rule through `SelectedOverrideId` plus the decisive specificity answer
  through `OverrideSelectionBasis`
- when REST governance rewrites shorthand `RequiredCapabilityKey` or clears it through
  `ClearRequiredCapability`, the same effective answer must drive
  `ProjectedEndpoint.RequiredCapabilityKey`, actual ASP.NET Core endpoint metadata,
  `/engine/rest-endpoints`, and `snapshot.RestEndpoints`; `RequireCapability(...)` and
  `ClearRequiredCapability()` now both follow last-declaration-wins so a host can intentionally
  replace or remove an earlier shorthand capability boundary without leaving stacked or hidden
  guards behind
- when published endpoint runtime truth needs to explain shorthand capability governance,
  `/engine/rest-endpoints` and `snapshot.RestEndpoints` now also expose
  `OriginalRequiredCapabilityKey`, `AppliedOverrideId`, `SelectedOverrideId`, and
  `OverrideSelectionBasis`, so the final published surface can
  distinguish “no capability boundary ever existed” from “a source capability boundary was cleared
  or rewritten” without forcing operators to reconstruct that answer only from candidate joins; the
  endpoint-level `AppliedOverrideId` should remain `null` for capability-only no-op matches whose
  effective published capability answer does not change, while `SelectedOverrideId` plus
  `OverrideSelectionBasis` still answer which rule won and why
- when published endpoint runtime truth needs to explain shorthand governance overlap directly,
  `/engine/rest-endpoints` and `snapshot.RestEndpoints` should also expose the ordered
  `MatchedOverrideIds` set from the originating shorthand candidate so operators can see matched
  override rules, including capability-only no-op matches, without joining back to
  `/engine/rest-endpoint-candidates`
- within that constrained pattern slice, placeholder-preserving rewrites stay the default and
  placeholder renames now also work when the effective explicit route-binding plan covers the
  renamed placeholder set exactly, placeholder removals now also work when the original
  projection already exposes explicit route-binding coverage for the original placeholder set and
  the effective explicit binding plan keeps every affected original route-bound property
  explicitly bound, placeholder additions now also work when the effective explicit route-binding
  plan covers the full final placeholder set and every newly route-bound property was either
  already explicitly bound in the original projection, for `POST`/`PUT`/`PATCH` already part of
  the original deterministic remaining-body fallback surface, or for shorthand candidates with no
  explicit binding plan already part of the original implicit query-fallback surface, bounded
  `RouteGroupPrefix` rewrites now also stay below the active REST root with no placeholders and no
  silent API-version drift, ASP.NET Core now materializes split effective groups when only some
  shorthand candidates in one authored group are remapped, moved to another document, or retagged
  so route mapping, endpoint-group metadata, endpoint tag metadata, and runtime catalogs stay
  aligned, no-explicit-plan shorthand candidates now also preserve their remaining implicit
  query-fallback surface when hosts add only partial
  explicit bindings, typed `BindingFallbackMode` values backed by
  `RestEndpointBindingFallbackMode` now keep that preserved mode visible on both candidate and
  published endpoint runtime surfaces, additive
  `metadata.bindingFallbackMode = preserve-source-implicit-fallback` and
  `metadata.bindingFallbackMode = preserve-remaining-body-fallback` remain compatibility-only
  metadata, explicit metadata-only profiles can now also opt into that same source implicit
  query-fallback answer from the start through
  `BehaviorRestProfile(PreserveImplicitQueryFallback = true)` when they already declare at least
  one explicit binding, and broader implicit-property promotion beyond that bounded query-fallback
  slice remains later work
- future shorthand or convention REST publication must compose through the shared projection,
  runtime-catalog, and collision-validation pipeline instead of bypassing it
- future agentic, AI, or multi-platform expansion should not outrun core engine contract quality,
  performance, security, and maintainability

## Near-term follow-through candidates

Recommended implementation sequence after the shipped normalization, runtime-catalog,
precedence-visibility, generated-module, and rule-centric authoring-policy follow-through slices:

1. extend low-code module-owned shorthand only when the projected publication source still keeps
   module ownership, candidate identity, and grouped runtime truth readable
2. extend the shipped suppression-plus-override governance baseline further only if those stronger
   contracts keep the runtime truth model understandable instead of introducing hidden rule layers
3. only then evaluate whether any additional convention-backed publication sources are worth the
   added complexity beyond the shipped `MapProfile<TBehavior>()` and `MapGeneratedProfiles(...)`
   surfaces

