# Cephalon.Data

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Data)
## Namespaces

- `Cephalon.Data.Configuration`
- `Cephalon.Data.Registration`

<a id="namespace-cephalon-data-configuration"></a>

## Namespace Cephalon.Data.Configuration

<a id="type-cephalon-data-configuration-dataruntimeoptions"></a>

### `DataRuntimeOptions`

Describes the host-owned options for the runtime-neutral Cephalon data pack.

#### Declaration
```csharp
public sealed class DataRuntimeOptions
```

#### Constructors

<a id="member-m-cephalon-data-configuration-dataruntimeoptions-ctor"></a>

##### `DataRuntimeOptions`

```csharp
DataRuntimeOptions()
```

Initializes a new instance of the `DataRuntimeOptions` class.

#### Properties

<a id="member-p-cephalon-data-configuration-dataruntimeoptions-registerreadstore"></a>

##### `RegisterReadStore`

```csharp
bool RegisterReadStore { get; set; }
```

Gets or sets a value indicating whether the pack should register the default read-store dispatcher.

<a id="member-p-cephalon-data-configuration-dataruntimeoptions-registerwritestore"></a>

##### `RegisterWriteStore`

```csharp
bool RegisterWriteStore { get; set; }
```

Gets or sets a value indicating whether the pack should register the default write-store dispatcher.

<a id="namespace-cephalon-data-registration"></a>

## Namespace Cephalon.Data.Registration

<a id="type-cephalon-data-registration-dataenginebuilderextensions"></a>

### `DataEngineBuilderExtensions`

Registers the runtime-neutral data pack with an `EngineBuilder`.

#### Declaration
```csharp
public static class DataEngineBuilderExtensions
```

#### Methods

<a id="member-m-cephalon-data-registration-dataenginebuilderextensions-adddata-cephalon-engine-composition-enginebuilder-system-action-cephalon-data-configuration-dataruntimeoptions"></a>

##### `AddData`

```csharp
EngineBuilder AddData(this EngineBuilder builder, Action<DataRuntimeOptions> configure)
```

Adds the data runtime pack to the engine.

Returns: The same engine builder for fluent composition.

Parameters:
- `builder`: The engine builder to extend.
- `configure`: An optional callback that configures the host-owned data runtime options.
