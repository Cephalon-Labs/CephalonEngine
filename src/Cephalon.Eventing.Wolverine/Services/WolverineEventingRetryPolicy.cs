using Cephalon.Eventing.Wolverine.Configuration;

namespace Cephalon.Eventing.Wolverine.Services;

internal static class WolverineEventingRetryPolicy
{
    public const string None = "none";

    public const string BoundedFixedDelay = "bounded-fixed-delay";

    public static string GetSubscriptionPolicyId(WolverineEventingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return GetSubscriptionMaxAttempts(options) > 1 ? BoundedFixedDelay : None;
    }

    public static int GetSubscriptionMaxAttempts(WolverineEventingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return Math.Max(1, options.SubscriptionMaxAttempts);
    }

    public static int GetSubscriptionRetryDelaySeconds(WolverineEventingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return Math.Max(1, options.SubscriptionRetryDelaySeconds);
    }
}
