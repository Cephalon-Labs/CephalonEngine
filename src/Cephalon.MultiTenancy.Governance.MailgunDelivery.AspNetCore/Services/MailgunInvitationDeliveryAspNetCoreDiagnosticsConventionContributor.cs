using Cephalon.Engine.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore.Services;

internal sealed class MailgunInvitationDeliveryAspNetCoreDiagnosticsConventionContributor : IDiagnosticsConventionContributor
{
    public DiagnosticsConvention DescribeDiagnosticsConvention() => MailgunInvitationDeliveryAspNetCoreDiagnosticsConventions.Convention;
}

internal static class MailgunInvitationDeliveryAspNetCoreDiagnosticsConventions
{
    public static readonly DiagnosticEventDefinition MailgunInvitationDeliveryStatusCallbackAccepted = new(
        Id: 4568,
        Name: "MailgunInvitationDeliveryStatusCallbackAccepted",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "Mailgun invitation delivery status callback accepted {EventCount} events, translated {TranslatedCount}, reconciled {ReconciledCount}, and skipped {SkippedCount}.",
        Description: "Emitted when the ASP.NET Core Mailgun webhook callback endpoint accepts and evaluates a callback payload.");

    public static readonly DiagnosticsConvention Convention = new(
        Source: "Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore",
        LoggerCategoryPrefix: "Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore",
        Description: "Structured diagnostics for ASP.NET Core Mailgun webhook tenant-invitation delivery status callback translation.",
        Events:
        [
            MailgunInvitationDeliveryStatusCallbackAccepted
        ]);
}

internal static class MailgunInvitationDeliveryAspNetCoreLogs
{
    private static readonly Action<ILogger, int, int, int, int, Exception?> CallbackAcceptedMessage =
        LoggerMessage.Define<int, int, int, int>(
            LogLevel.Information,
            new EventId(
                MailgunInvitationDeliveryAspNetCoreDiagnosticsConventions.MailgunInvitationDeliveryStatusCallbackAccepted.Id,
                MailgunInvitationDeliveryAspNetCoreDiagnosticsConventions.MailgunInvitationDeliveryStatusCallbackAccepted.Name),
            "Mailgun invitation delivery status callback accepted {EventCount} events, translated {TranslatedCount}, reconciled {ReconciledCount}, and skipped {SkippedCount}.");

    public static void CallbackAccepted(
        ILogger logger,
        int eventCount,
        int translatedCount,
        int reconciledCount,
        int skippedCount) =>
        CallbackAcceptedMessage(logger, eventCount, translatedCount, reconciledCount, skippedCount, null);
}
