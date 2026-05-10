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
| `GET /status` | `Cephalon.AspNetCore` | runtime status posture | base |
| `GET /app-model` | `Cephalon.AspNetCore` | application model and profile configuration | base |
| `GET /options` | `Cephalon.AspNetCore` | engine option settings | base |
| `GET /transports` | `Cephalon.AspNetCore` | configured transport descriptors | base |
| `GET /failure-policy` | `Cephalon.AspNetCore` | runtime failure policy | base |
| `GET /package-policy` | `Cephalon.AspNetCore` | package policy | base |
| `GET /trust-policy` | `Cephalon.AspNetCore` | trust policy | base |
| `GET /scaffold` | `Cephalon.AspNetCore` | scaffold descriptors | base |
| `GET /reference-docs` | `Cephalon.AspNetCore` | reference-doc projection metadata | base |
| `GET /localization` | `Cephalon.AspNetCore` | localization settings | base |
| `GET /diagnostics` | `Cephalon.AspNetCore` | runtime diagnostics surface (catalog + health summary) | base |
| `GET /diagnostics-conventions` | `Cephalon.AspNetCore` | canonical OpenTelemetry name set the engine and host adapters emit telemetry under (activity sources, meters, `cephalon.*` attribute keys); names sourced from `Cephalon.Diagnostics` constants | base |
| `GET /dependencies` | `Cephalon.AspNetCore` | runtime dependency-health evaluation | base |
| `GET /runtime-story` | `Cephalon.AspNetCore` | runtime operational story (`IRuntime.OperationalStory`) | base |
| `GET /resilience` | `Cephalon.AspNetCore` | requested resilience patterns and settings | base |
| `GET /behavior-resilience` | `Cephalon.AspNetCore` | effective behavior-execution resilience policies | optional |
| `GET /behavior-resilience/{policyId}` | `Cephalon.AspNetCore` | single resilience policy | optional |
| `GET /rate-limiting` | `Cephalon.AspNetCore` | rate-limiting policies | optional |
| `GET /capabilities` | `Cephalon.AspNetCore` | module capability manifests; shared provider-family keys aggregate `sourceModuleIds`, `providers`, `packs`, and `contributorCount` metadata | base |
| `GET /modules` | `Cephalon.AspNetCore` | module descriptors from manifest | base |
| `GET /packages` | `Cephalon.AspNetCore` | loaded package manifests | base |
| `GET /technologies` | `Cephalon.AspNetCore` | technology selections from manifest | base |
| `GET /technology-catalog` | `Cephalon.AspNetCore` | registered technology catalog | optional |
| `GET /technology-surfaces` | `Cephalon.AspNetCore` | runtime technology capability surfaces (companion governance surfaces such as `tenant-memberships`, `tenant-invitations`, `tenant-domain-ownership`, `tenant-governance-actions`, and `tenant-administration` are projected here as `surfaceId` drill-downs rather than top-level `/engine/*` routes; eventing projects operator surfaces such as `event-dispatch-remediations` here; data providers also project `data-management` CDC surfaces such as `cdc-captures` and `cdc-capture-runtimes` here; observability exporter and logging packs project sanitized `telemetry-export-*` and `logging-provider-serilog` active-pack surfaces here) | optional |
| `GET /patterns` | `Cephalon.AspNetCore` | pattern definitions (cell-based, strangler-fig, BFF, ...) | base |
| `GET /databases` | `Cephalon.AspNetCore` | configured databases | base |
| `GET /database-topology` | `Cephalon.AspNetCore` | operational database topology | base |
| `GET /database-roles` | `Cephalon.AspNetCore` | database role descriptors | base |
| `GET /database-migrations` | `Cephalon.AspNetCore` | database migration descriptors | base |
| `GET /database-migration-playbook` | `Cephalon.AspNetCore` | operational migration playbook | base |
| `GET /hosted-executions` | `Cephalon.AspNetCore` | hosted execution descriptors | optional |
| `GET /execution-graphs` | `Cephalon.AspNetCore` | execution graph definitions | optional |
| `GET /data-products` | `Cephalon.AspNetCore` | data product descriptors | optional |
| `GET /cdc-captures` | `Cephalon.AspNetCore` | CDC capture definitions | optional |
| `GET /cdc-captures/runtime*` | `Cephalon.AspNetCore` | live CDC runtime state and per-runtime drilldowns | optional |
| `GET /cdc-capture-runtimes` | `Cephalon.AspNetCore` | CDC execution-runtime catalog plus filter drilldowns by reporter, edge node, coordination, freshness, and governance | optional |
| `POST /cdc-capture-runtimes/{executionRuntimeId}/reports` | `Cephalon.AspNetCore` | external CDC runtime live reports | conditional (`ICdcCaptureExecutionRuntimeReportSink` registered, gated by `DataRuntimeOptions.EnableExternalCdcRuntimeReporting`) |
| `POST /cdc-capture-runtimes/{executionRuntimeId}/commands/{operationId}` | `Cephalon.AspNetCore` | issue a CDC managed-connector command | optional |
| `GET /audit-stores` | `Cephalon.AspNetCore` | audit store descriptors | optional |
| `GET /audit-history` | `Cephalon.AspNetCore` | queryable audit history | optional |
| `GET /audit-history/export` | `Cephalon.AspNetCore` | NDJSON export of audit history | conditional (`AppProfile.Audit.History.Export.Enabled`) |
| `GET /authorization-policies` | `Cephalon.AspNetCore` | authorization policy descriptors | optional |
| `GET /features` | `Cephalon.AspNetCore` | feature flag definitions plus enabled/disabled/by-module drill-downs | optional |
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
| `POST /knowledge-indexes/{collectionId}/reindex` | `Cephalon.AspNetCore` | trigger a manual knowledge reindex | optional |
| `GET /agent-tool-runs` | `Cephalon.AspNetCore` | agent tool run states plus retry-pending, idempotency-duplicates, approval-required, terminal-failures, by-tool, and per-run drill-downs | optional |
| `POST /agent-tools/{toolId}/runs` | `Cephalon.AspNetCore` | execute an agent tool through `IAgentToolDispatcher` | optional |
| `GET /event-subscription-readiness` | `Cephalon.AspNetCore` | event subscription execution readiness | optional |
| `GET /event-dispatch-runtimes` | `Cephalon.AspNetCore` | event dispatch runtime descriptors | optional |
| `GET /event-dispatches` | `Cephalon.AspNetCore` | event dispatch states with terminal-failure and per-outbox drill-downs | optional |
| `POST /event-dispatches/{outboxId}/commands/{operationId}` | `Cephalon.AspNetCore` | bounded event-dispatch remediation commands (`retry-now`, `retry-later`, `skip`, `quarantine`, `dead-letter`) through `IEventDispatchRemediationDispatcher`; `dead-letter` is dispatch-store terminal intent, not broker DLQ ownership; command ids are reserved before mutation and duplicates are rejected without mutation | optional |
| `GET /event-dispatch-remediation-commands*` | `Cephalon.AspNetCore` | bounded event-dispatch remediation command results with latest, summary including reserved/in-doubt counts, retention, direct in-doubt reservation list with optional `beforeUtc` cutoff, direct oldest retained in-doubt read with the same cutoff, observed-window, command-id, outbox, message, channel, operation, actor, correlation-id, reason, dispatch-outcome, command-outcome, filter-specific summaries, newest-first `limit` on list/filter reads, and duplicate-command recovery drill-downs | optional |
| `GET /event-publications/runtime` | `Cephalon.AspNetCore` | event publication runtime states with channel and per-publication drill-downs | optional |
| `POST /event-publications` | `Cephalon.AspNetCore` | dispatch an event publication through `IEventPublicationDispatcher` | optional |
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
| `POST /tenant-administration/commands` | `Cephalon.MultiTenancy.Governance.AspNetCore` | tenant administration workflow command endpoint | conditional (`MapCephalonTenantAdministrationCommands()`) |
| `POST /tenant-invitations/delivery-dispatches` | `Cephalon.MultiTenancy.Governance.AspNetCore` | tenant invitation delivery dispatch endpoint | conditional (`MapCephalonTenantInvitationDeliveryDispatches()`) |
| `POST /tenant-invitations/delivery-status` | `Cephalon.MultiTenancy.Governance.AspNetCore` | tenant invitation delivery-status callback endpoint | conditional (`MapCephalonTenantInvitationDeliveryStatusCallbacks()`) |
| `GET /tenant-invitations/delivery-status/observations` | `Cephalon.MultiTenancy.Governance.AspNetCore` | tenant invitation delivery-status observations endpoint | conditional (`MapCephalonTenantInvitationDeliveryStatusObservations()`) |

