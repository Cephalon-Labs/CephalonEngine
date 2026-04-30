using Cephalon.Engine.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore.Services;

internal sealed class AmazonSesInvitationDeliveryAspNetCoreDiagnosticsConventionContributor : IDiagnosticsConventionContributor
{
    public DiagnosticsConvention DescribeDiagnosticsConvention() => AmazonSesInvitationDeliveryAspNetCoreDiagnosticsConventions.Convention;
}

internal static class AmazonSesInvitationDeliveryAspNetCoreDiagnosticsConventions
{
    public static readonly DiagnosticEventDefinition AmazonSesInvitationDeliveryStatusCallbackAccepted = new(
        Id: 4578,
        Name: "AmazonSesInvitationDeliveryStatusCallbackAccepted",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "Amazon SES invitation delivery status callback accepted {EventCount} events, translated {TranslatedCount}, reconciled {ReconciledCount}, and skipped {SkippedCount}.",
        Description: "Emitted when the ASP.NET Core Amazon SES over SNS callback endpoint accepts and evaluates a callback payload.");

    public static readonly DiagnosticEventDefinition AmazonSesInvitationDeliveryStatusCallbackSignatureRejected = new(
        Id: 4579,
        Name: "AmazonSesInvitationDeliveryStatusCallbackSignatureRejected",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "Amazon SES invitation delivery status callback rejected by SNS signature verification with outcome {Outcome}.",
        Description: "Emitted when the ASP.NET Core Amazon SES over SNS callback endpoint rejects a payload before translation because SNS signature verification failed.");

    public static readonly DiagnosticEventDefinition AmazonSesInvitationDeliveryStatusCallbackReplayRejected = new(
        Id: 4580,
        Name: "AmazonSesInvitationDeliveryStatusCallbackReplayRejected",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "Amazon SES invitation delivery status callback replay rejected with outcome {Outcome}.",
        Description: "Emitted when the ASP.NET Core Amazon SES over SNS callback endpoint rejects a duplicate verified SNS message inside the process-local replay window.");

    public static readonly DiagnosticEventDefinition AmazonSesInvitationDeliveryStatusCallbackDuplicateMessageSkipped = new(
        Id: 4581,
        Name: "AmazonSesInvitationDeliveryStatusCallbackDuplicateMessageSkipped",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "Amazon SES invitation delivery status callback duplicate SNS message skipped for observation {ObservationId}.",
        Description: "Emitted when the ASP.NET Core Amazon SES over SNS callback endpoint skips a translated notification whose SNS message id is already recorded in the observation store.");

    public static readonly DiagnosticEventDefinition AmazonSesInvitationDeliveryStatusSubscriptionConfirmationConfirmed = new(
        Id: 4582,
        Name: "AmazonSesInvitationDeliveryStatusSubscriptionConfirmationConfirmed",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "Amazon SES invitation delivery status SNS subscription confirmation completed for message {MessageId} with outcome {Outcome}.",
        Description: "Emitted when the ASP.NET Core Amazon SES over SNS callback endpoint confirms a verified SNS subscription-confirmation envelope.");

    public static readonly DiagnosticEventDefinition AmazonSesInvitationDeliveryStatusSubscriptionConfirmationFailed = new(
        Id: 4583,
        Name: "AmazonSesInvitationDeliveryStatusSubscriptionConfirmationFailed",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "Amazon SES invitation delivery status SNS subscription confirmation failed for message {MessageId} with outcome {Outcome}.",
        Description: "Emitted when the ASP.NET Core Amazon SES over SNS callback endpoint cannot confirm a verified SNS subscription-confirmation envelope.");

    public static readonly DiagnosticEventDefinition AmazonSesInvitationDeliveryStatusUnsubscribeConfirmationObserved = new(
        Id: 4584,
        Name: "AmazonSesInvitationDeliveryStatusUnsubscribeConfirmationObserved",
        Severity: DiagnosticSeverity.Information,
        MessageTemplate: "Amazon SES invitation delivery status SNS unsubscribe confirmation observed for message {MessageId} with outcome {Outcome}.",
        Description: "Emitted when the ASP.NET Core Amazon SES over SNS callback endpoint observes a verified SNS unsubscribe-confirmation envelope without restoring the subscription.");

    public static readonly DiagnosticsConvention Convention = new(
        Source: "Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore",
        LoggerCategoryPrefix: "Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore",
        Description: "Structured diagnostics for ASP.NET Core Amazon SES over SNS tenant-invitation delivery status callback translation, signature verification, replay protection, message-id idempotency, subscription confirmation, and unsubscribe-confirmation observation.",
        Events:
        [
            AmazonSesInvitationDeliveryStatusCallbackAccepted,
            AmazonSesInvitationDeliveryStatusCallbackSignatureRejected,
            AmazonSesInvitationDeliveryStatusCallbackReplayRejected,
            AmazonSesInvitationDeliveryStatusCallbackDuplicateMessageSkipped,
            AmazonSesInvitationDeliveryStatusSubscriptionConfirmationConfirmed,
            AmazonSesInvitationDeliveryStatusSubscriptionConfirmationFailed,
            AmazonSesInvitationDeliveryStatusUnsubscribeConfirmationObserved
        ]);
}

