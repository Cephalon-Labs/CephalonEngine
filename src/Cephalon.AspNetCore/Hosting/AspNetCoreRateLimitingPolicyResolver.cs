using Cephalon.Abstractions.AppModel;
using Cephalon.Abstractions.Resilience;
using Cephalon.AspNetCore.Documentation;
using Cephalon.Engine.Manifest;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Cephalon.AspNetCore.Hosting;

internal static class AspNetCoreRateLimitingPolicyResolver
{
    internal const string PolicyId = "cephalon-public-http";
    internal const string EnabledExecutionMode = "aspnetcore-endpoint-policy";
    internal const string DisabledExecutionMode = "disabled";
    internal const string Scope = "public-http-endpoints";
    internal const int RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    internal const string DefaultAlgorithm = "SlidingWindow";
    internal const int DefaultPermitLimit = 100;
    internal const int DefaultQueueLimit = 0;
    internal const int DefaultWindowSeconds = 60;
    internal const int DefaultSegmentsPerWindow = 4;
    internal const string PartitionStrategy = "subject-or-tenant-or-ip";
    internal const string LongLivedTransportScope = "long-lived-transport-endpoints";
    internal const string BehaviorLongLivedTransportScope = "behavior-long-lived-transport-endpoints";

    private static readonly string[] BuiltInGraphQlTransportIds =
    [
        "graphql",
        "graphql-sse",
        "graphql-ws"
    ];

    private static readonly string[] BehaviorHttpTransportIds =
    [
        "http.graphql",
        "http.graphql-sse",
        "http.graphql-ws",
        "http.jsonrpc",
        "http.sse",
        "http.ws"
    ];

    private static readonly HashSet<string> LongLivedTransportKeys = new(
        new[]
        {
            "graphql-sse",
            "graphql-ws",
            "server-sent-events",
            "websocket",
            "http.graphql-sse",
            "http.graphql-ws",
            "http.sse",
            "http.ws"
        }.Select(NormalizeTransportKey),
        StringComparer.Ordinal);

    private static readonly HashSet<string> StreamTransportKeys = new(
        new[]
        {
            "graphql-sse",
            "server-sent-events",
            "http.graphql-sse",
            "http.sse"
        }.Select(NormalizeTransportKey),
        StringComparer.Ordinal);

    private static readonly HashSet<string> ConnectionTransportKeys = new(
        new[]
        {
            "graphql-ws",
            "websocket",
            "http.graphql-ws",
            "http.ws"
        }.Select(NormalizeTransportKey),
        StringComparer.Ordinal);

    private static readonly HashSet<string> SupportedHttpTransportKeys = new(
        [
            NormalizeTransportKey("rest-api"),
            NormalizeTransportKey("behavior-http"),
            NormalizeTransportKey("json-rpc"),
            .. BuiltInGraphQlTransportIds.Select(NormalizeTransportKey),
            NormalizeTransportKey("grpc"),
            NormalizeTransportKey("server-sent-events"),
            NormalizeTransportKey("web-socket"),
            .. BehaviorHttpTransportIds.Select(NormalizeTransportKey)
        ],
        StringComparer.Ordinal);

    internal static bool IsHttpTransportId(string transportId)
    {
        return !string.IsNullOrWhiteSpace(transportId) &&
            SupportedHttpTransportKeys.Contains(NormalizeTransportKey(transportId));
    }

    internal static AspNetCoreRateLimitingPolicyCatalog ResolvePolicies(
        RuntimeManifest manifest,
        IConfiguration configuration,
        ReferenceDocsHostingOptions? referenceDocsOptions)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(configuration);

        var publicTransportIds = ResolvePublicHttpTransportIds(manifest);
        var excludedPathPrefixes = ResolveExcludedPathPrefixes(configuration, referenceDocsOptions);
        var requested = manifest.AppProfile.Resilience.RateLimiting;
        var policies = new List<ResolvedAspNetCoreRateLimitingPolicy>();

        var defaultPolicy = ResolveDefaultPolicy(requested, publicTransportIds, excludedPathPrefixes);
        if (defaultPolicy is not null)
        {
            policies.Add(defaultPolicy);
        }

        for (var index = 0; index < requested.Overrides.Count; index++)
        {
            var overrideSelection = requested.Overrides[index];
            var policy = ResolveOverridePolicy(
                overrideSelection,
                publicTransportIds,
                order: index + 1);
            if (policy is not null)
            {
                policies.Add(policy);
            }
        }

