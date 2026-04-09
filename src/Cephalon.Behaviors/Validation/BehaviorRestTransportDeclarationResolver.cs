using Cephalon.Abstractions.Behaviors;

namespace Cephalon.Behaviors.Validation;

/// <summary>
/// Normalizes and validates the special REST declaration rules that Cephalon applies to
/// <c>[BehaviorAllowedTransports("http.rest")]</c> and fluent topology declarations.
/// </summary>
internal static class BehaviorRestTransportDeclarationResolver
{
    private const string RestTransportId = "http.rest";

    internal static BehaviorTopologyDescriptor? Resolve(
        string behaviorId,
        Type behaviorType,
        BehaviorTopologyDescriptor? descriptor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(behaviorId);
        ArgumentNullException.ThrowIfNull(behaviorType);

        var annotationDeclaresRest = BehaviorTypeDeclaresRestAnnotation(behaviorType);
        var topologyDeclaresRest = descriptor?.TransportIds.Any(static transportId =>
            string.Equals(transportId, RestTransportId, StringComparison.OrdinalIgnoreCase)) == true;

        if (annotationDeclaresRest && topologyDeclaresRest)
        {
            throw new BehaviorSecurityException(
                behaviorId,
                $"Behavior '{behaviorId}' declares '{RestTransportId}' in both [BehaviorAllowedTransports] and a fluent topology declaration. " +
                "Choose one generic REST declaration style per behavior.");
        }

        if (descriptor is null)
        {
            if (!annotationDeclaresRest)
            {
                return null;
            }

            descriptor = new BehaviorTopologyDescriptor(behaviorId, "direct", [RestTransportId]);
        }
        else if (annotationDeclaresRest)
        {
            descriptor = CloneWithAddedTransport(descriptor, RestTransportId);
        }

        BehaviorAllowlistValidator.Validate(descriptor, behaviorType);
        return descriptor;
    }

    private static bool BehaviorTypeDeclaresRestAnnotation(Type behaviorType)
    {
        var attribute = (BehaviorAllowedTransportsAttribute?)Attribute.GetCustomAttribute(
            behaviorType,
            typeof(BehaviorAllowedTransportsAttribute));

        return attribute?.Transports.Any(static transportId =>
            string.Equals(transportId, RestTransportId, StringComparison.OrdinalIgnoreCase)) == true;
    }

    private static BehaviorTopologyDescriptor CloneWithAddedTransport(
        BehaviorTopologyDescriptor descriptor,
        string transportId)
    {
        if (descriptor.TransportIds.Any(existing =>
                string.Equals(existing, transportId, StringComparison.OrdinalIgnoreCase)))
        {
            return descriptor;
        }

        var transportIds = descriptor.TransportIds
            .Append(transportId)
            .OrderBy(static candidate => candidate, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new BehaviorTopologyDescriptor(
            descriptor.Id,
            descriptor.Pattern,
            transportIds,
            inboxEnabled: descriptor.InboxEnabled,
            outboxEnabled: descriptor.OutboxEnabled,
            eventSourcingEnabled: descriptor.EventSourcingEnabled,
            apiSurface: descriptor.ApiSurface,
            displayName: descriptor.DisplayName,
            description: descriptor.Description,
            metadata: descriptor.Metadata);
    }
}
