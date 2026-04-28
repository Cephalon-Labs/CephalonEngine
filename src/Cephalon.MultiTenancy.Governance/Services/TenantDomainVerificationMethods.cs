namespace Cephalon.MultiTenancy.Governance.Services;

/// <summary>
/// Defines stable verification-method labels for tenant domain ownership descriptors.
/// </summary>
public static class TenantDomainVerificationMethods
{
    /// <summary>
    /// Domain ownership was verified by an operator or another trusted manual process.
    /// </summary>
    public const string Manual = "manual";

    /// <summary>
    /// Domain ownership is expected to be verified by a DNS TXT record.
    /// </summary>
    public const string DnsTxt = "dns-txt";

    /// <summary>
    /// Domain ownership is expected to be verified by an HTTP file or well-known endpoint.
    /// </summary>
    public const string HttpFile = "http-file";
}
