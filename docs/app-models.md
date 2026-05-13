# Cephalon App Models

Editable diagram: `docs/cephalon-app-models.drawio`

## Why this exists

Apps that attach to the Cephalon engine should be able to choose a project shape intentionally. But the choices need to be modeled correctly.

The original candidate list mixes different kinds of decisions:

- `Modular Architecture`: application composition style
- `Microservice Architecture`: deployment topology
- `Vertical Slice Architecture`: feature organization style
- `Shared Foundation Pattern`: shared platform/base layer
- `Strategy Pattern`: internal behavior extension pattern
- `REST API`, `JsonRpc`, `Grpc`, `GraphQL`, `SSE`, `WebSocket`: transport surface choices

Cephalon should not treat all of these as equal top-level architecture choices. Instead, it should define an `App Model` made from multiple dimensions.

## App model dimensions

### 1. Composition model

How the application is assembled.

Supported direction:

- `Modular`
- `Plugin-ready Modular` later

### 2. Deployment topology

How the application is deployed.

Supported direction:

- `SingleHost`
- `Microservice`
- `MicroserviceSuite`

### 3. Feature organization

How code is laid out inside the app or module.

Supported direction:

- `VerticalSlice`
- `ModuleFirst`

### 4. Shared foundation

The common runtime, contracts, policies, diagnostics, and conventions all apps inherit from the engine.

Supported direction:

- always on for Cephalon apps

### 5. Transport surface

How the app exposes commands, queries, events, or streams to the outside world.

Supported direction:

- `RestApi`
- `JsonRpc`
- `Grpc`
- `GraphQL`
- `ServerSentEvents`
- `WebSocket`

Multiple transports may be enabled together when the host supports them.
Transport selection and host adapter registration must stay aligned.
Some transports, such as `Grpc`, may expose both unary and streaming interaction styles inside the same transport choice.
On ASP.NET Core hosts, `RestApi` should expose OpenAPI and Scalar as the REST documentation surface.
Cephalon keeps REST document customization in OpenAPI transformers inside the ASP.NET Core host package.

### 6. Behavioral extension

How variable behavior is swapped or selected inside a feature/module.

Supported direction:

- optional strategy slots
- policy-driven selection later

### 7. Technology profile

How future-facing workload capabilities are layered onto the app model without turning every new technology into a new blueprint.

Supported direction:

- `AgenticWorkloads`
- `CellBasedArchitecture`
- `EventDrivenIntegration`
- `KnowledgeRetrieval`
- `RealtimeExperience`
- `EdgeNativeDelivery`
- `IdentityAccess`
- `MultiTenancy`
- `HybridCloudRuntime`
- `ServiceMeshIntegration`
- `ServerlessHosting`

Technology profiles should carry workload guidance, validation rules, and scaffold conventions for emerging stacks such as AI orchestration, event-driven integration, retrieval-heavy systems, realtime experiences, or edge-aware deployments.
That same layer now also carries `CellBasedArchitecture`, where cell boundaries stay a technology/runtime concern instead of becoming a new blueprint or deployment shape.
They may also be supplied by packages or project code when a team needs to model a future stack that is not part of the built-in catalog yet.

## Recommended interpretation of the candidate choices

| User term | What it really is in Cephalon |
| --- | --- |
| Modular Architecture | Primary composition model |
| Microservice Architecture | Deployment topology |
| Vertical Slice Architecture | Feature organization style |
| Shared Foundation Pattern | Mandatory engine foundation |
| Strategy Pattern | Internal extension mechanism |
| Agentic / Event-Driven / Knowledge / Realtime / Edge profiles | Technology profile |
| REST API / JsonRpc / Grpc / GraphQL / SSE / WebSocket | Transport surface |

## Predefined Cephalon blueprints

Cephalon should expose a small set of predefined blueprints instead of forcing users to compose every axis manually.

Developers should also be able to add supporting design patterns on top of a blueprint instead of treating them as competing top-level architectures.

### 1. `ModularMonolith`

Default blueprint for most new products.

Shape:

