using System.Text.Json.Serialization;

namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes one operator-facing CDC capture execution runtime visible to the current Cephalon runtime.
/// </summary>
public sealed class CdcCaptureExecutionRuntimeDescriptor
{
    private const string ExecutionOwnershipMetadataKey = "executionOwnership";
    private const string ExecutionTopologyMetadataKey = "executionTopology";
    private const string AcknowledgementModeMetadataKey = "acknowledgementMode";
    private const string HostedExecutionIdMetadataKey = "hostedExecutionId";
    private const string ExecutionGraphIdMetadataKey = "executionGraphId";
    private const string ObservationStaleAfterSecondsMetadataKey = "observationStaleAfterSeconds";
    private const string RejectOutOfOrderReportsMetadataKey = "rejectOutOfOrderReports";
    private const string ReporterLeaseSecondsMetadataKey = "reporterLeaseSeconds";
    private const string RejectConflictingReporterIdsMetadataKey = "rejectConflictingReporterIds";
    private const string EdgeNodeIdsMetadataKey = "edgeNodeIds";

    /// <summary>
    /// Creates a new CDC capture execution runtime descriptor.
    /// </summary>
    /// <param name="id">The stable execution-runtime identifier.</param>
    /// <param name="displayName">The operator-facing execution-runtime name.</param>
    /// <param name="description">The human-readable execution-runtime description.</param>
    /// <param name="metadata">Optional operator-facing metadata for the execution runtime.</param>
    /// <param name="cdcCaptureIds">
    /// Optional CDC capture identifiers explicitly owned by the execution runtime when ownership is bounded to a known capture set.
    /// </param>
    /// <param name="summary">Optional aggregate runtime summary describing the latest reported operator-facing state for the execution runtime.</param>
    [JsonConstructor]
    public CdcCaptureExecutionRuntimeDescriptor(
        string id,
        string displayName,
        string description,
        IReadOnlyDictionary<string, string>? metadata = null,
        IReadOnlyList<string>? cdcCaptureIds = null,
        CdcCaptureExecutionRuntimeSummary? summary = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("CDC capture execution runtime id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("CDC capture execution runtime display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("CDC capture execution runtime description is required.", nameof(description));
        }

        Id = id.Trim();
        DisplayName = displayName.Trim();
        Description = description.Trim();
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
        CdcCaptureIds = Normalize(cdcCaptureIds);
        Summary = summary ?? CdcCaptureExecutionRuntimeSummary.Empty;
        ExecutionOwnership = ResolveMetadata(Metadata, ExecutionOwnershipMetadataKey, "runtime-managed")!;
        ExecutionTopology = ResolveMetadata(Metadata, ExecutionTopologyMetadataKey, "not-configured")!;
        AcknowledgementMode = ResolveMetadata(Metadata, AcknowledgementModeMetadataKey);
        HostedExecutionId = ResolveMetadata(Metadata, HostedExecutionIdMetadataKey);
        ExecutionGraphId = ResolveMetadata(Metadata, ExecutionGraphIdMetadataKey);
        ObservationStaleAfterSeconds = ResolveNullableIntMetadata(Metadata, ObservationStaleAfterSecondsMetadataKey);
        RejectOutOfOrderReports = ResolveBooleanMetadata(Metadata, RejectOutOfOrderReportsMetadataKey);
        ReporterLeaseSeconds = ResolveNullableIntMetadata(Metadata, ReporterLeaseSecondsMetadataKey);
        RejectConflictingReporterIds = ResolveBooleanMetadata(Metadata, RejectConflictingReporterIdsMetadataKey);
        EdgeNodeIds = ResolveDelimitedMetadata(Metadata, EdgeNodeIdsMetadataKey);
    }

