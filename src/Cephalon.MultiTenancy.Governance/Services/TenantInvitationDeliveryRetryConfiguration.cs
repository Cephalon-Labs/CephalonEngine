using Cephalon.MultiTenancy.Governance.Configuration;

namespace Cephalon.MultiTenancy.Governance.Services;

internal static class TenantInvitationDeliveryRetryConfiguration
{
    public const int DefaultBackgroundSchedulingIntervalSeconds = 300;

    public static bool IsRetryRunnerEnabled(MultiTenancyGovernanceOptions options)
    {
        return options.EnableInvitationDeliveryRetryQueue &&
            options.EnableInvitationDeliveryDispatch;
    }

    public static bool IsBackgroundSchedulingEnabled(MultiTenancyGovernanceOptions options)
    {
        return options.EnableInvitationDeliveryRetryBackgroundScheduling &&
            IsRetryRunnerEnabled(options);
    }

    public static int ResolveBackgroundSchedulingIntervalSeconds(MultiTenancyGovernanceOptions options)
    {
        return options.InvitationDeliveryRetryBackgroundIntervalSeconds <= 0
            ? DefaultBackgroundSchedulingIntervalSeconds
            : options.InvitationDeliveryRetryBackgroundIntervalSeconds;
    }

    public static string ResolveBackgroundSchedulingOwnership(MultiTenancyGovernanceOptions options)
    {
        if (IsBackgroundSchedulingEnabled(options))
        {
            return "cephalon-managed";
        }

        return options.EnableInvitationDeliveryRetryBackgroundScheduling
            ? "not-configured"
            : "application-managed";
    }
}
