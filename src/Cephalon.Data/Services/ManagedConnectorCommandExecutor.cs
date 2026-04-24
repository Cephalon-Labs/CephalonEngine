using Cephalon.Abstractions.Data;

namespace Cephalon.Data.Services;

internal sealed class ManagedConnectorCommandExecutor(
    ICdcCaptureExecutionRuntimeCatalog runtimeCatalog,
    IEnumerable<ICdcCaptureExecutionRuntimeManagedConnectorExecutionAdapter> executionAdapters,
    ManagedConnectorCommandExecutionHistoryStore commandExecutionHistoryStore,
    TimeProvider timeProvider)
    : ICdcCaptureExecutionRuntimeManagedConnectorCommandExecutor
{
    private readonly ICdcCaptureExecutionRuntimeManagedConnectorExecutionAdapter[] executionAdapters = executionAdapters.ToArray();

    public async ValueTask<CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult> ExecuteAsync(
        string executionRuntimeId,
        string operationId,
        CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteCoreAsync(
                executionRuntimeId,
                operationId,
                request,
                CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionInvocationSources.OperatorRequest,
                cancellationToken)
            .ConfigureAwait(false);
    }

    internal async ValueTask<CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult> ExecuteAutomaticRetryAsync(
        string executionRuntimeId,
        string operationId,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteCoreAsync(
                executionRuntimeId,
                operationId,
                request: null,
                CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionInvocationSources.AutomaticRetry,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async ValueTask<CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult> ExecuteCoreAsync(
        string executionRuntimeId,
        string operationId,
        CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionRequest? request,
        string invocationSourceId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executionRuntimeId);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(invocationSourceId);

        var normalizedExecutionRuntimeId = executionRuntimeId.Trim();
        var normalizedOperationId = operationId.Trim();
        var normalizedInvocationSourceId = invocationSourceId.Trim();
        var runtime = runtimeCatalog.GetById(normalizedExecutionRuntimeId);
        if (runtime is null)
        {
            throw new InvalidOperationException(
                $"CDC capture execution runtime '{normalizedExecutionRuntimeId}' is not currently visible to the managed-connector execution lane.");
        }

        var executionAdapter = runtime.ManagedConnectorExecutionAdapter;
        var effectiveRequest = request;
        if (effectiveRequest is null &&
            string.Equals(
                normalizedInvocationSourceId,
                CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionInvocationSources.AutomaticRetry,
                StringComparison.OrdinalIgnoreCase))
        {
            effectiveRequest = CreateAutomaticRetryRequest(runtime);
        }

        if (string.Equals(
                normalizedOperationId,
                CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.None,
                StringComparison.OrdinalIgnoreCase))
        {
            return Record(CreateResult(
                runtime,
                executionAdapter,
                normalizedOperationId,
                normalizedInvocationSourceId,
                effectiveRequest,
                CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.NotApplicable,
                "Managed-connector operation 'none' does not represent a runnable provider command."));
        }

        if (string.Equals(
                normalizedInvocationSourceId,
                CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionInvocationSources.AutomaticRetry,
                StringComparison.OrdinalIgnoreCase) &&
            !runtime.ManagedConnectorProviderOwnedControlPlaneOwnership.CanExerciseProviderOwnedControlPlaneOnCurrentNode)
        {
            var providerOwnedControlPlaneOwnership = runtime.ManagedConnectorProviderOwnedControlPlaneOwnership;
            var providerExecutionOrchestration = runtime.ManagedConnectorProviderExecutionOrchestration;
            var providerOwnedWritePathExecution = runtime.ManagedConnectorProviderOwnedWritePathExecution;
            var coordinationState = providerOwnedControlPlaneOwnership.IsOperatorOnly ||
                                    providerExecutionOrchestration.IsOperatorOnly ||
                                    providerOwnedWritePathExecution.IsOperatorOnly
                ? CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.OperatorOnly
                : CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Blocked;
            var coordinationDescription = string.IsNullOrWhiteSpace(providerOwnedControlPlaneOwnership.Description)
                ? string.IsNullOrWhiteSpace(providerExecutionOrchestration.Description)
                ? string.IsNullOrWhiteSpace(providerOwnedWritePathExecution.Description)
                    ? string.IsNullOrWhiteSpace(runtime.ManagedConnectorSchedulerRecoveryExecutionHardening.Description)
                        ? string.IsNullOrWhiteSpace(runtime.ManagedConnectorDurableSharedSchedulerOrchestration.Description)
                            ? string.IsNullOrWhiteSpace(runtime.ManagedConnectorMultiNodeLeaseExecution.Description)
                                ? string.IsNullOrWhiteSpace(runtime.ManagedConnectorDistributedRetryOrchestration.Description)
                                    ? "Automatic background retry is not currently allowed to execute on this node."
                                    : runtime.ManagedConnectorDistributedRetryOrchestration.Description
                                : runtime.ManagedConnectorMultiNodeLeaseExecution.Description
                            : runtime.ManagedConnectorDurableSharedSchedulerOrchestration.Description
                        : runtime.ManagedConnectorSchedulerRecoveryExecutionHardening.Description
                    : providerOwnedWritePathExecution.Description
                : providerExecutionOrchestration.Description
                : providerOwnedControlPlaneOwnership.Description;

            return CreateResult(
                runtime,
                executionAdapter,
                normalizedOperationId,
                normalizedInvocationSourceId,
                effectiveRequest,
                coordinationState,
                coordinationDescription);
        }

        if (executionAdapter.AppliesToManagedConnector &&
            executionAdapter.HasAdaptableCommand &&
            !string.Equals(
                executionAdapter.OperationId,
                normalizedOperationId,
                StringComparison.OrdinalIgnoreCase))
        {
            return Record(CreateResult(
                runtime,
                executionAdapter,
                normalizedOperationId,
                normalizedInvocationSourceId,
                effectiveRequest,
                CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Blocked,
                $"Managed connector '{runtime.Id}' currently resolves operation '{executionAdapter.OperationId}', not '{normalizedOperationId}'."));
        }

        if (!executionAdapter.AppliesToManagedConnector)
        {
            return Record(CreateResult(
                runtime,
                executionAdapter,
                normalizedOperationId,
                normalizedInvocationSourceId,
                effectiveRequest,
                CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.NotApplicable,
                executionAdapter.Description ?? "The execution runtime does not currently participate in a managed-connector provider execution lane."));
        }

        if (executionAdapter.IsBlocked)
        {
            return Record(CreateResult(
                runtime,
                executionAdapter,
                normalizedOperationId,
                normalizedInvocationSourceId,
                effectiveRequest,
                CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Blocked,
                executionAdapter.Description ?? "The managed connector remains blocked before Cephalon can route a provider command."));
        }

        if (executionAdapter.IsOperatorOnly)
        {
            return Record(CreateResult(
                runtime,
                executionAdapter,
                normalizedOperationId,
                normalizedInvocationSourceId,
                effectiveRequest,
                CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.OperatorOnly,
                executionAdapter.Description ?? "The managed connector still remains operator-owned outside Cephalon."));
        }

        var providerExecutionAdapter = ResolveExecutionAdapter(runtime, executionAdapter.AdapterId);
        if (providerExecutionAdapter is null)
        {
            return Record(CreateResult(
                runtime,
                executionAdapter,
                normalizedOperationId,
                normalizedInvocationSourceId,
                effectiveRequest,
                CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Unavailable,
                executionAdapter.Description ?? "No matching provider execution adapter is currently registered for the managed connector."));
        }

        var result = await providerExecutionAdapter.ExecuteAsync(runtime, normalizedOperationId, effectiveRequest, cancellationToken);
        return Record(result with
        {
            InvocationSourceId = normalizedInvocationSourceId
        });
    }

    private ICdcCaptureExecutionRuntimeManagedConnectorExecutionAdapter? ResolveExecutionAdapter(
        CdcCaptureExecutionRuntimeDescriptor runtime,
        string adapterId)
    {
        foreach (var executionAdapter in executionAdapters)
        {
            if (!executionAdapter.CanHandle(runtime))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(adapterId) &&
                !string.Equals(
                    adapterId,
                    CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterIds.None,
                    StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(executionAdapter.AdapterId, adapterId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return executionAdapter;
        }

        return null;
    }

    private static CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult CreateResult(
        CdcCaptureExecutionRuntimeDescriptor runtime,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStatus executionAdapter,
        string requestedOperationId,
        string invocationSourceId,
        CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionRequest? request,
        string state,
        string description)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(executionAdapter);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedOperationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(invocationSourceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(state);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        var connectorId = runtime.Metadata.TryGetValue("connectorId", out var configuredConnectorId) &&
                          !string.IsNullOrWhiteSpace(configuredConnectorId)
            ? configuredConnectorId.Trim()
            : runtime.Id;
        var providerId = runtime.Metadata.TryGetValue("provider", out var configuredProviderId) &&
                         !string.IsNullOrWhiteSpace(configuredProviderId)
            ? configuredProviderId.Trim()
            : null;
        var resolvedOperationId = executionAdapter.HasAdaptableCommand
            ? executionAdapter.OperationId
            : CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.None;

        return new CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult(state, description)
        {
            ExecutionRuntimeId = runtime.Id,
            RequestedOperationId = requestedOperationId.Trim(),
            ResolvedOperationId = resolvedOperationId,
            ExecutionAdapterState = executionAdapter.State,
            CommandIssuanceState = executionAdapter.CommandIssuanceState,
            CommandEnvelopeState = executionAdapter.CommandEnvelopeState,
            AdapterId = executionAdapter.AdapterId,
            ProviderId = providerId,
            ConnectClusterId = executionAdapter.ConnectClusterId,
            ConnectorId = connectorId,
            ConnectorClass = executionAdapter.ConnectorClass,
            SourceProviderId = executionAdapter.SourceProviderId,
            ManagementMode = executionAdapter.ManagementMode,
            SourceId = executionAdapter.SourceId,
            InvocationSourceId = invocationSourceId.Trim(),
            CommandFingerprint = executionAdapter.CommandFingerprint,
            IssuanceFingerprint = executionAdapter.IssuanceFingerprint,
            AdapterFingerprint = executionAdapter.AdapterFingerprint,
            ExecutionFingerprint = CreateExecutionFingerprint(
                state,
                requestedOperationId,
                resolvedOperationId,
                executionAdapter.AdapterId,
                executionAdapter.AdapterFingerprint,
                null,
                null,
                executionAdapter.RequiresExplicitApproval,
                executionAdapter.IsDestructiveOperation,
                executionAdapter.WouldApplyChanges),
            RequiresExplicitApproval = executionAdapter.RequiresExplicitApproval,
            ApprovalApplied = request?.Approve == true,
            IsDestructiveOperation = executionAdapter.IsDestructiveOperation,
            DestructiveAllowanceApplied = request?.AllowDestructive == true,
            WouldApplyChanges = executionAdapter.WouldApplyChanges
        };
    }

    private static CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionRequest? CreateAutomaticRetryRequest(
        CdcCaptureExecutionRuntimeDescriptor runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);

        var automaticRetryExecution = runtime.ManagedConnectorAutomaticRetryExecution;
        if (!automaticRetryExecution.RequiresExplicitApproval &&
            !automaticRetryExecution.IsDestructiveOperation)
        {
            return null;
        }

        return new CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionRequest
        {
            Approve = automaticRetryExecution.CanReuseApprovalFromMatchingHistory,
            AllowDestructive = automaticRetryExecution.CanReuseDestructiveAllowanceFromMatchingHistory
        };
    }

    private CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult Record(
        CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return commandExecutionHistoryStore.Record(result, timeProvider.GetUtcNow());
    }

    private static string CreateExecutionFingerprint(
        string state,
        string requestedOperationId,
        string resolvedOperationId,
        string adapterId,
        string adapterFingerprint,
        string? httpMethod,
        string? relativePath,
        bool requiresExplicitApproval,
        bool isDestructiveOperation,
        bool wouldApplyChanges)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(state);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedOperationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(resolvedOperationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(adapterId);
        ArgumentException.ThrowIfNullOrWhiteSpace(adapterFingerprint);

        return string.Join(
            "|",
            [
                "cephalon-managed-connector-command-execution/v1",
                $"state={state.Trim()}",
                $"requested={requestedOperationId.Trim()}",
                $"resolved={resolvedOperationId.Trim()}",
                $"adapter={adapterId.Trim()}",
                $"adapterFingerprint={adapterFingerprint.Trim()}",
                $"httpMethod={(string.IsNullOrWhiteSpace(httpMethod) ? "none" : httpMethod.Trim())}",
                $"relativePath={(string.IsNullOrWhiteSpace(relativePath) ? "none" : relativePath.Trim())}",
                $"requiresExplicitApproval={requiresExplicitApproval.ToString().ToLowerInvariant()}",
                $"destructive={isDestructiveOperation.ToString().ToLowerInvariant()}",
                $"wouldApplyChanges={wouldApplyChanges.ToString().ToLowerInvariant()}"
            ]);
    }
}
