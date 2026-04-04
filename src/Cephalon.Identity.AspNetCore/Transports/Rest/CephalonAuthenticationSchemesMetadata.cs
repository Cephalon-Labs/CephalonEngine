namespace Cephalon.Identity.AspNetCore.Transports.Rest;

internal sealed class CephalonAuthenticationSchemesMetadata(string[] authenticationSchemes)
{
    public string[] AuthenticationSchemes { get; } = authenticationSchemes;
}
