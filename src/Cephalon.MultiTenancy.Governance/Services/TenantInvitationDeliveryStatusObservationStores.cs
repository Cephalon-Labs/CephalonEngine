using Cephalon.MultiTenancy.Governance.Configuration;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cephalon.MultiTenancy.Governance.Services;

internal static class TenantInvitationDeliveryStatusObservationStores
{
    public static ITenantInvitationDeliveryStatusObservationStore Create(MultiTenancyGovernanceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var historyLimit = ResolveHistoryLimit(options);
        return string.IsNullOrWhiteSpace(options.InvitationDeliveryStatusObservationStoreFilePath)
            ? new InMemoryTenantInvitationDeliveryStatusObservationStore(historyLimit)
            : new FileTenantInvitationDeliveryStatusObservationStore(
                options.InvitationDeliveryStatusObservationStoreFilePath,
                historyLimit);
    }

    public static int ResolveHistoryLimit(MultiTenancyGovernanceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return Math.Clamp(options.InvitationDeliveryStatusObservationHistoryLimit, 1, 1_000_000);
    }
}

internal sealed class InMemoryTenantInvitationDeliveryStatusObservationStore(int historyLimit)
    : ITenantInvitationDeliveryStatusObservationStore
{
    private readonly object gate = new();
    private readonly Dictionary<string, TenantInvitationDeliveryStatusObservationDescriptor> observations =
        new(StringComparer.OrdinalIgnoreCase);

    public string StoreKind => "in-memory";

    public bool IsDurable => false;

    public string Ownership => "cephalon-managed";

    public IReadOnlyList<TenantInvitationDeliveryStatusObservationDescriptor> Observations
    {
        get
        {
            lock (gate)
            {
                return Sort(observations.Values);
            }
        }
    }

    public int Count
    {
        get
        {
            lock (gate)
            {
                return observations.Count;
            }
        }
    }

    public void Upsert(TenantInvitationDeliveryStatusObservationDescriptor observation)
    {
        ArgumentNullException.ThrowIfNull(observation);

        lock (gate)
        {
            observations[observation.ObservationId] = observation;
            TrimToHistoryLimit(observations, historyLimit);
        }
    }

    internal static TenantInvitationDeliveryStatusObservationDescriptor[] Sort(
        IEnumerable<TenantInvitationDeliveryStatusObservationDescriptor> source)
    {
        return source
            .OrderBy(static observation => observation.TenantId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static observation => observation.InvitationId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static observation => observation.ObservedAtUtc)
            .ThenBy(static observation => observation.ObservationId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    internal static void TrimToHistoryLimit(
        Dictionary<string, TenantInvitationDeliveryStatusObservationDescriptor> observations,
        int historyLimit)
    {
        var limit = Math.Max(1, historyLimit);
        if (observations.Count <= limit)
        {
            return;
        }

        var removableIds = observations.Values
            .OrderBy(static observation => observation.ObservedAtUtc)
            .ThenBy(static observation => observation.RecordedAtUtc)
            .ThenBy(static observation => observation.ObservationId, StringComparer.OrdinalIgnoreCase)
            .Take(observations.Count - limit)
            .Select(static observation => observation.ObservationId)
            .ToArray();

        foreach (var observationId in removableIds)
        {
            observations.Remove(observationId);
        }
    }
}

internal sealed class FileTenantInvitationDeliveryStatusObservationStore : ITenantInvitationDeliveryStatusObservationStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly object gate = new();
    private readonly string path;
    private readonly int historyLimit;

    public FileTenantInvitationDeliveryStatusObservationStore(string path, int historyLimit)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Delivery status observation store path is required.", nameof(path));
        }

        this.path = Path.GetFullPath(path.Trim());
        this.historyLimit = Math.Max(1, historyLimit);
    }

    public string StoreKind => "file";

    public bool IsDurable => true;

    public string Ownership => "cephalon-managed";

    public IReadOnlyList<TenantInvitationDeliveryStatusObservationDescriptor> Observations
    {
        get
        {
            lock (gate)
            {
                return InMemoryTenantInvitationDeliveryStatusObservationStore.Sort(LoadCore().Values);
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

    public void Upsert(TenantInvitationDeliveryStatusObservationDescriptor observation)
    {
        ArgumentNullException.ThrowIfNull(observation);

        lock (gate)
        {
            var observations = LoadCore();
            observations[observation.ObservationId] = observation;
            InMemoryTenantInvitationDeliveryStatusObservationStore.TrimToHistoryLimit(observations, historyLimit);
            SaveCore(observations.Values);
        }
    }

    private Dictionary<string, TenantInvitationDeliveryStatusObservationDescriptor> LoadCore()
    {
        if (!File.Exists(path))
        {
            return new Dictionary<string, TenantInvitationDeliveryStatusObservationDescriptor>(StringComparer.OrdinalIgnoreCase);
        }

        var json = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, TenantInvitationDeliveryStatusObservationDescriptor>(StringComparer.OrdinalIgnoreCase);
        }

        var document = JsonSerializer.Deserialize<FileTenantInvitationDeliveryStatusObservationStoreDocument>(json, JsonOptions) ??
            new FileTenantInvitationDeliveryStatusObservationStoreDocument();

        var observations = new Dictionary<string, TenantInvitationDeliveryStatusObservationDescriptor>(StringComparer.OrdinalIgnoreCase);
        foreach (var observation in document.Observations.Select(static stored => stored.ToDescriptor()))
        {
            observations[observation.ObservationId] = observation;
        }

        return observations;
    }

    private void SaveCore(IEnumerable<TenantInvitationDeliveryStatusObservationDescriptor> observations)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var document = new FileTenantInvitationDeliveryStatusObservationStoreDocument
        {
            Observations = InMemoryTenantInvitationDeliveryStatusObservationStore.Sort(observations)
                .Select(static observation => FileTenantInvitationDeliveryStatusObservationStoreRecord.FromDescriptor(observation))
                .ToArray()
        };
        var json = JsonSerializer.Serialize(document, JsonOptions);
        var tempPath = $"{path}.{Guid.NewGuid():N}.tmp";
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, path, overwrite: true);
    }
}

