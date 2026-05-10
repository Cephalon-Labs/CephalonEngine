# Cephalon Architecture

Editable diagram: `docs/cephalon-architecture.drawio`
Planning docs: `docs/engine-roadmap.md`, `docs/engine-backlog.md`, `docs/app-models.md`
Architecture inventory: `docs/architecture-inventory.md`
Architecture recommendations: `docs/architecture-recommendations.md`
Current architecture review: `docs/architecture-review-2026-04.md`
Database direction: `docs/database-topology.md`
Docs hub: `docs/README.md`
Component docs: `docs/components/README.md`
Database topology: `docs/database-topology.md`

## Related research references

- [Architecture patterns research](architecture-patterns-research.md)
- [REST endpoint authoring strategy](architecture/rest-endpoint-authoring-strategy.md)
- [Design patterns reference](architecture/design-patterns-reference.md)
- [.NET ecosystem reference](dotnet-ecosystem-reference.md)
- [Database topology](database-topology.md)

## North star

Cephalon should evolve into an engine/framework, not a single app shell. That means the foundation needs to optimize for composition, discoverability, and multiple hosts from day one.

## Layers

### 1. Abstractions

`Cephalon.Abstractions` defines the stable contract modules implement:

- `IModule`
- `ModuleDescriptor`
- `Capability`
- `ICapabilityRegistry`

The goal is to keep module authors focused on capabilities and services rather than bootstrapping concerns.

Source structure:

- `AppModel` -> `Cephalon.Abstractions.AppModel`
- `AppModel/Scaffolding` -> `Cephalon.Abstractions.AppModel.Scaffolding`
- `Agentics` -> `Cephalon.Abstractions.Agentics`
- `Capabilities` -> `Cephalon.Abstractions.Capabilities`
- `Health` -> `Cephalon.Abstractions.Health`
- `Localization` -> `Cephalon.Abstractions.Localization`
- `Modules` -> `Cephalon.Abstractions.Modules`
- `Patterns` -> `Cephalon.Abstractions.Patterns`
- `Technologies` -> `Cephalon.Abstractions.Technologies`
- `Transports` -> `Cephalon.Abstractions.Transports`

### 2. Engine

`Cephalon.Engine` turns a set of modules into a runtime:

- validates duplicate registrations
- resolves dependency ordering
- discovers modules from assemblies when configured
- loads independently shipped module assemblies from explicit DLL paths, package manifests, or package directories when configured
- validates package compatibility metadata such as engine version and supported target frameworks
- carries package publisher and signature provenance metadata through runtime introspection
- verifies detached package signatures against trusted public keys when signing metadata and trust-store entries are both present
- validates optional package assembly integrity hashes and can trust packages by checksum allow-list
- models future-facing workload profiles as explicit technology descriptors instead of hardcoding every new tech stack into the blueprint set
- lets installed modules and packages contribute additional technology profiles through `ITechnologyContributor`
- lets installed modules adapt service and capability registration to active technology profiles through `ITechnologyServiceContributor` and `ITechnologyCapabilityContributor`
- lets modules and hosts declare explicit cell boundaries and governed cell routes through `ICellBoundaryContributor`, `ICellRouteContributor`, `ICellBoundaryCatalog`, `ICellRouteCatalog`, `engine.AddCellBoundary(...)`, and `engine.AddCellRoute(...)` without turning cells into a new blueprint
- lets modules and hosts declare outbox-linked CDC posture through `ICdcCaptureContributor`, `ICdcCaptureCatalog`, `ICdcCaptureRuntimeStateCatalog`, typed freshness/lag/publication status contracts, and `engine.AddCdcCapture(...)` without turning provider-specific change-stream execution into a host-only registry
- lets modules register services
- merges built-in and project-supplied language resources
- merges package-provided language packs from modules before project overrides
- collects capabilities into a manifest with source-module tracing, including aggregate metadata (`sourceModuleIds`, `providers`, `packs`, and `contributorCount`) when different provider modules expose the same shared capability family key
- executes module lifecycle hooks deterministically
- aggregates host-agnostic dependency health contributions
- exposes a runtime object that any host can consume

This is the piece we can later extend with plugin loading, workflows, orchestration, telemetry, and policy.

Source structure:

- `AppModel` -> `Cephalon.Engine.AppModel`
- `AppModel/Scaffolding` -> `Cephalon.Engine.AppModel.Scaffolding`
- `Composition` -> `Cephalon.Engine.Composition`
- `Composition/Packages` -> `Cephalon.Engine.Composition.Packages`
- `Configuration` -> `Cephalon.Engine.Configuration`
- `Diagnostics` -> `Cephalon.Engine.Diagnostics`
- `Localization` -> `Cephalon.Engine.Localization`
- `Manifest` -> `Cephalon.Engine.Manifest`
- `Patterns` -> `Cephalon.Engine.Patterns`
- `Runtime` -> `Cephalon.Engine.Runtime`
- `Technologies` -> `Cephalon.Engine.Technologies`
- `Trust` -> `Cephalon.Engine.Trust`
- `Transports` -> `Cephalon.Engine.Transports`

### 3. Host adapters

`Cephalon.AspNetCore` is the HTTP-first host core. It wires the engine into modern ASP.NET Core patterns and exposes:

- engine metadata endpoints
- engine package endpoint
- engine technology endpoint
- engine technology catalog endpoint
- blueprint scaffold endpoint
- engine options and policy endpoint
- engine failure-policy endpoint
- engine diagnostics endpoint
- engine localization endpoint
- engine dependency-health endpoint
- engine trust-policy endpoint
- runtime status endpoint
- health summary, liveness, and readiness endpoints
- OpenAPI and Scalar API docs for REST surfaces
- optional hosted reference-doc surfaces for generated API documentation
- built-in transport-aware module surfaces such as `/api`, `/sse`, and `/ws`

Companion adapter packages can extend that host with additional transport surfaces such as the
GraphQL prefix family (`/graphql`, `/graphql/schema`, `/graphql-sse`, `/graphql-ws`) for GraphQL,
`/json-rpc` for JSON-RPC, or `/grpc` for gRPC bindings with unary and streaming contracts.

`Cephalon.Worker` is the non-HTTP generic-host adapter. It starts and stops the same runtime inside worker processes and keeps module/background behavior aligned with standard hosted-service lifecycles.

`Cephalon.Observability` is the diagnostics companion package. It turns the engine's built-in logs, meter, and activity source into host-friendly startup summaries and conventions that both ASP.NET Core and worker hosts can opt into, and it publishes the active package-level event-id catalog through the runtime diagnostics surface.

`Cephalon.Observability.CassandraDependencies` is the optional Cassandra dependency-health companion package. It turns configured contact-point lists, optional keyspace selection, credentials, and CQL health queries into `IDependencyHealthContributor` data without pushing Cassandra-driver specifics into `Cephalon.Engine`.

`Cephalon.Observability.ClickHouseDependencies` is the optional ClickHouse dependency-health companion package. It turns configured ClickHouse connection strings or discrete host/protocol/database settings and health queries into `IDependencyHealthContributor` data without pushing ClickHouse-driver specifics into `Cephalon.Engine`.

`Cephalon.Observability.ConsulDependencies` is the optional Consul dependency-health companion package. It turns configured Consul leader checks, ACL tokens, and datacenter selection into `IDependencyHealthContributor` data without pushing Consul-specific control-plane behavior into `Cephalon.Engine`.

`Cephalon.Observability.ElasticsearchDependencies` is the optional Elasticsearch dependency-health companion package. It turns configured cluster-health requests, auth policies, and Elasticsearch `green`/`yellow`/`red` status mapping into `IDependencyHealthContributor` data without pushing Elasticsearch-specific HTTP behavior into `Cephalon.Engine`.

