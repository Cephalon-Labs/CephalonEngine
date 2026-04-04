# Cephalon.Ids.Sfid

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Ids.Sfid)
## Namespaces

- `Cephalon.Ids.Sfid.Configuration`
- `Cephalon.Ids.Sfid.Registration`

<a id="namespace-cephalon-ids-sfid-configuration"></a>

## Namespace Cephalon.Ids.Sfid.Configuration

<a id="type-cephalon-ids-sfid-configuration-sfididoptions"></a>

### `SfidIdOptions`

Describes the host-owned configuration used to bootstrap the official `Sfid.Net` generator.

#### Declaration
```csharp
public sealed class SfidIdOptions
```

#### Constructors

<a id="member-m-cephalon-ids-sfid-configuration-sfididoptions-ctor-system-nullable-system-int32-system-nullable-system-int32-system-nullable-system-int32-system-nullable-system-int32"></a>

##### `SfidIdOptions`

```csharp
SfidIdOptions(int? datacenterId, int? workerId, int? workerCapacity, int? clockRegressionToleranceMilliseconds)
```

Initializes a new instance of the `SfidIdOptions` class.

Parameters:
- `datacenterId`: The datacenter identifier supplied to the generator.
- `workerId`: The worker identifier supplied to the generator.
- `workerCapacity`: The optional worker-capacity override supplied to the generator.
- `clockRegressionToleranceMilliseconds`: The optional clock-regression tolerance, in milliseconds, supplied to the generator.

#### Fields

<a id="member-f-cephalon-ids-sfid-configuration-sfididoptions-defaultsectionpath"></a>

##### `DefaultSectionPath`

```csharp
const string DefaultSectionPath
```

Gets the default configuration path used by the Sfid id-strategy pack.

#### Properties

<a id="member-p-cephalon-ids-sfid-configuration-sfididoptions-clockregressiontolerancemilliseconds"></a>

##### `ClockRegressionToleranceMilliseconds`

```csharp
int? ClockRegressionToleranceMilliseconds { get; set; }
```

Gets the optional clock-regression tolerance, in milliseconds, supplied to the generator.

<a id="member-p-cephalon-ids-sfid-configuration-sfididoptions-datacenterid"></a>

##### `DatacenterId`

```csharp
int? DatacenterId { get; set; }
```

Gets the datacenter identifier supplied to the generator.

<a id="member-p-cephalon-ids-sfid-configuration-sfididoptions-empty"></a>

##### `Empty`

```csharp
SfidIdOptions Empty { get; }
```

Gets an empty options instance.

<a id="member-p-cephalon-ids-sfid-configuration-sfididoptions-hasvalues"></a>

##### `HasValues`

```csharp
bool HasValues { get; }
```

Gets a value indicating whether any explicit Sfid-generator inputs were supplied.

<a id="member-p-cephalon-ids-sfid-configuration-sfididoptions-workercapacity"></a>

##### `WorkerCapacity`

```csharp
int? WorkerCapacity { get; set; }
```

Gets the optional worker-capacity override supplied to the generator.

<a id="member-p-cephalon-ids-sfid-configuration-sfididoptions-workerid"></a>

##### `WorkerId`

```csharp
int? WorkerId { get; set; }
```

Gets the worker identifier supplied to the generator.

#### Methods

<a id="member-m-cephalon-ids-sfid-configuration-sfididoptions-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `FromConfiguration`

```csharp
SfidIdOptions FromConfiguration(IConfiguration configuration, string sectionPath)
```

Reads Sfid id-strategy options from configuration.

Returns: The parsed options.

Parameters:
- `configuration`: The configuration root to read.
- `sectionPath`: The configuration path that contains the Sfid id-strategy settings. The default is `Engine:Data:Ids:Sfid`.

<a id="namespace-cephalon-ids-sfid-registration"></a>

## Namespace Cephalon.Ids.Sfid.Registration

<a id="type-cephalon-ids-sfid-registration-sfidenginebuilderextensions"></a>

### `SfidEngineBuilderExtensions`

Registers the official `Sfid.Net`-backed identifier strategy pack with an `EngineBuilder`.

#### Declaration
```csharp
public static class SfidEngineBuilderExtensions
```

#### Methods

<a id="member-m-cephalon-ids-sfid-registration-sfidenginebuilderextensions-addsfidids-cephalon-engine-composition-enginebuilder-system-action-cephalon-ids-sfid-configuration-sfididoptions"></a>

##### `AddSfidIds`

```csharp
EngineBuilder AddSfidIds(this EngineBuilder builder, Action<SfidIdOptions> configure)
```

Adds the Sfid id-strategy pack to the engine.

Returns: The same engine builder for fluent composition.

Parameters:
- `builder`: The engine builder to extend.
- `configure`: An optional callback that configures the host-owned Sfid generator options.
