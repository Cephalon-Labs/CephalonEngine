# Cephalon conformance matrix

This document is the per-package adoption-truth matrix for every shipped `Cephalon.*` package. Operators, platform teams, and AI agents can read this page to compare packages along the same dimensions without having to cross-reference the maturity audit, the component catalog, and the runtime contract index by hand.

Cross-references: [`engine-surface-maturity-audit.md`](engine-surface-maturity-audit.md), [`runtime-contract-index.md`](runtime-contract-index.md), [`components/README.md`](components/README.md), [`architecture.md`](architecture.md), [`architecture-review-2026-05.md`](architecture-review-2026-05.md), [`engineering-standards.md`](engineering-standards.md), [`compatibility.md`](compatibility.md), [`project-memory.md`](project-memory.md).

## How to read this matrix

Each row describes one shipped Cephalon package along these dimensions:

- **Package** — the published `Cephalon.*` package name
- **Family** — the shipping family this package belongs to (12 families total, see below)
- **Maturity** — `M0`/`M1`/`M2`/`M3`/`M4` per [`engine-surface-maturity-audit.md`](engine-surface-maturity-audit.md)
- **Ownership mode** — `taxonomy-only` / `application-managed` / `cephalon-managed` / `provider-managed` (or a mixed combination)
- **Engine routes** — `/engine/*` HTTP routes the package contributes when active
- **Snapshot keys** — `snapshot.*` keys the package contributes to `RuntimeIntrospectionSnapshot`
- **Catalog interfaces** — `I*Catalog` / `I*RuntimeCatalog` interfaces the package owns
- **Notes** — one-line summary; "(audit pending)" flags rows where the maturity audit is currently silent or where component docs and audit doc disagree, while "(family-covered by maturity audit)" flags rows where the audit declares maturity through a consolidated family-level entry rather than per-package detail

Maturity recap (full definitions in the maturity audit):

- `M0` — taxonomy or descriptor surface only; no runtime claim
- `M1` — runtime catalog or descriptor truth wired in; no managed execution
- `M2` — narrow managed execution proof (single vertical slice or dispatch loop)
- `M3` — broader managed execution; multiple vertical paths working together
- `M4` — adoption-ready surface; consumers can rely on it across project shapes

Ownership recap:

- `taxonomy-only` — surface exists as a name and shape, no behavior claim
- `application-managed` — runtime ownership belongs to the consuming app
- `cephalon-managed` — runtime ownership belongs to the engine itself
- `provider-managed` — runtime ownership belongs to a specific provider companion pack

Family recap (12 families, in order below):

`core-runtime`, `host-adapter`, `transport-adapter`, `behaviors`, `eventing`, `agentics`, `data-and-cdc`, `event-sourcing`, `multi-tenancy`, `audit-and-identity`, `edge`, `observability`, `scaffolding-and-tooling`.

When this matrix conflicts with the maturity audit or a component doc, the audit is the authoritative truth for maturity and the component docs are the authoritative truth for ownership detail. This page is the consolidated read; it should not become a third source of truth.

## Core runtime

| Package | Maturity | Ownership | Engine routes | Snapshot keys | Catalog interfaces | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| `Cephalon.Abstractions` | M4 | cephalon-managed | — | — | many (`IModule`, `IAppBehavior`, `IBehaviorContext`, `IAgentToolDispatcher`, `IKnowledgeIndexCatalog`, `IEventPublicationDispatcher`, `ITenantResolver`, ...) | host-agnostic contract layer; stable across every domain |
| `Cephalon.Engine` | M4 | cephalon-managed | `/manifest`, `/snapshot`, `/app-model`, `/capabilities`, `/modules`, `/packages`, `/technologies`, `/patterns` | `Manifest`, `Status`, `OperationalStory`, `DiagnosticsConventions` plus composition for every snapshot key | `ICellBoundaryCatalog`, `ICellRouteCatalog`, `ICellHealthIsolationCatalog`, `ICellTrafficAutomationRuntimeCatalog`, `ITechnologyRuntimeCatalog`, `IFeatureFlagRuntimeCatalog` | composition center, manifest, runtime introspection, policy evaluation |

The core-runtime family is adoption-ready and stable. Priorities are compatibility hygiene, docs alignment, and absorbing new companion packs without core surgery.

## Host adapters