`Cephalon.Observability.HttpDependencies` is the optional external API dependency-health companion package. It turns configured HTTP upstream probes into `IDependencyHealthContributor` data without pushing provider-specific network checks into `Cephalon.Engine`. That package should stay protocol-generic: method, headers, auth, timeout, status, body, and TLS expectations belong there, but product-aware HTTP semantics should stay in dedicated packs when they need endpoint shaping or payload-aware mapping.

`Cephalon.Observability.KafkaDependencies` is the optional Kafka dependency-health companion package. It turns configured broker metadata and optional topic probes into `IDependencyHealthContributor` data without pushing Kafka client configuration into `Cephalon.Engine`.

`Cephalon.Observability.MemcachedDependencies` is the optional Memcached dependency-health companion package. It turns configured cache `version` probes into `IDependencyHealthContributor` data without pushing Memcached-specific socket logic into `Cephalon.Engine`.

`Cephalon.Observability.MongoDbDependencies` is the optional MongoDB dependency-health companion package. It turns configured database commands, TLS policy, and connection-string probing into `IDependencyHealthContributor` data without pushing MongoDB-specific connection logic into `Cephalon.Engine`.

`Cephalon.Observability.MqttDependencies` is the optional MQTT dependency-health companion package. It turns configured broker `CONNECT`, `CONNACK`, and `PINGREQ`/`PINGRESP` probes into `IDependencyHealthContributor` data without pushing MQTT protocol handling into `Cephalon.Engine`.

`Cephalon.Observability.MySqlDependencies` is the optional MySQL dependency-health companion package. It turns configured SQL queries, SSL/public-key retrieval policy, and MySQL connection probing into `IDependencyHealthContributor` data without pushing MySQL-specific connection logic into `Cephalon.Engine`.

`Cephalon.Observability.NatsDependencies` is the optional NATS dependency-health companion package. It turns configured broker `INFO`, `CONNECT`, and `PING`/`PONG` probes into `IDependencyHealthContributor` data without pushing NATS protocol handling into `Cephalon.Engine`.

`Cephalon.Observability.Neo4jDependencies` is the optional Neo4j dependency-health companion package. It turns configured Neo4j endpoint URIs, discrete host/port/scheme settings, database selection, and Cypher health queries into `IDependencyHealthContributor` data without pushing graph-driver specifics into `Cephalon.Engine`.

`Cephalon.Observability.OpenSearchDependencies` is the optional OpenSearch dependency-health companion package. It turns configured OpenSearch base URLs, optional index-specific cluster-health checks, and secured REST auth headers into `IDependencyHealthContributor` data without pushing search-cluster transport semantics into `Cephalon.Engine`.

`Cephalon.Observability.OracleDependencies` is the optional Oracle dependency-health companion package. It turns configured `SELECT 1 FROM DUAL`-style queries, Easy Connect data-source shaping, and connection-string probing into `IDependencyHealthContributor` data without pushing Oracle-specific connection logic into `Cephalon.Engine`.

`Cephalon.Observability.PostgresDependencies` is the optional Postgres dependency-health companion package. It turns configured database probes and health queries into `IDependencyHealthContributor` data without pushing Postgres-specific connection logic into `Cephalon.Engine`.

`Cephalon.Observability.RabbitMqDependencies` is the optional RabbitMQ dependency-health companion package. It turns configured broker connection probes into `IDependencyHealthContributor` data without pushing AMQP-specific connection logic into `Cephalon.Engine`.

`Cephalon.Observability.RedisDependencies` is the optional Redis and cache dependency-health companion package. It turns configured Redis `PING` probes, optional authentication, and logical database selection into `IDependencyHealthContributor` data without pushing Redis-specific socket logic into `Cephalon.Engine`.

`Cephalon.Observability.SqlServerDependencies` is the optional SQL Server and Azure SQL dependency-health companion package. It turns configured SQL queries, SQL authentication, and connection-string probing into `IDependencyHealthContributor` data without pushing SQL Server-specific connection logic into `Cephalon.Engine`.

`Cephalon.Observability.OpenTelemetry` is the optional exporter companion package. It takes the shared `Engine:Observability:Telemetry` contract and turns it into OTLP registration for logs, metrics, and traces without pushing exporter dependencies back into the engine or the baseline observability package.

`Cephalon.Observability.Aws` is the optional AWS observability companion package. It takes the shared `Engine:Observability:Telemetry` contract and turns it into OTLP registration plus AWS X-Ray-compatible trace IDs, AWS SDK instrumentation, and hosted AWS resource defaults without pushing vendor assumptions back into the engine or the baseline observability package.

`Cephalon.Observability.AzureMonitor` is the optional Azure Monitor exporter companion package. It takes the shared `Engine:Observability:Telemetry` contract and turns it into Azure Monitor / Application Insights registration plus hosted Azure resource defaults without pushing vendor assumptions back into the engine or the baseline observability package.

`Cephalon.Observability.Serilog` is the optional logger-provider companion package. It keeps Serilog-specific sink, enricher, and formatting registration outside `Cephalon.Engine` and `Cephalon.Observability` while still flowing engine and module logs through the shared `Microsoft.Extensions.Logging.ILogger` pipeline.

`Cephalon.Scaffolding` is the adoption companion package. It turns `AppProfile.Scaffold` into concrete solution, project, file, and folder output so future CLIs or templates do not need to re-encode blueprint rules. `Cephalon.Abstractions.AppModel` and `Cephalon.Abstractions.AppModel.Scaffolding` now also carry `SuiteBlueprint`, `SuiteScaffoldPlan`, and `SuiteScaffoldService`, while `Cephalon.Engine.AppModel` now ships a built-in `MicroserviceSuite` blueprint that composes its service slots from the existing `Microservice` blueprint instead of stretching the current single-app scaffold contract past its intended boundary.

`Cephalon.ReferenceDocs` is the optional reference-publishing companion package. It can turn compiled assemblies plus XML comments into publishable Markdown reference output when the repository wants a browsable API artifact, but the hand-authored `.md` guides under `README.md` and `docs/` remain the primary product and adoption documentation. The stable library surface stays centered on request/generate/write flows, while browser rendering and assembly-load plumbing remain internal implementation details.

`Cephalon.Cli` is the user-facing shell over scaffolding and documentation workflows. `CliApplication` is the stable entry point, while the individual command handlers and option objects remain internal implementation details. It parses blueprint, transport, pattern, module, and feature selections for app generation, and it also publishes, enables, validates, and opens generated reference-doc surfaces.

`Cephalon.Agentics`, `Cephalon.Eventing`, `Cephalon.Retrieval`, `Cephalon.MultiTenancy`, and `Cephalon.Edge` are the current baseline technology companion packages. They show the preferred pattern for future workloads that need reusable runtime services without bloating the engine core.

