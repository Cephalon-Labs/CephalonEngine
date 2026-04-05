using Cephalon.Abstractions.Audit;
using Cephalon.Audit.Configuration;

namespace Cephalon.Audit.Services;

internal sealed class ConfiguredInMemoryAuditWriter(
    AuditRuntimeOptions options,
    InMemoryAuditWriter innerWriter) : IAuditWriter
{
    public ValueTask WriteAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        cancellationToken.ThrowIfCancellationRequested();

        return options.EnableInMemoryWriter
            ? innerWriter.WriteAsync(entry, cancellationToken)
            : ValueTask.CompletedTask;
    }
}
