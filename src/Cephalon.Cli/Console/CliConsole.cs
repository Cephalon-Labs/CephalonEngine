namespace Cephalon.Cli.Console;

/// <summary>
/// Wraps the text writers used by the Cephalon CLI.
/// </summary>
internal sealed class CliConsole
{
    /// <summary>
    /// Creates a new CLI console abstraction.
    /// </summary>
    /// <param name="output">The writer used for standard output.</param>
    /// <param name="error">The writer used for error output.</param>
    internal CliConsole(TextWriter output, TextWriter error)
    {
        Output = output ?? throw new ArgumentNullException(nameof(output));
        Error = error ?? throw new ArgumentNullException(nameof(error));
    }

    /// <summary>
    /// Gets the writer used for standard output.
    /// </summary>
    internal TextWriter Output { get; }

    /// <summary>
    /// Gets the writer used for error output.
    /// </summary>
    internal TextWriter Error { get; }

    /// <summary>
    /// Writes a line to standard output.
    /// </summary>
    /// <param name="message">The message to write.</param>
    /// <param name="cancellationToken">A token that can cancel the write operation.</param>
    /// <returns>A task that completes when the message has been written.</returns>
    internal Task WriteOutputAsync(string message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Output.WriteLineAsync(message);
    }

    /// <summary>
    /// Writes a line to error output.
    /// </summary>
    /// <param name="message">The message to write.</param>
    /// <param name="cancellationToken">A token that can cancel the write operation.</param>
    /// <returns>A task that completes when the message has been written.</returns>
    internal Task WriteErrorAsync(string message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Error.WriteLineAsync(message);
    }
}
