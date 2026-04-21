# Cephalon Engine Architecture Inventory

Architecture inventory in this document reflects the repository state as of `April 22, 2026`.

Cross-references: `docs/architecture.md`, `docs/engine-roadmap.md`, `docs/engine-backlog.md`

## Overview

CephalonEngine is a configuration-driven, modular runtime platform. This document catalogs every registered blueprint, pattern, technology, transport, execution strategy, module, capability, data abstraction, diagnostics source, observability companion, and cloud exporter that ships with the engine.

## Blueprints

Blueprints define the top-level application shape. Each blueprint composes a fixed set of patterns.

### App blueprints (3)

- `modular-monolith` — Modular Monolith: a single host application with modular composition and module-first organization. Patterns: `modular-architecture`, `single-host-topology`, `module-first-organization`, `shared-foundation-pattern`.
- `modular-vertical-slice` — Modular Vertical Slice: a single host application with modules as boundaries and vertical slices inside each module. Patterns: `modular-architecture`, `single-host-topology`, `vertical-slice-organization`, `shared-foundation-pattern`.
- `microservice` — Microservice: an independently deployable service that still uses Cephalon modules and shared foundation. Patterns: `modular-architecture`, `microservice-topology`, `vertical-slice-organization`, `shared-foundation-pattern`.

### Suite blueprints (1)

- `microservice-suite` — Microservice Suite: a coordinated suite of Cephalon microservices composed from repeatable service slots based on the `microservice` app blueprint.

## Patterns

Patterns describe architectural decisions that can be composed into blueprints or selected individually through configuration.

### Pattern kinds (8)

- `Composition` (0) — how modules are composed together
- `Deployment` (1) — topology and deployment strategies
- `Organization` (2) — code organization within boundaries
- `Foundation` (3) — shared foundation and platform
- `Design` (4) — design patterns
- `Architecture` (5) — architecture-level patterns
- `Domain` (6) — domain-modeling patterns
- `Data` (7) — data and persistence patterns

### Built-in pattern descriptors (21)

#### Composition

- `modular-architecture` — Modular Architecture: composes the application from explicit modules with bounded responsibilities. Aliases: `ModularArchitecture`.

#### Deployment

- `single-host-topology` — Single Host Topology: runs the application as one deployable host while preserving internal boundaries. Aliases: `SingleHostTopology`, `SingleHost`. Conflicts with: `microservice-topology`.
- `microservice-topology` — Microservice Topology: deploys the application as an independently deployable service boundary. Aliases: `MicroserviceTopology`, `Microservice`. Conflicts with: `single-host-topology`.

#### Organization

- `vertical-slice-organization` — Vertical Slice Organization: organizes code around features so endpoints, handlers, and rules stay together. Aliases: `VerticalSliceOrganization`, `VerticalSlice`. Conflicts with: `module-first-organization`.
- `module-first-organization` — Module-First Organization: organizes code around modules first, then features inside each module. Aliases: `ModuleFirstOrganization`, `ModuleFirst`. Conflicts with: `vertical-slice-organization`.

#### Foundation

- `shared-foundation-pattern` — Shared Foundation Pattern: uses the Cephalon engine foundation for contracts, runtime conventions, and diagnostics. Aliases: `SharedFoundationPattern`, `SharedFoundation`.

#### Design

- `strategy-pattern` — Strategy Pattern: enables pluggable behaviors that can be swapped without changing the calling feature. Aliases: `StrategyPattern`, `Strategy`.
- `pipeline-pattern` — Pipeline Pattern: applies behavior through ordered stages such as validation, enrichment, and execution. Aliases: `PipelinePattern`, `Pipeline`.
- `mediator-pattern` — Mediator Pattern: routes requests through handlers to keep senders and receivers decoupled. Aliases: `MediatorPattern`, `Mediator`.
- `specification-pattern` — Specification Pattern: encapsulates reusable business rules and query predicates behind explicit specifications. Aliases: `SpecificationPattern`, `Specification`.

#### Architecture

