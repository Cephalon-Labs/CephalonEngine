using System.Globalization;
using System.Net;
using System.Text.Json.Serialization;
using Cephalon.Abstractions.Technologies;
using Cephalon.Edge.Traefik.Configuration;
using k8s;
using k8s.Autorest;
using k8s.Models;

namespace Cephalon.Edge.Traefik.Services;

internal sealed class TraefikTrafficObservationSource(
    TraefikTrafficMaterializerOptions options,
    TimeProvider timeProvider,
    IKubernetes? providedClient = null) : ITraefikTrafficObservationSource, ITraefikTrafficApplyService, IDisposable
{
    private const string LiveStatusSource = "traefik-ingressroute-observation";
    private const string ObservationUnavailableStatusSource = "observation-unavailable";
    private const string ObservationErrorStatusSource = "observation-error";
    private const string ApplyStatusSource = "control-plane-apply";
    private const string ApplyUnavailableStatusSource = "apply-unavailable";
    private const string ApplyErrorStatusSource = "apply-error";
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

    private readonly TraefikTrafficObservationOptions observationOptions = options.Observation;
    private Kubernetes? ownedClient;
    private bool disposed;

    public async ValueTask<CellTrafficAutomationProviderMaterializationResult> ApplyAsync(
        CellTrafficAutomationRuntimeDescriptor automation,
        TraefikIngressRouteProjection projection,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(automation);
        ArgumentNullException.ThrowIfNull(projection);

        var observedAtUtc = timeProvider.GetUtcNow();
        var metadata = projection.CreateMetadata();
        metadata["providerAction"] = TraefikTrafficObservationModes.ApplyAndReconcile;
        metadata["observationMode"] = TraefikTrafficObservationModes.ApplyAndReconcile;
        metadata["statusSource"] = ApplyStatusSource;
        metadata["ownershipState"] = CellTrafficAutomationOwnershipStates.Requested;
        metadata["dependencyState"] = CellTrafficAutomationDependencyStates.Unknown;
        metadata["driftState"] = CellTrafficAutomationDriftStates.Unknown;
        metadata["driftReasons"] = string.Empty;
        metadata["missingMiddlewareRefs"] = string.Empty;
        metadata["dependencyMissingRefs"] = string.Empty;
        metadata["lifecycleAction"] = CellTrafficAutomationLifecycleActions.Reconcile;
        metadata["ingressRouteWriteAction"] = "none";

        try
        {
            var client = GetClient();
            if (client is null)
            {
                metadata["statusSource"] = ApplyUnavailableStatusSource;
                metadata["resourceState"] = "client-unavailable";

                return new CellTrafficAutomationProviderMaterializationResult(
                    CellTrafficAutomationProviderMaterializationStates.Failed,
                    observedAtUtc,
                    "Apply-and-reconcile Traefik materialization requires either a registered IKubernetes client, an explicit kubeconfig path, or in-cluster configuration.",
                    metadata);
            }

            var ingressRoute = await TryReadIngressRouteAsync(client, projection, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(ingressRoute.Error))
            {
                metadata["statusSource"] = ApplyErrorStatusSource;
                metadata["resourceState"] = "ingressroute-read-failed";

                return new CellTrafficAutomationProviderMaterializationResult(
                    CellTrafficAutomationProviderMaterializationStates.Failed,
                    observedAtUtc,
                    ingressRoute.Error,
                    metadata);
            }

            metadata["ingressRouteExists"] = ingressRoute.Resource is null ? "false" : "true";

            if (ingressRoute.Resource is not null)
            {
                ApplyObservedIngressRouteMetadata(metadata, ingressRoute.Resource);
                var ownership = EvaluateApplyOwnership(ingressRoute.Resource, automation);
                ApplyOwnershipMetadata(metadata, ingressRoute.Resource, ownership);
                if (ownership.IsConflict)
                {
                    metadata["statusSource"] = ApplyErrorStatusSource;
                    metadata["resourceState"] = "ownership-conflict";
                    metadata["driftState"] = CellTrafficAutomationDriftStates.Unknown;
                    metadata["driftReasons"] = string.Empty;
                    metadata["ingressRouteWriteAction"] = "blocked";

                    return new CellTrafficAutomationProviderMaterializationResult(
                        CellTrafficAutomationProviderMaterializationStates.Failed,
                        observedAtUtc,
                        ownership.Error,
                        metadata);
                }
            }

            using var ingressRouteClient = new GenericClient(
                client,
                TraefikIngressRouteProjection.TraefikApiGroup,
                TraefikIngressRouteProjection.TraefikResourceVersion,
                "ingressroutes",
                disposeClient: false);
            var desiredRoute = projection.CreateIngressRouteResource(
                automation,
                ingressRoute.Resource?.Metadata?.ResourceVersion,
                ingressRoute.Resource);
            TraefikIngressRouteResource persistedRoute;
            if (ingressRoute.Resource is null)
            {
                persistedRoute = await ingressRouteClient.CreateNamespacedAsync(
                    desiredRoute,
                    projection.RouteNamespace,
                    cancellationToken).ConfigureAwait(false);
                metadata["ingressRouteWriteAction"] = "created";
                metadata["lifecycleAction"] = CellTrafficAutomationLifecycleActions.Create;
            }
            else
            {
                persistedRoute = await ingressRouteClient.ReplaceNamespacedAsync(
                    desiredRoute,
                    projection.RouteNamespace,
                    projection.IngressRouteName,
                    cancellationToken).ConfigureAwait(false);
                metadata["ingressRouteWriteAction"] = "replaced";
                metadata["lifecycleAction"] = CellTrafficAutomationLifecycleActions.Replace;
            }

            metadata["resourceState"] = "write-succeeded";
            metadata["ingressRouteExists"] = "true";
            metadata["ownershipState"] = CellTrafficAutomationOwnershipStates.Owned;
            metadata["driftState"] = CellTrafficAutomationDriftStates.Reconciling;
            metadata["driftReasons"] = string.Empty;
            ApplyObservedIngressRouteMetadata(metadata, persistedRoute);
            ApplyOwnershipMetadata(
                metadata,
                persistedRoute,
                new OwnershipEvaluation(CellTrafficAutomationOwnershipStates.Owned, false, null));

            if (!string.IsNullOrWhiteSpace(persistedRoute.Metadata?.ResourceVersion))
            {
                metadata["ingressRouteAppliedResourceVersion"] = persistedRoute.Metadata.ResourceVersion!;
            }

            if (persistedRoute.Metadata?.Generation is not null)
            {
                metadata["ingressRouteAppliedGeneration"] =
                    persistedRoute.Metadata.Generation.Value.ToString(CultureInfo.InvariantCulture);
            }

            return new CellTrafficAutomationProviderMaterializationResult(
                CellTrafficAutomationProviderMaterializationStates.Pending,
                observedAtUtc,
                metadata: metadata);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (HttpOperationException exception)
        {
            metadata["statusSource"] = ApplyErrorStatusSource;
            metadata["resourceState"] = "apply-error";
            metadata["ingressRouteWriteAction"] = "failed";

            return new CellTrafficAutomationProviderMaterializationResult(
                CellTrafficAutomationProviderMaterializationStates.Failed,
                observedAtUtc,
                string.IsNullOrWhiteSpace(exception.Message)
                    ? "The Traefik Kubernetes CRD API rejected the IngressRoute apply request."
                    : exception.Message,
                metadata);
        }
        catch (Exception exception)
        {
            metadata["statusSource"] = ApplyErrorStatusSource;
            metadata["resourceState"] = "apply-error";
            metadata["ingressRouteWriteAction"] = "failed";

            return new CellTrafficAutomationProviderMaterializationResult(
                CellTrafficAutomationProviderMaterializationStates.Failed,
                observedAtUtc,
                exception.Message,
                metadata);
        }
    }

    public async ValueTask<CellTrafficAutomationProviderMaterializationResult> ObserveAsync(
        CellTrafficAutomationRuntimeDescriptor automation,
        TraefikIngressRouteProjection projection,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(automation);
        ArgumentNullException.ThrowIfNull(projection);

        var observedAtUtc = timeProvider.GetUtcNow();
        var metadata = projection.CreateMetadata();
        ApplyObservationMetadata(metadata, observedAtUtc);

        try
        {
            var client = GetClient();
            if (client is null)
            {
                metadata["statusSource"] = ObservationUnavailableStatusSource;
                metadata["resourceState"] = "client-unavailable";
                metadata["ownershipState"] = CellTrafficAutomationOwnershipStates.Unknown;
                metadata["dependencyState"] = CellTrafficAutomationDependencyStates.Unknown;
                metadata["driftState"] = CellTrafficAutomationDriftStates.Unknown;
                metadata["driftReasons"] = string.Empty;
                metadata["dependencyMissingRefs"] = string.Empty;

                return new CellTrafficAutomationProviderMaterializationResult(
                    CellTrafficAutomationProviderMaterializationStates.Failed,
                    observedAtUtc,
                    "Observe-only Traefik materialization requires either a registered IKubernetes client, an explicit kubeconfig path, or in-cluster configuration.",
                    metadata);
            }

            var ingressRoute = await TryReadIngressRouteAsync(client, projection, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(ingressRoute.Error))
            {
                metadata["statusSource"] = ObservationErrorStatusSource;
                metadata["resourceState"] = "ingressroute-read-failed";
                metadata["ownershipState"] = CellTrafficAutomationOwnershipStates.Unknown;
                metadata["dependencyState"] = CellTrafficAutomationDependencyStates.Unknown;
                metadata["driftState"] = CellTrafficAutomationDriftStates.Unknown;
                metadata["driftReasons"] = string.Empty;
                metadata["dependencyMissingRefs"] = string.Empty;

                return new CellTrafficAutomationProviderMaterializationResult(
                    CellTrafficAutomationProviderMaterializationStates.Failed,
                    observedAtUtc,
                    ingressRoute.Error,
                    metadata);
            }

            metadata["statusSource"] = LiveStatusSource;
            metadata["ingressRouteExists"] = ingressRoute.Resource is null ? "false" : "true";

            if (ingressRoute.Resource is null)
            {
                metadata["resourceState"] = "missing-ingressroute";
                metadata["ownershipState"] = CellTrafficAutomationOwnershipStates.Requested;
                metadata["dependencyState"] = CellTrafficAutomationDependencyStates.Unknown;
                metadata["driftState"] = CellTrafficAutomationDriftStates.Unknown;
                metadata["driftReasons"] = string.Empty;
                metadata["dependencyMissingRefs"] = string.Empty;

                return new CellTrafficAutomationProviderMaterializationResult(
                    CellTrafficAutomationProviderMaterializationStates.Pending,
                    observedAtUtc,
                    metadata: metadata);
            }

            ApplyObservedIngressRouteMetadata(metadata, ingressRoute.Resource);
            var ownership = EvaluateOwnership(ingressRoute.Resource, automation);
            ApplyOwnershipMetadata(metadata, ingressRoute.Resource, ownership);

            var dependencies = await EvaluateDependenciesAsync(client, projection, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(dependencies.Error))
            {
                metadata["statusSource"] = ObservationErrorStatusSource;
                metadata["resourceState"] = dependencies.ResourceState;
                metadata["ownershipState"] = ownership.State;
                metadata["dependencyState"] = CellTrafficAutomationDependencyStates.Unknown;
                metadata["driftState"] = CellTrafficAutomationDriftStates.Unknown;
                metadata["driftReasons"] = string.Empty;
                metadata["dependencyMissingRefs"] = string.Empty;

                return new CellTrafficAutomationProviderMaterializationResult(
                    CellTrafficAutomationProviderMaterializationStates.Failed,
                    observedAtUtc,
                    dependencies.Error,
                    metadata);
            }

            ApplyDependencyMetadata(metadata, dependencies);

            var driftReasons = DetectDriftReasons(ingressRoute.Resource, projection);
            metadata["resourceState"] = "available";
            metadata["driftState"] = driftReasons.Count == 0
                ? CellTrafficAutomationDriftStates.InSync
                : CellTrafficAutomationDriftStates.Drifted;
            metadata["driftReasons"] = string.Join(",", driftReasons);

            if (ownership.IsConflict)
            {
                metadata["resourceState"] = "ownership-conflict";
                return new CellTrafficAutomationProviderMaterializationResult(
                    CellTrafficAutomationProviderMaterializationStates.Failed,
                    observedAtUtc,
                    ownership.Error,
                    metadata);
            }

            if (dependencies.MissingRefs.Count > 0)
            {
                return new CellTrafficAutomationProviderMaterializationResult(
                    CellTrafficAutomationProviderMaterializationStates.Failed,
                    observedAtUtc,
                    $"Observed Traefik dependencies are missing: {string.Join(", ", dependencies.MissingRefs)}.",
                    metadata);
            }

            if (driftReasons.Count > 0)
            {
                return new CellTrafficAutomationProviderMaterializationResult(
                    CellTrafficAutomationProviderMaterializationStates.Failed,
                    observedAtUtc,
                    $"Observed Traefik IngressRoute '{projection.ProviderRouteId}' drifts from the projected Cephalon traffic intent.",
                    metadata);
            }

            return new CellTrafficAutomationProviderMaterializationResult(
                CellTrafficAutomationProviderMaterializationStates.Applied,
                observedAtUtc,
                metadata: metadata);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            metadata["statusSource"] = ObservationErrorStatusSource;
            metadata["resourceState"] = "observation-error";
            metadata["ownershipState"] = CellTrafficAutomationOwnershipStates.Unknown;
            metadata["dependencyState"] = CellTrafficAutomationDependencyStates.Unknown;
            metadata["driftState"] = CellTrafficAutomationDriftStates.Unknown;
            metadata["driftReasons"] = string.Empty;
            metadata["dependencyMissingRefs"] = string.Empty;

            return new CellTrafficAutomationProviderMaterializationResult(
                CellTrafficAutomationProviderMaterializationStates.Failed,
                observedAtUtc,
                exception.Message,
                metadata);
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        ownedClient?.Dispose();
    }

    private IKubernetes? GetClient()
    {
        if (providedClient is not null)
        {
            return providedClient;
        }

        if (ownedClient is not null)
        {
            return ownedClient;
        }

        var configuration = CreateClientConfiguration();
        if (configuration is null)
        {
            return null;
        }

        ownedClient = new Kubernetes(configuration);
        return ownedClient;
    }

    private KubernetesClientConfiguration? CreateClientConfiguration()
    {
        if (observationOptions.UseInClusterConfiguration)
        {
            return KubernetesClientConfiguration.InClusterConfig();
        }

        if (string.IsNullOrWhiteSpace(observationOptions.KubeConfigPath))
        {
            return null;
        }

        return KubernetesClientConfiguration.BuildConfigFromConfigFile(
            kubeconfigPath: observationOptions.KubeConfigPath.Trim(),
            currentContext: NormalizeOptional(observationOptions.KubeContext),
            masterUrl: NormalizeOptional(observationOptions.MasterUrl));
    }

    private static async Task<DependencyEvaluation> EvaluateDependenciesAsync(
        IKubernetes client,
        TraefikIngressRouteProjection projection,
        CancellationToken cancellationToken)
    {
        var missingRefs = new List<string>();

        var service = await TryReadServiceAsync(
            client,
            projection.BackendNamespace,
            projection.BackendServiceName,
            cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(service.Error))
        {
            return DependencyEvaluation.FromError("service-read-failed", service.Error);
        }

        var serviceExists = service.Resource is not null;
        if (!serviceExists)
        {
            missingRefs.Add(projection.BackendReference);
        }

        var missingMiddlewareRefs = new List<string>();
        foreach (var middleware in projection.Middlewares)
        {
            var middlewareResult = await TryReadTraefikResourceAsync(
                client,
                "middlewares",
                middleware.Namespace,
                middleware.Name,
                cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(middlewareResult.Error))
            {
                return DependencyEvaluation.FromError("middleware-read-failed", middlewareResult.Error);
            }

            if (middlewareResult.Resource is null)
            {
                var middlewareRef = middleware.Reference;
                missingRefs.Add(middlewareRef);
                missingMiddlewareRefs.Add(middlewareRef);
            }
        }

        var tlsOptionsExists = true;
        var tlsOptionsRef = projection.TlsOptionsReference;
        if (!string.IsNullOrWhiteSpace(projection.TlsOptionsName))
        {
            var tlsOptions = await TryReadTraefikResourceAsync(
                client,
                "tlsoptions",
                projection.TlsOptionsNamespace,
                projection.TlsOptionsName!,
                cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(tlsOptions.Error))
            {
                return DependencyEvaluation.FromError("tlsoption-read-failed", tlsOptions.Error);
            }

            tlsOptionsExists = tlsOptions.Resource is not null;
            if (!tlsOptionsExists && tlsOptionsRef is not null)
            {
                missingRefs.Add(tlsOptionsRef);
            }
        }

        var tlsSecretExists = true;
        if (!string.IsNullOrWhiteSpace(projection.TlsSecretName))
        {
            var secret = await TryReadSecretAsync(
                client,
                projection.RouteNamespace,
                projection.TlsSecretName!,
                cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(secret.Error))
            {
                return DependencyEvaluation.FromError("secret-read-failed", secret.Error);
            }

            tlsSecretExists = secret.Resource is not null;
            if (!tlsSecretExists)
            {
                missingRefs.Add($"secret/{projection.RouteNamespace}/{projection.TlsSecretName}");
            }
        }

        return new DependencyEvaluation(
            serviceExists,
            tlsOptionsExists,
            tlsSecretExists,
            missingMiddlewareRefs,
            missingRefs,
            null,
            "available");
    }

    private void ApplyObservationMetadata(
        Dictionary<string, string> metadata,
        DateTimeOffset observedAtUtc)
    {
        metadata["providerAction"] = TraefikTrafficObservationModes.ObserveOnly;
        metadata["observationMode"] = TraefikTrafficObservationModes.Normalize(observationOptions.Mode);
        metadata["observationPollingIntervalSeconds"] =
            Math.Max(1, observationOptions.PollingIntervalSeconds).ToString(CultureInfo.InvariantCulture);
        metadata["observationStaleAfterSeconds"] =
            Math.Max(1, observationOptions.StaleAfterSeconds).ToString(CultureInfo.InvariantCulture);
        metadata["observationFreshUntilUtc"] = observedAtUtc
            .AddSeconds(Math.Max(1, observationOptions.StaleAfterSeconds))
            .ToString("O", CultureInfo.InvariantCulture);
        metadata["lifecycleAction"] = CellTrafficAutomationLifecycleActions.Observe;
    }

    private static void ApplyObservedIngressRouteMetadata(
        Dictionary<string, string> metadata,
        TraefikIngressRouteResource ingressRoute)
    {
        metadata["observedIngressRouteName"] = ingressRoute.Metadata?.Name ?? string.Empty;
        metadata["observedIngressRouteNamespace"] = ingressRoute.Metadata?.NamespaceProperty ?? string.Empty;
        metadata["observedIngressRouteGeneration"] =
            ingressRoute.Metadata?.Generation?.ToString(CultureInfo.InvariantCulture) ?? "0";

        var observedEntryPoints = NormalizeValues(ingressRoute.Spec?.EntryPoints);
        metadata["observedEntryPoints"] = string.Join(",", observedEntryPoints);
        metadata["observedEntryPointCount"] = observedEntryPoints.Count.ToString(CultureInfo.InvariantCulture);

        var routes = ingressRoute.Spec?.Routes ?? [];
        metadata["observedRouteCount"] = routes.Count.ToString(CultureInfo.InvariantCulture);

        if (routes.Count == 0)
        {
            metadata["observedMatchRule"] = string.Empty;
            metadata["observedRouteKind"] = string.Empty;
            metadata["observedMiddlewareRefs"] = string.Empty;
            metadata["observedServiceRefs"] = string.Empty;
            metadata["observedRoutePriority"] = string.Empty;
            metadata["observedBackendScheme"] = string.Empty;
            metadata["observedBackendPassHostHeader"] = string.Empty;
        }
        else
        {
            var route = routes[0];
            metadata["observedMatchRule"] = NormalizeOptional(route.Match) ?? string.Empty;
            metadata["observedRouteKind"] = NormalizeOptional(route.Kind) ?? "Rule";
            metadata["observedMiddlewareRefs"] = string.Join(
                ",",
                NormalizeMiddlewareReferences(route.Middlewares, ingressRoute.Metadata?.NamespaceProperty));
            metadata["observedServiceRefs"] = string.Join(
                ",",
                NormalizeServiceReferences(route.Services, ingressRoute.Metadata?.NamespaceProperty));

            if (route.Priority is not null)
            {
                metadata["observedRoutePriority"] = route.Priority.Value.ToString(CultureInfo.InvariantCulture);
            }

            if (route.Services is not null && route.Services.Count > 0)
            {
                var service = route.Services[0];
                metadata["observedBackendScheme"] = NormalizeOptional(service.Scheme) ?? string.Empty;
                metadata["observedBackendPassHostHeader"] = NormalizeOptional(ToBooleanLiteral(service.PassHostHeader)) ?? string.Empty;
            }
            else
            {
                metadata["observedBackendScheme"] = string.Empty;
                metadata["observedBackendPassHostHeader"] = string.Empty;
            }
        }

        metadata["observedTlsSecretName"] = NormalizeOptional(ingressRoute.Spec?.Tls?.SecretName) ?? string.Empty;
        metadata["observedTlsOptionsRef"] = NormalizeTlsOptionsReference(ingressRoute.Spec?.Tls?.Options, ingressRoute.Metadata?.NamespaceProperty) ?? string.Empty;
    }

    private static void ApplyOwnershipMetadata(
        Dictionary<string, string> metadata,
        TraefikIngressRouteResource ingressRoute,
        OwnershipEvaluation ownership)
    {
        metadata["ownershipState"] = ownership.State;
        metadata["managedBy"] = ReadMetadataValue(ingressRoute.Metadata?.Labels, TraefikOwnership.ManagedByLabel) ?? string.Empty;
        metadata["observedAutomationId"] = ReadMetadataValue(ingressRoute.Metadata?.Annotations, TraefikOwnership.AutomationIdAnnotation) ?? string.Empty;
        metadata["observedRouteId"] = ReadMetadataValue(ingressRoute.Metadata?.Annotations, TraefikOwnership.RouteIdAnnotation) ?? string.Empty;
        metadata["observedSourceModuleId"] = ReadMetadataValue(ingressRoute.Metadata?.Annotations, TraefikOwnership.SourceModuleIdAnnotation) ?? string.Empty;
    }

    private static void ApplyDependencyMetadata(
        Dictionary<string, string> metadata,
        DependencyEvaluation dependencies)
    {
        metadata["dependencyState"] = dependencies.MissingRefs.Count == 0
            ? CellTrafficAutomationDependencyStates.Satisfied
            : CellTrafficAutomationDependencyStates.Missing;
        metadata["backendServiceExists"] = dependencies.ServiceExists ? "true" : "false";
        metadata["tlsOptionsExists"] = dependencies.TlsOptionsExists ? "true" : "false";
        metadata["tlsSecretExists"] = dependencies.TlsSecretExists ? "true" : "false";
        metadata["dependencyMissingRefs"] = string.Join(",", dependencies.MissingRefs);
        metadata["missingMiddlewareRefs"] = string.Join(",", dependencies.MissingMiddlewareRefs);
    }

    private static List<string> DetectDriftReasons(
        TraefikIngressRouteResource ingressRoute,
        TraefikIngressRouteProjection projection)
    {
        var reasons = new List<string>();
        var observedEntryPoints = NormalizeValues(ingressRoute.Spec?.EntryPoints);
        if (!observedEntryPoints.SequenceEqual(projection.EntryPoints, Comparer))
        {
            reasons.Add("entry-points");
        }

        var routes = ingressRoute.Spec?.Routes ?? [];
        if (routes.Count != 1)
        {
            reasons.Add("route-count");
            return reasons;
        }

        var route = routes[0];
        if (!Comparer.Equals(NormalizeOptional(route.Match), NormalizeOptional(projection.MatchRule)))
        {
            reasons.Add("match-rule");
        }

        var observedKind = NormalizeOptional(route.Kind) ?? "Rule";
        if (!Comparer.Equals(observedKind, "Rule"))
        {
            reasons.Add("route-kind");
        }

        if (route.Priority != projection.Priority)
        {
            reasons.Add("route-priority");
        }

        var observedMiddlewareRefs = NormalizeMiddlewareReferences(route.Middlewares, ingressRoute.Metadata?.NamespaceProperty);
        var expectedMiddlewareRefs = projection.Middlewares
            .Select(static middleware => middleware.Reference)
            .OrderBy(static value => value, Comparer)
            .ToList();
        if (!observedMiddlewareRefs.SequenceEqual(expectedMiddlewareRefs, Comparer))
        {
            reasons.Add("middleware-refs");
        }

        var observedServiceRefs = NormalizeServiceReferences(route.Services, ingressRoute.Metadata?.NamespaceProperty);
        if (!observedServiceRefs.SequenceEqual([projection.BackendReference], Comparer))
        {
            reasons.Add("service-refs");
        }

        var observedService = route.Services?.Count > 0 ? route.Services[0] : null;
        if (!Comparer.Equals(
                NormalizeOptional(observedService?.Scheme),
                NormalizeOptional(projection.BackendScheme)))
        {
            reasons.Add("backend-scheme");
        }

        if (!Comparer.Equals(
                NormalizeOptional(ToBooleanLiteral(observedService?.PassHostHeader)),
                NormalizeOptional(ToBooleanLiteral(projection.PassHostHeader))))
        {
            reasons.Add("backend-pass-host-header");
        }

        if (!Comparer.Equals(
                NormalizeOptional(ingressRoute.Spec?.Tls?.SecretName),
                NormalizeOptional(projection.TlsSecretName)))
        {
            reasons.Add("tls-secret");
        }

        if (!Comparer.Equals(
                NormalizeTlsOptionsReference(ingressRoute.Spec?.Tls?.Options, ingressRoute.Metadata?.NamespaceProperty),
                NormalizeOptional(projection.TlsOptionsReference)))
        {
            reasons.Add("tls-options");
        }

        return reasons;
    }

    private static async Task<ResourceReadResult<TraefikIngressRouteResource>> TryReadIngressRouteAsync(
        IKubernetes client,
        TraefikIngressRouteProjection projection,
        CancellationToken cancellationToken)
    {
        try
        {
            using var ingressRouteClient = new GenericClient(
                client,
                TraefikIngressRouteProjection.TraefikApiGroup,
                TraefikIngressRouteProjection.TraefikResourceVersion,
                "ingressroutes",
                disposeClient: false);
            var ingressRoute = await ingressRouteClient.ReadNamespacedAsync<TraefikIngressRouteResource>(
                projection.RouteNamespace,
                projection.IngressRouteName,
                cancellationToken).ConfigureAwait(false);

            return new ResourceReadResult<TraefikIngressRouteResource>(ingressRoute, null);
        }
        catch (HttpOperationException exception) when (IsNotFound(exception))
        {
            return new ResourceReadResult<TraefikIngressRouteResource>(null, null);
        }
        catch (HttpOperationException exception)
        {
            return new ResourceReadResult<TraefikIngressRouteResource>(
                null,
                $"Failed to read Traefik IngressRoute '{projection.ProviderRouteId}': {(string.IsNullOrWhiteSpace(exception.Message) ? "Unknown Kubernetes API error." : exception.Message)}");
        }
    }

    private static async Task<ResourceReadResult<TraefikResourceStub>> TryReadTraefikResourceAsync(
        IKubernetes client,
        string pluralName,
        string resourceNamespace,
        string resourceName,
        CancellationToken cancellationToken)
    {
        try
        {
            using var resourceClient = new GenericClient(
                client,
                TraefikIngressRouteProjection.TraefikApiGroup,
                TraefikIngressRouteProjection.TraefikResourceVersion,
                pluralName,
                disposeClient: false);
            var resource = await resourceClient.ReadNamespacedAsync<TraefikResourceStub>(
                resourceNamespace,
                resourceName,
                cancellationToken).ConfigureAwait(false);

            return new ResourceReadResult<TraefikResourceStub>(resource, null);
        }
        catch (HttpOperationException exception) when (IsNotFound(exception))
        {
            return new ResourceReadResult<TraefikResourceStub>(null, null);
        }
        catch (HttpOperationException exception)
        {
            return new ResourceReadResult<TraefikResourceStub>(
                null,
                $"Failed to read Traefik resource '{pluralName}/{resourceNamespace}/{resourceName}': {(string.IsNullOrWhiteSpace(exception.Message) ? "Unknown Kubernetes API error." : exception.Message)}");
        }
    }

    private static async Task<ResourceReadResult<V1Service>> TryReadServiceAsync(
        IKubernetes client,
        string resourceNamespace,
        string resourceName,
        CancellationToken cancellationToken)
    {
        try
        {
            var resource = await client.CoreV1.ReadNamespacedServiceAsync(
                resourceName,
                resourceNamespace,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return new ResourceReadResult<V1Service>(resource, null);
        }
        catch (HttpOperationException exception) when (IsNotFound(exception))
        {
            return new ResourceReadResult<V1Service>(null, null);
        }
        catch (HttpOperationException exception)
        {
            return new ResourceReadResult<V1Service>(
                null,
                $"Failed to read Service 'service/{resourceNamespace}/{resourceName}': {(string.IsNullOrWhiteSpace(exception.Message) ? "Unknown Kubernetes API error." : exception.Message)}");
        }
    }

    private static async Task<ResourceReadResult<V1Secret>> TryReadSecretAsync(
        IKubernetes client,
        string resourceNamespace,
        string resourceName,
        CancellationToken cancellationToken)
    {
        try
        {
            var resource = await client.CoreV1.ReadNamespacedSecretAsync(
                resourceName,
                resourceNamespace,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return new ResourceReadResult<V1Secret>(resource, null);
        }
        catch (HttpOperationException exception) when (IsNotFound(exception))
        {
            return new ResourceReadResult<V1Secret>(null, null);
        }
        catch (HttpOperationException exception)
        {
            return new ResourceReadResult<V1Secret>(
                null,
                $"Failed to read Secret 'secret/{resourceNamespace}/{resourceName}': {(string.IsNullOrWhiteSpace(exception.Message) ? "Unknown Kubernetes API error." : exception.Message)}");
        }
    }

    private static OwnershipEvaluation EvaluateOwnership(
        TraefikIngressRouteResource ingressRoute,
        CellTrafficAutomationRuntimeDescriptor automation)
    {
        var managedBy = ReadMetadataValue(ingressRoute.Metadata?.Labels, TraefikOwnership.ManagedByLabel);
        var observedAutomationId = ReadMetadataValue(ingressRoute.Metadata?.Annotations, TraefikOwnership.AutomationIdAnnotation);
        var observedRouteId = ReadMetadataValue(ingressRoute.Metadata?.Annotations, TraefikOwnership.RouteIdAnnotation);
        var observedSourceModuleId = ReadMetadataValue(ingressRoute.Metadata?.Annotations, TraefikOwnership.SourceModuleIdAnnotation);

        if (!string.IsNullOrWhiteSpace(managedBy) &&
            !Comparer.Equals(managedBy, TraefikOwnership.ManagedByValue))
        {
            return new OwnershipEvaluation(
                CellTrafficAutomationOwnershipStates.OwnershipConflict,
                true,
                $"Traefik IngressRoute '{ingressRoute.Metadata?.Name ?? automation.RouteId}' is already marked as managed by '{managedBy}'.");
        }

        if (!string.IsNullOrWhiteSpace(observedAutomationId) &&
            !Comparer.Equals(observedAutomationId, automation.Id))
        {
            return new OwnershipEvaluation(
                CellTrafficAutomationOwnershipStates.OwnershipConflict,
                true,
                $"Traefik IngressRoute '{ingressRoute.Metadata?.Name ?? automation.RouteId}' is already owned by Cephalon automation '{observedAutomationId}' and cannot be assigned to '{automation.Id}'.");
        }

        if (!string.IsNullOrWhiteSpace(observedRouteId) &&
            !Comparer.Equals(observedRouteId, automation.RouteId))
        {
            return new OwnershipEvaluation(
                CellTrafficAutomationOwnershipStates.OwnershipConflict,
                true,
                $"Traefik IngressRoute '{ingressRoute.Metadata?.Name ?? automation.RouteId}' is tagged for route '{observedRouteId}' instead of '{automation.RouteId}'.");
        }

        if (!string.IsNullOrWhiteSpace(observedSourceModuleId) &&
            !Comparer.Equals(observedSourceModuleId, automation.SourceModuleId))
        {
            return new OwnershipEvaluation(
                CellTrafficAutomationOwnershipStates.OwnershipConflict,
                true,
                $"Traefik IngressRoute '{ingressRoute.Metadata?.Name ?? automation.RouteId}' is tagged for source module '{observedSourceModuleId}' instead of '{automation.SourceModuleId}'.");
        }

        if (!string.IsNullOrWhiteSpace(managedBy) ||
            !string.IsNullOrWhiteSpace(observedAutomationId) ||
            !string.IsNullOrWhiteSpace(observedRouteId) ||
            !string.IsNullOrWhiteSpace(observedSourceModuleId))
        {
            return new OwnershipEvaluation(CellTrafficAutomationOwnershipStates.Owned, false, null);
        }

        return new OwnershipEvaluation(CellTrafficAutomationOwnershipStates.Requested, false, null);
    }

    private static OwnershipEvaluation EvaluateApplyOwnership(
        TraefikIngressRouteResource ingressRoute,
        CellTrafficAutomationRuntimeDescriptor automation)
    {
        var ownership = EvaluateOwnership(ingressRoute, automation);
        if (ownership.IsConflict)
        {
            return ownership;
        }

        if (Comparer.Equals(ownership.State, CellTrafficAutomationOwnershipStates.Owned))
        {
            return ownership;
        }

        return new OwnershipEvaluation(
            CellTrafficAutomationOwnershipStates.OwnershipConflict,
            true,
            $"Traefik IngressRoute '{ingressRoute.Metadata?.Name ?? automation.RouteId}' already exists but is not marked as owned by Cephalon automation '{automation.Id}'.");
    }

    private static List<string> NormalizeValues(IReadOnlyList<string>? values)
    {
        return values?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(Comparer)
            .OrderBy(static value => value, Comparer)
            .ToList() ?? [];
    }

    private static List<string> NormalizeMiddlewareReferences(
        IReadOnlyList<TraefikIngressRouteMiddlewareReference>? middlewares,
        string? routeNamespace)
    {
        return middlewares?
            .Select(middleware =>
            {
                var middlewareNamespace = NormalizeOptional(middleware.NamespaceProperty) ?? NormalizeOptional(routeNamespace) ?? string.Empty;
                var middlewareName = NormalizeOptional(middleware.Name) ?? string.Empty;
                return $"middleware/{middlewareNamespace}/{middlewareName}";
            })
            .Where(static value => !string.IsNullOrWhiteSpace(value) && !value.EndsWith('/'))
            .Distinct(Comparer)
            .OrderBy(static value => value, Comparer)
            .ToList() ?? [];
    }

    private static List<string> NormalizeServiceReferences(
        IReadOnlyList<TraefikIngressRouteServiceReference>? services,
        string? routeNamespace)
    {
        return services?
            .Select(service =>
            {
                var serviceNamespace = NormalizeOptional(service.NamespaceProperty) ?? NormalizeOptional(routeNamespace) ?? string.Empty;
                var serviceName = NormalizeOptional(service.Name) ?? string.Empty;
                var port = service.Port?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
                return service.Weight is > 0
                    ? $"service/{serviceNamespace}/{serviceName}:{port}@weight/{service.Weight.Value.ToString(CultureInfo.InvariantCulture)}"
                    : $"service/{serviceNamespace}/{serviceName}:{port}";
            })
            .Where(static value => !string.IsNullOrWhiteSpace(value) && !value.EndsWith(":/", StringComparison.Ordinal))
            .Distinct(Comparer)
            .OrderBy(static value => value, Comparer)
            .ToList() ?? [];
    }

    private static string? NormalizeTlsOptionsReference(
        TraefikIngressRouteTlsOptionsReference? options,
        string? routeNamespace)
    {
        var optionsName = NormalizeOptional(options?.Name);
        if (optionsName is null)
        {
            return null;
        }

        var optionsNamespace = NormalizeOptional(options?.NamespaceProperty) ?? NormalizeOptional(routeNamespace) ?? string.Empty;
        return $"tlsoption/{optionsNamespace}/{optionsName}";
    }

    private static bool IsNotFound(HttpOperationException exception) =>
        exception.Response is not null &&
        exception.Response.StatusCode == HttpStatusCode.NotFound;

    private static string? ReadMetadataValue(
        IDictionary<string, string>? values,
        string key)
    {
        if (values is null)
        {
            return null;
        }

        foreach (var pair in values)
        {
            if (Comparer.Equals(pair.Key, key))
            {
                return NormalizeOptional(pair.Value);
            }
        }

        return null;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? ToBooleanLiteral(bool? value) => value switch
    {
        true => "true",
        false => "false",
        _ => null
    };

    private sealed record ResourceReadResult<T>(T? Resource, string? Error)
        where T : class;

    private sealed record OwnershipEvaluation(string State, bool IsConflict, string? Error);

    private sealed record DependencyEvaluation(
        bool ServiceExists,
        bool TlsOptionsExists,
        bool TlsSecretExists,
        IReadOnlyList<string> MissingMiddlewareRefs,
        IReadOnlyList<string> MissingRefs,
        string? Error,
        string ResourceState)
    {
        public static DependencyEvaluation FromError(string resourceState, string error) =>
            new(
                ServiceExists: false,
                TlsOptionsExists: false,
                TlsSecretExists: false,
                MissingMiddlewareRefs: [],
                MissingRefs: [],
                Error: error,
                ResourceState: resourceState);
    }
}

internal sealed class TraefikIngressRouteResource : KubernetesObject
{
    public V1ObjectMeta? Metadata { get; set; }

    public TraefikIngressRouteSpec? Spec { get; set; }
}

internal sealed class TraefikIngressRouteSpec
{
    public IReadOnlyList<string>? EntryPoints { get; set; }

    public IReadOnlyList<TraefikIngressRouteRoute>? Routes { get; set; }

    public TraefikIngressRouteTls? Tls { get; set; }
}

internal sealed class TraefikIngressRouteRoute
{
    public string? Match { get; set; }

    public string? Kind { get; set; }

    public int? Priority { get; set; }

    public IReadOnlyList<TraefikIngressRouteMiddlewareReference>? Middlewares { get; set; }

    public IReadOnlyList<TraefikIngressRouteServiceReference>? Services { get; set; }
}

internal sealed class TraefikIngressRouteMiddlewareReference
{
    public string? Name { get; set; }

    [JsonPropertyName("namespace")]
    public string? NamespaceProperty { get; set; }
}

internal sealed class TraefikIngressRouteServiceReference
{
    public string? Name { get; set; }

    [JsonPropertyName("namespace")]
    public string? NamespaceProperty { get; set; }

    public int? Port { get; set; }

    public int? Weight { get; set; }

    public string? Scheme { get; set; }

    public bool? PassHostHeader { get; set; }
}

internal sealed class TraefikIngressRouteTls
{
    public string? SecretName { get; set; }

    public TraefikIngressRouteTlsOptionsReference? Options { get; set; }
}

internal sealed class TraefikIngressRouteTlsOptionsReference
{
    public string? Name { get; set; }

    [JsonPropertyName("namespace")]
    public string? NamespaceProperty { get; set; }
}

internal sealed class TraefikResourceStub : KubernetesObject
{
    public V1ObjectMeta? Metadata { get; set; }
}

internal static class TraefikOwnership
{
    public const string ManagedByLabel = "cephalon.io/managed-by";
    public const string ManagedByValue = "edge-traefik";
    public const string AutomationIdAnnotation = "cephalon.io/cell-traffic-automation-id";
    public const string RouteIdAnnotation = "cephalon.io/cell-route-id";
    public const string SourceModuleIdAnnotation = "cephalon.io/source-module-id";
}
