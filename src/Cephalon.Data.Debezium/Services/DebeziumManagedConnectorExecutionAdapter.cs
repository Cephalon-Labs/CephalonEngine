using Cephalon.Abstractions.Data;
using Cephalon.Data.Debezium.Configuration;

namespace Cephalon.Data.Debezium.Services;

internal sealed class DebeziumManagedConnectorExecutionAdapter
    : ICdcCaptureExecutionRuntimeManagedConnectorExecutionAdapter
{
    private const string TransportKind = "http-rest";

    public string AdapterId => CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterIds.DebeziumKafkaConnectRest;

    public bool CanHandle(CdcCaptureExecutionRuntimeDescriptor runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);

        if (runtime.Metadata.TryGetValue("provider", out var providerId) &&
            string.Equals(providerId, DebeziumDataOptions.ProviderId, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return string.Equals(
            runtime.ManagedConnectorExecutionAdapter.AdapterId,
            AdapterId,
            StringComparison.OrdinalIgnoreCase);
    }

    public ValueTask<CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult> ExecuteAsync(
        CdcCaptureExecutionRuntimeDescriptor runtime,
        string operationId,
        CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);

        cancellationToken.ThrowIfCancellationRequested();

        var normalizedOperationId = operationId.Trim();
        var executionAdapter = runtime.ManagedConnectorExecutionAdapter;
        if (string.Equals(
                normalizedOperationId,
                CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.None,
                StringComparison.OrdinalIgnoreCase))
        {
            return ValueTask.FromResult(CreateResult(
                runtime,
                executionAdapter,
                normalizedOperationId,
                request,
                CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.NotApplicable,
                "Managed-connector operation 'none' does not represent a provider-facing Debezium command."));
        }

        if (!executionAdapter.AppliesToManagedConnector)
        {
            return ValueTask.FromResult(CreateResult(
                runtime,
                executionAdapter,
                normalizedOperationId,
                request,
                CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.NotApplicable,
                executionAdapter.Description ?? "The execution runtime does not currently participate in a Debezium-managed connector execution lane."));
        }

        if (!string.Equals(executionAdapter.OperationId, normalizedOperationId, StringComparison.OrdinalIgnoreCase))
        {
            return ValueTask.FromResult(CreateResult(
                runtime,
                executionAdapter,
                normalizedOperationId,
                request,
                CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Blocked,
                $"Managed connector '{runtime.Id}' currently resolves operation '{executionAdapter.OperationId}', not '{normalizedOperationId}'."));
        }

        if (executionAdapter.IsBlocked)
        {
            return ValueTask.FromResult(CreateResult(
                runtime,
                executionAdapter,
                normalizedOperationId,
                request,
                CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Blocked,
                executionAdapter.Description ?? "The managed connector remains blocked before Debezium command translation can proceed."));
        }

        if (executionAdapter.IsOperatorOnly)
        {
            return ValueTask.FromResult(CreateResult(
                runtime,
                executionAdapter,
                normalizedOperationId,
                request,
                CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.OperatorOnly,
                executionAdapter.Description ?? "The managed connector still remains operator-owned outside Cephalon."));
        }

        if (executionAdapter.IsUnavailable || !executionAdapter.IsReady)
        {
            return ValueTask.FromResult(CreateResult(
                runtime,
                executionAdapter,
                normalizedOperationId,
                request,
                CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Unavailable,
                executionAdapter.Description ?? "No matching Debezium execution adapter is currently available for this managed connector."));
        }

        if (executionAdapter.RequiresExplicitApproval && request?.Approve != true)
        {
            return ValueTask.FromResult(CreateResult(
                runtime,
                executionAdapter,
                normalizedOperationId,
                request,
                CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Blocked,
                "This managed-connector command requires explicit approval before Cephalon can translate it into a Debezium provider command."));
        }

        if (executionAdapter.IsDestructiveOperation && request?.AllowDestructive != true)
        {
            return ValueTask.FromResult(CreateResult(
                runtime,
                executionAdapter,
                normalizedOperationId,
                request,
                CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Blocked,
                "This managed-connector command is destructive and requires AllowDestructive=true before Cephalon can translate it."));
        }

        if (!executionAdapter.WouldApplyChanges ||
            string.Equals(
                runtime.ManagedConnectorCommandIssuance.State,
                CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates.Rejected,
                StringComparison.OrdinalIgnoreCase))
        {
            return ValueTask.FromResult(CreateResult(
                runtime,
                executionAdapter,
                normalizedOperationId,
                request,
                CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.NoOp,
                "The shared managed-connector truth indicates that no outbound Debezium provider command is needed right now."));
        }

        if (string.Equals(
                normalizedOperationId,
                CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Reconcile,
                StringComparison.OrdinalIgnoreCase))
        {
            return ValueTask.FromResult(CreateResult(
                runtime,
                executionAdapter,
                normalizedOperationId,
                request,
                CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Blocked,
                "Debezium reconcile translation still needs a future configuration-payload lane before Cephalon can emit a provider command."));
        }

        var connectorId = ResolveConnectorId(runtime);
        var httpMethod = ResolveHttpMethod(normalizedOperationId);
        var relativePath = ResolveRelativePath(connectorId, normalizedOperationId);

        return ValueTask.FromResult(CreateResult(
            runtime,
            executionAdapter,
            normalizedOperationId,
            request,
            CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Adapted,
            $"Cephalon translated the managed-connector request into Debezium provider command shape '{httpMethod} {relativePath}'. Provider completion remains later work, while command-execution outcomes now stay visible on the shared runtime surface.",
            httpMethod,
            relativePath));
    }

    private static string ResolveConnectorId(CdcCaptureExecutionRuntimeDescriptor runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);

        return runtime.Metadata.TryGetValue("connectorId", out var connectorId) &&
               !string.IsNullOrWhiteSpace(connectorId)
            ? connectorId.Trim()
            : runtime.Id;
    }

    private static string ResolveHttpMethod(string operationId)
    {
        if (string.Equals(
                operationId,
                CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Pause,
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                operationId,
                CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Resume,
                StringComparison.OrdinalIgnoreCase))
        {
            return "PUT";
        }

        if (string.Equals(
                operationId,
                CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Restart,
                StringComparison.OrdinalIgnoreCase))
        {
            return "POST";
        }

        if (string.Equals(
                operationId,
                CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Delete,
                StringComparison.OrdinalIgnoreCase))
        {
            return "DELETE";
        }

        return "POST";
    }

    private static string ResolveRelativePath(string connectorId, string operationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);

        var escapedConnectorId = Uri.EscapeDataString(connectorId.Trim());
        if (string.Equals(
                operationId,
                CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Pause,
                StringComparison.OrdinalIgnoreCase))
        {
            return $"/connectors/{escapedConnectorId}/pause";
        }

        if (string.Equals(
                operationId,
                CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Resume,
                StringComparison.OrdinalIgnoreCase))
        {
            return $"/connectors/{escapedConnectorId}/resume";
        }

        if (string.Equals(
                operationId,
                CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Restart,
                StringComparison.OrdinalIgnoreCase))
        {
            return $"/connectors/{escapedConnectorId}/restart?includeTasks=true&onlyFailed=false";
        }

        if (string.Equals(
                operationId,
                CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.Delete,
                StringComparison.OrdinalIgnoreCase))
        {
            return $"/connectors/{escapedConnectorId}";
        }

        return $"/connectors/{escapedConnectorId}";
    }

    private static CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult CreateResult(
        CdcCaptureExecutionRuntimeDescriptor runtime,
        CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStatus executionAdapter,
        string requestedOperationId,
        CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionRequest? request,
        string state,
        string description,
        string? httpMethod = null,
        string? relativePath = null)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(executionAdapter);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedOperationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(state);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        var connectorId = ResolveConnectorId(runtime);
        var providerId = runtime.Metadata.TryGetValue("provider", out var configuredProviderId) &&
                         !string.IsNullOrWhiteSpace(configuredProviderId)
            ? configuredProviderId.Trim()
            : DebeziumDataOptions.ProviderId;
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
            TransportKind = string.IsNullOrWhiteSpace(httpMethod) ? null : TransportKind,
            HttpMethod = string.IsNullOrWhiteSpace(httpMethod) ? null : httpMethod.Trim(),
            RelativePath = string.IsNullOrWhiteSpace(relativePath) ? null : relativePath.Trim(),
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
                httpMethod,
                relativePath,
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
