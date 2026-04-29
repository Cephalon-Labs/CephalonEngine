using Cephalon.Eventing.Wolverine.Configuration;

namespace Cephalon.Eventing.Wolverine.Services;

internal static class WolverineEventingRetryPolicy
{
    public const string None = "none";

    public const string BoundedFixedDelay = "bounded-fixed-delay";

    public static string GetDispatchPolicyId(WolverineEventingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return GetDispatchMaxAttempts(options) > 1 ? BoundedFixedDelay : None;
    }

    public static int GetDispatchMaxAttempts(WolverineEventingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return Math.Max(1, options.DispatchMaxAttempts);
    }

    public static int GetDispatchRetryDelaySeconds(WolverineEventingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return Math.Max(1, options.RetryDelaySeconds);
    }

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
