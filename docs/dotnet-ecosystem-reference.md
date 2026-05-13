# .NET Ecosystem Reference for CephalonEngine

> Comprehensive reference of .NET libraries, frameworks, and NuGet packages relevant to building CephalonEngine -- a modular-monolith engine framework on .NET 10. Researched April 2026; sections 14-16 (Aspire, AI/Agent libraries, Modern Caching) added May 13, 2026.

---

## 1. Hosting, Configuration, and Dependency Injection

### Microsoft.Extensions.Hosting (Generic Host / WebApplication)

**What it does:** Provides the Generic Host (`IHost`) and `WebApplicationBuilder`/`WebApplication` bootstrapping for .NET applications. The host encapsulates DI, configuration, logging, and hosted services into a single object that manages application lifetime.

**Key features:**
- `WebApplicationBuilder` (ASP.NET) and `HostApplicationBuilder` (non-HTTP) unify startup
- Automatic integration with `IConfiguration`, `ILogger`, and `IServiceProvider`
- `IHostedService` / `BackgroundService` lifecycle management
- `IHostEnvironment` for environment-aware behavior

**Framework relevance to CephalonEngine:** The engine's composition runtime (`Cephalon.Engine`) already relies on `Microsoft.Extensions.Configuration.Abstractions` and `Microsoft.Extensions.Logging.Abstractions`. The Generic Host is the natural entry point for all Cephalon host types (ASP.NET, Worker, Edge).

**Current version:** Ships with .NET 10 (10.0.x). LTS release.

---

### Microsoft.Extensions.Configuration

**What it does:** Layered configuration system with multiple providers (JSON, environment variables, user secrets, Azure Key Vault, command line).

**Key features:**
- Provider chain with override semantics (last writer wins)
- `IConfiguration` / `IConfigurationSection` for hierarchical reads
- Binding to POCOs via `ConfigurationBinder` or Options pattern
- Hot-reload support (`reloadOnChange: true` for JSON files)

**Framework relevance:** `Cephalon.Behaviors` already references `Microsoft.Extensions.Configuration.Abstractions` and `Microsoft.Extensions.Configuration.Binder`. All module-level configuration flows through this stack.

**Current version:** 10.0.x (ships in-box with .NET 10).

---

### Microsoft.Extensions.Options (IOptions / IOptionsSnapshot / IOptionsMonitor)

**What it does:** Strongly-typed access to configuration sections with validation and change-notification support.

**Key features and differences:**
- **`IOptions<T>`** -- Singleton. Read once at startup. Does NOT support named options or reload.
- **`IOptionsSnapshot<T>`** -- Scoped. Re-reads on each scope (each HTTP request). Supports named options via `.Get("name")`.
- **`IOptionsMonitor<T>`** -- Singleton but observes changes. Fires `OnChange` callback. Supports named options.
- **Validation:** `.ValidateDataAnnotations()`, `.Validate(Func)`, `.ValidateOnStart()` -- ensures fail-fast on misconfiguration.
- **Modern .NET 10 pattern:** `services.AddOptions<T>().BindConfiguration("Section").ValidateDataAnnotations().ValidateOnStart()`

**Framework relevance:** `Cephalon.Behaviors` already references `Microsoft.Extensions.Options`. Every Cephalon module should use the Options pattern for its configuration section, and the engine should enforce `ValidateOnStart()` so misconfigurations surface at boot time rather than at runtime.

**Current version:** 10.0.x (in-box).

---

### Microsoft.Extensions.DependencyInjection (Built-in DI)

**What it does:** The default IoC container for all .NET applications since .NET Core. Lightweight, fast, and the de-facto standard.

**Key features:**
- Transient, Scoped, Singleton lifetimes
- Open-generic registrations (`services.AddSingleton(typeof(IRepo<>), typeof(Repo<>))`)
- **Keyed services** (.NET 8+): `services.AddKeyedSingleton<ICache>("redis", ...)` -- resolve via `[FromKeyedServices("redis")]`
- Factory-based registrations
- `IServiceProviderIsService` for feature detection

**Limitations vs third-party containers:**
- No property injection
- No child/nested lifetime scopes beyond the built-in request scope
- No AOP / interception / decoration (need Scrutor or manual registration)
- No assembly scanning (need Scrutor)
- No conditional registrations

**Framework relevance:** CephalonEngine uses `Microsoft.Extensions.DependencyInjection.Abstractions` throughout. Keyed services are particularly relevant for multi-provider scenarios (e.g., keying different event-store backends by tenant or purpose).

**Current version:** 10.0.x (in-box). LTS.

---

### Autofac (Third-party DI)

**What it does:** Full-featured IoC container with advanced capabilities beyond the built-in container.

**Key features unique to Autofac:**
- **Property injection** -- automatically resolves required properties (Autofac 7.0+)
- **Modules** (`IModule`) -- modular, composable registration units
- **Child lifetime scopes** -- create nested scopes with specific registrations (e.g., per-unit-of-work)
- **Keyed/Named services** (predates .NET 8 keyed services)
- **Interception** via `Autofac.Extras.DynamicProxy` (Castle DynamicProxy)
- **Decorator support** built-in
- **Assembly scanning** with rich filtering

**When to use over built-in:** When you need property injection, child scopes, AOP/interception, or module-based registration composition across a large solution.

**Performance note:** The built-in container is approximately 5x faster than Autofac for simple resolve operations. Use Autofac only when its advanced features are required.

**Current version:** Autofac 9.0.0; `Autofac.Extensions.DependencyInjection` integrates with the Generic Host.

**Framework relevance:** CephalonEngine could optionally support Autofac for users who need child scopes or interception. The engine's `ICephalonModule` registration could be implemented as an Autofac `Module` for those scenarios.

---

### Lamar (Third-party DI by JasperFx)

**What it does:** Fast IoC container and StructureMap successor, optimized for ASP.NET Core. Part of the JasperFx "Critter Stack."

**Key features:**
- **Roslyn-based code generation** for dependency resolution (no Expression trees or IL emit)
- **Diagnostic tools** -- `WhatDoIHave()`, `WhatDidIScan()`, environment checks
- **Assembly scanning** with convention-based registration
- **Decorator / interceptor support**
- **Inline dependencies** and constructor selection policies

**When to use over built-in:** When you want Wolverine/Marten integration (Critter Stack uses Lamar internally), need advanced diagnostics, or are migrating from StructureMap.

**Current version:** Lamar 15.0.1. Actively maintained by JasperFx.

**Framework relevance:** Since CephalonEngine already integrates with Wolverine (`Cephalon.Eventing.Wolverine`), Lamar is a natural fit for users on the Critter Stack path.

---

## 2. Messaging and CQRS Frameworks

### Cephalon extraction policy

The libraries in this section are reference systems for ideas, not required Cephalon dependencies. For core engine work, extract proven capabilities from their public behavior and documentation, then rebuild the useful shape as Cephalon-owned contracts, configuration, runtime catalogs, diagnostics, validation, and operator surfaces. Direct package usage belongs only in optional interop, migration, or compatibility companions; it must not define engine completeness.

Extraction work should target superiority, not imitation. A Cephalon-native version of an ecosystem capability should be judged against the reference library on capability breadth, configuration model, host/provider neutrality, developer ceremony, runtime truth, operational control, failure handling, performance posture, security/compliance/auditability, testability, documentation quality, and dependency/licensing/upgrade risk. If Cephalon cannot yet outperform the reference in a dimension, record that as a known gap rather than borrowing the other framework's package to hide it.

For eventing, that rule now has a runtime checkpoint: `Cephalon.Eventing` emits `eventing-superiority-profile` plus capability `eventing.superiority-profile` so the active runtime can say which MassTransit/NServiceBus/Wolverine/MediatR-inspired dimensions are `claimed`, `partial`, or `not-claimed` before docs or roadmap items describe them as complete, including separate `durable-remediation-command-audit`, `durable-command-journal-replay-cursor`, `broker-dead-letter-replay-ownership`, `broker-topology-materialization-ownership`, `provider-partition-ownership`, `downstream-delivery-completion-ownership`, `broker-inbound-consumption-ownership`, `serialization-and-contract-versioning-ownership`, `tenant-and-correlation-context-ownership`, `scheduled-and-delayed-delivery-ownership`, `durable-retry-queue-ownership`, `idempotency-ownership`, `subscription-concurrency-ownership`, `subscription-ordering-ownership`, `process-manager-state-ownership`, and `choreography-handoff-ownership` posture so command-journal replay, broker replay ownership, broker topology materialization, provider partition ownership, downstream delivery completion, inbound broker consumption, wire-contract/versioning ownership, tenant/correlation context propagation, durable/provider scheduler ownership, durable retry queue ownership, broader idempotency ownership, subscription concurrency/backpressure ownership, subscription ordering ownership, process-manager state ownership, and choreography handoff ownership cannot be mistaken for one another. The event contract, serializer, schema registry, upcaster, and context policy catalogs are the current examples of that extraction rule: Cephalon owns code-first descriptors, `IEventContractCatalog`, `IEventSerializerCatalog`, `IEventSchemaRegistryCatalog`, `IEventUpcasterCatalog`, `IEventContextPolicyCatalog`, `event-contracts`, `event-serializers`, `event-schema-registries`, `event-upcasters`, `event-context-policies`, `eventing.contracts`, `eventing.serializers`, `eventing.schema-registries`, `eventing.upcasters`, and `eventing.context-policies` instead of depending on MassTransit, NServiceBus, Wolverine, or MediatR for contract-version, serializer-availability, schema-registry-availability, version-transition, or context-policy metadata; runtime evidence becomes `partial` until Cephalon or a provider owns executable upcaster execution, executable payload serialization, executable schema lookup, executable context propagation, and compatibility validation.

