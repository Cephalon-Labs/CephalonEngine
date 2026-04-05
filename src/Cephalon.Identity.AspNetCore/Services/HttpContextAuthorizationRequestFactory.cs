using System.Diagnostics;
using Cephalon.Abstractions.Authorization;
using Cephalon.Identity.AspNetCore.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Cephalon.Identity.AspNetCore.Services;

internal sealed class HttpContextAuthorizationRequestFactory
{
    private readonly IdentityAspNetCoreOptions options;
    private readonly IdentityPrincipalDescriptorFactory principalDescriptorFactory;

    public HttpContextAuthorizationRequestFactory(IdentityAspNetCoreOptions options)
        : this(options, new IdentityPrincipalDescriptorFactory(options))
    {
    }

    public HttpContextAuthorizationRequestFactory(
        IdentityAspNetCoreOptions options,
        IdentityPrincipalDescriptorFactory principalDescriptorFactory)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(principalDescriptorFactory);

        this.options = options;
        this.principalDescriptorFactory = principalDescriptorFactory;
    }

    public bool TryCreate(
        HttpContext httpContext,
        RestAuthorizationRequestMetadata metadata,
        out AuthorizationEvaluationRequest? request,
        out string? failureReason)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(metadata);

        request = null;
        failureReason = null;

        var principal = httpContext.User;
        if (!principalDescriptorFactory.TryCreate(principal, out var principalDescriptor, out failureReason))
        {
            return false;
        }

        var subjectDescriptor = principalDescriptor!;

        var resourceType = metadata.ResourceType ?? ResolveResourceType(httpContext);
        if (resourceType is null)
        {
            throw new InvalidOperationException(
                $"Cephalon authorization for policy '{metadata.PolicyId}' requires an explicit resource type or a route pattern with at least one literal segment.");
        }

        var resourceId = ResolveRouteValue(httpContext, metadata.ResourceIdRouteKey, options.ResourceIdRouteKeys);
        var resourceTenantId =
            ResolveRouteValue(httpContext, metadata.TenantRouteKey, options.TenantRouteKeys) ??
            ResolveHeaderValue(httpContext, options.TenantHeaderNames) ??
            (subjectDescriptor.TenantIds.Length == 1 ? subjectDescriptor.TenantIds[0] : null);
        var ownerSubjectId = ResolveRouteValue(httpContext, metadata.OwnerSubjectIdRouteKey, options.OwnerSubjectIdRouteKeys);
        var resourceAttributes = CreateResourceAttributes(httpContext, resourceId, resourceTenantId, ownerSubjectId);

        var action = metadata.Action ?? ResolveAction(httpContext.Request.Method);
        var contextAttributes = CreateContextAttributes(httpContext, action);
        var contextTenantId = resourceTenantId ?? (subjectDescriptor.TenantIds.Length == 1 ? subjectDescriptor.TenantIds[0] : null);
        var correlationId = Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;

        request = new AuthorizationEvaluationRequest(
            new AuthorizationSubject(
                subjectDescriptor.SubjectId,
                subjectDescriptor.DisplayName,
                subjectDescriptor.Roles,
                subjectDescriptor.TenantIds,
                subjectDescriptor.SubjectAttributes),
            new AuthorizationResource(resourceType, resourceId, resourceTenantId, ownerSubjectId, resourceAttributes),
            new AuthorizationContext(action, metadata.PolicyId, contextTenantId, correlationId, contextAttributes));
        return true;
    }

    private Dictionary<string, string> CreateResourceAttributes(
        HttpContext httpContext,
        string? resourceId,
        string? resourceTenantId,
        string? ownerSubjectId)
    {
        var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!options.IncludeRouteValuesAsResourceAttributes)
        {
            return attributes;
        }

        var ignoredKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "controller",
            "action",
            "page",
            "area"
        };
        AddRange(ignoredKeys, options.ResourceIdRouteKeys);
        AddRange(ignoredKeys, options.TenantRouteKeys);
        AddRange(ignoredKeys, options.OwnerSubjectIdRouteKeys);

        foreach (var (key, value) in httpContext.Request.RouteValues.OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (ignoredKeys.Contains(key))
            {
                continue;
            }

            var normalizedValue = NormalizeRouteValue(value);
            if (normalizedValue is null)
            {
                continue;
            }

            attributes[key] = normalizedValue;
        }

        if (resourceId is not null)
        {
            attributes["resourceId"] = resourceId;
        }

        if (resourceTenantId is not null)
        {
            attributes["tenantId"] = resourceTenantId;
        }

        if (ownerSubjectId is not null)
        {
            attributes["ownerSubjectId"] = ownerSubjectId;
        }

        return attributes;
    }

    private Dictionary<string, string> CreateContextAttributes(HttpContext httpContext, string action)
    {
        var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["operation"] = action,
            ["httpMethod"] = httpContext.Request.Method,
            ["requestPath"] = httpContext.Request.Path.ToString()
        };

        if (httpContext.GetEndpoint() is RouteEndpoint routeEndpoint)
        {
            if (!string.IsNullOrWhiteSpace(routeEndpoint.DisplayName))
            {
                attributes["endpointDisplayName"] = routeEndpoint.DisplayName;
            }

            if (!string.IsNullOrWhiteSpace(routeEndpoint.RoutePattern.RawText))
            {
                attributes["routePattern"] = routeEndpoint.RoutePattern.RawText;
            }
        }

        if (options.IncludeQueryStringAsContextAttributes)
        {
            foreach (var (key, value) in httpContext.Request.Query.OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase))
            {
                var normalizedValue = Normalize(string.Join(",", value.ToArray()));
                if (normalizedValue is null)
                {
                    continue;
                }

                attributes[$"query.{key}"] = normalizedValue;
            }
        }

        if (options.IncludeHeadersAsContextAttributes)
        {
            foreach (var (key, value) in httpContext.Request.Headers.OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase))
            {
                var normalizedValue = Normalize(string.Join(",", value.ToArray()));
                if (normalizedValue is null)
                {
                    continue;
                }

                attributes[$"header.{key}"] = normalizedValue;
            }
        }

        return attributes;
    }

    private static string ResolveAction(string method)
    {
        return method.Trim().ToUpperInvariant() switch
        {
            "GET" => "read",
            "HEAD" => "read",
            "OPTIONS" => "read",
            "POST" => "create",
            "PUT" => "update",
            "PATCH" => "update",
            "DELETE" => "delete",
            _ => method.Trim().ToLowerInvariant()
        };
    }

    private static string? ResolveResourceType(HttpContext httpContext)
    {
        var routePattern = (httpContext.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText;
        var segments = (routePattern ?? httpContext.Request.Path.ToString())
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Normalize)
            .Where(static segment => segment is not null && !segment.StartsWith('{') && !segment.EndsWith('}'))
            .Select(static segment => segment!)
            .ToArray();

        return segments.LastOrDefault();
    }

    private static string? ResolveRouteValue(
        HttpContext httpContext,
        string? explicitKey,
        IReadOnlyCollection<string> fallbackKeys)
    {
        if (TryGetRouteValue(httpContext.Request.RouteValues, explicitKey, out var explicitValue))
        {
            return explicitValue;
        }

        foreach (var key in fallbackKeys)
        {
            if (TryGetRouteValue(httpContext.Request.RouteValues, key, out var value))
            {
                return value;
            }
        }

        return null;
    }

    private static bool TryGetRouteValue(RouteValueDictionary routeValues, string? key, out string? value)
    {
        value = null;
        var normalizedKey = Normalize(key);
        if (normalizedKey is null)
        {
            return false;
        }

        foreach (var pair in routeValues)
        {
            if (!string.Equals(pair.Key, normalizedKey, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            value = NormalizeRouteValue(pair.Value);
            return value is not null;
        }

        return false;
    }

    private static string? ResolveHeaderValue(HttpContext httpContext, IReadOnlyCollection<string> headerNames)
    {
        foreach (var headerName in headerNames)
        {
            if (!httpContext.Request.Headers.TryGetValue(headerName, out var values))
            {
                continue;
            }

            var value = Normalize(string.Join(",", values.ToArray()));
            if (value is not null)
            {
                return value;
            }
        }

        return null;
    }

    private static string? NormalizeRouteValue(object? value)
    {
        return value switch
        {
            null => null,
            string stringValue => Normalize(stringValue),
            _ => Normalize(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture))
        };
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static void AddRange(HashSet<string> target, IEnumerable<string> values)
    {
        foreach (var value in values)
        {
            var normalizedValue = Normalize(value);
            if (normalizedValue is not null)
            {
                target.Add(normalizedValue);
            }
        }
    }
}

internal sealed record AuthorizationEvaluationRequest(
    AuthorizationSubject Subject,
    AuthorizationResource Resource,
    AuthorizationContext Context);
