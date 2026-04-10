namespace Cephalon.Abstractions.Resilience;

/// <summary>
/// Exposes the active HTTP rate-limiting policies visible to the current runtime.
/// </summary>
/// <remarks>
/// Implementations describe the effective policy applied by the active host adapter, such as
/// ASP.NET Core middleware-based request limiting. This surface is runtime-facing rather than
/// app-model-facing because it reflects what the host actually enforces after defaults and host
/// exclusions have been applied.
/// </remarks>
public interface IRateLimitingRuntimeCatalog
{
    /// <summary>
    /// Gets all rate-limiting policies visible to the current runtime.
    /// </summary>
    IReadOnlyList<RateLimitingRuntimeDescriptor> Policies { get; }

    /// <summary>
    /// Gets one rate-limiting policy by its stable identifier.
    /// </summary>
    /// <param name="policyId">The policy identifier to resolve.</param>
    /// <returns>The matching policy descriptor, or <see langword="null" /> when it is not active.</returns>
    RateLimitingRuntimeDescriptor? GetById(string policyId);

    /// <summary>
    /// Gets all rate-limiting policies that apply to the requested transport identifier.
    /// </summary>
    /// <param name="transportId">The stable transport identifier to filter by.</param>
    /// <returns>The matching policies, or an empty list when none target the transport.</returns>
    IReadOnlyList<RateLimitingRuntimeDescriptor> GetByTransportId(string transportId);
}
