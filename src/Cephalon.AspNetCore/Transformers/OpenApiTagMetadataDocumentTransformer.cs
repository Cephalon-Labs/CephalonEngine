using Cephalon.AspNetCore.Documentation;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Cephalon.AspNetCore.Transformers;

internal sealed class OpenApiTagMetadataDocumentTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var tags = context.DescriptionGroups
            .SelectMany(static group => group.Items)
            .SelectMany(GetTagMetadata)
            .GroupBy(static metadata => metadata.Name, StringComparer.OrdinalIgnoreCase)
            .Select(static group => new OpenApiTagMetadata(
                group.Key,
                group.Select(static item => item.Description)
                    .FirstOrDefault(static description => !string.IsNullOrWhiteSpace(description))))
            .OrderBy(static tag => tag.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (tags.Length == 0)
        {
            return Task.CompletedTask;
        }

        document.Tags ??= new HashSet<OpenApiTag>();
        foreach (var metadata in tags)
        {
            var existing = document.Tags.FirstOrDefault(tag =>
                string.Equals(tag.Name, metadata.Name, StringComparison.OrdinalIgnoreCase));
            if (existing is null)
            {
                document.Tags.Add(new OpenApiTag
                {
                    Name = metadata.Name,
                    Description = metadata.Description
                });
                continue;
            }

            if (string.IsNullOrWhiteSpace(existing.Description) &&
                !string.IsNullOrWhiteSpace(metadata.Description))
            {
                existing.Description = metadata.Description;
            }
        }

        return Task.CompletedTask;
    }

    private static IEnumerable<OpenApiTagMetadata> GetTagMetadata(ApiDescription description)
    {
        return description.ActionDescriptor.EndpointMetadata
            .OfType<OpenApiTagMetadata>()
            .Where(static metadata => !string.IsNullOrWhiteSpace(metadata.Name));
    }
}
