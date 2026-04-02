namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Describes the protocol capabilities supported by a transport.
/// </summary>
[Flags]
public enum TransportFeatures
{
    /// <summary>
    /// Indicates no transport features.
    /// </summary>
    None = 0,

    /// <summary>
    /// Indicates request-response interactions are supported.
    /// </summary>
    RequestResponse = 1,

    /// <summary>
    /// Indicates server-streaming interactions are supported.
    /// </summary>
    ServerStreaming = 2,

    /// <summary>
    /// Indicates client-streaming interactions are supported.
    /// </summary>
    ClientStreaming = 4,

    /// <summary>
    /// Indicates duplex-streaming interactions are supported.
    /// </summary>
    DuplexStreaming = 8
}
