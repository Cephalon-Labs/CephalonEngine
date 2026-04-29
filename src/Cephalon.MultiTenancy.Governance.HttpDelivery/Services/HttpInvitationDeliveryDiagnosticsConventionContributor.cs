using Cephalon.Engine.Diagnostics;

namespace Cephalon.MultiTenancy.Governance.HttpDelivery.Services;

internal sealed class HttpInvitationDeliveryDiagnosticsConventionContributor : IDiagnosticsConventionContributor
{
    public DiagnosticsConvention DescribeDiagnosticsConvention() => HttpInvitationDeliveryDiagnosticsConventions.Convention;
}

internal static class HttpInvitationDeliveryDiagnosticsConventions
{
    public static readonly DiagnosticEventDefinition HttpInvitationDeliveryAccepted = new(
        Id: 4550,
        Name: "HttpInvitationDeliveryAccepted",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "HTTP invitation delivery sender '{SenderId}' accepted invitation '{InvitationId}' for tenant '{TenantId}' with status code {StatusCode}.",
        Description: "Emitted when the HTTP invitation delivery webhook returns an accepted response.");

    public static readonly DiagnosticEventDefinition HttpInvitationDeliveryFailed = new(
        Id: 4551,
        Name: "HttpInvitationDeliveryFailed",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "HTTP invitation delivery sender '{SenderId}' failed invitation '{InvitationId}' for tenant '{TenantId}'. Reason: {Reason}.",
        Description: "Emitted when the HTTP invitation delivery webhook rejects, times out, or fails a dispatch attempt.");

    public static readonly DiagnosticsConvention Convention = new(
        Source: "Cephalon.MultiTenancy.Governance.HttpDelivery",
        LoggerCategoryPrefix: "Cephalon.MultiTenancy.Governance.HttpDelivery",
        Description: "Structured diagnostics for HTTP webhook tenant-invitation delivery.",
        Events:
        [
            HttpInvitationDeliveryAccepted,
            HttpInvitationDeliveryFailed
        ]);
}
