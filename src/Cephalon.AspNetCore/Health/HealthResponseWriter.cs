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

        var payload = new
        {
            status = report.Status.ToString(),
            totalDurationMs = report.TotalDuration.TotalMilliseconds,
            entries = report.Entries.ToDictionary(
                pair => pair.Key,
                pair => new
                {
                    status = pair.Value.Status.ToString(),
                    description = pair.Value.Description,
                    durationMs = pair.Value.Duration.TotalMilliseconds,
                    data = pair.Value.Data
                },
                StringComparer.OrdinalIgnoreCase)
        };

        return context.Response.WriteAsJsonAsync(payload);
    }
}
