using Cephalon.Abstractions.AppModel;
using Cephalon.Abstractions.Resilience;
using Cephalon.AspNetCore.Documentation;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Manifest;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Cephalon.AspNetCore.Hosting;

internal static class AspNetCoreRateLimitingPolicyResolver
{
    internal const string PolicyId = "cephalon-public-http";
    internal const string EnabledExecutionMode = "aspnetcore-global-middleware";
    internal const string DisabledExecutionMode = "disabled";
    internal const string Scope = "public-http-endpoints";
    internal const int RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    internal const string DefaultAlgorithm = "SlidingWindow";
    internal const int DefaultPermitLimit = 100;
    internal const int DefaultQueueLimit = 0;
    internal const int DefaultWindowSeconds = 60;
    internal const int DefaultSegmentsPerWindow = 4;
    internal const string PartitionStrategy = "subject-or-tenant-or-ip";

    private static readonly HashSet<string> SupportedHttpTransportKeys = new(
        [
            NormalizeTransportKey("rest-api"),
            NormalizeTransportKey("behavior-http"),
            NormalizeTransportKey("json-rpc"),
            NormalizeTransportKey("graphql"),
            NormalizeTransportKey("grpc"),
            NormalizeTransportKey("server-sent-events"),
            NormalizeTransportKey("web-socket")
        ],
        StringComparer.Ordinal);

    internal static bool IsHttpTransportId(string transportId)
    {
        return !string.IsNullOrWhiteSpace(transportId) &&
            SupportedHttpTransportKeys.Contains(NormalizeTransportKey(transportId));
    }

    internal static ResolvedAspNetCoreRateLimitingPolicy Resolve(
        RateLimitingSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return Resolve(settings, [], []);
    }

    internal static ResolvedAspNetCoreRateLimitingPolicy Resolve(
        RateLimitingSettings settings,
        IReadOnlyList<string> transportIds,
        IReadOnlyList<string> excludedPathPrefixes)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(transportIds);
        ArgumentNullException.ThrowIfNull(excludedPathPrefixes);

