namespace Cephalon.Eventing.Services;

/// <summary>
/// Provides read access to provider-neutral event context policy descriptors.
/// </summary>
public interface IEventContextPolicyCatalog
{
    /// <summary>
    /// Gets all context policy descriptors available to the active eventing runtime.
    /// </summary>
    IReadOnlyList<EventContextPolicyDescriptor> Policies { get; }

    /// <summary>
    /// Gets the context policies that cover a specific message-header name.
    /// </summary>
    /// <param name="headerName">The message-header name to match.</param>
    /// <returns>The matching context policies, or an empty list when no policy covers the header.</returns>
    IReadOnlyList<EventContextPolicyDescriptor> GetByHeaderName(string headerName);

    /// <summary>
    /// Tries to get a context policy by identifier.
    /// </summary>
    /// <param name="policyId">The context policy identifier.</param>
    /// <param name="policy">When this method returns, contains the matched context policy.</param>
    /// <returns><c>true</c> when the context policy was found; otherwise <c>false</c>.</returns>
    bool TryGet(string policyId, out EventContextPolicyDescriptor policy);
}
