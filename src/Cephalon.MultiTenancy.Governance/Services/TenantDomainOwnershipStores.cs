using Cephalon.MultiTenancy.Governance.Configuration;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cephalon.MultiTenancy.Governance.Services;

internal static class TenantDomainOwnershipStores
{
    public static ITenantDomainOwnershipStore Create(MultiTenancyGovernanceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return string.IsNullOrWhiteSpace(options.DomainOwnershipStoreFilePath)
            ? new InMemoryTenantDomainOwnershipStore()
            : new FileTenantDomainOwnershipStore(options.DomainOwnershipStoreFilePath);
    }
}

internal sealed class InMemoryTenantDomainOwnershipStore : ITenantDomainOwnershipStore
{
    private readonly object gate = new();
    private readonly Dictionary<string, TenantDomainOwnershipDescriptor> domainOwnerships = new(StringComparer.OrdinalIgnoreCase);

    public string StoreKind => "in-memory";

    public bool IsDurable => false;

    public string Ownership => "cephalon-managed";

    public IReadOnlyList<TenantDomainOwnershipDescriptor> DomainOwnerships
    {
        get
        {
            lock (gate)
            {
                return Sort(domainOwnerships.Values);
            }
        }
    }

    public int Count
    {
        get
        {
            lock (gate)
            {
                return domainOwnerships.Count;
            }
        }
    }

    public void Upsert(TenantDomainOwnershipDescriptor domainOwnership)
    {
        ArgumentNullException.ThrowIfNull(domainOwnership);

        lock (gate)
        {
            domainOwnerships[CreateKey(domainOwnership.TenantId, domainOwnership.DomainName)] = domainOwnership;
        }
    }

    internal static string CreateKey(string tenantId, string domainName)
    {
        return $"{tenantId.Trim()}|{TenantDomainOwnershipDescriptor.NormalizeDomainName(domainName)}";
    }

    internal static TenantDomainOwnershipDescriptor[] Sort(IEnumerable<TenantDomainOwnershipDescriptor> source)
    {
        return source
            .OrderBy(static domainOwnership => domainOwnership.TenantId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static domainOwnership => domainOwnership.DomainName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}

internal sealed class FileTenantDomainOwnershipStore : ITenantDomainOwnershipStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly object gate = new();
    private readonly string path;

    public FileTenantDomainOwnershipStore(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Domain ownership store path is required.", nameof(path));
        }

        this.path = Path.GetFullPath(path.Trim());
    }

    public string StoreKind => "file";

    public bool IsDurable => true;

    public string Ownership => "cephalon-managed";

    public IReadOnlyList<TenantDomainOwnershipDescriptor> DomainOwnerships
    {
        get
        {
            lock (gate)
            {
                return InMemoryTenantDomainOwnershipStore.Sort(LoadCore().Values);
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

    public void Upsert(TenantDomainOwnershipDescriptor domainOwnership)
    {
        ArgumentNullException.ThrowIfNull(domainOwnership);

        lock (gate)
        {
            var domainOwnerships = LoadCore();
            domainOwnerships[InMemoryTenantDomainOwnershipStore.CreateKey(domainOwnership.TenantId, domainOwnership.DomainName)] = domainOwnership;
            SaveCore(domainOwnerships.Values);
        }
    }

    private Dictionary<string, TenantDomainOwnershipDescriptor> LoadCore()
    {
        if (!File.Exists(path))
        {
            return new Dictionary<string, TenantDomainOwnershipDescriptor>(StringComparer.OrdinalIgnoreCase);
        }

        var json = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, TenantDomainOwnershipDescriptor>(StringComparer.OrdinalIgnoreCase);
        }

        var document = JsonSerializer.Deserialize<FileTenantDomainOwnershipStoreDocument>(json, JsonOptions) ??
            new FileTenantDomainOwnershipStoreDocument();

        var domainOwnerships = new Dictionary<string, TenantDomainOwnershipDescriptor>(StringComparer.OrdinalIgnoreCase);
        foreach (var domainOwnership in document.DomainOwnerships.Select(static stored => stored.ToDescriptor()))
        {
            domainOwnerships[InMemoryTenantDomainOwnershipStore.CreateKey(domainOwnership.TenantId, domainOwnership.DomainName)] = domainOwnership;
        }

        return domainOwnerships;
    }

    private void SaveCore(IEnumerable<TenantDomainOwnershipDescriptor> domainOwnerships)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var document = new FileTenantDomainOwnershipStoreDocument
        {
            DomainOwnerships = InMemoryTenantDomainOwnershipStore.Sort(domainOwnerships)
                .Select(static domainOwnership => FileTenantDomainOwnershipStoreRecord.FromDescriptor(domainOwnership))
                .ToArray()
        };
        var json = JsonSerializer.Serialize(document, JsonOptions);
        var tempPath = $"{path}.{Guid.NewGuid():N}.tmp";
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, path, overwrite: true);
    }
}

internal sealed class FileTenantDomainOwnershipStoreDocument
{
    public IReadOnlyList<FileTenantDomainOwnershipStoreRecord> DomainOwnerships { get; set; } = [];
}

internal sealed class FileTenantDomainOwnershipStoreRecord
{
    public string TenantId { get; set; } = string.Empty;

    public string DomainName { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public string Status { get; set; } = TenantDomainOwnershipStatuses.Pending;

    public string VerificationMethod { get; set; } = TenantDomainVerificationMethods.Manual;

    public DateTimeOffset? VerifiedAtUtc { get; set; }

    public DateTimeOffset? ExpiresAtUtc { get; set; }

    public string? SourceModuleId { get; set; }

    public IReadOnlyDictionary<string, string>? Metadata { get; set; }

    public static FileTenantDomainOwnershipStoreRecord FromDescriptor(TenantDomainOwnershipDescriptor descriptor)
    {
        return new FileTenantDomainOwnershipStoreRecord
        {
            TenantId = descriptor.TenantId,
            DomainName = descriptor.DomainName,
            DisplayName = descriptor.DisplayName,
            Status = descriptor.Status,
            VerificationMethod = descriptor.VerificationMethod,
            VerifiedAtUtc = descriptor.VerifiedAtUtc,
            ExpiresAtUtc = descriptor.ExpiresAtUtc,
            SourceModuleId = descriptor.SourceModuleId,
            Metadata = descriptor.Metadata
        };
    }

    public TenantDomainOwnershipDescriptor ToDescriptor()
    {
        return new TenantDomainOwnershipDescriptor(
            tenantId: TenantId,
            domainName: DomainName,
            displayName: DisplayName,
            status: Status,
            verificationMethod: VerificationMethod,
            verifiedAtUtc: VerifiedAtUtc,
            expiresAtUtc: ExpiresAtUtc,
            sourceModuleId: SourceModuleId,
            metadata: Metadata);
    }
}
