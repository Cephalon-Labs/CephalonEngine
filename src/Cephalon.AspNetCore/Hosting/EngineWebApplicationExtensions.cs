using Cephalon.AspNetCore.Documentation;
using Cephalon.AspNetCore.Diagnostics;
using Cephalon.AspNetCore.Health;
using Cephalon.Abstractions.AppModel;
using Cephalon.Abstractions.Audit;
using Cephalon.Abstractions.Authorization;
using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Execution;
using Cephalon.Abstractions.Localization;
using Cephalon.Abstractions.Patterns;
using Cephalon.Abstractions.Resilience;
using Cephalon.Abstractions.Technologies;
using Cephalon.Abstractions.Transports;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Diagnostics;
using Cephalon.Engine.Manifest;
using Cephalon.Engine.Runtime;
using Cephalon.Engine.Technologies;
using Cephalon.Engine.Trust;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using System.Globalization;
using System.Reflection;
using System.Text.Json;

namespace Cephalon.AspNetCore.Hosting;

/// <summary>
/// Maps the operator-facing HTTP surface exposed by a Cephalon ASP.NET Core host.
/// </summary>
public static class EngineWebApplicationExtensions
{
    private const string OpenApiToggleScriptResourceName = "Cephalon.AspNetCore.Assets.openapi-toggle.js";
    private const string ScalarFaviconResourceName = "Cephalon.AspNetCore.Assets.docs-favicon.svg";
    private const string ScalarRoutePrefixToken = "__CEPHALON_SCALAR_ROUTE_PREFIX__";
    private const string ScalarDocumentNamesToken = "__CEPHALON_SCALAR_DOCUMENT_NAMES__";
    private const string ScalarDefaultDocumentNameToken = "__CEPHALON_SCALAR_DEFAULT_DOCUMENT_NAME__";
    private static readonly string DocumentationAssetVersion = typeof(EngineWebApplicationExtensions)
        .Assembly
        .ManifestModule
        .ModuleVersionId
        .ToString("N");
    private static readonly Lazy<string> OpenApiToggleScriptTemplate = new(() => LoadEmbeddedAsset(
        OpenApiToggleScriptResourceName,
        "Scalar configuration script"));
    private static readonly Lazy<string> ScalarFavicon = new(() => LoadEmbeddedAsset(
        ScalarFaviconResourceName,
        "Scalar favicon"));