`Cephalon.Agentics` owns a narrow dispatcher/run-state lane for registered tools, including explicitly configured bounded process-local retry and duplicate-completed suppression, while autonomous planning, memory persistence, durable retry queues, durable inboxes, cross-node exactly-once delivery, distributed schedulers, and provider-specific AI orchestration stay outside the current proof. `Cephalon.MultiTenancy` keeps tenant resolution in the base pack while `Cephalon.MultiTenancy.Governance` owns concrete governance proofs for membership, invitations, delivery dispatch/retry/status reconciliation, delivery-status observation storage, tenant administration, domain-ownership verification, proof collection, polling, and governance actions. The optional ASP.NET Core, HTTP delivery, SMTP delivery, SendGrid delivery, Mailgun delivery, Amazon SES delivery, Microsoft Graph delivery, Microsoft Graph Azure Identity, SendGrid delivery ASP.NET Core, Mailgun delivery ASP.NET Core, and Amazon SES delivery ASP.NET Core companions keep routing, provider sender handoff, provider token acquisition, provider payload translation, optional SendGrid signed-webhook verification, bounded process-local signed-callback replay protection, observation-store-backed SendGrid event-id idempotency, optional Mailgun HMAC signed-webhook verification, bounded process-local Mailgun replay-token rejection, observation-store-backed Mailgun event-id idempotency, SNS-wrapped SES event translation, opt-in SNS signature verification, bounded process-local SNS replay protection, observation-store-backed Amazon SNS message-id idempotency, opt-in verified SNS subscription confirmation, and opt-in verified SNS unsubscribe-confirmation observation additive without moving notification-provider APIs into the governance core. Actual DNS proof publication, provider-backed proof mutation, distributed/provider-backed governance storage, Microsoft Entra app registration/consent/mailbox access policy, additional provider-specific email APIs beyond the shipped SMTP/SendGrid/Mailgun/Amazon SES/Microsoft Graph senders, SMS/chat/CRM/identity-provider senders, distributed retry queues, cross-node retry leases, identity-provider synchronization, provider-specific or distributed callback inboxes, distributed callback replay ledgers, distributed event-id ledgers, automatic resubscribe/restore, subscription lifecycle governance, callback translation beyond shipped SendGrid/Mailgun/Amazon SES translators, provider-specific callback signature semantics beyond shipped SendGrid/Mailgun/Amazon SNS hardening, provider polling, public onboarding, and tenant-admin UI/backoffice flows stay explicitly outside the current proof until a package owns them.

`Cephalon.Eventing` follows the same honesty rule: its direct in-process subscription lane can retry transient handler failures inside a bounded process-local attempt budget, can suppress duplicate completed subscription executions inside a bounded process-local retention window, can accept delayed publication requests through a bounded process-local scheduler, and can report latest publication runtime state for local direct execution, scheduled local handoff, or outbox `accepted` handoff. Event-dispatch remediation command idempotency and operator reads now flow through the provider-neutral `IEventDispatchRemediationCommandJournal`; the core pack provides a process-local fallback, while `Cephalon.Data.EntityFramework` can supply durable cross-node command history when the write-side `DbContext` opts into the journal context and outbox mapping. Broker-owned retries, durable retry queues, durable inbox ownership beyond explicit `IInbox` paths, durable or distributed scheduling, cross-node exactly-once delivery, downstream dispatch completion, and durable replay cursors stay outside the core pack until a provider or companion package truly owns them. `Cephalon.Eventing.Behaviors` now sits beside that baseline as an explicit bridge companion pack: it lets behavior-authored saga choreography hand off into the shared eventing publish path without making either baseline package absorb the other's contract ownership.

ENG-579 makes that command journal fail closed before mutation rather than only after a final result exists. The dispatcher now calls `ReserveAsync(...)` before applying a dispatch-store report, and the EF journal persists a `reserved` row that duplicate retries can observe across process/provider rebuilds until the final command result overwrites it. That keeps Wolverine optional while closing the in-doubt window between client retry uncertainty and dispatch-store mutation.

ENG-580 makes the same in-doubt state readable without a detail scan. `EventDispatchRemediationRuntimeSummary.ReservedCount` and `HasInDoubtCommands` now report retained `reserved` records in both process-local and durable journal lanes, while technology-surface metadata mirrors the posture for snapshot-driven operator UIs.

ENG-583 makes stale in-doubt detail reads provider-optimized: `IEventDispatchRemediationRuntimeCatalog.GetInDoubtBefore(...)` and `/engine/event-dispatch-remediation-commands/in-doubt?beforeUtc={beforeUtc}` return retained `reserved` records observed at or before an inclusive UTC cutoff, with invalid cutoff values rejected at the host boundary.

ENG-584 adds the single-record stale in-doubt read: `IEventDispatchRemediationRuntimeCatalog.GetOldestInDoubtBefore(...)` and `/engine/event-dispatch-remediation-commands/in-doubt/oldest?beforeUtc={beforeUtc}` return the oldest retained `reserved` command at or before the optional cutoff, return `404` when no retained row matches, and avoid list/pagination config because the query is intentionally direct.

ENG-585 adds the aggregate stale in-doubt read: `IEventDispatchRemediationRuntimeCatalog.GetInDoubtSummaryBefore(...)` and `/engine/event-dispatch-remediation-commands/in-doubt/summary?beforeUtc={beforeUtc}` return the retained `reserved` command summary at or before the optional cutoff, reject invalid cutoff values, and avoid list/pagination config because the query is intentionally a roll-up.

ENG-586 adds retained filter summaries for the same command journal. Outbox, message, channel, operation, actor, correlation, reason, outcome, and dispatch-outcome dashboards can call the matching `GetSummaryBy*` catalog method or `/engine/event-dispatch-remediation-commands/{filter}/{value}/summary` route and receive `EventDispatchRemediationRuntimeSummary` without materializing list rows, keeping the operator aggregate path provider-neutral and Wolverine-free.

`templates/Cephalon.TemplatePack` is the installable `dotnet new` companion surface. It packages the current shipped blueprint starters as lightweight templates for teams that want a fast entry point without using the repo-local CLI.

`Cephalon.MultiTenancy.Governance.AspNetCore` also maps `MapCephalonTenantInvitationDeliveryDispatches()` for fail-closed `POST /engine/tenant-invitations/delivery-dispatches` operator actions over `ITenantInvitationDeliveryDispatcher`, plus `MapCephalonTenantInvitationDeliveryStatusObservations()` for bounded/filterable `GET /engine/tenant-invitations/delivery-status/observations` reads, filtered status/attention/remediation/outcome/source/providerMessageId/channel/sender/tenant summaries, attention-category drill-downs, remediation-action filters, provider-message filters, and remediation hints over the host-agnostic delivery-status observation store. `Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore` maps `MapCephalonSendGridInvitationDeliveryStatusCallbacks()` for fail-closed SendGrid Event Webhook translation, optional SendGrid signed-webhook verification, bounded process-local signed-callback replay rejection, and observation-store-backed `sg_event_id` duplicate suppression into that same reconciler. `Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore` maps `MapCephalonMailgunInvitationDeliveryStatusCallbacks()` for fail-closed Mailgun webhook translation, optional Mailgun HMAC-SHA256 signed-webhook verification, bounded process-local token replay rejection, and observation-store-backed `event-data.id` duplicate suppression into that same reconciler. `Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore` maps `MapCephalonAmazonSesInvitationDeliveryStatusCallbacks()` for fail-closed SNS-wrapped Amazon SES event translation, opt-in SNS signature verification, bounded process-local SNS replay rejection, observation-store-backed SNS `MessageId` duplicate suppression, opt-in verified SNS subscription confirmation, and opt-in verified SNS unsubscribe-confirmation observation into that same reconciler. That keeps dispatch execution, provider payload translation, provider signature verification where implemented, local replay posture where implemented, event-id idempotency posture where implemented, SNS lifecycle posture where implemented, and operator/audit visibility in additive adapters while provider-specific callback inboxes, provider polling, distributed remediation execution, distributed replay ledgers, distributed event-id ledgers, callback translation beyond shipped SendGrid/Mailgun/Amazon SES translators, automatic resubscribe/restore, subscription lifecycle governance, and exactly-once delivery remain future package-owned work.

`docs/module-authoring.md` and `samples/Cephalon.ReferenceModule.*` are the module-authoring companions. They show how a reusable module package should register services, expose capabilities, participate in lifecycle, contribute localization, and optionally own a transport surface.

