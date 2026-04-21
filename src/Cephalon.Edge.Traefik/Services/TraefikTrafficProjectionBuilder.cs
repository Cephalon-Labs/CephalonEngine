using System.Globalization;
using Cephalon.Abstractions.Technologies;
using Cephalon.Edge.Traefik.Configuration;

namespace Cephalon.Edge.Traefik.Services;

internal static class TraefikTrafficProjectionBuilder
{
    public static IReadOnlyDictionary<string, TraefikIngressRouteProjection> Build(
        TraefikTrafficMaterializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var materializerId = NormalizeRequired(
            options.MaterializerId,
            $"{nameof(TraefikTrafficMaterializerOptions.MaterializerId)} is required.");
        _ = NormalizeRequired(
            options.ProviderId,
            $"{nameof(TraefikTrafficMaterializerOptions.ProviderId)} is required.");

        var defaultRouteNamespace = NormalizeOptional(options.RouteNamespace);
        var defaultEntryPoints = NormalizeValues(options.EntryPoints);
        var projections = new Dictionary<string, TraefikIngressRouteProjection>(StringComparer.OrdinalIgnoreCase);

        foreach (var route in options.Routes)
        {
            ArgumentNullException.ThrowIfNull(route);

            var routeId = NormalizeRequired(
                route.RouteId,
                $"{nameof(TraefikIngressRouteOptions.RouteId)} is required for Traefik traffic materialization.");
            var routeNamespace = NormalizeRequired(
                route.RouteNamespace ?? defaultRouteNamespace,
                $"Traefik traffic route '{routeId}' requires {nameof(TraefikIngressRouteOptions.RouteNamespace)} or a pack-level {nameof(TraefikTrafficMaterializerOptions.RouteNamespace)}.");
            var ingressRouteName = NormalizeOptional(route.IngressRouteName)
                ?? CreateDeterministicRouteName(routeId);
            var matchRule = NormalizeRequired(
                route.MatchRule,
                $"Traefik traffic route '{routeId}' requires {nameof(TraefikIngressRouteOptions.MatchRule)}.");
            var backendServiceName = NormalizeRequired(
                route.BackendServiceName,
                $"Traefik traffic route '{routeId}' requires {nameof(TraefikIngressRouteOptions.BackendServiceName)}.");
            var backendPort = route.BackendPort is > 0
                ? route.BackendPort.Value
                : throw new InvalidOperationException(
                    $"Traefik traffic route '{routeId}' requires a positive {nameof(TraefikIngressRouteOptions.BackendPort)}.");

            if (route.BackendWeight is <= 0)
            {
                throw new InvalidOperationException(
                    $"Traefik traffic route '{routeId}' must use a positive {nameof(TraefikIngressRouteOptions.BackendWeight)} when a weight is configured.");
            }

            var entryPoints = NormalizeValues(route.EntryPoints);
            if (entryPoints.Length == 0)
            {
                entryPoints = defaultEntryPoints;
            }

            var middlewares = route.Middlewares
                .Select(middleware => TraefikMiddlewareProjection.Create(middleware, routeNamespace, routeId))
                .ToArray();

            var projection = new TraefikIngressRouteProjection(
                routeId,
                routeNamespace,
                ingressRouteName,
                entryPoints,
                matchRule,
                route.Priority,
                middlewares,
                NormalizeOptional(route.BackendNamespace) ?? routeNamespace,
                backendServiceName,
                backendPort,
                route.BackendWeight,
                NormalizeOptional(route.BackendScheme),
                route.PassHostHeader,
                NormalizeOptional(route.TlsSecretName),
                NormalizeOptional(route.TlsOptionsName),
                NormalizeOptional(route.TlsOptionsNamespace) ?? routeNamespace);

            if (!projections.TryAdd(routeId, projection))
            {
                throw new InvalidOperationException(
                    $"Traefik traffic materializer '{materializerId}' declares duplicate projections for route '{routeId}'.");
            }
        }

        return projections;
    }

