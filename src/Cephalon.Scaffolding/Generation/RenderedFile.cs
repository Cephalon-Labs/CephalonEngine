namespace Cephalon.Scaffolding.Generation;

/// <summary>
/// Represents one file produced by scaffold generation.
/// </summary>
public sealed class RenderedFile
{
    /// <summary>
    /// Creates a new rendered file.
    /// </summary>
    /// <param name="path">The relative scaffold path of the file.</param>
    /// <param name="contents">The file contents that should be written.</param>
    public RenderedFile(string path, string contents)
    {
        Path = path ?? throw new ArgumentNullException(nameof(path));
        Contents = contents ?? throw new ArgumentNullException(nameof(contents));
    }

    /// <summary>
    /// Gets the relative scaffold path of the file.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets the contents that should be written to the file.
    /// </summary>
    public string Contents { get; }
}
