# Design Patterns Reference for CephalonEngine (.NET 10)

> Comprehensive reference for all design patterns essential to building CephalonEngine --
> a modular-monolith engine framework on .NET 10. Each pattern includes what it is,
> when to use it, modern C# implementation concepts, real-world .NET ecosystem examples,
> and how it applies specifically to framework/engine development.

---

## Table of Contents

1. [Creational Patterns](#1-creational-patterns)
2. [Structural Patterns](#2-structural-patterns)
3. [Behavioral Patterns](#3-behavioral-patterns)
4. [Concurrency Patterns](#4-concurrency-patterns)
5. [Enterprise / Integration Patterns](#5-enterprise--integration-patterns)

---

## 1. Creational Patterns

### 1.1 Builder (Fluent APIs, Framework Configuration)

**What it is:** The Builder pattern constructs complex objects step by step, separating construction logic from representation. In framework development, it manifests as fluent API chains that configure an engine, host, or pipeline.

**When to use it:**
- When an object requires many configuration steps or parameters
- When you need readable, discoverable configuration APIs for framework consumers
- When construction logic must support optional and order-independent settings

**Modern C#/.NET 10 implementation:**

```csharp
// The canonical .NET Builder: WebApplicationBuilder
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenTelemetry();
builder.Services.AddAuthentication();
var app = builder.Build();

// CephalonEngine-style builder
public sealed class EngineBuilder
{
    private readonly IServiceCollection _services = new ServiceCollection();
    private readonly List<ICephalonModule> _modules = [];

    public EngineBuilder AddModule<TModule>() where TModule : ICephalonModule, new()
    {
        _modules.Add(new TModule());
        return this; // fluent return
    }

    public EngineBuilder ConfigureServices(Action<IServiceCollection> configure)
    {
        configure(_services);
        return this;
    }

    public CephalonEngine Build()
    {
        foreach (var module in _modules)
            module.Register(_services);
        return new CephalonEngine(_services.BuildServiceProvider(), _modules);
    }
}

// Usage
var engine = new EngineBuilder()
    .AddModule<CatalogModule>()
    .AddModule<OrdersModule>()
    .ConfigureServices(s => s.AddLogging())
    .Build();
```

**Real-world .NET examples:**
- `WebApplicationBuilder` / `HostBuilder` (ASP.NET Core)
- `DbContextOptionsBuilder` (EF Core)
- `ConfigurationBuilder` (Microsoft.Extensions.Configuration)
- `ResiliencePipelineBuilder` (Polly v8+)
- `LoggerConfiguration` (Serilog)

**Framework/engine application:**
This is the single most important pattern for CephalonEngine's public API surface. The engine builder orchestrates module discovery, service registration, middleware pipeline assembly, and host configuration. Fluent methods like `AddModule<T>()`, `UseTenancy()`, `WithEventSourcing()` give consumers a discoverable, composable configuration experience. The builder internally aggregates `ICephalonModule` registrations, contributors, and capability selections before materializing the runtime.

---

### 1.2 Factory / Abstract Factory (Plugin Creation, Provider Resolution)

**What it is:** Factory Method delegates object creation to subclasses or delegates, while Abstract Factory provides an interface for creating families of related objects without specifying their concrete classes.

**When to use it:**
- When a framework must create plugin/provider instances without knowing concrete types
- When the creation process involves resolution from DI, configuration, or naming conventions
- When families of related objects must be created together (e.g., a data provider suite)

**Modern C#/.NET 10 implementation:**

```csharp
// Factory Method via DI + keyed services (.NET 8+)
builder.Services.AddKeyedSingleton<ITransport, HttpTransport>("http");
builder.Services.AddKeyedSingleton<ITransport, GrpcTransport>("grpc");

// Resolution
public class BehaviorDispatcher(IServiceProvider sp)
{
    public ITransport GetTransport(string key) =>
        sp.GetRequiredKeyedService<ITransport>(key);
}

// Abstract Factory for provider families
public interface IDataProviderFactory
{
    IWriteStore CreateWriteStore(string tenantId);
    IReadStore CreateReadStore(string tenantId);
    IEventStore CreateEventStore(string tenantId);
}

public class PostgresDataProviderFactory(IConfiguration config) : IDataProviderFactory
{
    public IWriteStore CreateWriteStore(string tenantId) =>
        new PostgresWriteStore(config.GetConnectionString(tenantId));
    // ... other members
}

// Factory with Func<T> delegate pattern
builder.Services.AddTransient<Func<string, ITransport>>(sp => key =>
    sp.GetRequiredKeyedService<ITransport>(key));
```

**Real-world .NET examples:**
- `IHttpClientFactory` (creates configured HttpClient instances)
- `ILoggerFactory` (creates ILogger instances per category)
- `IServiceScopeFactory` (creates DI scopes)
- `DbContext` creation via `IDbContextFactory<T>` (EF Core)
- Keyed services in .NET 8+ (`[FromKeyedServices]`)

**Framework/engine application:**
CephalonEngine uses factories extensively for provider resolution: tenant-aware data stores, transport adapters, and module-contributed services. The `ICatalog` / `IRegistry` / `IContributor` triple in the abstractions layer (e.g., `IBehaviorCatalog`, `IOutboxCatalog`) is fundamentally a factory pattern -- contributors register descriptors, the registry materializes them, and the catalog provides lookup. Keyed services eliminate the need for hand-rolled factories in most cases.

---

### 1.3 Prototype (Cloning Configurations)

**What it is:** The Prototype pattern creates new objects by copying an existing instance (the prototype), avoiding the cost of creating from scratch.

**When to use it:**
- When creating objects from scratch is expensive and a similar object already exists
- When you need to clone configuration snapshots or templates
- When you need immutable "base" configs that get customized per-tenant or per-module

**Modern C#/.NET 10 implementation:**

```csharp
// Records with 'with' expressions are the modern .NET Prototype
public record BehaviorTopologyDescriptor(
    string Name,
    string Transport,
    TimeSpan Timeout,
    IReadOnlyList<string> AllowedPatterns)
{
    // Clone with modifications using 'with'
    public BehaviorTopologyDescriptor WithTimeout(TimeSpan timeout) =>
        this with { Timeout = timeout };
}

// Usage: prototype-based config derivation
var baseConfig = new BehaviorTopologyDescriptor("Default", "http", TimeSpan.FromSeconds(30), ["query", "command"]);
var fastConfig = baseConfig with { Timeout = TimeSpan.FromSeconds(5), Name = "FastPath" };

// ICloneable is legacy; prefer records or manual deep-copy
public class EngineOptions : ICloneable
{
    public string AppName { get; set; } = "";
    public Dictionary<string, object> Metadata { get; set; } = [];

    public object Clone() => new EngineOptions
    {
        AppName = AppName,
        Metadata = new Dictionary<string, object>(Metadata)
    };
}
```

**Real-world .NET examples:**
- C# `record` types with `with` expressions (the idiomatic .NET prototype)
- `JsonSerializerOptions` copied via constructor overload
- `HttpRequestMessage` cloning for retry policies
- EF Core `DbContextOptions` derivation

**Framework/engine application:**
CephalonEngine's descriptor types (`BehaviorTopologyDescriptor`, `EventStreamDescriptor`, `OutboxDescriptor`, etc.) are ideal prototype candidates. Modules define base descriptors that get cloned and customized per-tenant. Using `record` types with `with` expressions makes this pattern nearly invisible -- it is the idiomatic C# approach. Configuration snapshots taken via `IOptionsSnapshot<T>` also leverage this pattern under the hood.

---

### 1.4 Singleton (Service Locators, Registries)

**What it is:** The Singleton pattern ensures a class has exactly one instance and provides global access to it. In modern .NET, this is almost exclusively managed through DI container lifetime scoping.

**When to use it:**
- For registries, catalogs, and shared state that must be consistent across the application
- For expensive-to-create objects with no per-request state
- For coordination points (event buses, configuration caches)

**Modern C#/.NET 10 implementation:**

```csharp
// DI-managed singleton (preferred)
builder.Services.AddSingleton<IBehaviorRegistry, BehaviorRegistry>();
builder.Services.AddSingleton<ICapabilityRegistry, CapabilityRegistry>();

// Thread-safe lazy singleton (when DI is unavailable)
public sealed class EngineMetrics
{
    private static readonly Lazy<EngineMetrics> _instance =
        new(() => new EngineMetrics(), LazyThreadSafetyMode.ExecutionAndPublication);

    public static EngineMetrics Instance => _instance.Value;
    private EngineMetrics() { }

    // Meter is itself a singleton per convention
    private readonly Meter _meter = new("CephalonEngine.Runtime");
    public Counter<long> BehaviorsDispatched => _meter.CreateCounter<long>("behaviors.dispatched");
}

// Generic host ensures single instance
builder.Services.AddSingleton(TimeProvider.System);
```

**Real-world .NET examples:**
- `IServiceProvider` (root container is a singleton)
- `IConfiguration` (singleton by default in host)
- `IMemoryCache` (singleton in DI)
- `Meter` and `ActivitySource` (OpenTelemetry singletons)
- `IOptions<T>` (singleton snapshot of configuration)

**Framework/engine application:**
CephalonEngine's registry types (`IBehaviorRegistry`, `ICapabilityRegistry`, `IAuthorizationPolicyRegistry`, `IEventStoreRegistry`, etc.) are all singletons -- they are populated at startup and queried at runtime. The engine runtime itself (`CephalonEngine` host object) is a singleton. The DI container manages all of this, making explicit singleton implementations unnecessary in most cases.

---

### 1.5 Object Pool (Connection Pools, Buffer Pools)

**What it is:** The Object Pool pattern manages a set of reusable objects to avoid the cost of repeated allocation and deallocation. Objects are "rented" from the pool, used, and then "returned."

**When to use it:**
- For objects that are expensive to create (database connections, buffers, socket event args)
- When the allocation/GC pressure of short-lived objects is a measurable bottleneck
- For high-throughput hot paths where allocation reduction matters

**Modern C#/.NET 10 implementation:**

```csharp
// ArrayPool<T> -- the most common pool in .NET
byte[] buffer = ArrayPool<byte>.Shared.Rent(4096);
try
{
    Span<byte> span = buffer.AsSpan(0, 4096);
    // Use the span
}
finally
{
    ArrayPool<byte>.Shared.Return(buffer);
}

// Microsoft.Extensions.ObjectPool for arbitrary objects
builder.Services.AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();
builder.Services.AddSingleton(sp =>
{
    var provider = sp.GetRequiredService<ObjectPoolProvider>();
    var policy = new DefaultPooledObjectPolicy<StringBuilder>();
    return provider.Create(policy);
});

// Custom pool policy
public class BehaviorContextPoolPolicy : IPooledObjectPolicy<BehaviorContext>
{
    public BehaviorContext Create() => new BehaviorContext();
    public bool Return(BehaviorContext obj)
    {
        obj.Reset(); // clear state for reuse
        return true;
    }
}

// Usage in hot path
public class BehaviorDispatcher(ObjectPool<BehaviorContext> pool)
{
    public async Task DispatchAsync(IAppBehavior behavior)
    {
        var ctx = pool.Get();
        try
        {
            await behavior.ExecuteAsync(ctx);
        }
        finally
        {
            pool.Return(ctx);
        }
    }
}
```

**Real-world .NET examples:**
- `ArrayPool<T>.Shared` (System.Buffers)
- `MemoryPool<T>` (System.Buffers)
- `ObjectPool<T>` (Microsoft.Extensions.ObjectPool, used heavily in Kestrel)
- `SocketAsyncEventArgs` pooling in Kestrel's transport layer
- `DbConnection` pooling in ADO.NET / EF Core
- `StringBuilder` pooling in ASP.NET Core's response writing

**Framework/engine application:**
CephalonEngine's hot paths -- behavior dispatch, event sourcing write loops, outbox staging -- benefit from object pooling. The `BehaviorContext` and `AuthorizationContext` types should be pooled to avoid per-request allocations. ArrayPool is used for buffer management in transport layers and serialization. The benchmarks (`BehaviorDispatchBenchmarks`, `EventSourcingBenchmarks`) should measure pool vs. non-pool allocation profiles.

---

## 2. Structural Patterns

### 2.1 Adapter (Wrapping External APIs)

**What it is:** The Adapter pattern converts the interface of a class into another interface that clients expect. It lets classes work together that otherwise could not because of incompatible interfaces.

**When to use it:**
- When integrating third-party libraries that don't conform to your abstractions
- When wrapping platform-specific APIs behind a portable interface
- When migrating from one library to another without changing consuming code

**Modern C#/.NET 10 implementation:**

```csharp
// Engine abstraction
public interface IEventStore
{
    Task AppendAsync(string streamId, IReadOnlyList<IDomainEvent> events, CancellationToken ct);
    Task<IReadOnlyList<IDomainEvent>> ReadAsync(string streamId, CancellationToken ct);
}

// Adapter wrapping EventStoreDB client
public class EventStoreDbAdapter(EventStoreClient client) : IEventStore
{
    public async Task AppendAsync(string streamId, IReadOnlyList<IDomainEvent> events, CancellationToken ct)
    {
        var eventData = events.Select(e => new EventData(
            Uuid.NewUuid(),
            e.GetType().Name,
            JsonSerializer.SerializeToUtf8Bytes(e)
        )).ToArray();

        await client.AppendToStreamAsync(streamId, StreamState.Any, eventData, cancellationToken: ct);
    }

    public async Task<IReadOnlyList<IDomainEvent>> ReadAsync(string streamId, CancellationToken ct)
    {
        // Adapt EventStoreDB's read result to our domain events
        var result = client.ReadStreamAsync(Direction.Forwards, streamId, StreamPosition.Start, cancellationToken: ct);
        // ... deserialization logic
    }
}
```

**Real-world .NET examples:**
- `ILogger` / `ILoggerProvider` (adapts Serilog, NLog, etc.)
- `Stream` as an adapter over different I/O sources
- `IHostedService` adapting background work to the host lifecycle
- `IDistributedCache` adapting Redis, SQL Server, etc.

**Framework/engine application:**
CephalonEngine's contributor model (`IEventStoreContributor`, `IOutboxContributor`, `IAuditStoreContributor`) is fundamentally an adapter pattern. Each contributor adapts a specific technology (Marten, EventStoreDB, PostgreSQL, etc.) to the engine's abstract store interfaces. This lets the engine stay technology-agnostic while consumers bring their own infrastructure.

---

### 2.2 Bridge (Abstraction / Implementation Separation)

**What it is:** The Bridge pattern decouples an abstraction from its implementation so the two can vary independently. Unlike Adapter (which makes incompatible things work together after the fact), Bridge is designed from the start to separate concerns.

**When to use it:**
- When both the abstraction hierarchy and the implementation hierarchy need to evolve independently
- When you want to avoid a Cartesian product explosion of subclasses
- When runtime switching of implementations is needed (e.g., per-tenant data providers)

**Modern C#/.NET 10 implementation:**

```csharp
// Abstraction: Transport
public interface IBehaviorTransport
{
    Task<BehaviorResult> SendAsync(BehaviorEnvelope envelope, CancellationToken ct);
}

// Abstraction: Serialization (orthogonal dimension)
public interface IPayloadSerializer
{
    byte[] Serialize<T>(T payload);
    T Deserialize<T>(byte[] data);
}

// Bridge: The behavior dispatcher bridges transport and serialization
public class BehaviorDispatcher(
    IBehaviorTransport transport,
    IPayloadSerializer serializer)
{
    public async Task<TResult> DispatchAsync<TResult>(object request, CancellationToken ct)
    {
        var envelope = new BehaviorEnvelope(serializer.Serialize(request));
        var result = await transport.SendAsync(envelope, ct);
        return serializer.Deserialize<TResult>(result.Payload);
    }
}

// Now transport and serialization vary independently:
// HttpTransport + JsonSerializer
// GrpcTransport + ProtobufSerializer
// InProcessTransport + NullSerializer
```

**Real-world .NET examples:**
- ADO.NET: `DbConnection` (abstraction) / `SqlConnection`, `NpgsqlConnection` (implementations)
- ASP.NET Core: `IServer` (abstraction) / Kestrel, IIS (implementations)
- Logging: `ILogger` (abstraction) / providers (implementations)

**Framework/engine application:**
CephalonEngine's transport layer is a Bridge: `IBehaviorTransport` (HTTP, gRPC, in-process) is orthogonal to `IPayloadSerializer` (JSON, Protobuf, MessagePack). The data access layer is another Bridge: `IWriteStore`/`IReadStore` (abstractions) can be backed by EF Core, Dapper, Marten, or raw ADO.NET without affecting the behavior layer above.

---

### 2.3 Composite (Tree Structures, Middleware Chains)

**What it is:** The Composite pattern composes objects into tree structures so that clients can treat individual objects and compositions uniformly.

**When to use it:**
- When you have hierarchical structures (modules containing sub-modules, permission trees)
- When individual items and groups of items should be treated the same way
- For middleware/pipeline composition where a step can be atomic or a sub-pipeline

**Modern C#/.NET 10 implementation:**

```csharp
// Module hierarchy (composite)
public interface ICephalonModule
{
    string Name { get; }
    void Register(IServiceCollection services);
    IReadOnlyList<ICephalonModule> SubModules => [];
}

public class CommerceModule : ICephalonModule
{
    public string Name => "Commerce";
    public IReadOnlyList<ICephalonModule> SubModules =>
    [
        new CatalogModule(),
        new OrdersModule(),
        new InventoryModule()
    ];

    public void Register(IServiceCollection services)
    {
        // Register own services, then recurse
        foreach (var sub in SubModules)
            sub.Register(services);
    }
}

// Authorization policy tree
public abstract record AuthorizationRule
{
    public abstract bool Evaluate(AuthorizationContext ctx);
}

public record AndRule(IReadOnlyList<AuthorizationRule> Rules) : AuthorizationRule
{
    public override bool Evaluate(AuthorizationContext ctx) =>
        Rules.All(r => r.Evaluate(ctx));
}

public record OrRule(IReadOnlyList<AuthorizationRule> Rules) : AuthorizationRule
{
    public override bool Evaluate(AuthorizationContext ctx) =>
        Rules.Any(r => r.Evaluate(ctx));
}

public record PermissionRule(string Permission) : AuthorizationRule
{
    public override bool Evaluate(AuthorizationContext ctx) =>
        ctx.Subject.HasPermission(Permission);
}
```

**Real-world .NET examples:**
- ASP.NET Core middleware pipeline (each middleware wraps the next, forming a chain)
- `IConfigurationBuilder` composing multiple configuration sources
- MVC filter pipeline (authorization, resource, action, exception, result filters)
- Blazor component tree

**Framework/engine application:**
CephalonEngine's module system is inherently a Composite: modules can contain sub-modules, creating a tree. The execution graph (`ExecutionGraphDescriptor` with nodes and edges) is a composite/graph structure. Authorization policies compose leaf rules (permissions, roles) into trees via AND/OR combinators.

---

### 2.4 Decorator (Adding Behavior Without Inheritance)

**What it is:** The Decorator pattern dynamically attaches additional responsibilities to an object by wrapping it. It provides a flexible alternative to subclassing for extending functionality.

**When to use it:**
- When you need cross-cutting concerns (logging, caching, validation, timing) without modifying core logic
- When responsibilities should be stackable and independently removable
- When open/closed principle matters -- extend via composition, not modification

**Modern C#/.NET 10 implementation:**

```csharp
// Interface
public interface IBehaviorDispatcher
{
    Task<object?> DispatchAsync(object request, CancellationToken ct);
}

// Core implementation
public class CoreBehaviorDispatcher : IBehaviorDispatcher { /* ... */ }

// Decorators
public class LoggingBehaviorDispatcher(
    IBehaviorDispatcher inner,
    ILogger<LoggingBehaviorDispatcher> logger) : IBehaviorDispatcher
{
    public async Task<object?> DispatchAsync(object request, CancellationToken ct)
    {
        logger.LogInformation("Dispatching {RequestType}", request.GetType().Name);
        var sw = Stopwatch.StartNew();
        var result = await inner.DispatchAsync(request, ct);
        logger.LogInformation("Dispatched in {Elapsed}ms", sw.ElapsedMilliseconds);
        return result;
    }
}

public class AuthorizingBehaviorDispatcher(
    IBehaviorDispatcher inner,
    IAuthorizationEvaluator auth) : IBehaviorDispatcher
{
    public async Task<object?> DispatchAsync(object request, CancellationToken ct)
    {
        var decision = await auth.EvaluateAsync(request, ct);
        if (!decision.IsAllowed)
            throw new BehaviorSecurityException(decision.Reason);
        return await inner.DispatchAsync(request, ct);
    }
}

// DI registration (Scrutor or manual)
builder.Services.AddSingleton<IBehaviorDispatcher, CoreBehaviorDispatcher>();
builder.Services.Decorate<IBehaviorDispatcher, AuthorizingBehaviorDispatcher>();
builder.Services.Decorate<IBehaviorDispatcher, LoggingBehaviorDispatcher>();
```

**Real-world .NET examples:**
- `DelegatingHandler` in HttpClient pipeline (each handler wraps the inner)
- Scrutor's `Decorate<TInterface, TDecorator>()` for DI-based decoration
- ASP.NET Core's `IMiddleware` wrapping pattern
- MediatR pipeline behaviors (`IPipelineBehavior<TRequest, TResponse>`)
- EF Core query interceptors

**Framework/engine application:**
Decoration is CephalonEngine's primary extensibility mechanism for cross-cutting concerns. The behavior dispatch pipeline layers authorization, validation, audit logging, telemetry, and outbox staging as decorators around the core dispatcher. Each decorator is independently testable. The `IBehaviorAdvisory` system can inspect the decorator chain to warn about missing concerns.

---

### 2.5 Facade (Simplified API Surfaces)

**What it is:** The Facade pattern provides a simplified, unified interface to a complex subsystem. It doesn't add new functionality -- it makes existing functionality easier to use.

**When to use it:**
- When a subsystem has many interacting classes but consumers need a simple entry point
- When you want to decouple consumers from subsystem internals
- When migrating complex internal APIs to a clean public API

**Modern C#/.NET 10 implementation:**

```csharp
// Complex subsystem behind the facade
public class CephalonRuntime : ICephalonRuntime
{
    private readonly IBehaviorRegistry _behaviors;
    private readonly ICapabilityRegistry _capabilities;
    private readonly ITenantResolver _tenants;
    private readonly IAuthorizationEvaluator _auth;
    private readonly IBehaviorDispatcher _dispatcher;

    // Facade: one method instead of five subsystem calls
    public async Task<TResult> ExecuteAsync<TResult>(
        object request,
        CancellationToken ct = default)
    {
        var tenant = await _tenants.ResolveAsync(ct);
        var descriptor = _behaviors.Resolve(request.GetType());
        var authCtx = new AuthorizationContext(tenant, descriptor);
        var decision = await _auth.EvaluateAsync(authCtx, ct);
        if (!decision.IsAllowed)
            throw new BehaviorSecurityException(decision.Reason);
        return await _dispatcher.DispatchAsync<TResult>(request, ct);
    }
}
```

**Real-world .NET examples:**
- `WebApplication` (facade over Kestrel, DI, configuration, logging, routing)
- `DbContext` (facade over connection, change tracking, query translation, migrations)
- `IHost` (facade over DI, configuration, logging, hosted services)
- `HttpClient` (facade over handlers, DNS, connection pool)

**Framework/engine application:**
The `CephalonEngine` / `ICephalonRuntime` type is the primary facade: it unifies module registration, behavior dispatch, tenant resolution, authorization, data access, and telemetry behind a clean API. Module authors interact with `ICephalonModule` as a facade over the contributor/registry/catalog subsystem. The `AppBlueprint`/`AppProfile` types are facade descriptors for the engine's configuration.

---

### 2.6 Proxy (Lazy Loading, Remote Calls, Interception)

**What it is:** The Proxy pattern provides a surrogate or placeholder for another object to control access to it. Variants include virtual proxy (lazy loading), remote proxy (network calls), protection proxy (access control), and caching proxy.

**When to use it:**
- When you need lazy initialization of expensive resources
- When adding access control or logging before/after method calls
- When transparently forwarding calls to a remote service
- When caching results to avoid repeated computation

**Modern C#/.NET 10 implementation:**

```csharp
// Virtual proxy (lazy loading)
public class LazyModuleProxy(Func<ICephalonModule> factory) : ICephalonModule
{
    private readonly Lazy<ICephalonModule> _inner = new(factory);

    public string Name => _inner.Value.Name;
    public void Register(IServiceCollection services) => _inner.Value.Register(services);
}

// Caching proxy
public class CachingBehaviorCatalog(IBehaviorCatalog inner, IMemoryCache cache) : IBehaviorCatalog
{
    public BehaviorTopologyDescriptor? Resolve(string name)
    {
        return cache.GetOrCreate($"behavior:{name}", entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            return inner.Resolve(name);
        });
    }
}

// DispatchProxy for runtime interception (.NET built-in)
public class LoggingProxy<T> : DispatchProxy where T : class
{
    private T _target = default!;
    private ILogger _logger = default!;

    public static T Create(T target, ILogger logger)
    {
        var proxy = Create<T, LoggingProxy<T>>() as LoggingProxy<T>;
        proxy!._target = target;
        proxy._logger = logger;
        return (proxy as T)!;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        _logger.LogDebug("Calling {Method}", targetMethod?.Name);
        return targetMethod?.Invoke(_target, args);
    }
}
```

**Real-world .NET examples:**
- `Lazy<T>` (virtual proxy in DI)
- `DispatchProxy` (runtime proxy generation)
- Entity Framework lazy-loading navigation proxies
- gRPC client stubs (remote proxies generated from .proto files)
- `IHttpClientFactory` handlers (protection/logging proxies)

**Framework/engine application:**
CephalonEngine uses proxies for lazy module loading (modules loaded on first access), caching of resolved behavior topologies, and interception of behavior dispatch for telemetry. The `DispatchProxy` pattern enables AOP-style concerns without external libraries. Remote behavior dispatch (calling behaviors across module boundaries in a microservice suite) is a remote proxy.

---

### 2.7 Flyweight (Shared Immutable State)

**What it is:** The Flyweight pattern minimizes memory usage by sharing as much data as possible with similar objects. Shared (intrinsic) state is stored once; unique (extrinsic) state is passed in by the client.

**When to use it:**
- When many objects share the same immutable state (descriptors, metadata, schema info)
- When memory pressure from large numbers of similar objects is a concern
- For string interning, descriptor caching, or metadata tables

**Modern C#/.NET 10 implementation:**

```csharp
// Descriptor flyweight: one instance per behavior type, shared across all requests
public sealed class BehaviorDescriptorCache
{
    private readonly ConcurrentDictionary<Type, BehaviorTopologyDescriptor> _cache = new();

    public BehaviorTopologyDescriptor GetOrCreate(Type behaviorType)
    {
        return _cache.GetOrAdd(behaviorType, type =>
        {
            // Intrinsic state: metadata, transport, allowed patterns
            var attr = type.GetCustomAttribute<AppBehaviorAttribute>();
            return new BehaviorTopologyDescriptor(
                Name: attr?.Name ?? type.Name,
                Transport: attr?.Transport ?? "in-process",
                Timeout: attr?.Timeout ?? TimeSpan.FromSeconds(30),
                AllowedPatterns: attr?.AllowedPatterns ?? ["query", "command"]
            );
        });
    }
}

// String interning is a built-in flyweight
var interned = string.Intern(tenantId); // reuse same string reference

// Frozen collections (.NET 8+) for immutable lookup tables
var behaviorMap = descriptors.ToFrozenDictionary(d => d.Name);
```

**Real-world .NET examples:**
- `string.Intern()` (string flyweight)
- `Type` metadata objects (one per type, shared globally)
- `HttpMethod.Get`, `HttpMethod.Post` (static singletons for common values)
- `FrozenDictionary<K,V>` / `FrozenSet<T>` for immutable lookup tables
- ASP.NET Core's `Endpoint` metadata (shared across all requests)

**Framework/engine application:**
CephalonEngine's descriptor types are flyweights: `BehaviorTopologyDescriptor`, `EventStreamDescriptor`, `ProjectionDescriptor`, etc. are created once at startup and shared across all requests. The extrinsic state (tenant ID, correlation ID, user context) is passed per-request through `IBehaviorContext`. `FrozenDictionary` should be used for the registry lookup tables after the startup phase completes.

---

## 3. Behavioral Patterns

### 3.1 Chain of Responsibility (Middleware Pipeline, Validation Chains)

**What it is:** The Chain of Responsibility pattern passes a request along a chain of handlers. Each handler decides either to process the request or to pass it to the next handler in the chain.

**When to use it:**
- For middleware pipelines where multiple concerns process a request sequentially
- For validation chains where multiple validators run in order
- When the set of handlers is dynamic and configurable

**Modern C#/.NET 10 implementation:**

```csharp
// ASP.NET Core-style middleware chain
public delegate Task BehaviorDelegate(IBehaviorContext context);

public interface IBehaviorMiddleware
{
    Task InvokeAsync(IBehaviorContext context, BehaviorDelegate next);
}

// Middleware implementations
public class TenantResolutionMiddleware(ITenantResolver resolver) : IBehaviorMiddleware
{
    public async Task InvokeAsync(IBehaviorContext context, BehaviorDelegate next)
    {
        context.TenantId = await resolver.ResolveAsync(context.CancellationToken);
        await next(context); // pass to next handler
    }
}

public class AuthorizationMiddleware(IAuthorizationEvaluator auth) : IBehaviorMiddleware
{
    public async Task InvokeAsync(IBehaviorContext context, BehaviorDelegate next)
    {
        var decision = await auth.EvaluateAsync(context, context.CancellationToken);
        if (!decision.IsAllowed)
            throw new BehaviorSecurityException(decision.Reason);
        await next(context);
    }
}

// Pipeline builder
public class BehaviorPipeline
{
    private readonly List<Func<BehaviorDelegate, BehaviorDelegate>> _components = [];

    public BehaviorPipeline Use(Func<BehaviorDelegate, BehaviorDelegate> middleware)
    {
        _components.Add(middleware);
        return this;
    }

    public BehaviorDelegate Build()
    {
        BehaviorDelegate app = _ => Task.CompletedTask;
        for (int i = _components.Count - 1; i >= 0; i--)
            app = _components[i](app);
        return app;
    }
}
```

**Real-world .NET examples:**
- ASP.NET Core middleware pipeline (`app.Use()`, `app.UseAuthentication()`, etc.)
- `DelegatingHandler` chain in `HttpClient`
- MediatR pipeline behaviors
- FluentValidation's validator chain
- .NET logging filter chain

**Framework/engine application:**
CephalonEngine's behavior dispatch pipeline is a Chain of Responsibility: tenant resolution -> authorization -> validation -> audit -> dispatch -> outbox staging. Each middleware is a link in the chain. The `IBehaviorAdvisory` system validates that the chain is complete (e.g., warns if authorization middleware is missing). This is the architectural backbone of request processing.

---

### 3.2 Command (CQRS Commands, Undo/Redo)

**What it is:** The Command pattern encapsulates a request as an object, thereby allowing parameterization, queuing, logging, and undo/redo of operations.

**When to use it:**
- For CQRS: separating write operations (commands) from read operations (queries)
- When operations need to be queued, logged, or replayed
- For undo/redo functionality
- When you need to decouple the invoker from the executor

**Modern C#/.NET 10 implementation:**

```csharp
// CephalonEngine's CQRS abstractions
public interface ICommand { }
public interface ICommand<TResult> : ICommand { }
public interface ICommandHandler<in TCommand> where TCommand : ICommand
{
    Task HandleAsync(TCommand command, CancellationToken ct);
}
public interface ICommandHandler<in TCommand, TResult> where TCommand : ICommand<TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken ct);
}

// Concrete command
public record CreateProductCommand(string Name, decimal Price, string Category) : ICommand<Guid>;

public class CreateProductHandler(IWriteStore store) : ICommandHandler<CreateProductCommand, Guid>
{
    public async Task<Guid> HandleAsync(CreateProductCommand command, CancellationToken ct)
    {
        var product = new Product(Guid.NewGuid(), command.Name, command.Price, command.Category);
        await store.AddAsync(product, ct);
        return product.Id;
    }
}

// Query side
public interface IQuery<TResult> { }
public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken ct);
}

public record GetProductQuery(Guid Id) : IQuery<ProductSnapshot?>;
```

**Real-world .NET examples:**
- MediatR's `IRequest<T>` / `IRequestHandler<T, TResponse>` (command/query dispatch)
- Wolverine's command handling
- Entity Framework's `SaveChanges()` batches commands internally
- ASP.NET Core's `IResult` (command results as return types)

**Framework/engine application:**
CephalonEngine already defines `ICommand`, `ICommand<TResult>`, `ICommandHandler<T>`, `IQuery<TResult>`, and `IQueryHandler<T, TResult>` in its abstractions. The behavior system (`IAppBehavior`) extends this further by adding transport, authorization, and topology metadata. Commands flow through the behavior pipeline, get dispatched to handlers, and produce domain events. The outbox pattern ensures commands and events are committed atomically.

---

### 3.3 Iterator (Custom Collections, Async Streams)

**What it is:** The Iterator pattern provides a way to access elements of a collection sequentially without exposing the underlying representation. In modern .NET, async iterators (`IAsyncEnumerable<T>`) are the critical variant.

**When to use it:**
- When you need to stream data from a source without loading everything into memory
- For paginated API results, event streams, log tails
- When consumers should not know whether data comes from a list, database, or network

**Modern C#/.NET 10 implementation:**

```csharp
// Async iterator for streaming events
public class EventStore : IEventStore
{
    public async IAsyncEnumerable<IDomainEvent> ReadStreamAsync(
        string streamId,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await using var reader = await OpenReaderAsync(streamId, ct);
        while (await reader.MoveNextAsync(ct))
        {
            yield return reader.Current;
        }
    }
}

// Consuming with await foreach
await foreach (var evt in eventStore.ReadStreamAsync("order-123"))
{
    projection.Apply(evt);
}

// With ConfigureAwait for library code
await foreach (var evt in eventStore.ReadStreamAsync("order-123")
    .ConfigureAwait(false))
{
    projection.Apply(evt);
}

// LINQ over async streams
var recentEvents = eventStore.ReadStreamAsync("order-123")
    .Where(e => e.Timestamp > DateTime.UtcNow.AddDays(-7));
```

**Real-world .NET examples:**
- `IAsyncEnumerable<T>` (C# 8+)
- EF Core's `AsAsyncEnumerable()` for streaming query results
- `System.Text.Json`'s `DeserializeAsyncEnumerable<T>()` for streaming JSON
- gRPC server streaming responses
- `Channel<T>.Reader.ReadAllAsync()` returns `IAsyncEnumerable<T>`

**Framework/engine application:**
CephalonEngine's event sourcing system should expose `IAsyncEnumerable<IDomainEvent>` for reading event streams, enabling consumers to process events lazily without buffering entire streams. Projection replay, audit log streaming, and behavior result streaming all benefit from async iterators. This pattern is essential for backpressure-aware processing of large datasets.

---

### 3.4 Mediator (MediatR-Style, In-Process Messaging)

**What it is:** The Mediator pattern defines an object that encapsulates how a set of objects interact. It promotes loose coupling by preventing objects from referring to each other explicitly.

**When to use it:**
- For in-process command/query/event dispatch without direct coupling
- When modules need to communicate without knowing about each other
- For CQRS implementations where handlers are discovered and invoked dynamically

**Modern C#/.NET 10 implementation:**

```csharp
// Simple mediator interface
public interface IMediator
{
    Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken ct = default);
    Task<TResult> QueryAsync<TResult>(IQuery<TResult> query, CancellationToken ct = default);
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default) where TEvent : IDomainEvent;
}

// DI-based mediator implementation
public class CephalonMediator(IServiceProvider sp) : IMediator
{
    public async Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken ct)
    {
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResult));
        dynamic handler = sp.GetRequiredService(handlerType);
        return await handler.HandleAsync((dynamic)command, ct);
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct) where TEvent : IDomainEvent
    {
        var handlers = sp.GetServices<IDomainEventHandler<TEvent>>();
        foreach (var handler in handlers)
            await handler.HandleAsync(@event, ct);
    }
}

// MediatR-style pipeline behavior (cross-cutting)
public interface IPipelineBehavior<TRequest, TResponse>
{
    Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken ct);
}

public class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators) : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<TResponse> HandleAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken ct)
    {
        var failures = validators
            .Select(v => v.Validate(request))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count > 0)
            throw new ValidationException(failures);

        return await next();
    }
}
```

**Real-world .NET examples:**
- MediatR (the canonical .NET mediator library)
- Wolverine (next-gen mediator with built-in outbox, saga support)
- `SignalR` hub (mediates between clients)
- `IMessageBus` in various frameworks

**Framework/engine application:**
CephalonEngine's behavior dispatch system is a mediator: behaviors are registered by type, the dispatcher (mediator) resolves the appropriate handler from DI and invokes it. The `IBehaviorCatalog` / `IBehaviorRegistry` acts as the mediator's routing table. Domain events are published through the mediator to all registered handlers. This keeps modules decoupled -- the OrdersModule publishes `OrderPlaced` without knowing that InventoryModule listens for it.

---

### 3.5 Memento (State Snapshots, Event Sourcing)

**What it is:** The Memento pattern captures and externalizes an object's internal state so it can be restored later, without violating encapsulation.

**When to use it:**
- For undo/redo functionality
- For event sourcing snapshots (periodically saving aggregate state to speed up replay)
- For transaction rollback (save state before operation, restore on failure)

**Modern C#/.NET 10 implementation:**

```csharp
// Event-sourced aggregate with snapshot support (Memento)
public abstract class Aggregate : IAggregate
{
    private readonly List<IDomainEvent> _uncommittedEvents = [];
    public Guid Id { get; protected set; }
    public long Version { get; protected set; }

    // Create a memento (snapshot)
    public abstract AggregateSnapshot TakeSnapshot();

    // Restore from memento
    public abstract void RestoreFromSnapshot(AggregateSnapshot snapshot);

    // Apply events
    protected void RaiseEvent(IDomainEvent @event)
    {
        Apply(@event);
        _uncommittedEvents.Add(@event);
        Version++;
    }

    protected abstract void Apply(IDomainEvent @event);
}

// Concrete snapshot (the memento)
public record OrderSnapshot(
    Guid Id,
    long Version,
    string Status,
    decimal Total,
    IReadOnlyList<OrderLineSnapshot> Lines) : AggregateSnapshot;

public class OrderAggregate : Aggregate
{
    private string _status = "draft";
    private decimal _total;
    private readonly List<OrderLine> _lines = [];

    public override AggregateSnapshot TakeSnapshot() =>
        new OrderSnapshot(Id, Version, _status, _total,
            _lines.Select(l => new OrderLineSnapshot(l.ProductId, l.Qty, l.Price)).ToList());

    public override void RestoreFromSnapshot(AggregateSnapshot snapshot)
    {
        if (snapshot is not OrderSnapshot s) throw new InvalidOperationException();
        Id = s.Id;
        Version = s.Version;
        _status = s.Status;
        _total = s.Total;
        _lines.Clear();
        _lines.AddRange(s.Lines.Select(l => new OrderLine(l.ProductId, l.Qty, l.Price)));
    }
}
```

**Real-world .NET examples:**
- Marten's snapshot support for event-sourced aggregates
- EventStoreDB snapshot projections
- `IMemoryCache` as a form of state snapshot
- EF Core's `SaveChanges()` / `AsNoTracking()` snapshot behavior

**Framework/engine application:**
CephalonEngine's event sourcing layer (`IAggregate`, `IEventStore`, `ISnapshotStore`) directly implements the Memento pattern. Aggregates accumulate domain events and can take snapshots at configurable intervals. When replaying, the system loads the latest snapshot (memento) and replays only subsequent events. This is critical for performance with long-lived aggregates.

---

### 3.6 Observer (Event System, Reactive Patterns)

**What it is:** The Observer pattern defines a one-to-many dependency between objects so that when one object changes state, all its dependents are notified automatically.

**When to use it:**
- For domain event publishing (multiple handlers react to a single event)
- For module-to-module communication without direct coupling
- For reactive UI updates, real-time notifications, webhook dispatch

**Modern C#/.NET 10 implementation:**

```csharp
// .NET's built-in observer interfaces
// IObservable<T> + IObserver<T>

// Domain event publisher (observer pattern via DI)
public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent @event, CancellationToken ct);
}

// Publishing to all observers
public class DomainEventPublisher(IServiceProvider sp)
{
    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct) where TEvent : IDomainEvent
    {
        var handlers = sp.GetServices<IDomainEventHandler<TEvent>>();
        var tasks = handlers.Select(h => h.HandleAsync(@event, ct));
        await Task.WhenAll(tasks); // fan-out to all observers
    }
}

// Event handler (observer)
public class WhenOrderPlaced_ReserveStock(IInventoryService inventory)
    : IDomainEventHandler<OrderPlacedEvent>
{
    public async Task HandleAsync(OrderPlacedEvent @event, CancellationToken ct)
    {
        await inventory.ReserveStockAsync(@event.OrderId, @event.Lines, ct);
    }
}

// Channel-based reactive observer for streaming
public class EventStream<T>
{
    private readonly Channel<T> _channel = Channel.CreateUnbounded<T>();

    public void Publish(T @event) => _channel.Writer.TryWrite(@event);
    public IAsyncEnumerable<T> Subscribe() => _channel.Reader.ReadAllAsync();
}
```

**Real-world .NET examples:**
- `IObservable<T>` / `IObserver<T>` (System namespace)
- `System.Reactive` (Rx.NET) for complex event processing
- `IChangeToken` / `IOptionsMonitor<T>` (configuration change notifications)
- ASP.NET Core's `IHostApplicationLifetime` (application lifetime events)
- SignalR (real-time observer-based communication)

**Framework/engine application:**
CephalonEngine's domain event system is the Observer pattern: when a behavior raises a `DomainEvent`, all registered `IDomainEventHandler<T>` implementations are notified. The module boundary is preserved because handlers are registered by each module independently. The outbox pattern extends this by persisting events before notification, ensuring durability.

---

### 3.7 State (State Machines, Workflow Engines)

**What it is:** The State pattern allows an object to alter its behavior when its internal state changes. The object appears to change its class.

**When to use it:**
- For workflow engines (order processing, approval flows)
- For saga orchestration (each step has its own state and transitions)
- When behavior varies dramatically based on the current state

**Modern C#/.NET 10 implementation:**

```csharp
// State pattern with discriminated unions (modern C# approach)
public abstract record OrderState
{
    public record Draft : OrderState;
    public record Submitted(DateTime SubmittedAt) : OrderState;
    public record Approved(DateTime ApprovedAt, string ApprovedBy) : OrderState;
    public record Shipped(string TrackingNumber) : OrderState;
    public record Delivered(DateTime DeliveredAt) : OrderState;
    public record Cancelled(string Reason) : OrderState;
}

public class OrderStateMachine
{
    public OrderState Transition(OrderState current, OrderCommand command) => (current, command) switch
    {
        (OrderState.Draft, SubmitOrderCommand c) =>
            new OrderState.Submitted(DateTime.UtcNow),

        (OrderState.Submitted, ApproveOrderCommand c) =>
            new OrderState.Approved(DateTime.UtcNow, c.ApprovedBy),

        (OrderState.Approved, ShipOrderCommand c) =>
            new OrderState.Shipped(c.TrackingNumber),

        (OrderState.Shipped, ConfirmDeliveryCommand c) =>
            new OrderState.Delivered(DateTime.UtcNow),

        (OrderState.Draft or OrderState.Submitted, CancelOrderCommand c) =>
            new OrderState.Cancelled(c.Reason),

        _ => throw new InvalidOperationException(
            $"Cannot apply {command.GetType().Name} in state {current.GetType().Name}")
    };
}

// MassTransit Automatonymous-style saga state machine
public class OrderSagaStateMachine : MassTransitStateMachine<OrderSagaState>
{
    public State Submitted { get; private set; } = default!;
    public State PaymentReceived { get; private set; } = default!;

    public Event<OrderSubmitted> OrderSubmittedEvent { get; private set; } = default!;
    public Event<PaymentCompleted> PaymentCompletedEvent { get; private set; } = default!;

    public OrderSagaStateMachine()
    {
        InstanceState(x => x.CurrentState);

        Event(() => OrderSubmittedEvent, x => x.CorrelateById(ctx => ctx.Message.OrderId));

        Initially(
            When(OrderSubmittedEvent)
                .Then(ctx => ctx.Saga.OrderId = ctx.Message.OrderId)
                .TransitionTo(Submitted));

        During(Submitted,
            When(PaymentCompletedEvent)
                .TransitionTo(PaymentReceived)
                .Finalize());
    }
}
```

**Real-world .NET examples:**
- MassTransit `Automatonymous` state machines (saga orchestration)
- Stateless library (lightweight state machine)
- Elsa Workflows (workflow engine)
- ASP.NET Core's `ConnectionState` in SignalR

**Framework/engine application:**
CephalonEngine's execution graph and saga support leverage the State pattern. The `IProcessCompletion` abstraction tracks process state transitions. Order processing workflows, approval chains, and long-running sagas are modeled as state machines. The behavior dispatch layer can use state to determine valid operations (e.g., you can only ship an approved order).

---

### 3.8 Strategy (Pluggable Algorithms, Transport Selection)

**What it is:** The Strategy pattern defines a family of algorithms, encapsulates each one, and makes them interchangeable. The algorithm varies independently from the clients that use it.

**When to use it:**
- When you have multiple algorithms for the same task (serialization, compression, hashing)
- When the algorithm should be selected at runtime (per-tenant, per-request)
- For pluggable framework components (transport, storage, identity provider)

**Modern C#/.NET 10 implementation:**

```csharp
// Strategy interface
public interface ITenantResolutionStrategy
{
    Task<string?> ResolveAsync(HttpContext context, CancellationToken ct);
}

// Concrete strategies
public class HeaderTenantStrategy : ITenantResolutionStrategy
{
    public Task<string?> ResolveAsync(HttpContext context, CancellationToken ct) =>
        Task.FromResult(context.Request.Headers["X-Tenant-Id"].FirstOrDefault());
}

public class SubdomainTenantStrategy : ITenantResolutionStrategy
{
    public Task<string?> ResolveAsync(HttpContext context, CancellationToken ct)
    {
        var host = context.Request.Host.Host;
        var subdomain = host.Split('.')[0];
        return Task.FromResult<string?>(subdomain);
    }
}

public class ClaimsTenantStrategy : ITenantResolutionStrategy
{
    public Task<string?> ResolveAsync(HttpContext context, CancellationToken ct) =>
        Task.FromResult(context.User?.FindFirst("tenant_id")?.Value);
}

// Strategy selection (composite strategy with fallback)
public class CompositeTenantResolver(IEnumerable<ITenantResolutionStrategy> strategies) : ITenantResolver
{
    public async Task<string> ResolveAsync(CancellationToken ct)
    {
        foreach (var strategy in strategies)
        {
            var tenantId = await strategy.ResolveAsync(httpContext, ct);
            if (tenantId is not null)
                return tenantId;
        }
        throw new TenantResolutionException("No tenant could be resolved");
    }
}

// Registration
builder.Services.AddSingleton<ITenantResolutionStrategy, HeaderTenantStrategy>();
builder.Services.AddSingleton<ITenantResolutionStrategy, SubdomainTenantStrategy>();
builder.Services.AddSingleton<ITenantResolutionStrategy, ClaimsTenantStrategy>();
```

**Real-world .NET examples:**
- `IComparer<T>` / `IEqualityComparer<T>` (sorting/hashing strategies)
- `JsonNamingPolicy` (camelCase, snake_case, etc.)
- Authentication schemes in ASP.NET Core (JWT, Cookie, OAuth -- pluggable strategies)
- `IDistributedCache` implementations (Redis, SQL Server, Memory)
- Polly resilience strategies (retry, circuit breaker, timeout)

**Framework/engine application:**
CephalonEngine is built on strategies: tenant resolution (`ITenantResolutionStrategy`), transport selection (`IBehaviorTransport` -- HTTP, gRPC, in-process), serialization (`IPayloadSerializer`), data provider selection, and authorization evaluation. The `BehaviorAllowedTransportsAttribute` and `BehaviorAllowedPatternsAttribute` configure which strategies are valid for each behavior. This is the primary extensibility mechanism.

---

### 3.9 Template Method (Framework Hooks, Lifecycle Methods)

**What it is:** The Template Method pattern defines the skeleton of an algorithm in a base class, letting subclasses override specific steps without changing the algorithm's structure.

**When to use it:**
- For framework lifecycle hooks (module initialization, startup, shutdown)
- When the overall algorithm is fixed but individual steps are customizable
- For plugin systems where plugins implement specific steps of a larger process

**Modern C#/.NET 10 implementation:**

```csharp
// Abstract base with template method
public abstract class CephalonModuleBase : ICephalonModule
{
    public string Name => GetType().Name.Replace("Module", "");

    // Template method: defines the skeleton
    public void Register(IServiceCollection services)
    {
        RegisterDefaults(services);     // step 1: framework defaults
        RegisterServices(services);     // step 2: module-specific (override)
        RegisterBehaviors(services);    // step 3: module-specific (override)
        RegisterValidation(services);   // step 4: optional (virtual)
    }

    private void RegisterDefaults(IServiceCollection services)
    {
        // Framework always does this
        services.AddLogging();
    }

    // Required override: modules must register their services
    protected abstract void RegisterServices(IServiceCollection services);

    // Required override: modules must register behaviors
    protected abstract void RegisterBehaviors(IServiceCollection services);

    // Optional override: validation is opt-in
    protected virtual void RegisterValidation(IServiceCollection services) { }
}

// Concrete module fills in the steps
public class CatalogModule : CephalonModuleBase
{
    protected override void RegisterServices(IServiceCollection services)
    {
        services.AddScoped<ICatalogRepository, CatalogRepository>();
    }

    protected override void RegisterBehaviors(IServiceCollection services)
    {
        services.AddScoped<IAppBehavior, CreateProductBehavior>();
        services.AddScoped<IAppBehavior, ListProductsBehavior>();
    }
}

// Hosted service with lifecycle template
public abstract class EngineBackgroundService : BackgroundService
{
    protected sealed override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await InitializeAsync(stoppingToken);
        try
        {
            await RunAsync(stoppingToken);
        }
        finally
        {
            await CleanupAsync();
        }
    }

    protected virtual Task InitializeAsync(CancellationToken ct) => Task.CompletedTask;
    protected abstract Task RunAsync(CancellationToken ct);
    protected virtual Task CleanupAsync() => Task.CompletedTask;
}
```

**Real-world .NET examples:**
- `BackgroundService.ExecuteAsync()` (template for hosted services)
- `ControllerBase` in ASP.NET MVC (OnActionExecuting, OnActionExecuted lifecycle)
- `DbContext.OnModelCreating()` (EF Core model configuration hook)
- `Startup.ConfigureServices()` / `Configure()` (ASP.NET Core startup template)
- xUnit's `IAsyncLifetime` (InitializeAsync/DisposeAsync test lifecycle)

**Framework/engine application:**
CephalonEngine's module system uses Template Method: `ICephalonModule` defines the registration skeleton, and concrete modules fill in the steps. The `IContributor` pattern (e.g., `IEventStoreContributor`, `IBehaviorContributor`) is a variant -- contributors implement specific steps of the registration process. Background services for outbox processing, projection replay, and health monitoring use `BackgroundService` as the template.

---

### 3.10 Visitor (AST Walking, Double Dispatch)

**What it is:** The Visitor pattern separates an algorithm from the object structure it operates on, allowing new operations to be added without modifying the objects.

**When to use it:**
- When you need to perform many unrelated operations on a structure of objects
- For traversing execution graphs, query expression trees, or schema definitions
- When you want to avoid cluttering domain objects with infrastructure concerns

**Modern C#/.NET 10 implementation:**

```csharp
// Execution graph node hierarchy
public abstract record ExecutionNode;
public record BehaviorNode(string Name, Type HandlerType) : ExecutionNode;
public record GatewayNode(string Name, GatewayKind Kind) : ExecutionNode;
public record SubGraphNode(string Name, IReadOnlyList<ExecutionNode> Children) : ExecutionNode;

// Visitor interface
public interface IExecutionNodeVisitor<out T>
{
    T Visit(BehaviorNode node);
    T Visit(GatewayNode node);
    T Visit(SubGraphNode node);
}

// Concrete visitor: generate Mermaid diagram from execution graph
public class MermaidDiagramVisitor : IExecutionNodeVisitor<string>
{
    public string Visit(BehaviorNode node) => $"    {node.Name}[/{node.Name}/]";
    public string Visit(GatewayNode node) => $"    {node.Name}{{{node.Name}}}";
    public string Visit(SubGraphNode node)
    {
        var children = string.Join("\n", node.Children.Select(c => c.Accept(this)));
        return $"    subgraph {node.Name}\n{children}\n    end";
    }
}

// Concrete visitor: validate execution graph
public class ValidationVisitor : IExecutionNodeVisitor<IReadOnlyList<string>>
{
    public IReadOnlyList<string> Visit(BehaviorNode node) =>
        node.HandlerType is null ? [$"Missing handler for {node.Name}"] : [];
    // ...
}

// Modern alternative: pattern matching (avoids visitor boilerplate for simple cases)
public static string Describe(ExecutionNode node) => node switch
{
    BehaviorNode b => $"Behavior: {b.Name}",
    GatewayNode g => $"Gateway: {g.Name} ({g.Kind})",
    SubGraphNode s => $"SubGraph: {s.Name} with {s.Children.Count} children",
    _ => "Unknown"
};
```

**Real-world .NET examples:**
- `System.Linq.Expressions.ExpressionVisitor` (LINQ expression tree walking)
- Roslyn's `CSharpSyntaxWalker` / `CSharpSyntaxRewriter`
- EF Core's query translation pipeline (expression visitors)
- ASP.NET Core's endpoint metadata traversal

**Framework/engine application:**
CephalonEngine's execution graph (`ExecutionGraphDescriptor` with nodes and edges) is a prime candidate for the Visitor pattern. Visitors can generate documentation, validate topology, generate Mermaid diagrams, compute metrics, or optimize the graph. The scaffolding system (`ScaffoldPlan`) also benefits -- a visitor can traverse the plan to generate project files. However, for simple cases, C# pattern matching (`switch` expressions) is preferred over the full visitor ceremony.

---

### 3.11 Specification (Query Building, Business Rules)

**What it is:** The Specification pattern encapsulates a business rule or query predicate into a reusable, composable object. Specifications can be combined with AND, OR, and NOT operators.

**When to use it:**
- For building composable query filters against repositories
- For validating business rules that can be combined
- For authorization rules that compose hierarchically
- When the same filter logic must work across in-memory collections and database queries

**Modern C#/.NET 10 implementation:**

```csharp
// Base specification using Expression<Func<T, bool>> for EF Core compatibility
public abstract class Specification<T>
{
    public abstract Expression<Func<T, bool>> ToExpression();

    public bool IsSatisfiedBy(T entity) => ToExpression().Compile()(entity);

    // Combinators
    public Specification<T> And(Specification<T> other) =>
        new AndSpecification<T>(this, other);
    public Specification<T> Or(Specification<T> other) =>
        new OrSpecification<T>(this, other);
    public Specification<T> Not() =>
        new NotSpecification<T>(this);
}

// Combinator implementations
public class AndSpecification<T>(Specification<T> left, Specification<T> right) : Specification<T>
{
    public override Expression<Func<T, bool>> ToExpression()
    {
        var leftExpr = left.ToExpression();
        var rightExpr = right.ToExpression();
        var param = Expression.Parameter(typeof(T));
        var body = Expression.AndAlso(
            Expression.Invoke(leftExpr, param),
            Expression.Invoke(rightExpr, param));
        return Expression.Lambda<Func<T, bool>>(body, param);
    }
}

// Concrete specifications
public class ActiveProductSpec : Specification<Product>
{
    public override Expression<Func<Product, bool>> ToExpression() =>
        p => p.IsActive && !p.IsDeleted;
}

public class InCategorySpec(string category) : Specification<Product>
{
    public override Expression<Func<Product, bool>> ToExpression() =>
        p => p.Category == category;
}

public class PriceRangeSpec(decimal min, decimal max) : Specification<Product>
{
    public override Expression<Func<Product, bool>> ToExpression() =>
        p => p.Price >= min && p.Price <= max;
}

// Usage: composable queries
var spec = new ActiveProductSpec()
    .And(new InCategorySpec("Electronics"))
    .And(new PriceRangeSpec(100, 500));

var products = await repository.FindAsync(spec);
```

**Real-world .NET examples:**
- Ardalis.Specification (popular .NET library)
- LinqKit's `PredicateBuilder` (expression composition)
- EF Core's `IQueryable<T>` (specifications as LINQ expressions)
- FluentValidation rules (validation specifications)

**Framework/engine application:**
CephalonEngine's authorization system uses specifications: authorization rules compose into trees (AND/OR/NOT of permissions, roles, claims). The data access layer benefits from specifications for building tenant-aware, filterable queries. The `IBehaviorCompatibilityRule` abstraction is a specification that evaluates whether a behavior configuration is valid. Specifications keep query logic reusable across behavior handlers, projections, and API endpoints.

---

## 4. Concurrency Patterns

### 4.1 Producer-Consumer (Channels, Queues)

**What it is:** The Producer-Consumer pattern decouples components that produce data from components that consume it, using a shared buffer. Producers add items to the buffer; consumers take items from it.

**When to use it:**
- For background processing pipelines (outbox dispatch, event processing)
- When producers and consumers run at different speeds (backpressure management)
- For fan-out/fan-in processing of domain events
- For rate-limited processing queues

**Modern C#/.NET 10 implementation:**

```csharp
// System.Threading.Channels -- the modern .NET producer-consumer primitive
public class OutboxProcessor : BackgroundService
{
    private readonly Channel<OutboxMessage> _channel;
    private readonly IOutbox _outbox;

    public OutboxProcessor(IOutbox outbox)
    {
        _outbox = outbox;
        _channel = Channel.CreateBounded<OutboxMessage>(new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });
    }

    // Producer: stage messages
    public async ValueTask StageAsync(OutboxMessage message, CancellationToken ct)
    {
        await _channel.Writer.WriteAsync(message, ct);
    }

    // Consumer: process messages in background
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            await _outbox.PublishAsync(message, stoppingToken);
        }
    }
}

// Multi-producer, multi-consumer with unbounded channel
Channel<Coordinates> channel = Channel.CreateUnbounded<Coordinates>(
    new UnboundedChannelOptions
    {
        SingleWriter = false,
        SingleReader = false
    });
```

**Real-world .NET examples:**
- `System.Threading.Channels` (Channel<T>) -- the primary primitive
- `BlockingCollection<T>` (older, synchronous alternative)
- `BufferBlock<T>` (TPL Dataflow)
- ASP.NET Core's Kestrel uses channels for socket I/O
- MassTransit's in-memory transport uses channels

**Framework/engine application:**
CephalonEngine's outbox processor, projection rebuild pipeline, and event dispatch system are all producer-consumer. Behavior handlers (producers) stage events into channels; background services (consumers) process them. Bounded channels with `BoundedChannelFullMode.Wait` provide built-in backpressure. The `OutboxStagingBenchmarks` should measure channel throughput under load.

---

### 4.2 Pipeline (System.IO.Pipelines)

**What it is:** The Pipeline pattern processes data through a series of stages, where each stage performs a transformation and passes the result to the next stage. `System.IO.Pipelines` is the .NET implementation optimized for high-throughput I/O.

**When to use it:**
- For high-throughput network protocol parsing (transport layer)
- When you need zero-copy, backpressure-aware data processing
- For streaming data transformations (read -> parse -> validate -> dispatch)

**Modern C#/.NET 10 implementation:**

```csharp
// System.IO.Pipelines for high-performance I/O
async Task ProcessConnectionAsync(Socket socket)
{
    var pipe = new Pipe(new PipeOptions(
        pauseWriterThreshold: 64 * 1024,
        resumeWriterThreshold: 32 * 1024));

    Task writing = FillPipeAsync(socket, pipe.Writer);
    Task reading = ReadPipeAsync(pipe.Reader);

    await Task.WhenAll(reading, writing);
}

async Task FillPipeAsync(Socket socket, PipeWriter writer)
{
    while (true)
    {
        Memory<byte> memory = writer.GetMemory(512);
        int bytesRead = await socket.ReceiveAsync(memory, SocketFlags.None);
        if (bytesRead == 0) break;
        writer.Advance(bytesRead);
        FlushResult result = await writer.FlushAsync();
        if (result.IsCompleted) break;
    }
    await writer.CompleteAsync();
}

async Task ReadPipeAsync(PipeReader reader)
{
    while (true)
    {
        ReadResult result = await reader.ReadAsync();
        ReadOnlySequence<byte> buffer = result.Buffer;

        while (TryParseMessage(ref buffer, out var message))
            ProcessMessage(message);

        reader.AdvanceTo(buffer.Start, buffer.End);
        if (result.IsCompleted) break;
    }
    await reader.CompleteAsync();
}

// Logical pipeline using channels (higher-level)
public class EventProcessingPipeline
{
    private readonly Channel<RawEvent> _stage1 = Channel.CreateBounded<RawEvent>(1000);
    private readonly Channel<ValidatedEvent> _stage2 = Channel.CreateBounded<ValidatedEvent>(1000);
    private readonly Channel<DispatchedEvent> _stage3 = Channel.CreateBounded<DispatchedEvent>(1000);

    public async Task RunAsync(CancellationToken ct)
    {
        await Task.WhenAll(
            DeserializeStage(ct),
            ValidateStage(ct),
            DispatchStage(ct));
    }
}
```

**Real-world .NET examples:**
- Kestrel HTTP server (uses `System.IO.Pipelines` for request parsing)
- SignalR protocol parsing
- `JsonSerializer.SerializeAsync` / `DeserializeAsync` with `PipeWriter`/`PipeReader`
- gRPC message framing

**Framework/engine application:**
CephalonEngine's transport layer should use `System.IO.Pipelines` for high-throughput network protocol parsing. The behavior HTTP transport bridge reads request bodies through `PipeReader` for zero-copy deserialization. For logical pipelines (event processing, projection rebuild), channel-based multi-stage pipelines provide the right abstraction.

---

### 4.3 Actor Model (Message-Passing Concurrency)

**What it is:** The Actor Model treats "actors" as the fundamental unit of computation. Each actor has its own state, processes messages sequentially from a mailbox, and communicates with other actors only through messages.

**When to use it:**
- For managing per-entity concurrent state (per-tenant, per-aggregate)
- When you need to eliminate shared mutable state
- For distributed systems where entities are spread across nodes

**Modern C#/.NET 10 implementation:**

```csharp
// Lightweight actor using Channel<T> as a mailbox
public abstract class Actor<TMessage> : IAsyncDisposable
{
    private readonly Channel<TMessage> _mailbox = Channel.CreateUnbounded<TMessage>();
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _processingLoop;

    protected Actor()
    {
        _processingLoop = Task.Run(ProcessMailboxAsync);
    }

    public ValueTask SendAsync(TMessage message) =>
        _mailbox.Writer.WriteAsync(message);

    private async Task ProcessMailboxAsync()
    {
        await foreach (var message in _mailbox.Reader.ReadAllAsync(_cts.Token))
        {
            await HandleAsync(message); // sequential processing -- no concurrency
        }
    }

    protected abstract Task HandleAsync(TMessage message);

    public async ValueTask DisposeAsync()
    {
        _mailbox.Writer.Complete();
        await _cts.CancelAsync();
        await _processingLoop;
        _cts.Dispose();
    }
}

// Aggregate actor: one actor per aggregate instance
public class OrderActor : Actor<OrderCommand>
{
    private OrderAggregate _state = new();

    protected override async Task HandleAsync(OrderCommand command)
    {
        // Sequential processing guarantees consistency
        _state.Handle(command);
        await PersistEventsAsync(_state.GetUncommittedEvents());
    }
}
```

**Real-world .NET examples:**
- Microsoft Orleans ("virtual actor" framework, now part of .NET Aspire)
- Akka.NET (full actor framework)
- Dapr actors (distributed actors)
- Proto.Actor (lightweight actor framework)

**Framework/engine application:**
CephalonEngine can use the actor model for per-aggregate concurrency in event sourcing: each aggregate has its own actor that processes commands sequentially, eliminating concurrency conflicts. The lightweight channel-based actor is sufficient for in-process scenarios; Orleans integration enables distributed scenarios. Tenant-scoped state management (caches, rate limiters) also maps to the actor model.

---

### 4.4 Async/Await Patterns (ValueTask, ConfigureAwait, Async Disposal)

**What it is:** Modern C# async/await patterns encompass `ValueTask` for allocation-free async returns, `ConfigureAwait` for context management, `IAsyncDisposable` for async cleanup, and `IAsyncEnumerable` for async streaming.

**When to use it:**
- `ValueTask<T>`: when the async method frequently completes synchronously (cache hits, pooled results)
- `ConfigureAwait(false)`: in library/framework code to avoid capturing the synchronization context
- `IAsyncDisposable`: when cleanup requires async I/O (flushing, closing connections)
- Everywhere in a framework -- these are not optional patterns

**Modern C#/.NET 10 implementation:**

```csharp
// ValueTask for hot-path methods that often complete synchronously
public class CachingBehaviorResolver(
    IBehaviorCatalog catalog,
    IMemoryCache cache)
{
    public ValueTask<BehaviorTopologyDescriptor?> ResolveAsync(string name)
    {
        if (cache.TryGetValue<BehaviorTopologyDescriptor>(name, out var cached))
            return ValueTask.FromResult<BehaviorTopologyDescriptor?>(cached);

        return ResolveSlowPathAsync(name);
    }

    private async ValueTask<BehaviorTopologyDescriptor?> ResolveSlowPathAsync(string name)
    {
        var descriptor = await catalog.ResolveAsync(name);
        if (descriptor is not null)
            cache.Set(name, descriptor, TimeSpan.FromMinutes(5));
        return descriptor;
    }
}

// ConfigureAwait(false) in framework code
public class EventStoreWriter(IEventStore store)
{
    public async Task AppendAsync(string streamId, IReadOnlyList<IDomainEvent> events, CancellationToken ct)
    {
        await store.AppendAsync(streamId, events, ct).ConfigureAwait(false);
        await NotifySubscribersAsync(streamId, events, ct).ConfigureAwait(false);
    }
}

// IAsyncDisposable for resource cleanup
public class TenantScope : IAsyncDisposable
{
    private readonly DbConnection _connection;
    private readonly IDbTransaction _transaction;

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (_transaction is not null)
            await _transaction.DisposeAsync().ConfigureAwait(false);
        if (_connection is not null)
            await _connection.DisposeAsync().ConfigureAwait(false);
    }
}

// Cancellation token propagation
public async Task<TResult> DispatchAsync<TResult>(
    ICommand<TResult> command,
    CancellationToken ct = default)
{
    ct.ThrowIfCancellationRequested();
    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
    linkedCts.CancelAfter(TimeSpan.FromSeconds(30)); // timeout
    return await _handler.HandleAsync(command, linkedCts.Token).ConfigureAwait(false);
}
```

**Real-world .NET examples:**
- `ValueTask` used throughout ASP.NET Core (Kestrel, middleware)
- `ConfigureAwait(false)` used in all .NET libraries (EF Core, HttpClient, etc.)
- `IAsyncDisposable` / `await using` for streams, connections, transactions
- `CancellationToken` propagation through every ASP.NET Core middleware

**Framework/engine application:**
These patterns are foundational to CephalonEngine. Every async method in the framework must: (1) use `ConfigureAwait(false)` since it's library code, (2) accept and propagate `CancellationToken`, (3) prefer `ValueTask<T>` on hot paths where synchronous completion is common, (4) implement `IAsyncDisposable` for resources that require async cleanup. The benchmarks should validate that `ValueTask` paths avoid allocations.

---

### 4.5 Semaphore/Throttling Patterns

**What it is:** Semaphore and throttling patterns limit the number of concurrent operations, preventing resource exhaustion and ensuring fair access to shared resources.

**When to use it:**
- For rate-limiting API calls to external services
- For controlling database connection concurrency per tenant
- For limiting parallel projection rebuilds or batch operations
- For bulkhead isolation (preventing one slow consumer from starving others)

**Modern C#/.NET 10 implementation:**

```csharp
// SemaphoreSlim for async throttling
public class ThrottledEventPublisher(IOutbox outbox)
{
    private readonly SemaphoreSlim _semaphore = new(maxCount: 10, initialCount: 10);

    public async Task PublishBatchAsync(IReadOnlyList<OutboxMessage> messages, CancellationToken ct)
    {
        var tasks = messages.Select(async msg =>
        {
            await _semaphore.WaitAsync(ct);
            try
            {
                await outbox.PublishAsync(msg, ct);
            }
            finally
            {
                _semaphore.Release();
            }
        });
        await Task.WhenAll(tasks);
    }
}

// .NET rate limiting middleware (built-in since .NET 7)
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("api", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 100;
        opt.QueueLimit = 10;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });

    options.AddSlidingWindowLimiter("behavior-dispatch", opt =>
    {
        opt.Window = TimeSpan.FromSeconds(10);
        opt.SegmentsPerWindow = 5;
        opt.PermitLimit = 50;
    });

    options.AddConcurrencyLimiter("projection-rebuild", opt =>
    {
        opt.PermitLimit = 3;
        opt.QueueLimit = 10;
    });
});

// Per-tenant throttling
public class TenantThrottler
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _tenantSemaphores = new();

    public SemaphoreSlim GetSemaphore(string tenantId, int maxConcurrency = 5)
    {
        return _tenantSemaphores.GetOrAdd(tenantId, _ => new SemaphoreSlim(maxConcurrency));
    }
}
```

**Real-world .NET examples:**
- `System.Threading.RateLimiting` (built-in rate limiters since .NET 7)
- ASP.NET Core rate limiting middleware
- `SemaphoreSlim` (async-compatible semaphore)
- Polly's Bulkhead isolation strategy
- EF Core connection pool limits

**Framework/engine application:**
CephalonEngine needs throttling at multiple levels: per-tenant request rate limiting, projection rebuild concurrency limits, outbox dispatch throttling, and external service call rate limiting. The built-in `System.Threading.RateLimiting` provides the primitives; CephalonEngine wraps them in tenant-aware, configurable policies. The bulkhead pattern isolates tenants so one noisy tenant cannot starve others.

---

## 5. Enterprise / Integration Patterns

### 5.1 Unit of Work + Repository

**What it is:** The Unit of Work pattern maintains a list of objects affected by a business transaction and coordinates writing out changes and resolving concurrency problems. The Repository pattern mediates between the domain and data mapping layers using a collection-like interface.

**When to use it:**
- When multiple repositories must participate in a single transaction
- When you need a clean separation between domain logic and data access
- When change tracking and atomic commits are required

**Modern C#/.NET 10 implementation:**

```csharp
// Unit of Work abstraction
public interface IUnitOfWork
{
    IRepository<T> Repository<T>() where T : class;
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitTransactionAsync(CancellationToken ct = default);
    Task RollbackTransactionAsync(CancellationToken ct = default);
}

// Repository abstraction
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> FindAsync(Specification<T> spec, CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    void Update(T entity);
    void Remove(T entity);
}

// EF Core implementation: DbContext IS the Unit of Work
public class CephalonDbContext(DbContextOptions options) : DbContext(options), IUnitOfWork
{
    public IRepository<T> Repository<T>() where T : class =>
        new EfRepository<T>(Set<T>());

    // SaveChangesAsync is already implemented by DbContext
    // Transaction management delegates to Database.BeginTransactionAsync()
}

// Behavior handler using Unit of Work
public class PlaceOrderHandler(IUnitOfWork uow, IOutbox outbox) : ICommandHandler<PlaceOrderCommand, Guid>
{
    public async Task<Guid> HandleAsync(PlaceOrderCommand command, CancellationToken ct)
    {
        var order = new Order(command.CustomerId, command.Lines);
        await uow.Repository<Order>().AddAsync(order, ct);

        var outboxMessage = new OutboxMessage(new OrderPlacedEvent(order.Id));
        await outbox.StageAsync(outboxMessage, ct);

        await uow.SaveChangesAsync(ct); // atomic: order + outbox message
        return order.Id;
    }
}
```

**Real-world .NET examples:**
- EF Core's `DbContext` (the canonical Unit of Work + Repository)
- Marten's `IDocumentSession` (Unit of Work for document/event storage)
- NHibernate's `ISession`
- Dapper + custom UoW wrapping `IDbTransaction`

**Framework/engine application:**
CephalonEngine's `IWriteStore` / `IReadStore` abstractions are repositories. The Unit of Work ensures that domain operations and outbox messages are committed atomically. EF Core's `DbContext` serves as the primary UoW implementation, but the abstractions allow Marten (for event sourcing) or raw ADO.NET as alternatives. The key insight: the outbox message must be part of the same UoW as the domain change.

---

### 5.2 Domain Events

**What it is:** Domain Events represent something significant that happened in the domain. They are raised by aggregates and handled by other parts of the system (other aggregates, projections, external integrations).

**When to use it:**
- For inter-module communication without direct coupling
- For maintaining eventual consistency across bounded contexts
- For triggering side effects (send email, update projection, publish to message broker)
- As the foundation of event sourcing

**Modern C#/.NET 10 implementation:**

```csharp
// Domain event abstraction (already in CephalonEngine)
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredAt { get; }
}

public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}

// Concrete domain events
public record OrderPlacedEvent(Guid OrderId, Guid CustomerId, decimal Total) : DomainEvent;
public record OrderShippedEvent(Guid OrderId, string TrackingNumber) : DomainEvent;

// Aggregate raises events
public class OrderAggregate : Aggregate
{
    public void Place(Guid customerId, decimal total)
    {
        RaiseEvent(new OrderPlacedEvent(Id, customerId, total));
    }

    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case OrderPlacedEvent e:
                _customerId = e.CustomerId;
                _total = e.Total;
                _status = "placed";
                break;
            case OrderShippedEvent e:
                _trackingNumber = e.TrackingNumber;
                _status = "shipped";
                break;
        }
    }
}

// Domain event handlers (cross-module reactions)
public class WhenOrderPlaced_CreateInvoice(IInvoiceService invoices)
    : IDomainEventHandler<OrderPlacedEvent>
{
    public async Task HandleAsync(OrderPlacedEvent @event, CancellationToken ct)
    {
        await invoices.CreateAsync(@event.OrderId, @event.Total, ct);
    }
}
```

**Real-world .NET examples:**
- MediatR's `INotification` / `INotificationHandler<T>` (in-process domain events)
- Wolverine's event publishing
- MassTransit message publishing (distributed domain events)
- Marten's event projections

**Framework/engine application:**
Domain events are the primary integration mechanism between CephalonEngine modules. The `DomainEvent` base record and `IDomainEvent` interface are already defined in the abstractions. Events flow through the outbox for reliable delivery and feed projections for read-model materialization. The behavior system raises events; the projection system and other modules consume them.

---

### 5.3 Saga / Process Manager

**What it is:** A Saga is a sequence of local transactions, where each transaction updates data within a single service. If a step fails, the saga executes compensating transactions to undo previous steps. A Process Manager is a stateful coordinator that tracks saga progress.

**When to use it:**
- For long-running business processes that span multiple modules or services
- When distributed transactions (2PC) are not feasible
- For order fulfillment, payment processing, onboarding workflows

**Modern C#/.NET 10 implementation:**

```csharp
// Saga state
public class OrderFulfillmentSaga
{
    public Guid OrderId { get; set; }
    public string CurrentState { get; set; } = "initial";
    public bool PaymentReceived { get; set; }
    public bool StockReserved { get; set; }
    public bool ShipmentCreated { get; set; }
}

// Orchestration-based saga
public class OrderFulfillmentOrchestrator(
    IPaymentService payments,
    IInventoryService inventory,
    IShippingService shipping)
{
    public async Task ExecuteAsync(OrderFulfillmentSaga saga, CancellationToken ct)
    {
        try
        {
            // Step 1: Reserve payment
            await payments.AuthorizeAsync(saga.OrderId, ct);
            saga.PaymentReceived = true;

            // Step 2: Reserve stock
            await inventory.ReserveStockAsync(saga.OrderId, ct);
            saga.StockReserved = true;

            // Step 3: Create shipment
            await shipping.CreateShipmentAsync(saga.OrderId, ct);
            saga.ShipmentCreated = true;

            saga.CurrentState = "completed";
        }
        catch (Exception)
        {
            // Compensating transactions
            if (saga.StockReserved)
                await inventory.ReleaseStockAsync(saga.OrderId, ct);
            if (saga.PaymentReceived)
                await payments.RefundAsync(saga.OrderId, ct);

            saga.CurrentState = "compensated";
            throw;
        }
    }
}

// Choreography-based saga (event-driven)
public class WhenPaymentReceived_ReserveStock(IInventoryService inventory)
    : IDomainEventHandler<PaymentReceivedEvent>
{
    public async Task HandleAsync(PaymentReceivedEvent @event, CancellationToken ct)
    {
        await inventory.ReserveStockAsync(@event.OrderId, ct);
        // If this fails, a compensating event is published
    }
}
```

**Real-world .NET examples:**
- MassTransit Sagas / `MassTransitStateMachine<T>`
- NServiceBus Sagas
- Wolverine's `IStartedByMessage` / `IMessageHandler` saga pattern
- Elsa Workflows for complex process management

**Framework/engine application:**
CephalonEngine's `IProcessCompletion` abstraction hints at saga support. The execution graph (`ExecutionGraphDescriptor`) can model saga flows. Sagas coordinate across modules -- the OrdersModule orchestrates payment, inventory, and shipping modules. The outbox pattern ensures reliable message delivery between saga steps. MassTransit integration provides state machine sagas for production use.

---

### 5.4 Outbox Pattern

**What it is:** The Outbox pattern ensures that domain changes and event publication happen atomically by storing outgoing messages in the same database transaction as the domain change. A background processor then reliably publishes the messages to the message broker.

**When to use it:**
- Whenever you need to publish events reliably after a domain change
- When message broker unavailability should not cause data loss
- For ensuring at-least-once delivery of domain events
- As the foundation of reliable inter-module communication

**Modern C#/.NET 10 implementation:**

```csharp
// CephalonEngine already has outbox abstractions
public interface IOutbox
{
    Task StageAsync(OutboxMessage message, CancellationToken ct);
}

public record OutboxMessage(
    Guid Id,
    string Type,
    byte[] Payload,
    DateTime CreatedAt,
    DateTime? ProcessedAt = null);

// Outbox staging within the UoW transaction
public class EfOutbox(CephalonDbContext db) : IOutbox
{
    public async Task StageAsync(OutboxMessage message, CancellationToken ct)
    {
        await db.OutboxMessages.AddAsync(message, ct);
        // NOT calling SaveChanges -- the UoW will commit this with the domain change
    }
}

// Background outbox processor
public class OutboxProcessor(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxProcessor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CephalonDbContext>();
            var publisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();

            var pending = await db.OutboxMessages
                .Where(m => m.ProcessedAt == null)
                .OrderBy(m => m.CreatedAt)
                .Take(100)
                .ToListAsync(stoppingToken);

            foreach (var message in pending)
            {
                await publisher.PublishAsync(message, stoppingToken);
                message.ProcessedAt = DateTime.UtcNow;
            }

            await db.SaveChangesAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }
}
```

**Real-world .NET examples:**
- MassTransit's transactional outbox (EF Core + RabbitMQ/Azure Service Bus)
- Wolverine's built-in outbox
- NServiceBus outbox
- Marten's outbox integration

**Framework/engine application:**
CephalonEngine already defines `IOutbox`, `IOutboxCatalog`, `IOutboxContributor`, `IOutboxRegistry`, `OutboxDescriptor`, and `OutboxMessage`. This is a first-class concept. The outbox ensures that when a behavior handler modifies domain state, the resulting events are reliably published. The `OutboxStagingBenchmarks` measure staging throughput. Contributors can plug in EF Core, Marten, or custom implementations.

---

### 5.5 Circuit Breaker

**What it is:** The Circuit Breaker pattern prevents an application from repeatedly attempting an operation that is likely to fail. It monitors failures and "opens the circuit" after a threshold, failing fast instead of wasting resources on doomed requests.

**States:** Closed (normal) -> Open (failing fast) -> Half-Open (testing recovery)

**When to use it:**
- For calls to external services (APIs, databases, message brokers)
- When cascading failures could bring down the entire system
- For protecting slow dependencies from overwhelming your service

**Modern C#/.NET 10 implementation:**

```csharp
// Polly v8+ with Microsoft.Extensions.Resilience
builder.Services.AddResiliencePipeline("external-api", static builder =>
{
    builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions
    {
        SamplingDuration = TimeSpan.FromSeconds(10),
        FailureRatio = 0.5,
        MinimumThroughput = 8,
        BreakDuration = TimeSpan.FromSeconds(30),
        ShouldHandle = new PredicateBuilder()
            .Handle<HttpRequestException>()
            .Handle<TimeoutException>()
    });
});

// Usage with DI
public class ExternalApiClient(
    ResiliencePipelineProvider<string> pipelineProvider,
    HttpClient httpClient)
{
    public async Task<string> GetDataAsync(CancellationToken ct)
    {
        var pipeline = pipelineProvider.GetPipeline("external-api");
        return await pipeline.ExecuteAsync(async token =>
        {
            var response = await httpClient.GetStringAsync("/api/data", token);
            return response;
        }, ct);
    }
}

// HttpClient-integrated circuit breaker
builder.Services.AddHttpClient("catalog-api")
    .AddResilienceHandler("catalog", static builder =>
    {
        builder.AddRetry(new HttpRetryStrategyOptions
        {
            BackoffType = DelayBackoffType.Exponential,
            MaxRetryAttempts = 3,
            UseJitter = true
        });
        builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
        {
            SamplingDuration = TimeSpan.FromSeconds(10),
            FailureRatio = 0.2,
            MinimumThroughput = 3
        });
        builder.AddTimeout(TimeSpan.FromSeconds(5));
    });
```

**Real-world .NET examples:**
- Polly v8 `CircuitBreakerStrategyOptions` (the standard)
- `Microsoft.Extensions.Http.Resilience` (built-in HTTP resilience)
- `Microsoft.Extensions.Resilience` (general resilience pipeline)
- Azure SDK retry policies

**Framework/engine application:**
CephalonEngine should provide circuit breaker integration for all external calls: remote behavior dispatch (cross-module HTTP/gRPC calls), external event store connections, message broker publishing, and external API integrations. The engine's resilience configuration should be a first-class concern in the builder: `engine.AddResilience(r => r.AddCircuitBreaker(...))`. The Polly v8 pipeline model integrates directly with `IHttpClientFactory`.

---

### 5.6 Retry with Exponential Backoff

**What it is:** The Retry pattern automatically re-executes a failed operation with increasing delays between attempts. Exponential backoff increases the delay geometrically, and jitter adds randomness to prevent thundering herds.

**When to use it:**
- For transient failures (network timeouts, temporary service unavailability)
- For database deadlock retries
- For message broker connection recovery
- Always paired with circuit breaker (retry within the circuit breaker)

**Modern C#/.NET 10 implementation:**

```csharp
// Polly v8 retry with exponential backoff + jitter
builder.Services.AddResiliencePipeline("database", static builder =>
{
    builder.AddRetry(new RetryStrategyOptions
    {
        MaxRetryAttempts = 5,
        BackoffType = DelayBackoffType.Exponential,
        UseJitter = true,
        Delay = TimeSpan.FromMilliseconds(200),
        ShouldHandle = new PredicateBuilder()
            .Handle<DbUpdateConcurrencyException>()
            .Handle<TimeoutException>()
    });
});

// HTTP retry with Retry-After header support
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy() =>
    HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(
            retryCount: 5,
            sleepDurationProvider: (count, response, context) =>
            {
                if (response.Result?.Headers.Contains("Retry-After") == true)
                {
                    var seconds = int.Parse(
                        response.Result.Headers.GetValues("Retry-After").First());
                    return TimeSpan.FromSeconds(seconds);
                }
                return TimeSpan.FromSeconds(Math.Pow(2, count));
            },
            onRetryAsync: (_, _, _, _) => Task.CompletedTask);

// EF Core built-in retry
builder.Services.AddDbContext<CephalonDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql =>
        npgsql.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null)));
```

**Real-world .NET examples:**
- Polly `RetryStrategyOptions` with `DelayBackoffType.Exponential`
- EF Core's `EnableRetryOnFailure()` (built-in retry for SQL Server, Postgres)
- Azure SDK automatic retries
- gRPC retry policies

**Framework/engine application:**
CephalonEngine should layer retry policies at multiple levels: behavior dispatch (retry on transient transport failures), data access (retry on deadlocks/timeouts via EF Core's built-in retry), outbox processing (retry on broker unavailability), and external API calls (retry with backoff). The retry policy should be configurable per-behavior via `BehaviorTopologyDescriptor` metadata.

---

### 5.7 Bulkhead Isolation

**What it is:** The Bulkhead pattern isolates elements of an application into pools so that if one fails, the others continue to function. Named after ship bulkheads that contain flooding to one compartment.

**When to use it:**
- For tenant isolation (one tenant's load cannot affect another)
- For isolating critical paths from non-critical ones
- For preventing a slow dependency from consuming all threads/connections
- For protecting shared resources (connection pools, thread pools)

**Modern C#/.NET 10 implementation:**

```csharp
// Polly bulkhead (concurrency limiter)
builder.Services.AddResiliencePipeline("critical-path", static builder =>
{
    builder.AddConcurrencyLimiter(new ConcurrencyLimiterOptions
    {
        PermitLimit = 25,
        QueueLimit = 50
    });
});

// .NET rate limiting as bulkhead
builder.Services.AddRateLimiter(options =>
{
    options.AddConcurrencyLimiter("projection-rebuild", opt =>
    {
        opt.PermitLimit = 3;    // max 3 concurrent projection rebuilds
        opt.QueueLimit = 10;    // queue up to 10 more
    });
});

// Tenant-level bulkhead via semaphore pools
public class TenantBulkhead
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _bulkheads = new();

    public async Task<T> ExecuteAsync<T>(string tenantId, Func<Task<T>> action, CancellationToken ct)
    {
        var semaphore = _bulkheads.GetOrAdd(tenantId, _ => new SemaphoreSlim(10));
        await semaphore.WaitAsync(ct);
        try
        {
            return await action();
        }
        finally
        {
            semaphore.Release();
        }
    }
}

// Connection pool isolation per tenant
public class TenantDbContextFactory(IConfiguration config)
{
    // Each tenant gets its own connection pool
    public CephalonDbContext CreateForTenant(string tenantId)
    {
        var connString = config.GetConnectionString(tenantId);
        var options = new DbContextOptionsBuilder<CephalonDbContext>()
            .UseNpgsql(connString, npgsql => npgsql
                .MaxPoolSize(20) // tenant-specific pool limit
                .EnableRetryOnFailure())
            .Options;
        return new CephalonDbContext(options);
    }
}
```

**Real-world .NET examples:**
- Polly's `ConcurrencyLimiterOptions` (bulkhead strategy)
- ASP.NET Core's `ConcurrencyLimiter` middleware
- `SemaphoreSlim` for manual bulkheading
- ADO.NET connection pool per connection string (implicit bulkhead)
- Kubernetes resource limits (infrastructure-level bulkhead)

**Framework/engine application:**
Bulkhead isolation is critical for CephalonEngine's multi-tenant architecture. Each tenant should have isolated: (1) connection pool limits, (2) behavior dispatch concurrency limits, (3) background processing quotas, (4) memory cache partitions. The engine's tenant resolution middleware can attach bulkhead policies per-tenant. This prevents the "noisy neighbor" problem that plagues multi-tenant systems.

---

## Cross-Cutting: How These Patterns Compose in CephalonEngine

The patterns above do not exist in isolation. In a framework like CephalonEngine, they compose into a coherent architecture:

```
Request Flow (patterns in action):
1. HTTP Request arrives
2. Chain of Responsibility: middleware pipeline processes request
   - Strategy: tenant resolution strategy resolves tenant
   - Proxy: caching proxy looks up behavior topology
   - Specification: authorization rules evaluate access
3. Mediator: routes to appropriate behavior handler
4. Command: behavior handler executes business logic
5. Observer: domain events raised to all subscribers
6. Unit of Work: domain changes + outbox staged atomically
7. Factory: tenant-specific data providers created
8. Memento: aggregate snapshot taken if threshold reached
9. Producer-Consumer: outbox processor publishes events
10. Circuit Breaker + Retry: external calls protected

Engine Bootstrap (patterns in action):
1. Builder: EngineBuilder configures modules, services, pipeline
2. Template Method: modules register services in lifecycle hooks
3. Composite: module tree resolved (parent modules contain sub-modules)
4. Singleton: registries, catalogs, configuration frozen
5. Flyweight: descriptors cached as frozen collections
6. Facade: CephalonRuntime exposes unified API
```

---

## Sources

- [Microsoft .NET Dependency Injection](https://learn.microsoft.com/dotnet/core/extensions/dependency-injection/overview)
- [ASP.NET Core Middleware](https://learn.microsoft.com/aspnet/core/fundamentals/middleware/?view=aspnetcore-10.0)
- [System.Threading.Channels](https://learn.microsoft.com/dotnet/core/extensions/channels)
- [System.IO.Pipelines](https://learn.microsoft.com/dotnet/standard/io/pipelines)
- [.NET Resilience with Polly](https://learn.microsoft.com/dotnet/core/resilience/)
- [Circuit Breaker Pattern](https://learn.microsoft.com/dotnet/architecture/microservices/implement-resilient-applications/implement-circuit-breaker-pattern)
- [Bulkhead Pattern](https://learn.microsoft.com/azure/architecture/patterns/bulkhead)
- [Saga Pattern](https://learn.microsoft.com/azure/architecture/patterns/saga)
- [IAsyncEnumerable and Async Streams](https://learn.microsoft.com/dotnet/csharp/asynchronous-programming/generate-consume-asynchronous-stream)
- [IAsyncDisposable](https://learn.microsoft.com/dotnet/fundamentals/runtime-libraries/system-iasyncdisposable)
- [Observer Design Pattern in .NET](https://learn.microsoft.com/dotnet/standard/events/observer-design-pattern)
- [Framework Design Guidelines](https://learn.microsoft.com/dotnet/standard/design-guidelines/)
- [ObjectPool in ASP.NET Core](https://learn.microsoft.com/aspnet/core/performance/memory?view=aspnetcore-10.0)
- [Domain Events Design and Implementation](https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-events-design-implementation)
- [Retry with Exponential Backoff](https://learn.microsoft.com/dotnet/architecture/microservices/implement-resilient-applications/implement-http-call-retries-exponential-backoff-polly)
- [Refactoring Guru: Design Patterns](https://refactoring.guru/design-patterns)
- [MassTransit Transactional Outbox](https://masstransit.io/documentation/patterns/transactional-outbox)