- `hexagonal-architecture` — Hexagonal Architecture: keeps domain logic at the center and isolates infrastructure behind explicit ports and adapters. Aliases: `HexagonalArchitecture`, `Hexagonal`, `PortsAndAdapters`.
- `layered-architecture` — Layered Architecture: organizes responsibilities into explicit layers with clear direction of dependency flow. Aliases: `LayeredArchitecture`, `Layered`.
- `clean-architecture` — Clean Architecture: keeps domain and application rules independent from infrastructure and delivery details. Aliases: `CleanArchitecture`, `Clean`.
- `onion-architecture` — Onion Architecture: organizes the app in concentric rings so dependencies point inward toward the core domain model. Aliases: `OnionArchitecture`, `Onion`.
- `strangler-fig` — Strangler Fig: routes traffic incrementally between legacy and new Cephalon boundaries so modernization can happen without one cutover. Aliases: `StranglerFig`, `Strangler`.
- `backend-for-frontend` — Backend for Frontend: shapes one backend surface per client experience so transport, payload, and policy choices can stay explicit per frontend. Aliases: `BackendForFrontend`, `BFF`.

#### Domain

- `domain-driven-design` — Domain-Driven Design: centers the model on domain language, aggregates, invariants, and explicit bounded contexts. Aliases: `DomainDrivenDesign`, `DDD`.
- `anti-corruption-layer` — Anti-Corruption Layer: protects the core domain model by translating across external, legacy, or upstream integration boundaries. Aliases: `AntiCorruptionLayer`, `ACL`.

#### Data

- `cqrs` — CQRS: separates command-side and query-side responsibilities so write and read concerns can evolve independently. Aliases: `Cqrs`, `CQRS`, `CqrsPattern`.
- `outbox` — Outbox: coordinates persistence and message publication through a durable handoff that can be replayed safely. Aliases: `Outbox`, `OutboxPattern`.
- `event-sourcing` — Event Sourcing: represents state changes as an ordered stream of domain events instead of only storing current state. Aliases: `EventSourcing`, `EventSourcingPattern`.

## Technologies

Technologies are additive workload profiles that prepare the runtime for specific capability families.

### Technology kinds (7)

- `Intelligence` (0) — AI, LLM, agents
- `Messaging` (1) — events, async, broker
- `Data` (2) — storage, retrieval, knowledge
- `Experience` (3) — UX, realtime, collaboration
- `Deployment` (4) — hosting, edge, serverless
- `Security` (5) — identity, authorization, policies
- `Platform` (6) — runtime, tenancy, mesh

### Built-in technology descriptors (11)

- `agentic-workloads` — Agentic Workloads (Intelligence): prepares the app model for assistants, tool-using agents, and long-running autonomous workflows. Aliases: `AgenticWorkloads`, `Agentic`. Package hint: `Cephalon.Agentics`.
- `cell-based-architecture` — Cell-Based Architecture (Platform): prepares the app for explicit module-owned cell boundaries, governed cell routes, cell health-isolation posture, and configuration-driven traffic-automation posture. Aliases: `CellBasedArchitecture`, `CellBased`, `Cells`.
- `event-driven-integration` — Event-Driven Integration (Messaging): prepares modules for broker-backed events, asynchronous workflows, and eventually consistent integration flows. Aliases: `EventDrivenIntegration`, `EventDriven`. Package hint: `Cephalon.Eventing`.
- `knowledge-retrieval` — Knowledge Retrieval (Data): prepares the app for semantic retrieval, indexing, search, and knowledge-backed experiences. Aliases: `KnowledgeRetrieval`, `Knowledge`. Package hint: `Cephalon.Retrieval`.
- `realtime-experience` — Realtime Experience (Experience): prepares the app for live collaboration, presence, streaming updates, and reactive client experiences. Aliases: `RealtimeExperience`, `Realtime`.
- `edge-native-delivery` — Edge-Native Delivery (Deployment): prepares the app for browser, device, edge, and intermittently connected deployment scenarios. Aliases: `EdgeNativeDelivery`, `EdgeNative`, `Edge`. Package hint: `Cephalon.Edge`.
- `serverless-hosting` — Serverless Hosting (Deployment): prepares the app for event-triggered or function-style hosting without changing the host-agnostic core runtime model. Aliases: `ServerlessHosting`, `Serverless`.
- `identity-access` — Identity Access (Security): prepares the app for configurable authentication and authorization flows such as RBAC, ABAC, and policy evaluation. Aliases: `IdentityAccess`, `Identity`. Package hint: `Cephalon.Identity`.
- `multi-tenancy` — Multi-Tenancy (Platform): prepares the app for tenant-aware routing, isolation, membership, and runtime answers. Aliases: `MultiTenancy`, `Multitenancy`. Package hint: `Cephalon.MultiTenancy`.
- `hybrid-cloud-runtime` — Hybrid Cloud Runtime (Platform): prepares the app for mixed on-premises, edge, and cloud deployment handoffs without changing the engine core. Aliases: `HybridCloudRuntime`, `HybridCloud`.
- `service-mesh-integration` — Service Mesh Integration (Platform): prepares the app for additive service-mesh coordination, policy handoff, and traffic-governance guidance. Aliases: `ServiceMeshIntegration`, `ServiceMesh`.

