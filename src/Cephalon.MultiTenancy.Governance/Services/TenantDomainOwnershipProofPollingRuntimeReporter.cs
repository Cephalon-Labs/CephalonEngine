using Cephalon.MultiTenancy.Governance.Configuration;
using System.Globalization;

namespace Cephalon.MultiTenancy.Governance.Services;

internal sealed class TenantDomainOwnershipProofPollingRuntimeReporter(
    MultiTenancyGovernanceOptions options) : ITenantDomainOwnershipProofPollingRuntimeCatalog
{
    private readonly object gate = new();
    private TenantDomainOwnershipProofPollingRuntimeSnapshot current = CreateInitialSnapshot(options);

    public TenantDomainOwnershipProofPollingRuntimeSnapshot Current
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
                lastReason: current.LastReason,
                lastCandidateCount: current.LastCandidateCount,
                lastVerificationCount: current.LastVerificationCount,
                lastVerifiedCount: current.LastVerifiedCount,
                lastRejectedCount: current.LastRejectedCount,
                lastFailedCount: current.LastFailedCount,
                lastError: null,
                runCount: current.RunCount,
                successfulRunCount: current.SuccessfulRunCount,
                failedRunCount: current.FailedRunCount);
        }
    }

    public void MarkCompleted(TenantDomainOwnershipProofPollingResult result, DateTimeOffset completedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(result);

        lock (gate)
        {
            current = CreateSnapshot(
                current,
                lastStartedAtUtc: current.LastStartedAtUtc,
                lastCompletedAtUtc: completedAtUtc,
                lastOutcome: result.Outcome,
                lastReason: result.Reason,
                lastCandidateCount: result.CandidateCount,
                lastVerificationCount: result.VerificationCount,
                lastVerifiedCount: result.VerifiedCount,
                lastRejectedCount: result.RejectedCount,
                lastFailedCount: result.FailedCount,
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
                lastReason: "Tenant-domain ownership proof background polling failed before a polling result was produced.",
                lastCandidateCount: current.LastCandidateCount,
                lastVerificationCount: current.LastVerificationCount,
                lastVerifiedCount: current.LastVerifiedCount,
                lastRejectedCount: current.LastRejectedCount,
                lastFailedCount: current.LastFailedCount,
                lastError: exception.Message,
                runCount: current.RunCount + 1,
                successfulRunCount: current.SuccessfulRunCount,
                failedRunCount: current.FailedRunCount + 1);
        }
    }

    private static TenantDomainOwnershipProofPollingRuntimeSnapshot CreateInitialSnapshot(MultiTenancyGovernanceOptions options)
    {
        return new TenantDomainOwnershipProofPollingRuntimeSnapshot(
            enabled: TenantDomainOwnershipProofPollingConfiguration.IsBackgroundPollingEnabled(options),
            ownership: TenantDomainOwnershipProofPollingConfiguration.ResolveBackgroundPollingOwnership(options),
            intervalSeconds: TenantDomainOwnershipProofPollingConfiguration.ResolveBackgroundPollingIntervalSeconds(options),
            batchLimit: TenantDomainOwnershipProofPollingConfiguration.ResolveBatchLimit(options),
            runOnStartup: options.DomainOwnershipProofBackgroundPollingRunOnStartup,
            dnsTxtResolverConfigured: options.DomainOwnershipDnsTxtProofResolverEndpoint is not null,
            runCount: 0,
            successfulRunCount: 0,
            failedRunCount: 0,
            lastStartedAtUtc: null,
            lastCompletedAtUtc: null,
            lastOutcome: null,
            lastReason: null,
            lastCandidateCount: 0,
            lastVerificationCount: 0,
            lastVerifiedCount: 0,
            lastRejectedCount: 0,
            lastFailedCount: 0,
            lastError: null,
            metadata: BuildMetadata(options));
    }

    private static TenantDomainOwnershipProofPollingRuntimeSnapshot CreateSnapshot(
        TenantDomainOwnershipProofPollingRuntimeSnapshot current,
        DateTimeOffset? lastStartedAtUtc,
        DateTimeOffset? lastCompletedAtUtc,
        string? lastOutcome,
        string? lastReason,
        int lastCandidateCount,
        int lastVerificationCount,
        int lastVerifiedCount,
        int lastRejectedCount,
        int lastFailedCount,
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
            ["lastCandidateCount"] = lastCandidateCount.ToString(CultureInfo.InvariantCulture),
            ["lastVerificationCount"] = lastVerificationCount.ToString(CultureInfo.InvariantCulture),
            ["lastVerifiedCount"] = lastVerifiedCount.ToString(CultureInfo.InvariantCulture),
            ["lastRejectedCount"] = lastRejectedCount.ToString(CultureInfo.InvariantCulture),
            ["lastFailedCount"] = lastFailedCount.ToString(CultureInfo.InvariantCulture)
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

        if (!string.IsNullOrWhiteSpace(lastReason))
        {
            metadata["lastReason"] = lastReason;
        }

        if (!string.IsNullOrWhiteSpace(lastError))
        {
            metadata["lastError"] = lastError;
        }
        else
        {
            metadata.Remove("lastError");
        }

        return new TenantDomainOwnershipProofPollingRuntimeSnapshot(
            current.Enabled,
            current.Ownership,
            current.IntervalSeconds,
            current.BatchLimit,
            current.RunOnStartup,
            current.DnsTxtResolverConfigured,
            runCount,
            successfulRunCount,
            failedRunCount,
            lastStartedAtUtc,
            lastCompletedAtUtc,
            lastOutcome,
            lastReason,
            lastCandidateCount,
            lastVerificationCount,
            lastVerifiedCount,
            lastRejectedCount,
            lastFailedCount,
            lastError,
            metadata);
    }

    private static Dictionary<string, string> BuildMetadata(MultiTenancyGovernanceOptions options)
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["enabled"] = TenantDomainOwnershipProofPollingConfiguration.IsBackgroundPollingEnabled(options).ToString().ToLowerInvariant(),
            ["ownership"] = TenantDomainOwnershipProofPollingConfiguration.ResolveBackgroundPollingOwnership(options),
            ["intervalSeconds"] = TenantDomainOwnershipProofPollingConfiguration.ResolveBackgroundPollingIntervalSeconds(options).ToString(CultureInfo.InvariantCulture),
            ["batchLimit"] = TenantDomainOwnershipProofPollingConfiguration.ResolveBatchLimit(options).ToString(CultureInfo.InvariantCulture),
            ["runOnStartup"] = options.DomainOwnershipProofBackgroundPollingRunOnStartup.ToString().ToLowerInvariant(),
            ["dnsTxtResolverConfigured"] = (options.DomainOwnershipDnsTxtProofResolverEndpoint is not null).ToString().ToLowerInvariant(),
            ["runCount"] = "0",
            ["successfulRunCount"] = "0",
            ["failedRunCount"] = "0"
        };
    }
}
