using Cephalon.Abstractions.Data;

namespace Cephalon.Engine.Data;

internal sealed class OutboxCatalogSnapshot : IOutboxCatalog
{
    private readonly IReadOnlyList<OutboxDescriptor> outboxes;
    private readonly Dictionary<string, OutboxDescriptor> outboxesById;
    private readonly Dictionary<string, IReadOnlyList<OutboxDescriptor>> outboxesBySourceModule;
    private readonly Dictionary<string, IReadOnlyList<OutboxDescriptor>> outboxesByProvider;
    private readonly Dictionary<string, IReadOnlyList<OutboxDescriptor>> outboxesByChannelId;

    public OutboxCatalogSnapshot(
        IEnumerable<OutboxDescriptor> outboxes,
        IOutboxDispatchPolicyCatalog? dispatchPolicyCatalog = null)
    {
        ArgumentNullException.ThrowIfNull(outboxes);

        this.outboxes = outboxes
            .Select(outbox => ApplyDispatchPolicy(outbox, dispatchPolicyCatalog))
            .ToArray();
        outboxesById = this.outboxes.ToDictionary(static outbox => outbox.Id, StringComparer.OrdinalIgnoreCase);
        outboxesBySourceModule = this.outboxes
            .GroupBy(static outbox => outbox.SourceModuleId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<OutboxDescriptor>)group.ToArray(),
                StringComparer.OrdinalIgnoreCase);
        outboxesByProvider = this.outboxes
            .GroupBy(static outbox => outbox.Provider, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<OutboxDescriptor>)group.ToArray(),
                StringComparer.OrdinalIgnoreCase);
        outboxesByChannelId = this.outboxes
            .SelectMany(static outbox => outbox.ChannelIds.Select(channelId => new KeyValuePair<string, OutboxDescriptor>(channelId, outbox)))
            .GroupBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<OutboxDescriptor>)group.Select(static pair => pair.Value).ToArray(),
                StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<OutboxDescriptor> Outboxes => outboxes;

    public OutboxDescriptor? GetById(string outboxId)
    {
        if (string.IsNullOrWhiteSpace(outboxId))
        {
            return null;
        }

        return outboxesById.TryGetValue(outboxId.Trim(), out var outbox)
            ? outbox
            : null;
    }

    public IReadOnlyList<OutboxDescriptor> GetBySourceModule(string sourceModuleId)
    {
        if (string.IsNullOrWhiteSpace(sourceModuleId))
        {
            return [];
        }

        return outboxesBySourceModule.TryGetValue(sourceModuleId.Trim(), out var matches)
            ? matches
            : [];
    }

    public IReadOnlyList<OutboxDescriptor> GetByProvider(string provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            return [];
        }

        return outboxesByProvider.TryGetValue(provider.Trim(), out var matches)
            ? matches
            : [];
    }

    public IReadOnlyList<OutboxDescriptor> GetByChannelId(string channelId)
    {
        if (string.IsNullOrWhiteSpace(channelId))
        {
            return [];
        }

        return outboxesByChannelId.TryGetValue(channelId.Trim(), out var matches)
            ? matches
            : [];
    }

    private static OutboxDescriptor ApplyDispatchPolicy(
        OutboxDescriptor outbox,
        IOutboxDispatchPolicyCatalog? dispatchPolicyCatalog)
    {
        var policy = dispatchPolicyCatalog?.GetByOutboxId(outbox.Id) ?? outbox.DispatchPolicy;
        var metadata = new Dictionary<string, string>(outbox.Metadata, StringComparer.OrdinalIgnoreCase)
        {
            ["dispatchPolicyId"] = policy.PolicyId,
            ["dispatchPolicyDisplayName"] = policy.DisplayName,
            ["dispatchExecutionMode"] = policy.ExecutionMode,
            ["dispatchOwnership"] = policy.ExecutionMode,
            ["dispatchBridge"] = policy.PolicyId,
            ["dispatchStore"] = string.Equals(policy.ExecutionMode, "disabled", StringComparison.OrdinalIgnoreCase)
                ? "not-configured"
                : "available",
            ["dispatchRuntime"] = string.IsNullOrWhiteSpace(policy.RuntimeId)
                ? "not-configured"
                : "configured",
            ["dispatchRuntimeId"] = string.IsNullOrWhiteSpace(policy.RuntimeId)
                ? "not-configured"
                : policy.RuntimeId
        };

        foreach (var pair in policy.Metadata)
        {
            metadata[$"dispatchPolicy.{pair.Key}"] = pair.Value;
        }

        return new OutboxDescriptor(
            id: outbox.Id,
            displayName: outbox.DisplayName,
            description: outbox.Description,
            sourceModuleId: outbox.SourceModuleId,
            provider: outbox.Provider,
            mode: outbox.Mode,
            channelIds: outbox.ChannelIds,
            tags: outbox.Tags,
            metadata: metadata,
            dispatchPolicy: policy);
    }
}