`Cephalon.Benchmarks` is the performance companion project. It measures composition, runtime lifecycle, and scaffolding paths through public engine APIs so regressions can be caught without relying on playground hosts.

`samples/` now contains both adoption-quality blueprint examples and reference module packages. The blueprint apps show project shape, while the reference packages show how to author distributable Cephalon modules cleanly.

The same runtime contract can later back messaging hosts, desktop shells, or AI orchestration hosts.

Per-component documentation for every `src/Cephalon.*` project now lives under `docs/components/`.

Source structure:

- `Hosting` -> `Cephalon.AspNetCore.Hosting`
- `Documentation` -> `Cephalon.AspNetCore.Documentation`
- `Diagnostics` -> `Cephalon.AspNetCore.Diagnostics`
- `Health` -> `Cephalon.AspNetCore.Health`
- `Modules` -> `Cephalon.AspNetCore.Modules`
- `Transformers` -> `Cephalon.AspNetCore.Transformers`
- `wwwroot/js` -> embedded Scalar/OpenAPI documentation assets owned by `Cephalon.AspNetCore`
- `wwwroot/icons` -> embedded docs branding assets owned by `Cephalon.AspNetCore`
- `Transports/Rest` -> `Cephalon.AspNetCore.Transports.Rest`
- `Transports/ServerSentEvents` -> `Cephalon.AspNetCore.Transports.ServerSentEvents`
- `Transports/WebSockets` -> `Cephalon.AspNetCore.Transports.WebSockets`

Companion adapter packages:

