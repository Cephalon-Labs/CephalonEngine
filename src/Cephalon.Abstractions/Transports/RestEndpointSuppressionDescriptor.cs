namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Describes one host-level REST endpoint suppression rule visible to the current runtime for
/// module-owned REST candidates that participate in host governance.
/// </summary>
public sealed class RestEndpointSuppressionDescriptor
{
    /// <summary>
    /// Creates a REST endpoint suppression descriptor.
    /// </summary>
    /// <param name="id">The stable suppression identifier.</param>
    /// <param name="candidateIds">The original candidate identifiers targeted by the suppression rule.</param>
    /// <param name="behaviorIds">The behavior identifiers targeted by the suppression rule.</param>
    /// <param name="sourceModuleIds">The source-module identifiers targeted by the suppression rule.</param>
    /// <param name="authoringStyles">
    /// The normalized authoring styles targeted by the suppression rule. Explicit module-DSL
    /// routes participate only when their owning route group opted into host governance.
    /// </param>
    /// <param name="apiVersionMajors">The effective API major versions targeted by the suppression rule.</param>
    /// <param name="methods">The effective HTTP methods targeted by the suppression rule.</param>
    /// <param name="relativePatterns">The shorthand relative route patterns targeted by the suppression rule.</param>
    /// <param name="routeGroupPrefixes">The published route-group prefixes targeted by the suppression rule.</param>
    /// <param name="openApiDocumentNames">
    /// The original candidate OpenAPI document names targeted by the suppression rule.
    /// </param>
    /// <param name="tagNames">
    /// The original candidate primary OpenAPI tag names targeted by the suppression rule.
    /// </param>
    /// <param name="bindingFallbackModes">
    /// The original candidate request-binding fallback modes targeted by the suppression rule.
    /// </param>
    /// <param name="targetBindings">
    /// The original candidate explicit binding descriptors targeted by the suppression rule before
    /// any override actions are applied.
    /// </param>
    /// <param name="matchedCandidateIds">
    /// The runtime candidate identifiers that matched this suppression rule, including candidates
    /// where another suppression rule won selection.
    /// </param>
    /// <param name="suppressedCandidateIds">
    /// The runtime candidate identifiers that were actually suppressed by this rule after
    /// governance selection completed.
    /// </param>
    /// <param name="skippedCandidateIds">
    /// The runtime candidate identifiers that this rule would otherwise target but skipped because
    /// the original projection did not allow host governance to participate.
    /// </param>
    /// <param name="selectionBases">
    /// The union of decisive specificity rules that selected this suppression rule for one or more
    /// runtime candidates.
    /// </param>
    public RestEndpointSuppressionDescriptor(
        string id,
        IReadOnlyList<string>? candidateIds = null,
        IReadOnlyList<string>? behaviorIds = null,
        IReadOnlyList<string>? sourceModuleIds = null,
        IReadOnlyList<string>? authoringStyles = null,
        IReadOnlyList<int>? apiVersionMajors = null,
        IReadOnlyList<string>? methods = null,
        IReadOnlyList<string>? relativePatterns = null,
        IReadOnlyList<string>? routeGroupPrefixes = null,
        IReadOnlyList<string>? openApiDocumentNames = null,
        IReadOnlyList<string>? tagNames = null,
        IReadOnlyList<RestEndpointBindingFallbackMode>? bindingFallbackModes = null,
        IReadOnlyList<RestEndpointBindingDescriptor>? targetBindings = null,
        IReadOnlyList<string>? matchedCandidateIds = null,
        IReadOnlyList<string>? suppressedCandidateIds = null,
        IReadOnlyList<string>? skippedCandidateIds = null,
        IReadOnlyList<RestEndpointGovernanceRuleSelectionBasis>? selectionBases = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("A non-empty suppression id is required.", nameof(id));
        }

