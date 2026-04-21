using System.Text.Json.Serialization;

namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes how a CDC capture binds to an operator-facing execution runtime.
/// </summary>
public sealed class CdcCaptureExecutionBindingDescriptor
{
    private const string ExecutionTopologyMetadataKey = "executionTopology";

    /// <summary>
    /// Creates a new CDC capture execution binding descriptor.
    /// </summary>
    /// <param name="cdcCaptureId">The stable CDC capture identifier.</param>
    /// <param name="authoredExecutionRuntimeId">
    /// The execution-runtime identifier authored directly on the CDC capture when one was declared.
    /// </param>
    /// <param name="requestedExecutionRuntimeId">
    /// The execution-runtime identifier requested for the CDC capture after any additive overrides are applied.
    /// </param>
    /// <param name="effectiveExecutionRuntimeId">
    /// The execution-runtime identifier that currently owns execution for the CDC capture.
    /// </param>
    /// <param name="executionOwnership">
    /// The operator-facing ownership mode for the effective execution runtime, such as <c>host-managed</c> or <c>external-managed</c>.
    /// </param>
    /// <param name="resolutionMode">
    /// The operator-facing reason that explains how the effective execution-runtime binding was selected.
    /// </param>
    /// <param name="metadata">Optional operator-facing metadata for the resolved binding.</param>
    [JsonConstructor]
    public CdcCaptureExecutionBindingDescriptor(
        string cdcCaptureId,
        string? authoredExecutionRuntimeId = null,
        string? requestedExecutionRuntimeId = null,
        string? effectiveExecutionRuntimeId = null,
        string executionOwnership = "not-configured",
        string resolutionMode = "unbound",
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(cdcCaptureId))
        {
            throw new ArgumentException("CDC capture id is required.", nameof(cdcCaptureId));
        }

        if (string.IsNullOrWhiteSpace(executionOwnership))
        {
            throw new ArgumentException("CDC capture execution ownership is required.", nameof(executionOwnership));
        }

        if (string.IsNullOrWhiteSpace(resolutionMode))
        {
            throw new ArgumentException("CDC capture execution resolution mode is required.", nameof(resolutionMode));
        }

        CdcCaptureId = cdcCaptureId.Trim();
        AuthoredExecutionRuntimeId = Normalize(authoredExecutionRuntimeId);
        RequestedExecutionRuntimeId = Normalize(requestedExecutionRuntimeId);
        EffectiveExecutionRuntimeId = Normalize(effectiveExecutionRuntimeId);
        ExecutionOwnership = executionOwnership.Trim();
        ResolutionMode = resolutionMode.Trim();
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
        ExecutionTopology = ResolveMetadata(Metadata, ExecutionTopologyMetadataKey, "not-configured")!;
    }

    /// <summary>
    /// Creates a new CDC capture execution binding descriptor with a first-class topology classification.
    /// </summary>
    /// <param name="cdcCaptureId">The stable CDC capture identifier.</param>
    /// <param name="authoredExecutionRuntimeId">
    /// The execution-runtime identifier authored directly on the CDC capture when one was declared.
    /// </param>
    /// <param name="requestedExecutionRuntimeId">
    /// The execution-runtime identifier requested for the CDC capture after any additive overrides are applied.
    /// </param>
    /// <param name="effectiveExecutionRuntimeId">
    /// The execution-runtime identifier that currently owns execution for the CDC capture.
    /// </param>
    /// <param name="executionOwnership">
    /// The operator-facing ownership mode for the effective execution runtime, such as <c>host-managed</c> or <c>external-managed</c>.
    /// </param>
    /// <param name="executionTopology">
    /// The operator-facing topology classification for the effective execution runtime, such as <c>shared-in-process-polling</c> or <c>provider-native</c>.
    /// </param>
    /// <param name="resolutionMode">
    /// The operator-facing reason that explains how the effective execution-runtime binding was selected.
    /// </param>
    /// <param name="metadata">Optional operator-facing metadata for the resolved binding.</param>
    public CdcCaptureExecutionBindingDescriptor(
        string cdcCaptureId,
        string? authoredExecutionRuntimeId,
        string? requestedExecutionRuntimeId,
        string? effectiveExecutionRuntimeId,
        string executionOwnership,
        string executionTopology,
        string resolutionMode = "unbound",
        IReadOnlyDictionary<string, string>? metadata = null)
        : this(
            cdcCaptureId,
            authoredExecutionRuntimeId,
            requestedExecutionRuntimeId,
            effectiveExecutionRuntimeId,
            executionOwnership,
            resolutionMode,
            MergeMetadata(metadata, executionTopology))
    {
    }

    /// <summary>
    /// Gets the stable CDC capture identifier.
    /// </summary>
    public string CdcCaptureId { get; }

    /// <summary>
    /// Gets the execution-runtime identifier authored directly on the CDC capture when one was declared.
    /// </summary>
    public string? AuthoredExecutionRuntimeId { get; }

    /// <summary>
    /// Gets the execution-runtime identifier requested for the CDC capture after additive overrides are applied.
    /// </summary>
    public string? RequestedExecutionRuntimeId { get; }

    /// <summary>
    /// Gets the execution-runtime identifier that currently owns execution for the CDC capture.
    /// </summary>
    public string? EffectiveExecutionRuntimeId { get; }

    /// <summary>
    /// Gets the operator-facing ownership mode for the effective execution runtime.
    /// </summary>
    public string ExecutionOwnership { get; }

    /// <summary>
    /// Gets the operator-facing topology classification for the effective execution runtime.
    /// </summary>
    public string ExecutionTopology { get; }

    /// <summary>
    /// Gets the operator-facing explanation for how the effective execution-runtime binding was selected.
    /// </summary>
    public string ResolutionMode { get; }

    /// <summary>
    /// Gets operator-facing metadata for the resolved binding.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    /// <summary>
    /// Gets a value indicating whether the CDC capture currently resolves to an active execution runtime.
    /// </summary>
    public bool IsBound => !string.IsNullOrWhiteSpace(EffectiveExecutionRuntimeId);

    /// <summary>
    /// Creates the default unbound execution-binding descriptor for the requested CDC capture.
    /// </summary>
    /// <param name="cdcCaptureId">The CDC capture identifier to bind.</param>
    /// <returns>An unbound execution-binding descriptor.</returns>
    public static CdcCaptureExecutionBindingDescriptor Unbound(string cdcCaptureId)
    {
        return new CdcCaptureExecutionBindingDescriptor(cdcCaptureId);
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static Dictionary<string, string> MergeMetadata(
        IReadOnlyDictionary<string, string>? metadata,
        string executionTopology)
    {
        if (string.IsNullOrWhiteSpace(executionTopology))
        {
            throw new ArgumentException("CDC capture execution topology is required.", nameof(executionTopology));
        }

        var normalizedMetadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
        normalizedMetadata[ExecutionTopologyMetadataKey] = executionTopology.Trim();
        return normalizedMetadata;
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
}
