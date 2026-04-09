using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using System.Net;

namespace Cephalon.AspNetCore.Transformers;

internal sealed class ResultModelDocumentTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        if (document.Paths is null)
            return Task.CompletedTask;

        var modelLinkPrefix = CreateModelLinkPrefix(context.DocumentName);

        foreach (var pathItem in document.Paths.Values)
        {
            if (pathItem?.Operations is null)
                continue;

            foreach (var operation in pathItem.Operations.Values)
            {
                if (operation?.Responses is null)
                    continue;

                foreach (var responseEntry in operation.Responses)
                {
                    if (!IsSuccessStatusCode(responseEntry.Key))
                        continue;

                    var response = responseEntry.Value;
                    if (response?.Content is null)
                        continue;

                    foreach (var mediaType in response.Content.Values)
                    {
                        var sourceSchema = ResolveSchema(mediaType.Schema, document);
                        if (sourceSchema is null)
                            continue;

                        if (IsResultModelSchema(sourceSchema) && ContainsErrorProperty(sourceSchema))
                        {
                            mediaType.Schema = CreateSuccessSchema(mediaType.Schema, sourceSchema, document);
                            var successSchema = ResolveSchema(mediaType.Schema, document);
                            if (successSchema is not null)
                            {
                                AppendModelLink(response, mediaType.Schema, successSchema, document, modelLinkPrefix);
                            }

                            continue;
                        }

                        ApplyReadableSchemaTitle(mediaType.Schema, sourceSchema, document);
                        AppendModelLink(response, mediaType.Schema, sourceSchema, document, modelLinkPrefix);
                    }
                }
            }
        }

        return Task.CompletedTask;
    }

    private static bool IsSuccessStatusCode(string? statusCode)
        => int.TryParse(statusCode, out var parsed) && parsed is >= 200 and <= 299;

    private static bool IsResultModelSchema(OpenApiSchema schema)
    {
        if (schema.Properties is null || schema.Properties.Count == 0)
            return false;

        return schema.Properties.Keys.Contains("data", StringComparer.OrdinalIgnoreCase) &&
               schema.Properties.Keys.Contains("status_code", StringComparer.OrdinalIgnoreCase);
    }

    private static bool ContainsErrorProperty(OpenApiSchema schema)
        => schema.Properties?.Keys.Contains("errors", StringComparer.OrdinalIgnoreCase) == true ||
           schema.Properties?.Keys.Contains("error", StringComparer.OrdinalIgnoreCase) == true;

    private static void ApplyReadableSchemaTitle(IOpenApiSchema? originalSchema, OpenApiSchema resolvedSchema, OpenApiDocument document)
    {
        var displayName = GetResponseDisplayName(originalSchema, resolvedSchema, document);
        if (string.IsNullOrWhiteSpace(displayName))
            return;

        if (originalSchema is OpenApiSchema openApiSchema)
        {
            openApiSchema.Title ??= displayName;
        }

        resolvedSchema.Title ??= displayName;
    }

    private static OpenApiSchemaReference CreateSuccessSchema(IOpenApiSchema? originalSchema, OpenApiSchema resolvedSchema, OpenApiDocument document)
    {
        var payloadDisplayName = GetResultPayloadDisplayName(resolvedSchema, document);
        var successDisplayName = string.IsNullOrWhiteSpace(payloadDisplayName)
            ? "ResultModel<object>"
            : $"ResultModel<{payloadDisplayName}>";
        var responseDisplayName = string.IsNullOrWhiteSpace(payloadDisplayName)
            ? successDisplayName
            : payloadDisplayName;
        var successSchemaId = CreateSuccessSchemaComponentId(successDisplayName, document);

        document.Components ??= new OpenApiComponents();
        document.Components.Schemas ??= new Dictionary<string, IOpenApiSchema>(StringComparer.OrdinalIgnoreCase);

        if (!document.Components.Schemas.ContainsKey(successSchemaId))
        {
            var successSchema = CloneWithoutErrors(resolvedSchema);
            successSchema.Title = responseDisplayName;
            document.Components.Schemas[successSchemaId] = successSchema;
        }
        else if (document.Components.Schemas[successSchemaId] is OpenApiSchema existingSchema &&
                 !string.Equals(existingSchema.Title, responseDisplayName, StringComparison.Ordinal))
        {
            existingSchema.Title = responseDisplayName;
        }

        return new OpenApiSchemaReference(successSchemaId, document)
        {
            Title = responseDisplayName,
            Description = resolvedSchema.Description
        };
    }

    private static string? GetResultPayloadDisplayName(OpenApiSchema resultModelSchema, OpenApiDocument document)
    {
        if (resultModelSchema.Properties is null ||
            !resultModelSchema.Properties.TryGetValue("data", out var dataSchemaReference) ||
            dataSchemaReference is null)
        {
            return null;
        }

        var dataSchema = ResolveSchema(dataSchemaReference, document);
        return dataSchema is null
            ? null
            : GetSchemaDisplayName(dataSchemaReference, dataSchema, document);
    }

    private static string? GetSchemaDisplayName(IOpenApiSchema? originalSchema, OpenApiSchema resolvedSchema, OpenApiDocument document)
    {
        var schemaId = TryGetSchemaComponentId(originalSchema, document) ??
                       TryGetSchemaComponentId(resolvedSchema, document);
        if (!string.IsNullOrWhiteSpace(schemaId))
            return schemaId;

        var composedDisplayName = GetComposedSchemaDisplayName(resolvedSchema, document);
        if (!string.IsNullOrWhiteSpace(composedDisplayName))
            return composedDisplayName;

        if (!string.IsNullOrWhiteSpace(resolvedSchema.Title))
            return resolvedSchema.Title;

        if (resolvedSchema.Type.HasValue && resolvedSchema.Type.Value.HasFlag(JsonSchemaType.Array) && resolvedSchema.Items is not null)
        {
            var itemSchema = ResolveSchema(resolvedSchema.Items, document);
            var itemDisplayName = itemSchema is null
                ? null
                : GetSchemaDisplayName(resolvedSchema.Items, itemSchema, document);

            return string.IsNullOrWhiteSpace(itemDisplayName)
                ? "Array"
                : $"{itemDisplayName}[]";
        }

        return GetPrimitiveTypeDisplayName(resolvedSchema.Type) ?? resolvedSchema.Type?.ToString();
    }

    private static string? GetResponseDisplayName(IOpenApiSchema? originalSchema, OpenApiSchema resolvedSchema, OpenApiDocument document)
    {
        if (IsResultModelSchema(resolvedSchema))
        {
            var payloadDisplayName = GetResultPayloadDisplayName(resolvedSchema, document);
            if (!string.IsNullOrWhiteSpace(payloadDisplayName))
                return payloadDisplayName;
        }

        return GetSchemaDisplayName(originalSchema, resolvedSchema, document);
    }

    private static void AppendModelLink(IOpenApiResponse response, IOpenApiSchema? schema, OpenApiSchema resolvedSchema, OpenApiDocument document, string modelLinkPrefix)
    {
        var displayName = GetResponseDisplayName(schema, resolvedSchema, document);
        if (string.IsNullOrWhiteSpace(displayName))
            return;

        var navigationTarget = GetModelNavigationTarget(schema, resolvedSchema, document) ?? displayName;
        if (string.IsNullOrWhiteSpace(navigationTarget))
            return;

        var href = $"{modelLinkPrefix}{navigationTarget}";
        if (!string.IsNullOrWhiteSpace(response.Description) &&
            response.Description.Contains(href, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var linkMarkup = $"<p><strong>Model:</strong> <a href=\"{href}\">{WebUtility.HtmlEncode(displayName)}</a></p>";
        response.Description = string.IsNullOrWhiteSpace(response.Description)
            ? linkMarkup
            : response.Description + Environment.NewLine + linkMarkup;
    }

    private static string CreateModelLinkPrefix(string? documentName)
    {
        if (string.IsNullOrWhiteSpace(documentName))
            return "#model/";

        var trimmedDocumentName = documentName.Trim();
        if (trimmedDocumentName.Length > 1 &&
            (trimmedDocumentName[0] == 'v' || trimmedDocumentName[0] == 'V') &&
            int.TryParse(trimmedDocumentName[1..], out var versionNumber))
        {
            return $"#version-{versionNumber}/model/";
        }

        return $"#{trimmedDocumentName}/model/";
    }

    private static string? GetModelNavigationTarget(IOpenApiSchema? originalSchema, OpenApiSchema resolvedSchema, OpenApiDocument document)
    {
        if (IsResultModelSchema(resolvedSchema))
        {
            var payloadTarget = GetResultPayloadNavigationTarget(resolvedSchema, document);
            if (!string.IsNullOrWhiteSpace(payloadTarget))
                return payloadTarget;
        }

        var displayName = GetSchemaDisplayName(originalSchema, resolvedSchema, document);
        return NormalizeNavigationTarget(displayName);
    }

    private static string? GetResultPayloadNavigationTarget(OpenApiSchema resultModelSchema, OpenApiDocument document)
    {
        if (resultModelSchema.Properties is null ||
            !resultModelSchema.Properties.TryGetValue("data", out var dataSchemaReference) ||
            dataSchemaReference is null)
        {
            return null;
        }

        return GetSchemaNavigationTarget(dataSchemaReference, document);
    }

    private static string? GetSchemaNavigationTarget(IOpenApiSchema schema, OpenApiDocument document)
    {
        var resolvedSchema = ResolveSchema(schema, document);
        if (resolvedSchema is null)
            return null;

        var schemaId = TryGetSchemaComponentId(schema, document) ??
                       TryGetSchemaComponentId(resolvedSchema, document);
        if (!string.IsNullOrWhiteSpace(schemaId))
            return NormalizeNavigationTarget(schemaId);

        var composedSchemas = resolvedSchema.OneOf?.Count > 0
            ? resolvedSchema.OneOf
            : resolvedSchema.AnyOf?.Count > 0
                ? resolvedSchema.AnyOf
                : resolvedSchema.AllOf?.Count > 0
                    ? resolvedSchema.AllOf
                    : null;

        if (composedSchemas is not null)
        {
            foreach (var composedSchema in composedSchemas)
            {
                var composedResolvedSchema = ResolveSchema(composedSchema, document);
                if (composedResolvedSchema is null || IsNullSchema(composedResolvedSchema))
                    continue;

                var target = GetSchemaNavigationTarget(composedSchema, document);
                if (!string.IsNullOrWhiteSpace(target))
                    return target;
            }
        }

        if (resolvedSchema.Type.HasValue &&
            resolvedSchema.Type.Value.HasFlag(JsonSchemaType.Array) &&
            resolvedSchema.Items is not null)
        {
            return GetSchemaNavigationTarget(resolvedSchema.Items, document);
        }

        return NormalizeNavigationTarget(GetSchemaDisplayName(schema, resolvedSchema, document));
    }

    private static string? NormalizeNavigationTarget(string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return null;

        var trimmedDisplayName = displayName.Trim();
        if (trimmedDisplayName.EndsWith("[]", StringComparison.Ordinal))
        {
            trimmedDisplayName = trimmedDisplayName[..^2];
        }

        if (trimmedDisplayName.Contains('<') && trimmedDisplayName.Contains('>'))
        {
            var genericStart = trimmedDisplayName.IndexOf('<');
            var genericEnd = trimmedDisplayName.LastIndexOf('>');
            if (genericStart >= 0 && genericEnd > genericStart)
            {
                trimmedDisplayName = trimmedDisplayName[(genericStart + 1)..genericEnd].Trim();
            }
        }

        if (trimmedDisplayName.Contains('|'))
        {
            trimmedDisplayName = trimmedDisplayName
                .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault() ?? trimmedDisplayName;
        }

        if (string.Equals(trimmedDisplayName, "object", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(trimmedDisplayName, "array", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(trimmedDisplayName, "null", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(trimmedDisplayName, "string", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(trimmedDisplayName, "integer", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(trimmedDisplayName, "number", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(trimmedDisplayName, "boolean", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return trimmedDisplayName;
    }

    private static string CreateSuccessSchemaComponentId(string successDisplayName, OpenApiDocument document)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.Schemas ??= new Dictionary<string, IOpenApiSchema>(StringComparer.OrdinalIgnoreCase);

        var candidate = string.IsNullOrWhiteSpace(successDisplayName)
            ? "ResultModel<object>"
            : successDisplayName.Trim();

        candidate = candidate
            .Replace("/", "_", StringComparison.Ordinal)
            .Replace("~", "_", StringComparison.Ordinal);

        return candidate;
    }

    private static string? GetComposedSchemaDisplayName(OpenApiSchema schema, OpenApiDocument document)
    {
        var composedSchemas = schema.OneOf?.Count > 0
            ? schema.OneOf
            : schema.AnyOf?.Count > 0
                ? schema.AnyOf
                : schema.AllOf?.Count > 0
                    ? schema.AllOf
                    : null;

        if (composedSchemas is null || composedSchemas.Count == 0)
            return null;

        var names = new List<string>();
        foreach (var composedSchema in composedSchemas)
        {
            var resolvedSchema = ResolveSchema(composedSchema, document);
            if (resolvedSchema is null || IsNullSchema(resolvedSchema))
                continue;

            var name = GetSchemaDisplayName(composedSchema, resolvedSchema, document);
            if (string.IsNullOrWhiteSpace(name) ||
                string.Equals(name, "null", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            names.Add(name);
        }

        names = names
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (names.Count == 0)
            return null;

        if (names.Count == 1)
            return names[0];

        var filteredNames = names
            .Where(name => !string.Equals(name, "object", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (filteredNames.Count == 1)
            return filteredNames[0];

        return string.Join(" | ", filteredNames.Count > 0 ? filteredNames : names);
    }

    private static string? GetPrimitiveTypeDisplayName(JsonSchemaType? schemaType)
    {
        if (!schemaType.HasValue)
            return null;

        var nonNullTypes = new List<string>();
        if (schemaType.Value.HasFlag(JsonSchemaType.String))
            nonNullTypes.Add("string");
        if (schemaType.Value.HasFlag(JsonSchemaType.Integer))
            nonNullTypes.Add("integer");
        if (schemaType.Value.HasFlag(JsonSchemaType.Number))
            nonNullTypes.Add("number");
        if (schemaType.Value.HasFlag(JsonSchemaType.Boolean))
            nonNullTypes.Add("boolean");
        if (schemaType.Value.HasFlag(JsonSchemaType.Object))
            nonNullTypes.Add("object");
        if (schemaType.Value.HasFlag(JsonSchemaType.Array))
            nonNullTypes.Add("array");

        nonNullTypes = nonNullTypes
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return nonNullTypes.Count switch
        {
            0 => schemaType.Value.HasFlag(JsonSchemaType.Null) ? "null" : null,
            1 => nonNullTypes[0],
            _ => string.Join(" | ", nonNullTypes)
        };
    }

    private static bool IsNullSchema(OpenApiSchema schema)
    {
        if (schema.OneOf?.Count > 0 || schema.AnyOf?.Count > 0 || schema.AllOf?.Count > 0)
            return false;

        if (!schema.Type.HasValue)
            return false;

        var schemaType = schema.Type.Value;
        return schemaType.HasFlag(JsonSchemaType.Null) &&
               !schemaType.HasFlag(JsonSchemaType.Object) &&
               !schemaType.HasFlag(JsonSchemaType.Array) &&
               !schemaType.HasFlag(JsonSchemaType.String) &&
               !schemaType.HasFlag(JsonSchemaType.Integer) &&
               !schemaType.HasFlag(JsonSchemaType.Number) &&
               !schemaType.HasFlag(JsonSchemaType.Boolean);
    }

    private static OpenApiSchema? ResolveSchema(IOpenApiSchema? schema, OpenApiDocument document)
    {
        var schemaId = TryGetSchemaComponentId(schema, document);
        if (!string.IsNullOrWhiteSpace(schemaId) &&
            document.Components?.Schemas is not null &&
            document.Components.Schemas.TryGetValue(schemaId, out var referencedSchema))
        {
            return UnwrapSchema(referencedSchema);
        }

        return UnwrapSchema(schema);
    }

    private static OpenApiSchema? UnwrapSchema(IOpenApiSchema? schema)
    {
        if (schema is null)
            return null;

        if (schema is OpenApiSchema concreteSchema)
            return concreteSchema;

        return TryGetNestedSchema(schema, "RecursiveTarget")
               ?? TryGetNestedSchema(schema, "Target")
               ?? TryGetNestedSchema(schema, "Value")
               ?? TryGetNestedSchema(schema, "Schema");
    }

    private static string? TryGetSchemaComponentId(IOpenApiSchema? schema, OpenApiDocument document)
    {
        if (schema is null)
            return null;

        var referenceId = TryGetReferenceId(schema);
        if (!string.IsNullOrWhiteSpace(referenceId))
            return referenceId;

        var schemaIdValue = TryGetSchemaIdValue(schema);
        if (!string.IsNullOrWhiteSpace(schemaIdValue) &&
            document.Components?.Schemas is not null &&
            document.Components.Schemas.ContainsKey(schemaIdValue))
        {
            return schemaIdValue;
        }

        var concreteSchema = UnwrapSchema(schema);
        if (concreteSchema is null || document.Components?.Schemas is null)
            return null;

        foreach (var schemaEntry in document.Components.Schemas)
        {
            if (ReferenceEquals(UnwrapSchema(schemaEntry.Value), concreteSchema))
                return schemaEntry.Key;
        }

        return null;
    }

    private static string? TryGetReferenceId(IOpenApiSchema? schema)
    {
        if (schema is null)
            return null;

        try
        {
            var targetElementIdProperty = schema.GetType().GetProperty("TargetElementId");
            if (targetElementIdProperty?.GetValue(schema) is string targetElementId &&
                !string.IsNullOrWhiteSpace(targetElementId))
            {
                return targetElementId;
            }
        }
        catch
        {
            // ignored
        }

        try
        {
            var referenceProperty = schema.GetType().GetProperty("Reference");
            var reference = referenceProperty?.GetValue(schema);
            if (reference is null)
                return null;

            var idProperty = reference.GetType().GetProperty("Id") ?? reference.GetType().GetProperty("id");
            if (idProperty?.GetValue(reference) is string referenceId && !string.IsNullOrWhiteSpace(referenceId))
                return referenceId;
        }
        catch
        {
            // ignored
        }

        return null;
    }

    private static string? TryGetSchemaIdValue(IOpenApiSchema? schema)
    {
        if (schema is null)
            return null;

        if (schema is OpenApiSchema openApiSchema && !string.IsNullOrWhiteSpace(openApiSchema.Id))
            return openApiSchema.Id;

        try
        {
            var idProperty = schema.GetType().GetProperty("Id") ?? schema.GetType().GetProperty("id");
            if (idProperty?.GetValue(schema) is string schemaId && !string.IsNullOrWhiteSpace(schemaId))
                return schemaId;
        }
        catch
        {
            // ignored
        }

        return null;
    }

    private static OpenApiSchema? TryGetNestedSchema(object instance, string propertyName)
    {
        try
        {
            var property = instance.GetType().GetProperty(propertyName);
            return property?.GetValue(instance) switch
            {
                OpenApiSchema openApiSchema => openApiSchema,
                IOpenApiSchema nestedSchema => UnwrapSchema(nestedSchema),
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    private static OpenApiSchema CloneWithoutErrors(OpenApiSchema source)
    {
        var clone = new OpenApiSchema
        {
            Title = source.Title,
            Description = source.Description,
            Type = source.Type,
            Format = source.Format,
            Deprecated = source.Deprecated,
            Example = source.Example,
            Default = source.Default,
            Items = source.Items,
            AdditionalProperties = source.AdditionalProperties,
            AdditionalPropertiesAllowed = source.AdditionalPropertiesAllowed,
            AllOf = source.AllOf?.ToList(),
            OneOf = source.OneOf?.ToList(),
            AnyOf = source.AnyOf?.ToList(),
            Extensions = source.Extensions?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
        };

        if (source.Properties is not null)
        {
            clone.Properties = source.Properties
                .Where(kvp =>
                    !string.Equals(kvp.Key, "errors", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(kvp.Key, "error", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);
        }

        if (source.Required is not null)
        {
            clone.Required = source.Required
                .Where(name =>
                    !string.Equals(name, "errors", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(name, "error", StringComparison.OrdinalIgnoreCase))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        return clone;
    }
}
