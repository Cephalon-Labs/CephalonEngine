using System.Diagnostics;

namespace Cephalon.Cli.Commands;

/// <summary>
/// Runs external commands for CLI workflows that need host-environment verification.
/// </summary>
internal static class CommandProcessRunner
{
    /// <summary>
    /// Gets or sets the test hook used to intercept command execution.
    /// </summary>
    internal static Func<string, IReadOnlyList<string>, string?, CancellationToken, Task<CommandProcessResult>>? RunOverride { get; set; }

    /// <summary>
    /// Executes a process and captures its output.
    /// </summary>
    /// <param name="fileName">The executable to start.</param>
    /// <param name="arguments">The arguments to pass to the executable.</param>
    /// <param name="workingDirectory">The optional working directory.</param>
    /// <param name="cancellationToken">A token that can cancel the wait for process completion.</param>
    /// <returns>The captured command result.</returns>
    internal static async Task<CommandProcessResult> RunAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileName);
        ArgumentNullException.ThrowIfNull(arguments);

        if (RunOverride is not null)
        {
            return await RunOverride(fileName, arguments, workingDirectory, cancellationToken);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            WorkingDirectory = string.IsNullOrWhiteSpace(workingDirectory)
                ? Directory.GetCurrentDirectory()
                : workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Could not start '{fileName}'.");

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        return new CommandProcessResult(
            process.ExitCode,
            await outputTask,
            await errorTask);
    }
}

/// <summary>
/// Represents the captured result of an external command.
/// </summary>
/// <param name="ExitCode">The process exit code.</param>
/// <param name="Output">The captured standard output.</param>
/// <param name="Error">The captured standard error.</param>
internal sealed record CommandProcessResult(int ExitCode, string Output, string Error);
