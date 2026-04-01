namespace Cephalon.ReferenceDocs.Generation;

/// <summary>
/// Represents the full rendered output of a reference documentation generation request.
/// </summary>
public sealed class RenderedReferenceDocs
{
    /// <summary>
    /// Creates a new rendered reference docs result.
    /// </summary>
    /// <param name="request">The original generation request.</param>
    /// <param name="files">The generated markdown files.</param>
    public RenderedReferenceDocs(
        ReferenceDocsRequest request,
        IReadOnlyList<ReferenceDocFile> files)
    {
        Request = request ?? throw new ArgumentNullException(nameof(request));
        Files = files ?? throw new ArgumentNullException(nameof(files));
    }

    /// <summary>
    /// Gets the original generation request.
    /// </summary>
    public ReferenceDocsRequest Request { get; }

    /// <summary>
    /// Gets the generated markdown files.
    /// </summary>
    public IReadOnlyList<ReferenceDocFile> Files { get; }
}
