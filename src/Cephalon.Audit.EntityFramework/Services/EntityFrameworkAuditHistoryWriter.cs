using System.Text.Json;
using Cephalon.Abstractions.AppModel;
using Cephalon.Abstractions.Audit;
using Cephalon.Audit.EntityFramework.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Audit.EntityFramework.Services;

internal sealed class EntityFrameworkAuditHistoryWriter<TDbContext>(
    IServiceScopeFactory scopeFactory,
    AppProfile appProfile) : IAuditWriter
    where TDbContext : Microsoft.EntityFrameworkCore.DbContext, IEntityFrameworkAuditHistoryContext
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async ValueTask WriteAsync(
        AuditEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (appProfile.Audit.Enabled == false ||
            appProfile.Audit.History.Enabled != true ||
            !EntityFrameworkAuditHistorySelection.MatchesProvider(appProfile.Audit.History.Provider))
        {
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
        dbContext.AuditEntries.Add(new EntityFrameworkAuditHistoryEntry
        {
            Id = entry.Id,
            Category = entry.Category,
            Action = entry.Action,
            Summary = entry.Summary,
            SubjectType = entry.SubjectType,
            SubjectId = entry.SubjectId,
            OccurredAtUtc = entry.OccurredAtUtc,
            PersistedAtUtc = DateTimeOffset.UtcNow,
            ActorId = entry.Actor.ActorId,
            ActorDisplayName = entry.Actor.DisplayName ?? string.Empty,
            ActorType = entry.Actor.ActorType ?? string.Empty,
            ActorIsSystem = entry.Actor.IsSystem,
            Outcome = entry.Outcome.ToString(),
            TenantId = entry.TenantId,
            CorrelationId = entry.CorrelationId,
            ChangesJson = JsonSerializer.Serialize(entry.Changes, SerializerOptions),
            TagsJson = JsonSerializer.Serialize(entry.Tags, SerializerOptions),
            MetadataJson = JsonSerializer.Serialize(entry.Metadata, SerializerOptions)
        });

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
