using System.Globalization;
using Cephalon.Abstractions.AppModel;
using Cephalon.Abstractions.Technologies;
using Cephalon.Abstractions.Tenancy;
using Cephalon.MultiTenancy.Configuration;

namespace Cephalon.MultiTenancy.Services;

internal sealed class MultiTenancyRuntimeSurfaceContributor(
    MultiTenancyRuntimeOptions options,
    AppProfile appProfile,
    IEnumerable<ITenantResolver> resolvers,
    IEnumerable<ITenantContextAccessor> contextAccessors) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        var configuredTenantIds = options.Tenants
            .Select(static tenant => tenant.TenantId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static tenantId => tenantId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var configuredDomains = options.Tenants
            .SelectMany(static tenant => tenant.Domains)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static domain => domain, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var resolverTypes = resolvers
            .Select(static resolver => resolver.GetType().FullName ?? resolver.GetType().Name)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static typeName => typeName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var accessorTypes = contextAccessors
            .Select(static accessor => accessor.GetType().FullName ?? accessor.GetType().Name)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static typeName => typeName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["tenancySelection"] = appProfile.Tenancy.Enabled switch
            {
                true => "enabled",
                false => "disabled",
                _ => "not-configured"
            },
            ["selectedMode"] = string.IsNullOrWhiteSpace(appProfile.Tenancy.Mode)
                ? "none"
                : appProfile.Tenancy.Mode!,
            ["configuredTenantCount"] = configuredTenantIds.Length.ToString(CultureInfo.InvariantCulture),
            ["configuredTenantIds"] = configuredTenantIds.Length == 0 ? "none" : string.Join(",", configuredTenantIds),
            ["configuredDomainCount"] = configuredDomains.Length.ToString(CultureInfo.InvariantCulture),
            ["defaultTenantId"] = string.IsNullOrWhiteSpace(options.DefaultTenantId)
                ? "none"
                : options.DefaultTenantId!,
            ["resolverCount"] = resolverTypes.Length.ToString(CultureInfo.InvariantCulture),
            ["resolverTypes"] = resolverTypes.Length == 0 ? "none" : string.Join(",", resolverTypes),
            ["ambientContextAccessorCount"] = accessorTypes.Length.ToString(CultureInfo.InvariantCulture),
            ["ambientContextAccessorTypes"] = accessorTypes.Length == 0 ? "none" : string.Join(",", accessorTypes),
            ["domainResolution"] = configuredDomains.Length == 0 ? "not-configured" : "configured",
            ["tenantKeyResolution"] = options.Tenants.Any(static tenant => !string.IsNullOrWhiteSpace(tenant.TenantKey))
                ? "configured"
                : "not-configured",
            ["defaultResolverEnabled"] = options.EnableDefaultResolver ? "true" : "false",
            ["resolutionStrategies"] = options.EnableDefaultResolver
                ? "requested-tenant-id,requested-tenant-key,host-name,default-tenant,single-tenant-fallback"
                : "disabled"
        };

        return new TechnologyRuntimeSurface(
            technologyId: "multi-tenancy",
            surfaceId: "tenant-resolution",
            displayName: "Tenant Resolution",
            description: "Projects the active Cephalon multi-tenancy resolution answer.",
            entries:
            [
                new TechnologyRuntimeEntry(
                    id: "tenant-runtime",
                    displayName: "Tenant Runtime",
                    description: "Summarizes configured tenants, resolution strategies, and ambient tenant-context availability for the active multi-tenancy companion pack.",
                    metadata: metadata)
            ]);
    }
}
