# Runtime contract index

This document is the consolidated map of Cephalon's runtime contract surface: every `/engine/*` HTTP route, every `snapshot.*` data key, and every runtime catalog interface that an operator, an AI agent, or external tooling can read to know what the engine is actually doing.

It exists because the engine's runtime truth is already machine-readable through `/engine/*` and `snapshot.*`, but a human or an autonomous agent should be able to discover the full surface from one page rather than scraping each route or grep-ing the source tree.

Cross-references: [`project-memory.md`](project-memory.md), [`architecture.md`](architecture.md), [`architecture-review-2026-05.md`](architecture-review-2026-05.md), [`engineering-standards.md`](engineering-standards.md), [`long-range-direction.md`](long-range-direction.md), [`engine-surface-maturity-audit.md`](engine-surface-maturity-audit.md), [`conformance-matrix.md`](conformance-matrix.md), [`compatibility.md`](compatibility.md), [`docs/components/README.md`](components/README.md).

## Why this document exists

Two specific concerns drive this index:

1. **Operator-facing discoverability.** A platform team standing up a Cephalon-based service should be able to ask one question — "what runtime answers does this engine expose?" — without reading source code. Today the answers live across `Cephalon.AspNetCore`, `Cephalon.Engine`, and many companion packs. This index puts them on one page.
2. **AI-agent-facing readability.** [`long-range-direction.md`](long-range-direction.md) Horizon 3 anticipates AI agents becoming primary consumers of frameworks. Agents reason better about a surface they can index than one they have to crawl. This page is intentionally written so an agent can map intent → route or intent → snapshot key without crawling.

This doc is part of the durable hand-authored docs graph and is updated together with the source whenever a new runtime surface ships.

## How to read this index

The runtime contract has three layered surfaces. Each layer has its own table below:

- **`/engine/*` routes** — HTTP-level introspection endpoints exposed by `Cephalon.AspNetCore`. They project the underlying catalogs as JSON.
- **`snapshot.*` data keys** — top-level properties on the canonical `RuntimeIntrospectionSnapshot` shape composed by `Cephalon.Engine`, returned by `/snapshot` and consumed by every runtime-story view.
- **Runtime catalog interfaces** — `Cephalon.Abstractions` interfaces that own the in-process truth which both routes and snapshots project from.

The same fact usually appears at all three layers. For example: `/engine/cells` → `snapshot.CellBoundaries` → `ICellBoundaryCatalog`. Operators consume the route, snapshot composers consume the key, internal services consume the catalog interface.

A surface is **base** if it ships unconditionally with the relevant adapter, and **optional/conditional** if it is registered only when the matching companion pack, configuration, or app-profile setting is present.

## `/engine/*` route catalog

The route prefix `/engine` is reserved for Cephalon engine introspection. App-owned REST endpoints publish under `/api/v{major}` and never collide with this prefix.

