using Cephalon.Abstractions.Technologies;
using Cephalon.Observability.Configuration;

namespace Cephalon.Observability.Runtime;

/// <summary>
/// Creates sanitized runtime-surface projections for Cephalon observability exporter companion packages.
/// </summary>
/// <remarks>
/// The generated metadata intentionally describes configured telemetry intent without exposing raw
/// endpoints, headers, tokens, connection strings, or other deployment secrets.
/// </remarks>
public static class TelemetryExportRuntimeSurfaceFactory
{
    /// <summary>
    /// Gets the technology identifier used by Cephalon observability runtime surfaces.
    /// </summary>
    public const string TechnologyId = "observability";

    /// <summary>
    /// Creates a telemetry-export runtime surface from shared telemetry export options.
    /// </summary>
    /// <param name="surfaceId">The stable surface identifier within the observability technology profile.</param>
    /// <param name="displayName">The operator-facing display name for the surface.</param>
    /// <param name="description">A human-readable description of the surface.</param>
    /// <param name="entryId">The stable entry identifier for the active exporter or provider.</param>
    /// <param name="entryDisplayName">The operator-facing display name for the entry.</param>
    /// <param name="entryDescription">A human-readable description of the entry.</param>
    /// <param name="telemetry">The shared telemetry export options to project.</param>
    /// <param name="metadata">Additional sanitized metadata to merge into the entry.</param>
    /// <returns>A runtime surface that can be exposed through the technology runtime catalog.</returns>
    public static TechnologyRuntimeSurface CreateSurface(
        string surfaceId,
        string displayName,
        string description,
        string entryId,
        string entryDisplayName,
        string entryDescription,
        TelemetryExportOptions telemetry,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(surfaceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentException.ThrowIfNullOrWhiteSpace(entryId);
        ArgumentException.ThrowIfNullOrWhiteSpace(entryDisplayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(entryDescription);
        ArgumentNullException.ThrowIfNull(telemetry);

        var entryMetadata = CreateBaseMetadata(telemetry);
        if (metadata is not null)
        {
            foreach (var item in metadata.OrderBy(static item => item.Key, StringComparer.OrdinalIgnoreCase))
            {
                entryMetadata[item.Key] = item.Value;
            }
        }

        return new TechnologyRuntimeSurface(
            technologyId: TechnologyId,
            surfaceId: surfaceId,
            displayName: displayName,
            description: description,
            entries:
            [
                new TechnologyRuntimeEntry(
                    id: entryId,
                    displayName: entryDisplayName,
                    description: entryDescription,
                    metadata: entryMetadata)
            ]);
    }

    /// <summary>
    /// Creates sanitized metadata from shared telemetry export options.
    /// </summary>
    /// <param name="telemetry">The shared telemetry export options to project.</param>
    /// <returns>A mutable metadata dictionary with stable, non-secret telemetry intent values.</returns>
    public static Dictionary<string, string> CreateBaseMetadata(TelemetryExportOptions telemetry)
    {
        ArgumentNullException.ThrowIfNull(telemetry);

        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["provider"] = ResolveValue(telemetry.Provider),
            ["protocol"] = ResolveValue(telemetry.Protocol),
            ["sharedEndpointMode"] = ResolveSharedEndpointMode(telemetry),
            ["sharedEndpointConfigured"] = string.IsNullOrWhiteSpace(telemetry.Endpoint) ? "false" : "true",
            ["useSelfHostedDefaults"] = telemetry.UseSelfHostedDefaults ? "true" : "false",
            ["exportLogs"] = telemetry.ExportLogs ? "true" : "false",
            ["exportMetrics"] = telemetry.ExportMetrics ? "true" : "false",
            ["exportTraces"] = telemetry.ExportTraces ? "true" : "false",
            ["secretProjection"] = "redacted"
        };
    }

    /// <summary>
    /// Resolves the shared telemetry endpoint mode without returning the configured endpoint value.
    /// </summary>
    /// <param name="telemetry">The shared telemetry export options to inspect.</param>
    /// <returns>A stable mode string describing whether a shared endpoint or self-hosted defaults are active.</returns>
    public static string ResolveSharedEndpointMode(TelemetryExportOptions telemetry)
    {
        ArgumentNullException.ThrowIfNull(telemetry);

        if (!string.IsNullOrWhiteSpace(telemetry.Endpoint))
        {
            return "configured-endpoint";
        }

        return telemetry.UseSelfHostedDefaults ? "self-hosted-defaults" : "not-configured";
    }

    private static string ResolveValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "not-configured" : value.Trim();
    }
}
