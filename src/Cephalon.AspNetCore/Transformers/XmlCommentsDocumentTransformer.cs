using System.Xml.Linq;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Cephalon.AspNetCore.Transformers;

/// <summary>
/// Enriches generated OpenAPI schemas with XML documentation comments discovered from loaded assemblies.
/// </summary>
/// <param name="xmlFiles">
/// Optional explicit XML documentation files to scan. When omitted, the transformer searches the
/// current application assemblies and base directory for generated XML documentation files.
/// </param>
/// <remarks>
/// This transformer keeps OpenAPI descriptions aligned with the XML comments written on public
/// contracts, including summaries, remarks, and examples when available.
/// </remarks>
public sealed class XmlCommentsDocumentTransformer(string[]? xmlFiles = null) : IOpenApiDocumentTransformer
{
    private static readonly char[] LineBreakChars = ['\r', '\n'];

    /// <summary>
    /// Applies XML comment data to the OpenAPI document schemas produced for the current request.
    /// </summary>
    /// <param name="document">The OpenAPI document being transformed.</param>
    /// <param name="context">The transformation context for the current document generation.</param>
    /// <param name="cancellationToken">A token that can cancel document transformation.</param>
    /// <returns>A task that completes when the transformation has finished.</returns>
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (document.Components?.Schemas is null || document.Components.Schemas.Count == 0)
        {
            return Task.CompletedTask;
        }

        var comments = LoadComments(xmlFiles);
        if (comments.Count == 0)
        {
            return Task.CompletedTask;
        }

        foreach (var (schemaId, schemaEntry) in document.Components.Schemas)
        {
            var schema = ResolveSchema(schemaEntry, document);
            if (schema is null)
            {
                continue;
            }

            if (TryGetTypeComments(comments, schemaId, out var typeComments))
            {
                ApplyComments(schema, typeComments);
            }

            if (schema.Properties is null || schema.Properties.Count == 0)
            {
                continue;
            }

            foreach (var (propertyName, propertySchemaEntry) in schema.Properties)
            {
                var propertySchema = ResolveSchema(propertySchemaEntry, document);
                if (propertySchema is null)
                {
                    continue;
                }

                if (TryGetPropertyComments(comments, schemaId, propertyName, out var propertyComments))
                {
                    ApplyComments(propertySchema, propertyComments);
                }
            }
        }