- `src/Cephalon.AspNetCore.JsonRpc/Hosting` -> `Cephalon.AspNetCore.JsonRpc.Hosting`
- `src/Cephalon.AspNetCore.JsonRpc/Modules` -> `Cephalon.AspNetCore.JsonRpc.Modules`
- `src/Cephalon.AspNetCore.JsonRpc/Routing` -> `Cephalon.AspNetCore.JsonRpc.Routing`
- `src/Cephalon.AspNetCore.GraphQL/Hosting` -> `Cephalon.AspNetCore.GraphQL.Hosting`
- `src/Cephalon.AspNetCore.GraphQL/Modules` -> `Cephalon.AspNetCore.GraphQL.Modules`
- `src/Cephalon.AspNetCore.GraphQL/Routing` -> `Cephalon.AspNetCore.GraphQL.Routing`
- `src/Cephalon.AspNetCore.Grpc/Hosting` -> `Cephalon.AspNetCore.Grpc.Hosting`
- `src/Cephalon.AspNetCore.Grpc/Modules` -> `Cephalon.AspNetCore.Grpc.Modules`
- `src/Cephalon.AspNetCore.Grpc/Protos` -> generated `Cephalon.AspNetCore.Grpc.Contracts.*`
- `src/Cephalon.AspNetCore.Grpc/Routing` -> `Cephalon.AspNetCore.Grpc.Routing`
- `src/Cephalon.Worker/Hosting` -> `Cephalon.Worker.Hosting`
- `src/Cephalon.Observability/Configuration` -> `Cephalon.Observability.Configuration`
- `src/Cephalon.Observability/Hosting` -> `Cephalon.Observability.Hosting`
- `src/Cephalon.Observability.CassandraDependencies/Configuration` -> `Cephalon.Observability.CassandraDependencies.Configuration`
- `src/Cephalon.Observability.CassandraDependencies/Hosting` -> `Cephalon.Observability.CassandraDependencies.Hosting`
- `src/Cephalon.Observability.CassandraDependencies/Services` -> `Cephalon.Observability.CassandraDependencies.Services`
- `src/Cephalon.Observability.ClickHouseDependencies/Configuration` -> `Cephalon.Observability.ClickHouseDependencies.Configuration`
- `src/Cephalon.Observability.ClickHouseDependencies/Hosting` -> `Cephalon.Observability.ClickHouseDependencies.Hosting`
- `src/Cephalon.Observability.ClickHouseDependencies/Services` -> `Cephalon.Observability.ClickHouseDependencies.Services`
- `src/Cephalon.Observability.ConsulDependencies/Configuration` -> `Cephalon.Observability.ConsulDependencies.Configuration`
- `src/Cephalon.Observability.ConsulDependencies/Hosting` -> `Cephalon.Observability.ConsulDependencies.Hosting`
- `src/Cephalon.Observability.ConsulDependencies/Services` -> `Cephalon.Observability.ConsulDependencies.Services`
- `src/Cephalon.Observability.ElasticsearchDependencies/Configuration` -> `Cephalon.Observability.ElasticsearchDependencies.Configuration`
- `src/Cephalon.Observability.ElasticsearchDependencies/Hosting` -> `Cephalon.Observability.ElasticsearchDependencies.Hosting`
- `src/Cephalon.Observability.ElasticsearchDependencies/Services` -> `Cephalon.Observability.ElasticsearchDependencies.Services`
- `src/Cephalon.Observability.HttpDependencies/Configuration` -> `Cephalon.Observability.HttpDependencies.Configuration`
- `src/Cephalon.Observability.HttpDependencies/Hosting` -> `Cephalon.Observability.HttpDependencies.Hosting`
- `src/Cephalon.Observability.HttpDependencies/Services` -> `Cephalon.Observability.HttpDependencies.Services`
- `src/Cephalon.Observability.KafkaDependencies/Configuration` -> `Cephalon.Observability.KafkaDependencies.Configuration`
- `src/Cephalon.Observability.KafkaDependencies/Hosting` -> `Cephalon.Observability.KafkaDependencies.Hosting`
- `src/Cephalon.Observability.KafkaDependencies/Services` -> `Cephalon.Observability.KafkaDependencies.Services`
- `src/Cephalon.Observability.MemcachedDependencies/Configuration` -> `Cephalon.Observability.MemcachedDependencies.Configuration`
- `src/Cephalon.Observability.MemcachedDependencies/Hosting` -> `Cephalon.Observability.MemcachedDependencies.Hosting`
- `src/Cephalon.Observability.MemcachedDependencies/Services` -> `Cephalon.Observability.MemcachedDependencies.Services`
- `src/Cephalon.Observability.MongoDbDependencies/Configuration` -> `Cephalon.Observability.MongoDbDependencies.Configuration`
- `src/Cephalon.Observability.MongoDbDependencies/Hosting` -> `Cephalon.Observability.MongoDbDependencies.Hosting`
- `src/Cephalon.Observability.MongoDbDependencies/Services` -> `Cephalon.Observability.MongoDbDependencies.Services`
- `src/Cephalon.Observability.MqttDependencies/Configuration` -> `Cephalon.Observability.MqttDependencies.Configuration`
- `src/Cephalon.Observability.MqttDependencies/Hosting` -> `Cephalon.Observability.MqttDependencies.Hosting`
- `src/Cephalon.Observability.MqttDependencies/Services` -> `Cephalon.Observability.MqttDependencies.Services`
- `src/Cephalon.Observability.MySqlDependencies/Configuration` -> `Cephalon.Observability.MySqlDependencies.Configuration`
- `src/Cephalon.Observability.MySqlDependencies/Hosting` -> `Cephalon.Observability.MySqlDependencies.Hosting`
- `src/Cephalon.Observability.MySqlDependencies/Services` -> `Cephalon.Observability.MySqlDependencies.Services`
- `src/Cephalon.Observability.NatsDependencies/Configuration` -> `Cephalon.Observability.NatsDependencies.Configuration`
- `src/Cephalon.Observability.NatsDependencies/Hosting` -> `Cephalon.Observability.NatsDependencies.Hosting`
- `src/Cephalon.Observability.NatsDependencies/Services` -> `Cephalon.Observability.NatsDependencies.Services`
- `src/Cephalon.Observability.Neo4jDependencies/Configuration` -> `Cephalon.Observability.Neo4jDependencies.Configuration`
- `src/Cephalon.Observability.Neo4jDependencies/Hosting` -> `Cephalon.Observability.Neo4jDependencies.Hosting`
- `src/Cephalon.Observability.Neo4jDependencies/Services` -> `Cephalon.Observability.Neo4jDependencies.Services`
- `src/Cephalon.Observability.OpenSearchDependencies/Configuration` -> `Cephalon.Observability.OpenSearchDependencies.Configuration`
- `src/Cephalon.Observability.OpenSearchDependencies/Hosting` -> `Cephalon.Observability.OpenSearchDependencies.Hosting`
- `src/Cephalon.Observability.OpenSearchDependencies/Services` -> `Cephalon.Observability.OpenSearchDependencies.Services`
- `src/Cephalon.Observability.OracleDependencies/Configuration` -> `Cephalon.Observability.OracleDependencies.Configuration`
- `src/Cephalon.Observability.OracleDependencies/Hosting` -> `Cephalon.Observability.OracleDependencies.Hosting`
- `src/Cephalon.Observability.OracleDependencies/Services` -> `Cephalon.Observability.OracleDependencies.Services`
- `src/Cephalon.Observability.PostgresDependencies/Configuration` -> `Cephalon.Observability.PostgresDependencies.Configuration`
- `src/Cephalon.Observability.PostgresDependencies/Hosting` -> `Cephalon.Observability.PostgresDependencies.Hosting`
- `src/Cephalon.Observability.PostgresDependencies/Services` -> `Cephalon.Observability.PostgresDependencies.Services`
- `src/Cephalon.Observability.RabbitMqDependencies/Configuration` -> `Cephalon.Observability.RabbitMqDependencies.Configuration`
- `src/Cephalon.Observability.RabbitMqDependencies/Hosting` -> `Cephalon.Observability.RabbitMqDependencies.Hosting`
- `src/Cephalon.Observability.RabbitMqDependencies/Services` -> `Cephalon.Observability.RabbitMqDependencies.Services`
- `src/Cephalon.Observability.RedisDependencies/Configuration` -> `Cephalon.Observability.RedisDependencies.Configuration`
- `src/Cephalon.Observability.RedisDependencies/Hosting` -> `Cephalon.Observability.RedisDependencies.Hosting`
- `src/Cephalon.Observability.RedisDependencies/Services` -> `Cephalon.Observability.RedisDependencies.Services`
- `src/Cephalon.Observability.SqlServerDependencies/Configuration` -> `Cephalon.Observability.SqlServerDependencies.Configuration`
- `src/Cephalon.Observability.SqlServerDependencies/Hosting` -> `Cephalon.Observability.SqlServerDependencies.Hosting`
- `src/Cephalon.Observability.SqlServerDependencies/Services` -> `Cephalon.Observability.SqlServerDependencies.Services`
- `src/Cephalon.Observability.OpenTelemetry/Hosting` -> `Cephalon.Observability.OpenTelemetry.Hosting`
- `src/Cephalon.Observability.Aws/Configuration` -> `Cephalon.Observability.Aws.Configuration`
- `src/Cephalon.Observability.Aws/Hosting` -> `Cephalon.Observability.Aws.Hosting`
- `src/Cephalon.Observability.AzureMonitor/Configuration` -> `Cephalon.Observability.AzureMonitor.Configuration`
- `src/Cephalon.Observability.AzureMonitor/Hosting` -> `Cephalon.Observability.AzureMonitor.Hosting`
- `src/Cephalon.Observability.Serilog/Hosting` -> `Cephalon.Observability.Serilog.Hosting`
- `src/Cephalon.Cli/Commands` -> `Cephalon.Cli.Commands`
- `src/Cephalon.Cli/Console` -> `Cephalon.Cli.Console`
- `src/Cephalon.ReferenceDocs/Generation` -> `Cephalon.ReferenceDocs.Generation`
- `src/Cephalon.ReferenceDocs/IO` -> `Cephalon.ReferenceDocs.IO`
- `src/Cephalon.Agentics/Configuration` -> `Cephalon.Agentics.Configuration`
- `src/Cephalon.Agentics/Modules` -> `Cephalon.Agentics.Modules`
- `src/Cephalon.Agentics/Registration` -> `Cephalon.Agentics.Registration`
- `src/Cephalon.Agentics/Services` -> `Cephalon.Agentics.Services`
- `src/Cephalon.Edge/Configuration` -> `Cephalon.Edge.Configuration`
- `src/Cephalon.Edge/Modules` -> `Cephalon.Edge.Modules`
- `src/Cephalon.Edge/Registration` -> `Cephalon.Edge.Registration`
- `src/Cephalon.Edge/Services` -> `Cephalon.Edge.Services`
- `src/Cephalon.Eventing.Behaviors/Modules` -> `Cephalon.Eventing.Behaviors.Modules`
- `src/Cephalon.Eventing.Behaviors/Registration` -> `Cephalon.Eventing.Behaviors.Registration`
- `src/Cephalon.Eventing.Behaviors/Services` -> `Cephalon.Eventing.Behaviors.Services`
- `src/Cephalon.Eventing/Configuration` -> `Cephalon.Eventing.Configuration`
- `src/Cephalon.Eventing/Modules` -> `Cephalon.Eventing.Modules`
- `src/Cephalon.Eventing/Registration` -> `Cephalon.Eventing.Registration`
- `src/Cephalon.Eventing/Services` -> `Cephalon.Eventing.Services`
- `src/Cephalon.Retrieval/Configuration` -> `Cephalon.Retrieval.Configuration`
- `src/Cephalon.Retrieval/Modules` -> `Cephalon.Retrieval.Modules`
- `src/Cephalon.Retrieval/Registration` -> `Cephalon.Retrieval.Registration`
- `src/Cephalon.Retrieval/Services` -> `Cephalon.Retrieval.Services`
- `src/Cephalon.MultiTenancy/Configuration` -> `Cephalon.MultiTenancy.Configuration`
- `src/Cephalon.MultiTenancy/Modules` -> `Cephalon.MultiTenancy.Modules`
- `src/Cephalon.MultiTenancy/Registration` -> `Cephalon.MultiTenancy.Registration`
- `src/Cephalon.MultiTenancy/Services` -> `Cephalon.MultiTenancy.Services`
- `src/Cephalon.MultiTenancy.Governance/Configuration` -> `Cephalon.MultiTenancy.Governance.Configuration`
- `src/Cephalon.MultiTenancy.Governance/Modules` -> `Cephalon.MultiTenancy.Governance.Modules`
- `src/Cephalon.MultiTenancy.Governance/Registration` -> `Cephalon.MultiTenancy.Governance.Registration`
- `src/Cephalon.MultiTenancy.Governance/Services` -> `Cephalon.MultiTenancy.Governance.Services`
- `src/Cephalon.MultiTenancy.Governance.AspNetCore/Configuration` -> `Cephalon.MultiTenancy.Governance.AspNetCore.Configuration`
- `src/Cephalon.MultiTenancy.Governance.AspNetCore/Hosting` -> `Cephalon.MultiTenancy.Governance.AspNetCore.Hosting`
- `src/Cephalon.MultiTenancy.Governance.AmazonSesDelivery/Configuration` -> `Cephalon.MultiTenancy.Governance.AmazonSesDelivery.Configuration`
- `src/Cephalon.MultiTenancy.Governance.AmazonSesDelivery/Hosting` -> `Cephalon.MultiTenancy.Governance.AmazonSesDelivery.Hosting`
- `src/Cephalon.MultiTenancy.Governance.AmazonSesDelivery/Services` -> `Cephalon.MultiTenancy.Governance.AmazonSesDelivery.Services`
- `src/Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore/Configuration` -> `Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore.Configuration`
- `src/Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore/Hosting` -> `Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore.Hosting`
- `src/Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore/Services` -> `Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore.Services`
- `src/Cephalon.MultiTenancy.Governance.HttpDelivery/Configuration` -> `Cephalon.MultiTenancy.Governance.HttpDelivery.Configuration`
- `src/Cephalon.MultiTenancy.Governance.HttpDelivery/Hosting` -> `Cephalon.MultiTenancy.Governance.HttpDelivery.Hosting`
- `src/Cephalon.MultiTenancy.Governance.HttpDelivery/Services` -> `Cephalon.MultiTenancy.Governance.HttpDelivery.Services`
- `src/Cephalon.MultiTenancy.Governance.MailgunDelivery/Configuration` -> `Cephalon.MultiTenancy.Governance.MailgunDelivery.Configuration`
- `src/Cephalon.MultiTenancy.Governance.MailgunDelivery/Hosting` -> `Cephalon.MultiTenancy.Governance.MailgunDelivery.Hosting`
- `src/Cephalon.MultiTenancy.Governance.MailgunDelivery/Services` -> `Cephalon.MultiTenancy.Governance.MailgunDelivery.Services`
- `src/Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore/Configuration` -> `Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore.Configuration`
- `src/Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore/Hosting` -> `Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore.Hosting`
- `src/Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore/Services` -> `Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore.Services`
- `src/Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery/Configuration` -> `Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.Configuration`
- `src/Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery/Hosting` -> `Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.Hosting`
- `src/Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery/Services` -> `Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.Services`
- `src/Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.AzureIdentity/Configuration` -> `Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.AzureIdentity.Configuration`
- `src/Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.AzureIdentity/Hosting` -> `Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.AzureIdentity.Hosting`
- `src/Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.AzureIdentity/Services` -> `Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.AzureIdentity.Services`
- `src/Cephalon.MultiTenancy.Governance.SendGridDelivery/Configuration` -> `Cephalon.MultiTenancy.Governance.SendGridDelivery.Configuration`
- `src/Cephalon.MultiTenancy.Governance.SendGridDelivery/Hosting` -> `Cephalon.MultiTenancy.Governance.SendGridDelivery.Hosting`
- `src/Cephalon.MultiTenancy.Governance.SendGridDelivery/Services` -> `Cephalon.MultiTenancy.Governance.SendGridDelivery.Services`
- `src/Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore/Configuration` -> `Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore.Configuration`
- `src/Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore/Hosting` -> `Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore.Hosting`
- `src/Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore/Services` -> `Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore.Services`
- `src/Cephalon.MultiTenancy.Governance.SmtpDelivery/Configuration` -> `Cephalon.MultiTenancy.Governance.SmtpDelivery.Configuration`
- `src/Cephalon.MultiTenancy.Governance.SmtpDelivery/Hosting` -> `Cephalon.MultiTenancy.Governance.SmtpDelivery.Hosting`
- `src/Cephalon.MultiTenancy.Governance.SmtpDelivery/Services` -> `Cephalon.MultiTenancy.Governance.SmtpDelivery.Services`
- `src/Cephalon.Scaffolding/Generation` -> `Cephalon.Scaffolding.Generation`
- `src/Cephalon.Scaffolding/IO` -> `Cephalon.Scaffolding.IO`
- `templates/Cephalon.TemplatePack/templates` -> installable `dotnet new` blueprint templates
- `samples/Cephalon.ReferenceModule.*` -> reference module package examples
- `benchmarks/Cephalon.Benchmarks/Composition` -> `Cephalon.Benchmarks.Composition`
- `benchmarks/Cephalon.Benchmarks/Runtime` -> `Cephalon.Benchmarks.Runtime`
- `benchmarks/Cephalon.Benchmarks/Scaffolding` -> `Cephalon.Benchmarks.Scaffolding`
- `benchmarks/Cephalon.Benchmarks/Support` -> `Cephalon.Benchmarks.Support`

