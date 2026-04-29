using Cephalon.Engine.Diagnostics;

namespace Cephalon.MultiTenancy.Governance.SendGridDelivery.Services;

internal sealed class SendGridInvitationDeliveryDiagnosticsConventionContributor : IDiagnosticsConventionContributor
{
    public DiagnosticsConvention DescribeDiagnosticsConvention() => SendGridInvitationDeliveryDiagnosticsConventions.Convention;
}

internal static class SendGridInvitationDeliveryDiagnosticsConventions
{
    public static readonly DiagnosticEventDefinition SendGridInvitationDeliveryAccepted = new(
        Id: 4560,
        Name: "SendGridInvitationDeliveryAccepted",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "SendGrid invitation delivery sender '{SenderId}' accepted invitation '{InvitationId}' for tenant '{TenantId}' with status code {StatusCode}.",
        Description: "Emitted when the SendGrid Mail Send API accepts an invitation message request.");

    public static readonly DiagnosticEventDefinition SendGridInvitationDeliveryFailed = new(
        Id: 4561,
        Name: "SendGridInvitationDeliveryFailed",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "SendGrid invitation delivery sender '{SenderId}' failed invitation '{InvitationId}' for tenant '{TenantId}'. Reason: {Reason}.",
        Description: "Emitted when the SendGrid invitation delivery sender cannot prepare or send a Mail Send request.");

    public static readonly DiagnosticsConvention Convention = new(
        Source: "Cephalon.MultiTenancy.Governance.SendGridDelivery",
        LoggerCategoryPrefix: "Cephalon.MultiTenancy.Governance.SendGridDelivery",
        Description: "Structured diagnostics for SendGrid Mail Send API tenant-invitation delivery.",
        Events:
        [
            SendGridInvitationDeliveryAccepted,
            SendGridInvitationDeliveryFailed
        ]);
}