        return Task.CompletedTask;
    }

    private static Dictionary<string, MemberComments> LoadComments(string[]? explicitXmlFiles)
    {
        var members = new Dictionary<string, MemberComments>(StringComparer.Ordinal);
        foreach (var xmlPath in ResolveXmlPaths(explicitXmlFiles))
        {
            try
            {
                var document = XDocument.Load(xmlPath);
                var xmlMembers = document.Root?
                    .Element("members")?
                    .Elements("member");

                if (xmlMembers is null)
                {
                    continue;
                }

                foreach (var member in xmlMembers)
                {
                    var memberName = (string?)member.Attribute("name");
                    if (string.IsNullOrWhiteSpace(memberName) || members.ContainsKey(memberName))
                    {
                        continue;
                    }

                    members[memberName] = new MemberComments(
                        Summary: Normalize(member.Element("summary")?.Value),
                        Remarks: Normalize(member.Element("remarks")?.Value),
                        Example: Normalize(member.Element("example")?.Value));
                }
            }
            catch
            {
                // Documentation generation should stay best-effort.
            }
        }

        return members;
    }

    private static string[] ResolveXmlPaths(string[]? explicitXmlFiles)
    {
        if (explicitXmlFiles is { Length: > 0 })
        {
            return explicitXmlFiles
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Where(File.Exists)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        var baseDirectory = AppContext.BaseDirectory;
        var assemblyXmlFiles = AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetName().Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => Path.Combine(baseDirectory, $"{name}.xml"))
            .Where(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path))
            .Select(path => path!)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        var baseDirectoryXmlFiles = Directory.Exists(baseDirectory)
            ? Directory.GetFiles(baseDirectory, "*.xml", SearchOption.TopDirectoryOnly)
            : [];

        return assemblyXmlFiles
            .Concat(baseDirectoryXmlFiles)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool TryGetTypeComments(
        IReadOnlyDictionary<string, MemberComments> comments,
        string schemaId,
        out MemberComments memberComments)
    {
        foreach (var (memberName, value) in comments)
        {
            if (!memberName.StartsWith("T:", StringComparison.Ordinal))
            {
                continue;
            }

            var typeName = memberName[2..];
            if (MatchesTypeName(typeName, schemaId))
            {
                memberComments = value;
                return true;
            }
        }

        memberComments = default;
        return false;
    }

    private static bool TryGetPropertyComments(
        IReadOnlyDictionary<string, MemberComments> comments,
        string schemaId,
        string propertyName,
        out MemberComments memberComments)
    {
        var candidateNames = GetPropertyCandidates(propertyName);

        foreach (var (memberName, value) in comments)
        {
            if (!memberName.StartsWith("P:", StringComparison.Ordinal))
            {
                continue;
            }

            var propertyPath = memberName[2..];
            var separatorIndex = propertyPath.LastIndexOf('.');
            if (separatorIndex <= 0 || separatorIndex >= propertyPath.Length - 1)
            {
                continue;
            }

            var typeName = propertyPath[..separatorIndex];
            var xmlPropertyName = propertyPath[(separatorIndex + 1)..];
            if (!MatchesTypeName(typeName, schemaId) ||
                !candidateNames.Contains(xmlPropertyName, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            memberComments = value;
            return true;
        }

        memberComments = default;
        return false;
    }

    private static bool MatchesTypeName(string typeName, string schemaId)
    {
        var simpleTypeName = typeName[(typeName.LastIndexOf('.') + 1)..];
        var normalizedTypeName = RemoveGenericSuffix(simpleTypeName);
        var normalizedSchemaId = RemoveGenericSuffix(schemaId);

        return string.Equals(normalizedTypeName, normalizedSchemaId, StringComparison.OrdinalIgnoreCase);
    }

    private static string RemoveGenericSuffix(string value)
    {
        var separatorIndex = value.IndexOf('`');
        return separatorIndex < 0 ? value : value[..separatorIndex];
    }

    private static string[] GetPropertyCandidates(string propertyName)
    {
        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            propertyName
        };

        if (!string.IsNullOrWhiteSpace(propertyName))
        {
            candidates.Add(char.ToUpperInvariant(propertyName[0]) + propertyName[1..]);
            candidates.Add(char.ToLowerInvariant(propertyName[0]) + propertyName[1..]);
        }

        return candidates.ToArray();
    }

    private static void ApplyComments(OpenApiSchema schema, MemberComments comments)
    {
        var sections = new List<string>();
        if (!string.IsNullOrWhiteSpace(comments.Summary))
        {
            sections.Add(comments.Summary);
        }

        if (!string.IsNullOrWhiteSpace(comments.Remarks))
        {
            sections.Add($"Remarks: {comments.Remarks}");
        }

        if (!string.IsNullOrWhiteSpace(comments.Example))
        {
            sections.Add($"Example: {comments.Example}");
        }

        if (sections.Count == 0)
        {
            return;
        }

        var description = string.Join(Environment.NewLine + Environment.NewLine, sections);
        schema.Description = string.IsNullOrWhiteSpace(schema.Description)
            ? description
            : description + Environment.NewLine + Environment.NewLine + schema.Description;
    }

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return string.Join(
            " ",
            value.Split(LineBreakChars, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    private static OpenApiSchema? ResolveSchema(IOpenApiSchema? schema, OpenApiDocument document)
    {
        if (schema is null)
        {
            return null;
        }

        if (schema is OpenApiSchema concreteSchema)
        {
            return concreteSchema;
        }

        var targetElementIdProperty = schema.GetType().GetProperty("TargetElementId");
        if (targetElementIdProperty?.GetValue(schema) is string targetElementId &&
            !string.IsNullOrWhiteSpace(targetElementId) &&
            document.Components?.Schemas is not null &&
            document.Components.Schemas.TryGetValue(targetElementId, out var referencedSchema))
        {
            return ResolveSchema(referencedSchema, document);
        }

        var targetProperty = schema.GetType().GetProperty("Target") ??
                             schema.GetType().GetProperty("RecursiveTarget") ??
                             schema.GetType().GetProperty("Value");
        if (targetProperty?.GetValue(schema) is IOpenApiSchema targetSchema)
        {
            return ResolveSchema(targetSchema, document);
        }

        return null;
    }

    private readonly record struct MemberComments(string? Summary, string? Remarks, string? Example);
}
