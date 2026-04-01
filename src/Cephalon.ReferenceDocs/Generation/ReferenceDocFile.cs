namespace Cephalon.ReferenceDocs.Generation;

/// <summary>
/// Represents one generated reference documentation file.
/// </summary>
public sealed class ReferenceDocFile
{
    /// <summary>
    /// Creates a new generated reference documentation file.
    /// </summary>
    /// <param name="path">The relative output path of the file.</param>
    /// <param name="contents">The markdown contents of the file.</param>
    public ReferenceDocFile(string path, string contents)
    {
        Path = path ?? throw new ArgumentNullException(nameof(path));
        Contents = contents ?? throw new ArgumentNullException(nameof(contents));
    }

    /// <summary>
    /// Gets the relative output path of the file.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets the markdown contents of the file.
    /// </summary>
    public string Contents { get; }
}
