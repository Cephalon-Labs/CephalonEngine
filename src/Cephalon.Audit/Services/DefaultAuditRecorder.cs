using System.Diagnostics;
using Cephalon.Abstractions.Audit;
using Cephalon.Abstractions.Ids;
using Cephalon.Abstractions.Tenancy;
using Microsoft.Extensions.Logging;

namespace Cephalon.Audit.Services;

internal sealed class DefaultAuditRecorder(
    IEnumerable<IAuditWriter> writers,
    IAuditActorAccessor actorAccessor,
    ILogger<DefaultAuditRecorder> logger,
    ITenantContextAccessor? tenantContextAccessor = null,
    IIdGenerator? idGenerator = null) : IAuditRecorder
{
    private readonly IAuditWriter[] activeWriters = writers?.ToArray() ?? throw new ArgumentNullException(nameof(writers));

    public async ValueTask<AuditEntry> RecordAsync(
        AuditRecordRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        cancellationToken.ThrowIfCancellationRequested();

        var tenantId = request.TenantId ?? tenantContextAccessor?.Current?.TenantId;
        var actor = request.Actor ?? actorAccessor.Current ?? CreateSystemActor();
        var entryId = request.EntryId;
        if (string.IsNullOrWhiteSpace(entryId))
        {
            entryId = await GenerateEntryIdAsync(tenantId, cancellationToken).ConfigureAwait(false);
        }

        var correlationId = request.CorrelationId;
        if (string.IsNullOrWhiteSpace(correlationId) && Activity.Current is not null)
        {
            correlationId = Activity.Current.TraceId.ToString();
        }

        var entry = new AuditEntry(
            id: entryId!,
            category: request.Category,
            action: request.Action,
            summary: request.Summary,
            subjectType: request.SubjectType,
            subjectId: request.SubjectId,
            occurredAtUtc: request.OccurredAtUtc ?? DateTimeOffset.UtcNow,
            actor: actor,
            outcome: request.Outcome,
            tenantId: tenantId,
            correlationId: correlationId,
            changes: request.Changes,
            tags: request.Tags,
            metadata: request.Metadata);

        try
        {
            for (var index = 0; index < activeWriters.Length; index++)
            {
                await activeWriters[index].WriteAsync(entry, cancellationToken).ConfigureAwait(false);
            }

            AuditLoggerMessages.AuditEntryWritten(logger, entry.Id, entry.Category, entry.Action, null);
            return entry;
        }
        catch (Exception exception)
        {
            AuditLoggerMessages.AuditEntryWriteFailed(logger, entry.Id, entry.Category, entry.Action, exception);
            throw;
        }
    }

    private async ValueTask<string> GenerateEntryIdAsync(string? tenantId, CancellationToken cancellationToken)
    {
        if (idGenerator is null)
        {
            return Guid.NewGuid().ToString("N");
        }

        return await idGenerator.GenerateAsync(
            new IdGenerationRequest(
                kind: "audit-entry",
                scope: "audit",
                tenantId: tenantId),
            cancellationToken).ConfigureAwait(false);
    }

    private static AuditActor CreateSystemActor()
    {
        return new AuditActor(
            actorId: "system",
            displayName: "system",
            actorType: "system",
            isSystem: true);
    }
}

internal static class AuditLoggerMessages
{
    private static readonly Action<ILogger, string, string, string, Exception?> AuditEntryWrittenMessage =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Information,
            new EventId(AuditDiagnosticsConventions.AuditEntryWritten.Id, AuditDiagnosticsConventions.AuditEntryWritten.Name),
            AuditDiagnosticsConventions.AuditEntryWritten.MessageTemplate);

    private static readonly Action<ILogger, string, string, string, Exception?> AuditEntryWriteFailedMessage =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Error,
            new EventId(AuditDiagnosticsConventions.AuditEntryWriteFailed.Id, AuditDiagnosticsConventions.AuditEntryWriteFailed.Name),
            AuditDiagnosticsConventions.AuditEntryWriteFailed.MessageTemplate);

    public static void AuditEntryWritten(ILogger logger, string auditEntryId, string category, string action, Exception? exception)
    {
        AuditEntryWrittenMessage(logger, auditEntryId, category, action, exception);
    }

    public static void AuditEntryWriteFailed(ILogger logger, string auditEntryId, string category, string action, Exception? exception)
    {
        AuditEntryWriteFailedMessage(logger, auditEntryId, category, action, exception);
    }
}
