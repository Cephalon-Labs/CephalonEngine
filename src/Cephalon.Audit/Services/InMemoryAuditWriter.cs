using System.Collections.Concurrent;
using Cephalon.Abstractions.Audit;
using Cephalon.Audit.Configuration;

namespace Cephalon.Audit.Services;

internal sealed class InMemoryAuditWriter(AuditRuntimeOptions options) : IAuditWriter
{
    private readonly ConcurrentQueue<AuditEntry> entries = new();

    public ValueTask WriteAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        cancellationToken.ThrowIfCancellationRequested();

        entries.Enqueue(entry);
        while (entries.Count > options.InMemoryBufferCapacity && entries.TryDequeue(out _))
        {
        }

        return ValueTask.CompletedTask;
    }
}
