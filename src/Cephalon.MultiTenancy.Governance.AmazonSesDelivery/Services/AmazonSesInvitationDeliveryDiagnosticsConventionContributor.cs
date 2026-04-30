using Cephalon.Engine.Diagnostics;

namespace Cephalon.MultiTenancy.Governance.AmazonSesDelivery.Services;

internal sealed class AmazonSesInvitationDeliveryDiagnosticsConventionContributor : IDiagnosticsConventionContributor
{
    public DiagnosticsConvention DescribeDiagnosticsConvention() => AmazonSesInvitationDeliveryDiagnosticsConventions.Convention;
}

internal static class AmazonSesInvitationDeliveryDiagnosticsConventions
{
    public static readonly DiagnosticEventDefinition AmazonSesInvitationDeliveryAccepted = new(
        Id: 4576,
        Name: "AmazonSesInvitationDeliveryAccepted",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "Amazon SES invitation delivery sender '{SenderId}' accepted invitation '{InvitationId}' for tenant '{TenantId}' with status code {StatusCode}.",
        Description: "Emitted when Amazon SES accepts an invitation SendEmail request.");

    public static readonly DiagnosticEventDefinition AmazonSesInvitationDeliveryFailed = new(
        Id: 4577,
        Name: "AmazonSesInvitationDeliveryFailed",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "Amazon SES invitation delivery sender '{SenderId}' failed invitation '{InvitationId}' for tenant '{TenantId}'. Reason: {Reason}.",
        Description: "Emitted when the Amazon SES invitation delivery sender cannot prepare or send a SendEmail request.");

    public static readonly DiagnosticsConvention Convention = new(
        Source: "Cephalon.MultiTenancy.Governance.AmazonSesDelivery",
        LoggerCategoryPrefix: "Cephalon.MultiTenancy.Governance.AmazonSesDelivery",
        Description: "Structured diagnostics for Amazon SES v2 tenant-invitation delivery.",
        Events:
        [
            AmazonSesInvitationDeliveryAccepted,
            AmazonSesInvitationDeliveryFailed
        ]);
}
