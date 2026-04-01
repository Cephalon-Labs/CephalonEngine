using Cephalon.ReferenceDocs.Generation;

namespace Cephalon.ReferenceDocs.IO;

/// <summary>
/// Writes rendered reference documentation to the local file system.
/// </summary>
public static class ReferenceDocsWriter
{
    /// <summary>
    /// Writes the supplied reference docs output to disk.
    /// </summary>
    /// <param name="rendered">The rendered reference docs to write.</param>
    /// <param name="overwrite">
    /// <see langword="true" /> to overwrite existing files; otherwise the write fails when a target file exists.
    /// </param>
    /// <param name="cancellationToken">A token that can cancel the write operation.</param>
    /// <returns>A task that completes when all files have been written.</returns>
    public static async Task WriteAsync(
        RenderedReferenceDocs rendered,
        bool overwrite = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rendered);

        Directory.CreateDirectory(rendered.Request.OutputPath);

        foreach (var file in rendered.Files)
        {
            var filePath = Path.Combine(rendered.Request.OutputPath, file.Path);
            var directory = Path.GetDirectoryName(filePath);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (!overwrite && File.Exists(filePath))
            {
                throw new InvalidOperationException(
                    $"Reference docs file '{file.Path}' already exists. Pass overwrite: true to replace it.");
            }

            await File.WriteAllTextAsync(filePath, file.Contents, cancellationToken);
        }
    }
}
