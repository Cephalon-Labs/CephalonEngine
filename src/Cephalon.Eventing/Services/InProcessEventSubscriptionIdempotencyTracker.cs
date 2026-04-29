using Cephalon.Eventing.Configuration;

namespace Cephalon.Eventing.Services;

internal sealed class InProcessEventSubscriptionIdempotencyTracker(EventingOptions options)
{
    private readonly Lock gate = new();
    private readonly Dictionary<IdempotencyKey, DateTimeOffset> completedExecutions = [];

    public bool TryGetCompleted(
        string subscriptionId,
        string publicationId,
        DateTimeOffset observedAtUtc,
        out DateTimeOffset completedAtUtc)
    {
        completedAtUtc = default;
        if (!InProcessEventingIdempotencyPolicy.IsEnabled(options))
        {
            return false;
        }

        var key = new IdempotencyKey(subscriptionId.Trim(), publicationId.Trim());
        lock (gate)
        {
            PruneExpired(observedAtUtc);
            return completedExecutions.TryGetValue(key, out completedAtUtc);
        }
    }

    public void MarkCompleted(
        string subscriptionId,
        string publicationId,
        DateTimeOffset completedAtUtc)
    {
        if (!InProcessEventingIdempotencyPolicy.IsEnabled(options))
        {
            return;
        }

        var key = new IdempotencyKey(subscriptionId.Trim(), publicationId.Trim());
        lock (gate)
        {
            PruneExpired(completedAtUtc);
            completedExecutions[key] = completedAtUtc;
        }
    }

    private void PruneExpired(DateTimeOffset observedAtUtc)
    {
        var retention = TimeSpan.FromMinutes(InProcessEventingIdempotencyPolicy.GetRetentionMinutes(options));
        foreach (var pair in completedExecutions.ToArray())
        {
            if (observedAtUtc - pair.Value > retention)
            {
                completedExecutions.Remove(pair.Key);
            }
        }
    }

    private readonly record struct IdempotencyKey(string SubscriptionId, string PublicationId)
    {
        public bool Equals(IdempotencyKey other)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(SubscriptionId, other.SubscriptionId) &&
                StringComparer.OrdinalIgnoreCase.Equals(PublicationId, other.PublicationId);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(SubscriptionId),
                StringComparer.OrdinalIgnoreCase.GetHashCode(PublicationId));
        }
    }
}
