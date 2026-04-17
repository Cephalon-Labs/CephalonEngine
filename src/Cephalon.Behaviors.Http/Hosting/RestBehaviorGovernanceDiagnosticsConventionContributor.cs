using Cephalon.Engine.Diagnostics;

namespace Cephalon.Behaviors.Http.Hosting;

internal sealed class RestBehaviorGovernanceDiagnosticsConventionContributor : IDiagnosticsConventionContributor
{
    public DiagnosticsConvention DescribeDiagnosticsConvention() => RestBehaviorGovernanceDiagnosticsConventions.Convention;
}

internal static class RestBehaviorGovernanceDiagnosticsConventions
{
    public const int GovernanceSuppressedId = 5200;
    public const int PrecedenceSuppressedId = 5201;
    public const int OverrideAppliedId = 5202;
    public const int OverrideNoOpId = 5203;
    public const int BindingFallbackPreservedId = 5204;
    public const int AuthoringPolicySuppressedId = 5205;
    public const int GovernanceSkippedId = 5206;

    public const string GovernanceSuppressedName = "RestEndpointGovernanceSuppressed";
    public const string PrecedenceSuppressedName = "RestEndpointPrecedenceSuppressed";
    public const string OverrideAppliedName = "RestEndpointOverrideApplied";
    public const string OverrideNoOpName = "RestEndpointOverrideNoOp";
    public const string BindingFallbackPreservedName = "RestEndpointBindingFallbackPreserved";
    public const string AuthoringPolicySuppressedName = "RestEndpointAuthoringPolicySuppressed";
    public const string GovernanceSkippedName = "RestEndpointGovernanceSkipped";

    public const string GovernanceSuppressedMessageTemplate = "REST endpoint candidate '{CandidateId}' for behavior '{BehaviorId}' from authoring style '{AuthoringStyle}' was suppressed by governance rule '{SuppressionId}' with suppression selection basis '{SuppressionSelectionBasis}'. Matched suppressions {MatchedSuppressionIds}.";
    public const string PrecedenceSuppressedMessageTemplate = "REST endpoint candidate '{CandidateId}' for behavior '{BehaviorId}' from authoring style '{AuthoringStyle}' was suppressed by higher-precedence candidate '{WinningCandidateId}' from authoring style '{WinningAuthoringStyle}'.";
    public const string OverrideAppliedMessageTemplate = "REST endpoint candidate '{CandidateId}' for behavior '{BehaviorId}' applied governance override '{OverrideId}' with override selection basis '{OverrideSelectionBasis}', selected action kind(s) {SelectedOverrideActionKinds}, applied action kind(s) {AppliedOverrideActionKinds}, and published route '{RoutePattern}'.";
    public const string OverrideNoOpMessageTemplate = "REST endpoint candidate '{CandidateId}' for behavior '{BehaviorId}' selected governance override '{SelectedOverrideId}' from matched override(s) {MatchedOverrideIds} with override selection basis '{OverrideSelectionBasis}', selected action kind(s) {SelectedOverrideActionKinds}, and applied action kind(s) {AppliedOverrideActionKinds} without changing the published runtime answer.";
    public const string BindingFallbackPreservedMessageTemplate = "REST endpoint candidate '{CandidateId}' for behavior '{BehaviorId}' preserved binding fallback mode '{BindingFallbackMode}' while reconciling governance override(s) {MatchedOverrideIds}.";
    public const string AuthoringPolicySuppressedMessageTemplate = "REST endpoint candidate '{CandidateId}' for behavior '{BehaviorId}' from authoring style '{AuthoringStyle}' was suppressed by authoring policy '{SuppressionKind}'. {SuppressionReason}";
    public const string GovernanceSkippedMessageTemplate = "REST endpoint candidate '{CandidateId}' for behavior '{BehaviorId}' from authoring style '{AuthoringStyle}' skipped host governance because the original projection did not allow host governance. Skipped suppressions {SkippedSuppressionIds}. Skipped overrides {SkippedOverrideIds}.";

    public static readonly DiagnosticEventDefinition GovernanceSuppressed = new(
        Id: GovernanceSuppressedId,
        Name: GovernanceSuppressedName,
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: GovernanceSuppressedMessageTemplate,
        Description: "Emitted when a REST governance suppression rule hides one behavior-backed candidate and records the decisive suppression rule-selection basis.");

    public static readonly DiagnosticEventDefinition PrecedenceSuppressed = new(
        Id: PrecedenceSuppressedId,
        Name: PrecedenceSuppressedName,
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: PrecedenceSuppressedMessageTemplate,
        Description: "Emitted when a REST behavior candidate loses publication because another authoring style has higher precedence.");

    public static readonly DiagnosticEventDefinition OverrideApplied = new(
        Id: OverrideAppliedId,
        Name: OverrideAppliedName,
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: OverrideAppliedMessageTemplate,
        Description: "Emitted when a matched REST governance override materially changes the published runtime answer and reports both the decisive override rule-selection basis and the selected-versus-applied override action dimensions.");

    public static readonly DiagnosticEventDefinition OverrideNoOp = new(
        Id: OverrideNoOpId,
        Name: OverrideNoOpName,
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: OverrideNoOpMessageTemplate,
        Description: "Emitted when a REST governance override matches a candidate but becomes a no-op after runtime-truth reconciliation while preserving both the decisive override rule-selection basis and the selected-versus-applied override action dimensions.");

    public static readonly DiagnosticEventDefinition BindingFallbackPreserved = new(
        Id: BindingFallbackPreservedId,
        Name: BindingFallbackPreservedName,
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: BindingFallbackPreservedMessageTemplate,
        Description: "Emitted when shorthand REST binding fallback remains visible after partial explicit override reconciliation.");

    public static readonly DiagnosticEventDefinition AuthoringPolicySuppressed = new(
        Id: AuthoringPolicySuppressedId,
        Name: AuthoringPolicySuppressedName,
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: AuthoringPolicySuppressedMessageTemplate,
        Description: "Emitted when authoring-policy enforcement suppresses one shorthand REST candidate.");

    public static readonly DiagnosticEventDefinition GovernanceSkipped = new(
        Id: GovernanceSkippedId,
        Name: GovernanceSkippedName,
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: GovernanceSkippedMessageTemplate,
        Description: "Emitted when host suppression or override rules target a behavior-backed REST candidate that did not opt into host governance.");

    public static readonly DiagnosticsConvention Convention = new(
        Source: "Cephalon.Behaviors.Http",
        LoggerCategoryPrefix: "Cephalon.Behaviors.Http",
        Description: "Structured governance, authoring-policy, precedence, override, override-action, skipped-governance, no-op, and fallback-preservation diagnostics for behavior-backed REST governance.",
        Events:
        [
            GovernanceSuppressed,
            PrecedenceSuppressed,
            OverrideApplied,
            OverrideNoOp,
            BindingFallbackPreserved,
            AuthoringPolicySuppressed,
            GovernanceSkipped
        ]);
}