| Route | Owner package | Answers | Optional/conditional |
| --- | --- | --- | --- |
| `GET /` | `Cephalon.AspNetCore` | engine manifest landing | base |
| `GET /manifest` | `Cephalon.AspNetCore` | canonical manifest of runtime shape | base |
| `GET /snapshot` | `Cephalon.Engine` | composed introspection snapshot across every active surface | base |
| `GET /app-model` | `Cephalon.AspNetCore` | application model and profile configuration | base |
| `GET /resilience` | `Cephalon.AspNetCore` | requested resilience patterns and settings | base |
| `GET /behavior-resilience` | `Cephalon.AspNetCore` | effective behavior-execution resilience policies | optional |
| `GET /behavior-resilience/{policyId}` | `Cephalon.AspNetCore` | single resilience policy | optional |
| `GET /rate-limiting` | `Cephalon.AspNetCore` | rate-limiting policies | optional |
| `GET /capabilities` | `Cephalon.AspNetCore` | module capability manifests | base |
| `GET /modules` | `Cephalon.AspNetCore` | module descriptors from manifest | base |
| `GET /packages` | `Cephalon.AspNetCore` | loaded package manifests | base |
| `GET /technologies` | `Cephalon.AspNetCore` | technology selections from manifest | base |
| `GET /technology-catalog` | `Cephalon.AspNetCore` | registered technology catalog | optional |
| `GET /technology-surfaces` | `Cephalon.AspNetCore` | runtime technology capability surfaces | optional |
| `GET /patterns` | `Cephalon.AspNetCore` | pattern definitions (cell-based, strangler-fig, BFF, ...) | base |
| `GET /databases` | `Cephalon.AspNetCore` | configured databases | base |
| `GET /database-topology` | `Cephalon.AspNetCore` | operational database topology | base |
| `GET /database-roles` | `Cephalon.AspNetCore` | database role descriptors | base |
| `GET /database-migrations` | `Cephalon.AspNetCore` | database migration descriptors | base |
| `GET /database-migration-playbook` | `Cephalon.AspNetCore` | operational migration playbook | base |
| `GET /hosted-executions` | `Cephalon.AspNetCore` | hosted execution descriptors | optional |
| `GET /execution-graphs` | `Cephalon.AspNetCore` | execution graph definitions | optional |
| `GET /data-products` | `Cephalon.AspNetCore` | data product descriptors | optional |
| `GET /data-products/runtime` | `Cephalon.AspNetCore` | data product descriptors with freshness/timing metadata | optional |
| `GET /cdc-captures` | `Cephalon.AspNetCore` | CDC capture definitions | optional |
| `GET /cdc-captures/runtime*` | `Cephalon.AspNetCore` | live CDC runtime state and per-runtime drilldowns | optional |
| `GET /cdc-capture-runtimes` | `Cephalon.AspNetCore` | CDC execution-runtime catalog plus filter drilldowns by reporter, edge node, coordination, freshness, and governance | optional |
| `POST /cdc-capture-runtimes/{executionRuntimeId}/reports` | `Cephalon.AspNetCore` | external CDC runtime live reports | conditional (`DataRuntimeOptions.EnableExternalCdcRuntimeReporting`) |
| `GET /audit-stores` | `Cephalon.AspNetCore` | audit store descriptors | optional |
| `GET /audit-history` | `Cephalon.AspNetCore` | queryable audit history | optional |
| `GET /audit-history/export` | `Cephalon.AspNetCore` | NDJSON export of audit history | conditional (`AppProfile.Audit.History.Export`) |
| `GET /authorization-policies` | `Cephalon.AspNetCore` | authorization policy descriptors | optional |
| `GET /features` | `Cephalon.AspNetCore` | feature flag definitions | optional |
| `GET /features/{featureFlagId}/evaluate` | `Cephalon.AspNetCore` | per-flag evaluation including provider-bridge results | optional |
| `GET /strangler-fig` | `Cephalon.AspNetCore` | strangler-fig migration routes | optional |
| `GET /strangler-fig/runtime` | `Cephalon.AspNetCore` | strangler-fig runtime policies | optional |
| `GET /strangler-fig/ingress` | `Cephalon.AspNetCore` | strangler-fig ingress routes | optional |
| `GET /strangler-fig/resolve` | `Cephalon.AspNetCore` | resolve a path to a strangler-fig routing decision | optional |
| `GET /strangler-fig/cutover` | `Cephalon.AspNetCore` | strangler-fig cutover state | conditional (`Engine:Migration:StranglerFig:AspNetCore`) |
| `GET /strangler-fig/cutover/resolve` | `Cephalon.AspNetCore` | resolve a path to its cutover decision | conditional |
| `GET /backend-for-frontend` | `Cephalon.AspNetCore` | BFF client bindings | optional |
| `GET /backend-for-frontend/rest-endpoints` | `Cephalon.AspNetCore` | BFF REST endpoint projections | optional |
| `GET /backend-for-frontend/rest-documents` | `Cephalon.AspNetCore` | BFF REST document artifacts | optional |
| `GET /cells` | `Cephalon.AspNetCore` | cell boundary descriptors | optional |
| `GET /cell-routes` | `Cephalon.AspNetCore` | cell routing governance | optional |
| `GET /cell-health-isolations` | `Cephalon.AspNetCore` | cell health isolation rules | optional |
| `GET /cell-traffic-automations` | `Cephalon.AspNetCore` | cell traffic automation policies plus provider/edge drilldowns | optional |
| `GET /knowledge-indexes` | `Cephalon.AspNetCore` | knowledge retrieval index states | optional |
| `POST /knowledge-indexes/{collectionId}/queries` | `Cephalon.AspNetCore` | execute a knowledge query | optional |
| `GET /agent-tools` | `Cephalon.AspNetCore` | registered agent tool definitions | optional |
| `POST /agent-tools/{toolId}/run` | `Cephalon.AspNetCore` | execute an agent tool | optional |
| `GET /event-subscription-readiness` | `Cephalon.AspNetCore` | event subscription execution readiness | optional |
| `GET /inboxes` | `Cephalon.AspNetCore` | inbox descriptors | optional |
| `GET /outboxes` | `Cephalon.AspNetCore` | outbox descriptors | optional |
| `GET /projections` | `Cephalon.AspNetCore` | data projection descriptors | optional |
| `GET /durable-executions` | `Cephalon.AspNetCore` | durable execution descriptors | optional |
| `GET /durable-executions/runtime` | `Cephalon.AspNetCore` | durable execution runtime state | optional |
| `GET /durable-executions/runtime/timers*` | `Cephalon.AspNetCore` | pending durable timers | optional |
| `GET /durable-executions/runtime/signals*` | `Cephalon.AspNetCore` | pending durable signals | optional |
| `GET /durable-executions/runtime/compensations*` | `Cephalon.AspNetCore` | durable compensation actions | optional |
| `GET /saga-choreographies` | `Cephalon.AspNetCore` | saga choreography definitions | optional |
| `GET /saga-choreographies/runtime*` | `Cephalon.AspNetCore` | saga publication runtime state | optional |
| `GET /rest-endpoints` | `Cephalon.AspNetCore` | published REST endpoint descriptors | optional |
| `GET /rest-endpoint-candidates` | `Cephalon.AspNetCore` | REST endpoint candidates pre-publication | optional |
| `GET /rest-endpoint-publication-groups` | `Cephalon.AspNetCore` | REST endpoint publication groupings | optional |
| `GET /rest-endpoint-authoring-policies` | `Cephalon.AspNetCore` | REST endpoint authoring governance | optional |
| `GET /rest-endpoint-overrides` | `Cephalon.AspNetCore` | REST endpoint runtime overrides | optional |
| `GET /rest-endpoint-suppressions` | `Cephalon.AspNetCore` | suppressed REST endpoint candidates | optional |
| `GET /tenant-memberships` | `Cephalon.MultiTenancy.Governance` | tenant membership descriptors | optional |
| `GET /tenant-invitations` | `Cephalon.MultiTenancy.Governance` | tenant invitation descriptors | optional |
| `GET /tenant-domain-ownership` | `Cephalon.MultiTenancy.Governance` | tenant domain ownership descriptors and proof state | optional |
| `GET /tenant-governance-actions` | `Cephalon.MultiTenancy.Governance` | tenant governance action descriptors and decisions | optional |
| `GET /tenant-administration` | `Cephalon.MultiTenancy.Governance` | tenant administration runtime metadata | optional |
| `POST /engine/tenant-administration/commands` | `Cephalon.MultiTenancy.Governance.AspNetCore` | tenant administration workflow command endpoint | conditional (`MapCephalonTenantAdministrationCommands()`) |

