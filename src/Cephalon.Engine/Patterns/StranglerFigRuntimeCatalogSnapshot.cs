using Cephalon.Abstractions.Patterns;
using Cephalon.Engine.Configuration;

namespace Cephalon.Engine.Patterns;

internal sealed class StranglerFigRuntimeCatalogSnapshot :
    IStranglerFigRuntimeCatalog,
    IStranglerFigMigrationRuntimeCatalog,
    IStranglerFigRouter
{
    private readonly StranglerFigRouteDescriptor[] routes;
    private readonly StranglerFigMigrationRuntimeDescriptor[] runtimeRoutes;
    private readonly StranglerFigMigrationRuntimeDescriptor[] resolutionOrder;
    private readonly Dictionary<string, StranglerFigRouteDescriptor> routesById;
    private readonly Dictionary<string, IReadOnlyList<StranglerFigRouteDescriptor>> routesBySourceModule;
    private readonly Dictionary<string, StranglerFigMigrationRuntimeDescriptor> runtimeRoutesById;
    private readonly Dictionary<string, IReadOnlyList<StranglerFigMigrationRuntimeDescriptor>> runtimeRoutesBySourceModule;

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
    }

    public IReadOnlyList<StranglerFigRouteDescriptor> Routes => routes;

    IReadOnlyList<StranglerFigMigrationRuntimeDescriptor> IStranglerFigMigrationRuntimeCatalog.Routes => runtimeRoutes;

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