    /// <summary>
    /// Maps Cephalon runtime, diagnostics, transport, and documentation endpoints onto the application.
    /// </summary>
    /// <param name="app">The ASP.NET Core application to extend.</param>
    /// <returns>The same application instance for fluent host composition.</returns>
    /// <remarks>
    /// <para>
    /// This method maps the engine introspection surface under <c>/engine</c>, health and diagnostics
    /// endpoints, and the routes contributed by the transports selected in the runtime manifest.
    /// </para>
    /// <para>
    /// When the REST transport is active, it also enables OpenAPI and Scalar documentation while keeping
    /// non-REST protocol routes out of the generated API description.
    /// </para>
    /// </remarks>
    public static WebApplication MapCephalon(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var runtime = app.Services.GetRequiredService<IRuntime>();
        var configuration = app.Services.GetRequiredService<IConfiguration>();
        var localizedTextCatalog = app.Services.GetRequiredService<ILocalizedTextCatalog>();
        var localizationSettings = app.Services.GetRequiredService<LocalizationSettings>();
        var referenceDocsOptions = app.Services.GetService<ReferenceDocsHostingOptions>() ?? new ReferenceDocsHostingOptions();
        var httpLoggingOptions = app.Services.GetService<HttpRequestResponseLoggingOptions>()
            ?? HttpRequestResponseLoggingOptions.FromConfiguration(configuration);
        var openApiEndpointOptions = OpenApiEndpointOptions.FromConfiguration(configuration);
        var referenceDocsSurface = CreateReferenceDocsSurface(referenceDocsOptions);
        var openApiDocumentNames = OpenApiDocumentNames.Resolve(configuration);
        var defaultOpenApiDocumentName = OpenApiDocumentNames.ResolveDefault(configuration);
        var restApiSelected = runtime.Manifest.AppProfile.Transports.Any(transport =>
            string.Equals(transport.Id, "rest-api", StringComparison.OrdinalIgnoreCase));
        var openApiToggleScriptRoute = BuildScalarAssetRoute(openApiEndpointOptions.ScalarRoutePrefix, "openapi-toggle.js");
        var scalarFaviconRoute = BuildScalarAssetRoute(openApiEndpointOptions.ScalarRoutePrefix, "assets/favicon.svg");
        var openApiToggleScriptReference = BuildVersionedAssetReference(openApiToggleScriptRoute);
        var scalarFaviconReference = BuildVersionedAssetReference(scalarFaviconRoute);

        app.UseRequestLocalization(BuildRequestLocalizationOptions(localizationSettings, localizedTextCatalog));
        if (httpLoggingOptions.Enabled)
        {
            app.UseMiddleware<HttpRequestResponseLoggingMiddleware>();
        }

        var engineGroup = app.MapGroup("/engine");
        engineGroup.ExcludeFromDescription();
        engineGroup.DisableRateLimiting();
        engineGroup.MapGet("/", (RuntimeManifest manifest) => TypedResults.Ok(manifest))
            .WithName("GetCephalonManifest");
        engineGroup.MapGet("/manifest", (RuntimeManifest manifest) => TypedResults.Ok(manifest))
            .WithName("GetCephalonManifestByPath");
        engineGroup.MapGet("/snapshot", ([FromServices] IRuntimeIntrospectionSnapshotProvider provider) =>
                TypedResults.Ok(provider.CreateSnapshot()))
            .WithName("GetCephalonSnapshot");
        engineGroup.MapGet("/app-model", (RuntimeManifest manifest) => TypedResults.Ok(manifest.AppProfile))
            .WithName("GetCephalonAppModel");
        engineGroup.MapGet("/resilience", (RuntimeManifest manifest) => TypedResults.Ok(manifest.AppProfile.Resilience))
            .WithName("GetCephalonResilience");
        engineGroup.MapGet("/behavior-resilience", (HttpContext httpContext) =>
            {
                var catalog = httpContext.RequestServices.GetService<Cephalon.Abstractions.Resilience.IBehaviorResilienceRuntimeCatalog>();
                return TypedResults.Ok(catalog?.Policies ?? []);
            })
            .WithName("GetCephalonBehaviorResilience");
        engineGroup.MapGet("/behavior-resilience/{policyId}", (string policyId, HttpContext httpContext) =>
            {
                var catalog = httpContext.RequestServices.GetService<Cephalon.Abstractions.Resilience.IBehaviorResilienceRuntimeCatalog>();
                var policy = catalog?.GetById(policyId);

                return policy is null ? Results.NotFound() : Results.Ok(policy);
            })
            .WithName("GetCephalonBehaviorResiliencePolicy");
        engineGroup.MapGet("/rate-limiting", ([FromServices] IRateLimitingRuntimeCatalog catalog) => TypedResults.Ok(catalog.Policies))
            .WithName("GetCephalonRateLimiting");
        engineGroup.MapGet("/rate-limiting/{policyId}", (string policyId, [FromServices] IRateLimitingRuntimeCatalog catalog) =>
            {
                var policy = catalog.GetById(policyId);

                return policy is null ? Results.NotFound() : Results.Ok(policy);
            })
            .WithName("GetCephalonRateLimitingPolicy");
        engineGroup.MapGet("/rest-endpoints", ([FromServices] IRestEndpointRuntimeCatalog catalog) => TypedResults.Ok(catalog.Endpoints))
            .WithName("GetCephalonRestEndpoints");
        engineGroup.MapGet("/rest-endpoints/{restEndpointId}", (string restEndpointId, [FromServices] IRestEndpointRuntimeCatalog catalog) =>
            {
                var endpoint = catalog.GetById(restEndpointId);

                return endpoint is null ? Results.NotFound() : Results.Ok(endpoint);
            })
            .WithName("GetCephalonRestEndpoint");
        engineGroup.MapGet("/rest-endpoint-candidates", ([FromServices] IRestEndpointCandidateRuntimeCatalog catalog) => TypedResults.Ok(catalog.Candidates))
            .WithName("GetCephalonRestEndpointCandidates");
        engineGroup.MapGet("/rest-endpoint-candidates/{candidateId}", (string candidateId, [FromServices] IRestEndpointCandidateRuntimeCatalog catalog) =>
            {
                var candidate = catalog.GetById(candidateId);

                return candidate is null ? Results.NotFound() : Results.Ok(candidate);
            })
            .WithName("GetCephalonRestEndpointCandidate");
        engineGroup.MapGet("/rest-endpoint-overrides", ([FromServices] IRestEndpointOverrideRuntimeCatalog catalog) => TypedResults.Ok(catalog.OverrideRules))
            .WithName("GetCephalonRestEndpointOverrides");
        engineGroup.MapGet("/rest-endpoint-overrides/{overrideId}", (string overrideId, [FromServices] IRestEndpointOverrideRuntimeCatalog catalog) =>
            {
                var restEndpointOverride = catalog.GetById(overrideId);

                return restEndpointOverride is null ? Results.NotFound() : Results.Ok(restEndpointOverride);
            })
            .WithName("GetCephalonRestEndpointOverride");
        engineGroup.MapGet("/rest-endpoint-suppressions", ([FromServices] IRestEndpointSuppressionRuntimeCatalog catalog) => TypedResults.Ok(catalog.Suppressions))
            .WithName("GetCephalonRestEndpointSuppressions");
        engineGroup.MapGet("/rest-endpoint-suppressions/{suppressionId}", (string suppressionId, [FromServices] IRestEndpointSuppressionRuntimeCatalog catalog) =>
            {
                var suppression = catalog.GetById(suppressionId);

                return suppression is null ? Results.NotFound() : Results.Ok(suppression);
            })
            .WithName("GetCephalonRestEndpointSuppression");
        engineGroup.MapGet("/databases", (RuntimeManifest manifest) => TypedResults.Ok(manifest.AppProfile.Databases))
            .WithName("GetCephalonDatabases");
        engineGroup.MapGet("/database-topology", ([FromServices] IDatabaseTopologyOperationalSnapshotProvider provider) =>
                TypedResults.Ok(provider.CreateSnapshot()))
            .WithName("GetCephalonDatabaseTopology");
        engineGroup.MapGet("/database-roles", ([FromServices] IDatabaseRoleCatalog catalog) => TypedResults.Ok(catalog.DatabaseRoles))
            .WithName("GetCephalonDatabaseRoles");
        engineGroup.MapGet("/database-roles/{databaseRoleId}", (string databaseRoleId, [FromServices] IDatabaseRoleCatalog catalog) =>
            {
                var databaseRole = catalog.GetById(databaseRoleId);

                return databaseRole is null ? Results.NotFound() : Results.Ok(databaseRole);
            })
            .WithName("GetCephalonDatabaseRole");
        engineGroup.MapGet("/database-migrations", ([FromServices] IDatabaseMigrationCatalog catalog) => TypedResults.Ok(catalog.DatabaseMigrations))
            .WithName("GetCephalonDatabaseMigrations");
        engineGroup.MapGet("/database-migrations/{databaseMigrationId}", (string databaseMigrationId, [FromServices] IDatabaseMigrationCatalog catalog) =>
            {
                var databaseMigration = catalog.GetById(databaseMigrationId);

                return databaseMigration is null ? Results.NotFound() : Results.Ok(databaseMigration);
            })
            .WithName("GetCephalonDatabaseMigration");
        engineGroup.MapGet("/database-migration-playbook", ([FromServices] IDatabaseMigrationOperationalPlaybookProvider provider) =>
                TypedResults.Ok(provider.CreatePlaybook()))
            .WithName("GetCephalonDatabaseMigrationPlaybook");
        engineGroup.MapGet("/scaffold", (RuntimeManifest manifest) =>
                manifest.AppProfile.Scaffold is null
                    ? Results.NotFound()
                    : Results.Ok(manifest.AppProfile.Scaffold))
            .WithName("GetCephalonScaffold");
        engineGroup.MapGet("/capabilities", (RuntimeManifest manifest) => TypedResults.Ok(manifest.Capabilities))
            .WithName("GetCephalonCapabilities");
        engineGroup.MapGet("/modules", (RuntimeManifest manifest) => TypedResults.Ok(manifest.Modules))
            .WithName("GetCephalonModules");
        engineGroup.MapGet("/packages", (RuntimeManifest manifest) => TypedResults.Ok(manifest.Packages))
            .WithName("GetCephalonPackages");
        engineGroup.MapGet("/hosted-executions", ([FromServices] IHostedExecutionRuntimeCatalog catalog) => TypedResults.Ok(catalog.HostedExecutions))
            .WithName("GetCephalonHostedExecutions");
        engineGroup.MapGet("/hosted-executions/{hostedExecutionId}", (string hostedExecutionId, [FromServices] IHostedExecutionRuntimeCatalog catalog) =>
            {
                var hostedExecution = catalog.GetById(hostedExecutionId);

                return hostedExecution is null ? Results.NotFound() : Results.Ok(hostedExecution);
            })
            .WithName("GetCephalonHostedExecution");
        engineGroup.MapGet("/execution-graphs", ([FromServices] IExecutionRuntimeCatalog catalog) => TypedResults.Ok(catalog.Graphs))
            .WithName("GetCephalonExecutionGraphs");
        engineGroup.MapGet("/execution-graphs/{graphId}", (string graphId, [FromServices] IExecutionRuntimeCatalog catalog) =>
            {
                var graph = catalog.GetById(graphId);

                return graph is null ? Results.NotFound() : Results.Ok(graph);
            })
            .WithName("GetCephalonExecutionGraph");
        engineGroup.MapGet("/projections", ([FromServices] IProjectionCatalog catalog) => TypedResults.Ok(catalog.Projections))
            .WithName("GetCephalonProjections");
        engineGroup.MapGet("/projections/{projectionId}", (string projectionId, [FromServices] IProjectionCatalog catalog) =>
            {
                var projection = catalog.GetById(projectionId);

                return projection is null ? Results.NotFound() : Results.Ok(projection);
            })
            .WithName("GetCephalonProjection");
        engineGroup.MapGet("/outboxes", ([FromServices] IOutboxCatalog catalog) => TypedResults.Ok(catalog.Outboxes))
            .WithName("GetCephalonOutboxes");
        engineGroup.MapGet("/outboxes/{outboxId}", (string outboxId, [FromServices] IOutboxCatalog catalog) =>
            {
                var outbox = catalog.GetById(outboxId);

                return outbox is null ? Results.NotFound() : Results.Ok(outbox);
            })
            .WithName("GetCephalonOutbox");
        engineGroup.MapGet("/event-dispatch-runtimes", (HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<IEventDispatchRuntimeDescriptorCatalog>()?
                    .Runtimes ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonEventDispatchRuntimes");
        engineGroup.MapGet("/event-dispatch-runtimes/{dispatchRuntimeId}", (string dispatchRuntimeId, HttpContext httpContext) =>
            {
                var runtimeDescriptor = httpContext.RequestServices
                    .GetService<IEventDispatchRuntimeDescriptorCatalog>()?
                    .GetById(dispatchRuntimeId);

                return runtimeDescriptor is null ? Results.NotFound() : Results.Ok(runtimeDescriptor);
            })
            .WithName("GetCephalonEventDispatchRuntime");
        engineGroup.MapGet("/event-dispatches", (HttpContext httpContext) =>
            {
                var states = httpContext.RequestServices
                    .GetService<IEventDispatchRuntimeCatalog>()?
                    .States ?? [];

                return Results.Ok(states);
            })
            .WithName("GetCephalonEventDispatches");
        engineGroup.MapGet("/event-dispatches/{outboxId}", (string outboxId, HttpContext httpContext) =>
            {
                var state = httpContext.RequestServices
                    .GetService<IEventDispatchRuntimeCatalog>()?
                    .GetByOutboxId(outboxId);

                return state is null ? Results.NotFound() : Results.Ok(state);
            })
            .WithName("GetCephalonEventDispatch");
        engineGroup.MapGet("/inboxes", ([FromServices] IInboxCatalog catalog) => TypedResults.Ok(catalog.Inboxes))
            .WithName("GetCephalonInboxes");
        engineGroup.MapGet("/inboxes/{inboxId}", (string inboxId, [FromServices] IInboxCatalog catalog) =>
            {
                var inbox = catalog.GetById(inboxId);

                return inbox is null ? Results.NotFound() : Results.Ok(inbox);
            })
            .WithName("GetCephalonInbox");
        engineGroup.MapGet("/audit-stores", ([FromServices] IAuditStoreCatalog catalog) => TypedResults.Ok(catalog.AuditStores))
            .WithName("GetCephalonAuditStores");
        engineGroup.MapGet("/audit-stores/{auditStoreId}", (string auditStoreId, [FromServices] IAuditStoreCatalog catalog) =>
            {
                var auditStore = catalog.GetById(auditStoreId);

                return auditStore is null ? Results.NotFound() : Results.Ok(auditStore);
            })
            .WithName("GetCephalonAuditStore");
        engineGroup.MapGet("/audit-history", async (
                string? category,
                string? action,
                string? subjectType,
                string? subjectId,
                string? actorId,
                string? tenantId,
                string? correlationId,
                string? outcome,
                DateTimeOffset? occurredFromUtc,
                DateTimeOffset? occurredToUtc,
                int? offset,
                int? limit,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var reader = httpContext.RequestServices.GetService<IAuditHistoryReader>();
                if (reader is null)
                {
                    return Results.NotFound();
                }

                if (!TryParseAuditOutcome(outcome, out var parsedOutcome))
                {
                    return Results.BadRequest($"Audit outcome '{outcome}' is not supported.");
                }

                var query = new AuditHistoryQuery(
                    category: category,
                    action: action,
                    subjectType: subjectType,
                    subjectId: subjectId,
                    actorId: actorId,
                    tenantId: tenantId,
                    correlationId: correlationId,
                    outcome: parsedOutcome,
                    occurredFromUtc: occurredFromUtc,
                    occurredToUtc: occurredToUtc,
                    offset: offset ?? 0,
                    limit: limit ?? AuditHistoryQuery.DefaultLimit);

                return Results.Ok(await reader.QueryAsync(query, cancellationToken).ConfigureAwait(false));
            })
            .WithName("GetCephalonAuditHistory");
        engineGroup.MapGet("/audit-history/{auditEntryId}", async (
                string auditEntryId,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var reader = httpContext.RequestServices.GetService<IAuditHistoryReader>();
                if (reader is null)
                {
                    return Results.NotFound();
                }

                var entry = await reader.GetByIdAsync(auditEntryId, cancellationToken).ConfigureAwait(false);
                return entry is null ? Results.NotFound() : Results.Ok(entry);
            })
            .WithName("GetCephalonAuditHistoryEntry");
        if (runtime.Manifest.AppProfile.Audit.History.Export.Enabled == true)
        {
            engineGroup.MapGet("/audit-history/export", async (
                    string? category,
                    string? action,
                    string? subjectType,
                    string? subjectId,
                    string? actorId,
                    string? tenantId,
                    string? correlationId,
                    string? outcome,
                    DateTimeOffset? occurredFromUtc,
                    DateTimeOffset? occurredToUtc,
                    int? maxEntries,
                    HttpContext httpContext,
                    CancellationToken cancellationToken) =>
                {
                    var exporter = httpContext.RequestServices.GetService<IAuditHistoryExporter>();
                    if (exporter is null)
                    {
                        return Results.NotFound();
                    }

                    if (!TryParseAuditOutcome(outcome, out var parsedOutcome))
                    {
                        return Results.BadRequest($"Audit outcome '{outcome}' is not supported.");
                    }

                    var exportLimit = ResolveAuditHistoryExportMaxEntries(runtime.Manifest.AppProfile, maxEntries);
                    var request = new AuditHistoryExportRequest(
                        category: category,
                        action: action,
                        subjectType: subjectType,
                        subjectId: subjectId,
                        actorId: actorId,
                        tenantId: tenantId,
                        correlationId: correlationId,
                        outcome: parsedOutcome,
                        occurredFromUtc: occurredFromUtc,
                        occurredToUtc: occurredToUtc,
                        maxEntries: exportLimit);

                    await httpContext.Response.WriteAuditHistoryNdjsonAsync(
                        exporter,
                        request,
                        fileName: "cephalon-audit-history.ndjson",
                        cancellationToken: cancellationToken).ConfigureAwait(false);

                    return Results.Empty;
                })
                .WithName("ExportCephalonAuditHistory");
        }
        engineGroup.MapGet("/authorization-policies", ([FromServices] IAuthorizationPolicyCatalog catalog) => TypedResults.Ok(catalog.Policies))
            .WithName("GetCephalonAuthorizationPolicies");
        engineGroup.MapGet("/authorization-policies/{policyId}", (string policyId, [FromServices] IAuthorizationPolicyCatalog catalog) =>
            {
                var policy = catalog.GetById(policyId);

                return policy is null ? Results.NotFound() : Results.Ok(policy);
            })
            .WithName("GetCephalonAuthorizationPolicy");
        engineGroup.MapGet("/strangler-fig", ([FromServices] IStranglerFigRuntimeCatalog catalog) => TypedResults.Ok(catalog.Routes))
            .WithName("GetCephalonStranglerFigRoutes");
        engineGroup.MapGet("/strangler-fig/resolve", async (
                string path,
                string? method,
                [FromServices] IStranglerFigRouter router,
                CancellationToken cancellationToken) =>
            {
                var resolution = await router.ResolveAsync(
                    new StranglerFigRequest(path, method ?? HttpMethods.Get),
                    cancellationToken).ConfigureAwait(false);

                return resolution is null
                    ? Results.NotFound()
                    : Results.Ok(resolution);
            })
            .WithName("ResolveCephalonStranglerFigRoute");
        engineGroup.MapGet("/strangler-fig/{routeId}", (string routeId, [FromServices] IStranglerFigRuntimeCatalog catalog) =>
            {
                var route = catalog.GetById(routeId);

                return route is null ? Results.NotFound() : Results.Ok(route);
            })
            .WithName("GetCephalonStranglerFigRoute");
        engineGroup.MapGet("/patterns", (RuntimeManifest manifest) => TypedResults.Ok(manifest.AppProfile.Patterns))
            .WithName("GetCephalonPatterns");
        engineGroup.MapGet("/technologies", (RuntimeManifest manifest) => TypedResults.Ok(manifest.AppProfile.Technologies))
            .WithName("GetCephalonTechnologies");
        engineGroup.MapGet("/technology-catalog", ([FromServices] TechnologyCatalogSnapshot catalog) => TypedResults.Ok(catalog.Technologies))
            .WithName("GetCephalonTechnologyCatalog");
        engineGroup.MapGet("/technology-surfaces", ([FromServices] ITechnologyRuntimeCatalog catalog) =>
                TypedResults.Ok(catalog.Surfaces))
            .WithName("GetCephalonTechnologySurfaces");
        engineGroup.MapGet("/technology-surfaces/{technologyId}", (string technologyId, [FromServices] ITechnologyRuntimeCatalog catalog) =>
                TypedResults.Ok(catalog.GetByTechnology(technologyId)))
            .WithName("GetCephalonTechnologySurface");
        engineGroup.MapGet("/transports", (RuntimeManifest manifest) => TypedResults.Ok(manifest.AppProfile.Transports))
            .WithName("GetCephalonTransports");
        engineGroup.MapGet("/dependencies", ([FromServices] RuntimeHealthEvaluator health) => TypedResults.Ok(health.EvaluateDependencies()))
            .WithName("GetCephalonDependencies");
        engineGroup.MapGet("/localization", (string? culture, [FromServices] ILocalizedTextCatalog catalog) =>
                TypedResults.Ok(catalog.CreateSnapshot(culture)))
            .WithName("GetCephalonLocalization");
        engineGroup.MapGet("/reference-docs", () => TypedResults.Ok(referenceDocsSurface))
            .WithName("GetCephalonReferenceDocs");
        engineGroup.MapGet("/options", ([FromServices] EngineOptions options) => TypedResults.Ok(options))
            .WithName("GetCephalonOptions");
        engineGroup.MapGet("/package-policy", ([FromServices] PackagePolicy packagePolicy) => TypedResults.Ok(packagePolicy))
            .WithName("GetCephalonPackagePolicy");
        engineGroup.MapGet("/failure-policy", ([FromServices] FailurePolicy failurePolicy) => TypedResults.Ok(failurePolicy))
            .WithName("GetCephalonFailurePolicy");
        engineGroup.MapGet("/trust-policy", ([FromServices] CapabilityPolicyEvaluator evaluator) => TypedResults.Ok(evaluator.Snapshot))
            .WithName("GetCephalonTrustPolicy");
        engineGroup.MapGet("/status", ([FromServices] IRuntime runtime) => TypedResults.Ok(runtime.StatusSnapshot))
            .WithName("GetCephalonStatus");
        engineGroup.MapGet("/runtime-story", ([FromServices] IRuntime runtime) => TypedResults.Ok(runtime.OperationalStory))
            .WithName("GetCephalonRuntimeStory");
        engineGroup.MapGet("/diagnostics", ([FromServices] RuntimeHealthEvaluator health, [FromServices] IRuntimeDiagnosticsCatalog diagnosticsCatalog) => TypedResults.Ok(new DiagnosticsSurface(
                MeterName: EngineDiagnostics.MeterName,
                ActivitySourceName: EngineDiagnostics.ActivitySourceName,
                Counters:
                [
                    EngineDiagnostics.EngineBuildCounterName,
                    EngineDiagnostics.RuntimeTransitionCounterName,
                    EngineDiagnostics.ModuleTransitionCounterName,
                    EngineDiagnostics.ExecutionGraphTransitionCounterName,
                    EngineDiagnostics.HostedExecutionTransitionCounterName,
                    EngineDiagnostics.RuntimeFailureCounterName,
                    EngineDiagnostics.ModuleFailureCounterName,
                    EngineDiagnostics.RuntimeRestartCounterName
                ],
                Conventions: diagnosticsCatalog.Conventions,
                Liveness: health.EvaluateLiveness(),
                Readiness: health.EvaluateReadiness(),
                SummaryPath: "/health",
                LivenessPath: "/health/live",
                ReadinessPath: "/health/ready")))
            .WithName("GetCephalonDiagnostics");
        engineGroup.MapGet("/modules/{moduleId}", (string moduleId, RuntimeManifest manifest) =>
            {
                var module = manifest.Modules.FirstOrDefault(item =>
                    string.Equals(item.Id, moduleId, StringComparison.OrdinalIgnoreCase));

                return module is null ? Results.NotFound() : Results.Ok(module);
            })
            .WithName("GetCephalonModule");

        app.MapHealthChecks("/health", CreateHealthCheckOptions(static _ => true))
            .WithDisplayName("Cephalon Health")
            .DisableRateLimiting()
            .ExcludeFromDescription();
        app.MapHealthChecks("/health/live", CreateHealthCheckOptions(static registration =>
                registration.Tags.Any(tag => string.Equals(tag, "live", StringComparison.OrdinalIgnoreCase))))
            .WithDisplayName("Cephalon Liveness")
            .DisableRateLimiting()
            .ExcludeFromDescription();
        app.MapHealthChecks("/health/ready", CreateHealthCheckOptions(static registration =>
                registration.Tags.Any(tag => string.Equals(tag, "ready", StringComparison.OrdinalIgnoreCase))))
            .WithDisplayName("Cephalon Readiness")
            .DisableRateLimiting()
            .ExcludeFromDescription();

        if (app.Services.GetService<IRateLimitingRuntimeCatalog>()?.Policies.Any(static policy =>
                string.Equals(policy.ExecutionMode, AspNetCoreRateLimitingPolicyResolver.EnabledExecutionMode, StringComparison.OrdinalIgnoreCase)) == true)
        {
            app.UseRateLimiter();
        }

        var mappers = app.Services.GetServices<ITransportRouteMapper>()
            .GroupBy(mapper => mapper.TransportId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.OrdinalIgnoreCase);

        var duplicateMapper = mappers.FirstOrDefault(pair => pair.Value.Length > 1);
        if (!string.IsNullOrWhiteSpace(duplicateMapper.Key))
        {
            throw new InvalidOperationException(
                $"Transport '{duplicateMapper.Key}' has multiple route mappers registered.");
        }

        foreach (var transport in runtime.Manifest.AppProfile.Transports)
        {
            if (!mappers.TryGetValue(transport.Id, out var mapper))
            {
                throw new InvalidOperationException(
                    $"Transport '{transport.Id}' was selected, but no ASP.NET Core route mapper was registered for it.");
            }

            mapper[0].MapRoutes(app, runtime);
        }

        if (restApiSelected)
        {
            // Keep the docs shell and bundled Scalar assets fresh across package upgrades.
            app.UseWhen(
                context => context.Request.Path.StartsWithSegments(openApiEndpointOptions.ScalarRoutePrefix, StringComparison.OrdinalIgnoreCase),
                branch => branch.Use(async (context, next) =>
                {
                    context.Response.OnStarting(() =>
                    {
                        context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
                        context.Response.Headers["Pragma"] = "no-cache";
                        return Task.CompletedTask;
                    });

                    await next();
                }));

            app.MapOpenApi(openApiEndpointOptions.RoutePattern)
                .DisableRateLimiting();
            app.MapGet(
                    openApiToggleScriptRoute,
                    () => Results.Text(
                        RenderOpenApiToggleScript(
                            openApiEndpointOptions.ScalarRoutePrefix,
                            openApiDocumentNames,
                            defaultOpenApiDocumentName),
                        "application/javascript"))
                .DisableRateLimiting()
                .ExcludeFromDescription();
            app.MapGet(scalarFaviconRoute, () => Results.Text(ScalarFavicon.Value, "image/svg+xml"))
                .DisableRateLimiting()
                .ExcludeFromDescription();
            app.MapGet("/favicon.ico", () => Results.Redirect(scalarFaviconReference))
                .DisableRateLimiting()
                .ExcludeFromDescription();
            app.UseWhen(
                context =>
                    (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method)) &&
                    context.Request.Path == openApiEndpointOptions.ScalarRoutePrefix,
                branch => branch.Run(context =>
                {
                    context.Response.Redirect(
                        BuildScalarCanonicalPath(
                            openApiEndpointOptions.ScalarRoutePrefix,
                            defaultOpenApiDocumentName,
                            context.Request.QueryString));
                    return Task.CompletedTask;
                }));
            app.MapScalarApiReference(openApiEndpointOptions.ScalarRoutePrefix, (options, httpContext) =>
            {
                var title = ResolveRestDocsText(
                    httpContext.RequestServices,
                    configurationKey: "OpenApi:Title",
                    localizationKey: "engine.docs.scalar.title",
                    fallbackValue: "Cephalon REST API");

                options.WithTitle(title);
                options.WithFavicon(scalarFaviconReference);
                options.WithJavaScriptConfiguration(openApiToggleScriptReference);
                options.WithOpenApiRoutePattern(openApiEndpointOptions.RoutePattern);

                foreach (var documentName in openApiDocumentNames)
                {
                    options.AddDocument(
                        documentName,
                        isDefault: string.Equals(
                            documentName,
                            defaultOpenApiDocumentName,
                            StringComparison.OrdinalIgnoreCase));
                }
            })
            .DisableRateLimiting();
        }

        MapReferenceDocs(app, referenceDocsOptions, referenceDocsSurface);

        return app;
    }

    private static RequestLocalizationOptions BuildRequestLocalizationOptions(
        LocalizationSettings settings,
        ILocalizedTextCatalog localizedTextCatalog)
    {
        var supportedCultures = localizedTextCatalog.SupportedCultures
            .Select(CultureInfo.GetCultureInfo)
            .ToList();

        var defaultCulture = string.IsNullOrWhiteSpace(settings.DefaultCulture)
            ? localizedTextCatalog.DefaultCulture
            : settings.DefaultCulture;

        return new RequestLocalizationOptions
        {
            DefaultRequestCulture = new RequestCulture(defaultCulture!),
            SupportedCultures = supportedCultures,
            SupportedUICultures = supportedCultures
        };
    }

    private static string ResolveRestDocsText(
        IServiceProvider services,
        string configurationKey,
        string localizationKey,
        string fallbackValue)
    {
        var configuration = services.GetRequiredService<IConfiguration>();
        var localizedTextCatalog = services.GetRequiredService<ILocalizedTextCatalog>();
        var configuredValue = configuration[configurationKey];

        if (!string.IsNullOrWhiteSpace(configuredValue))
        {
            return configuredValue.Trim();
        }

        return localizedTextCatalog.ResolveText(localizationKey, CultureInfo.CurrentUICulture.Name, fallbackValue);
    }

    private static void MapReferenceDocs(
        WebApplication app,
        ReferenceDocsHostingOptions options,
        ReferenceDocsSurface surface)
    {
        if (!surface.Enabled)
        {
            return;
        }

        ValidateReferenceDocsOptions(options, surface);

        app.MapGet(surface.RoutePrefix, () => Results.Redirect(surface.DefaultDocumentPath))
            .DisableRateLimiting()
            .ExcludeFromDescription();
        app.MapGet($"{surface.RoutePrefix}/", () => Results.Redirect(surface.DefaultDocumentPath))
            .DisableRateLimiting()
            .ExcludeFromDescription();
        app.MapGet($"{surface.RoutePrefix}/{{**filePath}}", (string? filePath) =>
                ServeReferenceDocsFile(options, filePath))
            .DisableRateLimiting()
            .ExcludeFromDescription();
    }

    private static void ValidateReferenceDocsOptions(
        ReferenceDocsHostingOptions options,
        ReferenceDocsSurface surface)
    {
        if (string.IsNullOrWhiteSpace(options.DirectoryPath))
        {
            throw new InvalidOperationException(
                "Reference docs hosting is enabled, but no documentation directory was configured.");
        }

        if (!Directory.Exists(options.DirectoryPath))
        {
            throw new InvalidOperationException(
                $"Reference docs directory '{options.DirectoryPath}' was not found.");
        }

        if (!File.Exists(Path.Combine(options.DirectoryPath, options.DefaultDocument)))
        {
            throw new InvalidOperationException(
                $"Reference docs default document '{surface.DefaultDocument}' was not found under '{options.DirectoryPath}'.");
        }
    }

    private static IResult ServeReferenceDocsFile(ReferenceDocsHostingOptions options, string? filePath)
    {
        if (string.IsNullOrWhiteSpace(options.DirectoryPath))
        {
            return Results.NotFound();
        }

        var relativePath = string.IsNullOrWhiteSpace(filePath)
            ? options.DefaultDocument
            : filePath.Trim().TrimStart('/');
        var fullPath = ResolveReferenceDocsFilePath(options.DirectoryPath, relativePath);
        if (fullPath is null || !File.Exists(fullPath))
        {
            return Results.NotFound();
        }

        var provider = new FileExtensionContentTypeProvider();
        provider.Mappings[".md"] = "text/markdown";
        if (!provider.TryGetContentType(fullPath, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        return TypedResults.PhysicalFile(fullPath, contentType);
    }

    private static string? ResolveReferenceDocsFilePath(string rootDirectoryPath, string relativePath)
    {
        var normalizedRoot = Path.GetFullPath(rootDirectoryPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var normalizedRelativePath = relativePath
            .Replace('/', Path.DirectorySeparatorChar)
            .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var candidate = Path.GetFullPath(Path.Combine(normalizedRoot, normalizedRelativePath));

        if (string.Equals(candidate, normalizedRoot, StringComparison.OrdinalIgnoreCase))
        {
            return candidate;
        }

        return candidate.StartsWith(
            normalizedRoot + Path.DirectorySeparatorChar,
            StringComparison.OrdinalIgnoreCase)
            ? candidate
            : null;
    }

    private static ReferenceDocsSurface CreateReferenceDocsSurface(ReferenceDocsHostingOptions options)
    {
        var routePrefix = NormalizeReferenceDocsRoutePrefix(options.RoutePrefix, strict: options.Enabled);
        var defaultDocument = string.IsNullOrWhiteSpace(options.DefaultDocument)
            ? "browse.html"
            : options.DefaultDocument.Trim();
        var available = !string.IsNullOrWhiteSpace(options.DirectoryPath) &&
            Directory.Exists(options.DirectoryPath) &&
            File.Exists(Path.Combine(options.DirectoryPath, defaultDocument));

        return new ReferenceDocsSurface(
            Enabled: options.Enabled,
            Available: available,
            RoutePrefix: routePrefix,
            DefaultDocument: defaultDocument,
            DefaultDocumentPath: $"{routePrefix}/{defaultDocument}",
            ReadmePath: $"{routePrefix}/README.md",
            BrowserPath: $"{routePrefix}/browse.html",
            NamespaceIndexPath: $"{routePrefix}/namespaces.md",
            TypeIndexPath: $"{routePrefix}/types.md",
            MemberIndexPath: $"{routePrefix}/members.md",
            ManifestPath: $"{routePrefix}/reference-manifest.json");
    }

    private static string NormalizeReferenceDocsRoutePrefix(string? routePrefix, bool strict)
    {
        var normalized = string.IsNullOrWhiteSpace(routePrefix)
            ? "/reference"
            : routePrefix.Trim();
        if (!normalized.StartsWith('/'))
        {
            normalized = "/" + normalized;
        }

        normalized = normalized.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(normalized) || string.Equals(normalized, "/", StringComparison.Ordinal))
        {
            if (!strict)
            {
                return "/reference";
            }

            throw new InvalidOperationException(
                "Reference docs route prefix must resolve to a non-root path such as '/reference'.");
        }

        return normalized;
    }

    private static string BuildVersionedAssetReference(string route)
    {
        return $"{route}?v={DocumentationAssetVersion}";
    }

    private static bool TryParseAuditOutcome(
        string? value,
        out AuditOutcome? outcome)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            outcome = null;
            return true;
        }

        if (Enum.TryParse<AuditOutcome>(value.Trim(), ignoreCase: true, out var parsed))
        {
            outcome = parsed;
            return true;
        }

        outcome = null;
        return false;
    }

    private static int ResolveAuditHistoryExportMaxEntries(
        AppProfile appProfile,
        int? requestedMaxEntries)
    {
        ArgumentNullException.ThrowIfNull(appProfile);

        var configuredMaxEntries = appProfile.Audit.History.Export.MaxEntries
            ?? AuditHistoryExportSettings.DefaultMaxEntries;
        if (requestedMaxEntries is not > 0)
        {
            return configuredMaxEntries;
        }

        return Math.Min(requestedMaxEntries.Value, configuredMaxEntries);
    }

    private static string BuildScalarCanonicalPath(string scalarRoutePrefix, string documentName, QueryString queryString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scalarRoutePrefix);
        ArgumentException.ThrowIfNullOrWhiteSpace(documentName);
        var query = queryString.HasValue ? queryString.Value : string.Empty;
        return $"{scalarRoutePrefix}/{documentName}{query}";
    }

