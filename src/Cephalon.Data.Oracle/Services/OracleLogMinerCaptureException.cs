namespace Cephalon.Data.Oracle.Services;

internal interface IOracleLogMinerCaptureFailureMetadata
{
    string FailureKind { get; }

    IReadOnlyDictionary<string, string> Metadata { get; }
}

internal sealed class OracleLogMinerCaptureException(
    string message,
    string failureKind,
    IReadOnlyDictionary<string, string>? metadata = null,
    Exception? innerException = null) : InvalidOperationException(message, innerException), IOracleLogMinerCaptureFailureMetadata
{
    public string FailureKind { get; } = failureKind;

    public IReadOnlyDictionary<string, string> Metadata { get; } =
        metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
}