        return ResolveCore(
            hasValues: settings.HasValues,
            enabled: settings.Enabled,
            algorithm: settings.Algorithm,
            permitLimit: settings.PermitLimit,
            queueLimit: settings.QueueLimit,
            windowSeconds: settings.WindowSeconds,
            segmentsPerWindow: settings.SegmentsPerWindow,
            transportIds: transportIds,
            excludedPathPrefixes: excludedPathPrefixes);
    }

    internal static ResolvedAspNetCoreRateLimitingPolicy Resolve(
        RateLimitingSelection selection,
        IReadOnlyList<string> transportIds,
        IReadOnlyList<string> excludedPathPrefixes)
    {
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentNullException.ThrowIfNull(transportIds);
        ArgumentNullException.ThrowIfNull(excludedPathPrefixes);

        return ResolveCore(
            hasValues: selection.HasValues,
            enabled: selection.Enabled,
            algorithm: selection.Algorithm,
            permitLimit: selection.PermitLimit,
            queueLimit: selection.QueueLimit,
            windowSeconds: selection.WindowSeconds,
            segmentsPerWindow: selection.SegmentsPerWindow,
            transportIds: transportIds,
            excludedPathPrefixes: excludedPathPrefixes);
    }

    internal static ResolvedAspNetCoreRateLimitingPolicy Resolve(
        RuntimeManifest manifest,
        IConfiguration configuration,
        ReferenceDocsHostingOptions? referenceDocsOptions)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(configuration);

        var transportIds = manifest.AppProfile.Transports
            .Select(static transport => transport.Id)
            .Where(IsHttpTransportId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static transportId => transportId, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return Resolve(
            manifest.AppProfile.Resilience.RateLimiting,
            transportIds,
            ResolveExcludedPathPrefixes(configuration, referenceDocsOptions));
    }

    internal static IReadOnlyList<string> ResolveExcludedPathPrefixes(
        IConfiguration configuration,
        ReferenceDocsHostingOptions? referenceDocsOptions)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var openApiOptions = OpenApiEndpointOptions.FromConfiguration(configuration);
        var prefixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "/engine",
            "/health",
            ResolveOpenApiRoutePrefix(openApiOptions.RoutePattern),
            openApiOptions.ScalarRoutePrefix,
            "/favicon.ico"
        };

        if (referenceDocsOptions?.Enabled == true)
        {
            prefixes.Add(ResolveReferenceDocsRoutePrefix(referenceDocsOptions.RoutePrefix));
        }

        return prefixes
            .OrderBy(static prefix => prefix, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static ResolvedAspNetCoreRateLimitingPolicy ResolveCore(
        bool hasValues,
        bool? enabled,
        string? algorithm,
        int? permitLimit,
        int? queueLimit,
        int? windowSeconds,
        int? segmentsPerWindow,
        IReadOnlyList<string> transportIds,
        IReadOnlyList<string> excludedPathPrefixes)
    {
        var normalizedTransportIds = transportIds
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static transportId => transportId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var normalizedExcludedPrefixes = excludedPathPrefixes
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static prefix => prefix, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["scope"] = Scope,
            ["partitionStrategy"] = PartitionStrategy
        };

        if (!hasValues && normalizedTransportIds.Length == 0)
        {
            metadata["reason"] = "no-http-transports-selected";
            return new ResolvedAspNetCoreRateLimitingPolicy(
                ExecutionMode: DisabledExecutionMode,
                Effective: new RateLimitingSelection(enabled: false),
                TransportIds: normalizedTransportIds,
                ExcludedPathPrefixes: normalizedExcludedPrefixes,
                Metadata: metadata);
        }

        if (enabled != true)
        {
            metadata["reason"] = hasValues
                ? "disabled-by-configuration"
                : "not-configured";
            return new ResolvedAspNetCoreRateLimitingPolicy(
                ExecutionMode: DisabledExecutionMode,
                Effective: new RateLimitingSelection(enabled: false),
                TransportIds: normalizedTransportIds,
                ExcludedPathPrefixes: normalizedExcludedPrefixes,
                Metadata: metadata);
        }

        if (normalizedTransportIds.Length == 0)
        {
            metadata["reason"] = "no-http-transports-selected";
            return new ResolvedAspNetCoreRateLimitingPolicy(
                ExecutionMode: DisabledExecutionMode,
                Effective: new RateLimitingSelection(enabled: false),
                TransportIds: normalizedTransportIds,
                ExcludedPathPrefixes: normalizedExcludedPrefixes,
                Metadata: metadata);
        }

        var resolvedAlgorithm = NormalizeAlgorithm(algorithm);
        var resolvedPermitLimit = permitLimit ?? DefaultPermitLimit;
        var resolvedQueueLimit = queueLimit ?? DefaultQueueLimit;
        var resolvedWindowSeconds = UsesWindow(resolvedAlgorithm)
            ? windowSeconds ?? DefaultWindowSeconds
            : (int?)null;
        var resolvedSegmentsPerWindow = string.Equals(
            resolvedAlgorithm,
            "SlidingWindow",
            StringComparison.OrdinalIgnoreCase)
            ? segmentsPerWindow ?? DefaultSegmentsPerWindow
            : (int?)null;

        metadata["reason"] = "configured";
        metadata["algorithm"] = resolvedAlgorithm;
        metadata["queueProcessingOrder"] = "OldestFirst";

        if (resolvedWindowSeconds.HasValue)
        {
            metadata["windowSeconds"] = resolvedWindowSeconds.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        if (resolvedSegmentsPerWindow.HasValue)
        {
            metadata["segmentsPerWindow"] = resolvedSegmentsPerWindow.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        return new ResolvedAspNetCoreRateLimitingPolicy(
            ExecutionMode: EnabledExecutionMode,
            Effective: new RateLimitingSelection(
                enabled: true,
                algorithm: resolvedAlgorithm,
                permitLimit: resolvedPermitLimit,
                queueLimit: resolvedQueueLimit,
                windowSeconds: resolvedWindowSeconds,
                segmentsPerWindow: resolvedSegmentsPerWindow),
            TransportIds: normalizedTransportIds,
            ExcludedPathPrefixes: normalizedExcludedPrefixes,
            Metadata: metadata);
    }

    private static string NormalizeAlgorithm(string? algorithm)
    {
        if (string.IsNullOrWhiteSpace(algorithm))
        {
            return DefaultAlgorithm;
        }

        var trimmed = algorithm.Trim();
        if (string.Equals(trimmed, "FixedWindow", StringComparison.OrdinalIgnoreCase))
        {
            return "FixedWindow";
        }

        if (string.Equals(trimmed, "SlidingWindow", StringComparison.OrdinalIgnoreCase))
        {
            return "SlidingWindow";
        }

        if (string.Equals(trimmed, "TokenBucket", StringComparison.OrdinalIgnoreCase))
        {
            return "TokenBucket";
        }

        if (string.Equals(trimmed, "ConcurrencyLimiter", StringComparison.OrdinalIgnoreCase))
        {
            return "ConcurrencyLimiter";
        }

        return trimmed;
    }

    private static string NormalizeTransportKey(string value)
    {
        return string.Concat(value.Where(static ch => char.IsLetterOrDigit(ch)))
            .ToUpperInvariant();
    }

    private static bool UsesWindow(string algorithm)
    {
        return string.Equals(algorithm, "FixedWindow", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(algorithm, "SlidingWindow", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(algorithm, "TokenBucket", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveOpenApiRoutePrefix(string routePattern)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(routePattern);

        var placeholderIndex = routePattern.IndexOf('{');
        var prefix = placeholderIndex >= 0
            ? routePattern[..placeholderIndex]
            : routePattern;
        prefix = prefix.TrimEnd('/');

        return string.IsNullOrWhiteSpace(prefix)
            ? "/openapi"
            : prefix;
    }

    private static string ResolveReferenceDocsRoutePrefix(string? routePrefix)
    {
        var normalized = string.IsNullOrWhiteSpace(routePrefix)
            ? "/reference"
            : routePrefix.Trim();

        if (!normalized.StartsWith('/'))
        {
            normalized = "/" + normalized;
        }

        return normalized.Length > 1
            ? normalized.TrimEnd('/')
            : normalized;
    }
}

internal sealed record ResolvedAspNetCoreRateLimitingPolicy(
    string ExecutionMode,
    RateLimitingSelection Effective,
    IReadOnlyList<string> TransportIds,
    IReadOnlyList<string> ExcludedPathPrefixes,
    IReadOnlyDictionary<string, string> Metadata)
{
    internal RateLimitingRuntimeDescriptor ToDescriptor(RateLimitingSelection requested)
    {
        ArgumentNullException.ThrowIfNull(requested);

        var enabled = string.Equals(
            ExecutionMode,
            AspNetCoreRateLimitingPolicyResolver.EnabledExecutionMode,
            StringComparison.OrdinalIgnoreCase);
        var description = enabled
            ? "Global ASP.NET Core HTTP rate limiting applied to public Cephalon endpoints with operator and documentation routes excluded."
            : "ASP.NET Core HTTP rate limiting requested by the Cephalon host, but not actively enforced.";

        return new RateLimitingRuntimeDescriptor(
            AspNetCoreRateLimitingPolicyResolver.PolicyId,
            "Cephalon Public HTTP Rate Limiter",
            description,
            ExecutionMode,
            AspNetCoreRateLimitingPolicyResolver.Scope,
            AspNetCoreRateLimitingPolicyResolver.RejectionStatusCode,
            TransportIds,
            ExcludedPathPrefixes,
            requested,
            Effective,
            Metadata);
    }
}