internal static class AmazonSesInvitationDeliveryAspNetCoreLogs
{
    private static readonly Action<ILogger, int, int, int, int, Exception?> CallbackAcceptedMessage =
        LoggerMessage.Define<int, int, int, int>(
            LogLevel.Information,
            new EventId(
                AmazonSesInvitationDeliveryAspNetCoreDiagnosticsConventions.AmazonSesInvitationDeliveryStatusCallbackAccepted.Id,
                AmazonSesInvitationDeliveryAspNetCoreDiagnosticsConventions.AmazonSesInvitationDeliveryStatusCallbackAccepted.Name),
            "Amazon SES invitation delivery status callback accepted {EventCount} events, translated {TranslatedCount}, reconciled {ReconciledCount}, and skipped {SkippedCount}.");

    private static readonly Action<ILogger, string, Exception?> CallbackSignatureRejectedMessage =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(
                AmazonSesInvitationDeliveryAspNetCoreDiagnosticsConventions.AmazonSesInvitationDeliveryStatusCallbackSignatureRejected.Id,
                AmazonSesInvitationDeliveryAspNetCoreDiagnosticsConventions.AmazonSesInvitationDeliveryStatusCallbackSignatureRejected.Name),
            "Amazon SES invitation delivery status callback rejected by SNS signature verification with outcome {Outcome}.");

    private static readonly Action<ILogger, string, Exception?> CallbackReplayRejectedMessage =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(
                AmazonSesInvitationDeliveryAspNetCoreDiagnosticsConventions.AmazonSesInvitationDeliveryStatusCallbackReplayRejected.Id,
                AmazonSesInvitationDeliveryAspNetCoreDiagnosticsConventions.AmazonSesInvitationDeliveryStatusCallbackReplayRejected.Name),
            "Amazon SES invitation delivery status callback replay rejected with outcome {Outcome}.");

    private static readonly Action<ILogger, string, Exception?> CallbackDuplicateMessageSkippedMessage =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(
                AmazonSesInvitationDeliveryAspNetCoreDiagnosticsConventions.AmazonSesInvitationDeliveryStatusCallbackDuplicateMessageSkipped.Id,
                AmazonSesInvitationDeliveryAspNetCoreDiagnosticsConventions.AmazonSesInvitationDeliveryStatusCallbackDuplicateMessageSkipped.Name),
            "Amazon SES invitation delivery status callback duplicate SNS message skipped for observation {ObservationId}.");

    private static readonly Action<ILogger, string, string, Exception?> SubscriptionConfirmationConfirmedMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(
                AmazonSesInvitationDeliveryAspNetCoreDiagnosticsConventions.AmazonSesInvitationDeliveryStatusSubscriptionConfirmationConfirmed.Id,
                AmazonSesInvitationDeliveryAspNetCoreDiagnosticsConventions.AmazonSesInvitationDeliveryStatusSubscriptionConfirmationConfirmed.Name),
            "Amazon SES invitation delivery status SNS subscription confirmation completed for message {MessageId} with outcome {Outcome}.");

    private static readonly Action<ILogger, string, string, Exception?> SubscriptionConfirmationFailedMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Warning,
            new EventId(
                AmazonSesInvitationDeliveryAspNetCoreDiagnosticsConventions.AmazonSesInvitationDeliveryStatusSubscriptionConfirmationFailed.Id,
                AmazonSesInvitationDeliveryAspNetCoreDiagnosticsConventions.AmazonSesInvitationDeliveryStatusSubscriptionConfirmationFailed.Name),
            "Amazon SES invitation delivery status SNS subscription confirmation failed for message {MessageId} with outcome {Outcome}.");

    private static readonly Action<ILogger, string, string, Exception?> UnsubscribeConfirmationObservedMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(
                AmazonSesInvitationDeliveryAspNetCoreDiagnosticsConventions.AmazonSesInvitationDeliveryStatusUnsubscribeConfirmationObserved.Id,
                AmazonSesInvitationDeliveryAspNetCoreDiagnosticsConventions.AmazonSesInvitationDeliveryStatusUnsubscribeConfirmationObserved.Name),
            "Amazon SES invitation delivery status SNS unsubscribe confirmation observed for message {MessageId} with outcome {Outcome}.");

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

    public static void CallbackDuplicateMessageSkipped(ILogger logger, string observationId) =>
        CallbackDuplicateMessageSkippedMessage(logger, observationId, null);

    public static void SubscriptionConfirmationConfirmed(ILogger logger, string messageId, string outcome) =>
        SubscriptionConfirmationConfirmedMessage(logger, messageId, outcome, null);

    public static void SubscriptionConfirmationFailed(ILogger logger, string messageId, string outcome) =>
        SubscriptionConfirmationFailedMessage(logger, messageId, outcome, null);

    public static void UnsubscribeConfirmationObserved(ILogger logger, string messageId, string outcome) =>
        UnsubscribeConfirmationObservedMessage(logger, messageId, outcome, null);
}
