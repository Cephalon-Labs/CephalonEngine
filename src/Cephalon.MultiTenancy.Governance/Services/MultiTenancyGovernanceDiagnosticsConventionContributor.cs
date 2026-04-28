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

    public static readonly DiagnosticEventDefinition DomainOwnershipValidationAllowed = new(
        Id: 4514,
        Name: "TenantDomainOwnershipValidationAllowed",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "Allowed tenant domain ownership validation for tenant '{TenantId}' and domain '{DomainName}' using '{VerificationMethod}'.",
        Description: "Emitted when the governance companion validates a declared tenant domain ownership.");

    public static readonly DiagnosticEventDefinition DomainOwnershipValidationDenied = new(
        Id: 4515,
        Name: "TenantDomainOwnershipValidationDenied",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "Denied tenant domain ownership validation for tenant '{TenantId}' and domain '{DomainName}'. Outcome: {Outcome}. Reason: {Reason}.",
        Description: "Emitted when the governance companion does not validate declared tenant domain ownership.");

    public static readonly DiagnosticEventDefinition GovernanceActionDecisionAllowed = new(
        Id: 4516,
        Name: "TenantGovernanceActionDecisionAllowed",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "Allowed tenant governance action decision for tenant '{TenantId}' and action '{ActionId}' of kind '{ActionKind}'.",
        Description: "Emitted when the governance companion allows an approved or remediated tenant-governance action.");

    public static readonly DiagnosticEventDefinition GovernanceActionDecisionDenied = new(
        Id: 4517,
        Name: "TenantGovernanceActionDecisionDenied",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "Denied tenant governance action decision for tenant '{TenantId}' and action '{ActionId}'. Outcome: {Outcome}. Reason: {Reason}.",
        Description: "Emitted when the governance companion does not allow a tenant-governance action.");

    public static readonly DiagnosticsConvention Convention = new(
        Source: "Cephalon.MultiTenancy.Governance",
        LoggerCategoryPrefix: "Cephalon.MultiTenancy.Governance",
        Description: "Structured diagnostics for tenant membership cataloging/evaluation, invitation cataloging/validation, declared domain-ownership cataloging/validation, and approval/remediation action decisions.",
        Events:
        [
            MembershipEvaluationAllowed,
            MembershipEvaluationDenied,
            InvitationValidationAllowed,
            InvitationValidationDenied,
            DomainOwnershipValidationAllowed,
            DomainOwnershipValidationDenied,
            GovernanceActionDecisionAllowed,
            GovernanceActionDecisionDenied
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

    private static readonly Action<ILogger, string, string, string, Exception?> DomainOwnershipValidationAllowedMessage =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Information,
            new EventId(
                MultiTenancyGovernanceDiagnosticsConventions.DomainOwnershipValidationAllowed.Id,
                MultiTenancyGovernanceDiagnosticsConventions.DomainOwnershipValidationAllowed.Name),
            MultiTenancyGovernanceDiagnosticsConventions.DomainOwnershipValidationAllowed.MessageTemplate);

    private static readonly Action<ILogger, string, string, string, string, Exception?> DomainOwnershipValidationDeniedMessage =
        LoggerMessage.Define<string, string, string, string>(
            LogLevel.Warning,
            new EventId(
                MultiTenancyGovernanceDiagnosticsConventions.DomainOwnershipValidationDenied.Id,
                MultiTenancyGovernanceDiagnosticsConventions.DomainOwnershipValidationDenied.Name),
            MultiTenancyGovernanceDiagnosticsConventions.DomainOwnershipValidationDenied.MessageTemplate);

    private static readonly Action<ILogger, string, string, string, Exception?> GovernanceActionDecisionAllowedMessage =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Information,
            new EventId(
                MultiTenancyGovernanceDiagnosticsConventions.GovernanceActionDecisionAllowed.Id,
                MultiTenancyGovernanceDiagnosticsConventions.GovernanceActionDecisionAllowed.Name),
            MultiTenancyGovernanceDiagnosticsConventions.GovernanceActionDecisionAllowed.MessageTemplate);

    private static readonly Action<ILogger, string, string, string, string, Exception?> GovernanceActionDecisionDeniedMessage =
        LoggerMessage.Define<string, string, string, string>(
            LogLevel.Warning,
            new EventId(
                MultiTenancyGovernanceDiagnosticsConventions.GovernanceActionDecisionDenied.Id,
                MultiTenancyGovernanceDiagnosticsConventions.GovernanceActionDecisionDenied.Name),
            MultiTenancyGovernanceDiagnosticsConventions.GovernanceActionDecisionDenied.MessageTemplate);

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

    public static void DomainOwnershipValidationAllowed(
        ILogger logger,
        string tenantId,
        string domainName,
        string verificationMethod,
        Exception? exception)
    {
        DomainOwnershipValidationAllowedMessage(logger, tenantId, domainName, verificationMethod, exception);
    }

    public static void DomainOwnershipValidationDenied(
        ILogger logger,
        string tenantId,
        string domainName,
        string outcome,
        string reason,
        Exception? exception)
    {
        DomainOwnershipValidationDeniedMessage(logger, tenantId, domainName, outcome, reason, exception);
    }

    public static void GovernanceActionDecisionAllowed(
        ILogger logger,
        string tenantId,
        string actionId,
        string actionKind,
        Exception? exception)
    {
        GovernanceActionDecisionAllowedMessage(logger, tenantId, actionId, actionKind, exception);
    }

    public static void GovernanceActionDecisionDenied(
        ILogger logger,
        string tenantId,
        string actionId,
        string outcome,
        string reason,
        Exception? exception)
    {
        GovernanceActionDecisionDeniedMessage(logger, tenantId, actionId, outcome, reason, exception);
    }
}
