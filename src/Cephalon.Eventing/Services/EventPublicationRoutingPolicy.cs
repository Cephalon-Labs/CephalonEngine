using Cephalon.Abstractions.Data;
using Cephalon.Eventing.Configuration;
using System.Globalization;

namespace Cephalon.Eventing.Services;

internal static class EventPublicationRoutingPolicy
{
    public const string None = "none";

    public const string PolicyId = "event-type-map";

    public static string GetPolicyId(EventingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return options.EnablePublicationRouting ? PolicyId : None;
    }

    public static string GetAutoChannelId(EventingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return NormalizeChannelId(options.PublicationRoutingAutoChannelId);
    }

    public static string GetRouteCount(EventingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return options.PublicationRoutes.Count.ToString(CultureInfo.InvariantCulture);
    }

    public static EventPublicationRoutingDecision Resolve(
        EventPublicationRequest request,
        EventingOptions options)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(options);

        if (!options.EnablePublicationRouting)
        {
            return new EventPublicationRoutingDecision(
                EffectiveChannelId: request.ChannelId,
                Metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
        }

        var requestedChannelId = NormalizeChannelId(request.ChannelId);
        var eventType = NormalizeEventType(request.EventType);
        var autoChannelId = GetAutoChannelId(options);
        var isAutoChannel = string.Equals(
            requestedChannelId,
            autoChannelId,
            StringComparison.OrdinalIgnoreCase);
        var hasRoute = TryResolveRoute(
            eventType,
            options.PublicationRoutes,
            out var routePattern,
            out var routeChannelId);

        if (isAutoChannel)
        {
            if (!hasRoute)
            {
                throw new ArgumentException(
                    $"Event publication routing could not resolve a channel for event type '{eventType}' from auto channel '{autoChannelId}'.",
                    nameof(request));
            }

            return CreateDecision(
                effectiveChannelId: routeChannelId,
                requestedChannelId,
                autoChannelId,
                state: "routed",
                source: "auto-channel",
                routePattern,
                routeChannelId);
        }

        if (hasRoute)
        {
            if (string.Equals(requestedChannelId, routeChannelId, StringComparison.OrdinalIgnoreCase))
            {
                return CreateDecision(
                    effectiveChannelId: requestedChannelId,
                    requestedChannelId,
                    autoChannelId,
                    state: "matched",
                    source: "explicit-channel",
                    routePattern,
                    routeChannelId);
            }

            if (options.PublicationRoutingRejectMismatchedExplicitChannel)
            {
                throw new ArgumentException(
                    $"Event publication channel '{requestedChannelId}' does not match configured route '{routePattern}' for event type '{eventType}'; expected channel '{routeChannelId}'.",
                    nameof(request));
            }

            return CreateDecision(
                effectiveChannelId: requestedChannelId,
                requestedChannelId,
                autoChannelId,
                state: "mismatched",
                source: "explicit-channel",
                routePattern,
                routeChannelId);
        }

        if (options.PublicationRoutingRequireMatchedRoute)
        {
            throw new ArgumentException(
                $"Event publication routing requires a configured route for event type '{eventType}'.",
                nameof(request));
        }

        return CreateDecision(
            effectiveChannelId: requestedChannelId,
            requestedChannelId,
            autoChannelId,
            state: "unmatched",
            source: "explicit-channel",
            routePattern: string.Empty,
            routeChannelId: string.Empty);
    }

    private static EventPublicationRoutingDecision CreateDecision(
        string effectiveChannelId,
        string requestedChannelId,
        string autoChannelId,
        string state,
        string source,
        string routePattern,
        string routeChannelId)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["routingPolicy"] = PolicyId,
            ["routingState"] = state,
            ["routingSource"] = source,
            ["routingAutoChannelId"] = autoChannelId,
            ["routingRequestedChannelId"] = requestedChannelId,
            ["routingEffectiveChannelId"] = effectiveChannelId
        };

        if (!string.IsNullOrWhiteSpace(routePattern))
        {
            metadata["routingRule"] = routePattern;
        }

        if (!string.IsNullOrWhiteSpace(routeChannelId))
        {
            metadata["routingRuleChannelId"] = routeChannelId;
        }

        return new EventPublicationRoutingDecision(
            EffectiveChannelId: effectiveChannelId,
            Metadata: metadata);
    }

    private static bool TryResolveRoute(
        string eventType,
        IDictionary<string, string> routes,
        out string routePattern,
        out string routeChannelId)
    {
        if (routes.TryGetValue(eventType, out var exactChannelId))
        {
            routePattern = eventType;
            routeChannelId = NormalizeChannelId(exactChannelId);
            return true;
        }

        var wildcard = routes
            .Where(static route => !string.IsNullOrWhiteSpace(route.Key) &&
                route.Key.Trim().EndsWith('*'))
            .Select(static route => new
            {
                Pattern = route.Key.Trim(),
                Prefix = route.Key.Trim()[..^1],
                ChannelId = route.Value
            })
            .Where(route => eventType.StartsWith(route.Prefix, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(static route => route.Prefix.Length)
            .ThenBy(static route => route.Pattern, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        if (wildcard is null)
        {
            routePattern = string.Empty;
            routeChannelId = string.Empty;
            return false;
        }

        routePattern = wildcard.Pattern;
        routeChannelId = NormalizeChannelId(wildcard.ChannelId);
        return true;
    }

    private static string NormalizeChannelId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Event publication routing channel id must not be blank.", nameof(value));
        }

        return value.Trim();
    }

    private static string NormalizeEventType(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Event publication routing event type must not be blank.", nameof(value));
        }

        return value.Trim();
    }
}

internal readonly record struct EventPublicationRoutingDecision(
    string EffectiveChannelId,
    IReadOnlyDictionary<string, string> Metadata);
