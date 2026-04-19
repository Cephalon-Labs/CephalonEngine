using Cephalon.Abstractions.Features;

namespace Cephalon.Engine.Features;

internal sealed class InMemoryFeatureToggle(IFeatureFlagRuntimeCatalog featureFlagCatalog) : IFeatureToggle
{
    public bool IsEnabled(string featureFlagId, FeatureFlagEvaluationContext? context = null)
    {
        return Evaluate(featureFlagId, context).IsEnabled;
    }

    public FeatureFlagEvaluationResult Evaluate(string featureFlagId, FeatureFlagEvaluationContext? context = null)
    {
        if (string.IsNullOrWhiteSpace(featureFlagId))
        {
            throw new ArgumentException("Feature flag id is required.", nameof(featureFlagId));
        }

        var normalizedFeatureFlagId = featureFlagId.Trim();
        var featureFlag = featureFlagCatalog.GetById(normalizedFeatureFlagId);
        if (featureFlag is null)
        {
            return new FeatureFlagEvaluationResult(
                FeatureId: normalizedFeatureFlagId,
                IsDefined: false,
                IsEnabled: false,
                Matched: false,
                Reason: "Feature flag is not defined in the active runtime.");
        }

        if (!featureFlag.Enabled)
        {
            return CreateResult(
                featureFlag,
                isEnabled: false,
                matched: false,
                reason: "Feature flag is disabled before targeting constraints are applied.");
        }

        var effectiveContext = context ?? FeatureFlagEvaluationContext.Empty;
        if (!featureFlag.Targeting.HasValues)
        {
            return CreateResult(
                featureFlag,
                isEnabled: true,
                matched: true,
                reason: "Feature flag is enabled without targeting constraints.");
        }

        if (TryMatchExclusion(featureFlag.Targeting, effectiveContext, out var exclusionReason))
        {
            return CreateResult(
                featureFlag,
                isEnabled: false,
                matched: false,
                reason: exclusionReason);
        }

        if (TryMatchInclusionFailure(featureFlag.Targeting, effectiveContext, out var inclusionFailureReason))
        {
            return CreateResult(
                featureFlag,
                isEnabled: false,
                matched: false,
                reason: inclusionFailureReason);
        }

        return CreateResult(
            featureFlag,
            isEnabled: true,
            matched: true,
            reason: "Feature flag is enabled for the supplied runtime context.");
    }

    private static FeatureFlagEvaluationResult CreateResult(
        FeatureFlagDescriptor featureFlag,
        bool isEnabled,
        bool matched,
        string reason)
    {
        return new FeatureFlagEvaluationResult(
            FeatureId: featureFlag.Id,
            IsDefined: true,
            IsEnabled: isEnabled,
            Matched: matched,
            Reason: reason,
            SourceKind: featureFlag.SourceKind,
            SourceModuleId: featureFlag.SourceModuleId);
    }

    private static bool TryMatchExclusion(
        FeatureFlagTargetingDescriptor targeting,
        FeatureFlagEvaluationContext context,
        out string reason)
    {
        if (Matches(targeting.ExcludedEnvironmentNames, context.EnvironmentName))
        {
            reason = "Feature flag is excluded for the supplied environment.";
            return true;
        }

        if (Matches(targeting.ExcludedModuleIds, context.ModuleId))
        {
            reason = "Feature flag is excluded for the supplied module.";
            return true;
        }

        if (Matches(targeting.ExcludedBehaviorIds, context.BehaviorId))
        {
            reason = "Feature flag is excluded for the supplied behavior.";
            return true;
        }

        if (Matches(targeting.ExcludedCapabilityKeys, context.CapabilityKey))
        {
            reason = "Feature flag is excluded for the supplied capability.";
            return true;
        }

        if (Matches(targeting.ExcludedTransportIds, context.TransportId))
        {
            reason = "Feature flag is excluded for the supplied transport.";
            return true;
        }

        if (Matches(targeting.ExcludedTenantIds, context.TenantId))
        {
            reason = "Feature flag is excluded for the supplied tenant.";
            return true;
        }

        if (Matches(targeting.ExcludedSubjectIds, context.SubjectId))
        {
            reason = "Feature flag is excluded for the supplied subject.";
            return true;
        }

        if (MatchesAnyTag(targeting.ExcludedTags, context.Tags))
        {
            reason = "Feature flag is excluded for one or more supplied tags.";
            return true;
        }

        reason = string.Empty;
        return false;
    }

    private static bool TryMatchInclusionFailure(
        FeatureFlagTargetingDescriptor targeting,
        FeatureFlagEvaluationContext context,
        out string reason)
    {
        if (!MatchesRequired(targeting.IncludedEnvironmentNames, context.EnvironmentName))
        {
            reason = "Feature flag is not included for the supplied environment.";
            return true;
        }

        if (!MatchesRequired(targeting.IncludedModuleIds, context.ModuleId))
        {
            reason = "Feature flag is not included for the supplied module.";
            return true;
        }

        if (!MatchesRequired(targeting.IncludedBehaviorIds, context.BehaviorId))
        {
            reason = "Feature flag is not included for the supplied behavior.";
            return true;
        }

        if (!MatchesRequired(targeting.IncludedCapabilityKeys, context.CapabilityKey))
        {
            reason = "Feature flag is not included for the supplied capability.";
            return true;
        }

        if (!MatchesRequired(targeting.IncludedTransportIds, context.TransportId))
        {
            reason = "Feature flag is not included for the supplied transport.";
            return true;
        }

        if (!MatchesRequired(targeting.IncludedTenantIds, context.TenantId))
        {
            reason = "Feature flag is not included for the supplied tenant.";
            return true;
        }

        if (!MatchesRequired(targeting.IncludedSubjectIds, context.SubjectId))
        {
            reason = "Feature flag is not included for the supplied subject.";
            return true;
        }

        if (!MatchesRequiredTags(targeting.IncludedTags, context.Tags))
        {
            reason = "Feature flag is not included for the supplied tags.";
            return true;
        }

        reason = string.Empty;
        return false;
    }

    private static bool Matches(IReadOnlyList<string> configuredValues, string? actualValue)
    {
        return configuredValues.Count > 0 &&
               !string.IsNullOrWhiteSpace(actualValue) &&
               configuredValues.Contains(actualValue.Trim(), StringComparer.OrdinalIgnoreCase);
    }

    private static bool MatchesRequired(IReadOnlyList<string> configuredValues, string? actualValue)
    {
        if (configuredValues.Count == 0)
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(actualValue) &&
               configuredValues.Contains(actualValue.Trim(), StringComparer.OrdinalIgnoreCase);
    }

    private static bool MatchesAnyTag(IReadOnlyList<string> configuredValues, IReadOnlyList<string> actualValues)
    {
        return configuredValues.Count > 0 &&
               actualValues.Intersect(configuredValues, StringComparer.OrdinalIgnoreCase).Any();
    }

    private static bool MatchesRequiredTags(IReadOnlyList<string> configuredValues, IReadOnlyList<string> actualValues)
    {
        if (configuredValues.Count == 0)
        {
            return true;
        }

        return actualValues.Intersect(configuredValues, StringComparer.OrdinalIgnoreCase).Any();
    }
}