The `/engine/cdc-capture-runtimes` family in particular has a large number of filter drill-downs that have been added incrementally (governance, drift, action-plans, write-path-readiness, preflight, dry-runs, execution-intents, execution-approvals, command-envelopes, command-issuances, command-retries, retry-execution-policies, command-journals, command-journal-durability, automatic-retries, automatic-retry-coordinations, distributed-retry-leases, distributed-retry-orchestrations, cross-node-idempotency-hardenings, multi-node-lease-executions, durable-shared-scheduler-orchestrations, scheduler-recovery-execution-hardenings, provider-owned-write-path-executions, provider-execution-orchestrations, provider-owned-control-plane-{ownership,mutation-reconcile,provisioning,apply-and-reconcile-executions,dependency-aware-apply-and-reconcile-hardenings,dependency-aware-provisioning-and-mutation-hardenings}, provider-specific-control-plane-materializers, and provider-specific-control-plane-dependency-aware-teardown-and-mutation-execution-hardenings). The `/engine/agent-tool-runs` family also has multiple filter drill-downs (`retry-pending`, `idempotency-duplicates`, `approval-required`, `terminal-failures`, `by-tool/{toolId}`, `{runId}`). Treat the route table above as the canonical entry point and let each route response document the available filters in its own metadata rather than re-enumerating every drill-down variant here.

