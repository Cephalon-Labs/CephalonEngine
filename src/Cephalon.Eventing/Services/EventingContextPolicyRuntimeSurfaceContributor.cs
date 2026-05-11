using Cephalon.Abstractions.Technologies;

namespace Cephalon.Eventing.Services;

internal sealed class EventingContextPolicyRuntimeSurfaceContributor(
    IEventContextPolicyCatalog contextPolicyCatalog) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        return new TechnologyRuntimeSurface(
            technologyId: "event-driven-integration",
            surfaceId: "event-context-policies",
            displayName: "Event Context Policies",
            description: "Provider-neutral tenant, correlation, causation, baggage, and message-header context policy descriptors available to the active eventing runtime.",
            entries: contextPolicyCatalog.Policies
                .Select(policy => new TechnologyRuntimeEntry(
                    id: policy.Id,
                    displayName: policy.DisplayName,
                    description: policy.Description,
                    metadata: BuildMetadata(policy)))
                .ToArray());
    }

    private static Dictionary<string, string> BuildMetadata(EventContextPolicyDescriptor policy)
    {
        var metadata = new Dictionary<string, string>(policy.Metadata, StringComparer.OrdinalIgnoreCase)
        {
            ["runtimeKind"] = policy.RuntimeKind,
            ["declaresTenantContext"] = policy.DeclaresTenantContext.ToString().ToLowerInvariant(),
            ["declaresCorrelationId"] = policy.DeclaresCorrelationId.ToString().ToLowerInvariant(),
            ["declaresCausationId"] = policy.DeclaresCausationId.ToString().ToLowerInvariant(),
            ["declaresBaggage"] = policy.DeclaresBaggage.ToString().ToLowerInvariant(),
            ["validatesMessageHeaders"] = policy.ValidatesMessageHeaders.ToString().ToLowerInvariant(),
            ["headerNames"] = string.Join(",", policy.HeaderNames),
            ["wolverineRequired"] = "false",
            ["tags"] = string.Join(",", policy.Tags)
        };

        return metadata;
    }
}