ENG-614 narrows the event context gap with a native implementation slice rather than a framework dependency: the active publisher evaluates `IEventContextPolicyCatalog`, enforces required `EventContextHeaderNames` headers when `ValidatesMessageHeaders = true`, and emits publisher-enforced validation plus direct in-process propagation evidence without installing Wolverine, MassTransit, NServiceBus, or MediatR. The remaining context gap is durable/provider/broker propagation and consumer-side extraction, not basic publisher-side validation.

### MediatR

**What it does:** In-process mediator pattern implementation for .NET. Decouples request/command senders from handlers.

**Key features:**
- **`IRequest<TResponse>` / `IRequestHandler<TRequest, TResponse>`** -- request/response pattern
- **`INotification` / `INotificationHandler`** -- pub/sub for in-process events
- **`IPipelineBehavior<TRequest, TResponse>`** -- cross-cutting middleware (logging, validation, transactions, caching)
- **Stream requests** (MediatR 10.0+) -- `IStreamRequest<TResponse>` returns `IAsyncEnumerable`
- Pipeline behaviors are ordered and composable

**When to use:** Best for in-process CQRS where you want handler isolation and a clean pipeline. Lightweight; no transport or durability.

**When to avoid:** When you need durable messaging, distributed pub/sub, or saga orchestration -- use Wolverine or MassTransit instead.

**Current version:** MediatR 12.x. Supports .NET 8+.

**Framework relevance:** CephalonEngine's behavior pipeline (`Cephalon.Behaviors`) is conceptually similar to MediatR pipeline behaviors, but the target is a Cephalon-owned in-process command/query/notification/stream pipeline with explicit runtime truth, not a hard MediatR dependency.

---

### Wolverine (JasperFx -- Critter Stack)

**What it does:** Next-generation .NET mediator + message bus + saga engine. Combines CQRS command handling, asynchronous messaging, scheduled jobs, and durable inbox/outbox in one framework.

**Key features:**
- **In-process command handling** (MediatR alternative with less ceremony)
- **Durable inbox/outbox** with Marten (PostgreSQL) or Polecat (SQL Server)
- **Transports:** RabbitMQ, Azure Service Bus, Amazon SQS, Kafka, TCP
- **Saga/workflow orchestration** built-in
- **HTTP endpoint generation** from message handlers
- **Aggregate handler workflow** for event-sourced aggregates
- **Code generation** via Roslyn (no reflection at runtime)
- **Polecat integration** (NEW, March 2026) -- SQL Server-backed event sourcing

**When to use over MediatR:** When you need durability, distributed messaging, saga orchestration, or the Critter Stack's event-sourcing integration.

**Current version:** Actively developed. Latest releases in March/April 2026. Fully supports .NET 10.

**Framework relevance:** CephalonEngine has `Cephalon.Eventing.Wolverine` as an optional provider-managed proof, but Wolverine's broader value should be mined into Cephalon-owned eventing capabilities such as durable inbox/outbox, scheduled delivery, error policy, handler discovery, diagnostics, and dead-letter replay without making Wolverine the default engine layer.

---

### MassTransit

**What it does:** Distributed application framework for .NET. Provides a message bus abstraction over RabbitMQ, Azure Service Bus, Amazon SQS, Kafka, and more.

**Key features:**
- **Consumer/Handler model** with strongly-typed messages
- **Saga orchestration** (state machine sagas and routing slips)
- **Transports:** RabbitMQ, Azure Service Bus, SQS, Kafka, gRPC, In-Memory
- **Middleware/filters** pipeline
- **Automatic topology configuration** (queues, exchanges)
- **Outbox pattern** support
- **Scheduling** via Quartz.NET or Hangfire integration
- **Multicast, versioning, encryption, retries, transactions**

**IMPORTANT licensing change:** MassTransit v9 (announced April 2025) transitions to **commercial licensing**. v8 remains Apache 2.0 and will receive security patches through at least end of 2026. **OpenTransit** is a community-driven open-source fork of v8 targeting .NET 10+.

**When to use:** Enterprise distributed messaging with complex topologies, saga state machines, and multi-transport support. When you need transport abstraction across RabbitMQ/Azure/AWS.

**Current version:** v8.x (OSS, Apache 2.0); v9.x (commercial).

**Framework relevance:** CephalonEngine should study MassTransit for transport topology, saga state machines, routing slips, middleware filters, outbox, scheduling, multi-bus, observability, and test-harness ergonomics, then express the useful pieces through Cephalon-owned config and runtime surfaces. A MassTransit/OpenTransit adapter is only justified for explicit interop or migration work, not for core engine completeness.

---

### NServiceBus

**What it does:** Enterprise-grade service bus for .NET with world-class tooling.

**Key features:**
- **Sagas, retries, recoverability** with advanced error handling
- **ServicePulse** (monitoring dashboard), **ServiceInsight** (message flow visualization), **ServiceControl** (the nervous system)
- **Transports:** RabbitMQ, Azure Service Bus, Amazon SQS, SQL Server, MSMQ, Learning
- **Outbox, delayed delivery, scheduling**
- **Long-running process management**

**Licensing:** Commercial license (per endpoint). Free for development/evaluation.

**When to use:** Large-scale enterprise systems where the tooling (ServicePulse, ServiceInsight) and support contract justify the cost.

**Current version:** NServiceBus 10.x docs are current in the official Particular documentation set.

**Framework relevance:** CephalonEngine should study NServiceBus for logical endpoint modeling, routing layers, recoverability, immediate/delayed retries, error queues, outbox consistency, correlation headers, saga best-practice validation, audit/monitoring/health signals, and ServiceControl-style operator workflows. Those should become Cephalon-owned contracts and operator surfaces rather than a dependency on NServiceBus packages.

---

### Brighter

**What it does:** Command Processor pattern implementation following the Ports & Adapters architecture.

**Key features:**
- **Command Processor** with handler pipeline (pre/post decorators)
- **Task Queue** for async dispatch
- **Outbox pattern** for reliable messaging
- Supports RabbitMQ, Kafka, AWS SNS/SQS, Azure Service Bus, Redis

**When to use:** When you want clean Ports & Adapters architecture with explicit pipeline decoration. Good for teams familiar with CQRS patterns from the Go4 Command pattern.

**Current version:** Actively maintained. Compatible with .NET 8+.

---

### Rebus

**What it does:** Lean, simple .NET service bus following "smart endpoints, dumb pipes."

**Key features:**
- **Extremely simple API** -- minimal ceremony
- **Transports:** RabbitMQ, Azure Service Bus, Amazon SQS, SQL Server, In-Memory, File System
- **Sagas, retries, timeouts, routing**
- **Free and open-source** (MIT license) -- will stay that way forever

**When to use:** Small to medium applications where simplicity is prioritized. When you want a messaging bus without the complexity of MassTransit or NServiceBus.

**Current version:** Rebus 10.x. Actively maintained.

---

## 3. Event Sourcing and Projections

### Marten (JasperFx -- Critter Stack)

**What it does:** .NET document database AND event store built on PostgreSQL. Uses PostgreSQL's native JSON support.

**Key features:**
- **Document Store:** LINQ-queryable document DB with ACID transactions
- **Event Store:** Full event sourcing with stream append, projections, subscriptions
- **Projection types:**
  - **Inline** -- run in same transaction as event append (strong consistency)
  - **Async** -- background processing via the Projection Daemon (eventual consistency)
  - **Live/Catch-up subscriptions** for real-time event consumption
  - **Flat Table Projections** -- project events into relational tables
- **Snapshotting** for long event streams
- **Multi-tenancy** (conjoined tenancy, database-per-tenant)
- **Event versioning and upcasting**

**When to use:** When using PostgreSQL and you want combined document DB + event store without a separate database. Excellent Wolverine integration.

**Current version:** Actively maintained (GitHub updated February 2026). Requires PostgreSQL 12+.

