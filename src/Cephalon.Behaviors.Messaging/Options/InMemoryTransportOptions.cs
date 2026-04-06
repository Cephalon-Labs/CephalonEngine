namespace Cephalon.Behaviors.Messaging.Options;

/// <summary>
/// Configuration options for the in-memory messaging transport binding.
/// </summary>
public sealed class InMemoryTransportOptions
{
    /// <summary>
    /// Gets or sets the maximum number of messages buffered before backpressure is applied.
    /// Default: 1000.
    /// </summary>
    public int Capacity { get; set; } = 1000;
}