        Id = id.Trim();
        CandidateIds = NormalizeList(candidateIds);
        BehaviorIds = NormalizeList(behaviorIds);
        SourceModuleIds = NormalizeList(sourceModuleIds);
        AuthoringStyles = NormalizeList(authoringStyles);
        ApiVersionMajors = NormalizeIntList(apiVersionMajors);
        Methods = NormalizeList(methods);
        RelativePatterns = NormalizeList(relativePatterns);
        RouteGroupPrefixes = NormalizeList(routeGroupPrefixes);
        OpenApiDocumentNames = NormalizeList(openApiDocumentNames);
        TagNames = NormalizeList(tagNames);
        BindingFallbackModes = NormalizeBindingFallbackModes(bindingFallbackModes);
        TargetBindings = NormalizeTargetBindings(targetBindings, nameof(targetBindings));
        MatchedCandidateIds = NormalizeOrderedList(matchedCandidateIds);
        SuppressedCandidateIds = NormalizeOrderedList(suppressedCandidateIds);
        SkippedCandidateIds = NormalizeOrderedList(skippedCandidateIds);
        SelectionBases = NormalizeSelectionBases(selectionBases, nameof(selectionBases));

        if (SuppressedCandidateIds.Except(MatchedCandidateIds, StringComparer.OrdinalIgnoreCase).Any())
        {
            throw new ArgumentException(
                "REST endpoint suppression descriptors cannot classify a candidate as suppressed unless the same candidate also appears in MatchedCandidateIds.",
                nameof(suppressedCandidateIds));
        }

        if (MatchedCandidateIds.Intersect(SkippedCandidateIds, StringComparer.OrdinalIgnoreCase).Any())
        {
            throw new ArgumentException(
                "REST endpoint suppression descriptors cannot classify the same candidate as both matched and skipped.",
                nameof(skippedCandidateIds));
        }