## Transports

Transports define the communication mechanism between client and server.

### Transport feature flags

- `RequestResponse` (1) — request/response interactions
- `ServerStreaming` (2) — server-streaming supported
- `ClientStreaming` (4) — client-streaming supported
- `DuplexStreaming` (8) — duplex streaming supported

### Built-in transport descriptors (6)

- `rest-api` — REST API: resource-oriented HTTP endpoints for request/response application integration. Features: `RequestResponse`. Aliases: `RestApi`, `Rest`, `HttpApi`. Adapter: `Cephalon.AspNetCore` (built-in).
- `json-rpc` — JSON-RPC: procedure-style JSON messaging over a transport chosen by the host. Features: `RequestResponse`. Adapter: `Cephalon.AspNetCore.JsonRpc`.
- `grpc` — gRPC: high-performance contract-first RPC with unary and streaming support. Features: `RequestResponse`, `ServerStreaming`, `ClientStreaming`, `DuplexStreaming`. Adapter: `Cephalon.AspNetCore.Grpc`.
- `graphql` — GraphQL: schema-based queries, mutations, and subscriptions over HTTP and WebSocket. Features: `RequestResponse`, `DuplexStreaming`. Adapter: `Cephalon.AspNetCore.GraphQL`.
- `server-sent-events` — Server-Sent Events: HTTP-based server push for one-way event streaming from server to client. Features: `ServerStreaming`. Aliases: `ServerSentEvents`, `Sse`. Adapter: `Cephalon.AspNetCore` (built-in).
- `websocket` — WebSocket: persistent bidirectional messaging between client and server. Features: `DuplexStreaming`. Aliases: `WebSocket`, `WebSockets`. Adapter: `Cephalon.AspNetCore` (built-in).

### Messaging bindings (3)

- `InMemoryTransportBinding` — in-process message dispatch for testing and single-host topologies
- `RabbitMqTransportBinding` — AMQP-based durable messaging with dead-letter and competing-consumer support
- `KafkaTransportBinding` — log-based durable messaging with partition-aware consumer groups

## Execution strategies

Execution strategies define how behaviors execute under a given pattern.

### Built-in strategies (7)

- `direct` — Direct: returns HTTP 200 when output is non-null, HTTP 204 when null. Synchronous request/response.
- `cqrs` — CQRS: null output means command (HTTP 202 Accepted), non-null output means query (HTTP 200 OK). Read/write separation at the behavior level.
- `event-driven` — Event-Driven: returns HTTP 202 Accepted immediately. Behavior runs in the background as fire-and-forget. Background faults are caught and logged.
- `saga-step` — Saga Step: loads saga state before invocation and persists it after successful execution. On exception, state is not saved so compensation logic can be applied. Saga identifier read from `IBehaviorContext.CorrelationId`.
- `saga-choreography` — Saga Choreography: invokes one choreography step, stages returned `SagaChoreographyPublication` entries through `ISagaChoreographyPublisher`, and returns HTTP 202 when publications were accepted.
- `process-manager` — Process Manager: loads process checkpoint before invocation, saves after successful step, deletes when process signals completion via `IProcessCompletion`. Process identifier read from `IBehaviorContext.CorrelationId`.
- `durable-execution` — Durable Execution: replays state from `IEventStore`, validates sequential stream-version output from `IDurableExecution<TInput, TState, TOutput>`, appends continuation events, can surface pending timers/signals and operator-facing compensation actions through the shared durable runtime state, returns HTTP 200 with local output, HTTP 202 when continuation work or timer/signal coordination remains without local output, and HTTP 204 when the step completed without output.

