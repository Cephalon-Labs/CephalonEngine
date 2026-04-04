namespace Cephalon.Cli.Commands;

/// <summary>
/// Represents the parsed options for the <c>cephalon package stage</c> command.
/// </summary>
internal sealed class PackageStageOptions
{
    /// <summary>
    /// Gets the published package artifact that should be staged.
    /// </summary>
    internal required string PackagePath { get; init; }

    /// <summary>
    /// Gets the output directory that should receive the staged manifest and assembly files.
    /// </summary>
    internal required string OutputPath { get; init; }

    /// <summary>
    /// Gets the target framework that should be staged from the package.
    /// </summary>
    internal required string TargetFramework { get; init; }

    /// <summary>
    /// Gets a value indicating whether an existing output directory may be replaced.
    /// </summary>
    internal bool Force { get; init; }
}