        return new AspNetCoreRateLimitingPolicyCatalog(policies);
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

    private static ResolvedAspNetCoreRateLimitingPolicy? ResolveDefaultPolicy(
        RateLimitingSelection selection,
        string[] publicTransportIds,
        IReadOnlyList<string> excludedPathPrefixes)
    {
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentNullException.ThrowIfNull(publicTransportIds);
        ArgumentNullException.ThrowIfNull(excludedPathPrefixes);

        if (selection.Enabled != true || publicTransportIds.Length == 0)
        {
            return null;
        }

        var effective = CreateEffectiveSelection(
            selection.Enabled,
            selection.Algorithm,
            selection.PermitLimit,
            selection.QueueLimit,
            selection.WindowSeconds,
            selection.SegmentsPerWindow);
        var scope = ResolveDefaultScope(publicTransportIds);
        var metadata = CreateMetadata(
            isOverride: false,
            overrideId: null,
            behaviorIds: [],
            transportIds: publicTransportIds,
            algorithm: effective.Algorithm,
            effective.WindowSeconds,
            effective.SegmentsPerWindow);
        metadata["reason"] = "configured";
        metadata["scope"] = scope;

        return new ResolvedAspNetCoreRateLimitingPolicy(
            Id: PolicyId,
            DisplayName: "Cephalon Public HTTP Rate Limiter",
            Description: BuildDefaultDescription(publicTransportIds),
            ExecutionMode: EnabledExecutionMode,
            Scope: scope,
            RejectionStatusCode: RejectionStatusCode,
            TransportIds: publicTransportIds,
            BehaviorIds: [],
            ExcludedPathPrefixes: excludedPathPrefixes,
            Requested: new RateLimitingSelection(
                enabled: selection.Enabled,
                algorithm: selection.Algorithm,
                permitLimit: selection.PermitLimit,
                queueLimit: selection.QueueLimit,
                windowSeconds: selection.WindowSeconds,
                segmentsPerWindow: selection.SegmentsPerWindow),
            Effective: effective,
            Metadata: metadata,
            Order: 0,
            IsOverride: false);
    }

    private static ResolvedAspNetCoreRateLimitingPolicy? ResolveOverridePolicy(
        RateLimitingOverrideSelection selection,
        string[] publicTransportIds,
        int order)
    {
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentNullException.ThrowIfNull(publicTransportIds);
        ArgumentOutOfRangeException.ThrowIfNegative(order);

        var targetedTransportIds = selection.TransportIds
            .Select(CanonicalizeTransportId)
            .Where(static value => value is not null)
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (targetedTransportIds.Length > 0)
        {
            targetedTransportIds = targetedTransportIds
                .Where(publicTransportIds.Contains)
                .ToArray();
        }

        var behaviorIds = selection.BehaviorIds
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (targetedTransportIds.Length == 0 && behaviorIds.Length == 0)
        {
            return null;
        }

        var hasPolicyInputs = HasOverridePolicyInputs(selection);
        var enabled = selection.Enabled ?? (hasPolicyInputs ? true : (bool?)null);
        var scope = ResolveOverrideScope(behaviorIds, targetedTransportIds);
        var metadata = CreateMetadata(
            isOverride: true,
            overrideId: selection.Id,
            behaviorIds,
            transportIds: targetedTransportIds,
            algorithm: NormalizeAlgorithm(selection.Algorithm),
            windowSeconds: selection.WindowSeconds,
            segmentsPerWindow: selection.SegmentsPerWindow);
        metadata["scope"] = scope;
        metadata["reason"] = enabled == false
            ? "disabled-by-override"
            : "configured";

        if (enabled != true)
        {
            return new ResolvedAspNetCoreRateLimitingPolicy(
                Id: BuildOverridePolicyId(selection.Id),
                DisplayName: $"Rate Limiting Override ({selection.Id})",
                Description: BuildOverrideDescription(selection.Id, behaviorIds, targetedTransportIds, enabled: false),
                ExecutionMode: DisabledExecutionMode,
                Scope: scope,
                RejectionStatusCode: RejectionStatusCode,
                TransportIds: targetedTransportIds,
                BehaviorIds: behaviorIds,
                ExcludedPathPrefixes: [],
                Requested: ToRequestedSelection(selection),
                Effective: new RateLimitingSelection(enabled: false),
                Metadata: metadata,
                Order: order,
                IsOverride: true);
        }

        var effective = CreateEffectiveSelection(
            enabled,
            selection.Algorithm,
            selection.PermitLimit,
            selection.QueueLimit,
            selection.WindowSeconds,
            selection.SegmentsPerWindow);

        return new ResolvedAspNetCoreRateLimitingPolicy(
            Id: BuildOverridePolicyId(selection.Id),
            DisplayName: $"Rate Limiting Override ({selection.Id})",
            Description: BuildOverrideDescription(selection.Id, behaviorIds, targetedTransportIds, enabled: true),
            ExecutionMode: EnabledExecutionMode,
            Scope: scope,
            RejectionStatusCode: RejectionStatusCode,
            TransportIds: targetedTransportIds,
            BehaviorIds: behaviorIds,
            ExcludedPathPrefixes: [],
            Requested: ToRequestedSelection(selection),
            Effective: effective,
            Metadata: metadata,
            Order: order,
            IsOverride: true);
    }

