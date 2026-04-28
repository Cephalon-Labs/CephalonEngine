namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Stable metadata keys written by tenant-domain ownership proof challenge issuance.
/// </summary>
public static class TenantDomainOwnershipProofChallengeMetadataKeys
{
    /// <summary>
    /// Metadata key for the last proof challenge issuance outcome.
    /// </summary>
    public const string LastProofChallengeOutcome = "lastProofChallengeOutcome";

    /// <summary>
    /// Metadata key for the source that requested proof challenge issuance.
    /// </summary>
    public const string LastProofChallengeSource = "lastProofChallengeSource";

    /// <summary>
    /// Metadata key for the actor that requested proof challenge issuance.
    /// </summary>
    public const string LastProofChallengeActor = "lastProofChallengeActor";

    /// <summary>
    /// Metadata key for the challenge issuance correlation identifier.
    /// </summary>
    public const string LastProofChallengeCorrelationId = "lastProofChallengeCorrelationId";

    /// <summary>
    /// Metadata key for the UTC timestamp when the challenge was issued.
    /// </summary>
    public const string LastProofChallengeIssuedAtUtc = "lastProofChallengeIssuedAtUtc";

    /// <summary>
    /// Metadata key for the UTC timestamp when the challenge expires.
    /// </summary>
    public const string LastProofChallengeExpiresAtUtc = "lastProofChallengeExpiresAtUtc";

    /// <summary>
    /// Metadata key for the SHA-256 fingerprint of the issued challenge value.
    /// </summary>
    public const string LastProofChallengeFingerprint = "lastProofChallengeFingerprint";

    /// <summary>
    /// Metadata key for the DNS TXT record name where the challenge should be published.
    /// </summary>
    public const string DnsTxtRecordName = "proofChallengeDnsTxtRecordName";

    /// <summary>
    /// Metadata key for the HTTP path where the challenge should be published.
    /// </summary>
    public const string HttpFilePath = "proofChallengeHttpFilePath";

    /// <summary>
    /// Metadata key that identifies Cephalon as the challenge issuance owner.
    /// </summary>
    public const string ProofChallengeOwnership = "proofChallengeOwnership";
}