| Package | Maturity | Ownership | Engine routes | Snapshot keys | Catalog interfaces | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| `Cephalon.AspNetCore` | M4 | cephalon-managed | every base `/engine/*` plus `/health`, `/health/live`, `/health/ready`; projects every optional `/engine/*` route when its companion is active | projects every `snapshot.*` key over HTTP | every shipped catalog interface via projection | HTTP-first host adapter; the `/engine/*` projection layer |
| `Cephalon.Worker` | M4 | cephalon-managed | — | — | — | generic-host worker without HTTP; split-configuration support |

Both host adapters are adoption-ready. Web hosts go through `Cephalon.AspNetCore`; pure background or worker hosts go through `Cephalon.Worker`. Adapter responsibilities stay thin: the host adapter does not own runtime truth, only its projection.

## Transport adapters

| Package | Maturity | Ownership | Engine routes | Snapshot keys | Catalog interfaces | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| `Cephalon.AspNetCore.GraphQL` | M2 | cephalon-managed | — (hooks into `Cephalon.AspNetCore`) | — | — | GraphQL transport route mapping companion |
| `Cephalon.AspNetCore.JsonRpc` | M2 | cephalon-managed | — (hooks into `Cephalon.AspNetCore`) | — | — | JSON-RPC transport route mapping companion |
| `Cephalon.AspNetCore.Grpc` | M2 | cephalon-managed | — (hooks into `Cephalon.AspNetCore`) | — | — | gRPC transport route mapping companion |

Transport adapters are narrow execution proofs. REST is base in `Cephalon.AspNetCore`; GraphQL, JSON-RPC, and gRPC ship as optional alternative media types over the same route ownership.

## Behaviors and execution

| Package | Maturity | Ownership | Engine routes | Snapshot keys | Catalog interfaces | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| `Cephalon.Behaviors` | M4 | cephalon-managed | — | `ExecutionGraphs`, `HostedExecutions`, `BehaviorResiliencePolicies`, `DurableExecutions`, `DurableExecutionStates`, `SagaChoreographies`, `SagaChoreographyPublicationStates` | `IBehaviorCatalog`, `IBehaviorRegistry`, `ISagaChoreographyRuntimeCatalog`, `ISagaChoreographyPublicationRuntimeStateCatalog`, `IBehaviorResilienceRuntimeCatalog`, `IDurableExecutionRuntimeCatalog`, `IDurableExecutionRuntimeStateCatalog` | adaptive behavior topology (ABT) dispatch, topology resolution, compatibility validation |
| `Cephalon.Behaviors.Http` | M2 | mixed: `application-managed` profile/publication activation + `cephalon-managed` materialization, governance, and runtime catalogs | — (publishes through `Cephalon.AspNetCore`) | — | — | REST profile metadata plus explicit module-owned public REST activation; Cephalon-managed materialization, governance, and runtime catalogs |
| `Cephalon.Behaviors.Messaging` | M1 | application-managed | — | — | — | messaging transport bindings, descriptors only |
| `Cephalon.Behaviors.Patterns` | M1 | application-managed | — | — | — | pattern composition helpers (saga publisher contracts, durable execution helpers) |
| `Cephalon.Behaviors.SourceGen` | M1 | cephalon-managed | — | — | — | source generator for behavior registration and topology helpers |

The behaviors family is the engine's core execution backbone. `Cephalon.Behaviors` is adoption-ready; HTTP and messaging companions own their narrower transport seams. Pattern and source-generator companions are catalog/utility surfaces.

## Eventing

| Package | Maturity | Ownership | Engine routes | Snapshot keys | Catalog interfaces | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| `Cephalon.Eventing` | M3 | mixed: application-managed + cephalon-managed | `/event-subscription-readiness`, `POST /engine/event-publications`, `/engine/event-publications/runtime*`, `/engine/event-dispatches/terminal-failures` | `EventDispatchRuntimes`, `EventDispatchStates`, `EventPublicationStates`, `EventSubscriptionExecutionReadiness` | `IEventSubscriptionExecutionBindingCatalog`, `IEventSubscriptionExecutionReadinessCatalog`, `IEventPublicationRuntimeCatalog`, `IEventDispatchRuntimeCatalog` | channel/subscription descriptors, in-process execution baseline, bounded retry/idempotency, publication/dispatch state truth |
| `Cephalon.Eventing.Wolverine` | M3 | provider-managed | (projected through parent eventing) | `EventDispatchStates`, `EventPublicationStates` | — | optional Wolverine-managed dispatch lane with terminal dispatch/subscription retry |
| `Cephalon.Eventing.Behaviors` | M1 | application-managed | — | — | — | behavior-native eventing bridge; saga choreography handoff into outbox-backed eventing |

