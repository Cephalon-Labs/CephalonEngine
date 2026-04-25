# Cephalon.Eventing.Wolverine

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Eventing.Wolverine)
## Namespaces

- `Cephalon.Eventing.Wolverine.Configuration`
- `Cephalon.Eventing.Wolverine.Registration`
- `Cephalon.Eventing.Wolverine.Services`

<a id="namespace-cephalon-eventing-wolverine-configuration"></a>

## Namespace Cephalon.Eventing.Wolverine.Configuration

<a id="type-cephalon-eventing-wolverine-configuration-wolverineeventingoptions"></a>

### `WolverineEventingOptions`

Describes the host-owned options for the optional Wolverine eventing companion pack.

#### Declaration
```csharp
public sealed class WolverineEventingOptions
```

#### Constructors

<a id="member-m-cephalon-eventing-wolverine-configuration-wolverineeventingoptions-ctor"></a>

##### `WolverineEventingOptions`

```csharp
WolverineEventingOptions()
```

Initializes a new instance of the `WolverineEventingOptions` class.

#### Properties

<a id="member-p-cephalon-eventing-wolverine-configuration-wolverineeventingoptions-configurehost"></a>

##### `ConfigureHost`

```csharp
Action<WolverineOptions> ConfigureHost { get; set; }
```

Gets or sets an optional callback that can extend Wolverine host wiring before the runtime starts.

<a id="member-p-cephalon-eventing-wolverine-configuration-wolverineeventingoptions-dispatchbatchsize"></a>

##### `DispatchBatchSize`

```csharp
int DispatchBatchSize { get; set; }
```

Gets or sets the maximum number of staged events the Wolverine-owned dispatch loop should read per polling cycle.

<a id="member-p-cephalon-eventing-wolverine-configuration-wolverineeventingoptions-dispatchpollingintervalseconds"></a>

##### `DispatchPollingIntervalSeconds`

```csharp
int DispatchPollingIntervalSeconds { get; set; }
```

Gets or sets the number of seconds the Wolverine-owned dispatch loop should wait between polling cycles.

<a id="member-p-cephalon-eventing-wolverine-configuration-wolverineeventingoptions-enabledispatchloop"></a>

##### `EnableDispatchLoop`

```csharp
bool EnableDispatchLoop { get; set; }
```

Gets or sets a value indicating whether the pack should own the durable staged-event dispatch loop instead of leaving dispatch consumer-managed. Defaults to `false`.

<a id="member-p-cephalon-eventing-wolverine-configuration-wolverineeventingoptions-enablehostwiring"></a>

##### `EnableHostWiring`

```csharp
bool EnableHostWiring { get; set; }
```

Gets or sets a value indicating whether the pack should register Wolverine host wiring into the current service collection.

<a id="member-p-cephalon-eventing-wolverine-configuration-wolverineeventingoptions-enableruntimesurface"></a>

##### `EnableRuntimeSurface`

```csharp
bool EnableRuntimeSurface { get; set; }
```

Gets or sets a value indicating whether the pack should publish its runtime surface into Cephalon technology introspection.

<a id="member-p-cephalon-eventing-wolverine-configuration-wolverineeventingoptions-enablesubscriptionexecution"></a>

##### `EnableSubscriptionExecution`

```csharp
bool EnableSubscriptionExecution { get; set; }
```

Gets or sets a value indicating whether the pack should execute declared event subscriptions through the Wolverine-managed staged-event dispatch path. Defaults to `false`.

<a id="member-p-cephalon-eventing-wolverine-configuration-wolverineeventingoptions-retrydelayseconds"></a>

##### `RetryDelaySeconds`

```csharp
int RetryDelaySeconds { get; set; }
```

Gets or sets the number of seconds the Wolverine-owned dispatch loop should wait before retrying a failed dispatch attempt.

<a id="member-p-cephalon-eventing-wolverine-configuration-wolverineeventingoptions-subscriptionretrydelayseconds"></a>

##### `SubscriptionRetryDelaySeconds`

```csharp
int SubscriptionRetryDelaySeconds { get; set; }
```

Gets or sets the number of seconds the Wolverine-managed subscription execution path should wait before requeueing a failed subscription attempt.

<a id="namespace-cephalon-eventing-wolverine-registration"></a>

## Namespace Cephalon.Eventing.Wolverine.Registration

<a id="type-cephalon-eventing-wolverine-registration-wolverineeventingenginebuilderextensions"></a>

