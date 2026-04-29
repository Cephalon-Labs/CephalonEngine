using Cephalon.Engine.Diagnostics;

namespace Cephalon.MultiTenancy.Governance.MailgunDelivery.Services;

internal sealed class MailgunInvitationDeliveryDiagnosticsConventionContributor : IDiagnosticsConventionContributor
{
    public DiagnosticsConvention DescribeDiagnosticsConvention() => MailgunInvitationDeliveryDiagnosticsConventions.Convention;
}

internal static class MailgunInvitationDeliveryDiagnosticsConventions
{
    public static readonly DiagnosticEventDefinition MailgunInvitationDeliveryAccepted = new(
        Id: 4566,
        Name: "MailgunInvitationDeliveryAccepted",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "Mailgun invitation delivery sender '{SenderId}' accepted invitation '{InvitationId}' for tenant '{TenantId}' with status code {StatusCode}.",
        Description: "Emitted when the Mailgun Messages API accepts an invitation message request.");

    public static readonly DiagnosticEventDefinition MailgunInvitationDeliveryFailed = new(
        Id: 4567,
        Name: "MailgunInvitationDeliveryFailed",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "Mailgun invitation delivery sender '{SenderId}' failed invitation '{InvitationId}' for tenant '{TenantId}'. Reason: {Reason}.",
        Description: "Emitted when the Mailgun invitation delivery sender cannot prepare or send a Messages API request.");

    public static readonly DiagnosticsConvention Convention = new(
        Source: "Cephalon.MultiTenancy.Governance.MailgunDelivery",
        LoggerCategoryPrefix: "Cephalon.MultiTenancy.Governance.MailgunDelivery",
        Description: "Structured diagnostics for Mailgun Messages API tenant-invitation delivery.",
        Events:
        [
            MailgunInvitationDeliveryAccepted,
            MailgunInvitationDeliveryFailed
        ]);
}
