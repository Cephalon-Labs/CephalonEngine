using Cephalon.Abstractions.Behaviors;
using Cephalon.AspNetCore.Hosting;
using Cephalon.AspNetCore.Transports.Rest;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Cephalon.Behaviors.Http.Hosting;

internal static class BehaviorRestResponseMapper
{
    public static bool UseResultModelEnvelope(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var configuration = services.GetService(typeof(IConfiguration)) as IConfiguration;
        return configuration is not null &&
            ApiRoutesOptions.FromConfiguration(configuration).UseResultModelEnvelope;
    }

    public static IResult MapSuccess<TOutput>(TOutput result, IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (!UseResultModelEnvelope(services))
        {
            return TypedResults.Ok(result);
        }

        return Results.Json(
            new ResultModel<TOutput>
            {
                Title = "Ok",
                Message = "Successful",
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Data = result
            },
            statusCode: StatusCodes.Status200OK);
    }

    public static IResult MapNotFound(string message, IServiceProvider services, string? code = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentNullException.ThrowIfNull(services);

        if (!UseResultModelEnvelope(services))
        {
            return TypedResults.NotFound();
        }

        return CreateErrorEnvelope(
            statusCode: StatusCodes.Status404NotFound,
            title: "Not found",
            message: message,
            code: code ?? "not_found",
            severity: BehaviorFaultSeverity.Error);
    }

    public static IResult MapBadRequest(string message, IServiceProvider services, string? code = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentNullException.ThrowIfNull(services);

        if (!UseResultModelEnvelope(services))
        {
            return TypedResults.BadRequest(CreateProblemDetails(message));
        }

        return CreateErrorEnvelope(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Invalid request",
            message: message,
            code: code ?? "invalid_request",
            severity: BehaviorFaultSeverity.Error);
    }

    public static IResult MapTooManyRequests(
        string message,
        IServiceProvider services,
        string? code = null,
        int? retryAfterSeconds = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentNullException.ThrowIfNull(services);

        if (!UseResultModelEnvelope(services))
        {
            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Too Many Requests",
                Detail = message
            };

            if (retryAfterSeconds.HasValue)
            {
                problem.Extensions["retryAfterSeconds"] = retryAfterSeconds.Value;
            }

            return Results.Json(
                problem,
                statusCode: StatusCodes.Status429TooManyRequests,
                contentType: "application/problem+json");
        }

