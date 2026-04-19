using Cephalon.AspNetCore.Documentation;
using Cephalon.AspNetCore.Transports.Rest;
using Cephalon.AspNetCore.Health;
using Cephalon.AspNetCore.Transports.ServerSentEvents;
using Cephalon.AspNetCore.Transports.WebSockets;
using Cephalon.AspNetCore.Transformers;
using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.Resilience;
using Cephalon.Abstractions.Transports;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Diagnostics;
using Cephalon.Engine.Manifest;
using Cephalon.Engine.Runtime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.RateLimiting;

namespace Cephalon.AspNetCore.Hosting;

/// <summary>
/// Registers the Cephalon ASP.NET Core host services on a <see cref="WebApplicationBuilder" />.
/// </summary>
public static class EngineWebApplicationBuilderExtensions
{
    /// <summary>
    /// Adds Cephalon's project-configuration conventions to the ASP.NET Core builder.
    /// </summary>
    /// <param name="builder">The ASP.NET Core application builder to extend.</param>
    /// <returns>The same builder instance for fluent composition.</returns>
    /// <remarks>
    /// This loads split configuration files from the project's <c>Configurations</c> folder so
    /// settings such as engine, OpenAPI, CORS, or hosted-doc options can be grouped by concern
    /// without taking away the standard <c>appsettings.json</c> and <c>appsettings.{Environment}.json</c>
    /// override flow.
    /// </remarks>
    public static WebApplicationBuilder AddCephalonProjectConfigurations(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Configuration.AddCephalonProjectConfigurations(
            builder.Environment.ContentRootPath,
            builder.Environment.EnvironmentName);

        return builder;
    }

    /// <summary>
    /// Adds Cephalon to the builder using configuration-only engine setup.
    /// </summary>
    /// <param name="builder">The ASP.NET Core application builder to extend.</param>
    /// <returns>The same builder instance for fluent composition.</returns>
    public static WebApplicationBuilder AddCephalon(this WebApplicationBuilder builder)
    {
        return builder.AddCephalon(static _ => { });
    }

