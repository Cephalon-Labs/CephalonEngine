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

    public static readonly DiagnosticsConvention Convention = new(
        Source: "Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore",
        LoggerCategoryPrefix: "Cephalon.MultiTenancy.Governance.AmazonSesDelivery.AspNetCore",
        Description: "Structured diagnostics for ASP.NET Core Amazon SES over SNS tenant-invitation delivery status callback translation.",
        Events:
        [
            AmazonSesInvitationDeliveryStatusCallbackAccepted
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

    public static void CallbackAccepted(
        ILogger logger,
        int eventCount,
        int translatedCount,
        int reconciledCount,
        int skippedCount) =>
        CallbackAcceptedMessage(logger, eventCount, translatedCount, reconciledCount, skippedCount, null);
}
