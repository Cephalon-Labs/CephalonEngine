# Cephalon.Worker

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Worker)
## Namespaces

- `Cephalon.Worker.Hosting`

<a id="namespace-cephalon-worker-hosting"></a>

## Namespace Cephalon.Worker.Hosting

<a id="type-cephalon-worker-hosting-workerhostapplicationbuilderextensions"></a>

### `WorkerHostApplicationBuilderExtensions`

Registers the Cephalon worker host adapter on a `HostApplicationBuilder`.

#### Declaration
```csharp
public static class WorkerHostApplicationBuilderExtensions
```

#### Methods

<a id="member-m-cephalon-worker-hosting-workerhostapplicationbuilderextensions-addcephalon-microsoft-extensions-hosting-hostapplicationbuilder"></a>

##### `AddCephalon`

```csharp
HostApplicationBuilder AddCephalon(this HostApplicationBuilder builder)
```

Adds Cephalon worker hosting using configuration-only engine setup.

Returns: The same builder instance for fluent composition.

Parameters:
- `builder`: The generic host application builder to extend.

<a id="member-m-cephalon-worker-hosting-workerhostapplicationbuilderextensions-addcephalon-microsoft-extensions-hosting-hostapplicationbuilder-system-action-cephalon-engine-composition-enginebuilder"></a>

##### `AddCephalon`

```csharp
HostApplicationBuilder AddCephalon(this HostApplicationBuilder builder, Action<EngineBuilder> configure)
```

Adds Cephalon worker hosting and allows additional code-based engine configuration.

Returns: The same builder instance for fluent composition.

Parameters:
- `builder`: The generic host application builder to extend.
- `configure`: The callback that configures the underlying engine builder.

<a id="member-m-cephalon-worker-hosting-workerhostapplicationbuilderextensions-addcephalonprojectconfigurations-microsoft-extensions-hosting-hostapplicationbuilder"></a>

##### `AddCephalonProjectConfigurations`

```csharp
HostApplicationBuilder AddCephalonProjectConfigurations(this HostApplicationBuilder builder)
```

Adds Cephalon's project-configuration conventions to the generic host builder.

Remarks: This loads split configuration files from the project's `Configurations` folder so engine and host-specific settings can be grouped by concern instead of one large JSON file.

Returns: The same builder instance for fluent composition.

Parameters:
- `builder`: The generic host application builder to extend.

<a id="type-cephalon-worker-hosting-workerservicecollectionextensions"></a>

### `WorkerServiceCollectionExtensions`

Adds the Cephalon worker adapter and runtime services to an `IServiceCollection`.

#### Declaration
```csharp
public static class WorkerServiceCollectionExtensions
```

#### Methods

<a id="member-m-cephalon-worker-hosting-workerservicecollectionextensions-addcephalonworker-microsoft-extensions-dependencyinjection-iservicecollection-system-action-cephalon-engine-composition-enginebuilder"></a>

##### `AddCephalonWorker`

```csharp
IServiceCollection AddCephalonWorker(this IServiceCollection services, Action<EngineBuilder> configure)
```

Adds Cephalon worker hosting using code-first engine configuration.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.
- `configure`: The callback that configures the engine builder.

<a id="member-m-cephalon-worker-hosting-workerservicecollectionextensions-addcephalonworker-microsoft-extensions-dependencyinjection-iservicecollection-microsoft-extensions-configuration-iconfiguration-system-action-cephalon-engine-composition-enginebuilder-system-string"></a>

##### `AddCephalonWorker`

```csharp
IServiceCollection AddCephalonWorker(this IServiceCollection services, IConfiguration configuration, Action<EngineBuilder> configure, string sectionPath)
```

Adds Cephalon worker hosting using configuration as the primary source of engine settings.

Returns: The same service collection for further registration.

Parameters:
- `services`: The target service collection.
- `configuration`: The application configuration root.
- `configure`: An optional callback that can extend or override the configuration-driven engine setup.
- `sectionPath`: The configuration section path that contains the engine settings. The default is `Engine`.