Eventing's core baseline owns the in-process execution lane and operator state surfaces. Wolverine is the first provider-managed dispatch follow-through. The behaviors-native bridge is the explicit phase-12 seam between saga choreography authoring and outbox-backed publication.

## Agentics and retrieval

| Package | Maturity | Ownership | Engine routes | Snapshot keys | Catalog interfaces | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| `Cephalon.Agentics` | M3 | mixed: application-managed + cephalon-managed | `/engine/agent-tools`, `POST /engine/agent-tools/{toolId}/runs`, `/engine/agent-tool-runs`, `/engine/agent-tool-runs/retry-pending`, `/engine/agent-tool-runs/idempotency-duplicates`, `/engine/agent-tool-runs/approval-required`, `/engine/agent-tool-runs/terminal-failures` | `AgentToolRuns` | `IAgentToolRunCatalog`, `IAgentToolDispatcher`, `IAgentToolExecutor`, `IAgentToolExecutionPolicy`, `IAgentToolExecutionObserver` | tool descriptors, managed dispatch loop, bounded retry/idempotency, operator action seams |
| `Cephalon.Retrieval` | M3 | mixed: application-managed + cephalon-managed | `/engine/knowledge-indexes`, `POST /engine/knowledge-indexes/{collectionId}/queries`, `POST /engine/knowledge-indexes/{collectionId}/reindex` | `KnowledgeIndexes` | `IKnowledgeIndexCatalog` | lexical indexing, query execution, freshness state, background reindex scheduler |

Both agentic surfaces are managed vertical proofs. Cephalon-managed dispatch and indexing already work; broader durable agent queues, memory persistence, and provider-specific orchestration remain future work.

## Data and CDC

| Package | Maturity | Ownership | Engine routes | Snapshot keys | Catalog interfaces | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| `Cephalon.Data` | M3 | mixed: cephalon-managed + provider-managed | `/engine/cdc-captures`, `/engine/cdc-captures/runtime*`, `/engine/cdc-capture-runtimes`, `POST /engine/cdc-capture-runtimes/{executionRuntimeId}/reports`, `/engine/data-products`, `/engine/outboxes`, `/engine/inboxes`, `/engine/projections`, `/engine/databases`, `/engine/database-topology`, `/engine/database-roles`, `/engine/database-migrations`, `/engine/database-migration-playbook` | `CdcCaptures`, `CdcCaptureStates`, `CdcCaptureExecutionRuntimes`, `DataProducts`, `Outboxes`, `Inboxes`, `Projections`, `DatabaseRoles`, `DatabaseMigrations`, `DatabaseMigrationPlaybook`, `DatabaseTopology` | `ICdcCaptureCatalog`, `ICdcCaptureExecutionRuntimeCatalog`, `IOutboxCatalog`, `IInboxCatalog`, `IDataProductCatalog`, `IProjectionCatalog`, `IDatabaseRoleCatalog`, `IDatabaseMigrationCatalog` | CDC runtime state, shared execution substrate, outbox/inbox descriptors, data product descriptors, database topology |
| `Cephalon.Data.EntityFramework` | M2 | cephalon-managed | — | — | `IDataProductCatalog`, `IOutboxCatalog`, `IInboxCatalog` | DbContext-backed read/write stores, outbox/inbox, entity event sourcing baseline |
| `Cephalon.Data.SqlServer` | M2 | provider-managed | — (CDC projects via `Cephalon.Data`) | `DatabaseRoles`, `DatabaseMigrations`, `DatabaseTopology` | `IDatabaseRoleCatalog`, `IDatabaseMigrationCatalog` | SQL Server data + CDC capture provider |
| `Cephalon.Data.Postgres` | M2 | provider-managed | — | `DatabaseRoles`, `DatabaseMigrations`, `DatabaseTopology` | `IDatabaseRoleCatalog`, `IDatabaseMigrationCatalog` | PostgreSQL data + logical-replication CDC provider |
| `Cephalon.Data.MySql` | M2 | provider-managed | — | `DatabaseRoles`, `DatabaseMigrations`, `DatabaseTopology` | `IDatabaseRoleCatalog`, `IDatabaseMigrationCatalog` | MySQL data pack |
| `Cephalon.Data.Oracle` | M2 | provider-managed | — | `DatabaseRoles`, `DatabaseMigrations`, `DatabaseTopology` | `IDatabaseRoleCatalog`, `IDatabaseMigrationCatalog` | Oracle Database data pack |
| `Cephalon.Data.MongoDB` | M2 | provider-managed | — | — | `IDataProductCatalog` | MongoDB data + change-stream CDC provider |
| `Cephalon.Data.Redis` | M1 | provider-managed | — | — | — | Redis cache and data structures pack (audit pending) |
| `Cephalon.Data.Neo4j` | M1 | provider-managed | — | — | — | Neo4j graph database pack (audit pending) |
| `Cephalon.Data.Cassandra` | M1 | provider-managed | — | — | — | Apache Cassandra pack (audit pending) |
| `Cephalon.Data.ClickHouse` | M1 | provider-managed | — | — | — | ClickHouse OLAP pack (audit pending) |
| `Cephalon.Data.Elasticsearch` | M1 | provider-managed | — | — | — | Elasticsearch pack (audit pending) |
| `Cephalon.Data.OpenSearch` | M1 | provider-managed | — | — | — | OpenSearch pack (audit pending) |
| `Cephalon.Data.Qdrant` | M1 | provider-managed | — | — | — | Qdrant vector database pack (audit pending) |
| `Cephalon.Data.Nats` | M1 | provider-managed | — | — | — | NATS messaging pack (audit pending) |
| `Cephalon.Data.Debezium` | M1 | provider-managed | — | — | `ICdcCaptureCatalog` | Debezium-managed CDC sourcing pack (audit pending) |

