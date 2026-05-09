using Cephalon.Eventing.Configuration;
using System.Globalization;

namespace Cephalon.Eventing.Services;

internal static class InProcessEventingRetryPolicy
{
    private const long MaxTimeSpanDelayMilliseconds = 922_337_203_685;

    public const string None = "none";

    public const string BoundedInProcess = "bounded-in-process";

    public const string FixedBackoff = "fixed";

    public const string ExponentialBackoff = "exponential";

    public static string GetPolicyId(EventingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return GetMaxAttempts(options) > 1 ? BoundedInProcess : None;
    }

    public static int GetMaxAttempts(EventingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return Math.Max(1, options.InProcessSubscriptionMaxAttempts);
    }

    public static int GetRetryDelayMilliseconds(EventingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return Math.Max(0, options.InProcessSubscriptionRetryDelayMilliseconds);
    }

    public static string GetBackoff(EventingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return NormalizeBackoff(options.InProcessSubscriptionRetryBackoff);
    }

    public static int GetBackoffMultiplier(EventingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return Math.Max(1, options.InProcessSubscriptionRetryBackoffMultiplier);
    }

    public static int GetMaxDelayMilliseconds(EventingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return Math.Max(0, options.InProcessSubscriptionRetryMaxDelayMilliseconds);
    }

    public static int GetJitterPercent(EventingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return Math.Clamp(options.InProcessSubscriptionRetryJitterPercent, 0, 100);
    }

    public static string NormalizeBackoff(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return FixedBackoff;
        }

        var normalized = value.Trim().ToLowerInvariant();
        return normalized switch
        {
            FixedBackoff => FixedBackoff,
            ExponentialBackoff => ExponentialBackoff,
            _ => throw new ArgumentException(
                $"In-process subscription retry backoff '{value}' is not supported. Use '{FixedBackoff}' or '{ExponentialBackoff}'.",
                nameof(value))
        };
    }

    public static TimeSpan CalculateDelay(
        EventingOptions options,
        EventPublication publication,
        EventSubscriptionDescriptor subscription,
        int failedAttempt)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(publication);
        ArgumentNullException.ThrowIfNull(subscription);

        if (failedAttempt < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(failedAttempt),
                failedAttempt,
                "Failed attempt must be greater than or equal to 1.");
        }

        var baseDelayMilliseconds = GetRetryDelayMilliseconds(options);
        if (baseDelayMilliseconds == 0)
        {
            return TimeSpan.Zero;
        }

        var delayMilliseconds = (long)baseDelayMilliseconds;
        if (string.Equals(GetBackoff(options), ExponentialBackoff, StringComparison.OrdinalIgnoreCase))
        {
            var multiplier = GetBackoffMultiplier(options);
            for (var i = 1; i < failedAttempt; i++)
            {
                delayMilliseconds = SaturatingMultiply(delayMilliseconds, multiplier);
            }
        }

        var maxDelayMilliseconds = GetMaxDelayMilliseconds(options);
        delayMilliseconds = CapDelay(delayMilliseconds, maxDelayMilliseconds);

        delayMilliseconds = ApplyDeterministicJitter(
            delayMilliseconds,
            GetJitterPercent(options),
            publication,
            subscription,
            failedAttempt);

        delayMilliseconds = CapDelay(delayMilliseconds, maxDelayMilliseconds);

        return TimeSpan.FromMilliseconds(Math.Max(0, delayMilliseconds));
    }

    private static long CapDelay(long delayMilliseconds, int maxDelayMilliseconds)
    {
        if (maxDelayMilliseconds > 0)
        {
            delayMilliseconds = Math.Min(delayMilliseconds, maxDelayMilliseconds);
        }

        return Math.Min(delayMilliseconds, MaxTimeSpanDelayMilliseconds);
    }

    private static long SaturatingMultiply(long value, int multiplier)
    {
        if (value == 0 || multiplier <= 1)
        {
            return value;
        }

        return value > long.MaxValue / multiplier
            ? long.MaxValue
            : value * multiplier;
    }

    private static long ApplyDeterministicJitter(
        long delayMilliseconds,
        int jitterPercent,
        EventPublication publication,
        EventSubscriptionDescriptor subscription,
        int failedAttempt)
    {
        if (delayMilliseconds == 0 || jitterPercent == 0)
        {
            return delayMilliseconds;
        }

        var hashInput = string.Create(
            CultureInfo.InvariantCulture,
            $"{publication.Id}|{publication.ChannelId}|{publication.EventType}|{subscription.Id}|{failedAttempt}");
        var bucket = (int)(ComputeFnv1a32(hashInput) % (uint)((jitterPercent * 2) + 1));
        var offsetPercent = bucket - jitterPercent;
        var jitteredDelay = delayMilliseconds + (delayMilliseconds * offsetPercent / 100);
        return Math.Max(0, jitteredDelay);
    }

    private static uint ComputeFnv1a32(string value)
    {
        const uint offsetBasis = 2166136261;
        const uint prime = 16777619;

        var hash = offsetBasis;
        foreach (var character in value)
        {
            hash ^= character;
            hash *= prime;
        }

        return hash;
    }
}
