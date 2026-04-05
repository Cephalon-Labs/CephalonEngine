# Cephalon.Eventing.Wolverine

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Eventing.Wolverine)
## Namespaces

- `Cephalon.Eventing.Wolverine.Configuration`
- `Cephalon.Eventing.Wolverine.Registration`

<a id="namespace-cephalon-eventing-wolverine-configuration"></a>

## Namespace Cephalon.Eventing.Wolverine.Configuration

<a id="type-cephalon-eventing-wolverine-configuration-wolverineeventingoptions"></a>

### `WolverineEventingOptions`

Describes the host-owned options for the official Wolverine eventing companion pack.

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

<a id="member-p-cephalon-eventing-wolverine-configuration-wolverineeventingoptions-retrydelayseconds"></a>

##### `RetryDelaySeconds`

```csharp
int RetryDelaySeconds { get; set; }
```

Gets or sets the number of seconds the Wolverine-owned dispatch loop should wait before retrying a failed dispatch attempt.

<a id="namespace-cephalon-eventing-wolverine-registration"></a>

## Namespace Cephalon.Eventing.Wolverine.Registration

<a id="type-cephalon-eventing-wolverine-registration-wolverineeventingenginebuilderextensions"></a>

### `WolverineEventingEngineBuilderExtensions`

Registers the official Wolverine eventing companion pack with an `EngineBuilder`.

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
