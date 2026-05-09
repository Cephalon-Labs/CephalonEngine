# Cephalon.AspNetCore

> **Maturity:** `M4` · **Ownership:** `cephalon-managed` — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.AspNetCore` is the HTTP-first host core for Cephalon.

See also: [Engine surface maturity audit](../engine-surface-maturity-audit.md), [Conformance matrix](../conformance-matrix.md), and [Runtime contract index](../runtime-contract-index.md) for the full `/engine/*` route catalog this host adapter projects, plus the per-package maturity and ownership truth. [Engineering standards](../engineering-standards.md) records the library-design baseline the host adapter ships against; [Long-range engine direction](../long-range-direction.md) frames why the host adapter stays a thin projection seam over the engine's host-agnostic catalogs.

## What it owns

- ASP.NET Core service registration for the engine
- project-level split-configuration loading through `AddCephalonProjectConfigurations()` and `AddCephalon(...)`
- runtime startup and shutdown integration through hosted services
- `/engine/*` metadata, status, diagnostics, and policy endpoints
- opt-in core operator surface selection through `Engine:AspNetCore:OperatorSurface:Mode=core`, keeping manifest, snapshot, app-model, resilience, scaffold, capabilities, modules, packages, patterns, technologies, transports, dependency health, localization, reference-doc metadata, options, package/failure/trust policy, status, runtime story, diagnostics, health checks, hosted reference docs, OpenAPI/Scalar, and selected public transport routes while omitting broad optional `/engine/*` drill-down families for cold-start-sensitive minimal API hosts
- an explicit trim/AOT boundary on `MapCephalon()`: the full operator route surface still contains ASP.NET Core Minimal API delegate binding, so the public method carries `RequiresUnreferencedCode` and `RequiresDynamicCode`; package-local analyzer builds are clean, and the deployment-mode harness audits that the manifest-backed route boundary still has both annotations, but this is not a Native AOT support claim. The opt-in `core` operator surface, the shared full/common routes, the full/default behavior-resilience, saga-choreography, durable-execution, foundation, tail introspection, and CDC runtime read-only GET route blocks now map their bounded route subsets through prebuilt `RequestDelegate` handlers and `MapMethods(...)`; the harness emits `CoreRouteDelegateAuditStatus` and `FullCommonRouteDelegateAuditStatus` plus scorecard/release/doctor readback for the generated core/full-common proofs, and Pester guards the full/default workflow/foundation/tail/CDC-runtime blocks, but they remain route-subset proofs, not a full-adapter trim/AOT claim
- `/engine/resilience` when the engine-owned resilience contract is active
- `/engine/strangler-fig`, `/engine/strangler-fig/runtime`, `/engine/strangler-fig/ingress`, `/engine/strangler-fig/resolve`, and `/engine/strangler-fig/cutover` when the engine-owned strangler-fig route, migration-policy, ingress, and ASP.NET Core cutover catalogs are active
- `/engine/backend-for-frontend` when the engine-owned backend-for-frontend client-binding catalog is active
- `/engine/backend-for-frontend/rest-endpoints` when the client-aware backend-for-frontend REST runtime catalog is active
- `/engine/backend-for-frontend/rest-documents` plus scope-specific filtered OpenAPI and Scalar materialization when the backend-for-frontend REST document catalog is active
- `/engine/features` when the engine-owned feature-flag catalog is active
- `/engine/saga-choreographies` plus `/engine/saga-choreographies/runtime` when the shared saga choreography runtime catalog and live publication-state catalog are active
- `/engine/durable-executions` plus `/engine/durable-executions/runtime` when the shared durable execution runtime catalogs are active
- `/engine/cells` when the engine-owned cell-boundary catalog is active
- `/engine/cell-routes` when the engine-owned cell-route catalog is active
- `/engine/cell-health-isolations` when the engine-owned cell health-isolation catalog is active
- `/engine/cell-traffic-automations` when the engine-owned cell traffic-automation catalog is active
- `/engine/cdc-captures` when the engine-owned CDC capture catalog is active
- `/engine/cdc-captures/runtime` when the shared CDC runtime-state catalog is active, including typed freshness, lag, and publication posture
- `/engine/cdc-capture-runtimes` when the shared CDC execution-runtime catalog is active
- reporter-, edge-, coordination-, remediation-, and governance-aware CDC drill-downs under the existing `/engine/cdc-captures/runtime/*` and `/engine/cdc-capture-runtimes/*` route families when the shared external CDC operator-story catalog is active
- `/engine/cdc-capture-runtimes/{executionRuntimeId}/reports` when the opt-in external CDC runtime report sink is active
- `/engine/rate-limiting` when ASP.NET Core rate-limiting enforcement is active
- `/engine/rest-endpoint-candidates` when the module-owned REST candidate catalog is active
- `/engine/rest-endpoint-publication-groups` when grouped module-owned REST publication visibility and authoring-policy state are active
- `/engine/rest-endpoint-authoring-policies` when behavior-level REST authoring-policy runtime answers are active
- `/engine/rest-endpoint-suppressions` for REST shorthand-governance visibility
- `/engine/rest-endpoint-overrides` for REST shorthand override-governance visibility
- `/engine/database-roles` when the engine-owned database-role catalog is active
- `/engine/database-migrations` when the engine-owned database-migration catalog is active
- `/engine/audit-history` and `/engine/audit-history/export` when durable audit-history services are active
- `/engine/event-dispatch-runtimes`, `/engine/event-dispatches`, and `/engine/event-dispatches/terminal-failures` when eventing packs register dispatch-runtime descriptors or live dispatch-state reporters
- `POST /engine/event-publications` when eventing packs register the abstraction-level publication dispatcher action seam; the request body is represented by `EventPublicationHttpRequest` so generated request delegates can bind the contract without private reflection
- `/engine/event-publications/runtime` when eventing packs register the abstraction-level publication runtime-state catalog
- `/engine/event-subscription-readiness` when eventing packs register the abstraction-level subscription execution-readiness catalog
- `/engine/agent-tool-runs` and `POST /engine/agent-tools/{toolId}/runs` when agentics packs register the abstraction-level agent-tool run-state catalog and dispatcher action seam; the run request body is represented by `AgentToolExecutionHttpRequest`
- `/engine/knowledge-indexes`, `POST /engine/knowledge-indexes/{collectionId}/queries`, and `POST /engine/knowledge-indexes/{collectionId}/reindex` when retrieval packs register the abstraction-level knowledge-index catalog, query command seam, and indexer command seam; query request bodies use `KnowledgeQueryHttpRequest`
- `/engine/package-policy`, `/engine/packages`, and the rest of the engine governance surface
- `/health`, `/health/live`, and `/health/ready` surfaces
- opt-in HTTP request/response logging with bounded request and response body capture under `Engine:Observability:HttpLogging`; `HttpRequestResponseLoggingMiddleware` adds correlation tags (`cephalon.http.request_id`, `cephalon.http.traceparent`) to `Activity.Current` and emits `cephalon.http.request.started` / `cephalon.http.request.body.logged` / `cephalon.http.response.completed` / `cephalon.http.response.body.logged` / `cephalon.http.request.failed` activity events with stable Cephalon-prefix tags (`cephalon.log.event_id`, `cephalon.http.request_id`, `cephalon.http.traceparent`, `cephalon.http.elapsed_ms`, `cephalon.http.body.truncated`, plus body-text payload tags), all routed through the `RedactionPipeline` resolved from DI so consumer-registered redaction filters scrub HTTP-emission attributes before exporter dispatch; this is the first M1 redaction emission site shipped through `ENG-365`
- OpenAPI and Scalar integration for REST APIs, including XML-comment enrichment that resolves generated `.xml` files from the publish/output base directory instead of `Assembly.Location` so single-file publish analysis stays clean
- REST result-envelope OpenAPI metadata through `ResultModelEnvelopeResponseMetadata`, allowing adapters to describe `ResultModel<T>` success envelopes without constructing closed generic envelope types at runtime
- optional hosted reference-doc delivery through `ReferenceDocs` host configuration
- built-in REST, SSE, and WebSocket transport route mapping
- companion adapter hooks for GraphQL, JSON-RPC, and gRPC transport packages

## Main surfaces

- `Hosting/AuditHistoryExportHttpResponseExtensions.cs`
- `Hosting/EventPublicationHttpRequest.cs`
- `Hosting/AgentToolExecutionHttpRequest.cs`
- `Hosting/KnowledgeQueryHttpRequest.cs`
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

The default `MapCephalon()` surface remains the full operator route catalog so existing hosts keep every `/engine/*` route family they already depend on. Cold-start-sensitive hosts can set `Engine:AspNetCore:OperatorSurface:Mode=core` before `AddCephalon(...)` / `MapCephalon()` to keep the core introspection, diagnostics, health, documentation, and selected public transport routes without paying to map optional drill-down families such as behavior-resilience, CDC runtime, eventing, agent-tool, retrieval, BFF, strangler-fig, cell, and data-product routes. That mode is additive and host-adapter-local: it does not change the engine manifest, module composition, selected transports, or package runtime catalogs. Its bounded operator routes now use request delegates instead of Minimal API delegate binding so the deployment-mode remediation lane has real runtime proof; `CoreRouteDelegateAuditStatus` makes that proof generated and fail-closed. The shared full/common routes (`/`, `/manifest`, `/snapshot`, `/app-model`, and `/resilience`) use the same helper in full mode and are guarded by `FullCommonRouteDelegateAuditStatus`. The full/default behavior-resilience, saga-choreography, durable-execution, foundation, tail introspection, and CDC runtime read-only GET route families now use result request delegates too, while the remaining default/full route catalog stays the compatibility-preserving surface and still carries the explicit trim/AOT boundary.

When teams choose to publish XML-comment-driven reference output, this host can also serve those static assets directly. That hosting surface is optional and sits beside the hand-authored `.md` guides instead of replacing them.

It also owns the ASP.NET Core side of Cephalon's split-configuration convention. Hosts can keep settings in `Configurations/Add*.json` and `Configurations/{group}/{Environment}.json`, and those files are loaded automatically when `AddCephalon(...)` runs.

When operators need deeper HTTP diagnostics, the same host surface can turn on request/response logging through `Engine:Observability:HttpLogging` or `AddCephalonHttpLogging(...)`. That flow keeps correlation in the shared `ILogger` pipeline by pushing `RequestId`, `TraceId`, `SpanId`, and `TraceParent` into request scopes, redacts known-sensitive query-string and payload fields before the log event is written, including JSON, form, and header-style `text/plain` content, and publishes the corresponding event-id range through `/engine/diagnostics`.

The same host now also maps `/engine/saga-choreographies/runtime` plus publication, behavior,
module, transport, channel, correlation, compensation, and failure drill-down routes when the
shared choreography publication-state catalog is active. Those routes surface the latest
strategy-owned publication handoff observations directly beside the existing static choreography
catalog without claiming downstream event-dispatch ownership.

The same host now also maps `/engine/cells`, `/engine/cells/{cellId}`, and
`/engine/cells/modules/{moduleId}` when the engine-owned `ICellBoundaryCatalog` is active, and it
also maps `/engine/cell-routes`, `/engine/cell-routes/{routeId}`,
`/engine/cell-routes/modules/{moduleId}`, `/engine/cell-routes/source-cells/{cellId}`, and
`/engine/cell-routes/target-cells/{cellId}` when the engine-owned `ICellRouteCatalog` is active.
It now also maps `/engine/cell-health-isolations`, `/engine/cell-health-isolations/{healthIsolationId}`,
`/engine/cell-health-isolations/modules/{moduleId}`, `/engine/cell-health-isolations/cells/{cellId}`,
and `/engine/cell-health-isolations/dependencies/{dependencyId}` when the engine-owned
`ICellHealthIsolationCatalog` is active. It also maps `/engine/cell-traffic-automations`,
`/engine/cell-traffic-automations/{automationId}`,
`/engine/cell-traffic-automations/modules/{moduleId}`,
`/engine/cell-traffic-automations/routes/{routeId}`,
`/engine/cell-traffic-automations/source-cells/{cellId}`,
`/engine/cell-traffic-automations/target-cells/{cellId}`, and
`/engine/cell-traffic-automations/providers/{providerId}`,
`/engine/cell-traffic-automations/edge-nodes/{edgeNodeId}`, and
`/engine/cell-traffic-automations/health-isolations/{healthIsolationId}` when the engine-owned
`ICellTrafficAutomationRuntimeCatalog` is active. `/engine/technology-surfaces/cell-based-architecture`
keeps the same topology, routing, health-isolation posture, and effective automation posture
visible through the existing technology-surface projection, including first-class `providerId` and
`edgeNodeIds` targeting. That same shared route set now also surfaces `providerMaterializerId`,
`providerMaterializationState`, `providerMaterializationObservedAtUtc`,
`providerMaterializationError`, `edgeMaterializerId`, `edgeMaterializationState`,
`edgeMaterializationObservedAtUtc`, `edgeMaterializationError`, and the derived
`materializationState`, `materializationObservedAtUtc`, plus `materializationError`, so the same
HTTP payload family can publish requested, selected, and observed provider-plus-edge reconciliation
truth without inventing a second control-plane or edge-only route family.
`providerMaterializationError`, plus `edgeMaterializerId`, `edgeMaterializationState`,
`edgeMaterializationObservedAtUtc`, and `edgeMaterializationError` when provider-managed or
edge-managed automation is reconciled through the engine catalog, so operators can inspect
selected materializer ownership and the latest startup reconciliation result without learning a
second ASP.NET Core-only materialization endpoint family.
That keeps the phase 13 cell baseline operator-facing without turning cell topology, traffic
posture, health-partition posture, or traffic automation into an ASP.NET Core-only concept.

The same host now also maps `/engine/data-products` plus `/engine/data-products/{dataProductId}`
when the engine-owned `IDataProductCatalog` is active. That keeps the first data mesh/runtime
catalog slice derived from the shared engine contract instead of introducing an ASP.NET Core-only
query-surface registry.

The same host now also maps `/engine/cdc-captures`, `/engine/cdc-captures/{cdcCaptureId}`,
`/engine/cdc-captures/modules/{moduleId}`, `/engine/cdc-captures/providers/{provider}`,
`/engine/cdc-captures/outboxes/{outboxId}`, `/engine/cdc-captures/sources/{sourceId}`, and
`/engine/cdc-captures/resources/{resourceId}` when the engine-owned `ICdcCaptureCatalog` is
active. That keeps the first outbox-linked CDC/runtime catalog slice derived from the shared
engine contract instead of introducing an ASP.NET Core-only sync registry.

The same host now also maps `/engine/cdc-captures/runtime`,
`/engine/cdc-captures/runtime/{cdcCaptureId}`,
`/engine/cdc-captures/runtime/modules/{moduleId}`,
`/engine/cdc-captures/runtime/providers/{provider}`,
`/engine/cdc-captures/runtime/outboxes/{outboxId}`,
`/engine/cdc-captures/runtime/sources/{sourceId}`, and
`/engine/cdc-captures/runtime/resources/{resourceId}` when the shared
`ICdcCaptureRuntimeStateCatalog` is active. That keeps the latest operator-facing capture posture,
counts, checkpoints, errors, typed freshness/lag/publication posture, and optional linked
`OutboxDispatchState` derived from the shared runtime catalog instead of inventing an ASP.NET
Core-only live CDC monitor.

That same route family now also maps
`/engine/cdc-captures/runtime/reporters/{reporterId}`,
`/engine/cdc-captures/runtime/edge-nodes/{edgeNodeId}`,
`/engine/cdc-captures/runtime/reporter-coordination/{coordinationState}`, and
`/engine/cdc-captures/runtime/reporter-coordination/issues/{degradedReason}` so operator flows can
ask the shared runtime-state catalog for one reporter, one edge node, one coordination posture, or
one degraded reason directly instead of re-filtering the whole capture-state payload client-side.

The same host now also maps `/engine/cdc-capture-runtimes`,
`/engine/cdc-capture-runtimes/{executionRuntimeId}`,
`/engine/cdc-captures/execution-runtimes/{executionRuntimeId}`, and
`/engine/cdc-captures/runtime/execution-runtimes/{executionRuntimeId}` when the shared
`ICdcCaptureExecutionRuntimeCatalog`, `ICdcCaptureCatalog`, and `ICdcCaptureRuntimeStateCatalog`
are active. That keeps runtime-first and capture-first CDC ownership answers on one shared catalog
path, including configuration-declared external or provider-native runtimes, instead of adding an
ASP.NET Core-only runner registry.

That same runtime-first route family now also maps
`/engine/cdc-capture-runtimes/reporters/{reporterId}`,
`/engine/cdc-capture-runtimes/edge-nodes/{edgeNodeId}`,
`/engine/cdc-capture-runtimes/reporter-coordination/{coordinationState}`, and
`/engine/cdc-capture-runtimes/reporter-coordination/issues/{degradedReason}` so operator flows can
narrow execution-runtime summaries by active or rejected reporter provenance, observed edge node,
coordination posture, or degraded reason on the same shared catalog path. The same route family
now also maps `/engine/cdc-capture-runtimes/remediation/{remediationState}` and
`/engine/cdc-capture-runtimes/remediation/categories/{remediationCategory}` plus
`/engine/cdc-capture-runtimes/governance/{governanceState}` and
`/engine/cdc-capture-runtimes/governance/categories/{governanceCategory}` plus
`/engine/cdc-capture-runtimes/drift/{driftState}` and
`/engine/cdc-capture-runtimes/drift/categories/{driftCategory}` plus
`/engine/cdc-capture-runtimes/action-plans/{actionPlanState}` and
`/engine/cdc-capture-runtimes/actions/{actionId}` plus
`/engine/cdc-capture-runtimes/write-path-readiness/{readinessState}` and
`/engine/cdc-capture-runtimes/write-path-readiness/categories/{readinessCategory}` plus
`/engine/cdc-capture-runtimes/preflight/{preflightState}`,
`/engine/cdc-capture-runtimes/preflight/categories/{preflightCategory}`, and
`/engine/cdc-capture-runtimes/preflight/operations/{operationId}` plus
`/engine/cdc-capture-runtimes/dry-runs/{dryRunState}`,
`/engine/cdc-capture-runtimes/dry-runs/categories/{dryRunCategory}`, and
`/engine/cdc-capture-runtimes/dry-runs/operations/{operationId}` plus
`/engine/cdc-capture-runtimes/execution-intents/{executionIntentState}`,
`/engine/cdc-capture-runtimes/execution-intents/categories/{executionIntentCategory}`,
`/engine/cdc-capture-runtimes/execution-intents/operations/{operationId}` plus
`/engine/cdc-capture-runtimes/execution-approvals/{executionApprovalState}`,
`/engine/cdc-capture-runtimes/execution-approvals/categories/{executionApprovalCategory}`, and
`/engine/cdc-capture-runtimes/execution-approvals/operations/{operationId}` plus
`/engine/cdc-capture-runtimes/command-envelopes/{commandState}`,
`/engine/cdc-capture-runtimes/command-envelopes/categories/{commandCategory}`, and
`/engine/cdc-capture-runtimes/command-envelopes/operations/{operationId}` plus
`/engine/cdc-capture-runtimes/command-issuances/{issuanceState}`,
`/engine/cdc-capture-runtimes/command-issuances/categories/{issuanceCategory}`, and
`/engine/cdc-capture-runtimes/command-issuances/operations/{operationId}` so operator flows can query the
shared execution-runtime remediation summary, managed-connector governance posture,
desired-versus-observed managed-connector drift posture, managed-connector action-planning
posture, managed-connector write-path readiness posture, managed-connector preflight posture,
managed-connector dry-run posture, managed-connector execution-intent posture, and
managed-connector execution-approval posture plus managed-connector command-envelope posture and
managed-connector command-issuance posture directly
instead of rebuilding a second HTTP-local remediation, governance, drift, action-planning,
readiness, preflight, dry-run, execution-intent, execution-approval, command-envelope, or
command-issuance index.

When `ICdcCaptureExecutionRuntimeReportSink` is active, the same host also maps
`POST /engine/cdc-capture-runtimes/{executionRuntimeId}/reports`. That route is intentionally
opt-in because it mutates operator-facing runtime state: it accepts `CdcCaptureRuntimeObservation`
payloads for the requested runtime, rejects observations whose captures are effectively owned by a
different execution runtime, and then returns the refreshed execution-runtime descriptor so
external/out-of-process CDC runners can publish live truth through the same `/engine/cdc-*` and
`snapshot` surfaces rather than a second host-only ingest registry. That same ingress now also
supports stable `reportId` retries, can reject out-of-order submissions when the declared runtime
requires ordered reporting, can reject conflicting `reporterId` submissions while an active
reporter lease still exists, can reject undeclared `edgeNodeId` values for edge-aware runtimes,
and refreshes `lastReportId`, `lastReporterId`, `activeReporterId`,
`reporterLeaseExpiresAtUtc`, `observedEdgeNodeIds`, typed `observationFreshness`, and typed
`reporterCoordination` answers on `/engine/cdc-captures/runtime*`, `/engine/cdc-capture-runtimes*`,
and `snapshot` from the same configured stale-window, reporter-lease, and edge-node policy instead
of a host-only timeout or topology monitor. That same typed coordination answer now also carries
participant-level `active`, `standby`, and `rejected` reporter truth, so host-level operator
surfaces can show current owners, previous owners kept as standby evidence, and rejected
conflicting reporters without adding a second ASP.NET Core-only coordination projection. It now
also carries `takeoverState`, `degradedReason`, `requiresTakeover`, and `hasCompletedTakeover`, so
the same `/engine/cdc-*` responses can distinguish an expired lease waiting for failover, a
completed takeover, a rejected conflict, and a multiple-active reporter ambiguity directly on the
shared runtime story. Those same payloads now also expose participant counters and normalize stale
rejected-conflict plus takeover-only standby evidence after later accepted reports, so the live
HTTP operator surface stays current without inventing an ASP.NET Core-only cleanup registry.

When teams use `Cephalon.Behaviors.Http` behavior-aware REST helpers, the resulting Minimal API endpoints flow through this same host-level OpenAPI + Scalar pipeline rather than requiring a separate documentation surface. That same pipeline now treats module-owned REST groups as the public REST documentation surface while keeping the generic behavior HTTP adapter endpoints out of REST OpenAPI + Scalar descriptions by default, so the docs do not show duplicate transport-adapter routes beside the module-owned API. `RestBehaviorModuleBase` remains the dedicated class-based authoring path for modules that both own behaviors and expose some of them over REST, while `engine.AddRestBehaviorModule<TMarker>(...)` now gives straightforward hosts a lower-ceremony inline registration path that still materializes as a real module and still maps through the generic `IRestModule` contract. Modules can now also fan one generated behavior-id root prefix into several owned public route groups through `IRestBehaviorModuleBuilder.MapGeneratedProfileGroups(...)` while still publishing on the existing `behavior-module-generated` shorthand path, and hosts can now govern that same grouped shorthand by exact behavior id or subtree `BehaviorIdPrefixes` without taking away module ownership. That same exact-versus-prefix governance trace is now also explicitly proven through `/engine/rest-endpoint-publication-groups`, `/engine/rest-endpoint-authoring-policies`, and the matching snapshot answers, including per-authoring-style grouped summaries where broader prefix rules remain visible as matched-only outcomes beside narrower exact winners. The same prefix selectors now also preserve skipped-governance truth for explicit module-DSL routes that never opted into host governance, so grouped/operator runtime answers keep those rules visible as skipped rather than collapsing them into a selector miss. All of those paths project DSL-backed and explicit manual module-owned REST endpoints into the same `/engine/rest-endpoints` runtime catalog plus duplicate-route validation baseline. For shorthand publication, that runtime answer now carries first-class `RestEndpointRuntimeDescriptor.AuthoringStyle` for explicit DSL, profile shorthand, generated shorthand, behavior-helper, and manual paths, first-class `RestEndpointRuntimeDescriptor.RouteGroupPrefix` plus `RelativePattern` for the resolved grouped publication boundary, first-class nullable `RestEndpointRuntimeDescriptor.BehaviorType` for behavior-backed implementation identity, first-class nullable `RestEndpointRuntimeDescriptor.SourceId` for published source identity, publishes the explicit route/query/header/body binding plan through the first-class `RestEndpointRuntimeDescriptor.BindingDescriptors` surface and the matching `bindingDescriptors` array in JSON responses when profile bindings exist, and also carries nullable `RestEndpointRuntimeDescriptor.CandidateId` plus nullable `RestEndpointRuntimeDescriptor.OriginalProjection` so published behavior-backed endpoints can show both the originating candidate id and the original projection method, route, document-version, tag name, binding plan, and host-governance eligibility directly on the final runtime answer. Published endpoints now also carry nullable `OriginalEndpointName`, `OriginalSummary`, and `OriginalDescription`, so operators can read original endpoint metadata directly beside the final effective endpoint name/summary/description without inferring it back out of behavior docs. When a host-level override intentionally clears one of those endpoint-metadata fields, the effective published endpoint metadata now stays empty all the way through actual ASP.NET Core route metadata plus `/engine/rest-endpoints`, while the original-metadata trio still preserves the source lineage. When a host-level override retags one candidate, the effective tag now flows through actual ASP.NET Core endpoint tag metadata plus the candidate and published runtime catalogs while `OriginalProjection.TagName` preserves the source tag. When a host-level override rewrites the effective OpenAPI document name, or when the authored group pins one through `.WithOpenApiDocumentName(...)`, that document name now flows through actual ASP.NET Core endpoint-group metadata plus the candidate and published runtime catalogs while `OriginalProjection.OpenApiDocumentName` preserves the source document lineage. If only some candidates in one authored group land in another effective document or tag, ASP.NET Core materialization splits the effective published groups by route-group prefix, document name, and tag so the live endpoints and runtime catalogs keep the same truth. Manual and behavior-helper endpoints keep `OriginalProjection = null` and the original-metadata trio `null`. Published endpoints now also carry nullable `OriginalRequiredCapabilityKey`, ordered `RequiredFeatureFlagIds`, ordered `OriginalRequiredFeatureFlagIds`, and `AppliedOverrideId`, and now also expose ordered `MatchedOverrideIds`, so the final runtime surface can explain both source-versus-effective capability-and-feature governance and the full override match set directly, including capability-only no-op matches, feature-only no-op matches, and reorder-only equivalent explicit-binding rewrites that preserve the same normalized binding set while leaving `AppliedOverrideId = null`. Configured override rules now also expose typed `RestEndpointOverrideDescriptor.ActionKinds`, while published endpoints and runtime candidates expose `SelectedOverrideActionKinds` plus `AppliedOverrideActionKinds` so operators can compare the override dimensions the winning rule declared against the subset that materially changed the live endpoint. That keeps no-op winners truthful too: the selected action kinds stay visible even when `AppliedOverrideId` and `AppliedOverrideActionKinds` remain empty after materialization reconciliation. Startup diagnostics now also keep that same declared-versus-effective override story visible for events `5202` and `5203`, echoing `OverrideSelectionBasis` plus selected and applied action-kind wire names beside the winning override id instead of forcing operators to infer them from later route diffs alone. The suppression path now mirrors that visibility too: event `5200` echoes `SuppressionSelectionBasis` beside the winning suppression id so startup logs answer both which rule won and why it won. When an explicit module-DSL route stays authoritative because the owning group did not opt into host governance, that same published endpoint answer now also keeps ordered `SkippedSuppressionIds` and `SkippedOverrideIds` visible so operators can see which host rules targeted the surface but were skipped for governance ineligibility rather than selector mismatch. Startup diagnostics now also emit `RestEndpointGovernanceSkipped` event `5206` when those skipped ids are present, so `/engine/diagnostics` and information-level startup logging explain that the host rules targeted the explicit surface but were intentionally bypassed because the original projection did not allow host governance. In that reorder-only binding case the source binding order remains the published runtime answer instead of letting a matched host rule claim a structural change that did not actually happen, and the same semantic binding-set comparison now also drives endpoint projection reuse plus post-materialization structural override checks so later synthetic shorthand paths cannot regress into order-sensitive endpoint provenance. Additive `metadata.authoringStyle`, `metadata.routeGroupPrefix`, `metadata.relativePattern`, `metadata.behaviorType`, and `metadata.sourceId` remain available for compatibility, but they are no longer the canonical published-endpoint authorship, route-boundary, behavior-implementation, or source-identity answer, and shorthand materialization now also consumes `ProjectedEndpoint.RouteGroupPrefix` directly instead of parsing compatibility metadata during route-group publication. The same host now also publishes `/engine/rest-endpoint-candidates` plus `/engine/rest-endpoint-candidates/{candidateId}` and the matching `snapshot.RestEndpointCandidates` answer so operators can see the original projection through `OriginalProjection`, the final effective mapped answer through `ProjectedEndpoint`, that same effective answer's original endpoint metadata through `ProjectedEndpoint.OriginalEndpointName` / `OriginalSummary` / `OriginalDescription`, and the published-versus-suppressed outcome plus any governing suppression or override rule. That candidate answer now also mirrors skipped explicit-DSL governance through `SkippedSuppressionIds` and `SkippedOverrideIds` while leaving `Matched*` empty, so runtime truth distinguishes “would have matched if governable” from an actual winning governance rule. Candidate runtime JSON now also keeps `status` on stable `published` / `suppressed` wire names instead of raw enum-number serialization, so `/engine/rest-endpoint-candidates`, `/engine/rest-endpoint-candidates/{candidateId}`, and `snapshot.RestEndpointCandidates` all expose the same readable operator vocabulary directly. It now also publishes `/engine/rest-endpoint-publication-groups` plus `/engine/rest-endpoint-publication-groups/{behaviorId}` and the matching `snapshot.RestEndpointPublicationGroups` answer so the same runtime truth is grouped per behavior, including the published candidate ids, precedence-suppressed candidate ids, governance-suppressed candidate ids, winning precedence rank when one exists, the ordered candidate set behind that grouped publication story, typed `AuthoringPolicy` data that distinguishes the implicit default single-winner boundary from an explicit `RestApi:AuthoringPolicies:{behaviorId}` configuration, grouped `HostGovernanceEligibleCandidateIds` / `HostGovernanceIneligibleCandidateIds`, grouped `SkippedSuppressionIds` / `SkippedOverrideIds`, and per-rule `SkippedSuppressionSummaries` / `SkippedOverrideSummaries` at both the behavior and per-authoring-style summary levels. Within that normalized behavior-projection path, explicit module DSL wins over both shorthand modes, and `MapProfile<TBehavior>()` wins over generated shorthand when both target the same behavior. Host governance does not change that boundary by default: an explicit module-DSL route group must first opt in through `AllowHostGovernance()`, and the matching host rule must also target authoring style `behavior-module-dsl`, before suppressions or overrides can govern that explicit DSL surface.

Grouped publication answers now also carry typed suppression `SelectionBasisSummaries` plus
override `SelectionBasisSummaries`, `SelectedActionKindSummaries`, and
`AppliedActionKindSummaries`, so the same ASP.NET Core runtime surface can answer why a grouped
host rule won and which override dimensions only stayed declared versus materially applied without
forcing operators back into `/engine/rest-endpoint-candidates`.

The same host now also publishes `/engine/rest-endpoint-authoring-policies` plus
`/engine/rest-endpoint-authoring-policies/{behaviorId}` and the matching
`snapshot.RestEndpointAuthoringPolicies` answer so operator tooling can read one behavior-level
authoring-policy answer without reopening grouped publication data first. That rule-centric
surface keeps explicitly configured `RestApi:AuthoringPolicies:{behaviorId}` entries visible even
when no current candidates match, carries the same default-versus-configured policy intent, and
separates `CandidateIds`, `RetainedCandidateIds`, `PublishedCandidateIds`,
`PrecedenceSuppressedCandidateIds`, `GovernanceSuppressedCandidateIds`, and grouped
`SuppressionSummaries` so authoring-policy suppression stays distinct from later precedence or
host-governance outcomes. It now also derives typed `AuthoringStyleSummaries`, so the same
rule-centric policy payload can show which normalized authoring style contributed each candidate,
which survived authoring policy, which stayed published, and which later moved into precedence,
governance, or authoring-policy suppression buckets without reopening publication-group payloads.
That same authoring-policy answer now also keeps `HostGovernanceEligibleCandidateIds`,
`HostGovernanceIneligibleCandidateIds`, `SkippedSuppressionIds`, `SkippedOverrideIds`,
`GovernanceSuppressionSummaries`, `GovernanceOverrideSummaries`, `SkippedSuppressionSummaries`,
and `SkippedOverrideSummaries` visible at both the behavior level and inside each authoring-style
summary, so explicit DSL ownership that never entered host governance remains readable from the
same rule-centric surface and one payload can answer which host rules matched, suppressed,
selected, applied, or stayed skipped.

The rule catalogs themselves now mirror that same truth directly: `/engine/rest-endpoint-suppressions`
and `snapshot.RestEndpointSuppressions` surface per-rule `MatchedCandidateIds`,
`SuppressedCandidateIds`, `SkippedCandidateIds`, `SelectionBases`, and
`SelectionBasisSummaries`, while
`/engine/rest-endpoint-overrides` plus `snapshot.RestEndpointOverrides` surface per-rule
`MatchedCandidateIds`, `SelectedCandidateIds`, `AppliedCandidateIds`, `SkippedCandidateIds`,
`SelectionBases`, `SelectionBasisSummaries`, `SelectedActionKinds`,
`SelectedActionKindSummaries`, `AppliedActionKinds`, and `AppliedActionKindSummaries`, all
derived lazily from the same candidate runtime catalog instead of staying as configuration-only
echoes. That keeps one route-level answer for both union-level runtime effect and grouped
candidate-level provenance.

For shorthand governance provenance, published endpoint and candidate answers now also use `SelectedOverrideId` to name the winning override rule when post-materialization reconciliation proves that the rule matched but did not materially change the effective published answer, so `AppliedOverrideId = null` no longer means “no override rule won.” Those same runtime answers now also expose `SelectedOverrideActionKinds` plus `AppliedOverrideActionKinds`, while the configured override catalog exposes `ActionKinds`, so operators can read the same winning-rule story at both the declared-rule and effective-runtime levels without reconstructing which override dimensions were intended versus which ones actually changed the published boundary. Those same runtime answers now also expose `OverrideSelectionBasis`, and suppressed candidates now also expose `SuppressionSelectionBasis`, so operators can see the earliest decisive specificity reason that chose one matching governance rule over another instead of reconstructing it from the ordered match set alone.

The first shipped host-governance slices for that shorthand path are `RestApi:Suppressions` and `RestApi:Overrides`. Both are ASP.NET Core-only config surfaces for descriptor-backed shorthand candidates. `RestApi:Suppressions` can suppress `MapProfile<TBehavior>()` candidates plus generated shorthand candidates from `MapGeneratedProfiles(...)` or `MapGeneratedProfileGroups(...)` before precedence resolution, publishes the configured rules through `/engine/rest-endpoint-suppressions` plus `snapshot.RestEndpointSuppressions`, and marks suppressed candidates with `SuppressedBySuppressionId` so runtime truth distinguishes config governance from candidate-to-candidate precedence. `RestApi:AuthoringPolicies:{behaviorId}` now sits between that suppression pass and final publication: `AllowMultiplePublishedCandidates = true` lets lower-precedence unsuppressed shorthand candidates remain published together for one grouped behavior boundary when their final route answers stay distinct, and ASP.NET Core materialization now also honors deterministic effective endpoint-name disambiguation whenever those co-published candidates would otherwise reuse the same endpoint name while `OriginalEndpointName` still preserves the source shorthand lineage. `PreferredAuthoringStyle`, `AllowedAuthoringStyles`, and `DisallowedAuthoringStyles` now also enforce shorthand publication policy for `behavior-module-profile` and `behavior-module-generated` candidates only, mark suppressed candidates with `SuppressedByAuthoringPolicyKind`, and keep grouped publication truth separate from config-governance suppression and candidate-precedence suppression. `RestApi:Overrides` now supports `ApiVersionMajor`, `OpenApiDocumentName`, `Method`, published `RouteGroupPrefix`, relative `Pattern`, `RequiredCapabilityKey`, `ClearRequiredCapability`, ordered `RequiredFeatureFlagIds`, `ClearRequiredFeatureFlags`, explicit `Bindings`, `RemovedBindingProperties`, shorthand binding resets through `ClearBindings`, typed `BindingMode`, host-governed preserved implicit query fallback through `PreserveImplicitQueryFallback`, shorthand endpoint metadata `EndpointName`, `Summary`, and `Description`, shorthand OpenAPI tag-name rewrites through `TagName`, plus endpoint-metadata clears `ClearEndpointName`, `ClearSummary`, and `ClearDescription` for those same shorthand candidates, publishes configured rules through `/engine/rest-endpoint-overrides` plus `snapshot.RestEndpointOverrides`, and records the applied rule on the candidate through `AppliedOverrideId` only when the selected rule materially changes the effective published answer. Capability-only no-op clears, same-key capability rewrites, feature-only no-op clears, same-set feature-flag rewrites, same-value endpoint-metadata rewrites, same-value document rewrites, same-value tag rewrites, reorder-only explicit-binding rewrites whose normalized descriptor set is equivalent to the source plan, and endpoint-metadata clears against source metadata the module already removed still remain visible through `MatchedOverrideIds`, and the final published `/engine/rest-endpoints` plus `snapshot.RestEndpoints` answer now keeps that ordered match set visible directly too, while both endpoint and candidate surfaces leave `AppliedOverrideId = null` once materialization proves the effective boundary, endpoint metadata, capability boundary, feature boundary, document name, tag, or binding plan did not change. In that reorder-only binding case the projected candidate and published endpoint also preserve the source explicit binding order instead of restating the override order as if it were new runtime truth. Both surfaces can now also refine `Behaviors`/`Modules` targeting with `ApiVersionMajors`, `Methods`, `RelativePatterns`, `RouteGroupPrefixes`, `OpenApiDocumentNames`, `TagNames`, `EndpointNames`, `HostGovernanceScopes`, and `BindingFallbackModes`; those selector refiners match the original shorthand candidate shape before override actions are applied, including the authored document, primary tag identity, original shorthand endpoint name through `ProjectedEndpoint.OriginalEndpointName`, original authored governance scope through `OriginalProjection.HostGovernanceScope`, and original shorthand binding-fallback identity, so one host can govern only one of several shorthand candidates that share the same behavior or module identity. `BindingFallbackModes` uses the same stable wire names that the runtime exposes for fallback truth, including `preserve-source-implicit-fallback` and `preserve-remaining-body-fallback`, while `HostGovernanceScopes` lets hosts target one authored route-group scope without depending on later rewritten route shape and can now also act as a rule's primary selector when that authored governance label is the clearest stable target. The suppression and override runtime catalogs keep those selector arrays visible directly, so `/engine/rest-endpoint-suppressions`, `/engine/rest-endpoint-overrides`, and `snapshot` continue to answer the exact targeting logic the host configured. Both rule families still fail fast when they omit every primary selector, but `HostGovernanceScopes` now counts beside `CandidateIds`, `Behaviors`, and `Modules` as one of those primary targets; override rules also fail fast when they omit all override actions, use a non-positive `ApiVersionMajor`, declare an unsupported HTTP method, declare an invalid route pattern, declare an invalid `RouteGroupPrefix`, set both `RequiredCapabilityKey` and `ClearRequiredCapability`, set both `RequiredFeatureFlagIds` and `ClearRequiredFeatureFlags`, try to both set and clear the same endpoint-metadata field in one rule, pair `replace-explicit` with `RemovedBindingProperties`, try to remove and override the same property in one rule, pair `ClearBindings` with `Bindings`, `RemovedBindingProperties`, `PreserveImplicitQueryFallback`, or an explicit `BindingMode`, use `ClearBindings` when the resulting route placeholders would rely on removed explicit route-binding aliases, or otherwise resolve to an invalid effective binding plan. When more than one rule matches the same candidate the host resolves the most specific rule deterministically by populated target dimensions first, then by behavior-targeted scope, narrower authoring-style scope, fewer total selector values, and finally stable rule-id ordering. That baseline remains intentionally narrow: explicit module DSL routes and manual module-owned REST endpoints stay authoritative, shorthand groups with explicit `.ApiVersion(...)` stay authoritative for version selection, `ApiVersionMajor` only re-derives the effective document name when the authored group did not pin one explicitly, and the current override slice rewrites only the effective API major version, OpenAPI document name, HTTP method, bounded published route-group prefix, constrained relative route pattern, required capability boundary, required feature-flag boundary, explicit binding plan or explicit-binding reset, preserved implicit query-fallback intent, shorthand endpoint metadata, and/or shorthand tag name so the mapped endpoint plus the `/api/v{major}` route segment, published group boundary, OpenAPI document name, ASP.NET Core endpoint metadata, and runtime catalogs remain aligned instead of drifting apart. Pattern overrides in this slice can change static path segments, reorder existing placeholders, rename placeholders when the effective explicit route-binding plan covers the renamed placeholder set exactly, remove placeholders when the original projection already exposes explicit route-binding coverage for the original placeholder set and the effective explicit binding plan keeps every affected original route-bound property explicitly bound, and add placeholders when the effective explicit route-binding plan covers the full final placeholder set and every newly route-bound property was either already explicitly bound in the original projection, for `POST`/`PUT`/`PATCH` already part of the original deterministic remaining-body fallback surface, for shorthand candidates with no explicit binding plan already part of the original implicit query-fallback surface, or for explicit shorthand candidates whose remaining source query surface was intentionally preserved either by the source profile or by a winning override that sets `PreserveImplicitQueryFallback = true`. Broader implicit-property promotion outside those shipped body-fallback, no-explicit-plan query-fallback, and source-or-host-preserved-explicit-query-fallback slices still fails fast, and the runtime fallback-mode answer now also clears once the effective binding plan fully consumes a preserved source implicit-query fallback surface. `RouteGroupPrefix` overrides stay shorthand-only, must remain beneath the active REST root, cannot declare placeholders, cannot silently change the effective API version, and now make the ASP.NET Core materializer split effective route groups when only one candidate in an authored shorthand group is remapped, moved to another document, or retagged so actual mapped endpoints, endpoint group metadata, endpoint tag metadata, and the runtime catalogs keep the same truth. Binding overrides now default to replacing the shorthand candidate's explicit binding descriptors, `BindingMode = merge-explicit` can upsert changed explicit bindings or withdraw selected original explicit bindings through `RemovedBindingProperties` while still leaving unbound route placeholders plus remaining request-body fields available for deterministic fallback, and `ClearBindings = true` can discard the source shorthand explicit binding plan entirely so the effective request composition returns to the implicit route/query/body baseline; removal targets must already exist in the source shorthand explicit binding plan, merge-mode removals on non-body-capable methods now also fail fast when they would stop explicitly binding a source query-bound property unless that property still belongs to a source profile that intentionally preserved implicit query fallback, a winning override that intentionally sets `PreserveImplicitQueryFallback = true`, or the host intentionally resets the full authored plan through `ClearBindings`, and `ClearBindings` still fails fast when the effective route would only remain valid through placeholder aliases that the source explicit route bindings had been supplying. Endpoint-metadata overrides now follow that same truth model too: a winning clear action removes the effective endpoint name, summary, or description from actual ASP.NET Core metadata and the runtime catalogs without erasing `OriginalEndpointName`, `OriginalSummary`, or `OriginalDescription`, and metadata-only no-op matches leave `AppliedOverrideId = null` once materialization proves the module already published the same answer. Capability-boundary overrides now follow the same truth model: shorthand candidate projections, actual endpoint metadata, `/engine/rest-endpoints`, and `snapshot.RestEndpoints` all surface the same `RequiredCapabilityKey`, `ClearRequiredCapability = true` is the explicit host answer for removing an inherited shorthand capability boundary, and `RequireCapability(...)` plus `ClearRequiredCapability()` now both treat the last declared capability registration as authoritative so a later host override can replace or intentionally remove an earlier shorthand `configureEndpoint` guard instead of double-enforcing or silently retaining it. Published endpoints now also expose nullable `OriginalRequiredCapabilityKey`, `AppliedOverrideId`, and ordered `MatchedOverrideIds`, so a final `RequiredCapabilityKey = null` can be read as “never had one” versus “host cleared one” while still preserving which shorthand override rules matched the live endpoint. Feature-boundary overrides now follow that same truth model: shorthand candidate projections, actual endpoint metadata, `/engine/rest-endpoints`, and `snapshot.RestEndpoints` all surface the same ordered `RequiredFeatureFlagIds`, `ClearRequiredFeatureFlags = true` is the explicit host answer for removing inherited shorthand feature gates, and `RequireFeatureFlag(...)`, `RequireFeatureFlags(...)`, and `ClearRequiredFeatureFlags()` now all treat the last declared feature registration as authoritative so a later host override can replace or intentionally remove an earlier shorthand `configureEndpoint` feature boundary instead of stacking or silently retaining it. Published endpoints now also expose ordered `OriginalRequiredFeatureFlagIds`, `AppliedOverrideId`, and ordered `MatchedOverrideIds`, so an empty final `RequiredFeatureFlagIds` answer can be read as “never had one” versus “host cleared one” while still preserving which shorthand override rules matched the live endpoint. `/engine/rest-endpoint-candidates` plus `snapshot.RestEndpointCandidates` follow that same truth model for capability-only no-op clear matches, same-key capability rewrites, feature-only no-op clear matches, same-set feature rewrites, same-value endpoint-metadata rewrites, same-value document rewrites, same-value tag rewrites, endpoint-metadata clears against source metadata the module already removed, `ClearBindings` matches against candidates that already had no explicit binding descriptors, reorder-only equivalent binding-plan rewrites, and preserved-fallback rewrites that newly expose source query surface after explicit binding withdrawal, keeping `MatchedOverrideIds` visible while leaving `AppliedOverrideId = null` when the effective published answer stays unchanged or the source projection itself still had no remaining fallback surface. Explicit module-DSL groups can now also stamp `WithHostGovernanceScope("scope")` onto their original projection so rule selectors and runtime catalogs can preserve one stable authored governance label, but that scope alone still does not opt the route into host governance; `AllowHostGovernance()` remains the separate gate for explicit DSL participation.

That same shorthand governance model now also lets hosts target exact `Behaviors` or subtree
`BehaviorIdPrefixes`, including grouped generated shorthand from
`MapGeneratedProfileGroups(...)`, while still matching against the original dot-separated
behavior-id hierarchy before override actions are applied. `BehaviorIdPrefixes` now count as
primary selectors beside `CandidateIds`, `Behaviors`, `Modules`, and `HostGovernanceScopes`, the
suppression and override runtime catalogs keep those configured prefix arrays visible directly, and
overlapping behavior-targeted rules now surface the narrower subtree winner through the stable
`narrower-behavior-scope` selection basis instead of leaving that precedence implicit.

That host-level materialization rule now also preserves authored-source versus effective fallback
truth independently: `OriginalProjection.BindingFallbackMode` stays the source profile answer,
while `ProjectedEndpoint.BindingFallbackMode` and the published endpoint may surface
`PreserveSourceImplicitFallback` only when a winning override legitimately re-exposes preserved
remaining query surface after explicit binding withdrawal.

Clarification: `Bindings:*:Source` and `TargetBindings:*:Source` now use stable `route`, `query`,
`header`, and `body` wire names in both host config and runtime JSON. For `BindingMode`,
`bindingMode = unspecified` is preserved only for `ClearBindings` rules that omitted an explicit
mode; other omitted modes still normalize to `replace-explicit` or `merge-explicit`.

For `BindingFallbackModes`, the host config contract is now wire-name-only: use
`preserve-source-implicit-fallback` and `preserve-remaining-body-fallback`, and expect startup to
fail fast if a rule still uses legacy PascalCase enum-member aliases.

By default the host registers the `v1` OpenAPI document and treats `/scalar/v1` as the canonical docs link by redirecting `/scalar` to the configured default document. The slash-suffixed Scalar shell at `/scalar/` still remains available for multi-document flows, and Cephalon's Scalar JavaScript normalizes hash-based selections such as `/scalar/#v2/` back into canonical versioned links. Hosts can move those surfaces through `OpenApi:RoutePattern` and `OpenApi:Scalar:RoutePrefix`, while the built-in REST mapper can move off `/api` through `ApiRoutes:Prefixes:Rest` or all the way to the version root with `ApiRoutes:Prefixes:Rest = ""`. The long-term versioned config contract is `OpenApi:EnabledVersions` plus `OpenApi:DefaultVersion`, for example `EnabledVersions: [2, 3]` and `DefaultVersion: 3`. In that model, modules and endpoints still declare which document they belong to through `.ApiVersion(...)`, `.WithOpenApiDocumentName(...)`, module-major defaults, or the constrained shorthand-only `RestApi:Overrides:*:ApiVersionMajor` / `OpenApiDocumentName` governance path, but the host treats `EnabledVersions` as the published-document allow-list: only enabled docs are registered under `/openapi/{document}.json`, only enabled docs appear in Scalar's injected selector, and a disabled default falls back to the first enabled document instead of silently publishing an extra version. When a module or shorthand override pins a non-versioned document name, that named document still follows the host's explicit named-document publication contract through legacy `OpenApi:Documents` and `OpenApi:DefaultDocument`. When more than one published document exists, Cephalon now mounts that selector into Scalar's own header bar so document switching stays inside the toolbar instead of floating over the page actions. When a host deliberately uses legacy `OpenApi:Documents` and `OpenApi:DefaultDocument` names instead of `v{major}`, the same allow-list semantics still apply and the injected selector keeps the more generic `Document` label instead of `Version`. `OpenApi:Version` remains available as a global `info.version` override for single-document hosts, but multi-document hosts now keep each document version truthful to its own resolved document name such as `v2` or `v3`.

When a host needs to trim or expand the default response set published for behavior-owned REST endpoints, use `OpenApi:BehaviorRest:DocumentedStatusCodes`. The default list is `[200, 201, 202, 204, 400, 401, 403, 404, 409, 500]`, which keeps `500` visible in Scalar/OpenAPI by default. Hosts can override that list with a smaller or larger HTTP-status set without changing the runtime behavior of the endpoints themselves. Cephalon still adds runtime-required answers back per route so the docs stay truthful: when `Engine:Resilience:RateLimiting:Enabled = true`, behavior-owned REST endpoints whose effective ASP.NET Core policy requires a limiter automatically publish `429`; when the effective behavior-execution bulkhead or rate-limiting policy resolved for that route is active, those same REST helpers also publish `429`; and when the effective behavior-execution timeout or circuit breaker resolved for that route is active, they also publish `503`. If a REST route keeps both the ASP.NET Core endpoint limiter and the behavior-execution limiter active, the ASP.NET Core middleware rejects first and the host-owned `429` envelope wins; behavior- or transport-scoped rate-limiting overrides under `Engine:Resilience:RateLimiting:Overrides` can disable the endpoint limiter for a subset of routes when a team wants the behavior-owned `429` semantics to surface instead. Behavior-execution overrides under `Engine:Resilience:BehaviorExecution:Overrides` can disable or narrow inherited timeout, circuit-breaker, bulkhead, or rate-limiting answers for specific behavior/transport combinations, so those endpoints omit `429` / `503` responses they can no longer emit.

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
collection. Error envelopes also carry a problem `type` URI and the HTTP `status` field, matching
the ASP.NET Core problem-details vocabulary while keeping the Cephalon envelope shape. The host's
ASP.NET Core `ProblemDetails` writer only translates REST-prefixed problem responses into
`ResultModelError` when that same envelope flag is enabled; when the flag is `false`, exception
handler and problem responses stay as native `ProblemDetails`. That setting is intentionally
REST-only. GraphQL keeps the standard
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
shared retry, timeout, circuit-breaker, bulkhead, and rate-limiting answers enforced by `Cephalon.Behaviors`, including
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
timeout, circuit-breaker, bulkhead, and rate-limiting enforcement apply consistently across transports, resolves narrower
`Engine:Resilience:BehaviorExecution:Overrides` entries with
`behavior+transport > behavior > transport > default` precedence, and lets explicit disable overrides
suppress inherited enforcement cleanly. The REST helper layer translates those behavior-execution
rejections into truthful HTTP responses (`503` for timeout or an open circuit breaker, `429` for
bulkhead saturation or behavior-execution rate limiting) while keeping OpenAPI in sync per route and
surfacing retry-after details for open circuits. When both the host-owned endpoint limiter and the
behavior-owned limiter apply to one REST route, the host middleware rejects first unless an
`Engine:Resilience:RateLimiting:Overrides` entry disables that endpoint policy for the targeted
behavior/transport pair. Retry now runs in that same shared pipeline only for explicitly idempotent behaviors
when the effective retry policy is active and the classifier marks the failure as transient, while
non-idempotent or unknown behaviors still fail without automatic replay.

The same host surface now also exposes the shipped strangler-fig authored, effective-policy,
ingress, and host-cutover answers directly. `/engine/strangler-fig` and
`/engine/strangler-fig/{routeId}`
publish the active migration-route catalog composed by `Cephalon.Engine`, while
`/engine/strangler-fig/resolve` evaluates one request-shaped `path` plus `method` pair through the
host-agnostic `IStranglerFigRouter`. The configuration-driven policy/progress overlay is also
visible here: `/engine/strangler-fig/runtime` plus `/engine/strangler-fig/runtime/{routeId}`
publish effective requested-versus-selected target answers, route-level progress state and
percent, and optional route notes from `Engine:Migration:StranglerFig` through the host-agnostic
`IStranglerFigMigrationRuntimeCatalog` without replacing the authored route catalog. The
engine-first ingress follow-through is visible here too: `/engine/strangler-fig/ingress`,
`/engine/strangler-fig/ingress/{routeId}`, and `/engine/strangler-fig/ingress/modules/{moduleId}`
publish the normalized ingress materialization answer from `IStranglerFigIngressRuntimeCatalog`,
including pass-through, local rewrite, absolute endpoint, and opaque endpoint truth without a
second ASP.NET Core-only ingress registry. When `Engine:Migration:StranglerFig:AspNetCore` is
enabled, `/engine/strangler-fig/cutover`, `/engine/strangler-fig/cutover/{routeId}`, and
`/engine/strangler-fig/cutover/resolve` publish how ASP.NET Core will execute that shared ingress
truth: rooted local selected endpoints rewrite in-process before endpoint execution, absolute HTTP
or HTTPS selected endpoints can redirect or proxy through the host-owned proxy client, and
unsupported or opaque selected endpoints fail truthfully with `502` while provider-specific
ingress or edge automation stays outside the current baseline.

The same operator surface now also exposes the shipped backend-for-frontend client-binding runtime.
`/engine/backend-for-frontend` publishes the merged client-binding catalog composed by
`Cephalon.Engine`, while `/engine/backend-for-frontend/{bindingId}` returns one binding by its
stable identifier and the client/module/transport drill-down routes expose the same catalog through
those narrower operator lenses. That host surface stays derived from the shared
`IBackendForFrontendRuntimeCatalog` and `snapshot.BackendForFrontendBindings` contracts rather than
inventing a second ASP.NET Core-only registry. The same host now also derives
`/engine/backend-for-frontend/rest-endpoints` plus its binding/client/module/published-endpoint/id
drill-down routes from that shared binding catalog and the published `IRestEndpointRuntimeCatalog`,
and `snapshot.BackendForFrontendRestEndpoints` carries the same derived client-aware REST truth for
operators or tooling that want one snapshot payload. That same truth path now also powers the first
scope-specific documentation/materialization follow-through: `/engine/backend-for-frontend/rest-documents`,
`/engine/backend-for-frontend/rest-documents/bindings/{bindingId}`,
`/engine/backend-for-frontend/rest-documents/clients/{clientId}`, and
`/engine/backend-for-frontend/rest-documents/{documentId}` expose the derived document catalog,
`snapshot.BackendForFrontendRestDocuments` carries the same materialized-document descriptors, and
the host now serves filtered OpenAPI documents plus scope-aware Scalar pages under the configured
OpenAPI and Scalar prefixes instead of inventing a BFF-only documentation registry. Binding routes
materialize under the configured OpenAPI route root as
`.../backend-for-frontend/bindings/{bindingId}/{documentName}.json`, client routes materialize as
`.../backend-for-frontend/clients/{clientId}/{documentName}.json`, and the matching Scalar pages
stay aligned with the configured Scalar prefix while keeping the selected `bindingId` or `clientId`
in the query string so the document selector can remain truthful to the scoped document set.

The same host surface now also exposes the shipped feature-flag runtime directly. When
`IFeatureFlagRuntimeCatalog` is active, `/engine/features`, `/engine/features/enabled`,
`/engine/features/disabled`, `/engine/features/modules/{moduleId}`, and
`/engine/features/{featureFlagId}` project the merged host-owned plus module-owned catalog,
including any typed `ProviderBindings` that keep external-provider bridges visible on the same
runtime truth. The evaluation route `/engine/features/{featureFlagId}/evaluate` resolves the
requested flag through the shared `IFeatureToggle` using query-string supplied environment, module,
behavior, capability, transport, tenant, subject, and repeated `tag` inputs, now also returning
provider-level gate answers through `ProviderResults` when one or more `IFeatureFlagProvider`
bindings participated. `/engine/snapshot` carries that same merged descriptor set through
`FeatureFlags`, and request-time REST or behavior execution keeps honoring the same provider-backed
answer because transport gating still flows through the shared feature evaluator instead of a
separate ASP.NET Core-only registry.

The same operator surface now also exposes the shipped choreography and durable execution runtime
catalogs directly. `/engine/saga-choreographies` plus its behavior/module/transport drill-down
routes read `ISagaChoreographyRuntimeCatalog` so operators can inspect choreography ownership,
transport exposure, feature gates, and result-shape metadata without depending on the optional
eventing bridge runtime surface. `/engine/durable-executions` plus
`/engine/durable-executions/runtime` and their drill-down routes read the shared durable execution
catalogs so operators can inspect both static durable workflow ownership and live per-stream
posture through the same `/engine/*` surface instead of reconstructing those answers from broader
behavior or event-store internals.

The host now also exposes additive event-dispatch operator answers directly. When eventing packs
register the corresponding catalogs, `/engine/event-dispatch-runtimes` and
`/engine/event-dispatch-runtimes/{dispatchRuntimeId}` publish dispatch-runtime descriptors such as
runtime id, ownership metadata, bridge mode, the outbox/runtime ids a managed loop is responsible
for, and a canonical aggregate `Summary` once live reports exist. `/engine/outboxes` also carries
the effective `DispatchPolicy` object per outbox so the same ownership answer is visible from the
engine-owned outbox catalog. `/engine/event-dispatches` and `/engine/event-dispatches/{outboxId}`
remain the per-outbox detail surface, publishing the latest live dispatch state per outbox path,
including reported outcome, retry intent, terminal-failure posture, timestamps, and totals from
`IEventDispatchRuntimeReporter`. `/engine/event-dispatches/terminal-failures` filters that same
catalog to outbox paths whose latest report is terminally failed, so operators do not have to parse
`reported.*` metadata to find exhausted dispatch paths. Those same answers also flow into
`/engine/snapshot` as
`EventDispatchRuntimes` and `EventDispatchStates`, which keeps operator tooling aligned across the
host route surface and the broader runtime snapshot without forcing adapter packs to re-aggregate
state by hand.

The host now also exposes bounded event-publication operator action and publication runtime-state
surfaces directly. When a selected eventing pack registers `IEventPublicationDispatcher`,
`POST /engine/event-publications` accepts
one publication body with `id`, `channelId`, `eventType`, JSON or string `payload`,
`occurredAtUtc`, `contentType`, `correlationId`, `tenantId`, optional headers, metadata, and actor
id. The route returns `EventPublicationResult`, records safe route-trigger metadata, returns `404`
when no publication dispatcher is active or the target channel is unknown, and returns `400` for
invalid publication bodies. The action stays bounded to the active publication path: it can trigger
the core in-process direct publisher or stage through an outbox-backed publisher, and the direct
publisher can optionally suppress duplicate completed executions process-locally, but it does not
claim durable broker dispatch, durable inbox ownership, cross-node idempotency, retry queues,
distributed scheduling, or provider-specific inbound consumption. When the selected runtime also
registers `IEventPublicationRuntimeCatalog`, `/engine/event-publications/runtime`,
`/engine/event-publications/runtime/{publicationId}`, and
`/engine/event-publications/runtime/channels/{channelId}` expose the latest reported publication
state from abstraction-level contracts, while `/engine/snapshot` carries the same entries through
`EventPublicationStates`. In-process entries report local succeeded, failed, or skipped execution
posture plus subscription counters; outbox-backed entries report `accepted` staged handoff and leave
downstream dispatch completion to the dispatch-runtime surfaces.

The host now also exposes additive agent-tool run-state answers and a bounded operator action
directly. When a selected agentics pack registers `IAgentToolRunCatalog` and `IAgentToolDispatcher`,
`/engine/agent-tool-runs`, `/engine/agent-tool-runs/{runId}`,
`/engine/agent-tool-runs/by-tool/{toolId}`, and `POST /engine/agent-tools/{toolId}/runs` publish or
create the latest reported run posture from abstraction-level contracts, while `/engine/snapshot`
carries the same entries through `AgentToolRuns`. The action route accepts optional run id, actor id,
correlation id, attempt, string arguments, and metadata, then records safe route-trigger metadata on
the returned run. That keeps host adapters able to publish and trigger agent-tool runtime truth
without depending on `Cephalon.Agentics` implementation types.

The host now also exposes additive retrieval index-state answers, bounded query execution, and a
manual reindex action directly. When a selected retrieval pack registers `IKnowledgeIndexCatalog`,
`IKnowledgeQueryEngine`, and `IKnowledgeIndexer`, `/engine/knowledge-indexes`,
`/engine/knowledge-indexes/{collectionId}`, `POST /engine/knowledge-indexes/{collectionId}/queries`,
and `POST /engine/knowledge-indexes/{collectionId}/reindex` publish, query, or refresh the latest
managed index and query posture from abstraction-level contracts, while `/engine/snapshot` carries
the same entries through `KnowledgeIndexes`. That keeps host adapters able to publish, query, and
remediate knowledge-index runtime truth without depending on `Cephalon.Retrieval` implementation
types.

## Related docs

- [Architecture](../architecture.md)
- [Engine surface maturity audit](../engine-surface-maturity-audit.md)
- [Conformance matrix](../conformance-matrix.md)
- [Runtime contract index](../runtime-contract-index.md)
- [Long-range engine direction](../long-range-direction.md)
- [Engineering standards](../engineering-standards.md)
- [Architecture review (May 2026)](../architecture-review-2026-05.md)
- [Deployment-mode support](../deployment-mode-support.md)
- [Operations](../operations.md)
- [Reference docs publishing](../reference-docs.md)


