using Cephalon.Abstractions.Data;
using System.Globalization;

namespace Cephalon.Eventing.Services;

internal static class EventPublicationSchedulingPolicy
{
    public const string None = "none";

    public const string PolicyId = "bounded-process-local";

    public const string Scope = "process-local";

    public const string Durability = "none";

    public const string ScheduledForUtcMetadataKey = "scheduledForUtc";

    public const string DelayMillisecondsMetadataKey = "delayMilliseconds";

    public static string GetPolicyId(Configuration.EventingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return options.EnablePublicationScheduling ? PolicyId : None;
    }

    public static string GetScope(Configuration.EventingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return options.EnablePublicationScheduling ? Scope : None;
    }

    public static string GetDurability(Configuration.EventingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return options.EnablePublicationScheduling ? Durability : None;
    }

    public static bool TryCreateSchedule(
        EventPublicationRequest request,
        DateTimeOffset nowUtc,
        out EventPublicationSchedule schedule)
    {
        ArgumentNullException.ThrowIfNull(request);

        var hasScheduledForUtc = TryGetMetadataValue(request.Metadata, ScheduledForUtcMetadataKey, out var scheduledForUtcValue);
        var hasDelayMilliseconds = TryGetMetadataValue(request.Metadata, DelayMillisecondsMetadataKey, out var delayMillisecondsValue);
        if (!hasScheduledForUtc && !hasDelayMilliseconds)
        {
            schedule = default;
            return false;
        }

        if (hasScheduledForUtc && hasDelayMilliseconds)
        {
            throw new ArgumentException(
                $"Specify either '{ScheduledForUtcMetadataKey}' or '{DelayMillisecondsMetadataKey}', not both.",
                nameof(request));
        }

        if (hasScheduledForUtc)
        {
            if (!DateTimeOffset.TryParse(
                    scheduledForUtcValue,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var scheduledForUtc))
            {
                throw new ArgumentException(
                    $"Publication metadata '{ScheduledForUtcMetadataKey}' must be an ISO-8601 UTC timestamp.",
                    nameof(request));
            }

            schedule = new EventPublicationSchedule(
                scheduledForUtc.ToUniversalTime(),
                CalculateDelayMilliseconds(nowUtc, scheduledForUtc.ToUniversalTime()),
                ScheduledForUtcMetadataKey);
            return true;
        }

        if (!int.TryParse(
                delayMillisecondsValue,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var delayMilliseconds) ||
            delayMilliseconds < 0)
        {
            throw new ArgumentException(
                $"Publication metadata '{DelayMillisecondsMetadataKey}' must be a non-negative integer.",
                nameof(request));
        }

        schedule = new EventPublicationSchedule(
            nowUtc.AddMilliseconds(delayMilliseconds),
            delayMilliseconds,
            DelayMillisecondsMetadataKey);
        return true;
    }

    public static Dictionary<string, string> CreateMetadata(
        IReadOnlyDictionary<string, string> metadata,
        EventPublicationSchedule schedule,
        string state,
        int pendingCount)
    {
        var scheduledMetadata = new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase)
        {
            ["schedulePolicy"] = PolicyId,
            ["scheduleState"] = state,
            ["scheduleScope"] = Scope,
            ["scheduleDurability"] = Durability,
            ["scheduleSource"] = schedule.SourceMetadataKey,
            [ScheduledForUtcMetadataKey] = schedule.ScheduledForUtc.ToString("O", CultureInfo.InvariantCulture),
            ["scheduleDelayMilliseconds"] = schedule.DelayMilliseconds.ToString(CultureInfo.InvariantCulture),
            ["scheduledPublicationPendingCount"] = pendingCount.ToString(CultureInfo.InvariantCulture)
        };

        return scheduledMetadata;
    }

    private static bool TryGetMetadataValue(
        IReadOnlyDictionary<string, string> metadata,
        string key,
        out string value)
    {
        if (metadata.TryGetValue(key, out var configured) &&
            !string.IsNullOrWhiteSpace(configured))
        {
            value = configured.Trim();
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static int CalculateDelayMilliseconds(
        DateTimeOffset nowUtc,
        DateTimeOffset scheduledForUtc)
    {
        var delay = scheduledForUtc - nowUtc;
        return delay <= TimeSpan.Zero
            ? 0
            : (int)Math.Ceiling(delay.TotalMilliseconds);
    }
}

internal readonly record struct EventPublicationSchedule(
    DateTimeOffset ScheduledForUtc,
    int DelayMilliseconds,
    string SourceMetadataKey);
