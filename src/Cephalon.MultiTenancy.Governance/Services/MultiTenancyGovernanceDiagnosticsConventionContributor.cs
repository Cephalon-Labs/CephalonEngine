using Cephalon.Engine.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Cephalon.MultiTenancy.Governance.Services;

internal sealed class MultiTenancyGovernanceDiagnosticsConventionContributor : IDiagnosticsConventionContributor
{
    public DiagnosticsConvention DescribeDiagnosticsConvention() => MultiTenancyGovernanceDiagnosticsConventions.Convention;
}

internal static class MultiTenancyGovernanceDiagnosticsConventions
{
    public static readonly DiagnosticEventDefinition MembershipEvaluationAllowed = new(
        Id: 4510,
        Name: "TenantMembershipEvaluationAllowed",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "Allowed tenant membership evaluation for tenant '{TenantId}' and principal '{PrincipalId}' with roles '{Roles}'.",
        Description: "Emitted when the governance companion grants access from an active tenant membership.");

    public static readonly DiagnosticEventDefinition MembershipEvaluationDenied = new(
        Id: 4511,
        Name: "TenantMembershipEvaluationDenied",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "Denied tenant membership evaluation for tenant '{TenantId}' and principal '{PrincipalId}'. Outcome: {Outcome}. Reason: {Reason}.",
        Description: "Emitted when the governance companion does not grant access from tenant membership.");

    public static readonly DiagnosticsConvention Convention = new(
        Source: "Cephalon.MultiTenancy.Governance",
        LoggerCategoryPrefix: "Cephalon.MultiTenancy.Governance",
        Description: "Structured diagnostics for tenant membership cataloging and evaluation.",
        Events:
        [
            MembershipEvaluationAllowed,
            MembershipEvaluationDenied
        ]);
}

internal static class MultiTenancyGovernanceLoggerMessages
{
    private static readonly Action<ILogger, string, string, string, Exception?> MembershipEvaluationAllowedMessage =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Information,
            new EventId(
                MultiTenancyGovernanceDiagnosticsConventions.MembershipEvaluationAllowed.Id,
                MultiTenancyGovernanceDiagnosticsConventions.MembershipEvaluationAllowed.Name),
            MultiTenancyGovernanceDiagnosticsConventions.MembershipEvaluationAllowed.MessageTemplate);

    private static readonly Action<ILogger, string, string, string, string, Exception?> MembershipEvaluationDeniedMessage =
        LoggerMessage.Define<string, string, string, string>(
            LogLevel.Warning,
            new EventId(
                MultiTenancyGovernanceDiagnosticsConventions.MembershipEvaluationDenied.Id,
                MultiTenancyGovernanceDiagnosticsConventions.MembershipEvaluationDenied.Name),
            MultiTenancyGovernanceDiagnosticsConventions.MembershipEvaluationDenied.MessageTemplate);

    public static void MembershipEvaluationAllowed(
        ILogger logger,
        string tenantId,
        string principalId,
        string roles,
        Exception? exception)
    {
        MembershipEvaluationAllowedMessage(logger, tenantId, principalId, roles, exception);
    }

    public static void MembershipEvaluationDenied(
        ILogger logger,
        string tenantId,
        string principalId,
        string outcome,
        string reason,
        Exception? exception)
    {
        MembershipEvaluationDeniedMessage(logger, tenantId, principalId, outcome, reason, exception);
    }
}
