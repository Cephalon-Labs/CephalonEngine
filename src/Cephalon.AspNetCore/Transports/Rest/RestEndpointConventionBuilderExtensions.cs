using Cephalon.Abstractions.Features;
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
    /// Requires one Cephalon feature flag to be enabled before a REST endpoint can execute.
    /// </summary>
    /// <param name="builder">The route handler builder to protect.</param>
    /// <param name="featureFlagId">The feature-flag identifier that must resolve to enabled.</param>
    /// <returns>The same route handler builder for further convention chaining.</returns>
    public static RouteHandlerBuilder RequireFeatureFlag(
        this RouteHandlerBuilder builder,
        string featureFlagId)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(featureFlagId);

        return builder.RequireFeatureFlags(featureFlagId);
    }

    /// <summary>
    /// Requires all requested Cephalon feature flags to be enabled before a REST endpoint can execute.
    /// </summary>
    /// <param name="builder">The route handler builder to protect.</param>
    /// <param name="featureFlagIds">The feature-flag identifiers that must resolve to enabled.</param>
    /// <returns>The same route handler builder for further convention chaining.</returns>
    /// <remarks>
    /// This guard keeps the endpoint published and introspectable while shifting rollout decisions
    /// to runtime evaluation at the HTTP boundary. If any required feature flag is unavailable for
    /// the request context, the endpoint returns a <c>404 Not Found</c> problem response.
    /// </remarks>
    public static RouteHandlerBuilder RequireFeatureFlags(
        this RouteHandlerBuilder builder,
        params string[] featureFlagIds)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var normalizedFeatureFlagIds = RestEndpointRuntimeMetadata.NormalizeFeatureFlagIds(featureFlagIds);
        if (normalizedFeatureFlagIds.Length == 0)
        {
            throw new ArgumentException(
                "At least one non-empty feature flag id is required.",
                nameof(featureFlagIds));
        }

        var registration = new RestEndpointFeatureFlagRegistration();

        builder.WithMetadata(new RestEndpointFeatureFlagMetadata(normalizedFeatureFlagIds));
        builder.WithMetadata(registration);

        builder.AddEndpointFilter(async (context, next) =>
        {
            var endpoint = context.HttpContext.GetEndpoint();
            if (endpoint is not null &&
                !ReferenceEquals(
                    endpoint.Metadata.OfType<RestEndpointFeatureFlagRegistration>().LastOrDefault(),
                    registration))
            {
                return await next(context);
            }

            var effectiveFeatureMetadata = endpoint?.Metadata
                .OfType<RestEndpointFeatureFlagMetadata>()
                .LastOrDefault();
            if (effectiveFeatureMetadata?.ClearsExisting == true ||
                effectiveFeatureMetadata is null ||
                effectiveFeatureMetadata.FeatureFlagIds.Count == 0)
            {
                return await next(context);
            }

            var featureToggle = context.HttpContext.RequestServices.GetRequiredService<IFeatureToggle>();
            var evaluationContext = RestEndpointFeatureFlagEvaluationContextFactory.Create(
                context.HttpContext,
                endpoint);
            var evaluation = effectiveFeatureMetadata.FeatureFlagIds
                .Select(featureFlagId => featureToggle.Evaluate(featureFlagId, evaluationContext))
                .FirstOrDefault(static result => !result.IsEnabled);
            if (evaluation is null)
            {
                return await next(context);
            }

            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Feature not available",
                detail: evaluation.Reason,
                extensions: new Dictionary<string, object?>
                {
                    ["featureFlagId"] = evaluation.FeatureId,
                    ["requiredFeatureFlagIds"] = effectiveFeatureMetadata.FeatureFlagIds.ToArray(),
                    ["sourceKind"] = evaluation.SourceKind?.ToString(),
                    ["sourceModuleId"] = evaluation.SourceModuleId
                });
        });

        return builder;
    }

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
        var registration = new RestEndpointCapabilityRegistration();

        builder.WithMetadata(new RestEndpointCapabilityMetadata(normalizedCapabilityKey));
        builder.WithMetadata(registration);

        builder.AddEndpointFilter(async (context, next) =>
        {
            var endpoint = context.HttpContext.GetEndpoint();
            if (endpoint is not null &&
                !ReferenceEquals(
                    endpoint.Metadata.OfType<RestEndpointCapabilityRegistration>().LastOrDefault(),
                    registration))
            {
                return await next(context);
            }

            var effectiveCapabilityMetadata = endpoint?.Metadata
                .OfType<RestEndpointCapabilityMetadata>()
                .LastOrDefault();
            if (effectiveCapabilityMetadata?.ClearsExisting == true ||
                string.IsNullOrWhiteSpace(effectiveCapabilityMetadata?.CapabilityKey))
            {
                return await next(context);
            }

            var effectiveCapabilityKey = effectiveCapabilityMetadata.CapabilityKey;
            var evaluator = context.HttpContext.RequestServices.GetRequiredService<CapabilityPolicyEvaluator>();
            var decision = evaluator.TryGetDecision(effectiveCapabilityKey, out var resolvedDecision)
                ? resolvedDecision
                : throw new InvalidOperationException(
                    $"Capability policy decision for '{effectiveCapabilityKey}' was not available.");

            if (!decision.IsAllowed)
            {
                return TypedResults.Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Capability access denied",
                    detail: FormatCapabilityDeniedDetail(decision),
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

    private static string FormatCapabilityDeniedDetail(CapabilityPolicyDecision decision)
    {
        ArgumentNullException.ThrowIfNull(decision);

        var reason = string.IsNullOrWhiteSpace(decision.Reason)
            ? "Capability access denied."
            : decision.Reason.Trim();
        return reason.Contains(decision.CapabilityKey, StringComparison.OrdinalIgnoreCase)
            ? reason
            : $"{reason} Capability '{decision.CapabilityKey}' was denied.";
    }

    /// <summary>
    /// Clears any previously declared Cephalon capability decision from a REST endpoint.
    /// </summary>
    /// <param name="builder">The route handler builder to update.</param>
    /// <returns>The same route handler builder for further convention chaining.</returns>
    /// <remarks>
    /// This uses the same last-declaration-wins model as <see cref="RequireCapability(RouteHandlerBuilder, string)" />.
    /// A later clear declaration suppresses earlier capability requirements for the same route.
    /// </remarks>
    public static RouteHandlerBuilder ClearRequiredCapability(this RouteHandlerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.WithMetadata(new RestEndpointCapabilityMetadata(null, ClearsExisting: true));
        builder.WithMetadata(new RestEndpointCapabilityRegistration());
        return builder;
    }

    /// <summary>
    /// Clears any previously declared Cephalon feature-flag requirements from a REST endpoint.
    /// </summary>
    /// <param name="builder">The route handler builder to update.</param>
    /// <returns>The same route handler builder for further convention chaining.</returns>
    /// <remarks>
    /// This uses the same last-declaration-wins model as
    /// <see cref="RequireFeatureFlags(RouteHandlerBuilder, string[])" />. A later clear
    /// declaration suppresses earlier feature requirements for the same route.
    /// </remarks>
    public static RouteHandlerBuilder ClearRequiredFeatureFlags(this RouteHandlerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.WithMetadata(new RestEndpointFeatureFlagMetadata([], ClearsExisting: true));
        builder.WithMetadata(new RestEndpointFeatureFlagRegistration());
        return builder;
    }
}
