namespace Cephalon.Cli.Commands;

/// <summary>
/// Represents the parsed options for the <c>cephalon docs enable-hosting</c> command.
/// </summary>
internal sealed class DocsEnableHostingOptions
{
    /// <summary>
    /// Gets the repository root used to derive the default reference-doc directory.
    /// </summary>
    internal required string RootPath { get; init; }

    /// <summary>
    /// Gets the appsettings file that should be updated.
    /// </summary>
    internal required string AppSettingsPath { get; init; }

    /// <summary>
    /// Gets the directory path value that should be written into the <c>ReferenceDocs</c> section.
    /// When this is <see langword="null" />, the command preserves an existing value or derives one from
    /// <see cref="RootPath" /> and <see cref="AppSettingsPath" />.
    /// </summary>
    internal string? DirectoryPath { get; init; }

    /// <summary>
    /// Gets the route prefix value that should be written into the <c>ReferenceDocs</c> section.
    /// When this is <see langword="null" />, the command preserves an existing route prefix or falls back to the default.
    /// </summary>
    internal string? RoutePrefix { get; init; }

    /// <summary>
    /// Gets the default document value that should be written into the <c>ReferenceDocs</c> section.
    /// When this is <see langword="null" />, the command preserves an existing value or falls back to the default.
    /// </summary>
    internal string? DefaultDocument { get; init; }
}
