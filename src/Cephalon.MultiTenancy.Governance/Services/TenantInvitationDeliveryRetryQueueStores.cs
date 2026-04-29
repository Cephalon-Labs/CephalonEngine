using Cephalon.MultiTenancy.Governance.Configuration;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cephalon.MultiTenancy.Governance.Services;

internal static class TenantInvitationDeliveryRetryQueueStores
{
    public static ITenantInvitationDeliveryRetryStore Create(MultiTenancyGovernanceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return string.IsNullOrWhiteSpace(options.InvitationDeliveryRetryQueueFilePath)
            ? new InMemoryTenantInvitationDeliveryRetryQueue()
            : new FileTenantInvitationDeliveryRetryQueue(options.InvitationDeliveryRetryQueueFilePath);
    }

    public static int ResolveMaxAttempts(MultiTenancyGovernanceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return Math.Max(1, options.InvitationDeliveryRetryMaxAttempts);
    }

    public static int ResolveRetryDelaySeconds(MultiTenancyGovernanceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return Math.Max(0, options.InvitationDeliveryRetryDelaySeconds);
    }

    public static int ResolveMaxItems(MultiTenancyGovernanceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return Math.Max(1, options.InvitationDeliveryRetryMaxItems);
    }
}

internal sealed class InMemoryTenantInvitationDeliveryRetryQueue : ITenantInvitationDeliveryRetryStore
{
    private readonly object gate = new();
    private readonly Dictionary<string, TenantInvitationDeliveryRetryDescriptor> entries = new(StringComparer.OrdinalIgnoreCase);

    public string StoreKind => "in-memory";

    public bool IsDurable => false;

    public string Ownership => "cephalon-managed";

    public IReadOnlyList<TenantInvitationDeliveryRetryDescriptor> Entries
    {
        get
        {
            lock (gate)
            {
                return Sort(entries.Values);
            }
        }
    }

    public int Count
    {
        get
        {
            lock (gate)
            {
                return entries.Count;
            }
        }
    }

    public TenantInvitationDeliveryRetryDescriptor? LatestEntry
    {
        get
        {
            lock (gate)
            {
                return entries.Values
                    .OrderByDescending(static entry => entry.LastAttemptAtUtc ?? entry.CreatedAtUtc)
                    .ThenByDescending(static entry => entry.NextAttemptAtUtc)
                    .FirstOrDefault();
            }
        }
    }

    public TenantInvitationDeliveryRetryDescriptor? GetById(string retryId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(retryId);

        lock (gate)
        {
            return entries.TryGetValue(retryId.Trim(), out var entry) ? entry : null;
        }
    }

    public IReadOnlyList<TenantInvitationDeliveryRetryDescriptor> GetPending(
        DateTimeOffset atUtc,
        int limit,
        bool dueOnly = true)
    {
        lock (gate)
        {
            return Sort(entries.Values)
                .Where(entry => string.Equals(entry.Status, TenantInvitationDeliveryRetryStatuses.Pending, StringComparison.OrdinalIgnoreCase))
                .Where(entry => !dueOnly || entry.NextAttemptAtUtc <= atUtc)
                .Take(Math.Max(1, limit))
                .ToArray();
        }
    }

    public void Upsert(TenantInvitationDeliveryRetryDescriptor entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        lock (gate)
        {
            entries[entry.RetryId] = entry;
        }
    }

    public bool Remove(string retryId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(retryId);

        lock (gate)
        {
            return entries.Remove(retryId.Trim());
        }
    }

    internal static TenantInvitationDeliveryRetryDescriptor[] Sort(IEnumerable<TenantInvitationDeliveryRetryDescriptor> source)
    {
        return source
            .OrderBy(static entry => entry.NextAttemptAtUtc)
            .ThenBy(static entry => entry.TenantId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static entry => entry.InvitationId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static entry => entry.Channel, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static entry => entry.SenderId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static entry => entry.RetryId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}

internal sealed class FileTenantInvitationDeliveryRetryQueue : ITenantInvitationDeliveryRetryStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly object gate = new();
    private readonly string path;

    public FileTenantInvitationDeliveryRetryQueue(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Invitation delivery retry queue path is required.", nameof(path));
        }

        this.path = Path.GetFullPath(path.Trim());
    }

    public string StoreKind => "file";

    public bool IsDurable => true;

    public string Ownership => "cephalon-managed";

    public IReadOnlyList<TenantInvitationDeliveryRetryDescriptor> Entries
    {
        get
        {
            lock (gate)
            {
                return InMemoryTenantInvitationDeliveryRetryQueue.Sort(LoadCore().Values);
            }
        }
    }

    public int Count
    {
        get
        {
            lock (gate)
            {
                return LoadCore().Count;
            }
        }
    }

    public TenantInvitationDeliveryRetryDescriptor? LatestEntry
    {
        get
        {
            lock (gate)
            {
                return LoadCore().Values
                    .OrderByDescending(static entry => entry.LastAttemptAtUtc ?? entry.CreatedAtUtc)
                    .ThenByDescending(static entry => entry.NextAttemptAtUtc)
                    .FirstOrDefault();
            }
        }
    }

    public TenantInvitationDeliveryRetryDescriptor? GetById(string retryId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(retryId);

        lock (gate)
        {
            return LoadCore().TryGetValue(retryId.Trim(), out var entry) ? entry : null;
        }
    }