Provider-specific invitation-delivery callbacks are also exposed under the `/engine/tenant-invitations/delivery-status/{provider}` namespace by their AspNetCore companion packs (for example `/engine/tenant-invitations/delivery-status/sendgrid` from `Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore`, `/engine/tenant-invitations/delivery-status/mailgun` from `Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore`, and `/engine/tenant-invitations/delivery-status/amazon-ses` from `Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore`); consult each provider companion's component doc for the canonical route pattern and signature-verification contract rather than re-listing them here.

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

`IDataProductCatalog`, `IProjectionCatalog`, `IOutboxCatalog`, `IInboxCatalog`, `IEventDispatchRuntimeCatalog`, `IEventDispatchRemediationRuntimeCatalog`, `IEventDispatchRemediationCommandReplayCursorCatalog`, `IEventPublicationRuntimeCatalog`, `IEventSubscriptionExecutionReadinessCatalog`, `ICdcCaptureCatalog`, `ICdcCaptureExecutionRuntimeCatalog`, `IDatabaseRoleCatalog`, `IDatabaseMigrationCatalog`.

`Cephalon.Eventing` also contributes `eventing-superiority-profile` through `ITechnologyRuntimeCatalog` / `TechnologySurfaces`; it is intentionally a technology surface rather than a new public catalog interface because it summarizes active runtime evidence and claim maturity (`claimed`, `partial`, `not-claimed`) from the existing eventing catalogs, including durable remediation command-audit evidence read from the active `IEventDispatchRemediationCommandJournal` descriptor. `IEventDispatchRemediationRuntimeCatalog.Summary` is the bounded remediation command-result roll-up behind `/engine/event-dispatch-remediation-commands/summary` and now carries reserved-command count, `HasInDoubtCommands`, oldest retained reserved-command id/timestamp, retention-truncation, dropped-command, incomplete-summary, and oldest-retained cutoff fields directly on `EventDispatchRemediationRuntimeSummary`; `IEventDispatchRemediationRuntimeCatalog.Latest` backs `/engine/event-dispatch-remediation-commands/latest` for the newest authoritative command record, `IEventDispatchRemediationRuntimeCatalog.Retention` backs `/engine/event-dispatch-remediation-commands/retention` so operators can tell whether bounded process-local command history has dropped older records, `IEventDispatchRemediationRuntimeCatalog.GetInDoubt()` backs `/engine/event-dispatch-remediation-commands/in-doubt` for retained reserved command records, `IEventDispatchRemediationRuntimeCatalog.GetInDoubtBefore(...)` backs `/engine/event-dispatch-remediation-commands/in-doubt?beforeUtc={beforeUtc}` for retained reserved records observed at or before an inclusive UTC cutoff, `IEventDispatchRemediationRuntimeCatalog.GetInDoubtSummaryBefore(...)` backs `/engine/event-dispatch-remediation-commands/in-doubt/summary?beforeUtc={beforeUtc}` for the retained reserved-record roll-up at or before the optional cutoff, `IEventDispatchRemediationRuntimeCatalog.GetOldestInDoubtBefore(...)` backs `/engine/event-dispatch-remediation-commands/in-doubt/oldest?beforeUtc={beforeUtc}` for the oldest retained reserved record at or before the optional cutoff, `IEventDispatchRemediationRuntimeCatalog.GetByObservedAt(...)` backs `/engine/event-dispatch-remediation-commands/observations?fromUtc={fromUtc}&toUtc={toUtc}` for inclusive observed-UTC incident-window reads over retained records, and `IEventDispatchRemediationRuntimeCatalog.GetSummaryByObservedAt(...)` backs `/engine/event-dispatch-remediation-commands/observations/summary?fromUtc={fromUtc}&toUtc={toUtc}` for retained-window roll-ups without materializing the full command list while marking `SummaryMayBeIncomplete` when the requested window may cross dropped history. ASP.NET Core list/filter routes over that catalog accept a positive `limit` query to return the newest retained records first; aggregate, latest, retention, in-doubt-summary, oldest-in-doubt, filter-summary, filter-retention, filter-latest, filter-oldest, and single-command reads keep their direct contracts. Capability and technology-surface metadata expose explicit list/result/in-doubt/in-doubt-summary/oldest-in-doubt/outbox/filter-summary/filter-retention/filter-latest/filter-oldest route keys, the command outcome route, `summaryReservedCount`, `summaryHasInDoubtCommands`, `summaryOldestReservedCommandId`, `summaryOldestReservedObservedAtUtc`, `commandInDoubt*` / `operatorCommandInDoubt*` keys, `commandInDoubtSummary*` / `operatorCommandInDoubtSummary*` keys, `commandOldestInDoubt*` / `operatorCommandOldestInDoubt*` keys, `commandFilterSummary*` / `operatorCommandFilterSummary*` keys, `commandFilterRetention*` / `operatorCommandFilterRetention*` keys, `commandFilterLatest*` / `operatorCommandFilterLatest*` keys, `commandFilterOldest*` / `operatorCommandFilterOldest*` keys, `commandReadLimit*` / `operatorCommandReadLimit*` keys, and `commandObservationWindow*` / `operatorCommandObservationWindow*` keys so clients can discover the route family, in-doubt summary posture, stale-reservation cutoff, stale-reservation aggregate read, oldest stale-reservation read, filter aggregate/retention/latest/oldest reads, list/filter limit, and observed-window policies from runtime introspection. The `event-dispatch-remediation-commands` technology surface also emits a catalog entry before any command exists, carrying list/result/in-doubt/in-doubt-summary/oldest-in-doubt/outbox/filter-summary/filter-retention/filter-latest/filter-oldest route, in-doubt cutoff, retention, idempotency, journal provider/durability/scope/replay-cursor posture, provider-neutral, and `wolverineRequired = false` metadata so clients can discover the route family without waiting for command history.

