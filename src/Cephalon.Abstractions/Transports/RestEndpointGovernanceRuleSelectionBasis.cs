using System.Text.Json.Serialization;

namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Describes the earliest decisive specificity rule that selected one matching REST governance rule
/// over another.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<RestEndpointGovernanceRuleSelectionBasis>))]
public enum RestEndpointGovernanceRuleSelectionBasis
{
    /// <summary>
    /// The selection basis was not classified.
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// Only one governance rule matched the candidate, so no tie-breaker was required.
    /// </summary>
    [JsonStringEnumMemberName("single-match")]
    SingleMatch = 1,

    /// <summary>
    /// A rule that targeted explicit candidate ids won over a broader rule that did not.
    /// </summary>
    [JsonStringEnumMemberName("candidate-targeting")]
    CandidateTargeting = 2,

    /// <summary>
    /// A rule that targeted a smaller candidate-id set won over a broader candidate-targeted rule.
    /// </summary>
    [JsonStringEnumMemberName("narrower-candidate-set")]
    NarrowerCandidateSet = 3,

    /// <summary>
    /// A rule that constrained more selector dimensions won over a less specific rule.
    /// </summary>
    [JsonStringEnumMemberName("more-target-dimensions")]
    MoreTargetDimensions = 4,

    /// <summary>
    /// A rule that explicitly targeted behaviors won over a broader module-level rule.
    /// </summary>
    [JsonStringEnumMemberName("behavior-targeting")]
    BehaviorTargeting = 5,

    /// <summary>
    /// A rule that constrained fewer authoring styles won over a broader authoring-style scope.
    /// </summary>
    [JsonStringEnumMemberName("narrower-authoring-style-scope")]
    NarrowerAuthoringStyleScope = 6,

    /// <summary>
    /// A rule with fewer total selector values won over an otherwise equally ranked broader rule.
    /// </summary>
    [JsonStringEnumMemberName("fewer-target-values")]
    FewerTargetValues = 7,

    /// <summary>
    /// The winning rule was selected by the final stable rule-id tie-breaker.
    /// </summary>
    [JsonStringEnumMemberName("stable-rule-id")]
    StableRuleId = 8
}
