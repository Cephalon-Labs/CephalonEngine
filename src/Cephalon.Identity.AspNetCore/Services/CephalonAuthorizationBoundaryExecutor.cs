using Cephalon.Abstractions.Authorization;
using Cephalon.Identity.AspNetCore.Configuration;
using Cephalon.Identity.AspNetCore.Transports.Rest;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.Identity.AspNetCore.Services;

internal sealed class CephalonAuthorizationBoundaryExecutor(
    HttpContextAuthorizationRequestFactory requestFactory,
    Cephalon.Abstractions.Authorization.IAuthorizationEvaluator evaluator,
    IdentityAspNetCoreOptions options)
{
    public async ValueTask<CephalonAuthorizationBoundaryExecutionResult> ExecuteAsync(
        HttpContext httpContext,
        RestAuthorizationRequestMetadata metadata,
        bool allowAnonymous,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(metadata);

        cancellationToken.ThrowIfCancellationRequested();

        if (allowAnonymous)
        {
            return CephalonAuthorizationBoundaryExecutionResult.Allow();
        }

        if (!requestFactory.TryCreate(httpContext, metadata, out var request, out var failureReason))
        {
            var challengeResult = await TryCreateAuthenticationBoundaryResultAsync(httpContext, forbid: false).ConfigureAwait(false);
            if (challengeResult is not null)
            {
                return challengeResult;
            }

            return CephalonAuthorizationBoundaryExecutionResult.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Authentication required",
                detail: failureReason ?? "An authenticated user is required for this endpoint.",
                extensions: new Dictionary<string, object?>
                {
                    ["policyId"] = metadata.PolicyId
                });
        }

        var decision = await evaluator.EvaluateAsync(
            request!.Subject,
            request.Resource,
            request.Context,
            cancellationToken).ConfigureAwait(false);
        httpContext.Items[options.AuthorizationDecisionItemKey] = decision;

        if (decision.IsAllowed)
        {
            return CephalonAuthorizationBoundaryExecutionResult.Allow();
        }

        var forbidResult = await TryCreateAuthenticationBoundaryResultAsync(httpContext, forbid: true).ConfigureAwait(false);
        if (forbidResult is not null)
        {
            return forbidResult;
        }

        return CephalonAuthorizationBoundaryExecutionResult.Problem(
            statusCode: StatusCodes.Status403Forbidden,
            title: "Authorization denied",
            detail: decision.Reason ?? "The current request was denied by the Cephalon authorization boundary.",
            extensions: new Dictionary<string, object?>
            {
                ["policyId"] = decision.PolicyId ?? metadata.PolicyId,
                ["modes"] = decision.Modes.Select(static mode => mode.ToString()).ToArray(),
                ["metadata"] = decision.Metadata
            });
    }

    private static async ValueTask<CephalonAuthorizationBoundaryExecutionResult?> TryCreateAuthenticationBoundaryResultAsync(
        HttpContext httpContext,
        bool forbid)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var endpoint = httpContext.GetEndpoint();
        var cephalonSchemes = endpoint?.Metadata
            .GetMetadata<CephalonAuthenticationSchemesMetadata>()?
            .AuthenticationSchemes ?? [];
        if (cephalonSchemes.Length > 0)
        {
            return forbid
                ? CephalonAuthorizationBoundaryExecutionResult.Forbid(cephalonSchemes)
                : CephalonAuthorizationBoundaryExecutionResult.Challenge(cephalonSchemes);
        }

        var explicitSchemes = endpoint?.Metadata
            .GetOrderedMetadata<IAuthorizeData>()
            .SelectMany(static endpointMetadata => SplitAuthenticationSchemes(endpointMetadata.AuthenticationSchemes))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static scheme => scheme, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
        if (explicitSchemes.Length > 0)
        {
            return forbid
                ? CephalonAuthorizationBoundaryExecutionResult.Forbid(explicitSchemes)
                : CephalonAuthorizationBoundaryExecutionResult.Challenge(explicitSchemes);
        }

        var schemeProvider = httpContext.RequestServices.GetService<IAuthenticationSchemeProvider>();
        if (schemeProvider is null)
        {
            return null;
        }

        var defaultScheme = forbid
            ? await schemeProvider.GetDefaultForbidSchemeAsync().ConfigureAwait(false) ??
              await schemeProvider.GetDefaultChallengeSchemeAsync().ConfigureAwait(false)
            : await schemeProvider.GetDefaultChallengeSchemeAsync().ConfigureAwait(false);
        if (defaultScheme is null)
        {
            return null;
        }

        return forbid
            ? CephalonAuthorizationBoundaryExecutionResult.Forbid([])
            : CephalonAuthorizationBoundaryExecutionResult.Challenge([]);
    }

    private static string[] SplitAuthenticationSchemes(string? schemes)
    {
        return schemes?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(static scheme => !string.IsNullOrWhiteSpace(scheme))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static scheme => scheme, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }
}