The `/engine/cdc-capture-runtimes` family in particular has a number of filter drilldowns that have been added incrementally; treat the route table above as the canonical entry point and let the route response document the available filters in its own metadata rather than re-enumerating each filter variant here.

Companion edge packages (`Cephalon.Edge.KubernetesGateway`, `Cephalon.Edge.Traefik`) project the same `cell-traffic-automations` truth back into provider-specific surfaces such as `kubernetes-gateway-traffic-materializations` and `traefik-ingressroute-traffic-materializations`; those are technology runtime surfaces, not new `/engine/*` routes.

## `snapshot.*` data key catalog

The top-level shape exposed by `/snapshot` is `RuntimeIntrospectionSnapshot`. Its current top-level keys, grouped by domain:

**Base composition**

`Manifest`, `Status`, `OperationalStory`, `DiagnosticsConventions`.

**Behavior, execution, and resilience**

`ExecutionGraphs`, `HostedExecutions`, `BehaviorResiliencePolicies`, `RateLimitingPolicies`, `Projections`, `DurableExecutions`, `DurableExecutionStates`.

**Saga choreography**

`SagaChoreographies`, `SagaChoreographyPublicationStates`.

**REST authoring and governance**

`RestEndpoints`, `RestEndpointCandidates`, `RestEndpointPublicationGroups`, `RestEndpointAuthoringPolicies`, `RestEndpointOverrides`, `RestEndpointSuppressions`.