**Framework relevance:** CephalonEngine has multiple event-sourcing providers. Marten would be the PostgreSQL provider, potentially replacing or complementing `Cephalon.EventSourcing.EntityFramework` for PostgreSQL users.

---

### Polecat (JasperFx -- Critter Stack) -- NEW 2026

**What it does:** SQL Server-backed event store and document database. Port of Marten targeting SQL Server 2025's JSON data type.

**Key features:**
- **Event Store** with projections, subscriptions, snapshots, async daemon
- **Document Database** with LINQ querying
- **Dynamic Consistency Boundaries**
- **Multi-tenancy** (conjoined tenancy, database-per-tenant)
- **Wolverine integration** for HTTP and CQRS

**When to use:** When your organization mandates SQL Server and you want Marten-equivalent event sourcing.

**Current version:** 1.0 (released March 2026). Fully supported by JasperFx Software.

**Framework relevance:** Polecat fills the SQL Server gap in the Critter Stack. CephalonEngine could add a `Cephalon.EventSourcing.Polecat` provider.

---

### EventStoreDB (Kurrent)

**What it does:** Purpose-built, dedicated event database designed for event streams at scale.

**Key features:**
- **Immutable append-only streams** -- events are never overwritten
- **Push-based subscriptions** (catch-up, persistent, filtered)
- **Server-side projections** (JavaScript)
- **gRPC client API** (`EventStore.Client.Grpc.Streams`)
- **Cluster mode** for high availability
- **OpenTelemetry integration** via `EventStore.Client.Extensions.OpenTelemetry`

**When to use:** When event sourcing is your primary architectural pattern and you want a dedicated, optimized event database rather than a general-purpose DB.

**Current version:** EventStore.Client.Grpc 23.3.9. (DB is now branded as "Kurrent").

---

### Eventuous

**What it does:** Lightweight, production-grade event sourcing library for .NET implementing DDD tactical patterns.

**Key features:**
- **Aggregates** with state folding, strongly-typed identities, optimistic concurrency
- **Command services, subscriptions, projections**
- **First-class backends:** KurrentDB (EventStoreDB), PostgreSQL, SQL Server, SQLite
- **Common abstraction layer** across backends

**When to use:** When you want a lightweight event sourcing framework that is less opinionated than Marten but more structured than rolling your own.

**Note:** API is not yet fully stable -- changes may occur between versions.

**Current version:** Available on NuGet. Active development. NDC Oslo 2026 session scheduled.

---

### Projection Patterns Summary

| Pattern | Consistency | Latency | Use Case |
|---------|------------|---------|----------|
| **Inline** | Strong (same transaction) | Zero | Read models that must be immediately consistent |
| **Async (Daemon)** | Eventual | Milliseconds to seconds | Dashboards, search indices, denormalized views |
| **Catch-up Subscription** | Eventual (replay) | Depends on gap | Rebuilding projections, new consumers |
| **Flat Table** | Depends on lifecycle | Varies | Reporting tables, analytics |

---

## 4. ORM and Data Access

### Entity Framework Core 10

**What it does:** The primary ORM for .NET. Full LINQ-to-SQL translation, change tracking, migrations, and now NativeAOT support.

**Key new features in EF Core 10:**
- **Vector search** -- `SqlVector<float>` type + `VECTOR_DISTANCE()` for AI/RAG workloads (SQL Server 2025 / Azure SQL)
- **JSON type support** -- native SQL Server JSON data type + `ExecuteUpdateAsync` on JSON columns
- **LeftJoin / RightJoin** -- first-class LINQ operators (new in .NET 10)
- **Improved parameterized collections** -- `IN` clauses use individual parameters with padding for plan cache efficiency
- **ExecuteUpdateAsync lambda** -- now accepts plain `Action` instead of requiring expression trees
- **Precompiled queries** (experimental) -- NativeAOT support with compile-time SQL generation via interceptors
- **OpenAPI 3.1** schema generation improvements
- **Split query consistency** fixes

**Interceptors:**
- `ISaveChangesInterceptor` -- intercept/modify SaveChanges operations
- `IDbCommandInterceptor` -- intercept raw SQL commands
- `IDbConnectionInterceptor` -- intercept connection open/close
- Use cases: audit trails, soft deletes, multi-tenancy query filters, read/write splitting

**Compiled Models:** Pre-generate model and mappings at build time; cuts startup overhead by 60-80%.

**Current version:** EF Core 10.0.x (LTS, supported through November 2028).

**Framework relevance:** CephalonEngine has `Cephalon.Data.EntityFramework` and `Cephalon.EventSourcing.EntityFramework`. Both reference `Microsoft.EntityFrameworkCore`. The engine should consider exposing interceptor registration APIs and compiled-model integration.

---

### Dapper

**What it does:** Micro-ORM developed by Stack Overflow. Thin layer over ADO.NET that maps query results to objects.

**Key features:**
- Raw SQL with parameterized queries
- Multi-mapping (one-to-many joins)
- Stored procedure support
- Dynamic return types
- Minimal overhead -- ~1.5-2x faster than EF Core for simple queries

**When to use vs EF Core:**
- **Use Dapper** for read-heavy hot paths, complex hand-tuned SQL, reporting queries
- **Use EF Core** for writes with change tracking, migrations, complex object graphs
- **Hybrid approach** (EF for writes, Dapper for reads) is common in high-performance systems

**Performance (2025 benchmarks):** EF Core 9+ with compiled queries is within 1.5-2x of Dapper for most read scenarios. Dapper still wins for raw single-record operations (~169us vs ~209us).

**Current version:** Dapper 2.x. Widely used.

---

### Marten (as Document Store)

See Section 3 above. Marten doubles as a document database on PostgreSQL with LINQ querying, making it an alternative to MongoDB for teams already on PostgreSQL.

---

### MongoDB .NET Driver

**What it does:** Official C#/.NET driver for MongoDB.

**Key features:**
- LINQ provider for type-safe queries
- Aggregation pipeline builder
- Change streams for real-time notifications
- GridFS for large file storage
- **Vector search index support** (v3.6) -- `CreateVectorSearchIndexModel` and auto-embedding

**Current version:** MongoDB.Driver 3.8.0 (May 2026). Cephalon's MongoDB-facing packages also keep `SharpCompress` pinned as a direct dependency so NuGet audit resolves the non-vulnerable compression package version in package consumers and release publish probes.

**Framework relevance:** CephalonEngine has `Cephalon.Data.MongoDB` and `Cephalon.EventSourcing.MongoDB`.

---

### StackExchange.Redis

**What it does:** High-performance .NET client for Redis, Garnet, Valkey, AWS ElastiCache, and Azure Managed Redis.

**Key features:**
- Multiplexed connection model (single connection, concurrent commands)
- Pub/Sub, Lua scripting, transactions, pipelining
- Sentinel and Cluster mode support
- `ConfigurationOptions` for fine-grained connection tuning

**Current version:** StackExchange.Redis 2.11.8.

**Microsoft.Extensions.Caching.StackExchangeRedis:** Provides `IDistributedCache` implementation backed by Redis. Current version 9.0.10. Bridges Redis into the standard .NET caching abstraction.

**Framework relevance:** CephalonEngine has `Cephalon.Data.Redis`. The engine should ensure the Redis provider supports both raw StackExchange.Redis operations and the `IDistributedCache` abstraction.

---

## 5. HTTP and API

### ASP.NET Core Minimal APIs (.NET 10)

**What it does:** Lightweight HTTP endpoint routing without controllers. The recommended approach for new APIs.

**Key .NET 10 features:**
- **Built-in validation** -- `builder.Services.AddValidation()` with DataAnnotations on query, header, and body parameters
- **Record type validation** support
- **Server-Sent Events** -- `TypedResults.ServerSentEvents` for streaming
- **OpenAPI 3.1** with JSON Schema draft 2020-12
- **XML doc comment** integration into OpenAPI via source generator
- **`IOpenApiDocumentProvider`** in DI for programmatic OpenAPI access
- **`IProblemDetailsService`** customization for validation errors
- **YAML OpenAPI** document support
- **Endpoint-specific OpenAPI operation transformers**

**Framework relevance:** `Cephalon.AspNetCore` already references `Microsoft.AspNetCore.OpenApi`. The engine's HTTP layer should leverage the new validation and SSE features.

---

### Carter

**What it does:** Module pattern for organizing Minimal APIs. Provides `ICarterModule` as an organizational unit.

**Key features:**
- `ICarterModule` interface for grouping related endpoints
- Automatic module discovery via assembly scanning
- FluentValidation integration
- Model binding helpers

**When to use:** When you want module-based organization of Minimal APIs without the full MVC controller overhead. Good for modular monoliths where each module exposes its own routes.

**Framework relevance:** CephalonEngine's module system (`ICephalonModule`) serves a similar purpose. Carter's approach could inspire the engine's HTTP endpoint registration patterns.

---

### FastEndpoints

