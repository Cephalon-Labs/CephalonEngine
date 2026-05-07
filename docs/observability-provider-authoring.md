# Observability Provider Authoring

This guide explains how to build a downstream observability companion package on top of Cephalon's shared telemetry contract without modifying `Cephalon.Engine`, `Cephalon.Abstractions`, or the shared `ILogger` pipeline.

Use this path when your deployment target needs provider-specific auth, hosted defaults, resource attributes, proxy handoff, or exporter behavior that Cephalon does not already ship as a first-party package.

## When to author a downstream package

Author a downstream companion package when:

- your deployment target has a documented host-side observability story that is specific enough to encode as reusable package behavior
- you need provider-specific config under `Engine:Observability:Telemetry:{ProviderName}`
- you need startup diagnostics, event-id conventions, or hosted defaults that should stay outside the engine core
- you want to reuse Cephalon's existing logs, metrics, traces, and request-correlation conventions instead of inventing another logging or telemetry abstraction

Do not author a downstream package when:

- the target platform only documents service-native or runtime-native telemetry for a different execution model than a Cephalon host
- the provider docs are still too ambiguous to support a truthful reusable package contract
- a standard OTLP collector, gateway, or one of Cephalon's shipped first-party companion packages already covers the deployment cleanly

## Package shape

The recommended downstream package layout mirrors the first-party observability companions:

```text
src/YourCompany.Cephalon.Observability.Cloudflare/
  Configuration/
    CloudflareTelemetryExportOptions.cs
  Hosting/
    CloudflareHostApplicationBuilderExtensions.cs
    CloudflareDiagnosticsConventionContributor.cs
    CloudflareSummaryHostedService.cs
    CloudflareTelemetryRuntimeContributor.cs
```

Recommended responsibilities:

- `Configuration/*Options.cs`
  - bind provider-specific settings from `Engine:Observability:Telemetry:{ProviderName}`
  - normalize optional strings, booleans, and ports
  - keep defaults explicit instead of relying on hidden ambient behavior
- `Hosting/*HostApplicationBuilderExtensions.cs`
  - read `ObservabilityOptions.FromConfiguration(builder.Configuration).Telemetry`
  - read provider-specific options
  - skip registration when the provider-specific path is not configured
  - reject unsupported signal combinations early
  - keep provider-specific wiring outside `Cephalon.Engine`
- `Hosting/*DiagnosticsConventionContributor.cs`
  - publish package-specific event ids through `IDiagnosticsConventionContributor`
- `Hosting/*SummaryHostedService.cs`
  - emit a one-time startup summary that describes the active provider mode without logging secrets
- `Hosting/*TelemetryRuntimeContributor.cs`
  - publish sanitized active-pack runtime truth through `ITechnologyRuntimeContributor`, using `TelemetryExportRuntimeSurfaceFactory` when the shared telemetry contract is enough
  - never project raw endpoints, headers, tokens, connection strings, sink secrets, or credential material

## Shared contract pattern

Downstream packages should treat `Engine:Observability:Telemetry` as the shared contract and add a provider-specific sub-section beneath it.

Example configuration:

```json
{
  "Engine": {
    "Observability": {
      "Telemetry": {
        "Provider": "OpenTelemetry",
        "Protocol": "otlp/http",
        "Endpoint": "http://localhost:4318",
        "ExportLogs": true,
        "ExportMetrics": true,
        "ExportTraces": true,
        "Cloudflare": {
          "AccountId": "edge-prod",
          "Mode": "worker-export-guidance",
          "Headers": "Authorization=Bearer sample-token"
        }
      }
    }
  }
}
```

Recommended extension-method pattern:

