# Cephalon.AspNetCore

`Cephalon.AspNetCore` is the HTTP-first host core for Cephalon.

## What it owns

- ASP.NET Core service registration for the engine
- project-level split-configuration loading through `AddCephalonProjectConfigurations()` and `AddCephalon(...)`
- runtime startup and shutdown integration through hosted services
- `/engine/*` metadata, status, diagnostics, and policy endpoints
- `/engine/package-policy`, `/engine/packages`, and the rest of the engine governance surface
- `/health`, `/health/live`, and `/health/ready` surfaces
- opt-in HTTP request/response logging with bounded request and response body capture plus default sensitive-value redaction under `Engine:Observability:HttpLogging`
- OpenAPI and Scalar integration for REST APIs
- optional hosted reference-doc delivery through `ReferenceDocs` host configuration
- built-in REST, SSE, and WebSocket transport route mapping
- companion adapter hooks for GraphQL, JSON-RPC, and gRPC transport packages

## Main surfaces

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
payloads or transport-neutral `BehaviorResult<T>` outcomes through `ResultModel<T>` /
`ResultModelError` on the wire. That setting is intentionally REST-only. GraphQL keeps the standard
`data` / `errors` contract, JSON-RPC keeps the standard `result` / `error` contract, and generic
behavior HTTP bindings do not get forced through the REST envelope.

## Related docs

- [Architecture](../architecture.md)
- [Operations](../operations.md)
- [Reference docs publishing](../reference-docs.md)
