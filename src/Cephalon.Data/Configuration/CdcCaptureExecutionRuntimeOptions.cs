using Cephalon.Abstractions.Data;

namespace Cephalon.Data.Configuration;

/// <summary>
/// Configures one host-owned CDC execution runtime declaration for the runtime-neutral data pack.
/// </summary>
/// <remarks>
/// These options seed additional operator-facing execution-runtime surfaces. Installed modules and
/// companion packs can still contribute runtimes through
/// <see cref="Services.ICdcCaptureExecutionRuntimeContributor" />.
/// </remarks>
public sealed class CdcCaptureExecutionRuntimeOptions
{
    /// <summary>
    /// Creates CDC execution runtime options with empty identity fields and a declared-runtime topology.
    /// </summary>
    public CdcCaptureExecutionRuntimeOptions()
    {
    }

    /// <summary>
    /// Gets or sets the stable execution-runtime identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the operator-facing execution-runtime name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the human-readable execution-runtime description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the operator-facing ownership mode for the runtime.
    /// </summary>
    public string ExecutionOwnership { get; set; } = "runtime-managed";

    /// <summary>
    /// Gets or sets the operator-facing topology classification for the runtime.
    /// </summary>
    public string ExecutionTopology { get; set; } = "declared-runtime";

    /// <summary>
    /// Gets or sets the operator-facing acknowledgement mode when the runtime reports one.
    /// </summary>
    public string? AcknowledgementMode { get; set; }

    /// <summary>
    /// Gets or sets the hosted-execution identifier when the runtime maps to a Cephalon hosted execution.
    /// </summary>
    public string? HostedExecutionId { get; set; }

    /// <summary>
    /// Gets or sets the execution-graph identifier when the runtime maps to a Cephalon execution graph.
    /// </summary>
    public string? ExecutionGraphId { get; set; }

    /// <summary>
    /// Gets or sets the report-freshness window, in seconds, used to mark external runtime observations stale.
    /// </summary>
    public int? ObservationStaleAfterSeconds { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the runtime should reject out-of-order external reports.
    /// </summary>
    public bool RejectOutOfOrderReports { get; set; } = true;

    /// <summary>
    /// Gets or sets the reporter-lease window, in seconds, used to keep one external reporter authoritative for the runtime.
    /// </summary>
    public int? ReporterLeaseSeconds { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the runtime should reject reports from conflicting reporter identities while an active lease still exists.
    /// </summary>
    public bool RejectConflictingReporterIds { get; set; }

    /// <summary>
    /// Gets the CDC capture identifiers explicitly owned by the runtime when ownership is bounded to a known capture set.
    /// </summary>
    public IList<string> CdcCaptureIds { get; } = [];

    /// <summary>
    /// Gets the declared edge-node identifiers that can originate observations for the runtime.
    /// </summary>
    public IList<string> EdgeNodeIds { get; } = [];

    /// <summary>
    /// Gets arbitrary operator-facing metadata that should flow through the runtime declaration.
    /// </summary>
    public IDictionary<string, string> Metadata { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    internal CdcCaptureExecutionRuntimeDescriptor ToDescriptor()
    {
        return new CdcCaptureExecutionRuntimeDescriptor(
            id: Id,
            displayName: DisplayName,
            description: Description,
            executionOwnership: ExecutionOwnership,
            executionTopology: ExecutionTopology,
            acknowledgementMode: AcknowledgementMode,
            hostedExecutionId: HostedExecutionId,
            executionGraphId: ExecutionGraphId,
            observationStaleAfterSeconds: ObservationStaleAfterSeconds,
            rejectOutOfOrderReports: RejectOutOfOrderReports,
            metadata: Metadata.Count == 0
                ? null
                : new Dictionary<string, string>(Metadata, StringComparer.OrdinalIgnoreCase),
            cdcCaptureIds: CdcCaptureIds.ToArray(),
            reporterLeaseSeconds: ReporterLeaseSeconds,
            rejectConflictingReporterIds: RejectConflictingReporterIds,
            edgeNodeIds: EdgeNodeIds.ToArray());
    }
}
