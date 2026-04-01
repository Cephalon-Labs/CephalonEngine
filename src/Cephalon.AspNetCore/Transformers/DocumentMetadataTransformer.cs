using Cephalon.Abstractions.Localization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi;
using System.Globalization;

namespace Cephalon.AspNetCore.Transformers;

internal sealed class DocumentMetadataTransformer(
    IConfiguration configuration,
    ILocalizedTextCatalog localizedTextCatalog) : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Info ??= new OpenApiInfo();
        document.Info.Title = GetSetting(
            "Title",
            fallbackValue: localizedTextCatalog.ResolveText(
                "engine.docs.rest.title",
                CultureInfo.CurrentUICulture.Name,
                fallback: "Cephalon REST API"));
        document.Info.Version = GetSetting("Version", fallbackValue: context.DocumentName ?? "v1");

        if (string.IsNullOrWhiteSpace(document.Info.Description))
        {
            document.Info.Description = GetSetting(
                "Description",
                fallbackValue: localizedTextCatalog.ResolveText(
                    "engine.docs.rest.description",
                    CultureInfo.CurrentUICulture.Name,
                    fallback: "REST surface exposed by the Cephalon ASP.NET Core host."));
        }

        return Task.CompletedTask;
    }

    private string GetSetting(string key, string fallbackValue)
    {
        var value = configuration[$"OpenApi:{key}"];
        return string.IsNullOrWhiteSpace(value) ? fallbackValue : value.Trim();
    }
}
