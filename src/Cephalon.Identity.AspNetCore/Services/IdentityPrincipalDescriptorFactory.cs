using System.Security.Claims;
using Cephalon.Identity.AspNetCore.Configuration;

namespace Cephalon.Identity.AspNetCore.Services;

internal sealed class IdentityPrincipalDescriptorFactory(IdentityAspNetCoreOptions options)
{
    public bool TryCreate(
        ClaimsPrincipal? principal,
        out IdentityPrincipalDescriptor? descriptor,
        out string? failureReason)
    {
        descriptor = null;
        failureReason = null;

        if (principal?.Identity?.IsAuthenticated != true)
        {
            failureReason = "An authenticated user is required for this endpoint.";
            return false;
        }

        var subjectId = ResolveClaimValue(principal, options.SubjectIdClaimTypes) ??
            (options.AllowIdentityNameAsSubjectIdFallback ? Normalize(principal.Identity?.Name) : null);
        if (subjectId is null)
        {
            failureReason = "The authenticated user did not provide a subject identifier that the Cephalon ASP.NET Core identity adapter could resolve.";
            return false;
        }

        var displayName = ResolveClaimValue(principal, options.DisplayNameClaimTypes) ?? Normalize(principal.Identity?.Name);
        var roles = ResolveClaimValues(principal, options.RoleClaimTypes);
        var tenantIds = ResolveClaimValues(principal, options.TenantClaimTypes);
        var subjectAttributes = CreateSubjectAttributes(principal);
        var actorAttributes = CreateAuditActorAttributes(principal, roles, tenantIds, subjectAttributes);

        descriptor = new IdentityPrincipalDescriptor(
            subjectId,
            displayName,
            roles,
            tenantIds,
            subjectAttributes,
            actorAttributes);
        return true;
    }

    private Dictionary<string, string> CreateSubjectAttributes(ClaimsPrincipal principal)
    {
        var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!options.IncludeAllClaimsAsSubjectAttributes)
        {
            return attributes;
        }

        var excludedClaimTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddRange(excludedClaimTypes, options.SubjectIdClaimTypes);
        AddRange(excludedClaimTypes, options.DisplayNameClaimTypes);
        AddRange(excludedClaimTypes, options.RoleClaimTypes);
        AddRange(excludedClaimTypes, options.TenantClaimTypes);

        foreach (var group in principal.Claims
                     .Where(claim => !excludedClaimTypes.Contains(claim.Type))
                     .GroupBy(claim => claim.Type, StringComparer.OrdinalIgnoreCase)
                     .OrderBy(static group => group.Key, StringComparer.OrdinalIgnoreCase))
        {
            var values = group
                .Select(static claim => Normalize(claim.Value))
                .Where(static value => value is not null)
                .Select(static value => value!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (values.Length == 0)
            {
                continue;
            }

            attributes[group.Key] = string.Join(",", values);
        }

        return attributes;
    }

    private static Dictionary<string, string> CreateAuditActorAttributes(
        ClaimsPrincipal principal,
        string[] roles,
        string[] tenantIds,
        IReadOnlyDictionary<string, string> subjectAttributes)
    {
        var attributes = new Dictionary<string, string>(subjectAttributes, StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(principal.Identity?.AuthenticationType))
        {
            attributes["authenticationType"] = principal.Identity.AuthenticationType!.Trim();
        }

        if (roles.Length > 0)
        {
            attributes["roles"] = string.Join(",", roles);
        }

        if (tenantIds.Length > 0)
        {
            attributes["tenantIds"] = string.Join(",", tenantIds);
        }

        return attributes;
    }

    private static string? ResolveClaimValue(ClaimsPrincipal principal, IReadOnlyCollection<string> claimTypes)
    {
        foreach (var claimType in claimTypes)
        {
            var value = principal.Claims
                .Where(claim => string.Equals(claim.Type, claimType, StringComparison.OrdinalIgnoreCase))
                .Select(static claim => Normalize(claim.Value))
                .FirstOrDefault(static value => value is not null);
            if (value is not null)
            {
                return value;
            }
        }

        return null;
    }

    private static string[] ResolveClaimValues(ClaimsPrincipal principal, IReadOnlyCollection<string> claimTypes)
    {
        return claimTypes
            .SelectMany(claimType => principal.Claims.Where(claim =>
                string.Equals(claim.Type, claimType, StringComparison.OrdinalIgnoreCase)))
            .SelectMany(static claim => SplitValues(claim.Value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string[] SplitValues(string? value)
    {
        return value?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(static item => Normalize(item))
            .Where(static item => item is not null)
            .Select(static item => item!)
            .ToArray() ?? [];
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

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}

internal sealed record IdentityPrincipalDescriptor(
    string SubjectId,
    string? DisplayName,
    string[] Roles,
    string[] TenantIds,
    IReadOnlyDictionary<string, string> SubjectAttributes,
    IReadOnlyDictionary<string, string> AuditActorAttributes);