**What it does:** Implementation of the REPR (Request-Endpoint-Response) design pattern for ASP.NET Core.

**Key features:**
- One class per endpoint (no controllers)
- Built-in request validation (FluentValidation integration)
- Pre/Post processor pipeline
- Command bus for in-process messaging
- Swagger/OpenAPI support
- JWT and cookie authentication helpers

**When to use:** When you want the structure of the REPR pattern with high performance. Good alternative to MVC controllers that provides strong endpoint isolation.

**Current version:** FastEndpoints 5.x. Very active development.

---

### Refit

**What it does:** Automatic type-safe REST client generation from interface definitions.

**Key features:**
- Define interfaces with HTTP attributes (`[Get]`, `[Post]`, etc.)
- **Source generator** (Refit 9.0+) -- compile-time client generation, no runtime reflection
- **AOT and trimming support** for .NET 10+
- Integrates with `HttpClientFactory`
- Supports multipart, form data, streaming

**When to use:** For typed HTTP client calls to external APIs. Eliminates boilerplate `HttpClient` code.

**Current version:** Refit 9.0.2 (November 2025). AOT-compatible.

---

### gRPC (.NET)

**What it does:** High-performance RPC framework using Protocol Buffers for service definition and binary serialization.

**Key features:**
- **Code generation** from `.proto` files
- **Bidirectional streaming** (unary, server-streaming, client-streaming, bidirectional)
- **`Grpc.Net.ClientFactory`** -- integrates with DI and `HttpClientFactory`
- **gRPC-Web** support for browser clients
- **Deadline/cancellation** propagation
- **Interceptors** for cross-cutting concerns

**Current version:** Grpc.Net.Client 2.76.0; Grpc.Net.ClientFactory 2.76.0.

**Framework relevance:** CephalonEngine has `Cephalon.AspNetCore.Grpc`. The test project already references `Grpc.Net.Client`.

---

### HotChocolate (GraphQL)

**What it does:** Open-source GraphQL server for .NET. Compliant with GraphQL October 2021 spec + drafts.

**Key features:**
- **Schema-first and code-first** approaches
- **Filtering, sorting, projections, pagination** as middleware attributes
- **DataLoader** for batching and caching
- **Subscriptions** (WebSocket, SSE)
- **Federation** (Fusion) for distributed data graphs
- **Nitro IDE** middleware
- **Stand-alone, serverless (Azure Function / AWS Lambda), and gateway** deployment

**Current version:** HotChocolate 15.1.13 (April 2026). V16 documentation is available.

**Framework relevance:** CephalonEngine has `Cephalon.AspNetCore.GraphQL`.

---

### SignalR

**What it does:** Real-time bidirectional communication over WebSockets (with SSE and Long Polling fallbacks).

**Key features:**
- **Hub-based** programming model
- **Groups** and **user-based** message targeting
- **Streaming** (server-to-client and client-to-server)
- **MessagePack** binary protocol option (in addition to JSON)
- **Automatic reconnection** and connection management
- **Azure SignalR Service** for scaling

**Use cases:** Dashboards, live notifications, collaborative editing, gaming, chat, GPS tracking.

**Current version:** Ships in-box with ASP.NET Core 10.

---

## 6. Serialization

### System.Text.Json

**What it does:** The built-in, high-performance JSON serializer for .NET.

**Key features:**
- **Source generators** (`JsonSerializerContext`) -- compile-time serialization for AOT/trimming/startup performance
- **Polymorphism** -- `[JsonDerivedType]` attribute with type discriminators (JSON, not source-gen fast path)
- **Custom converters** -- `JsonConverter<T>` (basic) and `JsonConverterFactory` (open-generic/enum)
- **Number handling** -- `AllowReadingFromString` (default in ASP.NET Core)
- **Streaming** -- `JsonSerializer.SerializeAsync` / `DeserializeAsync` with `Stream`
- **DOM** -- `JsonNode`, `JsonObject`, `JsonArray` for schema-less manipulation

**Limitations with source generators:**
- Polymorphism is supported in metadata-based source gen but NOT fast-path source gen
- Custom converters and type discrimination are not fully combinable with source generators

**Framework relevance:** CephalonEngine should standardize on `System.Text.Json` with source generators for its serialization layer. Custom converters are needed for domain types (e.g., strongly-typed IDs, discriminated unions).

---

### MessagePack for C#

**What it does:** Extremely fast binary serializer for .NET. 10x faster than MsgPack-Cli.

**Key features:**
- Zero-allocation serialization paths
- **LZ4 compression** built-in
- Full C# type system support (including nulls, unlike Protobuf)
- **Source generator** (`MessagePackAnalyzer`) for compile-time code generation
- Union types (discriminated unions)
- Contractless mode (no attributes required)

**When to use:** Inter-service communication where performance matters more than human readability. Event store payload serialization. SignalR binary protocol.

**Current version:** MessagePack-CSharp 3.x. Actively maintained.

---

### protobuf-net

**What it does:** Protocol Buffers implementation for .NET without requiring `.proto` files.

**Key features:**
- Attribute-based mapping (`[ProtoContract]`, `[ProtoMember]`)
- Compatible with Google's protobuf wire format
- Inheritance support
- Code-first approach (no `.proto` file needed)

**Limitations:**
- No null representation (protobuf limitation)
- Empty collections treated as null
- Slightly larger message sizes than MessagePack due to field tags

**When to use:** When interoperating with non-.NET systems that use Protocol Buffers, or when you want the protobuf wire format without maintaining `.proto` files.

**Current version:** protobuf-net 3.x.

---

## 7. Resilience and HTTP

### Microsoft.Extensions.Http.Resilience + Polly v8

**What it does:** Official resilience layer for `HttpClient` in .NET, built on top of Polly v8.

**Key features:**
- **Resilience strategies:** Retry, Circuit Breaker, Timeout, Rate Limiter, Fallback, Hedging
- **`AddStandardResilienceHandler()`** -- stacks multiple strategies with sensible defaults
- **`AddStandardHedgingHandler()`** -- sends parallel requests to reduce tail latency
- **Built-in telemetry** with OpenTelemetry
- **Fluent pipeline builder** API from Polly v8

**Polly v8 (standalone) strategies:**
- `RetryStrategyOptions` -- configurable retries with jitter
- `CircuitBreakerStrategyOptions` -- half-open/open/closed states
- `TimeoutStrategyOptions` -- per-request and overall timeouts
- `BulkheadStrategyOptions` -- concurrency limiting (renamed to `ConcurrencyLimiter` in v8)
- `FallbackStrategyOptions` -- graceful degradation
- `HedgingStrategyOptions` -- speculative execution

**Migration note:** `Microsoft.Extensions.Http.Polly` is **deprecated**. Use `Microsoft.Extensions.Http.Resilience` instead.

**Framework relevance:** CephalonEngine should provide built-in resilience configuration for its HTTP client registrations, using the standard resilience handler by default.

---

### HttpClientFactory Patterns

**Key patterns:**
- **Named clients** -- `services.AddHttpClient("github", ...)` with per-client configuration
- **Typed clients** -- `services.AddHttpClient<GitHubClient>()` for DI-friendly typed access
- **Refit integration** -- `services.AddRefitClient<IGitHubApi>()` for code-gen clients
- **Message handlers** -- `DelegatingHandler` pipeline for auth, logging, correlation
- **Resilience** -- chain `.AddStandardResilienceHandler()` onto any client registration

---

## 8. Background Processing

### IHostedService / BackgroundService

**What it does:** Built-in .NET abstraction for background tasks that run alongside the application host.

**Key features:**
- `BackgroundService` base class with `ExecuteAsync(CancellationToken)` override
- Automatic lifecycle management (start/stop with host)
- Graceful shutdown via cancellation token
- Multiple services run concurrently

**When to use:** Simple background loops, queue consumers, periodic tasks. No persistence or scheduling.

---

### System.Threading.Channels

**What it does:** Thread-safe, async-native producer/consumer data structures.

**Key features:**
- **`Channel.CreateBounded<T>(capacity)`** -- backpressure when full
- **`Channel.CreateUnbounded<T>()`** -- unbounded, fastest path
- **`ChannelReader<T>`** / **`ChannelWriter<T>`** -- separate read/write concerns
- Supports `IAsyncEnumerable` consumption
- Zero-allocation for many patterns

**When to use:** Decoupling fast HTTP request paths from slow background processing. In-process message queues. Signal/event buffering.

**Framework relevance:** Channels are the ideal primitive for CephalonEngine's internal event dispatching -- buffer events from the command side and consume them in background processors.

---

### Hangfire

**What it does:** Persistent background job framework with dashboard, retries, and scheduling.

**Key features:**
- **Fire-and-forget jobs** -- `BackgroundJob.Enqueue(() => DoWork())`
- **Delayed jobs** -- schedule for future execution
- **Recurring jobs** -- cron-based scheduling
- **Continuations** -- chain jobs
- **Dashboard** for monitoring and management
- **Storage backends:** SQL Server, PostgreSQL, Redis, MongoDB

