using Cephalon.Abstractions.Transports;
using Cephalon.AspNetCore.Documentation;
using Microsoft.Extensions.Configuration;

namespace Cephalon.AspNetCore.Documentation;

internal sealed class AspNetCoreBackendForFrontendRestDocumentRuntimeCatalog : IBackendForFrontendRestDocumentRuntimeCatalog
{
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

    private readonly BackendForFrontendRestDocumentRuntimeDescriptor[] documents;
    private readonly Dictionary<string, BackendForFrontendRestDocumentRuntimeDescriptor> documentsById;
    private readonly Dictionary<string, IReadOnlyList<BackendForFrontendRestDocumentRuntimeDescriptor>> documentsByBindingId;
    private readonly Dictionary<string, IReadOnlyList<BackendForFrontendRestDocumentRuntimeDescriptor>> documentsByClientId;

    public AspNetCoreBackendForFrontendRestDocumentRuntimeCatalog(
        IBackendForFrontendRestRuntimeCatalog backendForFrontendRestRuntimeCatalog,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(backendForFrontendRestRuntimeCatalog);
        ArgumentNullException.ThrowIfNull(configuration);

        var documentNames = OpenApiDocumentNames.Resolve(configuration);
        var openApiOptions = OpenApiEndpointOptions.FromConfiguration(configuration);
        var defaultDocumentName = OpenApiDocumentNames.ResolveDefault(configuration);
        documents = BuildDocuments(
            backendForFrontendRestRuntimeCatalog.Endpoints,
            documentNames,
            defaultDocumentName,
            openApiOptions.RoutePattern,
            openApiOptions.ScalarRoutePrefix);
        documentsById = documents.ToDictionary(static document => document.Id, Comparer);
        documentsByBindingId = documents
            .Where(static document => string.Equals(document.Kind, BackendForFrontendRestDocumentRuntimeDescriptor.BindingKind, StringComparison.Ordinal))
            .GroupBy(static document => document.ScopeId, Comparer)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<BackendForFrontendRestDocumentRuntimeDescriptor>)group.ToArray(),
                Comparer);
        documentsByClientId = documents
            .Where(static document => string.Equals(document.Kind, BackendForFrontendRestDocumentRuntimeDescriptor.ClientKind, StringComparison.Ordinal))
            .GroupBy(static document => document.ClientId, Comparer)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<BackendForFrontendRestDocumentRuntimeDescriptor>)group.ToArray(),
                Comparer);
    }

    public IReadOnlyList<BackendForFrontendRestDocumentRuntimeDescriptor> Documents => documents;

    public BackendForFrontendRestDocumentRuntimeDescriptor? GetById(string documentId)
    {
        if (string.IsNullOrWhiteSpace(documentId))
        {
            return null;
        }

        return documentsById.TryGetValue(documentId.Trim(), out var document)
            ? document
            : null;
    }

    public IReadOnlyList<BackendForFrontendRestDocumentRuntimeDescriptor> GetByBindingId(string bindingId)
    {
        if (string.IsNullOrWhiteSpace(bindingId))
        {
            return [];
        }

        return documentsByBindingId.TryGetValue(bindingId.Trim(), out var matches)
            ? matches
            : [];
    }

    public IReadOnlyList<BackendForFrontendRestDocumentRuntimeDescriptor> GetByClientId(string clientId)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return [];
        }

        return documentsByClientId.TryGetValue(clientId.Trim(), out var matches)
            ? matches
            : [];
    }

    private static BackendForFrontendRestDocumentRuntimeDescriptor[] BuildDocuments(
        IReadOnlyList<BackendForFrontendRestEndpointRuntimeDescriptor> runtimeEndpoints,
        IReadOnlyList<string> allowedDocumentNames,
        string defaultDocumentName,
        string openApiRoutePattern,
        string scalarRoutePrefix)
    {
        var documentOrder = allowedDocumentNames
            .Select((documentName, index) => new KeyValuePair<string, int>(documentName, index))
            .ToDictionary(static entry => entry.Key, static entry => entry.Value, Comparer);
        var normalizedEntries = runtimeEndpoints
            .Select(entry => new NormalizedEntry(
                entry,
                ResolveDocumentName(entry.Endpoint.OpenApiDocumentName, defaultDocumentName)))
            .Where(entry => documentOrder.ContainsKey(entry.DocumentName))
            .ToArray();
        var items = new List<BackendForFrontendRestDocumentRuntimeDescriptor>();

        items.AddRange(normalizedEntries
            .GroupBy(
                static entry => $"{entry.RuntimeEndpoint.BindingId}::{entry.DocumentName}",
                Comparer)
            .Select(group => CreateBindingDocument(
                group.ToArray(),
                group.First().RuntimeEndpoint.BindingId,
                group.First().RuntimeEndpoint.ClientId,
                group.First().DocumentName,
                openApiRoutePattern,
                scalarRoutePrefix))
            .OrderBy(static document => document.ClientId, Comparer)
            .ThenBy(static document => document.ScopeId, Comparer)
            .ThenBy(document => documentOrder[document.DocumentName]));

        items.AddRange(normalizedEntries
            .GroupBy(
                static entry => $"{entry.RuntimeEndpoint.ClientId}::{entry.DocumentName}",
                Comparer)
            .Select(group => CreateClientDocument(
                group.ToArray(),
                group.First().RuntimeEndpoint.ClientId,
                group.First().DocumentName,
                openApiRoutePattern,
                scalarRoutePrefix))
            .OrderBy(static document => document.ClientId, Comparer)
            .ThenBy(document => documentOrder[document.DocumentName]));

        return items.ToArray();
    }

    private static BackendForFrontendRestDocumentRuntimeDescriptor CreateBindingDocument(
        IReadOnlyList<NormalizedEntry> entries,
        string bindingId,
        string clientId,
        string documentName,
        string openApiRoutePattern,
        string scalarRoutePrefix)
    {
        return new BackendForFrontendRestDocumentRuntimeDescriptor(
            id: $"{BackendForFrontendRestDocumentRuntimeDescriptor.BindingKind}::{bindingId}::{documentName}",
            kind: BackendForFrontendRestDocumentRuntimeDescriptor.BindingKind,
            scopeId: bindingId,
            clientId: clientId,
            documentName: documentName,
            openApiPath: BackendForFrontendRestDocumentRoutes.BuildBindingOpenApiPath(openApiRoutePattern, bindingId, documentName),
            scalarPath: BackendForFrontendRestDocumentRoutes.BuildBindingScalarPath(scalarRoutePrefix, bindingId, documentName),
            bindingIds: [bindingId],
            sourceModuleIds: entries
                .Select(static entry => entry.RuntimeEndpoint.Endpoint.SourceModuleId)
                .OfType<string>()
                .ToArray(),
            runtimeEndpointIds: entries
                .Select(static entry => entry.RuntimeEndpoint.Id)
                .ToArray(),
            restEndpointIds: entries
                .Select(static entry => entry.RuntimeEndpoint.RestEndpointId)
                .ToArray());
    }

    private static BackendForFrontendRestDocumentRuntimeDescriptor CreateClientDocument(
        IReadOnlyList<NormalizedEntry> entries,
        string clientId,
        string documentName,
        string openApiRoutePattern,
        string scalarRoutePrefix)
    {
        return new BackendForFrontendRestDocumentRuntimeDescriptor(
            id: $"{BackendForFrontendRestDocumentRuntimeDescriptor.ClientKind}::{clientId}::{documentName}",
            kind: BackendForFrontendRestDocumentRuntimeDescriptor.ClientKind,
            scopeId: clientId,
            clientId: clientId,
            documentName: documentName,
            openApiPath: BackendForFrontendRestDocumentRoutes.BuildClientOpenApiPath(openApiRoutePattern, clientId, documentName),
            scalarPath: BackendForFrontendRestDocumentRoutes.BuildClientScalarPath(scalarRoutePrefix, clientId, documentName),
            bindingIds: entries
                .Select(static entry => entry.RuntimeEndpoint.BindingId)
                .ToArray(),
            sourceModuleIds: entries
                .Select(static entry => entry.RuntimeEndpoint.Endpoint.SourceModuleId)
                .OfType<string>()
                .ToArray(),
            runtimeEndpointIds: entries
                .Select(static entry => entry.RuntimeEndpoint.Id)
                .ToArray(),
            restEndpointIds: entries
                .Select(static entry => entry.RuntimeEndpoint.RestEndpointId)
                .ToArray());
    }

    private static string ResolveDocumentName(string? documentName, string defaultDocumentName)
    {
        return string.IsNullOrWhiteSpace(documentName)
            ? defaultDocumentName
            : documentName.Trim();
    }

    private sealed record NormalizedEntry(
        BackendForFrontendRestEndpointRuntimeDescriptor RuntimeEndpoint,
        string DocumentName);
}