## Current opinionated defaults

- module dependencies are explicit and type-based
- capabilities are first-class and introspectable
- app blueprints and pattern selections are first-class and introspectable
- blueprint scaffold plans are first-class and introspectable
- scaffold generation can be driven from the same app-profile contract used by runtime introspection
- the shipped `dotnet new` template pack should stay aligned with blueprint and scaffold-plan semantics
- module authoring starters should stay aligned with module contracts, lifecycle rules, localization behavior, and transport contribution interfaces
- transport selections are first-class and introspectable
- manifest v2 includes schema version, engine version, module metadata, and capability source-module mapping
- engine app shape is configuration-driven through the `Engine` section
- host builders can also split project configuration through `Configurations/Add*.json` and `Configurations/{group}/{Environment}.json`
- engine module discovery can be driven through `Engine:Discovery:Assemblies`
- engine package assembly loading can be driven through `Engine:Discovery:Packages`
- engine package-directory discovery can be driven through `Engine:Discovery:PackageDirectories`
- package manifests should use `version`, `compatibility.minimumEngineVersion`, `compatibility.maximumEngineVersion`, `compatibility.supportedTargetFrameworks`, and optional `integrity.sha256` when independently shipped packages need stronger compatibility or supply-chain signals
- package manifests can declare package-to-package `dependencies` with optional `minimumVersion` / `maximumVersion` bounds when a package expects another independently shipped package to be present
- package manifests can also carry `publisher`, `distribution`, `provenance`, `signature`, or `signatures` metadata so trust and operator diagnostics can reason about provenance
- package signatures can be cryptographically verified through `signature.keyId`, `signature.value`, or equivalent entries in `signatures[]`, together with `Engine:Trust:TrustedSignaturePublicKeys` or `Engine:Trust:TrustedSignatureCertificates` plus `TrustedSignatureCertificateAuthorities`
- engine package-governance requirements can be driven through `Engine:PackagePolicy`
- engine future-tech selection can be driven through `Engine:Technologies`
- engine future-tech catalog can be extended through `ITechnologyContributor` or `engine.RegisterTechnology(...)`
- active future-tech runtime surfaces can be inspected through `ITechnologyRuntimeContributor` and `/engine/technology-surfaces`
- explicit cell boundaries, governed cell routes, cell health-isolation posture, and configuration-driven traffic automation can be inspected through `ICellBoundaryCatalog`, `ICellRouteCatalog`, `ICellHealthIsolationCatalog`, `ICellTrafficAutomationRuntimeCatalog`, `/engine/cells`, `/engine/cell-routes`, `/engine/cell-health-isolations`, `/engine/cell-traffic-automations`, and `/engine/snapshot`, and active boundaries, routes, health-isolation answers, or automation overlays now auto-select `cell-based-architecture` without creating a second topology, traffic, health, or automation registry; that same shared automation surface now also carries additive `providerId` plus `edgeNodeIds` targeting, deterministic highest-priority provider or edge materializer selection, and derived overall `materializationState` truth instead of splitting traffic materialization into a second runtime catalog
- module-owned queryable data products can now be inspected through `IDataProductCatalog`, `/engine/data-products`, and `/engine/snapshot` without inventing a host-only data mesh registry
- module-owned CDC captures can now be inspected through `ICdcCaptureCatalog`, `ICdcCaptureRuntimeStateCatalog`, `/engine/cdc-captures`, `/engine/cdc-captures/runtime`, and `/engine/snapshot`, with explicit `OutboxId` linkage, typed freshness/lag/publication posture, linked `OutboxDispatchState` when the publication path reports it, and validation against active source modules and outboxes instead of a host-only sync registry
- merged operator-facing runtime introspection should come from `IRuntimeIntrospectionSnapshotProvider` and `/engine/snapshot` instead of recomposing manifest, status, technology surfaces, diagnostics conventions, and lifecycle story data ad hoc in hosts
- bounded event publication actions should flow through the host-agnostic `IEventPublicationDispatcher` and host adapter routes such as `POST /engine/event-publications` rather than exposing eventing package implementation types from host adapters
- event publication runtime-state answers should flow through the host-agnostic `IEventPublicationRuntimeCatalog`, `/engine/event-publications/runtime*`, and `snapshot.EventPublicationStates`, with outbox `accepted` handoff kept separate from downstream dispatch completion
- event dispatch runtime-state answers should flow through the host-agnostic `IEventDispatchRuntimeCatalog`, `/engine/event-dispatches`, `/engine/event-dispatches/terminal-failures`, and `snapshot.EventDispatchStates`, with first-class terminal-failure posture kept separate from any future broker-specific dead-letter or durable inbox claim
- event dispatch remediation should flow through the host-agnostic `IEventDispatchRemediationDispatcher`, `IEventDispatchRemediationRuntimeCatalog`, `IEventDispatchRemediationCommandJournal`, `EventDispatchRemediationRuntimeSummary`, `EventDispatchRemediationRuntimeRetention`, `event-dispatch-remediations`, `event-dispatch-remediation-commands`, `POST /engine/event-dispatches/{outboxId}/commands/{operationId}`, `/engine/event-dispatch-remediation-commands*` including `/engine/event-dispatch-remediation-commands`, `/engine/event-dispatch-remediation-commands/summary`, `/engine/event-dispatch-remediation-commands/latest`, `/engine/event-dispatch-remediation-commands/retention`, `/engine/event-dispatch-remediation-commands/in-doubt?beforeUtc={beforeUtc}`, `/engine/event-dispatch-remediation-commands/in-doubt/summary?beforeUtc={beforeUtc}`, `/engine/event-dispatch-remediation-commands/in-doubt/oldest?beforeUtc={beforeUtc}`, `/engine/event-dispatch-remediation-commands/observations?fromUtc={fromUtc}&toUtc={toUtc}`, `/engine/event-dispatch-remediation-commands/observations/summary?fromUtc={fromUtc}&toUtc={toUtc}`, `/engine/event-dispatch-remediation-commands/{commandId}`, `/engine/event-dispatch-remediation-commands/outboxes/{outboxId}`, `/engine/event-dispatch-remediation-commands/messages/{messageId}`, `/engine/event-dispatch-remediation-commands/channels/{channelId}`, `/engine/event-dispatch-remediation-commands/operations/{operationId}`, `/engine/event-dispatch-remediation-commands/actors/{actorId}`, `/engine/event-dispatch-remediation-commands/correlations/{correlationId}`, `/engine/event-dispatch-remediation-commands/reasons/{reason}`, `/engine/event-dispatch-remediation-commands/dispatch-outcomes/{dispatchOutcome}`, and `/engine/event-dispatch-remediation-commands/outcomes/{outcome}`, and `/engine/snapshot` `TechnologySurfaces`; bounded dispatch-store commands can claim `retry-now`, `retry-later`, `skip`, `quarantine`, and dispatch-store `dead-letter` only when an active `IEventDispatchStore` owns the outbox, command ids must be reserved before dispatch-store mutation through the active journal, duplicate command ids must reject without mutation under the native `unique-command-id` policy even when the existing record is still `reserved`, latest/summary/retention/in-doubt/stale-in-doubt/stale-in-doubt-summary/oldest-in-doubt/observation-window reads and observation-window summaries must remain tied to authoritative command records rather than duplicate HTTP attempts, `event-dispatch-remediation-commands` must expose a catalog entry even when history is empty so route/retention/idempotency/provider-neutral/Wolverine-free metadata is discoverable before the first command, metadata must advertise list/result/in-doubt/in-doubt-summary/oldest-in-doubt/outbox routes plus in-doubt cutoff, observed-window, read-limit, reservation policy, reserved/in-doubt summary posture, and oldest reserved command readback instead of forcing operator UIs to infer it from routes, list/filter reads may apply a positive `limit` query to return newest retained records without changing the host-agnostic catalog, in-doubt-summary reads must remain direct aggregate queries, oldest-in-doubt reads must remain direct single-record queries, retention readback must say when bounded process-local command history has truncated older records, summary readback must surface reserved/in-doubt counts, oldest retained reserved command id/timestamp, dropped-command, and incomplete-summary warnings when truncation affects the aggregate scope, and broker-specific dead-letter/replay remains unclaimed until a package owns that path
- future-tech runtime primitives should live in companion packages such as `Cephalon.Agentics`, `Cephalon.Eventing`, `Cephalon.Retrieval`, `Cephalon.MultiTenancy`, `Cephalon.MultiTenancy.Governance`, `Cephalon.MultiTenancy.Governance.HttpDelivery`, `Cephalon.MultiTenancy.Governance.AmazonSesDelivery`, `Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore`, `Cephalon.MultiTenancy.Governance.MailgunDelivery`, `Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore`, `Cephalon.MultiTenancy.Governance.SendGridDelivery`, `Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore`, `Cephalon.MultiTenancy.Governance.SmtpDelivery`, or `Cephalon.Edge`
- installed modules should extend shipped technology packs through pack-specific contributor services instead of hardcoding host-owned descriptor lists
- engine options can disable modules and capabilities through `Engine:Options`
- engine observability conventions can be tuned through `Engine:Observability`
- engine telemetry export guidance can be tuned through `Engine:Observability:Telemetry`
- runtime diagnostics conventions should flow through `IRuntimeDiagnosticsCatalog`, `/engine/diagnostics`, and `/engine/snapshot`
- runtime lifecycle answers should flow through `IRuntime.OperationalStory`, `/engine/runtime-story`, and `/engine/snapshot`
- hosts can turn Elasticsearch cluster-health checks into reusable dependency-health contributions through `Engine:Observability:DependencyHealth:Elasticsearch` and `Cephalon.Observability.ElasticsearchDependencies`
- hosts can turn Consul control-plane leader checks into reusable dependency-health contributions through `Engine:Observability:DependencyHealth:Consul` and `Cephalon.Observability.ConsulDependencies`
- hosts can turn external HTTP upstreams into reusable dependency-health contributions through `Engine:Observability:DependencyHealth:Http` and `Cephalon.Observability.HttpDependencies`, but that pack should stay generic across HTTP semantics rather than absorbing product-specific response mapping
- hosts can turn Kafka cluster metadata checks into reusable dependency-health contributions through `Engine:Observability:DependencyHealth:Kafka` and `Cephalon.Observability.KafkaDependencies`
- hosts can turn Memcached cache endpoints into reusable dependency-health contributions through `Engine:Observability:DependencyHealth:Memcached` and `Cephalon.Observability.MemcachedDependencies`
- hosts can turn MongoDB dependencies into reusable dependency-health contributions through `Engine:Observability:DependencyHealth:MongoDb` and `Cephalon.Observability.MongoDbDependencies`
- hosts can turn MQTT broker dependencies into reusable dependency-health contributions through `Engine:Observability:DependencyHealth:Mqtt` and `Cephalon.Observability.MqttDependencies`
- hosts can turn MySQL and MariaDB dependencies into reusable dependency-health contributions through `Engine:Observability:DependencyHealth:MySql` and `Cephalon.Observability.MySqlDependencies`
- hosts can turn NATS broker dependencies into reusable dependency-health contributions through `Engine:Observability:DependencyHealth:Nats` and `Cephalon.Observability.NatsDependencies`, with the package free to grow across NATS-native auth, TLS, and broker reachability semantics without becoming a catch-all for unrelated workload logic
- hosts can turn Neo4j graph dependencies into reusable dependency-health contributions through `Engine:Observability:DependencyHealth:Neo4j` and `Cephalon.Observability.Neo4jDependencies`
- hosts can turn OpenSearch cluster dependencies into reusable dependency-health contributions through `Engine:Observability:DependencyHealth:OpenSearch` and `Cephalon.Observability.OpenSearchDependencies`
- hosts can turn ClickHouse analytics dependencies into reusable dependency-health contributions through `Engine:Observability:DependencyHealth:ClickHouse` and `Cephalon.Observability.ClickHouseDependencies`
- hosts can turn Cassandra dependencies into reusable dependency-health contributions through `Engine:Observability:DependencyHealth:Cassandra` and `Cephalon.Observability.CassandraDependencies`
- hosts can turn Oracle dependencies into reusable dependency-health contributions through `Engine:Observability:DependencyHealth:Oracle` and `Cephalon.Observability.OracleDependencies`
- hosts can turn Postgres dependencies into reusable dependency-health contributions through `Engine:Observability:DependencyHealth:Postgres` and `Cephalon.Observability.PostgresDependencies`
- hosts can turn RabbitMQ dependencies into reusable dependency-health contributions through `Engine:Observability:DependencyHealth:RabbitMq` and `Cephalon.Observability.RabbitMqDependencies`
- hosts can turn Redis and cache endpoints into reusable dependency-health contributions through `Engine:Observability:DependencyHealth:Redis` and `Cephalon.Observability.RedisDependencies`
- hosts can turn SQL Server and Azure SQL dependencies into reusable dependency-health contributions through `Engine:Observability:DependencyHealth:SqlServer` and `Cephalon.Observability.SqlServerDependencies`
- hosts can turn that telemetry contract into a supported OTLP path through `Cephalon.Observability.OpenTelemetry`
- hosts can turn that telemetry contract into an AWS-hosted OTLP path through `Cephalon.Observability.Aws`
- hosts can turn that telemetry contract into a supported Azure Monitor path through `Cephalon.Observability.AzureMonitor`
- hosts can turn the shared `ILogger` pipeline into a supported Serilog path through `Cephalon.Observability.Serilog`
- engine trust and capability policy can be tuned through `Engine:Trust`
- engine trust policy can also allow-list package checksums through `Engine:Trust:AllowedPackageChecksums`
- engine trust policy can also allow-list publishers, signer fingerprints, and trusted signature public keys
- engine package metadata requirements can be tuned through `Engine:PackagePolicy`
- engine localization can be tuned through `Engine:Localization`
- engine startup, stop, and restart behavior can be tuned through `Engine:FailurePolicy`, including startup warmup, shutdown drain, and manual restart backoff windows
- installed modules can contribute dependency health through `IDependencyHealthContributor`
- installed modules can contribute localization resources through `ILocalizedResourceContributor`
- the runtime manifest is always available
- runtime lifecycle is explicit and introspectable
- runtime policy state is introspectable through `/engine/options`
- runtime failure policy is introspectable through `/engine/failure-policy`
- runtime diagnostics conventions are introspectable through `IRuntimeDiagnosticsCatalog`, `/engine/diagnostics`, and `/engine/snapshot`
- runtime lifecycle story is introspectable through `IRuntime.OperationalStory`, `/engine/runtime-story`, and `/engine/snapshot`
- runtime trust policy is introspectable through `/engine/trust-policy`
- runtime package loading is introspectable through `/engine/packages`, including package `kind`, resolved assembly `path`, discovery `sourcePath`, declared version/compatibility, package dependencies, external distribution metadata, provenance metadata, computed checksum, signature verification state, verification source, certificate thumbprints when applicable, and trust reason
- runtime package provenance is introspectable through `/engine/packages` and `/engine/trust-policy`, including publisher id, signature key id, signer fingerprint, signing-certificate thumbprints, and declared external distribution/provenance hints when the package manifest declared them
- runtime package governance is introspectable through `/engine/package-policy`
- runtime execution graphs are introspectable through `IExecutionGraphContributor`, `IExecutionRuntimeCatalog`, `/engine/execution-graphs`, and `/engine/snapshot`
- runtime technology selection is introspectable through `/engine/technologies`
- runtime technology catalog is introspectable through `/engine/technology-catalog`
- runtime dependency health is introspectable through `/engine/dependencies`
- runtime localization state is introspectable through `/engine/localization`
- runtime health semantics are exposed through `/health`, `/health/live`, and `/health/ready`
- blueprint-driven project shape is introspectable through `AppProfile.Scaffold` and `/engine/scaffold`
- future suite-level shape is modeled separately through `SuiteBlueprint`, `SuiteScaffoldPlan`, and `SuiteScaffoldService` instead of overloading the current app-level scaffold contract
- the same runtime can be hosted in ASP.NET Core or generic worker hosts without changing module contracts
- the engine emits built-in metrics and tracing through the `Cephalon.Engine` meter and activity source
- the host stays thin and pushes behavior into modules
- selected transports gate which host routes are mapped
- when `RestApi` is selected on ASP.NET Core, REST endpoints should surface through OpenAPI and Scalar
- keep REST OpenAPI customization inside `Cephalon.AspNetCore.Transformers`
- non-REST protocol endpoints should stay excluded from REST API documentation
- adapter packages must be registered for transports that are not built into the host core
- REST endpoints can opt into capability enforcement through `RequireCapability(...)`

