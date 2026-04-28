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

    public static readonly DiagnosticEventDefinition GovernanceActionWorkflowApplied = new(
        Id: 4518,
        Name: "TenantGovernanceActionWorkflowApplied",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "Applied tenant governance action workflow command '{Command}' for tenant '{TenantId}' and action '{ActionId}'. Status: {Status}.",
        Description: "Emitted when the governance companion applies an in-process tenant-governance action workflow transition.");

    public static readonly DiagnosticEventDefinition GovernanceActionWorkflowDenied = new(
        Id: 4519,
        Name: "TenantGovernanceActionWorkflowDenied",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "Denied tenant governance action workflow command '{Command}' for tenant '{TenantId}' and action '{ActionId}'. Outcome: {Outcome}. Reason: {Reason}.",
        Description: "Emitted when the governance companion rejects an in-process tenant-governance action workflow transition.");

    public static readonly DiagnosticEventDefinition GovernanceActionStorePersisted = new(
        Id: 4520,
        Name: "TenantGovernanceActionStorePersisted",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "Persisted tenant governance action state for tenant '{TenantId}' and action '{ActionId}' using store '{StoreKind}'. Durable: {Durable}.",
        Description: "Emitted when the governance companion stores runtime tenant-governance action state.");

    public static readonly DiagnosticEventDefinition GovernanceActionStorePersistenceFailed = new(
        Id: 4521,
        Name: "TenantGovernanceActionStorePersistenceFailed",
        Severity: DiagnosticSeverity.Error,
        MessageTemplate: "Failed to persist tenant governance action state for tenant '{TenantId}' and action '{ActionId}' using store '{StoreKind}'. Reason: {Reason}.",
        Description: "Emitted when the governance companion cannot store runtime tenant-governance action state.");

    public static readonly DiagnosticEventDefinition DomainOwnershipVerificationWorkflowApplied = new(
        Id: 4522,
        Name: "TenantDomainOwnershipVerificationWorkflowApplied",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "Applied tenant domain ownership verification workflow command '{Command}' for tenant '{TenantId}' and domain '{DomainName}'. Status: {Status}.",
        Description: "Emitted when the governance companion applies an in-process tenant-domain ownership verification workflow transition.");

    public static readonly DiagnosticEventDefinition DomainOwnershipVerificationWorkflowDenied = new(
        Id: 4523,
        Name: "TenantDomainOwnershipVerificationWorkflowDenied",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "Denied tenant domain ownership verification workflow command '{Command}' for tenant '{TenantId}' and domain '{DomainName}'. Outcome: {Outcome}. Reason: {Reason}.",
        Description: "Emitted when the governance companion rejects an in-process tenant-domain ownership verification workflow transition.");

    public static readonly DiagnosticEventDefinition DomainOwnershipStorePersisted = new(
        Id: 4524,
        Name: "TenantDomainOwnershipStorePersisted",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "Persisted tenant domain ownership state for tenant '{TenantId}' and domain '{DomainName}' using store '{StoreKind}'. Durable: {Durable}.",
        Description: "Emitted when the governance companion stores runtime tenant-domain ownership state.");

    public static readonly DiagnosticEventDefinition DomainOwnershipStorePersistenceFailed = new(
        Id: 4525,
        Name: "TenantDomainOwnershipStorePersistenceFailed",
        Severity: DiagnosticSeverity.Error,
        MessageTemplate: "Failed to persist tenant domain ownership state for tenant '{TenantId}' and domain '{DomainName}' using store '{StoreKind}'. Reason: {Reason}.",
        Description: "Emitted when the governance companion cannot store runtime tenant-domain ownership state.");

    public static readonly DiagnosticEventDefinition DomainOwnershipProofEvaluationVerified = new(
        Id: 4526,
        Name: "TenantDomainOwnershipProofEvaluationVerified",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "Verified tenant domain ownership proof for tenant '{TenantId}' and domain '{DomainName}' using '{VerificationMethod}'.",
        Description: "Emitted when the governance companion evaluates reported tenant-domain ownership proof and verifies the declaration.");

    public static readonly DiagnosticEventDefinition DomainOwnershipProofEvaluationDenied = new(
        Id: 4527,
        Name: "TenantDomainOwnershipProofEvaluationDenied",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "Denied tenant domain ownership proof evaluation for tenant '{TenantId}' and domain '{DomainName}'. Outcome: {Outcome}. Reason: {Reason}.",
        Description: "Emitted when the governance companion cannot verify reported tenant-domain ownership proof.");

    public static readonly DiagnosticEventDefinition DomainOwnershipProofChallengeIssued = new(
        Id: 4528,
        Name: "TenantDomainOwnershipProofChallengeIssued",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "Issued tenant domain ownership proof challenge for tenant '{TenantId}' and domain '{DomainName}' using '{VerificationMethod}'.",
        Description: "Emitted when the governance companion issues and stores a tenant-domain ownership proof challenge.");

    public static readonly DiagnosticEventDefinition DomainOwnershipProofChallengeDenied = new(
        Id: 4529,
        Name: "TenantDomainOwnershipProofChallengeDenied",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "Denied tenant domain ownership proof challenge for tenant '{TenantId}' and domain '{DomainName}'. Outcome: {Outcome}. Reason: {Reason}.",
        Description: "Emitted when the governance companion cannot issue a tenant-domain ownership proof challenge.");

    public static readonly DiagnosticEventDefinition DomainOwnershipProofPublicationPlanned = new(
        Id: 4530,
        Name: "TenantDomainOwnershipProofPublicationPlanned",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "Planned tenant domain ownership proof publication for tenant '{TenantId}' and domain '{DomainName}' using '{VerificationMethod}'.",
        Description: "Emitted when the governance companion builds tenant-domain ownership proof publication instructions.");

    public static readonly DiagnosticEventDefinition DomainOwnershipProofPublicationPlanDenied = new(
        Id: 4531,
        Name: "TenantDomainOwnershipProofPublicationPlanDenied",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "Denied tenant domain ownership proof publication planning for tenant '{TenantId}' and domain '{DomainName}'. Outcome: {Outcome}. Reason: {Reason}.",
        Description: "Emitted when the governance companion cannot build tenant-domain ownership proof publication instructions.");

    public static readonly DiagnosticsConvention Convention = new(
        Source: "Cephalon.MultiTenancy.Governance",
        LoggerCategoryPrefix: "Cephalon.MultiTenancy.Governance",
        Description: "Structured diagnostics for tenant membership cataloging/evaluation, invitation cataloging/validation, declared domain-ownership cataloging/validation, tenant-domain ownership verification workflow transitions, tenant-domain ownership proof evaluation, tenant-domain ownership proof challenge issuance, tenant-domain ownership proof publication planning, domain-ownership persistence, approval/remediation action decisions, in-process governance-action workflow transitions, and action-state persistence.",
        Events:
        [
            MembershipEvaluationAllowed,
            MembershipEvaluationDenied,
            InvitationValidationAllowed,
            InvitationValidationDenied,
            DomainOwnershipValidationAllowed,
            DomainOwnershipValidationDenied,
            GovernanceActionDecisionAllowed,
            GovernanceActionDecisionDenied,
            GovernanceActionWorkflowApplied,
            GovernanceActionWorkflowDenied,
            GovernanceActionStorePersisted,
            GovernanceActionStorePersistenceFailed,
            DomainOwnershipVerificationWorkflowApplied,
            DomainOwnershipVerificationWorkflowDenied,
            DomainOwnershipStorePersisted,
            DomainOwnershipStorePersistenceFailed,
            DomainOwnershipProofEvaluationVerified,
            DomainOwnershipProofEvaluationDenied,
            DomainOwnershipProofChallengeIssued,
            DomainOwnershipProofChallengeDenied,
            DomainOwnershipProofPublicationPlanned,
            DomainOwnershipProofPublicationPlanDenied
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

    private static readonly Action<ILogger, string, string, string, string, Exception?> GovernanceActionWorkflowAppliedMessage =
        LoggerMessage.Define<string, string, string, string>(
            LogLevel.Information,
            new EventId(
                MultiTenancyGovernanceDiagnosticsConventions.GovernanceActionWorkflowApplied.Id,
                MultiTenancyGovernanceDiagnosticsConventions.GovernanceActionWorkflowApplied.Name),
            MultiTenancyGovernanceDiagnosticsConventions.GovernanceActionWorkflowApplied.MessageTemplate);

    private static readonly Action<ILogger, string, string, string, string, string, Exception?> GovernanceActionWorkflowDeniedMessage =
        LoggerMessage.Define<string, string, string, string, string>(
            LogLevel.Warning,
            new EventId(
                MultiTenancyGovernanceDiagnosticsConventions.GovernanceActionWorkflowDenied.Id,
                MultiTenancyGovernanceDiagnosticsConventions.GovernanceActionWorkflowDenied.Name),
            MultiTenancyGovernanceDiagnosticsConventions.GovernanceActionWorkflowDenied.MessageTemplate);

    private static readonly Action<ILogger, string, string, string, string, Exception?> GovernanceActionStorePersistedMessage =
        LoggerMessage.Define<string, string, string, string>(
            LogLevel.Information,
            new EventId(
                MultiTenancyGovernanceDiagnosticsConventions.GovernanceActionStorePersisted.Id,
                MultiTenancyGovernanceDiagnosticsConventions.GovernanceActionStorePersisted.Name),
            MultiTenancyGovernanceDiagnosticsConventions.GovernanceActionStorePersisted.MessageTemplate);

    private static readonly Action<ILogger, string, string, string, string, Exception?> GovernanceActionStorePersistenceFailedMessage =
        LoggerMessage.Define<string, string, string, string>(
            LogLevel.Error,
            new EventId(
                MultiTenancyGovernanceDiagnosticsConventions.GovernanceActionStorePersistenceFailed.Id,
                MultiTenancyGovernanceDiagnosticsConventions.GovernanceActionStorePersistenceFailed.Name),
            MultiTenancyGovernanceDiagnosticsConventions.GovernanceActionStorePersistenceFailed.MessageTemplate);

    private static readonly Action<ILogger, string, string, string, string, Exception?> DomainOwnershipVerificationWorkflowAppliedMessage =
        LoggerMessage.Define<string, string, string, string>(
            LogLevel.Information,
            new EventId(
                MultiTenancyGovernanceDiagnosticsConventions.DomainOwnershipVerificationWorkflowApplied.Id,
                MultiTenancyGovernanceDiagnosticsConventions.DomainOwnershipVerificationWorkflowApplied.Name),
            MultiTenancyGovernanceDiagnosticsConventions.DomainOwnershipVerificationWorkflowApplied.MessageTemplate);

    private static readonly Action<ILogger, string, string, string, string, string, Exception?> DomainOwnershipVerificationWorkflowDeniedMessage =
        LoggerMessage.Define<string, string, string, string, string>(
            LogLevel.Warning,
            new EventId(
                MultiTenancyGovernanceDiagnosticsConventions.DomainOwnershipVerificationWorkflowDenied.Id,
                MultiTenancyGovernanceDiagnosticsConventions.DomainOwnershipVerificationWorkflowDenied.Name),
            MultiTenancyGovernanceDiagnosticsConventions.DomainOwnershipVerificationWorkflowDenied.MessageTemplate);

    private static readonly Action<ILogger, string, string, string, string, Exception?> DomainOwnershipStorePersistedMessage =
        LoggerMessage.Define<string, string, string, string>(
            LogLevel.Information,
            new EventId(
                MultiTenancyGovernanceDiagnosticsConventions.DomainOwnershipStorePersisted.Id,
                MultiTenancyGovernanceDiagnosticsConventions.DomainOwnershipStorePersisted.Name),
            MultiTenancyGovernanceDiagnosticsConventions.DomainOwnershipStorePersisted.MessageTemplate);

    private static readonly Action<ILogger, string, string, string, string, Exception?> DomainOwnershipStorePersistenceFailedMessage =
        LoggerMessage.Define<string, string, string, string>(
            LogLevel.Error,
            new EventId(
                MultiTenancyGovernanceDiagnosticsConventions.DomainOwnershipStorePersistenceFailed.Id,
                MultiTenancyGovernanceDiagnosticsConventions.DomainOwnershipStorePersistenceFailed.Name),
            MultiTenancyGovernanceDiagnosticsConventions.DomainOwnershipStorePersistenceFailed.MessageTemplate);

    private static readonly Action<ILogger, string, string, string, Exception?> DomainOwnershipProofEvaluationVerifiedMessage =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Information,
            new EventId(
                MultiTenancyGovernanceDiagnosticsConventions.DomainOwnershipProofEvaluationVerified.Id,
                MultiTenancyGovernanceDiagnosticsConventions.DomainOwnershipProofEvaluationVerified.Name),
            MultiTenancyGovernanceDiagnosticsConventions.DomainOwnershipProofEvaluationVerified.MessageTemplate);

    private static readonly Action<ILogger, string, string, string, string, Exception?> DomainOwnershipProofEvaluationDeniedMessage =
        LoggerMessage.Define<string, string, string, string>(
            LogLevel.Warning,
            new EventId(
                MultiTenancyGovernanceDiagnosticsConventions.DomainOwnershipProofEvaluationDenied.Id,
                MultiTenancyGovernanceDiagnosticsConventions.DomainOwnershipProofEvaluationDenied.Name),
            MultiTenancyGovernanceDiagnosticsConventions.DomainOwnershipProofEvaluationDenied.MessageTemplate);

    private static readonly Action<ILogger, string, string, string, Exception?> DomainOwnershipProofChallengeIssuedMessage =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Information,
            new EventId(
                MultiTenancyGovernanceDiagnosticsConventions.DomainOwnershipProofChallengeIssued.Id,
                MultiTenancyGovernanceDiagnosticsConventions.DomainOwnershipProofChallengeIssued.Name),
            MultiTenancyGovernanceDiagnosticsConventions.DomainOwnershipProofChallengeIssued.MessageTemplate);

    private static readonly Action<ILogger, string, string, string, string, Exception?> DomainOwnershipProofChallengeDeniedMessage =
        LoggerMessage.Define<string, string, string, string>(
            LogLevel.Warning,
            new EventId(
                MultiTenancyGovernanceDiagnosticsConventions.DomainOwnershipProofChallengeDenied.Id,
                MultiTenancyGovernanceDiagnosticsConventions.DomainOwnershipProofChallengeDenied.Name),
            MultiTenancyGovernanceDiagnosticsConventions.DomainOwnershipProofChallengeDenied.MessageTemplate);

    private static readonly Action<ILogger, string, string, string, Exception?> DomainOwnershipProofPublicationPlannedMessage =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Information,
            new EventId(
                MultiTenancyGovernanceDiagnosticsConventions.DomainOwnershipProofPublicationPlanned.Id,
                MultiTenancyGovernanceDiagnosticsConventions.DomainOwnershipProofPublicationPlanned.Name),
            MultiTenancyGovernanceDiagnosticsConventions.DomainOwnershipProofPublicationPlanned.MessageTemplate);

    private static readonly Action<ILogger, string, string, string, string, Exception?> DomainOwnershipProofPublicationPlanDeniedMessage =
        LoggerMessage.Define<string, string, string, string>(
            LogLevel.Warning,
            new EventId(
                MultiTenancyGovernanceDiagnosticsConventions.DomainOwnershipProofPublicationPlanDenied.Id,
                MultiTenancyGovernanceDiagnosticsConventions.DomainOwnershipProofPublicationPlanDenied.Name),
            MultiTenancyGovernanceDiagnosticsConventions.DomainOwnershipProofPublicationPlanDenied.MessageTemplate);

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

    public static void GovernanceActionWorkflowApplied(
        ILogger logger,
        string tenantId,
        string actionId,
        string command,
        string status,
        Exception? exception)
    {
        GovernanceActionWorkflowAppliedMessage(logger, command, tenantId, actionId, status, exception);
    }

    public static void GovernanceActionWorkflowDenied(
        ILogger logger,
        string tenantId,
        string actionId,
        string command,
        string outcome,
        string reason,
        Exception? exception)
    {
        GovernanceActionWorkflowDeniedMessage(logger, command, tenantId, actionId, outcome, reason, exception);
    }

    public static void GovernanceActionStorePersisted(
        ILogger logger,
        string tenantId,
        string actionId,
        string storeKind,
        string durable,
        Exception? exception)
    {
        GovernanceActionStorePersistedMessage(logger, tenantId, actionId, storeKind, durable, exception);
    }

    public static void GovernanceActionStorePersistenceFailed(
        ILogger logger,
        string tenantId,
        string actionId,
        string storeKind,
        string reason,
        Exception? exception)
    {
        GovernanceActionStorePersistenceFailedMessage(logger, tenantId, actionId, storeKind, reason, exception);
    }

    public static void DomainOwnershipVerificationWorkflowApplied(
        ILogger logger,
        string tenantId,
        string domainName,
        string command,
        string status,
        Exception? exception)
    {
        DomainOwnershipVerificationWorkflowAppliedMessage(logger, command, tenantId, domainName, status, exception);
    }

    public static void DomainOwnershipVerificationWorkflowDenied(
        ILogger logger,
        string tenantId,
        string domainName,
        string command,
        string outcome,
        string reason,
        Exception? exception)
    {
        DomainOwnershipVerificationWorkflowDeniedMessage(logger, command, tenantId, domainName, outcome, reason, exception);
    }

    public static void DomainOwnershipStorePersisted(
        ILogger logger,
        string tenantId,
        string domainName,
        string storeKind,
        string durable,
        Exception? exception)
    {
        DomainOwnershipStorePersistedMessage(logger, tenantId, domainName, storeKind, durable, exception);
    }

    public static void DomainOwnershipStorePersistenceFailed(
        ILogger logger,
        string tenantId,
        string domainName,
        string storeKind,
        string reason,
        Exception? exception)
    {
        DomainOwnershipStorePersistenceFailedMessage(logger, tenantId, domainName, storeKind, reason, exception);
    }

    public static void DomainOwnershipProofEvaluationVerified(
        ILogger logger,
        string tenantId,
        string domainName,
        string verificationMethod,
        Exception? exception)
    {
        DomainOwnershipProofEvaluationVerifiedMessage(logger, tenantId, domainName, verificationMethod, exception);
    }

    public static void DomainOwnershipProofEvaluationDenied(
        ILogger logger,
        string tenantId,
        string domainName,
        string outcome,
        string reason,
        Exception? exception)
    {
        DomainOwnershipProofEvaluationDeniedMessage(logger, tenantId, domainName, outcome, reason, exception);
    }

    public static void DomainOwnershipProofChallengeIssued(
        ILogger logger,
        string tenantId,
        string domainName,
        string verificationMethod,
        Exception? exception)
    {
        DomainOwnershipProofChallengeIssuedMessage(logger, tenantId, domainName, verificationMethod, exception);
    }

    public static void DomainOwnershipProofChallengeDenied(
        ILogger logger,
        string tenantId,
        string domainName,
        string outcome,
        string reason,
        Exception? exception)
    {
        DomainOwnershipProofChallengeDeniedMessage(logger, tenantId, domainName, outcome, reason, exception);
    }

    public static void DomainOwnershipProofPublicationPlanned(
        ILogger logger,
        string tenantId,
        string domainName,
        string verificationMethod,
        Exception? exception)
    {
        DomainOwnershipProofPublicationPlannedMessage(logger, tenantId, domainName, verificationMethod, exception);
    }

    public static void DomainOwnershipProofPublicationPlanDenied(
        ILogger logger,
        string tenantId,
        string domainName,
        string outcome,
        string reason,
        Exception? exception)
    {
        DomainOwnershipProofPublicationPlanDeniedMessage(logger, tenantId, domainName, outcome, reason, exception);
    }
}