## Modules

Modules are the primary composition unit. Each module registers services, capabilities, and lifecycle hooks.

### Core foundation modules (6)

- `behaviors` — Behaviors (`Cephalon.Behaviors`): ABT foundation with behavior topology, dispatch, and compatibility matrix.
- `data-runtime` — Data Runtime (`Cephalon.Data`): runtime-neutral command and query dispatching for data workloads.
- `event-sourcing` — Event Sourcing (`Cephalon.EventSourcing`): runtime-neutral event-sourcing baseline.
- `audit` — Audit (`Cephalon.Audit`): host-agnostic audit recording baseline.
- `identity-access` — Identity Access (`Cephalon.Identity`): host-agnostic identity and authorization baseline.
- `multi-tenancy` — Multi-Tenancy (`Cephalon.MultiTenancy`): host-agnostic tenant resolution and ambient tenant-context baseline.

### Data provider modules (12)

- `entity-framework-data` — Entity Framework Data (`Cephalon.Data.EntityFramework`): relational DbContext registration. Store type: relational.
- `mongodb-data` — MongoDB Data (`Cephalon.Data.MongoDB`): document store registration plus provider-native MongoDB change-stream CDC runtime. Store type: document.
- `sqlserver-data` — SQL Server Data (`Cephalon.Data.SqlServer`): relational provider-native SQL Server CDC runtime. Store type: relational.
- `postgres-data` — PostgreSQL Data (`Cephalon.Data.Postgres`): relational provider-native logical-replication CDC runtime. Store type: relational.
- `redis-data` — Redis Data (`Cephalon.Data.Redis`): key-value store registration. Store type: key-value.
- `neo4j-data` — Neo4j Data (`Cephalon.Data.Neo4j`): graph store registration. Store type: graph.
- `cassandra-data` — Cassandra Data (`Cephalon.Data.Cassandra`): wide-column store registration. Store type: wide-column.
- `clickhouse-data` — ClickHouse Data (`Cephalon.Data.ClickHouse`): analytics store registration. Store type: analytics.
- `elasticsearch-data` — Elasticsearch Data (`Cephalon.Data.Elasticsearch`): search store registration. Store type: search.
- `opensearch-data` — OpenSearch Data (`Cephalon.Data.OpenSearch`): search store registration. Store type: search.
- `qdrant-data` — Qdrant Data (`Cephalon.Data.Qdrant`): vector store registration. Store type: vector.
- `nats-data` — NATS Data (`Cephalon.Data.Nats`): ledger store registration. Store type: ledger.

### Event sourcing provider modules (10)

- `entity-framework-event-sourcing` — Entity Framework Event Sourcing (`Cephalon.EventSourcing.EntityFramework`)
- `Cephalon.EventSourcing.MongoDB`
- `Cephalon.EventSourcing.Redis`
- `Cephalon.EventSourcing.Neo4j`
- `Cephalon.EventSourcing.Cassandra`
- `Cephalon.EventSourcing.ClickHouse`
- `Cephalon.EventSourcing.Elasticsearch`
- `Cephalon.EventSourcing.OpenSearch`
- `Cephalon.EventSourcing.Qdrant`
- `Cephalon.EventSourcing.Nats`

### Specialized runtime modules (6)

- `eventing-runtime` — Eventing Runtime (`Cephalon.Eventing`): companion runtime services for event-driven integration workloads.
- `eventing-behaviors` — Eventing Behaviors (`Cephalon.Eventing.Behaviors`): explicit saga choreography bridge into the shared eventing publish path.
- `wolverine-eventing` — Wolverine Eventing (`Cephalon.Eventing.Wolverine`): official Wolverine host wiring for event-driven integration workloads.
- `agentics-runtime` — Agentics Runtime (`Cephalon.Agentics`): companion runtime services for agentic workloads.
- `edge-runtime` — Edge Runtime (`Cephalon.Edge`): companion runtime services for edge-native delivery workloads.
- `knowledge-retrieval-runtime` — Knowledge Retrieval Runtime (`Cephalon.Retrieval`): companion runtime services for retrieval-heavy applications.

### Identifier strategy modules (1)