**When to use:** Web applications needing persistent job scheduling with a UI dashboard. Email sending, report generation, data synchronization.

**Current version:** Hangfire 1.8.x (updated February 2026).

---

### Quartz.NET

**What it does:** Enterprise-grade job scheduling library. Port of Java's Quartz.

**Key features:**
- **Cron expressions** with timezone awareness
- **Job clustering** -- multiple nodes, single executor
- **Calendar exclusions** (holidays, blackout periods)
- **Misfire handling** policies
- **Persistent job store** (SQL Server, PostgreSQL)
- **Listener model** for job/trigger/scheduler events

**When to use:** Complex scheduling requirements -- timezone-aware cron, holiday exclusions, clustered execution, job overlap prevention.

**Current version:** Quartz.NET 3.x.

---

## 9. Validation

### FluentValidation

**What it does:** Strongly-typed validation rules using a fluent API.

**Key features:**
- `AbstractValidator<T>` with chainable rule builders
- Complex rules: `Must()`, `When()`, `Unless()`, `SetValidator()` for nested objects
- **Async validation** support
- **DI integration** via `FluentValidation.DependencyInjectionExtensions`
- Custom validators and property validators

**IMPORTANT:** `FluentValidation.AspNetCore` is **deprecated**. Use endpoint filters (Minimal APIs) or manual validation instead of the auto-validation pipeline.

**Current version:** FluentValidation 12.1.1. Supports .NET 8+.

---

### DataAnnotations

**What it does:** Attribute-based validation using `System.ComponentModel.DataAnnotations`.

**Key features:**
- `[Required]`, `[Range]`, `[StringLength]`, `[RegularExpression]`, `[EmailAddress]`
- `[CustomValidation]` for complex rules
- `IValidatableObject` interface for model-level validation
- Supported by EF Core, ASP.NET MVC, Blazor, and now Minimal APIs

---

### Microsoft.Extensions.Validation (.NET 10) -- NEW

**What it does:** New framework validation package that extracts validation logic from ASP.NET Core into a standalone package.

**Key features:**
- **`builder.Services.AddValidation()`** -- registers validation services
- Automatic endpoint filter for Minimal APIs
- Validates query strings, headers, route parameters, and request body
- Uses DataAnnotations (`[Required]`, `[Range]`, etc.) and `IValidatableObject`
- **Record type** support
- **`IProblemDetailsService`** integration for custom error responses
- **`DisableValidation()`** per-endpoint opt-out
- Usable **outside** ASP.NET Core HTTP scenarios

**Experimental features:** `[ValidatableType]` and `[SkipValidation]` attributes are marked experimental in .NET 10.

**Current version:** Microsoft.Extensions.Validation 10.0.x.

**Framework relevance:** CephalonEngine should integrate with `Microsoft.Extensions.Validation` for its behavior pipeline validation, allowing module authors to use DataAnnotations on command/query types that get validated before handler execution.

---

## 10. Testing

### xUnit.net v3

**What it does:** The most popular .NET test framework. v3 is a major rewrite.

**Key v3 features:**
- **`AssemblyFixtureAttribute`** -- assembly-wide fixtures (created before any test, disposed after all)
- **`TestContext`** -- pipeline state + cancellation token for downstream methods
- **Query filter language** (`-filter`) for complex test filtering
- **Immutable collection assertions** (`System.Collections.Immutable` support)
- **Failure cause tracking** (Assertion, Timeout, Exception)
- **NativeAOT-compatible** assertion library (`xunit.v3.assert.aot`)
- **`dotnet test` integration** with Microsoft Testing Platform (MTP v2)
- New templates: `dotnet new xunit3`

**Current version:** xunit.v3 3.2.2 (January 2026).

**Framework relevance:** CephalonEngine's test projects already use xUnit. Upgrade path to v3 should be planned.

---

### NUnit

**What it does:** Alternative test framework with a rich assertion model and attribute-based test configuration.

**Key features:**
- `Assert.That(...)` constraint-based assertions
- `[TestCase]`, `[TestCaseSource]` for parameterized tests
- `[SetUp]`, `[TearDown]`, `[OneTimeSetUp]`, `[OneTimeTearDown]`
- Parallel test execution

**Current version:** NUnit 4.x.

---

### Mocking: Moq vs NSubstitute vs FakeItEasy

| Feature | Moq | NSubstitute | FakeItEasy |
|---------|-----|-------------|------------|
| Market share | ~70% | ~25% | ~15% |
| Syntax style | Lambda-based `.Setup()` | Direct method calls (cleanest) | `A.CallTo(...)` |
| `.Object` needed | Yes | No | No |
| Async support | `.ReturnsAsync()` | `.Returns()` (handles both) | `.Returns()` |
| Controversy | SponsorLink bundling | None | None |

**Recommendation for new projects:** NSubstitute (cleanest syntax, no controversy) or FakeItEasy (simplest API).

---

### TestContainers

**What it does:** Manages Docker containers for integration tests -- databases, message brokers, etc.

**Key features:**
- Pre-built modules: PostgreSQL, SQL Server, MongoDB, Redis, RabbitMQ, Kafka, Elasticsearch
- Automatic container lifecycle (create, start, stop, remove)
- xUnit v3 integration (`Testcontainers.XunitV3`)
- Random port mapping for parallel test execution
- Resource reaper for cleanup

**Current version:** Testcontainers.NET (latest). .NET Standard 2.0+.

---

### Verify (Snapshot Testing)

**What it does:** Snapshot testing that serializes test results and compares to stored baselines.

**Key features:**
- Supports xUnit, NUnit, MSTest
- Automatic `.verified.txt` file management
- Diff tool integration (Beyond Compare, VS Code, etc.)
- Scrubbers for non-deterministic data (GUIDs, dates)
- Supports .NET 10 (`net10`)

**Current version:** Latest Verify package. Active development.

---

### Bogus

**What it does:** Fake data generator for .NET. Port of faker.js.

**Key features:**
- `Faker<T>` for generating typed fake objects
- Locale support (names, addresses by country)
- Deterministic generation via seed
- Rich API: `f.Name.FullName()`, `f.Internet.Email()`, `f.Commerce.Price()`

---

### Respawn

**What it does:** Intelligent database reset for integration tests.

**Key features:**
- Deletes data from tables in correct foreign-key order
- Much faster than dropping/recreating databases
- Checkpoint model: configure once, reset repeatedly
- Supports SQL Server, PostgreSQL, MySQL

---

### Alba

**What it does:** Integration testing helper for ASP.NET Core. Wraps `TestServer` with a declarative API.

**Key features:**
- Declarative scenario syntax for HTTP assertions
- Works with both `Startup.cs` and `WebApplicationBuilder` patterns
- Response body assertions, header checks, status code validation
- Integrates with Wolverine for testing message-driven endpoints

**Current version:** Alba 8.5.2. Supports .NET 8+.

---

### WebApplicationFactory

**What it does:** Built-in ASP.NET Core test host for integration testing.

**Key features:**
- Creates in-memory `TestServer`
- Override services, configuration, and logging for tests
- `HttpClient` creation for end-to-end HTTP testing
- `WithWebHostBuilder()` for per-test customization

**Framework relevance:** CephalonEngine tests already use `Microsoft.AspNetCore.TestHost`. Alba provides a nicer API on top of `WebApplicationFactory`.

---

## 11. Observability

### OpenTelemetry .NET

**What it does:** Vendor-neutral observability framework for traces, metrics, and logs.

**.NET implementation architecture:** Unlike other platforms, .NET provides native APIs that OpenTelemetry builds on:
- **Traces:** `System.Diagnostics.ActivitySource` (= OTel Tracer) + `Activity` (= OTel Span)
- **Metrics:** `System.Diagnostics.Metrics.Meter` (= OTel Meter)
- **Logs:** `Microsoft.Extensions.Logging.ILogger` (= OTel Logger)

**Key packages:**
- `OpenTelemetry.Extensions.Hosting` -- host integration
- `OpenTelemetry.Exporter.OpenTelemetryProtocol` -- OTLP export
- `OpenTelemetry.Instrumentation.AspNetCore` -- automatic HTTP tracing
- `OpenTelemetry.Instrumentation.Http` -- HttpClient tracing (optional on .NET 9+)

**.NET 10 additions:**
- **Telemetry schema URLs** on `ActivitySource` and `Meter`
- **Out-of-proc trace support** for Activity events and links
- **Rate-limit trace-sampling** support
- New `ActivitySourceOptions` for simplified construction

**Framework relevance:** CephalonEngine has `Cephalon.Observability.OpenTelemetry` which already references the key OTel packages. The engine should emit activities from its behavior pipeline, event sourcing operations, and module lifecycle events.

---

### Serilog