        var details = retryAfterSeconds.HasValue
            ? $"Retry after {retryAfterSeconds.Value} seconds."
            : null;
        return CreateErrorEnvelope(
            statusCode: StatusCodes.Status429TooManyRequests,
            title: "Too Many Requests",
            message: message,
            code: code ?? "too_many_requests",
            severity: BehaviorFaultSeverity.Error,
            details: details);
    }

    public static IResult MapServiceUnavailable(string message, IServiceProvider services, string? code = null)
        => MapServiceUnavailable(message, services, code, retryAfterSeconds: null);

    public static IResult MapServiceUnavailable(
        string message,
        IServiceProvider services,
        string? code,
        int? retryAfterSeconds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentNullException.ThrowIfNull(services);

        if (!UseResultModelEnvelope(services))
        {
            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "Service Unavailable",
                Detail = message
            };

            if (retryAfterSeconds.HasValue)
            {
                problem.Extensions["retryAfterSeconds"] = retryAfterSeconds.Value;
            }

            return Results.Json(
                problem,
                statusCode: StatusCodes.Status503ServiceUnavailable,
                contentType: "application/problem+json");
        }

        var details = retryAfterSeconds.HasValue
            ? $"Retry after {retryAfterSeconds.Value} seconds."
            : null;
        return CreateErrorEnvelope(
            statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "Service Unavailable",
            message: message,
            code: code ?? "service_unavailable",
            severity: BehaviorFaultSeverity.Error,
            details: details);
    }

    public static IResult MapBehaviorResult(IBehaviorResult result, IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(services);

        return result.Status switch
        {
            BehaviorResultStatus.Ok => result.HasValue
                ? MapSuccessObject(result.Value, services, StatusCodes.Status200OK, result.Message)
                : TypedResults.NoContent(),
            BehaviorResultStatus.Created => result.HasValue
                ? MapSuccessObject(result.Value, services, StatusCodes.Status201Created, result.Message)
                : Results.StatusCode(StatusCodes.Status201Created),
            BehaviorResultStatus.Accepted => result.HasValue
                ? MapSuccessObject(result.Value, services, StatusCodes.Status202Accepted, result.Message)
                : Results.StatusCode(StatusCodes.Status202Accepted),
            BehaviorResultStatus.NoContent => TypedResults.NoContent(),
            BehaviorResultStatus.Invalid => MapErrorResult(result, services, StatusCodes.Status400BadRequest, "Invalid request"),
            BehaviorResultStatus.Unauthorized => MapErrorResult(result, services, StatusCodes.Status401Unauthorized, "Unauthorized"),
            BehaviorResultStatus.Forbidden => MapErrorResult(result, services, StatusCodes.Status403Forbidden, "Forbidden"),
            BehaviorResultStatus.NotFound => MapErrorResult(result, services, StatusCodes.Status404NotFound, "Not found"),
            BehaviorResultStatus.Conflict => MapErrorResult(result, services, StatusCodes.Status409Conflict, "Conflict"),
            _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    private static IResult MapSuccessObject(object? value, IServiceProvider services, int statusCode, string message)
    {
        if (!UseResultModelEnvelope(services))
        {
            return statusCode switch
            {
                StatusCodes.Status201Created => Results.Json(value, statusCode: StatusCodes.Status201Created),
                StatusCodes.Status202Accepted => Results.Json(value, statusCode: StatusCodes.Status202Accepted),
                _ => Results.Json(value, statusCode: StatusCodes.Status200OK)
            };
        }

        var envelope = new ResultModel<object?>
        {
            Title = GetDefaultTitle(statusCode),
            Message = string.IsNullOrWhiteSpace(message) ? GetDefaultTitle(statusCode) : message,
            Success = true,
            StatusCode = statusCode,
            Data = value
        };

        return Results.Json(envelope, statusCode: statusCode);
    }

    private static IResult MapErrorResult(
        IBehaviorResult result,
        IServiceProvider services,
        int statusCode,
        string title)
    {
        if (!UseResultModelEnvelope(services))
        {
            return statusCode switch
            {
                StatusCodes.Status401Unauthorized => TypedResults.Unauthorized(),
                StatusCodes.Status403Forbidden => TypedResults.Problem(
                    statusCode: statusCode,
                    title: title,
                    detail: result.Message),
                StatusCodes.Status404NotFound => TypedResults.NotFound(),
                StatusCodes.Status409Conflict => TypedResults.Problem(
                    statusCode: statusCode,
                    title: title,
                    detail: result.Message),
                _ => TypedResults.BadRequest(CreateProblemDetails(result.Message))
            };
        }

        return CreateErrorEnvelope(
            statusCode,
            title,
            result.Message,
            result.Code ?? GetDefaultErrorKey(statusCode),
            result.Fault?.Severity ?? BehaviorFaultSeverity.Error,
            result.Fault?.Details,
            result.Fault);
    }

    private static IResult CreateErrorEnvelope(
        int statusCode,
        string title,
        string message,
        string code,
        BehaviorFaultSeverity severity,
        string? details = null,
        BehaviorFault? fault = null)
    {
        var envelope = new ResultModelError
        {
            Title = title,
            Message = message,
            Success = false,
            StatusCode = statusCode,
            Errors = BuildErrors(code, message, severity, details, fault)
        };

        return Results.Json(envelope, statusCode: statusCode);
    }

    private static List<ResultModelErrorDetail> BuildErrors(
        string code,
        string message,
        BehaviorFaultSeverity severity,
        string? details,
        BehaviorFault? fault)
    {
        if (fault?.InnerFaults.Count > 0)
        {
            return fault.InnerFaults
                .Select(MapFault)
                .ToList();
        }

        return
        [
            new ResultModelErrorDetail
            {
                Key = code,
                Message = message,
                Severity = severity,
                Details = details
            }
        ];
    }

    private static ResultModelErrorDetail MapFault(BehaviorFault fault)
    {
        ArgumentNullException.ThrowIfNull(fault);

        return new ResultModelErrorDetail
        {
            Key = string.IsNullOrWhiteSpace(fault.Code) ? "unknown_error" : fault.Code,
            Message = string.IsNullOrWhiteSpace(fault.Message) ? "The request failed." : fault.Message,
            Severity = fault.Severity,
            Details = fault.Details
        };
    }

    private static ProblemDetails CreateProblemDetails(string detail)
    {
        return new ProblemDetails
        {
            Title = "Behavior request rejected.",
            Detail = detail,
            Status = StatusCodes.Status400BadRequest
        };
    }

    private static string GetDefaultTitle(int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status201Created => "Created",
            StatusCodes.Status202Accepted => "Accepted",
            _ => "Ok"
        };
    }

    private static string GetDefaultErrorKey(int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status401Unauthorized => "unauthorized",
            StatusCodes.Status403Forbidden => "forbidden",
            StatusCodes.Status404NotFound => "not_found",
            StatusCodes.Status409Conflict => "conflict",
            _ => "invalid_request"
        };
    }
}