`Cephalon.Data` core ships as M3 with truthful cross-provider CDC truth. Relational providers (`SqlServer`, `Postgres`, `MySql`, `Oracle`) own schema/role/migration truth at M2 and contribute provider-native CDC where applicable. Non-relational providers stay at M1 catalog/descriptor maturity until their managed-execution proof lands.

## Event sourcing

| Package | Maturity | Ownership | Engine routes | Snapshot keys | Catalog interfaces | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| `Cephalon.EventSourcing` | M1 | application-managed | — | — | — | event-sourced aggregate contracts and descriptors (family-covered by maturity audit) |
| `Cephalon.EventSourcing.EntityFramework` | M1 | provider-managed | — | — | — | Entity Framework event store (family-covered by maturity audit) |
| `Cephalon.EventSourcing.MongoDB` | M1 | provider-managed | — | — | — | MongoDB event store (family-covered by maturity audit) |
| `Cephalon.EventSourcing.Redis` | M1 | provider-managed | — | — | — | Redis event store (family-covered by maturity audit) |
| `Cephalon.EventSourcing.Neo4j` | M1 | provider-managed | — | — | — | Neo4j event store (family-covered by maturity audit) |
| `Cephalon.EventSourcing.Cassandra` | M1 | provider-managed | — | — | — | Cassandra event store (family-covered by maturity audit) |
| `Cephalon.EventSourcing.ClickHouse` | M1 | provider-managed | — | — | — | ClickHouse event store (family-covered by maturity audit) |
| `Cephalon.EventSourcing.Elasticsearch` | M1 | provider-managed | — | — | — | Elasticsearch event store (family-covered by maturity audit) |
| `Cephalon.EventSourcing.OpenSearch` | M1 | provider-managed | — | — | — | OpenSearch event store (family-covered by maturity audit) |
| `Cephalon.EventSourcing.Qdrant` | M1 | provider-managed | — | — | — | Qdrant event store (family-covered by maturity audit) |
| `Cephalon.EventSourcing.Nats` | M1 | provider-managed | — | — | — | NATS event store (family-covered by maturity audit) |

The event-sourcing family is currently catalog-only. The maturity audit does not yet enumerate each pack individually; broad managed-execution proof remains future work.

## Multi-tenancy