    /// <summary>
    /// Adds Cephalon to the builder and allows additional code-based engine configuration.
    /// </summary>
    /// <param name="builder">The ASP.NET Core application builder to extend.</param>
    /// <param name="configure">The callback that configures the underlying engine builder.</param>
    /// <returns>The same builder instance for fluent composition.</returns>
    /// <remarks>
    /// This method wires OpenAPI, Scalar-ready document transformers, health checks, hosted runtime
    /// startup, and the built-in ASP.NET Core transport mappers before registering the engine itself.
    /// </remarks>
    public static WebApplicationBuilder AddCephalon(
        this WebApplicationBuilder builder,
        Action<EngineBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddCephalonProjectConfigurations();
        builder.AddCephalonHttpLogging();

        foreach (var documentName in OpenApiDocumentNames.Resolve(builder.Configuration))
        {
            builder.Services.AddOpenApi(documentName, ConfigureOpenApiDocument);
        }

        builder.Services.AddProblemDetails();
        builder.Services.AddHealthChecks()
            .AddCheck<LivenessHealthCheck>("cephalon.liveness", tags: ["live", "engine"])
            .AddCheck<ReadinessHealthCheck>("cephalon.readiness", tags: ["ready", "engine"]);
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ITransportRouteMapper, RestTransportRouteMapper>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ITransportRouteMapper, ServerSentEventsTransportRouteMapper>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ITransportRouteMapper, WebSocketTransportRouteMapper>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, EngineHostedService>());
        var restApiGovernanceOptions = RestApiGovernanceOptions.FromConfiguration(builder.Configuration);
        builder.Services.TryAddSingleton(restApiGovernanceOptions);
        builder.Services.TryAddSingleton<AspNetCoreRestEndpointCandidateRuntimeCatalog>();
        builder.Services.TryAddSingleton<IRestEndpointCandidateRuntimeCatalog>(serviceProvider =>
            serviceProvider.GetRequiredService<AspNetCoreRestEndpointCandidateRuntimeCatalog>());
        builder.Services.TryAddSingleton<IRestEndpointCandidateRuntimeRegistry>(serviceProvider =>
            serviceProvider.GetRequiredService<AspNetCoreRestEndpointCandidateRuntimeCatalog>());
        builder.Services.TryAddSingleton<IRestEndpointAuthoringPolicyRuntimeCatalog>(serviceProvider =>
            new AspNetCoreRestEndpointAuthoringPolicyRuntimeCatalog(
                serviceProvider.GetRequiredService<IRestEndpointCandidateRuntimeCatalog>(),
                restApiGovernanceOptions));
        builder.Services.TryAddSingleton<IRestEndpointPublicationGroupRuntimeCatalog>(serviceProvider =>
            new AspNetCoreRestEndpointPublicationGroupRuntimeCatalog(
                serviceProvider.GetRequiredService<IRestEndpointCandidateRuntimeCatalog>(),
                restApiGovernanceOptions));
        builder.Services.TryAddSingleton<IRestEndpointOverrideRuntimeCatalog>(serviceProvider =>
            new AspNetCoreRestEndpointOverrideRuntimeCatalog(
                serviceProvider.GetRequiredService<IRestEndpointCandidateRuntimeCatalog>(),
                restApiGovernanceOptions));
        builder.Services.TryAddSingleton<IRestEndpointSuppressionRuntimeCatalog>(serviceProvider =>
            new AspNetCoreRestEndpointSuppressionRuntimeCatalog(
                serviceProvider.GetRequiredService<IRestEndpointCandidateRuntimeCatalog>(),
                restApiGovernanceOptions));
        builder.Services.TryAddSingleton<AspNetCoreRestEndpointRuntimeCatalog>();
        builder.Services.TryAddSingleton<IRestEndpointRuntimeCatalog>(serviceProvider =>
            serviceProvider.GetRequiredService<AspNetCoreRestEndpointRuntimeCatalog>());
        builder.Services.TryAddSingleton<IRestEndpointRuntimeRegistry>(serviceProvider =>
            serviceProvider.GetRequiredService<AspNetCoreRestEndpointRuntimeCatalog>());
        builder.AddReferenceDocsHosting();
        builder.Services.AddCephalon(builder.Configuration, configure);
        var stranglerFigCutoverOptions = AspNetCoreStranglerFigCutoverOptions.FromConfiguration(builder.Configuration);
        builder.Services.TryAddSingleton(stranglerFigCutoverOptions);
        builder.Services.TryAddSingleton<AspNetCoreStranglerFigCutoverCatalog>();
        builder.Services.AddHttpClient(AspNetCoreStranglerFigCutoverMiddleware.ProxyHttpClientName)
            .ConfigurePrimaryHttpMessageHandler(static () => new HttpClientHandler
            {
                AllowAutoRedirect = false,
                UseCookies = false
            });
        var referenceDocsOptions = ReferenceDocsHostingOptions.FromConfiguration(
            builder.Configuration,
            contentRootPath: builder.Environment.ContentRootPath);
        var rateLimitingPolicyCatalog = CreateRateLimitingPolicyCatalog(
            builder.Services,
            builder.Configuration,
            referenceDocsOptions);
        builder.Services.AddSingleton(rateLimitingPolicyCatalog);
        builder.Services.TryAddSingleton<IRateLimitingRuntimeCatalog>(_ =>
            new AspNetCoreRateLimitingRuntimeCatalog(rateLimitingPolicyCatalog));
        AddAspNetCoreRateLimiting(builder, rateLimitingPolicyCatalog);

