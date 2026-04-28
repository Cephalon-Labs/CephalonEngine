using Cephalon.MultiTenancy.Governance.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cephalon.MultiTenancy.Governance.Services;

internal sealed class TenantDomainOwnershipProofPollingHostedService(
    MultiTenancyGovernanceOptions options,
    ITenantDomainOwnershipProofPollingRunner pollingRunner,
    TenantDomainOwnershipProofPollingRuntimeReporter runtimeReporter,
    TimeProvider timeProvider,
    ILogger<TenantDomainOwnershipProofPollingHostedService> logger) : BackgroundService
{
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!TenantDomainOwnershipProofPollingConfiguration.IsBackgroundPollingEnabled(options))
        {
            return;
        }

        MultiTenancyGovernanceLoggerMessages.DomainOwnershipProofBackgroundPollingStarted(
            logger,
            TenantDomainOwnershipProofPollingConfiguration.ResolveBackgroundPollingIntervalSeconds(options),
            TenantDomainOwnershipProofPollingConfiguration.ResolveBatchLimit(options),
            null);
        await base.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (!TenantDomainOwnershipProofPollingConfiguration.IsBackgroundPollingEnabled(options))
        {
            return;
        }

        await base.StopAsync(cancellationToken).ConfigureAwait(false);
        MultiTenancyGovernanceLoggerMessages.DomainOwnershipProofBackgroundPollingStopped(logger, null);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!TenantDomainOwnershipProofPollingConfiguration.IsBackgroundPollingEnabled(options))
        {
            return;
        }

        if (options.DomainOwnershipProofBackgroundPollingRunOnStartup)
        {
            await PollOnceAsync(stoppingToken).ConfigureAwait(false);
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(
            TenantDomainOwnershipProofPollingConfiguration.ResolveBackgroundPollingIntervalSeconds(options)));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                await PollOnceAsync(stoppingToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task PollOnceAsync(CancellationToken cancellationToken)
    {
        var startedAtUtc = timeProvider.GetUtcNow();
        runtimeReporter.MarkStarted(startedAtUtc);

        try
        {
            var result = await pollingRunner.PollAsync(
                new TenantDomainOwnershipProofPollingRequest(
                    dnsTxtResolverEndpoint: options.DomainOwnershipDnsTxtProofResolverEndpoint,
                    source: string.IsNullOrWhiteSpace(options.DomainOwnershipProofBackgroundPollingSource)
                        ? "background-proof-polling"
                        : options.DomainOwnershipProofBackgroundPollingSource,
                    atUtc: startedAtUtc,
                    maxItems: TenantDomainOwnershipProofPollingConfiguration.ResolveBatchLimit(options),
                    includeHttpFile: options.EnableDomainOwnershipHttpProofCollection,
                    includeDnsTxt: options.EnableDomainOwnershipDnsTxtProofCollection &&
                        options.DomainOwnershipDnsTxtProofResolverEndpoint is not null,
                    includeRejected: true,
                    includeMissingExpectedProof: false,
                    recordPublicationPlan: false,
                    metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        [TenantDomainOwnershipProofPollingMetadataKeys.ProofPollingRunnerOwnership] = "cephalon-managed",
                        [TenantDomainOwnershipProofPollingMetadataKeys.ExternalProofPollingOwnership] = "cephalon-managed",
                        [TenantDomainOwnershipProofPollingMetadataKeys.BackgroundProofPollingOwnership] = "cephalon-managed",
                        ["backgroundProofPolling"] = "true"
                    }),
                cancellationToken).ConfigureAwait(false);

            runtimeReporter.MarkCompleted(result, timeProvider.GetUtcNow());
            MultiTenancyGovernanceLoggerMessages.DomainOwnershipProofBackgroundPollingCompleted(
                logger,
                result.Outcome,
                result.VerificationCount,
                result.VerifiedCount,
                result.RejectedCount,
                result.FailedCount,
                null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            runtimeReporter.MarkFailed(exception, timeProvider.GetUtcNow());
            MultiTenancyGovernanceLoggerMessages.DomainOwnershipProofBackgroundPollingFailed(logger, exception.Message, exception);
        }
    }
}
