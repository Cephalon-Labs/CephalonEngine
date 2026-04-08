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

When teams use `Cephalon.Behaviors.Http` behavior-aware REST helpers, the resulting Minimal API endpoints flow through this same host-level OpenAPI + Scalar pipeline rather than requiring a separate documentation surface.

By default the host registers the `v1` OpenAPI document and treats `/scalar/v1` as the canonical docs link by redirecting `/scalar` to the configured default document. The slash-suffixed Scalar shell at `/scalar/` still remains available for multi-document flows, and Cephalon's Scalar JavaScript normalizes hash-based selections such as `/scalar/#v2/` back into canonical versioned links. Hosts can move those surfaces through `OpenApi:RoutePattern` and `OpenApi:Scalar:RoutePrefix`, while the built-in REST mapper can move off `/api` through `ApiRoutes:Prefixes:Rest`. The long-term versioned config contract is `OpenApi:EnabledVersions` plus `OpenApi:DefaultVersion`, for example `EnabledVersions: [1, 2]` and `DefaultVersion: 2`, so Scalar can render a version selector while endpoints mapped with `BehaviorRestEndpointGroup.ApiVersion(2)` or defaulted from a module version `2.x` continue to appear in `/openapi/v2.json` and under version-aligned REST paths such as `/api/v2/...`. Legacy `OpenApi:Documents` and `OpenApi:DefaultDocument` string settings still work for backward compatibility or custom non-version document names. `OpenApi:Version` remains available as a global `info.version` override for single-document hosts, but multi-document hosts now keep each document version truthful to its own resolved document name such as `v1` or `v2`. This round only versions the built-in REST surfaces and behavior-aware REST helpers; the generic behavior GraphQL/SSE/WebSocket bindings still keep their existing behavior-id routes until Cephalon grows a deeper transport-surface contract.

## Related docs

- [Architecture](../architecture.md)
- [Operations](../operations.md)
- [Reference docs publishing](../reference-docs.md)
