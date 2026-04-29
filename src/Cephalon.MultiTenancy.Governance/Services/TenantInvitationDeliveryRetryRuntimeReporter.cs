using Cephalon.MultiTenancy.Governance.Configuration;
using System.Globalization;

namespace Cephalon.MultiTenancy.Governance.Services;

internal sealed class TenantInvitationDeliveryRetryRuntimeReporter(
    MultiTenancyGovernanceOptions options) : ITenantInvitationDeliveryRetryRuntimeCatalog
{
    private readonly object gate = new();
    private TenantInvitationDeliveryRetryRuntimeSnapshot current = CreateInitialSnapshot(options);

    public TenantInvitationDeliveryRetryRuntimeSnapshot Current
    {
        get
        {
            lock (gate)
            {
                return current;
            }
        }
    }

    public void MarkStarted(DateTimeOffset startedAtUtc)
    {
        lock (gate)
        {
            current = CreateSnapshot(
                current,
                lastStartedAtUtc: startedAtUtc,
                lastCompletedAtUtc: current.LastCompletedAtUtc,
                lastOutcome: current.LastOutcome,
                lastAttemptedCount: current.LastAttemptedCount,
                lastDispatchedCount: current.LastDispatchedCount,
                lastFailedCount: current.LastFailedCount,
                lastExhaustedCount: current.LastExhaustedCount,
                lastTerminalCount: current.LastTerminalCount,
                lastRemainingPendingCount: current.LastRemainingPendingCount,
                lastError: null,
                runCount: current.RunCount,
                successfulRunCount: current.SuccessfulRunCount,
                failedRunCount: current.FailedRunCount);
        }
    }

    public void MarkCompleted(TenantInvitationDeliveryRetryResult result, DateTimeOffset completedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(result);

        lock (gate)
        {
            current = CreateSnapshot(
                current,
                lastStartedAtUtc: current.LastStartedAtUtc,
                lastCompletedAtUtc: completedAtUtc,
                lastOutcome: result.Outcome,
                lastAttemptedCount: result.AttemptedCount,
                lastDispatchedCount: result.DispatchedCount,
                lastFailedCount: result.FailedCount,
                lastExhaustedCount: result.ExhaustedCount,
                lastTerminalCount: result.TerminalCount,
                lastRemainingPendingCount: result.RemainingPendingCount,
                lastError: null,
                runCount: current.RunCount + 1,
                successfulRunCount: current.SuccessfulRunCount + 1,
                failedRunCount: current.FailedRunCount);
        }
    }

    public void MarkFailed(Exception exception, DateTimeOffset completedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(exception);

        lock (gate)
        {
            current = CreateSnapshot(
                current,
                lastStartedAtUtc: current.LastStartedAtUtc,
                lastCompletedAtUtc: completedAtUtc,
                lastOutcome: "failed",
                lastAttemptedCount: current.LastAttemptedCount,
                lastDispatchedCount: current.LastDispatchedCount,
                lastFailedCount: current.LastFailedCount,
                lastExhaustedCount: current.LastExhaustedCount,
                lastTerminalCount: current.LastTerminalCount,
                lastRemainingPendingCount: current.LastRemainingPendingCount,
                lastError: exception.Message,
                runCount: current.RunCount + 1,
                successfulRunCount: current.SuccessfulRunCount,
                failedRunCount: current.FailedRunCount + 1);
        }
    }

    private static TenantInvitationDeliveryRetryRuntimeSnapshot CreateInitialSnapshot(MultiTenancyGovernanceOptions options)
    {
        return new TenantInvitationDeliveryRetryRuntimeSnapshot(
            enabled: TenantInvitationDeliveryRetryConfiguration.IsBackgroundSchedulingEnabled(options),
            ownership: TenantInvitationDeliveryRetryConfiguration.ResolveBackgroundSchedulingOwnership(options),
            intervalSeconds: TenantInvitationDeliveryRetryConfiguration.ResolveBackgroundSchedulingIntervalSeconds(options),
            maxItems: TenantInvitationDeliveryRetryQueueStores.ResolveMaxItems(options),
            runOnStartup: options.InvitationDeliveryRetryBackgroundRunOnStartup,
            runCount: 0,
            successfulRunCount: 0,
            failedRunCount: 0,
            lastStartedAtUtc: null,
            lastCompletedAtUtc: null,
            lastOutcome: null,
            lastAttemptedCount: 0,
            lastDispatchedCount: 0,
            lastFailedCount: 0,
            lastExhaustedCount: 0,
            lastTerminalCount: 0,
            lastRemainingPendingCount: 0,
            lastError: null,
            metadata: BuildMetadata(options));
    }

    private static TenantInvitationDeliveryRetryRuntimeSnapshot CreateSnapshot(
        TenantInvitationDeliveryRetryRuntimeSnapshot current,
        DateTimeOffset? lastStartedAtUtc,
        DateTimeOffset? lastCompletedAtUtc,
        string? lastOutcome,
        int lastAttemptedCount,
        int lastDispatchedCount,
        int lastFailedCount,
        int lastExhaustedCount,
        int lastTerminalCount,
        int lastRemainingPendingCount,
        string? lastError,
        long runCount,
        long successfulRunCount,
        long failedRunCount)
    {
        var metadata = new Dictionary<string, string>(current.Metadata, StringComparer.OrdinalIgnoreCase)
        {
            ["runCount"] = runCount.ToString(CultureInfo.InvariantCulture),
            ["successfulRunCount"] = successfulRunCount.ToString(CultureInfo.InvariantCulture),
            ["failedRunCount"] = failedRunCount.ToString(CultureInfo.InvariantCulture),
            ["lastAttemptedCount"] = lastAttemptedCount.ToString(CultureInfo.InvariantCulture),
            ["lastDispatchedCount"] = lastDispatchedCount.ToString(CultureInfo.InvariantCulture),
            ["lastFailedCount"] = lastFailedCount.ToString(CultureInfo.InvariantCulture),
            ["lastExhaustedCount"] = lastExhaustedCount.ToString(CultureInfo.InvariantCulture),
            ["lastTerminalCount"] = lastTerminalCount.ToString(CultureInfo.InvariantCulture),
            ["lastRemainingPendingCount"] = lastRemainingPendingCount.ToString(CultureInfo.InvariantCulture)
        };

        if (lastStartedAtUtc is not null)
        {
            metadata["lastStartedAtUtc"] = lastStartedAtUtc.Value.ToString("O", CultureInfo.InvariantCulture);
        }

        if (lastCompletedAtUtc is not null)
        {
            metadata["lastCompletedAtUtc"] = lastCompletedAtUtc.Value.ToString("O", CultureInfo.InvariantCulture);
        }

        if (!string.IsNullOrWhiteSpace(lastOutcome))
        {
            metadata["lastOutcome"] = lastOutcome;
        }

        if (!string.IsNullOrWhiteSpace(lastError))
        {
            metadata["lastError"] = lastError;
        }
        else
        {
            metadata.Remove("lastError");
        }

        return new TenantInvitationDeliveryRetryRuntimeSnapshot(
            current.Enabled,
            current.Ownership,
            current.IntervalSeconds,
            current.MaxItems,
            current.RunOnStartup,
            runCount,
            successfulRunCount,
            failedRunCount,
            lastStartedAtUtc,
            lastCompletedAtUtc,
            lastOutcome,
            lastAttemptedCount,
            lastDispatchedCount,
            lastFailedCount,
            lastExhaustedCount,
            lastTerminalCount,
            lastRemainingPendingCount,
            lastError,
            metadata);
    }

    private static Dictionary<string, string> BuildMetadata(MultiTenancyGovernanceOptions options)
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["enabled"] = TenantInvitationDeliveryRetryConfiguration.IsBackgroundSchedulingEnabled(options).ToString().ToLowerInvariant(),
            ["ownership"] = TenantInvitationDeliveryRetryConfiguration.ResolveBackgroundSchedulingOwnership(options),
            ["intervalSeconds"] = TenantInvitationDeliveryRetryConfiguration.ResolveBackgroundSchedulingIntervalSeconds(options).ToString(CultureInfo.InvariantCulture),
            ["maxItems"] = TenantInvitationDeliveryRetryQueueStores.ResolveMaxItems(options).ToString(CultureInfo.InvariantCulture),
            ["runOnStartup"] = options.InvitationDeliveryRetryBackgroundRunOnStartup.ToString().ToLowerInvariant(),
            ["runCount"] = "0",
            ["successfulRunCount"] = "0",
            ["failedRunCount"] = "0"
        };
    }
}
