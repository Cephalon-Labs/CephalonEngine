namespace Cephalon.Abstractions.Data;

/// <summary>
/// Describes the active dispatch-execution policy for one durable outbox surface.
/// </summary>
public sealed class OutboxDispatchPolicyDescriptor
{
    /// <summary>
    /// Creates a new outbox dispatch-policy descriptor.
    /// </summary>
    /// <param name="outboxId">The stable outbox identifier that the policy applies to.</param>
    /// <param name="policyId">The stable dispatch-policy identifier.</param>
    /// <param name="displayName">The operator-facing dispatch-policy name.</param>
    /// <param name="description">The human-readable dispatch-policy description.</param>
    /// <param name="executionMode">
    /// The execution ownership mode, such as <c>disabled</c>, <c>consumer-managed</c>, or <c>runtime-managed</c>.
    /// </param>
    /// <param name="runtimeId">
    /// The optional dispatch-runtime identifier that explicitly owns execution for the outbox when the policy is runtime-managed.
    /// </param>
    /// <param name="metadata">Optional operator-facing metadata associated with the dispatch policy.</param>
    public OutboxDispatchPolicyDescriptor(
        string outboxId,
        string policyId,
        string displayName,
        string description,
        string executionMode,
        string? runtimeId = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(outboxId))
        {
            throw new ArgumentException("Outbox id is required.", nameof(outboxId));
        }

        if (string.IsNullOrWhiteSpace(policyId))
        {
            throw new ArgumentException("Dispatch policy id is required.", nameof(policyId));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Dispatch policy display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Dispatch policy description is required.", nameof(description));
        }

        if (string.IsNullOrWhiteSpace(executionMode))
        {
            throw new ArgumentException("Dispatch policy execution mode is required.", nameof(executionMode));
        }

        OutboxId = outboxId.Trim();
        PolicyId = policyId.Trim();
        DisplayName = displayName.Trim();
        Description = description.Trim();
        ExecutionMode = executionMode.Trim();
        RuntimeId = string.IsNullOrWhiteSpace(runtimeId) ? null : runtimeId.Trim();
        Metadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the stable outbox identifier that the policy applies to.
    /// </summary>
    public string OutboxId { get; }

    /// <summary>
    /// Gets the stable dispatch-policy identifier.
    /// </summary>
    public string PolicyId { get; }

    /// <summary>
    /// Gets the operator-facing dispatch-policy name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the human-readable dispatch-policy description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the execution ownership mode for the outbox.
    /// </summary>
    public string ExecutionMode { get; }

    /// <summary>
    /// Gets the optional dispatch-runtime identifier that explicitly owns execution for the outbox.
    /// </summary>
    public string? RuntimeId { get; }

    /// <summary>
    /// Gets operator-facing metadata associated with the dispatch policy.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    /// <summary>
    /// Creates the default disabled dispatch policy for an outbox.
    /// </summary>
    /// <param name="outboxId">The stable outbox identifier.</param>
    /// <param name="metadata">Optional operator-facing metadata associated with the policy.</param>
    /// <returns>The default disabled dispatch policy descriptor.</returns>
    public static OutboxDispatchPolicyDescriptor Disabled(
        string outboxId,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        return new OutboxDispatchPolicyDescriptor(
            outboxId: outboxId,
            policyId: "disabled",
            displayName: "Dispatch Disabled",
            description: "No durable dispatch execution path currently owns staged-event handoff for this outbox.",
            executionMode: "disabled",
            metadata: metadata);
    }

    /// <summary>
    /// Creates an explicit unsupported dispatch policy for an outbox that can stage messages but does not
    /// currently support Cephalon-managed mutable dispatch-state ownership.
    /// </summary>
    /// <param name="outboxId">The stable outbox identifier.</param>
    /// <param name="description">
    /// An optional operator-facing description explaining why the current provider intentionally remains
    /// outside the managed-dispatch contract.
    /// </param>
    /// <param name="metadata">Optional operator-facing metadata associated with the policy.</param>
    /// <returns>The explicit unsupported dispatch policy descriptor.</returns>
    public static OutboxDispatchPolicyDescriptor Unsupported(
        string outboxId,
        string? description = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        var policyMetadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["dispatchStore"] = "unsupported",
            ["dispatchRuntime"] = "unsupported",
            ["dispatchRuntimeId"] = "not-supported",
            ["dispatchOwnership"] = "disabled"
        };

        if (metadata is not null)
        {
            foreach (var pair in metadata)
            {
                policyMetadata[pair.Key] = pair.Value;
            }
        }

        return new OutboxDispatchPolicyDescriptor(
            outboxId: outboxId,
            policyId: "unsupported",
            displayName: "Dispatch Unsupported",
            description: string.IsNullOrWhiteSpace(description)
                ? "This outbox can stage durable events, but the current provider pack does not yet support Cephalon-managed mutable dispatch-state ownership for it."
                : description,
            executionMode: "disabled",
            metadata: policyMetadata);
    }
}