ENG-579 adds the command-reservation write side to that same read model. The active `IEventDispatchRemediationCommandJournal` reserves command ids before dispatch-store mutation and can expose `reserved` in-doubt records until final accepted/rejected results are recorded, so duplicate retries reject without mutation even before finalization.

ENG-580 adds the matching aggregate readback: `EventDispatchRemediationRuntimeSummary.ReservedCount` and `HasInDoubtCommands` expose retained reserved command records directly, and the `event-dispatch-remediation-commands` technology-surface catalog entry mirrors them through `summaryReservedCount` and `summaryHasInDoubtCommands`.

ENG-581 adds the age anchor for that aggregate readback: `EventDispatchRemediationRuntimeSummary.OldestReservedCommandId` and `OldestReservedObservedAtUtc` identify the oldest retained reserved command visible to the summary, and the `event-dispatch-remediation-commands` technology-surface catalog entry mirrors them through `summaryOldestReservedCommandId` and `summaryOldestReservedObservedAtUtc`.

ENG-582 adds the direct in-doubt readback: `IEventDispatchRemediationRuntimeCatalog.GetInDoubt()` and `/engine/event-dispatch-remediation-commands/in-doubt` expose retained `reserved` records without making operator clients scan every command-result record.

ENG-583 adds the stale in-doubt cutoff: `IEventDispatchRemediationRuntimeCatalog.GetInDoubtBefore(...)` and `/engine/event-dispatch-remediation-commands/in-doubt?beforeUtc={beforeUtc}` filter retained `reserved` records by inclusive observed UTC cutoff while preserving the same `limit`, paging, and route-bound continuation-token behavior.

