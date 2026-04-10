using Cephalon.Abstractions.Audit;
using Cephalon.Audit.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Sample.Showcase.Infrastructure;

/// <summary>
/// Small helper for recording showcase audit entries without coupling endpoint code to service-resolution details.
/// </summary>
internal static class ShowcaseAuditHelper
{
    /// <summary>
    /// Records an audit entry when the active request can resolve an <see cref="IAuditRecorder" />.
    /// </summary>
    /// <param name="httpContext">The active HTTP request context.</param>
    /// <param name="request">The audit request to record.</param>
    /// <returns>A task that completes when the audit pipeline finishes.</returns>
    public static async ValueTask RecordAsync(
        HttpContext httpContext,
        AuditRecordRequest request)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(request);

        var recorder = httpContext.RequestServices.GetService<IAuditRecorder>();
        if (recorder is null)
        {
            return;
        }

        await recorder.RecordAsync(request, httpContext.RequestAborted).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates a consistent metadata dictionary for showcase audit entries.
    /// </summary>
    /// <param name="moduleId">The module identifier associated with the operation.</param>
    /// <param name="endpoint">The REST endpoint path associated with the operation.</param>
    /// <returns>The normalized metadata dictionary.</returns>
    public static IReadOnlyDictionary<string, string> CreateMetadata(
        string moduleId,
        string endpoint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);

        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["moduleId"] = moduleId.Trim(),
            ["endpoint"] = endpoint.Trim(),
            ["transport"] = "http.rest",
            ["sample"] = "showcase"
        };
    }
}
