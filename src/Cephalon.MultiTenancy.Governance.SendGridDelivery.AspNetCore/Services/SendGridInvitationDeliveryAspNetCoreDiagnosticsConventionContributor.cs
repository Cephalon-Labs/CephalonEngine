using Cephalon.Engine.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore.Services;

internal sealed class SendGridInvitationDeliveryAspNetCoreDiagnosticsConventionContributor : IDiagnosticsConventionContributor
{
    public DiagnosticsConvention DescribeDiagnosticsConvention() => SendGridInvitationDeliveryAspNetCoreDiagnosticsConventions.Convention;
}

internal static class SendGridInvitationDeliveryAspNetCoreDiagnosticsConventions
{
    public static readonly DiagnosticEventDefinition SendGridInvitationDeliveryStatusCallbackAccepted = new(
        Id: 4562,
        Name: "SendGridInvitationDeliveryStatusCallbackAccepted",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "SendGrid invitation delivery status callback accepted {EventCount} events, translated {TranslatedCount}, reconciled {ReconciledCount}, and skipped {SkippedCount}.",
        Description: "Emitted when the ASP.NET Core SendGrid Event Webhook callback endpoint accepts and evaluates a callback payload.");

    public static readonly DiagnosticsConvention Convention = new(
        Source: "Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore",
        LoggerCategoryPrefix: "Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore",
        Description: "Structured diagnostics for ASP.NET Core SendGrid Event Webhook tenant-invitation delivery status callback translation.",
        Events:
        [
            SendGridInvitationDeliveryStatusCallbackAccepted
        ]);
}

internal static class SendGridInvitationDeliveryAspNetCoreLogs
{
    private static readonly Action<ILogger, int, int, int, int, Exception?> CallbackAcceptedMessage =
        LoggerMessage.Define<int, int, int, int>(
            LogLevel.Information,
            new EventId(
                SendGridInvitationDeliveryAspNetCoreDiagnosticsConventions.SendGridInvitationDeliveryStatusCallbackAccepted.Id,
                SendGridInvitationDeliveryAspNetCoreDiagnosticsConventions.SendGridInvitationDeliveryStatusCallbackAccepted.Name),
            "SendGrid invitation delivery status callback accepted {EventCount} events, translated {TranslatedCount}, reconciled {ReconciledCount}, and skipped {SkippedCount}.");

    public static void CallbackAccepted(
        ILogger logger,
        int eventCount,
        int translatedCount,
        int reconciledCount,
        int skippedCount) =>
        CallbackAcceptedMessage(logger, eventCount, translatedCount, reconciledCount, skippedCount, null);
}