- single host
- modular composition
- module-first structure
- shared engine foundation
- optional strategies inside modules

Use when:

- the product is new
- domain boundaries matter
- independent deployment is not needed yet

### 2. `ModularVerticalSlice`

Best default for product teams building features quickly.

Shape:

- single host
- modular composition
- vertical-slice organization inside each module
- shared engine foundation
- optional strategies per slice

Use when:

- the team wants strong feature ownership
- HTTP/API/application logic should stay close together
- the product may later split by domain

### 3. `Microservice`

A single service built on Cephalon engine conventions.

Shape:

- one independently deployable service
- modular composition inside the service
- usually vertical slices inside modules
- shared engine foundation

Use when:

- service boundaries are already clear
- separate deployment/scale/security boundaries are needed

### 4. `MicroserviceSuite`

A higher-level solution template, not just one app.

Shape:

- multiple Cephalon services composed from the existing `Microservice` blueprint
- a shared foundation project for suite-level contracts and conventions
- a shared governance package for suite-level policy and additive gateway/control-plane guidance
- optional gateway/control-plane pieces can layer on later without changing the suite contract

Use when:

- multiple services are intentionally designed together
- platform governance and conventions matter across services
- teams want a reference sample that keeps each service on the shipped `Microservice` host wiring while sharing one suite foundation

## Engine contract today

The engine now models these choices as first-class runtime objects, not as plain text labels.

Current shipped shape:

- `AppBlueprint` defines the built-in project shape and required baseline patterns
- `SuiteBlueprint` defines the built-in suite-level project shape when Cephalon needs a coordinated multi-service blueprint
- `AppProfile` captures the selected blueprint, patterns, technologies, transports, and scaffold plan
- `ScaffoldPlan` captures how that blueprint should turn into projects, folders, conventions, and package hints
- `SuiteScaffoldPlan` and `SuiteScaffoldService` now capture the solution-level shape for shared projects plus per-service slots without changing the current app-level runtime contract
- built-in scaffold plans live under `Cephalon.Engine.AppModel.Scaffolding`, and built-in suite blueprints now live under `Cephalon.Engine.AppModel`

Simplified contract:

```csharp
public sealed class AppProfile
{
    public string BlueprintId { get; }
    public IReadOnlyList<PatternDescriptor> Patterns { get; }
    public ScaffoldPlan? Scaffold { get; }
    public IReadOnlyList<TechnologyDescriptor> Technologies { get; }
    public IReadOnlyList<TransportDescriptor> Transports { get; }
    public DataSelection Data { get; }
    public DatabaseTopologySelection Databases { get; }
    public IdentitySelection Identity { get; }
    public TenancySelection Tenancy { get; }
    public AuditSelection Audit { get; }
    public MessagingSelection Messaging { get; }
    public ResilienceSelection Resilience { get; }
}
```

