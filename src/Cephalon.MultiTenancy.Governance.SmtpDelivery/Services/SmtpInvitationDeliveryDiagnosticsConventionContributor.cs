using Cephalon.Engine.Diagnostics;

namespace Cephalon.MultiTenancy.Governance.SmtpDelivery.Services;

internal sealed class SmtpInvitationDeliveryDiagnosticsConventionContributor : IDiagnosticsConventionContributor
{
    public DiagnosticsConvention DescribeDiagnosticsConvention() => SmtpInvitationDeliveryDiagnosticsConventions.Convention;
}

internal static class SmtpInvitationDeliveryDiagnosticsConventions
{
    public static readonly DiagnosticEventDefinition SmtpInvitationDeliveryAccepted = new(
        Id: 4558,
        Name: "SmtpInvitationDeliveryAccepted",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "SMTP invitation delivery sender '{SenderId}' accepted invitation '{InvitationId}' for tenant '{TenantId}' using relay '{RelayHost}'.",
        Description: "Emitted when the SMTP invitation delivery relay accepts a message.");

    public static readonly DiagnosticEventDefinition SmtpInvitationDeliveryFailed = new(
        Id: 4559,
        Name: "SmtpInvitationDeliveryFailed",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "SMTP invitation delivery sender '{SenderId}' failed invitation '{InvitationId}' for tenant '{TenantId}'. Reason: {Reason}.",
        Description: "Emitted when the SMTP invitation delivery sender cannot prepare or send a message.");

    public static readonly DiagnosticsConvention Convention = new(
        Source: "Cephalon.MultiTenancy.Governance.SmtpDelivery",
        LoggerCategoryPrefix: "Cephalon.MultiTenancy.Governance.SmtpDelivery",
        Description: "Structured diagnostics for SMTP relay tenant-invitation delivery.",
        Events:
        [
            SmtpInvitationDeliveryAccepted,
            SmtpInvitationDeliveryFailed
        ]);
}
