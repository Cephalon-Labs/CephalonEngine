namespace Cephalon.Cli.Commands;

/// <summary>
/// Represents the parsed options for the <c>cephalon doctor</c> command.
/// </summary>
internal sealed class DoctorOptions
{
    /// <summary>
    /// Gets or sets the optional generated-app root that should be validated in addition to the machine baseline.
    /// </summary>
    internal string? AppRootPath { get; set; }

    /// <summary>
    /// Gets or sets the optional generated engine-completion scorecard JSON artifact that should be summarized.
    /// </summary>
    internal string? ScorecardPath { get; set; }
}