    private static string NormalizeRequired(string? value, string message)
    {
        var normalized = NormalizeOptional(value);
        return normalized ?? throw new InvalidOperationException(message);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string[] NormalizeValues(IEnumerable<string>? values)
    {
        return values?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }

    private static string CreateDeterministicRouteName(string routeId)
    {
        Span<char> buffer = stackalloc char[routeId.Length];
        var length = 0;
        var previousWasHyphen = false;

        foreach (var character in routeId.Trim().ToLowerInvariant())
        {
            if (char.IsAsciiLetterOrDigit(character))
            {
                if (length < buffer.Length)
                {
                    buffer[length++] = character;
                }

                previousWasHyphen = false;
                continue;
            }

            if (previousWasHyphen || length == 0)
            {
                continue;
            }

            if (length < buffer.Length)
            {
                buffer[length++] = '-';
            }

            previousWasHyphen = true;
        }

        while (length > 0 && buffer[length - 1] == '-')
        {
            length--;
        }

        if (length == 0)
        {
            return "cell-traffic-route";
        }

        return new string(buffer[..length]);
    }
}

internal sealed record TraefikMiddlewareProjection(
    string Name,
    string Namespace)
{
    public static TraefikMiddlewareProjection Create(
        TraefikMiddlewareReferenceOptions options,
        string routeNamespace,
        string routeId)
    {
        ArgumentNullException.ThrowIfNull(options);

        var name = string.IsNullOrWhiteSpace(options.Name)
            ? throw new InvalidOperationException(
                $"Traefik traffic route '{routeId}' declares a middleware reference without a name.")
            : options.Name.Trim();
        var @namespace = string.IsNullOrWhiteSpace(options.Namespace)
            ? routeNamespace
            : options.Namespace.Trim();

        return new TraefikMiddlewareProjection(name, @namespace);
    }

    public string Reference => $"middleware/{Namespace}/{Name}";
}

internal sealed record TraefikIngressRouteProjection(
    string RouteId,
    string RouteNamespace,
    string IngressRouteName,
    IReadOnlyList<string> EntryPoints,
    string MatchRule,
    int? Priority,
    IReadOnlyList<TraefikMiddlewareProjection> Middlewares,
    string BackendNamespace,
    string BackendServiceName,
    int BackendPort,
    int? BackendWeight,
    string? BackendScheme,
    bool? PassHostHeader,
    string? TlsSecretName,
    string? TlsOptionsName,
    string TlsOptionsNamespace)
{
    public const string TraefikApiGroup = "traefik.io";
    public const string TraefikResourceVersion = "v1alpha1";
    public const string TraefikApiVersion = $"{TraefikApiGroup}/{TraefikResourceVersion}";
    public const string ConfiguredIntentStatusSource = "configured-intent";

    public string ProviderRouteId => $"ingressroute/{RouteNamespace}/{IngressRouteName}";

    public string BackendReference =>
        BackendWeight is > 0
            ? $"service/{BackendNamespace}/{BackendServiceName}:{BackendPort.ToString(CultureInfo.InvariantCulture)}@weight/{BackendWeight.Value.ToString(CultureInfo.InvariantCulture)}"
            : $"service/{BackendNamespace}/{BackendServiceName}:{BackendPort.ToString(CultureInfo.InvariantCulture)}";

    public string MiddlewareReferences => string.Join(",", Middlewares.Select(static middleware => middleware.Reference));

    public string? TlsOptionsReference =>
        string.IsNullOrWhiteSpace(TlsOptionsName)
            ? null
            : $"tlsoption/{TlsOptionsNamespace}/{TlsOptionsName}";

    public TraefikIngressRouteResource CreateIngressRouteResource(
        CellTrafficAutomationRuntimeDescriptor automation,
        string? resourceVersion = null,
        TraefikIngressRouteResource? existing = null)
    {
        ArgumentNullException.ThrowIfNull(automation);

        var labels = existing?.Metadata?.Labels is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(existing.Metadata.Labels, StringComparer.OrdinalIgnoreCase);
        var annotations = existing?.Metadata?.Annotations is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(existing.Metadata.Annotations, StringComparer.OrdinalIgnoreCase);

        labels[TraefikOwnership.ManagedByLabel] = TraefikOwnership.ManagedByValue;
        annotations[TraefikOwnership.AutomationIdAnnotation] = automation.Id;
        annotations[TraefikOwnership.RouteIdAnnotation] = automation.RouteId;
        annotations[TraefikOwnership.SourceModuleIdAnnotation] = automation.SourceModuleId;

        return new TraefikIngressRouteResource
        {
            ApiVersion = TraefikApiVersion,
            Kind = "IngressRoute",
            Metadata = new k8s.Models.V1ObjectMeta
            {
                Name = IngressRouteName,
                NamespaceProperty = RouteNamespace,
                ResourceVersion = resourceVersion,
                Labels = labels,
                Annotations = annotations
            },
            Spec = new TraefikIngressRouteSpec
            {
                EntryPoints = EntryPoints.Count == 0 ? null : [.. EntryPoints],
                Routes =
                [
                    new TraefikIngressRouteRoute
                    {
                        Match = MatchRule,
                        Kind = "Rule",
                        Priority = Priority,
                        Middlewares = Middlewares.Count == 0
                            ? null
                            : [.. Middlewares.Select(middleware => new TraefikIngressRouteMiddlewareReference
                            {
                                Name = middleware.Name,
                                NamespaceProperty = StringComparer.OrdinalIgnoreCase.Equals(middleware.Namespace, RouteNamespace)
                                    ? null
                                    : middleware.Namespace
                            })],
                        Services =
                        [
                            new TraefikIngressRouteServiceReference
                            {
                                Name = BackendServiceName,
                                NamespaceProperty = StringComparer.OrdinalIgnoreCase.Equals(BackendNamespace, RouteNamespace)
                                    ? null
                                    : BackendNamespace,
                                Port = BackendPort,
                                Weight = BackendWeight,
                                Scheme = BackendScheme,
                                PassHostHeader = PassHostHeader
                            }
                        ]
                    }
                ],
                Tls = string.IsNullOrWhiteSpace(TlsSecretName) && string.IsNullOrWhiteSpace(TlsOptionsName)
                    ? null
                    : new TraefikIngressRouteTls
                    {
                        SecretName = TlsSecretName,
                        Options = string.IsNullOrWhiteSpace(TlsOptionsName)
                            ? null
                            : new TraefikIngressRouteTlsOptionsReference
                            {
                                Name = TlsOptionsName,
                                NamespaceProperty = StringComparer.OrdinalIgnoreCase.Equals(TlsOptionsNamespace, RouteNamespace)
                                    ? null
                                    : TlsOptionsNamespace
                            }
                    }
            }
        };
    }

    public Dictionary<string, string> CreateMetadata()
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["traefikApiVersion"] = TraefikApiVersion,
            ["resourceKind"] = "IngressRoute",
            ["ingressRouteNamespace"] = RouteNamespace,
            ["ingressRouteName"] = IngressRouteName,
            ["providerRouteId"] = ProviderRouteId,
            ["ingressRouteResourceId"] = ProviderRouteId,
            ["entryPoints"] = string.Join(",", EntryPoints),
            ["entryPointCount"] = EntryPoints.Count.ToString(CultureInfo.InvariantCulture),
            ["matchRule"] = MatchRule,
            ["routeKind"] = "Rule",
            ["middlewareRefs"] = MiddlewareReferences,
            ["middlewareRefCount"] = Middlewares.Count.ToString(CultureInfo.InvariantCulture),
            ["serviceRefs"] = BackendReference,
            ["serviceRefCount"] = "1",
            ["statusSource"] = ConfiguredIntentStatusSource
        };

        if (Priority is not null)
        {
            metadata["routePriority"] = Priority.Value.ToString(CultureInfo.InvariantCulture);
        }

        if (!string.IsNullOrWhiteSpace(BackendScheme))
        {
            metadata["backendScheme"] = BackendScheme!;
        }

        if (PassHostHeader is not null)
        {
            metadata["backendPassHostHeader"] = PassHostHeader.Value ? "true" : "false";
        }

        if (!string.IsNullOrWhiteSpace(TlsSecretName))
        {
            metadata["tlsSecretName"] = TlsSecretName!;
        }

        if (TlsOptionsReference is not null)
        {
            metadata["tlsOptionsRef"] = TlsOptionsReference;
        }

        return metadata;
    }
}