| Package | Maturity | Ownership | Engine routes | Snapshot keys | Catalog interfaces | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| `Cephalon.MultiTenancy` | M2 | cephalon-managed | — | `TenantMemberships`, `TenantInvitations`, `TenantDomainOwnership`, `TenantGovernanceActions`, `TenantAdministration` | `ITenantResolver`, `ITenantContextAccessor` | tenant resolution, ambient context, governance boundaries |
| `Cephalon.MultiTenancy.Governance` | M2 | mixed: cephalon-managed + provider-managed | `/engine/tenant-memberships`, `/engine/tenant-invitations`, `/engine/tenant-domain-ownership`, `/engine/tenant-governance-actions`, `/engine/tenant-administration` | `TenantMemberships`, `TenantInvitations`, `TenantInvitationDeliveryRuns`, `TenantDomainOwnership`, `TenantGovernanceActions`, `TenantAdministration` | `ITenantMembershipCatalog`, `ITenantInvitationCatalog`, `ITenantDomainOwnershipCatalog`, `ITenantGovernanceActionCatalog`, `ITenantAdministrationWorkflow`, plus 20+ governance interfaces | governance descriptors, stores, validators, delivery dispatch, workflow transitions |
| `Cephalon.MultiTenancy.Governance.AspNetCore` | M2 | cephalon-managed | `POST /engine/tenant-administration/commands` | `TenantInvitationDeliveryRuns`, `TenantAdministration` | — | ASP.NET Core tenant-admin commands, delivery callbacks, observation reads |
| `Cephalon.MultiTenancy.Governance.HttpDelivery` | M2 | cephalon-managed | — | — | `ITenantInvitationDeliverySender` | HTTP webhook invitation sender |
| `Cephalon.MultiTenancy.Governance.SmtpDelivery` | M2 | cephalon-managed | — | — | `ITenantInvitationDeliverySender` | SMTP relay invitation sender |
| `Cephalon.MultiTenancy.Governance.SendGridDelivery` | M2 | cephalon-managed | — | — | `ITenantInvitationDeliverySender` | SendGrid Mail API invitation sender |
| `Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore` | M2 | cephalon-managed | — (callback receiver) | — | — | SendGrid Event Webhook translation and verification |
| `Cephalon.MultiTenancy.Governance.MailgunDelivery` | M2 | cephalon-managed | — | — | `ITenantInvitationDeliverySender` | Mailgun Messages API invitation sender |
| `Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore` | M2 | cephalon-managed | — (callback receiver) | — | — | Mailgun webhook translation, HMAC verification, idempotency |
| `Cephalon.MultiTenancy.Governance.AmazonSesDelivery` | M2 | cephalon-managed | — | — | `ITenantInvitationDeliverySender` | Amazon SES v2 invitation sender |
| `Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore` | M2 | cephalon-managed | — (callback receiver) | — | — | Amazon SES SNS callback translation, verification, idempotency |
| `Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery` | M2 | cephalon-managed | — | — | `ITenantInvitationDeliverySender` | Microsoft Graph sendMail invitation sender |
| `Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.AzureIdentity` | M2 | cephalon-managed | — | — | — | Azure.Identity token provider for Microsoft Graph |

Multi-tenancy is one of the deepest companion proofs in the repo: 21 governance proofs landed between `ENG-235` and `ENG-256`, plus six independent invitation-delivery integrations. Distributed governance storage, identity-provider sync, and additional sender families remain future work.

## Audit and identity

| Package | Maturity | Ownership | Engine routes | Snapshot keys | Catalog interfaces | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| `Cephalon.Audit` | M1 | mixed: `cephalon-managed` in-memory writer baseline, catalog projection, ambient actor resolution + `application-managed` consumer actor accessors and durable storage | `/engine/audit-stores`, `/engine/audit-history`, `/engine/audit-history/export` | `AuditStores` | `IAuditStoreCatalog` | host-agnostic audit-recording baseline; `/engine/audit-*` routes are M1 catalog projections of `IAuditStoreCatalog`/`IAuditHistoryReader`/`IAuditHistoryExporter` truth |
| `Cephalon.Audit.EntityFramework` | M1 | provider-managed | — | — | — | Entity Framework audit history: queryable `IAuditHistoryReader` plus bounded NDJSON `IAuditHistoryExporter` over `Engine:Audit:History` configuration |
| `Cephalon.Identity` | M1 | mixed: `cephalon-managed` default metadata-driven evaluator, runtime surface, catalog projection, diagnostics + `application-managed` identity scheme and principal flow | `/engine/authorization-policies` | `AuthorizationPolicies` | `IAuthorizationPolicyCatalog` | host-agnostic identity and authorization baseline; `/engine/authorization-policies` is the M1 catalog projection of `IAuthorizationPolicyCatalog` truth, and `identity-authorization` is the runtime surface |
| `Cephalon.Identity.AspNetCore` | M1 | application-managed | — | — | — | ASP.NET Core integration: minimal-API and controller policy mapping over the host-agnostic evaluator, optional `ClaimsPrincipal` projection into the ambient `IAuditActorAccessor` contract |
| `Cephalon.Ids.Sfid` | M2 | cephalon-managed | — | — | — | Snowflake ID (Sfid) generator with canonical sorting |

