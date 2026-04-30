using Cephalon.Engine.Diagnostics;

namespace Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.AzureIdentity.Services;

internal sealed class MicrosoftGraphInvitationDeliveryAzureIdentityDiagnosticsConventionContributor : IDiagnosticsConventionContributor
{
    public DiagnosticsConvention DescribeDiagnosticsConvention() => MicrosoftGraphInvitationDeliveryAzureIdentityDiagnosticsConventions.Convention;
}

internal static class MicrosoftGraphInvitationDeliveryAzureIdentityDiagnosticsConventions
{
    public static readonly DiagnosticEventDefinition MicrosoftGraphAzureIdentityTokenAcquired = new(
        Id: 4574,
        Name: "MicrosoftGraphAzureIdentityTokenAcquired",
        Severity: DiagnosticSeverity.Debug,
        MessageTemplate: "Microsoft Graph Azure Identity token provider acquired a token for {ScopeCount} scopes using credential '{CredentialType}' that expires at {ExpiresOnUtc}.",
        Description: "Emitted when Azure Identity returns a Microsoft Graph access token without logging the token value.");

    public static readonly DiagnosticEventDefinition MicrosoftGraphAzureIdentityTokenFailed = new(
        Id: 4575,
        Name: "MicrosoftGraphAzureIdentityTokenFailed",
        Severity: DiagnosticSeverity.Warning,
        MessageTemplate: "Microsoft Graph Azure Identity token provider failed to acquire a token for {ScopeCount} scopes using credential '{CredentialType}'. Reason: {Reason}.",
        Description: "Emitted when Azure Identity cannot return a Microsoft Graph access token for invitation delivery.");

    public static readonly DiagnosticsConvention Convention = new(
        Source: "Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.AzureIdentity",
        LoggerCategoryPrefix: "Cephalon.MultiTenancy.Governance.MicrosoftGraphDelivery.AzureIdentity",
        Description: "Structured diagnostics for Azure Identity token acquisition used by Microsoft Graph tenant-invitation delivery.",
        Events:
        [
            MicrosoftGraphAzureIdentityTokenAcquired,
            MicrosoftGraphAzureIdentityTokenFailed
        ]);
}
