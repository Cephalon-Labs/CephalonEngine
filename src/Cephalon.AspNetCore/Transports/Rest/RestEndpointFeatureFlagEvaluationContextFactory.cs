using System.Security.Claims;
using Cephalon.Abstractions.Features;
using Cephalon.Abstractions.Tenancy;
using Cephalon.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.Hosting;

namespace Cephalon.AspNetCore.Transports.Rest;

internal static class RestEndpointFeatureFlagEvaluationContextFactory
{
    internal static FeatureFlagEvaluationContext Create(HttpContext httpContext, Endpoint? endpoint)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var services = httpContext.RequestServices;
        var environmentName = ResolveEnvironmentName(services);
        var moduleId = endpoint?.Metadata.GetMetadata<RestModuleEndpointMetadata>()?.ModuleId;
        var behaviorId = endpoint?.Metadata.GetMetadata<RestBehaviorEndpointMetadata>()?.BehaviorId;
        var capabilityKey = endpoint is null
            ? null
            : RestEndpointRuntimeMetadata.ResolveEffectiveRequiredCapabilityKey(
                endpoint.Metadata.OfType<RestEndpointCapabilityMetadata>());
        var tenantId = ResolveTenantId(httpContext);
        var subjectId = ResolveSubjectId(httpContext);
        var tags = ResolveTags(endpoint);

        return new FeatureFlagEvaluationContext(
            environmentName: environmentName,
            moduleId: moduleId,
            behaviorId: behaviorId,
            capabilityKey: capabilityKey,
            transportId: "rest-api",
            tenantId: tenantId,
            subjectId: subjectId,
            tags: tags);
    }

    private static string? ResolveEnvironmentName(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.GetService(typeof(AspNetCoreHostEnvironmentSnapshot)) is AspNetCoreHostEnvironmentSnapshot snapshot &&
               !string.IsNullOrWhiteSpace(snapshot.EnvironmentName)
            ? snapshot.EnvironmentName
            : services.GetService(typeof(IHostEnvironment)) is IHostEnvironment hostEnvironment
            ? hostEnvironment.EnvironmentName
            : services.GetService(typeof(IWebHostEnvironment)) is IWebHostEnvironment webHostEnvironment
                ? webHostEnvironment.EnvironmentName
                : null;
    }

    private static string? ResolveTenantId(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var accessor = httpContext.RequestServices.GetService(typeof(ITenantContextAccessor)) as ITenantContextAccessor;
        var currentTenantId = accessor?.Current?.TenantId;
        if (!string.IsNullOrWhiteSpace(currentTenantId))
        {
            return currentTenantId;
        }

        return httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault();
    }

    private static string? ResolveSubjectId(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        return httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? httpContext.User.FindFirst("sub")?.Value
            ?? httpContext.User.Identity?.Name;
    }

    private static string[] ResolveTags(Endpoint? endpoint)
    {
        if (endpoint is null)
        {
            return [];
        }

        return endpoint.Metadata
            .OfType<ITagsMetadata>()
            .SelectMany(static metadata => metadata.Tags)
            .Concat(
                endpoint.Metadata
                    .OfType<RestBehaviorEndpointMetadata>()
                    .Select(static metadata => metadata.TagName))
            .Where(static tag => !string.IsNullOrWhiteSpace(tag))
            .Select(static tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static tag => tag, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
