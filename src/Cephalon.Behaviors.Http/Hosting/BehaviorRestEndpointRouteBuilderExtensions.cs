using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using Cephalon.Abstractions.Modules;
using Cephalon.Behaviors.Http.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using System.Globalization;

namespace Cephalon.Behaviors.Http.Hosting;

/// <summary>
/// Adds Minimal API helpers that route versioned REST endpoints into Cephalon behaviors.
/// </summary>
public static class BehaviorRestEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Creates a REST-oriented behavior route group owned by the supplied module.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder receiving the group.</param>
    /// <param name="module">The module that owns the endpoints.</param>
    /// <param name="prefix">The route prefix relative to the enclosing endpoint builder.</param>
    /// <returns>A behavior-aware route-group wrapper.</returns>
    public static BehaviorRestEndpointGroup MapBehaviorRestGroup(
        this IEndpointRouteBuilder endpoints,
        IModule module,
        string prefix)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(module);
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);

        return new BehaviorRestEndpointGroup(endpoints, module, prefix);
    }
}

internal static class BehaviorRequestJsonComposer
{
    public static async Task<JsonElement> ComposeAsync<TInput>(
        HttpContext context,
        bool acceptsBody,
        IReadOnlyList<BehaviorRestBindingDescriptor>? bindings = null,
        bool preserveImplicitQueryFallback = false)
        => await ComposeAsync(
                context,
                typeof(TInput),
                acceptsBody,
                bindings,
                preserveImplicitQueryFallback)
            .ConfigureAwait(false);

    public static async Task<JsonElement> ComposeAsync(
        HttpContext context,
        Type inputType,
        bool acceptsBody,
        IReadOnlyList<BehaviorRestBindingDescriptor>? bindings = null,
        bool preserveImplicitQueryFallback = false)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(inputType);

        if (bindings is { Count: > 0 })
        {
            return await ComposeExplicitAsync(
                    context,
                    inputType,
                    acceptsBody,
                    bindings,
                    preserveImplicitQueryFallback)
                .ConfigureAwait(false);
        }

        if (IsSimpleInputType(inputType))
        {
            return await ComposeScalarAsync(context, acceptsBody).ConfigureAwait(false);
        }

        var payload = new JsonObject();
        if (acceptsBody)
        {
            var body = await ReadBodyNodeAsync(context).ConfigureAwait(false);
            if (body is JsonObject bodyObject)
            {
                foreach (var property in bodyObject)
                {
                    payload[property.Key] = property.Value?.DeepClone();
                }
            }
            else if (body is not null && GetValueKind(body) is not JsonValueKind.Null)
            {
                throw new JsonException("Behavior REST helpers expect JSON object bodies for non-scalar inputs.");
            }
        }

        MergeQuery(payload, context.Request.Query);
        MergeRouteValues(payload, context.Request.RouteValues);

