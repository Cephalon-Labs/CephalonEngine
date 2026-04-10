using System.Text.Json;
using Cephalon.Abstractions.AppModel;
using Cephalon.Abstractions.Audit;
using Cephalon.Audit.EntityFramework.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Audit.EntityFramework.Services;

internal sealed class EntityFrameworkAuditHistoryReader<TDbContext>(
    IServiceScopeFactory scopeFactory,
    AppProfile appProfile) : IAuditHistoryReader
    where TDbContext : DbContext, IEntityFrameworkAuditHistoryContext
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async ValueTask<AuditHistoryEntry?> GetByIdAsync(
        string auditEntryId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(auditEntryId);

        if (!IsEnabled())
        {
            return null;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
        var entity = await dbContext.AuditEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(entry => entry.Id == auditEntryId.Trim(), cancellationToken)
            .ConfigureAwait(false);

        return entity is null
            ? null
            : Map(entity);
    }

    public async ValueTask<AuditHistoryQueryResult> QueryAsync(
        AuditHistoryQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (!IsEnabled())
        {
            return new AuditHistoryQueryResult(
                entries: [],
                offset: query.Offset,
                limit: query.Limit,
                totalCount: 0);
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
        IQueryable<EntityFrameworkAuditHistoryEntry> entries = dbContext.AuditEntries.AsNoTracking();

        if (query.Category is not null)
        {
            entries = entries.Where(entry => entry.Category == query.Category);
        }

        if (query.Action is not null)
        {
            entries = entries.Where(entry => entry.Action == query.Action);
        }

        if (query.SubjectType is not null)
        {
            entries = entries.Where(entry => entry.SubjectType == query.SubjectType);
        }

        if (query.SubjectId is not null)
        {
            entries = entries.Where(entry => entry.SubjectId == query.SubjectId);
        }

        if (query.ActorId is not null)
        {
            entries = entries.Where(entry => entry.ActorId == query.ActorId);
        }

        if (query.TenantId is not null)
        {
            entries = entries.Where(entry => entry.TenantId == query.TenantId);
        }

        if (query.CorrelationId is not null)
        {
            entries = entries.Where(entry => entry.CorrelationId == query.CorrelationId);
        }

        if (query.Outcome is { } outcome)
        {
            var outcomeValue = outcome.ToString();
            entries = entries.Where(entry => entry.Outcome == outcomeValue);
        }

        if (query.OccurredFromUtc is { } occurredFromUtc)
        {
            entries = entries.Where(entry => entry.OccurredAtUtc >= occurredFromUtc);
        }

        if (query.OccurredToUtc is { } occurredToUtc)
        {
            entries = entries.Where(entry => entry.OccurredAtUtc <= occurredToUtc);
        }

        var totalCount = await entries.CountAsync(cancellationToken).ConfigureAwait(false);
        var page = await entries
            .OrderByDescending(entry => entry.OccurredAtUtc)
            .ThenByDescending(entry => entry.PersistedAtUtc)
            .ThenByDescending(entry => entry.Id)
            .Skip(query.Offset)
            .Take(query.Limit)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new AuditHistoryQueryResult(
            entries: page.Select(Map).ToArray(),
            offset: query.Offset,
            limit: query.Limit,
            totalCount: totalCount);
    }

    private bool IsEnabled()
    {
        return appProfile.Audit.Enabled != false &&
            appProfile.Audit.History.Enabled == true &&
            EntityFrameworkAuditHistorySelection.MatchesProvider(appProfile.Audit.History.Provider);
    }

    private static AuditHistoryEntry Map(EntityFrameworkAuditHistoryEntry entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new AuditHistoryEntry(
            id: entity.Id,
            category: entity.Category,
            action: entity.Action,
            summary: entity.Summary,
            subjectType: entity.SubjectType,
            subjectId: entity.SubjectId,
            occurredAtUtc: entity.OccurredAtUtc,
            persistedAtUtc: entity.PersistedAtUtc,
            actor: new AuditActor(
                actorId: entity.ActorId,
                displayName: entity.ActorDisplayName,
                actorType: entity.ActorType,
                isSystem: entity.ActorIsSystem),
            outcome: ParseOutcome(entity.Outcome),
            tenantId: entity.TenantId,
            correlationId: entity.CorrelationId,
            changes: Deserialize<IReadOnlyList<AuditChange>>(entity.ChangesJson, Array.Empty<AuditChange>()),
            tags: Deserialize<IReadOnlyList<string>>(entity.TagsJson, Array.Empty<string>()),
            metadata: Deserialize(entity.MetadataJson, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)));
    }

    private static AuditOutcome ParseOutcome(string? value)
    {
        return Enum.TryParse<AuditOutcome>(value, ignoreCase: true, out var parsed)
            ? parsed
            : AuditOutcome.Unknown;
    }

    private static T Deserialize<T>(string? payload, T fallback)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return fallback;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(payload, SerializerOptions) ?? fallback;
        }
        catch (JsonException)
        {
            return fallback;
        }
    }
}
