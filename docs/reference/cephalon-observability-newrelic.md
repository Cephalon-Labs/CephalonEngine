# Cephalon.Observability.NewRelic

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Observability.NewRelic)
## Namespaces

- `Cephalon.Observability.NewRelic.Configuration`
- `Cephalon.Observability.NewRelic.Hosting`

<a id="namespace-cephalon-observability-newrelic-configuration"></a>

## Namespace Cephalon.Observability.NewRelic.Configuration

<a id="type-cephalon-observability-newrelic-configuration-newrelictelemetryexportoptions"></a>

### `NewRelicTelemetryExportOptions`

Configures New Relic observability defaults on top of the shared Cephalon telemetry contract.

#### Declaration
```csharp
public sealed class NewRelicTelemetryExportOptions
```

#### Constructors

<a id="member-m-cephalon-observability-newrelic-configuration-newrelictelemetryexportoptions-ctor"></a>

##### `NewRelicTelemetryExportOptions`

```csharp
NewRelicTelemetryExportOptions()
```

Initializes a new instance of the `NewRelicTelemetryExportOptions` class.

#### Properties

<a id="member-p-cephalon-observability-newrelic-configuration-newrelictelemetryexportoptions-endpoint"></a>

##### `Endpoint`

```csharp
string Endpoint { get; set; }
```

Gets or sets the base New Relic OTLP endpoint used for direct ingestion.

Remarks: If this value is omitted and direct ingestion is enabled, the package derives the endpoint from `Region`. For OTLP/HTTP the package treats this as the base endpoint and appends the signal-specific `/v1/traces`, `/v1/metrics`, or `/v1/logs` suffix automatically.

<a id="member-p-cephalon-observability-newrelic-configuration-newrelictelemetryexportoptions-headers"></a>

##### `Headers`

```csharp
string Headers { get; set; }
```

Gets or sets the raw OTLP headers string used for direct New Relic ingestion.

Remarks: Use the standard OTLP `key=value` comma-separated format. This value takes precedence over `LicenseKey` when both are configured.

<a id="member-p-cephalon-observability-newrelic-configuration-newrelictelemetryexportoptions-licensekey"></a>

##### `LicenseKey`

```csharp
string LicenseKey { get; set; }
```

Gets or sets the New Relic license key used to build the required `api-key` header when the package should construct OTLP headers from structured settings.

<a id="member-p-cephalon-observability-newrelic-configuration-newrelictelemetryexportoptions-region"></a>

##### `Region`

```csharp
string Region { get; set; }
```

Gets or sets the New Relic OTLP region used when the package derives the direct-ingestion endpoint.

Remarks: Supported values are `us`, `eu`, and `fedramp`. If omitted, the package defaults to the New Relic US OTLP endpoint.

<a id="member-p-cephalon-observability-newrelic-configuration-newrelictelemetryexportoptions-servicenamespace"></a>

##### `ServiceNamespace`

```csharp
string ServiceNamespace { get; set; }
```

Gets or sets the optional `service.namespace` resource attribute to stamp onto exported telemetry.

<a id="member-p-cephalon-observability-newrelic-configuration-newrelictelemetryexportoptions-usenativeotlpendpoint"></a>

##### `UseNativeOtlpEndpoint`

```csharp
bool UseNativeOtlpEndpoint { get; set; }
```

Gets or sets a value indicating whether the package should target the New Relic native OTLP endpoint when no shared collector endpoint is configured.

Remarks: This direct path is intended for the documented New Relic native OTLP ingestion route. Teams that prefer a collector or gateway can keep using the shared `Endpoint` or `UseSelfHostedDefaults` contract instead.

#### Methods

<a id="member-m-cephalon-observability-newrelic-configuration-newrelictelemetryexportoptions-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `FromConfiguration`

```csharp
NewRelicTelemetryExportOptions FromConfiguration(IConfiguration configuration, string sectionPath)
```

Binds New Relic telemetry export options from configuration.

Returns: The bound New Relic telemetry export options.

Parameters:
- `configuration`: The application configuration root.
- `sectionPath`: The configuration section path that contains the engine settings. The default is `Engine`.

<a id="namespace-cephalon-observability-newrelic-hosting"></a>

## Namespace Cephalon.Observability.NewRelic.Hosting

<a id="type-cephalon-observability-newrelic-hosting-newrelichostapplicationbuilderextensions"></a>

### `NewRelicHostApplicationBuilderExtensions`

Adds New Relic native OTLP endpoint wiring and `api-key` authentication guidance for Cephalon hosts.

#### Declaration
```csharp
public static class NewRelicHostApplicationBuilderExtensions
```

#### Methods

<a id="member-m-cephalon-observability-newrelic-hosting-newrelichostapplicationbuilderextensions-addcephalonnewrelic-1-0-system-action-cephalon-observability-newrelic-configuration-newrelictelemetryexportoptions"></a>

##### `AddCephalonNewRelic`

```csharp
TBuilder AddCephalonNewRelic<TBuilder>(this TBuilder builder, Action<NewRelicTelemetryExportOptions> configure)
```

Adds New Relic-aware OpenTelemetry registration for the Cephalon engine diagnostics surface.

Remarks: This package keeps New Relic-specific OTLP endpoint and `api-key` authentication concerns outside `Cephalon.Engine` and the baseline observability package. It still uses the shared `Engine:Observability:Telemetry` contract so hosts can keep one explicit telemetry surface.

When `Endpoint` or `UseSelfHostedDefaults` is configured on the shared telemetry contract, the package keeps using that collector-oriented OTLP path and only layers New Relic-friendly resource attributes on top. When those shared endpoint settings are absent and the New Relic options opt into direct endpoint usage, the package targets either the configured New Relic OTLP endpoint or the documented regional default and applies either the raw OTLP headers string or the required `api-key` header built from the configured license key.

Returns: The same builder instance for fluent host composition.

Type parameters:
- `TBuilder`: The host-application builder type to extend.

Parameters:
- `builder`: The target host-application builder.
- `configure`: An optional callback that can extend or override the configuration-driven New Relic telemetry export options.
