namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Stable metadata keys written by tenant-domain ownership HTTP proof publication.
/// </summary>
public static class TenantDomainOwnershipHttpProofPublicationMetadataKeys
{
    /// <summary>
    /// Metadata key for the last HTTP proof publication outcome.
    /// </summary>
    public const string LastHttpProofPublicationOutcome = "lastHttpProofPublicationOutcome";

    /// <summary>
    /// Metadata key for the UTC timestamp when HTTP proof publication was recorded.
    /// </summary>
    public const string LastHttpProofPublishedAtUtc = "lastHttpProofPublishedAtUtc";

    /// <summary>
    /// Metadata key for the source that requested HTTP proof publication.
    /// </summary>
    public const string LastHttpProofPublicationSource = "lastHttpProofPublicationSource";

    /// <summary>
    /// Metadata key for the actor that requested HTTP proof publication.
    /// </summary>
    public const string LastHttpProofPublicationActor = "lastHttpProofPublicationActor";

    /// <summary>
    /// Metadata key for the HTTP proof publication correlation identifier.
    /// </summary>
    public const string LastHttpProofPublicationCorrelationId = "lastHttpProofPublicationCorrelationId";

    /// <summary>
    /// Metadata key that identifies Cephalon as the HTTP proof publication owner.
    /// </summary>
    public const string HttpProofPublicationOwnership = "httpProofPublicationOwnership";

    /// <summary>
    /// Metadata key for the HTTP path where the proof file is published.
    /// </summary>
    public const string HttpProofPublicationPath = "httpProofPublicationPath";

    /// <summary>
    /// Metadata key for the HTTP content type used by the published proof file.
    /// </summary>
    public const string HttpProofPublicationContentType = "httpProofPublicationContentType";

    /// <summary>
    /// Metadata key for the SHA-256 fingerprint of the published proof-file content.
    /// </summary>
    public const string HttpProofPublicationContentFingerprint = "httpProofPublicationContentFingerprint";

    /// <summary>
    /// Metadata key that identifies the component responsible for serving the proof file.
    /// </summary>
    public const string HttpProofPublicationServedBy = "httpProofPublicationServedBy";
}
