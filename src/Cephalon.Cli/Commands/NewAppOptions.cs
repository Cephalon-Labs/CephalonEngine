namespace Cephalon.Cli.Commands;

/// <summary>
/// Represents the parsed options for the <c>cephalon new</c> command.
/// </summary>
internal sealed class NewAppOptions
{
    /// <summary>
    /// Gets the application name to generate.
    /// </summary>
    internal required string AppName { get; init; }

    /// <summary>
    /// Gets the blueprint to use for generation.
    /// </summary>
    internal required string Blueprint { get; init; }

    /// <summary>
    /// Gets the output directory where the scaffold should be written.
    /// </summary>
    internal required string OutputPath { get; init; }

    /// <summary>
    /// Gets the Cephalon package version to write into the generated scaffold.
    /// </summary>
    internal required string PackageVersion { get; init; }

    /// <summary>
    /// Gets the target framework for generated projects.
    /// </summary>
    internal required string TargetFramework { get; init; }

    /// <summary>
    /// Gets the module names requested for generation.
    /// </summary>
    internal required IReadOnlyList<string> Modules { get; init; }

    /// <summary>
    /// Gets the feature or slice names requested for generation.
    /// </summary>
    internal required IReadOnlyList<string> Features { get; init; }

    /// <summary>
    /// Gets the additional patterns requested for generation.
    /// </summary>
    internal required IReadOnlyList<string> Patterns { get; init; }

    /// <summary>
    /// Gets the technology profiles requested for generation.
    /// </summary>
    internal required IReadOnlyList<string> Technologies { get; init; }

    /// <summary>
    /// Gets the transports requested for generation.
    /// </summary>
    internal required IReadOnlyList<string> Transports { get; init; }

    /// <summary>
    /// Gets a value indicating whether existing files may be overwritten.
    /// </summary>
    internal bool Force { get; init; }
}
