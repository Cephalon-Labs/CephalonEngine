using Cephalon.Identity.AspNetCore.Services;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Identity.AspNetCore.Transports.Rest;

/// <summary>
/// Requires a Cephalon authorization decision before an ASP.NET Core controller or action can execute.
/// </summary>
/// <remarks>
/// This keeps controller and action authorization low ceremony by reusing the same Cephalon request-shaping,
/// challenge, forbid, and problem-details behavior already used by the minimal-API helper surface.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequireCephalonAuthorizationAttribute(string policyId) : Attribute, IFilterFactory, IOrderedFilter
{
    /// <summary>
    /// Gets the Cephalon authorization policy id that must allow the request.
    /// </summary>
    public string PolicyId { get; } = !string.IsNullOrWhiteSpace(policyId)
        ? policyId.Trim()
        : throw new ArgumentException("Authorization policy id is required.", nameof(policyId));

    /// <summary>
    /// Gets or sets the optional action to evaluate.
    /// </summary>
    public string? Action { get; set; }

    /// <summary>
    /// Gets or sets the optional logical resource type.
    /// </summary>
    public string? ResourceType { get; set; }

    /// <summary>
    /// Gets or sets the optional route-value key that provides the resource identifier.
    /// </summary>
    public string? ResourceIdRouteKey { get; set; }

    /// <summary>
    /// Gets or sets the optional route-value key that provides the tenant identifier.
    /// </summary>
    public string? TenantRouteKey { get; set; }

    /// <summary>
    /// Gets or sets the optional route-value key that provides the owning subject identifier.
    /// </summary>
    public string? OwnerSubjectIdRouteKey { get; set; }

    /// <summary>
    /// Gets the MVC filter order used to run the Cephalon authorization boundary early in the authorization stage.
    /// </summary>
    public int Order => int.MinValue + 100;

    /// <summary>
    /// Gets a value indicating whether the MVC filter instance can be reused across requests.
    /// </summary>
    public bool IsReusable => false;

    /// <summary>
    /// Creates the MVC authorization filter that evaluates the current request through the shared Cephalon boundary executor.
    /// </summary>
    /// <param name="serviceProvider">The request-scoped service provider used to resolve executor services.</param>
    /// <returns>The filter instance that will enforce the declared Cephalon authorization metadata.</returns>
    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        var executor = serviceProvider.GetRequiredService<CephalonAuthorizationBoundaryExecutor>();
        return new MvcCephalonAuthorizationFilter(executor, CreateMetadata());
    }

    internal RestAuthorizationRequestMetadata CreateMetadata()
    {
        return new RestAuthorizationRequestMetadata(
            PolicyId,
            Action,
            ResourceType,
            ResourceIdRouteKey,
            TenantRouteKey,
            OwnerSubjectIdRouteKey);
    }
}
