namespace Cephalon.Cli.Commands;

/// <summary>
/// Represents the parsed options for the <c>cephalon docs publish</c> command.
/// </summary>
internal sealed class DocsPublishOptions
{
    /// <summary>
    /// Gets the repository root that should be scanned for built assemblies and XML docs.
    /// </summary>
    internal required string RootPath { get; init; }

    /// <summary>
    /// Gets the output directory where the generated reference docs should be written.
    /// </summary>
    internal required string OutputPath { get; init; }

    /// <summary>
    /// Gets the build configuration that should be read from.
    /// </summary>
    internal required string Configuration { get; init; }

    /// <summary>
    /// Gets the target framework that should be read from.
    /// </summary>
    internal required string TargetFramework { get; init; }

    /// <summary>
    /// Gets the assemblies to publish. When empty, the generator uses its curated default set.
    /// </summary>
    internal required IReadOnlyList<string> Assemblies { get; init; }

    /// <summary>
    /// Gets a value indicating whether existing files should be preserved instead of overwritten.
    /// </summary>
    internal bool NoOverwrite { get; init; }

    /// <summary>
    /// Gets a value indicating whether the command should also enable hosted reference-doc serving in an appsettings file.
    /// </summary>
    internal bool EnableHosting { get; init; }

    /// <summary>
    /// Gets the appsettings file to update when <see cref="EnableHosting" /> is enabled.
    /// </summary>
    internal string? AppSettingsPath { get; init; }

    /// <summary>
    /// Gets the optional directory path override to write into the <c>ReferenceDocs</c> section when hosting is enabled.
    /// </summary>
    internal string? HostingDirectoryPath { get; init; }

    /// <summary>
    /// Gets the optional route prefix override to write into the <c>ReferenceDocs</c> section when hosting is enabled.
    /// </summary>
    internal string? HostingRoutePrefix { get; init; }

    /// <summary>
    /// Gets the optional default document override to write into the <c>ReferenceDocs</c> section when hosting is enabled.
    /// </summary>
    internal string? HostingDefaultDocument { get; init; }

    /// <summary>
    /// Gets a value indicating whether the command should open the generated or hosted reference-doc surface after publishing.
    /// </summary>
    internal bool OpenOutput { get; init; }

    /// <summary>
    /// Gets the optional host base URL used when <see cref="OpenOutput" /> should open a hosted route instead of a local file.
    /// </summary>
    internal string? HostUrl { get; init; }

    /// <summary>
    /// Gets a value indicating whether the command should validate hosted reference-doc configuration after publishing.
    /// </summary>
    internal bool ValidateHosting { get; init; }
}
