using System.Text;
using Microsoft.AspNetCore.Http;

namespace Cephalon.AspNetCore.Documentation;

internal static class BackendForFrontendRestDocumentRoutes
{
    public static string ResolveOpenApiRoutePrefix(string routePattern)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(routePattern);

        var placeholderIndex = routePattern.IndexOf("{documentName}", StringComparison.OrdinalIgnoreCase);
        var prefix = placeholderIndex >= 0
            ? routePattern[..placeholderIndex]
            : routePattern;
        prefix = prefix.Trim();

        if (!prefix.StartsWith('/'))
        {
            prefix = $"/{prefix}";
        }

        prefix = prefix.TrimEnd('/');
        return prefix.Length == 0 ? "/" : prefix;
    }

    public static string BuildBindingOpenApiPath(string routePattern, string bindingId, string documentName)
    {
        return $"{ResolveOpenApiRoutePrefix(routePattern)}/backend-for-frontend/bindings/{Uri.EscapeDataString(bindingId)}/{Uri.EscapeDataString(documentName)}.json";
    }

    public static string BuildClientOpenApiPath(string routePattern, string clientId, string documentName)
    {
        return $"{ResolveOpenApiRoutePrefix(routePattern)}/backend-for-frontend/clients/{Uri.EscapeDataString(clientId)}/{Uri.EscapeDataString(documentName)}.json";
    }

    public static string BuildBindingOpenApiRoutePattern(string routePattern)
    {
        return $"{ResolveOpenApiRoutePrefix(routePattern)}/backend-for-frontend/bindings/{{bindingId}}/{{documentName}}.json";
    }

    public static string BuildClientOpenApiRoutePattern(string routePattern)
    {
        return $"{ResolveOpenApiRoutePrefix(routePattern)}/backend-for-frontend/clients/{{clientId}}/{{documentName}}.json";
    }

    public static string BuildBindingScalarPrefix(string scalarRoutePrefix)
    {
        return $"{NormalizePrefix(scalarRoutePrefix)}/backend-for-frontend/bindings";
    }

    public static string BuildClientScalarPrefix(string scalarRoutePrefix)
    {
        return $"{NormalizePrefix(scalarRoutePrefix)}/backend-for-frontend/clients";
    }

    public static string BuildBindingScalarPath(string scalarRoutePrefix, string bindingId, string documentName)
    {
        return BuildScopedScalarPath(
            BuildBindingScalarPrefix(scalarRoutePrefix),
            "bindingId",
            bindingId,
            documentName);
    }

    public static string BuildClientScalarPath(string scalarRoutePrefix, string clientId, string documentName)
    {
        return BuildScopedScalarPath(
            BuildClientScalarPrefix(scalarRoutePrefix),
            "clientId",
            clientId,
            documentName);
    }

    public static string AppendScopedQueryString(
        string path,
        string parameterName,
        string scopeId,
        QueryString queryString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(parameterName);
        ArgumentException.ThrowIfNullOrWhiteSpace(scopeId);

        var builder = new StringBuilder();
        builder.Append(path);
        builder.Append('?');
        builder.Append(Uri.EscapeDataString(parameterName.Trim()));
        builder.Append('=');
        builder.Append(Uri.EscapeDataString(scopeId.Trim()));

        if (queryString.HasValue)
        {
            foreach (var segment in queryString.Value!.TrimStart('?')
                         .Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var equalsIndex = segment.IndexOf('=');
                var name = equalsIndex >= 0 ? segment[..equalsIndex] : segment;
                if (string.Equals(name, parameterName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                builder.Append('&');
                builder.Append(segment);
            }
        }

        return builder.ToString();
    }

    private static string NormalizePrefix(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var normalized = value.Trim();
        if (!normalized.StartsWith('/'))
        {
            normalized = $"/{normalized}";
        }

        return normalized.TrimEnd('/');
    }

    private static string BuildScopedScalarPath(
        string scalarPrefix,
        string parameterName,
        string scopeId,
        string documentName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(parameterName);
        ArgumentException.ThrowIfNullOrWhiteSpace(scopeId);
        ArgumentException.ThrowIfNullOrWhiteSpace(documentName);

        return $"{NormalizePrefix(scalarPrefix)}/{Uri.EscapeDataString(documentName)}?{Uri.EscapeDataString(parameterName.Trim())}={Uri.EscapeDataString(scopeId.Trim())}";
    }
}