**What it does:** Structured logging library for .NET. 2.5B+ NuGet downloads.

**Key features:**
- **Structured events** -- key-value pairs, not just string messages
- **Sinks:** Console, File, Seq, Elasticsearch, Application Insights, Datadog, and 100+ more
- **Enrichers:** Add context properties (machine name, thread ID, correlation ID)
- **`Serilog.Extensions.Hosting`** -- integrates with Generic Host
- **`Serilog.Settings.Configuration`** -- configure from `appsettings.json`
- **Message templates** with destructuring (`{@Object}`, `{$Type}`)

**Current version:** Serilog 4.x. `Serilog.Extensions.Hosting` 9.x.

**Framework relevance:** CephalonEngine has `Cephalon.Observability.Serilog` which references `Serilog.Extensions.Hosting` and `Serilog.Settings.Configuration`.

---

### Seq

**What it does:** Structured log aggregation and analysis server. First-class Serilog integration.

**Key features:**
- Real-time log streaming with structured queries
- Dashboards and alerts
- API key-based authentication for clients
- Self-hosted or Seq Cloud
- CLEF (Compact Log Event Format) support

---

### System.Diagnostics Primitives

| Type | OTel Equivalent | Purpose |
|------|----------------|---------|
| `ActivitySource` | Tracer | Creates spans/activities |
| `Activity` | Span | Represents a unit of work |
| `Meter` | Meter | Creates metric instruments |
| `Counter<T>` | Counter | Monotonically increasing value |
| `Histogram<T>` | Histogram | Distribution of values |
| `UpDownCounter<T>` | UpDownCounter | Value that can increase or decrease |
| `DiagnosticSource` | N/A | Rich payload event source (legacy, use ActivitySource) |
| `ActivityListener` | SpanProcessor | Observe/filter activities |

**Best practices:**
- Create `ActivitySource` as `static readonly` -- expensive to create, reuse throughout lifetime
- Use hierarchical names: `CephalonEngine.Behaviors`, `CephalonEngine.EventSourcing`
- Activity is only created if listeners are subscribed (zero cost when not observed)

---

## 12. Source Generators and Compile-Time

### Roslyn Incremental Source Generators

**What it does:** Compile-time code generation using the Roslyn compiler platform.

**Key concepts:**
- **`IIncrementalGenerator`** (Roslyn 4.x+) -- the ONLY generator API to use for .NET 7+
- **Pipeline model:** Declare transformation graph; Roslyn caches and replays incrementally
- **`ForAttributeWithMetadataName`** -- preferred entry point when targeting attributes
- **`CreateSyntaxProvider`** -- fallback when triggering on syntax structure without attributes

**Framework relevance:** CephalonEngine already has `Cephalon.Behaviors.SourceGen` (targets `netstandard2.0`, references `Microsoft.CodeAnalysis.CSharp`, has `EnforceExtendedAnalyzerRules` and `IsRoslynComponent`). This is the correct setup for an incremental source generator.

**NOTE:** `ISourceGenerator` (the v1 API) is deprecated. The Roslyn team blocks access to older APIs after Roslyn 4.10.0 / .NET 9. Always use `IIncrementalGenerator`.

---

### Compile-Time DI Registration

Source generators can scan for types with specific attributes and emit registration code:

```
// Generator discovers [RegisterService] types and emits:
public static class GeneratedServiceRegistration
{
    public static IServiceCollection AddGeneratedServices(this IServiceCollection services)
    {
        services.AddSingleton<IFoo, Foo>();
        services.AddScoped<IBar, Bar>();
        return services;
    }
}
```

This eliminates manual `AddSingleton`/`AddScoped` calls and provides compile-time verification of registrations.

---

### Popular Source Generator Packages

| Package | Purpose |
|---------|---------|
| **Mapperly** | Object mapping (compile-time AutoMapper alternative) |
| **StronglyTypedId** | Generate strongly-typed ID wrappers |
| **RapidEnum** | Fast enum utilities (zero allocation) |
| **MemoryPack** | Zero-encoding binary serializer |
| **Refit** | REST client generation from interfaces |
| **System.Text.Json** | JSON serialization context generation |
| **Facet** | DTO/ViewModel scaffolding with typed LINQ projections |

---

## 13. Middleware and Pipeline Libraries

### System.IO.Pipelines

**What it does:** High-performance I/O primitive for processing streams of bytes.

**Key features:**
- **`Pipe`** -- connects `PipeWriter` (producer) and `PipeReader` (consumer)
- **Back-pressure** via `PauseWriterThreshold` / `ResumeWriterThreshold`
- **`PipeScheduler`** -- controls which threads handle async callbacks
- **Zero-copy** where possible (works with `Memory<byte>`, `ReadOnlySequence<byte>`)
- Used internally by Kestrel, SignalR, and gRPC

**When to use:** Custom protocol implementations, high-throughput byte stream processing, network servers. Not for application-level middleware.

**Current version:** System.IO.Pipelines 10.0.0 (ships with .NET 10).

---

### Scrutor

**What it does:** Assembly scanning and decoration extensions for `Microsoft.Extensions.DependencyInjection`.

**Key features:**
- **Assembly scanning** -- `services.Scan(scan => scan.FromAssemblyOf<T>().AddClasses(...).AsImplementedInterfaces())`
- **Service decoration** -- `services.Decorate<IService, DecoratedService>()` -- wraps existing registrations
- Convention-based registration (match by namespace, suffix, interface)

**When to use:** When you need assembly scanning or the Decorator pattern with the built-in DI container. Alternative to Autofac's scanning when you want to stay on the built-in container.

**Current version:** Scrutor 7.0.0 (November 2025). 3M+ downloads.

**Framework relevance:** CephalonEngine's module discovery could use Scrutor for scanning module assemblies for handler types, behavior implementations, and projection registrations.

---

### Middleware Pattern in CephalonEngine Context

The middleware/pipeline pattern appears in multiple layers:

1. **ASP.NET Core Middleware** -- `app.Use(...)` for HTTP request pipeline
2. **MediatR-style Pipeline Behaviors** -- `IPipelineBehavior<TRequest, TResponse>` for command/query handling
3. **Wolverine Middleware** -- handler pipeline with before/after semantics
4. **EF Core Interceptors** -- `ISaveChangesInterceptor`, `IDbCommandInterceptor`
5. **CephalonEngine Behaviors** -- the engine's own pipeline via `Cephalon.Behaviors`

Each layer provides cross-cutting concern injection (logging, validation, transactions, auth) at different levels of the stack.

---

## 14. Cloud-Native Orchestration: Aspire

Aspire is Microsoft's cloud-native developer stack. Originally released as ".NET Aspire" alongside .NET 8 in November 2023, Aspire 13.0 (November 2025) rebranded to plain "Aspire" with explicit polyglot ambitions: although still primarily a .NET-first product, the AppHost programming model now contemplates Python, Node, and (in 13.2+ preview) TypeScript-authored app hosts. Cadence has tracked .NET majors plus three-to-four mid-year point releases.

### Aspire.Hosting (AppHost) and Aspire.AppHost.Sdk

**What it does:** Defines the application composition graph in code. An *AppHost* project (a console app using `Aspire.Hosting`) declares the resources that make up the distributed application -- ASP.NET services, worker projects, Postgres / Redis / Kafka / MongoDB containers, Dapr sidecars, project-to-project references, environment variables, and connection strings. The AppHost runs the composition locally for F5 / `dotnet run` development and emits manifests for production publishing.

**Key features:**
- `IDistributedApplicationBuilder` programmatic composition: `builder.AddProject<Projects.Api>("api")`, `builder.AddPostgres("db")`, `builder.AddRedis("cache")`, `builder.AddKafka("events")`, and so on
- automatic OpenTelemetry wiring between resources (tracing, metrics, logs)
- service discovery and connection-string injection -- `db.GetConnectionString("Database")` resolves to the actual local or production endpoint
- secret management via `IResourceWithConnectionString` plus user-secrets / Azure Key Vault providers
- publishers (`AddDockerComposePublisher()`, `AddKubernetesPublisher()`, `AddAzureContainerAppsPublisher()`) generate deployment manifests from the same graph
- the Aspire CLI (`aspire run`, `aspire publish`, `aspire add`) is the supported developer entry point as of Aspire 13.1+

**Framework relevance to CephalonEngine:** Aspire is *not* a runtime that Cephalon depends on, but it is the dominant local-dev / orchestration story for new .NET projects in the 2025-2026 wave. Three integration angles for the engine to consider:
1. Cephalon's templates / scaffolding could emit an opt-in Aspire AppHost project alongside the generated host, so adopters get the Aspire dashboard, OTEL wiring, and Postgres/Redis/Kafka containerization without writing AppHost code themselves
2. Cephalon's runtime catalogs (`/engine/*`, `snapshot.*`) and the diagnostics-conventions surface already align with Aspire's OpenTelemetry assumptions; minor work on attribute naming would let the Aspire dashboard render Cephalon-specific spans/metrics first-class
3. Aspire publishers (Docker Compose, Kubernetes manifests, Azure Container Apps) are an additive deployment surface that can sit *alongside* the existing `azure-container-apps-deployment.md` / `kubernetes-deployment.md` / `iis-deployment.md` / `linux-systemd-deployment.md` / `windows-service-deployment.md` guidance rather than replace it