ENG-584 adds the oldest stale in-doubt read: `IEventDispatchRemediationRuntimeCatalog.GetOldestInDoubtBefore(...)` and `/engine/event-dispatch-remediation-commands/in-doubt/oldest?beforeUtc={beforeUtc}` return one oldest retained `reserved` record for the optional inclusive cutoff, return `404` when no retained record matches, and stay out of list pagination/read-limit metadata because the response is intentionally single-record.

ENG-585 adds the stale in-doubt summary read: `IEventDispatchRemediationRuntimeCatalog.GetInDoubtSummaryBefore(...)` and `/engine/event-dispatch-remediation-commands/in-doubt/summary?beforeUtc={beforeUtc}` return the retained `reserved` roll-up for the optional inclusive cutoff, reject invalid cutoff values, and stay out of list pagination/read-limit metadata because the response is intentionally an aggregate.

ENG-586 adds filtered summary reads: `IEventDispatchRemediationRuntimeCatalog.GetSummaryByOutboxId(...)`, `GetSummaryByMessageId(...)`, `GetSummaryByChannelId(...)`, `GetSummaryByOperationId(...)`, `GetSummaryByActorId(...)`, `GetSummaryByCorrelationId(...)`, `GetSummaryByReason(...)`, `GetSummaryByOutcome(...)`, and `GetSummaryByDispatchOutcome(...)` back `/engine/event-dispatch-remediation-commands/outboxes/{outboxId}/summary`, `/messages/{messageId}/summary`, `/channels/{channelId}/summary`, `/operations/{operationId}/summary`, `/actors/{actorId}/summary`, `/correlations/{correlationId}/summary`, `/reasons/{reason}/summary`, `/outcomes/{outcome}/summary`, and `/dispatch-outcomes/{dispatchOutcome}/summary` so clients can read retained filter roll-ups without materializing list routes.

ENG-587 adds filtered latest reads: `IEventDispatchRemediationRuntimeCatalog.GetLatestByOutboxId(...)`, `GetLatestByMessageId(...)`, `GetLatestByChannelId(...)`, `GetLatestByOperationId(...)`, `GetLatestByActorId(...)`, `GetLatestByCorrelationId(...)`, `GetLatestByReason(...)`, `GetLatestByOutcome(...)`, and `GetLatestByDispatchOutcome(...)` back `/engine/event-dispatch-remediation-commands/outboxes/{outboxId}/latest`, `/messages/{messageId}/latest`, `/channels/{channelId}/latest`, `/operations/{operationId}/latest`, `/actors/{actorId}/latest`, `/correlations/{correlationId}/latest`, `/reasons/{reason}/latest`, `/outcomes/{outcome}/latest`, and `/dispatch-outcomes/{dispatchOutcome}/latest` so clients can open the newest retained matching command directly and receive `404` when no retained match exists.

