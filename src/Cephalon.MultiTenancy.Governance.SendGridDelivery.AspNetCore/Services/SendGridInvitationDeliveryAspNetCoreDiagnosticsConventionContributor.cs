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

    public static readonly DiagnosticEventDefinition SendGridInvitationDeliveryStatusCallbackSignatureRejected = new(
        Id: 4563,
        Name: "SendGridInvitationDeliveryStatusCallbackSignatureRejected",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "SendGrid invitation delivery status callback signature rejected with outcome {Outcome}.",
        Description: "Emitted when the ASP.NET Core SendGrid Event Webhook callback endpoint rejects a required signed webhook signature before translation.");

    public static readonly DiagnosticEventDefinition SendGridInvitationDeliveryStatusCallbackReplayRejected = new(
        Id: 4564,
        Name: "SendGridInvitationDeliveryStatusCallbackReplayRejected",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "SendGrid invitation delivery status callback replay rejected with outcome {Outcome}.",
        Description: "Emitted when the ASP.NET Core SendGrid Event Webhook callback endpoint rejects a duplicate verified signed webhook inside the process-local replay window.");

    public static readonly DiagnosticsConvention Convention = new(
        Source: "Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore",
        LoggerCategoryPrefix: "Cephalon.MultiTenancy.Governance.SendGridDelivery.AspNetCore",
        Description: "Structured diagnostics for ASP.NET Core SendGrid Event Webhook tenant-invitation delivery status callback translation.",
        Events:
        [
            SendGridInvitationDeliveryStatusCallbackAccepted,
            SendGridInvitationDeliveryStatusCallbackSignatureRejected,
            SendGridInvitationDeliveryStatusCallbackReplayRejected
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

    private static readonly Action<ILogger, string, Exception?> CallbackSignatureRejectedMessage =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(
                SendGridInvitationDeliveryAspNetCoreDiagnosticsConventions.SendGridInvitationDeliveryStatusCallbackSignatureRejected.Id,
                SendGridInvitationDeliveryAspNetCoreDiagnosticsConventions.SendGridInvitationDeliveryStatusCallbackSignatureRejected.Name),
            "SendGrid invitation delivery status callback signature rejected with outcome {Outcome}.");

    private static readonly Action<ILogger, string, Exception?> CallbackReplayRejectedMessage =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(
                SendGridInvitationDeliveryAspNetCoreDiagnosticsConventions.SendGridInvitationDeliveryStatusCallbackReplayRejected.Id,
                SendGridInvitationDeliveryAspNetCoreDiagnosticsConventions.SendGridInvitationDeliveryStatusCallbackReplayRejected.Name),
            "SendGrid invitation delivery status callback replay rejected with outcome {Outcome}.");

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