The current runtime still resolves app-level `AppProfile` objects only. The suite layer is now modeled separately through `SuiteBlueprint`, with the built-in `MicroserviceSuite` blueprint composing repeatable service slots from the shipped `Microservice` scaffold contract instead of redefining service internals at the suite layer.
The resolved app profile now also carries structured configuration selections for `Data`, `Databases`, `Identity`, `Tenancy`, `Audit`, `Messaging`, and `Resilience`. The new `Databases` block is the engine-owned physical-topology baseline for shared runtime tuning plus the first named roles (`Write`, `Read`, `Outbox`, `History`) and nested migration policy. `Audit.History` now also carries nested `Export` plus `Retention` inputs so the durable audit-history read and export surfaces stay visible in the same app-model contract. `Resilience` remains the contract-first phase-11 baseline for `Retry`, `Timeout`, `CircuitBreaker`, `Bulkhead`, and `RateLimiting` policy intent through both `/engine/app-model` and `/engine/resilience`. The ASP.NET Core endpoint-policy layer now projects effective public transport limiting through `RuntimeIntrospectionSnapshot.RateLimitingPolicies`, including request-response `grpc` coverage with gRPC-native `ResourceExhausted` rejection semantics; the gRPC adapter also enforces configured direct-module `Timeout`, `CircuitBreaker`, and `Bulkhead` policy for `IGrpcModule` calls, keeps those outcomes gRPC-native through `DeadlineExceeded` / `Unavailable` / `ResourceExhausted` responses with Cephalon metadata trailers, and reports the live `grpc-direct-module-resilience` posture through `/engine/technology-surfaces`. The JSON-RPC adapter now enforces the same configured direct-module timeout, circuit-breaker, and bulkhead posture for `IJsonRpcModule` endpoints, keeps those outcomes JSON-RPC-native through `-32053` / `-32029` error envelopes with HTTP `503` / `429` metadata, reports the live `json-rpc-direct-module-resilience` posture through `/engine/technology-surfaces`, and publishes cumulative direct-module timeout-occurrence, circuit-open transition, and circuit-open rejection counters with last-occurrence timestamps on that same surface for operator observability. The base ASP.NET Core SSE and WebSocket mappers now enforce the same configured direct-module timeout, circuit-breaker, and bulkhead posture for `IServerSentEventsModule` and `IWebSocketModule` endpoints, keep those outcomes streaming-native through SSE `event: error` payloads and WebSocket text error frames, and report `sse-direct-module-resilience` plus `websocket-direct-module-resilience` through `/engine/technology-surfaces` with `wolverineRequired=false` and `consumerCodeRequired=false`. The shipped behavior-execution follow-through also carries additive `BehaviorExecutionOverrides` plus effective retry, timeout, circuit-breaker, and bulkhead answers through `/engine/behavior-resilience` and `RuntimeIntrospectionSnapshot.BehaviorResiliencePolicies`, while the generic behavior HTTP adapters keep behavior-owned rate-limiting, bulkhead saturation, timeout, and open-circuit outcomes protocol-native; bulkhead saturation returns `429` metadata plus `behavior_execution_rejected`. Those overrides can now target behavior ids, transport ids, or both, and the runtime resolves them with behavior+transport precedence over behavior-only, transport-only, and default answers. The behavior contract layer now also lets authors declare replay safety explicitly through `BehaviorIdempotencyAttribute`, and `IBehaviorResilienceRuntimeCatalog.Resolve(...)` can reflect the resulting retry eligibility as `eligible`, `ineligible`, or `unknown` per behavior. Automatic retry execution now runs through the shared behavior pipeline with configured attempts, backoff, delays, and jitter only when the effective policy requests retry and the classifier sees an explicitly idempotent transient failure; non-idempotent or unknown behaviors still fail without automatic replay. Circuit breaker continues to participate in the same shared dispatch pipeline and publishes live runtime metadata such as circuit state and retry-after timing. `AppProfile.Databases` remains the requested topology selection, while the resolved operator-facing role truth now also lives beside it through `IDatabaseRoleCatalog`, `/engine/database-roles`, and `RuntimeIntrospectionSnapshot.DatabaseRoles`. The resolved operator-facing migration truth now lives beside it through `IDatabaseMigrationCatalog`, `/engine/database-migrations`, and `RuntimeIntrospectionSnapshot.DatabaseMigrations`. Legacy display-name aliases still resolve for compatibility, but the shipped starter and template surfaces now emit canonical kebab-case ids plus those structured sections by default.

The runtime should support configuration-driven blueprint, pattern, technology, and transport selection, for example:

