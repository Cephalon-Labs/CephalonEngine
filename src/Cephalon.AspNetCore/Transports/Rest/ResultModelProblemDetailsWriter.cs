using System.Text.Json;
using Cephalon.AspNetCore;
using Cephalon.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Cephalon.AspNetCore.Transports.Rest;

internal sealed class ResultModelProblemDetailsWriter(
    IConfiguration configuration) : IProblemDetailsWriter
{
    public bool CanWrite(ProblemDetailsContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var options = ApiRoutesOptions.FromConfiguration(configuration);
        return options.UseResultModelEnvelope &&
            IsRestRequest(context.HttpContext.Request.Path, options);
    }

    public async ValueTask WriteAsync(ProblemDetailsContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var problem = context.ProblemDetails;
        var statusCode = NormalizeStatusCode(problem.Status ?? context.HttpContext.Response.StatusCode);
        var message = string.IsNullOrWhiteSpace(problem.Detail)
            ? problem.Title ?? "The request failed."
            : problem.Detail;
        var title = string.IsNullOrWhiteSpace(problem.Title)
            ? "Error"
            : problem.Title;
        var type = string.IsNullOrWhiteSpace(problem.Type)
            ? ResultModelProblemTypes.Resolve(statusCode)
            : problem.Type;

        var envelope = new ResultModelError
        {
            Type = type,
            Title = title,
            Message = message,
            Success = false,
            StatusCode = statusCode,
            Errors =
            [
                new ResultModelErrorDetail
                {
                    Key = ResolveErrorKey(statusCode),
                    Message = message,
                    Details = problem.Detail
                }
            ]
        };

        context.HttpContext.Response.StatusCode = statusCode;
        context.HttpContext.Response.ContentType = "application/json; charset=utf-8";

        await JsonSerializer.SerializeAsync(
                context.HttpContext.Response.Body,
                envelope,
                AspNetCoreJsonSerializerContext.Default.ResultModelError,
                context.HttpContext.RequestAborted)
            .ConfigureAwait(false);
    }

    private static bool IsRestRequest(PathString path, ApiRoutesOptions options)
    {
        if (IsKnownNonRestPath(path, options))
        {
            return false;
        }

        return string.IsNullOrEmpty(options.RestPrefix)
            ? path.HasValue
            : path.StartsWithSegments(options.RestPrefix);
    }

    private static bool IsKnownNonRestPath(PathString path, ApiRoutesOptions options)
    {
        return path.StartsWithSegments("/engine") ||
            path.StartsWithSegments("/openapi") ||
            path.StartsWithSegments("/scalar") ||
            StartsWithConfiguredPrefix(path, options.GraphQLPrefix) ||
            StartsWithConfiguredPrefix(path, options.JsonRpcPrefix) ||
            StartsWithConfiguredPrefix(path, options.GrpcPrefix) ||
            StartsWithConfiguredPrefix(path, options.WsPrefix) ||
            StartsWithConfiguredPrefix(path, options.SsePrefix) ||
            StartsWithConfiguredPrefix(path, options.GraphQLWsPrefix) ||
            StartsWithConfiguredPrefix(path, options.GraphQLSsePrefix);
    }

    private static bool StartsWithConfiguredPrefix(PathString path, string prefix)
        => !string.IsNullOrWhiteSpace(prefix) && path.StartsWithSegments(prefix);

    private static int NormalizeStatusCode(int statusCode)
        => statusCode is >= 400 and <= 599
            ? statusCode
            : StatusCodes.Status500InternalServerError;

    private static string ResolveErrorKey(int statusCode)
        => statusCode switch
        {
            StatusCodes.Status400BadRequest => "bad_request",
            StatusCodes.Status401Unauthorized => "unauthorized",
            StatusCodes.Status403Forbidden => "forbidden",
            StatusCodes.Status404NotFound => "not_found",
            StatusCodes.Status409Conflict => "conflict",
            StatusCodes.Status429TooManyRequests => "too_many_requests",
            StatusCodes.Status503ServiceUnavailable => "service_unavailable",
            _ => "http_error"
        };
}
