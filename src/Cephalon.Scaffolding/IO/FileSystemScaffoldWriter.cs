using Cephalon.Scaffolding.Generation;

namespace Cephalon.Scaffolding.IO;

/// <summary>
/// Writes a rendered scaffold to the local file system.
/// </summary>
public sealed class FileSystemScaffoldWriter
{
    /// <summary>
    /// Writes the supplied scaffold to disk.
    /// </summary>
    /// <param name="rootPath">The target root directory.</param>
    /// <param name="scaffold">The rendered scaffold to write.</param>
    /// <param name="overwrite">
    /// <see langword="true" /> to overwrite existing files; otherwise the write fails when a target file exists.
    /// </param>
    /// <param name="cancellationToken">A token that can cancel the write operation.</param>
    /// <returns>A task that completes when all folders and files have been written.</returns>
    public static async Task WriteAsync(
        string rootPath,
        RenderedScaffold scaffold,
        bool overwrite = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        ArgumentNullException.ThrowIfNull(scaffold);

        Directory.CreateDirectory(rootPath);

        foreach (var folder in scaffold.Projects.Select(project => project.Path)
                     .Concat(scaffold.Folders.Select(folder => folder.Path))
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            Directory.CreateDirectory(Path.Combine(rootPath, folder));
        }

        foreach (var file in scaffold.Files)
        {
            var filePath = Path.Combine(rootPath, file.Path);
            var directory = Path.GetDirectoryName(filePath);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (!overwrite && File.Exists(filePath))
            {
                throw new InvalidOperationException(
                    $"Scaffold file '{file.Path}' already exists. Pass overwrite: true to replace it.");
            }

            await File.WriteAllTextAsync(filePath, file.Contents, cancellationToken);
        }
    }
}