        if (SelectionBases.Count > 0 && SuppressedCandidateIds.Count == 0)
        {
            throw new ArgumentException(
                "REST endpoint suppression descriptors can only declare SelectionBases when at least one candidate was actually suppressed by the rule.",
                nameof(selectionBases));
        }
    }

    /// <summary>
    /// Gets the stable suppression identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the original candidate identifiers targeted by this suppression rule.
    /// </summary>
    public IReadOnlyList<string> CandidateIds { get; }

    /// <summary>
    /// Gets the behavior identifiers targeted by this suppression rule.
    /// </summary>
    public IReadOnlyList<string> BehaviorIds { get; }

    /// <summary>
    /// Gets the source-module identifiers targeted by this suppression rule.
    /// </summary>
    public IReadOnlyList<string> SourceModuleIds { get; }

    /// <summary>
    /// Gets the normalized authoring styles targeted by this suppression rule. Explicit
    /// module-DSL routes participate only when their owning route group opted into host
    /// governance.
    /// </summary>
    public IReadOnlyList<string> AuthoringStyles { get; }

    /// <summary>
    /// Gets the effective API major versions targeted by this suppression rule.
    /// </summary>
    public IReadOnlyList<int> ApiVersionMajors { get; }

    /// <summary>
    /// Gets the effective HTTP methods targeted by this suppression rule.
    /// </summary>
    public IReadOnlyList<string> Methods { get; }

    /// <summary>
    /// Gets the relative route patterns targeted by this suppression rule.
    /// </summary>
    public IReadOnlyList<string> RelativePatterns { get; }

    /// <summary>
    /// Gets the published route-group prefixes targeted by this suppression rule.
    /// </summary>
    public IReadOnlyList<string> RouteGroupPrefixes { get; }

    /// <summary>
    /// Gets the original candidate OpenAPI document names targeted by this suppression rule.
    /// </summary>
    public IReadOnlyList<string> OpenApiDocumentNames { get; }

    /// <summary>
    /// Gets the original candidate primary OpenAPI tag names targeted by this suppression rule.
    /// </summary>
    public IReadOnlyList<string> TagNames { get; }

    /// <summary>
    /// Gets the original candidate request-binding fallback modes targeted by this suppression
    /// rule.
    /// </summary>
    public IReadOnlyList<RestEndpointBindingFallbackMode> BindingFallbackModes { get; }

    /// <summary>
    /// Gets the original candidate explicit binding descriptors targeted by this suppression rule
    /// before any override actions are applied.
    /// </summary>
    public IReadOnlyList<RestEndpointBindingDescriptor> TargetBindings { get; }

    /// <summary>
    /// Gets the runtime candidate identifiers that matched this suppression rule, including
    /// candidates where another suppression rule won selection.
    /// </summary>
    public IReadOnlyList<string> MatchedCandidateIds { get; }

    /// <summary>
    /// Gets the runtime candidate identifiers that were actually suppressed by this rule after
    /// governance selection completed.
    /// </summary>
    public IReadOnlyList<string> SuppressedCandidateIds { get; }

    /// <summary>
    /// Gets the runtime candidate identifiers that this rule would otherwise target but skipped
    /// because the original projection did not allow host governance to participate.
    /// </summary>
    public IReadOnlyList<string> SkippedCandidateIds { get; }

    /// <summary>
    /// Gets the union of decisive specificity rules that selected this suppression rule for one or
    /// more runtime candidates.
    /// </summary>
    public IReadOnlyList<RestEndpointGovernanceRuleSelectionBasis> SelectionBases { get; }

    private static string[] NormalizeList(IReadOnlyList<string>? values)
    {
        return values?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }

    private static int[] NormalizeIntList(IReadOnlyList<int>? values)
    {
        return values?
            .Distinct()
            .OrderBy(static value => value)
            .ToArray() ?? [];
    }

    private static RestEndpointBindingFallbackMode[] NormalizeBindingFallbackModes(
        IReadOnlyList<RestEndpointBindingFallbackMode>? values)
    {
        if (values is null)
        {
            return [];
        }

        var normalized = values
            .Distinct()
            .OrderBy(static value => value)
            .ToArray();
        if (normalized.Any(static value => !Enum.IsDefined(value)))
        {
            throw new ArgumentOutOfRangeException(
                nameof(values),
                "REST endpoint suppression binding fallback selectors must use supported fallback modes.");
        }

        return normalized;
    }

    private static RestEndpointBindingDescriptor[] NormalizeTargetBindings(
        IReadOnlyList<RestEndpointBindingDescriptor>? values,
        string paramName)
    {
        if (values is null)
        {
            return [];
        }

        var normalized = new List<RestEndpointBindingDescriptor>(values.Count);
        var seenByProperty = new Dictionary<string, RestEndpointBindingDescriptor>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in values)
        {
            if (value is null)
            {
                continue;
            }

            var binding = new RestEndpointBindingDescriptor(
                value.PropertyName,
                value.Source,
                value.Name);
            var propertyName = binding.PropertyName.Trim();
            if (seenByProperty.TryGetValue(propertyName, out var existing))
            {
                if (existing.Source != binding.Source ||
                    !string.Equals(existing.Name, binding.Name, StringComparison.Ordinal))
                {
                    throw new ArgumentException(
                        $"REST endpoint suppression target bindings cannot declare more than one binding selector for property '{propertyName}'.",
                        paramName);
                }

                continue;
            }

            seenByProperty[propertyName] = binding;
            normalized.Add(binding);
        }

        return normalized
            .OrderBy(static binding => binding.PropertyName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static binding => binding.Source)
            .ThenBy(static binding => binding.Name ?? string.Empty, StringComparer.Ordinal)
            .ToArray();
    }

    private static string[] NormalizeOrderedList(IReadOnlyList<string>? values)
    {
        if (values is null || values.Count == 0)
        {
            return [];
        }

        var normalized = new List<string>(values.Count);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            var trimmed = value.Trim();
            if (seen.Add(trimmed))
            {
                normalized.Add(trimmed);
            }
        }

        return normalized.ToArray();
    }

    private static RestEndpointGovernanceRuleSelectionBasis[] NormalizeSelectionBases(
        IReadOnlyList<RestEndpointGovernanceRuleSelectionBasis>? values,
        string paramName)
    {
        if (values is null || values.Count == 0)
        {
            return [];
        }

        var normalized = values
            .Distinct()
            .OrderBy(static value => value)
            .ToArray();
        if (normalized.Any(static value =>
                !Enum.IsDefined(value) ||
                value == RestEndpointGovernanceRuleSelectionBasis.Unspecified))
        {
            throw new ArgumentException(
                "REST endpoint suppression selection-basis answers must use supported values.",
                paramName);
        }

        return normalized;
    }
}
