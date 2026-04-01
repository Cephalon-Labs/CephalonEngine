using System.Diagnostics;

namespace Cephalon.Cli.Commands;

/// <summary>
/// Opens generated or hosted reference-doc targets for the Cephalon CLI.
/// </summary>
internal static class DocumentationLauncher
{
    /// <summary>
    /// Gets or sets the test hook used to intercept open requests without launching a real browser.
    /// </summary>
    internal static Func<Uri, CancellationToken, Task>? OpenOverride { get; set; }

    /// <summary>
    /// Opens the supplied documentation target with the operating system shell.
    /// </summary>
    /// <param name="target">The file or URL to open.</param>
    /// <param name="cancellationToken">A token that can cancel the launch request before it starts.</param>
    /// <returns>A task that completes when the launch request has been issued.</returns>
    internal static async Task OpenAsync(Uri target, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(target);

        if (OpenOverride is not null)
        {
            await OpenOverride(target, cancellationToken);
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var launchTarget = target.IsFile
            ? target.LocalPath
            : target.AbsoluteUri;
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = launchTarget,
            UseShellExecute = true
        });

        if (process is null)
        {
            throw new InvalidOperationException($"Could not open '{launchTarget}'.");
        }
    }
}