    /// <summary>
    /// Creates a new CDC capture execution runtime descriptor with first-class ownership and topology semantics.
    /// </summary>
    /// <param name="id">The stable execution-runtime identifier.</param>
    /// <param name="displayName">The operator-facing execution-runtime name.</param>
    /// <param name="description">The human-readable execution-runtime description.</param>
    /// <param name="executionOwnership">
    /// The operator-facing execution-ownership mode, such as <c>host-managed</c> or <c>external-managed</c>.
    /// </param>
    /// <param name="executionTopology">
    /// The operator-facing execution-topology classification, such as <c>shared-in-process-polling</c> or <c>provider-native</c>.
    /// </param>
    /// <param name="acknowledgementMode">
    /// The operator-facing acknowledgement mode when the runtime reports one, such as <c>post-stage-provider</c>.
    /// </param>
    /// <param name="hostedExecutionId">
    /// The stable hosted-execution identifier when the runtime is backed by a Cephalon hosted execution.
    /// </param>
    /// <param name="executionGraphId">
    /// The stable execution-graph identifier when the runtime is backed by a Cephalon execution graph.
    /// </param>
    /// <param name="observationStaleAfterSeconds">
    /// The optional report-freshness window, in seconds, used to decide when external runtime observations become stale.
    /// </param>
    /// <param name="rejectOutOfOrderReports">
    /// A value indicating whether the runtime should reject external observations that arrive older than the current latest report.
    /// </param>
    /// <param name="metadata">Optional operator-facing metadata for the execution runtime.</param>
    /// <param name="cdcCaptureIds">
    /// Optional CDC capture identifiers explicitly owned by the execution runtime when ownership is bounded to a known capture set.
    /// </param>
    /// <param name="summary">Optional aggregate runtime summary describing the latest reported operator-facing state for the execution runtime.</param>
    /// <param name="reporterLeaseSeconds">
    /// The optional reporter-lease window, in seconds, used to keep one external reporter authoritative for the runtime.
    /// </param>
    /// <param name="rejectConflictingReporterIds">
    /// A value indicating whether the runtime should reject reports from conflicting reporter identities while an active lease still exists.
    /// </param>
    /// <param name="edgeNodeIds">
    /// The declared edge-node identifiers that can originate observations for the runtime when the topology is edge-aware.
    /// </param>
    public CdcCaptureExecutionRuntimeDescriptor(
        string id,
        string displayName,
        string description,
        string executionOwnership,
        string executionTopology,
        string? acknowledgementMode = null,
        string? hostedExecutionId = null,
        string? executionGraphId = null,
        int? observationStaleAfterSeconds = null,
        bool rejectOutOfOrderReports = false,
        IReadOnlyDictionary<string, string>? metadata = null,
        IReadOnlyList<string>? cdcCaptureIds = null,
        CdcCaptureExecutionRuntimeSummary? summary = null,
        int? reporterLeaseSeconds = null,
        bool rejectConflictingReporterIds = false,
        IReadOnlyList<string>? edgeNodeIds = null)
        : this(
            id,
            displayName,
            description,
            MergeMetadata(
                metadata,
                executionOwnership,
                executionTopology,
                acknowledgementMode,
                hostedExecutionId,
                executionGraphId,
                observationStaleAfterSeconds,
                rejectOutOfOrderReports,
                reporterLeaseSeconds,
                rejectConflictingReporterIds,
                edgeNodeIds),
            cdcCaptureIds,
            summary)
    {
    }

    /// <summary>
    /// Gets the stable execution-runtime identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the operator-facing execution-runtime name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the human-readable execution-runtime description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets operator-facing metadata for the execution runtime.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    /// <summary>
    /// Gets the operator-facing execution-ownership mode for the runtime.
    /// </summary>
    public string ExecutionOwnership { get; }

    /// <summary>
    /// Gets the operator-facing execution-topology classification for the runtime.
    /// </summary>
    public string ExecutionTopology { get; }

    /// <summary>
    /// Gets the operator-facing acknowledgement mode for the runtime when one was declared.
    /// </summary>
    public string? AcknowledgementMode { get; }

    /// <summary>
    /// Gets the linked hosted-execution identifier when the runtime is backed by a Cephalon hosted execution.
    /// </summary>
    public string? HostedExecutionId { get; }

