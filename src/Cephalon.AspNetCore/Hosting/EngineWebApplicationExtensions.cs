using Cephalon.AspNetCore.Documentation;
using Cephalon.AspNetCore.Diagnostics;
using Cephalon.AspNetCore.Health;
using Cephalon.Abstractions.AppModel;
using Cephalon.Abstractions.Audit;
using Cephalon.Abstractions.Authorization;
using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Execution;
using Cephalon.Abstractions.Features;
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
        engineGroup.MapGet("/saga-choreographies", (HttpContext httpContext) =>
            {
                var catalog = httpContext.RequestServices.GetService<ISagaChoreographyRuntimeCatalog>();
                return TypedResults.Ok(catalog?.SagaChoreographies ?? []);
            })
            .WithName("GetCephalonSagaChoreographies");
        engineGroup.MapGet("/saga-choreographies/modules/{moduleId}", (string moduleId, HttpContext httpContext) =>
            {
                var catalog = httpContext.RequestServices.GetService<ISagaChoreographyRuntimeCatalog>();
                return TypedResults.Ok(catalog?.GetBySourceModule(moduleId) ?? []);
            })
            .WithName("GetCephalonSagaChoreographiesByModule");
        engineGroup.MapGet("/saga-choreographies/transports/{transportId}", (string transportId, HttpContext httpContext) =>
            {
                var catalog = httpContext.RequestServices.GetService<ISagaChoreographyRuntimeCatalog>();
                return TypedResults.Ok(catalog?.GetByTransportId(transportId) ?? []);
            })
            .WithName("GetCephalonSagaChoreographiesByTransport");
        engineGroup.MapGet("/saga-choreographies/{behaviorId}", (string behaviorId, HttpContext httpContext) =>
            {
                var catalog = httpContext.RequestServices.GetService<ISagaChoreographyRuntimeCatalog>();
                var choreography = catalog?.GetById(behaviorId);

                return choreography is null ? Results.NotFound() : Results.Ok(choreography);
            })
            .WithName("GetCephalonSagaChoreography");
        engineGroup.MapGet("/saga-choreographies/runtime", (HttpContext httpContext) =>
            {
                var catalog = httpContext.RequestServices.GetService<ISagaChoreographyPublicationRuntimeStateCatalog>();
                return TypedResults.Ok(catalog?.States ?? []);
            })
            .WithName("GetCephalonSagaChoreographyPublicationStates");
        engineGroup.MapGet("/saga-choreographies/runtime/behaviors/{behaviorId}", (string behaviorId, HttpContext httpContext) =>
            {
                var catalog = httpContext.RequestServices.GetService<ISagaChoreographyPublicationRuntimeStateCatalog>();
                return TypedResults.Ok(catalog?.GetByBehaviorId(behaviorId) ?? []);
            })
            .WithName("GetCephalonSagaChoreographyPublicationStatesByBehavior");
        engineGroup.MapGet("/saga-choreographies/runtime/modules/{moduleId}", (string moduleId, HttpContext httpContext) =>
            {
                var catalog = httpContext.RequestServices.GetService<ISagaChoreographyPublicationRuntimeStateCatalog>();
                return TypedResults.Ok(catalog?.GetBySourceModule(moduleId) ?? []);
            })
            .WithName("GetCephalonSagaChoreographyPublicationStatesByModule");
        engineGroup.MapGet("/saga-choreographies/runtime/transports/{transportId}", (string transportId, HttpContext httpContext) =>
            {
                var catalog = httpContext.RequestServices.GetService<ISagaChoreographyPublicationRuntimeStateCatalog>();
                return TypedResults.Ok(catalog?.GetByTransportId(transportId) ?? []);
            })
            .WithName("GetCephalonSagaChoreographyPublicationStatesByTransport");
        engineGroup.MapGet("/saga-choreographies/runtime/channels/{channelId}", (string channelId, HttpContext httpContext) =>
            {
                var catalog = httpContext.RequestServices.GetService<ISagaChoreographyPublicationRuntimeStateCatalog>();
                return TypedResults.Ok(catalog?.GetByChannelId(channelId) ?? []);
            })
            .WithName("GetCephalonSagaChoreographyPublicationStatesByChannel");
        engineGroup.MapGet("/saga-choreographies/runtime/correlations/{correlationId}", (string correlationId, HttpContext httpContext) =>
            {
                var catalog = httpContext.RequestServices.GetService<ISagaChoreographyPublicationRuntimeStateCatalog>();
                return TypedResults.Ok(catalog?.GetByCorrelationId(correlationId) ?? []);
            })
            .WithName("GetCephalonSagaChoreographyPublicationStatesByCorrelation");
        engineGroup.MapGet("/saga-choreographies/runtime/compensations", (HttpContext httpContext) =>
            {
                var catalog = httpContext.RequestServices.GetService<ISagaChoreographyPublicationRuntimeStateCatalog>();
                return TypedResults.Ok(catalog?.GetCompensationPublications() ?? []);
            })
            .WithName("GetCephalonSagaChoreographyCompensationPublicationStates");
        engineGroup.MapGet("/saga-choreographies/runtime/failures", (HttpContext httpContext) =>
            {
                var catalog = httpContext.RequestServices.GetService<ISagaChoreographyPublicationRuntimeStateCatalog>();
                return TypedResults.Ok(catalog?.GetFailedPublications() ?? []);
            })
            .WithName("GetCephalonSagaChoreographyFailedPublicationStates");
        engineGroup.MapGet("/saga-choreographies/runtime/publications/{publicationStateId}", (string publicationStateId, HttpContext httpContext) =>
            {
                var catalog = httpContext.RequestServices.GetService<ISagaChoreographyPublicationRuntimeStateCatalog>();
                var state = catalog?.GetById(publicationStateId);

                return state is null ? Results.NotFound() : Results.Ok(state);
            })
            .WithName("GetCephalonSagaChoreographyPublicationState");
        engineGroup.MapGet("/durable-executions", (HttpContext httpContext) =>
            {
                var catalog = httpContext.RequestServices.GetService<IDurableExecutionRuntimeCatalog>();
                return TypedResults.Ok(catalog?.DurableExecutions ?? []);
            })
            .WithName("GetCephalonDurableExecutions");
        engineGroup.MapGet("/durable-executions/modules/{moduleId}", (string moduleId, HttpContext httpContext) =>
            {
                var catalog = httpContext.RequestServices.GetService<IDurableExecutionRuntimeCatalog>();
                return TypedResults.Ok(catalog?.GetBySourceModule(moduleId) ?? []);
            })
            .WithName("GetCephalonDurableExecutionsByModule");
        engineGroup.MapGet("/durable-executions/transports/{transportId}", (string transportId, HttpContext httpContext) =>
            {
                var catalog = httpContext.RequestServices.GetService<IDurableExecutionRuntimeCatalog>();
                return TypedResults.Ok(catalog?.GetByTransportId(transportId) ?? []);
            })
            .WithName("GetCephalonDurableExecutionsByTransport");
        engineGroup.MapGet("/durable-executions/{behaviorId}", (string behaviorId, HttpContext httpContext) =>
            {
                var catalog = httpContext.RequestServices.GetService<IDurableExecutionRuntimeCatalog>();
                var durableExecution = catalog?.GetById(behaviorId);

                return durableExecution is null ? Results.NotFound() : Results.Ok(durableExecution);
            })
            .WithName("GetCephalonDurableExecution");
        engineGroup.MapGet("/durable-executions/runtime", (HttpContext httpContext) =>
            {
                var catalog = httpContext.RequestServices.GetService<IDurableExecutionRuntimeStateCatalog>();
                return TypedResults.Ok(catalog?.States ?? []);
            })
            .WithName("GetCephalonDurableExecutionStates");
        engineGroup.MapGet("/durable-executions/runtime/behaviors/{behaviorId}", (string behaviorId, HttpContext httpContext) =>
            {
                var catalog = httpContext.RequestServices.GetService<IDurableExecutionRuntimeStateCatalog>();
                return TypedResults.Ok(catalog?.GetByBehaviorId(behaviorId) ?? []);
            })
            .WithName("GetCephalonDurableExecutionStatesByBehavior");
        engineGroup.MapGet("/durable-executions/runtime/modules/{moduleId}", (string moduleId, HttpContext httpContext) =>
            {
                var catalog = httpContext.RequestServices.GetService<IDurableExecutionRuntimeStateCatalog>();
                return TypedResults.Ok(catalog?.GetBySourceModule(moduleId) ?? []);
            })
            .WithName("GetCephalonDurableExecutionStatesByModule");
        engineGroup.MapGet("/durable-executions/runtime/transports/{transportId}", (string transportId, HttpContext httpContext) =>
            {
                var catalog = httpContext.RequestServices.GetService<IDurableExecutionRuntimeStateCatalog>();
                return TypedResults.Ok(catalog?.GetByTransportId(transportId) ?? []);
            })
            .WithName("GetCephalonDurableExecutionStatesByTransport");
        engineGroup.MapGet("/durable-executions/runtime/timers", (HttpContext httpContext) =>
            {
                var catalog = httpContext.RequestServices.GetService<IDurableExecutionRuntimeStateCatalog>();
                return TypedResults.Ok(catalog?.GetWithPendingTimers() ?? []);
            })
            .WithName("GetCephalonDurableExecutionStatesWithPendingTimers");
        engineGroup.MapGet("/durable-executions/runtime/timers/{timerId}", (string timerId, HttpContext httpContext) =>
            {
                var catalog = httpContext.RequestServices.GetService<IDurableExecutionRuntimeStateCatalog>();
                return TypedResults.Ok(catalog?.GetByPendingTimerId(timerId) ?? []);
            })
            .WithName("GetCephalonDurableExecutionStatesByPendingTimer");
        engineGroup.MapGet("/durable-executions/runtime/signals", (HttpContext httpContext) =>
            {
                var catalog = httpContext.RequestServices.GetService<IDurableExecutionRuntimeStateCatalog>();
                return TypedResults.Ok(catalog?.GetWithPendingSignals() ?? []);
            })
            .WithName("GetCephalonDurableExecutionStatesWithPendingSignals");
        engineGroup.MapGet("/durable-executions/runtime/signals/{signalId}", (string signalId, HttpContext httpContext) =>
            {
                var catalog = httpContext.RequestServices.GetService<IDurableExecutionRuntimeStateCatalog>();
                return TypedResults.Ok(catalog?.GetByPendingSignalId(signalId) ?? []);
            })
            .WithName("GetCephalonDurableExecutionStatesByPendingSignal");
        engineGroup.MapGet("/durable-executions/runtime/compensations", (HttpContext httpContext) =>
            {
                var catalog = httpContext.RequestServices.GetService<IDurableExecutionRuntimeStateCatalog>();
                return TypedResults.Ok(catalog?.GetWithCompensationActions() ?? []);
            })
            .WithName("GetCephalonDurableExecutionStatesWithCompensationActions");
        engineGroup.MapGet("/durable-executions/runtime/compensations/{compensationId}", (string compensationId, HttpContext httpContext) =>
            {
                var catalog = httpContext.RequestServices.GetService<IDurableExecutionRuntimeStateCatalog>();
                return TypedResults.Ok(catalog?.GetByCompensationActionId(compensationId) ?? []);
            })
            .WithName("GetCephalonDurableExecutionStatesByCompensationAction");
        engineGroup.MapGet("/durable-executions/runtime/streams/{streamId}", (string streamId, HttpContext httpContext) =>
            {
                var catalog = httpContext.RequestServices.GetService<IDurableExecutionRuntimeStateCatalog>();
                var state = catalog?.GetByStreamId(streamId);

                return state is null ? Results.NotFound() : Results.Ok(state);
            })
            .WithName("GetCephalonDurableExecutionState");
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
        engineGroup.MapGet("/rest-endpoint-authoring-policies", ([FromServices] IRestEndpointAuthoringPolicyRuntimeCatalog catalog) => TypedResults.Ok(catalog.Policies))
            .WithName("GetCephalonRestEndpointAuthoringPolicies");
        engineGroup.MapGet("/rest-endpoint-authoring-policies/{behaviorId}", (string behaviorId, [FromServices] IRestEndpointAuthoringPolicyRuntimeCatalog catalog) =>
            {
                var policy = catalog.GetByBehaviorId(behaviorId);

                return policy is null ? Results.NotFound() : Results.Ok(policy);
            })
            .WithName("GetCephalonRestEndpointAuthoringPolicy");
        engineGroup.MapGet("/rest-endpoint-publication-groups", ([FromServices] IRestEndpointPublicationGroupRuntimeCatalog catalog) => TypedResults.Ok(catalog.Groups))
            .WithName("GetCephalonRestEndpointPublicationGroups");
        engineGroup.MapGet("/rest-endpoint-publication-groups/{behaviorId}", (string behaviorId, [FromServices] IRestEndpointPublicationGroupRuntimeCatalog catalog) =>
            {
                var group = catalog.GetByBehaviorId(behaviorId);

                return group is null ? Results.NotFound() : Results.Ok(group);
            })
            .WithName("GetCephalonRestEndpointPublicationGroup");
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
        engineGroup.MapGet("/data-products", ([FromServices] IDataProductCatalog catalog) => TypedResults.Ok(catalog.DataProducts))
            .WithName("GetCephalonDataProducts");
        engineGroup.MapGet("/data-products/{dataProductId}", (string dataProductId, [FromServices] IDataProductCatalog catalog) =>
            {
                var dataProduct = catalog.GetById(dataProductId);

                return dataProduct is null ? Results.NotFound() : Results.Ok(dataProduct);
            })
            .WithName("GetCephalonDataProduct");
        engineGroup.MapGet("/cdc-captures", ([FromServices] ICdcCaptureCatalog catalog) => TypedResults.Ok(catalog.CdcCaptures))
            .WithName("GetCephalonCdcCaptures");
        engineGroup.MapGet("/cdc-capture-runtimes", (HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .Runtimes ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimes");
        engineGroup.MapGet("/cdc-capture-runtimes/reporters/{reporterId}", (string reporterId, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByReporterId(reporterId) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByReporter");
        engineGroup.MapGet("/cdc-capture-runtimes/edge-nodes/{edgeNodeId}", (string edgeNodeId, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByEdgeNodeId(edgeNodeId) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByEdgeNode");
        engineGroup.MapGet("/cdc-capture-runtimes/reporter-coordination/{coordinationState}", (string coordinationState, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByReporterCoordinationState(coordinationState) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByReporterCoordinationState");
        engineGroup.MapGet("/cdc-capture-runtimes/reporter-coordination/issues/{degradedReason}", (string degradedReason, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByReporterCoordinationIssueReason(degradedReason) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByReporterCoordinationIssueReason");
        engineGroup.MapGet("/cdc-capture-runtimes/remediation/{remediationState}", (string remediationState, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByRemediationState(remediationState) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByRemediationState");
        engineGroup.MapGet("/cdc-capture-runtimes/remediation/categories/{remediationCategory}", (string remediationCategory, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByRemediationCategory(remediationCategory) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByRemediationCategory");
        engineGroup.MapGet("/cdc-capture-runtimes/governance/{governanceState}", (string governanceState, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorGovernanceState(governanceState) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorGovernanceState");
        engineGroup.MapGet("/cdc-capture-runtimes/governance/categories/{governanceCategory}", (string governanceCategory, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorGovernanceCategory(governanceCategory) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorGovernanceCategory");
        engineGroup.MapGet("/cdc-capture-runtimes/drift/{driftState}", (string driftState, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorDriftState(driftState) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorDriftState");
        engineGroup.MapGet("/cdc-capture-runtimes/drift/categories/{driftCategory}", (string driftCategory, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorDriftCategory(driftCategory) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorDriftCategory");
        engineGroup.MapGet("/cdc-capture-runtimes/action-plans/{actionPlanState}", (string actionPlanState, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorActionPlanState(actionPlanState) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorActionPlanState");
        engineGroup.MapGet("/cdc-capture-runtimes/actions/{actionId}", (string actionId, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorActionId(actionId) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorAction");
        engineGroup.MapGet("/cdc-capture-runtimes/write-path-readiness/{readinessState}", (string readinessState, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorWritePathReadinessState(readinessState) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorWritePathReadinessState");
        engineGroup.MapGet("/cdc-capture-runtimes/write-path-readiness/categories/{readinessCategory}", (string readinessCategory, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorWritePathReadinessCategory(readinessCategory) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorWritePathReadinessCategory");
        engineGroup.MapGet("/cdc-capture-runtimes/preflight/{preflightState}", (string preflightState, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorPreflightState(preflightState) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorPreflightState");
        engineGroup.MapGet("/cdc-capture-runtimes/preflight/categories/{preflightCategory}", (string preflightCategory, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorPreflightCategory(preflightCategory) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorPreflightCategory");
        engineGroup.MapGet("/cdc-capture-runtimes/preflight/operations/{operationId}", (string operationId, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorPreflightOperationId(operationId) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorPreflightOperation");
        engineGroup.MapGet("/cdc-capture-runtimes/dry-runs/{dryRunState}", (string dryRunState, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorDryRunState(dryRunState) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorDryRunState");
        engineGroup.MapGet("/cdc-capture-runtimes/dry-runs/categories/{dryRunCategory}", (string dryRunCategory, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorDryRunCategory(dryRunCategory) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorDryRunCategory");
        engineGroup.MapGet("/cdc-capture-runtimes/dry-runs/operations/{operationId}", (string operationId, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorDryRunOperationId(operationId) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorDryRunOperation");
        engineGroup.MapGet("/cdc-capture-runtimes/execution-intents/{executionIntentState}", (string executionIntentState, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorExecutionIntentState(executionIntentState) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorExecutionIntentState");
        engineGroup.MapGet("/cdc-capture-runtimes/execution-intents/categories/{executionIntentCategory}", (string executionIntentCategory, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorExecutionIntentCategory(executionIntentCategory) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorExecutionIntentCategory");
        engineGroup.MapGet("/cdc-capture-runtimes/execution-intents/operations/{operationId}", (string operationId, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorExecutionIntentOperationId(operationId) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorExecutionIntentOperation");
        engineGroup.MapGet("/cdc-capture-runtimes/execution-approvals/{executionApprovalState}", (string executionApprovalState, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorExecutionApprovalState(executionApprovalState) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorExecutionApprovalState");
        engineGroup.MapGet("/cdc-capture-runtimes/execution-approvals/categories/{executionApprovalCategory}", (string executionApprovalCategory, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorExecutionApprovalCategory(executionApprovalCategory) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorExecutionApprovalCategory");
        engineGroup.MapGet("/cdc-capture-runtimes/execution-approvals/operations/{operationId}", (string operationId, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorExecutionApprovalOperationId(operationId) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorExecutionApprovalOperation");
        engineGroup.MapGet("/cdc-capture-runtimes/command-envelopes/{commandState}", (string commandState, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorCommandEnvelopeState(commandState) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorCommandEnvelopeState");
        engineGroup.MapGet("/cdc-capture-runtimes/command-envelopes/categories/{commandCategory}", (string commandCategory, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorCommandEnvelopeCategory(commandCategory) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorCommandEnvelopeCategory");
        engineGroup.MapGet("/cdc-capture-runtimes/command-envelopes/operations/{operationId}", (string operationId, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorCommandEnvelopeOperationId(operationId) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorCommandEnvelopeOperation");
        engineGroup.MapGet("/cdc-capture-runtimes/command-issuances/{issuanceState}", (string issuanceState, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorCommandIssuanceState(issuanceState) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorCommandIssuanceState");
        engineGroup.MapGet("/cdc-capture-runtimes/command-issuances/categories/{issuanceCategory}", (string issuanceCategory, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorCommandIssuanceCategory(issuanceCategory) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorCommandIssuanceCategory");
        engineGroup.MapGet("/cdc-capture-runtimes/command-issuances/operations/{operationId}", (string operationId, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorCommandIssuanceOperationId(operationId) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorCommandIssuanceOperation");
        engineGroup.MapGet("/cdc-capture-runtimes/execution-adapters/{executionAdapterState}", (string executionAdapterState, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorExecutionAdapterState(executionAdapterState) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorExecutionAdapterState");
        engineGroup.MapGet("/cdc-capture-runtimes/execution-adapters/categories/{executionAdapterCategory}", (string executionAdapterCategory, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorExecutionAdapterCategory(executionAdapterCategory) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorExecutionAdapterCategory");
        engineGroup.MapGet("/cdc-capture-runtimes/execution-adapters/operations/{operationId}", (string operationId, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorExecutionAdapterOperationId(operationId) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorExecutionAdapterOperation");
        engineGroup.MapGet("/cdc-capture-runtimes/command-executions/{executionState}", (string executionState, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorCommandExecutionState(executionState) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorCommandExecutionState");
        engineGroup.MapGet("/cdc-capture-runtimes/command-executions/operations/{operationId}", (string operationId, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorCommandExecutionOperationId(operationId) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorCommandExecutionOperation");
        engineGroup.MapGet("/cdc-capture-runtimes/command-retries/{retryState}", (string retryState, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorCommandRetryState(retryState) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorCommandRetryState");
        engineGroup.MapGet("/cdc-capture-runtimes/command-retries/categories/{retryCategory}", (string retryCategory, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorCommandRetryCategory(retryCategory) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorCommandRetryCategory");
        engineGroup.MapGet("/cdc-capture-runtimes/command-retries/operations/{operationId}", (string operationId, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorCommandRetryOperationId(operationId) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorCommandRetryOperation");
        engineGroup.MapGet("/cdc-capture-runtimes/retry-execution-policies/{policyState}", (string policyState, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorRetryExecutionPolicyState(policyState) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorRetryExecutionPolicyState");
        engineGroup.MapGet("/cdc-capture-runtimes/retry-execution-policies/categories/{policyCategory}", (string policyCategory, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorRetryExecutionPolicyCategory(policyCategory) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorRetryExecutionPolicyCategory");
        engineGroup.MapGet("/cdc-capture-runtimes/retry-execution-policies/operations/{operationId}", (string operationId, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorRetryExecutionPolicyOperationId(operationId) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorRetryExecutionPolicyOperation");
        engineGroup.MapGet("/cdc-capture-runtimes/command-journals/{journalState}", (string journalState, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorCommandJournalState(journalState) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorCommandJournalState");
        engineGroup.MapGet("/cdc-capture-runtimes/command-journals/categories/{journalCategory}", (string journalCategory, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorCommandJournalCategory(journalCategory) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorCommandJournalCategory");
        engineGroup.MapGet("/cdc-capture-runtimes/command-journal-durability/{durabilityState}", (string durabilityState, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorCommandJournalDurabilityState(durabilityState) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorCommandJournalDurabilityState");
        engineGroup.MapGet("/cdc-capture-runtimes/command-journal-durability/categories/{durabilityCategory}", (string durabilityCategory, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorCommandJournalDurabilityCategory(durabilityCategory) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorCommandJournalDurabilityCategory");
        engineGroup.MapGet("/cdc-capture-runtimes/automatic-retries/{automaticRetryState}", (string automaticRetryState, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorAutomaticRetryExecutionState(automaticRetryState) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorAutomaticRetryExecutionState");
        engineGroup.MapGet("/cdc-capture-runtimes/automatic-retries/categories/{automaticRetryCategory}", (string automaticRetryCategory, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorAutomaticRetryExecutionCategory(automaticRetryCategory) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorAutomaticRetryExecutionCategory");
        engineGroup.MapGet("/cdc-capture-runtimes/automatic-retries/operations/{operationId}", (string operationId, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorAutomaticRetryExecutionOperationId(operationId) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorAutomaticRetryExecutionOperation");
        engineGroup.MapGet("/cdc-capture-runtimes/automatic-retry-coordinations/{coordinationState}", (string coordinationState, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorAutomaticRetryCoordinationState(coordinationState) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorAutomaticRetryCoordinationState");
        engineGroup.MapGet("/cdc-capture-runtimes/automatic-retry-coordinations/categories/{coordinationCategory}", (string coordinationCategory, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorAutomaticRetryCoordinationCategory(coordinationCategory) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorAutomaticRetryCoordinationCategory");
        engineGroup.MapGet("/cdc-capture-runtimes/automatic-retry-coordinations/owners/{ownerId}", (string ownerId, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorAutomaticRetryCoordinationOwnerId(ownerId) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorAutomaticRetryCoordinationOwner");
        engineGroup.MapGet("/cdc-capture-runtimes/distributed-retry-leases/{leaseState}", (string leaseState, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorDistributedRetryLeaseState(leaseState) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorDistributedRetryLeaseState");
        engineGroup.MapGet("/cdc-capture-runtimes/distributed-retry-leases/categories/{leaseCategory}", (string leaseCategory, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorDistributedRetryLeaseCategory(leaseCategory) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorDistributedRetryLeaseCategory");
        engineGroup.MapGet("/cdc-capture-runtimes/distributed-retry-leases/owners/{ownerId}", (string ownerId, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorDistributedRetryLeaseOwnerId(ownerId) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorDistributedRetryLeaseOwner");
        engineGroup.MapGet("/cdc-capture-runtimes/cross-node-idempotency-hardenings/{hardeningState}", (string hardeningState, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorCrossNodeIdempotencyHardeningState(hardeningState) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorCrossNodeIdempotencyHardeningState");
        engineGroup.MapGet("/cdc-capture-runtimes/cross-node-idempotency-hardenings/categories/{hardeningCategory}", (string hardeningCategory, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorCrossNodeIdempotencyHardeningCategory(hardeningCategory) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorCrossNodeIdempotencyHardeningCategory");
        engineGroup.MapGet("/cdc-capture-runtimes/cross-node-idempotency-hardenings/owners/{ownerId}", (string ownerId, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorCrossNodeIdempotencyHardeningOwnerId(ownerId) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorCrossNodeIdempotencyHardeningOwner");
        engineGroup.MapGet("/cdc-capture-runtimes/cross-node-idempotency-hardenings/fingerprints/{retryFingerprint}", (string retryFingerprint, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorCrossNodeIdempotencyHardeningRetryFingerprint(retryFingerprint) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorCrossNodeIdempotencyHardeningRetryFingerprint");
        engineGroup.MapGet("/cdc-capture-runtimes/distributed-retry-orchestrations/{orchestrationState}", (string orchestrationState, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorDistributedRetryOrchestrationState(orchestrationState) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorDistributedRetryOrchestrationState");
        engineGroup.MapGet("/cdc-capture-runtimes/distributed-retry-orchestrations/categories/{orchestrationCategory}", (string orchestrationCategory, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorDistributedRetryOrchestrationCategory(orchestrationCategory) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorDistributedRetryOrchestrationCategory");
        engineGroup.MapGet("/cdc-capture-runtimes/distributed-retry-orchestrations/owners/{ownerId}", (string ownerId, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorDistributedRetryOrchestrationOwnerId(ownerId) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorDistributedRetryOrchestrationOwner");
        engineGroup.MapGet("/cdc-capture-runtimes/multi-node-lease-executions/{leaseExecutionState}", (string leaseExecutionState, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorMultiNodeLeaseExecutionState(leaseExecutionState) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorMultiNodeLeaseExecutionState");
        engineGroup.MapGet("/cdc-capture-runtimes/multi-node-lease-executions/categories/{leaseExecutionCategory}", (string leaseExecutionCategory, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorMultiNodeLeaseExecutionCategory(leaseExecutionCategory) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorMultiNodeLeaseExecutionCategory");
        engineGroup.MapGet("/cdc-capture-runtimes/multi-node-lease-executions/owners/{ownerId}", (string ownerId, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorMultiNodeLeaseExecutionOwnerId(ownerId) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorMultiNodeLeaseExecutionOwner");
        engineGroup.MapGet("/cdc-capture-runtimes/durable-shared-scheduler-orchestrations/{schedulerState}", (string schedulerState, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorDurableSharedSchedulerOrchestrationState(schedulerState) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorDurableSharedSchedulerOrchestrationState");
        engineGroup.MapGet("/cdc-capture-runtimes/durable-shared-scheduler-orchestrations/categories/{schedulerCategory}", (string schedulerCategory, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorDurableSharedSchedulerOrchestrationCategory(schedulerCategory) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorDurableSharedSchedulerOrchestrationCategory");
        engineGroup.MapGet("/cdc-capture-runtimes/durable-shared-scheduler-orchestrations/owners/{ownerId}", (string ownerId, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorDurableSharedSchedulerOrchestrationOwnerId(ownerId) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorDurableSharedSchedulerOrchestrationOwner");
        engineGroup.MapGet("/cdc-capture-runtimes/scheduler-recovery-execution-hardenings/{hardeningState}", (string hardeningState, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorSchedulerRecoveryExecutionHardeningState(hardeningState) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorSchedulerRecoveryExecutionHardeningState");
        engineGroup.MapGet("/cdc-capture-runtimes/scheduler-recovery-execution-hardenings/categories/{hardeningCategory}", (string hardeningCategory, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorSchedulerRecoveryExecutionHardeningCategory(hardeningCategory) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorSchedulerRecoveryExecutionHardeningCategory");
        engineGroup.MapGet("/cdc-capture-runtimes/scheduler-recovery-execution-hardenings/owners/{ownerId}", (string ownerId, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorSchedulerRecoveryExecutionHardeningOwnerId(ownerId) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorSchedulerRecoveryExecutionHardeningOwner");
        engineGroup.MapGet("/cdc-capture-runtimes/scheduler-recovery-execution-hardenings/fingerprints/{retryFingerprint}", (string retryFingerprint, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorSchedulerRecoveryExecutionHardeningRetryFingerprint(retryFingerprint) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorSchedulerRecoveryExecutionHardeningRetryFingerprint");
        engineGroup.MapGet("/cdc-capture-runtimes/provider-owned-write-path-executions/{providerExecutionState}", (string providerExecutionState, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorProviderOwnedWritePathExecutionState(providerExecutionState) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorProviderOwnedWritePathExecutionState");
        engineGroup.MapGet("/cdc-capture-runtimes/provider-owned-write-path-executions/categories/{providerExecutionCategory}", (string providerExecutionCategory, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorProviderOwnedWritePathExecutionCategory(providerExecutionCategory) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorProviderOwnedWritePathExecutionCategory");
        engineGroup.MapGet("/cdc-capture-runtimes/provider-owned-write-path-executions/operations/{operationId}", (string operationId, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorProviderOwnedWritePathExecutionOperationId(operationId) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorProviderOwnedWritePathExecutionOperation");
        engineGroup.MapGet("/cdc-capture-runtimes/provider-execution-orchestrations/{providerExecutionOrchestrationState}", (string providerExecutionOrchestrationState, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorProviderExecutionOrchestrationState(providerExecutionOrchestrationState) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorProviderExecutionOrchestrationState");
        engineGroup.MapGet("/cdc-capture-runtimes/provider-execution-orchestrations/categories/{providerExecutionOrchestrationCategory}", (string providerExecutionOrchestrationCategory, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorProviderExecutionOrchestrationCategory(providerExecutionOrchestrationCategory) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorProviderExecutionOrchestrationCategory");
        engineGroup.MapGet("/cdc-capture-runtimes/provider-execution-orchestrations/operations/{operationId}", (string operationId, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorProviderExecutionOrchestrationOperationId(operationId) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorProviderExecutionOrchestrationOperation");
        engineGroup.MapGet("/cdc-capture-runtimes/provider-owned-control-plane-ownership/{providerOwnedControlPlaneOwnershipState}", (string providerOwnedControlPlaneOwnershipState, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorProviderOwnedControlPlaneOwnershipState(providerOwnedControlPlaneOwnershipState) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorProviderOwnedControlPlaneOwnershipState");
        engineGroup.MapGet("/cdc-capture-runtimes/provider-owned-control-plane-ownership/categories/{providerOwnedControlPlaneOwnershipCategory}", (string providerOwnedControlPlaneOwnershipCategory, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorProviderOwnedControlPlaneOwnershipCategory(providerOwnedControlPlaneOwnershipCategory) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorProviderOwnedControlPlaneOwnershipCategory");
        engineGroup.MapGet("/cdc-capture-runtimes/provider-owned-control-plane-ownership/operations/{operationId}", (string operationId, HttpContext httpContext) =>
            {
                var runtimes = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetByManagedConnectorProviderOwnedControlPlaneOwnershipOperationId(operationId) ?? [];

                return Results.Ok(runtimes);
            })
            .WithName("GetCephalonCdcCaptureRuntimesByManagedConnectorProviderOwnedControlPlaneOwnershipOperation");
        engineGroup.MapGet("/cdc-capture-runtimes/{executionRuntimeId}/command-executions", (string executionRuntimeId, HttpContext httpContext) =>
            {
                var runtimeCatalog = httpContext.RequestServices.GetService<ICdcCaptureExecutionRuntimeCatalog>();
                var runtimeDescriptor = runtimeCatalog?.GetById(executionRuntimeId);
                if (runtimeDescriptor is null)
                {
                    return Results.NotFound();
                }

                return Results.Ok(runtimeCatalog?.GetManagedConnectorCommandExecutionHistory(executionRuntimeId) ?? []);
            })
            .WithName("GetCephalonManagedConnectorCommandExecutionHistory");
        engineGroup.MapGet("/cdc-capture-runtimes/{executionRuntimeId}/command-journal", (string executionRuntimeId, HttpContext httpContext) =>
            {
                var runtimeDescriptor = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetById(executionRuntimeId);

                return runtimeDescriptor is null
                    ? Results.NotFound()
                    : Results.Ok(runtimeDescriptor.ManagedConnectorCommandJournal);
            })
            .WithName("GetCephalonManagedConnectorCommandJournal");
        engineGroup.MapGet("/cdc-capture-runtimes/{executionRuntimeId}/command-journal-durability", (string executionRuntimeId, HttpContext httpContext) =>
            {
                var runtimeDescriptor = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetById(executionRuntimeId);

                return runtimeDescriptor is null
                    ? Results.NotFound()
                    : Results.Ok(runtimeDescriptor.ManagedConnectorCommandJournalDurability);
            })
            .WithName("GetCephalonManagedConnectorCommandJournalDurability");
        engineGroup.MapGet("/cdc-capture-runtimes/{executionRuntimeId}/distributed-retry-lease", (string executionRuntimeId, HttpContext httpContext) =>
            {
                var runtimeDescriptor = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetById(executionRuntimeId);

                return runtimeDescriptor is null
                    ? Results.NotFound()
                    : Results.Ok(runtimeDescriptor.ManagedConnectorDistributedRetryLease);
            })
            .WithName("GetCephalonManagedConnectorDistributedRetryLease");
        engineGroup.MapGet("/cdc-capture-runtimes/{executionRuntimeId}/cross-node-idempotency-hardening", (string executionRuntimeId, HttpContext httpContext) =>
            {
                var runtimeDescriptor = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetById(executionRuntimeId);

                return runtimeDescriptor is null
                    ? Results.NotFound()
                    : Results.Ok(runtimeDescriptor.ManagedConnectorCrossNodeIdempotencyHardening);
            })
            .WithName("GetCephalonManagedConnectorCrossNodeIdempotencyHardening");
        engineGroup.MapGet("/cdc-capture-runtimes/{executionRuntimeId}/distributed-retry-orchestration", (string executionRuntimeId, HttpContext httpContext) =>
            {
                var runtimeDescriptor = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetById(executionRuntimeId);

                return runtimeDescriptor is null
                    ? Results.NotFound()
                    : Results.Ok(runtimeDescriptor.ManagedConnectorDistributedRetryOrchestration);
            })
            .WithName("GetCephalonManagedConnectorDistributedRetryOrchestration");
        engineGroup.MapGet("/cdc-capture-runtimes/{executionRuntimeId}/multi-node-lease-execution", (string executionRuntimeId, HttpContext httpContext) =>
            {
                var runtimeDescriptor = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetById(executionRuntimeId);

                return runtimeDescriptor is null
                    ? Results.NotFound()
                    : Results.Ok(runtimeDescriptor.ManagedConnectorMultiNodeLeaseExecution);
            })
            .WithName("GetCephalonManagedConnectorMultiNodeLeaseExecution");
        engineGroup.MapGet("/cdc-capture-runtimes/{executionRuntimeId}/durable-shared-scheduler-orchestration", (string executionRuntimeId, HttpContext httpContext) =>
            {
                var runtimeDescriptor = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetById(executionRuntimeId);

                return runtimeDescriptor is null
                    ? Results.NotFound()
                    : Results.Ok(runtimeDescriptor.ManagedConnectorDurableSharedSchedulerOrchestration);
            })
            .WithName("GetCephalonManagedConnectorDurableSharedSchedulerOrchestration");
        engineGroup.MapGet("/cdc-capture-runtimes/{executionRuntimeId}/scheduler-recovery-execution-hardening", (string executionRuntimeId, HttpContext httpContext) =>
            {
                var runtimeDescriptor = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetById(executionRuntimeId);

                return runtimeDescriptor is null
                    ? Results.NotFound()
                    : Results.Ok(runtimeDescriptor.ManagedConnectorSchedulerRecoveryExecutionHardening);
            })
            .WithName("GetCephalonManagedConnectorSchedulerRecoveryExecutionHardening");
        engineGroup.MapGet("/cdc-capture-runtimes/{executionRuntimeId}/provider-owned-write-path-execution", (string executionRuntimeId, HttpContext httpContext) =>
            {
                var runtimeDescriptor = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetById(executionRuntimeId);

                return runtimeDescriptor is null
                    ? Results.NotFound()
                    : Results.Ok(runtimeDescriptor.ManagedConnectorProviderOwnedWritePathExecution);
            })
            .WithName("GetCephalonManagedConnectorProviderOwnedWritePathExecution");
        engineGroup.MapGet("/cdc-capture-runtimes/{executionRuntimeId}/provider-execution-orchestration", (string executionRuntimeId, HttpContext httpContext) =>
            {
                var runtimeDescriptor = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetById(executionRuntimeId);

                return runtimeDescriptor is null
                    ? Results.NotFound()
                    : Results.Ok(runtimeDescriptor.ManagedConnectorProviderExecutionOrchestration);
            })
            .WithName("GetCephalonManagedConnectorProviderExecutionOrchestration");
        engineGroup.MapGet("/cdc-capture-runtimes/{executionRuntimeId}/provider-owned-control-plane-ownership", (string executionRuntimeId, HttpContext httpContext) =>
            {
                var runtimeDescriptor = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetById(executionRuntimeId);

                return runtimeDescriptor is null
                    ? Results.NotFound()
                    : Results.Ok(runtimeDescriptor.ManagedConnectorProviderOwnedControlPlaneOwnership);
            })
            .WithName("GetCephalonManagedConnectorProviderOwnedControlPlaneOwnership");
        engineGroup.MapGet("/cdc-capture-runtimes/{executionRuntimeId}", (string executionRuntimeId, HttpContext httpContext) =>
            {
                var runtimeDescriptor = httpContext.RequestServices
                    .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                    .GetById(executionRuntimeId);

                return runtimeDescriptor is null ? Results.NotFound() : Results.Ok(runtimeDescriptor);
            })
            .WithName("GetCephalonCdcCaptureRuntime");
        if (app.Services.GetService<ICdcCaptureExecutionRuntimeReportSink>() is not null)
        {
            engineGroup.MapPost("/cdc-capture-runtimes/{executionRuntimeId}/reports", async (
                    string executionRuntimeId,
                    CdcCaptureRuntimeObservation[]? observations,
                    HttpContext httpContext,
                    CancellationToken cancellationToken) =>
                {
                    if (observations is null || observations.Length == 0)
                    {
                        return Results.BadRequest("At least one CDC capture runtime observation is required.");
                    }

                    var runtimeCatalog = httpContext.RequestServices.GetService<ICdcCaptureExecutionRuntimeCatalog>();
                    var runtimeDescriptor = runtimeCatalog?.GetById(executionRuntimeId);
                    if (runtimeDescriptor is null)
                    {
                        return Results.NotFound();
                    }

                    var reportSink = httpContext.RequestServices.GetRequiredService<ICdcCaptureExecutionRuntimeReportSink>();

                    try
                    {
                        await reportSink.ReportAsync(executionRuntimeId, observations, cancellationToken);
                    }
                    catch (InvalidOperationException exception)
                    {
                        return Results.BadRequest(exception.Message);
                    }

                    return Results.Ok(runtimeCatalog?.GetById(executionRuntimeId) ?? runtimeDescriptor);
                })
                .WithName("PostCephalonCdcCaptureRuntimeReports");
        }
        if (app.Services.GetService<ICdcCaptureExecutionRuntimeManagedConnectorCommandExecutor>() is not null)
        {
            engineGroup.MapPost("/cdc-capture-runtimes/{executionRuntimeId}/commands/{operationId}", async (
                    string executionRuntimeId,
                    string operationId,
                    CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionRequest? request,
                    HttpContext httpContext,
                    CancellationToken cancellationToken) =>
                {
                    var runtimeCatalog = httpContext.RequestServices.GetService<ICdcCaptureExecutionRuntimeCatalog>();
                    var runtimeDescriptor = runtimeCatalog?.GetById(executionRuntimeId);
                    if (runtimeDescriptor is null)
                    {
                        return Results.NotFound();
                    }

                    var executor = httpContext.RequestServices.GetRequiredService<ICdcCaptureExecutionRuntimeManagedConnectorCommandExecutor>();

                    try
                    {
                        var result = await executor.ExecuteAsync(
                            executionRuntimeId,
                            operationId,
                            request,
                            cancellationToken);

                        return Results.Ok(result);
                    }
                    catch (InvalidOperationException exception)
                    {
                        return Results.BadRequest(exception.Message);
                    }
                })
                .WithName("PostCephalonManagedConnectorCommandExecution");
        }
        engineGroup.MapGet("/cdc-captures/runtime", (HttpContext httpContext) =>
            {
                var catalog = httpContext.RequestServices.GetService<ICdcCaptureRuntimeStateCatalog>();
                return TypedResults.Ok(catalog?.States ?? []);
            })
            .WithName("GetCephalonCdcCaptureStates");
        engineGroup.MapGet("/cdc-captures/runtime/{cdcCaptureId}", (string cdcCaptureId, HttpContext httpContext) =>
            {
                var catalog = httpContext.RequestServices.GetService<ICdcCaptureRuntimeStateCatalog>();
                var state = catalog?.GetById(cdcCaptureId);

                return state is null ? Results.NotFound() : Results.Ok(state);
            })
            .WithName("GetCephalonCdcCaptureState");
        engineGroup.MapGet("/cdc-captures/runtime/modules/{moduleId}", (string moduleId, HttpContext httpContext) =>
            {
                var catalog = httpContext.RequestServices.GetService<ICdcCaptureRuntimeStateCatalog>();
                return TypedResults.Ok(catalog?.GetBySourceModule(moduleId) ?? []);
            })
            .WithName("GetCephalonCdcCaptureStatesByModule");
        engineGroup.MapGet("/cdc-captures/runtime/providers/{provider}", (string provider, HttpContext httpContext) =>
            {
                var catalog = httpContext.RequestServices.GetService<ICdcCaptureRuntimeStateCatalog>();
                return TypedResults.Ok(catalog?.GetByProvider(provider) ?? []);
            })
            .WithName("GetCephalonCdcCaptureStatesByProvider");
        engineGroup.MapGet("/cdc-captures/runtime/outboxes/{outboxId}", (string outboxId, HttpContext httpContext) =>
            {
                var catalog = httpContext.RequestServices.GetService<ICdcCaptureRuntimeStateCatalog>();
                return TypedResults.Ok(catalog?.GetByOutboxId(outboxId) ?? []);
            })
            .WithName("GetCephalonCdcCaptureStatesByOutbox");
        engineGroup.MapGet("/cdc-captures/runtime/sources/{sourceId}", (string sourceId, HttpContext httpContext) =>
            {
                var catalog = httpContext.RequestServices.GetService<ICdcCaptureRuntimeStateCatalog>();
                return TypedResults.Ok(catalog?.GetBySourceId(sourceId) ?? []);
            })
            .WithName("GetCephalonCdcCaptureStatesBySource");
        engineGroup.MapGet("/cdc-captures/runtime/resources/{resourceId}", (string resourceId, HttpContext httpContext) =>
            {
                var catalog = httpContext.RequestServices.GetService<ICdcCaptureRuntimeStateCatalog>();
                return TypedResults.Ok(catalog?.GetByResourceId(resourceId) ?? []);
            })
            .WithName("GetCephalonCdcCaptureStatesByResource");
        engineGroup.MapGet("/cdc-captures/runtime/execution-runtimes/{executionRuntimeId}", (string executionRuntimeId, HttpContext httpContext) =>
            {
                var catalog = httpContext.RequestServices.GetService<ICdcCaptureRuntimeStateCatalog>();
                var states = catalog?.GetByExecutionRuntimeId(executionRuntimeId) ?? [];

                return TypedResults.Ok(states);
            })
            .WithName("GetCephalonCdcCaptureStatesByExecutionRuntime");
        engineGroup.MapGet("/cdc-captures/runtime/reporters/{reporterId}", (string reporterId, HttpContext httpContext) =>
            {
                var catalog = httpContext.RequestServices.GetService<ICdcCaptureRuntimeStateCatalog>();
                return TypedResults.Ok(catalog?.GetByReporterId(reporterId) ?? []);
            })
            .WithName("GetCephalonCdcCaptureStatesByReporter");
        engineGroup.MapGet("/cdc-captures/runtime/edge-nodes/{edgeNodeId}", (string edgeNodeId, HttpContext httpContext) =>
            {
                var catalog = httpContext.RequestServices.GetService<ICdcCaptureRuntimeStateCatalog>();
                return TypedResults.Ok(catalog?.GetByEdgeNodeId(edgeNodeId) ?? []);
            })
            .WithName("GetCephalonCdcCaptureStatesByEdgeNode");
        engineGroup.MapGet("/cdc-captures/runtime/reporter-coordination/{coordinationState}", (string coordinationState, HttpContext httpContext) =>
            {
                var catalog = httpContext.RequestServices.GetService<ICdcCaptureRuntimeStateCatalog>();
                return TypedResults.Ok(catalog?.GetByReporterCoordinationState(coordinationState) ?? []);
            })
            .WithName("GetCephalonCdcCaptureStatesByReporterCoordinationState");
        engineGroup.MapGet("/cdc-captures/runtime/reporter-coordination/issues/{degradedReason}", (string degradedReason, HttpContext httpContext) =>
            {
                var catalog = httpContext.RequestServices.GetService<ICdcCaptureRuntimeStateCatalog>();
                return TypedResults.Ok(catalog?.GetByReporterCoordinationIssueReason(degradedReason) ?? []);
            })
            .WithName("GetCephalonCdcCaptureStatesByReporterCoordinationIssueReason");
        engineGroup.MapGet("/cdc-captures/{cdcCaptureId}", (string cdcCaptureId, [FromServices] ICdcCaptureCatalog catalog) =>
            {
                var cdcCapture = catalog.GetById(cdcCaptureId);

                return cdcCapture is null ? Results.NotFound() : Results.Ok(cdcCapture);
            })
            .WithName("GetCephalonCdcCapture");
        engineGroup.MapGet("/cdc-captures/modules/{moduleId}", (string moduleId, [FromServices] ICdcCaptureCatalog catalog) =>
                TypedResults.Ok(catalog.GetBySourceModule(moduleId)))
            .WithName("GetCephalonCdcCapturesByModule");
        engineGroup.MapGet("/cdc-captures/providers/{provider}", (string provider, [FromServices] ICdcCaptureCatalog catalog) =>
                TypedResults.Ok(catalog.GetByProvider(provider)))
            .WithName("GetCephalonCdcCapturesByProvider");
        engineGroup.MapGet("/cdc-captures/outboxes/{outboxId}", (string outboxId, [FromServices] ICdcCaptureCatalog catalog) =>
                TypedResults.Ok(catalog.GetByOutboxId(outboxId)))
            .WithName("GetCephalonCdcCapturesByOutbox");
        engineGroup.MapGet("/cdc-captures/sources/{sourceId}", (string sourceId, [FromServices] ICdcCaptureCatalog catalog) =>
                TypedResults.Ok(catalog.GetBySourceId(sourceId)))
            .WithName("GetCephalonCdcCapturesBySource");
        engineGroup.MapGet("/cdc-captures/resources/{resourceId}", (string resourceId, [FromServices] ICdcCaptureCatalog catalog) =>
                TypedResults.Ok(catalog.GetByResourceId(resourceId)))
            .WithName("GetCephalonCdcCapturesByResource");
        engineGroup.MapGet("/cdc-captures/execution-runtimes/{executionRuntimeId}", (string executionRuntimeId, [FromServices] ICdcCaptureCatalog catalog) =>
                TypedResults.Ok(catalog.GetByExecutionRuntimeId(executionRuntimeId)))
            .WithName("GetCephalonCdcCapturesByExecutionRuntime");
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
        engineGroup.MapGet("/features", ([FromServices] IFeatureFlagRuntimeCatalog catalog) => TypedResults.Ok(catalog.FeatureFlags))
            .WithName("GetCephalonFeatures");
        engineGroup.MapGet("/features/enabled", ([FromServices] IFeatureFlagRuntimeCatalog catalog) => TypedResults.Ok(catalog.GetEnabled()))
            .WithName("GetCephalonEnabledFeatures");
        engineGroup.MapGet("/features/disabled", ([FromServices] IFeatureFlagRuntimeCatalog catalog) => TypedResults.Ok(catalog.GetDisabled()))
            .WithName("GetCephalonDisabledFeatures");
        engineGroup.MapGet("/features/modules/{moduleId}", (string moduleId, [FromServices] IFeatureFlagRuntimeCatalog catalog) =>
                TypedResults.Ok(catalog.GetBySourceModule(moduleId)))
            .WithName("GetCephalonFeaturesByModule");
        engineGroup.MapGet("/features/{featureFlagId}/evaluate", (
                string featureFlagId,
                string? environmentName,
                string? moduleId,
                string? behaviorId,
                string? capabilityKey,
                string? transportId,
                string? tenantId,
                string? subjectId,
                HttpRequest request,
                [FromServices] IFeatureToggle featureToggle) =>
            {
                var context = new FeatureFlagEvaluationContext(
                    environmentName: environmentName,
                    moduleId: moduleId,
                    behaviorId: behaviorId,
                    capabilityKey: capabilityKey,
                    transportId: transportId,
                    tenantId: tenantId,
                    subjectId: subjectId,
                    tags: request.Query["tag"]
                        .Select(static value => value?.ToString())
                        .Where(static value => !string.IsNullOrWhiteSpace(value))
                        .Select(static value => value!)
                        .ToArray());

                return TypedResults.Ok(featureToggle.Evaluate(featureFlagId, context));
            })
            .WithName("EvaluateCephalonFeature");
        engineGroup.MapGet("/features/{featureFlagId}", (string featureFlagId, [FromServices] IFeatureFlagRuntimeCatalog catalog) =>
            {
                var featureFlag = catalog.GetById(featureFlagId);

                return featureFlag is null ? Results.NotFound() : Results.Ok(featureFlag);
            })
            .WithName("GetCephalonFeature");
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
        engineGroup.MapGet("/strangler-fig/runtime", ([FromServices] IStranglerFigMigrationRuntimeCatalog catalog) => TypedResults.Ok(catalog.Routes))
            .WithName("GetCephalonStranglerFigRuntimeRoutes");
        engineGroup.MapGet("/strangler-fig/ingress", ([FromServices] IStranglerFigIngressRuntimeCatalog catalog) => TypedResults.Ok(catalog.Routes))
            .WithName("GetCephalonStranglerFigIngressRoutes");
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
        engineGroup.MapGet("/strangler-fig/runtime/{routeId}", (string routeId, [FromServices] IStranglerFigMigrationRuntimeCatalog catalog) =>
            {
                var route = catalog.GetById(routeId);

                return route is null ? Results.NotFound() : Results.Ok(route);
            })
            .WithName("GetCephalonStranglerFigRuntimeRoute");
        engineGroup.MapGet("/strangler-fig/ingress/modules/{moduleId}", (string moduleId, [FromServices] IStranglerFigIngressRuntimeCatalog catalog) =>
            {
                return Results.Ok(catalog.GetBySourceModule(moduleId));
            })
            .WithName("GetCephalonStranglerFigIngressRoutesByModule");
        engineGroup.MapGet("/strangler-fig/ingress/{routeId}", (string routeId, [FromServices] IStranglerFigIngressRuntimeCatalog catalog) =>
            {
                var route = catalog.GetById(routeId);

                return route is null ? Results.NotFound() : Results.Ok(route);
            })
            .WithName("GetCephalonStranglerFigIngressRoute");
        engineGroup.MapGet("/strangler-fig/cutover", ([FromServices] AspNetCoreStranglerFigCutoverCatalog catalog) => TypedResults.Ok(catalog.Routes))
            .WithName("GetCephalonStranglerFigCutoverRoutes");
        engineGroup.MapGet("/strangler-fig/cutover/resolve", async (
                string path,
                string? method,
                string? query,
                [FromServices] IStranglerFigRouter router,
                [FromServices] AspNetCoreStranglerFigCutoverCatalog catalog,
                CancellationToken cancellationToken) =>
            {
                var resolution = await router.ResolveAsync(
                        new StranglerFigRequest(path, method ?? HttpMethods.Get),
                        cancellationToken)
                    .ConfigureAwait(false);

                if (resolution is null)
                {
                    return Results.NotFound();
                }

                return Results.Ok(catalog.CreateDecision(
                    resolution,
                    NormalizeOptionalQueryString(query)));
            })
            .WithName("ResolveCephalonStranglerFigCutover");
        engineGroup.MapGet("/strangler-fig/cutover/{routeId}", (string routeId, [FromServices] AspNetCoreStranglerFigCutoverCatalog catalog) =>
            {
                var route = catalog.GetById(routeId);

                return route is null ? Results.NotFound() : Results.Ok(route);
            })
            .WithName("GetCephalonStranglerFigCutoverRoute");
        engineGroup.MapGet("/backend-for-frontend", ([FromServices] IBackendForFrontendRuntimeCatalog catalog) => TypedResults.Ok(catalog.Bindings))
            .WithName("GetCephalonBackendForFrontendBindings");
        engineGroup.MapGet("/backend-for-frontend/clients/{clientId}", (string clientId, [FromServices] IBackendForFrontendRuntimeCatalog catalog) =>
                TypedResults.Ok(catalog.GetByClientId(clientId)))
            .WithName("GetCephalonBackendForFrontendBindingsByClient");
        engineGroup.MapGet("/backend-for-frontend/modules/{moduleId}", (string moduleId, [FromServices] IBackendForFrontendRuntimeCatalog catalog) =>
                TypedResults.Ok(catalog.GetBySourceModule(moduleId)))
            .WithName("GetCephalonBackendForFrontendBindingsByModule");
        engineGroup.MapGet("/backend-for-frontend/transports/{transportId}", (string transportId, [FromServices] IBackendForFrontendRuntimeCatalog catalog) =>
                TypedResults.Ok(catalog.GetByTransportId(transportId)))
            .WithName("GetCephalonBackendForFrontendBindingsByTransport");
        engineGroup.MapGet("/backend-for-frontend/rest-endpoints", ([FromServices] IBackendForFrontendRestRuntimeCatalog catalog) =>
                TypedResults.Ok(catalog.Endpoints))
            .WithName("GetCephalonBackendForFrontendRestEndpoints");
        engineGroup.MapGet("/backend-for-frontend/rest-endpoints/bindings/{bindingId}", (string bindingId, [FromServices] IBackendForFrontendRestRuntimeCatalog catalog) =>
                TypedResults.Ok(catalog.GetByBindingId(bindingId)))
            .WithName("GetCephalonBackendForFrontendRestEndpointsByBinding");
        engineGroup.MapGet("/backend-for-frontend/rest-endpoints/clients/{clientId}", (string clientId, [FromServices] IBackendForFrontendRestRuntimeCatalog catalog) =>
                TypedResults.Ok(catalog.GetByClientId(clientId)))
            .WithName("GetCephalonBackendForFrontendRestEndpointsByClient");
        engineGroup.MapGet("/backend-for-frontend/rest-endpoints/modules/{moduleId}", (string moduleId, [FromServices] IBackendForFrontendRestRuntimeCatalog catalog) =>
                TypedResults.Ok(catalog.GetBySourceModule(moduleId)))
            .WithName("GetCephalonBackendForFrontendRestEndpointsByModule");
        engineGroup.MapGet("/backend-for-frontend/rest-endpoints/published/{restEndpointId}", (string restEndpointId, [FromServices] IBackendForFrontendRestRuntimeCatalog catalog) =>
                TypedResults.Ok(catalog.GetByRestEndpointId(restEndpointId)))
            .WithName("GetCephalonBackendForFrontendRestEndpointsByPublishedEndpoint");
        engineGroup.MapGet("/backend-for-frontend/rest-endpoints/{runtimeEndpointId}", (string runtimeEndpointId, [FromServices] IBackendForFrontendRestRuntimeCatalog catalog) =>
            {
                var runtimeEndpoint = catalog.GetById(runtimeEndpointId);

                return runtimeEndpoint is null ? Results.NotFound() : Results.Ok(runtimeEndpoint);
            })
            .WithName("GetCephalonBackendForFrontendRestEndpoint");
        engineGroup.MapGet("/backend-for-frontend/rest-documents", ([FromServices] IBackendForFrontendRestDocumentRuntimeCatalog catalog) =>
                TypedResults.Ok(catalog.Documents))
            .WithName("GetCephalonBackendForFrontendRestDocuments");
        engineGroup.MapGet("/backend-for-frontend/rest-documents/bindings/{bindingId}", (string bindingId, [FromServices] IBackendForFrontendRestDocumentRuntimeCatalog catalog) =>
                TypedResults.Ok(catalog.GetByBindingId(bindingId)))
            .WithName("GetCephalonBackendForFrontendRestDocumentsByBinding");
        engineGroup.MapGet("/backend-for-frontend/rest-documents/clients/{clientId}", (string clientId, [FromServices] IBackendForFrontendRestDocumentRuntimeCatalog catalog) =>
                TypedResults.Ok(catalog.GetByClientId(clientId)))
            .WithName("GetCephalonBackendForFrontendRestDocumentsByClient");
        engineGroup.MapGet("/backend-for-frontend/rest-documents/{documentId}", (string documentId, [FromServices] IBackendForFrontendRestDocumentRuntimeCatalog catalog) =>
            {
                var document = catalog.GetById(documentId);

                return document is null ? Results.NotFound() : Results.Ok(document);
            })
            .WithName("GetCephalonBackendForFrontendRestDocument");
        engineGroup.MapGet("/backend-for-frontend/{bindingId}", (string bindingId, [FromServices] IBackendForFrontendRuntimeCatalog catalog) =>
            {
                var binding = catalog.GetById(bindingId);

                return binding is null ? Results.NotFound() : Results.Ok(binding);
            })
            .WithName("GetCephalonBackendForFrontendBinding");
        engineGroup.MapGet("/patterns", (RuntimeManifest manifest) => TypedResults.Ok(manifest.AppProfile.Patterns))
            .WithName("GetCephalonPatterns");
        engineGroup.MapGet("/cells", ([FromServices] ICellBoundaryCatalog catalog) => TypedResults.Ok(catalog.CellBoundaries))
            .WithName("GetCephalonCellBoundaries");
        engineGroup.MapGet("/cells/modules/{moduleId}", (string moduleId, [FromServices] ICellBoundaryCatalog catalog) =>
                TypedResults.Ok(catalog.GetByModule(moduleId)))
            .WithName("GetCephalonCellBoundariesByModule");
        engineGroup.MapGet("/cells/{cellId}", (string cellId, [FromServices] ICellBoundaryCatalog catalog) =>
            {
                var cellBoundary = catalog.GetById(cellId);

                return cellBoundary is null ? Results.NotFound() : Results.Ok(cellBoundary);
            })
            .WithName("GetCephalonCellBoundary");
        engineGroup.MapGet("/cell-routes", ([FromServices] ICellRouteCatalog catalog) => TypedResults.Ok(catalog.Routes))
            .WithName("GetCephalonCellRoutes");
        engineGroup.MapGet("/cell-routes/modules/{moduleId}", (string moduleId, [FromServices] ICellRouteCatalog catalog) =>
                TypedResults.Ok(catalog.GetBySourceModule(moduleId)))
            .WithName("GetCephalonCellRoutesByModule");
        engineGroup.MapGet("/cell-routes/source-cells/{cellId}", (string cellId, [FromServices] ICellRouteCatalog catalog) =>
                TypedResults.Ok(catalog.GetBySourceCellId(cellId)))
            .WithName("GetCephalonCellRoutesBySourceCell");
        engineGroup.MapGet("/cell-routes/target-cells/{cellId}", (string cellId, [FromServices] ICellRouteCatalog catalog) =>
                TypedResults.Ok(catalog.GetByTargetCellId(cellId)))
            .WithName("GetCephalonCellRoutesByTargetCell");
        engineGroup.MapGet("/cell-routes/{routeId}", (string routeId, [FromServices] ICellRouteCatalog catalog) =>
            {
                var cellRoute = catalog.GetById(routeId);

                return cellRoute is null ? Results.NotFound() : Results.Ok(cellRoute);
            })
            .WithName("GetCephalonCellRoute");
        engineGroup.MapGet("/cell-health-isolations", ([FromServices] ICellHealthIsolationCatalog catalog) => TypedResults.Ok(catalog.HealthIsolations))
            .WithName("GetCephalonCellHealthIsolations");
        engineGroup.MapGet("/cell-health-isolations/modules/{moduleId}", (string moduleId, [FromServices] ICellHealthIsolationCatalog catalog) =>
                TypedResults.Ok(catalog.GetBySourceModule(moduleId)))
            .WithName("GetCephalonCellHealthIsolationsByModule");
        engineGroup.MapGet("/cell-health-isolations/cells/{cellId}", (string cellId, [FromServices] ICellHealthIsolationCatalog catalog) =>
                TypedResults.Ok(catalog.GetByCellId(cellId)))
            .WithName("GetCephalonCellHealthIsolationsByCell");
        engineGroup.MapGet("/cell-health-isolations/dependencies/{dependencyId}", (string dependencyId, [FromServices] ICellHealthIsolationCatalog catalog) =>
                TypedResults.Ok(catalog.GetByDependencyId(dependencyId)))
            .WithName("GetCephalonCellHealthIsolationsByDependency");
        engineGroup.MapGet("/cell-health-isolations/{healthIsolationId}", (string healthIsolationId, [FromServices] ICellHealthIsolationCatalog catalog) =>
            {
                var healthIsolation = catalog.GetById(healthIsolationId);

                return healthIsolation is null ? Results.NotFound() : Results.Ok(healthIsolation);
            })
            .WithName("GetCephalonCellHealthIsolation");
        engineGroup.MapGet("/cell-traffic-automations", ([FromServices] ICellTrafficAutomationRuntimeCatalog catalog) =>
                TypedResults.Ok(catalog.Automations))
            .WithName("GetCephalonCellTrafficAutomations");
        engineGroup.MapGet("/cell-traffic-automations/modules/{moduleId}", (string moduleId, [FromServices] ICellTrafficAutomationRuntimeCatalog catalog) =>
                TypedResults.Ok(catalog.GetBySourceModule(moduleId)))
            .WithName("GetCephalonCellTrafficAutomationsByModule");
        engineGroup.MapGet("/cell-traffic-automations/routes/{routeId}", (string routeId, [FromServices] ICellTrafficAutomationRuntimeCatalog catalog) =>
            {
                var automation = catalog.GetByRouteId(routeId);

                return automation is null ? Results.NotFound() : Results.Ok(automation);
            })
            .WithName("GetCephalonCellTrafficAutomationByRoute");
        engineGroup.MapGet("/cell-traffic-automations/source-cells/{cellId}", (string cellId, [FromServices] ICellTrafficAutomationRuntimeCatalog catalog) =>
                TypedResults.Ok(catalog.GetBySourceCellId(cellId)))
            .WithName("GetCephalonCellTrafficAutomationsBySourceCell");
        engineGroup.MapGet("/cell-traffic-automations/target-cells/{cellId}", (string cellId, [FromServices] ICellTrafficAutomationRuntimeCatalog catalog) =>
                TypedResults.Ok(catalog.GetByTargetCellId(cellId)))
            .WithName("GetCephalonCellTrafficAutomationsByTargetCell");
        engineGroup.MapGet("/cell-traffic-automations/providers/{providerId}", (string providerId, [FromServices] ICellTrafficAutomationRuntimeCatalog catalog) =>
                TypedResults.Ok(catalog.GetByProvider(providerId)))
            .WithName("GetCephalonCellTrafficAutomationsByProvider");
        engineGroup.MapGet("/cell-traffic-automations/edge-nodes/{edgeNodeId}", (string edgeNodeId, [FromServices] ICellTrafficAutomationRuntimeCatalog catalog) =>
                TypedResults.Ok(catalog.GetByEdgeNodeId(edgeNodeId)))
            .WithName("GetCephalonCellTrafficAutomationsByEdgeNode");
        engineGroup.MapGet("/cell-traffic-automations/health-isolations/{healthIsolationId}", (string healthIsolationId, [FromServices] ICellTrafficAutomationRuntimeCatalog catalog) =>
                TypedResults.Ok(catalog.GetByHealthIsolationId(healthIsolationId)))
            .WithName("GetCephalonCellTrafficAutomationsByHealthIsolation");
        engineGroup.MapGet("/cell-traffic-automations/{automationId}", (string automationId, [FromServices] ICellTrafficAutomationRuntimeCatalog catalog) =>
            {
                var automation = catalog.GetById(automationId);

                return automation is null ? Results.NotFound() : Results.Ok(automation);
            })
            .WithName("GetCephalonCellTrafficAutomation");
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

        var stranglerFigCutoverCatalog = app.Services.GetService<AspNetCoreStranglerFigCutoverCatalog>();
        if (stranglerFigCutoverCatalog?.HasActiveHandlers == true)
        {
            app.UseMiddleware<AspNetCoreStranglerFigCutoverMiddleware>();
        }

        app.UseRouting();

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

            MapBackendForFrontendRestOpenApiDocuments(app, openApiEndpointOptions);
            MapBackendForFrontendRestScalarSurfaces(
                app,
                openApiEndpointOptions,
                configuration,
                defaultOpenApiDocumentName);
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

    private static QueryString NormalizeOptionalQueryString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return QueryString.Empty;
        }

        var normalized = value.Trim();
        return new QueryString(normalized.StartsWith('?') ? normalized : "?" + normalized);
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

    private static string BuildVersionedScopedAssetReference(
        string route,
        string scopeParameterName,
        string scopeId)
    {
        return $"{route}?v={DocumentationAssetVersion}&{Uri.EscapeDataString(scopeParameterName)}={Uri.EscapeDataString(scopeId)}";
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

    private static void MapBackendForFrontendRestOpenApiDocuments(
        WebApplication app,
        OpenApiEndpointOptions openApiEndpointOptions)
    {
        var bindingRoutePattern = BackendForFrontendRestDocumentRoutes.BuildBindingOpenApiRoutePattern(openApiEndpointOptions.RoutePattern);
        var clientRoutePattern = BackendForFrontendRestDocumentRoutes.BuildClientOpenApiRoutePattern(openApiEndpointOptions.RoutePattern);

        app.MapGet(bindingRoutePattern, async (
                string bindingId,
                string documentName,
                [FromServices] AspNetCoreBackendForFrontendRestDocumentPublisher publisher,
                CancellationToken cancellationToken) =>
            {
                var payload = await publisher
                    .GenerateBindingDocumentAsync(bindingId, documentName, cancellationToken)
                    .ConfigureAwait(false);

                return payload is null
                    ? Results.NotFound()
                    : Results.Text(payload, "application/json");
            })
            .DisableRateLimiting()
            .ExcludeFromDescription();

        app.MapGet(clientRoutePattern, async (
                string clientId,
                string documentName,
                [FromServices] AspNetCoreBackendForFrontendRestDocumentPublisher publisher,
                CancellationToken cancellationToken) =>
            {
                var payload = await publisher
                    .GenerateClientDocumentAsync(clientId, documentName, cancellationToken)
                    .ConfigureAwait(false);

                return payload is null
                    ? Results.NotFound()
                    : Results.Text(payload, "application/json");
            })
            .DisableRateLimiting()
            .ExcludeFromDescription();
    }

    private static void MapBackendForFrontendRestScalarSurfaces(
        WebApplication app,
        OpenApiEndpointOptions openApiEndpointOptions,
        IConfiguration configuration,
        string defaultOpenApiDocumentName)
    {
        MapBackendForFrontendRestScalarSurface(
            app,
            openApiEndpointOptions,
            configuration,
            defaultOpenApiDocumentName,
            scopeParameterName: "bindingId",
            scalarRoutePrefix: BackendForFrontendRestDocumentRoutes.BuildBindingScalarPrefix(openApiEndpointOptions.ScalarRoutePrefix),
            getDocuments: static (catalog, scopeId) => catalog.GetByBindingId(scopeId),
            buildScopedOpenApiRoutePattern: static (routePattern, scopeId) =>
                BackendForFrontendRestDocumentRoutes
                    .BuildBindingOpenApiRoutePattern(routePattern)
                    .Replace("{bindingId}", Uri.EscapeDataString(scopeId.Trim()), StringComparison.Ordinal),
            resolveTitleSuffix: static (services, scopeId) =>
            {
                var bindings = services.GetRequiredService<IBackendForFrontendRuntimeCatalog>();
                return bindings.GetById(scopeId)?.DisplayName ?? scopeId;
            });

        MapBackendForFrontendRestScalarSurface(
            app,
            openApiEndpointOptions,
            configuration,
            defaultOpenApiDocumentName,
            scopeParameterName: "clientId",
            scalarRoutePrefix: BackendForFrontendRestDocumentRoutes.BuildClientScalarPrefix(openApiEndpointOptions.ScalarRoutePrefix),
            getDocuments: static (catalog, scopeId) => catalog.GetByClientId(scopeId),
            buildScopedOpenApiRoutePattern: static (routePattern, scopeId) =>
                BackendForFrontendRestDocumentRoutes
                    .BuildClientOpenApiRoutePattern(routePattern)
                    .Replace("{clientId}", Uri.EscapeDataString(scopeId.Trim()), StringComparison.Ordinal),
            resolveTitleSuffix: static (_, scopeId) => scopeId);
    }

    private static void MapBackendForFrontendRestScalarSurface(
        WebApplication app,
        OpenApiEndpointOptions openApiEndpointOptions,
        IConfiguration configuration,
        string defaultOpenApiDocumentName,
        string scopeParameterName,
        string scalarRoutePrefix,
        Func<IBackendForFrontendRestDocumentRuntimeCatalog, string, IReadOnlyList<BackendForFrontendRestDocumentRuntimeDescriptor>> getDocuments,
        Func<string, string, string> buildScopedOpenApiRoutePattern,
        Func<IServiceProvider, string, string> resolveTitleSuffix)
    {
        var openApiToggleScriptRoute = BuildScalarAssetRoute(scalarRoutePrefix, "openapi-toggle.js");
        var scalarFaviconRoute = BuildScalarAssetRoute(scalarRoutePrefix, "assets/favicon.svg");
        var scalarFaviconReference = BuildVersionedAssetReference(scalarFaviconRoute);

        app.MapGet(
                openApiToggleScriptRoute,
                (HttpContext httpContext, [FromServices] IBackendForFrontendRestDocumentRuntimeCatalog catalog) =>
                {
                    var scopeId = httpContext.Request.Query[scopeParameterName].ToString();
                    var documents = string.IsNullOrWhiteSpace(scopeId)
                        ? []
                        : getDocuments(catalog, scopeId);
                    var defaultDocumentName = documents.Count > 0
                        ? documents[0].DocumentName
                        : defaultOpenApiDocumentName;
                    var documentNames = documents
                        .Select(static document => document.DocumentName)
                        .ToArray();

                    return Results.Text(
                        RenderOpenApiToggleScript(
                            scalarRoutePrefix,
                            documentNames,
                            defaultDocumentName),
                        "application/javascript");
                })
            .DisableRateLimiting()
            .ExcludeFromDescription();
        app.MapGet(scalarFaviconRoute, () => Results.Text(ScalarFavicon.Value, "image/svg+xml"))
            .DisableRateLimiting()
            .ExcludeFromDescription();
        app.UseWhen(
            context =>
                (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method)) &&
                context.Request.Path == scalarRoutePrefix,
            branch => branch.Run(context =>
            {
                var scopeId = context.Request.Query[scopeParameterName].ToString();
                if (string.IsNullOrWhiteSpace(scopeId))
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return Task.CompletedTask;
                }

                var catalog = context.RequestServices.GetRequiredService<IBackendForFrontendRestDocumentRuntimeCatalog>();
                var documents = getDocuments(catalog, scopeId);
                if (documents.Count == 0)
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return Task.CompletedTask;
                }

                context.Response.Redirect(
                    BackendForFrontendRestDocumentRoutes.AppendScopedQueryString(
                        BuildScalarCanonicalPath(
                            scalarRoutePrefix,
                            documents[0].DocumentName,
                            QueryString.Empty),
                        scopeParameterName,
                        scopeId,
                        context.Request.QueryString));
                return Task.CompletedTask;
            }));
        app.MapScalarApiReference(scalarRoutePrefix, (options, httpContext) =>
        {
            var scopeId = httpContext.Request.Query[scopeParameterName].ToString();
            var catalog = httpContext.RequestServices.GetRequiredService<IBackendForFrontendRestDocumentRuntimeCatalog>();
            var documents = string.IsNullOrWhiteSpace(scopeId)
                ? []
                : getDocuments(catalog, scopeId);
            var defaultDocument = documents.Count > 0
                ? documents[0].DocumentName
                : defaultOpenApiDocumentName;
            var title = ResolveRestDocsText(
                httpContext.RequestServices,
                configurationKey: "OpenApi:Title",
                localizationKey: "engine.docs.scalar.title",
                fallbackValue: "Cephalon REST API");

            if (!string.IsNullOrWhiteSpace(scopeId))
            {
                title = $"{title} ({resolveTitleSuffix(httpContext.RequestServices, scopeId)})";
                options.WithJavaScriptConfiguration(
                    BuildVersionedScopedAssetReference(
                        openApiToggleScriptRoute,
                        scopeParameterName,
                        scopeId.Trim()));
                options.WithOpenApiRoutePattern(buildScopedOpenApiRoutePattern(openApiEndpointOptions.RoutePattern, scopeId));
            }
            else
            {
                options.WithJavaScriptConfiguration(BuildVersionedAssetReference(openApiToggleScriptRoute));
                options.WithOpenApiRoutePattern(openApiEndpointOptions.RoutePattern);
            }

            options.WithTitle(title);
            options.WithFavicon(scalarFaviconReference);

            foreach (var document in documents)
            {
                options.AddDocument(
                    document.DocumentName,
                    isDefault: string.Equals(
                        document.DocumentName,
                        defaultDocument,
                        StringComparison.OrdinalIgnoreCase));
            }
        })
        .DisableRateLimiting();
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
