using Cephalon.MultiTenancy.Governance.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Cephalon.MultiTenancy.Governance.Services;

internal sealed class TenantDomainOwnershipHttpProofCollector(
    MultiTenancyGovernanceOptions options,
    ITenantDomainOwnershipProofPublicationPlanner publicationPlanner,
    ITenantDomainOwnershipProofEvaluator proofEvaluator,
    TimeProvider timeProvider,
    HttpClient httpClient,
    ILogger<TenantDomainOwnershipHttpProofCollector> logger) : ITenantDomainOwnershipHttpProofCollector
{
    public async ValueTask<TenantDomainOwnershipHttpProofCollectionResult> CollectAsync(
        TenantDomainOwnershipHttpProofCollectionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        cancellationToken.ThrowIfCancellationRequested();

        var collectedAtUtc = request.AtUtc ?? timeProvider.GetUtcNow();
        var result = options.EnableDomainOwnershipHttpProofCollection
            ? await CollectCoreAsync(request, collectedAtUtc, cancellationToken).ConfigureAwait(false)
            : CreateResult(
                request,
                TenantDomainOwnershipHttpProofCollectionOutcomes.Disabled,
                collected: false,
                evaluated: false,
                collectedAtUtc,
                collectionUri: null,
                statusCode: null,
                contentLength: null,
                observedProofFingerprint: null,
                publicationPlanResult: null,
                evaluationResult: null,
                domainOwnership: null,
                reason: "Tenant-domain ownership HTTP proof collection is disabled.",
                metadata: BuildResultMetadata(
                    request,
                    TenantDomainOwnershipHttpProofCollectionOutcomes.Disabled,
                    collectedAtUtc,
                    collectionUri: null,
                    statusCode: null,
                    contentLength: null,
                    observedProof: null,
                    publicationPlanResult: null));

        if (result.Collected && result.Evaluated)
        {
            MultiTenancyGovernanceLoggerMessages.DomainOwnershipHttpProofCollected(
                logger,
                result.TenantId,
                result.DomainName,
                result.CollectionUri?.ToString() ?? "unknown",
                null);
        }
        else
        {
            MultiTenancyGovernanceLoggerMessages.DomainOwnershipHttpProofCollectionDenied(
                logger,
                result.TenantId,
                result.DomainName,
                result.Outcome,
                result.Reason,
                null);
        }

        return result;
    }

    private async ValueTask<TenantDomainOwnershipHttpProofCollectionResult> CollectCoreAsync(
        TenantDomainOwnershipHttpProofCollectionRequest request,
        DateTimeOffset collectedAtUtc,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(request.VerificationMethod, TenantDomainVerificationMethods.HttpFile, StringComparison.OrdinalIgnoreCase))
        {
            return CreateResult(
                request,
                TenantDomainOwnershipHttpProofCollectionOutcomes.UnsupportedVerificationMethod,
                collected: false,
                evaluated: false,
                collectedAtUtc,
                collectionUri: null,
                statusCode: null,
                contentLength: null,
                observedProofFingerprint: null,
                publicationPlanResult: null,
                evaluationResult: null,
                domainOwnership: null,
                reason: "Only HTTP file verification can be collected by the built-in HTTP proof collector.",
                metadata: BuildResultMetadata(
                    request,
                    TenantDomainOwnershipHttpProofCollectionOutcomes.UnsupportedVerificationMethod,
                    collectedAtUtc,
                    collectionUri: null,
                    statusCode: null,
                    contentLength: null,
                    observedProof: null,
                    publicationPlanResult: null));
        }

        var publicationPlan = await publicationPlanner.PlanAsync(
            new TenantDomainOwnershipProofPublicationPlanRequest(
                request.TenantId,
                request.DomainName,
                verificationMethod: TenantDomainVerificationMethods.HttpFile,
                source: request.Source ?? "http-proof-collector",
                actor: request.Actor,
                atUtc: collectedAtUtc,
                correlationId: request.CorrelationId,
                recordPlan: request.RecordPublicationPlan,
                metadata: request.Metadata),
            cancellationToken).ConfigureAwait(false);

        if (!publicationPlan.Planned)
        {
            var outcome = MapPublicationPlanOutcome(publicationPlan.Outcome);
            return CreateResult(
                request,
                outcome,
                collected: false,
                evaluated: false,
                collectedAtUtc,
                collectionUri: null,
                statusCode: null,
                contentLength: null,
                observedProofFingerprint: null,
                publicationPlan,
                evaluationResult: null,
                publicationPlan.DomainOwnership,
                $"Tenant-domain ownership HTTP proof collection could not build a publication plan. {publicationPlan.Reason}",
                BuildResultMetadata(
                    request,
                    outcome,
                    collectedAtUtc,
                    collectionUri: null,
                    statusCode: null,
                    contentLength: null,
                    observedProof: null,
                    publicationPlan));
        }

        if (string.IsNullOrWhiteSpace(publicationPlan.HttpFilePath) ||
            string.IsNullOrWhiteSpace(publicationPlan.HttpFileContent))
        {
            return CreateResult(
                request,
                TenantDomainOwnershipHttpProofCollectionOutcomes.MissingPublicationPlan,
                collected: false,
                evaluated: false,
                collectedAtUtc,
                collectionUri: null,
                statusCode: null,
                contentLength: null,
                observedProofFingerprint: null,
                publicationPlan,
                evaluationResult: null,
                publicationPlan.DomainOwnership,
                "Publication planning did not return an HTTP file path and expected proof content.",
                BuildResultMetadata(
                    request,
                    TenantDomainOwnershipHttpProofCollectionOutcomes.MissingPublicationPlan,
                    collectedAtUtc,
                    collectionUri: null,
                    statusCode: null,
                    contentLength: null,
                    observedProof: null,
                    publicationPlan));
        }

        if (!TryCreateCollectionUri(request, publicationPlan, out var collectionUri, out var invalidUriReason))
        {
            return CreateResult(
                request,
                TenantDomainOwnershipHttpProofCollectionOutcomes.InvalidUri,
                collected: false,
                evaluated: false,
                collectedAtUtc,
                collectionUri: null,
                statusCode: null,
                contentLength: null,
                observedProofFingerprint: null,
                publicationPlan,
                evaluationResult: null,
                publicationPlan.DomainOwnership,
                invalidUriReason,
                BuildResultMetadata(
                    request,
                    TenantDomainOwnershipHttpProofCollectionOutcomes.InvalidUri,
                    collectedAtUtc,
                    collectionUri: null,
                    statusCode: null,
                    contentLength: null,
                    observedProof: null,
                    publicationPlan));
        }

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(ResolveTimeout(request));

        HttpResponseMessage response;
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Get, collectionUri);
            response = await httpClient.SendAsync(
                httpRequest,
                HttpCompletionOption.ResponseHeadersRead,
                timeoutSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return CreateResult(
                request,
                TenantDomainOwnershipHttpProofCollectionOutcomes.RequestFailed,
                collected: false,
                evaluated: false,
                collectedAtUtc,
                collectionUri,
                statusCode: null,
                contentLength: null,
                observedProofFingerprint: null,
                publicationPlan,
                evaluationResult: null,
                publicationPlan.DomainOwnership,
                "Tenant-domain ownership HTTP proof collection timed out.",
                BuildResultMetadata(
                    request,
                    TenantDomainOwnershipHttpProofCollectionOutcomes.RequestFailed,
                    collectedAtUtc,
                    collectionUri,
                    statusCode: null,
                    contentLength: null,
                    observedProof: null,
                    publicationPlan));
        }
        catch (HttpRequestException exception)
        {
            return CreateRequestFailedResult(request, collectedAtUtc, collectionUri, publicationPlan, exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return CreateRequestFailedResult(request, collectedAtUtc, collectionUri, publicationPlan, exception.Message);
        }

        using (response)
        {
            var statusCode = (int)response.StatusCode;
            if (response.StatusCode != HttpStatusCode.OK)
            {
                return CreateResult(
                    request,
                    TenantDomainOwnershipHttpProofCollectionOutcomes.UnexpectedStatusCode,
                    collected: false,
                    evaluated: false,
                    collectedAtUtc,
                    collectionUri,
                    statusCode,
                    contentLength: response.Content.Headers.ContentLength,
                    observedProofFingerprint: null,
                    publicationPlan,
                    evaluationResult: null,
                    publicationPlan.DomainOwnership,
                    $"Tenant-domain ownership HTTP proof endpoint returned status code {statusCode.ToString(CultureInfo.InvariantCulture)}.",
                    BuildResultMetadata(
                        request,
                        TenantDomainOwnershipHttpProofCollectionOutcomes.UnexpectedStatusCode,
                        collectedAtUtc,
                        collectionUri,
                        statusCode,
                        response.Content.Headers.ContentLength,
                        observedProof: null,
                        publicationPlan));
            }

            HttpProofBodyReadResult body;
            try
            {
                body = await ReadBodyAsync(
                    response.Content,
                    ResolveMaxResponseBytes(),
                    timeoutSource.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return CreateResult(
                    request,
                    TenantDomainOwnershipHttpProofCollectionOutcomes.RequestFailed,
                    collected: false,
                    evaluated: false,
                    collectedAtUtc,
                    collectionUri,
                    statusCode,
                    response.Content.Headers.ContentLength,
                    observedProofFingerprint: null,
                    publicationPlan,
                    evaluationResult: null,
                    publicationPlan.DomainOwnership,
                    "Tenant-domain ownership HTTP proof collection timed out while reading the response body.",
                    BuildResultMetadata(
                        request,
                        TenantDomainOwnershipHttpProofCollectionOutcomes.RequestFailed,
                        collectedAtUtc,
                        collectionUri,
                        statusCode,
                        response.Content.Headers.ContentLength,
                        observedProof: null,
                        publicationPlan));
            }

            if (body.TooLarge)
            {
                return CreateResult(
                    request,
                    TenantDomainOwnershipHttpProofCollectionOutcomes.ResponseTooLarge,
                    collected: false,
                    evaluated: false,
                    collectedAtUtc,
                    collectionUri,
                    statusCode,
                    body.Length,
                    observedProofFingerprint: null,
                    publicationPlan,
                    evaluationResult: null,
                    publicationPlan.DomainOwnership,
                    "Tenant-domain ownership HTTP proof endpoint response exceeded the configured size limit.",
                    BuildResultMetadata(
                        request,
                        TenantDomainOwnershipHttpProofCollectionOutcomes.ResponseTooLarge,
                        collectedAtUtc,
                        collectionUri,
                        statusCode,
                        body.Length,
                        observedProof: null,
                        publicationPlan));
            }

            if (string.IsNullOrWhiteSpace(body.Value))
            {
                return CreateResult(
                    request,
                    TenantDomainOwnershipHttpProofCollectionOutcomes.EmptyResponse,
                    collected: false,
                    evaluated: false,
                    collectedAtUtc,
                    collectionUri,
                    statusCode,
                    body.Length,
                    observedProofFingerprint: null,
                    publicationPlan,
                    evaluationResult: null,
                    publicationPlan.DomainOwnership,
                    "Tenant-domain ownership HTTP proof endpoint returned an empty response body.",
                    BuildResultMetadata(
                        request,
                        TenantDomainOwnershipHttpProofCollectionOutcomes.EmptyResponse,
                        collectedAtUtc,
                        collectionUri,
                        statusCode,
                        body.Length,
                        observedProof: null,
                        publicationPlan));
            }

            var observedProof = body.Value.Trim();
            var metadata = BuildResultMetadata(
                request,
                TenantDomainOwnershipHttpProofCollectionOutcomes.Collected,
                collectedAtUtc,
                collectionUri,
                statusCode,
                body.Length,
                observedProof,
                publicationPlan);
            var evaluation = await proofEvaluator.EvaluateAsync(
                new TenantDomainOwnershipProofEvaluationRequest(
                    request.TenantId,
                    request.DomainName,
                    observedProof,
                    verificationMethod: TenantDomainVerificationMethods.HttpFile,
                    expectedProof: publicationPlan.HttpFileContent,
                    source: request.Source ?? "http-proof-collector",
                    actor: request.Actor,
                    atUtc: collectedAtUtc,
                    expiresAtUtc: request.ExpiresAtUtc,
                    correlationId: request.CorrelationId,
                    metadata: metadata),
                cancellationToken).ConfigureAwait(false);
            var evaluated = evaluation.Applied &&
                (string.Equals(evaluation.Outcome, TenantDomainOwnershipProofEvaluationOutcomes.Verified, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(evaluation.Outcome, TenantDomainOwnershipProofEvaluationOutcomes.Rejected, StringComparison.OrdinalIgnoreCase));
            var outcome = evaluated
                ? TenantDomainOwnershipHttpProofCollectionOutcomes.Collected
                : TenantDomainOwnershipHttpProofCollectionOutcomes.EvaluationFailed;

            return CreateResult(
                request,
                outcome,
                collected: true,
                evaluated,
                collectedAtUtc,
                collectionUri,
                statusCode,
                body.Length,
                ComputeFingerprint(observedProof),
                publicationPlan,
                evaluation,
                evaluation.DomainOwnership ?? publicationPlan.DomainOwnership,
                evaluated
                    ? "Tenant-domain ownership HTTP proof was collected and evaluated."
                    : $"Tenant-domain ownership HTTP proof was collected, but evaluation did not apply a terminal workflow outcome. {evaluation.Reason}",
                BuildResultMetadata(
                    request,
                    outcome,
                    collectedAtUtc,
                    collectionUri,
                    statusCode,
                    body.Length,
                    observedProof,
                    publicationPlan));
        }
    }

    private static TenantDomainOwnershipHttpProofCollectionResult CreateRequestFailedResult(
        TenantDomainOwnershipHttpProofCollectionRequest request,
        DateTimeOffset collectedAtUtc,
        Uri collectionUri,
        TenantDomainOwnershipProofPublicationPlanResult publicationPlan,
        string reason)
    {
        return CreateResult(
            request,
            TenantDomainOwnershipHttpProofCollectionOutcomes.RequestFailed,
            collected: false,
            evaluated: false,
            collectedAtUtc,
            collectionUri,
            statusCode: null,
            contentLength: null,
            observedProofFingerprint: null,
            publicationPlan,
            evaluationResult: null,
            publicationPlan.DomainOwnership,
            $"Tenant-domain ownership HTTP proof endpoint could not be reached. {reason}",
            BuildResultMetadata(
                request,
                TenantDomainOwnershipHttpProofCollectionOutcomes.RequestFailed,
                collectedAtUtc,
                collectionUri,
                statusCode: null,
                contentLength: null,
                observedProof: null,
                publicationPlan));
    }

    private bool TryCreateCollectionUri(
        TenantDomainOwnershipHttpProofCollectionRequest request,
        TenantDomainOwnershipProofPublicationPlanResult publicationPlan,
        out Uri collectionUri,
        out string reason)
    {
        collectionUri = null!;
        reason = string.Empty;

        Uri baseUri;
        try
        {
            baseUri = request.CollectionBaseUri ?? new Uri($"https://{publicationPlan.DomainName}", UriKind.Absolute);
        }
        catch (UriFormatException)
        {
            reason = "Tenant-domain ownership HTTP proof collection base URI could not be built from the requested domain.";
            return false;
        }

        if (!baseUri.IsAbsoluteUri)
        {
            reason = "Tenant-domain ownership HTTP proof collection base URI must be absolute.";
            return false;
        }

        if (!string.Equals(baseUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) &&
            !(options.AllowInsecureDomainOwnershipHttpProofCollection &&
              string.Equals(baseUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)))
        {
            reason = "Tenant-domain ownership HTTP proof collection requires HTTPS unless insecure HTTP collection is explicitly enabled.";
            return false;
        }

        var baseHost = TenantDomainOwnershipDescriptor.NormalizeDomainName(baseUri.Host);
        if (!string.Equals(baseHost, publicationPlan.DomainName, StringComparison.OrdinalIgnoreCase))
        {
            reason = "Tenant-domain ownership HTTP proof collection base URI host must match the declared domain.";
            return false;
        }

        var builder = new UriBuilder(baseUri.Scheme, baseUri.Host, baseUri.IsDefaultPort ? -1 : baseUri.Port)
        {
            Path = publicationPlan.HttpFilePath!.TrimStart('/'),
            Query = string.Empty,
            Fragment = string.Empty
        };
        collectionUri = builder.Uri;
        return true;
    }

    private TimeSpan ResolveTimeout(TenantDomainOwnershipHttpProofCollectionRequest request)
    {
        if (request.Timeout is { } timeout && timeout > TimeSpan.Zero)
        {
            return timeout;
        }

        var seconds = options.DomainOwnershipHttpProofCollectionTimeoutSeconds <= 0
            ? 10
            : options.DomainOwnershipHttpProofCollectionTimeoutSeconds;
        return TimeSpan.FromSeconds(seconds);
    }

    private int ResolveMaxResponseBytes()
    {
        return options.DomainOwnershipHttpProofCollectionMaxResponseBytes <= 0
            ? 4096
            : options.DomainOwnershipHttpProofCollectionMaxResponseBytes;
    }

    private static async ValueTask<HttpProofBodyReadResult> ReadBodyAsync(
        HttpContent content,
        int maxResponseBytes,
        CancellationToken cancellationToken)
    {
        if (content.Headers.ContentLength is { } contentLength &&
            contentLength > maxResponseBytes)
        {
            return new HttpProofBodyReadResult(null, true, contentLength);
        }

        await using var stream = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var buffer = new MemoryStream();
        var readBuffer = new byte[Math.Min(8192, Math.Max(1, maxResponseBytes))];
        int read;
        while ((read = await stream.ReadAsync(readBuffer, cancellationToken).ConfigureAwait(false)) > 0)
        {
            if (buffer.Length + read > maxResponseBytes)
            {
                return new HttpProofBodyReadResult(null, true, buffer.Length + read);
            }

            buffer.Write(readBuffer, 0, read);
        }

        var value = Encoding.UTF8.GetString(buffer.ToArray());
        return new HttpProofBodyReadResult(value, false, buffer.Length);
    }

    private static string MapPublicationPlanOutcome(string outcome)
    {
        return outcome switch
        {
            TenantDomainOwnershipProofPublicationPlanOutcomes.Disabled => TenantDomainOwnershipHttpProofCollectionOutcomes.Disabled,
            TenantDomainOwnershipProofPublicationPlanOutcomes.NotFound => TenantDomainOwnershipHttpProofCollectionOutcomes.NotFound,
            TenantDomainOwnershipProofPublicationPlanOutcomes.TenantMismatch => TenantDomainOwnershipHttpProofCollectionOutcomes.TenantMismatch,
            TenantDomainOwnershipProofPublicationPlanOutcomes.VerificationMethodMismatch => TenantDomainOwnershipHttpProofCollectionOutcomes.VerificationMethodMismatch,
            TenantDomainOwnershipProofPublicationPlanOutcomes.MissingExpectedProof => TenantDomainOwnershipHttpProofCollectionOutcomes.MissingExpectedProof,
            TenantDomainOwnershipProofPublicationPlanOutcomes.UnsupportedVerificationMethod => TenantDomainOwnershipHttpProofCollectionOutcomes.UnsupportedVerificationMethod,
            TenantDomainOwnershipProofPublicationPlanOutcomes.StoreFailed => TenantDomainOwnershipHttpProofCollectionOutcomes.StoreFailed,
            _ => TenantDomainOwnershipHttpProofCollectionOutcomes.MissingPublicationPlan
        };
    }

    private static Dictionary<string, string> BuildResultMetadata(
        TenantDomainOwnershipHttpProofCollectionRequest request,
        string outcome,
        DateTimeOffset collectedAtUtc,
        Uri? collectionUri,
        int? statusCode,
        long? contentLength,
        string? observedProof,
        TenantDomainOwnershipProofPublicationPlanResult? publicationPlanResult)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in request.Metadata)
        {
            metadata[pair.Key] = pair.Value;
        }

        metadata[TenantDomainOwnershipHttpProofCollectionMetadataKeys.LastHttpProofCollectionOutcome] = outcome;
        metadata[TenantDomainOwnershipHttpProofCollectionMetadataKeys.LastHttpProofCollectedAtUtc] = collectedAtUtc.ToString("O", CultureInfo.InvariantCulture);
        metadata[TenantDomainOwnershipHttpProofCollectionMetadataKeys.LastHttpProofCollectionSource] = request.Source ?? "http-proof-collector";
        metadata[TenantDomainOwnershipHttpProofCollectionMetadataKeys.HttpProofCollectionOwnership] = "cephalon-managed";
        metadata[TenantDomainOwnershipHttpProofCollectionMetadataKeys.DnsTxtProofCollectionOwnership] = "application-managed";
        metadata[TenantDomainOwnershipHttpProofCollectionMetadataKeys.ExternalProofPollingOwnership] = "application-managed";

        if (!string.IsNullOrWhiteSpace(request.Actor))
        {
            metadata[TenantDomainOwnershipHttpProofCollectionMetadataKeys.LastHttpProofCollectionActor] = request.Actor;
        }

        if (!string.IsNullOrWhiteSpace(request.CorrelationId))
        {
            metadata[TenantDomainOwnershipHttpProofCollectionMetadataKeys.LastHttpProofCollectionCorrelationId] = request.CorrelationId;
        }

        if (collectionUri is not null)
        {
            metadata[TenantDomainOwnershipHttpProofCollectionMetadataKeys.LastHttpProofCollectionUri] = collectionUri.ToString();
        }

        if (statusCode is not null)
        {
            metadata[TenantDomainOwnershipHttpProofCollectionMetadataKeys.LastHttpProofCollectionStatusCode] = statusCode.Value.ToString(CultureInfo.InvariantCulture);
        }

        if (contentLength is not null)
        {
            metadata[TenantDomainOwnershipHttpProofCollectionMetadataKeys.LastHttpProofCollectionContentLength] = contentLength.Value.ToString(CultureInfo.InvariantCulture);
        }

        if (!string.IsNullOrWhiteSpace(observedProof))
        {
            metadata[TenantDomainOwnershipHttpProofCollectionMetadataKeys.LastHttpProofCollectionObservedFingerprint] = ComputeFingerprint(observedProof);
        }

        if (publicationPlanResult is not null)
        {
            metadata[TenantDomainOwnershipHttpProofCollectionMetadataKeys.LastHttpProofCollectionPublicationPlanOutcome] = publicationPlanResult.Outcome;
        }

        return metadata;
    }

    private static string ComputeFingerprint(string proof)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(proof.Trim()))).ToLowerInvariant();
    }

    private static TenantDomainOwnershipHttpProofCollectionResult CreateResult(
        TenantDomainOwnershipHttpProofCollectionRequest request,
        string outcome,
        bool collected,
        bool evaluated,
        DateTimeOffset collectedAtUtc,
        Uri? collectionUri,
        int? statusCode,
        long? contentLength,
        string? observedProofFingerprint,
        TenantDomainOwnershipProofPublicationPlanResult? publicationPlanResult,
        TenantDomainOwnershipProofEvaluationResult? evaluationResult,
        TenantDomainOwnershipDescriptor? domainOwnership,
        string reason,
        IReadOnlyDictionary<string, string>? metadata)
    {
        return new TenantDomainOwnershipHttpProofCollectionResult(
            request.TenantId,
            request.DomainName,
            request.VerificationMethod,
            outcome,
            collected,
            evaluated,
            collectedAtUtc,
            collectionUri,
            statusCode,
            contentLength,
            observedProofFingerprint,
            publicationPlanResult,
            evaluationResult,
            domainOwnership,
            reason,
            metadata);
    }

    private readonly record struct HttpProofBodyReadResult(string? Value, bool TooLarge, long Length);
}
