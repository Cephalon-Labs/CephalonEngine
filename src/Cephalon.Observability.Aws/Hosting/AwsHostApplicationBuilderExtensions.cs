using System.Reflection;
using Cephalon.Abstractions.Technologies;
using Cephalon.Engine.Diagnostics;
using Cephalon.Observability.Aws.Configuration;
using Cephalon.Observability.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Extensions.AWS.Trace;
using OpenTelemetry.Instrumentation.AWSLambda;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Cephalon.Observability.Aws.Hosting;

/// <summary>
/// Adds AWS-hosted observability defaults and OTLP exporter wiring for Cephalon hosts.
/// </summary>
public static class AwsHostApplicationBuilderExtensions
{
    /// <summary>
    /// Adds AWS-aware OpenTelemetry registration for the Cephalon engine diagnostics surface.
    /// </summary>
    /// <typeparam name="TBuilder">The host-application builder type to extend.</typeparam>
    /// <param name="builder">The target host-application builder.</param>
    /// <param name="configure">
    /// An optional callback that can extend or override the configuration-driven AWS telemetry export options.
    /// </param>
    /// <returns>The same builder instance for fluent host composition.</returns>
    /// <remarks>
    /// <para>
    /// This package keeps AWS-specific propagation, resource detection, and AWS SDK instrumentation outside
    /// <c>Cephalon.Engine</c> and the baseline observability package. It still uses the shared
    /// <c>Engine:Observability:Telemetry</c> contract and the same OTLP exporter path as the cloud-neutral
    /// OpenTelemetry package.
    /// </para>
    /// <para>
    /// Registration is skipped when every signal is disabled or when no export endpoint is configured and
    /// explicit self-hosted defaults are not enabled. When <c>HostedPlatform</c> is supplied, the package
    /// adds AWS-specific resource detectors and hosted-platform defaults on top of the existing service-name,
    /// service-version, and optional <c>deployment.environment.name</c> defaults.
    /// </para>
    /// </remarks>
    public static TBuilder AddCephalonAws<TBuilder>(
        this TBuilder builder,
        Action<AwsTelemetryExportOptions>? configure = null)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        var telemetry = ObservabilityOptions.FromConfiguration(builder.Configuration).Telemetry;
        var options = AwsTelemetryExportOptions.FromConfiguration(builder.Configuration);
        configure?.Invoke(options);

        if (!ShouldRegister(telemetry))
        {
            return builder;
        }

        EnsureProviderIsSupported(telemetry.Provider);

        if (telemetry.ExportTraces && options.UseXRayPropagator)
        {
            Sdk.SetDefaultTextMapPropagator(new AWSXRayPropagator());
        }

        var serviceName = ResolveServiceName(builder.Environment.ApplicationName);
        var serviceVersion = ResolveServiceVersion();
        var exporterProtocol = ResolveExporterProtocol(telemetry.Protocol);
        var platformAttributes = ResolveHostedPlatformAttributes(options.HostedPlatform, builder.Environment.EnvironmentName);