- `sfid-id-strategy` — Sfid Id Strategy (`Cephalon.Ids.Sfid`): official Sfid.Net-backed identifier generation.

## Capabilities

Capabilities are the fine-grained feature advertisements exposed by modules.

### Behavior capabilities (7)

- `behaviors.cqrs` — CQRS Behaviors
- `behaviors.event-driven` — Event-Driven Behaviors
- `behaviors.saga` — Saga Behaviors
- `behaviors.saga-choreography` — Saga Choreography Behaviors
- `behaviors.process-manager` — Process Manager Behaviors
- `behaviors.durable-execution` — Durable Execution Behaviors
- `behaviors.direct` — Direct Behaviors

### Data core capabilities (2)

- `data.read` — Data Read Store
- `data.write` — Data Write Store

### Entity Framework capabilities (6)

- `data.entity-framework` — Entity Framework Data Provider
- `data.dbcontext.read` — Read DbContext
- `data.dbcontext.write` — Write DbContext
- `data.outbox` — Entity Framework Outbox
- `data.inbox` — Entity Framework Inbox
- `data.projections.entity-framework` — Entity Framework Projections

### MongoDB capabilities (5)

- `data.mongodb` — MongoDB Data Provider
- `data.document-store` — Document Store
- `data.outbox.mongodb` — MongoDB Outbox
- `data.inbox.mongodb` — MongoDB Inbox
- `data.cdc.mongodb` — MongoDB Change Stream CDC

### SQL Server capabilities (3)

- `data.sqlserver` — SQL Server Data Provider
- `data.relational-store` — Relational Store
- `data.cdc.sqlserver` — SQL Server CDC

### PostgreSQL capabilities (3)

- `data.postgresql` — PostgreSQL Data Provider
- `data.relational-store` — Relational Store
- `data.cdc.postgresql` — PostgreSQL Logical Replication CDC

### Redis capabilities (4)

- `data.redis` — Redis Data Provider
- `data.key-value-store` — Key-Value Store
- `data.outbox.redis` — Redis Outbox
- `data.inbox.redis` — Redis Inbox

### Neo4j capabilities (4)

- `data.neo4j` — Neo4j Data Provider
- `data.graph-store` — Graph Store
- `data.outbox.neo4j` — Neo4j Outbox
- `data.inbox.neo4j` — Neo4j Inbox

### Cassandra capabilities (4)

- `data.cassandra` — Cassandra Data Provider
- `data.wide-column-store` — Wide-Column Store
- `data.outbox.cassandra` — Cassandra Outbox
- `data.inbox.cassandra` — Cassandra Inbox

### ClickHouse capabilities (4)

- `data.clickhouse` — ClickHouse Data Provider
- `data.analytics-store` — Analytics Store
- `data.outbox.clickhouse` — ClickHouse Outbox
- `data.inbox.clickhouse` — ClickHouse Inbox

### Elasticsearch capabilities (4)

- `data.elasticsearch` — Elasticsearch Data Provider
- `data.search-store` — Search Store
- `data.outbox.elasticsearch` — Elasticsearch Outbox
- `data.inbox.elasticsearch` — Elasticsearch Inbox

### OpenSearch capabilities (4)

- `data.opensearch` — OpenSearch Data Provider
- `data.search-store` — Search Store
- `data.outbox.opensearch` — OpenSearch Outbox
- `data.inbox.opensearch` — OpenSearch Inbox

### Qdrant capabilities (4)

- `data.qdrant` — Qdrant Data Provider
- `data.vector-store` — Vector Store
- `data.outbox.qdrant` — Qdrant Outbox
- `data.inbox.qdrant` — Qdrant Inbox

### NATS capabilities (4)

- `data.nats` — NATS Data Provider
- `data.ledger-store` — Ledger Store
- `data.outbox.nats` — NATS Outbox
- `data.inbox.nats` — NATS Inbox

### Eventing capabilities (6)

- `eventing.publish` — Event Publishing
- `eventing.subscriptions` — Event Subscription Descriptors
- `eventing.channels` — Event Channels
- `eventing.behaviors.saga-choreography` — Saga Choreography Eventing Bridge
- `eventing.wolverine` — Wolverine Eventing Adapter
- `eventing.wolverine.dispatch` — Wolverine Dispatch Loop