```json
{
  "Engine": {
    "Blueprint": "modular-vertical-slice",
    "Patterns": ["strategy-pattern", "pipeline-pattern", "cqrs"],
    "Technologies": ["event-driven-integration", "identity-access"],
    "Transports": ["rest-api"],
    "Data": {
      "ReadWriteSplit": true,
      "Ids": {
        "Generator": "Sfid"
      }
    },
    "Identity": {
      "Enabled": true,
      "AuthorizationModes": ["RBAC", "ABAC", "Policy"]
    },
    "Tenancy": {
      "Enabled": false
    },
    "Audit": {
      "Enabled": true,
      "History": {
        "Enabled": true,
        "Provider": "entity-framework",
        "DatabaseRole": "history",
        "Export": {
          "Enabled": true,
          "MaxEntries": 1000
        },
        "Retention": {
          "Enabled": true,
          "MaxAgeDays": 90,
          "DeleteBatchSize": 250,
          "ApplyOnStartup": true
        }
      }
    },
    "Messaging": {},
    "Resilience": {
      "Retry": {
        "Enabled": true,
        "MaxAttempts": 3,
        "Backoff": "Exponential",
        "BaseDelayMilliseconds": 2000,
        "UseJitter": true
      },
      "Timeout": {
        "Enabled": true,
        "TotalTimeoutSeconds": 30,
        "AttemptTimeoutSeconds": 10
      },
      "CircuitBreaker": {
        "Enabled": true,
        "FailureRatio": 0.1,
        "MinimumThroughput": 100,
        "SamplingDurationSeconds": 30,
        "BreakDurationSeconds": 5
      },
      "Bulkhead": {
        "Enabled": true,
        "MaxConcurrentExecutions": 64,
        "MaxQueuedActions": 128
      },
      "RateLimiting": {
        "Enabled": true,
        "Algorithm": "SlidingWindow",
        "PermitLimit": 200,
        "QueueLimit": 20,
        "WindowSeconds": 60,
        "SegmentsPerWindow": 4
      }
    }
  }
}
```

That gives developers a controlled way to mix architecture choices, design patterns, future-facing technology profiles, and delivery protocols without losing validation or runtime introspection.

Technology profiles are intentionally lighter than blueprints. They should add workload semantics, dependency hints, validation rules, and scaffold conventions without forcing an entirely new project shape.
They should also be additive by default: built-ins come from the engine, packages can contribute more through `ITechnologyContributor`, and projects can register their own descriptors without rewriting the blueprint catalog.
When a profile needs reusable runtime services, Cephalon prefers a companion package such as `Cephalon.Agentics`, `Cephalon.Eventing`, `Cephalon.Retrieval`, or `Cephalon.Edge` over expanding the engine core directly.
`Engine:Technologies` selecting `event-driven-integration` activates the native `Cephalon.Eventing` package hints and scaffold wiring, but it does not imply a broker/provider adapter. `Engine:Messaging:Provider = Wolverine` remains an explicit opt-in that adds `Cephalon.Eventing.Wolverine` and its provider-managed registration.
The native pack now reads `Engine:Messaging:Channels` through `engine.AddEventingFromConfiguration(configuration)` for environment-owned channel metadata, while subscription descriptors, descriptor-bearing executors, and direct execution middleware stay code-first through `EventingOptions.Subscriptions`, `IEventSubscriptionContributor`, `services.AddCephalonEventSubscriptionExecutor<TExecutor>()`, `EventSubscriptionAttribute`, optional `IEventSubscriptionDescriptorProvider`, and `services.AddCephalonEventSubscriptionExecutionMiddleware<TMiddleware>()`; direct `IEventSubscriptionExecutor` and `IEventSubscriptionExecutionMiddleware` registrations remain supported for hosts that need custom DI ownership. `Engine:Messaging:Subscriptions` and `Engine:Messaging:SubscriptionHandlers` are rejected so hot-path publish/subscribe behavior stays typed and fast. The same helper reads `Engine:Messaging:InProcessSubscriptions`, including `Idempotency:Store = inbox` for completed-execution duplicate suppression backed by exactly one registered `IInbox`; it also reads `Engine:Messaging:Publications:Routing` for provider-neutral event-type-to-channel publication routing and explicit-channel route validation, plus `Engine:Messaging:Publications:Scheduling` for bounded process-local delayed publication acceptance through `scheduledForUtc` or `delayMilliseconds` request metadata. The messaging model should keep evolving from a provider switch into a config-driven capability model for environment-owned policy and topology: provider, transport, routing, durability, delivery guarantee, retry/backoff, scheduling, dead-letter/replay, topology, tenant/correlation policy, observability, and compliance should be modeled as engine-owned selections first, then fulfilled by the native eventing pack or an optional provider companion. Wolverine parity is the minimum benchmark for those selections, not a reason to make Wolverine the default architecture.

