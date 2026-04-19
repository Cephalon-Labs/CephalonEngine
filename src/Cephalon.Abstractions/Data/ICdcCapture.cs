namespace Cephalon.Abstractions.Data;

/// <summary>
/// Captures database changes and shapes them into outbox-ready publications.
/// </summary>
public interface ICdcCapture
{
    /// <summary>
    /// Reads captured database changes and yields the resulting outbox messages.
    /// </summary>
    /// <param name="cancellationToken">The token that cancels the capture stream.</param>
    /// <returns>An async stream of outbox messages produced from the captured changes.</returns>
    IAsyncEnumerable<OutboxMessage> CaptureAsync(CancellationToken cancellationToken = default);
}