Audit and identity ship as host-agnostic baselines at M1: each pack contributes truthful catalog projections (`/engine/audit-*`, `/engine/authorization-policies`) plus narrow Cephalon-managed defaults (in-memory audit writer, metadata-driven authorization evaluator, identity runtime surface) without forcing one durable storage opinion or one identity scheme onto consumers. The Entity Framework audit pack is the first durable provider follow-through. Sfid stands alone at M2 as a fully managed identifier-generator surface. Distributed audit ledgers, broader authorization-decision proof, durable policy stores, and provider-specific identity-provider sync remain future work.

## Edge

| Package | Maturity | Ownership | Engine routes | Snapshot keys | Catalog interfaces | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| `Cephalon.Edge` | M2 | cephalon-managed | `/engine/cells`, `/engine/cell-routes`, `/engine/cell-health-isolations`, `/engine/cell-traffic-automations` | `CellBoundaries`, `CellRoutes`, `CellHealthIsolations`, `CellTrafficAutomations` | `ICellTrafficAutomationRuntimeCatalog` (contributor) | edge node descriptors, cell traffic automation materialization seam |
| `Cephalon.Edge.KubernetesGateway` | M3 | provider-managed | — (projects via `Cephalon.Edge`) | `CellTrafficAutomations` | `ICellTrafficAutomationProviderMaterializer` | Kubernetes Gateway API materialization with apply-and-reconcile + cleanup sweep |
| `Cephalon.Edge.Traefik` | M3 | provider-managed | — (projects via `Cephalon.Edge`) | `CellTrafficAutomations` | `ICellTrafficAutomationProviderMaterializer` | Traefik IngressRoute materialization with apply-and-reconcile + cleanup sweep |

Edge already has two real provider-native control-plane materializers running on a shared lifecycle vocabulary (ownership, dependency, drift, lifecycle action, condition). Additional edge providers (Istio, Envoy, AWS App Mesh, etc.) remain future work.

## Observability

| Package | Maturity | Ownership | Engine routes | Snapshot keys | Catalog interfaces | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| `Cephalon.Observability` | M2 | cephalon-managed | (startup summary projects via `/manifest`) | `Manifest` (startup summary) | — | observability options, startup summaries, shared telemetry contract |
| `Cephalon.Observability.OpenTelemetry` | M1 | cephalon-managed | — | — | — | OTLP exporter configuration (family-covered by maturity audit) |
| `Cephalon.Observability.Kubernetes` | M1 | cephalon-managed | — | — | — | Kubernetes collector configuration (family-covered by maturity audit) |
| `Cephalon.Observability.Aws` | M1 | cephalon-managed | — | — | — | AWS X-Ray and CloudWatch configuration (family-covered by maturity audit) |
| `Cephalon.Observability.Gcp` | M1 | cephalon-managed | — | — | — | GCP Cloud Trace and Monitoring configuration (family-covered by maturity audit) |
| `Cephalon.Observability.AzureMonitor` | M1 | cephalon-managed | — | — | — | Azure Monitor configuration (family-covered by maturity audit) |
| `Cephalon.Observability.NewRelic` | M1 | cephalon-managed | — | — | — | New Relic OTLP configuration (family-covered by maturity audit) |
| `Cephalon.Observability.GrafanaCloud` | M1 | cephalon-managed | — | — | — | Grafana Cloud configuration (family-covered by maturity audit) |
| `Cephalon.Observability.AlibabaCloud` | M1 | cephalon-managed | — | — | — | Alibaba Cloud configuration (family-covered by maturity audit) |
| `Cephalon.Observability.OracleCloud` | M1 | cephalon-managed | — | — | — | Oracle Cloud APM configuration (family-covered by maturity audit) |
| `Cephalon.Observability.DigitalOcean` | M1 | cephalon-managed | — | — | — | DigitalOcean App Platform configuration (family-covered by maturity audit) |
| `Cephalon.Observability.HuaweiCloud` | M1 | cephalon-managed | — | — | — | Huawei Cloud APM configuration (family-covered by maturity audit) |
| `Cephalon.Observability.OpenShift` | M1 | cephalon-managed | — | — | — | OpenShift in-cluster collector configuration (family-covered by maturity audit) |
| `Cephalon.Observability.Tanzu` | M1 | cephalon-managed | — | — | — | VMware Tanzu configuration (family-covered by maturity audit) |
| `Cephalon.Observability.Serilog` | M1 | cephalon-managed | — | — | — | Serilog provider integration (family-covered by maturity audit) |
| `Cephalon.Observability.DependencyHealth.Core` | M3 | cephalon-managed | `/engine/dependencies` | — | `IDependencyHealthContributor` | dependency-health probe infrastructure with operator metadata (freshness/duration/failure tracking) |
| `Cephalon.Observability.*Dependencies` (Cassandra / ClickHouse / Consul / Elasticsearch / Http / Kafka / Memcached / MongoDB / MQTT / MySQL / NATS / Neo4j / OpenSearch / Oracle / PostgreSQL / RabbitMQ / Redis / SqlServer) | M3 | provider-managed | (projects via `/engine/dependencies`) | — | `IDependencyHealthContributor` | provider-specific dependency-health probe execution with operator-facing freshness/duration/failure-count metadata

