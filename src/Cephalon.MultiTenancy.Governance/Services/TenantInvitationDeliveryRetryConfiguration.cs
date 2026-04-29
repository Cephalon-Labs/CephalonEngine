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

    public static bool IsExecutionCoordinationEnabled(MultiTenancyGovernanceOptions options)
    {
        return options.EnableInvitationDeliveryRetryExecutionCoordination &&
            IsRetryRunnerEnabled(options);
    }

    public static string ResolveExecutionCoordinationOwnership(MultiTenancyGovernanceOptions options)
    {
        if (IsExecutionCoordinationEnabled(options))
        {
            return "cephalon-managed";
        }

        return IsRetryRunnerEnabled(options)
            ? "application-managed"
            : "not-configured";
    }

    public static string ResolveExecutionCoordinationScope(MultiTenancyGovernanceOptions options)
    {
        return IsExecutionCoordinationEnabled(options)
            ? TenantInvitationDeliveryRetryExecutionCoordinator.ScopeProcessLocal
            : TenantInvitationDeliveryRetryExecutionCoordinator.ScopeNone;
    }

    public static string ResolveExecutionCoordinationMode(MultiTenancyGovernanceOptions options)
    {
        return IsExecutionCoordinationEnabled(options)
            ? TenantInvitationDeliveryRetryExecutionCoordinator.ModeSkipOverlap
            : TenantInvitationDeliveryRetryExecutionCoordinator.ModeDisabled;
    }
}
