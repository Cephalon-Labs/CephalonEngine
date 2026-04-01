namespace Cephalon.Abstractions.Transports;

[Flags]
public enum TransportFeatures
{
    None = 0,
    RequestResponse = 1,
    ServerStreaming = 2,
    ClientStreaming = 4,
    DuplexStreaming = 8
}