### Identity capabilities (1)

- `identity.authorization` — Identity Authorization

### Multi-tenancy capabilities (1)

- `tenancy.resolution` — Tenant Resolution

### Agentics capabilities (4)

- `agentics.runtime` — Agentic Runtime
- `agentics.execution` — Agent Execution
- `agentics.memory` — Agent Memory
- `agentics.tools` — Agent Tools

### Edge capabilities (3)

- `edge.offline` — Offline Mode
- `edge.sync` — Edge Synchronization
- `edge.nodes` — Edge Nodes

### Knowledge retrieval capabilities (3)

- `retrieval.query` — Retrieval Query
- `retrieval.ingestion` — Retrieval Ingestion
- `retrieval.collections` — Knowledge Collections

## Data abstractions

Host-agnostic contracts defined in `Cephalon.Abstractions` for data workloads.

### Command and query (CQRS)

- `ICommand` — marker interface for write-side commands
- `ICommand<TResult>` — typed command with result
- `ICommandHandler<TCommand>` — handler for untyped command
- `ICommandHandler<TCommand, TResult>` — handler for command with result
- `IQuery<TResult>` — typed query with result
- `IQueryHandler<TQuery, TResult>` — handler for query
- `IReadStore` — query handler dispatch interface
- `IWriteStore` — command handler dispatch interface

### Outbox and inbox

- `IOutbox` — outbound message staging interface
- `IOutboxRegistry` / `IOutboxCatalog` / `IOutboxContributor` — registration and discovery
- `OutboxDescriptor` — metadata for outbox implementations
- `OutboxMessage` — durable message format
- `IInbox` — inbound message tracking interface
- `IInboxRegistry` / `IInboxCatalog` / `IInboxContributor` — registration and discovery
- `InboxDescriptor` — metadata for inbox implementations
- `InboxMessage` — tracked message format

### Projections

- `IProjection<TMessage>` — read-model building interface
- `IProjectionRegistry` / `IProjectionCatalog` / `IProjectionContributor` — registration and discovery
- `ProjectionDescriptor` — metadata for projection implementations

### Data products

- `IDataProduct<T>` — module-owned queryable data-product interface
- `IDataProductRegistry` / `IDataProductCatalog` / `IDataProductContributor` — registration and discovery
- `DataProductDescriptor` — metadata for data-product implementations

### CDC captures

- `ICdcCapture` — host-agnostic CDC execution interface for provider-specific implementations
- `ICdcCaptureRegistry` / `ICdcCaptureCatalog` / `ICdcCaptureContributor` — registration and discovery
- `CdcCaptureDescriptor` — metadata for CDC capture implementations and outbox linkage
- `CdcCaptureFreshnessStatus` / `CdcCaptureLagStatus` / `CdcCapturePublicationStatus` — typed freshness, lag, and publication posture for operator/runtime reporting
- `CdcCaptureRuntimeState` / `ICdcCaptureRuntimeStateCatalog` — latest capture outcome, totals, typed operational posture, and optional linked outbox dispatch truth

### Event sourcing

- `IEventStore` — event stream persistence interface
- `IEventStoreCatalog` / `IEventStoreRegistry` / `IEventStoreContributor` — registration and discovery
- `ISnapshotStore` — snapshot persistence interface
- `IDomainEvent` — marker for domain event types
- `IAggregate` — aggregate root interface
- `DomainEvent` — base class for events
- `EventStreamDescriptor` — metadata for event streams
- `EventStreamConcurrencyException` — optimistic concurrency handling

### Identity and authorization

- `IAuthorizationEvaluator` — policy evaluation interface
- `IAuthorizationPolicyCatalog` / `IAuthorizationPolicyRegistry` — policy discovery
- `IAuthorizationPolicyContributor` — module-contributed authorization policies

### Multi-tenancy

- `ITenantContextAccessor` — ambient tenant context
- `ITenantResolver` — tenant resolution interface

### Audit

- `IAuditRecorder` — audit entry recording
- `IAuditWriter` — audit persistence interface
- `IAuditStoreCatalog` — audit store discovery

### Identifier generation

- `IIdGenerator` — provider-agnostic identifier generation

## Diagnostics conventions

Structured diagnostics sources with stable event ID ranges.

