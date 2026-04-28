using Cephalon.MultiTenancy.Governance.Configuration;

namespace Cephalon.MultiTenancy.Governance.Services;

internal static class TenantDomainOwnershipProofPollingConfiguration
{
    public const int DefaultBatchLimit = 50;
    public const int DefaultBackgroundPollingIntervalSeconds = 300;

    public static bool IsProofVerificationRunnerEnabled(MultiTenancyGovernanceOptions options)
    {
        return options.EnableDomainOwnershipProofVerificationRunner &&
            options.EnableDomainOwnershipProofChallengeIssuance &&
            options.EnableDomainOwnershipProofPublicationPlanning &&
            options.EnableDomainOwnershipProofEvaluation &&
            options.EnableDomainOwnershipVerificationWorkflow;
    }

    public static bool IsProofPollingRunnerEnabled(MultiTenancyGovernanceOptions options)
    {
        return options.EnableDomainOwnershipProofPollingRunner &&
            IsProofVerificationRunnerEnabled(options);
    }

    public static bool IsBackgroundPollingEnabled(MultiTenancyGovernanceOptions options)
    {
        return options.EnableDomainOwnershipProofBackgroundPolling &&
            IsProofPollingRunnerEnabled(options);
    }

    public static int ResolveBatchLimit(MultiTenancyGovernanceOptions options, int? requestedMaxItems = null)
    {
        return requestedMaxItems ?? (options.DomainOwnershipProofPollingMaxItems <= 0
            ? DefaultBatchLimit
            : options.DomainOwnershipProofPollingMaxItems);
    }

    public static int ResolveBackgroundPollingIntervalSeconds(MultiTenancyGovernanceOptions options)
    {
        return options.DomainOwnershipProofBackgroundPollingIntervalSeconds <= 0
            ? DefaultBackgroundPollingIntervalSeconds
            : options.DomainOwnershipProofBackgroundPollingIntervalSeconds;
    }

    public static string ResolveBackgroundPollingOwnership(MultiTenancyGovernanceOptions options)
    {
        if (IsBackgroundPollingEnabled(options))
        {
            return "cephalon-managed";
        }

        return options.EnableDomainOwnershipProofBackgroundPolling
            ? "not-configured"
            : "application-managed";
    }
}
