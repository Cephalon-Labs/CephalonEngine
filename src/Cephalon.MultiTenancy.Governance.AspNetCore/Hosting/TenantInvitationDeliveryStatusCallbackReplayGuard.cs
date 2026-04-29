namespace Cephalon.MultiTenancy.Governance.AspNetCore.Hosting;

internal sealed class TenantInvitationDeliveryStatusCallbackReplayGuard
{
    private readonly Lock gate = new();
    private readonly Dictionary<string, DateTimeOffset> acceptedCallbacks = new(StringComparer.Ordinal);

    public TenantInvitationDeliveryStatusCallbackReplayDecision TryRecord(
        string replayKey,
        DateTimeOffset observedAtUtc,
        TimeSpan retention,
        int cacheLimit)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(replayKey);

        var effectiveRetention = retention <= TimeSpan.Zero ? TimeSpan.FromSeconds(1) : retention;
        var effectiveCacheLimit = Math.Max(1, cacheLimit);
        var normalizedReplayKey = replayKey.Trim();

        lock (gate)
        {
            PruneExpired(observedAtUtc, effectiveRetention);
            if (acceptedCallbacks.ContainsKey(normalizedReplayKey))
            {
                return new TenantInvitationDeliveryStatusCallbackReplayDecision(
                    Accepted: false,
                    Outcome: "duplicate-rejected",
                    CacheSize: acceptedCallbacks.Count);
            }

            while (acceptedCallbacks.Count >= effectiveCacheLimit)
            {
                RemoveOldest();
            }

            acceptedCallbacks[normalizedReplayKey] = observedAtUtc;
            return new TenantInvitationDeliveryStatusCallbackReplayDecision(
                Accepted: true,
                Outcome: "recorded",
                CacheSize: acceptedCallbacks.Count);
        }
    }

    public void Forget(string replayKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(replayKey);

        lock (gate)
        {
            acceptedCallbacks.Remove(replayKey.Trim());
        }
    }

    private void PruneExpired(DateTimeOffset observedAtUtc, TimeSpan retention)
    {
        foreach (var pair in acceptedCallbacks.ToArray())
        {
            if (observedAtUtc - pair.Value > retention)
            {
                acceptedCallbacks.Remove(pair.Key);
            }
        }
    }

    private void RemoveOldest()
    {
        string? oldestKey = null;
        var oldestObservedAtUtc = DateTimeOffset.MaxValue;
        foreach (var pair in acceptedCallbacks)
        {
            if (pair.Value < oldestObservedAtUtc)
            {
                oldestKey = pair.Key;
                oldestObservedAtUtc = pair.Value;
            }
        }

        if (oldestKey is not null)
        {
            acceptedCallbacks.Remove(oldestKey);
        }
    }
}

internal sealed record TenantInvitationDeliveryStatusCallbackReplayDecision(
    bool Accepted,
    string Outcome,
    int CacheSize);
