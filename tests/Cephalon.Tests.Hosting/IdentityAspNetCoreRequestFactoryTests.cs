using System.Security.Claims;
using Cephalon.Identity.AspNetCore.Configuration;
using Cephalon.Identity.AspNetCore.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Cephalon.Tests.Hosting;

public sealed class IdentityAspNetCoreRequestFactoryTests
{
    [Fact]
    public void TryCreateFailsWhenSubjectIdFallbackIsDisabledAndNoSubjectClaimIsPresent()
    {
        var options = new IdentityAspNetCoreOptions
        {
            AllowIdentityNameAsSubjectIdFallback = false
        };
        var factory = new HttpContextAuthorizationRequestFactory(options);
        var httpContext = CreateHttpContext(
            principal: CreatePrincipal(
                new Claim(ClaimTypes.Name, "user-with-name-only"),
                new Claim("tenant_id", "tenant-001")),
            method: "GET",
            path: "/tenants/tenant-001/documents/doc-001/internal",
            routePattern: "/tenants/{tenantId}/documents/{id}/{classification}",
            routeValues: new Dictionary<string, object?>
            {
                ["tenantId"] = "tenant-001",
                ["id"] = "doc-001",
                ["classification"] = "internal"
            });
        var metadata = new RestAuthorizationRequestMetadata(
            policyId: "tenant-boundary",
            action: null,
            resourceType: "document",
            resourceIdRouteKey: null,
            tenantRouteKey: null,
            ownerSubjectIdRouteKey: null);

        var created = factory.TryCreate(httpContext, metadata, out var request, out var failureReason);

        Assert.False(created);
        Assert.Null(request);
        Assert.NotNull(failureReason);
        Assert.Contains("subject identifier", failureReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryCreateHonorsConfiguredClaimTypesAndOptionalProjectionFlags()
    {
        var options = new IdentityAspNetCoreOptions
        {
            IncludeAllClaimsAsSubjectAttributes = true,
            IncludeRouteValuesAsResourceAttributes = true,
            IncludeQueryStringAsContextAttributes = true,
            IncludeHeadersAsContextAttributes = true
        };
        Replace(options.SubjectIdClaimTypes, "custom-subject");
        Replace(options.DisplayNameClaimTypes, "custom-name");
        Replace(options.RoleClaimTypes, "custom-role");
        Replace(options.TenantClaimTypes, "custom-tenant");
        var factory = new HttpContextAuthorizationRequestFactory(options);
        var httpContext = CreateHttpContext(
            principal: CreatePrincipal(
                new Claim("custom-subject", "user-001"),
                new Claim("custom-name", "User One"),
                new Claim("custom-role", "tenant-admin"),
                new Claim("custom-tenant", "tenant-001"),
                new Claim("region", "apac"),
                new Claim("department", "ops")),
            method: "GET",
            path: "/tenants/tenant-001/documents/doc-001/internal",
            routePattern: "/tenants/{tenantId}/documents/{id}/{classification}",
            routeValues: new Dictionary<string, object?>
            {
                ["tenantId"] = "tenant-001",
                ["id"] = "doc-001",
                ["classification"] = "internal"
            },
            query: new Dictionary<string, string>
            {
                ["preview"] = "true"
            },
            headers: new Dictionary<string, string>
            {
                ["X-Correlation-Id"] = "corr-001"
            });
        var metadata = new RestAuthorizationRequestMetadata(
            policyId: "tenant-boundary",
            action: null,
            resourceType: "document",
            resourceIdRouteKey: null,
            tenantRouteKey: null,
            ownerSubjectIdRouteKey: null);

        var created = factory.TryCreate(httpContext, metadata, out var request, out var failureReason);

        Assert.True(created);
        Assert.Null(failureReason);
        Assert.NotNull(request);

        Assert.Equal("user-001", request.Subject.SubjectId);
        Assert.Equal("User One", request.Subject.DisplayName);
        Assert.Equal(["tenant-admin"], request.Subject.Roles);
        Assert.Equal(["tenant-001"], request.Subject.TenantIds);
        Assert.Equal("apac", request.Subject.Attributes["region"]);
        Assert.Equal("ops", request.Subject.Attributes["department"]);

        Assert.Equal("document", request.Resource.ResourceType);
        Assert.Equal("doc-001", request.Resource.ResourceId);
        Assert.Equal("tenant-001", request.Resource.TenantId);
        Assert.Equal("internal", request.Resource.Attributes["classification"]);
        Assert.Equal("doc-001", request.Resource.Attributes["resourceId"]);
        Assert.Equal("tenant-001", request.Resource.Attributes["tenantId"]);

        Assert.Equal("read", request.Context.Action);
        Assert.Equal("tenant-boundary", request.Context.PolicyId);
        Assert.Equal("true", request.Context.Attributes["query.preview"]);
        Assert.Equal("corr-001", request.Context.Attributes["header.X-Correlation-Id"]);
        Assert.Equal("Test endpoint", request.Context.Attributes["endpointDisplayName"]);
        Assert.Equal("/tenants/{tenantId}/documents/{id}/{classification}", request.Context.Attributes["routePattern"]);
    }

    [Fact]
    public void TryCreateCanDisableSubjectRouteHeaderAndQueryProjection()
    {
        var options = new IdentityAspNetCoreOptions
        {
            IncludeAllClaimsAsSubjectAttributes = false,
            IncludeRouteValuesAsResourceAttributes = false,
            IncludeQueryStringAsContextAttributes = false,
            IncludeHeadersAsContextAttributes = false
        };
        var factory = new HttpContextAuthorizationRequestFactory(options);
        var httpContext = CreateHttpContext(
            principal: CreatePrincipal(
                new Claim(ClaimTypes.NameIdentifier, "user-002"),
                new Claim(ClaimTypes.Role, "member"),
                new Claim("tenant_id", "tenant-001"),
                new Claim("region", "apac")),
            method: "PATCH",
            path: "/tenants/tenant-001/documents/doc-002/owners/user-002",
            routePattern: "/tenants/{tenantId}/documents/{id}/owners/{ownerSubjectId}",
            routeValues: new Dictionary<string, object?>
            {
                ["tenantId"] = "tenant-001",
                ["id"] = "doc-002",
                ["ownerSubjectId"] = "user-002"
            },
            query: new Dictionary<string, string>
            {
                ["preview"] = "true"
            },
            headers: new Dictionary<string, string>
            {
                ["X-Debug"] = "enabled"
            });
        var metadata = new RestAuthorizationRequestMetadata(
            policyId: "document-owner",
            action: null,
            resourceType: "document",
            resourceIdRouteKey: null,
            tenantRouteKey: null,
            ownerSubjectIdRouteKey: null);

        var created = factory.TryCreate(httpContext, metadata, out var request, out var failureReason);

        Assert.True(created);
        Assert.Null(failureReason);
        Assert.NotNull(request);

        Assert.Empty(request.Subject.Attributes);
        Assert.Empty(request.Resource.Attributes);
        Assert.Equal("doc-002", request.Resource.ResourceId);
        Assert.Equal("tenant-001", request.Resource.TenantId);
        Assert.Equal("user-002", request.Resource.OwnerSubjectId);
        Assert.DoesNotContain("query.preview", request.Context.Attributes.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("header.X-Debug", request.Context.Attributes.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Equal("update", request.Context.Action);
    }

    private static ClaimsPrincipal CreatePrincipal(params Claim[] claims)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Test"));
    }

    private static DefaultHttpContext CreateHttpContext(
        ClaimsPrincipal principal,
        string method,
        string path,
        string routePattern,
        IReadOnlyDictionary<string, object?> routeValues,
        Dictionary<string, string>? query = null,
        Dictionary<string, string>? headers = null)
    {
        var httpContext = new DefaultHttpContext
        {
            User = principal
        };
        httpContext.Request.Method = method;
        httpContext.Request.Path = path;

        foreach (var (key, value) in routeValues)
        {
            httpContext.Request.RouteValues[key] = value;
        }

        if (query is not null && query.Count > 0)
        {
            httpContext.Request.QueryString = QueryString.Create(query.Select(static pair =>
                new KeyValuePair<string, string?>(pair.Key, pair.Value)));
        }

        if (headers is not null)
        {
            foreach (var (key, value) in headers)
            {
                httpContext.Request.Headers[key] = value;
            }
        }

        httpContext.SetEndpoint(new RouteEndpoint(
            requestDelegate: static _ => Task.CompletedTask,
            routePattern: RoutePatternFactory.Parse(routePattern),
            order: 0,
            metadata: new EndpointMetadataCollection(),
            displayName: "Test endpoint"));
        return httpContext;
    }

    private static void Replace(List<string> target, params string[] values)
    {
        target.Clear();
        target.AddRange(values);
    }
}