The observability family ships adoption-ready telemetry configuration packs and dependency-health probe execution with operator automation support. Genuine telemetry collection happens in the host runtime's OpenTelemetry stack; dependency-health probes now support operator workflows through freshness, duration, and failure-count tracking.

## Scaffolding and tooling

| Package | Maturity | Ownership | Engine routes | Snapshot keys | Catalog interfaces | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| `Cephalon.Scaffolding` | M4 | cephalon-managed | — | — | — | scaffold generation, package version catalog, deployment asset rendering |
| `Cephalon.Cli` | M4 | cephalon-managed | — | — | — | blueprint-driven app generation, package staging, doctor checks, docs publishing |
| `Cephalon.ReferenceDocs` | M2 | cephalon-managed | — | — | — | XML-doc reference publishing, hosted docs configuration |

The tooling family ships adoption-ready CLI plus scaffolding and an XML-doc reference publisher. Reference-doc generation runs against the live engine surface and is wired into release validation.

## Family summary at a glance

- **Adoption-ready (M4):** core runtime, host adapters, behaviors core, scaffolding, CLI
- **Broad managed execution (M3):** eventing core, eventing Wolverine bridge, agentics, retrieval, data core, edge providers (Kubernetes Gateway, Traefik), observability dependency-health core + provider probes
- **Narrow managed execution (M2):** transport adapters, behaviors HTTP, EntityFramework data, relational data providers (SqlServer, Postgres, MySql, Oracle, MongoDB), multi-tenancy core + governance + delivery senders, edge core, observability core, Sfid, ReferenceDocs
- **Catalog-only (M1):** behaviors messaging/patterns/sourcegen, eventing behaviors bridge, non-relational data providers (Redis, Neo4j, Cassandra, ClickHouse, Elasticsearch, OpenSearch, Qdrant, Nats, Debezium), event-sourcing family, audit, identity, observability provider configuration packs
- **Taxonomy-only (M0):** (none in current audit)

## Inconsistencies observed (potential next ENG-* cards)

No open inconsistencies remain at this snapshot. The list below records the alignment items resolved in May-June 2026 so the durable history stays visible.

### Resolved alignment items (kept as durable history)

