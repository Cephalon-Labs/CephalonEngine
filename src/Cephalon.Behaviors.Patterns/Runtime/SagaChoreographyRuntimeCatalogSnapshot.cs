using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.Execution;
using Cephalon.Behaviors.Patterns.Abstractions;
using Cephalon.Behaviors.Services;

namespace Cephalon.Behaviors.Patterns.Runtime;

internal sealed class SagaChoreographyRuntimeCatalogSnapshot : ISagaChoreographyRuntimeCatalog
{
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

    private readonly SagaChoreographyRuntimeDescriptor[] sagaChoreographies;
    private readonly Dictionary<string, SagaChoreographyRuntimeDescriptor> sagaChoreographiesById;
    private readonly Dictionary<string, IReadOnlyList<SagaChoreographyRuntimeDescriptor>> sagaChoreographiesBySourceModule;
    private readonly Dictionary<string, IReadOnlyList<SagaChoreographyRuntimeDescriptor>> sagaChoreographiesByTransport;

    public SagaChoreographyRuntimeCatalogSnapshot(
        IBehaviorCatalog behaviorCatalog,
        IBehaviorTypeRegistry typeRegistry,
        IEnumerable<SagaChoreographyRuntimeSlot>? sagaChoreographyRuntimeSlots = null)
    {
        ArgumentNullException.ThrowIfNull(behaviorCatalog);
        ArgumentNullException.ThrowIfNull(typeRegistry);

        var slotsByBehaviorType = (sagaChoreographyRuntimeSlots ?? [])
            .ToDictionary(static slot => slot.BehaviorType);

        sagaChoreographies = behaviorCatalog
            .GetByPattern("saga-choreography")
            .Select(descriptor => CreateDescriptor(descriptor, typeRegistry, slotsByBehaviorType))
            .OrderBy(static descriptor => descriptor.SourceModuleId, Comparer)
            .ThenBy(static descriptor => descriptor.Id, Comparer)
            .ToArray();

        sagaChoreographiesById = sagaChoreographies.ToDictionary(static descriptor => descriptor.Id, Comparer);
        sagaChoreographiesBySourceModule = sagaChoreographies
            .Where(static descriptor => !string.IsNullOrWhiteSpace(descriptor.SourceModuleId))
            .GroupBy(static descriptor => descriptor.SourceModuleId!, Comparer)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<SagaChoreographyRuntimeDescriptor>)group.ToArray(),
                Comparer);
        sagaChoreographiesByTransport = sagaChoreographies
            .SelectMany(static descriptor => descriptor.TransportIds.Select(transportId => (TransportId: transportId, Descriptor: descriptor)))
            .GroupBy(static entry => entry.TransportId, Comparer)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<SagaChoreographyRuntimeDescriptor>)group
                    .Select(static entry => entry.Descriptor)
                    .ToArray(),
                Comparer);
    }

    public IReadOnlyList<SagaChoreographyRuntimeDescriptor> SagaChoreographies => sagaChoreographies;

    public SagaChoreographyRuntimeDescriptor? GetById(string behaviorId)
    {
        if (string.IsNullOrWhiteSpace(behaviorId))
        {
            return null;
        }

        return sagaChoreographiesById.TryGetValue(behaviorId.Trim(), out var choreography)
            ? choreography
            : null;
    }

    public IReadOnlyList<SagaChoreographyRuntimeDescriptor> GetBySourceModule(string sourceModuleId)
    {
        if (string.IsNullOrWhiteSpace(sourceModuleId))
        {
            return [];
        }

        return sagaChoreographiesBySourceModule.TryGetValue(sourceModuleId.Trim(), out var matches)
            ? matches
            : [];
    }

    public IReadOnlyList<SagaChoreographyRuntimeDescriptor> GetByTransportId(string transportId)
    {
        if (string.IsNullOrWhiteSpace(transportId))
        {
            return [];
        }

        return sagaChoreographiesByTransport.TryGetValue(transportId.Trim(), out var matches)
            ? matches
            : [];
    }

    private static SagaChoreographyRuntimeDescriptor CreateDescriptor(
        BehaviorTopologyDescriptor descriptor,
        IBehaviorTypeRegistry typeRegistry,
        Dictionary<Type, SagaChoreographyRuntimeSlot> slotsByBehaviorType)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(typeRegistry);
        ArgumentNullException.ThrowIfNull(slotsByBehaviorType);

        if (!typeRegistry.TryGetType(descriptor.Id, out var behaviorType) || behaviorType is null)
        {
            throw new InvalidOperationException(
                $"Saga choreography behavior '{descriptor.Id}' does not have a registered implementation type.");
        }

        if (!slotsByBehaviorType.TryGetValue(behaviorType, out var slot))
        {
            throw new InvalidOperationException(
                $"Saga choreography behavior '{descriptor.Id}' with implementation type '{GetTypeName(behaviorType)}' requires a source-generated or explicitly registered SagaChoreographyRuntimeSlot before its runtime catalog metadata can be projected.");
        }

        var metadata = new Dictionary<string, string>(descriptor.Metadata, Comparer)
        {
            ["pattern"] = descriptor.Pattern,
            ["capabilityKey"] = "behaviors.saga-choreography",
            ["compatibilityRuleId"] = "ABT-005",
            ["publisherContract"] = typeof(ISagaChoreographyPublisher).FullName ?? nameof(ISagaChoreographyPublisher),
            ["publicationMode"] = "choreography-publications",
            ["authoringModel"] = slot.AuthoringModel,
            ["publicationResultShape"] = slot.PublicationResultShape,
            ["apiSurfaceGroupPath"] = descriptor.ApiSurface.GroupPath,
            ["apiSurfaceOperationPath"] = descriptor.ApiSurface.OperationPath
        };

        return new SagaChoreographyRuntimeDescriptor(
            id: descriptor.Id,
            displayName: descriptor.DisplayName ?? descriptor.Id,
            description: descriptor.Description ?? $"Saga choreography behavior '{descriptor.Id}'.",
            behaviorType: GetTypeName(behaviorType),
            inputType: GetTypeName(slot.InputType),
            resultType: GetTypeName(slot.ResultType),
            localOutputType: slot.LocalOutputType,
            sourceModuleId: descriptor.SourceModuleId,
            transportIds: descriptor.TransportIds,
            requiredFeatureFlagIds: descriptor.RequiredFeatureFlagIds,
            successStatusCodes: [200, 202, 204],
            metadata: metadata);
    }

    private static string GetTypeName(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return type.FullName ?? type.Name;
    }
}
