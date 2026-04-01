namespace Cephalon.Cli.Commands;

/// <summary>
/// Represents the parsed options for the <c>cephalon docs validate-hosting</c> command.
/// </summary>
internal sealed class DocsValidateHostingOptions
{
    /// <summary>
    /// Gets the appsettings file that should be inspected.
    /// </summary>
    internal required string AppSettingsPath { get; init; }

    /// <summary>
    /// Gets the optional host base URL used to print the expected hosted routes.
    /// </summary>
    internal string? HostUrl { get; init; }
}
