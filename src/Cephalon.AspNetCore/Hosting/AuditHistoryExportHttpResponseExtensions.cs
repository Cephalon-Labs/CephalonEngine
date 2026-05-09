using System.Text;
using System.Text.Json;
using Cephalon.Abstractions.Audit;
using Cephalon.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Cephalon.AspNetCore.Hosting;

/// <summary>
/// Writes audit-history export responses for ASP.NET Core hosts.
/// </summary>
public static class AuditHistoryExportHttpResponseExtensions
{
    /// <summary>
    /// Writes the supplied audit-history export as newline-delimited JSON.
    /// </summary>
    /// <param name="response">The HTTP response to populate.</param>
    /// <param name="exporter">The audit-history exporter that supplies the entries.</param>
    /// <param name="request">The export request to execute.</param>
    /// <param name="fileName">An optional download file name.</param>
    /// <param name="cancellationToken">The token that cancels the response stream.</param>
    /// <returns>A task that completes when the response has been written.</returns>
    public static async Task WriteAuditHistoryNdjsonAsync(
        this HttpResponse response,
        IAuditHistoryExporter exporter,
        AuditHistoryExportRequest request,
        string? fileName = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(exporter);
        ArgumentNullException.ThrowIfNull(request);

        response.StatusCode = StatusCodes.Status200OK;
        response.ContentType = "application/x-ndjson; charset=utf-8";
        response.Headers[HeaderNames.ContentDisposition] = CreateContentDisposition(
            string.IsNullOrWhiteSpace(fileName)
                ? CreateDefaultFileName()
                : fileName.Trim());

        var lineBreak = Encoding.UTF8.GetBytes("\n");

        await foreach (var entry in exporter.ExportAsync(request, cancellationToken).WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            await JsonSerializer.SerializeAsync(
                    response.Body,
                    entry,
                    AspNetCoreJsonSerializerContext.Default.AuditHistoryEntry,
                    cancellationToken)
                .ConfigureAwait(false);
            await response.Body.WriteAsync(lineBreak, cancellationToken).ConfigureAwait(false);
        }

        await response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string CreateDefaultFileName()
    {
        return $"audit-history-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.ndjson";
    }

    private static string CreateContentDisposition(string fileName)
    {
        return $"attachment; filename=\"{fileName.Replace("\"", string.Empty, StringComparison.Ordinal)}\"";
    }
}
