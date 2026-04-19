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
        IBehaviorTypeRegistry typeRegistry)
    {
        ArgumentNullException.ThrowIfNull(behaviorCatalog);
        ArgumentNullException.ThrowIfNull(typeRegistry);

        sagaChoreographies = behaviorCatalog
            .GetByPattern("saga-choreography")
            .Select(descriptor => CreateDescriptor(descriptor, typeRegistry))
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
        IBehaviorTypeRegistry typeRegistry)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(typeRegistry);

        if (!typeRegistry.TryGetType(descriptor.Id, out var behaviorType) || behaviorType is null)
        {
            throw new InvalidOperationException(
                $"Saga choreography behavior '{descriptor.Id}' does not have a registered implementation type.");
        }

        var behaviorInterface = behaviorType
            .GetInterfaces()
            .FirstOrDefault(static candidate =>
                candidate.IsGenericType &&
                candidate.GetGenericTypeDefinition() == typeof(IAppBehavior<,>))
            ?? throw new InvalidOperationException(
                $"Behavior '{descriptor.Id}' selected the saga-choreography pattern but '{behaviorType.FullName}' does not implement IAppBehavior<TInput, TResult>.");

        var genericArguments = behaviorInterface.GetGenericArguments();
        var inputType = genericArguments[0];
        var resultType = genericArguments[1];
        var authoringModel = ResolveAuthoringModel(behaviorType);
        var resultShape = ResolveResultShape(resultType, out var localOutputType);
        var metadata = new Dictionary<string, string>(descriptor.Metadata, Comparer)
        {
            ["pattern"] = descriptor.Pattern,
            ["capabilityKey"] = "behaviors.saga-choreography",
            ["compatibilityRuleId"] = "ABT-005",
            ["publisherContract"] = typeof(ISagaChoreographyPublisher).FullName ?? nameof(ISagaChoreographyPublisher),
            ["publicationMode"] = "choreography-publications",
            ["authoringModel"] = authoringModel,
            ["publicationResultShape"] = resultShape,
            ["apiSurfaceGroupPath"] = descriptor.ApiSurface.GroupPath,
            ["apiSurfaceOperationPath"] = descriptor.ApiSurface.OperationPath
        };

        return new SagaChoreographyRuntimeDescriptor(
            id: descriptor.Id,
            displayName: descriptor.DisplayName ?? descriptor.Id,
            description: descriptor.Description ?? $"Saga choreography behavior '{descriptor.Id}'.",
            behaviorType: GetTypeName(behaviorType),
            inputType: GetTypeName(inputType),
            resultType: GetTypeName(resultType),
            localOutputType: localOutputType,
            sourceModuleId: descriptor.SourceModuleId,
            transportIds: descriptor.TransportIds,
            requiredFeatureFlagIds: descriptor.RequiredFeatureFlagIds,
            successStatusCodes: [200, 202, 204],
            metadata: metadata);
    }

    private static string ResolveAuthoringModel(Type behaviorType)
    {
        ArgumentNullException.ThrowIfNull(behaviorType);

        return behaviorType
            .GetInterfaces()
            .Any(static candidate =>
                candidate.IsGenericType &&
                (candidate.GetGenericTypeDefinition() == typeof(ISagaEventReactor<>) ||
                 candidate.GetGenericTypeDefinition() == typeof(ISagaEventReactor<,>)))
            ? "reactor"
            : "behavior";
    }

    private static string ResolveResultShape(Type resultType, out string? localOutputType)
    {
        ArgumentNullException.ThrowIfNull(resultType);

        localOutputType = null;

        if (resultType == typeof(SagaChoreographyPublication))
        {
            return "single-publication";
        }

        if (IsPublicationSequence(resultType))
        {
            return "publication-sequence";
        }

        if (resultType == typeof(SagaChoreographyStepResult))
        {
            return "step-result";
        }

        if (resultType.IsGenericType &&
            resultType.GetGenericTypeDefinition() == typeof(SagaChoreographyStepResult<>))
        {
            localOutputType = GetTypeName(resultType.GetGenericArguments()[0]);
            return "typed-step-result";
        }

        if (resultType == typeof(ISagaChoreographyStepResult))
        {
            return "step-result-contract";
        }

        if (typeof(ISagaChoreographyStepResult).IsAssignableFrom(resultType))
        {
            return "custom-step-result";
        }

        return "custom-result";
    }

    private static bool IsPublicationSequence(Type resultType)
    {
        ArgumentNullException.ThrowIfNull(resultType);

        if (resultType.IsArray)
        {
            return resultType.GetElementType() == typeof(SagaChoreographyPublication);
        }

        if (!resultType.IsGenericType)
        {
            return false;
        }

        if (resultType.GetGenericTypeDefinition() == typeof(IEnumerable<>) &&
            resultType.GetGenericArguments()[0] == typeof(SagaChoreographyPublication))
        {
            return true;
        }

        return resultType
            .GetInterfaces()
            .Any(static candidate =>
                candidate.IsGenericType &&
                candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>) &&
                candidate.GetGenericArguments()[0] == typeof(SagaChoreographyPublication));
    }

    private static string GetTypeName(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return type.FullName ?? type.Name;
    }
}
