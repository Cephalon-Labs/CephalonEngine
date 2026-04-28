using Cephalon.MultiTenancy.Governance.Configuration;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cephalon.MultiTenancy.Governance.Services;

internal static class TenantInvitationStores
{
    public static ITenantInvitationStore Create(MultiTenancyGovernanceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return string.IsNullOrWhiteSpace(options.InvitationStoreFilePath)
            ? new InMemoryTenantInvitationStore()
            : new FileTenantInvitationStore(options.InvitationStoreFilePath);
    }
}

internal sealed class InMemoryTenantInvitationStore : ITenantInvitationStore
{
    private readonly object gate = new();
    private readonly Dictionary<string, TenantInvitationDescriptor> invitations = new(StringComparer.OrdinalIgnoreCase);

    public string StoreKind => "in-memory";

    public bool IsDurable => false;

    public string Ownership => "cephalon-managed";

    public IReadOnlyList<TenantInvitationDescriptor> Invitations
    {
        get
        {
            lock (gate)
            {
                return Sort(invitations.Values);
            }
        }
    }

    public int Count
    {
        get
        {
            lock (gate)
            {
                return invitations.Count;
            }
        }
    }

    public void Upsert(TenantInvitationDescriptor invitation)
    {
        ArgumentNullException.ThrowIfNull(invitation);

        lock (gate)
        {
            invitations[CreateKey(invitation.TenantId, invitation.InvitationId)] = invitation;
        }
    }

    internal static string CreateKey(string tenantId, string invitationId)
    {
        return $"{tenantId.Trim()}|{invitationId.Trim()}";
    }

    internal static TenantInvitationDescriptor[] Sort(IEnumerable<TenantInvitationDescriptor> source)
    {
        return source
            .OrderBy(static invitation => invitation.TenantId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static invitation => invitation.InvitationId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}

internal sealed class FileTenantInvitationStore : ITenantInvitationStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly object gate = new();
    private readonly string path;

    public FileTenantInvitationStore(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Invitation store path is required.", nameof(path));
        }

        this.path = Path.GetFullPath(path.Trim());
    }

    public string StoreKind => "file";

    public bool IsDurable => true;

    public string Ownership => "cephalon-managed";

    public IReadOnlyList<TenantInvitationDescriptor> Invitations
    {
        get
        {
            lock (gate)
            {
                return InMemoryTenantInvitationStore.Sort(LoadCore().Values);
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

    public void Upsert(TenantInvitationDescriptor invitation)
    {
        ArgumentNullException.ThrowIfNull(invitation);

        lock (gate)
        {
            var invitations = LoadCore();
            invitations[InMemoryTenantInvitationStore.CreateKey(invitation.TenantId, invitation.InvitationId)] = invitation;
            SaveCore(invitations.Values);
        }
    }

    private Dictionary<string, TenantInvitationDescriptor> LoadCore()
    {
        if (!File.Exists(path))
        {
            return new Dictionary<string, TenantInvitationDescriptor>(StringComparer.OrdinalIgnoreCase);
        }

        var json = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, TenantInvitationDescriptor>(StringComparer.OrdinalIgnoreCase);
        }

        var document = JsonSerializer.Deserialize<FileTenantInvitationStoreDocument>(json, JsonOptions) ??
            new FileTenantInvitationStoreDocument();

        var invitations = new Dictionary<string, TenantInvitationDescriptor>(StringComparer.OrdinalIgnoreCase);
        foreach (var invitation in document.Invitations.Select(static stored => stored.ToDescriptor()))
        {
            invitations[InMemoryTenantInvitationStore.CreateKey(invitation.TenantId, invitation.InvitationId)] = invitation;
        }

        return invitations;
    }

    private void SaveCore(IEnumerable<TenantInvitationDescriptor> invitations)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var document = new FileTenantInvitationStoreDocument
        {
            Invitations = InMemoryTenantInvitationStore.Sort(invitations)
                .Select(static invitation => FileTenantInvitationStoreRecord.FromDescriptor(invitation))
                .ToArray()
        };
        var json = JsonSerializer.Serialize(document, JsonOptions);
        var tempPath = $"{path}.{Guid.NewGuid():N}.tmp";
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, path, overwrite: true);
    }
}

internal sealed class FileTenantInvitationStoreDocument
{
    public IReadOnlyList<FileTenantInvitationStoreRecord> Invitations { get; set; } = [];
}

internal sealed class FileTenantInvitationStoreRecord
{
    public string InvitationId { get; set; } = string.Empty;

    public string TenantId { get; set; } = string.Empty;

    public string InviteeId { get; set; } = string.Empty;

    public string InviteeKind { get; set; } = "user";

    public string? DisplayName { get; set; }

    public IReadOnlyList<string>? Roles { get; set; }

    public string Status { get; set; } = TenantInvitationStatuses.Pending;

    public DateTimeOffset? CreatedAtUtc { get; set; }

    public DateTimeOffset? ExpiresAtUtc { get; set; }

    public string? SourceModuleId { get; set; }

    public IReadOnlyDictionary<string, string>? Metadata { get; set; }

    public static FileTenantInvitationStoreRecord FromDescriptor(TenantInvitationDescriptor descriptor)
    {
        return new FileTenantInvitationStoreRecord
        {
            InvitationId = descriptor.InvitationId,
            TenantId = descriptor.TenantId,
            InviteeId = descriptor.InviteeId,
            InviteeKind = descriptor.InviteeKind,
            DisplayName = descriptor.DisplayName,
            Roles = descriptor.Roles,
            Status = descriptor.Status,
            CreatedAtUtc = descriptor.CreatedAtUtc,
            ExpiresAtUtc = descriptor.ExpiresAtUtc,
            SourceModuleId = descriptor.SourceModuleId,
            Metadata = descriptor.Metadata
        };
    }

    public TenantInvitationDescriptor ToDescriptor()
    {
        return new TenantInvitationDescriptor(
            invitationId: InvitationId,
            tenantId: TenantId,
            inviteeId: InviteeId,
            inviteeKind: InviteeKind,
            displayName: DisplayName,
            roles: Roles,
            status: Status,
            createdAtUtc: CreatedAtUtc,
            expiresAtUtc: ExpiresAtUtc,
            sourceModuleId: SourceModuleId,
            metadata: Metadata);
    }
}
