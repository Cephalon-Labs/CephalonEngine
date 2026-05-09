# Architecture Patterns & Principles for CephalonEngine

> Comprehensive research for building a modular-monolith .NET 10 framework/engine.
> Compiled April 2026.

---

## Table of Contents

1. [Architecture Patterns](#1-architecture-patterns)
2. [SOLID Principles in Framework Design](#2-solid-principles-in-framework-design)
3. [DDD for Framework Authors](#3-ddd-for-framework-authors)
4. [Framework Design Principles](#4-framework-design-principles)
5. [Messaging and Communication Patterns](#5-messaging-and-communication-patterns)
6. [Resilience and Observability](#6-resilience-and-observability)

---

## 1. Architecture Patterns

### 1.1 Clean Architecture

**Core Idea:** Business logic sits at the center, with dependencies pointing inward. Infrastructure, UI, and frameworks are outer-ring concerns that depend on the core -- never the reverse. Robert C. Martin formalized this; Microsoft adopted it as the recommended approach in their [eShopOnWeb reference](https://learn.microsoft.com/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures#clean-architecture).

**Layers (inside-out):**

| Layer | Contents | Dependencies |
|-------|----------|-------------|
| **Domain / Entities** | Aggregates, Entities, Value Objects, Domain Events, Specifications | None -- the innermost ring |
| **Application Core** | Use Cases (Commands/Queries), Application Services, Interfaces (ports), DTOs | Domain only |
| **Infrastructure** | EF Core DbContext, Repositories, External service clients, File I/O, Email | Application Core interfaces |
| **Presentation / UI** | Controllers, Minimal API endpoints, Razor Pages, ViewModels | Application Core interfaces |

**The Dependency Rule:** Source-code dependencies can only point inward. The Application Core defines `IOrderRepository`; the Infrastructure layer provides `OrderRepository : IOrderRepository`. At runtime, DI wires them together in the composition root (`Program.cs`).

**Applying to CephalonEngine:** The engine already follows this -- `Cephalon.Abstractions` is the innermost ring defining contracts, `Cephalon.Engine` holds core logic, and `Cephalon.Data.*` / `Cephalon.AspNetCore.*` are infrastructure adapters. When building a framework, the key insight is: **the framework's extension points (interfaces, abstract base classes) live in the core packages**, while concrete implementations ship as separate NuGet packages that framework consumers can swap.

**Enforcement:** Use [NetArchTest](https://github.com/BenMorris/NetArchTest) or ArchUnitNET to write unit tests that verify your dependency rules:

```csharp
[Fact]
public void Domain_Should_Not_Reference_Infrastructure()
{
    var result = Types.InAssembly(typeof(Order).Assembly)
        .ShouldNot()
        .HaveDependencyOn("Cephalon.Data.EntityFramework")
        .GetResult();
    Assert.True(result.IsSuccessful);
}
```

**Sources:**
- [Microsoft: Common Web Application Architectures](https://learn.microsoft.com/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures#clean-architecture)
- [Milan Jovanovic: Building Your First Use Case With Clean Architecture](https://www.milanjovanovic.tech/blog/building-your-first-use-case-with-clean-architecture)

---

### 1.2 Hexagonal Architecture / Ports and Adapters

**Core Idea:** Isolate the application core from all external concerns (databases, UIs, messaging systems) through explicitly defined **ports** (interfaces) and **adapters** (implementations). Alistair Cockburn coined the term in 2005.

**Structure:**

```
                 ┌──────────────────┐
  Driving        │                  │        Driven
  Adapters  ──>  │   Application    │  <──  Adapters
  (HTTP,CLI)     │      Core        │  (DB, MQ, SMTP)
                 │                  │
                 └──────────────────┘
                   Ports (interfaces)
```

- **Incoming (Driving) Ports:** Interfaces the application exposes -- what the world can ask it to do (e.g., `IOrderService.PlaceOrder()`).
- **Outgoing (Driven) Ports:** Interfaces the application needs from the outside world (e.g., `IOrderRepository`, `IEmailSender`).
- **Adapters:** Concrete implementations -- an HTTP controller is a driving adapter; an EF Core repository is a driven adapter.

**Applying to CephalonEngine:** Every `Cephalon.Data.*` package is a driven adapter. `Cephalon.AspNetCore` is a driving adapter. The pattern reinforces the idea that `Cephalon.Abstractions` must define the ports, and each adapter package implements them. This cleanly maps to NuGet package boundaries -- users install only the adapters they need.

**Sources:**
- [Alistair Cockburn: Hexagonal Architecture](https://alistair.cockburn.us/hexagonal-architecture)
- [Hexagonal Architecture with .NET (2025)](https://engineering87.github.io/2025/07/19/exagonal-architecture.html)

---

### 1.3 Onion Architecture

**Core Idea:** Introduced by Jeffrey Palermo. Visualized as concentric circles (like an onion). Inner layers have zero knowledge of outer layers. It explicitly mandates that each layer depends only on the next inner layer.

**Layers (inside-out):**
1. **Domain Model** -- Entities, Value Objects
2. **Domain Services** -- Business logic that spans entities
3. **Application Services** -- Orchestration, use cases
4. **Infrastructure & UI** -- All external concerns

**Difference from Clean Architecture:** Clean Architecture is more flexible with layering; you can skip layers. Onion Architecture requires strict layer-by-layer dependency. Both share the same fundamental principle -- dependency inversion toward the core. Onion is more DDD-friendly; Clean Architecture integrates better with CQRS/MediatR patterns.

**Applying to CephalonEngine:** The engine is closer to Clean Architecture due to its module-per-concern organization, but the principle of strict inward dependency is valuable for enforcing boundaries between `Cephalon.Engine` (core) and the various adapter assemblies.

**Sources:**
- [Code Maze: Differences Between Onion Architecture and Clean Architecture](https://code-maze.com/dotnet-differences-between-onion-architecture-and-clean-architecture/)
- [Jeffrey Palermo: The Onion Architecture](https://jeffreypalermo.com/blog/the-onion-architecture-part-1/)

---

### 1.4 Vertical Slice Architecture

**Core Idea:** Organize code by **feature** instead of by technical layer. Each feature (a "slice") is a self-contained unit: request model, handler, validation, data access, response model -- all co-located. Popularized by Jimmy Bogard.

**Structure:**
```
Features/
├── PlaceOrder/
│   ├── PlaceOrderCommand.cs
│   ├── PlaceOrderHandler.cs
│   ├── PlaceOrderValidator.cs
│   └── PlaceOrderEndpoint.cs
├── GetOrderById/
│   ├── GetOrderByIdQuery.cs
│   ├── GetOrderByIdHandler.cs
│   └── GetOrderByIdEndpoint.cs
```

**Key Principle:** Each slice can make its own architectural decisions. One slice might use EF Core with full DDD; another might use Dapper for a simple read query. There is no forced uniformity across slices.

**Applying to CephalonEngine:** Vertical slices fit naturally inside a modular monolith as the **micro-architecture** within each module. The engine provides the scaffolding -- base handler types, validation pipelines, endpoint conventions -- and each module's internal features use vertical slices. This is exactly the approach used in the `Cephalon.Behaviors` system.

**Combining with Modular Monolith:** The **macro** architecture (how you decompose into modules) is modular-monolith. The **micro** architecture (how each module organizes internally) is vertical slices. Each module can independently choose its internal structure without affecting other modules.

**Sources:**
- [Milan Jovanovic: Where Vertical Slices Fit Inside the Modular Monolith Architecture](https://www.milanjovanovic.tech/blog/where-vertical-slices-fit-inside-the-modular-monolith-architecture)
- [Anton Martyniuk: Building a Modular Monolith With Vertical Slice Architecture in .NET](https://antondevtips.com/blog/building-a-modular-monolith-with-vertical-slice-architecture-in-dotnet)

---

### 1.5 Modular Monolith

**Core Idea:** A single deployable unit that is internally divided into distinct, loosely coupled modules. Each module encapsulates specific business functionality and interacts with others through well-defined interfaces. It combines the simplicity of monolithic deployment with the clean boundaries of microservices.

**Module Boundary Rules:**
1. **Strong Encapsulation:** Modules never depend on each other's internal classes, only on public contracts.
2. **Data Ownership:** Each module owns its data exclusively. No module directly accesses another module's database tables.
3. **Explicit Communication:** Inter-module communication happens through well-defined contracts -- not random method calls.

**Inter-Module Communication Patterns:**

| Pattern | Mechanism | When to Use |
|---------|-----------|-------------|
| **Synchronous (Method calls)** | In-process call via interfaces/mediator | Query data from another module; need immediate response |
| **Asynchronous (Integration Events)** | Publish/subscribe via event bus | Notify other modules of state changes; eventual consistency acceptable |
| **Shared Contracts** | Shared NuGet package with DTOs/interfaces | Define the public API surface between modules |

**Domain Events vs. Integration Events:**
- **Domain Events** are internal to a module -- raised and handled within the same bounded context, within the same transaction.
- **Integration Events** cross module boundaries -- published through an event bus, eventually consistent.

**Applying to CephalonEngine:** This is the engine's primary architecture. Each `Cephalon.*` package is a module with clear boundaries. `Cephalon.Eventing` provides the inter-module event bus. `Cephalon.Abstractions` holds the shared contracts. The critical guideline: a module should be extractable to a microservice later without rewriting its internals.

**Sources:**
- [Milan Jovanovic: Modular Monolith Communication Patterns](https://www.milanjovanovic.tech/blog/modular-monolith-communication-patterns)
- [Kamil Grzybek: Modular Monolith Integration Styles](https://www.kamilgrzybek.com/blog/posts/modular-monolith-integration-styles)
- [ABP.IO: Modular Monolith Architecture](https://abp.io/architecture/modular-monolith)

---

### 1.6 CQRS (Command Query Responsibility Segregation)

**Core Idea:** Separate the model for reading data (queries) from the model for writing data (commands). Each side can be optimized independently -- the write side for consistency and validation, the read side for performance and scalability.

**Structure:**
```
Commands (Write Side)          Queries (Read Side)
┌─────────────────┐          ┌─────────────────┐
│ PlaceOrderCmd   │          │ GetOrderQuery   │
│      ↓          │          │      ↓          │
│ CommandHandler  │          │ QueryHandler    │
│      ↓          │          │      ↓          │
│ Domain Model    │          │ Read Model/DTO  │
│      ↓          │          │      ↓          │
│ Write DB (EF)   │          │ Read DB (Dapper)│
└─────────────────┘          └─────────────────┘
```

**Key Insight from Microsoft:** CQRS and DDD patterns are **not** top-level architectures. They are architecture patterns applied within a bounded context. Different bounded contexts can and should use different patterns. Forcing CQRS everywhere leads to over-engineering.

**Simplified CQRS (Single Database):** Both read and write sides use the same database but different models. The write side uses EF Core with rich domain entities; the read side uses Dapper with flat DTOs. This is the approach used in eShopOnContainers and is sufficient for most scenarios.

**Full CQRS (Separate Databases):** Write side persists to one store; read side queries from a denormalized read store. Event-driven synchronization keeps them in sync. Only justified for high-scale scenarios.

**Applying to CephalonEngine:** The `Cephalon.Behaviors` system already implements a CQRS pipeline with command/query separation. The engine should provide both simplified (single DB) and full (separate stores) CQRS support, letting module authors choose. The `Cephalon.Behaviors.Patterns` package can offer base types like `ICommand<TResponse>`, `IQuery<TResponse>`, and pipeline behaviors for validation, logging, and authorization.

**Sources:**
- [Microsoft: Apply simplified CQRS and DDD patterns](https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/apply-simplified-microservice-cqrs-ddd-patterns)
- [Microsoft: CQRS Pattern (Azure Architecture Center)](https://learn.microsoft.com/azure/architecture/patterns/cqrs)

---

### 1.7 Event Sourcing

**Core Idea:** Instead of storing current state, store a sequential list of all events that led to the current state. The event store is the system of record. Current state is derived by replaying events.

**Core Components:**

| Component | Purpose |
|-----------|---------|
| **Event Store** | Append-only log of domain events. Events are never updated or deleted. |
| **Aggregate** | Domain object that enforces business rules and produces events. Reconstructed from its event history. |
| **Projection** | Read model built by processing event streams. Optimized for specific query patterns. Can be rebuilt at any time. |
| **Snapshot** | Periodic capture of aggregate state to avoid replaying all events from the beginning. |

**Snapshot Strategy:** Snapshots should be created based on business-relevant boundaries (end of day, monthly close) or event count thresholds. They trade storage for faster aggregate reconstruction.

**Combining with CQRS:** Event sourcing pairs naturally with CQRS. The event store is the write model; projections are the read model. Events produced on the write side feed into projection builders that maintain denormalized read models.

**Key .NET Libraries:**
- **Marten** (PostgreSQL-backed): Full event store + projection engine. Used by CephalonEngine's EventSourcing modules.
- **EventStoreDB**: Purpose-built event store database.
- **Wolverine**: Provides event handling and projection capabilities alongside messaging.

**Applying to CephalonEngine:** The `Cephalon.EventSourcing` package and its provider-specific packages (`*.EntityFramework`, `*.MongoDB`, `*.Cassandra`, etc.) already implement this pattern. The engine should provide:
- Base aggregate types that manage event collection and replay
- Snapshot infrastructure with configurable strategies
- Projection base classes that subscribe to event streams
- Integration with the outbox pattern for reliable event publishing

**Sources:**
- [Microsoft: Event Sourcing Pattern](https://learn.microsoft.com/azure/architecture/patterns/event-sourcing)
- [Oskar Dudycz: EventSourcing.NetCore (GitHub)](https://github.com/oskardudycz/EventSourcing.NetCore)
- [Marten: Understanding Event Sourcing](https://martendb.io/events/learning.html)

---

### 1.8 Event-Driven Architecture

**Core Idea:** Components communicate through events rather than direct calls. An event bus enables publish/subscribe communication where producers and consumers are decoupled -- producers don't know who consumes their events, and consumers don't know who produced them.

**Key Patterns:**
- **Event Notification:** Simple signal that something happened. Consumer decides what to do.
- **Event-Carried State Transfer:** Event contains all data needed so consumer doesn't need to call back.
- **Event Sourcing:** Events are the system of record (see section 1.7).

**Event Bus Implementations:**
- **In-process:** MediatR notifications, Wolverine local queues
- **Out-of-process:** RabbitMQ, Azure Service Bus, Kafka, NATS

**Applying to CephalonEngine:** `Cephalon.Eventing` is the in-process event bus. `Cephalon.Eventing.Wolverine` bridges to external transports. The engine should support both fire-and-forget notifications and durable event delivery (via the outbox pattern). The key API surface:

```csharp
// Publishing
await eventBus.PublishAsync(new OrderPlacedEvent(orderId));

// Subscribing (handler discovered via DI)
public class OrderPlacedHandler : IEventHandler<OrderPlacedEvent>
{
    public Task HandleAsync(OrderPlacedEvent @event, CancellationToken ct) { ... }
}
```

**Sources:**
- [Microsoft: Event-Driven Architecture Style](https://learn.microsoft.com/azure/architecture/guide/architecture-styles/event-driven)
- [Microsoft: Implementing event-based communication between microservices](https://learn.microsoft.com/dotnet/architecture/microservices/multi-container-microservice-net-applications/integration-event-based-microservice-communications)

---

### 1.9 Microkernel / Plugin Architecture

**Core Idea:** The system is divided into a minimal **core** that provides essential functionality and **plugins** that extend it with additional features. The core defines interfaces/contracts; plugins implement them and register through a discovery mechanism.

**Structural Elements:**
- **Core System:** Implements base functionality (routing, DI, configuration, lifecycle)
- **Plugin Interface:** Contracts that plugins must implement
- **Plugin Registry:** Tracks available plugins and manages lifecycle
- **Plugin Modules:** Independent units that provide specific capabilities

**Communication Rules:** Plugins communicate only through the core, never directly with each other. This enforces modularity at the system level.

**.NET Implementation Options:**
- **MEF (Managed Extensibility Framework):** Microsoft's built-in plugin framework using `[Export]`/`[Import]` attributes.
- **Custom DI-based:** Plugins implement a known interface (e.g., `ICephalonModule`) and register services in the DI container.
- **Assembly scanning:** The host loads plugin assemblies from a directory and discovers types via reflection or source generation.

**Applying to CephalonEngine:** The engine itself is a microkernel. `Cephalon.Engine` is the core. Each `Cephalon.*` package is a plugin. The module registration pattern:

```csharp
builder.UseCephalon(engine =>
{
    engine.AddModule<AuditModule>();
    engine.AddModule<EventSourcingModule>();
    engine.AddModule<ObservabilityModule>();
});
```

This is exactly the Microkernel pattern -- each module registers its services, middleware, and event handlers through a well-defined `ICephalonModule` interface.

**Sources:**
- [Applying the Microkernel Pattern in C#](https://en.ittrip.xyz/c-sharp/microkernel-pattern-csharp)
- [Building a Plugin Architecture with MEF](https://www.elementsofcomputerscience.com/posts/building-plugin-architecture-with-mef-01/)

---

### 1.10 Pipe and Filter Architecture

**Core Idea:** A processing task is divided into a sequence of independent filters connected by pipes. Each filter receives input, processes it, and produces output. Filters are composable, reorderable, and replaceable.

**ASP.NET Core Middleware is a Pipe and Filter:** The request pipeline is the canonical .NET implementation. Each middleware component:
1. Can perform work before calling `next()`
2. Calls (or doesn't call) the next middleware in the pipeline
3. Can perform work after `next()` returns

```csharp
app.Use(async (context, next) =>
{
    // Before: filter input
    await next(context);
    // After: filter output
});
```

**Beyond HTTP Middleware -- Behavior Pipelines:** The same pattern applies to command/query processing pipelines:

```csharp
// MediatR-style pipeline behaviors
public class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        // Validate (filter)
        await _validator.ValidateAndThrowAsync(request, ct);
        // Pass to next filter
        return await next();
    }
}
```

**Applying to CephalonEngine:** The `Cephalon.Behaviors` pipeline is a textbook Pipe and Filter implementation. The engine composes behaviors (validation, authorization, logging, caching, retry) as filters in a pipeline around every command/query handler. Users can insert custom behaviors at any point. This is one of the most powerful extensibility patterns in the engine.

**Sources:**
- [Microsoft: Pipes and Filters Pattern (Azure Architecture Center)](https://learn.microsoft.com/azure/architecture/patterns/pipes-and-filters)
- [Microsoft: ASP.NET Core Middleware](https://learn.microsoft.com/aspnet/core/fundamentals/middleware/?view=aspnetcore-10.0)
- [Code Maze: Pipes and Filters Architectural Pattern in C#](https://code-maze.com/csharp-pipes-and-filters-architectural-pattern/)

---

## 2. SOLID Principles in Framework Design

### 2.1 Single Responsibility Principle (SRP)

**Principle:** A class should have only one reason to change. It should have exactly one responsibility.

**Framework Application:** In CephalonEngine, SRP manifests at multiple levels:
- **Package level:** `Cephalon.Observability.Serilog` handles Serilog integration and nothing else. `Cephalon.Data.Redis` handles Redis data access only.
- **Class level:** An `OrderCommandHandler` handles order commands. An `OrderProjection` maintains the read model. They don't overlap.
- **Method level:** A pipeline behavior does one thing -- validate, authorize, or log -- never all three.

**Practical Example:**
```csharp
// BAD: One class doing everything
public class OrderService
{
    public void PlaceOrder(Order order) { /* validate + save + notify + log */ }
}

// GOOD: Single responsibility per class
public class PlaceOrderHandler : ICommandHandler<PlaceOrderCommand, OrderId> { ... }
public class PlaceOrderValidator : IValidator<PlaceOrderCommand> { ... }
public class OrderPlacedNotifier : IEventHandler<OrderPlacedEvent> { ... }
```

**Sources:**
- [Microsoft: Architectural Principles -- Single Responsibility](https://learn.microsoft.com/dotnet/architecture/modern-web-apps-azure/architectural-principles#common-design-principles)

---

### 2.2 Open/Closed Principle (OCP)

**Principle:** Software entities should be open for extension but closed for modification. You should be able to add new behavior without changing existing code.

**Framework Application:** This is the most critical SOLID principle for framework design. CephalonEngine must be extensible without requiring users to fork or modify the engine source.

**Extension Points in CephalonEngine:**
- **Pipeline Behaviors:** Users add new behaviors to the command/query pipeline without modifying existing ones.
- **Module Registration:** New modules plug in via `engine.AddModule<T>()`.
- **Provider Packages:** New database providers implement `IEventStore` or `IRepository<T>` without touching the core.
- **Middleware:** Custom middleware inserts into the HTTP pipeline.
- **Event Handlers:** New handlers subscribe to events without modifying publishers.

**Practical Example:**
```csharp
// The engine defines the extension point (interface)
public interface IPipelineBehavior<TRequest, TResponse> { ... }

// Users extend without modifying engine code
public class CachingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
{
    // Custom caching logic
}
```

---

### 2.3 Liskov Substitution Principle (LSP)

**Principle:** Objects of a supertype should be replaceable with objects of a subtype without altering correctness. Subtypes must honor the contract of their base type.

**Framework Application:** When framework users implement engine interfaces, their implementations must behave correctly in all contexts where the interface is expected.

**Contract Design Guidelines:**
- Document preconditions, postconditions, and invariants on all public interfaces.
- Use `abstract` base classes with template methods when you need to enforce a protocol.
- Prefer composition over inheritance -- interfaces with default implementations (C# 8+) over deep class hierarchies.

**Practical Example:**
```csharp
// Contract: IRepository<T>.GetByIdAsync must return null for missing entities, not throw
public interface IRepository<T> where T : class, IAggregateRoot
{
    /// <returns>The entity, or null if not found.</returns>
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
}

// Any implementation (EF, Mongo, Redis) must honor this contract
// Throwing NotFoundException would violate LSP
```

---

### 2.4 Interface Segregation Principle (ISP)

**Principle:** No client should be forced to depend on methods it does not use. Prefer many small, specific interfaces over few large, general ones.

**Framework Application:** This directly impacts CephalonEngine's API surface. Large "god" interfaces force implementers to provide methods they don't need, and force consumers to depend on capabilities they don't use.

**Practical Example:**
```csharp
// BAD: One large interface
public interface IRepository<T>
{
    Task<T?> GetByIdAsync(Guid id);
    Task<List<T>> GetAllAsync();
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
    Task<int> CountAsync();
    Task<bool> ExistsAsync(Guid id);
}

// GOOD: Segregated interfaces
public interface IReadRepository<T>
{
    Task<T?> GetByIdAsync(Guid id);
    Task<List<T>> GetAllAsync();
}

public interface IWriteRepository<T>
{
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
}
```

This allows query handlers to depend only on `IReadRepository<T>` and command handlers to depend on `IWriteRepository<T>`.

---

### 2.5 Dependency Inversion Principle (DIP)

**Principle:** High-level modules should not depend on low-level modules. Both should depend on abstractions. Abstractions should not depend on details.

**Framework Application:** This is the foundational principle of Clean Architecture and the reason CephalonEngine's package structure works. The engine core (`Cephalon.Engine`, `Cephalon.Abstractions`) defines abstractions. Infrastructure packages (`Cephalon.Data.*`, `Cephalon.Observability.*`) depend on those abstractions and provide implementations.

**DI Container as the Glue:** ASP.NET Core's built-in DI container wires everything together at the composition root:

```csharp
// In the composition root (Program.cs)
builder.Services.AddScoped<IOrderRepository, EfOrderRepository>();
builder.Services.AddSingleton<IEventStore, MartenEventStore>();
```

**Sources:**
- [Microsoft: Dependency Inversion](https://learn.microsoft.com/dotnet/architecture/modern-web-apps-azure/architectural-principles#common-design-principles)
- [SOLID Principles in .NET Core API](https://dev.to/manonaga2188/solid-principles-in-net-core-api-a-complete-guide-with-practical-examples-53ka)

---

## 3. DDD for Framework Authors

### 3.1 Aggregates, Entities, Value Objects

**Aggregates:** A cluster of domain objects treated as a single unit for data changes. The aggregate root is the single entry point. All modifications go through the root, which enforces invariants.

```csharp
public class Order : Entity, IAggregateRoot
{
    private DateTime _orderDate;
    public Address Address { get; private set; }  // Value Object
    private int? _buyerId;  // FK to another aggregate -- NO navigation property
    public OrderStatus OrderStatus { get; private set; }
    private readonly List<OrderItem> _orderItems;
    public IReadOnlyCollection<OrderItem> OrderItems => _orderItems;

    public void AddItem(int productId, decimal price, int units)
    {
        // Business rule enforcement happens here
        var existingItem = _orderItems.SingleOrDefault(i => i.ProductId == productId);
        if (existingItem != null)
            existingItem.AddUnits(units);
        else
            _orderItems.Add(new OrderItem(productId, price, units));
    }
}
```

**Entities:** Objects with a unique identity that persists over time. Two entities with the same ID are the same entity, even if their attributes differ.

**Value Objects:** Immutable objects defined by their attributes, not identity. Two value objects with the same attributes are equal.

```csharp
public record Address(string Street, string City, string State, string ZipCode);
// Records are perfect for value objects in modern C#
```

**Aggregate Design Rules:**
1. Reference other aggregates by ID only (no navigation properties across aggregate boundaries).
2. One transaction = one aggregate. Cross-aggregate consistency uses eventual consistency via domain events.
3. Keep aggregates small. Prefer more, smaller aggregates over fewer, larger ones.

**Applying to CephalonEngine:** The engine should provide base types in `Cephalon.Abstractions`:
- `Entity` base class with identity management
- `AggregateRoot` base class with domain event collection
- `ValueObject` base record with structural equality

**Sources:**
- [Microsoft: Design a DDD-oriented microservice](https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/ddd-oriented-microservice)
- [Microsoft: Design a microservice domain model](https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/microservice-domain-model)

---

### 3.2 Domain Events and Integration Events

**Domain Events:** Represent something that happened within a single bounded context. Handled within the same transaction (or immediately after commit). Used to trigger side effects across aggregates within the same module.

**The Deferred Dispatch Pattern (recommended by Microsoft eShop):**
1. Aggregate root collects events in a list: `this.AddDomainEvent(new OrderStartedEvent(...))`
2. Events are dispatched right before or after `SaveChanges()` by the DbContext
3. MediatR (or Wolverine) dispatches to handlers synchronously within the same transaction

**Integration Events:** Cross module boundaries. Published through an event bus. Eventually consistent. Different schema from domain events -- they represent the public contract between modules.

```csharp
// Domain Event (internal to module)
public record OrderStartedDomainEvent(Order Order, int CardTypeId, string CardNumber) : IDomainEvent;

// Integration Event (public contract between modules)
public record OrderPlacedIntegrationEvent(Guid OrderId, DateTime PlacedAt, decimal TotalAmount) : IIntegrationEvent;
```

**The Flow:**
1. Command handler modifies aggregate, aggregate raises domain event
2. Domain event handler performs in-module side effects (e.g., create Buyer aggregate)
3. Domain event handler publishes integration event to the event bus
4. Other modules receive integration event and act on it

**Sources:**
- [Microsoft: Domain Events Design and Implementation](https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-events-design-implementation)

---

### 3.3 Bounded Contexts as Modules

**Core Idea:** Each bounded context maps to a module (or microservice). Within a bounded context, terms have specific meanings. The same real-world concept can have different representations in different bounded contexts.

**Example:** "Customer" in the Sales context has order history and preferences. "Customer" in the Shipping context has addresses and delivery preferences. They share an ID but are different models.

**Applying to CephalonEngine:** Each engine module is a bounded context:
- `Cephalon.Identity` -- the Identity context
- `Cephalon.Audit` -- the Audit context
- `Cephalon.EventSourcing` -- the Event Sourcing context

Each context should have exclusive access to its own persistence store (or schema within a shared database).

---

### 3.4 Anti-Corruption Layer (ACL)

**Core Idea:** A set of defensive patterns placed between your domain model and external systems (or other bounded contexts) to prevent foreign concepts from corrupting your domain model.

**Implementation:** An ACL translates between the external model and the internal domain model:

```csharp
// ACL translating from external payment gateway to internal domain
public class PaymentGatewayAdapter : IPaymentService
{
    private readonly ExternalPaymentClient _client;

    public async Task<PaymentResult> ProcessPayment(Money amount, PaymentMethod method)
    {
        // Translate from domain model to external API model
        var externalRequest = new ExternalPaymentRequest
        {
            AmountInCents = (int)(amount.Value * 100),
            Currency = amount.Currency.Code,
            // ... mapping
        };

        var externalResponse = await _client.ChargeAsync(externalRequest);

        // Translate back to domain model
        return new PaymentResult(
            externalResponse.WasSuccessful,
            Money.FromCents(externalResponse.ChargedAmountCents, amount.Currency));
    }
}
```

**Applying to CephalonEngine:** Every adapter package (`Cephalon.Data.*`) is an ACL. It translates between the engine's domain abstractions and the external system's API. This is why adapter packages reference `Cephalon.Abstractions` (the domain) and the external SDK, but the domain never references the adapters.

---

### 3.5 Shared Kernel

**Core Idea:** A small, carefully managed set of code shared across multiple bounded contexts. It contains types that are truly universal -- common value objects, base classes, shared interfaces.

**In CephalonEngine:** `Cephalon.Abstractions` is the shared kernel. It contains:
- Base entity/aggregate types
- Common interfaces (`ICommand`, `IQuery`, `IEventHandler`)
- Shared value objects (`TenantId`, `CorrelationId`)

**Caution:** The shared kernel creates coupling. Every change to it affects all modules. Keep it minimal and stable. Only truly shared concepts belong here.

**Sources:**
- [Shared Kernel in DDD (DevIQ)](https://deviq.com/domain-driven-design/shared-kernel/)
- [Anti-Corruption Layer (DevIQ)](https://deviq.com/domain-driven-design/anti-corruption-layer/)

---

## 4. Framework Design Principles

### 4.1 Microsoft's Framework Design Guidelines

The canonical reference for .NET library/framework design. Key principles:

**Naming Conventions:**
- Use PascalCase for all public members
- Prefix interfaces with `I`
- Use consistent suffixes (`-Exception`, `-Attribute`, `-EventArgs`)
- Namespace should reflect the logical grouping: `Cephalon.EventSourcing.Projections`

**Type Design:**
- Prefer classes over interfaces for versioned contracts (you can add members to a class without breaking consumers)
- Use sealed classes by default; unseal only when extension is intended
- Avoid `static` classes for functionality that might need testing/mocking

**Member Design:**
- Use method overloads for convenience; make the simplest overload the easiest to discover
- Provide sensible defaults for optional parameters
- Throw `ArgumentException`/`ArgumentNullException` for invalid parameters

**Sources:**
- [Microsoft: Framework Design Guidelines](https://learn.microsoft.com/dotnet/standard/design-guidelines/)
- [dotnet/runtime: Framework Design Guidelines Digest](https://github.com/dotnet/runtime/blob/main/docs/coding-guidelines/framework-design-guidelines-digest.md)

---

### 4.2 API Design: Pit of Success

**Core Idea:** Design APIs so that the most natural, obvious usage is the correct usage. Users should "fall into" correct behavior without effort. Coined by Rico Mariani and Brad Abrams at Microsoft.

**Principles:**
1. **Make wrong usage hard/impossible:** Use the type system to prevent errors. A `NonEmptyString` type eliminates null/empty checks. A `Money` type prevents mixing currencies.
2. **Provide good defaults:** `builder.UseCephalon()` should work out of the box with sensible defaults. Advanced configuration is opt-in.
3. **Progressive disclosure:** Simple scenarios should require minimal code. Complex scenarios should be possible but not forced.
4. **Fail fast and clearly:** Throw meaningful exceptions at the earliest point of misuse, not deep in a call stack.

**CephalonEngine Application:**
```csharp
// Pit of Success: Works with zero configuration
builder.UseCephalon();

// Progressive disclosure: Opt into complexity only when needed
builder.UseCephalon(engine =>
{
    engine.AddEventSourcing(es => es.UsePostgres(connectionString));
    engine.AddObservability(obs => obs.UseOpenTelemetry());
});
```

**Sources:**
- [Microsoft: The Pit of Success](https://learn.microsoft.com/archive/blogs/brada/the-pit-of-success)

---

### 4.3 Convention over Configuration

**Core Idea:** Provide sensible defaults based on conventions. Users only configure what deviates from the convention. ASP.NET Core MVC exemplifies this -- controllers are discovered by naming convention, routes are inferred from controller/action names.

**CephalonEngine Conventions:**
- Command handlers are discovered automatically by scanning assemblies for `ICommandHandler<T>` implementations
- Event handlers follow the naming convention `{EventName}Handler`
- Module configuration classes implement `ICephalonModule` and are auto-discovered
- Database migrations follow `{Timestamp}_{Description}` naming

**When to Break Convention:** Always allow explicit configuration to override conventions. The convention is the default path; configuration is the escape hatch.

---

### 4.4 Extensibility Patterns

**Hooks:** Allow users to inject code at specific lifecycle points.
```csharp
engine.OnModuleLoading += (sender, args) => { /* custom logic */ };
```

**Interceptors:** Wrap existing behavior with before/after logic.
```csharp
// Wolverine's middleware pattern
public static async Task<TResponse> Before<TResponse>(
    ILogger logger, /* parameters injected by convention */)
{
    logger.LogInformation("Before handler");
}
```

**Middleware:** The pipe-and-filter approach for HTTP request processing.

**Pipeline Behaviors:** The pipe-and-filter approach for command/query processing.

**Strategy Pattern:** Allow users to provide custom implementations of key algorithms.
```csharp
engine.AddEventSourcing(es =>
{
    es.UseSnapshotStrategy<TimeBasedSnapshotStrategy>(); // User-provided strategy
});
```

---

### 4.5 Backward Compatibility Strategies

**Rules for Library Authors (from Microsoft):**
- Adding a new interface implementation to a type: **Requires judgment**
- Moving a type from one assembly to another: **Allowed** (use `TypeForwardedToAttribute`)
- Adding non-abstract members to a class: **Not a binary breaking change**
- Adding members to an interface: **Binary breaking change** (use default interface methods in C# 8+ to mitigate)
- Removing a public type: **Breaking change**

**Strategies for CephalonEngine:**
1. **Semantic Versioning:** Major version bumps for breaking changes, minor for new features, patch for bug fixes.
2. **Obsolete before Remove:** Mark APIs `[Obsolete]` in version N, remove in version N+2.
3. **TypeForwardedTo:** When reorganizing packages, forward types to the new assembly.
4. **Default Interface Methods:** Add new members to interfaces without breaking existing implementations.
5. **API Analyzers:** Use Roslyn analyzers to detect breaking changes in PRs.

**Sources:**
- [Microsoft: Change Rules for Compatibility](https://learn.microsoft.com/dotnet/core/compatibility/library-change-rules)
- [Microsoft: How Code Changes Can Affect Compatibility](https://learn.microsoft.com/dotnet/core/compatibility/categories)

---

### 4.6 Assembly/Package Design

**When to Split into Separate Packages:**
- The functionality has distinct infrastructure dependencies (e.g., `Cephalon.Data.Redis` vs. `Cephalon.Data.MongoDB`)
- Users should be able to install one without the other
- The package has a different release cadence

**When to Keep in One Package:**
- The types are tightly coupled and always used together
- Splitting would create circular dependencies
- The combined package size is small

**CephalonEngine's Package Strategy:**
- `Cephalon.Abstractions` -- shared kernel, minimal dependencies
- `Cephalon.Engine` -- core runtime, depends only on Abstractions
- `Cephalon.{Feature}` -- feature packages depend on Abstractions
- `Cephalon.{Feature}.{Provider}` -- provider-specific adapters depend on Feature + external SDK

**Versioning:** All packages in the engine should version together (same version number). This avoids diamond dependency problems and makes it clear which versions are compatible.

**Sources:**
- [NuGet Best Practices and Versioning](https://medium.com/@sweetondonie/nuget-best-practices-and-versioning-for-net-developers-cedc8ede5f16)

---

### 4.7 Source Generators for Framework Users

**Core Idea:** Roslyn source generators analyze user code at compile time and produce additional C# source files. For framework authors, this enables zero-reflection registration, AOT-friendly code, and compile-time validation.

**CephalonEngine Use Cases:**
1. **Handler Discovery:** Generate `IServiceCollection` registration code for all command/query handlers in the user's assembly.
2. **Endpoint Mapping:** Generate minimal API endpoint registrations from handler attributes.
3. **Serialization:** Generate `System.Text.Json` serialization contexts for all event types.
4. **Validation:** Generate FluentValidation validators from data annotations at compile time.

**Implementation Pattern:**
```csharp
[Generator]
public class HandlerRegistrationGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var handlers = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "Cephalon.Abstractions.CommandHandlerAttribute",
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, _) => /* extract handler info */);

        context.RegisterSourceOutput(handlers, (spc, handler) =>
        {
            // Generate: services.AddScoped<ICommandHandler<Cmd, Resp>, HandlerType>();
        });
    }
}
```

**C# Interceptors (C# 12+):** Allow source generators to intercept specific method calls and substitute generated code. This enables compile-time optimization of hot paths without changing user code. `Cephalon.Behaviors.SourceGen` already uses this pattern.

**Sources:**
- [Andrew Lock: Creating a Source Generator (series)](https://andrewlock.net/creating-a-source-generator-part-11-implementing-an-interceptor-with-a-source-generator/)
- [Microsoft: Compile-time Configuration Source Generation](https://learn.microsoft.com/dotnet/core/extensions/configuration-generator)

---

## 5. Messaging and Communication Patterns

### 5.1 In-Process Messaging (MediatR vs. Wolverine)

**MediatR:**
- Lightweight in-process mediator
- `IRequest<TResponse>` / `IRequestHandler<TRequest, TResponse>` for request/response
- `INotification` / `INotificationHandler<TNotification>` for pub/sub
- Pipeline behaviors for cross-cutting concerns
- **Limitation:** No durable messaging, no outbox, no retry. Notifications are fire-and-forget.

**Wolverine:**
- Full-featured messaging framework (in-process AND distributed)
- Built-in transactional outbox
- Retry policies, error handling, dead letter queues
- Saga/workflow support
- Convention-based handler discovery (no marker interfaces required)
- Marten integration for event sourcing

**Key Difference:** MediatR's `INotificationHandler` is strictly fire-and-forget -- unsuitable for scenarios requiring delivery guarantees. Wolverine supports both fire-and-forget ("Buffered") and durable transactional inbox/outbox with local queues, ensuring work is not lost during errors.

**CephalonEngine Choice:** The engine does not require Wolverine as its messaging foundation. `Cephalon.Eventing` owns the runtime-neutral channel, subscription, publication, dispatch, in-process execution, readiness, and operator-state contracts so the engine remains complete without a provider adapter. `Cephalon.Eventing.Wolverine` is an optional provider-managed companion for teams that deliberately choose Wolverine durability or dispatch/subscription execution; future companions can plug into the same seams without changing consumer app code.

**Sources:**
- [Jeremy Miller: Wolverine for MediatR Users](https://jeremydmiller.com/2025/01/28/wolverine-for-mediatr-users/)
- [TheCodeMan: MediatR Alternative - Wolverine](https://thecodeman.net/posts/mediatr-alternative-wolverine)

---

### 5.2 Message Bus Patterns

**Publish/Subscribe:** One producer publishes a message; multiple consumers receive it. Each consumer gets its own copy. Used for integration events between modules.

**Request/Reply:** One requester sends a message and waits for a response from a specific handler. Used for cross-module queries.

**Point-to-Point:** One message, one consumer. Used for commands that should be handled exactly once.

**Applying to CephalonEngine:**
```csharp
// Publish/Subscribe (integration events)
await bus.PublishAsync(new OrderShippedEvent(orderId));

// Request/Reply (cross-module query)
var inventory = await bus.InvokeAsync<InventoryStatus>(
    new CheckInventoryQuery(productId));

// Point-to-Point (command)
await bus.InvokeAsync(new ProcessPaymentCommand(orderId, amount));
```

---

### 5.3 Inbox/Outbox Patterns

**Transactional Outbox:** When a business operation produces both a database change and an event, the outbox pattern ensures atomicity. Events are written to an "outbox" table in the same database transaction as the business data. A background process reads the outbox and publishes events to the message bus.

```
┌─────────────────────────────┐
│    Single Transaction       │
│  ┌────────────┐  ┌───────┐ │
│  │ Business   │  │Outbox │ │
│  │ Tables     │  │Table  │ │
│  └────────────┘  └───────┘ │
└─────────────────────────────┘
         ↓ (background relay)
    ┌────────────┐
    │ Message Bus│
    └────────────┘
```

**Transactional Inbox:** The receiving side stores incoming messages in an "inbox" table before processing. This ensures idempotent handling -- if the same message arrives twice, it's detected via the inbox and skipped.

**Wolverine's Implementation:** Wolverine provides a built-in outbox that works with both EF Core and Marten. The outbox is integrated into the unit of work:

```csharp
// Wolverine automatically persists outgoing messages in the same transaction
public static async Task Handle(
    PlaceOrderCommand command,
    OrderDbContext db,
    IMessageContext context)
{
    var order = new Order(command.CustomerId, command.Items);
    db.Orders.Add(order);

    // This message is persisted to the outbox, not sent immediately
    await context.PublishAsync(new OrderPlacedEvent(order.Id));

    // Both the order AND the outbox message are saved atomically
    await db.SaveChangesAsync();
}
```

**Sources:**
- [Wolverine: Durable Messaging - Transactional Inbox/Outbox](https://wolverinefx.net/guide/durability/)
- [Jeremy Miller: Build Resilient Systems with Wolverine's Transactional Outbox](https://jeremydmiller.com/2024/12/08/build-resilient-systems-with-wolverines-transactional-outbox/)

---

### 5.4 Competing Consumers

**Core Idea:** Multiple consumer instances read from the same queue and compete to process messages. Each message is delivered to exactly one consumer. This enables horizontal scaling of message processing.

**Key Benefits:**
- Load distribution across multiple instances
- Fault tolerance -- if one consumer dies, others continue processing
- Dynamic scaling -- add/remove consumers based on queue depth

**Implementation Considerations:**
- Messages must be idempotent (processing the same message twice should be safe)
- Message ordering is not guaranteed across competing consumers
- Use the inbox pattern to deduplicate

**Sources:**
- [Microsoft: Competing Consumers Pattern](https://learn.microsoft.com/azure/architecture/patterns/competing-consumers)

---

### 5.5 Dead Letter Queues (DLQ)

**Core Idea:** A specialized queue that stores messages that cannot be processed successfully. After exceeding a configured retry count (`MaxDeliveryCount`), messages are automatically moved to the DLQ instead of being discarded.

**Use Cases:**
- Messages with invalid schemas
- Handler exceptions that persist after all retries
- Messages that exceed processing time limits

**Operational Importance:** A growing DLQ indicates a systemic issue. The engine should surface DLQ depth as a health check metric and provide tooling to inspect, replay, or purge dead-lettered messages.

**Applying to CephalonEngine:** The observability module should expose DLQ metrics. The eventing module should provide:
- Configurable retry policies with exponential backoff
- Automatic dead-lettering after max retries
- Admin endpoints to inspect and replay dead letters

**Sources:**
- [Microsoft: Service Bus Dead-Letter Queues](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-dead-letter-queues)

---

## 6. Resilience and Observability

### 6.1 Health Checks

**Core Idea:** Expose endpoints that report the health of the application and its dependencies. Two standard endpoints:
- `/health` -- Full health check (includes dependency checks)
- `/alive` -- Liveness probe (is the process running?)

**ASP.NET Core Implementation:**
```csharp
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgres")
    .AddRedis(redisConnection, name: "redis")
    .AddCheck<EventStoreHealthCheck>("event-store");

app.MapHealthChecks("/health");
app.MapHealthChecks("/alive", new HealthCheckOptions
{
    Predicate = _ => false  // No dependency checks for liveness
});
```

**Applying to CephalonEngine:** Each `Cephalon.Observability.*Dependencies` package registers health checks for its specific infrastructure. The `Cephalon.Observability` module aggregates them. This maps perfectly to Kubernetes readiness/liveness probes.

**Sources:**
- [Microsoft: .NET observability with OpenTelemetry](https://learn.microsoft.com/dotnet/core/diagnostics/observability-with-otel)

---

### 6.2 Structured Logging (Serilog + OpenTelemetry)

**Core Idea:** Logs are structured data (key-value properties), not flat strings. This enables searching, filtering, and aggregation across millions of log entries.

**The Three Pillars of .NET Logging:**
1. `Microsoft.Extensions.Logging.ILogger<T>` -- the abstraction
2. **Serilog** -- the most popular structured logging implementation
3. **OpenTelemetry Logging** -- vendor-neutral export

**Serilog Best Practices:**
- Configure via `appsettings.json`, not code (allows per-environment tuning without redeployment)
- Use enrichers for contextual data: `ThreadId`, `MachineName`, `CorrelationId`
- Use `Serilog.Sinks.Async` in production to avoid blocking the request thread
- Avoid console/file sinks in production; use Seq, Elasticsearch, or OTLP
- Never log sensitive data; use destructuring policies to redact

**CephalonEngine Integration (`Cephalon.Observability.Serilog`):**
```csharp
builder.UseCephalon(engine =>
{
    engine.AddObservability(obs =>
    {
        obs.UseSerilog(log => log
            .Enrich.WithCorrelationId()
            .Enrich.WithTenantId()
            .WriteTo.OpenTelemetry());
    });
});
```

**Sources:**
- [Milan Jovanovic: 5 Serilog Best Practices](https://www.milanjovanovic.tech/blog/5-serilog-best-practices-for-better-structured-logging)
- [Serilog.net](https://serilog.net/)

---

### 6.3 Distributed Tracing

**Core Idea:** Track a request as it flows through multiple services/modules. Each operation creates a "span" within a "trace." Parent-child relationships between spans reveal the full call graph.

**.NET Implementation:**
- `System.Diagnostics.Activity` -- the built-in tracing primitive
- `System.Diagnostics.ActivitySource` -- creates new activities/spans
- OpenTelemetry collects and exports them

```csharp
private static readonly ActivitySource Source = new("Cephalon.Engine");

public async Task HandleAsync(PlaceOrderCommand command, CancellationToken ct)
{
    using var activity = Source.StartActivity("PlaceOrder");
    activity?.SetTag("order.customer_id", command.CustomerId);
    // ... processing
}
```

**Applying to CephalonEngine:** The `Cephalon.Observability.OpenTelemetry` package configures tracing for all engine components. Each pipeline behavior, event handler, and data access operation should create spans. The engine's ActivitySource name should be `Cephalon.Engine` with child sources per module.

---

### 6.4 Metrics Collection

**Core Idea:** Counters, gauges, and histograms that track system behavior over time. Unlike logs (discrete events), metrics are aggregated numerical measurements.

**.NET Implementation:**
- `System.Diagnostics.Metrics.Meter` -- creates metrics instruments
- `Counter<T>`, `Histogram<T>`, `UpDownCounter<T>` -- instrument types
- OpenTelemetry collects and exports to Prometheus, Grafana, Azure Monitor

```csharp
private static readonly Meter Meter = new("Cephalon.Engine", "1.0.0");
private static readonly Counter<long> CommandsProcessed =
    Meter.CreateCounter<long>("cephalon.commands.processed");
private static readonly Histogram<double> CommandDuration =
    Meter.CreateHistogram<double>("cephalon.commands.duration", "ms");
```

**Key Metrics for CephalonEngine:**
- Commands/queries processed per second
- Handler execution duration (histogram)
- Event store append latency
- Projection lag
- Active tenants (gauge)
- DLQ depth (gauge)

**Sources:**
- [Microsoft: .NET observability with OpenTelemetry](https://learn.microsoft.com/dotnet/core/diagnostics/observability-with-otel)

---

### 6.5 OpenTelemetry Configuration

The standard approach for .NET applications:

```csharp
var otel = builder.Services.AddOpenTelemetry();

// Logging
builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
});

// Metrics
otel.WithMetrics(metrics =>
{
    metrics.AddAspNetCoreInstrumentation();
    metrics.AddMeter("Cephalon.Engine");
    metrics.AddMeter("Microsoft.AspNetCore.Hosting");
});

// Tracing
otel.WithTracing(tracing =>
{
    tracing.AddAspNetCoreInstrumentation();
    tracing.AddHttpClientInstrumentation();
    tracing.AddSource("Cephalon.Engine");
});

// Export
otel.UseOtlpExporter();
```

**.NET's Unique OTel Advantage:** Unlike other platforms, .NET provides logging, metrics, and tracing APIs in the framework itself (`ILogger`, `Meter`, `ActivitySource`). OpenTelemetry in .NET doesn't replace these APIs -- it collects from them and exports. This means CephalonEngine libraries instrument with standard .NET APIs, and OTel integration is purely a deployment concern.

**Sources:**
- [Microsoft: .NET Implementation of OpenTelemetry](https://learn.microsoft.com/dotnet/core/diagnostics/observability-with-otel#net-implementation-of-opentelemetry)

---

### 6.6 Feature Flags

**Core Idea:** Conditionally enable or disable features at runtime without redeployment. Enables beta access, gradual rollouts, A/B testing, and kill switches.

**Microsoft.FeatureManagement Library:**
```csharp
// Registration
builder.Services.AddFeatureManagement();

// Usage via DI
public class ProductController(IFeatureManager featureManager)
{
    public async Task<IActionResult> Index()
    {
        if (await featureManager.IsEnabledAsync("NewProductPage"))
            return View("NewIndex");
        return View("Index");
    }
}

// ASP.NET Core integration
[FeatureGate("FeatureX")]
public IActionResult SecretFeature() => View();

// Razor view integration
<feature name="FeatureX">
    <p>This content appears only when 'FeatureX' is enabled.</p>
</feature>
```

**Configuration:**
```json
{
  "feature_management": {
    "feature_flags": [
      { "id": "NewProductPage", "enabled": true },
      {
        "id": "BetaFeature",
        "enabled": true,
        "conditions": {
          "client_filters": [
            { "name": "Microsoft.Targeting", "parameters": { "Audience": { "DefaultRolloutPercentage": 25 } } }
          ]
        }
      }
    ]
  }
}
```

**Applying to CephalonEngine:** The engine should integrate with `Microsoft.FeatureManagement` and provide:
- Feature-gated module activation
- Feature flags as pipeline behavior conditions
- Multi-tenant feature flag support (different tenants see different features)
- Telemetry integration (which features are evaluated, for whom)

**Sources:**
- [Microsoft: .NET Feature Flag Management](https://learn.microsoft.com/azure/azure-app-configuration/feature-management-dotnet-reference)
- [GitHub: microsoft/FeatureManagement-Dotnet](https://github.com/microsoft/FeatureManagement-Dotnet)

---

## Summary: How It All Fits Together in CephalonEngine

```
┌─────────────────────────────────────────────────────────┐
│                    CephalonEngine                        │
│                                                         │
│  ┌─────────────────────────────────────────────────┐   │
│  │          Cephalon.Abstractions                   │   │
│  │  (Shared Kernel: base types, interfaces, DTOs)   │   │
│  └─────────────────────────────────────────────────┘   │
│                         ▲                               │
│  ┌──────────────────────┼──────────────────────────┐   │
│  │          Cephalon.Engine (Microkernel Core)      │   │
│  │  Module loader, DI composition, pipeline engine  │   │
│  └──────────────────────┼──────────────────────────┘   │
│           ▲              ▲              ▲               │
│  ┌────────┴───┐  ┌──────┴──────┐  ┌───┴────────┐     │
│  │ Cephalon.  │  │ Cephalon.   │  │ Cephalon.  │     │
│  │ Behaviors  │  │ EventSourcing│  │Observability│     │
│  │ (Pipe &    │  │ (ES + CQRS) │  │(OTel,Health│     │
│  │  Filter)   │  │             │  │ Serilog)   │     │
│  └────────────┘  └─────────────┘  └────────────┘     │
│       ▲                ▲                ▲              │
│  ┌────┴────┐    ┌──────┴──────┐  ┌─────┴──────┐     │
│  │Behaviors│    │ ES.Postgres │  │ Obs.Azure  │     │
│  │.SourceGen│   │ ES.MongoDB  │  │ Obs.AWS    │     │
│  │(Roslyn)  │   │ ES.Redis    │  │ Obs.GCP    │     │
│  └──────────┘   └─────────────┘  └────────────┘     │
│                                                       │
│  ┌─────────────────────────────────────────────────┐ │
│  │          Cephalon.Eventing.Wolverine            │ │
│  │  (Event Bus, Outbox, DLQ, Competing Consumers)  │ │
│  └─────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────┘
```

**Architecture Pattern Mix:**
- **Macro:** Modular Monolith (each `Cephalon.*` package is a module)
- **Module Internals:** Vertical Slice Architecture (features organized by use case)
- **Dependency Management:** Clean Architecture (dependency rule, DIP)
- **Extension Model:** Microkernel/Plugin (modules register into the core)
- **Request Processing:** Pipe and Filter (behaviors pipeline, middleware)
- **Data Patterns:** CQRS + Event Sourcing (optional, per module)
- **Communication:** Event-Driven Architecture (domain events + integration events)
- **Resilience:** Outbox + Inbox + DLQ + Competing Consumers
- **Observability:** OpenTelemetry (logs + metrics + traces + health checks)

Every pattern reinforces the others. The modular monolith gives you deployment simplicity. Clean architecture gives you testability. The plugin model gives you extensibility. CQRS gives you scalability. The event-driven approach gives you loose coupling. And OpenTelemetry gives you visibility into all of it.