- Engine Core (`Cephalon.Engine`) — event IDs 1000+
- ASP.NET Core Hosting (`Cephalon.AspNetCore`) — event IDs 2000+
- Observability (`Cephalon.Observability`) — event IDs 3000+
- Eventing (`Cephalon.Eventing`) — event IDs 4000–4099
- Wolverine Eventing (`Cephalon.Eventing.Wolverine`) — event IDs 4300–4305
- Identity (`Cephalon.Identity`) — event IDs 4400–4401
- Multi-Tenancy (`Cephalon.MultiTenancy`) — event IDs 4500–4502
- Audit (`Cephalon.Audit`) — event IDs 4600–4601
- Behaviors (`Cephalon.Behaviors`) — event IDs 5100–5109

## Observability dependency health probes (18)

Each probe is a standalone companion package implementing `IDependencyHealthContributor`.

- `Cephalon.Observability.CassandraDependencies` — Cassandra contact-point and keyspace probes
- `Cephalon.Observability.ClickHouseDependencies` — ClickHouse connection and health query probes
- `Cephalon.Observability.ConsulDependencies` — Consul leader checks, ACL tokens, datacenter probes
- `Cephalon.Observability.ElasticsearchDependencies` — Elasticsearch cluster-health probes
- `Cephalon.Observability.HttpDependencies` — external HTTP upstream probes
- `Cephalon.Observability.KafkaDependencies` — Kafka broker metadata and topic probes
- `Cephalon.Observability.MemcachedDependencies` — Memcached version probes
- `Cephalon.Observability.MongoDbDependencies` — MongoDB connection and TLS probes
- `Cephalon.Observability.MqttDependencies` — MQTT CONNECT/PINGREQ probes
- `Cephalon.Observability.MySqlDependencies` — MySQL connection and health query probes
- `Cephalon.Observability.NatsDependencies` — NATS INFO/CONNECT/PING probes
- `Cephalon.Observability.Neo4jDependencies` — Neo4j endpoint and Cypher health probes
- `Cephalon.Observability.OpenSearchDependencies` — OpenSearch cluster-health probes
- `Cephalon.Observability.OracleDependencies` — Oracle connection and `SELECT 1 FROM DUAL` probes
- `Cephalon.Observability.PostgresDependencies` — Postgres connection and health query probes
- `Cephalon.Observability.RabbitMqDependencies` — RabbitMQ broker connection probes
- `Cephalon.Observability.RedisDependencies` — Redis PING and authentication probes
- `Cephalon.Observability.SqlServerDependencies` — SQL Server and Azure SQL connection probes

## Observability cloud and platform exporters (14)

Each exporter is a standalone companion package on top of the cloud-neutral OTLP baseline.

- `Cephalon.Observability.OpenTelemetry` — cloud-neutral OTLP exporter (logs, metrics, traces)
- `Cephalon.Observability.Aws` — AWS X-Ray traces, CloudWatch, SDK instrumentation
- `Cephalon.Observability.AzureMonitor` — Azure Monitor / Application Insights
- `Cephalon.Observability.Gcp` — Google Cloud Trace and Monitoring
- `Cephalon.Observability.AlibabaCloud` — Alibaba Cloud ARMS/SLS
- `Cephalon.Observability.HuaweiCloud` — Huawei Cloud APM
- `Cephalon.Observability.OracleCloud` — Oracle Cloud APM traces/metrics
- `Cephalon.Observability.DigitalOcean` — DigitalOcean collector defaults
- `Cephalon.Observability.GrafanaCloud` — Grafana Cloud OTLP endpoint with access-policy auth
- `Cephalon.Observability.NewRelic` — New Relic native OTLP with api-key auth
- `Cephalon.Observability.Serilog` — Serilog sink/enricher provider integration
- `Cephalon.Observability.Kubernetes` — in-cluster collector wiring and resource defaults
- `Cephalon.Observability.OpenShift` — Red Hat OpenShift platform defaults
- `Cephalon.Observability.Tanzu` — VMware Tanzu/Wavefront proxy defaults

## Host adapters

