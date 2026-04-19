namespace Cephalon.Abstractions.Transports;

/// <summary>
/// Describes one backend-for-frontend REST documentation surface materialized for one scope and
/// one published OpenAPI document.
/// </summary>
/// <remarks>
/// These runtime descriptors keep client-aware REST documentation surfaces introspectable without
/// turning OpenAPI JSON or Scalar page routes into a second source of truth outside the shared
/// backend-for-frontend and REST runtime catalogs.
/// </remarks>
public sealed class BackendForFrontendRestDocumentRuntimeDescriptor
{
    /// <summary>
    /// The stable kind identifier used when the documentation surface is scoped to one binding.
    /// </summary>
    public const string BindingKind = "binding";

    /// <summary>
    /// The stable kind identifier used when the documentation surface is scoped to one client.
    /// </summary>
    public const string ClientKind = "client";

    /// <summary>
    /// Creates a backend-for-frontend REST documentation runtime descriptor.
    /// </summary>
    /// <param name="id">The stable documentation-surface identifier.</param>
    /// <param name="kind">
    /// The stable scope kind. Supported values are <see cref="BindingKind" /> and
    /// <see cref="ClientKind" />.
    /// </param>
    /// <param name="scopeId">
    /// The stable scope identifier. For binding-scoped surfaces this is the binding identifier,
    /// while client-scoped surfaces use the client identifier.
    /// </param>
    /// <param name="clientId">The stable client identifier served by the materialized document.</param>
    /// <param name="documentName">The resolved OpenAPI document name.</param>
    /// <param name="openApiPath">The rooted OpenAPI JSON path for the filtered document.</param>
    /// <param name="scalarPath">The rooted Scalar page path for the filtered document.</param>
    /// <param name="bindingIds">
    /// The backend-for-frontend binding identifiers that contribute to the materialized document.
    /// Binding-scoped surfaces contain one value, while client-scoped surfaces can aggregate more
    /// than one binding.
    /// </param>
    /// <param name="sourceModuleIds">
    /// The published-endpoint source-module identifiers represented in the materialized document.
    /// </param>
    /// <param name="runtimeEndpointIds">
    /// The client-aware runtime endpoint identifiers included in the materialized document.
    /// </param>
    /// <param name="restEndpointIds">
    /// The published REST endpoint identifiers included in the materialized document.
    /// </param>
    public BackendForFrontendRestDocumentRuntimeDescriptor(
        string id,
        string kind,
        string scopeId,
        string clientId,
        string documentName,
        string openApiPath,
        string scalarPath,
        IReadOnlyList<string>? bindingIds = null,
        IReadOnlyList<string>? sourceModuleIds = null,
        IReadOnlyList<string>? runtimeEndpointIds = null,
        IReadOnlyList<string>? restEndpointIds = null)
    {
        Id = NormalizeRequired(id, nameof(id));
        Kind = NormalizeKind(kind);
        ScopeId = NormalizeRequired(scopeId, nameof(scopeId));
        ClientId = NormalizeRequired(clientId, nameof(clientId));
        DocumentName = NormalizeRequired(documentName, nameof(documentName));
        OpenApiPath = NormalizeRequired(openApiPath, nameof(openApiPath));
        ScalarPath = NormalizeRequired(scalarPath, nameof(scalarPath));
        BindingIds = NormalizeOrderedList(bindingIds);
        SourceModuleIds = NormalizeOrderedList(sourceModuleIds);
        RuntimeEndpointIds = NormalizeOrderedList(runtimeEndpointIds);
        RestEndpointIds = NormalizeOrderedList(restEndpointIds);

        if (BindingIds.Count == 0)
        {
            throw new ArgumentException(
                "At least one contributing backend-for-frontend binding id is required.",
                nameof(bindingIds));
        }

        if (SourceModuleIds.Count == 0)
        {
            throw new ArgumentException(
                "At least one contributing source-module id is required.",
                nameof(sourceModuleIds));
        }

        if (RuntimeEndpointIds.Count == 0)
        {
            throw new ArgumentException(
                "At least one included runtime endpoint id is required.",
                nameof(runtimeEndpointIds));
        }

        if (RestEndpointIds.Count == 0)
        {
            throw new ArgumentException(
                "At least one included published REST endpoint id is required.",
                nameof(restEndpointIds));
        }

        if (string.Equals(Kind, BindingKind, StringComparison.Ordinal))
        {
            if (BindingIds.Count != 1)
            {
                throw new ArgumentException(
                    "Binding-scoped documentation surfaces must reference exactly one binding id.",
                    nameof(bindingIds));
            }

            if (!string.Equals(ScopeId, BindingIds[0], StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "Binding-scoped documentation surfaces must use the binding id as the scope id.",
                    nameof(scopeId));
            }
        }
        else if (!string.Equals(ScopeId, ClientId, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Client-scoped documentation surfaces must use the client id as the scope id.",
                nameof(scopeId));
        }
    }

    /// <summary>
    /// Gets the stable documentation-surface identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the stable scope kind.
    /// </summary>
    public string Kind { get; }

    /// <summary>
    /// Gets the stable scope identifier.
    /// </summary>
    public string ScopeId { get; }

    /// <summary>
    /// Gets the stable client identifier represented by the materialized document.
    /// </summary>
    public string ClientId { get; }

    /// <summary>
    /// Gets the binding identifier when the surface is scoped to one binding.
    /// </summary>
    public string? BindingId => string.Equals(Kind, BindingKind, StringComparison.Ordinal)
        ? ScopeId
        : null;

    /// <summary>
    /// Gets the resolved OpenAPI document name.
    /// </summary>
    public string DocumentName { get; }

    /// <summary>
    /// Gets the rooted OpenAPI JSON path for the filtered document.
    /// </summary>
    public string OpenApiPath { get; }

    /// <summary>
    /// Gets the rooted Scalar page path for the filtered document.
    /// </summary>
    public string ScalarPath { get; }

    /// <summary>
    /// Gets the contributing backend-for-frontend binding identifiers.
    /// </summary>
    public IReadOnlyList<string> BindingIds { get; }

    /// <summary>
    /// Gets the published-endpoint source-module identifiers represented in the materialized document.
    /// </summary>
    public IReadOnlyList<string> SourceModuleIds { get; }

    /// <summary>
    /// Gets the included client-aware runtime endpoint identifiers.
    /// </summary>
    public IReadOnlyList<string> RuntimeEndpointIds { get; }

    /// <summary>
    /// Gets the included published REST endpoint identifiers.
    /// </summary>
    public IReadOnlyList<string> RestEndpointIds { get; }

    private static string NormalizeKind(string value)
    {
        var normalized = NormalizeRequired(value, nameof(value)).ToLowerInvariant();
        return normalized switch
        {
            BindingKind => BindingKind,
            ClientKind => ClientKind,
            _ => throw new ArgumentException(
                $"Documentation surface kinds must be '{BindingKind}' or '{ClientKind}'.",
                nameof(value))
        };
    }

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A non-empty value is required.", paramName);
        }

        return value.Trim();
    }

    private static string[] NormalizeOrderedList(IReadOnlyList<string>? values)
    {
        return values?
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }
}
