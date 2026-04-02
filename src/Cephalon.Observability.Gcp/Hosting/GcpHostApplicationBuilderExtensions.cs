using System.Reflection;
using Cephalon.Engine.Diagnostics;
using Cephalon.Observability.Configuration;
using Cephalon.Observability.Gcp.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Cephalon.Observability.Gcp.Hosting;

/// <summary>
/// Adds GCP-hosted observability defaults and OTLP exporter wiring for Cephalon hosts.
/// </summary>
public static class GcpHostApplicationBuilderExtensions
{
    private static readonly Uri GoogleManagedIngestionEndpoint = new("https://telemetry.googleapis.com", UriKind.Absolute);

    /// <summary>
    /// Adds GCP-aware OpenTelemetry registration for the Cephalon engine diagnostics surface.
    /// </summary>
    /// <typeparam name="TBuilder">The host-application builder type to extend.</typeparam>
    /// <param name="builder">The target host-application builder.</param>
    /// <param name="configure">
    /// An optional callback that can extend or override the configuration-driven GCP telemetry export options.
    /// </param>
    /// <returns>The same builder instance for fluent host composition.</returns>
    /// <remarks>
    /// <para>
    /// This package keeps GCP-specific resource defaults and Google-managed ingestion concerns outside
    /// <c>Cephalon.Engine</c> and the baseline observability package. It still uses the shared
    /// <c>Engine:Observability:Telemetry</c> contract so hosts can keep one explicit telemetry surface.
    /// </para>
    /// <para>
    /// When <c>Endpoint</c> or <c>UseSelfHostedDefaults</c> is configured, the package keeps using the shared
    /// collector-oriented OTLP path and layers GCP resource defaults on top. When those shared endpoint settings
    /// are absent and <c>UseGoogleManagedIngestion</c> is enabled, the package targets
    /// <c>https://telemetry.googleapis.com</c> for traces and metrics over OTLP/HTTP by using Application
    /// Default Credentials, while logs stay on the shared collector or platform logging path.
    /// </para>
    /// </remarks>
    public static TBuilder AddCephalonGcp<TBuilder>(
        this TBuilder builder,
        Action<GcpTelemetryExportOptions>? configure = null)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        var telemetry = ObservabilityOptions.FromConfiguration(builder.Configuration).Telemetry;
        var options = GcpTelemetryExportOptions.FromConfiguration(builder.Configuration);
        configure?.Invoke(options);

        var endpointMode = ResolveEndpointMode(telemetry, options);
        if (!ShouldRegister(telemetry, endpointMode))
        {
            return builder;
        }

        EnsureProviderIsSupported(telemetry.Provider);

        var directManagedIngestion = endpointMode == GcpEndpointMode.GoogleManagedIngestion;
        var exporterProtocol = ResolveExporterProtocol(telemetry.Protocol, directManagedIngestion);
        var serviceName = ResolveServiceName(builder.Environment.ApplicationName);
        var serviceVersion = ResolveServiceVersion();
        var platformAttributes = ResolveHostedPlatformAttributes(options.HostedPlatform, options.Location, builder.Environment.EnvironmentName);

        builder.Services.AddSingleton(telemetry);
        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton<IDiagnosticsConventionContributor, GcpDiagnosticsConventionContributor>();
        builder.Services.AddHostedService<GcpSummaryHostedService>();

