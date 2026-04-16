# Cephalon.AspNetCore

`Cephalon.AspNetCore` is the HTTP-first host core for Cephalon.

## What it owns

- ASP.NET Core service registration for the engine
- project-level split-configuration loading through `AddCephalonProjectConfigurations()` and `AddCephalon(...)`
- runtime startup and shutdown integration through hosted services
- `/engine/*` metadata, status, diagnostics, and policy endpoints
- `/engine/resilience` when the engine-owned resilience contract is active
- `/engine/strangler-fig` and `/engine/strangler-fig/resolve` when the engine-owned strangler-fig runtime catalog is active
- `/engine/rate-limiting` when ASP.NET Core rate-limiting enforcement is active
- `/engine/rest-endpoint-candidates` when the module-owned REST candidate catalog is active
- `/engine/rest-endpoint-publication-groups` when grouped module-owned REST publication visibility is active
- `/engine/rest-endpoint-suppressions` for REST shorthand-governance visibility
- `/engine/rest-endpoint-overrides` for REST shorthand override-governance visibility
- `/engine/database-roles` when the engine-owned database-role catalog is active
- `/engine/database-migrations` when the engine-owned database-migration catalog is active
- `/engine/audit-history` and `/engine/audit-history/export` when durable audit-history services are active
- `/engine/event-dispatch-runtimes` and `/engine/event-dispatches` when eventing packs register dispatch-runtime descriptors or live dispatch-state reporters
- `/engine/package-policy`, `/engine/packages`, and the rest of the engine governance surface
- `/health`, `/health/live`, and `/health/ready` surfaces
- opt-in HTTP request/response logging with bounded request and response body capture plus default sensitive-value redaction under `Engine:Observability:HttpLogging`
- OpenAPI and Scalar integration for REST APIs
- optional hosted reference-doc delivery through `ReferenceDocs` host configuration
- built-in REST, SSE, and WebSocket transport route mapping
- companion adapter hooks for GraphQL, JSON-RPC, and gRPC transport packages

## Main surfaces

- `Hosting/AuditHistoryExportHttpResponseExtensions.cs`
- `Hosting/EngineWebApplicationBuilderExtensions.cs`
- `Hosting/EngineWebApplicationExtensions.cs`
- `Hosting/EngineHostedService.cs`
- `Hosting/HttpRequestResponseLoggingOptions.cs`
- `Hosting/ITransportRouteMapper.cs`
- `Documentation/ReferenceDocsHostingOptions.cs`
- `Documentation/ReferenceDocsSurface.cs`
- `Diagnostics/DiagnosticsSurface.cs`
- `Health/LivenessHealthCheck.cs`
- `Health/ReadinessHealthCheck.cs`
- `Transports/Rest/IRestModule.cs`
- `Transports/Rest/RestEndpointConventionBuilderExtensions.cs`
- `Transports/Rest/RestTransportRouteMapper.cs`
- `Transformers/*`

## Source structure

- `Diagnostics`
- `Documentation`
- `Health`
- `Hosting`
- `Modules`
- `Transformers`
- `Transports/Rest`
- `Transports/ServerSentEvents`
- `Transports/WebSockets`
- `wwwroot/js`
- `wwwroot/icons`

## How it fits

This package keeps the HTTP host thin. Most behavior stays in the engine or modules, while the host package maps runtime state and selected transports into ASP.NET Core primitives.

When teams choose to publish XML-comment-driven reference output, this host can also serve those static assets directly. That hosting surface is optional and sits beside the hand-authored `.md` guides instead of replacing them.

It also owns the ASP.NET Core side of Cephalon's split-configuration convention. Hosts can keep settings in `Configurations/Add*.json` and `Configurations/{group}/{Environment}.json`, and those files are loaded automatically when `AddCephalon(...)` runs.

When operators need deeper HTTP diagnostics, the same host surface can turn on request/response logging through `Engine:Observability:HttpLogging` or `AddCephalonHttpLogging(...)`. That flow keeps correlation in the shared `ILogger` pipeline by pushing `RequestId`, `TraceId`, `SpanId`, and `TraceParent` into request scopes, redacts known-sensitive query-string and payload fields before the log event is written, including JSON, form, and header-style `text/plain` content, and publishes the corresponding event-id range through `/engine/diagnostics`.

