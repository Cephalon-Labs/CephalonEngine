using Cephalon.MultiTenancy.Governance.Configuration;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cephalon.MultiTenancy.Governance.Services;

internal static class TenantGovernanceActionStores
{
    public static ITenantGovernanceActionStore Create(MultiTenancyGovernanceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return string.IsNullOrWhiteSpace(options.GovernanceActionStoreFilePath)
            ? new InMemoryTenantGovernanceActionStore()
            : new FileTenantGovernanceActionStore(options.GovernanceActionStoreFilePath);
    }
}

internal sealed class InMemoryTenantGovernanceActionStore : ITenantGovernanceActionStore
{
    private readonly object gate = new();
    private readonly Dictionary<string, TenantGovernanceActionDescriptor> actions = new(StringComparer.OrdinalIgnoreCase);

    public string StoreKind => "in-memory";

    public bool IsDurable => false;

    public string Ownership => "cephalon-managed";

    public IReadOnlyList<TenantGovernanceActionDescriptor> Actions
    {
        get
        {
            lock (gate)
            {
                return Sort(actions.Values);
            }
        }
    }

    public int Count
    {
        get
        {
            lock (gate)
            {
                return actions.Count;
            }
        }
    }

    public void Upsert(TenantGovernanceActionDescriptor action)
    {
        ArgumentNullException.ThrowIfNull(action);

        lock (gate)
        {
            actions[CreateKey(action.TenantId, action.ActionId)] = action;
        }
    }

    internal static string CreateKey(string tenantId, string actionId)
    {
        return $"{tenantId.Trim()}|{actionId.Trim()}";
    }

    internal static TenantGovernanceActionDescriptor[] Sort(IEnumerable<TenantGovernanceActionDescriptor> source)
    {
        return source
            .OrderBy(static action => action.TenantId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static action => action.ActionId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}

internal sealed class FileTenantGovernanceActionStore : ITenantGovernanceActionStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly object gate = new();
    private readonly string path;

    public FileTenantGovernanceActionStore(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Governance action store path is required.", nameof(path));
        }

        this.path = Path.GetFullPath(path.Trim());
    }

    public string StoreKind => "file";

    public bool IsDurable => true;

    public string Ownership => "cephalon-managed";

    public IReadOnlyList<TenantGovernanceActionDescriptor> Actions
    {
        get
        {
            lock (gate)
            {
                return InMemoryTenantGovernanceActionStore.Sort(LoadCore().Values);
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

    public void Upsert(TenantGovernanceActionDescriptor action)
    {
        ArgumentNullException.ThrowIfNull(action);

        lock (gate)
        {
            var actions = LoadCore();
            actions[InMemoryTenantGovernanceActionStore.CreateKey(action.TenantId, action.ActionId)] = action;
            SaveCore(actions.Values);
        }
    }

    private Dictionary<string, TenantGovernanceActionDescriptor> LoadCore()
    {
        if (!File.Exists(path))
        {
            return new Dictionary<string, TenantGovernanceActionDescriptor>(StringComparer.OrdinalIgnoreCase);
        }

        var json = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, TenantGovernanceActionDescriptor>(StringComparer.OrdinalIgnoreCase);
        }

        var document = JsonSerializer.Deserialize<FileTenantGovernanceActionStoreDocument>(json, JsonOptions) ??
            new FileTenantGovernanceActionStoreDocument();

        var actions = new Dictionary<string, TenantGovernanceActionDescriptor>(StringComparer.OrdinalIgnoreCase);
        foreach (var action in document.Actions.Select(static stored => stored.ToDescriptor()))
        {
            actions[InMemoryTenantGovernanceActionStore.CreateKey(action.TenantId, action.ActionId)] = action;
        }

        return actions;
    }

    private void SaveCore(IEnumerable<TenantGovernanceActionDescriptor> actions)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var document = new FileTenantGovernanceActionStoreDocument
        {
            Actions = InMemoryTenantGovernanceActionStore.Sort(actions)
                .Select(static action => FileTenantGovernanceActionStoreRecord.FromDescriptor(action))
                .ToArray()
        };
        var json = JsonSerializer.Serialize(document, JsonOptions);
        var tempPath = $"{path}.{Guid.NewGuid():N}.tmp";
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, path, overwrite: true);
    }
}

internal sealed class FileTenantGovernanceActionStoreDocument
{
    public IReadOnlyList<FileTenantGovernanceActionStoreRecord> Actions { get; set; } = [];
}

internal sealed class FileTenantGovernanceActionStoreRecord
{
    public string ActionId { get; set; } = string.Empty;

    public string TenantId { get; set; } = string.Empty;

    public string ActionKind { get; set; } = string.Empty;

    public string? SubjectKind { get; set; }

    public string? SubjectId { get; set; }

    public string? DisplayName { get; set; }

    public string Status { get; set; } = TenantGovernanceActionStatuses.PendingApproval;

    public string? RequestedBy { get; set; }

    public string? ApprovedBy { get; set; }

    public DateTimeOffset? CreatedAtUtc { get; set; }

    public DateTimeOffset? DecidedAtUtc { get; set; }

    public DateTimeOffset? ExpiresAtUtc { get; set; }

    public string? SourceModuleId { get; set; }

    public IReadOnlyDictionary<string, string>? Metadata { get; set; }

    public static FileTenantGovernanceActionStoreRecord FromDescriptor(TenantGovernanceActionDescriptor descriptor)
    {
        return new FileTenantGovernanceActionStoreRecord
        {
            ActionId = descriptor.ActionId,
            TenantId = descriptor.TenantId,
            ActionKind = descriptor.ActionKind,
            SubjectKind = descriptor.SubjectKind,
            SubjectId = descriptor.SubjectId,
            DisplayName = descriptor.DisplayName,
            Status = descriptor.Status,
            RequestedBy = descriptor.RequestedBy,
            ApprovedBy = descriptor.ApprovedBy,
            CreatedAtUtc = descriptor.CreatedAtUtc,
            DecidedAtUtc = descriptor.DecidedAtUtc,
            ExpiresAtUtc = descriptor.ExpiresAtUtc,
            SourceModuleId = descriptor.SourceModuleId,
            Metadata = descriptor.Metadata
        };
    }

    public TenantGovernanceActionDescriptor ToDescriptor()
    {
        return new TenantGovernanceActionDescriptor(
            actionId: ActionId,
            tenantId: TenantId,
            actionKind: ActionKind,
            subjectKind: SubjectKind,
            subjectId: SubjectId,
            displayName: DisplayName,
            status: Status,
            requestedBy: RequestedBy,
            approvedBy: ApprovedBy,
            createdAtUtc: CreatedAtUtc,
            decidedAtUtc: DecidedAtUtc,
            expiresAtUtc: ExpiresAtUtc,
            sourceModuleId: SourceModuleId,
            metadata: Metadata);
    }
}