## Database topology

The shipped phase-8 data baseline is intentionally narrower than the long-term engine direction.

Current recommendation:

- keep `Engine:Data` as the logical app-model selection layer and let the shipped `Engine:Databases` contract own physical runtime tuning, the first named roles, and nested migration policy
- treat named roles such as `Write`, `Read`, and `History` as first-class runtime descriptors that the engine can validate and expose through introspection, including `/engine/databases` for raw topology and `/engine/database-roles` for resolved runtime truth
- keep durable audit history as an additive provider-pack follow-through instead of pretending the current `Cephalon.Audit` baseline already owns one durable storage model
- prefer provider registration helpers, shared model-builder slices, and interceptors over mandatory `DbContext` base classes; convenience base classes can exist later, but they should stay optional DX helpers

The first shipped follow-through on that rule is `Cephalon.Audit.EntityFramework`, which persists durable audit history through `Engine:Audit:History` plus a selected engine-owned database role, defaulting to `History`, while keeping `Cephalon.Audit` narrow and host-agnostic. See [Database topology](database-topology.md) for the current baseline and the remaining post-`ENG-062` gaps.

## Next expansion points

- richer capability metadata and policy
- startup hooks and lifecycle events
- execution runners, lifecycle events, and event-bus coordination on top of the shipped execution-graph contract
- edge-runtime traffic materializers plus broader provider and edge reconciliation follow-through on top of the shipped cell-boundary, cell-route, cell-health-isolation, provider-aware plus edge-aware traffic-automation catalogs, and the new shared provider-materializer contract
- richer operator-runtime answers, cloud-targeted tracing/export follow-through, and any additional provider-specific dependency-health packs only when concrete adoption needs justify expanding beyond the shipped Cassandra, ClickHouse, Consul, Elasticsearch, HTTP, Kafka, Memcached, MongoDB, MQTT, MySQL, NATS, Neo4j, OpenSearch, Oracle, Postgres, RabbitMQ, Redis, SQL Server, OpenTelemetry, AWS, and Azure Monitor observability companions
- richer parameterized templates and generators driven by scaffold plans
- richer localization catalogs and package-provided language packs
- sustained benchmark coverage for hot engine paths
