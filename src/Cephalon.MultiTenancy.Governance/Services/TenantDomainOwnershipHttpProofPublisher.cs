using Cephalon.MultiTenancy.Governance.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace Cephalon.MultiTenancy.Governance.Services;

internal sealed class TenantDomainOwnershipHttpProofPublisher(
    MultiTenancyGovernanceOptions options,
    ITenantDomainOwnershipProofPublicationPlanner publicationPlanner,
    ITenantDomainOwnershipStore domainOwnershipStore,
    TimeProvider timeProvider,
    ILogger<TenantDomainOwnershipHttpProofPublisher> logger) : ITenantDomainOwnershipHttpProofPublisher
{
    public async ValueTask<TenantDomainOwnershipHttpProofPublicationResult> PublishAsync(
        TenantDomainOwnershipHttpProofPublicationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var publishedAtUtc = request.AtUtc ?? timeProvider.GetUtcNow();
        if (!options.EnableDomainOwnershipHttpProofPublication ||
            !options.EnableDomainOwnershipProofPublicationPlanning)
        {
            return Denied(
                request,
                TenantDomainOwnershipHttpProofPublicationOutcomes.Disabled,
                publishedAtUtc,
                httpFilePath: null,
                httpFileContent: null,
                httpContentType: null,
                proofFingerprint: null,
                domainOwnership: null,
                reason: "Tenant-domain ownership HTTP proof publication is disabled.",
                metadata: BuildDeniedMetadata(request, null, TenantDomainOwnershipHttpProofPublicationOutcomes.Disabled, publishedAtUtc));
        }

        var plan = await publicationPlanner.PlanAsync(
            new TenantDomainOwnershipProofPublicationPlanRequest(
                request.TenantId,
                request.DomainName,
                verificationMethod: TenantDomainVerificationMethods.HttpFile,
                source: request.Source,
                actor: request.Actor,
                atUtc: publishedAtUtc,
                correlationId: request.CorrelationId,
                recordPlan: request.RecordPublication,
                metadata: request.Metadata),
            cancellationToken).ConfigureAwait(false);

        if (!plan.Planned)
        {
            return Denied(
                request,
                TenantDomainOwnershipHttpProofPublicationOutcomes.PublicationPlanUnavailable,
                publishedAtUtc,
                plan.HttpFilePath,
                plan.HttpFileContent,
                plan.HttpContentType,
                plan.ProofFingerprint,
                plan.DomainOwnership,
                plan.Reason,
                BuildDeniedMetadata(request, plan.Metadata, TenantDomainOwnershipHttpProofPublicationOutcomes.PublicationPlanUnavailable, publishedAtUtc));
        }

        if (string.IsNullOrWhiteSpace(plan.HttpFilePath) ||
            string.IsNullOrWhiteSpace(plan.HttpFileContent) ||
            string.IsNullOrWhiteSpace(plan.HttpContentType) ||
            string.IsNullOrWhiteSpace(plan.ProofFingerprint) ||
            plan.DomainOwnership is null)
        {
            return Denied(
                request,
                TenantDomainOwnershipHttpProofPublicationOutcomes.MissingHttpFilePublicationPlan,
                publishedAtUtc,
                plan.HttpFilePath,
                plan.HttpFileContent,
                plan.HttpContentType,
                plan.ProofFingerprint,
                plan.DomainOwnership,
                "Tenant-domain ownership proof publication planning did not produce HTTP file instructions.",
                BuildDeniedMetadata(request, plan.Metadata, TenantDomainOwnershipHttpProofPublicationOutcomes.MissingHttpFilePublicationPlan, publishedAtUtc));
        }

        var metadata = BuildPublishedMetadata(request, plan, publishedAtUtc);
        var publishedDomainOwnership = new TenantDomainOwnershipDescriptor(
            tenantId: plan.DomainOwnership.TenantId,
            domainName: plan.DomainOwnership.DomainName,
            displayName: plan.DomainOwnership.DisplayName,
            status: plan.DomainOwnership.Status,
            verificationMethod: plan.DomainOwnership.VerificationMethod,
            verifiedAtUtc: plan.DomainOwnership.VerifiedAtUtc,
            expiresAtUtc: plan.DomainOwnership.ExpiresAtUtc,
            sourceModuleId: plan.DomainOwnership.SourceModuleId,
            metadata: metadata);

        if (request.RecordPublication)
        {
            try
            {
                domainOwnershipStore.Upsert(publishedDomainOwnership);
                MultiTenancyGovernanceLoggerMessages.DomainOwnershipStorePersisted(
                    logger,
                    publishedDomainOwnership.TenantId,
                    publishedDomainOwnership.DomainName,
                    domainOwnershipStore.StoreKind,
                    domainOwnershipStore.IsDurable.ToString().ToLowerInvariant(),
                    null);
            }
            catch (Exception exception)
            {
                var reason = "Tenant-domain ownership HTTP proof publication could not persist domain ownership state.";
                MultiTenancyGovernanceLoggerMessages.DomainOwnershipStorePersistenceFailed(
                    logger,
                    request.TenantId,
                    request.DomainName,
                    domainOwnershipStore.StoreKind,
                    reason,
                    exception);

                return Denied(
                    request,
                    TenantDomainOwnershipHttpProofPublicationOutcomes.StoreFailed,
                    publishedAtUtc,
                    plan.HttpFilePath,
                    plan.HttpFileContent,
                    plan.HttpContentType,
                    plan.ProofFingerprint,
                    plan.DomainOwnership,
                    reason,
                    BuildDeniedMetadata(request, metadata, TenantDomainOwnershipHttpProofPublicationOutcomes.StoreFailed, publishedAtUtc));
            }
        }

        var result = new TenantDomainOwnershipHttpProofPublicationResult(
            request.TenantId,
            request.DomainName,
            TenantDomainOwnershipHttpProofPublicationOutcomes.Published,
            published: true,
            recorded: request.RecordPublication,
            publishedAtUtc,
            plan.HttpFilePath,
            plan.HttpFileContent,
            plan.HttpContentType,
            plan.ProofFingerprint,
            request.RecordPublication ? publishedDomainOwnership : plan.DomainOwnership,
            request.RecordPublication
                ? "Tenant-domain ownership HTTP proof file was materialized and recorded."
                : "Tenant-domain ownership HTTP proof file was materialized without recording metadata.",
            metadata);

        MultiTenancyGovernanceLoggerMessages.DomainOwnershipHttpProofPublished(
            logger,
            result.TenantId,
            result.DomainName,
            result.HttpFilePath ?? "unknown",
            null);

        return result;
    }

    private TenantDomainOwnershipHttpProofPublicationResult Denied(
        TenantDomainOwnershipHttpProofPublicationRequest request,
        string outcome,
        DateTimeOffset publishedAtUtc,
        string? httpFilePath,
        string? httpFileContent,
        string? httpContentType,
        string? proofFingerprint,
        TenantDomainOwnershipDescriptor? domainOwnership,
        string reason,
        IReadOnlyDictionary<string, string> metadata)
    {
        MultiTenancyGovernanceLoggerMessages.DomainOwnershipHttpProofPublicationDenied(
            logger,
            request.TenantId,
            request.DomainName,
            outcome,
            reason,
            null);

        return new TenantDomainOwnershipHttpProofPublicationResult(
            request.TenantId,
            request.DomainName,
            outcome,
            published: false,
            recorded: false,
            publishedAtUtc,
            httpFilePath,
            httpFileContent,
            httpContentType,
            proofFingerprint,
            domainOwnership,
            reason,
            metadata);
    }

    private static Dictionary<string, string> BuildPublishedMetadata(
        TenantDomainOwnershipHttpProofPublicationRequest request,
        TenantDomainOwnershipProofPublicationPlanResult plan,
        DateTimeOffset publishedAtUtc)
    {
        var metadata = CopyMetadata(plan.Metadata);
        foreach (var pair in request.Metadata)
        {
            metadata[pair.Key] = pair.Value;
        }

        metadata[TenantDomainOwnershipHttpProofPublicationMetadataKeys.LastHttpProofPublicationOutcome] = TenantDomainOwnershipHttpProofPublicationOutcomes.Published;
        metadata[TenantDomainOwnershipHttpProofPublicationMetadataKeys.LastHttpProofPublishedAtUtc] = publishedAtUtc.ToString("O", CultureInfo.InvariantCulture);
        metadata[TenantDomainOwnershipHttpProofPublicationMetadataKeys.LastHttpProofPublicationSource] = request.Source ?? "application";
        metadata[TenantDomainOwnershipHttpProofPublicationMetadataKeys.HttpProofPublicationOwnership] = "cephalon-managed";
        metadata[TenantDomainOwnershipHttpProofPublicationMetadataKeys.HttpProofPublicationPath] = plan.HttpFilePath!;
        metadata[TenantDomainOwnershipHttpProofPublicationMetadataKeys.HttpProofPublicationContentType] = plan.HttpContentType!;
        metadata[TenantDomainOwnershipHttpProofPublicationMetadataKeys.HttpProofPublicationContentFingerprint] = plan.ProofFingerprint!;
        metadata[TenantDomainOwnershipHttpProofPublicationMetadataKeys.HttpProofPublicationServedBy] = "cephalon-multitenancy-governance";
        metadata[TenantDomainOwnershipProofMetadataKeys.ExpectedProof] = plan.HttpFileContent!;
        metadata[TenantDomainOwnershipProofMetadataKeys.ExpectedHttpFileProof] = plan.HttpFileContent!;

        if (!string.IsNullOrWhiteSpace(request.Actor))
        {
            metadata[TenantDomainOwnershipHttpProofPublicationMetadataKeys.LastHttpProofPublicationActor] = request.Actor;
        }

        if (!string.IsNullOrWhiteSpace(request.CorrelationId))
        {
            metadata[TenantDomainOwnershipHttpProofPublicationMetadataKeys.LastHttpProofPublicationCorrelationId] = request.CorrelationId;
        }

        return metadata;
    }

    private static Dictionary<string, string> BuildDeniedMetadata(
        TenantDomainOwnershipHttpProofPublicationRequest request,
        IReadOnlyDictionary<string, string>? sourceMetadata,
        string outcome,
        DateTimeOffset publishedAtUtc)
    {
        var metadata = CopyMetadata(sourceMetadata);
        foreach (var pair in request.Metadata)
        {
            metadata[pair.Key] = pair.Value;
        }

        metadata[TenantDomainOwnershipHttpProofPublicationMetadataKeys.LastHttpProofPublicationOutcome] = outcome;
        metadata[TenantDomainOwnershipHttpProofPublicationMetadataKeys.LastHttpProofPublishedAtUtc] = publishedAtUtc.ToString("O", CultureInfo.InvariantCulture);
        metadata[TenantDomainOwnershipHttpProofPublicationMetadataKeys.LastHttpProofPublicationSource] = request.Source ?? "application";
        metadata[TenantDomainOwnershipHttpProofPublicationMetadataKeys.HttpProofPublicationOwnership] = "not-configured";
        return metadata;
    }

    private static Dictionary<string, string> CopyMetadata(IReadOnlyDictionary<string, string>? metadata)
    {
        if (metadata is null)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        return metadata
            .Where(static pair => !string.IsNullOrWhiteSpace(pair.Key))
            .ToDictionary(
                static pair => pair.Key.Trim(),
                static pair => pair.Value,
                StringComparer.OrdinalIgnoreCase);
    }
}