**Current version:** Aspire 13.3 (April 2026). Aspire 13.2 added TypeScript AppHost preview and stable Docker Compose publishing. Aspire 13.0 dropped the ".NET" prefix and introduced the polyglot platform; Aspire 13.1 (January 2026) added MCP integration and CLI enhancements.

---

### Aspire.ServiceDefaults

**What it does:** Shared extension method (`builder.AddServiceDefaults()`) injected into every service project to apply consistent observability, health-check, service-discovery, and resilience configuration. Generated by the Aspire template into a `*.ServiceDefaults` library that every Aspire-managed service references.

**Key features:**
- OpenTelemetry tracing, metrics, and logs configured with sensible defaults
- standard `/health` and `/alive` health-check endpoints
- service discovery via `Microsoft.Extensions.ServiceDiscovery`
- HTTP resilience handlers via `Microsoft.Extensions.Http.Resilience` (Polly v8 under the hood)

**Framework relevance:** Cephalon already owns `/engine/runtime-story`, `/engine/diagnostics`, `/engine/dependencies`, and the diagnostics-conventions surface, plus first-class Polly v8 integration through `Cephalon.Resilience` and `Cephalon.Behaviors`. The ServiceDefaults pattern is a useful template for the engine's own host-bootstrapping conventions and worth referencing when documenting "what a Cephalon host gives you out of the box" against the broader ecosystem.

**Current version:** Tracks the Aspire release that generated the template; the *content* is mostly a few lines of standard `Microsoft.Extensions.*` configuration so it has no independent version cadence.

---

### Aspire integration packages

**What they do:** Resource-specific NuGet packages that teach the AppHost how to spin up and connect to a given backing service. Each integration ships in two halves: a *hosting* package (consumed by the AppHost, e.g. `Aspire.Hosting.PostgreSQL`) and a *client* package (consumed by the service, e.g. `Aspire.Npgsql` or `Aspire.Microsoft.EntityFrameworkCore.SqlServer`).

**Coverage as of Aspire 13.3:** PostgreSQL, SQL Server, MySQL, Oracle, MongoDB, Redis / Garnet / Valkey, Kafka, RabbitMQ, NATS, Elasticsearch, Qdrant, Milvus, Azure (Service Bus, Storage, Cosmos DB, Key Vault, OpenAI, AI Search, Event Hubs), AWS (CDK + service primitives), Dapr, Keycloak, MailDev, Ollama, and several others. The Aspire Community Toolkit adds further integrations.

**Framework relevance:** Cephalon already publishes provider packages for many of the same backing services (`Cephalon.Data.SqlServer`, `Cephalon.Data.Postgres`, `Cephalon.Data.MongoDB`, `Cephalon.Data.Redis`, `Cephalon.Data.Cassandra`, `Cephalon.Data.ClickHouse`, `Cephalon.Data.Elasticsearch`, `Cephalon.Data.OpenSearch`, `Cephalon.Data.Neo4j`, `Cephalon.Data.Qdrant`, `Cephalon.Data.Nats`). An Aspire-integration companion family (e.g. `Cephalon.Aspire.Data.Postgres`) is a plausible additive surface that would let an Aspire AppHost declare a Cephalon-aware database resource and have the Cephalon runtime catalog / governance / migration policy picked up automatically. This is *not* a near-term commitment, but it's the natural shape if Aspire continues to dominate as the local-dev / orchestration story.

---

### Aspire Dashboard

**What it does:** Standalone web application bundled with the Aspire workload that renders distributed-tracing, metrics, structured logs, console output, and resource state across every resource declared by the AppHost. Runs locally during `aspire run` and is also deployable as a standalone container against any OTLP-emitting workload (Cephalon-based or not).

**Framework relevance:** Because the dashboard consumes OTLP, any Cephalon host that emits OpenTelemetry signals (which every Cephalon host already does through `Cephalon.Observability` and `Cephalon.Observability.OpenTelemetry`) renders in the Aspire dashboard for free. Worth calling out in the observability provider authoring guidance so adopters know they don't need a separate APM to start.

**Current version:** Ships with the Aspire workload; can be pulled standalone as `mcr.microsoft.com/dotnet/aspire-dashboard` container.

---

## 15. AI and Agent Libraries

The .NET AI stack consolidated rapidly between mid-2024 and early 2026. The shape that has settled out:

- **Microsoft.Extensions.AI** is the provider-neutral abstraction layer (`IChatClient`, `IEmbeddingGenerator`)
- **Microsoft Agent Framework (MAF)** is the production-ready agent / orchestration layer, succeeding both Semantic Kernel and the .NET port of AutoGen for new agent work
- **Semantic Kernel** remains actively maintained but is no longer the recommended starting point for greenfield agent applications
- Provider SDKs (`OpenAI`, `Azure.AI.OpenAI`, `Azure.AI.Inference`, `Anthropic.SDK`, `Mistral.SDK`, `Ollama.NET`) plug into the M.E.AI abstractions

### Microsoft.Extensions.AI

**What it does:** Unified abstractions for AI model interaction, modeled after the rest of the `Microsoft.Extensions.*` family. Centers on `IChatClient` (chat completion, streaming, multi-modal content, tool / function calling) and `IEmbeddingGenerator<TInput, TEmbedding>` (vector embeddings). Vendor-specific implementation packages (`Microsoft.Extensions.AI.OpenAI`, `Microsoft.Extensions.AI.AzureAIInference`, `Microsoft.Extensions.AI.Ollama`, etc.) adapt provider SDKs to the common interface.

**Key features:**
- provider-neutral `IChatClient` / `IEmbeddingGenerator` so consumer code does not bind to a specific model vendor
- middleware-style pipeline (`UseFunctionInvocation()`, `UseDistributedCache()`, `UseLogging()`, `UseOpenTelemetry()`) for cross-cutting AI concerns
- first-class function / tool calling with automatic invocation
- streaming responses with structured chunk types
- targets `net8.0`, `net9.0`, `net10.0`, `netstandard2.0`, `net462+` so the abstraction layer is portable to almost any host

**Framework relevance to CephalonEngine:** This is the abstraction layer `Cephalon.Agentics` should align with for any model-side concerns. The existing agentics tool-execution and run-state baseline (ENG-232 / ENG-Agentics roadmap) is a higher-level concept than M.E.AI -- M.E.AI sits below it, providing the actual LLM transport. A future `Cephalon.Agentics.MicrosoftExtensionsAI` companion is the natural shape: Cephalon owns tool descriptors, run catalogs, approval / safety gating, and policy contracts, while M.E.AI owns the actual chat / embedding I/O.

**Current version:** `Microsoft.Extensions.AI` 10.5.x stable (May 2026). API surface stabilized in late 2025 and is now considered production-ready.

---

### Microsoft Agent Framework (MAF)

**What it does:** Production-ready agent / multi-agent orchestration framework, released as Microsoft Agent Framework 1.0 in April 2026. Combines the lessons from Semantic Kernel (planner / plugin model) and AutoGen (multi-agent collaboration) into a single supported framework with long-term-support commitments. Provides agent abstractions, conversation / thread state, tool / function calling, agent-to-agent messaging, workflow orchestration, and an evaluation framework.

**Key features:**
- agent abstractions over any M.E.AI `IChatClient`
- typed conversation state with persistence hooks
- multi-agent orchestration patterns (sequential, group chat, handoff, supervisor)
- workflow / process integration for long-running agent loops
- evaluation framework for measuring agent quality across versions
- production-ready stability and LTS commitment

**Framework relevance:** MAF and `Cephalon.Agentics` solve different problems but live in the same space. MAF owns *how the agent reasons and orchestrates* (planner, tools, memory, multi-agent topology); Cephalon.Agentics owns *how the host platform governs the agent* (run catalogs, idempotency, retry, approval, audit, runtime-state, terminal-failure posture, operator observability). The natural integration shape is an opt-in companion (`Cephalon.Agentics.MicrosoftAgentFramework` or similar) where MAF runs the agent loop and Cephalon.Agentics owns the bounded, observable, replay-safe execution wrapper. This stays consistent with Cephalon's "host-agnostic core, additive companion packs" principle in [`long-range-direction.md`](long-range-direction.md).

**Current version:** Microsoft Agent Framework 1.0 GA (April 2026). The .NET package surface ships in parallel with the Python release.

---

### Semantic Kernel

