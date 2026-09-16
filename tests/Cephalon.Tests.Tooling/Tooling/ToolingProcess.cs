using System.Diagnostics;
using System.Text;

namespace Cephalon.Tests.Tooling;

internal static class ToolingProcess
{
    internal static ToolingProcessResult Run(
        string fileName, string arguments, string workingDirectory, TimeSpan? timeout = null)
        => RunAsync(fileName, arguments, workingDirectory, timeout ?? TimeSpan.FromMinutes(10))
            .GetAwaiter().GetResult();

    private static async Task<ToolingProcessResult> RunAsync(
        string fileName, string arguments, string workingDirectory, TimeSpan timeout)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout, TimeSpan.Zero);
        var startInfo = new ProcessStartInfo(fileName, arguments)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        // Nested builds must not leave reusable workers holding our redirected pipes open.
        startInfo.Environment["MSBUILDDISABLENODEREUSE"] = "1";
        startInfo.Environment["DOTNET_CLI_USE_MSBUILD_SERVER"] = "false";
        startInfo.Environment["UseSharedCompilation"] = "false";
        var output = new StringBuilder();
        var error = new StringBuilder();
        var outputClosed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var errorClosed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var process = new Process { StartInfo = startInfo };
        process.OutputDataReceived += (_, args) => Capture(args.Data, output, outputClosed);
        process.ErrorDataReceived += (_, args) => Capture(args.Data, error, errorClosed);
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        var completion = Task.WhenAll(process.WaitForExitAsync(), outputClosed.Task, errorClosed.Task);
        try
        {
            // One budget includes exit AND pipe EOF. A root process can exit before descendants close pipes.
            await completion.WaitAsync(timeout).ConfigureAwait(false);
            return new(process.ExitCode, Snapshot(output), Snapshot(error));
        }
        catch (TimeoutException)
        {
            try
            {
                if (!process.HasExited) process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
                // Exit raced with the kill request.
            }

            try
            {
                await completion.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                // An already-exited root cannot kill an orphan retaining its inherited pipe handles.
                process.CancelOutputRead();
                process.CancelErrorRead();
            }

            return new(-1, Snapshot(output), Snapshot(error)
                + $"{Environment.NewLine}Process or redirected output timed out after {timeout.TotalSeconds} seconds.");
        }
    }

    private static void Capture(string? line, StringBuilder buffer, TaskCompletionSource closed)
    {
        if (line is null) { closed.TrySetResult(); return; }
        lock (buffer) { buffer.AppendLine(line); }
    }

    private static string Snapshot(StringBuilder buffer)
    {
        lock (buffer) { return buffer.ToString(); }
    }
}

internal sealed record ToolingProcessResult(int ExitCode, string Output, string Error);
