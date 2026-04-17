using Microsoft.Extensions.Logging;

namespace Cephalon.Behaviors.Http.Hosting;

internal static class RestBehaviorGovernanceLoggerMessages
{
    private static readonly Action<ILogger, string, string, string, string, string, Exception?> GovernanceSuppressed =
        LoggerMessage.Define<string, string, string, string, string>(
            LogLevel.Information,
            new EventId(
                RestBehaviorGovernanceDiagnosticsConventions.GovernanceSuppressedId,
                RestBehaviorGovernanceDiagnosticsConventions.GovernanceSuppressedName),
            RestBehaviorGovernanceDiagnosticsConventions.GovernanceSuppressedMessageTemplate);

    private static readonly Action<ILogger, string, string, string, string, string, Exception?> PrecedenceSuppressed =
        LoggerMessage.Define<string, string, string, string, string>(
            LogLevel.Information,
            new EventId(
                RestBehaviorGovernanceDiagnosticsConventions.PrecedenceSuppressedId,
                RestBehaviorGovernanceDiagnosticsConventions.PrecedenceSuppressedName),
            RestBehaviorGovernanceDiagnosticsConventions.PrecedenceSuppressedMessageTemplate);

    private static readonly Action<ILogger, string, string, string, string, Exception?> OverrideApplied =
        LoggerMessage.Define<string, string, string, string>(
            LogLevel.Information,
            new EventId(
                RestBehaviorGovernanceDiagnosticsConventions.OverrideAppliedId,
                RestBehaviorGovernanceDiagnosticsConventions.OverrideAppliedName),
            RestBehaviorGovernanceDiagnosticsConventions.OverrideAppliedMessageTemplate);

    private static readonly Action<ILogger, string, string, string, string, Exception?> OverrideNoOp =
        LoggerMessage.Define<string, string, string, string>(
            LogLevel.Information,
            new EventId(
                RestBehaviorGovernanceDiagnosticsConventions.OverrideNoOpId,
                RestBehaviorGovernanceDiagnosticsConventions.OverrideNoOpName),
            RestBehaviorGovernanceDiagnosticsConventions.OverrideNoOpMessageTemplate);

    private static readonly Action<ILogger, string, string, string, string, Exception?> BindingFallbackPreserved =
        LoggerMessage.Define<string, string, string, string>(
            LogLevel.Information,
            new EventId(
                RestBehaviorGovernanceDiagnosticsConventions.BindingFallbackPreservedId,
                RestBehaviorGovernanceDiagnosticsConventions.BindingFallbackPreservedName),
            RestBehaviorGovernanceDiagnosticsConventions.BindingFallbackPreservedMessageTemplate);

    private static readonly Action<ILogger, string, string, string, string, string, Exception?> AuthoringPolicySuppressed =
        LoggerMessage.Define<string, string, string, string, string>(
            LogLevel.Information,
            new EventId(
                RestBehaviorGovernanceDiagnosticsConventions.AuthoringPolicySuppressedId,
                RestBehaviorGovernanceDiagnosticsConventions.AuthoringPolicySuppressedName),
            RestBehaviorGovernanceDiagnosticsConventions.AuthoringPolicySuppressedMessageTemplate);

    private static readonly Action<ILogger, string, string, string, string, string, Exception?> GovernanceSkipped =
        LoggerMessage.Define<string, string, string, string, string>(
            LogLevel.Information,
            new EventId(
                RestBehaviorGovernanceDiagnosticsConventions.GovernanceSkippedId,
                RestBehaviorGovernanceDiagnosticsConventions.GovernanceSkippedName),
            RestBehaviorGovernanceDiagnosticsConventions.GovernanceSkippedMessageTemplate);

    public static void LogGovernanceSuppressed(
        ILogger logger,
        string candidateId,
        string behaviorId,
        string authoringStyle,
        string suppressionId,
        string matchedSuppressionIds)
    {
        GovernanceSuppressed(logger, candidateId, behaviorId, authoringStyle, suppressionId, matchedSuppressionIds, null);
    }

    public static void LogPrecedenceSuppressed(
        ILogger logger,
        string candidateId,
        string behaviorId,
        string authoringStyle,
        string winningCandidateId,
        string winningAuthoringStyle)
    {
        PrecedenceSuppressed(logger, candidateId, behaviorId, authoringStyle, winningCandidateId, winningAuthoringStyle, null);
    }

    public static void LogOverrideApplied(
        ILogger logger,
        string candidateId,
        string behaviorId,
        string overrideId,
        string routePattern)
    {
        OverrideApplied(logger, candidateId, behaviorId, overrideId, routePattern, null);
    }

    public static void LogOverrideNoOp(
        ILogger logger,
        string candidateId,
        string behaviorId,
        string selectedOverrideId,
        string matchedOverrideIds)
    {
        OverrideNoOp(logger, candidateId, behaviorId, selectedOverrideId, matchedOverrideIds, null);
    }

    public static void LogBindingFallbackPreserved(
        ILogger logger,
        string candidateId,
        string behaviorId,
        string bindingFallbackMode,
        string matchedOverrideIds)
    {
        BindingFallbackPreserved(logger, candidateId, behaviorId, bindingFallbackMode, matchedOverrideIds, null);
    }

    public static void LogAuthoringPolicySuppressed(
        ILogger logger,
        string candidateId,
        string behaviorId,
        string authoringStyle,
        string suppressionKind,
        string suppressionReason)
    {
        AuthoringPolicySuppressed(
            logger,
            candidateId,
            behaviorId,
            authoringStyle,
            suppressionKind,
            suppressionReason,
            null);
    }

    public static void LogGovernanceSkipped(
        ILogger logger,
        string candidateId,
        string behaviorId,
        string authoringStyle,
        string skippedSuppressionIds,
        string skippedOverrideIds)
    {
        GovernanceSkipped(
            logger,
            candidateId,
            behaviorId,
            authoringStyle,
            skippedSuppressionIds,
            skippedOverrideIds,
            null);
    }
}
