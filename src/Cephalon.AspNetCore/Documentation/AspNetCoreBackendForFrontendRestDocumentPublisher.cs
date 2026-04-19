using System.Text.Json;
using System.Text.Json.Nodes;
using Cephalon.Abstractions.Patterns;
using Cephalon.Abstractions.Transports;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace Cephalon.AspNetCore.Documentation;

internal sealed class AspNetCoreBackendForFrontendRestDocumentPublisher(
    IServiceProvider serviceProvider,
    IBackendForFrontendRuntimeCatalog backendForFrontendRuntimeCatalog,
    IBackendForFrontendRestRuntimeCatalog backendForFrontendRestRuntimeCatalog,
    IBackendForFrontendRestDocumentRuntimeCatalog backendForFrontendRestDocumentRuntimeCatalog,
    IConfiguration configuration)
{
    private static readonly HashSet<string> OperationKeys =
    [
        "get",
        "put",
        "post",
        "delete",
        "options",
        "head",
        "patch",
        "trace"
    ];

    private readonly string defaultDocumentName = OpenApiDocumentNames.ResolveDefault(configuration);

    public Task<string?> GenerateBindingDocumentAsync(
        string bindingId,
        string documentName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(bindingId) || string.IsNullOrWhiteSpace(documentName))
        {
            return Task.FromResult<string?>(null);
        }

        var descriptor = backendForFrontendRestDocumentRuntimeCatalog
            .GetByBindingId(bindingId)
            .FirstOrDefault(document =>
                string.Equals(document.DocumentName, documentName.Trim(), StringComparison.OrdinalIgnoreCase));
        if (descriptor is null)
        {
            return Task.FromResult<string?>(null);
        }

        var binding = backendForFrontendRuntimeCatalog.GetById(bindingId.Trim());
        var scopeLabel = binding?.DisplayName ?? descriptor.ScopeId;
        var scopeDescription = binding?.Description;
        return GenerateDocumentAsync(descriptor, scopeLabel, scopeDescription, cancellationToken);
    }

    public Task<string?> GenerateClientDocumentAsync(
        string clientId,
        string documentName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(documentName))
        {
            return Task.FromResult<string?>(null);
        }

        var descriptor = backendForFrontendRestDocumentRuntimeCatalog
            .GetByClientId(clientId)
            .FirstOrDefault(document =>
                string.Equals(document.DocumentName, documentName.Trim(), StringComparison.OrdinalIgnoreCase));
        if (descriptor is null)
        {
            return Task.FromResult<string?>(null);
        }

        return GenerateDocumentAsync(
            descriptor,
            scopeLabel: descriptor.ClientId,
            scopeDescription: null,
            cancellationToken);
    }

    private async Task<string?> GenerateDocumentAsync(
        BackendForFrontendRestDocumentRuntimeDescriptor descriptor,
        string scopeLabel,
        string? scopeDescription,
        CancellationToken cancellationToken)
    {
        var sourcePayload = await GenerateSourceDocumentAsync(descriptor.DocumentName, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(sourcePayload))
        {
            return null;
        }

        var root = JsonNode.Parse(sourcePayload) as JsonObject;
        if (root is null)
        {
            return null;
        }

        var allowedOperations = BuildAllowedOperations(descriptor);
        if (!FilterPaths(root, allowedOperations))
        {
            return null;
        }

        PruneTags(root);
        PruneComponents(root);
        ApplyScopeMetadata(root, descriptor, scopeLabel, scopeDescription);

        return root.ToJsonString(new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    private async Task<string> GenerateSourceDocumentAsync(string documentName, CancellationToken cancellationToken)
    {
        var provider = serviceProvider.GetRequiredKeyedService<IOpenApiDocumentProvider>(documentName);
        var document = await provider.GetOpenApiDocumentAsync(cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return await document.SerializeAsJsonAsync(OpenApiSpecVersion.OpenApi3_0, cancellationToken).ConfigureAwait(false);
    }

    private Dictionary<string, HashSet<string>> BuildAllowedOperations(
        BackendForFrontendRestDocumentRuntimeDescriptor descriptor)
    {
        var allowedOperations = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var runtimeEndpointId in descriptor.RuntimeEndpointIds)
        {
            var runtimeEndpoint = backendForFrontendRestRuntimeCatalog.GetById(runtimeEndpointId);
            if (runtimeEndpoint is null)
            {
                continue;
            }

            var runtimeDocumentName = ResolveDocumentName(runtimeEndpoint.Endpoint.OpenApiDocumentName);
            if (!string.Equals(runtimeDocumentName, descriptor.DocumentName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!allowedOperations.TryGetValue(runtimeEndpoint.Endpoint.RoutePattern, out var methods))
            {
                methods = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                allowedOperations[runtimeEndpoint.Endpoint.RoutePattern] = methods;
            }

            methods.Add(runtimeEndpoint.Endpoint.Method);
        }

        return allowedOperations;
    }

    private static bool FilterPaths(JsonObject root, Dictionary<string, HashSet<string>> allowedOperations)
    {
        if (root["paths"] is not JsonObject paths)
        {
            return false;
        }

        var filteredPaths = new JsonObject();

        foreach (var path in paths)
        {
            if (!allowedOperations.TryGetValue(path.Key, out var allowedMethods) ||
                path.Value is not JsonObject pathItem)
            {
                continue;
            }

            var filteredPathItem = new JsonObject();
            var hasAllowedOperation = false;
            foreach (var property in pathItem)
            {
                if (property.Value is null)
                {
                    continue;
                }

                if (!OperationKeys.Contains(property.Key))
                {
                    filteredPathItem[property.Key] = property.Value.DeepClone();
                    continue;
                }

                if (!allowedMethods.Contains(property.Key))
                {
                    continue;
                }

                filteredPathItem[property.Key] = property.Value.DeepClone();
                hasAllowedOperation = true;
            }

            if (hasAllowedOperation)
            {
                filteredPaths[path.Key] = filteredPathItem;
            }
        }

        if (filteredPaths.Count == 0)
        {
            return false;
        }

        root["paths"] = filteredPaths;
        return true;
    }

    private static void PruneTags(JsonObject root)
    {
        if (root["tags"] is not JsonArray tags || root["paths"] is not JsonObject paths)
        {
            return;
        }

        var usedTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pathItem in paths.Select(static path => path.Value).OfType<JsonObject>())
        {
            foreach (var operation in pathItem
                         .Where(static property => OperationKeys.Contains(property.Key))
                         .Select(static property => property.Value)
                         .OfType<JsonObject>())
            {
                if (operation["tags"] is not JsonArray operationTags)
                {
                    continue;
                }

                foreach (var tag in operationTags.Select(static value => value?.GetValue<string>()).OfType<string>())
                {
                    usedTags.Add(tag);
                }
            }
        }

        if (usedTags.Count == 0)
        {
            root.Remove("tags");
            return;
        }

        var filteredTags = new JsonArray();
        foreach (var tag in tags.OfType<JsonObject>())
        {
            var tagName = tag["name"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(tagName) || !usedTags.Contains(tagName))
            {
                continue;
            }

            filteredTags.Add(tag.DeepClone());
        }

        if (filteredTags.Count == 0)
        {
            root.Remove("tags");
            return;
        }

        root["tags"] = filteredTags;
    }

    private static void PruneComponents(JsonObject root)
    {
        if (root["components"] is not JsonObject components)
        {
            return;
        }

        var neededReferences = new HashSet<ComponentReference>();
        CollectReferences(root, neededReferences, skipComponentsProperty: true);
        if (neededReferences.Count == 0)
        {
            root.Remove("components");
            return;
        }

        var pending = new Queue<ComponentReference>(neededReferences);
        while (pending.Count > 0)
        {
            var reference = pending.Dequeue();
            if (components[reference.Section] is not JsonObject section ||
                !section.TryGetPropertyValue(reference.Id, out var componentNode) ||
                componentNode is null)
            {
                continue;
            }

            var nestedReferences = new HashSet<ComponentReference>();
            CollectReferences(componentNode, nestedReferences, skipComponentsProperty: false);
            foreach (var nestedReference in nestedReferences)
            {
                if (neededReferences.Add(nestedReference))
                {
                    pending.Enqueue(nestedReference);
                }
            }
        }

        var filteredComponents = new JsonObject();
        foreach (var sectionEntry in components)
        {
            if (sectionEntry.Value is not JsonObject section)
            {
                continue;
            }

            var filteredSection = new JsonObject();
            foreach (var component in section)
            {
                var reference = new ComponentReference(sectionEntry.Key, component.Key);
                if (!neededReferences.Contains(reference) || component.Value is null)
                {
                    continue;
                }

                filteredSection[component.Key] = component.Value.DeepClone();
            }

            if (filteredSection.Count > 0)
            {
                filteredComponents[sectionEntry.Key] = filteredSection;
            }
        }

        if (filteredComponents.Count == 0)
        {
            root.Remove("components");
            return;
        }

        root["components"] = filteredComponents;
    }

    private static void CollectReferences(
        JsonNode? node,
        ISet<ComponentReference> references,
        bool skipComponentsProperty)
    {
        if (node is null)
        {
            return;
        }

        if (node is JsonObject obj)
        {
            foreach (var property in obj)
            {
                if (skipComponentsProperty &&
                    string.Equals(property.Key, "components", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (string.Equals(property.Key, "$ref", StringComparison.Ordinal) &&
                    property.Value is JsonValue referenceValue &&
                    referenceValue.TryGetValue<string>(out var referencePath) &&
                    TryParseComponentReference(referencePath, out var reference))
                {
                    references.Add(reference);
                    continue;
                }

                CollectReferences(property.Value, references, skipComponentsProperty: false);
            }

            return;
        }

        if (node is JsonArray array)
        {
            foreach (var item in array)
            {
                CollectReferences(item, references, skipComponentsProperty: false);
            }
        }
    }

    private static bool TryParseComponentReference(string referencePath, out ComponentReference reference)
    {
        reference = default;
        if (string.IsNullOrWhiteSpace(referencePath) ||
            !referencePath.StartsWith("#/components/", StringComparison.Ordinal))
        {
            return false;
        }

        var segments = referencePath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length != 4)
        {
            return false;
        }

        reference = new ComponentReference(segments[2], segments[3]);
        return true;
    }

    private static void ApplyScopeMetadata(
        JsonObject root,
        BackendForFrontendRestDocumentRuntimeDescriptor descriptor,
        string scopeLabel,
        string? scopeDescription)
    {
        var info = root["info"] as JsonObject ?? new JsonObject();
        root["info"] = info;

        var existingTitle = info["title"]?.GetValue<string>() ?? "Cephalon REST API";
        var suffix = string.Equals(descriptor.Kind, BackendForFrontendRestDocumentRuntimeDescriptor.BindingKind, StringComparison.Ordinal)
            ? $"BFF Binding: {scopeLabel}"
            : $"BFF Client: {scopeLabel}";
        info["title"] = $"{existingTitle} ({suffix})";

        var suffixDescription = string.Equals(descriptor.Kind, BackendForFrontendRestDocumentRuntimeDescriptor.BindingKind, StringComparison.Ordinal)
            ? $"Filtered for backend-for-frontend binding '{descriptor.ScopeId}'."
            : $"Filtered for backend-for-frontend client '{descriptor.ClientId}'.";
        if (!string.IsNullOrWhiteSpace(scopeDescription))
        {
            suffixDescription = $"{suffixDescription} {scopeDescription.Trim()}";
        }

        var existingDescription = info["description"]?.GetValue<string>();
        info["description"] = string.IsNullOrWhiteSpace(existingDescription)
            ? suffixDescription
            : $"{existingDescription.Trim()} {suffixDescription}";

        root["x-cephalon-backend-for-frontend"] = new JsonObject
        {
            ["kind"] = descriptor.Kind,
            ["scopeId"] = descriptor.ScopeId,
            ["clientId"] = descriptor.ClientId,
            ["documentName"] = descriptor.DocumentName,
            ["bindingIds"] = new JsonArray(descriptor.BindingIds.Select(static value => (JsonNode?)JsonValue.Create(value)).ToArray()),
            ["runtimeEndpointIds"] = new JsonArray(descriptor.RuntimeEndpointIds.Select(static value => (JsonNode?)JsonValue.Create(value)).ToArray()),
            ["restEndpointIds"] = new JsonArray(descriptor.RestEndpointIds.Select(static value => (JsonNode?)JsonValue.Create(value)).ToArray())
        };
    }

    private string ResolveDocumentName(string? documentName)
    {
        return string.IsNullOrWhiteSpace(documentName)
            ? defaultDocumentName
            : documentName.Trim();
    }

    private readonly record struct ComponentReference(string Section, string Id);
}
