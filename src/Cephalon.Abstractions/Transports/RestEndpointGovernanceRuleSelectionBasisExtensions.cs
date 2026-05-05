namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Provides canonical wire-name helpers for <see cref="RestEndpointGovernanceRuleSelectionBasis"/>.
/// </summary>
public static class RestEndpointGovernanceRuleSelectionBasisExtensions
{
    /// <summary>
    /// Gets the stable wire name used by JSON serialization and runtime introspection for the
    /// selection basis.
    /// </summary>
    /// <param name="basis">The selection basis.</param>
    /// <returns>The stable wire name.</returns>
    public static string GetWireName(this RestEndpointGovernanceRuleSelectionBasis basis)
    {
        return basis switch
        {
            RestEndpointGovernanceRuleSelectionBasis.Unspecified => "Unspecified",
            RestEndpointGovernanceRuleSelectionBasis.SingleMatch => "single-match",
            RestEndpointGovernanceRuleSelectionBasis.CandidateTargeting => "candidate-targeting",
            RestEndpointGovernanceRuleSelectionBasis.NarrowerCandidateSet => "narrower-candidate-set",
            RestEndpointGovernanceRuleSelectionBasis.MoreTargetDimensions => "more-target-dimensions",
            RestEndpointGovernanceRuleSelectionBasis.BehaviorTargeting => "behavior-targeting",
            RestEndpointGovernanceRuleSelectionBasis.NarrowerBehaviorScope => "narrower-behavior-scope",
            RestEndpointGovernanceRuleSelectionBasis.NarrowerAuthoringStyleScope => "narrower-authoring-style-scope",
            RestEndpointGovernanceRuleSelectionBasis.FewerTargetValues => "fewer-target-values",
            RestEndpointGovernanceRuleSelectionBasis.StableRuleId => "stable-rule-id",
            _ => throw new ArgumentOutOfRangeException(
                nameof(basis),
                basis,
                "A supported REST endpoint governance rule selection basis is required.")
        };
    }

    /// <summary>
    /// Tries to parse the stable wire name used by JSON serialization and runtime introspection into
    /// a selection basis.
    /// </summary>
    /// <param name="value">The wire name to parse.</param>
    /// <param name="basis">The parsed selection basis when the wire name is recognized.</param>
    /// <returns><see langword="true"/> when the wire name maps to a supported selection basis; otherwise, <see langword="false"/>.</returns>
    public static bool TryParseWireName(string? value, out RestEndpointGovernanceRuleSelectionBasis basis)
    {
        switch (value?.Trim())
        {
            case "Unspecified":
                basis = RestEndpointGovernanceRuleSelectionBasis.Unspecified;
                return true;
            case "single-match":
                basis = RestEndpointGovernanceRuleSelectionBasis.SingleMatch;
                return true;
            case "candidate-targeting":
                basis = RestEndpointGovernanceRuleSelectionBasis.CandidateTargeting;
                return true;
            case "narrower-candidate-set":
                basis = RestEndpointGovernanceRuleSelectionBasis.NarrowerCandidateSet;
                return true;
            case "more-target-dimensions":
                basis = RestEndpointGovernanceRuleSelectionBasis.MoreTargetDimensions;
                return true;
            case "behavior-targeting":
                basis = RestEndpointGovernanceRuleSelectionBasis.BehaviorTargeting;
                return true;
            case "narrower-behavior-scope":
                basis = RestEndpointGovernanceRuleSelectionBasis.NarrowerBehaviorScope;
                return true;
            case "narrower-authoring-style-scope":
                basis = RestEndpointGovernanceRuleSelectionBasis.NarrowerAuthoringStyleScope;
                return true;
            case "fewer-target-values":
                basis = RestEndpointGovernanceRuleSelectionBasis.FewerTargetValues;
                return true;
            case "stable-rule-id":
                basis = RestEndpointGovernanceRuleSelectionBasis.StableRuleId;
                return true;
            default:
                basis = default;
                return false;
        }
    }
}