        return builder;
    }

    private static void ConfigureOpenApiDocument(OpenApiOptions options)
    {
        options.AddDocumentTransformer<DocumentMetadataTransformer>();
        options.AddDocumentTransformer<OpenApiTagMetadataDocumentTransformer>();
        options.AddDocumentTransformer<SecuritySchemeTransformer>();
        options.AddDocumentTransformer(new XmlCommentsDocumentTransformer());
        options.AddDocumentTransformer<ResultModelDocumentTransformer>();
    }

    private static AspNetCoreRateLimitingPolicyCatalog CreateRateLimitingPolicyCatalog(
        IServiceCollection services,
        IConfiguration configuration,
        ReferenceDocsHostingOptions referenceDocsOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(referenceDocsOptions);

        var runtimeManifest = TryResolveRuntimeManifest(services);
        if (runtimeManifest is null)
        {
            return new AspNetCoreRateLimitingPolicyCatalog([]);
        }

        return AspNetCoreRateLimitingPolicyResolver.ResolvePolicies(
            runtimeManifest,
            configuration,
            referenceDocsOptions);
    }

    private static void AddAspNetCoreRateLimiting(
        WebApplicationBuilder builder,
        AspNetCoreRateLimitingPolicyCatalog policyCatalog)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(policyCatalog);

        if (!policyCatalog.HasEnabledPolicies)
        {
            return;
        }

        var apiRoutesOptions = ApiRoutesOptions.FromConfiguration(builder.Configuration);

        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = AspNetCoreRateLimitingPolicyResolver.RejectionStatusCode;
            foreach (var policy in policyCatalog.EnabledPolicies)
            {
                options.AddPolicy(policy.Id, httpContext =>
                {
                    var partitionKey = ResolveRateLimitingPartitionKey(httpContext);
                    return CreatePartition(policy, partitionKey);
                });
            }

            options.OnRejected = (context, cancellationToken) =>
                WriteRateLimitRejectedResponseAsync(context, apiRoutesOptions.UseResultModelEnvelope, cancellationToken);
        });
    }

    private static RuntimeManifest? TryResolveRuntimeManifest(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var manifest = services.LastOrDefault(static descriptor => descriptor.ServiceType == typeof(RuntimeManifest))
            ?.ImplementationInstance as RuntimeManifest;
        if (manifest is not null)
        {
            return manifest;
        }

        return (services.LastOrDefault(static descriptor => descriptor.ServiceType == typeof(IRuntime))
            ?.ImplementationInstance as IRuntime)?.Manifest;
    }

    private static RateLimitPartition<string> CreatePartition(
        ResolvedAspNetCoreRateLimitingPolicy resolvedPolicy,
        string partitionKey)
    {
        ArgumentNullException.ThrowIfNull(resolvedPolicy);
        ArgumentException.ThrowIfNullOrWhiteSpace(partitionKey);

        var effective = resolvedPolicy.Effective;
        var queueLimit = effective.QueueLimit ?? AspNetCoreRateLimitingPolicyResolver.DefaultQueueLimit;
        var permitLimit = effective.PermitLimit ?? AspNetCoreRateLimitingPolicyResolver.DefaultPermitLimit;
        var algorithm = effective.Algorithm ?? AspNetCoreRateLimitingPolicyResolver.DefaultAlgorithm;

        if (string.Equals(algorithm, "FixedWindow", StringComparison.OrdinalIgnoreCase))
        {
            var windowSeconds = effective.WindowSeconds ?? AspNetCoreRateLimitingPolicyResolver.DefaultWindowSeconds;
            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey,
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    QueueLimit = queueLimit,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    Window = TimeSpan.FromSeconds(windowSeconds),
                    AutoReplenishment = true
                });
        }

        if (string.Equals(algorithm, "TokenBucket", StringComparison.OrdinalIgnoreCase))
        {
            var windowSeconds = effective.WindowSeconds ?? AspNetCoreRateLimitingPolicyResolver.DefaultWindowSeconds;
            return RateLimitPartition.GetTokenBucketLimiter(
                partitionKey,
                _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = permitLimit,
                    TokensPerPeriod = permitLimit,
                    QueueLimit = queueLimit,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(windowSeconds),
                    AutoReplenishment = true
                });
        }

        if (string.Equals(algorithm, "ConcurrencyLimiter", StringComparison.OrdinalIgnoreCase))
        {
            return RateLimitPartition.GetConcurrencyLimiter(
                partitionKey,
                _ => new ConcurrencyLimiterOptions
                {
                    PermitLimit = permitLimit,
                    QueueLimit = queueLimit,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                });
        }

        var segmentsPerWindow = effective.SegmentsPerWindow ?? AspNetCoreRateLimitingPolicyResolver.DefaultSegmentsPerWindow;
        var slidingWindowSeconds = effective.WindowSeconds ?? AspNetCoreRateLimitingPolicyResolver.DefaultWindowSeconds;
        return RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey,
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                QueueLimit = queueLimit,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                Window = TimeSpan.FromSeconds(slidingWindowSeconds),
                SegmentsPerWindow = segmentsPerWindow,
                AutoReplenishment = true
            });
    }

    private static string ResolveRateLimitingPartitionKey(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var subjectId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.Identity?.Name;
        if (!string.IsNullOrWhiteSpace(subjectId))
        {
            return "subject:" + subjectId.Trim();
        }

        if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantId) &&
            !string.IsNullOrWhiteSpace(tenantId))
        {
            return "tenant:" + tenantId.ToString().Trim();
        }

        return context.Connection.RemoteIpAddress is not null
            ? "ip:" + context.Connection.RemoteIpAddress
            : "anonymous";
    }

    private static async ValueTask WriteRateLimitRejectedResponseAsync(
        OnRejectedContext context,
        bool useResultModelEnvelope,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        var retryAfterSeconds = TryResolveRetryAfterSeconds(context);
        if (retryAfterSeconds.HasValue)
        {
            context.HttpContext.Response.Headers.RetryAfter = retryAfterSeconds.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        if (useResultModelEnvelope)
        {
            var details = retryAfterSeconds.HasValue
                ? $"Retry after {retryAfterSeconds.Value} seconds."
                : null;
            var envelope = new ResultModelError
            {
                Title = "Too Many Requests",
                Message = "The request exceeded the configured Cephalon rate limit.",
                Success = false,
                StatusCode = AspNetCoreRateLimitingPolicyResolver.RejectionStatusCode,
                Errors =
                [
                    new ResultModelErrorDetail
                    {
                        Key = "rate_limit_exceeded",
                        Message = "The request exceeded the configured Cephalon rate limit.",
                        Severity = BehaviorFaultSeverity.Error,
                        Details = details
                    }
                ]
            };

            await Results.Json(
                    envelope,
                    statusCode: AspNetCoreRateLimitingPolicyResolver.RejectionStatusCode)
                .ExecuteAsync(context.HttpContext)
                .ConfigureAwait(false);
            return;
        }

        var problem = new ProblemDetails
        {
            Status = AspNetCoreRateLimitingPolicyResolver.RejectionStatusCode,
            Title = "Too Many Requests",
            Detail = "The request exceeded the configured Cephalon rate limit."
        };

        if (retryAfterSeconds.HasValue)
        {
            problem.Extensions["retryAfterSeconds"] = retryAfterSeconds.Value;
        }

        await Results.Json(
                problem,
                statusCode: AspNetCoreRateLimitingPolicyResolver.RejectionStatusCode,
                contentType: "application/problem+json")
            .ExecuteAsync(context.HttpContext)
            .ConfigureAwait(false);
    }

    private static int? TryResolveRetryAfterSeconds(OnRejectedContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
            ? (int)Math.Ceiling(retryAfter.TotalSeconds)
            : null;
    }

    /// <summary>
    /// Adds Cephalon's HTTP request and response logging options to the ASP.NET Core host.
    /// </summary>
    /// <param name="builder">The ASP.NET Core application builder to extend.</param>
    /// <param name="configure">
    /// An optional callback that can extend or override the configuration-driven request-logging setup.
    /// </param>
    /// <returns>The same builder instance for fluent host composition.</returns>
    /// <remarks>
    /// The logging contract is read from <c>Engine:Observability:HttpLogging</c> so teams can opt into
    /// request/response summaries and bounded body capture without introducing a separate host-specific section.
    /// </remarks>
    public static WebApplicationBuilder AddCephalonHttpLogging(
        this WebApplicationBuilder builder,
        Action<HttpRequestResponseLoggingOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddCephalonProjectConfigurations();

        var options = HttpRequestResponseLoggingOptions.FromConfiguration(builder.Configuration);
        configure?.Invoke(options);

        builder.Services.RemoveAll<HttpRequestResponseLoggingOptions>();
        builder.Services.AddSingleton(options);
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IDiagnosticsConventionContributor, AspNetCoreDiagnosticsConventionContributor>());

        return builder;
    }

    /// <summary>
    /// Adds hosted reference-doc configuration to the ASP.NET Core host.
    /// </summary>
    /// <param name="builder">The ASP.NET Core application builder to extend.</param>
    /// <param name="configure">
    /// An optional callback that can extend or override the configuration-driven hosting setup.
    /// </param>
    /// <returns>The same builder instance for fluent composition.</returns>
    /// <remarks>
    /// Reference-doc hosting stays in the host layer because it serves already-generated static
    /// artifacts such as <c>browse.html</c>, <c>members.md</c>, and <c>reference-manifest.json</c>.
    /// </remarks>
    public static WebApplicationBuilder AddReferenceDocsHosting(
        this WebApplicationBuilder builder,
        Action<ReferenceDocsHostingOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddCephalonProjectConfigurations();

        var options = ReferenceDocsHostingOptions.FromConfiguration(
            builder.Configuration,
            contentRootPath: builder.Environment.ContentRootPath);
        configure?.Invoke(options);

        builder.Services.RemoveAll<ReferenceDocsHostingOptions>();
        builder.Services.AddSingleton(options);

        return builder;
    }
}
