# Cephalon.Observability.Aws

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Observability.Aws)
## Namespaces

- `Cephalon.Observability.Aws.Configuration`
- `Cephalon.Observability.Aws.Hosting`

<a id="namespace-cephalon-observability-aws-configuration"></a>

## Namespace Cephalon.Observability.Aws.Configuration

<a id="type-cephalon-observability-aws-configuration-awstelemetryexportoptions"></a>

### `AwsTelemetryExportOptions`

Configures AWS-hosted observability defaults on top of the shared Cephalon telemetry contract.

#### Declaration
```csharp
public sealed class AwsTelemetryExportOptions
```

#### Constructors

<a id="member-m-cephalon-observability-aws-configuration-awstelemetryexportoptions-ctor"></a>

##### `AwsTelemetryExportOptions`

```csharp
AwsTelemetryExportOptions()
```

Initializes a new instance of the `AwsTelemetryExportOptions` class.

#### Properties

<a id="member-p-cephalon-observability-aws-configuration-awstelemetryexportoptions-enableawssdkinstrumentation"></a>

##### `EnableAwsSdkInstrumentation`

```csharp
bool EnableAwsSdkInstrumentation { get; set; }
```

Gets or sets a value indicating whether AWS SDK client instrumentation should be enabled for traces.

<a id="member-p-cephalon-observability-aws-configuration-awstelemetryexportoptions-enablelambdacontextextraction"></a>

##### `EnableLambdaContextExtraction`

```csharp
bool EnableLambdaContextExtraction { get; set; }
```

Gets or sets a value indicating whether Lambda context extraction should be configured when the hosted platform is `lambda`.

Remarks: This does not wrap Lambda handlers automatically. It only configures the OpenTelemetry Lambda extension so hosts that already use the wrapper APIs can keep AWS X-Ray context extraction aligned.

<a id="member-p-cephalon-observability-aws-configuration-awstelemetryexportoptions-hostedplatform"></a>

##### `HostedPlatform`

```csharp
string HostedPlatform { get; set; }
```

Gets or sets the hosted AWS platform whose default resource attributes and detectors should be applied.

Remarks: Supported values are `ec2`, `ecs`, `eks`, `elasticbeanstalk`, and `lambda`. The package maps them to the current OpenTelemetry `cloud.platform` attribute values and uses the matching AWS resource detector when one is available.

<a id="member-p-cephalon-observability-aws-configuration-awstelemetryexportoptions-usexraypropagator"></a>

##### `UseXRayPropagator`

```csharp
bool UseXRayPropagator { get; set; }
```

Gets or sets a value indicating whether the AWS X-Ray text-map propagator should become the default propagator for the host when traces are enabled.

<a id="member-p-cephalon-observability-aws-configuration-awstelemetryexportoptions-usexraytraceids"></a>

##### `UseXRayTraceIds`

```csharp
bool UseXRayTraceIds { get; set; }
```

Gets or sets a value indicating whether AWS X-Ray-compatible trace identifiers should be used.

#### Methods

<a id="member-m-cephalon-observability-aws-configuration-awstelemetryexportoptions-fromconfiguration-microsoft-extensions-configuration-iconfiguration-system-string"></a>

##### `FromConfiguration`

```csharp
AwsTelemetryExportOptions FromConfiguration(IConfiguration configuration, string sectionPath)
```

Binds AWS telemetry export options from configuration.

Returns: The bound AWS telemetry export options.

Parameters:
- `configuration`: The application configuration root.
- `sectionPath`: The configuration section path that contains the engine settings. The default is `Engine`.

<a id="namespace-cephalon-observability-aws-hosting"></a>

## Namespace Cephalon.Observability.Aws.Hosting

<a id="type-cephalon-observability-aws-hosting-awshostapplicationbuilderextensions"></a>

### `AwsHostApplicationBuilderExtensions`

Adds AWS-hosted observability defaults and OTLP exporter wiring for Cephalon hosts.

#### Declaration
```csharp
public static class AwsHostApplicationBuilderExtensions
```

#### Methods

<a id="member-m-cephalon-observability-aws-hosting-awshostapplicationbuilderextensions-addcephalonaws-1-0-system-action-cephalon-observability-aws-configuration-awstelemetryexportoptions"></a>

##### `AddCephalonAws`

```csharp
TBuilder AddCephalonAws<TBuilder>(this TBuilder builder, Action<AwsTelemetryExportOptions> configure)
```

Adds AWS-aware OpenTelemetry registration for the Cephalon engine diagnostics surface.

Remarks: This package keeps AWS-specific propagation, resource detection, and AWS SDK instrumentation outside `Cephalon.Engine` and the baseline observability package. It still uses the shared `Engine:Observability:Telemetry` contract and the same OTLP exporter path as the cloud-neutral OpenTelemetry package.

Registration is skipped when every signal is disabled or when no export endpoint is configured and explicit self-hosted defaults are not enabled. When `HostedPlatform` is supplied, the package adds AWS-specific resource detectors and hosted-platform defaults on top of the existing service-name, service-version, and optional `deployment.environment.name` defaults.

Returns: The same builder instance for fluent host composition.

Type parameters:
- `TBuilder`: The host-application builder type to extend.

Parameters:
- `builder`: The target host-application builder.
- `configure`: An optional callback that can extend or override the configuration-driven AWS telemetry export options.
