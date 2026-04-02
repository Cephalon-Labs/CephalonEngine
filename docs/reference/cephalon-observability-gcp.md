# Cephalon.Observability.Gcp

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Observability.Gcp)
## Namespaces

- `Cephalon.Observability.Gcp.Configuration`
- `Cephalon.Observability.Gcp.Hosting`

<a id="namespace-cephalon-observability-gcp-configuration"></a>

## Namespace Cephalon.Observability.Gcp.Configuration

<a id="type-cephalon-observability-gcp-configuration-gcptelemetryexportoptions"></a>

### `GcpTelemetryExportOptions`

Configures GCP-hosted observability defaults on top of the shared Cephalon telemetry contract.

#### Declaration
```csharp
public sealed class GcpTelemetryExportOptions
```

#### Constructors

<a id="member-m-cephalon-observability-gcp-configuration-gcptelemetryexportoptions-ctor"></a>

##### `GcpTelemetryExportOptions`

```csharp
GcpTelemetryExportOptions()
```

Initializes a new instance of the `GcpTelemetryExportOptions` class.

#### Properties

<a id="member-p-cephalon-observability-gcp-configuration-gcptelemetryexportoptions-hostedplatform"></a>

##### `HostedPlatform`

```csharp
string HostedPlatform { get; set; }
```

Gets or sets the hosted GCP platform whose default resource attributes should be applied.

Remarks: Supported values are `gce`, `gke`, `cloudrun`, `appengine`, and `functions`. The package maps them to the current OpenTelemetry `cloud.platform` attribute values.

<a id="member-p-cephalon-observability-gcp-configuration-gcptelemetryexportoptions-location"></a>

##### `Location`

```csharp
string Location { get; set; }
```

Gets or sets the GCP location to stamp onto exported resources when one should be made explicit.

Remarks: When the value looks like a zonal location such as `asia-southeast1-b`, the package also derives the matching `cloud.region`. When omitted, the package falls back to common GCP runtime environment variables when they are available.

<a id="member-p-cephalon-observability-gcp-configuration-gcptelemetryexportoptions-quotaprojectid"></a>

##### `QuotaProjectId`

```csharp
string QuotaProjectId { get; set; }
```

Gets or sets the optional quota project header used for Google-managed ingestion requests.

Remarks: This value is written to the `x-goog-user-project` header only for Google-managed ingestion. Shared collector or self-hosted OTLP paths ignore it.

<a id="member-p-cephalon-observability-gcp-configuration-gcptelemetryexportoptions-useapplicationdefaultcredentials"></a>

##### `UseApplicationDefaultCredentials`

```csharp
bool UseApplicationDefaultCredentials { get; set; }
```

Gets or sets a value indicating whether Google-managed ingestion should authenticate by using Application Default Credentials.

Remarks: This setting only applies when `UseGoogleManagedIngestion` is enabled and no shared collector endpoint is configured.

<a id="member-p-cephalon-observability-gcp-configuration-gcptelemetryexportoptions-usegooglemanagedingestion"></a>

##### `UseGoogleManagedIngestion`

```csharp
bool UseGoogleManagedIngestion { get; set; }
```

Gets or sets a value indicating whether the package should use Google-managed OTLP ingestion for traces and metrics when no shared collector endpoint is configured.

Remarks: When this flag is enabled, the package targets `https://telemetry.googleapis.com` for traces and metrics by using OTLP/HTTP plus Application Default Credentials. Logs stay on the shared collector or platform logging path instead of being redirected through the Google-managed ingestion endpoint.

#### Methods

<a id="member-m-cephalon-observability-gcp-configuration-gcptelemetryexportoptions-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `FromConfiguration`

```csharp
GcpTelemetryExportOptions FromConfiguration(IConfiguration configuration, string sectionPath)
```

Binds GCP telemetry export options from configuration.

Returns: The bound GCP telemetry export options.

Parameters:
- `configuration`: The application configuration root.
- `sectionPath`: The configuration section path that contains the engine settings. The default is `Engine`.

<a id="namespace-cephalon-observability-gcp-hosting"></a>

## Namespace Cephalon.Observability.Gcp.Hosting

<a id="type-cephalon-observability-gcp-hosting-gcphostapplicationbuilderextensions"></a>

### `GcpHostApplicationBuilderExtensions`

Adds GCP-hosted observability defaults and OTLP exporter wiring for Cephalon hosts.

#### Declaration
```csharp
public static class GcpHostApplicationBuilderExtensions
```

#### Methods

<a id="member-m-cephalon-observability-gcp-hosting-gcphostapplicationbuilderextensions-addcephalongcp-1-0-system-action-cephalon-observability-gcp-configuration-gcptelemetryexportoptions"></a>

##### `AddCephalonGcp`

```csharp
TBuilder AddCephalonGcp<TBuilder>(this TBuilder builder, Action<GcpTelemetryExportOptions> configure)
```

Adds GCP-aware OpenTelemetry registration for the Cephalon engine diagnostics surface.

Remarks: This package keeps GCP-specific resource defaults and Google-managed ingestion concerns outside `Cephalon.Engine` and the baseline observability package. It still uses the shared `Engine:Observability:Telemetry` contract so hosts can keep one explicit telemetry surface.

When `Endpoint` or `UseSelfHostedDefaults` is configured, the package keeps using the shared collector-oriented OTLP path and layers GCP resource defaults on top. When those shared endpoint settings are absent and `UseGoogleManagedIngestion` is enabled, the package targets `https://telemetry.googleapis.com` for traces and metrics over OTLP/HTTP by using Application Default Credentials, while logs stay on the shared collector or platform logging path.

Returns: The same builder instance for fluent host composition.

Type parameters:
- `TBuilder`: The host-application builder type to extend.

Parameters:
- `builder`: The target host-application builder.
- `configure`: An optional callback that can extend or override the configuration-driven GCP telemetry export options.