        return JsonSerializer.SerializeToElement(payload);
    }

    private static async Task<JsonElement> ComposeExplicitAsync(
        HttpContext context,
        Type inputType,
        bool acceptsBody,
        IReadOnlyList<BehaviorRestBindingDescriptor> bindings,
        bool preserveImplicitQueryFallback)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(inputType);
        ArgumentNullException.ThrowIfNull(bindings);

        if (IsSimpleInputType(inputType))
        {
            throw new JsonException("Explicit behavior REST bindings are supported only for object inputs.");
        }

        JsonObject? bodyObject = null;
        if (acceptsBody)
        {
            bodyObject = await ReadBodyObjectAsync(context).ConfigureAwait(false);
        }

        var inputProperties = inputType
            .GetProperties()
            .Select(static property => property.Name)
            .ToDictionary(static name => name, static name => name, StringComparer.OrdinalIgnoreCase);
        var explicitProperties = bindings
            .Select(static binding => binding.PropertyName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var explicitBodyProperties = bindings
            .Where(static binding => binding.Source == BehaviorRestBindingSource.Body)
            .Select(static binding => binding.PropertyName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var reservedBodyKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var payload = new JsonObject();
        foreach (var binding in bindings)
        {
            var sourceName = string.IsNullOrWhiteSpace(binding.Name)
                ? binding.PropertyName
                : binding.Name.Trim();

            switch (binding.Source)
            {
                case BehaviorRestBindingSource.Route:
                    if (context.Request.RouteValues.TryGetValue(sourceName, out var routeValue) &&
                        routeValue is not null)
                    {
                        payload[binding.PropertyName] = ParseScalarNode(
                            Convert.ToString(routeValue, CultureInfo.InvariantCulture) ?? string.Empty);
                    }

                    break;

                case BehaviorRestBindingSource.Query:
                    if (context.Request.Query.TryGetValue(sourceName, out var queryValues))
                    {
                        payload[binding.PropertyName] = CreateMultiValueNode(queryValues);
                    }

                    break;

                case BehaviorRestBindingSource.Header:
                    if (context.Request.Headers.TryGetValue(sourceName, out var headerValues))
                    {
                        payload[binding.PropertyName] = CreateMultiValueNode(headerValues);
                    }

                    break;

                case BehaviorRestBindingSource.Body:
                    if (TryGetJsonPropertyValue(bodyObject, sourceName, out var bodyValue))
                    {
                        payload[binding.PropertyName] = bodyValue?.DeepClone();
                        reservedBodyKeys.Add(sourceName);
                    }

                    break;

                default:
                    throw new JsonException(
                        $"Unsupported explicit behavior REST binding source '{binding.Source}'.");
            }
        }

        var inferredRouteProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var routeValue in context.Request.RouteValues)
        {
            if (routeValue.Value is null ||
                !TryResolveInputProperty(inputProperties, routeValue.Key, out var propertyName) ||
                explicitProperties.Contains(propertyName))
            {
                continue;
            }

            payload[propertyName] = ParseScalarNode(
                Convert.ToString(routeValue.Value, CultureInfo.InvariantCulture) ?? string.Empty);
            inferredRouteProperties.Add(propertyName);
        }

        if (bodyObject is not null)
        {
            var lockedBodyProperties = new HashSet<string>(explicitProperties, StringComparer.OrdinalIgnoreCase);
            lockedBodyProperties.ExceptWith(explicitBodyProperties);
            lockedBodyProperties.UnionWith(inferredRouteProperties);

            foreach (var property in bodyObject)
            {
                if (reservedBodyKeys.Contains(property.Key))
                {
                    continue;
                }

                if (!TryResolveInputProperty(inputProperties, property.Key, out var propertyName))
                {
                    payload[property.Key] = property.Value?.DeepClone();
                    continue;
                }

                if (explicitBodyProperties.Contains(propertyName))
                {
                    continue;
                }

                if (lockedBodyProperties.Contains(propertyName))
                {
                    throw new JsonException(
                        $"JSON body property '{property.Key}' conflicts with an explicit REST binding for input property '{propertyName}'.");
                }

                payload[propertyName] = property.Value?.DeepClone();
            }
        }

        if (preserveImplicitQueryFallback)
        {
            MergeImplicitQueryFallback(
                payload,
                context.Request.Query,
                inputProperties,
                explicitProperties,
                inferredRouteProperties);
        }

        return JsonSerializer.SerializeToElement(payload);
    }

    private static async Task<JsonElement> ComposeScalarAsync(HttpContext context, bool acceptsBody)
    {
        if (acceptsBody)
        {
            var body = await ReadBodyNodeAsync(context).ConfigureAwait(false);
            if (body is not null && GetValueKind(body) is not JsonValueKind.Null)
            {
                return JsonSerializer.SerializeToElement(body);
            }
        }

        var scalarValues = new List<JsonValue?>();

        foreach (var pair in context.Request.Query.Where(static candidate => candidate.Value.Count > 0))
        {
            if (pair.Value.Count == 1)
            {
                scalarValues.Add(ParseScalarNode(pair.Value[0]));
            }
            else
            {
                throw new JsonException("Scalar behavior inputs cannot bind multiple query-string values.");
            }
        }

        foreach (var routeValue in context.Request.RouteValues.Values)
        {
            if (routeValue is null)
            {
                continue;
            }

            scalarValues.Add(ParseScalarNode(Convert.ToString(routeValue, CultureInfo.InvariantCulture) ?? string.Empty));
        }

        return scalarValues.Count switch
        {
            0 => JsonSerializer.SerializeToElement<object?>(null),
            1 => JsonSerializer.SerializeToElement(scalarValues[0]),
            _ => throw new JsonException("Scalar behavior inputs cannot bind multiple route or query values.")
        };
    }

    private static async Task<JsonNode?> ReadBodyNodeAsync(HttpContext context)
    {
        if (context.Request.ContentLength is 0)
        {
            return null;
        }

        if (context.Request.Body.CanSeek)
        {
            context.Request.Body.Position = 0;
        }

        return await JsonNode.ParseAsync(context.Request.Body, cancellationToken: context.RequestAborted).ConfigureAwait(false);
    }

    private static async Task<JsonObject?> ReadBodyObjectAsync(HttpContext context)
    {
        var body = await ReadBodyNodeAsync(context).ConfigureAwait(false);
        if (body is null)
        {
            return null;
        }

        if (body is JsonObject bodyObject)
        {
            return bodyObject;
        }

        if (GetValueKind(body) is JsonValueKind.Null)
        {
            return null;
        }

        throw new JsonException("Explicit behavior REST bindings expect JSON object bodies.");
    }

    private static void MergeQuery(
        JsonObject payload,
        IQueryCollection query)
    {
        foreach (var pair in query)
        {
            if (pair.Value.Count == 1)
            {
                payload[pair.Key] = ParseScalarNode(pair.Value[0]);
                continue;
            }

            if (pair.Value.Count > 1)
            {
                var values = new JsonArray();
                foreach (var value in pair.Value)
                {
                    values.Add(ParseScalarNode(value));
                }

                payload[pair.Key] = values;
            }
        }
    }

    private static void MergeImplicitQueryFallback(
        JsonObject payload,
        IQueryCollection query,
        Dictionary<string, string> inputProperties,
        HashSet<string> explicitProperties,
        HashSet<string> inferredRouteProperties)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(inputProperties);
        ArgumentNullException.ThrowIfNull(explicitProperties);
        ArgumentNullException.ThrowIfNull(inferredRouteProperties);

        var lockedQueryProperties = new HashSet<string>(explicitProperties, StringComparer.OrdinalIgnoreCase);
        lockedQueryProperties.UnionWith(inferredRouteProperties);

        foreach (var pair in query)
        {
            if (!TryResolveInputProperty(inputProperties, pair.Key, out var propertyName) ||
                lockedQueryProperties.Contains(propertyName))
            {
                continue;
            }

            payload[propertyName] = CreateMultiValueNode(pair.Value);
        }
    }

    private static void MergeRouteValues(
        JsonObject payload,
        RouteValueDictionary routeValues)
    {
        foreach (var pair in routeValues)
        {
            if (pair.Value is null)
            {
                continue;
            }

            payload[pair.Key] = ParseScalarNode(Convert.ToString(pair.Value, CultureInfo.InvariantCulture) ?? string.Empty);
        }
    }

    private static JsonValue? ParseScalarNode(string? value)
    {
        if (value is null)
        {
            return JsonValue.Create((string?)null);
        }

        if (bool.TryParse(value, out var boolValue))
        {
            return JsonValue.Create(boolValue);
        }

        if (long.TryParse(value, out var longValue))
        {
            return JsonValue.Create(longValue);
        }

        if (double.TryParse(value, out var doubleValue) &&
            !double.IsNaN(doubleValue) &&
            !double.IsInfinity(doubleValue))
        {
            return JsonValue.Create(doubleValue);
        }

        return JsonValue.Create(value);
    }

    private static JsonNode? CreateMultiValueNode(StringValues values)
    {
        if (values.Count == 0)
        {
            return null;
        }

        if (values.Count == 1)
        {
            return ParseScalarNode(values[0]);
        }

        var array = new JsonArray();
        foreach (var value in values)
        {
            array.Add(ParseScalarNode(value));
        }

        return array;
    }

    private static bool TryResolveInputProperty(
        Dictionary<string, string> inputProperties,
        string candidate,
        out string propertyName)
    {
        ArgumentNullException.ThrowIfNull(inputProperties);
        ArgumentException.ThrowIfNullOrWhiteSpace(candidate);

        return inputProperties.TryGetValue(candidate, out propertyName!);
    }

    private static bool TryGetJsonPropertyValue(JsonObject? bodyObject, string propertyName, out JsonNode? value)
    {
        value = null;
        if (bodyObject is null || string.IsNullOrWhiteSpace(propertyName))
        {
            return false;
        }

        foreach (var property in bodyObject)
        {
            if (!string.Equals(property.Key, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            value = property.Value;
            return true;
        }

        return false;
    }

    private static bool IsSimpleInputType(Type inputType)
    {
        ArgumentNullException.ThrowIfNull(inputType);

        var type = Nullable.GetUnderlyingType(inputType) ?? inputType;
        return type.IsPrimitive ||
               type.IsEnum ||
               type == typeof(string) ||
               type == typeof(decimal) ||
               type == typeof(Guid) ||
               type == typeof(DateTime) ||
               type == typeof(DateTimeOffset) ||
               type == typeof(DateOnly) ||
               type == typeof(TimeOnly);
    }

    private static JsonValueKind GetValueKind(JsonNode node)
    {
        return JsonSerializer.SerializeToElement(node).ValueKind;
    }
}

internal static class BehaviorXmlDocumentation
{
    private static readonly char[] LineBreakChars = ['\r', '\n'];
    private static readonly Lock SyncRoot = new();
    private static readonly Dictionary<string, Dictionary<string, XmlTypeComments>> Cache = new(StringComparer.OrdinalIgnoreCase);

    public static string? GetSummary(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return TryGetComments(type, out var comments)
            ? comments.Summary
            : null;
    }

    public static string? GetRemarks(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return TryGetComments(type, out var comments)
            ? comments.Remarks
            : null;
    }

    public static string? GetDescription(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return TryGetComments(type, out var comments)
            ? comments.Remarks
            : null;
    }

    private static bool TryGetComments(Type type, out XmlTypeComments comments)
    {
        var typeName = GetXmlMemberTypeName(type);
        var assemblyName = type.Assembly.GetName().Name;
        var xmlPath = string.IsNullOrWhiteSpace(assemblyName)
            ? null
            : Path.Combine(AppContext.BaseDirectory, $"{assemblyName}.xml");
        if (string.IsNullOrWhiteSpace(xmlPath) || !File.Exists(xmlPath))
        {
            comments = default;
            return false;
        }

        lock (SyncRoot)
        {
            if (!Cache.TryGetValue(xmlPath, out var memberCache))
            {
                memberCache = Load(xmlPath);
                Cache[xmlPath] = memberCache;
            }

            if (memberCache.TryGetValue(typeName, out comments))
            {
                return true;
            }
        }

        comments = default;
        return false;
    }

    private static Dictionary<string, XmlTypeComments> Load(string xmlPath)
    {
        var comments = new Dictionary<string, XmlTypeComments>(StringComparer.Ordinal);

        try
        {
            var document = XDocument.Load(xmlPath);
            var members = document.Root?
                .Element("members")?
                .Elements("member");
            if (members is null)
            {
                return comments;
            }

            foreach (var member in members)
            {
                var rawName = (string?)member.Attribute("name");
                if (string.IsNullOrWhiteSpace(rawName) ||
                    !rawName.StartsWith("T:", StringComparison.Ordinal) ||
                    rawName.Length <= 2)
                {
                    continue;
                }

                comments[rawName[2..]] = new XmlTypeComments(
                    Normalize(member.Element("summary")?.Value),
                    Normalize(member.Element("remarks")?.Value));
            }
        }
        catch
        {
            // XML comments stay best-effort for docs enrichment.
        }

        return comments;
    }

    private static string GetXmlMemberTypeName(Type type)
    {
        return (type.FullName ?? type.Name).Replace('+', '.');
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

    private readonly record struct XmlTypeComments(string? Summary, string? Remarks);
}
