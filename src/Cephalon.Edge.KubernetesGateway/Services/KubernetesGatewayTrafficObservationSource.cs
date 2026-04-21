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
    IKubernetes? providedClient = null) : IKubernetesGatewayTrafficObservationSource, IKubernetesGatewayTrafficApplyService, IDisposable
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
        metadata["gatewayWriteAction"] = "none";
        metadata["gatewayWriteReason"] = "preprovisioned-dependency";

        try
        {
            var client = GetClient();
            if (client is null)
            {
                metadata["statusSource"] = ApplyUnavailableStatusSource;
                metadata["resourceState"] = "client-unavailable";
                metadata["driftState"] = "unknown";
                metadata["driftReasons"] = string.Empty;
                metadata["httpRouteWriteAction"] = "none";

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
                metadata["driftState"] = "unknown";
                metadata["driftReasons"] = string.Empty;
                metadata["httpRouteWriteAction"] = "none";

                return new CellTrafficAutomationProviderMaterializationResult(
                    CellTrafficAutomationProviderMaterializationStates.Failed,
                    observedAtUtc,
                    httpRoute.Error,
                    metadata);
            }

            if (httpRoute.Resource is not null)
            {
                ApplyOwnershipMetadata(metadata, httpRoute.Resource, automation);
                var ownership = EvaluateOwnership(httpRoute.Resource, automation);
                if (!ownership.IsOwned)
                {
                    metadata["statusSource"] = ApplyErrorStatusSource;
                    metadata["resourceState"] = "ownership-conflict";
                    metadata["driftState"] = "unknown";
                    metadata["driftReasons"] = string.Empty;
                    metadata["httpRouteWriteAction"] = "blocked";
                    metadata["ownershipState"] = ownership.State;

                    return new CellTrafficAutomationProviderMaterializationResult(
                        CellTrafficAutomationProviderMaterializationStates.Failed,
                        observedAtUtc,
                        ownership.Error,
                        metadata);
                }
            }
            else
            {
                metadata["ownershipState"] = "missing";
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
            }
            else
            {
                persistedRoute = await httpRouteClient.ReplaceNamespacedAsync(
                    desiredRoute,
                    projection.RouteNamespace,
                    projection.HttpRouteName,
                    cancellationToken).ConfigureAwait(false);
                metadata["httpRouteWriteAction"] = "replaced";
            }

            metadata["resourceState"] = "write-succeeded";
            metadata["driftState"] = "reconciling";
            metadata["driftReasons"] = string.Empty;
            metadata["ownershipState"] = "owned";
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
            metadata["driftState"] = "unknown";
            metadata["driftReasons"] = string.Empty;
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
            metadata["driftState"] = "unknown";
            metadata["driftReasons"] = string.Empty;
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
                metadata["driftState"] = "unknown";
                metadata["driftReasons"] = string.Empty;

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
                metadata["driftState"] = "unknown";
                metadata["driftReasons"] = string.Empty;

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
                metadata["driftState"] = "unknown";
                metadata["driftReasons"] = string.Empty;

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
                ? "missing"
                : EvaluateOwnership(httpRoute.Resource, automation).State;

            if (gateway.Resource is null || httpRoute.Resource is null)
            {
                metadata["resourceState"] = ResolveMissingResourceState(gateway.Resource is not null, httpRoute.Resource is not null);
                metadata["driftState"] = "unknown";
                metadata["driftReasons"] = string.Empty;

                return new CellTrafficAutomationProviderMaterializationResult(
                    CellTrafficAutomationProviderMaterializationStates.Pending,
                    observedAtUtc,
                    metadata: metadata);
            }

            ApplyGatewayMetadata(metadata, gateway.Resource);
            var parentStatus = SelectParentStatus(httpRoute.Resource, projection);
            ApplyHttpRouteMetadata(metadata, httpRoute.Resource, parentStatus, projection);
            ApplyOwnershipMetadata(metadata, httpRoute.Resource, automation);

            var driftReasons = DetectDriftReasons(gateway.Resource, httpRoute.Resource, projection, parentStatus);
            metadata["resourceState"] = "available";
            metadata["driftState"] = driftReasons.Count == 0 ? "in-sync" : "drifted";
            metadata["driftReasons"] = string.Join(",", driftReasons);

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
            metadata["driftState"] = "unknown";
            metadata["driftReasons"] = string.Empty;

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

    private void ApplyObservationMetadata(
        Dictionary<string, string> metadata,
        DateTimeOffset observedAtUtc)
    {
        metadata["providerAction"] = "observe-only";
        metadata["observationMode"] = KubernetesGatewayTrafficObservationModes.Normalize(observationOptions.Mode);
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
        KubernetesGatewayHttpRouteResource httpRoute,
        CellTrafficAutomationRuntimeDescriptor automation)
    {
        var managedBy = ReadMetadataValue(httpRoute.Metadata?.Labels, KubernetesGatewayOwnership.ManagedByLabel);
        var automationId = ReadMetadataValue(httpRoute.Metadata?.Annotations, KubernetesGatewayOwnership.AutomationIdAnnotation);
        var sourceModuleId = ReadMetadataValue(httpRoute.Metadata?.Annotations, KubernetesGatewayOwnership.SourceModuleIdAnnotation);
        var routeId = ReadMetadataValue(httpRoute.Metadata?.Annotations, KubernetesGatewayOwnership.RouteIdAnnotation);
        var ownership = EvaluateOwnership(httpRoute, automation);

        metadata["ownershipState"] = ownership.State;
        metadata["managedBy"] = managedBy ?? string.Empty;
        metadata["observedAutomationId"] = automationId ?? string.Empty;
        metadata["observedSourceModuleId"] = sourceModuleId ?? string.Empty;
        metadata["observedRouteId"] = routeId ?? string.Empty;
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

    private static OwnershipEvaluation EvaluateOwnership(
        KubernetesGatewayHttpRouteResource httpRoute,
        CellTrafficAutomationRuntimeDescriptor automation)
    {
        var observedAutomationId = ReadMetadataValue(httpRoute.Metadata?.Annotations, KubernetesGatewayOwnership.AutomationIdAnnotation);
        if (string.IsNullOrWhiteSpace(observedAutomationId))
        {
            return new OwnershipEvaluation(
                State: "unowned",
                IsOwned: false,
                Error: $"Kubernetes Gateway HTTPRoute '{httpRoute.Metadata?.Name ?? automation.RouteId}' exists but is not marked as owned by Cephalon automation '{automation.Id}'.");
        }

        if (!Comparer.Equals(observedAutomationId, automation.Id))
        {
            return new OwnershipEvaluation(
                State: "foreign-automation",
                IsOwned: false,
                Error: $"Kubernetes Gateway HTTPRoute '{httpRoute.Metadata?.Name ?? automation.RouteId}' is already owned by Cephalon automation '{observedAutomationId}' and cannot be reassigned to '{automation.Id}'.");
        }

        return new OwnershipEvaluation("owned", true, null);
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

    private sealed record ObservedStateEvaluation(string State, string? Error);

    private sealed record OwnershipEvaluation(string State, bool IsOwned, string? Error);
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
