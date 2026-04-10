using Cephalon.Abstractions.Audit;
using Cephalon.Audit.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cephalon.Sample.Showcase.Infrastructure;

/// <summary>
/// Small helper for recording showcase audit entries without coupling endpoint code to service-resolution details.
/// </summary>
internal static class ShowcaseAuditHelper
{
    private static readonly Action<ILogger, string, string, Exception?> AuditRecordingFailedMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Warning,
            new EventId(2901, "ShowcaseAuditRecordingFailed"),
            "Showcase audit recording failed for category '{Category}' and action '{Action}' after the primary operation completed.");

    /// <summary>
    /// Records an audit entry when the active request can resolve an <see cref="IAuditRecorder" />.
    /// Durable audit persistence is best-effort in the showcase so already-committed business writes
    /// do not report false HTTP failures when the additive audit path fails afterward.
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

        try
        {
            await recorder.RecordAsync(request, httpContext.RequestAborted).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (httpContext.RequestAborted.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            var logger = httpContext.RequestServices
                .GetService<ILoggerFactory>()
                ?.CreateLogger("Cephalon.Sample.Showcase.Audit");
            if (logger is not null)
            {
                AuditRecordingFailedMessage(logger, request.Category, request.Action, exception);
            }
        }
    }

    /// <summary>
    /// Creates a consistent metadata dictionary for showcase audit entries using the active request path.
    /// </summary>
    /// <param name="httpContext">The active HTTP request context.</param>
    /// <param name="moduleId">The module identifier associated with the operation.</param>
    /// <returns>The normalized metadata dictionary.</returns>
    public static IReadOnlyDictionary<string, string> CreateMetadata(
        HttpContext httpContext,
        string moduleId)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleId);

        var endpoint = httpContext.Request.Path.HasValue
            ? httpContext.Request.Path.Value
            : "/";

        return CreateMetadata(moduleId, endpoint ?? "/");
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
