using Cephalon.Engine.Diagnostics;

namespace Cephalon.Identity.Services;

internal sealed class IdentityDiagnosticsConventionContributor : IDiagnosticsConventionContributor
{
    public DiagnosticsConvention DescribeDiagnosticsConvention() => IdentityDiagnosticsConventions.Convention;
}

internal static class IdentityDiagnosticsConventions
{
    public static readonly DiagnosticEventDefinition AuthorizationAllowed = new(
        Id: 4400,
        Name: "IdentityAuthorizationAllowed",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "Identity policy '{PolicyId}' allowed subject '{SubjectId}' for action '{Action}'.",
        Description: "Emitted when the default Cephalon identity evaluator allows an authorization request.");

    public static readonly DiagnosticEventDefinition AuthorizationDenied = new(
        Id: 4401,
        Name: "IdentityAuthorizationDenied",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "Identity policy '{PolicyId}' denied subject '{SubjectId}' for action '{Action}'.",
        Description: "Emitted when the default Cephalon identity evaluator denies an authorization request.");

    public static readonly DiagnosticsConvention Convention = new(
        Source: "Cephalon.Identity",
        LoggerCategoryPrefix: "Cephalon.Identity",
        Description: "Structured diagnostics for the default Cephalon identity and authorization evaluator.",
        Events:
        [
            AuthorizationAllowed,
            AuthorizationDenied
        ]);
}
