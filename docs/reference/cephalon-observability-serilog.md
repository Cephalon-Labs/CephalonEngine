# Cephalon.Observability.Serilog

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Observability.Serilog)
## Namespaces

- `Cephalon.Observability.Serilog.Hosting`

<a id="namespace-cephalon-observability-serilog-hosting"></a>

## Namespace Cephalon.Observability.Serilog.Hosting

<a id="type-cephalon-observability-serilog-hosting-seriloghostapplicationbuilderextensions"></a>

### `SerilogHostApplicationBuilderExtensions`

Adds Serilog provider wiring for Cephalon hosts without changing the shared `ILogger` contract.

#### Declaration
```csharp
public static class SerilogHostApplicationBuilderExtensions
```

#### Methods

<a id="member-m-cephalon-observability-serilog-hosting-seriloghostapplicationbuilderextensions-addcephalonserilog-1-0-system-action-system-iserviceprovider-serilog-loggerconfiguration"></a>

##### `AddCephalonSerilog`

```csharp
TBuilder AddCephalonSerilog<TBuilder>(this TBuilder builder, Action<IServiceProvider, LoggerConfiguration> configure)
```

Adds Serilog as an `ILogger` provider for the target host builder.

Remarks: This package keeps provider-specific logging integration outside `Cephalon.Engine` and `Cephalon.Observability`. Hosts opt in explicitly when they want Serilog sinks, enrichers, or formatting while still logging through injected `ILogger<T>` services.

The standard top-level `Serilog` configuration section is read automatically when present. If no `Serilog` section exists and no code-based configuration callback is supplied, registration is skipped so hosts do not accidentally replace their existing logging setup with an empty pipeline.

Returns: The same builder instance for fluent host composition.

Type parameters:
- `TBuilder`: The host-application builder type to extend.

Parameters:
- `builder`: The target host-application builder.
- `configure`: An optional callback that can extend or override the configuration-driven Serilog pipeline.
