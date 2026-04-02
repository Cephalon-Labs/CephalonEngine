using Cephalon.Engine.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cephalon.Observability.Aws.Configuration;

/// <summary>
/// Configures AWS-hosted observability defaults on top of the shared Cephalon telemetry contract.
/// </summary>
public sealed class AwsTelemetryExportOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AwsTelemetryExportOptions" /> class.
    /// </summary>
    public AwsTelemetryExportOptions()
    {
    }

    /// <summary>
    /// Gets or sets the hosted AWS platform whose default resource attributes and detectors should be applied.
    /// </summary>
    /// <remarks>
    /// Supported values are <c>ec2</c>, <c>ecs</c>, <c>eks</c>, <c>elasticbeanstalk</c>, and <c>lambda</c>.
    /// The package maps them to the current OpenTelemetry <c>cloud.platform</c> attribute values and uses
    /// the matching AWS resource detector when one is available.
    /// </remarks>
    public string? HostedPlatform { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether AWS X-Ray-compatible trace identifiers should be used.
    /// </summary>
    public bool UseXRayTraceIds { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the AWS X-Ray text-map propagator should become the default
    /// propagator for the host when traces are enabled.
    /// </summary>
    public bool UseXRayPropagator { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether AWS SDK client instrumentation should be enabled for traces.
    /// </summary>
    public bool EnableAwsSdkInstrumentation { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether Lambda context extraction should be configured when the hosted
    /// platform is <c>lambda</c>.
    /// </summary>
    /// <remarks>
    /// This does not wrap Lambda handlers automatically. It only configures the OpenTelemetry Lambda extension
    /// so hosts that already use the wrapper APIs can keep AWS X-Ray context extraction aligned.
    /// </remarks>
    public bool EnableLambdaContextExtraction { get; set; } = true;

    /// <summary>
    /// Binds AWS telemetry export options from configuration.
    /// </summary>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="sectionPath">
    /// The configuration section path that contains the engine settings. The default is <c>Engine</c>.
    /// </param>
    /// <returns>The bound AWS telemetry export options.</returns>
    public static AwsTelemetryExportOptions FromConfiguration(
        IConfiguration configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("Observability")
            .GetSection("Telemetry")
            .GetSection("Aws");

        return FromSection(section);
    }

    internal static AwsTelemetryExportOptions FromSection(IConfigurationSection section)
    {
        ArgumentNullException.ThrowIfNull(section);

        return new AwsTelemetryExportOptions
        {
            HostedPlatform = string.IsNullOrWhiteSpace(section["HostedPlatform"])
                ? null
                : section["HostedPlatform"]!.Trim(),
            UseXRayTraceIds = GetBoolean(section["UseXRayTraceIds"], defaultValue: true),
            UseXRayPropagator = GetBoolean(section["UseXRayPropagator"], defaultValue: true),
            EnableAwsSdkInstrumentation = GetBoolean(section["EnableAwsSdkInstrumentation"], defaultValue: true),
            EnableLambdaContextExtraction = GetBoolean(section["EnableLambdaContextExtraction"], defaultValue: true)
        };
    }

    private static bool GetBoolean(string? value, bool defaultValue)
    {
        return bool.TryParse(value, out var parsed) ? parsed : defaultValue;
    }
}