    private static string[] ResolvePublicHttpTransportIds(RuntimeManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        var transportIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var transport in manifest.AppProfile.Transports)
        {
            if (!IsHttpTransportId(transport.Id))
            {
                continue;
            }

            var canonicalId = CanonicalizeTransportId(transport.Id);
            if (canonicalId is null)
            {
                continue;
            }

            transportIds.Add(canonicalId);
            if (string.Equals(canonicalId, "graphql", StringComparison.OrdinalIgnoreCase))
            {
                transportIds.Add("graphql-sse");
                transportIds.Add("graphql-ws");
            }

            if (string.Equals(canonicalId, "behavior-http", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var behaviorTransportId in BehaviorHttpTransportIds)
                {
                    transportIds.Add(behaviorTransportId);
                }
            }
        }

        return transportIds
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static RateLimitingSelection CreateEffectiveSelection(
        bool? enabled,
        string? algorithm,
        int? permitLimit,
        int? queueLimit,
        int? windowSeconds,
        int? segmentsPerWindow)
    {
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

        return new RateLimitingSelection(
            enabled: enabled,
            algorithm: resolvedAlgorithm,
            permitLimit: resolvedPermitLimit,
            queueLimit: resolvedQueueLimit,
            windowSeconds: resolvedWindowSeconds,
            segmentsPerWindow: resolvedSegmentsPerWindow);
    }

    private static RateLimitingSelection ToRequestedSelection(RateLimitingOverrideSelection selection)
    {
        ArgumentNullException.ThrowIfNull(selection);

        return new RateLimitingSelection(
            enabled: selection.Enabled,
            algorithm: selection.Algorithm,
            permitLimit: selection.PermitLimit,
            queueLimit: selection.QueueLimit,
            windowSeconds: selection.WindowSeconds,
            segmentsPerWindow: selection.SegmentsPerWindow);
    }