        var openTelemetry = builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                resource.AddService(serviceName, serviceVersion: serviceVersion);
                ConfigureGcpResource(resource, platformAttributes, telemetry.UseSelfHostedDefaults, builder.Environment.EnvironmentName);
            });

        if (telemetry.ExportTraces)
        {
            openTelemetry.WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation();
                tracing.AddSource(EngineDiagnostics.ActivitySourceName);
                tracing.AddOtlpExporter(exporter =>
                    ConfigureExporter(exporter, telemetry, options, exporterProtocol, endpointMode, TelemetrySignal.Traces));
            });
        }

        if (telemetry.ExportMetrics)
        {
            openTelemetry.WithMetrics(metrics =>
            {
                metrics.AddMeter(EngineDiagnostics.MeterName);
                metrics.AddOtlpExporter(exporter =>
                    ConfigureExporter(exporter, telemetry, options, exporterProtocol, endpointMode, TelemetrySignal.Metrics));
            });
        }

        if (ShouldExportLogs(telemetry, endpointMode))
        {
            builder.Logging.AddOpenTelemetry(logging =>
            {
                logging.IncludeFormattedMessage = true;
                logging.IncludeScopes = true;
                logging.ParseStateValues = true;
                logging.SetResourceBuilder(BuildResourceBuilder(
                    serviceName,
                    serviceVersion,
                    platformAttributes,
                    telemetry.UseSelfHostedDefaults,
                    builder.Environment.EnvironmentName));
                logging.AddOtlpExporter(exporter =>
                    ConfigureExporter(exporter, telemetry, options, exporterProtocol, endpointMode, TelemetrySignal.Logs));
            });
        }

        return builder;
    }

    internal static string? NormalizeHostedPlatform(string hostedPlatform)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hostedPlatform);

        return hostedPlatform.Trim().ToLowerInvariant() switch
        {
            "gce" => "gcp_compute_engine",
            "computeengine" => "gcp_compute_engine",
            "compute-engine" => "gcp_compute_engine",
            "gcp_compute_engine" => "gcp_compute_engine",
            "gke" => "gcp_kubernetes_engine",
            "kubernetesengine" => "gcp_kubernetes_engine",
            "kubernetes-engine" => "gcp_kubernetes_engine",
            "gcp_kubernetes_engine" => "gcp_kubernetes_engine",
            "cloudrun" => "gcp_cloud_run",
            "cloud-run" => "gcp_cloud_run",
            "gcp_cloud_run" => "gcp_cloud_run",
            "appengine" => "gcp_app_engine",
            "app-engine" => "gcp_app_engine",
            "gcp_app_engine" => "gcp_app_engine",
            "functions" => "gcp_cloud_functions",
            "cloudfunctions" => "gcp_cloud_functions",
            "cloud-functions" => "gcp_cloud_functions",
            "gcp_cloud_functions" => "gcp_cloud_functions",
            _ => null
        };
    }

    private static bool ShouldRegister(TelemetryExportOptions telemetry, GcpEndpointMode endpointMode)
    {
        ArgumentNullException.ThrowIfNull(telemetry);

        return endpointMode switch
        {
            GcpEndpointMode.None => false,
            GcpEndpointMode.GoogleManagedIngestion => telemetry.ExportMetrics || telemetry.ExportTraces,
            _ => telemetry.ExportLogs || telemetry.ExportMetrics || telemetry.ExportTraces
        };
    }

    private static bool ShouldExportLogs(TelemetryExportOptions telemetry, GcpEndpointMode endpointMode)
    {
        ArgumentNullException.ThrowIfNull(telemetry);

        return telemetry.ExportLogs && endpointMode != GcpEndpointMode.GoogleManagedIngestion;
    }

    private static GcpEndpointMode ResolveEndpointMode(
        TelemetryExportOptions telemetry,
        GcpTelemetryExportOptions options)
    {
        ArgumentNullException.ThrowIfNull(telemetry);
        ArgumentNullException.ThrowIfNull(options);

        if (!string.IsNullOrWhiteSpace(telemetry.Endpoint))
        {
            return GcpEndpointMode.ConfiguredEndpoint;
        }

        if (telemetry.UseSelfHostedDefaults)
        {
            return GcpEndpointMode.SelfHostedDefaults;
        }

        return options.UseGoogleManagedIngestion
            ? GcpEndpointMode.GoogleManagedIngestion
            : GcpEndpointMode.None;
    }

    private static void EnsureProviderIsSupported(string provider)
    {
        if (string.Equals(provider, "OpenTelemetry", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        throw new InvalidOperationException(
            $"Cephalon GCP observability integration only supports telemetry provider 'OpenTelemetry'. Configured provider '{provider}' is not supported.");
    }

    private static string ResolveServiceName(string? applicationName)
    {
        return string.IsNullOrWhiteSpace(applicationName)
            ? "Cephalon.Host"
            : applicationName.Trim();
    }

    private static string? ResolveServiceVersion()
    {
        var entryAssembly = Assembly.GetEntryAssembly();
        return entryAssembly?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion
            ?? entryAssembly?.GetName().Version?.ToString();
    }

    private static OtlpExportProtocol ResolveExporterProtocol(string protocol, bool directManagedIngestion)
    {
        if (directManagedIngestion)
        {
            return NormalizeHttpProtocol(protocol)
                ? OtlpExportProtocol.HttpProtobuf
                : throw new InvalidOperationException(
                    $"Cephalon GCP direct managed ingestion requires telemetry protocol 'otlp/http'. Configured protocol '{protocol}' is not supported.");
        }

        if (string.IsNullOrWhiteSpace(protocol))
        {
            return OtlpExportProtocol.Grpc;
        }

        return protocol.Trim().ToLowerInvariant() switch
        {
            "otlp" => OtlpExportProtocol.Grpc,
            "grpc" => OtlpExportProtocol.Grpc,
            "otlp/grpc" => OtlpExportProtocol.Grpc,
            "http" => OtlpExportProtocol.HttpProtobuf,
            "otlp/http" => OtlpExportProtocol.HttpProtobuf,
            "otlp-http" => OtlpExportProtocol.HttpProtobuf,
            "http/protobuf" => OtlpExportProtocol.HttpProtobuf,
            "httpprotobuf" => OtlpExportProtocol.HttpProtobuf,
            _ => throw new InvalidOperationException(
                $"Telemetry protocol '{protocol}' is not supported by Cephalon GCP observability integration. Use 'otlp', 'otlp/grpc', or 'otlp/http'.")
        };
    }

    private static bool NormalizeHttpProtocol(string protocol)
    {
        if (string.IsNullOrWhiteSpace(protocol))
        {
            return false;
        }

        return protocol.Trim().ToLowerInvariant() is "http" or "otlp/http" or "otlp-http" or "http/protobuf" or "httpprotobuf";
    }

    private static void ConfigureExporter(
        OtlpExporterOptions exporter,
        TelemetryExportOptions telemetry,
        GcpTelemetryExportOptions options,
        OtlpExportProtocol exporterProtocol,
        GcpEndpointMode endpointMode,
        TelemetrySignal signal)
    {
        ArgumentNullException.ThrowIfNull(exporter);
        ArgumentNullException.ThrowIfNull(telemetry);
        ArgumentNullException.ThrowIfNull(options);

        exporter.Protocol = exporterProtocol;

        if (endpointMode == GcpEndpointMode.GoogleManagedIngestion)
        {
            if (!options.UseApplicationDefaultCredentials)
            {
                throw new InvalidOperationException(
                    "Cephalon GCP direct managed ingestion requires Application Default Credentials. Set Engine:Observability:Telemetry:Gcp:UseApplicationDefaultCredentials to 'true' or configure a shared OTLP endpoint instead.");
            }

            exporter.HttpClientFactory = () => new HttpClient(new GcpManagedIngestionAuthHandler(options.QuotaProjectId), disposeHandler: true);
            exporter.Endpoint = BuildSignalEndpoint(GoogleManagedIngestionEndpoint, signal);
            return;
        }

        var endpoint = ResolveCollectorEndpoint(telemetry, exporterProtocol);
        if (endpoint is null)
        {
            return;
        }

        exporter.Endpoint = exporterProtocol == OtlpExportProtocol.HttpProtobuf
            ? BuildSignalEndpoint(endpoint, signal)
            : endpoint;
    }

    private static ResourceBuilder BuildResourceBuilder(
        string serviceName,
        string? serviceVersion,
        IReadOnlyList<KeyValuePair<string, object>> platformAttributes,
        bool useSelfHostedDefaults,
        string? environmentName)
    {
        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(serviceName, serviceVersion: serviceVersion);
        ConfigureGcpResource(resourceBuilder, platformAttributes, useSelfHostedDefaults, environmentName);
        return resourceBuilder;
    }

    private static void ConfigureGcpResource(
        ResourceBuilder resourceBuilder,
        IReadOnlyList<KeyValuePair<string, object>> platformAttributes,
        bool useSelfHostedDefaults,
        string? environmentName)
    {
        ArgumentNullException.ThrowIfNull(resourceBuilder);
        ArgumentNullException.ThrowIfNull(platformAttributes);

        if (platformAttributes.Count > 0)
        {
            resourceBuilder.AddAttributes(platformAttributes);
        }

        if (useSelfHostedDefaults && !string.IsNullOrWhiteSpace(environmentName))
        {
            resourceBuilder.AddAttributes(
            [
                new KeyValuePair<string, object>(
                    "deployment.environment.name",
                    environmentName.Trim())
            ]);
        }
    }

    private static List<KeyValuePair<string, object>> ResolveHostedPlatformAttributes(
        string? hostedPlatform,
        string? configuredLocation,
        string? environmentName)
    {
        var attributes = new List<KeyValuePair<string, object>>();

        if (!string.IsNullOrWhiteSpace(hostedPlatform))
        {
            var normalizedPlatform = NormalizeHostedPlatform(hostedPlatform);
            if (normalizedPlatform is null)
            {
                throw new InvalidOperationException(
                    $"GCP hosted platform '{hostedPlatform}' is not supported. Use 'gce', 'gke', 'cloudrun', 'appengine', or 'functions'.");
            }

            attributes.Add(new KeyValuePair<string, object>("cloud.provider", "gcp"));
            attributes.Add(new KeyValuePair<string, object>("cloud.platform", normalizedPlatform));
        }

        var resolvedLocation = ResolveLocation(configuredLocation);
        if (!string.IsNullOrWhiteSpace(resolvedLocation))
        {
            attributes.Add(new KeyValuePair<string, object>("location", resolvedLocation));

            if (LooksLikeZone(resolvedLocation, out var region))
            {
                attributes.Add(new KeyValuePair<string, object>("cloud.availability_zone", resolvedLocation));
                attributes.Add(new KeyValuePair<string, object>("cloud.region", region));
            }
            else
            {
                attributes.Add(new KeyValuePair<string, object>("cloud.region", resolvedLocation));
            }
        }

        if (!string.IsNullOrWhiteSpace(environmentName))
        {
            attributes.Add(new KeyValuePair<string, object>(
                "deployment.environment.name",
                environmentName.Trim()));
        }

        return attributes;
    }

    private static string? ResolveLocation(string? configuredLocation)
    {
        if (!string.IsNullOrWhiteSpace(configuredLocation))
        {
            return configuredLocation.Trim();
        }

        var candidate = Environment.GetEnvironmentVariable("GOOGLE_CLOUD_LOCATION") ??
            Environment.GetEnvironmentVariable("GOOGLE_CLOUD_REGION") ??
            Environment.GetEnvironmentVariable("FUNCTION_REGION") ??
            Environment.GetEnvironmentVariable("K_REGION");

        return string.IsNullOrWhiteSpace(candidate)
            ? null
            : candidate.Trim();
    }

    private static bool LooksLikeZone(string location, out string region)
    {
        region = string.Empty;

        var trimmedLocation = location.Trim();
        var lastSeparator = trimmedLocation.LastIndexOf('-');
        if (lastSeparator < 0 || lastSeparator == trimmedLocation.Length - 1)
        {
            return false;
        }

        var zoneSuffix = trimmedLocation[(lastSeparator + 1)..];
        if (zoneSuffix.Length == 1 && char.IsLetter(zoneSuffix[0]))
        {
            region = trimmedLocation[..lastSeparator];
            return true;
        }

        return false;
    }

    private static Uri? ResolveCollectorEndpoint(
        TelemetryExportOptions telemetry,
        OtlpExportProtocol exporterProtocol)
    {
        ArgumentNullException.ThrowIfNull(telemetry);

        if (!string.IsNullOrWhiteSpace(telemetry.Endpoint))
        {
            if (!Uri.TryCreate(telemetry.Endpoint, UriKind.Absolute, out var endpoint))
            {
                throw new InvalidOperationException(
                    $"Telemetry endpoint '{telemetry.Endpoint}' is not a valid absolute URI.");
            }

            return endpoint;
        }

        if (!telemetry.UseSelfHostedDefaults)
        {
            return null;
        }

        return exporterProtocol switch
        {
            OtlpExportProtocol.Grpc => new Uri("http://localhost:4317", UriKind.Absolute),
            OtlpExportProtocol.HttpProtobuf => new Uri("http://localhost:4318", UriKind.Absolute),
            _ => throw new ArgumentOutOfRangeException(nameof(exporterProtocol))
        };
    }

    private static Uri BuildSignalEndpoint(Uri endpoint, TelemetrySignal signal)
    {
        var signalPath = signal switch
        {
            TelemetrySignal.Logs => "/v1/logs",
            TelemetrySignal.Metrics => "/v1/metrics",
            TelemetrySignal.Traces => "/v1/traces",
            _ => throw new ArgumentOutOfRangeException(nameof(signal))
        };

        var builder = new UriBuilder(endpoint);
        var trimmedPath = builder.Path.TrimEnd('/');
        if (string.IsNullOrEmpty(trimmedPath))
        {
            builder.Path = signalPath;
            return builder.Uri;
        }

        if (IsSignalPath(trimmedPath))
        {
            builder.Path = signalPath;
            return builder.Uri;
        }

        builder.Path = $"{trimmedPath}{signalPath}";
        return builder.Uri;
    }

    private static bool IsSignalPath(string path)
    {
        return string.Equals(path, "/v1/logs", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(path, "/v1/metrics", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(path, "/v1/traces", StringComparison.OrdinalIgnoreCase);
    }

    private enum TelemetrySignal
    {
        Logs,
        Metrics,
        Traces
    }

    private enum GcpEndpointMode
    {
        None,
        ConfiguredEndpoint,
        SelfHostedDefaults,
        GoogleManagedIngestion
    }
}
