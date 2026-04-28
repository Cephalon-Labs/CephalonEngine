using Cephalon.MultiTenancy.Governance.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Cephalon.MultiTenancy.Governance.Services;

internal sealed class TenantDomainOwnershipDnsTxtProofCollector(
    MultiTenancyGovernanceOptions options,
    ITenantDomainOwnershipProofPublicationPlanner publicationPlanner,
    ITenantDomainOwnershipProofEvaluator proofEvaluator,
    TimeProvider timeProvider,
    HttpClient httpClient,
    ILogger<TenantDomainOwnershipDnsTxtProofCollector> logger) : ITenantDomainOwnershipDnsTxtProofCollector
{
    public async ValueTask<TenantDomainOwnershipDnsTxtProofCollectionResult> CollectAsync(
        TenantDomainOwnershipDnsTxtProofCollectionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        cancellationToken.ThrowIfCancellationRequested();

        var collectedAtUtc = request.AtUtc ?? timeProvider.GetUtcNow();
        var result = options.EnableDomainOwnershipDnsTxtProofCollection
            ? await CollectCoreAsync(request, collectedAtUtc, cancellationToken).ConfigureAwait(false)
            : CreateResult(
                request,
                TenantDomainOwnershipDnsTxtProofCollectionOutcomes.Disabled,
                collected: false,
                evaluated: false,
                collectedAtUtc,
                resolverUri: null,
                dnsTxtRecordName: null,
                statusCode: null,
                contentLength: null,
                observedTxtRecordCount: 0,
                observedProofFingerprint: null,
                publicationPlanResult: null,
                evaluationResult: null,
                domainOwnership: null,
                reason: "Tenant-domain ownership DNS TXT proof collection is disabled.",
                metadata: BuildResultMetadata(
                    request,
                    TenantDomainOwnershipDnsTxtProofCollectionOutcomes.Disabled,
                    collectedAtUtc,
                    resolverUri: null,
                    dnsTxtRecordName: null,
                    statusCode: null,
                    contentLength: null,
                    observedTxtRecordCount: 0,
                    observedProof: null,
                    publicationPlanResult: null,
                    dnsTxtProofCollectionOwnership: "not-configured"));

        if (result.Collected && result.Evaluated)
        {
            MultiTenancyGovernanceLoggerMessages.DomainOwnershipDnsTxtProofCollected(
                logger,
                result.TenantId,
                result.DomainName,
                result.DnsTxtRecordName ?? "unknown",
                null);
        }
        else
        {
            MultiTenancyGovernanceLoggerMessages.DomainOwnershipDnsTxtProofCollectionDenied(
                logger,
                result.TenantId,
                result.DomainName,
                result.Outcome,
                result.Reason,
                null);
        }

        return result;
    }

    private async ValueTask<TenantDomainOwnershipDnsTxtProofCollectionResult> CollectCoreAsync(
        TenantDomainOwnershipDnsTxtProofCollectionRequest request,
        DateTimeOffset collectedAtUtc,
        CancellationToken cancellationToken)
    {
        var collectionOwnership = ResolveCollectionOwnership(request);
        if (!string.Equals(request.VerificationMethod, TenantDomainVerificationMethods.DnsTxt, StringComparison.OrdinalIgnoreCase))
        {
            return CreateResult(
                request,
                TenantDomainOwnershipDnsTxtProofCollectionOutcomes.UnsupportedVerificationMethod,
                collected: false,
                evaluated: false,
                collectedAtUtc,
                resolverUri: null,
                dnsTxtRecordName: null,
                statusCode: null,
                contentLength: null,
                observedTxtRecordCount: 0,
                observedProofFingerprint: null,
                publicationPlanResult: null,
                evaluationResult: null,
                domainOwnership: null,
                reason: "Only DNS TXT verification can be collected by the built-in DNS TXT proof collector.",
                metadata: BuildResultMetadata(
                    request,
                    TenantDomainOwnershipDnsTxtProofCollectionOutcomes.UnsupportedVerificationMethod,
                    collectedAtUtc,
                    resolverUri: null,
                    dnsTxtRecordName: null,
                    statusCode: null,
                    contentLength: null,
                    observedTxtRecordCount: 0,
                    observedProof: null,
                    publicationPlanResult: null,
                    collectionOwnership));
        }

        var publicationPlan = await publicationPlanner.PlanAsync(
            new TenantDomainOwnershipProofPublicationPlanRequest(
                request.TenantId,
                request.DomainName,
                verificationMethod: TenantDomainVerificationMethods.DnsTxt,
                source: request.Source ?? "dns-txt-proof-collector",
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
                resolverUri: null,
                dnsTxtRecordName: null,
                statusCode: null,
                contentLength: null,
                observedTxtRecordCount: 0,
                observedProofFingerprint: null,
                publicationPlan,
                evaluationResult: null,
                publicationPlan.DomainOwnership,
                $"Tenant-domain ownership DNS TXT proof collection could not build a publication plan. {publicationPlan.Reason}",
                BuildResultMetadata(
                    request,
                    outcome,
                    collectedAtUtc,
                    resolverUri: null,
                    dnsTxtRecordName: null,
                    statusCode: null,
                    contentLength: null,
                    observedTxtRecordCount: 0,
                    observedProof: null,
                    publicationPlan,
                    collectionOwnership));
        }

        if (string.IsNullOrWhiteSpace(publicationPlan.DnsTxtRecordName) ||
            string.IsNullOrWhiteSpace(publicationPlan.DnsTxtRecordValue))
        {
            return CreateResult(
                request,
                TenantDomainOwnershipDnsTxtProofCollectionOutcomes.MissingPublicationPlan,
                collected: false,
                evaluated: false,
                collectedAtUtc,
                resolverUri: null,
                dnsTxtRecordName: publicationPlan.DnsTxtRecordName,
                statusCode: null,
                contentLength: null,
                observedTxtRecordCount: 0,
                observedProofFingerprint: null,
                publicationPlan,
                evaluationResult: null,
                publicationPlan.DomainOwnership,
                "Publication planning did not return a DNS TXT record name and expected proof value.",
                BuildResultMetadata(
                    request,
                    TenantDomainOwnershipDnsTxtProofCollectionOutcomes.MissingPublicationPlan,
                    collectedAtUtc,
                    resolverUri: null,
                    publicationPlan.DnsTxtRecordName,
                    statusCode: null,
                    contentLength: null,
                    observedTxtRecordCount: 0,
                    observedProof: null,
                    publicationPlan,
                    collectionOwnership));
        }

        var resolverEndpoint = request.ResolverEndpoint ?? options.DomainOwnershipDnsTxtProofResolverEndpoint;
        if (resolverEndpoint is null)
        {
            return CreateResult(
                request,
                TenantDomainOwnershipDnsTxtProofCollectionOutcomes.ResolverNotConfigured,
                collected: false,
                evaluated: false,
                collectedAtUtc,
                resolverUri: null,
                publicationPlan.DnsTxtRecordName,
                statusCode: null,
                contentLength: null,
                observedTxtRecordCount: 0,
                observedProofFingerprint: null,
                publicationPlan,
                evaluationResult: null,
                publicationPlan.DomainOwnership,
                "Tenant-domain ownership DNS TXT proof collection requires an explicit DNS-over-HTTPS resolver endpoint.",
                BuildResultMetadata(
                    request,
                    TenantDomainOwnershipDnsTxtProofCollectionOutcomes.ResolverNotConfigured,
                    collectedAtUtc,
                    resolverUri: null,
                    publicationPlan.DnsTxtRecordName,
                    statusCode: null,
                    contentLength: null,
                    observedTxtRecordCount: 0,
                    observedProof: null,
                    publicationPlan,
                    dnsTxtProofCollectionOwnership: "not-configured"));
        }

        if (!TryCreateResolverUri(resolverEndpoint, publicationPlan.DnsTxtRecordName, out var resolverUri, out var invalidUriReason))
        {
            return CreateResult(
                request,
                TenantDomainOwnershipDnsTxtProofCollectionOutcomes.InvalidResolverUri,
                collected: false,
                evaluated: false,
                collectedAtUtc,
                resolverUri: null,
                publicationPlan.DnsTxtRecordName,
                statusCode: null,
                contentLength: null,
                observedTxtRecordCount: 0,
                observedProofFingerprint: null,
                publicationPlan,
                evaluationResult: null,
                publicationPlan.DomainOwnership,
                invalidUriReason,
                BuildResultMetadata(
                    request,
                    TenantDomainOwnershipDnsTxtProofCollectionOutcomes.InvalidResolverUri,
                    collectedAtUtc,
                    resolverUri: null,
                    publicationPlan.DnsTxtRecordName,
                    statusCode: null,
                    contentLength: null,
                    observedTxtRecordCount: 0,
                    observedProof: null,
                    publicationPlan,
                    collectionOwnership));
        }

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(ResolveTimeout(request));

        HttpResponseMessage response;
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Get, resolverUri);
            httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/dns-json"));
            response = await httpClient.SendAsync(
                httpRequest,
                HttpCompletionOption.ResponseHeadersRead,
                timeoutSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return CreateRequestFailedResult(request, collectedAtUtc, resolverUri, publicationPlan, "Tenant-domain ownership DNS TXT proof collection timed out.");
        }
        catch (HttpRequestException exception)
        {
            return CreateRequestFailedResult(request, collectedAtUtc, resolverUri, publicationPlan, exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return CreateRequestFailedResult(request, collectedAtUtc, resolverUri, publicationPlan, exception.Message);
        }

        using (response)
        {
            var statusCode = (int)response.StatusCode;
            if (response.StatusCode != HttpStatusCode.OK)
            {
                return CreateResult(
                    request,
                    TenantDomainOwnershipDnsTxtProofCollectionOutcomes.UnexpectedStatusCode,
                    collected: false,
                    evaluated: false,
                    collectedAtUtc,
                    resolverUri,
                    publicationPlan.DnsTxtRecordName,
                    statusCode,
                    response.Content.Headers.ContentLength,
                    observedTxtRecordCount: 0,
                    observedProofFingerprint: null,
                    publicationPlan,
                    evaluationResult: null,
                    publicationPlan.DomainOwnership,
                    $"Tenant-domain ownership DNS TXT proof resolver returned status code {statusCode.ToString(CultureInfo.InvariantCulture)}.",
                    BuildResultMetadata(
                        request,
                        TenantDomainOwnershipDnsTxtProofCollectionOutcomes.UnexpectedStatusCode,
                        collectedAtUtc,
                        resolverUri,
                        publicationPlan.DnsTxtRecordName,
                        statusCode,
                        response.Content.Headers.ContentLength,
                        observedTxtRecordCount: 0,
                        observedProof: null,
                        publicationPlan,
                        collectionOwnership));
            }

            DnsTxtBodyReadResult body;
            try
            {
                body = await ReadBodyAsync(
                    response.Content,
                    ResolveMaxResponseBytes(),
                    timeoutSource.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return CreateRequestFailedResult(request, collectedAtUtc, resolverUri, publicationPlan, "Tenant-domain ownership DNS TXT proof collection timed out while reading the response body.");
            }

            if (body.TooLarge)
            {
                return CreateResult(
                    request,
                    TenantDomainOwnershipDnsTxtProofCollectionOutcomes.ResponseTooLarge,
                    collected: false,
                    evaluated: false,
                    collectedAtUtc,
                    resolverUri,
                    publicationPlan.DnsTxtRecordName,
                    statusCode,
                    body.Length,
                    observedTxtRecordCount: 0,
                    observedProofFingerprint: null,
                    publicationPlan,
                    evaluationResult: null,
                    publicationPlan.DomainOwnership,
                    "Tenant-domain ownership DNS TXT proof resolver response exceeded the configured size limit.",
                    BuildResultMetadata(
                        request,
                        TenantDomainOwnershipDnsTxtProofCollectionOutcomes.ResponseTooLarge,
                        collectedAtUtc,
                        resolverUri,
                        publicationPlan.DnsTxtRecordName,
                        statusCode,
                        body.Length,
                        observedTxtRecordCount: 0,
                        observedProof: null,
                        publicationPlan,
                        collectionOwnership));
            }

            if (string.IsNullOrWhiteSpace(body.Value))
            {
                return CreateResult(
                    request,
                    TenantDomainOwnershipDnsTxtProofCollectionOutcomes.EmptyResponse,
                    collected: false,
                    evaluated: false,
                    collectedAtUtc,
                    resolverUri,
                    publicationPlan.DnsTxtRecordName,
                    statusCode,
                    body.Length,
                    observedTxtRecordCount: 0,
                    observedProofFingerprint: null,
                    publicationPlan,
                    evaluationResult: null,
                    publicationPlan.DomainOwnership,
                    "Tenant-domain ownership DNS TXT proof resolver returned an empty response body.",
                    BuildResultMetadata(
                        request,
                        TenantDomainOwnershipDnsTxtProofCollectionOutcomes.EmptyResponse,
                        collectedAtUtc,
                        resolverUri,
                        publicationPlan.DnsTxtRecordName,
                        statusCode,
                        body.Length,
                        observedTxtRecordCount: 0,
                        observedProof: null,
                        publicationPlan,
                        collectionOwnership));
            }

            if (!TryReadTxtAnswers(body.Value, out var observedTxtRecords))
            {
                return CreateResult(
                    request,
                    TenantDomainOwnershipDnsTxtProofCollectionOutcomes.InvalidResponse,
                    collected: false,
                    evaluated: false,
                    collectedAtUtc,
                    resolverUri,
                    publicationPlan.DnsTxtRecordName,
                    statusCode,
                    body.Length,
                    observedTxtRecordCount: 0,
                    observedProofFingerprint: null,
                    publicationPlan,
                    evaluationResult: null,
                    publicationPlan.DomainOwnership,
                    "Tenant-domain ownership DNS TXT proof resolver response could not be parsed as DNS JSON.",
                    BuildResultMetadata(
                        request,
                        TenantDomainOwnershipDnsTxtProofCollectionOutcomes.InvalidResponse,
                        collectedAtUtc,
                        resolverUri,
                        publicationPlan.DnsTxtRecordName,
                        statusCode,
                        body.Length,
                        observedTxtRecordCount: 0,
                        observedProof: null,
                        publicationPlan,
                        collectionOwnership));
            }

            if (observedTxtRecords.Count == 0)
            {
                return CreateResult(
                    request,
                    TenantDomainOwnershipDnsTxtProofCollectionOutcomes.NoTxtRecords,
                    collected: false,
                    evaluated: false,
                    collectedAtUtc,
                    resolverUri,
                    publicationPlan.DnsTxtRecordName,
                    statusCode,
                    body.Length,
                    observedTxtRecordCount: 0,
                    observedProofFingerprint: null,
                    publicationPlan,
                    evaluationResult: null,
                    publicationPlan.DomainOwnership,
                    "Tenant-domain ownership DNS TXT proof resolver returned no TXT answers for the planned proof record.",
                    BuildResultMetadata(
                        request,
                        TenantDomainOwnershipDnsTxtProofCollectionOutcomes.NoTxtRecords,
                        collectedAtUtc,
                        resolverUri,
                        publicationPlan.DnsTxtRecordName,
                        statusCode,
                        body.Length,
                        observedTxtRecordCount: 0,
                        observedProof: null,
                        publicationPlan,
                        collectionOwnership));
            }

            var expectedProof = publicationPlan.DnsTxtRecordValue.Trim();
            var observedProof = observedTxtRecords.FirstOrDefault(record => string.Equals(record, expectedProof, StringComparison.Ordinal));
            if (observedProof is null)
            {
                return CreateResult(
                    request,
                    TenantDomainOwnershipDnsTxtProofCollectionOutcomes.NoMatchingTxtRecord,
                    collected: false,
                    evaluated: false,
                    collectedAtUtc,
                    resolverUri,
                    publicationPlan.DnsTxtRecordName,
                    statusCode,
                    body.Length,
                    observedTxtRecords.Count,
                    observedProofFingerprint: null,
                    publicationPlan,
                    evaluationResult: null,
                    publicationPlan.DomainOwnership,
                    "Tenant-domain ownership DNS TXT proof resolver returned TXT answers, but none matched the expected proof value.",
                    BuildResultMetadata(
                        request,
                        TenantDomainOwnershipDnsTxtProofCollectionOutcomes.NoMatchingTxtRecord,
                        collectedAtUtc,
                        resolverUri,
                        publicationPlan.DnsTxtRecordName,
                        statusCode,
                        body.Length,
                        observedTxtRecords.Count,
                        observedProof: null,
                        publicationPlan,
                        collectionOwnership));
            }

            var metadata = BuildResultMetadata(
                request,
                TenantDomainOwnershipDnsTxtProofCollectionOutcomes.Collected,
                collectedAtUtc,
                resolverUri,
                publicationPlan.DnsTxtRecordName,
                statusCode,
                body.Length,
                observedTxtRecords.Count,
                observedProof,
                publicationPlan,
                collectionOwnership);
            var evaluation = await proofEvaluator.EvaluateAsync(
                new TenantDomainOwnershipProofEvaluationRequest(
                    request.TenantId,
                    request.DomainName,
                    observedProof,
                    verificationMethod: TenantDomainVerificationMethods.DnsTxt,
                    expectedProof: expectedProof,
                    source: request.Source ?? "dns-txt-proof-collector",
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
                ? TenantDomainOwnershipDnsTxtProofCollectionOutcomes.Collected
                : TenantDomainOwnershipDnsTxtProofCollectionOutcomes.EvaluationFailed;

            return CreateResult(
                request,
                outcome,
                collected: true,
                evaluated,
                collectedAtUtc,
                resolverUri,
                publicationPlan.DnsTxtRecordName,
                statusCode,
                body.Length,
                observedTxtRecords.Count,
                ComputeFingerprint(observedProof),
                publicationPlan,
                evaluation,
                evaluation.DomainOwnership ?? publicationPlan.DomainOwnership,
                evaluated
                    ? "Tenant-domain ownership DNS TXT proof was collected and evaluated."
                    : $"Tenant-domain ownership DNS TXT proof was collected, but evaluation did not apply a terminal workflow outcome. {evaluation.Reason}",
                BuildResultMetadata(
                    request,
                    outcome,
                    collectedAtUtc,
                    resolverUri,
                    publicationPlan.DnsTxtRecordName,
                    statusCode,
                    body.Length,
                    observedTxtRecords.Count,
                    observedProof,
                    publicationPlan,
                    collectionOwnership));
        }
    }

    private TenantDomainOwnershipDnsTxtProofCollectionResult CreateRequestFailedResult(
        TenantDomainOwnershipDnsTxtProofCollectionRequest request,
        DateTimeOffset collectedAtUtc,
        Uri resolverUri,
        TenantDomainOwnershipProofPublicationPlanResult publicationPlan,
        string reason)
    {
        return CreateResult(
            request,
            TenantDomainOwnershipDnsTxtProofCollectionOutcomes.RequestFailed,
            collected: false,
            evaluated: false,
            collectedAtUtc,
            resolverUri,
            publicationPlan.DnsTxtRecordName,
            statusCode: null,
            contentLength: null,
            observedTxtRecordCount: 0,
            observedProofFingerprint: null,
            publicationPlan,
            evaluationResult: null,
            publicationPlan.DomainOwnership,
            $"Tenant-domain ownership DNS TXT proof resolver could not be reached. {reason}",
            BuildResultMetadata(
                request,
                TenantDomainOwnershipDnsTxtProofCollectionOutcomes.RequestFailed,
                collectedAtUtc,
                resolverUri,
                publicationPlan.DnsTxtRecordName,
                statusCode: null,
                contentLength: null,
                observedTxtRecordCount: 0,
                observedProof: null,
                publicationPlan,
                ResolveCollectionOwnership(request)));
    }

    private static bool TryCreateResolverUri(
        Uri resolverEndpoint,
        string dnsTxtRecordName,
        out Uri resolverUri,
        out string reason)
    {
        resolverUri = null!;
        reason = string.Empty;

        if (!resolverEndpoint.IsAbsoluteUri)
        {
            reason = "Tenant-domain ownership DNS TXT proof resolver endpoint must be absolute.";
            return false;
        }

        if (!string.Equals(resolverEndpoint.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            reason = "Tenant-domain ownership DNS TXT proof collection requires an HTTPS DNS-over-HTTPS resolver endpoint.";
            return false;
        }

        var builder = new UriBuilder(resolverEndpoint)
        {
            Fragment = string.Empty
        };
        var appendedQuery = AppendQuery(builder.Query, "name", TenantDomainOwnershipDescriptor.NormalizeDomainName(dnsTxtRecordName));
        appendedQuery = AppendQuery(appendedQuery, "type", "TXT");
        builder.Query = appendedQuery;
        resolverUri = builder.Uri;
        return true;
    }

    private static string AppendQuery(string existingQuery, string key, string value)
    {
        var query = existingQuery.TrimStart('?');
        var pair = $"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}";
        return string.IsNullOrEmpty(query)
            ? pair
            : $"{query}&{pair}";
    }

    private TimeSpan ResolveTimeout(TenantDomainOwnershipDnsTxtProofCollectionRequest request)
    {
        if (request.Timeout is { } timeout && timeout > TimeSpan.Zero)
        {
            return timeout;
        }

        var seconds = options.DomainOwnershipDnsTxtProofCollectionTimeoutSeconds <= 0
            ? 10
            : options.DomainOwnershipDnsTxtProofCollectionTimeoutSeconds;
        return TimeSpan.FromSeconds(seconds);
    }

    private int ResolveMaxResponseBytes()
    {
        return options.DomainOwnershipDnsTxtProofCollectionMaxResponseBytes <= 0
            ? 16384
            : options.DomainOwnershipDnsTxtProofCollectionMaxResponseBytes;
    }

    private string ResolveCollectionOwnership(TenantDomainOwnershipDnsTxtProofCollectionRequest request)
    {
        return options.EnableDomainOwnershipDnsTxtProofCollection &&
            (request.ResolverEndpoint is not null || options.DomainOwnershipDnsTxtProofResolverEndpoint is not null)
            ? "cephalon-managed"
            : "not-configured";
    }

    private string ResolveHttpProofCollectionOwnership()
    {
        return options.EnableDomainOwnershipHttpProofCollection &&
            options.EnableDomainOwnershipProofPublicationPlanning &&
            options.EnableDomainOwnershipProofEvaluation &&
            options.EnableDomainOwnershipVerificationWorkflow
            ? "cephalon-managed"
            : "not-configured";
    }

    private static async ValueTask<DnsTxtBodyReadResult> ReadBodyAsync(
        HttpContent content,
        int maxResponseBytes,
        CancellationToken cancellationToken)
    {
        if (content.Headers.ContentLength is { } contentLength &&
            contentLength > maxResponseBytes)
        {
            return new DnsTxtBodyReadResult(null, true, contentLength);
        }

        await using var stream = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var buffer = new MemoryStream();
        var readBuffer = new byte[Math.Min(8192, Math.Max(1, maxResponseBytes))];
        int read;
        while ((read = await stream.ReadAsync(readBuffer, cancellationToken).ConfigureAwait(false)) > 0)
        {
            if (buffer.Length + read > maxResponseBytes)
            {
                return new DnsTxtBodyReadResult(null, true, buffer.Length + read);
            }

            buffer.Write(readBuffer, 0, read);
        }

        var value = Encoding.UTF8.GetString(buffer.ToArray());
        return new DnsTxtBodyReadResult(value, false, buffer.Length);
    }

    private static bool TryReadTxtAnswers(string body, out IReadOnlyList<string> txtRecords)
    {
        var records = new List<string>();
        txtRecords = records;

        try
        {
            using var document = JsonDocument.Parse(body);
            if (!TryGetProperty(document.RootElement, "Answer", out var answers) &&
                !TryGetProperty(document.RootElement, "answers", out answers))
            {
                return true;
            }

            if (answers.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            foreach (var answer in answers.EnumerateArray())
            {
                if (answer.ValueKind != JsonValueKind.Object ||
                    !IsTxtAnswer(answer) ||
                    !TryGetProperty(answer, "data", out var data) ||
                    data.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                var normalized = NormalizeTxtValue(data.GetString());
                if (!string.IsNullOrWhiteSpace(normalized))
                {
                    records.Add(normalized);
                }
            }

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool IsTxtAnswer(JsonElement answer)
    {
        if (!TryGetProperty(answer, "type", out var type))
        {
            return true;
        }

        return type.ValueKind switch
        {
            JsonValueKind.Number => type.TryGetInt32(out var numericType) && numericType == 16,
            JsonValueKind.String => string.Equals(type.GetString(), "TXT", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(type.GetString(), "16", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    private static bool TryGetProperty(JsonElement element, string propertyName, out JsonElement property)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            property = default;
            return false;
        }

        foreach (var current in element.EnumerateObject())
        {
            if (string.Equals(current.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                property = current.Value;
                return true;
            }
        }

        property = default;
        return false;
    }

    private static string NormalizeTxtValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        value = value.Trim();
        if (!value.Contains('"', StringComparison.Ordinal))
        {
            return value;
        }

        var builder = new StringBuilder();
        var insideQuotes = false;
        var escaped = false;
        for (var index = 0; index < value.Length; index++)
        {
            var current = value[index];
            if (!insideQuotes)
            {
                if (current == '"')
                {
                    insideQuotes = true;
                }

                continue;
            }

            if (escaped)
            {
                builder.Append(current);
                escaped = false;
                continue;
            }

            if (current == '\\')
            {
                escaped = true;
                continue;
            }

            if (current == '"')
            {
                insideQuotes = false;
                continue;
            }

            builder.Append(current);
        }

        return builder.Length == 0 ? value.Trim('"') : builder.ToString();
    }

    private static string MapPublicationPlanOutcome(string outcome)
    {
        return outcome switch
        {
            TenantDomainOwnershipProofPublicationPlanOutcomes.Disabled => TenantDomainOwnershipDnsTxtProofCollectionOutcomes.Disabled,
            TenantDomainOwnershipProofPublicationPlanOutcomes.NotFound => TenantDomainOwnershipDnsTxtProofCollectionOutcomes.NotFound,
            TenantDomainOwnershipProofPublicationPlanOutcomes.TenantMismatch => TenantDomainOwnershipDnsTxtProofCollectionOutcomes.TenantMismatch,
            TenantDomainOwnershipProofPublicationPlanOutcomes.VerificationMethodMismatch => TenantDomainOwnershipDnsTxtProofCollectionOutcomes.VerificationMethodMismatch,
            TenantDomainOwnershipProofPublicationPlanOutcomes.MissingExpectedProof => TenantDomainOwnershipDnsTxtProofCollectionOutcomes.MissingExpectedProof,
            TenantDomainOwnershipProofPublicationPlanOutcomes.UnsupportedVerificationMethod => TenantDomainOwnershipDnsTxtProofCollectionOutcomes.UnsupportedVerificationMethod,
            TenantDomainOwnershipProofPublicationPlanOutcomes.StoreFailed => TenantDomainOwnershipDnsTxtProofCollectionOutcomes.StoreFailed,
            _ => TenantDomainOwnershipDnsTxtProofCollectionOutcomes.MissingPublicationPlan
        };
    }

    private Dictionary<string, string> BuildResultMetadata(
        TenantDomainOwnershipDnsTxtProofCollectionRequest request,
        string outcome,
        DateTimeOffset collectedAtUtc,
        Uri? resolverUri,
        string? dnsTxtRecordName,
        int? statusCode,
        long? contentLength,
        int observedTxtRecordCount,
        string? observedProof,
        TenantDomainOwnershipProofPublicationPlanResult? publicationPlanResult,
        string dnsTxtProofCollectionOwnership)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in request.Metadata)
        {
            metadata[pair.Key] = pair.Value;
        }

        metadata[TenantDomainOwnershipDnsTxtProofCollectionMetadataKeys.LastDnsTxtProofCollectionOutcome] = outcome;
        metadata[TenantDomainOwnershipDnsTxtProofCollectionMetadataKeys.LastDnsTxtProofCollectedAtUtc] = collectedAtUtc.ToString("O", CultureInfo.InvariantCulture);
        metadata[TenantDomainOwnershipDnsTxtProofCollectionMetadataKeys.LastDnsTxtProofCollectionSource] = request.Source ?? "dns-txt-proof-collector";
        metadata[TenantDomainOwnershipDnsTxtProofCollectionMetadataKeys.DnsTxtProofCollectionOwnership] = dnsTxtProofCollectionOwnership;
        metadata[TenantDomainOwnershipDnsTxtProofCollectionMetadataKeys.HttpProofCollectionOwnership] = ResolveHttpProofCollectionOwnership();
        metadata[TenantDomainOwnershipDnsTxtProofCollectionMetadataKeys.ExternalProofPollingOwnership] = "application-managed";
        metadata[TenantDomainOwnershipDnsTxtProofCollectionMetadataKeys.LastDnsTxtProofCollectionObservedTxtRecordCount] = observedTxtRecordCount.ToString(CultureInfo.InvariantCulture);

        if (!string.IsNullOrWhiteSpace(request.Actor))
        {
            metadata[TenantDomainOwnershipDnsTxtProofCollectionMetadataKeys.LastDnsTxtProofCollectionActor] = request.Actor;
        }

        if (!string.IsNullOrWhiteSpace(request.CorrelationId))
        {
            metadata[TenantDomainOwnershipDnsTxtProofCollectionMetadataKeys.LastDnsTxtProofCollectionCorrelationId] = request.CorrelationId;
        }

        if (resolverUri is not null)
        {
            metadata[TenantDomainOwnershipDnsTxtProofCollectionMetadataKeys.LastDnsTxtProofCollectionResolverUri] = resolverUri.ToString();
        }

        if (!string.IsNullOrWhiteSpace(dnsTxtRecordName))
        {
            metadata[TenantDomainOwnershipDnsTxtProofCollectionMetadataKeys.LastDnsTxtProofCollectionRecordName] = TenantDomainOwnershipDescriptor.NormalizeDomainName(dnsTxtRecordName);
        }

        if (statusCode is not null)
        {
            metadata[TenantDomainOwnershipDnsTxtProofCollectionMetadataKeys.LastDnsTxtProofCollectionStatusCode] = statusCode.Value.ToString(CultureInfo.InvariantCulture);
        }

        if (contentLength is not null)
        {
            metadata[TenantDomainOwnershipDnsTxtProofCollectionMetadataKeys.LastDnsTxtProofCollectionContentLength] = contentLength.Value.ToString(CultureInfo.InvariantCulture);
        }

        if (!string.IsNullOrWhiteSpace(observedProof))
        {
            metadata[TenantDomainOwnershipDnsTxtProofCollectionMetadataKeys.LastDnsTxtProofCollectionObservedFingerprint] = ComputeFingerprint(observedProof);
        }

        if (publicationPlanResult is not null)
        {
            metadata[TenantDomainOwnershipDnsTxtProofCollectionMetadataKeys.LastDnsTxtProofCollectionPublicationPlanOutcome] = publicationPlanResult.Outcome;
        }

        return metadata;
    }

    private static string ComputeFingerprint(string proof)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(proof.Trim()))).ToLowerInvariant();
    }

    private static TenantDomainOwnershipDnsTxtProofCollectionResult CreateResult(
        TenantDomainOwnershipDnsTxtProofCollectionRequest request,
        string outcome,
        bool collected,
        bool evaluated,
        DateTimeOffset collectedAtUtc,
        Uri? resolverUri,
        string? dnsTxtRecordName,
        int? statusCode,
        long? contentLength,
        int observedTxtRecordCount,
        string? observedProofFingerprint,
        TenantDomainOwnershipProofPublicationPlanResult? publicationPlanResult,
        TenantDomainOwnershipProofEvaluationResult? evaluationResult,
        TenantDomainOwnershipDescriptor? domainOwnership,
        string reason,
        IReadOnlyDictionary<string, string>? metadata)
    {
        return new TenantDomainOwnershipDnsTxtProofCollectionResult(
            request.TenantId,
            request.DomainName,
            request.VerificationMethod,
            outcome,
            collected,
            evaluated,
            collectedAtUtc,
            resolverUri,
            dnsTxtRecordName,
            statusCode,
            contentLength,
            observedTxtRecordCount,
            observedProofFingerprint,
            publicationPlanResult,
            evaluationResult,
            domainOwnership,
            reason,
            metadata);
    }

    private readonly record struct DnsTxtBodyReadResult(string? Value, bool TooLarge, long Length);
}
