using System.Globalization;
using System.Net;
using Cephalon.Abstractions.Technologies;
using Cephalon.Edge.KubernetesGateway.Configuration;
using k8s;
using k8s.Autorest;

namespace Cephalon.Edge.KubernetesGateway.Services;

internal sealed class KubernetesGatewayTrafficObservationSource(
    KubernetesGatewayTrafficMaterializerOptions options,
    TimeProvider timeProvider,
    IKubernetes? providedClient = null,
    Func<ICellTrafficAutomationRuntimeCatalog>? runtimeCatalogAccessor = null) : IKubernetesGatewayTrafficObservationSource, IKubernetesGatewayTrafficApplyService, IDisposable
{
    private const string LiveStatusSource = "gateway-api-status";
    private const string ObservationUnavailableStatusSource = "observation-unavailable";
    private const string ObservationErrorStatusSource = "observation-error";
    private const string ApplyStatusSource = "control-plane-apply";
    private const string ApplyUnavailableStatusSource = "apply-unavailable";
    private const string ApplyErrorStatusSource = "apply-error";
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

    private readonly KubernetesGatewayTrafficObservationOptions observationOptions = options.Observation;
    private Kubernetes? ownedClient;
    private bool disposed;

    public async ValueTask<CellTrafficAutomationProviderMaterializationResult> ApplyAsync(
        CellTrafficAutomationRuntimeDescriptor automation,
        KubernetesGatewayTrafficRouteProjection projection,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(automation);
        ArgumentNullException.ThrowIfNull(projection);

        var observedAtUtc = timeProvider.GetUtcNow();
        var metadata = projection.CreateMetadata();
        metadata["providerAction"] = KubernetesGatewayTrafficObservationModes.ApplyAndReconcile;
        metadata["observationMode"] = KubernetesGatewayTrafficObservationModes.ApplyAndReconcile;
        metadata["statusSource"] = ApplyStatusSource;
        metadata["ownershipState"] = CellTrafficAutomationOwnershipStates.Requested;
        metadata["dependencyState"] = CellTrafficAutomationDependencyStates.Unknown;
        metadata["driftState"] = CellTrafficAutomationDriftStates.Unknown;
        metadata["driftReasons"] = string.Empty;
        metadata["lifecycleAction"] = CellTrafficAutomationLifecycleActions.Reconcile;
        metadata["gatewayWriteAction"] = "none";
        metadata["gatewayWriteReason"] = "preprovisioned-dependency";
        metadata["httpRouteWriteAction"] = "none";

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
                    "Apply-and-reconcile Kubernetes Gateway materialization requires either a registered IKubernetes client, an explicit kubeconfig path, or in-cluster configuration.",
                    metadata);
            }

            var httpRoute = await TryReadHttpRouteAsync(client, projection, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(httpRoute.Error))
            {
                metadata["statusSource"] = ApplyErrorStatusSource;
                metadata["resourceState"] = "httproute-read-failed";

                return new CellTrafficAutomationProviderMaterializationResult(
                    CellTrafficAutomationProviderMaterializationStates.Failed,
                    observedAtUtc,
                    httpRoute.Error,
                    metadata);
            }

            OwnershipEvaluation? ownership = null;
            if (httpRoute.Resource is not null)
            {
                ownership = EvaluateOwnership(httpRoute.Resource, automation);
                ApplyOwnershipMetadata(metadata, ownership);
                if (ownership.IsConflict)
                {
                    metadata["statusSource"] = ApplyErrorStatusSource;
                    metadata["resourceState"] = "ownership-conflict";
                    metadata["httpRouteWriteAction"] = "blocked";

                    return new CellTrafficAutomationProviderMaterializationResult(
                        CellTrafficAutomationProviderMaterializationStates.Failed,
                        observedAtUtc,
                        ownership.Error,
                        metadata);
                }
            }
            else
            {
                metadata["ownershipState"] = CellTrafficAutomationOwnershipStates.Requested;
            }

            using var httpRouteClient = new GenericClient(
                client,
                KubernetesGatewayTrafficRouteProjection.GatewayApiGroup,
                KubernetesGatewayTrafficRouteProjection.GatewayApiVersion,
                "httproutes",
                disposeClient: false);
            var desiredRoute = projection.CreateHttpRouteResource(
                automation,
                httpRoute.Resource?.Metadata?.ResourceVersion,
                httpRoute.Resource);
            KubernetesGatewayHttpRouteResource persistedRoute;
            if (httpRoute.Resource is null)
            {
                persistedRoute = await httpRouteClient.CreateNamespacedAsync(
                    desiredRoute,
                    projection.RouteNamespace,
                    cancellationToken).ConfigureAwait(false);
                metadata["httpRouteWriteAction"] = "created";
                metadata["lifecycleAction"] = CellTrafficAutomationLifecycleActions.Create;
            }
            else
            {
                persistedRoute = await httpRouteClient.ReplaceNamespacedAsync(
                    desiredRoute,
                    projection.RouteNamespace,
                    projection.HttpRouteName,
                    cancellationToken).ConfigureAwait(false);
                metadata["httpRouteWriteAction"] = "replaced";
                metadata["lifecycleAction"] = ownership?.CanTransfer == true
                    ? CellTrafficAutomationLifecycleActions.Transfer
                    : CellTrafficAutomationLifecycleActions.Replace;
            }

            metadata["resourceState"] = "write-succeeded";
            metadata["dependencyState"] = CellTrafficAutomationDependencyStates.Satisfied;
            metadata["driftState"] = CellTrafficAutomationDriftStates.Reconciling;
            metadata["driftReasons"] = string.Empty;
            metadata["ownershipState"] = CellTrafficAutomationOwnershipStates.Owned;
            ApplyTransferMetadata(metadata, ownership);
            if (!string.IsNullOrWhiteSpace(persistedRoute.Metadata?.ResourceVersion))
            {
                metadata["httpRouteAppliedResourceVersion"] = persistedRoute.Metadata.ResourceVersion!;
            }

            if (persistedRoute.Metadata?.Generation is not null)
            {
                metadata["httpRouteAppliedGeneration"] =
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
            metadata["httpRouteWriteAction"] = "failed";

            return new CellTrafficAutomationProviderMaterializationResult(
                CellTrafficAutomationProviderMaterializationStates.Failed,
                observedAtUtc,
                string.IsNullOrWhiteSpace(exception.Message)
                    ? "The Kubernetes Gateway API rejected the HTTPRoute apply request."
                    : exception.Message,
                metadata);
        }
        catch (Exception exception)
        {
            metadata["statusSource"] = ApplyErrorStatusSource;
            metadata["resourceState"] = "apply-error";
            metadata["httpRouteWriteAction"] = "failed";

            return new CellTrafficAutomationProviderMaterializationResult(
                CellTrafficAutomationProviderMaterializationStates.Failed,
                observedAtUtc,
                exception.Message,
                metadata);
        }
    }

    public async ValueTask<CellTrafficAutomationProviderMaterializationResult> ObserveAsync(
        CellTrafficAutomationRuntimeDescriptor automation,
        KubernetesGatewayTrafficRouteProjection projection,
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

                return new CellTrafficAutomationProviderMaterializationResult(
                    CellTrafficAutomationProviderMaterializationStates.Failed,
                    observedAtUtc,
                    "Observe-only Kubernetes Gateway materialization requires either a registered IKubernetes client, an explicit kubeconfig path, or in-cluster configuration.",
                    metadata);
            }

            var gateway = await TryReadGatewayAsync(client, projection, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(gateway.Error))
            {
                metadata["statusSource"] = ObservationErrorStatusSource;
                metadata["resourceState"] = "gateway-read-failed";

                return new CellTrafficAutomationProviderMaterializationResult(
                    CellTrafficAutomationProviderMaterializationStates.Failed,
                    observedAtUtc,
                    gateway.Error,
                    metadata);
            }

            var httpRoute = await TryReadHttpRouteAsync(client, projection, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(httpRoute.Error))
            {
                metadata["statusSource"] = ObservationErrorStatusSource;
                metadata["resourceState"] = "httproute-read-failed";

                return new CellTrafficAutomationProviderMaterializationResult(
                    CellTrafficAutomationProviderMaterializationStates.Failed,
                    observedAtUtc,
                    httpRoute.Error,
                    metadata);
            }

            metadata["statusSource"] = LiveStatusSource;
            metadata["gatewayExists"] = gateway.Resource is null ? "false" : "true";
            metadata["httpRouteExists"] = httpRoute.Resource is null ? "false" : "true";
            metadata["ownershipState"] = httpRoute.Resource is null
                ? CellTrafficAutomationOwnershipStates.Requested
                : EvaluateOwnership(httpRoute.Resource, automation).State;

            if (gateway.Resource is null || httpRoute.Resource is null)
            {
                metadata["resourceState"] = ResolveMissingResourceState(gateway.Resource is not null, httpRoute.Resource is not null);
                metadata["dependencyState"] = gateway.Resource is null
                    ? CellTrafficAutomationDependencyStates.Missing
                    : CellTrafficAutomationDependencyStates.Satisfied;

                return new CellTrafficAutomationProviderMaterializationResult(
                    CellTrafficAutomationProviderMaterializationStates.Pending,
                    observedAtUtc,
                    metadata: metadata);
            }

            ApplyGatewayMetadata(metadata, gateway.Resource);
            var parentStatus = SelectParentStatus(httpRoute.Resource, projection);
            ApplyHttpRouteMetadata(metadata, httpRoute.Resource, parentStatus, projection);
            var ownership = EvaluateOwnership(httpRoute.Resource, automation);
            ApplyOwnershipMetadata(metadata, ownership);

            var driftReasons = DetectDriftReasons(gateway.Resource, httpRoute.Resource, projection, parentStatus);
            metadata["resourceState"] = "available";
            metadata["dependencyState"] = CellTrafficAutomationDependencyStates.Satisfied;
            metadata["driftState"] = driftReasons.Count == 0
                ? CellTrafficAutomationDriftStates.InSync
                : CellTrafficAutomationDriftStates.Drifted;
            metadata["driftReasons"] = string.Join(",", driftReasons);
            if (ownership.IsConflict || ownership.CanTransfer)
            {
                metadata["resourceState"] = ownership.ResourceState;

                return new CellTrafficAutomationProviderMaterializationResult(
                    CellTrafficAutomationProviderMaterializationStates.Failed,
                    observedAtUtc,
                    ownership.Error,
                    metadata);
            }

            var evaluation = EvaluateObservedState(gateway.Resource, httpRoute.Resource, driftReasons, parentStatus);
            return new CellTrafficAutomationProviderMaterializationResult(
                evaluation.State,
                observedAtUtc,
                evaluation.Error,
                metadata);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            metadata["statusSource"] = ObservationErrorStatusSource;
            metadata["resourceState"] = "observation-error";

            return new CellTrafficAutomationProviderMaterializationResult(
                CellTrafficAutomationProviderMaterializationStates.Failed,
                observedAtUtc,
                exception.Message,
                metadata);
        }
    }

    public async ValueTask<KubernetesGatewayTrafficCleanupSweepResult> SweepCleanupAsync(
        IReadOnlyList<CellTrafficAutomationRuntimeDescriptor> activeAutomations,
        IReadOnlyCollection<KubernetesGatewayTrafficRouteProjection> activeProjections,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(activeAutomations);
        ArgumentNullException.ThrowIfNull(activeProjections);

        var observedAtUtc = timeProvider.GetUtcNow();
        var namespaces = activeProjections
            .Select(static projection => projection.RouteNamespace)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Distinct(Comparer)
            .OrderBy(static value => value, Comparer)
            .ToArray();
        var desiredProviderRouteIds = activeProjections
            .Select(static projection => projection.ProviderRouteId)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .ToHashSet(Comparer);
        var removedResourceIds = new List<string>();
        var lifecycleActions = new List<string>();
        var deletedTransferredResourceCount = 0;
        var prunedOrphanResourceCount = 0;

        if (namespaces.Length == 0)
        {
            return new KubernetesGatewayTrafficCleanupSweepResult(
                observedAtUtc,
                "idle",
                metadata: CreateCleanupSweepMetadata(
                    activeAutomations.Count,
                    namespaces,
                    removedResourceIds,
                    lifecycleActions,
                    deletedTransferredResourceCount,
                    prunedOrphanResourceCount));
        }

        try
        {
            var client = GetClient();
            if (client is null)
            {
                return new KubernetesGatewayTrafficCleanupSweepResult(
                    observedAtUtc,
                    "failed",
                    "Cleanup sweep requires either a registered IKubernetes client, an explicit kubeconfig path, or in-cluster configuration.",
                    CreateCleanupSweepMetadata(
                        activeAutomations.Count,
                        namespaces,
                        removedResourceIds,
                        lifecycleActions,
                        deletedTransferredResourceCount,
                        prunedOrphanResourceCount));
            }

            foreach (var routeNamespace in namespaces)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var listedRoutes = await TryListHttpRoutesAsync(client, routeNamespace, cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(listedRoutes.Error))
                {
                    return new KubernetesGatewayTrafficCleanupSweepResult(
                        observedAtUtc,
                        "failed",
                        listedRoutes.Error,
                        CreateCleanupSweepMetadata(
                            activeAutomations.Count,
                            namespaces,
                            removedResourceIds,
                            lifecycleActions,
                            deletedTransferredResourceCount,
                            prunedOrphanResourceCount));
                }

                foreach (var httpRoute in listedRoutes.Resources)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var disposition = EvaluateCleanupDisposition(httpRoute, desiredProviderRouteIds);
                    if (!disposition.ShouldRemove)
                    {
                        continue;
                    }

                    var deleteError = await TryDeleteHttpRouteAsync(
                        client,
                        disposition.ResourceNamespace,
                        disposition.ResourceName,
                        cancellationToken).ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(deleteError))
                    {
                        return new KubernetesGatewayTrafficCleanupSweepResult(
                            observedAtUtc,
                            "failed",
                            deleteError,
                            CreateCleanupSweepMetadata(
                                activeAutomations.Count,
                                namespaces,
                                removedResourceIds,
                                lifecycleActions,
                                deletedTransferredResourceCount,
                                prunedOrphanResourceCount));
                    }

                    removedResourceIds.Add(disposition.ProviderRouteId);
                    lifecycleActions.Add(disposition.LifecycleAction);
                    if (Comparer.Equals(disposition.LifecycleAction, CellTrafficAutomationLifecycleActions.Delete))
                    {
                        deletedTransferredResourceCount++;
                    }
                    else if (Comparer.Equals(disposition.LifecycleAction, CellTrafficAutomationLifecycleActions.Prune))
                    {
                        prunedOrphanResourceCount++;
                    }
                }
            }

            return new KubernetesGatewayTrafficCleanupSweepResult(
                observedAtUtc,
                removedResourceIds.Count == 0 ? "idle" : "applied",
                metadata: CreateCleanupSweepMetadata(
                    activeAutomations.Count,
                    namespaces,
                    removedResourceIds,
                    lifecycleActions,
                    deletedTransferredResourceCount,
                    prunedOrphanResourceCount));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return new KubernetesGatewayTrafficCleanupSweepResult(
                observedAtUtc,
                "failed",
                exception.Message,
                CreateCleanupSweepMetadata(
                    activeAutomations.Count,
                    namespaces,
                    removedResourceIds,
                    lifecycleActions,
                    deletedTransferredResourceCount,
                    prunedOrphanResourceCount));
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

    private static async Task<ResourceReadResult<KubernetesGatewayResource>> TryReadGatewayAsync(
        IKubernetes client,
        KubernetesGatewayTrafficRouteProjection projection,
        CancellationToken cancellationToken)
    {
        try
        {
            using var gatewayClient = new GenericClient(
                client,
                KubernetesGatewayTrafficRouteProjection.GatewayApiGroup,
                KubernetesGatewayTrafficRouteProjection.GatewayApiVersion,
                "gateways",
                disposeClient: false);
            var gateway = await gatewayClient.ReadNamespacedAsync<KubernetesGatewayResource>(
                projection.GatewayNamespace,
                projection.GatewayName,
                cancellationToken).ConfigureAwait(false);

            return new ResourceReadResult<KubernetesGatewayResource>(gateway, null);
        }
        catch (HttpOperationException exception) when (IsNotFound(exception))
        {
            return new ResourceReadResult<KubernetesGatewayResource>(null, null);
        }
        catch (HttpOperationException exception)
        {
            return new ResourceReadResult<KubernetesGatewayResource>(
                null,
                $"Failed to read Gateway '{projection.GatewayResourceId}': {(string.IsNullOrWhiteSpace(exception.Message) ? "Unknown Kubernetes API error." : exception.Message)}");
        }
    }

    private static async Task<ResourceReadResult<KubernetesGatewayHttpRouteResource>> TryReadHttpRouteAsync(
        IKubernetes client,
        KubernetesGatewayTrafficRouteProjection projection,
        CancellationToken cancellationToken)
    {
        try
        {
            using var httpRouteClient = new GenericClient(
                client,
                KubernetesGatewayTrafficRouteProjection.GatewayApiGroup,
                KubernetesGatewayTrafficRouteProjection.GatewayApiVersion,
                "httproutes",
                disposeClient: false);
            var httpRoute = await httpRouteClient.ReadNamespacedAsync<KubernetesGatewayHttpRouteResource>(
                projection.RouteNamespace,
                projection.HttpRouteName,
                cancellationToken).ConfigureAwait(false);

            return new ResourceReadResult<KubernetesGatewayHttpRouteResource>(httpRoute, null);
        }
        catch (HttpOperationException exception) when (IsNotFound(exception))
        {
            return new ResourceReadResult<KubernetesGatewayHttpRouteResource>(null, null);
        }
        catch (HttpOperationException exception)
        {
            return new ResourceReadResult<KubernetesGatewayHttpRouteResource>(
                null,
                $"Failed to read HTTPRoute '{projection.ProviderRouteId}': {(string.IsNullOrWhiteSpace(exception.Message) ? "Unknown Kubernetes API error." : exception.Message)}");
        }
    }

    private static async Task<ResourceListResult<KubernetesGatewayHttpRouteResource>> TryListHttpRoutesAsync(
        IKubernetes client,
        string routeNamespace,
        CancellationToken cancellationToken)
    {
        try
        {
            using var httpRouteClient = new GenericClient(
                client,
                KubernetesGatewayTrafficRouteProjection.GatewayApiGroup,
                KubernetesGatewayTrafficRouteProjection.GatewayApiVersion,
                "httproutes",
                disposeClient: false);
            var httpRoutes = await httpRouteClient.ListNamespacedAsync<KubernetesGatewayHttpRouteResourceList>(
                routeNamespace,
                cancel: cancellationToken).ConfigureAwait(false);
            var resources = httpRoutes?.Items is null
                ? Array.Empty<KubernetesGatewayHttpRouteResource>()
                : httpRoutes.Items
                    .Where(static item => item is not null)
                    .ToArray();

            return new ResourceListResult<KubernetesGatewayHttpRouteResource>(resources, null);
        }
        catch (HttpOperationException exception)
        {
            return new ResourceListResult<KubernetesGatewayHttpRouteResource>(
                Array.Empty<KubernetesGatewayHttpRouteResource>(),
                $"Failed to list HTTPRoutes in namespace '{routeNamespace}': {(string.IsNullOrWhiteSpace(exception.Message) ? "Unknown Kubernetes API error." : exception.Message)}");
        }
    }

    private static async Task<string?> TryDeleteHttpRouteAsync(
        IKubernetes client,
        string routeNamespace,
        string routeName,
        CancellationToken cancellationToken)
    {
        try
        {
            using var httpRouteClient = new GenericClient(
                client,
                KubernetesGatewayTrafficRouteProjection.GatewayApiGroup,
                KubernetesGatewayTrafficRouteProjection.GatewayApiVersion,
                "httproutes",
                disposeClient: false);
            await httpRouteClient.DeleteNamespacedAsync<KubernetesGatewayHttpRouteResource>(
                routeNamespace,
                routeName,
                cancellationToken).ConfigureAwait(false);
            return null;
        }
        catch (HttpOperationException exception) when (IsNotFound(exception))
        {
            return null;
        }
        catch (HttpOperationException exception)
        {
            return $"Failed to delete HTTPRoute 'httproute/{routeNamespace}/{routeName}': {(string.IsNullOrWhiteSpace(exception.Message) ? "Unknown Kubernetes API error." : exception.Message)}";
        }
    }

    private void ApplyObservationMetadata(
        Dictionary<string, string> metadata,
        DateTimeOffset observedAtUtc)
    {
        metadata["providerAction"] = "observe-only";
        metadata["observationMode"] = KubernetesGatewayTrafficObservationModes.Normalize(observationOptions.Mode);
        metadata["ownershipState"] = CellTrafficAutomationOwnershipStates.Requested;
        metadata["dependencyState"] = CellTrafficAutomationDependencyStates.Unknown;
        metadata["driftState"] = CellTrafficAutomationDriftStates.Unknown;
        metadata["driftReasons"] = string.Empty;
        metadata["lifecycleAction"] = CellTrafficAutomationLifecycleActions.Observe;
        metadata["observationPollingIntervalSeconds"] =
            Math.Max(1, observationOptions.PollingIntervalSeconds).ToString(CultureInfo.InvariantCulture);
        metadata["observationStaleAfterSeconds"] =
            Math.Max(1, observationOptions.StaleAfterSeconds).ToString(CultureInfo.InvariantCulture);
        metadata["observationFreshUntilUtc"] = observedAtUtc
            .AddSeconds(Math.Max(1, observationOptions.StaleAfterSeconds))
            .ToString("O", CultureInfo.InvariantCulture);
    }

    private static void ApplyGatewayMetadata(
        Dictionary<string, string> metadata,
        KubernetesGatewayResource gateway)
    {
        metadata["gatewayGeneration"] = gateway.Metadata?.Generation?.ToString(CultureInfo.InvariantCulture) ?? "0";
        metadata["gatewayObservedClassName"] = gateway.Spec?.GatewayClassName ?? string.Empty;

        var accepted = FindCondition(gateway.Status?.Conditions, "Accepted");
        var programmed = FindCondition(gateway.Status?.Conditions, "Programmed");

        ApplyConditionMetadata(metadata, "gatewayAccepted", accepted);
        ApplyConditionMetadata(metadata, "gatewayProgrammed", programmed);
    }

    private static void ApplyHttpRouteMetadata(
        Dictionary<string, string> metadata,
        KubernetesGatewayHttpRouteResource httpRoute,
        KubernetesGatewayHttpRouteParentStatus? parentStatus,
        KubernetesGatewayTrafficRouteProjection projection)
    {
        metadata["httpRouteGeneration"] = httpRoute.Metadata?.Generation?.ToString(CultureInfo.InvariantCulture) ?? "0";
        metadata["httpRouteParentStatusCount"] = (httpRoute.Status?.Parents?.Count ?? 0).ToString(CultureInfo.InvariantCulture);
        metadata["httpRouteObservedParentRefs"] = string.Join(",", NormalizeParentReferences(httpRoute.Spec?.ParentRefs, httpRoute.Metadata?.Namespace));
        metadata["httpRouteObservedBackendRefs"] = string.Join(",", NormalizeBackendReferences(httpRoute.Spec?.Rules, httpRoute.Metadata?.Namespace));
        metadata["httpRouteObservedHostnames"] = string.Join(",", NormalizeValues(httpRoute.Spec?.Hostnames));

        if (parentStatus is not null)
        {
            metadata["httpRouteControllerName"] = parentStatus.ControllerName ?? string.Empty;
        }
        else
        {
            metadata["httpRouteControllerName"] = string.Empty;
        }

        var accepted = FindCondition(parentStatus?.Conditions, "Accepted");
        var resolvedRefs = FindCondition(parentStatus?.Conditions, "ResolvedRefs");
        var programmed = FindCondition(parentStatus?.Conditions, "Programmed");

        ApplyConditionMetadata(metadata, "httpRouteAccepted", accepted);
        ApplyConditionMetadata(metadata, "httpRouteResolvedRefs", resolvedRefs);
        ApplyConditionMetadata(metadata, "httpRouteProgrammed", programmed);

        metadata["httpRouteExpectedParentRef"] = projection.ParentReference;
        metadata["httpRouteExpectedBackendRef"] = projection.BackendReference;
    }

    private static void ApplyOwnershipMetadata(
        Dictionary<string, string> metadata,
        OwnershipEvaluation ownership)
    {
        metadata["ownershipState"] = ownership.State;
        metadata["managedBy"] = ownership.ManagedBy ?? string.Empty;
        metadata["observedAutomationId"] = ownership.ObservedAutomationId ?? string.Empty;
        metadata["observedSourceModuleId"] = ownership.ObservedSourceModuleId ?? string.Empty;
        metadata["observedRouteId"] = ownership.ObservedRouteId ?? string.Empty;
        metadata["ownershipReason"] = ownership.Reason;
        metadata["activeOwnerAutomationId"] = ownership.ActiveOwnerId ?? string.Empty;
    }

    private static void ApplyTransferMetadata(
        Dictionary<string, string> metadata,
        OwnershipEvaluation? ownership)
    {
        if (ownership?.CanTransfer != true)
        {
            return;
        }

        metadata["previousAutomationId"] = ownership.ObservedAutomationId ?? string.Empty;
        metadata["previousRouteId"] = ownership.ObservedRouteId ?? string.Empty;
        metadata["previousSourceModuleId"] = ownership.ObservedSourceModuleId ?? string.Empty;
        metadata["previousOwnershipReason"] = ownership.Reason;
    }

    private static List<string> DetectDriftReasons(
        KubernetesGatewayResource gateway,
        KubernetesGatewayHttpRouteResource httpRoute,
        KubernetesGatewayTrafficRouteProjection projection,
        KubernetesGatewayHttpRouteParentStatus? parentStatus)
    {
        var reasons = new List<string>();
        var observedGatewayClassName = NormalizeOptional(gateway.Spec?.GatewayClassName);
        if (!string.IsNullOrWhiteSpace(projection.GatewayClassName) &&
            !Comparer.Equals(observedGatewayClassName, NormalizeOptional(projection.GatewayClassName)))
        {
            reasons.Add("gateway-class");
        }

        var observedParentRefs = NormalizeParentReferences(httpRoute.Spec?.ParentRefs, httpRoute.Metadata?.Namespace);
        if (!observedParentRefs.SequenceEqual([projection.ParentReference], Comparer))
        {
            reasons.Add("parent-ref");
        }

        var observedHostnames = NormalizeValues(httpRoute.Spec?.Hostnames);
        var expectedHostnames = NormalizeValues(projection.Hostnames);
        if (!observedHostnames.SequenceEqual(expectedHostnames, Comparer))
        {
            reasons.Add("hostnames");
        }

        var observedBackendRefs = NormalizeBackendReferences(httpRoute.Spec?.Rules, httpRoute.Metadata?.Namespace);
        if (!observedBackendRefs.SequenceEqual([projection.BackendReference], Comparer))
        {
            reasons.Add("backend-ref");
        }

        if (!string.IsNullOrWhiteSpace(projection.ControllerName) &&
            !Comparer.Equals(NormalizeOptional(parentStatus?.ControllerName), NormalizeOptional(projection.ControllerName)))
        {
            reasons.Add("controller-name");
        }

        return reasons;
    }

    private static ObservedStateEvaluation EvaluateObservedState(
        KubernetesGatewayResource gateway,
        KubernetesGatewayHttpRouteResource httpRoute,
        List<string> driftReasons,
        KubernetesGatewayHttpRouteParentStatus? parentStatus)
    {
        if (driftReasons.Count > 0)
        {
            return new ObservedStateEvaluation(
                CellTrafficAutomationProviderMaterializationStates.Failed,
                "Observed Kubernetes Gateway resources drift from the projected Cephalon traffic intent.");
        }

        var gatewayGeneration = gateway.Metadata?.Generation;
        var httpRouteGeneration = httpRoute.Metadata?.Generation;

        var gatewayAccepted = FindCondition(gateway.Status?.Conditions, "Accepted");
        var gatewayProgrammed = FindCondition(gateway.Status?.Conditions, "Programmed");
        var httpRouteAccepted = FindCondition(parentStatus?.Conditions, "Accepted");
        var httpRouteResolvedRefs = FindCondition(parentStatus?.Conditions, "ResolvedRefs");

        if (IsFalse(gatewayAccepted))
        {
            return new ObservedStateEvaluation(
                CellTrafficAutomationProviderMaterializationStates.Failed,
                gatewayAccepted?.Message ?? gatewayAccepted?.Reason ?? "The target Gateway reported Accepted=False.");
        }

        if (IsFalse(httpRouteAccepted))
        {
            return new ObservedStateEvaluation(
                CellTrafficAutomationProviderMaterializationStates.Failed,
                httpRouteAccepted?.Message ?? httpRouteAccepted?.Reason ?? "The target HTTPRoute reported Accepted=False.");
        }

        if (IsFalse(httpRouteResolvedRefs))
        {
            return new ObservedStateEvaluation(
                CellTrafficAutomationProviderMaterializationStates.Failed,
                httpRouteResolvedRefs?.Message ?? httpRouteResolvedRefs?.Reason ?? "The target HTTPRoute reported ResolvedRefs=False.");
        }

        if (!IsTrue(gatewayAccepted) ||
            !IsConditionCurrent(gatewayAccepted, gatewayGeneration) ||
            !IsTrue(gatewayProgrammed) ||
            !IsConditionCurrent(gatewayProgrammed, gatewayGeneration) ||
            !IsTrue(httpRouteAccepted) ||
            !IsConditionCurrent(httpRouteAccepted, httpRouteGeneration) ||
            !IsTrue(httpRouteResolvedRefs) ||
            !IsConditionCurrent(httpRouteResolvedRefs, httpRouteGeneration))
        {
            return new ObservedStateEvaluation(
                CellTrafficAutomationProviderMaterializationStates.Pending,
                null);
        }

        return new ObservedStateEvaluation(
            CellTrafficAutomationProviderMaterializationStates.Applied,
            null);
    }

    private static string ResolveMissingResourceState(bool gatewayExists, bool httpRouteExists)
    {
        return (gatewayExists, httpRouteExists) switch
        {
            (false, false) => "missing-gateway-and-httproute",
            (false, true) => "missing-gateway",
            (true, false) => "missing-httproute",
            _ => "available"
        };
    }

    private static void ApplyConditionMetadata(
        Dictionary<string, string> metadata,
        string keyPrefix,
        KubernetesGatewayCondition? condition)
    {
        metadata[$"{keyPrefix}Condition"] = GetConditionState(condition);

        if (condition?.ObservedGeneration is not null)
        {
            metadata[$"{keyPrefix}ObservedGeneration"] =
                condition.ObservedGeneration.Value.ToString(CultureInfo.InvariantCulture);
        }

        if (!string.IsNullOrWhiteSpace(condition?.Reason))
        {
            metadata[$"{keyPrefix}Reason"] = condition.Reason!;
        }

        if (!string.IsNullOrWhiteSpace(condition?.Message))
        {
            metadata[$"{keyPrefix}Message"] = condition.Message!;
        }
    }

    private static KubernetesGatewayHttpRouteParentStatus? SelectParentStatus(
        KubernetesGatewayHttpRouteResource httpRoute,
        KubernetesGatewayTrafficRouteProjection projection)
    {
        return httpRoute.Status?.Parents?
            .FirstOrDefault(parent =>
            {
                var parentName = NormalizeOptional(parent.ParentRef?.Name);
                var parentNamespace = NormalizeOptional(parent.ParentRef?.Namespace)
                    ?? NormalizeOptional(httpRoute.Metadata?.Namespace);
                var sectionName = NormalizeOptional(parent.ParentRef?.SectionName);

                return Comparer.Equals(parentName, NormalizeOptional(projection.GatewayName)) &&
                    Comparer.Equals(parentNamespace, NormalizeOptional(projection.GatewayNamespace)) &&
                    Comparer.Equals(sectionName, NormalizeOptional(projection.ListenerName));
            });
    }

    private static List<string> NormalizeParentReferences(
        IReadOnlyList<KubernetesGatewayHttpRouteParentReference>? parentRefs,
        string? routeNamespace)
    {
        return parentRefs?
            .Select(parent =>
            {
                var gatewayNamespace = NormalizeOptional(parent.Namespace) ?? NormalizeOptional(routeNamespace) ?? string.Empty;
                var gatewayName = NormalizeOptional(parent.Name) ?? string.Empty;
                var listenerName = NormalizeOptional(parent.SectionName);

                return string.IsNullOrWhiteSpace(listenerName)
                    ? $"gateway/{gatewayNamespace}/{gatewayName}"
                    : $"gateway/{gatewayNamespace}/{gatewayName}#listener/{listenerName}";
            })
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Distinct(Comparer)
            .OrderBy(static value => value, Comparer)
            .ToList() ?? [];
    }

    private static List<string> NormalizeBackendReferences(
        IReadOnlyList<KubernetesGatewayHttpRouteRule>? rules,
        string? routeNamespace)
    {
        return rules?
            .SelectMany(static rule => rule.BackendRefs ?? [])
            .Select(backend =>
            {
                var backendNamespace = NormalizeOptional(backend.Namespace) ?? NormalizeOptional(routeNamespace) ?? string.Empty;
                var backendName = NormalizeOptional(backend.Name) ?? string.Empty;
                var port = backend.Port?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
                var weight = backend.Weight;

                return weight is > 0
                    ? $"service/{backendNamespace}/{backendName}:{port}@weight/{weight.Value.ToString(CultureInfo.InvariantCulture)}"
                    : $"service/{backendNamespace}/{backendName}:{port}";
            })
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Distinct(Comparer)
            .OrderBy(static value => value, Comparer)
            .ToList() ?? [];
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

    private static KubernetesGatewayCondition? FindCondition(
        IReadOnlyList<KubernetesGatewayCondition>? conditions,
        string conditionType)
    {
        return conditions?
            .FirstOrDefault(condition => Comparer.Equals(condition.Type, conditionType));
    }

    private static bool IsTrue(KubernetesGatewayCondition? condition) =>
        Comparer.Equals(GetConditionState(condition), "true");

    private static bool IsFalse(KubernetesGatewayCondition? condition) =>
        Comparer.Equals(GetConditionState(condition), "false");

    private static bool IsConditionCurrent(
        KubernetesGatewayCondition? condition,
        long? generation)
    {
        if (condition?.ObservedGeneration is null || generation is null)
        {
            return false;
        }

        return condition.ObservedGeneration.Value >= generation.Value;
    }

    private static string GetConditionState(KubernetesGatewayCondition? condition)
    {
        var normalized = NormalizeOptional(condition?.Status);
        return normalized?.ToLowerInvariant() switch
        {
            "true" => "true",
            "false" => "false",
            _ => KubernetesGatewayTrafficRouteProjection.UnknownConditionState
        };
    }

    private static bool IsNotFound(HttpOperationException exception) =>
        exception.Response is not null &&
        exception.Response.StatusCode == HttpStatusCode.NotFound;

    internal OwnershipEvaluation EvaluateOwnership(
        KubernetesGatewayHttpRouteResource httpRoute,
        CellTrafficAutomationRuntimeDescriptor automation)
    {
        ArgumentNullException.ThrowIfNull(httpRoute);
        ArgumentNullException.ThrowIfNull(automation);

        var managedBy = ReadMetadataValue(httpRoute.Metadata?.Labels, KubernetesGatewayOwnership.ManagedByLabel);
        var observedAutomationId = ReadMetadataValue(httpRoute.Metadata?.Annotations, KubernetesGatewayOwnership.AutomationIdAnnotation);
        var observedRouteId = ReadMetadataValue(httpRoute.Metadata?.Annotations, KubernetesGatewayOwnership.RouteIdAnnotation);
        var observedSourceModuleId = ReadMetadataValue(httpRoute.Metadata?.Annotations, KubernetesGatewayOwnership.SourceModuleIdAnnotation);
        var activeOwner = ResolveActiveOwner(observedAutomationId, observedRouteId, observedSourceModuleId);

        if (!string.IsNullOrWhiteSpace(managedBy) &&
            !Comparer.Equals(managedBy, KubernetesGatewayOwnership.ManagedByValue))
        {
            return new OwnershipEvaluation(
                State: CellTrafficAutomationOwnershipStates.OwnershipConflict,
                IsOwned: false,
                IsConflict: true,
                CanTransfer: false,
                Reason: "foreign-manager",
                ResourceState: "ownership-conflict",
                Error: $"Kubernetes Gateway HTTPRoute '{httpRoute.Metadata?.Name ?? automation.RouteId}' is already marked as managed by '{managedBy}'.",
                ManagedBy: managedBy,
                ObservedAutomationId: observedAutomationId,
                ObservedRouteId: observedRouteId,
                ObservedSourceModuleId: observedSourceModuleId,
                ActiveOwnerId: activeOwner?.Id);
        }

        if (MatchesCurrentAutomation(observedAutomationId, observedRouteId, observedSourceModuleId, automation))
        {
            return new OwnershipEvaluation(
                State: CellTrafficAutomationOwnershipStates.Owned,
                IsOwned: true,
                IsConflict: false,
                CanTransfer: false,
                Reason: "current-owner",
                ResourceState: "available",
                Error: null,
                ManagedBy: managedBy,
                ObservedAutomationId: observedAutomationId,
                ObservedRouteId: observedRouteId,
                ObservedSourceModuleId: observedSourceModuleId,
                ActiveOwnerId: activeOwner?.Id);
        }

        if (activeOwner is not null && !Comparer.Equals(activeOwner.Id, automation.Id))
        {
            return new OwnershipEvaluation(
                State: CellTrafficAutomationOwnershipStates.OwnershipConflict,
                IsOwned: false,
                IsConflict: true,
                CanTransfer: false,
                Reason: "active-foreign-owner",
                ResourceState: "ownership-conflict",
                Error: $"Kubernetes Gateway HTTPRoute '{httpRoute.Metadata?.Name ?? automation.RouteId}' is already owned by active Cephalon automation '{activeOwner.Id}' and cannot be reassigned to '{automation.Id}'.",
                ManagedBy: managedBy,
                ObservedAutomationId: observedAutomationId,
                ObservedRouteId: observedRouteId,
                ObservedSourceModuleId: observedSourceModuleId,
                ActiveOwnerId: activeOwner.Id);
        }

        if (HasCephalonOwnershipMetadata(managedBy, observedAutomationId, observedRouteId, observedSourceModuleId) ||
            activeOwner is not null)
        {
            return new OwnershipEvaluation(
                State: CellTrafficAutomationOwnershipStates.Orphaned,
                IsOwned: false,
                IsConflict: false,
                CanTransfer: true,
                Reason: activeOwner is null ? "stale-owner" : "incomplete-current-owner",
                ResourceState: "orphaned-httproute",
                Error: $"Kubernetes Gateway HTTPRoute '{httpRoute.Metadata?.Name ?? automation.RouteId}' carries stale or incomplete Cephalon ownership metadata and must be reconciled before it can be reported as owned by automation '{automation.Id}'.",
                ManagedBy: managedBy,
                ObservedAutomationId: observedAutomationId,
                ObservedRouteId: observedRouteId,
                ObservedSourceModuleId: observedSourceModuleId,
                ActiveOwnerId: activeOwner?.Id);
        }

        return new OwnershipEvaluation(
            State: CellTrafficAutomationOwnershipStates.OwnershipConflict,
            IsOwned: false,
            IsConflict: true,
            CanTransfer: false,
            Reason: "external-unmanaged-resource",
            ResourceState: "ownership-conflict",
            Error: $"Kubernetes Gateway HTTPRoute '{httpRoute.Metadata?.Name ?? automation.RouteId}' already exists but is not marked as a Cephalon-managed resource for automation '{automation.Id}'.",
            ManagedBy: managedBy,
            ObservedAutomationId: observedAutomationId,
            ObservedRouteId: observedRouteId,
            ObservedSourceModuleId: observedSourceModuleId,
            ActiveOwnerId: activeOwner?.Id);
    }

    internal CleanupDisposition EvaluateCleanupDisposition(
        KubernetesGatewayHttpRouteResource httpRoute,
        IReadOnlySet<string> desiredProviderRouteIds)
    {
        ArgumentNullException.ThrowIfNull(httpRoute);
        ArgumentNullException.ThrowIfNull(desiredProviderRouteIds);

        var routeNamespace = NormalizeOptional(httpRoute.Metadata?.Namespace);
        var routeName = NormalizeOptional(httpRoute.Metadata?.Name);
        if (routeNamespace is null || routeName is null)
        {
            return CleanupDisposition.None;
        }

        var providerRouteId = $"httproute/{routeNamespace}/{routeName}";
        if (desiredProviderRouteIds.Contains(providerRouteId))
        {
            return CleanupDisposition.None;
        }

        var managedBy = ReadMetadataValue(httpRoute.Metadata?.Labels, KubernetesGatewayOwnership.ManagedByLabel);
        var observedAutomationId = ReadMetadataValue(httpRoute.Metadata?.Annotations, KubernetesGatewayOwnership.AutomationIdAnnotation);
        var observedRouteId = ReadMetadataValue(httpRoute.Metadata?.Annotations, KubernetesGatewayOwnership.RouteIdAnnotation);
        var observedSourceModuleId = ReadMetadataValue(httpRoute.Metadata?.Annotations, KubernetesGatewayOwnership.SourceModuleIdAnnotation);
        if (!CanCleanupManagedResource(managedBy, observedAutomationId, observedRouteId, observedSourceModuleId))
        {
            return CleanupDisposition.None;
        }

        var activeOwner = ResolveActiveOwner(observedAutomationId, observedRouteId, observedSourceModuleId);
        return activeOwner is not null
            ? new CleanupDisposition(
                true,
                CellTrafficAutomationLifecycleActions.Delete,
                CellTrafficAutomationOwnershipStates.Transferred,
                "active-owner-transferred",
                routeNamespace,
                routeName,
                providerRouteId)
            : new CleanupDisposition(
                true,
                CellTrafficAutomationLifecycleActions.Prune,
                CellTrafficAutomationOwnershipStates.Pruned,
                "stale-owner",
                routeNamespace,
                routeName,
                providerRouteId);
    }

    private CellTrafficAutomationRuntimeDescriptor? ResolveActiveOwner(
        string? observedAutomationId,
        string? observedRouteId,
        string? observedSourceModuleId)
    {
        var catalog = runtimeCatalogAccessor?.Invoke();
        if (catalog is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(observedAutomationId))
        {
            var owner = catalog.GetById(observedAutomationId);
            if (owner is not null)
            {
                return owner;
            }
        }

        if (!string.IsNullOrWhiteSpace(observedRouteId))
        {
            var owner = catalog.GetByRouteId(observedRouteId);
            if (owner is not null)
            {
                return owner;
            }
        }

        return null;
    }

    private static bool MatchesCurrentAutomation(
        string? observedAutomationId,
        string? observedRouteId,
        string? observedSourceModuleId,
        CellTrafficAutomationRuntimeDescriptor automation)
    {
        return Comparer.Equals(observedAutomationId, automation.Id) &&
            Comparer.Equals(observedRouteId, automation.RouteId) &&
            Comparer.Equals(observedSourceModuleId, automation.SourceModuleId);
    }

    private static bool HasCephalonOwnershipMetadata(
        string? managedBy,
        string? observedAutomationId,
        string? observedRouteId,
        string? observedSourceModuleId)
    {
        return (!string.IsNullOrWhiteSpace(managedBy) &&
                Comparer.Equals(managedBy, KubernetesGatewayOwnership.ManagedByValue)) ||
            !string.IsNullOrWhiteSpace(observedAutomationId) ||
            !string.IsNullOrWhiteSpace(observedRouteId) ||
            !string.IsNullOrWhiteSpace(observedSourceModuleId);
    }

    private static bool CanCleanupManagedResource(
        string? managedBy,
        string? observedAutomationId,
        string? observedRouteId,
        string? observedSourceModuleId)
    {
        return (string.IsNullOrWhiteSpace(managedBy) ||
                Comparer.Equals(managedBy, KubernetesGatewayOwnership.ManagedByValue)) &&
            HasCephalonOwnershipMetadata(managedBy, observedAutomationId, observedRouteId, observedSourceModuleId);
    }

    private static Dictionary<string, string> CreateCleanupSweepMetadata(
        int activeAutomationCount,
        IReadOnlyList<string> namespaces,
        List<string> removedResourceIds,
        IReadOnlyList<string> lifecycleActions,
        int deletedTransferredResourceCount,
        int prunedOrphanResourceCount)
    {
        var distinctActions = lifecycleActions
            .Distinct(Comparer)
            .OrderBy(static value => value, Comparer)
            .ToArray();
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["activeAutomationCount"] = activeAutomationCount.ToString(CultureInfo.InvariantCulture),
            ["cleanupStrategy"] = "primary-only",
            ["candidateCount"] = (deletedTransferredResourceCount + prunedOrphanResourceCount).ToString(CultureInfo.InvariantCulture),
            ["removedResourceCount"] = removedResourceIds.Count.ToString(CultureInfo.InvariantCulture),
            ["deletedTransferredResourceCount"] = deletedTransferredResourceCount.ToString(CultureInfo.InvariantCulture),
            ["prunedOrphanResourceCount"] = prunedOrphanResourceCount.ToString(CultureInfo.InvariantCulture),
            ["primaryCandidateCount"] = (deletedTransferredResourceCount + prunedOrphanResourceCount).ToString(CultureInfo.InvariantCulture),
            ["removedPrimaryResourceCount"] = removedResourceIds.Count.ToString(CultureInfo.InvariantCulture),
            ["deletedTransferredPrimaryResourceCount"] = deletedTransferredResourceCount.ToString(CultureInfo.InvariantCulture),
            ["prunedOrphanPrimaryResourceCount"] = prunedOrphanResourceCount.ToString(CultureInfo.InvariantCulture),
            ["primaryResourceIds"] = string.Join(",", removedResourceIds.OrderBy(static value => value, Comparer)),
            ["dependencyCandidateCount"] = "0",
            ["removedDependencyResourceCount"] = "0",
            ["deletedTransferredDependencyResourceCount"] = "0",
            ["prunedOrphanDependencyResourceCount"] = "0",
            ["dependencyResourceIds"] = string.Empty,
            ["dependencyKinds"] = string.Empty,
            ["dependencyNamespaces"] = string.Empty,
            ["resourceIds"] = string.Join(",", removedResourceIds.OrderBy(static value => value, Comparer)),
            ["lifecycleActions"] = string.Join(",", distinctActions),
            ["namespaces"] = string.Join(",", namespaces)
        };
    }

    private static string? ReadMetadataValue(
        IReadOnlyDictionary<string, string>? values,
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

    private sealed record ResourceReadResult<T>(T? Resource, string? Error)
        where T : class;

    private sealed record ResourceListResult<T>(IReadOnlyList<T> Resources, string? Error)
        where T : class;

    private sealed record ObservedStateEvaluation(string State, string? Error);

    internal sealed record CleanupDisposition(
        bool ShouldRemove,
        string LifecycleAction,
        string OwnershipState,
        string Reason,
        string ResourceNamespace,
        string ResourceName,
        string ProviderRouteId)
    {
        public static CleanupDisposition None { get; } = new(
            false,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    internal sealed record OwnershipEvaluation(
        string State,
        bool IsOwned,
        bool IsConflict,
        bool CanTransfer,
        string Reason,
        string ResourceState,
        string? Error,
        string? ManagedBy,
        string? ObservedAutomationId,
        string? ObservedRouteId,
        string? ObservedSourceModuleId,
        string? ActiveOwnerId);
}

internal sealed class KubernetesGatewayResource : KubernetesObject
{
    public KubernetesGatewayObjectMetadata? Metadata { get; set; }

    public KubernetesGatewaySpec? Spec { get; set; }

    public KubernetesGatewayStatus? Status { get; set; }
}

internal sealed class KubernetesGatewaySpec
{
    public string? GatewayClassName { get; set; }
}

internal sealed class KubernetesGatewayStatus
{
    public IReadOnlyList<KubernetesGatewayCondition>? Conditions { get; set; }
}

internal sealed class KubernetesGatewayHttpRouteResource : KubernetesObject
{
    public KubernetesGatewayObjectMetadata? Metadata { get; set; }

    public KubernetesGatewayHttpRouteSpec? Spec { get; set; }

    public KubernetesGatewayHttpRouteStatus? Status { get; set; }
}

internal sealed class KubernetesGatewayHttpRouteResourceList : KubernetesObject
{
    public IReadOnlyList<KubernetesGatewayHttpRouteResource>? Items { get; set; }
}

internal sealed class KubernetesGatewayHttpRouteSpec
{
    public IReadOnlyList<KubernetesGatewayHttpRouteParentReference>? ParentRefs { get; set; }

    public IReadOnlyList<string>? Hostnames { get; set; }

    public IReadOnlyList<KubernetesGatewayHttpRouteRule>? Rules { get; set; }
}

internal sealed class KubernetesGatewayHttpRouteRule
{
    public IReadOnlyList<KubernetesGatewayHttpRouteBackendRef>? BackendRefs { get; set; }
}

internal sealed class KubernetesGatewayHttpRouteBackendRef
{
    public string? Group { get; set; }

    public string? Kind { get; set; }

    public string? Name { get; set; }

    public string? Namespace { get; set; }

    public int? Port { get; set; }

    public int? Weight { get; set; }
}

internal sealed class KubernetesGatewayHttpRouteStatus
{
    public IReadOnlyList<KubernetesGatewayHttpRouteParentStatus>? Parents { get; set; }
}

internal sealed class KubernetesGatewayHttpRouteParentStatus
{
    public KubernetesGatewayHttpRouteParentReference? ParentRef { get; set; }

    public string? ControllerName { get; set; }

    public IReadOnlyList<KubernetesGatewayCondition>? Conditions { get; set; }
}

internal sealed class KubernetesGatewayHttpRouteParentReference
{
    public string? Group { get; set; }

    public string? Kind { get; set; }

    public string? Name { get; set; }

    public string? Namespace { get; set; }

    public string? SectionName { get; set; }
}

internal sealed class KubernetesGatewayObjectMetadata
{
    public string? Name { get; set; }

    public string? Namespace { get; set; }

    public string? ResourceVersion { get; set; }

    public long? Generation { get; set; }

    public Dictionary<string, string>? Labels { get; set; }

    public Dictionary<string, string>? Annotations { get; set; }
}

internal sealed class KubernetesGatewayCondition
{
    public string? Type { get; set; }

    public string? Status { get; set; }

    public long? ObservedGeneration { get; set; }

    public string? Reason { get; set; }

    public string? Message { get; set; }
}

internal static class KubernetesGatewayOwnership
{
    public const string ManagedByLabel = "cephalon.io/managed-by";
    public const string ManagedByValue = "edge-kubernetes-gateway";
    public const string AutomationIdAnnotation = "cephalon.io/cell-traffic-automation-id";
    public const string RouteIdAnnotation = "cephalon.io/cell-route-id";
    public const string SourceModuleIdAnnotation = "cephalon.io/source-module-id";
}