When teams use `Cephalon.Behaviors.Http` behavior-aware REST helpers, the resulting Minimal API endpoints flow through this same host-level OpenAPI + Scalar pipeline rather than requiring a separate documentation surface. That same pipeline now treats module-owned REST groups as the public REST documentation surface while keeping the generic behavior HTTP adapter endpoints out of REST OpenAPI + Scalar descriptions by default, so the docs do not show duplicate transport-adapter routes beside the module-owned API. `RestBehaviorModuleBase` remains the dedicated class-based authoring path for modules that both own behaviors and expose some of them over REST, while `engine.AddRestBehaviorModule<TMarker>(...)` now gives straightforward hosts a lower-ceremony inline registration path that still materializes as a real module and still maps through the generic `IRestModule` contract. Both paths now project DSL-backed and explicit manual module-owned REST endpoints into the same `/engine/rest-endpoints` runtime catalog plus duplicate-route validation baseline. For shorthand publication, that runtime answer now carries first-class `RestEndpointRuntimeDescriptor.AuthoringStyle` for explicit DSL, profile shorthand, generated shorthand, behavior-helper, and manual paths, first-class `RestEndpointRuntimeDescriptor.RouteGroupPrefix` plus `RelativePattern` for the resolved grouped publication boundary, first-class nullable `RestEndpointRuntimeDescriptor.BehaviorType` for behavior-backed implementation identity, first-class nullable `RestEndpointRuntimeDescriptor.SourceId` for published source identity, publishes the explicit route/query/header/body binding plan through the first-class `RestEndpointRuntimeDescriptor.BindingDescriptors` surface and the matching `bindingDescriptors` array in JSON responses when profile bindings exist, and also carries nullable `RestEndpointRuntimeDescriptor.CandidateId` so published behavior-backed endpoints can join back to the originating shorthand candidate directly. Additive `metadata.authoringStyle`, `metadata.routeGroupPrefix`, `metadata.relativePattern`, `metadata.behaviorType`, and `metadata.sourceId` remain available for compatibility, but they are no longer the canonical published-endpoint authorship, route-boundary, behavior-implementation, or source-identity answer. The same host now also publishes `/engine/rest-endpoint-candidates` plus `/engine/rest-endpoint-candidates/{candidateId}` and the matching `snapshot.RestEndpointCandidates` answer so operators can see the original shorthand projection through `OriginalProjection`, the final effective mapped answer through `ProjectedEndpoint`, and the published-versus-suppressed outcome plus any governing suppression or override rule. It now also publishes `/engine/rest-endpoint-publication-groups` plus `/engine/rest-endpoint-publication-groups/{behaviorId}` and the matching `snapshot.RestEndpointPublicationGroups` answer so the same runtime truth is grouped per behavior, including the published candidate ids, precedence-suppressed candidate ids, governance-suppressed candidate ids, winning precedence rank when one exists, and the ordered candidate set behind that grouped publication story. Within that normalized behavior-projection path, explicit module DSL wins over both shorthand modes, and `MapProfile<TBehavior>()` wins over generated shorthand when both target the same behavior.

