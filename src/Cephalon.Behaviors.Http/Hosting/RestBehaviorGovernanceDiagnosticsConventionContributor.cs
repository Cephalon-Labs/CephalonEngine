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

    public const string GovernanceSuppressedName = "RestEndpointGovernanceSuppressed";
    public const string PrecedenceSuppressedName = "RestEndpointPrecedenceSuppressed";
    public const string OverrideAppliedName = "RestEndpointOverrideApplied";
    public const string OverrideNoOpName = "RestEndpointOverrideNoOp";
    public const string BindingFallbackPreservedName = "RestEndpointBindingFallbackPreserved";
    public const string AuthoringPolicySuppressedName = "RestEndpointAuthoringPolicySuppressed";

    public const string GovernanceSuppressedMessageTemplate = "REST endpoint candidate '{CandidateId}' for behavior '{BehaviorId}' from authoring style '{AuthoringStyle}' was suppressed by governance rule '{SuppressionId}'. Matched suppressions {MatchedSuppressionIds}.";
    public const string PrecedenceSuppressedMessageTemplate = "REST endpoint candidate '{CandidateId}' for behavior '{BehaviorId}' from authoring style '{AuthoringStyle}' was suppressed by higher-precedence candidate '{WinningCandidateId}' from authoring style '{WinningAuthoringStyle}'.";
    public const string OverrideAppliedMessageTemplate = "REST endpoint candidate '{CandidateId}' for behavior '{BehaviorId}' applied governance override '{OverrideId}' and published route '{RoutePattern}'.";
    public const string OverrideNoOpMessageTemplate = "REST endpoint candidate '{CandidateId}' for behavior '{BehaviorId}' matched governance override(s) {MatchedOverrideIds} without changing the published runtime answer.";
    public const string BindingFallbackPreservedMessageTemplate = "REST endpoint candidate '{CandidateId}' for behavior '{BehaviorId}' preserved binding fallback mode '{BindingFallbackMode}' while reconciling governance override(s) {MatchedOverrideIds}.";
    public const string AuthoringPolicySuppressedMessageTemplate = "REST endpoint candidate '{CandidateId}' for behavior '{BehaviorId}' from authoring style '{AuthoringStyle}' was suppressed by authoring policy '{SuppressionKind}'. {SuppressionReason}";

    public static readonly DiagnosticEventDefinition GovernanceSuppressed = new(
        Id: GovernanceSuppressedId,
        Name: GovernanceSuppressedName,
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: GovernanceSuppressedMessageTemplate,
        Description: "Emitted when a REST governance suppression rule hides one behavior-backed candidate.");

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
        Description: "Emitted when a matched REST governance override materially changes the published runtime answer.");

    public static readonly DiagnosticEventDefinition OverrideNoOp = new(
        Id: OverrideNoOpId,
        Name: OverrideNoOpName,
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: OverrideNoOpMessageTemplate,
        Description: "Emitted when a REST governance override matches a candidate but becomes a no-op after runtime-truth reconciliation.");

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

    public static readonly DiagnosticsConvention Convention = new(
        Source: "Cephalon.Behaviors.Http",
        LoggerCategoryPrefix: "Cephalon.Behaviors.Http",
        Description: "Structured governance, authoring-policy, precedence, override, no-op, and fallback-preservation diagnostics for behavior-backed REST governance.",
        Events:
        [
            GovernanceSuppressed,
            PrecedenceSuppressed,
            OverrideApplied,
            OverrideNoOp,
            BindingFallbackPreserved,
            AuthoringPolicySuppressed
        ]);
}
