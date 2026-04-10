using Cephalon.Abstractions.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Eventing.Services;

internal sealed class OutboxDispatchPolicyCatalog : IOutboxDispatchPolicyCatalog
{
    private readonly Dictionary<string, OutboxDispatchPolicyDescriptor> index;

    public OutboxDispatchPolicyCatalog(
        IReadOnlyList<OutboxDescriptor> outboxes,
        IEventDispatchRuntimeDescriptorCatalog dispatchRuntimeCatalog,
        IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(outboxes);
        ArgumentNullException.ThrowIfNull(dispatchRuntimeCatalog);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        using var scope = serviceProvider.CreateScope();
        var dispatchStores = scope.ServiceProvider.GetServices<IEventDispatchStore>().ToArray();
        var storesByOutboxId = IndexDispatchStores(dispatchStores);
        var runtimesByOutboxId = IndexDispatchRuntimes(dispatchRuntimeCatalog.Runtimes);

        Policies = outboxes
            .OrderBy(static outbox => outbox.Id, StringComparer.OrdinalIgnoreCase)
            .Select(outbox => CreatePolicy(outbox, storesByOutboxId, runtimesByOutboxId))
            .ToArray();
        index = Policies.ToDictionary(static policy => policy.OutboxId, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<OutboxDispatchPolicyDescriptor> Policies { get; }

    public OutboxDispatchPolicyDescriptor? GetByOutboxId(string outboxId)
    {
        if (string.IsNullOrWhiteSpace(outboxId))
        {
            return null;
        }

        return index.TryGetValue(outboxId.Trim(), out var policy)
            ? policy
            : null;
    }

    private static OutboxDispatchPolicyDescriptor CreatePolicy(
        OutboxDescriptor outbox,
        Dictionary<string, IEventDispatchStore> storesByOutboxId,
        Dictionary<string, EventDispatchRuntimeDescriptor> runtimesByOutboxId)
    {
        storesByOutboxId.TryGetValue(outbox.Id, out var store);
        runtimesByOutboxId.TryGetValue(outbox.Id, out var runtime);

        if (runtime is not null && store is null)
        {
            throw new InvalidOperationException(
                $"Dispatch runtime '{runtime.Id}' claims outbox '{outbox.Id}', but no active IEventDispatchStore owns that outbox.");
        }

        if (runtime is not null)
        {
            var policyId = ResolveManagedPolicyId(runtime);
            var metadata = new Dictionary<string, string>(runtime.Metadata, StringComparer.OrdinalIgnoreCase)
            {
                ["dispatchStore"] = "available",
                ["dispatchRuntime"] = "configured",
                ["dispatchRuntimeId"] = runtime.Id,
                ["dispatchOwnership"] = "runtime-managed",
                ["dispatchPolicyId"] = policyId
            };

            if (store is not null)
            {
                metadata["dispatchStoreType"] = store.GetType().FullName ?? store.GetType().Name;
            }

            return new OutboxDispatchPolicyDescriptor(
                outboxId: outbox.Id,
                policyId: policyId,
                displayName: ResolveManagedDisplayName(runtime),
                description: $"Dispatch execution is owned by the '{runtime.DisplayName}' runtime.",
                executionMode: "runtime-managed",
                runtimeId: runtime.Id,
                metadata: metadata);
        }

        if (store is not null)
        {
            return new OutboxDispatchPolicyDescriptor(
                outboxId: outbox.Id,
                policyId: "consumer-managed",
                displayName: "Consumer-managed Dispatch",
                description: "A durable dispatch store is available for this outbox, but no managed dispatch runtime currently owns execution.",
                executionMode: "consumer-managed",
                metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["dispatchStore"] = "available",
                    ["dispatchRuntime"] = "not-configured",
                    ["dispatchOwnership"] = "consumer-managed",
                    ["dispatchStoreType"] = store.GetType().FullName ?? store.GetType().Name
                });
        }

        return CreateDisabledPolicy(outbox.DispatchPolicy);
    }

    private static Dictionary<string, IEventDispatchStore> IndexDispatchStores(
        IReadOnlyList<IEventDispatchStore> dispatchStores)
    {
        var storesByOutboxId = new Dictionary<string, IEventDispatchStore>(StringComparer.OrdinalIgnoreCase);

        foreach (var dispatchStore in dispatchStores)
        {
            foreach (var outboxId in dispatchStore.OutboxIds)
            {
                if (storesByOutboxId.TryGetValue(outboxId, out var existingStore))
                {
                    throw new InvalidOperationException(
                        $"Outbox '{outboxId}' is owned by multiple dispatch stores: '{existingStore.GetType().FullName}' and '{dispatchStore.GetType().FullName}'.");
                }

                storesByOutboxId[outboxId] = dispatchStore;
            }
        }

        return storesByOutboxId;
    }

    private static Dictionary<string, EventDispatchRuntimeDescriptor> IndexDispatchRuntimes(
        IReadOnlyList<EventDispatchRuntimeDescriptor> dispatchRuntimes)
    {
        var runtimesByOutboxId = new Dictionary<string, EventDispatchRuntimeDescriptor>(StringComparer.OrdinalIgnoreCase);

        foreach (var runtime in dispatchRuntimes)
        {
            foreach (var outboxId in runtime.OutboxIds)
            {
                if (runtimesByOutboxId.TryGetValue(outboxId, out var existingRuntime))
                {
                    throw new InvalidOperationException(
                        $"Outbox '{outboxId}' is claimed by multiple dispatch runtimes: '{existingRuntime.Id}' and '{runtime.Id}'.");
                }

                runtimesByOutboxId[outboxId] = runtime;
            }
        }

        return runtimesByOutboxId;
    }

    private static string ResolveManagedPolicyId(EventDispatchRuntimeDescriptor runtime)
    {
        if (runtime.Metadata.TryGetValue("dispatchBridge", out var policyId) &&
            !string.IsNullOrWhiteSpace(policyId))
        {
            return policyId.Trim();
        }

        if (runtime.Metadata.TryGetValue("dispatchOwnership", out policyId) &&
            !string.IsNullOrWhiteSpace(policyId))
        {
            return policyId.Trim();
        }

        return "runtime-managed";
    }

    private static string ResolveManagedDisplayName(EventDispatchRuntimeDescriptor runtime)
    {
        if (runtime.Metadata.TryGetValue("adapter", out var adapter) &&
            !string.IsNullOrWhiteSpace(adapter))
        {
            var normalizedAdapter = adapter.Trim().Replace("-", " ", StringComparison.Ordinal);
            return $"{char.ToUpperInvariant(normalizedAdapter[0])}{normalizedAdapter[1..]}-managed Dispatch";
        }

        return $"{runtime.DisplayName} Dispatch";
    }

    private static OutboxDispatchPolicyDescriptor CreateDisabledPolicy(
        OutboxDispatchPolicyDescriptor basePolicy)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["dispatchStore"] = "not-configured",
            ["dispatchRuntime"] = "not-configured",
            ["dispatchOwnership"] = "disabled"
        };

        foreach (var pair in basePolicy.Metadata)
        {
            metadata[pair.Key] = pair.Value;
        }

        return new OutboxDispatchPolicyDescriptor(
            outboxId: basePolicy.OutboxId,
            policyId: basePolicy.PolicyId,
            displayName: basePolicy.DisplayName,
            description: basePolicy.Description,
            executionMode: basePolicy.ExecutionMode,
            runtimeId: basePolicy.RuntimeId,
            metadata: metadata);
    }
}
