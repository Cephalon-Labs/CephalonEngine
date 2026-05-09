using Cephalon.Abstractions.Patterns;
using Cephalon.AspNetCore;
using Cephalon.AspNetCore.Documentation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;

namespace Cephalon.AspNetCore.Hosting;

internal sealed class AspNetCoreStranglerFigCutoverMiddleware
{
    internal const string ProxyHttpClientName = "cephalon-strangler-fig-proxy";

    private static readonly HashSet<string> HopByHopHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Connection",
        "Keep-Alive",
        "Proxy-Authenticate",
        "Proxy-Authorization",
        "TE",
        "Trailer",
        "Transfer-Encoding",
        "Upgrade"
    };

    private readonly RequestDelegate next;
    private readonly IStranglerFigRouter router;
    private readonly AspNetCoreStranglerFigCutoverCatalog catalog;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly string[] excludedPathPrefixes;

    public AspNetCoreStranglerFigCutoverMiddleware(
        RequestDelegate next,
        IStranglerFigRouter router,
        AspNetCoreStranglerFigCutoverCatalog catalog,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ReferenceDocsHostingOptions referenceDocsOptions)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(router);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(referenceDocsOptions);

        this.next = next;
        this.router = router;
        this.catalog = catalog;
        this.httpClientFactory = httpClientFactory;
        excludedPathPrefixes = AspNetCoreRateLimitingPolicyResolver
            .ResolveExcludedPathPrefixes(configuration, referenceDocsOptions)
            .ToArray();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (ShouldSkip(context.Request.Path))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        var requestedPath = context.Request.Path.HasValue
            ? context.Request.Path.Value!
            : "/";
        var resolution = await router.ResolveAsync(
                new StranglerFigRequest(requestedPath, context.Request.Method),
                context.RequestAborted)
            .ConfigureAwait(false);
        if (resolution is null)
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        var decision = catalog.CreateDecision(resolution, context.Request.QueryString);
        if (!decision.InterceptsRequest)
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        StampResponseHeaders(context, decision);

        switch (decision.HandlingMode)
        {
            case AspNetCoreStranglerFigCutoverCatalog.RewriteLocalPathHandlingMode:
                context.Request.Path = decision.DestinationPath is null
                    ? context.Request.Path
                    : new PathString(decision.DestinationPath);
                context.Request.QueryString = string.IsNullOrWhiteSpace(decision.DestinationQuery)
                    ? QueryString.Empty
                    : new QueryString(decision.DestinationQuery);
                await next(context).ConfigureAwait(false);
                return;
            case AspNetCoreStranglerFigCutoverCatalog.RedirectAbsoluteUriHandlingMode:
                context.Response.StatusCode = decision.ResponseStatusCode ?? StatusCodes.Status307TemporaryRedirect;
                context.Response.Headers.Location = decision.DestinationUri;
                return;
            case AspNetCoreStranglerFigCutoverCatalog.ProxyAbsoluteUriHandlingMode:
                await ProxyAsync(context, decision).ConfigureAwait(false);
                return;
            case AspNetCoreStranglerFigCutoverCatalog.UnsupportedEndpointHandlingMode:
                await WriteUnsupportedEndpointResponseAsync(context, decision).ConfigureAwait(false);
                return;
            default:
                await next(context).ConfigureAwait(false);
                return;
        }
    }

    private bool ShouldSkip(PathString requestPath)
    {
        for (var index = 0; index < excludedPathPrefixes.Length; index++)
        {
            if (requestPath.StartsWithSegments(excludedPathPrefixes[index], StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private async Task ProxyAsync(
        HttpContext context,
        AspNetCoreStranglerFigCutoverDecision decision)
    {
        if (string.IsNullOrWhiteSpace(decision.DestinationUri))
        {
            throw new InvalidOperationException("Strangler-fig proxy decisions require a destination URI.");
        }

        using var requestMessage = CreateProxyRequest(context, decision.DestinationUri);
        var client = httpClientFactory.CreateClient(ProxyHttpClientName);
        using var responseMessage = await client.SendAsync(
                requestMessage,
                HttpCompletionOption.ResponseHeadersRead,
                context.RequestAborted)
            .ConfigureAwait(false);

        context.Response.StatusCode = (int)responseMessage.StatusCode;
        CopyResponseHeaders(responseMessage, context.Response);

        if (responseMessage.Content is not null)
        {
            await responseMessage.Content.CopyToAsync(context.Response.Body, context.RequestAborted)
                .ConfigureAwait(false);
        }
    }

    private static HttpRequestMessage CreateProxyRequest(HttpContext context, string destinationUri)
    {
        var requestMessage = new HttpRequestMessage(new HttpMethod(context.Request.Method), destinationUri);
        HttpContent? content = null;

        if (RequestMayHaveBody(context.Request))
        {
            content = new StreamContent(context.Request.Body);
            requestMessage.Content = content;
        }

        foreach (var header in context.Request.Headers)
        {
            if (string.Equals(header.Key, "Host", StringComparison.OrdinalIgnoreCase) ||
                HopByHopHeaders.Contains(header.Key))
            {
                continue;
            }

            if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.AsEnumerable()))
            {
                content?.Headers.TryAddWithoutValidation(header.Key, header.Value.AsEnumerable());
            }
        }

        requestMessage.Headers.TryAddWithoutValidation("X-Forwarded-Host", context.Request.Host.Value);
        requestMessage.Headers.TryAddWithoutValidation("X-Forwarded-Proto", context.Request.Scheme);
        requestMessage.Headers.TryAddWithoutValidation("X-Forwarded-Method", context.Request.Method);
        requestMessage.Headers.TryAddWithoutValidation("X-Forwarded-Path", context.Request.Path.Value ?? "/");

        if (context.Connection.RemoteIpAddress is not null)
        {
            requestMessage.Headers.TryAddWithoutValidation(
                "X-Forwarded-For",
                context.Connection.RemoteIpAddress.ToString());
        }

        return requestMessage;
    }

    private static void CopyResponseHeaders(HttpResponseMessage source, HttpResponse destination)
    {
        foreach (var header in source.Headers)
        {
            if (HopByHopHeaders.Contains(header.Key))
            {
                continue;
            }

            destination.Headers[header.Key] = header.Value.ToArray();
        }

        if (source.Content is null)
        {
            return;
        }

        foreach (var header in source.Content.Headers)
        {
            if (HopByHopHeaders.Contains(header.Key) ||
                string.Equals(header.Key, HeaderNames.ContentLength, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            destination.Headers[header.Key] = header.Value.ToArray();
        }
    }

    private static bool RequestMayHaveBody(HttpRequest request)
    {
        return request.ContentLength > 0 ||
            request.Headers.ContainsKey(HeaderNames.TransferEncoding) ||
            HttpMethods.IsPost(request.Method) ||
            HttpMethods.IsPut(request.Method) ||
            HttpMethods.IsPatch(request.Method) ||
            HttpMethods.IsDelete(request.Method);
    }

    private static void StampResponseHeaders(
        HttpContext context,
        AspNetCoreStranglerFigCutoverDecision decision)
    {
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Cephalon-StranglerFig-RouteId"] = decision.Resolution.RouteId;
            context.Response.Headers["X-Cephalon-StranglerFig-Handling"] = decision.HandlingMode;
            context.Response.Headers["X-Cephalon-StranglerFig-Target"] = decision.Resolution.SelectedTarget.ToString().ToLowerInvariant();
            return Task.CompletedTask;
        });
    }

    private static Task WriteUnsupportedEndpointResponseAsync(
        HttpContext context,
        AspNetCoreStranglerFigCutoverDecision decision)
    {
        var problem = new StranglerFigUnsupportedEndpointProblem
        {
            Status = decision.ResponseStatusCode ?? StatusCodes.Status502BadGateway,
            Title = "Unsupported strangler-fig cutover endpoint.",
            Detail = decision.FailureReason,
            RouteId = decision.Resolution.RouteId,
            SelectedEndpoint = decision.Resolution.SelectedEndpoint,
            SelectedTarget = decision.Resolution.SelectedTarget.ToString().ToLowerInvariant(),
            HandlingMode = decision.HandlingMode
        };

        return Results.Json(
                problem,
                AspNetCoreJsonSerializerContext.Default.StranglerFigUnsupportedEndpointProblem,
                statusCode: problem.Status,
                contentType: "application/problem+json")
            .ExecuteAsync(context);
    }
}
