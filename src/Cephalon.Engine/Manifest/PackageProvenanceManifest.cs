namespace Cephalon.Engine.Manifest;

/// <summary>
/// Describes provenance metadata declared for an independently shipped package.
/// </summary>
public sealed class PackageProvenanceManifest
{
    /// <summary>
    /// Creates a package provenance manifest entry.
    /// </summary>
    /// <param name="sourceRepository">The source repository URI declared by the package manifest, when available.</param>
    /// <param name="sourceRevision">The source revision, tag, or commit identifier declared by the package manifest, when available.</param>
    /// <param name="buildUri">The build or pipeline URI declared by the package manifest, when available.</param>
    /// <param name="statementUri">The provenance statement or attestation URI declared by the package manifest, when available.</param>
    public PackageProvenanceManifest(
        string? sourceRepository,
        string? sourceRevision,
        string? buildUri,
        string? statementUri)
    {
        SourceRepository = sourceRepository;
        SourceRevision = sourceRevision;
        BuildUri = buildUri;
        StatementUri = statementUri;
    }

    /// <summary>
    /// Gets the declared source repository URI, when available.
    /// </summary>
    public string? SourceRepository { get; }

    /// <summary>
    /// Gets the declared source revision, tag, or commit identifier, when available.
    /// </summary>
    public string? SourceRevision { get; }

    /// <summary>
    /// Gets the declared build or pipeline URI, when available.
    /// </summary>
    public string? BuildUri { get; }

    /// <summary>
    /// Gets the declared provenance statement or attestation URI, when available.
    /// </summary>
    public string? StatementUri { get; }
}
