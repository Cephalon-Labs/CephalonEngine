namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Stable metadata keys written by tenant-domain ownership HTTP proof collection.
/// </summary>
public static class TenantDomainOwnershipHttpProofCollectionMetadataKeys
{
    /// <summary>
    /// Metadata key for the last HTTP proof collection outcome.
    /// </summary>
    public const string LastHttpProofCollectionOutcome = "lastHttpProofCollectionOutcome";

    /// <summary>
    /// Metadata key for the source that requested HTTP proof collection.
    /// </summary>
    public const string LastHttpProofCollectionSource = "lastHttpProofCollectionSource";

    /// <summary>
    /// Metadata key for the actor that requested HTTP proof collection when known.
    /// </summary>
    public const string LastHttpProofCollectionActor = "lastHttpProofCollectionActor";

    /// <summary>
    /// Metadata key for the HTTP proof collection correlation identifier.
    /// </summary>
    public const string LastHttpProofCollectionCorrelationId = "lastHttpProofCollectionCorrelationId";

    /// <summary>
    /// Metadata key for the UTC timestamp when HTTP proof collection executed.
    /// </summary>
    public const string LastHttpProofCollectedAtUtc = "lastHttpProofCollectedAtUtc";

    /// <summary>
    /// Metadata key for the URI used to collect the HTTP proof.
    /// </summary>
    public const string LastHttpProofCollectionUri = "lastHttpProofCollectionUri";

    /// <summary>
    /// Metadata key for the HTTP status code returned by the proof endpoint.
    /// </summary>
    public const string LastHttpProofCollectionStatusCode = "lastHttpProofCollectionStatusCode";

    /// <summary>
    /// Metadata key for the SHA-256 fingerprint of the collected HTTP proof body.
    /// </summary>
    public const string LastHttpProofCollectionObservedFingerprint = "lastHttpProofCollectionObservedFingerprint";

    /// <summary>
    /// Metadata key for the collected HTTP proof response body length.
    /// </summary>
    public const string LastHttpProofCollectionContentLength = "lastHttpProofCollectionContentLength";

    /// <summary>
    /// Metadata key for the nested publication-plan outcome used by collection.
    /// </summary>
    public const string LastHttpProofCollectionPublicationPlanOutcome = "lastHttpProofCollectionPublicationPlanOutcome";

    /// <summary>
    /// Metadata key that identifies Cephalon as the HTTP proof collection owner.
    /// </summary>
    public const string HttpProofCollectionOwnership = "httpProofCollectionOwnership";

    /// <summary>
    /// Metadata key that keeps DNS TXT proof collection ownership explicit.
    /// </summary>
    public const string DnsTxtProofCollectionOwnership = "dnsTxtProofCollectionOwnership";

    /// <summary>
    /// Metadata key that keeps background proof polling ownership explicit.
    /// </summary>
    public const string ExternalProofPollingOwnership = "externalProofPollingOwnership";
}