    private static Dictionary<string, string> CreateMetadata(
        bool isOverride,
        string? overrideId,
        string[] behaviorIds,
        string[] transportIds,
        string? algorithm,
        int? windowSeconds,
        int? segmentsPerWindow)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["partitionStrategy"] = PartitionStrategy,
            ["queueProcessingOrder"] = "OldestFirst",
            ["isOverride"] = isOverride ? "true" : "false"
        };

        if (!string.IsNullOrWhiteSpace(overrideId))
        {
            metadata["overrideId"] = overrideId.Trim();
        }

        if (behaviorIds.Length > 0)
        {
            metadata["behaviorIds"] = string.Join(",", behaviorIds);
        }

        if (transportIds.Length > 0)
        {
            metadata["transportIds"] = string.Join(",", transportIds);
        }

        if (TryResolveTransportKind(transportIds) is { } transportKind)
        {
            metadata["transportKind"] = transportKind;
        }

        if (TryResolveTransportSemantics(transportIds, algorithm) is { } transportSemantics)
        {
            metadata["transportSemantics"] = transportSemantics;
        }

        if (TryResolveEnforcementMoment(transportIds, algorithm) is { } enforcementMoment)
        {
            metadata["enforcementMoment"] = enforcementMoment;
        }

        var longLivedTransportIds = transportIds
            .Where(IsCanonicalLongLivedTransportId)
            .ToArray();
        if (longLivedTransportIds.Length > 0)
        {
            metadata["longLivedTransportIds"] = string.Join(",", longLivedTransportIds);
        }

        if (!string.IsNullOrWhiteSpace(algorithm))
        {
            metadata["algorithm"] = algorithm.Trim();
        }

        if (windowSeconds.HasValue)
        {
            metadata["windowSeconds"] = windowSeconds.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        if (segmentsPerWindow.HasValue)
        {
            metadata["segmentsPerWindow"] = segmentsPerWindow.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        return metadata;
    }

    private static bool HasOverridePolicyInputs(RateLimitingOverrideSelection selection)
    {
        return selection.Enabled.HasValue ||
            selection.Algorithm is not null ||
            selection.PermitLimit.HasValue ||
            selection.QueueLimit.HasValue ||
            selection.WindowSeconds.HasValue ||
            selection.SegmentsPerWindow.HasValue;
    }

    private static string BuildOverridePolicyId(string overrideId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(overrideId);

        var normalized = string.Concat(overrideId.Trim().Select(static ch =>
            char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : '-'));
        return $"cephalon-rate-limit-{normalized.Trim('-')}";
    }

    private static string BuildOverrideDescription(
        string overrideId,
        string[] behaviorIds,
        string[] transportIds,
        bool enabled)
    {
        var targets = new List<string>();
        if (behaviorIds.Length > 0)
        {
            targets.Add($"behaviors [{string.Join(", ", behaviorIds)}]");
        }

        if (transportIds.Length > 0)
        {
            targets.Add(BuildTransportTargetDescription(transportIds));
        }

        var targetDescription = targets.Count == 0
            ? "the selected surface"
            : string.Join(" and ", targets);

        return enabled
            ? $"ASP.NET Core rate-limiting override '{overrideId}' applied to {targetDescription}."
            : $"ASP.NET Core rate-limiting override '{overrideId}' disables limiting for {targetDescription}.";
    }

    private static string ResolveOverrideScope(
        string[] behaviorIds,
        string[] transportIds)
    {
        var targetsOnlyLongLived = transportIds.Length > 0 &&
            transportIds.All(IsCanonicalLongLivedTransportId);

        return (behaviorIds.Length > 0, transportIds.Length > 0) switch
        {
            (true, true) when targetsOnlyLongLived => BehaviorLongLivedTransportScope,
            (true, true) => "behavior-transport-endpoints",
            (true, false) => "behavior-endpoints",
            (false, true) when targetsOnlyLongLived => LongLivedTransportScope,
            (false, true) => "transport-endpoints",
            _ => Scope
        };
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

    internal static string? CanonicalizeTransportId(string transportId)
    {
        if (string.IsNullOrWhiteSpace(transportId))
        {
            return null;
        }

        var normalized = transportId.Trim()
            .Replace('_', '-')
            .ToLowerInvariant();

        return normalized switch
        {
            "rest-api" or "rest" or "http.rest" => "rest-api",
            "behavior-http" => "behavior-http",
            "json-rpc" => "json-rpc",
            "graphql" => "graphql",
            "graphql-sse" => "graphql-sse",
            "graphql-ws" => "graphql-ws",
            "grpc" => "grpc",
            "server-sent-events" or "sse" => "server-sent-events",
            "web-socket" or "websocket" or "ws" => "websocket",
            "http.jsonrpc" or "http.json-rpc" => "http.jsonrpc",
            "http.graphql" => "http.graphql",
            "http.graphql-sse" => "http.graphql-sse",
            "http.graphql-ws" => "http.graphql-ws",
            "http.sse" => "http.sse",
            "http.ws" => "http.ws",
            _ => null
        };
    }

    private static bool UsesWindow(string algorithm)
    {
        return string.Equals(algorithm, "FixedWindow", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(algorithm, "SlidingWindow", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(algorithm, "TokenBucket", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveDefaultScope(string[] transportIds)
    {
        ArgumentNullException.ThrowIfNull(transportIds);

        return transportIds.Length > 0 && transportIds.All(IsCanonicalLongLivedTransportId)
            ? LongLivedTransportScope
            : Scope;
    }

    private static string BuildDefaultDescription(string[] transportIds)
    {
        ArgumentNullException.ThrowIfNull(transportIds);

        return transportIds.Length > 0 && transportIds.All(IsCanonicalLongLivedTransportId)
            ? "Default ASP.NET Core rate limiting applied to public long-lived Cephalon HTTP transports."
            : "Default ASP.NET Core rate limiting applied to public Cephalon HTTP endpoints.";
    }

    private static string BuildTransportTargetDescription(string[] transportIds)
    {
        ArgumentNullException.ThrowIfNull(transportIds);

        var label = transportIds.Length > 0 && transportIds.All(IsCanonicalLongLivedTransportId)
            ? "long-lived transports"
            : "transports";
        return $"{label} [{string.Join(", ", transportIds)}]";
    }

    private static string? TryResolveTransportKind(string[] transportIds)
    {
        ArgumentNullException.ThrowIfNull(transportIds);

        if (transportIds.Length == 0)
        {
            return null;
        }

        var hasLongLived = transportIds.Any(IsCanonicalLongLivedTransportId);
        var hasRequestResponse = transportIds.Any(static transportId => !IsCanonicalLongLivedTransportId(transportId));
        if (hasLongLived && hasRequestResponse)
        {
            return "mixed-request-and-long-lived";
        }

        if (!hasLongLived)
        {
            return "request-response";
        }

        var hasStream = transportIds.Any(IsCanonicalStreamTransportId);
        var hasConnection = transportIds.Any(IsCanonicalConnectionTransportId);

        return (hasStream, hasConnection) switch
        {
            (true, true) => "long-lived-mixed",
            (true, false) => "long-lived-stream",
            (false, true) => "long-lived-connection",
            _ => "long-lived"
        };
    }

    private static string? TryResolveTransportSemantics(string[] transportIds, string? algorithm)
    {
        ArgumentNullException.ThrowIfNull(transportIds);

        var transportKind = TryResolveTransportKind(transportIds);
        if (transportKind is null)
        {
            return null;
        }

        var usesConcurrency = string.Equals(
            NormalizeAlgorithm(algorithm),
            "ConcurrencyLimiter",
            StringComparison.OrdinalIgnoreCase);

        return (usesConcurrency, transportKind) switch
        {
            (true, "request-response") => "in-flight-request-concurrency",
            (true, "long-lived-stream") => "active-stream-concurrency",
            (true, "long-lived-connection") => "active-connection-concurrency",
            (true, "long-lived-mixed") => "active-stream-and-connection-concurrency",
            (true, "mixed-request-and-long-lived") => "mixed-request-and-session-concurrency",
            (false, "request-response") => "request-entry-rate",
            (false, "long-lived-stream") => "stream-entry-rate",
            (false, "long-lived-connection") => "connection-entry-rate",
            (false, "long-lived-mixed") => "session-entry-rate",
            (false, "mixed-request-and-long-lived") => "mixed-request-and-session-entry-rate",
            _ => "request-entry-rate"
        };
    }

    private static string? TryResolveEnforcementMoment(string[] transportIds, string? algorithm)
    {
        ArgumentNullException.ThrowIfNull(transportIds);

        if (transportIds.Length == 0)
        {
            return null;
        }

        var usesConcurrency = string.Equals(
            NormalizeAlgorithm(algorithm),
            "ConcurrencyLimiter",
            StringComparison.OrdinalIgnoreCase);
        var hasLongLived = transportIds.Any(IsCanonicalLongLivedTransportId);
        var hasRequestResponse = transportIds.Any(static transportId => !IsCanonicalLongLivedTransportId(transportId));

        return (usesConcurrency, hasLongLived, hasRequestResponse) switch
        {
            (true, true, true) => "held-until-request-completes-or-session-closes",
            (true, true, false) => "held-until-session-closes",
            (true, false, true) => "held-until-request-completes",
            (false, true, true) => "checked-on-request-or-session-entry",
            (false, true, false) => "checked-on-session-establishment-only",
            _ => "checked-on-request-entry"
        };
    }

    private static bool IsCanonicalLongLivedTransportId(string transportId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transportId);

        return LongLivedTransportKeys.Contains(NormalizeTransportKey(transportId));
    }

    private static bool IsCanonicalStreamTransportId(string transportId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transportId);

        return StreamTransportKeys.Contains(NormalizeTransportKey(transportId));
    }

    private static bool IsCanonicalConnectionTransportId(string transportId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transportId);

        return ConnectionTransportKeys.Contains(NormalizeTransportKey(transportId));
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

internal sealed class AspNetCoreRateLimitingPolicyCatalog
{
    private readonly ResolvedAspNetCoreRateLimitingPolicy[] policies;
    private readonly ResolvedAspNetCoreRateLimitingPolicy[] enabledPolicies;

    public AspNetCoreRateLimitingPolicyCatalog(IReadOnlyList<ResolvedAspNetCoreRateLimitingPolicy> policies)
    {
        ArgumentNullException.ThrowIfNull(policies);

        this.policies = policies
            .OrderBy(static policy => policy.Order)
            .ToArray();
        enabledPolicies = this.policies
            .Where(static policy => policy.IsEnabled)
            .ToArray();
    }

    public IReadOnlyList<ResolvedAspNetCoreRateLimitingPolicy> Policies => policies;

    public IReadOnlyList<ResolvedAspNetCoreRateLimitingPolicy> EnabledPolicies => enabledPolicies;

    public bool HasEnabledPolicies => enabledPolicies.Length > 0;

    public RateLimitingEndpointPolicyResolution Resolve(string transportId, string? behaviorId = null)
    {
        var canonicalTransportId = AspNetCoreRateLimitingPolicyResolver.CanonicalizeTransportId(transportId);
        if (canonicalTransportId is null)
        {
            return RateLimitingEndpointPolicyResolution.None;
        }

        var candidates = policies
            .Where(policy => policy.Matches(canonicalTransportId, behaviorId))
            .OrderBy(static policy => policy.Specificity)
            .ThenBy(static policy => policy.Order)
            .ToArray();
        if (candidates.Length == 0)
        {
            return RateLimitingEndpointPolicyResolution.None;
        }

        var selected = candidates[^1];
        return selected.IsEnabled
            ? RateLimitingEndpointPolicyResolution.Require(selected)
            : RateLimitingEndpointPolicyResolution.Disable(selected);
    }
}

internal enum RateLimitingEndpointPolicyMode
{
    None,
    Require,
    Disable
}

internal sealed record RateLimitingEndpointPolicyResolution(
    RateLimitingEndpointPolicyMode Mode,
    ResolvedAspNetCoreRateLimitingPolicy? Policy)
{
    public static RateLimitingEndpointPolicyResolution None { get; } = new(RateLimitingEndpointPolicyMode.None, null);

    public static RateLimitingEndpointPolicyResolution Disable(ResolvedAspNetCoreRateLimitingPolicy policy)
        => new(RateLimitingEndpointPolicyMode.Disable, policy);

    public static RateLimitingEndpointPolicyResolution Require(ResolvedAspNetCoreRateLimitingPolicy policy)
        => new(RateLimitingEndpointPolicyMode.Require, policy);
}

internal sealed record ResolvedAspNetCoreRateLimitingPolicy(
    string Id,
    string DisplayName,
    string Description,
    string ExecutionMode,
    string Scope,
    int RejectionStatusCode,
    IReadOnlyList<string> TransportIds,
    IReadOnlyList<string> BehaviorIds,
    IReadOnlyList<string> ExcludedPathPrefixes,
    RateLimitingSelection Requested,
    RateLimitingSelection Effective,
    IReadOnlyDictionary<string, string> Metadata,
    int Order,
    bool IsOverride)
{
    public bool IsEnabled => string.Equals(
        ExecutionMode,
        AspNetCoreRateLimitingPolicyResolver.EnabledExecutionMode,
        StringComparison.OrdinalIgnoreCase);

    public int Specificity => (BehaviorIds.Count > 0, TransportIds.Count > 0) switch
    {
        (true, true) => 3,
        (true, false) => 2,
        (false, true) => 1,
        _ => 0
    };

    public bool Matches(string transportId, string? behaviorId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transportId);

        if (TransportIds.Count > 0 &&
            !TransportIds.Contains(transportId, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        if (BehaviorIds.Count == 0)
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(behaviorId) &&
            BehaviorIds.Contains(behaviorId.Trim(), StringComparer.OrdinalIgnoreCase);
    }

    public RateLimitingRuntimeDescriptor ToDescriptor()
    {
        return new RateLimitingRuntimeDescriptor(
            Id,
            DisplayName,
            Description,
            ExecutionMode,
            Scope,
            RejectionStatusCode,
            TransportIds,
            ExcludedPathPrefixes,
            Requested,
            Effective,
            Metadata);
    }
}
