using Microsoft.OpenApi;

namespace Cephalon.AspNetCore.Transformers;

internal sealed class OpenApiSecuritySchemeSettings
{
    public string? Name { get; init; }

    public SecuritySchemeType Type { get; init; } = SecuritySchemeType.Http;

    public ParameterLocation In { get; init; } = ParameterLocation.Header;

    public string? Description { get; init; }

    public string? Scheme { get; init; }

    public string? BearerFormat { get; init; }

    public string? OpenIdConnectUrl { get; init; }
}
