using Microsoft.Extensions.Logging;

namespace Cephalon.Behaviors.Http.Hosting;

internal static partial class RestBehaviorGovernanceLoggerMessages
{
    private static readonly Action<ILogger, string, string, string, string, string, string, Exception?> GovernanceSuppressed =
        LoggerMessage.Define<string, string, string, string, string, string>(
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

    [LoggerMessage(
        EventId = RestBehaviorGovernanceDiagnosticsConventions.OverrideAppliedId,
        Level = LogLevel.Information,
        Message = RestBehaviorGovernanceDiagnosticsConventions.OverrideAppliedMessageTemplate)]
    private static partial void OverrideApplied(
        ILogger logger,
        string candidateId,
        string behaviorId,
        string overrideId,
        string overrideSelectionBasis,
        string selectedOverrideActionKinds,
        string appliedOverrideActionKinds,
        string routePattern);

    [LoggerMessage(
        EventId = RestBehaviorGovernanceDiagnosticsConventions.OverrideNoOpId,
        Level = LogLevel.Information,
        Message = RestBehaviorGovernanceDiagnosticsConventions.OverrideNoOpMessageTemplate)]
    private static partial void OverrideNoOp(
        ILogger logger,
        string candidateId,
        string behaviorId,
        string selectedOverrideId,
        string matchedOverrideIds,
        string overrideSelectionBasis,
        string selectedOverrideActionKinds,
        string appliedOverrideActionKinds);

    public static void LogGovernanceSuppressed(
        ILogger logger,
        string candidateId,
        string behaviorId,
        string authoringStyle,
        string suppressionId,
        string suppressionSelectionBasis,
        string matchedSuppressionIds)
    {
        GovernanceSuppressed(
            logger,
            candidateId,
            behaviorId,
            authoringStyle,
            suppressionId,
            suppressionSelectionBasis,
            matchedSuppressionIds,
            null);
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
        string overrideSelectionBasis,
        string selectedOverrideActionKinds,
        string appliedOverrideActionKinds,
        string routePattern)
    {
        OverrideApplied(
            logger,
            candidateId,
            behaviorId,
            overrideId,
            overrideSelectionBasis,
            selectedOverrideActionKinds,
            appliedOverrideActionKinds,
            routePattern);
    }

    public static void LogOverrideNoOp(
        ILogger logger,
        string candidateId,
        string behaviorId,
        string selectedOverrideId,
        string matchedOverrideIds,
        string overrideSelectionBasis,
        string selectedOverrideActionKinds,
        string appliedOverrideActionKinds)
    {
        OverrideNoOp(
            logger,
            candidateId,
            behaviorId,
            selectedOverrideId,
            matchedOverrideIds,
            overrideSelectionBasis,
            selectedOverrideActionKinds,
            appliedOverrideActionKinds);
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
