using Cephalon.Abstractions.Modules;

namespace Cephalon.Behaviors.Http.Hosting;

internal static class RestBehaviorEndpointMetadataConventions
{
    internal static string BuildOperationName(string moduleId, int? moduleVersionMajor, string behaviorId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(behaviorId);

        var versionSegment = moduleVersionMajor.HasValue
            ? $"v{moduleVersionMajor.Value}"
            : "v0";
        return $"{NormalizeSegment(moduleId)}.{versionSegment}.{NormalizeSegment(behaviorId)}";
    }

    internal static (string Summary, string? Description) ResolveOperationDocumentation(
        Type behaviorType,
        ModuleDescriptor moduleDescriptor,
        string behaviorId)
    {
        ArgumentNullException.ThrowIfNull(behaviorType);
        ArgumentNullException.ThrowIfNull(moduleDescriptor);
        ArgumentException.ThrowIfNullOrWhiteSpace(behaviorId);

        var xmlSummary = BehaviorXmlDocumentation.GetSummary(behaviorType);
        var summary = xmlSummary
            ?? behaviorId;
        var description = BehaviorXmlDocumentation.GetDescription(behaviorType);
        if (string.IsNullOrWhiteSpace(description) && string.IsNullOrWhiteSpace(xmlSummary))
        {
            description = moduleDescriptor.Description;
        }

        return (summary, description);
    }

    private static string NormalizeSegment(string value)
    {
        return string.Concat(value.Select(static ch =>
            char.IsLetterOrDigit(ch) ? ch : '_'));
    }
}
