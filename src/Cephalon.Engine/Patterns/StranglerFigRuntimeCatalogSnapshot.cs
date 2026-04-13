using Cephalon.Abstractions.Patterns;

namespace Cephalon.Engine.Patterns;

internal sealed class StranglerFigRuntimeCatalogSnapshot : IStranglerFigRuntimeCatalog, IStranglerFigRouter
{
    private readonly StranglerFigRouteDescriptor[] routes;
    private readonly StranglerFigRouteDescriptor[] resolutionOrder;
    private readonly Dictionary<string, StranglerFigRouteDescriptor> routesById;
    private readonly Dictionary<string, IReadOnlyList<StranglerFigRouteDescriptor>> routesBySourceModule;

    public StranglerFigRuntimeCatalogSnapshot(IEnumerable<StranglerFigRouteDescriptor> routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        this.routes = routes
            .OrderBy(static route => route.SourceModuleId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static route => route.PathPrefix, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static route => route.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        resolutionOrder = this.routes
            .OrderByDescending(static route => route.PathPrefix.Length)
            .ThenBy(static route => route.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        routesById = this.routes.ToDictionary(static route => route.Id, StringComparer.OrdinalIgnoreCase);
        routesBySourceModule = this.routes
            .GroupBy(static route => route.SourceModuleId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<StranglerFigRouteDescriptor>)group.ToArray(),
                StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<StranglerFigRouteDescriptor> Routes => routes;

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

            var (selectedTarget, selectedEndpoint, resolutionMode) = SelectTarget(route);
            return ValueTask.FromResult<StranglerFigRouteResolution?>(new StranglerFigRouteResolution(
                RouteId: route.Id,
                RouteDisplayName: route.DisplayName,
                SourceModuleId: route.SourceModuleId,
                RequestedPath: requestedPath,
                RequestedMethod: requestedMethod,
                MatchedPathPrefix: route.PathPrefix,
                SelectedTarget: selectedTarget,
                SelectedEndpoint: selectedEndpoint,
                LegacyEndpoint: route.LegacyEndpoint,
                ModernEndpoint: route.ModernEndpoint,
                ResolutionMode: resolutionMode,
                Metadata: route.Metadata));
        }

        return ValueTask.FromResult<StranglerFigRouteResolution?>(null);
    }

    private static (StranglerFigTarget SelectedTarget, string SelectedEndpoint, string ResolutionMode) SelectTarget(
        StranglerFigRouteDescriptor route)
    {
        var preferredEndpoint = route.PreferredTarget switch
        {
            StranglerFigTarget.Legacy => route.LegacyEndpoint,
            StranglerFigTarget.Modern => route.ModernEndpoint,
            _ => null
        };
        if (!string.IsNullOrWhiteSpace(preferredEndpoint))
        {
            return (route.PreferredTarget, preferredEndpoint, "preferred-target");
        }

        var fallbackTarget = route.PreferredTarget == StranglerFigTarget.Legacy
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

    private static bool MethodMatches(StranglerFigRouteDescriptor route, string requestedMethod)
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
}