The first shipped host-governance slices for that shorthand path are `RestApi:Suppressions` and `RestApi:Overrides`. Both are ASP.NET Core-only config surfaces for descriptor-backed shorthand candidates. `RestApi:Suppressions` can suppress `MapProfile<TBehavior>()` and `MapGeneratedProfiles(...)` candidates before precedence resolution, publishes the configured rules through `/engine/rest-endpoint-suppressions` plus `snapshot.RestEndpointSuppressions`, and marks suppressed candidates with `SuppressedBySuppressionId` so runtime truth distinguishes config governance from candidate-to-candidate precedence. `RestApi:Overrides` now supports `ApiVersionMajor`, `Method`, published `RouteGroupPrefix`, relative `Pattern`, explicit `Bindings`, `RemovedBindingProperties`, and typed `BindingMode` for those same shorthand candidates, publishes configured rules through `/engine/rest-endpoint-overrides` plus `snapshot.RestEndpointOverrides`, and records the applied rule on the candidate through `AppliedOverrideId`. Both surfaces can now also refine `Behaviors`/`Modules` targeting with `ApiVersionMajors`, `Methods`, `RelativePatterns`, and `RouteGroupPrefixes`; those selector refiners match the original shorthand candidate shape before override actions are applied, so one host can govern only one of several shorthand candidates that share the same behavior or module identity. Both rule families now fail fast when `Behaviors` and `Modules` are both missing; override rules also fail fast when they omit all override actions, use a non-positive `ApiVersionMajor`, declare an unsupported HTTP method, declare an invalid route pattern, declare an invalid `RouteGroupPrefix`, pair `ReplaceExplicit` with `RemovedBindingProperties`, try to remove and override the same property in one rule, or resolve to an invalid effective binding plan. When more than one rule matches the same candidate the host resolves the most specific rule deterministically by populated target dimensions first, then by behavior-targeted scope, narrower authoring-style scope, fewer total selector values, and finally stable rule-id ordering. That baseline remains intentionally narrow: explicit module DSL routes and manual module-owned REST endpoints stay authoritative, shorthand groups with explicit `.ApiVersion(...)` stay authoritative for version selection, and the current override slice rewrites only the effective API major version, HTTP method, bounded published route-group prefix, constrained relative route pattern, and/or explicit binding plan so the mapped endpoint plus the `/api/v{major}` route segment, published group boundary, OpenAPI document name, and runtime catalogs remain aligned instead of drifting apart. Pattern overrides in this slice can change static path segments, reorder existing placeholders, rename placeholders when the effective explicit route-binding plan covers the renamed placeholder set exactly, remove placeholders when the original projection already exposes explicit route-binding coverage for the original placeholder set and the effective explicit binding plan keeps every affected original route-bound property explicitly bound, and add placeholders when the effective explicit route-binding plan covers the full final placeholder set and every newly route-bound property was either already explicitly bound in the original projection or, for `POST`/`PUT`/`PATCH`, already part of the original deterministic remaining-body fallback surface; broader implicit-property promotion outside that constrained body-fallback path still fails fast. `RouteGroupPrefix` overrides stay shorthand-only, must remain beneath the active REST root, cannot declare placeholders, cannot silently change the effective API version, and now make the ASP.NET Core materializer split effective route groups when only one candidate in an authored shorthand group is remapped so actual mapped endpoints and the runtime catalogs keep the same truth. Binding overrides now default to replacing the shorthand candidate's explicit binding descriptors, but `BindingMode = MergeExplicit` can upsert changed explicit bindings or withdraw selected original explicit bindings through `RemovedBindingProperties` while still leaving unbound route placeholders plus remaining request-body fields available for deterministic fallback; removal targets must already exist in the source shorthand explicit binding plan so runtime truth does not drift into silent merge-time guesswork.

By default the host registers the `v1` OpenAPI document and treats `/scalar/v1` as the canonical docs link by redirecting `/scalar` to the configured default document. The slash-suffixed Scalar shell at `/scalar/` still remains available for multi-document flows, and Cephalon's Scalar JavaScript normalizes hash-based selections such as `/scalar/#v2/` back into canonical versioned links. Hosts can move those surfaces through `OpenApi:RoutePattern` and `OpenApi:Scalar:RoutePrefix`, while the built-in REST mapper can move off `/api` through `ApiRoutes:Prefixes:Rest` or all the way to the version root with `ApiRoutes:Prefixes:Rest = ""`. The long-term versioned config contract is `OpenApi:EnabledVersions` plus `OpenApi:DefaultVersion`, for example `EnabledVersions: [2, 3]` and `DefaultVersion: 3`. In that model, modules and endpoints still declare which document they belong to through `.ApiVersion(...)`, module-major defaults, or the constrained shorthand-only `RestApi:Overrides:*:ApiVersionMajor` governance path, but the host treats `EnabledVersions` as the published-document allow-list: only enabled docs are registered under `/openapi/{document}.json`, only enabled docs appear in Scalar's injected selector, and a disabled default falls back to the first enabled document instead of silently publishing an extra version. When more than one published document exists, Cephalon now mounts that selector into Scalar's own header bar so document switching stays inside the toolbar instead of floating over the page actions. When a host deliberately uses legacy `OpenApi:Documents` and `OpenApi:DefaultDocument` names instead of `v{major}`, the same allow-list semantics still apply and the injected selector keeps the more generic `Document` label instead of `Version`. `OpenApi:Version` remains available as a global `info.version` override for single-document hosts, but multi-document hosts now keep each document version truthful to its own resolved document name such as `v2` or `v3`.

When a host needs to trim or expand the default response set published for behavior-owned REST endpoints, use `OpenApi:BehaviorRest:DocumentedStatusCodes`. The default list is `[200, 201, 202, 204, 400, 401, 403, 404, 409, 500]`, which keeps `500` visible in Scalar/OpenAPI by default. Hosts can override that list with a smaller or larger HTTP-status set without changing the runtime behavior of the endpoints themselves. Cephalon still adds runtime-required answers back per route so the docs stay truthful: when `Engine:Resilience:RateLimiting:Enabled = true`, behavior-owned REST endpoints whose effective ASP.NET Core policy requires a limiter automatically publish `429`; when the effective behavior-execution bulkhead resolved for that route is active, those same REST helpers also publish `429`; and when the effective behavior-execution timeout or circuit breaker resolved for that route is active, they also publish `503`. Behavior- or transport-scoped rate-limiting overrides under `Engine:Resilience:RateLimiting:Overrides` can disable the ASP.NET Core limiter for a subset of endpoints, and behavior-execution overrides under `Engine:Resilience:BehaviorExecution:Overrides` can disable or narrow inherited timeout, circuit-breaker, or bulkhead answers for specific behavior/transport combinations, so those endpoints omit host-level `429` / `503` responses they can no longer emit.

