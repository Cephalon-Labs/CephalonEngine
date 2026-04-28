namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Stable metadata keys written by tenant-domain ownership DNS TXT proof collection.
/// </summary>
public static class TenantDomainOwnershipDnsTxtProofCollectionMetadataKeys
{
    /// <summary>
    /// Metadata key for the last DNS TXT proof collection outcome.
    /// </summary>
    public const string LastDnsTxtProofCollectionOutcome = "lastDnsTxtProofCollectionOutcome";

    /// <summary>
    /// Metadata key for the source that requested DNS TXT proof collection.
    /// </summary>
    public const string LastDnsTxtProofCollectionSource = "lastDnsTxtProofCollectionSource";

    /// <summary>
    /// Metadata key for the actor that requested DNS TXT proof collection when known.
    /// </summary>
    public const string LastDnsTxtProofCollectionActor = "lastDnsTxtProofCollectionActor";

    /// <summary>
    /// Metadata key for the DNS TXT proof collection correlation identifier.
    /// </summary>
    public const string LastDnsTxtProofCollectionCorrelationId = "lastDnsTxtProofCollectionCorrelationId";

    /// <summary>
    /// Metadata key for the UTC timestamp when DNS TXT proof collection executed.
    /// </summary>
    public const string LastDnsTxtProofCollectedAtUtc = "lastDnsTxtProofCollectedAtUtc";

    /// <summary>
    /// Metadata key for the resolver URI used to collect the DNS TXT proof.
    /// </summary>
    public const string LastDnsTxtProofCollectionResolverUri = "lastDnsTxtProofCollectionResolverUri";

    /// <summary>
    /// Metadata key for the DNS TXT record name queried during collection.
    /// </summary>
    public const string LastDnsTxtProofCollectionRecordName = "lastDnsTxtProofCollectionRecordName";

    /// <summary>
    /// Metadata key for the HTTP status code returned by the DNS TXT resolver.
    /// </summary>
    public const string LastDnsTxtProofCollectionStatusCode = "lastDnsTxtProofCollectionStatusCode";

    /// <summary>
    /// Metadata key for the DNS TXT resolver response body length.
    /// </summary>
    public const string LastDnsTxtProofCollectionContentLength = "lastDnsTxtProofCollectionContentLength";

    /// <summary>
    /// Metadata key for the number of TXT answers observed by collection.
    /// </summary>
    public const string LastDnsTxtProofCollectionObservedTxtRecordCount = "lastDnsTxtProofCollectionObservedTxtRecordCount";

    /// <summary>
    /// Metadata key for the SHA-256 fingerprint of the matching collected DNS TXT proof.
    /// </summary>
    public const string LastDnsTxtProofCollectionObservedFingerprint = "lastDnsTxtProofCollectionObservedFingerprint";

    /// <summary>
    /// Metadata key for the nested publication-plan outcome used by collection.
    /// </summary>
    public const string LastDnsTxtProofCollectionPublicationPlanOutcome = "lastDnsTxtProofCollectionPublicationPlanOutcome";

    /// <summary>
    /// Metadata key that identifies Cephalon as the DNS TXT proof collection owner when a resolver endpoint is configured.
    /// </summary>
    public const string DnsTxtProofCollectionOwnership = "dnsTxtProofCollectionOwnership";

    /// <summary>
    /// Metadata key for HTTP proof collection ownership.
    /// </summary>
    public const string HttpProofCollectionOwnership = "httpProofCollectionOwnership";

    /// <summary>
    /// Metadata key that keeps on-demand external proof polling ownership explicit.
    /// </summary>
    public const string ExternalProofPollingOwnership = "externalProofPollingOwnership";

    /// <summary>
    /// Metadata key that keeps automatic background proof polling ownership explicit.
    /// </summary>
    public const string BackgroundProofPollingOwnership = "backgroundProofPollingOwnership";
}