ENG-588 adds filtered oldest reads: `IEventDispatchRemediationRuntimeCatalog.GetOldestByOutboxId(...)`, `GetOldestByMessageId(...)`, `GetOldestByChannelId(...)`, `GetOldestByOperationId(...)`, `GetOldestByActorId(...)`, `GetOldestByCorrelationId(...)`, `GetOldestByReason(...)`, `GetOldestByOutcome(...)`, and `GetOldestByDispatchOutcome(...)` back `/engine/event-dispatch-remediation-commands/outboxes/{outboxId}/oldest`, `/messages/{messageId}/oldest`, `/channels/{channelId}/oldest`, `/operations/{operationId}/oldest`, `/actors/{actorId}/oldest`, `/correlations/{correlationId}/oldest`, `/reasons/{reason}/oldest`, `/outcomes/{outcome}/oldest`, and `/dispatch-outcomes/{dispatchOutcome}/oldest` so clients can open the oldest retained matching command directly and receive `404` when no retained match exists.

ENG-589 adds filtered retention reads: `IEventDispatchRemediationRuntimeCatalog.GetRetentionByOutboxId(...)`, `GetRetentionByMessageId(...)`, `GetRetentionByChannelId(...)`, `GetRetentionByOperationId(...)`, `GetRetentionByActorId(...)`, `GetRetentionByCorrelationId(...)`, `GetRetentionByReason(...)`, `GetRetentionByOutcome(...)`, and `GetRetentionByDispatchOutcome(...)` back `/engine/event-dispatch-remediation-commands/outboxes/{outboxId}/retention`, `/messages/{messageId}/retention`, `/channels/{channelId}/retention`, `/operations/{operationId}/retention`, `/actors/{actorId}/retention`, `/correlations/{correlationId}/retention`, `/reasons/{reason}/retention`, `/outcomes/{outcome}/retention`, and `/dispatch-outcomes/{dispatchOutcome}/retention` so clients can read the matching retained-history posture directly and receive an empty `EventDispatchRemediationRuntimeRetention` when no retained match exists.

ENG-593 adds the durable command-journal replay cursor seam: `IEventDispatchRemediationCommandReplayCursorCatalog.LatestReplayCursor` and `GetAfterReplayCursor(...)` let provider-owned journals expose stable oldest-first replay over `(ObservedAtUtc, CommandId)` without changing ASP.NET Core newest-first paging tokens or claiming broker replay ownership.

ENG-594 projects that seam into `eventing-superiority-profile` through `durable-command-journal-replay-cursor`, so runtime consumers can distinguish durable command-journal replay support from durable audit posture and broker replay ownership.

ENG-595 adds `broker-dead-letter-replay-ownership` to the same profile so runtime consumers can see that broker DLQ ownership and broker replay stay `not-claimed` until a provider owns a broker descriptor and replay action path.

ENG-596 adds `broker-topology-materialization-ownership` to the same profile so runtime consumers can see that native route/channel governance is separate from broker exchange, queue, topic, and partition provisioning/verification.

ENG-597 adds `provider-partition-ownership` to the same profile so runtime consumers can see that route and topology evidence are separate from provider partition assignment, affinity, rebalancing, and ordering guarantees.

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
- the redaction surface is a cross-cutting contract that does not appear in any of the catalog tables above because it is consumer-registered through DI: `Cephalon.Diagnostics.Redaction.IRedactionFilter` (with `RedactionContext` and the orchestration helper `RedactionPipeline`) is registered against the consumer's `IServiceCollection` via `services.AddSingleton<IRedactionFilter>(...)` plus `services.AddRedactionPipeline()`; engine emission sites (`Cephalon.AspNetCore`'s HTTP request/response logging middleware, `Cephalon.Engine`'s module-phase activity tags, `Cephalon.Eventing.Wolverine`'s dispatch-time tags) resolve the pipeline lazily and route attribute values through it before exporter dispatch — see [`components/diagnostics.md`](components/diagnostics.md) *Redaction quick start* for the canonical adoption recipe

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
