using Cephalon.Abstractions.Data;

namespace Cephalon.Data.Services;

internal sealed class ManagedConnectorCommandExecutor(
    ICdcCaptureExecutionRuntimeCatalog runtimeCatalog,
    IEnumerable<ICdcCaptureExecutionRuntimeManagedConnectorExecutionAdapter> executionAdapters)
    : ICdcCaptureExecutionRuntimeManagedConnectorCommandExecutor
{
    private readonly ICdcCaptureExecutionRuntimeManagedConnectorExecutionAdapter[] executionAdapters = executionAdapters.ToArray();

    public async ValueTask<CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult> ExecuteAsync(
        string executionRuntimeId,
        string operationId,
        CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executionRuntimeId);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);

        var normalizedExecutionRuntimeId = executionRuntimeId.Trim();
        var normalizedOperationId = operationId.Trim();
        var runtime = runtimeCatalog.GetById(normalizedExecutionRuntimeId);
        if (runtime is null)
        {
            throw new InvalidOperationException(
                $"CDC capture execution runtime '{normalizedExecutionRuntimeId}' is not currently visible to the managed-connector execution lane.");
        }

        var executionAdapter = runtime.ManagedConnectorExecutionAdapter;
        if (string.Equals(
                normalizedOperationId,
                CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.None,
                StringComparison.OrdinalIgnoreCase))
        {
            return CreateResult(
                runtime,
                executionAdapter,
                normalizedOperationId,
                CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.NotApplicable,
                "Managed-connector operation 'none' does not represent a runnable provider command.");
        }

        if (executionAdapter.AppliesToManagedConnector &&
            executionAdapter.HasAdaptableCommand &&
            !string.Equals(
                executionAdapter.OperationId,
                normalizedOperationId,
                StringComparison.OrdinalIgnoreCase))
        {
            return CreateResult(
                runtime,
                executionAdapter,
                normalizedOperationId,
                CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Blocked,
                $"Managed connector '{runtime.Id}' currently resolves operation '{executionAdapter.OperationId}', not '{normalizedOperationId}'.");
        }

        if (!executionAdapter.AppliesToManagedConnector)
        {
            return CreateResult(
                runtime,
                executionAdapter,
                normalizedOperationId,
                CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.NotApplicable,
                executionAdapter.Description ?? "The execution runtime does not currently participate in a managed-connector provider execution lane.");
        }

        if (executionAdapter.IsBlocked)
        {
            return CreateResult(
                runtime,
                executionAdapter,
                normalizedOperationId,
                CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Blocked,
                executionAdapter.Description ?? "The managed connector remains blocked before Cephalon can route a provider command.");
        }

        if (executionAdapter.IsOperatorOnly)
        {
            return CreateResult(
                runtime,
                executionAdapter,
                normalizedOperationId,
                CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.OperatorOnly,
                executionAdapter.Description ?? "The managed connector still remains operator-owned outside Cephalon.");
        }

        var providerExecutionAdapter = ResolveExecutionAdapter(runtime, executionAdapter.AdapterId);
        if (providerExecutionAdapter is null)
        {
            return CreateResult(
                runtime,
                executionAdapter,
                normalizedOperationId,
                CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Unavailable,
                executionAdapter.Description ?? "No matching provider execution adapter is currently registered for the managed connector.");
        }

        return await providerExecutionAdapter.ExecuteAsync(runtime, normalizedOperationId, request, cancellationToken);
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
        string state,
        string description)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(executionAdapter);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedOperationId);
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
            IsDestructiveOperation = executionAdapter.IsDestructiveOperation,
            WouldApplyChanges = executionAdapter.WouldApplyChanges
        };
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
