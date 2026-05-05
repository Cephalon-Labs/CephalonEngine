namespace Cephalon.Data.Postgres.Services;

internal interface IPostgresLogicalReplicationCaptureFailureMetadata
{
    string FailureKind { get; }

    IReadOnlyDictionary<string, string> Metadata { get; }
}

internal sealed class PostgresLogicalReplicationCaptureException(
    string message,
    string failureKind,
    IReadOnlyDictionary<string, string>? metadata = null,
    Exception? innerException = null) : InvalidOperationException(message, innerException), IPostgresLogicalReplicationCaptureFailureMetadata
{
    public string FailureKind { get; } = failureKind;

    public IReadOnlyDictionary<string, string> Metadata { get; } =
        metadata is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(metadata, StringComparer.OrdinalIgnoreCase);
}
