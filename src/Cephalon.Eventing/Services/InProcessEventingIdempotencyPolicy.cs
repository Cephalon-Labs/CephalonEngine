using Cephalon.Eventing.Configuration;

namespace Cephalon.Eventing.Services;

internal static class InProcessEventingIdempotencyPolicy
{
    public const string None = "none";

    public const string CompletedPublication = "completed-publication";

    public const string KeyShape = "subscription-publication";

    public const string Scope = "process-local";

    public const string Durability = "none";

    public static bool IsEnabled(EventingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return options.EnableInProcessSubscriptionIdempotency;
    }

    public static string GetPolicyId(EventingOptions options)
    {
        return IsEnabled(options) ? CompletedPublication : None;
    }

    public static string GetKeyShape(EventingOptions options)
    {
        return IsEnabled(options) ? KeyShape : None;
    }

    public static string GetScope(EventingOptions options)
    {
        return IsEnabled(options) ? Scope : None;
    }

    public static int GetRetentionMinutes(EventingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return Math.Max(1, options.InProcessSubscriptionIdempotencyRetentionMinutes);
    }
}
