using Cephalon.Abstractions.Technologies;
using Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.AzureIdentity.Configuration;
using System.Globalization;

namespace Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.AzureIdentity.Services;

internal sealed class MicrosoftGraphInvitationDeliveryAzureIdentityRuntimeSurfaceContributor(
    MicrosoftGraphInvitationDeliveryAzureIdentityOptions options) : ITechnologyRuntimeContributor
{
    public TechnologyRuntimeSurface DescribeRuntimeSurface()
    {
        var authorityHost = options.TryGetAuthorityHost();
        var scopes = options.GetScopes();
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ownership"] = "provider-managed",
            ["package"] = "Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.AzureIdentity",
            ["provider"] = "azure-identity",
            ["transport"] = "azure-identity-token-credential",
            ["runtimeState"] = "configured",
            ["tokenProviderOwnership"] = "provider-managed",
            ["targetProvider"] = "microsoft-graph",
            ["credentialSource"] = "DefaultAzureCredential-or-host-supplied-TokenCredential",
            ["scopeCount"] = scopes.Length.ToString(CultureInfo.InvariantCulture),
            ["scopes"] = string.Join(",", scopes),
            ["tenantIdConfigured"] = (!string.IsNullOrWhiteSpace(options.TenantId)).ToString().ToLowerInvariant(),
            ["managedIdentityClientIdConfigured"] = (!string.IsNullOrWhiteSpace(options.ManagedIdentityClientId)).ToString().ToLowerInvariant(),
            ["authorityHostConfigured"] = (!string.IsNullOrWhiteSpace(options.AuthorityHost)).ToString().ToLowerInvariant(),
            ["authorityHost"] = authorityHost?.Host ?? "default",
            ["excludeInteractiveBrowserCredential"] = options.ExcludeInteractiveBrowserCredential.ToString().ToLowerInvariant(),
            ["excludeManagedIdentityCredential"] = options.ExcludeManagedIdentityCredential.ToString().ToLowerInvariant(),
            ["secretProjection"] = "redacted"
        };

        return new TechnologyRuntimeSurface(
            technologyId: "multi-tenancy",
            surfaceId: "tenant-invitation-delivery-microsoft-graph-azure-identity",
            displayName: "Microsoft Graph Azure Identity Token Provider",
            description: "Projects the Azure Identity token provider used by Microsoft Graph tenant invitation delivery without exposing credentials or tokens.",
            entries:
            [
                new TechnologyRuntimeEntry(
                    id: "microsoft-graph-azure-identity-token-provider",
                    displayName: "Microsoft Graph Azure Identity Token Provider",
                    description: "Summarizes the configured Azure Identity credential chain, target scopes, authority posture, and credential exclusions.",
                    metadata: metadata)
            ]);
    }
}
