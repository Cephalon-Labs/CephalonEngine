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

    public static readonly DiagnosticEventDefinition InvitationValidationAllowed = new(
        Id: 4512,
        Name: "TenantInvitationValidationAllowed",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "Allowed tenant invitation validation for tenant '{TenantId}' and invitation '{InvitationId}' with roles '{Roles}'.",
        Description: "Emitted when the governance companion validates a pending tenant invitation.");

    public static readonly DiagnosticEventDefinition InvitationValidationDenied = new(
        Id: 4513,
        Name: "TenantInvitationValidationDenied",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "Denied tenant invitation validation for tenant '{TenantId}' and invitation '{InvitationId}'. Outcome: {Outcome}. Reason: {Reason}.",
        Description: "Emitted when the governance companion does not validate a tenant invitation.");

    public static readonly DiagnosticsConvention Convention = new(
        Source: "Cephalon.MultiTenancy.Governance",
        LoggerCategoryPrefix: "Cephalon.MultiTenancy.Governance",
        Description: "Structured diagnostics for tenant membership cataloging, membership evaluation, invitation cataloging, and invitation validation.",
        Events:
        [
            MembershipEvaluationAllowed,
            MembershipEvaluationDenied,
            InvitationValidationAllowed,
            InvitationValidationDenied
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

    private static readonly Action<ILogger, string, string, string, Exception?> InvitationValidationAllowedMessage =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Information,
            new EventId(
                MultiTenancyGovernanceDiagnosticsConventions.InvitationValidationAllowed.Id,
                MultiTenancyGovernanceDiagnosticsConventions.InvitationValidationAllowed.Name),
            MultiTenancyGovernanceDiagnosticsConventions.InvitationValidationAllowed.MessageTemplate);

    private static readonly Action<ILogger, string, string, string, string, Exception?> InvitationValidationDeniedMessage =
        LoggerMessage.Define<string, string, string, string>(
            LogLevel.Warning,
            new EventId(
                MultiTenancyGovernanceDiagnosticsConventions.InvitationValidationDenied.Id,
                MultiTenancyGovernanceDiagnosticsConventions.InvitationValidationDenied.Name),
            MultiTenancyGovernanceDiagnosticsConventions.InvitationValidationDenied.MessageTemplate);

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

    public static void InvitationValidationAllowed(
        ILogger logger,
        string tenantId,
        string invitationId,
        string roles,
        Exception? exception)
    {
        InvitationValidationAllowedMessage(logger, tenantId, invitationId, roles, exception);
    }

    public static void InvitationValidationDenied(
        ILogger logger,
        string tenantId,
        string invitationId,
        string outcome,
        string reason,
        Exception? exception)
    {
        InvitationValidationDeniedMessage(logger, tenantId, invitationId, outcome, reason, exception);
    }
}
