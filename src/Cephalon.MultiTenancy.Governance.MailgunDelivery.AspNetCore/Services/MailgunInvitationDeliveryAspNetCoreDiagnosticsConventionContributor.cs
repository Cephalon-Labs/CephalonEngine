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

    public static readonly DiagnosticEventDefinition MailgunInvitationDeliveryStatusCallbackSignatureRejected = new(
        Id: 4569,
        Name: "MailgunInvitationDeliveryStatusCallbackSignatureRejected",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "Mailgun invitation delivery status callback signature rejected with outcome {Outcome}.",
        Description: "Emitted when the ASP.NET Core Mailgun webhook callback endpoint rejects a required Mailgun signature before reconciliation.");

    public static readonly DiagnosticEventDefinition MailgunInvitationDeliveryStatusCallbackReplayRejected = new(
        Id: 4570,
        Name: "MailgunInvitationDeliveryStatusCallbackReplayRejected",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "Mailgun invitation delivery status callback replay rejected with outcome {Outcome}.",
        Description: "Emitted when the ASP.NET Core Mailgun webhook callback endpoint rejects a duplicate verified signed webhook token inside the process-local replay window.");

    public static readonly DiagnosticsConvention Convention = new(
        Source: "Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore",
        LoggerCategoryPrefix: "Cephalon.MultiTenancy.Governance.MailgunDelivery.AspNetCore",
        Description: "Structured diagnostics for ASP.NET Core Mailgun webhook tenant-invitation delivery status callback translation, signature verification, and replay protection.",
        Events:
        [
            MailgunInvitationDeliveryStatusCallbackAccepted,
            MailgunInvitationDeliveryStatusCallbackSignatureRejected,
            MailgunInvitationDeliveryStatusCallbackReplayRejected
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

    private static readonly Action<ILogger, string, Exception?> CallbackSignatureRejectedMessage =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(
                MailgunInvitationDeliveryAspNetCoreDiagnosticsConventions.MailgunInvitationDeliveryStatusCallbackSignatureRejected.Id,
                MailgunInvitationDeliveryAspNetCoreDiagnosticsConventions.MailgunInvitationDeliveryStatusCallbackSignatureRejected.Name),
            "Mailgun invitation delivery status callback signature rejected with outcome {Outcome}.");

    private static readonly Action<ILogger, string, Exception?> CallbackReplayRejectedMessage =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(
                MailgunInvitationDeliveryAspNetCoreDiagnosticsConventions.MailgunInvitationDeliveryStatusCallbackReplayRejected.Id,
                MailgunInvitationDeliveryAspNetCoreDiagnosticsConventions.MailgunInvitationDeliveryStatusCallbackReplayRejected.Name),
            "Mailgun invitation delivery status callback replay rejected with outcome {Outcome}.");

    public static void CallbackAccepted(
        ILogger logger,
        int eventCount,
        int translatedCount,
        int reconciledCount,
        int skippedCount) =>
        CallbackAcceptedMessage(logger, eventCount, translatedCount, reconciledCount, skippedCount, null);

    public static void CallbackSignatureRejected(ILogger logger, string outcome) =>
        CallbackSignatureRejectedMessage(logger, outcome, null);

    public static void CallbackReplayRejected(ILogger logger, string outcome) =>
        CallbackReplayRejectedMessage(logger, outcome, null);
}
