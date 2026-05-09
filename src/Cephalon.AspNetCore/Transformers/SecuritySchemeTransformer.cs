using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi;

namespace Cephalon.AspNetCore.Transformers;

internal sealed class SecuritySchemeTransformer(IConfiguration configuration) : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var configuredSchemes = ReadSecuritySchemes(configuration.GetSection("OpenApi:SecuritySchemes"));

        if (configuredSchemes is null || configuredSchemes.Length == 0)
        {
            return;
        }

        var securitySchemes = configuredSchemes
            .Where(scheme => !string.IsNullOrWhiteSpace(scheme.Name))
            .GroupBy(scheme => scheme.Name!, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Last())
            .ToDictionary(
                scheme => scheme.Name!,
                CreateOpenApiScheme,
                StringComparer.OrdinalIgnoreCase);

        if (securitySchemes.Count == 0)
        {
            return;
        }

        if (document.Paths is null)
        {
            return;
        }

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>(StringComparer.OrdinalIgnoreCase);

        foreach (var (schemeName, scheme) in securitySchemes)
        {
            document.Components.SecuritySchemes[schemeName] = scheme;
        }

        foreach (var operation in document.Paths.Values.SelectMany(path =>
                     path.Operations is null
                         ? Enumerable.Empty<OpenApiOperation>()
                         : path.Operations.Values))
        {
            operation.Security ??= [];

            foreach (var schemeName in securitySchemes.Keys)
            {
                if (operation.Security.Any(requirement => requirement.Keys.Any(key =>
                        string.Equals(GetSecuritySchemeId(key), schemeName, StringComparison.OrdinalIgnoreCase))))
                {
                    continue;
                }

                operation.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference(schemeName, document)] = []
                });
            }
        }
    }

    private static OpenApiSecurityScheme CreateOpenApiScheme(OpenApiSecuritySchemeSettings scheme)
    {
        var openApiScheme = new OpenApiSecurityScheme
        {
            Type = scheme.Type,
            Name = scheme.Name,
            In = scheme.In,
            Description = scheme.Description
        };

        if (!string.IsNullOrWhiteSpace(scheme.Scheme))
        {
            openApiScheme.Scheme = scheme.Scheme;
        }

        if (!string.IsNullOrWhiteSpace(scheme.BearerFormat))
        {
            openApiScheme.BearerFormat = scheme.BearerFormat;
        }

        if (!string.IsNullOrWhiteSpace(scheme.OpenIdConnectUrl) &&
            Uri.TryCreate(scheme.OpenIdConnectUrl, UriKind.Absolute, out var openIdConnectUrl))
        {
            openApiScheme.OpenIdConnectUrl = openIdConnectUrl;
        }

        return openApiScheme;
    }

    private static string? GetSecuritySchemeId(IOpenApiSecurityScheme scheme)
    {
        if (scheme is OpenApiSecuritySchemeReference reference &&
            !string.IsNullOrWhiteSpace(reference.Reference?.Id))
        {
            return reference.Reference.Id;
        }

        if (scheme is OpenApiSecurityScheme concreteScheme && !string.IsNullOrWhiteSpace(concreteScheme.Name))
        {
            return concreteScheme.Name;
        }

        return null;
    }

    private static OpenApiSecuritySchemeSettings[]? ReadSecuritySchemes(IConfigurationSection section)
    {
        var schemes = section.GetChildren()
            .Select(ReadSecurityScheme)
            .Where(static scheme => !string.IsNullOrWhiteSpace(scheme.Name))
            .ToArray();

        return schemes.Length == 0
            ? null
            : schemes;
    }

    private static OpenApiSecuritySchemeSettings ReadSecurityScheme(IConfigurationSection section)
    {
        return new OpenApiSecuritySchemeSettings
        {
            Name = section["Name"],
            Type = ReadEnum(section["Type"], SecuritySchemeType.Http),
            In = ReadEnum(section["In"], ParameterLocation.Header),
            Description = section["Description"],
            Scheme = section["Scheme"],
            BearerFormat = section["BearerFormat"],
            OpenIdConnectUrl = section["OpenIdConnectUrl"]
        };
    }

    private static TEnum ReadEnum<TEnum>(string? value, TEnum fallback)
        where TEnum : struct
    {
        return string.IsNullOrWhiteSpace(value) || !Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed)
            ? fallback
            : parsed;
    }
}
