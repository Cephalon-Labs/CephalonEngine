# Cephalon.AspNetCore

`Cephalon.AspNetCore` is the HTTP-first host core for Cephalon.

## What it owns

- ASP.NET Core service registration for the engine
- project-level split-configuration loading through `AddCephalonProjectConfigurations()` and `AddCephalon(...)`
- runtime startup and shutdown integration through hosted services
- `/engine/*` metadata, status, diagnostics, and policy endpoints
- `/engine/resilience` when the engine-owned resilience contract is active
- `/engine/rate-limiting` when ASP.NET Core rate-limiting enforcement is active
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

When teams use `Cephalon.Behaviors.Http` behavior-aware REST helpers, the resulting Minimal API endpoints flow through this same host-level OpenAPI + Scalar pipeline rather than requiring a separate documentation surface. That same pipeline now treats module-owned REST groups as the public REST documentation surface while keeping the generic behavior HTTP adapter endpoints out of REST OpenAPI + Scalar descriptions by default, so the docs do not show duplicate transport-adapter routes beside the module-owned API. `RestBehaviorModuleBase` is the low-ceremony authoring path for modules that both own behaviors and expose some of them over REST, while the ASP.NET Core host still maps those modules through the generic `IRestModule` contract.

By default the host registers the `v1` OpenAPI document and treats `/scalar/v1` as the canonical docs link by redirecting `/scalar` to the configured default document. The slash-suffixed Scalar shell at `/scalar/` still remains available for multi-document flows, and Cephalon's Scalar JavaScript normalizes hash-based selections such as `/scalar/#v2/` back into canonical versioned links. Hosts can move those surfaces through `OpenApi:RoutePattern` and `OpenApi:Scalar:RoutePrefix`, while the built-in REST mapper can move off `/api` through `ApiRoutes:Prefixes:Rest` or all the way to the version root with `ApiRoutes:Prefixes:Rest = ""`. The long-term versioned config contract is `OpenApi:EnabledVersions` plus `OpenApi:DefaultVersion`, for example `EnabledVersions: [1, 2]` and `DefaultVersion: 2`, so Scalar can render a version selector while endpoints mapped with `BehaviorRestEndpointGroup.ApiVersion(2)` or defaulted from a module version `2.x` continue to appear in `/openapi/v2.json` and under version-aligned REST paths such as `/api/v2/...` or `/v2/...` when the REST prefix is empty. Legacy `OpenApi:Documents` and `OpenApi:DefaultDocument` string settings still work for backward compatibility or custom non-version document names. `OpenApi:Version` remains available as a global `info.version` override for single-document hosts, but multi-document hosts now keep each document version truthful to its own resolved document name such as `v1` or `v2`.

When a host needs to trim or expand the default response set published for behavior-owned REST endpoints, use `OpenApi:BehaviorRest:DocumentedStatusCodes`. The default list is `[200, 201, 202, 204, 400, 401, 403, 404, 409, 500]`, which keeps `500` visible in Scalar/OpenAPI by default. Hosts can override that list with a smaller or larger HTTP-status set without changing the runtime behavior of the endpoints themselves. Cephalon still adds runtime-required answers back per route so the docs stay truthful: when `Engine:Resilience:RateLimiting:Enabled = true`, behavior-owned REST endpoints whose effective ASP.NET Core policy requires a limiter automatically publish `429`; when the effective behavior-execution bulkhead resolved for that route is active, those same REST helpers also publish `429`; and when the effective behavior-execution timeout resolved for that route is active, they also publish `503`. Behavior- or transport-scoped rate-limiting overrides under `Engine:Resilience:RateLimiting:Overrides` can disable the ASP.NET Core limiter for a subset of endpoints, and behavior-execution overrides under `Engine:Resilience:BehaviorExecution:Overrides` can disable or narrow inherited timeout-plus-bulkhead answers for specific behavior/transport combinations, so those endpoints omit host-level `429` / `503` responses they can no longer emit.

The same host layer also owns the prefix policy for the generic behavior HTTP bindings. Route-shaped
generic behavior transports now project canonical versioned paths through `ApiRoutes:Prefixes:Rest`,
`ApiRoutes:Prefixes:GraphQL`, `ApiRoutes:Prefixes:JsonRpc`, `ApiRoutes:Prefixes:Sse`,
`ApiRoutes:Prefixes:Ws`, `ApiRoutes:Prefixes:GraphQLWs`, `ApiRoutes:Prefixes:GraphQLSse`,
and `ApiRoutes:DefaultBehaviorDocumentName`. Older flat or behavior-specific prefix aliases are no
longer part of the public config contract. That lets a host keep
generic behavior REST on `/api/v1/...`, GraphQL on `/graphql/v1/...`, JSON-RPC on `/json-rpc/v1/...`,
GraphQL-over-SSE on `/graphql-sse/v1/...`, GraphQL-over-WebSocket on `/graphql-ws/v1/...`, SSE on
`/sse/v1/...`, and WebSocket on `/ws/v1/...`. The older `/behaviors/{id}` aliases are gone, so the
built-in host transport mappers follow the same canonical prefix set,
so GraphQL, JSON-RPC, gRPC, SSE, and WebSocket transports can all move together under the `ApiRoutes`
section instead of each surface inventing its own default root path. When generic behavior HTTP
routes and module-owned REST helpers both exist in one host, the generic routes keep running as
transport-adapter endpoints while the module-owned REST groups own the published REST OpenAPI tag,
summary, and description surface.

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
such as override ids and targeted behavior ids. When behavior-execution resilience is active,
`/engine/behavior-resilience` plus `/engine/behavior-resilience/{policyId}` publish the effective
shared timeout-plus-bulkhead answers enforced by `Cephalon.Behaviors`, including targeted behavior ids,
targeted transport ids, and explicit disable overrides, and `/engine/snapshot` carries the same answers
through `RateLimitingPolicies` plus `BehaviorResiliencePolicies`.

The first shipped runtime follow-through uses `Microsoft.AspNetCore.RateLimiting` as the ASP.NET Core
enforcement primitive for public Cephalon HTTP endpoints while intentionally excluding `/engine`,
`/health`, `/openapi`, the configured Scalar route prefix, `/favicon.ico`, and hosted reference-doc
routes so operator and documentation surfaces remain available under pressure. The baseline now resolves
named endpoint policies from `Engine:Resilience:RateLimiting` plus `Engine:Resilience:RateLimiting:Overrides`,
applies them with behavior-aware precedence across module-owned REST routes and generic behavior HTTP
bindings, and keeps Scalar/OpenAPI truthful per endpoint. The next shipped follow-through now adds a
shared behavior-dispatch middleware in `Cephalon.Behaviors` so timeout and bulkhead enforcement apply
consistently across transports, resolves narrower `Engine:Resilience:BehaviorExecution:Overrides` entries
with `behavior+transport > behavior > transport > default` precedence, and lets explicit disable
overrides suppress inherited enforcement cleanly. The REST helper layer translates those
behavior-execution rejections into truthful HTTP responses (`503` for timeout, `429` for bulkhead
saturation) while keeping OpenAPI in sync per route. Retry and circuit breaker remain contract-only
until later phase-11 work adds safe idempotency-aware enforcement.

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