The same host layer also owns the prefix policy for the generic behavior HTTP bindings. Route-shaped
generic behavior transports now project canonical versioned paths through `ApiRoutes:Prefixes:Rest`,
`ApiRoutes:Prefixes:GraphQL`, `ApiRoutes:Prefixes:JsonRpc`, `ApiRoutes:Prefixes:Sse`,
`ApiRoutes:Prefixes:Ws`, `ApiRoutes:Prefixes:GraphQLWs`, `ApiRoutes:Prefixes:GraphQLSse`, and
`ApiRoutes:DefaultBehaviorDocumentName`. When that explicit behavior-route override is not set,
Cephalon falls back to the raw configured `OpenApi:DefaultVersion` for the generic adapter route
segment. `OpenApi:EnabledVersions` and legacy `OpenApi:Documents` still govern only which
OpenAPI + Scalar documents get published; they do not suppress the generic behavior transport route
segment. Older flat or behavior-specific prefix aliases are no longer part of the public config
contract. That lets a host keep generic behavior REST on `/api/v1/...`, GraphQL on
`/graphql/v1/...`, JSON-RPC on `/json-rpc/v1/...`, GraphQL-over-SSE on `/graphql-sse/v1/...`,
GraphQL-over-WebSocket on `/graphql-ws/v1/...`, SSE on `/sse/v1/...`, and WebSocket on
`/ws/v1/...`. The older `/behaviors/{id}` aliases are gone, so the built-in host transport mappers
follow the same canonical prefix set, so GraphQL, JSON-RPC, gRPC, SSE, and WebSocket transports can
all move together under the `ApiRoutes` section instead of each surface inventing its own default
root path. When generic behavior HTTP routes and module-owned REST helpers both exist in one host,
the generic routes keep running as transport-adapter endpoints while the module-owned REST groups own
the published REST OpenAPI tag, summary, and description surface.

The built-in GraphQL host adapter now follows that same prefix contract while keeping protocol
surfaces explicit. By default `/graphql` handles GraphQL over HTTP, `/graphql/schema` serves the
schema document, `/graphql-sse` handles GraphQL-over-SSE, and `/graphql-ws` handles
GraphQL-over-WebSocket. Hosts can move those roots through `ApiRoutes:Prefixes:GraphQL`,
`ApiRoutes:Prefixes:GraphQLSse`, and `ApiRoutes:Prefixes:GraphQLWs` without falling back to a
separate GraphQL-specific route model.

The ASP.NET Core host also owns Cephalon's optional REST response envelope policy. When
`ApiRoutes:ResultEnvelope:Enabled = true`, module-owned REST endpoints can project raw behavior
payloads or transport-neutral `Result<T>` outcomes through `ResultModel<T>` /
`ResultModelError` on the wire, with structured failure details exposed through an `errors`
collection. That setting is intentionally REST-only. GraphQL keeps the standard
`data` / `errors` contract, JSON-RPC keeps the standard `result` / `error` contract, and generic
behavior HTTP bindings do not get forced through the REST envelope.

The same host adapter now carries the first durable audit-history export helper for HTTP responses. When
`Engine:Audit:History:Export:Enabled = true` and a provider pack such as
`Cephalon.Audit.EntityFramework` registers `IAuditHistoryExporter`, the engine route
`/engine/audit-history/export` becomes available and streams NDJSON with a bounded `maxEntries`
cap. Applications that want their own public export endpoint can reuse
`AuditHistoryExportHttpResponseExtensions.WriteAuditHistoryNdjsonAsync(...)` instead of rewriting
response headers and newline-delimited serialization by hand.

The host now also exposes the engine-owned database-role and database-migration catalogs directly.
`/engine/databases` remains the raw requested-topology answer projected from `AppProfile.Databases`,
while `/engine/database-roles` and `/engine/database-roles/{databaseRoleId}` publish the resolved
runtime truth for each active role, and `/engine/database-migrations` plus
`/engine/database-migrations/{databaseMigrationId}` publish the logical migration targets and their
current execution state. That split keeps requested configuration visible without losing
operator-facing answers such as requested-versus-resolved role ids, `UseRole` resolution,
consumers, co-location, audit-history metadata, live provider-contributed role health,
migration status, and provider-added deploy-time command templates such as Entity Framework
bundle/script/update guidance.

