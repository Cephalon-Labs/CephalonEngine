using Cephalon.MultiTenancy.Governance.Services;

namespace Cephalon.MultiTenancy.Governance.AspNetCore.Hosting;

/// <summary>
/// Describes a bounded read of normalized tenant-invitation delivery status observations.
/// </summary>
/// <remarks>
/// The result is an operator/audit view over <see cref="ITenantInvitationDeliveryStatusObservationStore" />. It does
/// not represent a provider-specific callback inbox, provider polling state, or distributed replay ledger.
/// </remarks>
public sealed class TenantInvitationDeliveryStatusObservationQueryResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TenantInvitationDeliveryStatusObservationQueryResult" /> class.
    /// </summary>
    public TenantInvitationDeliveryStatusObservationQueryResult()
    {
    }

    /// <summary>
    /// Gets the observation store kind, such as <c>in-memory</c> or <c>file</c>.
    /// </summary>
    public string StoreKind { get; init; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether the underlying observation store survives process restarts.
    /// </summary>
    public bool IsDurable { get; init; }

    /// <summary>
    /// Gets the ownership mode reported by the underlying observation store.
    /// </summary>
    public string Ownership { get; init; } = string.Empty;

    /// <summary>
    /// Gets the number of observations in the store before endpoint filters are applied.
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Gets the number of observations that matched the supplied endpoint filters before the response limit was applied.
    /// </summary>
    public int MatchedCount { get; init; }

    /// <summary>
    /// Gets the number of observations included in this response after filtering and limiting.
    /// </summary>
    public int ReturnedCount { get; init; }

    /// <summary>
    /// Gets the number of aggregate summary buckets derived from the filtered observations.
    /// </summary>
    public int SummaryCount { get; init; }

    /// <summary>
    /// Gets the effective response limit used for this read.
    /// </summary>
    public int Limit { get; init; }

    /// <summary>
    /// Gets the normalized filters applied to this read.
    /// </summary>
    public IReadOnlyDictionary<string, string> Filters { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the normalized delivery status observations returned by this read.
    /// </summary>
    public IReadOnlyList<TenantInvitationDeliveryStatusObservationDescriptor> Observations { get; init; } = [];

    /// <summary>
    /// Gets aggregate operator summaries derived from the filtered observations before the response limit is applied.
    /// </summary>
    public IReadOnlyList<TenantInvitationDeliveryStatusObservationSummaryDescriptor> Summaries { get; init; } = [];
}
