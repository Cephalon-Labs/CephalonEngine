using System.Reflection;
using Cephalon.Engine.Diagnostics;
using Cephalon.Observability.Configuration;
using Cephalon.Observability.NewRelic.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Cephalon.Observability.NewRelic.Hosting;

/// <summary>
/// Adds New Relic native OTLP endpoint wiring and <c>api-key</c> authentication guidance for Cephalon hosts.
/// </summary>
public static class NewRelicHostApplicationBuilderExtensions
{
    /// <summary>
    /// Adds New Relic-aware OpenTelemetry registration for the Cephalon engine diagnostics surface.
    /// </summary>
    /// <typeparam name="TBuilder">The host-application builder type to extend.</typeparam>
    /// <param name="builder">The target host-application builder.</param>
    /// <param name="configure">
    /// An optional callback that can extend or override the configuration-driven New Relic telemetry export options.
    /// </param>
    /// <returns>The same builder instance for fluent host composition.</returns>
    /// <remarks>
    /// <para>
    /// This package keeps New Relic-specific OTLP endpoint and <c>api-key</c> authentication concerns outside
    /// <c>Cephalon.Engine</c> and the baseline observability package. It still uses the shared
    /// <c>Engine:Observability:Telemetry</c> contract so hosts can keep one explicit telemetry surface.
    /// </para>
    /// <para>
    /// When <c>Endpoint</c> or <c>UseSelfHostedDefaults</c> is configured on the shared telemetry contract,
    /// the package keeps using that collector-oriented OTLP path and only layers New Relic-friendly resource
    /// attributes on top. When those shared endpoint settings are absent and the New Relic options opt into
    /// direct endpoint usage, the package targets either the configured New Relic OTLP endpoint or the documented
    /// regional default and applies either the raw OTLP headers string or the required <c>api-key</c> header built
    /// from the configured license key.
    /// </para>
    /// </remarks>
    public static TBuilder AddCephalonNewRelic<TBuilder>(
        this TBuilder builder,
        Action<NewRelicTelemetryExportOptions>? configure = null)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        var telemetry = ObservabilityOptions.FromConfiguration(builder.Configuration).Telemetry;
        var options = NewRelicTelemetryExportOptions.FromConfiguration(builder.Configuration);
        configure?.Invoke(options);

        var endpointMode = ResolveEndpointMode(telemetry, options);
        if (!ShouldRegister(telemetry, endpointMode))
        {
            return builder;
        }

        EnsureProviderIsSupported(telemetry.Provider);

        var serviceName = ResolveServiceName(builder.Environment.ApplicationName);
        var serviceVersion = ResolveServiceVersion();
        var exporterProtocol = ResolveExporterProtocol(telemetry.Protocol);
        var resourceAttributes = ResolveResourceAttributes(options.ServiceNamespace, builder.Environment.EnvironmentName);

        builder.Services.AddSingleton(telemetry);
        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton<IDiagnosticsConventionContributor, NewRelicDiagnosticsConventionContributor>();
        builder.Services.AddHostedService<NewRelicSummaryHostedService>();

        var openTelemetry = builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                resource.AddService(serviceName, serviceVersion: serviceVersion);
                ConfigureNewRelicResource(resource, resourceAttributes);
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

        if (telemetry.ExportLogs)
        {
            builder.Logging.AddOpenTelemetry(logging =>
            {
                logging.IncludeFormattedMessage = true;
                logging.IncludeScopes = true;
                logging.ParseStateValues = true;
                logging.SetResourceBuilder(BuildResourceBuilder(serviceName, serviceVersion, resourceAttributes));
                logging.AddOtlpExporter(exporter =>
                    ConfigureExporter(exporter, telemetry, options, exporterProtocol, endpointMode, TelemetrySignal.Logs));
            });
        }