```csharp
public static class CloudflareHostApplicationBuilderExtensions
{
    public static TBuilder AddCompanyCloudflareObservability<TBuilder>(
        this TBuilder builder,
        Action<CloudflareTelemetryExportOptions>? configure = null)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        var telemetry = ObservabilityOptions.FromConfiguration(builder.Configuration).Telemetry;
        var options = CloudflareTelemetryExportOptions.FromConfiguration(builder.Configuration);
        configure?.Invoke(options);

        if (!options.IsConfigured)
        {
            return builder;
        }

        if (!string.Equals(telemetry.Provider, "OpenTelemetry", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Cloudflare companion wiring requires Engine:Observability:Telemetry:Provider to stay set to 'OpenTelemetry'.");
        }

        builder.Services.AddSingleton(telemetry);
        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton<IDiagnosticsConventionContributor, CloudflareDiagnosticsConventionContributor>();
        builder.Services.AddHostedService<CloudflareSummaryHostedService>();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, CloudflareTelemetryRuntimeContributor>());

        // Add provider-specific OTLP/export/auth/resource wiring here.

        return builder;
    }
}
```

## Design rules

- keep `TelemetryExportOptions` as the shared source of truth for provider, protocol, endpoint, and enabled signals
- keep the package additive; return the builder unchanged when the provider-specific path is not configured
- do not create a second Cephalon logging abstraction; continue to use the existing `ILogger` pipeline
- publish a startup summary so operators can see the effective provider mode, signal selection, and trust/auth mode without leaking secrets
- publish diagnostics conventions so `/engine/diagnostics` and `/engine/snapshot` can stay introspectable when the package is active
- publish a sanitized runtime surface so `/engine/technology-surfaces` and `/engine/snapshot` can show that the package is active without exposing deployment secrets
- reject unsupported protocol or signal combinations with explicit exceptions instead of silently degrading behavior
- keep hosted defaults, auth rules, trust bundles, and resource attributes inside the provider package instead of pushing them into `Cephalon.Engine`
- document whether the package supports logs, metrics, traces, or only a subset of signals

## Cloudflare-specific guidance

Cloudflare is the current motivating case for this guidance because the platform docs do not yet support a truthful first-party `Cephalon.Observability.Cloudflare` package contract for external .NET hosts.

What the current Cloudflare docs do support:

- Cloudflare Workers-native logs and traces
- exporting OpenTelemetry-compliant traces and logs from Workers to third-party OTLP destinations

What the current Cloudflare docs do not yet support as a Cephalon first-party package claim:

- a documented generic OTLP ingestion target for external Cephalon hosts
- metrics export in the current Workers OpenTelemetry export path
- a clean host-side promise that external trace context will propagate the same way it does inside the Worker-native model

That means the truthful Cephalon path today is:

1. keep first-party Cephalon host integrations focused on the shipped self-hosted, Azure Monitor, AWS, GCP, Huawei Cloud, Alibaba Cloud, OpenShift, DigitalOcean, and Tanzu packages
2. treat Cloudflare-specific work as downstream companion-package guidance unless Cloudflare later documents a host-side ingestion model that maps cleanly to a reusable .NET host package
3. prefer a standard OTLP collector or gateway in front of the final destination when that gives a clearer and more supportable contract

## Startup-summary checklist

When a downstream provider package is active, the startup summary should answer:

- which provider mode is active
- whether the package is using the shared endpoint, self-hosted defaults, a provider gateway, or some other explicit path
- which signals are enabled
- which hosted platform or runtime defaults were applied
- which trust or auth mode is active, without logging secret values

## Validation checklist

Before shipping a downstream provider package, verify that it:

- keeps the shared `Engine:Observability:Telemetry` contract intact
- documents every public option type and extension method with XML comments
- includes focused tests for skip-registration, supported-path registration, and unsupported-path validation
- documents any signal limitations or trust limitations explicitly
- updates hand-authored docs, operational guidance, and planning metadata together when the scope changes

## Related docs

- [Operations](operations.md)
- [Cephalon.Observability](components/observability.md)
- [Cephalon.Observability.OpenTelemetry](components/observability-opentelemetry.md)
- [Module authoring](module-authoring.md)