    /// <summary>
    /// Gets the linked execution-graph identifier when the runtime is backed by a Cephalon execution graph.
    /// </summary>
    public string? ExecutionGraphId { get; }

    /// <summary>
    /// Gets the report-freshness window, in seconds, used to mark external runtime observations stale when one was declared.
    /// </summary>
    public int? ObservationStaleAfterSeconds { get; }

    /// <summary>
    /// Gets a value indicating whether the runtime rejects out-of-order external runtime reports.
    /// </summary>
    public bool RejectOutOfOrderReports { get; }

    /// <summary>
    /// Gets the optional reporter-lease window, in seconds, used to keep one external reporter authoritative for the runtime.
    /// </summary>
    public int? ReporterLeaseSeconds { get; }

    /// <summary>
    /// Gets a value indicating whether the runtime rejects reports from conflicting reporter identities while an active reporter lease still exists.
    /// </summary>
    public bool RejectConflictingReporterIds { get; }

    /// <summary>
    /// Gets the declared edge-node identifiers that can originate observations for the runtime.
    /// </summary>
    public IReadOnlyList<string> EdgeNodeIds { get; }

    /// <summary>
    /// Gets the CDC capture identifiers explicitly owned by the execution runtime.
    /// </summary>
    public IReadOnlyList<string> CdcCaptureIds { get; }

    /// <summary>
    /// Gets the latest aggregate runtime summary reported for the execution runtime.
    /// </summary>
    public CdcCaptureExecutionRuntimeSummary Summary { get; }

    /// <summary>
    /// Gets the operator-facing managed-connector governance posture for the execution runtime.
    /// </summary>
    public CdcCaptureExecutionRuntimeManagedConnectorGovernanceStatus ManagedConnectorGovernance { get; init; } =
        new(CdcCaptureExecutionRuntimeManagedConnectorGovernanceStates.Unknown);

    /// <summary>
    /// Gets the operator-facing desired-versus-observed managed-connector drift posture for the execution runtime.
    /// </summary>
    public CdcCaptureExecutionRuntimeManagedConnectorDriftStatus ManagedConnectorDrift { get; init; } =
        new(CdcCaptureExecutionRuntimeManagedConnectorDriftStates.Unknown);

    /// <summary>
    /// Gets the operator-facing managed-connector action plan derived from remediation, governance, and drift posture.
    /// </summary>
    public CdcCaptureExecutionRuntimeManagedConnectorActionPlanStatus ManagedConnectorActionPlan { get; init; } =
        new(CdcCaptureExecutionRuntimeManagedConnectorActionPlanStates.NotApplicable);

    /// <summary>
    /// Gets the operator-facing managed-connector write-path readiness posture derived from coverage, remediation, governance, drift, and action planning.
    /// </summary>
    public CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStatus ManagedConnectorWritePathReadiness { get; init; } =
        new(CdcCaptureExecutionRuntimeManagedConnectorWritePathReadinessStates.NotApplicable);

    /// <summary>
    /// Gets the operator-facing managed-connector preflight posture derived from coverage, remediation, governance, drift, action planning, and write-path readiness.
    /// </summary>
    public CdcCaptureExecutionRuntimeManagedConnectorPreflightStatus ManagedConnectorPreflight { get; init; } =
        new(CdcCaptureExecutionRuntimeManagedConnectorPreflightStates.NotApplicable);

    /// <summary>
    /// Gets the operator-facing managed-connector dry-run posture derived from coverage, remediation, governance, drift, action planning, write-path readiness, and preflight truth.
    /// </summary>
    public CdcCaptureExecutionRuntimeManagedConnectorDryRunStatus ManagedConnectorDryRun { get; init; } =
        new(CdcCaptureExecutionRuntimeManagedConnectorDryRunStates.NotApplicable);

    /// <summary>
    /// Gets the operator-facing managed-connector execution intent derived from dry-run, preflight, write-path readiness, action planning, governance, remediation, and runtime coverage truth.
    /// </summary>
    public CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStatus ManagedConnectorExecutionIntent { get; init; } =
        new(CdcCaptureExecutionRuntimeManagedConnectorExecutionIntentStates.NotApplicable);

