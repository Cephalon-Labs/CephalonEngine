using System.Text.Json.Nodes;
using System.Text.Json;
using Cephalon.AspNetCore;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cephalon.AspNetCore.Health;

internal static class HealthResponseWriter
{
    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(report);

        context.Response.ContentType = "application/json; charset=utf-8";

        var payload = new HealthResponsePayload
        {
            Status = report.Status.ToString(),
            TotalDurationMs = report.TotalDuration.TotalMilliseconds,
            Entries = report.Entries.ToDictionary(
                pair => pair.Key,
                pair => new HealthResponseEntryPayload
                {
                    Status = pair.Value.Status.ToString(),
                    Description = pair.Value.Description,
                    DurationMs = pair.Value.Duration.TotalMilliseconds,
                    Data = ConvertHealthData(pair.Value.Data)
                },
                StringComparer.OrdinalIgnoreCase)
        };

        return JsonSerializer.SerializeAsync(
            context.Response.Body,
            payload,
            AspNetCoreJsonSerializerContext.Default.HealthResponsePayload,
            context.RequestAborted);
    }

    private static Dictionary<string, JsonNode?> ConvertHealthData(
        IReadOnlyDictionary<string, object> data)
    {
        if (data.Count == 0)
        {
            return new Dictionary<string, JsonNode?>(StringComparer.OrdinalIgnoreCase);
        }

        return data.ToDictionary(
            static pair => pair.Key,
            static pair => ConvertHealthDataValue(pair.Value),
            StringComparer.OrdinalIgnoreCase);
    }

    private static JsonNode? ConvertHealthDataValue(object? value)
    {
        return value switch
        {
            null => null,
            JsonNode jsonNode => jsonNode,
            JsonElement jsonElement => JsonNode.Parse(jsonElement.GetRawText()),
            string stringValue => JsonValue.Create(stringValue),
            bool boolValue => JsonValue.Create(boolValue),
            byte byteValue => JsonValue.Create(byteValue),
            sbyte sbyteValue => JsonValue.Create(sbyteValue),
            short shortValue => JsonValue.Create(shortValue),
            ushort ushortValue => JsonValue.Create(ushortValue),
            int intValue => JsonValue.Create(intValue),
            uint uintValue => JsonValue.Create(uintValue),
            long longValue => JsonValue.Create(longValue),
            ulong ulongValue => JsonValue.Create(ulongValue),
            float floatValue => JsonValue.Create(floatValue),
            double doubleValue => JsonValue.Create(doubleValue),
            decimal decimalValue => JsonValue.Create(decimalValue),
            DateTime dateTimeValue => JsonValue.Create(dateTimeValue),
            DateTimeOffset dateTimeOffsetValue => JsonValue.Create(dateTimeOffsetValue),
            Guid guidValue => JsonValue.Create(guidValue),
            TimeSpan timeSpanValue => JsonValue.Create(timeSpanValue.ToString("c", CultureInfo.InvariantCulture)),
            Uri uriValue => JsonValue.Create(uriValue.ToString()),
            HealthResponseDependencyPayload[] dependencies => ConvertDependencies(dependencies),
            _ => JsonValue.Create(value.ToString())
        };
    }

    private static JsonArray ConvertDependencies(IEnumerable<HealthResponseDependencyPayload> dependencies)
    {
        var array = new JsonArray();
        foreach (var dependency in dependencies)
        {
            var dependencyObject = new JsonObject
            {
                ["id"] = JsonValue.Create(dependency.Id),
                ["displayName"] = JsonValue.Create(dependency.DisplayName),
                ["state"] = JsonValue.Create(dependency.State),
                ["description"] = JsonValue.Create(dependency.Description),
                ["required"] = JsonValue.Create(dependency.Required),
                ["source"] = JsonValue.Create(dependency.Source)
            };

            array.Add((JsonNode?)dependencyObject);
        }

        return array;
    }
}
