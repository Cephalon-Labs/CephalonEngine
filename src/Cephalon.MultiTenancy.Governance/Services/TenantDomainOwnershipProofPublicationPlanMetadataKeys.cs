namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Stable metadata keys written by tenant-domain ownership proof publication planning.
/// </summary>
public static class TenantDomainOwnershipProofPublicationPlanMetadataKeys
{
    /// <summary>
    /// Metadata key for the last publication planning outcome.
    /// </summary>
    public const string LastProofPublicationPlanOutcome = "lastProofPublicationPlanOutcome";

    /// <summary>
    /// Metadata key for the source that requested publication planning.
    /// </summary>
    public const string LastProofPublicationPlanSource = "lastProofPublicationPlanSource";

    /// <summary>
    /// Metadata key for the actor that requested publication planning.
    /// </summary>
    public const string LastProofPublicationPlanActor = "lastProofPublicationPlanActor";

    /// <summary>
    /// Metadata key for the publication planning correlation identifier.
    /// </summary>
    public const string LastProofPublicationPlanCorrelationId = "lastProofPublicationPlanCorrelationId";

    /// <summary>
    /// Metadata key for the UTC timestamp when publication planning executed.
    /// </summary>
    public const string LastProofPublicationPlannedAtUtc = "lastProofPublicationPlannedAtUtc";

    /// <summary>
    /// Metadata key for the SHA-256 fingerprint of the planned proof value.
    /// </summary>
    public const string LastProofPublicationPlanFingerprint = "lastProofPublicationPlanFingerprint";

    /// <summary>
    /// Metadata key for the DNS TXT record name where the proof should be published.
    /// </summary>
    public const string DnsTxtRecordName = "proofPublicationDnsTxtRecordName";

    /// <summary>
    /// Metadata key for the DNS TXT record value fingerprint.
    /// </summary>
    public const string DnsTxtRecordValueFingerprint = "proofPublicationDnsTxtRecordValueFingerprint";

    /// <summary>
    /// Metadata key for the HTTP path where the proof file should be published.
    /// </summary>
    public const string HttpFilePath = "proofPublicationHttpFilePath";

    /// <summary>
    /// Metadata key for the HTTP file content fingerprint.
    /// </summary>
    public const string HttpFileContentFingerprint = "proofPublicationHttpFileContentFingerprint";

    /// <summary>
    /// Metadata key for the HTTP content type used by the publication plan.
    /// </summary>
    public const string HttpContentType = "proofPublicationHttpContentType";

    /// <summary>
    /// Metadata key that identifies Cephalon as the publication planning owner.
    /// </summary>
    public const string ProofPublicationPlanningOwnership = "proofPublicationPlanningOwnership";

    /// <summary>
    /// Metadata key that keeps external publication ownership explicit.
    /// </summary>
    public const string ExternalPublicationOwnership = "proofExternalPublicationOwnership";
}