    /// <summary>
    /// Gets the operator-facing managed-connector execution-approval and safety-gating posture derived from execution intent, dry-run, preflight, write-path readiness, governance, remediation, and runtime coverage truth.
    /// </summary>
    public CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStatus ManagedConnectorExecutionApproval { get; init; } =
        new(CdcCaptureExecutionRuntimeManagedConnectorExecutionApprovalStates.NotApplicable);

    /// <summary>
    /// Gets the operator-facing managed-connector write-path command envelope derived from execution approval, execution intent, dry-run, preflight, and the broader shared runtime truth.
    /// </summary>
    public CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStatus ManagedConnectorCommandEnvelope { get; init; } =
        new(CdcCaptureExecutionRuntimeManagedConnectorCommandEnvelopeStates.NotApplicable);

    /// <summary>
    /// Gets the operator-facing managed-connector command-issuance posture derived from command envelopes, execution approval, execution intent, dry-run, preflight, and the broader shared runtime truth.
    /// </summary>
    public CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStatus ManagedConnectorCommandIssuance { get; init; } =
        new(CdcCaptureExecutionRuntimeManagedConnectorCommandIssuanceStates.NotApplicable);

    /// <summary>
    /// Gets the operator-facing managed-connector provider execution-adapter posture derived from command issuance, command envelopes, execution approval, execution intent, and the broader shared runtime truth.
    /// </summary>
    public CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStatus ManagedConnectorExecutionAdapter { get; init; } =
        new(CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterStates.NotApplicable);

    /// <summary>
    /// Gets the latest recorded managed-connector command-execution outcome visible on the shared runtime surface.
    /// </summary>
    public CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionResult ManagedConnectorCommandExecution { get; init; } =
        new(CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionStates.NotApplicable);

    /// <summary>
    /// Gets the operator-facing managed-connector command-retry and idempotency posture derived from the shared command lane plus bounded execution history.
    /// </summary>
    public CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStatus ManagedConnectorCommandRetry { get; init; } =
        new(CdcCaptureExecutionRuntimeManagedConnectorCommandRetryStates.NotApplicable);

    /// <summary>
    /// Gets the operator-facing managed-connector retry-execution policy derived from command retry, execution approval, execution adapter, and the broader shared runtime truth.
    /// </summary>
    public CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStatus ManagedConnectorRetryExecutionPolicy { get; init; } =
        new(CdcCaptureExecutionRuntimeManagedConnectorRetryExecutionPolicyStates.NotApplicable);

    /// <summary>
    /// Gets the operator-facing bounded managed-connector command journal derived from shared command history, command retry, and retry-execution policy truth.
    /// </summary>
    public CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStatus ManagedConnectorCommandJournal { get; init; } =
        new(CdcCaptureExecutionRuntimeManagedConnectorCommandJournalStates.NotApplicable);

    /// <summary>
    /// Gets the operator-facing managed-connector automatic background retry execution posture derived from retry policy, bounded command history, and the latest command-execution outcomes.
    /// </summary>
    public CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionStatus ManagedConnectorAutomaticRetryExecution { get; init; } =
        new(CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryExecutionStates.NotApplicable);

    /// <summary>
    /// Gets the operator-facing managed-connector automatic background retry coordination posture derived from reporter coordination, execution ownership, and the current host-owned coordination owner id.
    /// </summary>
    public CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStatus ManagedConnectorAutomaticRetryCoordination { get; init; } =
        new(CdcCaptureExecutionRuntimeManagedConnectorAutomaticRetryCoordinationStates.NotApplicable);

    private static string[] Normalize(IReadOnlyList<string>? values)
    {
        return values?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }

