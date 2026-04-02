# Cephalon.Observability.OpenTelemetry

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Observability.OpenTelemetry)
## Namespaces

- `Cephalon.Observability.OpenTelemetry.Hosting`

<a id="namespace-cephalon-observability-opentelemetry-hosting"></a>

## Namespace Cephalon.Observability.OpenTelemetry.Hosting

<a id="type-cephalon-observability-opentelemetry-hosting-opentelemetryhostapplicationbuilderextensions"></a>

### `OpenTelemetryHostApplicationBuilderExtensions`

Adds OpenTelemetry OTLP exporter wiring for Cephalon hosts.

#### Declaration
```csharp
public static class OpenTelemetryHostApplicationBuilderExtensions
```

#### Methods

<a id="member-m-cephalon-observability-opentelemetry-hosting-opentelemetryhostapplicationbuilderextensions-addcephalonopentelemetry-1-0-system-action-cephalon-observability-configuration-telemetryexportoptions"></a>

##### `AddCephalonOpenTelemetry`

```csharp
TBuilder AddCephalonOpenTelemetry<TBuilder>(this TBuilder builder, Action<TelemetryExportOptions> configure)
```

Adds OpenTelemetry exporter registration for the Cephalon engine diagnostics surface.

Remarks: This package keeps exporter wiring outside `Cephalon.Engine` and `Cephalon.Observability`. Hosts opt in explicitly when they want a supported OpenTelemetry path instead of guidance-only settings.

Registration is skipped when no export endpoint is configured or when every signal is disabled. The endpoint is interpreted as a base collector endpoint for HTTP/protobuf and the signal-specific OTLP paths are appended automatically.

Returns: The same builder instance for fluent host composition.

Type parameters:
- `TBuilder`: The host-application builder type to extend.

Parameters:
- `builder`: The target host-application builder.
- `configure`: An optional callback that can extend or override the configuration-driven telemetry export options.
