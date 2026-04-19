using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.Execution;
using Cephalon.Behaviors.Patterns.Abstractions;
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
        IBehaviorTypeRegistry typeRegistry)
    {
        ArgumentNullException.ThrowIfNull(behaviorCatalog);
        ArgumentNullException.ThrowIfNull(typeRegistry);

        durableExecutions = behaviorCatalog
            .GetByPattern("durable-execution")
            .Select(descriptor => CreateDescriptor(descriptor, typeRegistry))
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
        IBehaviorTypeRegistry typeRegistry)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(typeRegistry);

        if (!typeRegistry.TryGetType(descriptor.Id, out var behaviorType) || behaviorType is null)
        {
            throw new InvalidOperationException(
                $"Durable execution behavior '{descriptor.Id}' does not have a registered implementation type.");
        }

        var durableInterface = behaviorType
            .GetInterfaces()
            .FirstOrDefault(static candidate =>
                candidate.IsGenericType &&
                candidate.GetGenericTypeDefinition() == typeof(IDurableExecution<,,>))
            ?? throw new InvalidOperationException(
                $"Behavior '{descriptor.Id}' selected the durable-execution pattern but '{behaviorType.FullName}' does not implement IDurableExecution<TInput, TState, TOutput>.");

        var genericArguments = durableInterface.GetGenericArguments();
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
            inputType: GetTypeName(genericArguments[0]),
            stateType: GetTypeName(genericArguments[1]),
            outputType: GetTypeName(genericArguments[2]),
            executionMode: "event-store-replay",
            sourceModuleId: descriptor.SourceModuleId,
            transportIds: descriptor.TransportIds,
            requiredFeatureFlagIds: descriptor.RequiredFeatureFlagIds,
            eventSourcingEnabled: descriptor.EventSourcingEnabled,
            requiresEventStore: true,
            successStatusCodes: [200, 202, 204],
            metadata: metadata);
    }

    private static string GetTypeName(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return type.FullName ?? type.Name;
    }
}