### `WolverineEventingEngineBuilderExtensions`

Registers the optional Wolverine eventing companion pack with an `EngineBuilder`.

#### Declaration
```csharp
public static class WolverineEventingEngineBuilderExtensions
```

#### Methods

<a id="member-m-cephalon-eventing-wolverine-registration-wolverineeventingenginebuilderextensions-addwolverineeventing-cephalon-engine-composition-enginebuilder-system-action-cephalon-eventing-wolverine-configuration-wolverineeventingoptions"></a>

##### `AddWolverineEventing`

```csharp
EngineBuilder AddWolverineEventing(this EngineBuilder builder, Action<WolverineEventingOptions> configure)
```

Adds the Wolverine eventing companion pack to the engine.

Returns: The same engine builder for fluent composition.

Parameters:
- `builder`: The engine builder to extend.
- `configure`: An optional callback that configures the host-owned Wolverine eventing options, including the opt-in managed dispatch loop.

<a id="namespace-cephalon-eventing-wolverine-services"></a>

## Namespace Cephalon.Eventing.Wolverine.Services

<a id="type-cephalon-eventing-wolverine-services-wolverinemanagedeventsubscriptionexecutionhandler"></a>

### `WolverineManagedEventSubscriptionExecutionHandler`

Infrastructure retry handler used by the Wolverine eventing pack for managed subscription executions.

#### Declaration
```csharp
public sealed class WolverineManagedEventSubscriptionExecutionHandler
```

#### Constructors

<a id="member-m-cephalon-eventing-wolverine-services-wolverinemanagedeventsubscriptionexecutionhandler-ctor"></a>

##### `WolverineManagedEventSubscriptionExecutionHandler`

```csharp
WolverineManagedEventSubscriptionExecutionHandler()
```

Initializes a new instance of the `WolverineManagedEventSubscriptionExecutionHandler` class.

#### Methods

<a id="member-m-cephalon-eventing-wolverine-services-wolverinemanagedeventsubscriptionexecutionhandler-handle-cephalon-eventing-wolverine-services-wolverinemanagedeventsubscriptionexecutionrequest-wolverine-envelope-system-iserviceprovider-wolverine-imessagebus-system-threading-cancellationtoken"></a>

##### `Handle`

```csharp
Task Handle(WolverineManagedEventSubscriptionExecutionRequest request, Envelope envelope, IServiceProvider services, IMessageBus messageBus, CancellationToken cancellationToken)
```

Replays one managed subscription execution attempt from Wolverine's scheduled-message pipeline.

Returns: A task that completes when the managed retry attempt finishes.

Parameters:
- `request`: The infrastructure retry message describing the managed subscription attempt.
- `envelope`: The Wolverine envelope that carries retry-attempt metadata.
- `services`: The current service provider scope.
- `messageBus`: The active Wolverine message bus.
- `cancellationToken`: The cancellation token for the current retry attempt.

<a id="type-cephalon-eventing-wolverine-services-wolverinemanagedeventsubscriptionexecutionrequest"></a>

### `WolverineManagedEventSubscriptionExecutionRequest`

Infrastructure message used by the Wolverine eventing pack to requeue managed subscription executions.

#### Declaration
```csharp
public sealed class WolverineManagedEventSubscriptionExecutionRequest
```

#### Constructors

<a id="member-m-cephalon-eventing-wolverine-services-wolverinemanagedeventsubscriptionexecutionrequest-ctor-system-string-cephalon-eventing-services-eventpublication"></a>

##### `WolverineManagedEventSubscriptionExecutionRequest`

```csharp
WolverineManagedEventSubscriptionExecutionRequest(string subscriptionId, EventPublication publication)
```

Creates a new infrastructure retry message for one managed event-subscription execution.

Parameters:
- `subscriptionId`: The declared subscription identifier that should be retried.
- `publication`: The staged publication that should be delivered to the subscription.

#### Properties

<a id="member-p-cephalon-eventing-wolverine-services-wolverinemanagedeventsubscriptionexecutionrequest-publication"></a>

##### `Publication`

```csharp
EventPublication Publication { get; }
```

Gets the staged publication that should be delivered to the managed subscription.

<a id="member-p-cephalon-eventing-wolverine-services-wolverinemanagedeventsubscriptionexecutionrequest-subscriptionid"></a>

##### `SubscriptionId`

```csharp
string SubscriptionId { get; }
```

Gets the declared subscription identifier that should be executed.
