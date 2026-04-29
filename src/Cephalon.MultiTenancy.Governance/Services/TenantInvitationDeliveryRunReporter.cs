using Cephalon.MultiTenancy.Governance.Configuration;

namespace Cephalon.MultiTenancy.Governance.Services;

internal sealed class TenantInvitationDeliveryRunReporter(
    MultiTenancyGovernanceOptions options) : ITenantInvitationDeliveryRunCatalog
{
    private readonly object gate = new();
    private readonly List<TenantInvitationDeliveryRunDescriptor> runs = [];

    public IReadOnlyList<TenantInvitationDeliveryRunDescriptor> Runs
    {
        get
        {
            lock (gate)
            {
                return runs.ToArray();
            }
        }
    }

    public TenantInvitationDeliveryRunDescriptor? LatestRun
    {
        get
        {
            lock (gate)
            {
                return runs.Count == 0 ? null : runs[^1];
            }
        }
    }

    public int Count
    {
        get
        {
            lock (gate)
            {
                return runs.Count;
            }
        }
    }

    public IReadOnlyList<TenantInvitationDeliveryRunDescriptor> GetByTenantId(string tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        lock (gate)
        {
            return runs
                .Where(run => string.Equals(run.TenantId, tenantId.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }
    }

    public IReadOnlyList<TenantInvitationDeliveryRunDescriptor> GetByInvitationId(string invitationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(invitationId);

        lock (gate)
        {
            return runs
                .Where(run => string.Equals(run.InvitationId, invitationId.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }
    }

    public void Record(TenantInvitationDeliveryResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        lock (gate)
        {
            runs.Add(new TenantInvitationDeliveryRunDescriptor(
                result.TenantId,
                result.InvitationId,
                result.Outcome,
                result.Dispatched,
                result.Recorded,
                result.DispatchedAtUtc,
                result.Channel,
                result.SenderId,
                result.ProviderMessageId,
                result.Reason,
                result.Metadata));

            var historyLimit = Math.Max(1, options.InvitationDeliveryRunHistoryLimit);
            if (runs.Count > historyLimit)
            {
                runs.RemoveRange(0, runs.Count - historyLimit);
            }
        }
    }
}