    private static Dictionary<string, string> MergeMetadata(
        IReadOnlyDictionary<string, string>? metadata,
        string executionOwnership,
        string executionTopology,
        string? acknowledgementMode,
        string? hostedExecutionId,
        string? executionGraphId,
        int? observationStaleAfterSeconds,
        bool rejectOutOfOrderReports,
        int? reporterLeaseSeconds,
        bool rejectConflictingReporterIds,
        IReadOnlyList<string>? edgeNodeIds)
    {
        if (string.IsNullOrWhiteSpace(executionOwnership))
        {
            throw new ArgumentException("CDC capture execution-runtime ownership is required.", nameof(executionOwnership));
        }

        if (string.IsNullOrWhiteSpace(executionTopology))
        {
            throw new ArgumentException("CDC capture execution-runtime topology is required.", nameof(executionTopology));
        }

        var normalizedMetadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);

        normalizedMetadata[ExecutionOwnershipMetadataKey] = executionOwnership.Trim();
        normalizedMetadata[ExecutionTopologyMetadataKey] = executionTopology.Trim();

        UpsertOptional(normalizedMetadata, AcknowledgementModeMetadataKey, acknowledgementMode);
        UpsertOptional(normalizedMetadata, HostedExecutionIdMetadataKey, hostedExecutionId);
        UpsertOptional(normalizedMetadata, ExecutionGraphIdMetadataKey, executionGraphId);
        UpsertOptionalInt(normalizedMetadata, ObservationStaleAfterSecondsMetadataKey, observationStaleAfterSeconds);
        UpsertOptionalBoolean(normalizedMetadata, RejectOutOfOrderReportsMetadataKey, rejectOutOfOrderReports);
        UpsertOptionalInt(normalizedMetadata, ReporterLeaseSecondsMetadataKey, reporterLeaseSeconds);
        UpsertOptionalBoolean(normalizedMetadata, RejectConflictingReporterIdsMetadataKey, rejectConflictingReporterIds);
        UpsertOptionalList(normalizedMetadata, EdgeNodeIdsMetadataKey, edgeNodeIds);

        return normalizedMetadata;
    }

    private static string[] ResolveDelimitedMetadata(
        IReadOnlyDictionary<string, string> metadata,
        string key)
    {
        if (!metadata.TryGetValue(key, out var value) ||
            string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(static item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static item => item, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string? ResolveMetadata(
        IReadOnlyDictionary<string, string> metadata,
        string key,
        string? defaultValue = null)
    {
        if (metadata.TryGetValue(key, out var value) &&
            !string.IsNullOrWhiteSpace(value))
        {
            return value.Trim();
        }

        return string.IsNullOrWhiteSpace(defaultValue)
            ? null
            : defaultValue.Trim();
    }

    private static int? ResolveNullableIntMetadata(
        IReadOnlyDictionary<string, string> metadata,
        string key)
    {
        if (!metadata.TryGetValue(key, out var value) ||
            string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return int.TryParse(value.Trim(), out var parsed) ? parsed : null;
    }

    private static bool ResolveBooleanMetadata(
        IReadOnlyDictionary<string, string> metadata,
        string key)
    {
        if (!metadata.TryGetValue(key, out var value) ||
            string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return bool.TryParse(value.Trim(), out var parsed) && parsed;
    }

    private static void UpsertOptional(
        Dictionary<string, string> metadata,
        string key,
        string? value)
    {
        var normalizedValue = string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
        if (normalizedValue is null)
        {
            metadata.Remove(key);
            return;
        }

        metadata[key] = normalizedValue;
    }

    private static void UpsertOptionalInt(
        Dictionary<string, string> metadata,
        string key,
        int? value)
    {
        if (value is null)
        {
            metadata.Remove(key);
            return;
        }

        metadata[key] = value.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private static void UpsertOptionalBoolean(
        Dictionary<string, string> metadata,
        string key,
        bool value)
    {
        if (!value)
        {
            metadata.Remove(key);
            return;
        }

        metadata[key] = bool.TrueString;
    }

    private static void UpsertOptionalList(
        Dictionary<string, string> metadata,
        string key,
        IReadOnlyList<string>? values)
    {
        var normalizedValues = Normalize(values);
        if (normalizedValues.Length == 0)
        {
            metadata.Remove(key);
            return;
        }

        metadata[key] = string.Join(",", normalizedValues);
    }
}
