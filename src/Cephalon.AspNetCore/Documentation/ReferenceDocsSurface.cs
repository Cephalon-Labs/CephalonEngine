namespace Cephalon.AspNetCore.Documentation;

/// <summary>
/// Describes the operator-facing HTTP surface for hosted Cephalon reference documentation.
/// </summary>
/// <param name="Enabled">Whether reference-doc hosting is enabled for the current host.</param>
/// <param name="Available">
/// Whether the configured documentation directory and default document were found and mapped.
/// </param>
/// <param name="RoutePrefix">The route prefix where the documentation is served.</param>
/// <param name="DefaultDocument">The document opened when the route prefix is requested.</param>
/// <param name="DefaultDocumentPath">The hosted path to the configured default document.</param>
/// <param name="ReadmePath">The hosted path to the reference-doc landing page.</param>
/// <param name="BrowserPath">The hosted path to the interactive browser UI.</param>
/// <param name="NamespaceIndexPath">The hosted path to the namespace index.</param>
/// <param name="TypeIndexPath">The hosted path to the type index.</param>
/// <param name="MemberIndexPath">The hosted path to the member index.</param>
/// <param name="ManifestPath">The hosted path to the machine-readable manifest.</param>
public sealed record ReferenceDocsSurface(
    bool Enabled,
    bool Available,
    string RoutePrefix,
    string DefaultDocument,
    string DefaultDocumentPath,
    string ReadmePath,
    string BrowserPath,
    string NamespaceIndexPath,
    string TypeIndexPath,
    string MemberIndexPath,
    string ManifestPath);