        builder.Services.AddSingleton(telemetry);
        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton<IDiagnosticsConventionContributor, AwsDiagnosticsConventionContributor>();
        builder.Services.AddHostedService<AwsSummaryHostedService>();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ITechnologyRuntimeContributor, AwsTelemetryRuntimeContributor>());

        var openTelemetry = builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                resource.AddService(serviceName, serviceVersion: serviceVersion);
                ConfigureAwsResource(resource, options, platformAttributes, telemetry.UseSelfHostedDefaults, builder.Environment.EnvironmentName);
            });

        if (telemetry.ExportTraces)
        {
            openTelemetry.WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation();
                tracing.AddSource(EngineDiagnostics.ActivitySourceName);

                if (options.UseXRayTraceIds)
                {
                    tracing.AddXRayTraceId();
                }

                if (options.EnableAwsSdkInstrumentation)
                {
                    tracing.AddAWSInstrumentation();
                }

                if (string.Equals(NormalizeHostedPlatform(options.HostedPlatform ?? string.Empty), "aws_lambda", StringComparison.Ordinal))
                {
                    tracing.AddAWSLambdaConfigurations(lambda =>
                    {
                        lambda.DisableAwsXRayContextExtraction = !options.EnableLambdaContextExtraction;
                    });
                }

                tracing.AddOtlpExporter(exporter =>
                    ConfigureExporter(exporter, telemetry, exporterProtocol, TelemetrySignal.Traces));
            });
        }

        if (telemetry.ExportMetrics)
        {
            openTelemetry.WithMetrics(metrics =>
            {
                metrics.AddMeter(EngineDiagnostics.MeterName);
                metrics.AddOtlpExporter(exporter =>
                    ConfigureExporter(exporter, telemetry, exporterProtocol, TelemetrySignal.Metrics));
            });
        }

        if (telemetry.ExportLogs)
        {
            builder.Logging.AddOpenTelemetry(logging =>
            {
                logging.IncludeFormattedMessage = true;
                logging.IncludeScopes = true;
                logging.ParseStateValues = true;
                logging.SetResourceBuilder(BuildResourceBuilder(
                    serviceName,
                    serviceVersion,
                    options,
                    telemetry.UseSelfHostedDefaults,
                    builder.Environment.EnvironmentName,
                    platformAttributes));
                logging.AddOtlpExporter(exporter =>
                    ConfigureExporter(exporter, telemetry, exporterProtocol, TelemetrySignal.Logs));
            });
        }

        return builder;
    }

    internal static string? NormalizeHostedPlatform(string hostedPlatform)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hostedPlatform);

        return hostedPlatform.Trim().ToLowerInvariant() switch
        {
            "ec2" => "aws_ec2",
            "aws_ec2" => "aws_ec2",
            "ecs" => "aws_ecs",
            "aws_ecs" => "aws_ecs",
            "eks" => "aws_eks",
            "aws_eks" => "aws_eks",
            "elasticbeanstalk" => "aws_elastic_beanstalk",
            "elastic-beanstalk" => "aws_elastic_beanstalk",
            "aws_elastic_beanstalk" => "aws_elastic_beanstalk",
            "lambda" => "aws_lambda",
            "aws_lambda" => "aws_lambda",
            _ => null
        };
    }

    private static bool ShouldRegister(TelemetryExportOptions telemetry)
    {
        ArgumentNullException.ThrowIfNull(telemetry);

        return (!string.IsNullOrWhiteSpace(telemetry.Endpoint) || telemetry.UseSelfHostedDefaults) &&
            (telemetry.ExportLogs || telemetry.ExportMetrics || telemetry.ExportTraces);
    }

    private static void EnsureProviderIsSupported(string provider)
    {
        if (string.Equals(provider, "OpenTelemetry", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        throw new InvalidOperationException(
            $"Cephalon AWS observability integration only supports telemetry provider 'OpenTelemetry'. Configured provider '{provider}' is not supported.");
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
                $"Telemetry protocol '{protocol}' is not supported by Cephalon AWS observability integration. Use 'otlp', 'otlp/grpc', or 'otlp/http'.")
        };
    }

    private static void ConfigureExporter(
        OtlpExporterOptions exporter,
        TelemetryExportOptions telemetry,
        OtlpExportProtocol exporterProtocol,
        TelemetrySignal signal)
    {
        ArgumentNullException.ThrowIfNull(exporter);
        ArgumentNullException.ThrowIfNull(telemetry);

        exporter.Protocol = exporterProtocol;

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
        AwsTelemetryExportOptions options,
        bool useSelfHostedDefaults,
        string? environmentName,
        IReadOnlyList<KeyValuePair<string, object>> platformAttributes)
    {
        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(serviceName, serviceVersion: serviceVersion);

        ConfigureAwsResource(resourceBuilder, options, platformAttributes, useSelfHostedDefaults, environmentName);
        return resourceBuilder;
    }

    private static void ConfigureAwsResource(
        ResourceBuilder resourceBuilder,
        AwsTelemetryExportOptions options,
        IReadOnlyList<KeyValuePair<string, object>> platformAttributes,
        bool useSelfHostedDefaults,
        string? environmentName)
    {
        ArgumentNullException.ThrowIfNull(resourceBuilder);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(platformAttributes);

        var normalizedPlatform = string.IsNullOrWhiteSpace(options.HostedPlatform)
            ? null
            : NormalizeHostedPlatform(options.HostedPlatform);

        if (!string.IsNullOrWhiteSpace(options.HostedPlatform) && normalizedPlatform is null)
        {
            throw new InvalidOperationException(
                $"AWS hosted platform '{options.HostedPlatform}' is not supported. Use 'ec2', 'ecs', 'eks', 'elasticbeanstalk', or 'lambda'.");
        }

        switch (normalizedPlatform)
        {
            case "aws_ec2":
                resourceBuilder.AddAWSEC2Detector();
                break;
            case "aws_ecs":
                resourceBuilder.AddAWSECSDetector();
                break;
            case "aws_eks":
                resourceBuilder.AddAWSEKSDetector();
                break;
            case "aws_elastic_beanstalk":
                resourceBuilder.AddAWSEBSDetector();
                break;
        }

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

    private static IReadOnlyList<KeyValuePair<string, object>> ResolveHostedPlatformAttributes(
        string? hostedPlatform,
        string? environmentName)
    {
        if (string.IsNullOrWhiteSpace(hostedPlatform))
        {
            return Array.Empty<KeyValuePair<string, object>>();
        }

        var normalizedPlatform = NormalizeHostedPlatform(hostedPlatform);
        if (normalizedPlatform is null)
        {
            throw new InvalidOperationException(
                $"AWS hosted platform '{hostedPlatform}' is not supported. Use 'ec2', 'ecs', 'eks', 'elasticbeanstalk', or 'lambda'.");
        }

        var attributes = new List<KeyValuePair<string, object>>
        {
            new("cloud.provider", "aws"),
            new("cloud.platform", normalizedPlatform)
        };

        if (string.Equals(normalizedPlatform, "aws_lambda", StringComparison.Ordinal))
        {
            var region = Environment.GetEnvironmentVariable("AWS_REGION") ??
                Environment.GetEnvironmentVariable("AWS_DEFAULT_REGION");
            if (!string.IsNullOrWhiteSpace(region))
            {
                attributes.Add(new KeyValuePair<string, object>("cloud.region", region.Trim()));
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
}