- `Cephalon.AspNetCore` — HTTP-first host core with engine metadata, health, transport, and docs endpoints
- `Cephalon.AspNetCore.JsonRpc` — JSON-RPC transport adapter
- `Cephalon.AspNetCore.GraphQL` — GraphQL transport adapter
- `Cephalon.AspNetCore.Grpc` — gRPC transport adapter with unary and streaming contracts
- `Cephalon.Identity.AspNetCore` — ASP.NET Core identity/authorization host adapter
- `Cephalon.Worker` — non-HTTP generic-host adapter for worker processes

## Tooling and adoption

- `Cephalon.Cli` — user-facing CLI for scaffolding, docs, and package workflows
- `Cephalon.Scaffolding` — scaffold generation from blueprint and app-profile contracts
- `Cephalon.TemplatePack` — installable `dotnet new` templates for shipped blueprints
- `Cephalon.ReferenceDocs` — reference-doc generation from compiled assemblies

## Sample applications

- `Cephalon.Sample.ModularMonolith` — modular-monolith blueprint sample
- `Cephalon.Sample.ModularVerticalSlice` — modular-vertical-slice blueprint sample
- `Cephalon.Sample.Microservice` — microservice blueprint sample
- `Cephalon.Sample.MicroserviceSuite` — microservice-suite sample with CatalogService, OrdersService, shared governance and foundation
- `Cephalon.ReferenceModule.Operations` — reference module authoring example

## Runtime introspection surfaces

The engine exposes operator-facing runtime information through these endpoints:

- `/engine/snapshot` — merged runtime introspection snapshot
- `/engine/app-model` — app profile with blueprint, patterns, technologies, transports
- `/engine/manifest` — runtime manifest with modules, capabilities, metadata
- `/engine/runtime-story` — lifecycle story with state, events, timeline
- `/engine/cells` — active cell-boundary catalog
- `/engine/cell-routes` — active governed cell-route catalog
- `/engine/cell-health-isolations` — active cell health-isolation catalog
- `/engine/cell-traffic-automations` — effective cell traffic-automation catalog
- `/engine/technologies` — active technology selections
- `/engine/technology-catalog` — full technology catalog
- `/engine/technology-surfaces` — active technology runtime surfaces
- `/engine/packages` — loaded package metadata, trust, provenance
- `/engine/options` — engine options and capability policy
- `/engine/diagnostics` — diagnostics conventions and event-id catalog
- `/engine/dependencies` — dependency health contributions
- `/engine/localization` — localization state
- `/engine/scaffold` — blueprint scaffold plan
- `/engine/failure-policy` — startup/stop/restart behavior
- `/engine/trust-policy` — trust and capability policy
- `/engine/package-policy` — package governance requirements
- `/engine/execution-graphs` — execution graph descriptors
- `/engine/hosted-executions` — hosted execution descriptors
- `/engine/rest-endpoints` — final published REST endpoint catalog
- `/engine/rest-endpoint-candidates` — authoring and governance-aware REST candidate catalog
- `/engine/rest-endpoint-publication-groups` — grouped REST publication story per behavior and authoring style
- `/engine/rest-endpoint-authoring-policies` — grouped shorthand authoring-policy answers
- `/engine/rest-endpoint-suppressions` — active REST suppression rules with runtime outcomes
- `/engine/rest-endpoint-overrides` — active REST override rules with selected-versus-applied outcomes
- `/engine/data-products` — data product descriptors
- `/engine/cdc-captures` — CDC capture descriptors
- `/engine/projections` — projection descriptors
- `/engine/inboxes` — inbox surfaces
- `/engine/outboxes` — outbox surfaces
- `/engine/authorization-policies` — authorization policy descriptors
- `/engine/audit-stores` — audit store surfaces
- `/health` — health summary
- `/health/live` — liveness probe
- `/health/ready` — readiness probe

## Summary counts

- Blueprints: 3 app + 1 suite = **4**
- Patterns: **21**
- Technologies: **11**
- Transports: **6** + 3 messaging bindings = **9**
- Execution strategies: **7**
- Modules: 6 core + 10 data + 10 event-sourcing + 5 specialized + 1 identifier = **32**
- Capabilities: **71+**
- Data abstractions: **24+** interfaces
- Diagnostics sources: **9**
- Dependency health probes: **18**
- Cloud/platform exporters: **14**
- Host adapters: **6**
- Runtime introspection endpoints: **33+**
- Sample applications: **5**
