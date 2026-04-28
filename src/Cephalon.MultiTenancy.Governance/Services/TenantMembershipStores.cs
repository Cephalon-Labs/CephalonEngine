using Cephalon.MultiTenancy.Governance.Configuration;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cephalon.MultiTenancy.Governance.Services;

internal static class TenantMembershipStores
{
    public static ITenantMembershipStore Create(MultiTenancyGovernanceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return string.IsNullOrWhiteSpace(options.MembershipStoreFilePath)
            ? new InMemoryTenantMembershipStore()
            : new FileTenantMembershipStore(options.MembershipStoreFilePath);
    }
}

internal sealed class InMemoryTenantMembershipStore : ITenantMembershipStore
{
    private readonly object gate = new();
    private readonly Dictionary<string, TenantMembershipDescriptor> memberships = new(StringComparer.OrdinalIgnoreCase);

    public string StoreKind => "in-memory";

    public bool IsDurable => false;

    public string Ownership => "cephalon-managed";

    public IReadOnlyList<TenantMembershipDescriptor> Memberships
    {
        get
        {
            lock (gate)
            {
                return Sort(memberships.Values);
            }
        }
    }

    public int Count
    {
        get
        {
            lock (gate)
            {
                return memberships.Count;
            }
        }
    }

    public void Upsert(TenantMembershipDescriptor membership)
    {
        ArgumentNullException.ThrowIfNull(membership);

        lock (gate)
        {
            memberships[CreateKey(membership.TenantId, membership.PrincipalKind, membership.PrincipalId)] = membership;
        }
    }

    internal static string CreateKey(string tenantId, string principalKind, string principalId)
    {
        return $"{tenantId.Trim()}|{principalKind.Trim()}|{principalId.Trim()}";
    }

    internal static TenantMembershipDescriptor[] Sort(IEnumerable<TenantMembershipDescriptor> source)
    {
        return source
            .OrderBy(static membership => membership.TenantId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static membership => membership.PrincipalKind, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static membership => membership.PrincipalId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}

internal sealed class FileTenantMembershipStore : ITenantMembershipStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly object gate = new();
    private readonly string path;

    public FileTenantMembershipStore(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Membership store path is required.", nameof(path));
        }

        this.path = Path.GetFullPath(path.Trim());
    }

    public string StoreKind => "file";

    public bool IsDurable => true;

    public string Ownership => "cephalon-managed";

    public IReadOnlyList<TenantMembershipDescriptor> Memberships
    {
        get
        {
            lock (gate)
            {
                return InMemoryTenantMembershipStore.Sort(LoadCore().Values);
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

    public void Upsert(TenantMembershipDescriptor membership)
    {
        ArgumentNullException.ThrowIfNull(membership);

        lock (gate)
        {
            var memberships = LoadCore();
            memberships[InMemoryTenantMembershipStore.CreateKey(membership.TenantId, membership.PrincipalKind, membership.PrincipalId)] = membership;
            SaveCore(memberships.Values);
        }
    }

    private Dictionary<string, TenantMembershipDescriptor> LoadCore()
    {
        if (!File.Exists(path))
        {
            return new Dictionary<string, TenantMembershipDescriptor>(StringComparer.OrdinalIgnoreCase);
        }

        var json = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, TenantMembershipDescriptor>(StringComparer.OrdinalIgnoreCase);
        }

        var document = JsonSerializer.Deserialize<FileTenantMembershipStoreDocument>(json, JsonOptions) ??
            new FileTenantMembershipStoreDocument();

        var memberships = new Dictionary<string, TenantMembershipDescriptor>(StringComparer.OrdinalIgnoreCase);
        foreach (var membership in document.Memberships.Select(static stored => stored.ToDescriptor()))
        {
            memberships[InMemoryTenantMembershipStore.CreateKey(membership.TenantId, membership.PrincipalKind, membership.PrincipalId)] = membership;
        }

        return memberships;
    }

    private void SaveCore(IEnumerable<TenantMembershipDescriptor> memberships)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var document = new FileTenantMembershipStoreDocument
        {
            Memberships = InMemoryTenantMembershipStore.Sort(memberships)
                .Select(static membership => FileTenantMembershipStoreRecord.FromDescriptor(membership))
                .ToArray()
        };
        var json = JsonSerializer.Serialize(document, JsonOptions);
        var tempPath = $"{path}.{Guid.NewGuid():N}.tmp";
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, path, overwrite: true);
    }
}

internal sealed class FileTenantMembershipStoreDocument
{
    public IReadOnlyList<FileTenantMembershipStoreRecord> Memberships { get; set; } = [];
}

internal sealed class FileTenantMembershipStoreRecord
{
    public string TenantId { get; set; } = string.Empty;

    public string PrincipalId { get; set; } = string.Empty;

    public string PrincipalKind { get; set; } = "user";

    public string? DisplayName { get; set; }

    public IReadOnlyList<string>? Roles { get; set; }

    public string Status { get; set; } = TenantMembershipStatuses.Active;

    public DateTimeOffset? EffectiveFromUtc { get; set; }

    public DateTimeOffset? ExpiresAtUtc { get; set; }

    public string? SourceModuleId { get; set; }

    public IReadOnlyDictionary<string, string>? Metadata { get; set; }

    public static FileTenantMembershipStoreRecord FromDescriptor(TenantMembershipDescriptor descriptor)
    {
        return new FileTenantMembershipStoreRecord
        {
            TenantId = descriptor.TenantId,
            PrincipalId = descriptor.PrincipalId,
            PrincipalKind = descriptor.PrincipalKind,
            DisplayName = descriptor.DisplayName,
            Roles = descriptor.Roles,
            Status = descriptor.Status,
            EffectiveFromUtc = descriptor.EffectiveFromUtc,
            ExpiresAtUtc = descriptor.ExpiresAtUtc,
            SourceModuleId = descriptor.SourceModuleId,
            Metadata = descriptor.Metadata
        };
    }

    public TenantMembershipDescriptor ToDescriptor()
    {
        return new TenantMembershipDescriptor(
            tenantId: TenantId,
            principalId: PrincipalId,
            principalKind: PrincipalKind,
            displayName: DisplayName,
            roles: Roles,
            status: Status,
            effectiveFromUtc: EffectiveFromUtc,
            expiresAtUtc: ExpiresAtUtc,
            sourceModuleId: SourceModuleId,
            metadata: Metadata);
    }
}