**What it does:** Microsoft's earlier-generation AI orchestration framework. Provides `Kernel`, `KernelFunction`, plugin / skill abstractions, planners (function-calling planner, Handlebars planner, Stepwise planner), memory connectors, and a Process Framework for long-running workflows. Still actively maintained but no longer the recommended starting point for new agent work; new investment is concentrated in MAF.

**Framework relevance:** Mostly historical / migration concern. Adopters who already have Semantic Kernel codebases will keep them working; new Cephalon-shaped agent integrations should target M.E.AI + MAF instead. Documenting the SK boundary clearly is helpful so adopters do not mistake SK for the current Microsoft direction.

**Current version:** `Microsoft.SemanticKernel` 1.76.0 (May 11, 2026). Targets `net8.0` and `netstandard2.0`.

---

### Provider SDKs

**OpenAI .NET SDK (`OpenAI`):** Official SDK published by the OpenAI organization since 2024, replacing the older `Azure.AI.OpenAI` for OpenAI-direct usage. Supports chat completions, embeddings, image generation, audio, assistants, and the Responses API. Plugs into `Microsoft.Extensions.AI` through `Microsoft.Extensions.AI.OpenAI`.

**Azure.AI.OpenAI:** Azure-flavored OpenAI client; uses Azure AD authentication, regional endpoints, and Azure-specific governance. Continues to ship alongside the OpenAI SDK for Azure OpenAI Service users.

**Azure.AI.Inference:** Azure Foundry / Azure AI Studio inference SDK, used for non-OpenAI models served through Azure AI. Plugs into `Microsoft.Extensions.AI` through `Microsoft.Extensions.AI.AzureAIInference`.

**Anthropic SDK / Mistral SDK / Ollama .NET:** Community and vendor SDKs for non-Microsoft providers. Most ship M.E.AI adapters so consumer code stays vendor-neutral at the `IChatClient` boundary.

**Framework relevance:** No direct Cephalon dependency. The M.E.AI abstraction is the contract the engine should align with; specific provider SDKs are an implementation detail that adopters select.

---

### AutoGen.NET

**What it does:** .NET port of the original AutoGen multi-agent framework. Multi-agent conversations, group chat patterns, code-execution agents. Maintenance-only as of 2026; new investment redirected to Microsoft Agent Framework.

**Framework relevance:** Treat as a research / migration reference rather than a target. Cephalon-aware multi-agent patterns should go through MAF.

---

## 16. Modern Caching: HybridCache and FusionCache

The .NET caching story split into three layers by 2026:

1. `IMemoryCache` (in-process, in-box) -- single-process L1 cache
2. `IDistributedCache` (Redis, SQL Server, NCache, etc.) -- multi-process L2 cache
3. *Hybrid* caches that compose L1 + L2 with stampede protection and tag-based invalidation

### HybridCache (Microsoft.Extensions.Caching.Hybrid)

**What it does:** First-party two-tier cache that sits in front of `IMemoryCache` (L1) and `IDistributedCache` (L2). Adds:
- stampede protection (concurrent requests for the same missing key share a single backing-store call)
- atomic `GetOrCreateAsync(key, factory)` semantics
- tag-based invalidation (`RemoveByTagAsync("tenant:42")`)
- explicit local vs distributed expiration controls
- `[HybridCache]` source-generator pattern for declarative caching (preview, expected GA in the .NET 11 wave)

**Key features:**
- ships as `Microsoft.Extensions.Caching.Hybrid` (out-of-band) targeting `net8.0`+, so it's portable across the current LTS surface
- transparently uses `IMemoryCache` if no `IDistributedCache` is registered; transparently fronts a distributed cache when one is registered
- explicit `HybridCacheEntryFlags` to suppress L1 or L2 per-call when needed

**Framework relevance to CephalonEngine:** Cephalon does not need to invent its own caching primitives -- HybridCache is the right substrate for any future `Cephalon.Caching` companion. The interesting Cephalon angles are (a) projecting cache configuration (TTL, tags, distributed-cache provider selection) through the `Engine:Caching` configuration shape with the same contributor / registry / catalog pattern used elsewhere; (b) reporting live cache posture (hit rate, stampede protections triggered, L1 vs L2 hit ratio) through a `/engine/technology-surfaces` cache surface so operator observability matches the rest of the engine.

**Current version:** `Microsoft.Extensions.Caching.Hybrid` 10.x stable (early 2026). Originally previewed in the .NET 9 wave; promoted to stable when the source-generator-driven `[HybridCache]` declarative pattern shipped.

---

### FusionCache

**What it does:** Long-running community alternative to HybridCache from ZiggyCreatures. Predates the Microsoft implementation; pioneered several of the ideas HybridCache later adopted (stampede protection, fail-safe / soft expiration, eager refresh, backplane notifications).

**Key features:**
- L1 + L2 + backplane (Redis pub/sub, NATS, or custom) so multiple processes can invalidate each other's L1
- "fail-safe" mode that returns stale data if the backing store is unreachable
- "soft" vs "hard" timeouts on factory calls
- eager refresh (refresh in background before expiration)
- tag-based invalidation, distributed locking, automatic OpenTelemetry / metrics

**Framework relevance:** FusionCache is the most feature-complete community option and a reasonable choice when adopters want fail-safe or eager-refresh semantics that HybridCache does not (yet) offer. Worth documenting in adopter guidance as the "richer alternative" so the choice is explicit rather than ambient.

**Current version:** FusionCache 2.x (2026). Targets `net8.0`+ plus `netstandard2.0`.

---

## Compatibility Matrix Summary

| Library/Framework | Latest Version | .NET 10 | License | Actively Maintained |
|---|---|---|---|---|
| Microsoft.Extensions.* | 10.0.x | Yes (in-box) | MIT | Yes |
| EF Core | 10.0.x | Yes (LTS) | MIT | Yes |
| Wolverine | Latest 2026 | Yes | MIT | Yes |
| Marten | 7.x | Yes | MIT | Yes |
| Polecat | 1.x | Yes | MIT | Yes (NEW) |
| MassTransit v8 | 8.x | Yes | Apache 2.0 | Patches only |
| MassTransit v9 | 9.x | Yes | Commercial | Yes |
| MediatR | 12.x | Yes | Apache 2.0 | Yes |
| Autofac | 9.0.0 | Yes | MIT | Yes |
| Lamar | 15.0.1 | Yes | MIT | Yes |
| FluentValidation | 12.1.1 | Yes | Apache 2.0 | Yes |
| Polly | 8.x | Yes | BSD-3 | Yes |
| Serilog | 4.x | Yes | Apache 2.0 | Yes |
| OpenTelemetry .NET | Latest | Yes | Apache 2.0 | Yes |
| HotChocolate | 15.1.13 | Yes | MIT | Yes |
| xUnit v3 | 3.2.2 | Yes | Apache 2.0 | Yes |
| Refit | 9.0.2 | Yes (AOT) | MIT | Yes |
| Scrutor | 7.0.0 | Yes | MIT | Yes |
| StackExchange.Redis | 2.11.8 | Yes | MIT | Yes |
| MongoDB.Driver | 3.8.0 | Yes | Apache 2.0 | Yes |
| Hangfire | 1.8.x | Yes | LGPL/Commercial | Yes |
| Quartz.NET | 3.x | Yes | Apache 2.0 | Yes |
| Verify | Latest | Yes (net10) | MIT | Yes |
| TestContainers | Latest | Yes | MIT | Yes |
| Alba | 8.5.2 | Yes | MIT | Yes |
| System.Text.Json | 10.0.x | Yes (in-box) | MIT | Yes |
| MessagePack-CSharp | 3.x | Yes | MIT | Yes |
| protobuf-net | 3.x | Yes | Apache 2.0 | Yes |
| EventStoreDB Client | 23.3.9 | Yes | Apache 2.0 | Yes |
| Eventuous | Latest RC | Yes | MIT | Yes |
| Dapper | 2.x | Yes | Apache 2.0 | Yes |
| SignalR | 10.0.x | Yes (in-box) | MIT | Yes |
| FastEndpoints | 5.x | Yes | MIT | Yes |
| Carter | Latest | Yes | MIT | Yes |
| System.IO.Pipelines | 10.0.0 | Yes (in-box) | MIT | Yes |
| Aspire (Hosting / AppHost) | 13.3 (Apr 2026) | Yes | MIT | Yes |
| Microsoft.Extensions.AI | 10.5.x | Yes | MIT | Yes |
| Microsoft Agent Framework | 1.0 GA (Apr 2026) | Yes | MIT | Yes |
| Semantic Kernel | 1.76.0 | Yes | MIT | Maintenance |
| AutoGen.NET | latest | Yes | MIT | Maintenance |
| OpenAI .NET SDK | latest | Yes | MIT | Yes |
| Microsoft.Extensions.Caching.Hybrid | 10.x | Yes | MIT | Yes |
| FusionCache | 2.x | Yes | MIT | Yes |