The host now exposes both requested and effective resilience answers. `/engine/resilience` returns
the requested resilience-policy selection projected into `AppProfile.Resilience`, covering `Retry`,
`Timeout`, `CircuitBreaker`, `Bulkhead`, and `RateLimiting`. When ASP.NET Core enforcement is active,
`/engine/rate-limiting` plus `/engine/rate-limiting/{policyId}` publish the effective public-HTTP
policies, covered transport ids, excluded route prefixes, rejection status, and host-specific metadata
such as override ids, targeted behavior ids, `transportKind`, `transportSemantics`,
`enforcementMoment`, and `longLivedTransportIds`. That lets operator tooling distinguish
request-response policies from long-lived stream or connection policies instead of flattening every
limiter into the same generic endpoint label. When behavior-execution resilience is active,
`/engine/behavior-resilience` plus `/engine/behavior-resilience/{policyId}` publish the effective
shared timeout, circuit-breaker, and bulkhead answers enforced by `Cephalon.Behaviors`, including
targeted behavior ids, targeted transport ids, explicit disable overrides, and live circuit metadata
such as open/half-open/closed state plus retry-after timing, and `/engine/snapshot` carries the same
answers through `RateLimitingPolicies` plus `BehaviorResiliencePolicies`.

The first shipped runtime follow-through uses `Microsoft.AspNetCore.RateLimiting` as the ASP.NET Core
enforcement primitive for public Cephalon HTTP endpoints while intentionally excluding `/engine`,
`/health`, `/openapi`, the configured Scalar route prefix, `/favicon.ico`, and hosted reference-doc
routes so operator and documentation surfaces remain available under pressure. The same ASP.NET Core
runtime now also keeps long-lived HTTP transport truth visible for stream and connection surfaces in
`/engine/rate-limiting` rather than treating GraphQL-SSE, GraphQL-WS, SSE, and WebSocket routes as
undifferentiated request-response endpoints. The behavior-pipeline
follow-through now adds a shared behavior-dispatch middleware in `Cephalon.Behaviors` so retry,
timeout, circuit-breaker, and bulkhead enforcement apply consistently across transports, resolves narrower
`Engine:Resilience:BehaviorExecution:Overrides` entries with
`behavior+transport > behavior > transport > default` precedence, and lets explicit disable overrides
suppress inherited enforcement cleanly. The REST helper layer translates those behavior-execution
rejections into truthful HTTP responses (`503` for timeout or an open circuit breaker, `429` for
bulkhead saturation) while keeping OpenAPI in sync per route and surfacing retry-after details for
open circuits. Retry now runs in that same shared pipeline only for explicitly idempotent behaviors
when the effective retry policy is active and the classifier marks the failure as transient, while
non-idempotent or unknown behaviors still fail without automatic replay.

The same host surface now also exposes the first shipped strangler-fig runtime answers directly.
`/engine/strangler-fig` and `/engine/strangler-fig/{routeId}` publish the active migration-route
catalog composed by `Cephalon.Engine`, while `/engine/strangler-fig/resolve` evaluates one
request-shaped `path` plus `method` pair through the host-agnostic `IStranglerFigRouter`. That
baseline keeps migration-route ownership and request-resolution truth operator-visible before
Cephalon adds any host-specific proxy or traffic-manager behavior.

The host now also exposes additive event-dispatch operator answers directly. When eventing packs
register the corresponding catalogs, `/engine/event-dispatch-runtimes` and
`/engine/event-dispatch-runtimes/{dispatchRuntimeId}` publish dispatch-runtime descriptors such as
runtime id, ownership metadata, bridge mode, the outbox/runtime ids a managed loop is responsible
for, and a canonical aggregate `Summary` once live reports exist. `/engine/outboxes` also carries
the effective `DispatchPolicy` object per outbox so the same ownership answer is visible from the
engine-owned outbox catalog. `/engine/event-dispatches` and `/engine/event-dispatches/{outboxId}`
remain the per-outbox detail surface, publishing the latest live dispatch state per outbox path,
including reported outcome, retry intent, timestamps, and totals from
`IEventDispatchRuntimeReporter`. Those same answers also flow into `/engine/snapshot` as
`EventDispatchRuntimes` and `EventDispatchStates`, which keeps operator tooling aligned across the
host route surface and the broader runtime snapshot without forcing adapter packs to re-aggregate
state by hand.

## Related docs

- [Architecture](../architecture.md)
- [Operations](../operations.md)
- [Reference docs publishing](../reference-docs.md)
