using Cephalon.Eventing.Configuration;

namespace Cephalon.Eventing.Services;

internal static class InProcessEventingRetryPolicy
{
    public const string None = "none";

    public const string BoundedInProcess = "bounded-in-process";

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
}
