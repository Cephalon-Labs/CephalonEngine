using Cephalon.MultiTenancy.Governance.AspNetCore.Configuration;
using Cephalon.MultiTenancy.Governance.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using System.Globalization;

namespace Cephalon.MultiTenancy.Governance.AspNetCore.Hosting;

/// <summary>
/// Maps ASP.NET Core endpoints for reading normalized tenant-invitation delivery status observations.
/// </summary>
public static class TenantInvitationDeliveryStatusObservationEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps the optional tenant-invitation delivery status observation read endpoint.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder to extend.</param>
    /// <returns>The same endpoint route builder for fluent routing composition.</returns>
    /// <remarks>
    /// The endpoint is opt-in, reads the host-agnostic <see cref="ITenantInvitationDeliveryStatusObservationStore" />,
    /// and performs a fail-closed authorization check by default. It exposes bounded normalized observation history only;
    /// provider-specific callback inboxes, provider polling, and distributed replay semantics remain application-managed
    /// or future provider-pack responsibilities.
    /// </remarks>
    public static IEndpointRouteBuilder MapCephalonTenantInvitationDeliveryStatusObservations(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var options = endpoints.ServiceProvider.GetService<MultiTenancyGovernanceAspNetCoreOptions>() ??
            new MultiTenancyGovernanceAspNetCoreOptions();
        if (!options.EnableTenantInvitationDeliveryStatusObservationEndpoint)
        {
            return endpoints;
        }

        var routePattern = Normalize(options.TenantInvitationDeliveryStatusObservationRoutePattern) ??
            MultiTenancyGovernanceAspNetCoreOptions.DefaultTenantInvitationDeliveryStatusObservationRoutePattern;
        var builder = endpoints
            .MapGet(
                routePattern,
                (
                    HttpContext context,
                    ITenantInvitationDeliveryStatusObservationStore observationStore) =>
                    ReadObservationsAsync(context, observationStore, options))
            .WithName("CephalonTenantInvitationDeliveryStatusObservations")
            .Produces<TenantInvitationDeliveryStatusObservationQueryResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        if (options.ExcludeTenantInvitationDeliveryStatusObservationEndpointFromDescription)
        {
            builder.ExcludeFromDescription();
        }

        ApplyAuthorizationMetadata(endpoints, builder, options);

        endpoints.ServiceProvider
            .GetService<TenantInvitationDeliveryStatusEndpointRuntimeCatalog>()
            ?.RecordObservationEndpointMapped(
                routePattern,
                options.RequireTenantInvitationDeliveryStatusObservationAuthorization,
                Normalize(options.TenantInvitationDeliveryStatusObservationAuthorizationPolicy),
                options.ExcludeTenantInvitationDeliveryStatusObservationEndpointFromDescription,
                GetDefaultLimit(options),
                GetMaxLimit(options));

        return endpoints;
    }

    private static async Task<IResult> ReadObservationsAsync(
        HttpContext context,
        ITenantInvitationDeliveryStatusObservationStore observationStore,
        MultiTenancyGovernanceAspNetCoreOptions options)
    {
        var authorizationResult = await AuthorizeAsync(context, options).ConfigureAwait(false);
        if (authorizationResult is not null)
        {
            return authorizationResult;
        }

        var query = ParseQuery(context, options);
        if (query.Failure is not null)
        {
            return query.Failure;
        }

        var observations = observationStore.Observations;
        var filtered = observations
            .Where(query.Matches)
            .OrderByDescending(static observation => observation.RecordedAtUtc)
            .ThenByDescending(static observation => observation.ObservedAtUtc)
            .ThenBy(static observation => observation.ObservationId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var limited = filtered
            .Take(query.Limit)
            .ToArray();
        var summaries = BuildSummaries(filtered);

        return Results.Json(new TenantInvitationDeliveryStatusObservationQueryResult
        {
            StoreKind = observationStore.StoreKind,
            IsDurable = observationStore.IsDurable,
            Ownership = observationStore.Ownership,
            TotalCount = observations.Count,
            MatchedCount = filtered.Length,
            ReturnedCount = limited.Length,
            SummaryCount = summaries.Length,
            Limit = query.Limit,
            Filters = query.Filters,
            Observations = limited,
            Summaries = summaries
        });
    }

    private static TenantInvitationDeliveryStatusObservationSummaryDescriptor[] BuildSummaries(
        IReadOnlyList<TenantInvitationDeliveryStatusObservationDescriptor> observations)
    {
        return
        [
            .. BuildSummaryDimension(observations, "status", static observation => observation.Status),
            .. BuildAttentionSummaries(observations),
            .. BuildSummaryDimension(observations, "outcome", static observation => observation.Outcome),
            .. BuildSummaryDimension(observations, "source", static observation => observation.Source),
            .. BuildSummaryDimension(observations, "channel", static observation => observation.Channel),
            .. BuildSummaryDimension(observations, "sender", static observation => observation.SenderId),
            .. BuildSummaryDimension(observations, "tenant", static observation => observation.TenantId)
        ];
    }

    private static TenantInvitationDeliveryStatusObservationSummaryDescriptor[] BuildAttentionSummaries(
        IReadOnlyList<TenantInvitationDeliveryStatusObservationDescriptor> observations)
    {
        return observations
            .SelectMany(
                static observation => GetAttentionCategories(observation)
                    .Select(category => new AttentionObservation(category, observation)))
            .GroupBy(static item => item.Category, StringComparer.OrdinalIgnoreCase)
            .Select(group => new TenantInvitationDeliveryStatusObservationSummaryDescriptor(
                "attention",
                group.Key,
                group.Count(),
                group.Count(static item => item.Observation.Reconciled),
                group.Count(static item => item.Observation.Recorded),
                group.Max(static item => item.Observation.ObservedAtUtc),
                group.Max(static item => item.Observation.RecordedAtUtc)))
            .OrderByDescending(static summary => summary.Count)
            .ThenBy(static summary => summary.Value, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static TenantInvitationDeliveryStatusObservationSummaryDescriptor[] BuildSummaryDimension(
        IReadOnlyList<TenantInvitationDeliveryStatusObservationDescriptor> observations,
        string dimension,
        Func<TenantInvitationDeliveryStatusObservationDescriptor, string?> selector)
    {
        return observations
            .GroupBy(
                observation => NormalizeSummaryValue(selector(observation)),
                StringComparer.OrdinalIgnoreCase)
            .Select(group => new TenantInvitationDeliveryStatusObservationSummaryDescriptor(
                dimension,
                group.Key,
                group.Count(),
                group.Count(static observation => observation.Reconciled),
                group.Count(static observation => observation.Recorded),
                group.Max(static observation => observation.ObservedAtUtc),
                group.Max(static observation => observation.RecordedAtUtc)))
            .OrderByDescending(static summary => summary.Count)
            .ThenBy(static summary => summary.Value, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static ObservationReadQuery ParseQuery(
        HttpContext context,
        MultiTenancyGovernanceAspNetCoreOptions options)
    {
        var filters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var tenantId = ReadFilter(context, "tenantId", filters);
        var invitationId = ReadFilter(context, "invitationId", filters);
        var status = ReadFilter(context, "status", filters);
        var outcome = ReadFilter(context, "outcome", filters);
        var source = ReadFilter(context, "source", filters);
        var correlationId = ReadFilter(context, "correlationId", filters);
        var attention = ReadAttentionFilter(context, filters);
        if (attention.Failure is not null)
        {
            return ObservationReadQuery.Fail(attention.Failure);
        }

        var reconciled = ReadBooleanFilter(context, "reconciled", filters);
        if (reconciled.Failure is not null)
        {
            return ObservationReadQuery.Fail(reconciled.Failure);
        }

        var recorded = ReadBooleanFilter(context, "recorded", filters);
        if (recorded.Failure is not null)
        {
            return ObservationReadQuery.Fail(recorded.Failure);
        }

        var limit = ReadLimit(context, options);
        if (limit.Failure is not null)
        {
            return ObservationReadQuery.Fail(limit.Failure);
        }

        return new ObservationReadQuery(
            limit.Value,
            filters,
            observation =>
                Matches(observation.TenantId, tenantId) &&
                Matches(observation.InvitationId, invitationId) &&
                Matches(observation.Status, status) &&
                Matches(observation.Outcome, outcome) &&
                Matches(observation.Source, source) &&
                Matches(observation.CorrelationId, correlationId) &&
                MatchesAttention(observation, attention.Value) &&
                Matches(observation.Reconciled, reconciled.Value) &&
                Matches(observation.Recorded, recorded.Value),
            null);
    }

    private static TextFilterReadResult ReadAttentionFilter(
        HttpContext context,
        Dictionary<string, string> filters)
    {
        if (!TryReadSingleQueryValue(context, "attention", out var value))
        {
            return new TextFilterReadResult(null, null);
        }

        var normalized = TenantInvitationDeliveryStatusObservationAttentionCategories.Normalize(value);
        if (normalized is null)
        {
            return new TextFilterReadResult(
                null,
                Results.Problem(
                    title: "Tenant invitation delivery status observation attention filter is invalid.",
                    detail: $"Set 'attention' to one of: {TenantInvitationDeliveryStatusObservationAttentionCategories.KnownValues}.",
                    statusCode: StatusCodes.Status400BadRequest));
        }

        filters["attention"] = normalized;
        return new TextFilterReadResult(normalized, null);
    }

    private static string? ReadFilter(
        HttpContext context,
        string name,
        Dictionary<string, string> filters)
    {
        if (!TryReadSingleQueryValue(context, name, out var value))
        {
            return null;
        }

        filters[name] = value;
        return value;
    }

    private static BooleanFilterReadResult ReadBooleanFilter(
        HttpContext context,
        string name,
        Dictionary<string, string> filters)
    {
        if (!TryReadSingleQueryValue(context, name, out var value))
        {
            return new BooleanFilterReadResult(null, null);
        }

        if (!bool.TryParse(value, out var parsed))
        {
            return new BooleanFilterReadResult(
                null,
                Results.Problem(
                    title: $"Tenant invitation delivery status observation filter '{name}' is invalid.",
                    detail: $"Set '{name}' to 'true' or 'false'.",
                    statusCode: StatusCodes.Status400BadRequest));
        }

        filters[name] = parsed.ToString().ToLowerInvariant();
        return new BooleanFilterReadResult(parsed, null);
    }

    private static LimitReadResult ReadLimit(
        HttpContext context,
        MultiTenancyGovernanceAspNetCoreOptions options)
    {
        var defaultLimit = GetDefaultLimit(options);
        if (!TryReadSingleQueryValue(context, "limit", out var value))
        {
            return new LimitReadResult(defaultLimit, null);
        }

        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) ||
            parsed < 1)
        {
            return new LimitReadResult(
                defaultLimit,
                Results.Problem(
                    title: "Tenant invitation delivery status observation limit is invalid.",
                    detail: "Set 'limit' to a positive whole number.",
                    statusCode: StatusCodes.Status400BadRequest));
        }

        return new LimitReadResult(Math.Min(parsed, GetMaxLimit(options)), null);
    }

    private static int GetDefaultLimit(MultiTenancyGovernanceAspNetCoreOptions options)
    {
        return Math.Clamp(options.TenantInvitationDeliveryStatusObservationDefaultLimit, 1, GetMaxLimit(options));
    }

    private static int GetMaxLimit(MultiTenancyGovernanceAspNetCoreOptions options)
    {
        return Math.Clamp(options.TenantInvitationDeliveryStatusObservationMaxLimit, 1, 1_000_000);
    }

    private static bool TryReadSingleQueryValue(
        HttpContext context,
        string name,
        out string value)
    {
        value = string.Empty;
        if (!context.Request.Query.TryGetValue(name, out StringValues values) ||
            values.Count == 0)
        {
            return false;
        }

        value = values[0]?.Trim() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(value);
    }

    private static bool Matches(string? actual, string? expected)
    {
        return expected is null ||
            string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
    }

    private static bool Matches(bool actual, bool? expected)
    {
        return expected is null || actual == expected.Value;
    }

    private static bool MatchesAttention(
        TenantInvitationDeliveryStatusObservationDescriptor observation,
        string? expected)
    {
        return expected is null ||
            GetAttentionCategories(observation)
                .Any(category => string.Equals(category, expected, StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<string> GetAttentionCategories(
        TenantInvitationDeliveryStatusObservationDescriptor observation)
    {
        switch (observation.Status)
        {
            case TenantInvitationDeliveryStatuses.Failed:
            case TenantInvitationDeliveryStatuses.Bounced:
                yield return TenantInvitationDeliveryStatusObservationAttentionCategories.DeliveryFailed;
                break;
            case TenantInvitationDeliveryStatuses.Deferred:
                yield return TenantInvitationDeliveryStatusObservationAttentionCategories.DeliveryDeferred;
                break;
            case TenantInvitationDeliveryStatuses.Suppressed:
                yield return TenantInvitationDeliveryStatusObservationAttentionCategories.DeliverySuppressed;
                break;
            case TenantInvitationDeliveryStatuses.Unknown:
                yield return TenantInvitationDeliveryStatusObservationAttentionCategories.DeliveryUnknown;
                break;
        }

        if (!observation.Reconciled ||
            !string.Equals(
                observation.Outcome,
                TenantInvitationDeliveryStatusReconciliationOutcomes.Reconciled,
                StringComparison.OrdinalIgnoreCase))
        {
            yield return TenantInvitationDeliveryStatusObservationAttentionCategories.ReconciliationGap;
        }

        if (!observation.Recorded)
        {
            yield return TenantInvitationDeliveryStatusObservationAttentionCategories.RecordingGap;
        }
    }

    private static async ValueTask<IResult?> AuthorizeAsync(
        HttpContext context,
        MultiTenancyGovernanceAspNetCoreOptions options)
    {
        if (!options.RequireTenantInvitationDeliveryStatusObservationAuthorization)
        {
            return null;
        }

        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Results.Problem(
                title: "Tenant invitation delivery status observation authorization is required.",
                detail: "The Cephalon tenant-invitation delivery status observation endpoint is fail-closed by default.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var authorizationPolicy = Normalize(options.TenantInvitationDeliveryStatusObservationAuthorizationPolicy);
        if (authorizationPolicy is null)
        {
            return null;
        }

        var authorizationService = context.RequestServices.GetService<IAuthorizationService>();
        if (authorizationService is null)
        {
            return Results.Problem(
                title: "Tenant invitation delivery status observation authorization cannot be evaluated.",
                detail: "Register ASP.NET Core authorization services or disable endpoint authorization deliberately.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        var authorization = await authorizationService
            .AuthorizeAsync(context.User, context, authorizationPolicy)
            .ConfigureAwait(false);
        if (authorization.Succeeded)
        {
            return null;
        }

        return Results.Problem(
            title: "Tenant invitation delivery status observation authorization failed.",
            detail: "The authenticated principal is not authorized to read tenant-invitation delivery status observations.",
            statusCode: StatusCodes.Status403Forbidden);
    }

    private static void ApplyAuthorizationMetadata(
        IEndpointRouteBuilder endpoints,
        IEndpointConventionBuilder builder,
        MultiTenancyGovernanceAspNetCoreOptions options)
    {
        if (!options.RequireTenantInvitationDeliveryStatusObservationAuthorization ||
            endpoints.ServiceProvider.GetService<IAuthorizationService>() is null ||
            endpoints.ServiceProvider.GetService<IAuthenticationSchemeProvider>() is null)
        {
            return;
        }

        var authorizationPolicy = Normalize(options.TenantInvitationDeliveryStatusObservationAuthorizationPolicy);
        if (authorizationPolicy is null)
        {
            builder.RequireAuthorization();
        }
        else
        {
            builder.RequireAuthorization(authorizationPolicy);
        }
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static string NormalizeSummaryValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "none"
            : value.Trim();
    }

    private sealed record ObservationReadQuery(
        int Limit,
        IReadOnlyDictionary<string, string> Filters,
        Func<TenantInvitationDeliveryStatusObservationDescriptor, bool> Matches,
        IResult? Failure)
    {
        public static ObservationReadQuery Fail(IResult failure) =>
            new(0, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase), static _ => false, failure);
    }

    private sealed record BooleanFilterReadResult(bool? Value, IResult? Failure);

    private sealed record TextFilterReadResult(string? Value, IResult? Failure);

    private sealed record LimitReadResult(int Value, IResult? Failure);

    private sealed record AttentionObservation(
        string Category,
        TenantInvitationDeliveryStatusObservationDescriptor Observation);
}