    private static string BuildScalarAssetRoute(string scalarRoutePrefix, string relativeAssetPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scalarRoutePrefix);
        ArgumentException.ThrowIfNullOrWhiteSpace(relativeAssetPath);

        var normalizedAssetPath = relativeAssetPath.TrimStart('/');
        return $"{scalarRoutePrefix}/{normalizedAssetPath}";
    }

    private static string RenderOpenApiToggleScript(
        string scalarRoutePrefix,
        IReadOnlyList<string> documentNames,
        string defaultDocumentName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scalarRoutePrefix);
        ArgumentNullException.ThrowIfNull(documentNames);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultDocumentName);

        var rendered = OpenApiToggleScriptTemplate.Value.Replace(
            ScalarRoutePrefixToken,
            scalarRoutePrefix,
            StringComparison.Ordinal);
        rendered = rendered.Replace(
            ScalarDocumentNamesToken,
            JsonSerializer.Serialize(documentNames),
            StringComparison.Ordinal);
        rendered = rendered.Replace(
            ScalarDefaultDocumentNameToken,
            JsonSerializer.Serialize(defaultDocumentName),
            StringComparison.Ordinal);

        return rendered;
    }

    private static HealthCheckOptions CreateHealthCheckOptions(Func<HealthCheckRegistration, bool> predicate)
    {
        return new HealthCheckOptions
        {
            Predicate = predicate,
            ResponseWriter = HealthResponseWriter.WriteAsync
        };
    }

    private static string LoadEmbeddedAsset(string resourceName, string assetDescription)
    {
        using var stream = typeof(EngineWebApplicationExtensions).Assembly
            .GetManifestResourceStream(resourceName);

        if (stream is null)
        {
            throw new InvalidOperationException(
                $"Embedded {assetDescription} '{resourceName}' was not found.");
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
