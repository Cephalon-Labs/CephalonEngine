using Cephalon.Abstractions.Patterns;
using Cephalon.Engine.Configuration;

namespace Cephalon.Engine.Patterns;

internal sealed class StranglerFigRuntimeCatalogSnapshot :
    IStranglerFigRuntimeCatalog,
    IStranglerFigMigrationRuntimeCatalog,
    IStranglerFigIngressRuntimeCatalog,
    IStranglerFigRouter
{
    private const string AbsoluteUriEndpointKind = "absolute-uri";
    private const string LocalPathEndpointKind = "local-path";
    private const string OpaqueEndpointKind = "opaque";
    private const string PassThroughIngressMode = "pass-through";
    private const string RewriteLocalPathIngressMode = "rewrite-local-path";
    private const string ProxyAbsoluteUriIngressMode = "proxy-absolute-uri";
    private const string OpaqueEndpointIngressMode = "opaque-endpoint";

    private readonly StranglerFigRouteDescriptor[] routes;
    private readonly StranglerFigMigrationRuntimeDescriptor[] runtimeRoutes;
    private readonly StranglerFigIngressRuntimeDescriptor[] ingressRoutes;
    private readonly StranglerFigMigrationRuntimeDescriptor[] resolutionOrder;
    private readonly Dictionary<string, StranglerFigRouteDescriptor> routesById;
    private readonly Dictionary<string, IReadOnlyList<StranglerFigRouteDescriptor>> routesBySourceModule;
    private readonly Dictionary<string, StranglerFigMigrationRuntimeDescriptor> runtimeRoutesById;
    private readonly Dictionary<string, IReadOnlyList<StranglerFigMigrationRuntimeDescriptor>> runtimeRoutesBySourceModule;
    private readonly Dictionary<string, StranglerFigIngressRuntimeDescriptor> ingressRoutesById;
    private readonly Dictionary<string, IReadOnlyList<StranglerFigIngressRuntimeDescriptor>> ingressRoutesBySourceModule;

    public StranglerFigRuntimeCatalogSnapshot(
        IEnumerable<StranglerFigRouteDescriptor> routes,
        StranglerFigMigrationSettings? migrationSettings = null)
    {
        ArgumentNullException.ThrowIfNull(routes);

        this.routes = routes
            .OrderBy(static route => route.SourceModuleId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static route => route.PathPrefix, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static route => route.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        routesById = this.routes.ToDictionary(static route => route.Id, StringComparer.OrdinalIgnoreCase);
        routesBySourceModule = this.routes
            .GroupBy(static route => route.SourceModuleId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<StranglerFigRouteDescriptor>)group.ToArray(),
                StringComparer.OrdinalIgnoreCase);

        var effectiveMigrationSettings = migrationSettings ?? StranglerFigMigrationSettings.Empty;
        var routePoliciesById = effectiveMigrationSettings.Routes.ToDictionary(
            static route => route.RouteId,
            StringComparer.OrdinalIgnoreCase);
        ValidateKnownPolicyRoutes(routePoliciesById.Keys);

        runtimeRoutes = this.routes
            .Select(route => CreateRuntimeDescriptor(route, effectiveMigrationSettings, routePoliciesById))
            .OrderBy(static route => route.SourceModuleId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static route => route.PathPrefix, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static route => route.RouteId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        ingressRoutes = runtimeRoutes
            .Select(CreateIngressDescriptor)
            .OrderBy(static route => route.SourceModuleId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static route => route.PathPrefix, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static route => route.RouteId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        resolutionOrder = runtimeRoutes
            .OrderByDescending(static route => route.PathPrefix.Length)
            .ThenBy(static route => route.RouteId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        runtimeRoutesById = runtimeRoutes.ToDictionary(static route => route.RouteId, StringComparer.OrdinalIgnoreCase);
        runtimeRoutesBySourceModule = runtimeRoutes
            .GroupBy(static route => route.SourceModuleId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<StranglerFigMigrationRuntimeDescriptor>)group.ToArray(),
                StringComparer.OrdinalIgnoreCase);
        ingressRoutesById = ingressRoutes.ToDictionary(static route => route.RouteId, StringComparer.OrdinalIgnoreCase);
        ingressRoutesBySourceModule = ingressRoutes
            .GroupBy(static route => route.SourceModuleId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<StranglerFigIngressRuntimeDescriptor>)group.ToArray(),
                StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<StranglerFigRouteDescriptor> Routes => routes;

    IReadOnlyList<StranglerFigMigrationRuntimeDescriptor> IStranglerFigMigrationRuntimeCatalog.Routes => runtimeRoutes;

    IReadOnlyList<StranglerFigIngressRuntimeDescriptor> IStranglerFigIngressRuntimeCatalog.Routes => ingressRoutes;

    public StranglerFigRouteDescriptor? GetById(string routeId)
    {
        if (string.IsNullOrWhiteSpace(routeId))
        {
            return null;
        }

        return routesById.TryGetValue(routeId.Trim(), out var route)
            ? route
            : null;
    }

    StranglerFigMigrationRuntimeDescriptor? IStranglerFigMigrationRuntimeCatalog.GetById(string routeId)
    {
        if (string.IsNullOrWhiteSpace(routeId))
        {
            return null;
        }

        return runtimeRoutesById.TryGetValue(routeId.Trim(), out var route)
            ? route
            : null;
    }

    StranglerFigIngressRuntimeDescriptor? IStranglerFigIngressRuntimeCatalog.GetById(string routeId)
    {
        if (string.IsNullOrWhiteSpace(routeId))
        {
            return null;
        }

        return ingressRoutesById.TryGetValue(routeId.Trim(), out var route)
            ? route
            : null;
    }

    public IReadOnlyList<StranglerFigRouteDescriptor> GetBySourceModule(string sourceModuleId)
    {
        if (string.IsNullOrWhiteSpace(sourceModuleId))
        {
            return [];
        }

        return routesBySourceModule.TryGetValue(sourceModuleId.Trim(), out var matches)
            ? matches
            : [];
    }

    IReadOnlyList<StranglerFigMigrationRuntimeDescriptor> IStranglerFigMigrationRuntimeCatalog.GetBySourceModule(string sourceModuleId)
    {
        if (string.IsNullOrWhiteSpace(sourceModuleId))
        {
            return [];
        }

        return runtimeRoutesBySourceModule.TryGetValue(sourceModuleId.Trim(), out var matches)
            ? matches
            : [];
    }

    IReadOnlyList<StranglerFigIngressRuntimeDescriptor> IStranglerFigIngressRuntimeCatalog.GetBySourceModule(string sourceModuleId)
    {
        if (string.IsNullOrWhiteSpace(sourceModuleId))
        {
            return [];
        }

        return ingressRoutesBySourceModule.TryGetValue(sourceModuleId.Trim(), out var matches)
            ? matches
            : [];
    }

    public ValueTask<StranglerFigRouteResolution?> ResolveAsync(
        StranglerFigRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        cancellationToken.ThrowIfCancellationRequested();

        var requestedPath = NormalizePath(request.Path);
        var requestedMethod = request.Method.Trim().ToUpperInvariant();
        for (var index = 0; index < resolutionOrder.Length; index++)
        {
            var route = resolutionOrder[index];
            if (!MethodMatches(route, requestedMethod) || !PathMatches(requestedPath, route.PathPrefix))
            {
                continue;
            }

            return ValueTask.FromResult<StranglerFigRouteResolution?>(new StranglerFigRouteResolution(
                RouteId: route.RouteId,
                RouteDisplayName: route.DisplayName,
                SourceModuleId: route.SourceModuleId,
                RequestedPath: requestedPath,
                RequestedMethod: requestedMethod,
                MatchedPathPrefix: route.PathPrefix,
                SelectedTarget: route.EffectiveTarget,
                SelectedEndpoint: route.SelectedEndpoint,
                LegacyEndpoint: route.LegacyEndpoint,
                ModernEndpoint: route.ModernEndpoint,
                ResolutionMode: BuildResolutionMode(route),
                Metadata: BuildResolutionMetadata(route)));
        }

        return ValueTask.FromResult<StranglerFigRouteResolution?>(null);
    }

    private static StranglerFigMigrationRuntimeDescriptor CreateRuntimeDescriptor(
        StranglerFigRouteDescriptor route,
        StranglerFigMigrationSettings migrationSettings,
        Dictionary<string, StranglerFigRoutePolicySettings> routePoliciesById)
    {
        var routePolicy = routePoliciesById.TryGetValue(route.Id, out var configuredRoutePolicy)
            ? configuredRoutePolicy
            : null;
        var (requestedTarget, requestedTargetSource) = ResolveRequestedTarget(route, migrationSettings, routePolicy);
        var (effectiveTarget, selectedEndpoint, selectionMode) = SelectTarget(route, requestedTarget);
        var progressState = routePolicy?.ProgressState
            ?? migrationSettings.DefaultProgressState
            ?? "not-started";
        var progressPercent = routePolicy?.ProgressPercent
            ?? migrationSettings.DefaultProgressPercent
            ?? 0;

        var runtimeMetadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(routePolicy?.Notes))
        {
            runtimeMetadata["note"] = routePolicy.Notes!;
        }

        return new StranglerFigMigrationRuntimeDescriptor(
            routeId: route.Id,
            sourceModuleId: route.SourceModuleId,
            displayName: route.DisplayName,
            description: route.Description,
            pathPrefix: route.PathPrefix,
            authoredTarget: route.PreferredTarget,
            requestedTarget: requestedTarget,
            effectiveTarget: effectiveTarget,
            requestedTargetSource: requestedTargetSource,
            selectionMode: selectionMode,
            selectedEndpoint: selectedEndpoint,
            legacyEndpoint: route.LegacyEndpoint,
            modernEndpoint: route.ModernEndpoint,
            methods: route.Methods,
            progressState: progressState,
            progressPercent: progressPercent,
            metadata: route.Metadata,
            runtimeMetadata: runtimeMetadata);
    }

    private static StranglerFigIngressRuntimeDescriptor CreateIngressDescriptor(
        StranglerFigMigrationRuntimeDescriptor route)
    {
        var endpoint = ResolveIngressEndpoint(route.SelectedEndpoint);
        var ingressMode = ResolveIngressMode(route, endpoint);

        return new StranglerFigIngressRuntimeDescriptor(
            routeId: route.RouteId,
            sourceModuleId: route.SourceModuleId,
            displayName: route.DisplayName,
            description: route.Description,
            pathPrefix: route.PathPrefix,
            requestedTarget: route.RequestedTarget,
            effectiveTarget: route.EffectiveTarget,
            requestedTargetSource: route.RequestedTargetSource,
            selectionMode: route.SelectionMode,
            selectedEndpoint: route.SelectedEndpoint,
            selectedEndpointKind: endpoint.Kind,
            ingressMode: ingressMode,
            canMaterialize: !string.Equals(ingressMode, OpaqueEndpointIngressMode, StringComparison.OrdinalIgnoreCase),
            targetPathPrefix: endpoint.LocalPath,
            targetQuery: endpoint.Query,
            targetUri: endpoint.AbsoluteUri?.AbsoluteUri,
            methods: route.Methods,
            progressState: route.ProgressState,
            progressPercent: route.ProgressPercent,
            metadata: route.Metadata,
            runtimeMetadata: route.RuntimeMetadata);
    }

    private void ValidateKnownPolicyRoutes(IReadOnlyCollection<string> configuredRouteIds)
    {
        var unknownRouteIds = configuredRouteIds
            .Where(routeId => !routesById.ContainsKey(routeId))
            .OrderBy(static routeId => routeId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (unknownRouteIds.Length > 0)
        {
            throw new InvalidOperationException(
                $"Strangler-fig migration policy references unknown route ids: {string.Join(", ", unknownRouteIds)}.");
        }
    }

    private static (StranglerFigTarget RequestedTarget, string RequestedTargetSource) ResolveRequestedTarget(
        StranglerFigRouteDescriptor route,
        StranglerFigMigrationSettings migrationSettings,
        StranglerFigRoutePolicySettings? routePolicy)
    {
        if (routePolicy?.Target is StranglerFigTarget routeTarget)
        {
            return (routeTarget, "migration-route");
        }

        if (migrationSettings.DefaultTarget is StranglerFigTarget defaultTarget)
        {
            return (defaultTarget, "migration-default");
        }

        return (route.PreferredTarget, "authored-route");
    }

    private static (StranglerFigTarget EffectiveTarget, string SelectedEndpoint, string SelectionMode) SelectTarget(
        StranglerFigRouteDescriptor route,
        StranglerFigTarget requestedTarget)
    {
        var requestedEndpoint = requestedTarget switch
        {
            StranglerFigTarget.Legacy => route.LegacyEndpoint,
            StranglerFigTarget.Modern => route.ModernEndpoint,
            _ => null
        };
        if (!string.IsNullOrWhiteSpace(requestedEndpoint))
        {
            return (requestedTarget, requestedEndpoint, "requested-target");
        }

        var fallbackTarget = requestedTarget == StranglerFigTarget.Legacy
            ? StranglerFigTarget.Modern
            : StranglerFigTarget.Legacy;
        var fallbackEndpoint = fallbackTarget == StranglerFigTarget.Legacy
            ? route.LegacyEndpoint
            : route.ModernEndpoint;

        if (!string.IsNullOrWhiteSpace(fallbackEndpoint))
        {
            return (fallbackTarget, fallbackEndpoint, "fallback-target");
        }

        throw new InvalidOperationException(
            $"Strangler-fig route '{route.Id}' does not expose a usable legacy or modern endpoint.");
    }

    private static Dictionary<string, string> BuildResolutionMetadata(
        StranglerFigMigrationRuntimeDescriptor route)
    {
        var metadata = new Dictionary<string, string>(route.Metadata, StringComparer.OrdinalIgnoreCase)
        {
            ["migrationRequestedTarget"] = ToTargetValue(route.RequestedTarget),
            ["migrationRequestedTargetSource"] = route.RequestedTargetSource,
            ["migrationSelectionMode"] = route.SelectionMode,
            ["migrationProgressState"] = route.ProgressState,
            ["migrationProgressPercent"] = route.ProgressPercent.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };

        if (route.RuntimeMetadata.TryGetValue("note", out var note) &&
            !string.IsNullOrWhiteSpace(note))
        {
            metadata["migrationNote"] = note;
        }

        return metadata;
    }

    private static string BuildResolutionMode(StranglerFigMigrationRuntimeDescriptor route)
    {
        var configured = !string.Equals(route.RequestedTargetSource, "authored-route", StringComparison.OrdinalIgnoreCase);
        return (configured, route.SelectionMode) switch
        {
            (true, "requested-target") => "configured-target",
            (true, "fallback-target") => "configured-fallback-target",
            (_, "fallback-target") => "fallback-target",
            _ => "preferred-target"
        };
    }

    private static string ResolveIngressMode(
        StranglerFigMigrationRuntimeDescriptor route,
        ResolvedEndpoint endpoint)
    {
        return endpoint.Kind switch
        {
            LocalPathEndpointKind when PathsEquivalent(endpoint.LocalPath, route.PathPrefix) &&
                string.IsNullOrEmpty(endpoint.Query) => PassThroughIngressMode,
            LocalPathEndpointKind => RewriteLocalPathIngressMode,
            AbsoluteUriEndpointKind => ProxyAbsoluteUriIngressMode,
            _ => OpaqueEndpointIngressMode
        };
    }

    private static string ToTargetValue(StranglerFigTarget target)
    {
        return target switch
        {
            StranglerFigTarget.Legacy => "legacy",
            StranglerFigTarget.Modern => "modern",
            _ => target.ToString().ToLowerInvariant()
        };
    }

    private static bool MethodMatches(StranglerFigMigrationRuntimeDescriptor route, string requestedMethod)
    {
        return route.Methods.Count == 0 ||
            route.Methods.Contains(requestedMethod, StringComparer.OrdinalIgnoreCase);
    }

    private static bool PathMatches(string requestedPath, string pathPrefix)
    {
        if (string.Equals(pathPrefix, "/", StringComparison.Ordinal))
        {
            return true;
        }

        return string.Equals(requestedPath, pathPrefix, StringComparison.OrdinalIgnoreCase) ||
            requestedPath.StartsWith(pathPrefix + "/", StringComparison.OrdinalIgnoreCase);
    }

    private static ResolvedEndpoint ResolveIngressEndpoint(string selectedEndpoint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(selectedEndpoint);

        if (Uri.TryCreate(selectedEndpoint, UriKind.Absolute, out var absoluteUri))
        {
            if (string.Equals(absoluteUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(absoluteUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                return new ResolvedEndpoint(
                    Kind: AbsoluteUriEndpointKind,
                    LocalPath: NormalizePath(absoluteUri.AbsolutePath),
                    Query: NormalizeOptionalQuery(absoluteUri.Query),
                    AbsoluteUri: absoluteUri);
            }

            return new ResolvedEndpoint(
                Kind: OpaqueEndpointKind,
                LocalPath: null,
                Query: null,
                AbsoluteUri: null);
        }

        if (!selectedEndpoint.StartsWith('/'))
        {
            return new ResolvedEndpoint(
                Kind: OpaqueEndpointKind,
                LocalPath: null,
                Query: null,
                AbsoluteUri: null);
        }

        var normalized = selectedEndpoint.Trim();
        var hashIndex = normalized.IndexOf('#');
        if (hashIndex >= 0)
        {
            normalized = normalized[..hashIndex];
        }

        var queryIndex = normalized.IndexOf('?');
        var path = queryIndex >= 0
            ? normalized[..queryIndex]
            : normalized;
        var query = queryIndex >= 0
            ? normalized[queryIndex..]
            : null;

        return new ResolvedEndpoint(
            Kind: LocalPathEndpointKind,
            LocalPath: NormalizePath(path),
            Query: NormalizeOptionalQuery(query),
            AbsoluteUri: null);
    }

    private static bool PathsEquivalent(string? left, string? right)
    {
        return string.Equals(
            NormalizePath(left ?? "/"),
            NormalizePath(right ?? "/"),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizePath(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Request path is required.", nameof(value));
        }

        var normalized = value.Trim();
        if (Uri.TryCreate(normalized, UriKind.Absolute, out var absoluteUri))
        {
            normalized = absoluteUri.AbsolutePath;
        }

        var separatorIndex = normalized.IndexOfAny(['?', '#']);
        if (separatorIndex >= 0)
        {
            normalized = normalized[..separatorIndex];
        }

        if (!normalized.StartsWith('/'))
        {
            normalized = "/" + normalized.TrimStart('/');
        }

        normalized = normalized.Length > 1
            ? normalized.TrimEnd('/')
            : normalized;

        return string.IsNullOrWhiteSpace(normalized)
            ? "/"
            : normalized;
    }

    private static string? NormalizeOptionalQuery(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || string.Equals(trimmed, "?", StringComparison.Ordinal))
        {
            return null;
        }

        return trimmed.StartsWith('?')
            ? trimmed
            : "?" + trimmed;
    }

    private sealed record ResolvedEndpoint(
        string Kind,
        string? LocalPath,
        string? Query,
        Uri? AbsoluteUri);
}
