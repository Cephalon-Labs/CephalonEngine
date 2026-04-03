using Cephalon.Engine.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cephalon.Observability.NewRelic.Configuration;

/// <summary>
/// Configures New Relic observability defaults on top of the shared Cephalon telemetry contract.
/// </summary>
public sealed class NewRelicTelemetryExportOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NewRelicTelemetryExportOptions" /> class.
    /// </summary>
    public NewRelicTelemetryExportOptions()
    {
    }

    /// <summary>
    /// Gets or sets a value indicating whether the package should target the New Relic native OTLP endpoint
    /// when no shared collector endpoint is configured.
    /// </summary>
    /// <remarks>
    /// This direct path is intended for the documented New Relic native OTLP ingestion route. Teams that prefer
    /// a collector or gateway can keep using the shared <c>Endpoint</c> or <c>UseSelfHostedDefaults</c> contract instead.
    /// </remarks>
    public bool UseNativeOtlpEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the base New Relic OTLP endpoint used for direct ingestion.
    /// </summary>
    /// <remarks>
    /// If this value is omitted and direct ingestion is enabled, the package derives the endpoint from
    /// <see cref="Region" />. For OTLP/HTTP the package treats this as the base endpoint and appends the
    /// signal-specific <c>/v1/traces</c>, <c>/v1/metrics</c>, or <c>/v1/logs</c> suffix automatically.
    /// </remarks>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Gets or sets the raw OTLP headers string used for direct New Relic ingestion.
    /// </summary>
    /// <remarks>
    /// Use the standard OTLP <c>key=value</c> comma-separated format. This value takes precedence over
    /// <see cref="LicenseKey" /> when both are configured.
    /// </remarks>
    public string? Headers { get; set; }

    /// <summary>
    /// Gets or sets the New Relic license key used to build the required <c>api-key</c> header
    /// when the package should construct OTLP headers from structured settings.
    /// </summary>
    public string? LicenseKey { get; set; }

    /// <summary>
    /// Gets or sets the New Relic OTLP region used when the package derives the direct-ingestion endpoint.
    /// </summary>
    /// <remarks>
    /// Supported values are <c>us</c>, <c>eu</c>, and <c>fedramp</c>. If omitted, the package defaults
    /// to the New Relic US OTLP endpoint.
    /// </remarks>
    public string? Region { get; set; }

    /// <summary>
    /// Gets or sets the optional <c>service.namespace</c> resource attribute to stamp onto exported telemetry.
    /// </summary>
    public string? ServiceNamespace { get; set; }

    /// <summary>
    /// Binds New Relic telemetry export options from configuration.
    /// </summary>
    /// <param name="configuration">The application configuration root.</param>
    /// <param name="sectionPath">
    /// The configuration section path that contains the engine settings. The default is <c>Engine</c>.
    /// </param>
    /// <returns>The bound New Relic telemetry export options.</returns>
    public static NewRelicTelemetryExportOptions FromConfiguration(
        IConfiguration configuration,
        string sectionPath = EngineSettings.SectionName)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration
            .GetSection(sectionPath)
            .GetSection("Observability")
            .GetSection("Telemetry")
            .GetSection("NewRelic");

        return FromSection(section);
    }

    internal static NewRelicTelemetryExportOptions FromSection(IConfigurationSection section)
    {
        ArgumentNullException.ThrowIfNull(section);

        return new NewRelicTelemetryExportOptions
        {
            UseNativeOtlpEndpoint = GetBoolean(section["UseNativeOtlpEndpoint"], defaultValue: false),
            Endpoint = Normalize(section["Endpoint"]),
            Headers = Normalize(section["Headers"]),
            LicenseKey = Normalize(section["LicenseKey"]),
            Region = Normalize(section["Region"]),
            ServiceNamespace = Normalize(section["ServiceNamespace"])
        };
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static bool GetBoolean(string? value, bool defaultValue)
    {
        return bool.TryParse(value, out var parsed) ? parsed : defaultValue;
    }
}
