using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.Features;
using Cephalon.Behaviors.Services;

namespace Cephalon.Behaviors.Features;

internal static class BehaviorFeatureFlagEvaluationContextFactory
{
    internal static FeatureFlagEvaluationContext Create(
        BehaviorExecutionInvocation invocation,
        string? environmentName)
    {
        ArgumentNullException.ThrowIfNull(invocation);

        var capabilityKey = ResolveMetadata(invocation.Context.Metadata, "CapabilityKey")
            ?? ResolveMetadata(invocation.Descriptor.Metadata, "CapabilityKey");
        var transportId = ResolveMetadata(invocation.Context.Metadata, "TransportId");
        if (string.IsNullOrWhiteSpace(transportId) &&
            invocation.Descriptor.TransportIds.Count == 1)
        {
            transportId = invocation.Descriptor.TransportIds[0];
        }

        var tenantId = ResolveMetadata(invocation.Context.Metadata, "TenantId");
        var subjectId = ResolveMetadata(invocation.Context.Metadata, "SubjectId")
            ?? ResolveMetadata(invocation.Context.Metadata, "UserId");
        var tags = ResolveTags(invocation);

        return new FeatureFlagEvaluationContext(
            environmentName: NormalizeOptional(environmentName)
                ?? ResolveMetadata(invocation.Context.Metadata, "EnvironmentName"),
            moduleId: invocation.Descriptor.SourceModuleId,
            behaviorId: invocation.BehaviorId,
            capabilityKey: capabilityKey,
            transportId: transportId,
            tenantId: tenantId,
            subjectId: subjectId,
            tags: tags);
    }

    private static string[] ResolveTags(BehaviorExecutionInvocation invocation)
    {
        var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddTags(tags, ResolveMetadata(invocation.Context.Metadata, "FeatureFlagTags"));
        AddTags(tags, ResolveMetadata(invocation.Context.Metadata, "Tags"));
        AddTags(tags, ResolveMetadata(invocation.Descriptor.Metadata, "FeatureFlagTags"));
        AddTags(tags, ResolveMetadata(invocation.Descriptor.Metadata, "Tags"));

        return tags
            .OrderBy(static tag => tag, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static void AddTags(HashSet<string> tags, string? rawValue)
    {
        ArgumentNullException.ThrowIfNull(tags);

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return;
        }

        foreach (var tag in rawValue.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            tags.Add(tag);
        }
    }

    private static string? ResolveMetadata(
        IReadOnlyDictionary<string, string> metadata,
        string key)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (metadata.TryGetValue(key, out var value))
        {
            return NormalizeOptional(value);
        }

        foreach (var entry in metadata)
        {
            if (string.Equals(entry.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                return NormalizeOptional(entry.Value);
            }
        }

        return null;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
