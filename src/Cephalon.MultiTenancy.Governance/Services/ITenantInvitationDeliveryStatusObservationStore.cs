namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Stores tenant invitation delivery status observations recorded by the governance reconciler.
/// </summary>
/// <remarks>
/// The store is host-agnostic and records normalized reconciliation observations only. It does not translate
/// provider-specific callback payloads, verify provider-specific signatures, poll delivery providers, or provide
/// distributed exactly-once delivery semantics.
/// </remarks>
public interface ITenantInvitationDeliveryStatusObservationStore
{
    /// <summary>
    /// Gets the store kind, such as <c>in-memory</c> or <c>file</c>.
    /// </summary>
    string StoreKind { get; }

    /// <summary>
    /// Gets a value indicating whether the observation store survives process restarts.
    /// </summary>
    bool IsDurable { get; }

    /// <summary>
    /// Gets the ownership mode for the observation store.
    /// </summary>
    string Ownership { get; }

    /// <summary>
    /// Gets the recorded delivery status observations.
    /// </summary>
    IReadOnlyList<TenantInvitationDeliveryStatusObservationDescriptor> Observations { get; }

    /// <summary>
    /// Gets the number of recorded delivery status observations.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Records or replaces a delivery status observation.
    /// </summary>
    /// <param name="observation">The observation to record.</param>
    void Upsert(TenantInvitationDeliveryStatusObservationDescriptor observation);
}