    public IReadOnlyList<TenantInvitationDeliveryRetryDescriptor> GetPending(
        DateTimeOffset atUtc,
        int limit,
        bool dueOnly = true)
    {
        lock (gate)
        {
            return InMemoryTenantInvitationDeliveryRetryQueue.Sort(LoadCore().Values)
                .Where(entry => string.Equals(entry.Status, TenantInvitationDeliveryRetryStatuses.Pending, StringComparison.OrdinalIgnoreCase))
                .Where(entry => !dueOnly || entry.NextAttemptAtUtc <= atUtc)
                .Take(Math.Max(1, limit))
                .ToArray();
        }
    }

    public void Upsert(TenantInvitationDeliveryRetryDescriptor entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        lock (gate)
        {
            var entries = LoadCore();
            entries[entry.RetryId] = entry;
            SaveCore(entries.Values);
        }
    }

    public bool Remove(string retryId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(retryId);

        lock (gate)
        {
            var entries = LoadCore();
            var removed = entries.Remove(retryId.Trim());
            if (removed)
            {
                SaveCore(entries.Values);
            }

            return removed;
        }
    }

    private Dictionary<string, TenantInvitationDeliveryRetryDescriptor> LoadCore()
    {
        if (!File.Exists(path))
        {
            return new Dictionary<string, TenantInvitationDeliveryRetryDescriptor>(StringComparer.OrdinalIgnoreCase);
        }

        var json = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, TenantInvitationDeliveryRetryDescriptor>(StringComparer.OrdinalIgnoreCase);
        }

        var document = JsonSerializer.Deserialize<FileTenantInvitationDeliveryRetryQueueDocument>(json, JsonOptions) ??
            new FileTenantInvitationDeliveryRetryQueueDocument();

        var entries = new Dictionary<string, TenantInvitationDeliveryRetryDescriptor>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in (document.Entries ?? []).Select(static stored => stored.ToDescriptor()))
        {
            entries[entry.RetryId] = entry;
        }

        return entries;
    }

    private void SaveCore(IEnumerable<TenantInvitationDeliveryRetryDescriptor> entries)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var document = new FileTenantInvitationDeliveryRetryQueueDocument
        {
            Entries = InMemoryTenantInvitationDeliveryRetryQueue.Sort(entries)
                .Select(static entry => FileTenantInvitationDeliveryRetryQueueRecord.FromDescriptor(entry))
                .ToArray()
        };
        var json = JsonSerializer.Serialize(document, JsonOptions);
        var tempPath = $"{path}.{Guid.NewGuid():N}.tmp";
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, path, overwrite: true);
    }
}

internal sealed class FileTenantInvitationDeliveryRetryQueueDocument
{
    public IReadOnlyList<FileTenantInvitationDeliveryRetryQueueRecord> Entries { get; set; } = [];
}

internal sealed class FileTenantInvitationDeliveryRetryQueueRecord
{
    public string RetryId { get; set; } = string.Empty;

    public string TenantId { get; set; } = string.Empty;

    public string InvitationId { get; set; } = string.Empty;

    public string? Channel { get; set; }

    public string? SenderId { get; set; }

    public string? Source { get; set; }

    public string? Actor { get; set; }

    public string? CorrelationId { get; set; }

    public bool RecordDelivery { get; set; } = true;

    public string Status { get; set; } = TenantInvitationDeliveryRetryStatuses.Pending;

    public int AttemptCount { get; set; }

    public int MaxAttempts { get; set; } = 3;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset NextAttemptAtUtc { get; set; }

    public DateTimeOffset? LastAttemptAtUtc { get; set; }

    public string? LastOutcome { get; set; }

    public string? LastReason { get; set; }

    public IReadOnlyDictionary<string, string>? Metadata { get; set; }

    public static FileTenantInvitationDeliveryRetryQueueRecord FromDescriptor(TenantInvitationDeliveryRetryDescriptor descriptor)
    {
        return new FileTenantInvitationDeliveryRetryQueueRecord
        {
            RetryId = descriptor.RetryId,
            TenantId = descriptor.TenantId,
            InvitationId = descriptor.InvitationId,
            Channel = descriptor.Channel,
            SenderId = descriptor.SenderId,
            Source = descriptor.Source,
            Actor = descriptor.Actor,
            CorrelationId = descriptor.CorrelationId,
            RecordDelivery = descriptor.RecordDelivery,
            Status = descriptor.Status,
            AttemptCount = descriptor.AttemptCount,
            MaxAttempts = descriptor.MaxAttempts,
            CreatedAtUtc = descriptor.CreatedAtUtc,
            NextAttemptAtUtc = descriptor.NextAttemptAtUtc,
            LastAttemptAtUtc = descriptor.LastAttemptAtUtc,
            LastOutcome = descriptor.LastOutcome,
            LastReason = descriptor.LastReason,
            Metadata = descriptor.Metadata
        };
    }

    public TenantInvitationDeliveryRetryDescriptor ToDescriptor()
    {
        return new TenantInvitationDeliveryRetryDescriptor(
            RetryId,
            TenantId,
            InvitationId,
            Channel,
            SenderId,
            Source,
            Actor,
            CorrelationId,
            RecordDelivery,
            Status,
            AttemptCount,
            MaxAttempts,
            CreatedAtUtc,
            NextAttemptAtUtc,
            LastAttemptAtUtc,
            LastOutcome,
            LastReason,
            Metadata);
    }
}
