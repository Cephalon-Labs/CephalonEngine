using Cephalon.MultiTenancy.Governance.Configuration;
using System.Globalization;

namespace Cephalon.MultiTenancy.Governance.Services;

internal sealed class TenantInvitationDeliveryRetryExecutionCoordinator(
    MultiTenancyGovernanceOptions options) : ITenantInvitationDeliveryRetryExecutionCoordinationCatalog, IDisposable
{
    internal const string ScopeProcessLocal = "process-local";
    internal const string ScopeNone = "none";
    internal const string ModeSkipOverlap = "skip-overlap";
    internal const string ModeDisabled = "disabled";

    private readonly SemaphoreSlim executionGate = new(1, 1);
    private readonly object stateGate = new();
    private TenantInvitationDeliveryRetryExecutionCoordinationSnapshot current = CreateInitialSnapshot(options);
    private bool disposed;

    public TenantInvitationDeliveryRetryExecutionCoordinationSnapshot Current
    {
        get
        {
            lock (stateGate)
            {
                return current;
            }
        }
    }

    public TenantInvitationDeliveryRetryExecutionLease TryBegin(DateTimeOffset startedAtUtc)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        if (!TenantInvitationDeliveryRetryConfiguration.IsExecutionCoordinationEnabled(options))
        {
            return TenantInvitationDeliveryRetryExecutionLease.Uncoordinated;
        }

        if (!executionGate.Wait(0))
        {
            MarkSkipped(startedAtUtc);
            return TenantInvitationDeliveryRetryExecutionLease.Skipped;
        }

        MarkStarted(startedAtUtc);
        return TenantInvitationDeliveryRetryExecutionLease.Coordinated;
    }

    public void MarkCompleted(
        TenantInvitationDeliveryRetryExecutionLease lease,
        TenantInvitationDeliveryRetryResult result,
        DateTimeOffset completedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (!lease.IsCoordinated)
        {
            return;
        }

        try
        {
            lock (stateGate)
            {
                current = CreateSnapshot(
                    current,
                    isRunning: false,
                    attemptCount: current.AttemptCount,
                    acceptedCount: current.AcceptedCount,
                    skippedCount: current.SkippedCount,
                    completedCount: current.CompletedCount + 1,
                    failedCount: current.FailedCount,
                    lastStartedAtUtc: current.LastStartedAtUtc,
                    lastCompletedAtUtc: completedAtUtc,
                    lastSkippedAtUtc: current.LastSkippedAtUtc,
                    lastOutcome: result.Outcome,
                    lastError: null);
            }
        }
        finally
        {
            executionGate.Release();
        }
    }

    public void MarkFailed(
        TenantInvitationDeliveryRetryExecutionLease lease,
        Exception exception,
        DateTimeOffset completedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (!lease.IsCoordinated)
        {
            return;
        }

        try
        {
            var outcome = exception is OperationCanceledException
                ? "canceled"
                : TenantInvitationDeliveryRetryOutcomes.Failed;

            lock (stateGate)
            {
                current = CreateSnapshot(
                    current,
                    isRunning: false,
                    attemptCount: current.AttemptCount,
                    acceptedCount: current.AcceptedCount,
                    skippedCount: current.SkippedCount,
                    completedCount: current.CompletedCount,
                    failedCount: current.FailedCount + 1,
                    lastStartedAtUtc: current.LastStartedAtUtc,
                    lastCompletedAtUtc: completedAtUtc,
                    lastSkippedAtUtc: current.LastSkippedAtUtc,
                    lastOutcome: outcome,
                    lastError: exception.Message);
            }
        }
        finally
        {
            executionGate.Release();
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        executionGate.Dispose();
    }

    private void MarkStarted(DateTimeOffset startedAtUtc)
    {
        lock (stateGate)
        {
            current = CreateSnapshot(
                current,
                isRunning: true,
                attemptCount: current.AttemptCount + 1,
                acceptedCount: current.AcceptedCount + 1,
                skippedCount: current.SkippedCount,
                completedCount: current.CompletedCount,
                failedCount: current.FailedCount,
                lastStartedAtUtc: startedAtUtc,
                lastCompletedAtUtc: current.LastCompletedAtUtc,
                lastSkippedAtUtc: current.LastSkippedAtUtc,
                lastOutcome: current.LastOutcome,
                lastError: null);
        }
    }

    private void MarkSkipped(DateTimeOffset skippedAtUtc)
    {
        lock (stateGate)
        {
            current = CreateSnapshot(
                current,
                isRunning: current.IsRunning,
                attemptCount: current.AttemptCount + 1,
                acceptedCount: current.AcceptedCount,
                skippedCount: current.SkippedCount + 1,
                completedCount: current.CompletedCount,
                failedCount: current.FailedCount,
                lastStartedAtUtc: current.LastStartedAtUtc,
                lastCompletedAtUtc: current.LastCompletedAtUtc,
                lastSkippedAtUtc: skippedAtUtc,
                lastOutcome: TenantInvitationDeliveryRetryOutcomes.AlreadyRunning,
                lastError: null);
        }
    }

    private static TenantInvitationDeliveryRetryExecutionCoordinationSnapshot CreateInitialSnapshot(
        MultiTenancyGovernanceOptions options)
    {
        return new TenantInvitationDeliveryRetryExecutionCoordinationSnapshot(
            enabled: TenantInvitationDeliveryRetryConfiguration.IsExecutionCoordinationEnabled(options),
            ownership: TenantInvitationDeliveryRetryConfiguration.ResolveExecutionCoordinationOwnership(options),
            scope: TenantInvitationDeliveryRetryConfiguration.ResolveExecutionCoordinationScope(options),
            mode: TenantInvitationDeliveryRetryConfiguration.ResolveExecutionCoordinationMode(options),
            isRunning: false,
            attemptCount: 0,
            acceptedCount: 0,
            skippedCount: 0,
            completedCount: 0,
            failedCount: 0,
            lastStartedAtUtc: null,
            lastCompletedAtUtc: null,
            lastSkippedAtUtc: null,
            lastOutcome: null,
            lastError: null,
            metadata: BuildMetadata(options, isRunning: false, 0, 0, 0, 0, 0, null, null, null, null, null));
    }

    private static TenantInvitationDeliveryRetryExecutionCoordinationSnapshot CreateSnapshot(
        TenantInvitationDeliveryRetryExecutionCoordinationSnapshot current,
        bool isRunning,
        long attemptCount,
        long acceptedCount,
        long skippedCount,
        long completedCount,
        long failedCount,
        DateTimeOffset? lastStartedAtUtc,
        DateTimeOffset? lastCompletedAtUtc,
        DateTimeOffset? lastSkippedAtUtc,
        string? lastOutcome,
        string? lastError)
    {
        return new TenantInvitationDeliveryRetryExecutionCoordinationSnapshot(
            current.Enabled,
            current.Ownership,
            current.Scope,
            current.Mode,
            isRunning,
            attemptCount,
            acceptedCount,
            skippedCount,
            completedCount,
            failedCount,
            lastStartedAtUtc,
            lastCompletedAtUtc,
            lastSkippedAtUtc,
            lastOutcome,
            lastError,
            BuildMetadata(
                current.Enabled,
                current.Ownership,
                current.Scope,
                current.Mode,
                isRunning,
                attemptCount,
                acceptedCount,
                skippedCount,
                completedCount,
                failedCount,
                lastStartedAtUtc,
                lastCompletedAtUtc,
                lastSkippedAtUtc,
                lastOutcome,
                lastError));
    }

    private static Dictionary<string, string> BuildMetadata(
        MultiTenancyGovernanceOptions options,
        bool isRunning,
        long attemptCount,
        long acceptedCount,
        long skippedCount,
        long completedCount,
        long failedCount,
        DateTimeOffset? lastStartedAtUtc,
        DateTimeOffset? lastCompletedAtUtc,
        DateTimeOffset? lastSkippedAtUtc,
        string? lastOutcome,
        string? lastError)
    {
        return BuildMetadata(
            TenantInvitationDeliveryRetryConfiguration.IsExecutionCoordinationEnabled(options),
            TenantInvitationDeliveryRetryConfiguration.ResolveExecutionCoordinationOwnership(options),
            TenantInvitationDeliveryRetryConfiguration.ResolveExecutionCoordinationScope(options),
            TenantInvitationDeliveryRetryConfiguration.ResolveExecutionCoordinationMode(options),
            isRunning,
            attemptCount,
            acceptedCount,
            skippedCount,
            completedCount,
            failedCount,
            lastStartedAtUtc,
            lastCompletedAtUtc,
            lastSkippedAtUtc,
            lastOutcome,
            lastError);
    }

    private static Dictionary<string, string> BuildMetadata(
        bool enabled,
        string ownership,
        string scope,
        string mode,
        bool isRunning,
        long attemptCount,
        long acceptedCount,
        long skippedCount,
        long completedCount,
        long failedCount,
        DateTimeOffset? lastStartedAtUtc,
        DateTimeOffset? lastCompletedAtUtc,
        DateTimeOffset? lastSkippedAtUtc,
        string? lastOutcome,
        string? lastError)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["enabled"] = enabled.ToString().ToLowerInvariant(),
            ["ownership"] = ownership,
            ["scope"] = scope,
            ["mode"] = mode,
            ["isRunning"] = isRunning.ToString().ToLowerInvariant(),
            ["attemptCount"] = attemptCount.ToString(CultureInfo.InvariantCulture),
            ["acceptedCount"] = acceptedCount.ToString(CultureInfo.InvariantCulture),
            ["skippedCount"] = skippedCount.ToString(CultureInfo.InvariantCulture),
            ["completedCount"] = completedCount.ToString(CultureInfo.InvariantCulture),
            ["failedCount"] = failedCount.ToString(CultureInfo.InvariantCulture)
        };

        if (lastStartedAtUtc is not null)
        {
            metadata["lastStartedAtUtc"] = lastStartedAtUtc.Value.ToString("O", CultureInfo.InvariantCulture);
        }

        if (lastCompletedAtUtc is not null)
        {
            metadata["lastCompletedAtUtc"] = lastCompletedAtUtc.Value.ToString("O", CultureInfo.InvariantCulture);
        }

        if (lastSkippedAtUtc is not null)
        {
            metadata["lastSkippedAtUtc"] = lastSkippedAtUtc.Value.ToString("O", CultureInfo.InvariantCulture);
        }

        if (!string.IsNullOrWhiteSpace(lastOutcome))
        {
            metadata["lastOutcome"] = lastOutcome;
        }

        if (!string.IsNullOrWhiteSpace(lastError))
        {
            metadata["lastError"] = lastError;
        }

        return metadata;
    }
}

internal readonly record struct TenantInvitationDeliveryRetryExecutionLease(
    bool ShouldExecute,
    bool IsCoordinated)
{
    public static TenantInvitationDeliveryRetryExecutionLease Coordinated { get; } = new(
        ShouldExecute: true,
        IsCoordinated: true);

    public static TenantInvitationDeliveryRetryExecutionLease Uncoordinated { get; } = new(
        ShouldExecute: true,
        IsCoordinated: false);

    public static TenantInvitationDeliveryRetryExecutionLease Skipped { get; } = new(
        ShouldExecute: false,
        IsCoordinated: false);
}
