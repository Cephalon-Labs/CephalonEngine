using Cephalon.Eventing.Configuration;

namespace Cephalon.Eventing.Services;

internal static class InProcessEventingIdempotencyPolicy
{
    public const string None = "none";

    public const string CompletedPublication = "completed-publication";

    public const string KeyShape = "subscription-publication";

    public const string Scope = "process-local";

    public const string Durability = "none";

    public const string ProcessLocalStore = "process-local";

    public const string InboxStore = "inbox";

    public const string InboxScope = "durable-store";

    public const string InboxDurability = "inbox";

    public static bool IsEnabled(EventingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return options.EnableInProcessSubscriptionIdempotency;
    }

    public static bool UsesInbox(EventingOptions options)
    {
        return IsEnabled(options) &&
            string.Equals(options.InProcessSubscriptionIdempotencyStore, InboxStore, StringComparison.OrdinalIgnoreCase);
    }

    public static string GetPolicyId(EventingOptions options)
    {
        return IsEnabled(options) ? CompletedPublication : None;
    }

    public static string GetStore(EventingOptions options)
    {
        return IsEnabled(options) ? NormalizeStore(options.InProcessSubscriptionIdempotencyStore) : None;
    }

    public static string GetKeyShape(EventingOptions options)
    {
        return IsEnabled(options) ? KeyShape : None;
    }

    public static string GetScope(EventingOptions options)
    {
        if (!IsEnabled(options))
        {
            return None;
        }

        return UsesInbox(options) ? InboxScope : Scope;
    }

    public static string GetDurability(EventingOptions options)
    {
        if (!IsEnabled(options))
        {
            return None;
        }

        return UsesInbox(options) ? InboxDurability : Durability;
    }

    public static int GetRetentionMinutes(EventingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return Math.Max(1, options.InProcessSubscriptionIdempotencyRetentionMinutes);
    }

    public static string NormalizeStore(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("In-process subscription idempotency store must be a non-empty value.", nameof(value));
        }

        var normalized = value.Trim();
        if (string.Equals(normalized, ProcessLocalStore, StringComparison.OrdinalIgnoreCase))
        {
            return ProcessLocalStore;
        }

        if (string.Equals(normalized, InboxStore, StringComparison.OrdinalIgnoreCase))
        {
            return InboxStore;
        }

        throw new ArgumentOutOfRangeException(
            nameof(value),
            value,
            $"In-process subscription idempotency store must be either '{ProcessLocalStore}' or '{InboxStore}'.");
    }
}
