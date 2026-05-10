# .NET Ecosystem Reference for CephalonEngine

> Comprehensive reference of .NET libraries, frameworks, and NuGet packages relevant to building CephalonEngine -- a modular-monolith engine framework on .NET 10. Researched April 2026.

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

For eventing, that rule now has a runtime checkpoint: `Cephalon.Eventing` emits `eventing-superiority-profile` plus capability `eventing.superiority-profile` so the active runtime can say which MassTransit/NServiceBus/Wolverine/MediatR-inspired dimensions are `claimed`, `partial`, or `not-claimed` before docs or roadmap items describe them as complete, including separate `durable-remediation-command-audit`, `durable-command-journal-replay-cursor`, `broker-dead-letter-replay-ownership`, `broker-topology-materialization-ownership`, `provider-partition-ownership`, `downstream-delivery-completion-ownership`, `broker-inbound-consumption-ownership`, and `serialization-and-contract-versioning-ownership` posture so command-journal replay, broker replay ownership, broker topology materialization, provider partition ownership, downstream delivery completion, inbound broker consumption, and wire-contract/versioning ownership cannot be mistaken for one another.

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