internal sealed class CephalonAuthorizationBoundaryExecutionResult
{
    private CephalonAuthorizationBoundaryExecutionResult(
        bool isAllowed,
        bool usesAuthenticationBoundary,
        bool isForbid,
        string[] authenticationSchemes,
        ProblemDetails? problemDetails)
    {
        IsAllowed = isAllowed;
        UsesAuthenticationBoundary = usesAuthenticationBoundary;
        IsForbid = isForbid;
        AuthenticationSchemes = authenticationSchemes;
        ProblemDetails = problemDetails;
    }

    public bool IsAllowed { get; }

    public bool UsesAuthenticationBoundary { get; }

    public bool IsForbid { get; }

    public string[] AuthenticationSchemes { get; }

    public ProblemDetails? ProblemDetails { get; }

    public static CephalonAuthorizationBoundaryExecutionResult Allow()
    {
        return new CephalonAuthorizationBoundaryExecutionResult(
            isAllowed: true,
            usesAuthenticationBoundary: false,
            isForbid: false,
            authenticationSchemes: [],
            problemDetails: null);
    }

    public static CephalonAuthorizationBoundaryExecutionResult Challenge(string[] authenticationSchemes)
    {
        return new CephalonAuthorizationBoundaryExecutionResult(
            isAllowed: false,
            usesAuthenticationBoundary: true,
            isForbid: false,
            authenticationSchemes: authenticationSchemes ?? [],
            problemDetails: null);
    }

    public static CephalonAuthorizationBoundaryExecutionResult Forbid(string[] authenticationSchemes)
    {
        return new CephalonAuthorizationBoundaryExecutionResult(
            isAllowed: false,
            usesAuthenticationBoundary: true,
            isForbid: true,
            authenticationSchemes: authenticationSchemes ?? [],
            problemDetails: null);
    }

    public static CephalonAuthorizationBoundaryExecutionResult Problem(
        int statusCode,
        string title,
        string detail,
        IDictionary<string, object?>? extensions = null)
    {
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail
        };

        if (extensions is not null)
        {
            foreach (var (key, value) in extensions)
            {
                problem.Extensions[key] = value;
            }
        }

        return new CephalonAuthorizationBoundaryExecutionResult(
            isAllowed: false,
            usesAuthenticationBoundary: false,
            isForbid: false,
            authenticationSchemes: [],
            problemDetails: problem);
    }

    public IResult ToMinimalApiResult()
    {
        if (ProblemDetails is not null)
        {
            return TypedResults.Problem(
                statusCode: ProblemDetails.Status,
                title: ProblemDetails.Title,
                detail: ProblemDetails.Detail,
                extensions: ProblemDetails.Extensions);
        }

        return IsForbid
            ? (AuthenticationSchemes.Length == 0
                ? Results.Forbid()
                : Results.Forbid(authenticationSchemes: AuthenticationSchemes))
            : (AuthenticationSchemes.Length == 0
                ? Results.Challenge()
                : Results.Challenge(authenticationSchemes: AuthenticationSchemes));
    }

    public IActionResult ToMvcActionResult()
    {
        if (ProblemDetails is not null)
        {
            return new ObjectResult(ProblemDetails)
            {
                StatusCode = ProblemDetails.Status
            };
        }

        return IsForbid
            ? (AuthenticationSchemes.Length == 0
                ? new ForbidResult()
                : new ForbidResult(AuthenticationSchemes))
            : (AuthenticationSchemes.Length == 0
                ? new ChallengeResult()
                : new ChallengeResult(AuthenticationSchemes));
    }
}