        return builder;
    }

    internal static bool HasDirectEndpointConfiguration(NewRelicTelemetryExportOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return options.UseNativeOtlpEndpoint ||
            !string.IsNullOrWhiteSpace(options.Endpoint) ||
            !string.IsNullOrWhiteSpace(options.Headers) ||
            !string.IsNullOrWhiteSpace(options.LicenseKey) ||
            !string.IsNullOrWhiteSpace(options.Region);
    }

    internal static string? ResolveSelfHostedCollectorEndpoint(string? protocol)
    {
        var normalizedProtocol = string.IsNullOrWhiteSpace(protocol)
            ? "otlp"
            : protocol.Trim().ToLowerInvariant();

        return normalizedProtocol switch
        {
            "otlp" => "http://localhost:4317",
            "grpc" => "http://localhost:4317",
            "otlp/grpc" => "http://localhost:4317",
            "http" => "http://localhost:4318",
            "otlp/http" => "http://localhost:4318",
            "otlp-http" => "http://localhost:4318",
            "http/protobuf" => "http://localhost:4318",
            "httpprotobuf" => "http://localhost:4318",
            _ => null
        };
    }

    internal static string BuildApiKeyHeader(string licenseKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(licenseKey);

        return $"api-key={licenseKey.Trim()}";
    }

    internal static string NormalizeRegion(string? region)
    {
        if (string.IsNullOrWhiteSpace(region))
        {
            return "us";
        }

        return region.Trim().ToLowerInvariant() switch
        {
            "us" => "us",
            "us1" => "us",
            "eu" => "eu",
            "eu01" => "eu",
            "fedramp" => "fedramp",
            "us-fedramp" => "fedramp",
            "us_fedramp" => "fedramp",
            "gov" => "fedramp",
            "govcloud" => "fedramp",
            "us-gov" => "fedramp",
            "us_gov" => "fedramp",
            _ => throw new InvalidOperationException(
                $"New Relic region '{region}' is not supported. Use 'us', 'eu', or 'fedramp'.")
        };
    }

    internal static Uri ResolveRegionalEndpoint(string? region)
    {
        return NormalizeRegion(region) switch
        {
            "us" => new Uri("https://otlp.nr-data.net", UriKind.Absolute),
            "eu" => new Uri("https://otlp.eu01.nr-data.net", UriKind.Absolute),
            "fedramp" => new Uri("https://gov-otlp.nr-data.net", UriKind.Absolute),
            _ => throw new InvalidOperationException("Unsupported New Relic region.")
        };
    }

    private static bool ShouldRegister(TelemetryExportOptions telemetry, NewRelicEndpointMode endpointMode)
    {
        ArgumentNullException.ThrowIfNull(telemetry);

        return endpointMode != NewRelicEndpointMode.None &&
            (telemetry.ExportLogs || telemetry.ExportMetrics || telemetry.ExportTraces);
    }

    private static NewRelicEndpointMode ResolveEndpointMode(
        TelemetryExportOptions telemetry,
        NewRelicTelemetryExportOptions options)
    {
        ArgumentNullException.ThrowIfNull(telemetry);
        ArgumentNullException.ThrowIfNull(options);

        if (!string.IsNullOrWhiteSpace(telemetry.Endpoint))
        {
            return NewRelicEndpointMode.ConfiguredEndpoint;
        }

        if (telemetry.UseSelfHostedDefaults)
        {
            return NewRelicEndpointMode.SelfHostedDefaults;
        }

        return HasDirectEndpointConfiguration(options)
            ? NewRelicEndpointMode.NewRelicDirect
            : NewRelicEndpointMode.None;
    }

    private static void EnsureProviderIsSupported(string provider)
    {
        if (string.Equals(provider, "OpenTelemetry", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        throw new InvalidOperationException(
            $"Cephalon New Relic observability integration only supports telemetry provider 'OpenTelemetry'. Configured provider '{provider}' is not supported.");
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

    private static OtlpExportProtocol ResolveExporterProtocol(string protocol)
    {
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
                $"Telemetry protocol '{protocol}' is not supported by Cephalon New Relic observability integration. Use 'otlp', 'otlp/grpc', or 'otlp/http'.")
        };
    }

    private static void ConfigureExporter(
        OtlpExporterOptions exporter,
        TelemetryExportOptions telemetry,
        NewRelicTelemetryExportOptions options,
        OtlpExportProtocol exporterProtocol,
        NewRelicEndpointMode endpointMode,
        TelemetrySignal signal)
    {
        ArgumentNullException.ThrowIfNull(exporter);
        ArgumentNullException.ThrowIfNull(telemetry);
        ArgumentNullException.ThrowIfNull(options);

        exporter.Protocol = exporterProtocol;

        if (endpointMode == NewRelicEndpointMode.NewRelicDirect)
        {
            exporter.Headers = ResolveDirectHeaders(options);
        }

        var endpoint = ResolveEndpoint(telemetry, options, exporterProtocol, endpointMode);
        if (endpoint is null)
        {
            return;
        }

        exporter.Endpoint = exporterProtocol == OtlpExportProtocol.HttpProtobuf
            ? BuildSignalEndpoint(endpoint, signal)
            : endpoint;
    }

    private static string ResolveDirectHeaders(NewRelicTelemetryExportOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!string.IsNullOrWhiteSpace(options.Headers))
        {
            return options.Headers.Trim();
        }

        if (!string.IsNullOrWhiteSpace(options.LicenseKey))
        {
            return BuildApiKeyHeader(options.LicenseKey);
        }

        throw new InvalidOperationException(
            "Cephalon New Relic direct OTLP endpoint mode requires Engine:Observability:Telemetry:NewRelic:Headers or Engine:Observability:Telemetry:NewRelic:LicenseKey to be configured.");
    }

    private static Uri? ResolveEndpoint(
        TelemetryExportOptions telemetry,
        NewRelicTelemetryExportOptions options,
        OtlpExportProtocol exporterProtocol,
        NewRelicEndpointMode endpointMode)
    {
        ArgumentNullException.ThrowIfNull(telemetry);
        ArgumentNullException.ThrowIfNull(options);

        return endpointMode switch
        {
            NewRelicEndpointMode.ConfiguredEndpoint => ResolveAbsoluteEndpoint(
                telemetry.Endpoint,
                "Telemetry endpoint"),
            NewRelicEndpointMode.SelfHostedDefaults => ResolveSelfHostedEndpoint(exporterProtocol),
            NewRelicEndpointMode.NewRelicDirect => string.IsNullOrWhiteSpace(options.Endpoint)
                ? ResolveRegionalEndpoint(options.Region)
                : ResolveAbsoluteEndpoint(options.Endpoint, "New Relic OTLP endpoint"),
            _ => null
        };
    }

    private static Uri ResolveSelfHostedEndpoint(OtlpExportProtocol exporterProtocol)
    {
        return exporterProtocol switch
        {
            OtlpExportProtocol.Grpc => new Uri("http://localhost:4317", UriKind.Absolute),
            OtlpExportProtocol.HttpProtobuf => new Uri("http://localhost:4318", UriKind.Absolute),
            _ => throw new ArgumentOutOfRangeException(nameof(exporterProtocol))
        };
    }

    private static Uri ResolveAbsoluteEndpoint(string? value, string description)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Cephalon New Relic observability integration requires {description.ToLowerInvariant()} to be configured.");
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var endpoint))
        {
            throw new InvalidOperationException(
                $"{description} '{value}' is not a valid absolute URI.");
        }

        return endpoint;
    }

    private static ResourceBuilder BuildResourceBuilder(
        string serviceName,
        string? serviceVersion,
        IReadOnlyList<KeyValuePair<string, object>> resourceAttributes)
    {
        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(serviceName, serviceVersion: serviceVersion);
        ConfigureNewRelicResource(resourceBuilder, resourceAttributes);
        return resourceBuilder;
    }

    private static void ConfigureNewRelicResource(
        ResourceBuilder resourceBuilder,
        IReadOnlyList<KeyValuePair<string, object>> resourceAttributes)
    {
        ArgumentNullException.ThrowIfNull(resourceBuilder);
        ArgumentNullException.ThrowIfNull(resourceAttributes);

        if (resourceAttributes.Count > 0)
        {
            resourceBuilder.AddAttributes(resourceAttributes);
        }
    }

    private static List<KeyValuePair<string, object>> ResolveResourceAttributes(
        string? serviceNamespace,
        string? environmentName)
    {
        var attributes = new List<KeyValuePair<string, object>>();

        if (!string.IsNullOrWhiteSpace(serviceNamespace))
        {
            attributes.Add(new KeyValuePair<string, object>("service.namespace", serviceNamespace.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(environmentName))
        {
            attributes.Add(new KeyValuePair<string, object>(
                "deployment.environment.name",
                environmentName.Trim()));
        }

        return attributes;
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

    private enum NewRelicEndpointMode
    {
        None,
        ConfiguredEndpoint,
        SelfHostedDefaults,
        NewRelicDirect
    }

    private enum TelemetrySignal
    {
        Logs,
        Metrics,
        Traces
    }
}