Remediation command idempotency follows the same app-model split. Command execution remains code-owned by the dispatcher and mutable dispatch store, while the active command journal owns reservation, durability, and duplicate rejection. ENG-579 makes `reserve-before-mutation` the native default so apps do not need Wolverine, MassTransit, or NServiceBus package semantics to close duplicate operator-command retries.

If a transport is implemented by a companion host package, the app must both select it in `Engine:Transports` and register the matching adapter in startup.

Code can still layer on top when a host needs to override or add behavior:

```csharp
builder.AddCephalon(engine =>
{
    engine.AddPattern(BuiltInPatterns.MediatorPattern);
    engine.RegisterTechnology(new TechnologyDescriptor(
        id: "digital-twin-orchestration",
        displayName: "Digital Twin Orchestration",
        description: "Project-specific catalog entry contributed in startup.",
        kind: TechnologyKind.Experience));
    engine.AddTechnology(new TechnologyDescriptor(
        id: "custom-digital-twin",
        displayName: "Custom Digital Twin",
        description: "Project-specific future technology profile.",
        kind: TechnologyKind.Experience));
});
```

The same override model now applies to language resources. Installed modules can contribute language packs through `ILocalizedResourceContributor`, hosts can declare baseline localization in `Engine:Localization`, apps can add project-specific cultures with `engine.AddLanguageResources(...)`, and projects can still replace the localization contract through DI when they need complete ownership of how human-language text is resolved.

The same override model now applies to future-tech choices as well. Built-in technology profiles come from `Cephalon.Engine.Technologies.BuiltInTechnologies`, while a project can add its own descriptors through `engine.AddTechnology(...)` when it needs to model a domain-specific runtime such as digital twins, robotics, spatial computing, or custom inference pipelines.
Installed modules and package-loaded assemblies can also contribute technology profiles through `ITechnologyContributor`, which then become visible through `/engine/technology-catalog` and selectable through `Engine:Technologies`.
Installed modules can also react to active selections through `ITechnologyServiceContributor` and `ITechnologyCapabilityContributor`, which makes future-tech profiles useful at runtime instead of only decorative in metadata.

## Scaffold plan contract

Each built-in blueprint now carries a scaffold plan that can drive future generators and templates.

The scaffold plan includes:

- `Projects`: named project templates, roles, scopes, dependencies, and recommended Cephalon package references
- `Folders`: module or feature folder conventions scoped to the owning project
- `Conventions`: startup and composition rules that keep generated hosts thin
- `Metadata`: small shape hints such as organization style or deployment topology

Current placeholder tokens in scaffold templates:

- `{AppName}`
- `{ModuleName}`
- `{FeatureName}`

Transport selection can enrich the scaffold plan further. For example, selecting `JsonRpc`, `Grpc`, or `GraphQL` adds adapter package hints and host registration conventions to the host project plan. Technology selection can enrich it too by appending workload guidance and future-ready conventions without forcing a whole new blueprint.

The same scaffold plan is visible:

- in the runtime manifest under `appProfile.scaffold`
- through `GET /engine/scaffold`

The same scaffold plan now also drives `Cephalon.Scaffolding`, which can render:

- a `.slnx` solution file
- concrete `.csproj` files
- starter `Program.cs`, split `Configurations/Add*.json` host settings, and standard `appsettings*.json` override files
- module stubs and contracts
- placeholder feature folders for blueprint-specific slice structures

`Cephalon.Cli` now sits on top of that generator so developers can pick blueprints, patterns, technologies, transports, modules, and features from one command without redefining the app model rules.

`Cephalon.TemplatePack` is now the lighter `dotnet new` surface over the shipped blueprint family:

- `cephalon-monolith` -> `ModularMonolith`
- `cephalon-slice` -> `ModularVerticalSlice`
- `cephalon-microservice` -> `Microservice`

That means Cephalon now has two generation surfaces with different tradeoffs:

- `Cephalon.TemplatePack` for fast starter installs
- `Cephalon.Cli` for richer blueprint, transport, pattern, module, and feature composition

