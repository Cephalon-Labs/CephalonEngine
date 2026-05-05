using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.Execution;
using Cephalon.Behaviors.Patterns.Strategies;
using Cephalon.Behaviors.Services;

namespace Cephalon.Behaviors.Patterns.Runtime;

internal sealed class DurableExecutionRuntimeCatalogSnapshot : IDurableExecutionRuntimeCatalog
{
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

    private readonly DurableExecutionRuntimeDescriptor[] durableExecutions;
    private readonly Dictionary<string, DurableExecutionRuntimeDescriptor> durableExecutionsById;
    private readonly Dictionary<string, IReadOnlyList<DurableExecutionRuntimeDescriptor>> durableExecutionsBySourceModule;
    private readonly Dictionary<string, IReadOnlyList<DurableExecutionRuntimeDescriptor>> durableExecutionsByTransport;

    public DurableExecutionRuntimeCatalogSnapshot(
        IBehaviorCatalog behaviorCatalog,
        IBehaviorTypeRegistry typeRegistry,
        IEnumerable<DurableExecutionSlot>? durableExecutionSlots = null)
    {
        ArgumentNullException.ThrowIfNull(behaviorCatalog);
        ArgumentNullException.ThrowIfNull(typeRegistry);

        var generatedSlots = BuildSlotMap(durableExecutionSlots ?? []);
        durableExecutions = behaviorCatalog
            .GetByPattern("durable-execution")
            .Select(descriptor => CreateDescriptor(descriptor, typeRegistry, generatedSlots))
            .OrderBy(static descriptor => descriptor.SourceModuleId, Comparer)
            .ThenBy(static descriptor => descriptor.Id, Comparer)
            .ToArray();

        durableExecutionsById = durableExecutions.ToDictionary(static descriptor => descriptor.Id, Comparer);
        durableExecutionsBySourceModule = durableExecutions
            .Where(static descriptor => !string.IsNullOrWhiteSpace(descriptor.SourceModuleId))
            .GroupBy(static descriptor => descriptor.SourceModuleId!, Comparer)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<DurableExecutionRuntimeDescriptor>)group.ToArray(),
                Comparer);
        durableExecutionsByTransport = durableExecutions
            .SelectMany(static descriptor => descriptor.TransportIds.Select(transportId => (TransportId: transportId, Descriptor: descriptor)))
            .GroupBy(static entry => entry.TransportId, Comparer)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<DurableExecutionRuntimeDescriptor>)group
                    .Select(static entry => entry.Descriptor)
                    .ToArray(),
                Comparer);
    }

    public IReadOnlyList<DurableExecutionRuntimeDescriptor> DurableExecutions => durableExecutions;

    public DurableExecutionRuntimeDescriptor? GetById(string behaviorId)
    {
        if (string.IsNullOrWhiteSpace(behaviorId))
        {
            return null;
        }

        return durableExecutionsById.TryGetValue(behaviorId.Trim(), out var durableExecution)
            ? durableExecution
            : null;
    }

    public IReadOnlyList<DurableExecutionRuntimeDescriptor> GetBySourceModule(string sourceModuleId)
    {
        if (string.IsNullOrWhiteSpace(sourceModuleId))
        {
            return [];
        }

        return durableExecutionsBySourceModule.TryGetValue(sourceModuleId.Trim(), out var matches)
            ? matches
            : [];
    }

    public IReadOnlyList<DurableExecutionRuntimeDescriptor> GetByTransportId(string transportId)
    {
        if (string.IsNullOrWhiteSpace(transportId))
        {
            return [];
        }

        return durableExecutionsByTransport.TryGetValue(transportId.Trim(), out var matches)
            ? matches
            : [];
    }

    private static DurableExecutionRuntimeDescriptor CreateDescriptor(
        BehaviorTopologyDescriptor descriptor,
        IBehaviorTypeRegistry typeRegistry,
        IReadOnlyDictionary<Type, DurableExecutionSlot> generatedSlots)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(typeRegistry);
        ArgumentNullException.ThrowIfNull(generatedSlots);

        if (!typeRegistry.TryGetType(descriptor.Id, out var behaviorType) || behaviorType is null)
        {
            throw new InvalidOperationException(
                $"Durable execution behavior '{descriptor.Id}' does not have a registered implementation type.");
        }

        var slot = ResolveDurableSlot(descriptor.Id, behaviorType, generatedSlots);
        var metadata = new Dictionary<string, string>(descriptor.Metadata, Comparer)
        {
            ["pattern"] = descriptor.Pattern,
            ["capabilityKey"] = "behaviors.durable-execution",
            ["compatibilityRuleId"] = "ABT-006",
            ["streamIdentityMode"] = "behavior-defined",
            ["replayMode"] = "event-store-replay",
            ["appendMode"] = "optimistic-concurrency",
            ["continuationMode"] = "domain-events",
            ["completionMode"] = "step-result",
            ["apiSurfaceGroupPath"] = descriptor.ApiSurface.GroupPath,
            ["apiSurfaceOperationPath"] = descriptor.ApiSurface.OperationPath
        };

        return new DurableExecutionRuntimeDescriptor(
            id: descriptor.Id,
            displayName: descriptor.DisplayName ?? descriptor.Id,
            description: descriptor.Description ?? $"Durable execution workflow '{descriptor.Id}'.",
            behaviorType: GetTypeName(behaviorType),
            inputType: GetTypeName(slot.InputType),
            stateType: GetTypeName(slot.StateType),
            outputType: GetTypeName(slot.OutputType),
            executionMode: "event-store-replay",
            sourceModuleId: descriptor.SourceModuleId,
            transportIds: descriptor.TransportIds,
            requiredFeatureFlagIds: descriptor.RequiredFeatureFlagIds,
            eventSourcingEnabled: descriptor.EventSourcingEnabled,
            requiresEventStore: true,
            successStatusCodes: [200, 202, 204],
            metadata: metadata);
    }

    private static Dictionary<Type, DurableExecutionSlot> BuildSlotMap(
        IEnumerable<DurableExecutionSlot> durableExecutionSlots)
    {
        var map = new Dictionary<Type, DurableExecutionSlot>();
        foreach (var slot in durableExecutionSlots)
        {
            ArgumentNullException.ThrowIfNull(slot);

            if (map.TryGetValue(slot.BehaviorType, out var existingSlot))
            {
                if (existingSlot.InputType == slot.InputType &&
                    existingSlot.StateType == slot.StateType &&
                    existingSlot.OutputType == slot.OutputType)
                {
                    continue;
                }

                throw new InvalidOperationException(
                    $"A durable execution slot for behavior type '{slot.BehaviorType.FullName}' has already been registered with a different durable contract.");
            }

            map.Add(slot.BehaviorType, slot);
        }

        return map;
    }

    private static DurableExecutionSlot ResolveDurableSlot(
        string behaviorId,
        Type behaviorType,
        IReadOnlyDictionary<Type, DurableExecutionSlot> generatedSlots)
    {
        if (generatedSlots.TryGetValue(behaviorType, out var generatedSlot))
        {
            return generatedSlot;
        }

        throw new InvalidOperationException(
            $"Durable execution behavior '{behaviorId}' with implementation type '{behaviorType.FullName}' requires a source-generated or explicitly registered DurableExecutionSlot before its runtime catalog metadata can be projected.");
    }

    private static string GetTypeName(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return type.FullName ?? type.Name;
    }
}
