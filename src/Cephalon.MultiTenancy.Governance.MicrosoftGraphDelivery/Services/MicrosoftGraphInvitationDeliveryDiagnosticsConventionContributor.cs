using Cephalon.Engine.Diagnostics;

namespace Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.Services;

internal sealed class MicrosoftGraphInvitationDeliveryDiagnosticsConventionContributor : IDiagnosticsConventionContributor
{
    public DiagnosticsConvention DescribeDiagnosticsConvention() => MicrosoftGraphInvitationDeliveryDiagnosticsConventions.Convention;
}

internal static class MicrosoftGraphInvitationDeliveryDiagnosticsConventions
{
    public static readonly DiagnosticEventDefinition MicrosoftGraphInvitationDeliveryAccepted = new(
        Id: 4572,
        Name: "MicrosoftGraphInvitationDeliveryAccepted",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "Microsoft Graph invitation delivery sender '{SenderId}' accepted invitation '{InvitationId}' for tenant '{TenantId}' with status code {StatusCode}.",
        Description: "Emitted when Microsoft Graph sendMail accepts an invitation message request.");

    public static readonly DiagnosticEventDefinition MicrosoftGraphInvitationDeliveryFailed = new(
        Id: 4573,
        Name: "MicrosoftGraphInvitationDeliveryFailed",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "Microsoft Graph invitation delivery sender '{SenderId}' failed invitation '{InvitationId}' for tenant '{TenantId}'. Reason: {Reason}.",
        Description: "Emitted when the Microsoft Graph invitation delivery sender cannot prepare or send a sendMail request.");

    public static readonly DiagnosticsConvention Convention = new(
        Source: "Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery",
        LoggerCategoryPrefix: "Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery",
        Description: "Structured diagnostics for Microsoft Graph sendMail tenant-invitation delivery.",
        Events:
        [
            MicrosoftGraphInvitationDeliveryAccepted,
            MicrosoftGraphInvitationDeliveryFailed
        ]);
}