- **Dependency-health probe maturity uplift from M0 → M3 (May 2026):** added operator-facing runtime metadata (CheckedAtUtc, ProbeDurationMilliseconds, ConsecutiveFailureCount) to support observability loops and health trending. Core probe infrastructure and all 18 per-provider probes promoted from M0 (taxonomy-only) to M3 (managed execution) with genuine probe runtime, operator automation support, and 68 hosting test coverage across 14 providers.
- **Trust policy operator metadata uplift (June 2026):** added runtime freshness and performance metadata to trust surfaces (`TrustSnapshot.EvaluatedAtUtc`, `PackageTrustDecision.VerifiedAtUtc`, `PackageTrustDecision.VerificationDurationMilliseconds`, `CapabilityPolicyDecision.EvaluatedAtUtc`). This closes the operator-observability gap for `/engine/trust-policy` without changing ownership or compatibility posture.
- **Diagnostics surface operator metadata uplift (June 2026):** added diagnostics payload freshness and evaluation timing metadata (`generatedAtUtc`, `livenessEvaluationDurationMilliseconds`, `readinessEvaluationDurationMilliseconds`) through `DiagnosticsSurface` and `/engine/diagnostics`, with targeted hosting assertions in both typed and JSON flows.
- **`Cephalon.Behaviors.Http` REST publication ownership** (resolved May 2026): the matrix row now records the explicit `application-managed` profile/publication activation plus `cephalon-managed` materialization/runtime-catalog split, mirroring the maturity audit row. The shared component doc remains the per-package detail source.
- **Event-sourcing family audit coverage** (resolved May 2026): the maturity audit now records the catalog-only stance for the event-sourcing family and confirms no provider pack has shipped managed-execution proof yet. See the audit's "Event sourcing" group entry.
- **Observability provider audit closure** (resolved May 2026): the maturity audit now records the cloud-platform configuration-pack baseline plus the dependency-health pack family stance. Each cloud pack stays M1 cephalon-managed configuration binding; deeper telemetry collection remains in the host runtime's OpenTelemetry stack.
- **`Cephalon.Audit` and `Cephalon.Identity` route projection** (resolved May 2026): the maturity audit now records explicit Audit, Audit.EntityFramework, Identity, Identity.AspNetCore, and Ids.Sfid rows, and the matrix Notes field for each row reflects whether `/engine/*` routes are M1 catalog projections or runtime-surface projections.
- **Multi-tenancy delivery sender maturity granularity** (resolved May 2026): the maturity audit now records a consolidated invitation-delivery sender family row that captures the deliberate shared-M2 stance across HttpDelivery, SmtpDelivery, SendGridDelivery, MailgunDelivery, AmazonSesDelivery, and MicrosoftGraphDelivery (plus their AspNetCore and AzureIdentity companions). The family floor stays M2 because each sender owns the same managed dispatch, callback handling shape, replay protection, and idempotency hygiene over a different upstream API. Per-sender promotion above the floor requires sender-specific adoption proof plus matching matrix/component-doc/planning updates in one slice.
- **EventSourcing and Observability "(audit pending)" Notes-field accuracy** (resolved May 2026): the audit's family-level rows already enumerate every shipped `Cephalon.EventSourcing` pack (core plus 10 provider packs at `M1` family-level) and every shipped `Cephalon.Observability` configuration / dependency-health-core pack (cloud/exporter family at `M1` `cephalon-managed`). The Notes-field tag on those 26 matrix rows now reads "(family-covered by maturity audit)" instead of "(audit pending)" so the matrix reflects that the audit is no longer silent on those packs. The "(audit pending)" tag remains on Notes only when the audit is genuinely silent — currently the 9 non-relational `Cephalon.Data.*` rows whose individual audit lines are still pending.

When a row in this matrix becomes inaccurate, update both this page and the [`engine-surface-maturity-audit.md`](engine-surface-maturity-audit.md) entry in the same slice. Do not let one page drift while the other is updated.

## Maintenance posture

This document is the consolidated read across maturity audit + component catalog + runtime contract index. It is meant to evolve continuously rather than freeze. The standing rules for when to update this matrix vs. when to leave it alone live in [`planning-governance.md`](planning-governance.md) under the ["Conformance matrix maintenance"](planning-governance.md) section; this paragraph is the local reminder, not the durable rule.

- update this matrix in the same slice as the source of truth: when a package changes maturity or ownership, update both `engine-surface-maturity-audit.md` and the matching row here
- add a new row when a new shipped `Cephalon.*` package enters the repository
- delete rows when a shipped package is intentionally retired and its retirement is documented in `compatibility.md`
- when a row's `Notes` field reaches more than one line of detail, move that detail to the matching component doc and link to it instead
- review the matrix at the end of each architecture-review cycle to confirm it still matches reality

This page does not assign new policies or new requirements. It only consolidates what other docs already declare.