Module authoring now has a parallel starter path too:

- `cephalon-module` for host-agnostic package starters
- `cephalon-rest-module` for REST-capable package starters

## What Cephalon now scaffolds from a blueprint

- solution/project layout
- host type and startup conventions
- module structure
- feature folder conventions
- diagnostics and manifest defaults
- test project layout
- sample modules/slices

### `ModularMonolith` scaffold shape

- host project: `src/{AppName}.Host`
- foundation project: `src/{AppName}.Foundation`
- repeatable module project: `src/{AppName}.Modules.{ModuleName}`
- test project: `tests/{AppName}.Tests`
- module folders: `Application`, `Domain`, `Infrastructure`, `Endpoints`, `Strategies`

### `ModularVerticalSlice` scaffold shape

- host project: `src/{AppName}.Host`
- foundation project: `src/{AppName}.Foundation`
- repeatable module project: `src/{AppName}.Modules.{ModuleName}`
- test project: `tests/{AppName}.Tests`
- slice folders: `Features/{FeatureName}/Commands`, `Queries`, `Endpoints`, `Contracts`, `Policies`, `Strategies`

### `Microservice` scaffold shape

- service host project: `src/{AppName}.Service`
- foundation project: `src/{AppName}.Foundation`
- contracts project: `src/{AppName}.Contracts`
- repeatable module project: `src/{AppName}.Modules.{ModuleName}`
- test project: `tests/{AppName}.Service.Tests`
- slice folders: `Features/{FeatureName}/Api`, `Application`, `Contracts`, `Policies`

## Proposed first blueprint set

- `ModularMonolith`
- `ModularVerticalSlice`
- `Microservice`
- `MicroserviceSuite`

`MicroserviceSuite` is now available as a suite-level blueprint that composes repeatable service slots from the shipped `Microservice` scaffold contract. The reference sample lives under `samples/Cephalon.Sample.MicroserviceSuite`, now includes a shared governance package, and documents additive optional gateway/control-plane guidance without turning those layers into required suite-contract pieces.

## Suggested configuration

Use the `Engine` section as the primary source of truth for blueprint, pattern, technology, and transport selection.

```json
{
  "Engine": {
    "Blueprint": "modular-vertical-slice",
    "Patterns": ["strategy-pattern", "pipeline-pattern"],
    "Technologies": ["identity-access"],
    "Transports": ["rest-api"],
    "Data": {
      "Ids": {
        "Generator": "Sfid"
      }
    },
    "Identity": {
      "Enabled": true,
      "AuthorizationModes": ["RBAC", "ABAC", "Policy"]
    },
    "Tenancy": {
      "Enabled": false
    },
    "Audit": {
      "Enabled": true,
      "History": {
        "Enabled": true,
        "Provider": "entity-framework",
        "DatabaseRole": "history",
        "Export": {
          "Enabled": true,
          "MaxEntries": 1000
        },
        "Retention": {
          "Enabled": true,
          "MaxAgeDays": 90,
          "DeleteBatchSize": 250,
          "ApplyOnStartup": true
        }
      }
    },
    "Messaging": {},
    "Resilience": {
      "Retry": {
        "Enabled": true
      }
    }
  }
}
```

## Decision rules

- if the user says `Modular`, choose `ModularMonolith` by default
- if the user says `Vertical Slice`, choose `ModularVerticalSlice` by default
- if the user says `Microservice`, generate a Cephalon-powered service, not a distributed suite automatically
- `Shared Foundation` is not optional for Cephalon apps
- `Strategy Pattern` should appear as generated extension points, not as the whole project shape
- technology profiles should carry future-facing workload guidance instead of turning every new tech stack into a blueprint
- transport selection is explicit and may include more than one protocol when the chosen host supports it

## Recommendation for Cephalon v1

For v1, support:

- `ModularMonolith`
- `ModularVerticalSlice`
- `Microservice`
- `MicroserviceSuite`

That is enough to prove the model cleanly across single-app and multi-service shapes without pretending every pattern is a first-class runtime concern on day one.
