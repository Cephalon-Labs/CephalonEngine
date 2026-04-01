using Cephalon.Engine.Trust;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;

namespace Cephalon.AspNetCore.Transports.Rest;

/// <summary>
/// Adds Cephalon-specific conventions to REST route handlers.
/// </summary>
public static class RestEndpointConventionBuilderExtensions
{
    /// <summary>
    /// Requires a Cephalon capability decision before a REST endpoint can execute.
    /// </summary>
    /// <param name="builder">The route handler builder to protect.</param>
    /// <param name="capabilityKey">The capability key that must be allowed for the request.</param>
    /// <returns>The same route handler builder for further convention chaining.</returns>
    /// <remarks>
    /// This guard enforces the current trust and capability policy at the HTTP boundary. If the
    /// capability is denied, the endpoint returns a <c>403 Forbidden</c> problem response.
    /// </remarks>
    public static RouteHandlerBuilder RequireCapability(
        this RouteHandlerBuilder builder,
        string capabilityKey)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(capabilityKey);

        var normalizedCapabilityKey = capabilityKey.Trim();

        builder.AddEndpointFilter(async (context, next) =>
        {
            var evaluator = context.HttpContext.RequestServices.GetRequiredService<CapabilityPolicyEvaluator>();
            var decision = evaluator.TryGetDecision(normalizedCapabilityKey, out var resolvedDecision)
                ? resolvedDecision
                : throw new InvalidOperationException(
                    $"Capability policy decision for '{normalizedCapabilityKey}' was not available.");

            if (!decision.IsAllowed)
            {
                return TypedResults.Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Capability access denied",
                    detail: decision.Reason,
                    extensions: new Dictionary<string, object?>
                    {
                        ["capabilityKey"] = decision.CapabilityKey,
                        ["access"] = decision.Access.ToString(),
                        ["sourceModuleId"] = decision.SourceModuleId,
                        ["sourcePackageId"] = decision.SourcePackageId
                    });
            }

            return await next(context);
        });

        return builder;
    }
}
