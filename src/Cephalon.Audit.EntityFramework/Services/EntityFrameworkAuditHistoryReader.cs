using System.Runtime.CompilerServices;
using System.Text.Json;
using Cephalon.Abstractions.AppModel;
using Cephalon.Abstractions.Audit;
using Cephalon.Audit.EntityFramework.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Audit.EntityFramework.Services;

internal sealed class EntityFrameworkAuditHistoryReader<TDbContext>(
    IServiceScopeFactory scopeFactory,
    AppProfile appProfile) : IAuditHistoryReader, IAuditHistoryExporter
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
        var filteredEntries = ApplyFilters(dbContext.AuditEntries.AsNoTracking(), query);

        var totalCount = await filteredEntries.CountAsync(cancellationToken).ConfigureAwait(false);
        var page = await OrderForExport(filteredEntries)
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

    public async IAsyncEnumerable<AuditHistoryEntry> ExportAsync(
        AuditHistoryExportRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!IsEnabled())
        {
            yield break;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
        var filteredEntries = ApplyFilters(dbContext.AuditEntries.AsNoTracking(), request);

        var exportEntries = OrderForExport(filteredEntries)
            .Take(request.MaxEntries)
            .AsAsyncEnumerable();

        await foreach (var entity in exportEntries.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            yield return Map(entity);
        }
    }

    private bool IsEnabled()
    {
        return appProfile.Audit.Enabled != false &&
            appProfile.Audit.History.Enabled == true &&
            EntityFrameworkAuditHistorySelection.MatchesProvider(appProfile.Audit.History.Provider);
    }

    private static IQueryable<EntityFrameworkAuditHistoryEntry> ApplyFilters(
        IQueryable<EntityFrameworkAuditHistoryEntry> entries,
        AuditHistoryQuery query)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(query);

        return ApplyFilters(
            entries,
            query.Category,
            query.Action,
            query.SubjectType,
            query.SubjectId,
            query.ActorId,
            query.TenantId,
            query.CorrelationId,
            query.Outcome,
            query.OccurredFromUtc,
            query.OccurredToUtc);
    }

    private static IQueryable<EntityFrameworkAuditHistoryEntry> ApplyFilters(
        IQueryable<EntityFrameworkAuditHistoryEntry> entries,
        AuditHistoryExportRequest request)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(request);

        return ApplyFilters(
            entries,
            request.Category,
            request.Action,
            request.SubjectType,
            request.SubjectId,
            request.ActorId,
            request.TenantId,
            request.CorrelationId,
            request.Outcome,
            request.OccurredFromUtc,
            request.OccurredToUtc);
    }

    private static IQueryable<EntityFrameworkAuditHistoryEntry> ApplyFilters(
        IQueryable<EntityFrameworkAuditHistoryEntry> entries,
        string? category,
        string? action,
        string? subjectType,
        string? subjectId,
        string? actorId,
        string? tenantId,
        string? correlationId,
        AuditOutcome? outcome,
        DateTimeOffset? occurredFromUtc,
        DateTimeOffset? occurredToUtc)
    {
        if (category is not null)
        {
            entries = entries.Where(entry => entry.Category == category);
        }

        if (action is not null)
        {
            entries = entries.Where(entry => entry.Action == action);
        }

        if (subjectType is not null)
        {
            entries = entries.Where(entry => entry.SubjectType == subjectType);
        }

        if (subjectId is not null)
        {
            entries = entries.Where(entry => entry.SubjectId == subjectId);
        }

        if (actorId is not null)
        {
            entries = entries.Where(entry => entry.ActorId == actorId);
        }

        if (tenantId is not null)
        {
            entries = entries.Where(entry => entry.TenantId == tenantId);
        }

        if (correlationId is not null)
        {
            entries = entries.Where(entry => entry.CorrelationId == correlationId);
        }

        if (outcome is { } parsedOutcome)
        {
            var outcomeValue = parsedOutcome.ToString();
            entries = entries.Where(entry => entry.Outcome == outcomeValue);
        }

        if (occurredFromUtc is { } from)
        {
            entries = entries.Where(entry => entry.OccurredAtUtc >= from);
        }

        if (occurredToUtc is { } to)
        {
            entries = entries.Where(entry => entry.OccurredAtUtc <= to);
        }

        return entries;
    }

    private static IOrderedQueryable<EntityFrameworkAuditHistoryEntry> OrderForExport(
        IQueryable<EntityFrameworkAuditHistoryEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        return entries
            .OrderByDescending(entry => entry.OccurredAtUtc)
            .ThenByDescending(entry => entry.PersistedAtUtc)
            .ThenByDescending(entry => entry.Id);
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