internal sealed class FileTenantInvitationDeliveryStatusObservationStoreDocument
{
    public IReadOnlyList<FileTenantInvitationDeliveryStatusObservationStoreRecord> Observations { get; set; } = [];
}

internal sealed class FileTenantInvitationDeliveryStatusObservationStoreRecord
{
    public string ObservationId { get; set; } = string.Empty;

    public string TenantId { get; set; } = string.Empty;

    public string InvitationId { get; set; } = string.Empty;

    public string Status { get; set; } = TenantInvitationDeliveryStatuses.Delivered;

    public string Outcome { get; set; } = TenantInvitationDeliveryStatusReconciliationOutcomes.Reconciled;

    public bool Reconciled { get; set; }

    public bool Recorded { get; set; }

    public DateTimeOffset ObservedAtUtc { get; set; }

    public DateTimeOffset RecordedAtUtc { get; set; }

    public string? ProviderMessageId { get; set; }

    public string? SenderId { get; set; }

    public string? Channel { get; set; }

    public string? Source { get; set; }

    public string? Actor { get; set; }

    public string? CorrelationId { get; set; }

    public string? Reason { get; set; }

    public IReadOnlyDictionary<string, string>? Metadata { get; set; }

    public static FileTenantInvitationDeliveryStatusObservationStoreRecord FromDescriptor(
        TenantInvitationDeliveryStatusObservationDescriptor descriptor)
    {
        return new FileTenantInvitationDeliveryStatusObservationStoreRecord
        {
            ObservationId = descriptor.ObservationId,
            TenantId = descriptor.TenantId,
            InvitationId = descriptor.InvitationId,
            Status = descriptor.Status,
            Outcome = descriptor.Outcome,
            Reconciled = descriptor.Reconciled,
            Recorded = descriptor.Recorded,
            ObservedAtUtc = descriptor.ObservedAtUtc,
            RecordedAtUtc = descriptor.RecordedAtUtc,
            ProviderMessageId = descriptor.ProviderMessageId,
            SenderId = descriptor.SenderId,
            Channel = descriptor.Channel,
            Source = descriptor.Source,
            Actor = descriptor.Actor,
            CorrelationId = descriptor.CorrelationId,
            Reason = descriptor.Reason,
            Metadata = descriptor.Metadata
        };
    }

    public TenantInvitationDeliveryStatusObservationDescriptor ToDescriptor()
    {
        return new TenantInvitationDeliveryStatusObservationDescriptor(
            observationId: ObservationId,
            tenantId: TenantId,
            invitationId: InvitationId,
            status: Status,
            outcome: Outcome,
            reconciled: Reconciled,
            recorded: Recorded,
            observedAtUtc: ObservedAtUtc,
            recordedAtUtc: RecordedAtUtc,
            providerMessageId: ProviderMessageId,
            senderId: SenderId,
            channel: Channel,
            source: Source,
            actor: Actor,
            correlationId: CorrelationId,
            reason: Reason,
            metadata: Metadata);
    }
}