**Database and data**

`DatabaseRoles`, `DatabaseMigrations`, `DatabaseMigrationPlaybook`, `DatabaseTopology`, `DataProducts`, `Outboxes`, `Inboxes`, `EventDispatchRuntimes`, `EventDispatchStates`, `EventPublicationStates`, `EventSubscriptionExecutionReadiness`.

**Change data capture**

`CdcCaptures`, `CdcCaptureStates`, `CdcCaptureExecutionRuntimes`.

**Cell-based architecture and traffic automation**

`CellBoundaries`, `CellRoutes`, `CellHealthIsolations`, `CellTrafficAutomations`.

**Migration and integration**

`StranglerFigRoutes`, `StranglerFigRoutePolicies`, `StranglerFigIngressRoutes`, `BackendForFrontendBindings`, `BackendForFrontendRestEndpoints`, `BackendForFrontendRestDocuments`.

**Technology surfaces and capabilities**

`TechnologySurfaces`, `FeatureFlags`.

**Audit, identity, governance, agentic**

`AuditStores`, `AuthorizationPolicies`, `AgentToolRuns`, `KnowledgeIndexes`.

**Multi-tenancy governance** (added through `Cephalon.MultiTenancy.Governance`)

`TenantMemberships`, `TenantInvitations`, `TenantInvitationDeliveryRuns`, `TenantDomainOwnership`, `TenantGovernanceActions`, `TenantAdministration`.

When a snapshot key holds a list, its element type is the matching descriptor (for example, `CellRoutes` holds `CellRouteDescriptor` items; `DurableExecutionStates` holds `DurableExecutionRuntimeState` items). The element type names follow the pattern `{Surface}Descriptor` for static catalog entries and `{Surface}RuntimeState` for live state, with a few additional `{Surface}RuntimeDescriptor` exceptions where the surface itself is runtime-derived.

When a key is empty (`?? []`), that does not by itself mean the surface is inactive; some catalogs ship empty arrays even when their underlying companion is registered. Consult the `Manifest` and `Capabilities` blocks for whether a surface is intended to be active.

## Runtime catalog interface catalog

These interfaces live in `Cephalon.Abstractions` and own the in-process truth that the routes and snapshots project from. New interfaces should follow the same `I{Surface}Catalog` or `I{Surface}RuntimeCatalog` naming pattern.

**Behavior, execution, durable, resilience, REST**

`IExecutionRuntimeCatalog`, `IHostedExecutionRuntimeCatalog`, `IBehaviorResilienceRuntimeCatalog`, `IRateLimitingRuntimeCatalog`, `IDurableExecutionRuntimeCatalog`, `IDurableExecutionRuntimeStateCatalog`, `ISagaChoreographyRuntimeCatalog`, `ISagaChoreographyPublicationRuntimeStateCatalog`, `IRestEndpointRuntimeCatalog`, `IRestEndpointCandidateRuntimeCatalog`, `IRestEndpointAuthoringPolicyRuntimeCatalog`, `IRestEndpointPublicationGroupRuntimeCatalog`, `IRestEndpointOverrideRuntimeCatalog`, `IRestEndpointSuppressionRuntimeCatalog`.

**Data, eventing, CDC**

`IDataProductCatalog`, `IProjectionCatalog`, `IOutboxCatalog`, `IInboxCatalog`, `IEventDispatchRuntimeCatalog`, `IEventPublicationRuntimeCatalog`, `IEventSubscriptionExecutionReadinessCatalog`, `ICdcCaptureCatalog`, `ICdcCaptureExecutionRuntimeCatalog`, `IDatabaseRoleCatalog`, `IDatabaseMigrationCatalog`.

**Cells and traffic automation**

`ICellBoundaryCatalog`, `ICellRouteCatalog`, `ICellHealthIsolationCatalog`, `ICellTrafficAutomationRuntimeCatalog`.

**Migration and integration**

`IStranglerFigRuntimeCatalog`, `IStranglerFigMigrationRuntimeCatalog`, `IStranglerFigIngressRuntimeCatalog`, `IBackendForFrontendRuntimeCatalog`, `IBackendForFrontendRestRuntimeCatalog`, `IBackendForFrontendRestDocumentRuntimeCatalog`.

