using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Technologies;
using System.Globalization;

namespace Cephalon.Eventing.Services;

internal sealed class EventingDispatchRemediationCommandRuntimeSurfaceContributor(
    IEventDispatchRemediationRuntimeCatalog commands) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        return new TechnologyRuntimeSurface(
            technologyId: "event-driven-integration",
            surfaceId: "event-dispatch-remediation-commands",
            displayName: "Event Dispatch Remediation Commands",
            description: "Bounded operator command results recorded by the provider-neutral event-dispatch remediation dispatcher.",
            entries: commands.States
                .Select(CreateEntry)
                .ToArray());
    }

    private static TechnologyRuntimeEntry CreateEntry(EventDispatchRemediationRuntimeState state)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["commandId"] = state.CommandId,
            ["outboxId"] = state.OutboxId,
            ["messageId"] = state.MessageId,
            ["channelId"] = state.ChannelId,
            ["operationId"] = state.OperationId,
            ["outcome"] = state.Outcome,
            ["dispatchOutcome"] = state.DispatchOutcome,
            ["observedAtUtc"] = state.ObservedAtUtc.ToString("O", CultureInfo.InvariantCulture),
            ["commandScope"] = "dispatch-store",
            ["commandOperationRoute"] = "/engine/event-dispatch-remediation-commands/operations/{operationId}",
            ["commandActorRoute"] = "/engine/event-dispatch-remediation-commands/actors/{actorId}",
            ["commandCorrelationRoute"] = "/engine/event-dispatch-remediation-commands/correlations/{correlationId}",
            ["commandReasonRoute"] = "/engine/event-dispatch-remediation-commands/reasons/{reason}",
            ["commandMessageRoute"] = "/engine/event-dispatch-remediation-commands/messages/{messageId}",
            ["commandChannelRoute"] = "/engine/event-dispatch-remediation-commands/channels/{channelId}",
            ["commandDispatchOutcomeRoute"] = "/engine/event-dispatch-remediation-commands/dispatch-outcomes/{dispatchOutcome}",
            ["providerNeutral"] = "true",
            ["wolverineRequired"] = "false",
            [EventDispatchRemediationMetadataKeys.CommandIdempotencyPolicy] = "unique-command-id",
            [EventDispatchRemediationMetadataKeys.DuplicateCommandPolicy] = "reject-without-mutation",
            ["hasError"] = string.IsNullOrWhiteSpace(state.Error) ? "false" : "true"
        };

        if (!string.IsNullOrWhiteSpace(state.Error))
        {
            metadata["error"] = state.Error;
        }

        if (state.Metadata.Count > 0)
        {
            metadata["reportedMetadataKeys"] = string.Join(
                ",",
                state.Metadata.Keys.OrderBy(static key => key, StringComparer.OrdinalIgnoreCase));
            foreach (var pair in state.Metadata.OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase))
            {
                metadata[$"reported.{pair.Key}"] = pair.Value;
            }
        }

        return new TechnologyRuntimeEntry(
            id: state.CommandId,
            displayName: state.CommandId,
            description: ResolveDescription(state),
            metadata: metadata);
    }

    private static string ResolveDescription(EventDispatchRemediationRuntimeState state) =>
        string.Equals(state.Outcome, EventDispatchRemediationOutcomes.Accepted, StringComparison.OrdinalIgnoreCase)
            ? "The event-dispatch remediation command was accepted and applied through the active dispatch store."
            : "The event-dispatch remediation command was rejected before mutating dispatch-store state.";
}
