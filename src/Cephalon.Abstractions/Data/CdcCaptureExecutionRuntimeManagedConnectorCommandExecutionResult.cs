namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes the immediate result of one managed-connector command-execution request evaluated by Cephalon.
/// </summary>
public sealed record CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult
{
    /// <summary>
    /// Creates a new managed-connector command-execution result.
    /// </summary>
    /// <param name="state">
    /// The stable command-execution state, such as <c>unrecorded</c>, <c>blocked</c>, <c>operator-only</c>, <c>unavailable</c>, <c>no-op</c>, <c>adapted</c>, <c>failed</c>, or <c>not-applicable</c>.
    /// </param>
    /// <param name="description">An optional operator-facing command-execution summary.</param>
    public CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult(
        string state,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            throw new ArgumentException("Managed-connector command-execution state is required.", nameof(state));
        }

        State = state.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    /// <summary>
    /// Gets the stable managed-connector command-execution state.
    /// </summary>
    public string State { get; }

    /// <summary>
    /// Gets the stable recorded command-execution attempt identifier when Cephalon has persisted one outcome.
    /// </summary>
    public string AttemptId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the timestamp when Cephalon recorded this command-execution outcome.
    /// </summary>
    public DateTimeOffset? RecordedAtUtc { get; init; }

    /// <summary>
    /// Gets an optional operator-facing command-execution summary.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the stable execution-runtime identifier currently associated with the request.
    /// </summary>
    public string ExecutionRuntimeId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the operation identifier originally requested by the caller.
    /// </summary>
    public string RequestedOperationId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.None;

    /// <summary>
    /// Gets the operation identifier Cephalon resolved for the provider command.
    /// </summary>
    public string ResolvedOperationId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterOperationIds.None;

    /// <summary>
    /// Gets the current execution-adapter state that informed the request result.
    /// </summary>
    public string ExecutionAdapterState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStates.NotApplicable;

    /// <summary>
    /// Gets the current command-issuance state that informed the request result.
    /// </summary>
    public string CommandIssuanceState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates.NotApplicable;

    /// <summary>
    /// Gets the current command-envelope state that informed the request result.
    /// </summary>
    public string CommandEnvelopeState { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStates.NotApplicable;

    /// <summary>
    /// Gets the current provider execution-adapter identifier that handled the request when one was available.
    /// </summary>
    public string AdapterId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterIds.None;

    /// <summary>
    /// Gets the provider identifier associated with the command when one was available.
    /// </summary>
    public string? ProviderId { get; init; }

    /// <summary>
    /// Gets the transport kind used by the provider execution adapter when one was resolved.
    /// </summary>
    public string? TransportKind { get; init; }

    /// <summary>
    /// Gets the outbound HTTP method that would be used by the provider execution adapter when one was resolved.
    /// </summary>
    public string? HttpMethod { get; init; }

    /// <summary>
    /// Gets the outbound relative request path that would be used by the provider execution adapter when one was resolved.
    /// </summary>
    public string? RelativePath { get; init; }

    /// <summary>
    /// Gets the connector-cluster identifier targeted by the provider command when one was available.
    /// </summary>
    public string? ConnectClusterId { get; init; }

    /// <summary>
    /// Gets the provider-facing connector identifier targeted by the provider command when one was available.
    /// </summary>
    public string? ConnectorId { get; init; }

    /// <summary>
    /// Gets the provider-facing connector-class identifier targeted by the provider command when one was available.
    /// </summary>
    public string? ConnectorClass { get; init; }

    /// <summary>
    /// Gets the provider-facing source-provider identifier targeted by the provider command when one was available.
    /// </summary>
    public string? SourceProviderId { get; init; }

    /// <summary>
    /// Gets the declared managed-connector management mode when one is known.
    /// </summary>
    public string? ManagementMode { get; init; }

    /// <summary>
    /// Gets the primary source identifier Cephalon used to derive the request result.
    /// </summary>
    public string SourceId { get; init; } = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterSources.Unknown;

    /// <summary>
    /// Gets the deterministic command fingerprint already associated with the current managed connector.
    /// </summary>
    public string CommandFingerprint { get; init; } = string.Empty;

    /// <summary>
    /// Gets the deterministic issuance fingerprint already associated with the current managed connector.
    /// </summary>
    public string IssuanceFingerprint { get; init; } = string.Empty;

    /// <summary>
    /// Gets the deterministic execution-adapter fingerprint already associated with the current managed connector.
    /// </summary>
    public string AdapterFingerprint { get; init; } = string.Empty;

    /// <summary>
    /// Gets the deterministic command-execution fingerprint Cephalon currently derives for the request result.
    /// </summary>
    public string ExecutionFingerprint { get; init; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether the request still requires explicit approval.
    /// </summary>
    public bool RequiresExplicitApproval { get; init; }

    /// <summary>
    /// Gets a value indicating whether the request targets a destructive connector operation.
    /// </summary>
    public bool IsDestructiveOperation { get; init; }

    /// <summary>
    /// Gets a value indicating whether the request would still apply one or more shared write-path changes.
    /// </summary>
    public bool WouldApplyChanges { get; init; }

    /// <summary>
    /// Gets a value indicating whether the request currently carries one translated provider command.
    /// </summary>
    public bool HasProviderCommand =>
        !string.IsNullOrWhiteSpace(HttpMethod) &&
        !string.IsNullOrWhiteSpace(RelativePath);

    /// <summary>
    /// Gets a value indicating whether Cephalon has not yet recorded any command-execution outcome for the runtime.
    /// </summary>
    public bool IsUnrecorded => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Unrecorded, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether Cephalon has recorded one concrete command-execution outcome.
    /// </summary>
    public bool HasRecordedOutcome => RecordedAtUtc.HasValue;

    /// <summary>
    /// Gets a value indicating whether the request is currently blocked.
    /// </summary>
    public bool IsBlocked => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Blocked, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the request currently remains operator-owned.
    /// </summary>
    public bool IsOperatorOnly => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.OperatorOnly, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether no provider execution adapter is currently available for the request.
    /// </summary>
    public bool IsUnavailable => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Unavailable, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the request currently resolves to no provider work.
    /// </summary>
    public bool IsNoOp => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.NoOp, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the request was translated into one provider-facing command shape.
    /// </summary>
    public bool IsAdapted => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Adapted, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the request failed while Cephalon was translating it.
    /// </summary>
    public bool IsFailed => string.Equals(State, CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.Failed, StringComparison.OrdinalIgnoreCase);
}