**Technology, feature, audit, identity, agentic, knowledge**

`ITechnologyRuntimeCatalog`, `IFeatureFlagRuntimeCatalog`, `IAuditStoreCatalog`, `IAuthorizationPolicyCatalog`, `IAgentToolRunCatalog`, `IKnowledgeIndexCatalog`.

**Multi-tenancy governance**

`ITenantResolver`, `ITenantContextAccessor`, `ITenantMembershipCatalog`, `ITenantMembershipEvaluator`, `ITenantInvitationCatalog`, `ITenantInvitationValidator`, `ITenantInvitationDeliveryRunCatalog`, `ITenantInvitationDeliveryDispatcher`, `ITenantInvitationDeliverySender`, `ITenantDomainOwnershipCatalog`, `ITenantDomainOwnershipValidator`, `ITenantDomainOwnershipStore`, `ITenantDomainOwnershipVerificationWorkflow`, `ITenantDomainOwnershipProofEvaluator`, `ITenantDomainOwnershipProofChallengeIssuer`, `ITenantDomainOwnershipProofPublicationPlanner`, `ITenantDomainOwnershipHttpProofCollector`, `ITenantDomainOwnershipDnsTxtProofCollector`, `ITenantDomainOwnershipProofVerificationRunner`, `ITenantDomainOwnershipProofPollingRunner`, `ITenantDomainOwnershipHttpProofPublisher`, `ITenantDomainOwnershipHttpProofPublicationCatalog`, `ITenantGovernanceActionCatalog`, `ITenantGovernanceActionDecider`, `ITenantGovernanceActionWorkflow`, `ITenantGovernanceActionStore`, `ITenantAdministrationWorkflow`.

## Conventions and conditional surfaces

The runtime contract follows a small number of conventions that operators and AI agents can rely on across the engine:

- routes under `/engine/*` are introspection-only by default; mutating routes (`POST`/`PUT`/`PATCH`) are explicit, named in the route catalog above, and only registered when their owning companion or workflow is active
- catalog interfaces live in `Cephalon.Abstractions` so consumers can take a thin contract dependency without pulling the full engine; runtime services live in `Cephalon.Engine` or the relevant companion pack
- snapshot composition is additive: each owner contributes through an `IRuntimeIntrospectionSnapshotContributor` (or equivalent) so adding a new runtime surface does not require changing `RuntimeIntrospectionSnapshot` itself outside its owning slice
- conditional routes are documented inline above (`conditional (...)`); when a configuration option toggles a route on, the same option is also referenced in the runtime catalog metadata so the route's existence stays explainable

When a route, snapshot key, or catalog interface is added, this index must be updated in the same slice. When a route, snapshot key, or catalog interface is renamed or retired, this index must be updated; for retirement, leave a one-line note explaining the replacement so AI agents do not have to reconstruct lineage.

## Discovery patterns for AI consumers

An AI agent or external automation can discover the live engine by following this short sequence:

1. fetch `GET /engine/manifest` for the high-level shape
2. fetch `GET /engine/snapshot` for the composed runtime truth
3. consult this index to translate intent (for example, "list active CDC captures and their freshness") into the matching route or snapshot key
4. follow drill-down routes (for example, `/engine/cdc-captures/runtime`) when the snapshot's summary needs more detail
5. when an answer is missing from the snapshot, check whether the underlying companion is active in `Manifest` or `Capabilities` before assuming the surface is broken

For agents that need machine-readable contract metadata, `docs/reference/*` is the generated XML-doc reference layer that documents the public types behind these surfaces; pair this index with that reference output when explanation or signature-level detail is needed.

## Maintenance posture

This document is meant to evolve with the runtime contract.

- update it in the same slice whenever a new `/engine/*` route, snapshot key, or runtime catalog interface ships
- prefer rewriting the affected table or section in place over appending; the value of this index is its compactness
- keep the conditional/optional notes truthful so agents and operators do not assume a surface exists when it is gated behind a configuration switch
- whenever this index drifts more than a month from the underlying source (verified against the `engine-surface-maturity-audit.md` truth), refresh the affected sections together
- the canonical naming pattern for new entries is `/engine/{kebab-case-surface}` for routes, `snapshot.{PascalCaseSurface}` for snapshot keys, and `I{Surface}Catalog` or `I{Surface}RuntimeCatalog` for interfaces
