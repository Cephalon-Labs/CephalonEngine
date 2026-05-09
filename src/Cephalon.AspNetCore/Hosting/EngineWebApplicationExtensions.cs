using Cephalon.AspNetCore.Documentation;
using Cephalon.AspNetCore.Diagnostics;
using Cephalon.AspNetCore.Health;
using Cephalon.AspNetCore;
using Cephalon.Abstractions.AppModel;
using Cephalon.Abstractions.Audit;
using Cephalon.Abstractions.Authorization;
using Cephalon.Abstractions.Agentics;
using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Execution;
using Cephalon.Abstractions.Features;
using Cephalon.Abstractions.Localization;
using Cephalon.Abstractions.Patterns;
using Cephalon.Abstractions.Retrieval;
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
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Text.Json;

namespace Cephalon.AspNetCore.Hosting;

/// <summary>
/// Maps the operator-facing HTTP surface exposed by a Cephalon ASP.NET Core host.
/// </summary>
public static class EngineWebApplicationExtensions
{
    private const string DynamicRouteMappingRequiresUnreferencedCodeMessage =
        "Cephalon.AspNetCore maps operator endpoints through ASP.NET Core Minimal API delegate binding. " +
        "The current full operator surface is not a trim-safe support claim; use it only when reflection-based route binding is acceptable.";
    private const string DynamicRouteMappingRequiresDynamicCodeMessage =
        "Cephalon.AspNetCore maps operator endpoints through ASP.NET Core Minimal API delegate binding. " +
        "The current full operator surface is not a Native AOT support claim; use it only when dynamic route binding is acceptable.";
    private const string OpenApiToggleScriptResourceName = "Cephalon.AspNetCore.Assets.openapi-toggle.js";
    private const string ScalarFaviconResourceName = "Cephalon.AspNetCore.Assets.docs-favicon.svg";
    private const string ScalarRoutePrefixToken = "__CEPHALON_SCALAR_ROUTE_PREFIX__";
    private const string ScalarDocumentNamesToken = "__CEPHALON_SCALAR_DOCUMENT_NAMES__";
    private const string ScalarDefaultDocumentNameToken = "__CEPHALON_SCALAR_DEFAULT_DOCUMENT_NAME__";
    private const string OperatorSurfaceModeConfigurationKey = "Engine:AspNetCore:OperatorSurface:Mode";
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
    /// <para>
    /// The current full operator route surface uses ASP.NET Core Minimal API delegate binding, which is
    /// not a trim or Native AOT support claim for this package. The annotation is intentional so package-local
    /// analyzer builds report the boundary where consumers would otherwise receive framework warnings.
    /// </para>
    /// <para>
    /// Setting <c>Engine:AspNetCore:OperatorSurface:Mode</c> to <c>core</c> maps the bounded core
    /// operator routes through prebuilt request delegates instead of Minimal API delegate binding. That mode
    /// reduces the dynamic route boundary for the core route subset, but it does not make the full adapter
    /// surface a trim or Native AOT support claim.
    /// </para>
    /// </remarks>
    [RequiresUnreferencedCode(DynamicRouteMappingRequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(DynamicRouteMappingRequiresDynamicCodeMessage)]
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
        if (ResolveOperatorSurfaceMode(configuration) == AspNetCoreOperatorSurfaceMode.Core)
        {
            MapCephalonCoreOperatorRoutes(engineGroup, referenceDocsSurface);
            MapCephalonHostInfrastructure(
                app,
                runtime,
                configuration,
                referenceDocsOptions,
                referenceDocsSurface,
                openApiEndpointOptions,
                openApiDocumentNames,
                defaultOpenApiDocumentName,
                openApiToggleScriptRoute,
                scalarFaviconRoute,
                openApiToggleScriptReference,
                scalarFaviconReference,
                restApiSelected);

            return app;
        }

        MapCephalonFullCommonOperatorRoutes(engineGroup);

        MapGetResultRequestDelegate(engineGroup, "/behavior-resilience", "GetCephalonBehaviorResilience", static httpContext =>
            {
                var catalog = httpContext.RequestServices.GetService<Cephalon.Abstractions.Resilience.IBehaviorResilienceRuntimeCatalog>();
                return TypedResults.Ok(catalog?.Policies ?? []);
            });
        MapGetResultRequestDelegate(engineGroup, "/behavior-resilience/{policyId}", "GetCephalonBehaviorResiliencePolicy", static httpContext =>
            {
                var policyId = GetRouteValue(httpContext, "policyId");
                var catalog = httpContext.RequestServices.GetService<Cephalon.Abstractions.Resilience.IBehaviorResilienceRuntimeCatalog>();
                var policy = catalog?.GetById(policyId);

                return policy is null ? Results.NotFound() : Results.Ok(policy);
            });
        MapGetResultRequestDelegate(engineGroup, "/saga-choreographies", "GetCephalonSagaChoreographies", static httpContext =>
            {
                var catalog = httpContext.RequestServices.GetService<ISagaChoreographyRuntimeCatalog>();
                return TypedResults.Ok(catalog?.SagaChoreographies ?? []);
            });
        MapGetResultRequestDelegate(engineGroup, "/saga-choreographies/modules/{moduleId}", "GetCephalonSagaChoreographiesByModule", static httpContext =>
            {
                var moduleId = GetRouteValue(httpContext, "moduleId");
                var catalog = httpContext.RequestServices.GetService<ISagaChoreographyRuntimeCatalog>();
                return TypedResults.Ok(catalog?.GetBySourceModule(moduleId) ?? []);
            });
        MapGetResultRequestDelegate(engineGroup, "/saga-choreographies/transports/{transportId}", "GetCephalonSagaChoreographiesByTransport", static httpContext =>
            {
                var transportId = GetRouteValue(httpContext, "transportId");
                var catalog = httpContext.RequestServices.GetService<ISagaChoreographyRuntimeCatalog>();
                return TypedResults.Ok(catalog?.GetByTransportId(transportId) ?? []);
            });
        MapGetResultRequestDelegate(engineGroup, "/saga-choreographies/{behaviorId}", "GetCephalonSagaChoreography", static httpContext =>
            {
                var behaviorId = GetRouteValue(httpContext, "behaviorId");
                var catalog = httpContext.RequestServices.GetService<ISagaChoreographyRuntimeCatalog>();
                var choreography = catalog?.GetById(behaviorId);

                return choreography is null ? Results.NotFound() : Results.Ok(choreography);
            });
        MapGetResultRequestDelegate(engineGroup, "/saga-choreographies/runtime", "GetCephalonSagaChoreographyPublicationStates", static httpContext =>
            {
                var catalog = httpContext.RequestServices.GetService<ISagaChoreographyPublicationRuntimeStateCatalog>();
                return TypedResults.Ok(catalog?.States ?? []);
            });
        MapGetResultRequestDelegate(engineGroup, "/saga-choreographies/runtime/behaviors/{behaviorId}", "GetCephalonSagaChoreographyPublicationStatesByBehavior", static httpContext =>
            {
                var behaviorId = GetRouteValue(httpContext, "behaviorId");
                var catalog = httpContext.RequestServices.GetService<ISagaChoreographyPublicationRuntimeStateCatalog>();
                return TypedResults.Ok(catalog?.GetByBehaviorId(behaviorId) ?? []);
            });
        MapGetResultRequestDelegate(engineGroup, "/saga-choreographies/runtime/modules/{moduleId}", "GetCephalonSagaChoreographyPublicationStatesByModule", static httpContext =>
            {
                var moduleId = GetRouteValue(httpContext, "moduleId");
                var catalog = httpContext.RequestServices.GetService<ISagaChoreographyPublicationRuntimeStateCatalog>();
                return TypedResults.Ok(catalog?.GetBySourceModule(moduleId) ?? []);
            });
        MapGetResultRequestDelegate(engineGroup, "/saga-choreographies/runtime/transports/{transportId}", "GetCephalonSagaChoreographyPublicationStatesByTransport", static httpContext =>
            {
                var transportId = GetRouteValue(httpContext, "transportId");
                var catalog = httpContext.RequestServices.GetService<ISagaChoreographyPublicationRuntimeStateCatalog>();
                return TypedResults.Ok(catalog?.GetByTransportId(transportId) ?? []);
            });
        MapGetResultRequestDelegate(engineGroup, "/saga-choreographies/runtime/channels/{channelId}", "GetCephalonSagaChoreographyPublicationStatesByChannel", static httpContext =>
            {
                var channelId = GetRouteValue(httpContext, "channelId");
                var catalog = httpContext.RequestServices.GetService<ISagaChoreographyPublicationRuntimeStateCatalog>();
                return TypedResults.Ok(catalog?.GetByChannelId(channelId) ?? []);
            });
        MapGetResultRequestDelegate(engineGroup, "/saga-choreographies/runtime/correlations/{correlationId}", "GetCephalonSagaChoreographyPublicationStatesByCorrelation", static httpContext =>
            {
                var correlationId = GetRouteValue(httpContext, "correlationId");
                var catalog = httpContext.RequestServices.GetService<ISagaChoreographyPublicationRuntimeStateCatalog>();
                return TypedResults.Ok(catalog?.GetByCorrelationId(correlationId) ?? []);
            });
        MapGetResultRequestDelegate(engineGroup, "/saga-choreographies/runtime/compensations", "GetCephalonSagaChoreographyCompensationPublicationStates", static httpContext =>
            {
                var catalog = httpContext.RequestServices.GetService<ISagaChoreographyPublicationRuntimeStateCatalog>();
                return TypedResults.Ok(catalog?.GetCompensationPublications() ?? []);
            });
        MapGetResultRequestDelegate(engineGroup, "/saga-choreographies/runtime/failures", "GetCephalonSagaChoreographyFailedPublicationStates", static httpContext =>
            {
                var catalog = httpContext.RequestServices.GetService<ISagaChoreographyPublicationRuntimeStateCatalog>();
                return TypedResults.Ok(catalog?.GetFailedPublications() ?? []);
            });
        MapGetResultRequestDelegate(engineGroup, "/saga-choreographies/runtime/publications/{publicationStateId}", "GetCephalonSagaChoreographyPublicationState", static httpContext =>
            {
                var publicationStateId = GetRouteValue(httpContext, "publicationStateId");
                var catalog = httpContext.RequestServices.GetService<ISagaChoreographyPublicationRuntimeStateCatalog>();
                var state = catalog?.GetById(publicationStateId);

                return state is null ? Results.NotFound() : Results.Ok(state);
            });
        MapGetResultRequestDelegate(engineGroup, "/durable-executions", "GetCephalonDurableExecutions", static httpContext =>
            {
                var catalog = httpContext.RequestServices.GetService<IDurableExecutionRuntimeCatalog>();
                return TypedResults.Ok(catalog?.DurableExecutions ?? []);
            });
        MapGetResultRequestDelegate(engineGroup, "/durable-executions/modules/{moduleId}", "GetCephalonDurableExecutionsByModule", static httpContext =>
            {
                var moduleId = GetRouteValue(httpContext, "moduleId");
                var catalog = httpContext.RequestServices.GetService<IDurableExecutionRuntimeCatalog>();
                return TypedResults.Ok(catalog?.GetBySourceModule(moduleId) ?? []);
            });
        MapGetResultRequestDelegate(engineGroup, "/durable-executions/transports/{transportId}", "GetCephalonDurableExecutionsByTransport", static httpContext =>
            {
                var transportId = GetRouteValue(httpContext, "transportId");
                var catalog = httpContext.RequestServices.GetService<IDurableExecutionRuntimeCatalog>();
                return TypedResults.Ok(catalog?.GetByTransportId(transportId) ?? []);
            });
        MapGetResultRequestDelegate(engineGroup, "/durable-executions/{behaviorId}", "GetCephalonDurableExecution", static httpContext =>
            {
                var behaviorId = GetRouteValue(httpContext, "behaviorId");
                var catalog = httpContext.RequestServices.GetService<IDurableExecutionRuntimeCatalog>();
                var durableExecution = catalog?.GetById(behaviorId);

                return durableExecution is null ? Results.NotFound() : Results.Ok(durableExecution);
            });
        MapGetResultRequestDelegate(engineGroup, "/durable-executions/runtime", "GetCephalonDurableExecutionStates", static httpContext =>
            {
                var catalog = httpContext.RequestServices.GetService<IDurableExecutionRuntimeStateCatalog>();
                return TypedResults.Ok(catalog?.States ?? []);
            });
        MapGetResultRequestDelegate(engineGroup, "/durable-executions/runtime/behaviors/{behaviorId}", "GetCephalonDurableExecutionStatesByBehavior", static httpContext =>
            {
                var behaviorId = GetRouteValue(httpContext, "behaviorId");
                var catalog = httpContext.RequestServices.GetService<IDurableExecutionRuntimeStateCatalog>();
                return TypedResults.Ok(catalog?.GetByBehaviorId(behaviorId) ?? []);
            });
        MapGetResultRequestDelegate(engineGroup, "/durable-executions/runtime/modules/{moduleId}", "GetCephalonDurableExecutionStatesByModule", static httpContext =>
            {
                var moduleId = GetRouteValue(httpContext, "moduleId");
                var catalog = httpContext.RequestServices.GetService<IDurableExecutionRuntimeStateCatalog>();
                return TypedResults.Ok(catalog?.GetBySourceModule(moduleId) ?? []);
            });
        MapGetResultRequestDelegate(engineGroup, "/durable-executions/runtime/transports/{transportId}", "GetCephalonDurableExecutionStatesByTransport", static httpContext =>
            {
                var transportId = GetRouteValue(httpContext, "transportId");
                var catalog = httpContext.RequestServices.GetService<IDurableExecutionRuntimeStateCatalog>();
                return TypedResults.Ok(catalog?.GetByTransportId(transportId) ?? []);
            });
        MapGetResultRequestDelegate(engineGroup, "/durable-executions/runtime/timers", "GetCephalonDurableExecutionStatesWithPendingTimers", static httpContext =>
            {
                var catalog = httpContext.RequestServices.GetService<IDurableExecutionRuntimeStateCatalog>();
                return TypedResults.Ok(catalog?.GetWithPendingTimers() ?? []);
            });
        MapGetResultRequestDelegate(engineGroup, "/durable-executions/runtime/timers/{timerId}", "GetCephalonDurableExecutionStatesByPendingTimer", static httpContext =>
            {
                var timerId = GetRouteValue(httpContext, "timerId");
                var catalog = httpContext.RequestServices.GetService<IDurableExecutionRuntimeStateCatalog>();
                return TypedResults.Ok(catalog?.GetByPendingTimerId(timerId) ?? []);
            });
        MapGetResultRequestDelegate(engineGroup, "/durable-executions/runtime/signals", "GetCephalonDurableExecutionStatesWithPendingSignals", static httpContext =>
            {
                var catalog = httpContext.RequestServices.GetService<IDurableExecutionRuntimeStateCatalog>();
                return TypedResults.Ok(catalog?.GetWithPendingSignals() ?? []);
            });
        MapGetResultRequestDelegate(engineGroup, "/durable-executions/runtime/signals/{signalId}", "GetCephalonDurableExecutionStatesByPendingSignal", static httpContext =>
            {
                var signalId = GetRouteValue(httpContext, "signalId");
                var catalog = httpContext.RequestServices.GetService<IDurableExecutionRuntimeStateCatalog>();
                return TypedResults.Ok(catalog?.GetByPendingSignalId(signalId) ?? []);
            });
        MapGetResultRequestDelegate(engineGroup, "/durable-executions/runtime/compensations", "GetCephalonDurableExecutionStatesWithCompensationActions", static httpContext =>
            {
                var catalog = httpContext.RequestServices.GetService<IDurableExecutionRuntimeStateCatalog>();
                return TypedResults.Ok(catalog?.GetWithCompensationActions() ?? []);
            });
        MapGetResultRequestDelegate(engineGroup, "/durable-executions/runtime/compensations/{compensationId}", "GetCephalonDurableExecutionStatesByCompensationAction", static httpContext =>
            {
                var compensationId = GetRouteValue(httpContext, "compensationId");
                var catalog = httpContext.RequestServices.GetService<IDurableExecutionRuntimeStateCatalog>();
                return TypedResults.Ok(catalog?.GetByCompensationActionId(compensationId) ?? []);
            });
        MapGetResultRequestDelegate(engineGroup, "/durable-executions/runtime/streams/{streamId}", "GetCephalonDurableExecutionState", static httpContext =>
            {
                var streamId = GetRouteValue(httpContext, "streamId");
                var catalog = httpContext.RequestServices.GetService<IDurableExecutionRuntimeStateCatalog>();
                var state = catalog?.GetByStreamId(streamId);

                return state is null ? Results.NotFound() : Results.Ok(state);
            });
        MapGetResultRequestDelegate(engineGroup, "/rate-limiting", "GetCephalonRateLimiting", static context =>
            TypedResults.Ok(GetRequiredService<IRateLimitingRuntimeCatalog>(context).Policies));
        MapGetResultRequestDelegate(engineGroup, "/rate-limiting/{policyId}", "GetCephalonRateLimitingPolicy", static context =>
            {
                var policyId = GetRouteValue(context, "policyId");
                var catalog = GetRequiredService<IRateLimitingRuntimeCatalog>(context);
                var policy = catalog.GetById(policyId);

                return policy is null ? Results.NotFound() : Results.Ok(policy);
            });
        MapGetResultRequestDelegate(engineGroup, "/rest-endpoints", "GetCephalonRestEndpoints", static context =>
            TypedResults.Ok(GetRequiredService<IRestEndpointRuntimeCatalog>(context).Endpoints));
        MapGetResultRequestDelegate(engineGroup, "/rest-endpoints/{restEndpointId}", "GetCephalonRestEndpoint", static context =>
            {
                var restEndpointId = GetRouteValue(context, "restEndpointId");
                var catalog = GetRequiredService<IRestEndpointRuntimeCatalog>(context);
                var endpoint = catalog.GetById(restEndpointId);

                return endpoint is null ? Results.NotFound() : Results.Ok(endpoint);
            });
        MapGetResultRequestDelegate(engineGroup, "/rest-endpoint-candidates", "GetCephalonRestEndpointCandidates", static context =>
            TypedResults.Ok(GetRequiredService<IRestEndpointCandidateRuntimeCatalog>(context).Candidates));
        MapGetResultRequestDelegate(engineGroup, "/rest-endpoint-candidates/{candidateId}", "GetCephalonRestEndpointCandidate", static context =>
            {
                var candidateId = GetRouteValue(context, "candidateId");
                var catalog = GetRequiredService<IRestEndpointCandidateRuntimeCatalog>(context);
                var candidate = catalog.GetById(candidateId);

                return candidate is null ? Results.NotFound() : Results.Ok(candidate);
            });
        MapGetResultRequestDelegate(engineGroup, "/rest-endpoint-authoring-policies", "GetCephalonRestEndpointAuthoringPolicies", static context =>
            TypedResults.Ok(GetRequiredService<IRestEndpointAuthoringPolicyRuntimeCatalog>(context).Policies));
        MapGetResultRequestDelegate(engineGroup, "/rest-endpoint-authoring-policies/{behaviorId}", "GetCephalonRestEndpointAuthoringPolicy", static context =>
            {
                var behaviorId = GetRouteValue(context, "behaviorId");
                var catalog = GetRequiredService<IRestEndpointAuthoringPolicyRuntimeCatalog>(context);
                var policy = catalog.GetByBehaviorId(behaviorId);

                return policy is null ? Results.NotFound() : Results.Ok(policy);
            });
        MapGetResultRequestDelegate(engineGroup, "/rest-endpoint-publication-groups", "GetCephalonRestEndpointPublicationGroups", static context =>
            TypedResults.Ok(GetRequiredService<IRestEndpointPublicationGroupRuntimeCatalog>(context).Groups));
        MapGetResultRequestDelegate(engineGroup, "/rest-endpoint-publication-groups/{behaviorId}", "GetCephalonRestEndpointPublicationGroup", static context =>
            {
                var behaviorId = GetRouteValue(context, "behaviorId");
                var catalog = GetRequiredService<IRestEndpointPublicationGroupRuntimeCatalog>(context);
                var group = catalog.GetByBehaviorId(behaviorId);

                return group is null ? Results.NotFound() : Results.Ok(group);
            });
        MapGetResultRequestDelegate(engineGroup, "/rest-endpoint-overrides", "GetCephalonRestEndpointOverrides", static context =>
            TypedResults.Ok(GetRequiredService<IRestEndpointOverrideRuntimeCatalog>(context).OverrideRules));
        MapGetResultRequestDelegate(engineGroup, "/rest-endpoint-overrides/{overrideId}", "GetCephalonRestEndpointOverride", static context =>
            {
                var overrideId = GetRouteValue(context, "overrideId");
                var catalog = GetRequiredService<IRestEndpointOverrideRuntimeCatalog>(context);
                var restEndpointOverride = catalog.GetById(overrideId);

                return restEndpointOverride is null ? Results.NotFound() : Results.Ok(restEndpointOverride);
            });
        MapGetResultRequestDelegate(engineGroup, "/rest-endpoint-suppressions", "GetCephalonRestEndpointSuppressions", static context =>
            TypedResults.Ok(GetRequiredService<IRestEndpointSuppressionRuntimeCatalog>(context).Suppressions));
        MapGetResultRequestDelegate(engineGroup, "/rest-endpoint-suppressions/{suppressionId}", "GetCephalonRestEndpointSuppression", static context =>
            {
                var suppressionId = GetRouteValue(context, "suppressionId");
                var catalog = GetRequiredService<IRestEndpointSuppressionRuntimeCatalog>(context);
                var suppression = catalog.GetById(suppressionId);

                return suppression is null ? Results.NotFound() : Results.Ok(suppression);
            });
        MapGetResultRequestDelegate(engineGroup, "/databases", "GetCephalonDatabases", static context =>
            TypedResults.Ok(GetRequiredService<RuntimeManifest>(context).AppProfile.Databases));
        MapGetResultRequestDelegate(engineGroup, "/database-topology", "GetCephalonDatabaseTopology", static context =>
            TypedResults.Ok(GetRequiredService<IDatabaseTopologyOperationalSnapshotProvider>(context).CreateSnapshot()));
        MapGetResultRequestDelegate(engineGroup, "/database-roles", "GetCephalonDatabaseRoles", static context =>
            TypedResults.Ok(GetRequiredService<IDatabaseRoleCatalog>(context).DatabaseRoles));
        MapGetResultRequestDelegate(engineGroup, "/database-roles/{databaseRoleId}", "GetCephalonDatabaseRole", static context =>
            {
                var databaseRoleId = GetRouteValue(context, "databaseRoleId");
                var catalog = GetRequiredService<IDatabaseRoleCatalog>(context);
                var databaseRole = catalog.GetById(databaseRoleId);

                return databaseRole is null ? Results.NotFound() : Results.Ok(databaseRole);
            });
        MapGetResultRequestDelegate(engineGroup, "/database-migrations", "GetCephalonDatabaseMigrations", static context =>
            TypedResults.Ok(GetRequiredService<IDatabaseMigrationCatalog>(context).DatabaseMigrations));
        MapGetResultRequestDelegate(engineGroup, "/database-migrations/{databaseMigrationId}", "GetCephalonDatabaseMigration", static context =>
            {
                var databaseMigrationId = GetRouteValue(context, "databaseMigrationId");
                var catalog = GetRequiredService<IDatabaseMigrationCatalog>(context);
                var databaseMigration = catalog.GetById(databaseMigrationId);

                return databaseMigration is null ? Results.NotFound() : Results.Ok(databaseMigration);
            });
        MapGetResultRequestDelegate(engineGroup, "/database-migration-playbook", "GetCephalonDatabaseMigrationPlaybook", static context =>
            TypedResults.Ok(GetRequiredService<IDatabaseMigrationOperationalPlaybookProvider>(context).CreatePlaybook()));
        MapGetResultRequestDelegate(engineGroup, "/scaffold", "GetCephalonScaffold", static context =>
            {
                var scaffold = GetRequiredService<RuntimeManifest>(context).AppProfile.Scaffold;

                return scaffold is null ? Results.NotFound() : Results.Ok(scaffold);
            });
        MapGetResultRequestDelegate(engineGroup, "/capabilities", "GetCephalonCapabilities", static context =>
            TypedResults.Ok(GetRequiredService<RuntimeManifest>(context).Capabilities));
        MapGetResultRequestDelegate(engineGroup, "/modules", "GetCephalonModules", static context =>
            TypedResults.Ok(GetRequiredService<RuntimeManifest>(context).Modules));
        MapGetResultRequestDelegate(engineGroup, "/packages", "GetCephalonPackages", static context =>
            TypedResults.Ok(GetRequiredService<RuntimeManifest>(context).Packages));
        MapGetResultRequestDelegate(engineGroup, "/hosted-executions", "GetCephalonHostedExecutions", static context =>
            TypedResults.Ok(GetRequiredService<IHostedExecutionRuntimeCatalog>(context).HostedExecutions));
        MapGetResultRequestDelegate(engineGroup, "/hosted-executions/{hostedExecutionId}", "GetCephalonHostedExecution", static context =>
            {
                var hostedExecutionId = GetRouteValue(context, "hostedExecutionId");
                var catalog = GetRequiredService<IHostedExecutionRuntimeCatalog>(context);
                var hostedExecution = catalog.GetById(hostedExecutionId);

                return hostedExecution is null ? Results.NotFound() : Results.Ok(hostedExecution);
            });
        MapGetResultRequestDelegate(engineGroup, "/execution-graphs", "GetCephalonExecutionGraphs", static context =>
            TypedResults.Ok(GetRequiredService<IExecutionRuntimeCatalog>(context).Graphs));
        MapGetResultRequestDelegate(engineGroup, "/execution-graphs/{graphId}", "GetCephalonExecutionGraph", static context =>
            {
                var graphId = GetRouteValue(context, "graphId");
                var catalog = GetRequiredService<IExecutionRuntimeCatalog>(context);
                var graph = catalog.GetById(graphId);

                return graph is null ? Results.NotFound() : Results.Ok(graph);
            });
        MapGetResultRequestDelegate(engineGroup, "/data-products", "GetCephalonDataProducts", static context =>
            TypedResults.Ok(GetRequiredService<IDataProductCatalog>(context).DataProducts));
        MapGetResultRequestDelegate(engineGroup, "/data-products/{dataProductId}", "GetCephalonDataProduct", static context =>
            {
                var dataProductId = GetRouteValue(context, "dataProductId");
                var catalog = GetRequiredService<IDataProductCatalog>(context);
                var dataProduct = catalog.GetById(dataProductId);

                return dataProduct is null ? Results.NotFound() : Results.Ok(dataProduct);
            });
        MapGetResultRequestDelegate(engineGroup, "/cdc-captures", "GetCephalonCdcCaptures", static context =>
            TypedResults.Ok(GetRequiredService<ICdcCaptureCatalog>(context).CdcCaptures));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes", "GetCephalonCdcCaptureRuntimes", static (_, catalog) =>
            catalog.Runtimes);
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/reporters/{reporterId}", "GetCephalonCdcCaptureRuntimesByReporter", static (context, catalog) =>
            catalog.GetByReporterId(GetRouteValue(context, "reporterId")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/edge-nodes/{edgeNodeId}", "GetCephalonCdcCaptureRuntimesByEdgeNode", static (context, catalog) =>
            catalog.GetByEdgeNodeId(GetRouteValue(context, "edgeNodeId")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/reporter-coordination/{coordinationState}", "GetCephalonCdcCaptureRuntimesByReporterCoordinationState", static (context, catalog) =>
            catalog.GetByReporterCoordinationState(GetRouteValue(context, "coordinationState")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/reporter-coordination/issues/{degradedReason}", "GetCephalonCdcCaptureRuntimesByReporterCoordinationIssueReason", static (context, catalog) =>
            catalog.GetByReporterCoordinationIssueReason(GetRouteValue(context, "degradedReason")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/remediation/{remediationState}", "GetCephalonCdcCaptureRuntimesByRemediationState", static (context, catalog) =>
            catalog.GetByRemediationState(GetRouteValue(context, "remediationState")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/remediation/categories/{remediationCategory}", "GetCephalonCdcCaptureRuntimesByRemediationCategory", static (context, catalog) =>
            catalog.GetByRemediationCategory(GetRouteValue(context, "remediationCategory")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/governance/{governanceState}", "GetCephalonCdcCaptureRuntimesByManagedConnectorGovernanceState", static (context, catalog) =>
            catalog.GetByManagedConnectorGovernanceState(GetRouteValue(context, "governanceState")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/governance/categories/{governanceCategory}", "GetCephalonCdcCaptureRuntimesByManagedConnectorGovernanceCategory", static (context, catalog) =>
            catalog.GetByManagedConnectorGovernanceCategory(GetRouteValue(context, "governanceCategory")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/drift/{driftState}", "GetCephalonCdcCaptureRuntimesByManagedConnectorDriftState", static (context, catalog) =>
            catalog.GetByManagedConnectorDriftState(GetRouteValue(context, "driftState")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/drift/categories/{driftCategory}", "GetCephalonCdcCaptureRuntimesByManagedConnectorDriftCategory", static (context, catalog) =>
            catalog.GetByManagedConnectorDriftCategory(GetRouteValue(context, "driftCategory")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/action-plans/{actionPlanState}", "GetCephalonCdcCaptureRuntimesByManagedConnectorActionPlanState", static (context, catalog) =>
            catalog.GetByManagedConnectorActionPlanState(GetRouteValue(context, "actionPlanState")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/actions/{actionId}", "GetCephalonCdcCaptureRuntimesByManagedConnectorAction", static (context, catalog) =>
            catalog.GetByManagedConnectorActionId(GetRouteValue(context, "actionId")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/write-path-readiness/{readinessState}", "GetCephalonCdcCaptureRuntimesByManagedConnectorWritePathReadinessState", static (context, catalog) =>
            catalog.GetByManagedConnectorWritePathReadinessState(GetRouteValue(context, "readinessState")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/write-path-readiness/categories/{readinessCategory}", "GetCephalonCdcCaptureRuntimesByManagedConnectorWritePathReadinessCategory", static (context, catalog) =>
            catalog.GetByManagedConnectorWritePathReadinessCategory(GetRouteValue(context, "readinessCategory")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/preflight/{preflightState}", "GetCephalonCdcCaptureRuntimesByManagedConnectorPreflightState", static (context, catalog) =>
            catalog.GetByManagedConnectorPreflightState(GetRouteValue(context, "preflightState")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/preflight/categories/{preflightCategory}", "GetCephalonCdcCaptureRuntimesByManagedConnectorPreflightCategory", static (context, catalog) =>
            catalog.GetByManagedConnectorPreflightCategory(GetRouteValue(context, "preflightCategory")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/preflight/operations/{operationId}", "GetCephalonCdcCaptureRuntimesByManagedConnectorPreflightOperation", static (context, catalog) =>
            catalog.GetByManagedConnectorPreflightOperationId(GetRouteValue(context, "operationId")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/dry-runs/{dryRunState}", "GetCephalonCdcCaptureRuntimesByManagedConnectorDryRunState", static (context, catalog) =>
            catalog.GetByManagedConnectorDryRunState(GetRouteValue(context, "dryRunState")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/dry-runs/categories/{dryRunCategory}", "GetCephalonCdcCaptureRuntimesByManagedConnectorDryRunCategory", static (context, catalog) =>
            catalog.GetByManagedConnectorDryRunCategory(GetRouteValue(context, "dryRunCategory")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/dry-runs/operations/{operationId}", "GetCephalonCdcCaptureRuntimesByManagedConnectorDryRunOperation", static (context, catalog) =>
            catalog.GetByManagedConnectorDryRunOperationId(GetRouteValue(context, "operationId")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/execution-intents/{executionIntentState}", "GetCephalonCdcCaptureRuntimesByManagedConnectorExecutionIntentState", static (context, catalog) =>
            catalog.GetByManagedConnectorExecutionIntentState(GetRouteValue(context, "executionIntentState")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/execution-intents/categories/{executionIntentCategory}", "GetCephalonCdcCaptureRuntimesByManagedConnectorExecutionIntentCategory", static (context, catalog) =>
            catalog.GetByManagedConnectorExecutionIntentCategory(GetRouteValue(context, "executionIntentCategory")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/execution-intents/operations/{operationId}", "GetCephalonCdcCaptureRuntimesByManagedConnectorExecutionIntentOperation", static (context, catalog) =>
            catalog.GetByManagedConnectorExecutionIntentOperationId(GetRouteValue(context, "operationId")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/execution-approvals/{executionApprovalState}", "GetCephalonCdcCaptureRuntimesByManagedConnectorExecutionApprovalState", static (context, catalog) =>
            catalog.GetByManagedConnectorExecutionApprovalState(GetRouteValue(context, "executionApprovalState")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/execution-approvals/categories/{executionApprovalCategory}", "GetCephalonCdcCaptureRuntimesByManagedConnectorExecutionApprovalCategory", static (context, catalog) =>
            catalog.GetByManagedConnectorExecutionApprovalCategory(GetRouteValue(context, "executionApprovalCategory")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/execution-approvals/operations/{operationId}", "GetCephalonCdcCaptureRuntimesByManagedConnectorExecutionApprovalOperation", static (context, catalog) =>
            catalog.GetByManagedConnectorExecutionApprovalOperationId(GetRouteValue(context, "operationId")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/command-envelopes/{commandState}", "GetCephalonCdcCaptureRuntimesByManagedConnectorCommandEnvelopeState", static (context, catalog) =>
            catalog.GetByManagedConnectorCommandEnvelopeState(GetRouteValue(context, "commandState")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/command-envelopes/categories/{commandCategory}", "GetCephalonCdcCaptureRuntimesByManagedConnectorCommandEnvelopeCategory", static (context, catalog) =>
            catalog.GetByManagedConnectorCommandEnvelopeCategory(GetRouteValue(context, "commandCategory")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/command-envelopes/operations/{operationId}", "GetCephalonCdcCaptureRuntimesByManagedConnectorCommandEnvelopeOperation", static (context, catalog) =>
            catalog.GetByManagedConnectorCommandEnvelopeOperationId(GetRouteValue(context, "operationId")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/command-issuances/{issuanceState}", "GetCephalonCdcCaptureRuntimesByManagedConnectorCommandIssuanceState", static (context, catalog) =>
            catalog.GetByManagedConnectorCommandIssuanceState(GetRouteValue(context, "issuanceState")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/command-issuances/categories/{issuanceCategory}", "GetCephalonCdcCaptureRuntimesByManagedConnectorCommandIssuanceCategory", static (context, catalog) =>
            catalog.GetByManagedConnectorCommandIssuanceCategory(GetRouteValue(context, "issuanceCategory")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/command-issuances/operations/{operationId}", "GetCephalonCdcCaptureRuntimesByManagedConnectorCommandIssuanceOperation", static (context, catalog) =>
            catalog.GetByManagedConnectorCommandIssuanceOperationId(GetRouteValue(context, "operationId")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/execution-adapters/{executionAdapterState}", "GetCephalonCdcCaptureRuntimesByManagedConnectorExecutionAdapterState", static (context, catalog) =>
            catalog.GetByManagedConnectorExecutionAdapterState(GetRouteValue(context, "executionAdapterState")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/execution-adapters/categories/{executionAdapterCategory}", "GetCephalonCdcCaptureRuntimesByManagedConnectorExecutionAdapterCategory", static (context, catalog) =>
            catalog.GetByManagedConnectorExecutionAdapterCategory(GetRouteValue(context, "executionAdapterCategory")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/execution-adapters/operations/{operationId}", "GetCephalonCdcCaptureRuntimesByManagedConnectorExecutionAdapterOperation", static (context, catalog) =>
            catalog.GetByManagedConnectorExecutionAdapterOperationId(GetRouteValue(context, "operationId")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/command-executions/{executionState}", "GetCephalonCdcCaptureRuntimesByManagedConnectorCommandExecutionState", static (context, catalog) =>
            catalog.GetByManagedConnectorCommandExecutionState(GetRouteValue(context, "executionState")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/command-executions/operations/{operationId}", "GetCephalonCdcCaptureRuntimesByManagedConnectorCommandExecutionOperation", static (context, catalog) =>
            catalog.GetByManagedConnectorCommandExecutionOperationId(GetRouteValue(context, "operationId")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/command-retries/{retryState}", "GetCephalonCdcCaptureRuntimesByManagedConnectorCommandRetryState", static (context, catalog) =>
            catalog.GetByManagedConnectorCommandRetryState(GetRouteValue(context, "retryState")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/command-retries/categories/{retryCategory}", "GetCephalonCdcCaptureRuntimesByManagedConnectorCommandRetryCategory", static (context, catalog) =>
            catalog.GetByManagedConnectorCommandRetryCategory(GetRouteValue(context, "retryCategory")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/command-retries/operations/{operationId}", "GetCephalonCdcCaptureRuntimesByManagedConnectorCommandRetryOperation", static (context, catalog) =>
            catalog.GetByManagedConnectorCommandRetryOperationId(GetRouteValue(context, "operationId")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/retry-execution-policies/{policyState}", "GetCephalonCdcCaptureRuntimesByManagedConnectorRetryExecutionPolicyState", static (context, catalog) =>
            catalog.GetByManagedConnectorRetryExecutionPolicyState(GetRouteValue(context, "policyState")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/retry-execution-policies/categories/{policyCategory}", "GetCephalonCdcCaptureRuntimesByManagedConnectorRetryExecutionPolicyCategory", static (context, catalog) =>
            catalog.GetByManagedConnectorRetryExecutionPolicyCategory(GetRouteValue(context, "policyCategory")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/retry-execution-policies/operations/{operationId}", "GetCephalonCdcCaptureRuntimesByManagedConnectorRetryExecutionPolicyOperation", static (context, catalog) =>
            catalog.GetByManagedConnectorRetryExecutionPolicyOperationId(GetRouteValue(context, "operationId")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/command-journals/{journalState}", "GetCephalonCdcCaptureRuntimesByManagedConnectorCommandJournalState", static (context, catalog) =>
            catalog.GetByManagedConnectorCommandJournalState(GetRouteValue(context, "journalState")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/command-journals/categories/{journalCategory}", "GetCephalonCdcCaptureRuntimesByManagedConnectorCommandJournalCategory", static (context, catalog) =>
            catalog.GetByManagedConnectorCommandJournalCategory(GetRouteValue(context, "journalCategory")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/command-journal-durability/{durabilityState}", "GetCephalonCdcCaptureRuntimesByManagedConnectorCommandJournalDurabilityState", static (context, catalog) =>
            catalog.GetByManagedConnectorCommandJournalDurabilityState(GetRouteValue(context, "durabilityState")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/command-journal-durability/categories/{durabilityCategory}", "GetCephalonCdcCaptureRuntimesByManagedConnectorCommandJournalDurabilityCategory", static (context, catalog) =>
            catalog.GetByManagedConnectorCommandJournalDurabilityCategory(GetRouteValue(context, "durabilityCategory")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/automatic-retries/{automaticRetryState}", "GetCephalonCdcCaptureRuntimesByManagedConnectorAutomaticRetryExecutionState", static (context, catalog) =>
            catalog.GetByManagedConnectorAutomaticRetryExecutionState(GetRouteValue(context, "automaticRetryState")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/automatic-retries/categories/{automaticRetryCategory}", "GetCephalonCdcCaptureRuntimesByManagedConnectorAutomaticRetryExecutionCategory", static (context, catalog) =>
            catalog.GetByManagedConnectorAutomaticRetryExecutionCategory(GetRouteValue(context, "automaticRetryCategory")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/automatic-retries/operations/{operationId}", "GetCephalonCdcCaptureRuntimesByManagedConnectorAutomaticRetryExecutionOperation", static (context, catalog) =>
            catalog.GetByManagedConnectorAutomaticRetryExecutionOperationId(GetRouteValue(context, "operationId")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/automatic-retry-coordinations/{coordinationState}", "GetCephalonCdcCaptureRuntimesByManagedConnectorAutomaticRetryCoordinationState", static (context, catalog) =>
            catalog.GetByManagedConnectorAutomaticRetryCoordinationState(GetRouteValue(context, "coordinationState")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/automatic-retry-coordinations/categories/{coordinationCategory}", "GetCephalonCdcCaptureRuntimesByManagedConnectorAutomaticRetryCoordinationCategory", static (context, catalog) =>
            catalog.GetByManagedConnectorAutomaticRetryCoordinationCategory(GetRouteValue(context, "coordinationCategory")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/automatic-retry-coordinations/owners/{ownerId}", "GetCephalonCdcCaptureRuntimesByManagedConnectorAutomaticRetryCoordinationOwner", static (context, catalog) =>
            catalog.GetByManagedConnectorAutomaticRetryCoordinationOwnerId(GetRouteValue(context, "ownerId")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/distributed-retry-leases/{leaseState}", "GetCephalonCdcCaptureRuntimesByManagedConnectorDistributedRetryLeaseState", static (context, catalog) =>
            catalog.GetByManagedConnectorDistributedRetryLeaseState(GetRouteValue(context, "leaseState")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/distributed-retry-leases/categories/{leaseCategory}", "GetCephalonCdcCaptureRuntimesByManagedConnectorDistributedRetryLeaseCategory", static (context, catalog) =>
            catalog.GetByManagedConnectorDistributedRetryLeaseCategory(GetRouteValue(context, "leaseCategory")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/distributed-retry-leases/owners/{ownerId}", "GetCephalonCdcCaptureRuntimesByManagedConnectorDistributedRetryLeaseOwner", static (context, catalog) =>
            catalog.GetByManagedConnectorDistributedRetryLeaseOwnerId(GetRouteValue(context, "ownerId")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/cross-node-idempotency-hardenings/{hardeningState}", "GetCephalonCdcCaptureRuntimesByManagedConnectorCrossNodeIdempotencyHardeningState", static (context, catalog) =>
            catalog.GetByManagedConnectorCrossNodeIdempotencyHardeningState(GetRouteValue(context, "hardeningState")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/cross-node-idempotency-hardenings/categories/{hardeningCategory}", "GetCephalonCdcCaptureRuntimesByManagedConnectorCrossNodeIdempotencyHardeningCategory", static (context, catalog) =>
            catalog.GetByManagedConnectorCrossNodeIdempotencyHardeningCategory(GetRouteValue(context, "hardeningCategory")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/cross-node-idempotency-hardenings/owners/{ownerId}", "GetCephalonCdcCaptureRuntimesByManagedConnectorCrossNodeIdempotencyHardeningOwner", static (context, catalog) =>
            catalog.GetByManagedConnectorCrossNodeIdempotencyHardeningOwnerId(GetRouteValue(context, "ownerId")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/cross-node-idempotency-hardenings/fingerprints/{retryFingerprint}", "GetCephalonCdcCaptureRuntimesByManagedConnectorCrossNodeIdempotencyHardeningRetryFingerprint", static (context, catalog) =>
            catalog.GetByManagedConnectorCrossNodeIdempotencyHardeningRetryFingerprint(GetRouteValue(context, "retryFingerprint")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/distributed-retry-orchestrations/{orchestrationState}", "GetCephalonCdcCaptureRuntimesByManagedConnectorDistributedRetryOrchestrationState", static (context, catalog) =>
            catalog.GetByManagedConnectorDistributedRetryOrchestrationState(GetRouteValue(context, "orchestrationState")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/distributed-retry-orchestrations/categories/{orchestrationCategory}", "GetCephalonCdcCaptureRuntimesByManagedConnectorDistributedRetryOrchestrationCategory", static (context, catalog) =>
            catalog.GetByManagedConnectorDistributedRetryOrchestrationCategory(GetRouteValue(context, "orchestrationCategory")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/distributed-retry-orchestrations/owners/{ownerId}", "GetCephalonCdcCaptureRuntimesByManagedConnectorDistributedRetryOrchestrationOwner", static (context, catalog) =>
            catalog.GetByManagedConnectorDistributedRetryOrchestrationOwnerId(GetRouteValue(context, "ownerId")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/multi-node-lease-executions/{leaseExecutionState}", "GetCephalonCdcCaptureRuntimesByManagedConnectorMultiNodeLeaseExecutionState", static (context, catalog) =>
            catalog.GetByManagedConnectorMultiNodeLeaseExecutionState(GetRouteValue(context, "leaseExecutionState")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/multi-node-lease-executions/categories/{leaseExecutionCategory}", "GetCephalonCdcCaptureRuntimesByManagedConnectorMultiNodeLeaseExecutionCategory", static (context, catalog) =>
            catalog.GetByManagedConnectorMultiNodeLeaseExecutionCategory(GetRouteValue(context, "leaseExecutionCategory")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/multi-node-lease-executions/owners/{ownerId}", "GetCephalonCdcCaptureRuntimesByManagedConnectorMultiNodeLeaseExecutionOwner", static (context, catalog) =>
            catalog.GetByManagedConnectorMultiNodeLeaseExecutionOwnerId(GetRouteValue(context, "ownerId")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/durable-shared-scheduler-orchestrations/{schedulerState}", "GetCephalonCdcCaptureRuntimesByManagedConnectorDurableSharedSchedulerOrchestrationState", static (context, catalog) =>
            catalog.GetByManagedConnectorDurableSharedSchedulerOrchestrationState(GetRouteValue(context, "schedulerState")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/durable-shared-scheduler-orchestrations/categories/{schedulerCategory}", "GetCephalonCdcCaptureRuntimesByManagedConnectorDurableSharedSchedulerOrchestrationCategory", static (context, catalog) =>
            catalog.GetByManagedConnectorDurableSharedSchedulerOrchestrationCategory(GetRouteValue(context, "schedulerCategory")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/durable-shared-scheduler-orchestrations/owners/{ownerId}", "GetCephalonCdcCaptureRuntimesByManagedConnectorDurableSharedSchedulerOrchestrationOwner", static (context, catalog) =>
            catalog.GetByManagedConnectorDurableSharedSchedulerOrchestrationOwnerId(GetRouteValue(context, "ownerId")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/scheduler-recovery-execution-hardenings/{hardeningState}", "GetCephalonCdcCaptureRuntimesByManagedConnectorSchedulerRecoveryExecutionHardeningState", static (context, catalog) =>
            catalog.GetByManagedConnectorSchedulerRecoveryExecutionHardeningState(GetRouteValue(context, "hardeningState")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/scheduler-recovery-execution-hardenings/categories/{hardeningCategory}", "GetCephalonCdcCaptureRuntimesByManagedConnectorSchedulerRecoveryExecutionHardeningCategory", static (context, catalog) =>
            catalog.GetByManagedConnectorSchedulerRecoveryExecutionHardeningCategory(GetRouteValue(context, "hardeningCategory")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/scheduler-recovery-execution-hardenings/owners/{ownerId}", "GetCephalonCdcCaptureRuntimesByManagedConnectorSchedulerRecoveryExecutionHardeningOwner", static (context, catalog) =>
            catalog.GetByManagedConnectorSchedulerRecoveryExecutionHardeningOwnerId(GetRouteValue(context, "ownerId")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/scheduler-recovery-execution-hardenings/fingerprints/{retryFingerprint}", "GetCephalonCdcCaptureRuntimesByManagedConnectorSchedulerRecoveryExecutionHardeningRetryFingerprint", static (context, catalog) =>
            catalog.GetByManagedConnectorSchedulerRecoveryExecutionHardeningRetryFingerprint(GetRouteValue(context, "retryFingerprint")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/provider-owned-write-path-executions/{providerExecutionState}", "GetCephalonCdcCaptureRuntimesByManagedConnectorProviderOwnedWritePathExecutionState", static (context, catalog) =>
            catalog.GetByManagedConnectorProviderOwnedWritePathExecutionState(GetRouteValue(context, "providerExecutionState")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/provider-owned-write-path-executions/categories/{providerExecutionCategory}", "GetCephalonCdcCaptureRuntimesByManagedConnectorProviderOwnedWritePathExecutionCategory", static (context, catalog) =>
            catalog.GetByManagedConnectorProviderOwnedWritePathExecutionCategory(GetRouteValue(context, "providerExecutionCategory")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/provider-owned-write-path-executions/operations/{operationId}", "GetCephalonCdcCaptureRuntimesByManagedConnectorProviderOwnedWritePathExecutionOperation", static (context, catalog) =>
            catalog.GetByManagedConnectorProviderOwnedWritePathExecutionOperationId(GetRouteValue(context, "operationId")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/provider-execution-orchestrations/{providerExecutionOrchestrationState}", "GetCephalonCdcCaptureRuntimesByManagedConnectorProviderExecutionOrchestrationState", static (context, catalog) =>
            catalog.GetByManagedConnectorProviderExecutionOrchestrationState(GetRouteValue(context, "providerExecutionOrchestrationState")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/provider-execution-orchestrations/categories/{providerExecutionOrchestrationCategory}", "GetCephalonCdcCaptureRuntimesByManagedConnectorProviderExecutionOrchestrationCategory", static (context, catalog) =>
            catalog.GetByManagedConnectorProviderExecutionOrchestrationCategory(GetRouteValue(context, "providerExecutionOrchestrationCategory")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/provider-execution-orchestrations/operations/{operationId}", "GetCephalonCdcCaptureRuntimesByManagedConnectorProviderExecutionOrchestrationOperation", static (context, catalog) =>
            catalog.GetByManagedConnectorProviderExecutionOrchestrationOperationId(GetRouteValue(context, "operationId")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/provider-owned-control-plane-ownership/{providerOwnedControlPlaneOwnershipState}", "GetCephalonCdcCaptureRuntimesByManagedConnectorProviderOwnedControlPlaneOwnershipState", static (context, catalog) =>
            catalog.GetByManagedConnectorProviderOwnedControlPlaneOwnershipState(GetRouteValue(context, "providerOwnedControlPlaneOwnershipState")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/provider-owned-control-plane-ownership/categories/{providerOwnedControlPlaneOwnershipCategory}", "GetCephalonCdcCaptureRuntimesByManagedConnectorProviderOwnedControlPlaneOwnershipCategory", static (context, catalog) =>
            catalog.GetByManagedConnectorProviderOwnedControlPlaneOwnershipCategory(GetRouteValue(context, "providerOwnedControlPlaneOwnershipCategory")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/provider-owned-control-plane-ownership/operations/{operationId}", "GetCephalonCdcCaptureRuntimesByManagedConnectorProviderOwnedControlPlaneOwnershipOperation", static (context, catalog) =>
            catalog.GetByManagedConnectorProviderOwnedControlPlaneOwnershipOperationId(GetRouteValue(context, "operationId")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/provider-owned-control-plane-mutation-reconcile/{providerOwnedControlPlaneMutationReconcileState}", "GetCephalonCdcCaptureRuntimesByManagedConnectorProviderOwnedControlPlaneMutationReconcileState", static (context, catalog) =>
            catalog.GetByManagedConnectorProviderOwnedControlPlaneMutationReconcileState(GetRouteValue(context, "providerOwnedControlPlaneMutationReconcileState")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/provider-owned-control-plane-mutation-reconcile/categories/{providerOwnedControlPlaneMutationReconcileCategory}", "GetCephalonCdcCaptureRuntimesByManagedConnectorProviderOwnedControlPlaneMutationReconcileCategory", static (context, catalog) =>
            catalog.GetByManagedConnectorProviderOwnedControlPlaneMutationReconcileCategory(GetRouteValue(context, "providerOwnedControlPlaneMutationReconcileCategory")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/provider-owned-control-plane-mutation-reconcile/operations/{operationId}", "GetCephalonCdcCaptureRuntimesByManagedConnectorProviderOwnedControlPlaneMutationReconcileOperation", static (context, catalog) =>
            catalog.GetByManagedConnectorProviderOwnedControlPlaneMutationReconcileOperationId(GetRouteValue(context, "operationId")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/provider-owned-control-plane-provisioning/{providerOwnedControlPlaneProvisioningState}", "GetCephalonCdcCaptureRuntimesByManagedConnectorProviderOwnedControlPlaneProvisioningState", static (context, catalog) =>
            catalog.GetByManagedConnectorProviderOwnedControlPlaneProvisioningState(GetRouteValue(context, "providerOwnedControlPlaneProvisioningState")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/provider-owned-control-plane-provisioning/categories/{providerOwnedControlPlaneProvisioningCategory}", "GetCephalonCdcCaptureRuntimesByManagedConnectorProviderOwnedControlPlaneProvisioningCategory", static (context, catalog) =>
            catalog.GetByManagedConnectorProviderOwnedControlPlaneProvisioningCategory(GetRouteValue(context, "providerOwnedControlPlaneProvisioningCategory")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/provider-owned-control-plane-provisioning/operations/{operationId}", "GetCephalonCdcCaptureRuntimesByManagedConnectorProviderOwnedControlPlaneProvisioningOperation", static (context, catalog) =>
            catalog.GetByManagedConnectorProviderOwnedControlPlaneProvisioningOperationId(GetRouteValue(context, "operationId")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/provider-owned-control-plane-apply-and-reconcile-executions/{applyAndReconcileExecutionState}", "GetCephalonCdcCaptureRuntimesByManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionState", static (context, catalog) =>
            catalog.GetByManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionState(GetRouteValue(context, "applyAndReconcileExecutionState")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/provider-owned-control-plane-apply-and-reconcile-executions/categories/{applyAndReconcileExecutionCategory}", "GetCephalonCdcCaptureRuntimesByManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionCategory", static (context, catalog) =>
            catalog.GetByManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionCategory(GetRouteValue(context, "applyAndReconcileExecutionCategory")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/provider-owned-control-plane-apply-and-reconcile-executions/operations/{operationId}", "GetCephalonCdcCaptureRuntimesByManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionOperation", static (context, catalog) =>
            catalog.GetByManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecutionOperationId(GetRouteValue(context, "operationId")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/provider-owned-control-plane-dependency-aware-apply-and-reconcile-hardenings/{hardeningState}", "GetCephalonCdcCaptureRuntimesByManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningState", static (context, catalog) =>
            catalog.GetByManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningState(GetRouteValue(context, "hardeningState")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/provider-owned-control-plane-dependency-aware-apply-and-reconcile-hardenings/categories/{hardeningCategory}", "GetCephalonCdcCaptureRuntimesByManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategory", static (context, catalog) =>
            catalog.GetByManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningCategory(GetRouteValue(context, "hardeningCategory")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/provider-owned-control-plane-dependency-aware-apply-and-reconcile-hardenings/operations/{operationId}", "GetCephalonCdcCaptureRuntimesByManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningOperation", static (context, catalog) =>
            catalog.GetByManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardeningOperationId(GetRouteValue(context, "operationId")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/provider-owned-control-plane-dependency-aware-provisioning-and-mutation-hardenings/{hardeningState}", "GetCephalonCdcCaptureRuntimesByManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningState", static (context, catalog) =>
            catalog.GetByManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningState(GetRouteValue(context, "hardeningState")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/provider-owned-control-plane-dependency-aware-provisioning-and-mutation-hardenings/categories/{hardeningCategory}", "GetCephalonCdcCaptureRuntimesByManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningCategory", static (context, catalog) =>
            catalog.GetByManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningCategory(GetRouteValue(context, "hardeningCategory")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/provider-owned-control-plane-dependency-aware-provisioning-and-mutation-hardenings/operations/{operationId}", "GetCephalonCdcCaptureRuntimesByManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningOperation", static (context, catalog) =>
            catalog.GetByManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardeningOperationId(GetRouteValue(context, "operationId")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/provider-specific-control-plane-materializers/{materializerState}", "GetCephalonCdcCaptureRuntimesByManagedConnectorProviderSpecificControlPlaneMaterializerState", static (context, catalog) =>
            catalog.GetByManagedConnectorProviderSpecificControlPlaneMaterializerState(GetRouteValue(context, "materializerState")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/provider-specific-control-plane-materializers/categories/{materializerCategory}", "GetCephalonCdcCaptureRuntimesByManagedConnectorProviderSpecificControlPlaneMaterializerCategory", static (context, catalog) =>
            catalog.GetByManagedConnectorProviderSpecificControlPlaneMaterializerCategory(GetRouteValue(context, "materializerCategory")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/provider-specific-control-plane-materializers/providers/{providerId}", "GetCephalonCdcCaptureRuntimesByManagedConnectorProviderSpecificControlPlaneMaterializerProvider", static (context, catalog) =>
            catalog.GetByManagedConnectorProviderSpecificControlPlaneMaterializerProviderId(GetRouteValue(context, "providerId")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/provider-specific-control-plane-materializers/provider-surfaces/{providerSurfaceId}", "GetCephalonCdcCaptureRuntimesByManagedConnectorProviderSpecificControlPlaneMaterializerProviderSurface", static (context, catalog) =>
            catalog.GetByManagedConnectorProviderSpecificControlPlaneMaterializerProviderSurfaceId(GetRouteValue(context, "providerSurfaceId")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/provider-specific-control-plane-materializers/materializers/{materializerId}", "GetCephalonCdcCaptureRuntimesByManagedConnectorProviderSpecificControlPlaneMaterializerId", static (context, catalog) =>
            catalog.GetByManagedConnectorProviderSpecificControlPlaneMaterializerId(GetRouteValue(context, "materializerId")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/provider-specific-control-plane-materializers/transports/{transportKind}", "GetCephalonCdcCaptureRuntimesByManagedConnectorProviderSpecificControlPlaneMaterializerTransport", static (context, catalog) =>
            catalog.GetByManagedConnectorProviderSpecificControlPlaneMaterializerTransportKind(GetRouteValue(context, "transportKind")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/provider-specific-control-plane-materializers/connect-clusters/{connectClusterId}", "GetCephalonCdcCaptureRuntimesByManagedConnectorProviderSpecificControlPlaneMaterializerConnectCluster", static (context, catalog) =>
            catalog.GetByManagedConnectorProviderSpecificControlPlaneMaterializerConnectClusterId(GetRouteValue(context, "connectClusterId")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/provider-specific-control-plane-materializers/connector-classes/{connectorClass}", "GetCephalonCdcCaptureRuntimesByManagedConnectorProviderSpecificControlPlaneMaterializerConnectorClass", static (context, catalog) =>
            catalog.GetByManagedConnectorProviderSpecificControlPlaneMaterializerConnectorClass(GetRouteValue(context, "connectorClass")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/provider-specific-control-plane-materializers/source-providers/{sourceProviderId}", "GetCephalonCdcCaptureRuntimesByManagedConnectorProviderSpecificControlPlaneMaterializerSourceProvider", static (context, catalog) =>
            catalog.GetByManagedConnectorProviderSpecificControlPlaneMaterializerSourceProviderId(GetRouteValue(context, "sourceProviderId")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/provider-specific-control-plane-materializers/connectors/{connectorId}", "GetCephalonCdcCaptureRuntimesByManagedConnectorProviderSpecificControlPlaneMaterializerConnector", static (context, catalog) =>
            catalog.GetByManagedConnectorProviderSpecificControlPlaneMaterializerConnectorId(GetRouteValue(context, "connectorId")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/provider-specific-control-plane-materializers/workers/{workerId}", "GetCephalonCdcCaptureRuntimesByManagedConnectorProviderSpecificControlPlaneMaterializerWorker", static (context, catalog) =>
            catalog.GetByManagedConnectorProviderSpecificControlPlaneMaterializerWorkerId(GetRouteValue(context, "workerId")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/provider-specific-control-plane-materializers/current-nodes/{canUseOnCurrentNode:bool}", "GetCephalonCdcCaptureRuntimesByManagedConnectorProviderSpecificControlPlaneMaterializerCurrentNode", static (context, catalog) =>
            catalog.GetByManagedConnectorProviderSpecificControlPlaneMaterializerCanUseOnCurrentNode(GetBooleanRouteValue(context, "canUseOnCurrentNode")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/provider-specific-control-plane-materializers/operations/{operationId}", "GetCephalonCdcCaptureRuntimesByManagedConnectorProviderSpecificControlPlaneMaterializerOperation", static (context, catalog) =>
            catalog.GetByManagedConnectorProviderSpecificControlPlaneMaterializerOperationId(GetRouteValue(context, "operationId")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/provider-specific-control-plane-dependency-aware-teardown-and-mutation-execution-hardenings/{hardeningState}", "GetCephalonCdcCaptureRuntimesByManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningState", static (context, catalog) =>
            catalog.GetByManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningState(GetRouteValue(context, "hardeningState")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/provider-specific-control-plane-dependency-aware-teardown-and-mutation-execution-hardenings/categories/{hardeningCategory}", "GetCephalonCdcCaptureRuntimesByManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningCategory", static (context, catalog) =>
            catalog.GetByManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningCategory(GetRouteValue(context, "hardeningCategory")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/provider-specific-control-plane-dependency-aware-teardown-and-mutation-execution-hardenings/providers/{providerId}", "GetCephalonCdcCaptureRuntimesByManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningProvider", static (context, catalog) =>
            catalog.GetByManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningProviderId(GetRouteValue(context, "providerId")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/provider-specific-control-plane-dependency-aware-teardown-and-mutation-execution-hardenings/provider-surfaces/{providerSurfaceId}", "GetCephalonCdcCaptureRuntimesByManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningProviderSurface", static (context, catalog) =>
            catalog.GetByManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningProviderSurfaceId(GetRouteValue(context, "providerSurfaceId")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/provider-specific-control-plane-dependency-aware-teardown-and-mutation-execution-hardenings/materializers/{materializerId}", "GetCephalonCdcCaptureRuntimesByManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningMaterializer", static (context, catalog) =>
            catalog.GetByManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningMaterializerId(GetRouteValue(context, "materializerId")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/provider-specific-control-plane-dependency-aware-teardown-and-mutation-execution-hardenings/transports/{transportKind}", "GetCephalonCdcCaptureRuntimesByManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningTransport", static (context, catalog) =>
            catalog.GetByManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningTransportKind(GetRouteValue(context, "transportKind")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/provider-specific-control-plane-dependency-aware-teardown-and-mutation-execution-hardenings/connect-clusters/{connectClusterId}", "GetCephalonCdcCaptureRuntimesByManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningConnectCluster", static (context, catalog) =>
            catalog.GetByManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningConnectClusterId(GetRouteValue(context, "connectClusterId")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/provider-specific-control-plane-dependency-aware-teardown-and-mutation-execution-hardenings/connector-classes/{connectorClass}", "GetCephalonCdcCaptureRuntimesByManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningConnectorClass", static (context, catalog) =>
            catalog.GetByManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningConnectorClass(GetRouteValue(context, "connectorClass")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/provider-specific-control-plane-dependency-aware-teardown-and-mutation-execution-hardenings/source-providers/{sourceProviderId}", "GetCephalonCdcCaptureRuntimesByManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningSourceProvider", static (context, catalog) =>
            catalog.GetByManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningSourceProviderId(GetRouteValue(context, "sourceProviderId")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/provider-specific-control-plane-dependency-aware-teardown-and-mutation-execution-hardenings/connectors/{connectorId}", "GetCephalonCdcCaptureRuntimesByManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningConnector", static (context, catalog) =>
            catalog.GetByManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningConnectorId(GetRouteValue(context, "connectorId")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/provider-specific-control-plane-dependency-aware-teardown-and-mutation-execution-hardenings/workers/{workerId}", "GetCephalonCdcCaptureRuntimesByManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningWorker", static (context, catalog) =>
            catalog.GetByManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningWorkerId(GetRouteValue(context, "workerId")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/provider-specific-control-plane-dependency-aware-teardown-and-mutation-execution-hardenings/current-nodes/{canExecuteOnCurrentNode:bool}", "GetCephalonCdcCaptureRuntimesByManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningCurrentNode", static (context, catalog) =>
            catalog.GetByManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningCanExecuteOnCurrentNode(GetBooleanRouteValue(context, "canExecuteOnCurrentNode")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/provider-specific-control-plane-dependency-aware-teardown-and-mutation-execution-hardenings/current-nodes/teardowns/{canExecuteTeardownOnCurrentNode:bool}", "GetCephalonCdcCaptureRuntimesByManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningCurrentNodeTeardown", static (context, catalog) =>
            catalog.GetByManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningCanExecuteTeardownOnCurrentNode(GetBooleanRouteValue(context, "canExecuteTeardownOnCurrentNode")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/provider-specific-control-plane-dependency-aware-teardown-and-mutation-execution-hardenings/current-nodes/mutation-executions/{canExecuteMutationExecutionOnCurrentNode:bool}", "GetCephalonCdcCaptureRuntimesByManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningCurrentNodeMutationExecution", static (context, catalog) =>
            catalog.GetByManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningCanExecuteMutationExecutionOnCurrentNode(GetBooleanRouteValue(context, "canExecuteMutationExecutionOnCurrentNode")));
        MapCdcCaptureRuntimeCollectionRoute(engineGroup, "/cdc-capture-runtimes/provider-specific-control-plane-dependency-aware-teardown-and-mutation-execution-hardenings/operations/{operationId}", "GetCephalonCdcCaptureRuntimesByManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningOperation", static (context, catalog) =>
            catalog.GetByManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardeningOperationId(GetRouteValue(context, "operationId")));
        MapGetResultRequestDelegate(engineGroup, "/cdc-capture-runtimes/{executionRuntimeId}/command-executions", "GetCephalonManagedConnectorCommandExecutionHistory", static context =>
            GetCdcCaptureRuntimeCommandExecutionHistory(context));
        MapCdcCaptureRuntimeDescriptorRoute(engineGroup, "/cdc-capture-runtimes/{executionRuntimeId}/command-journal", "GetCephalonManagedConnectorCommandJournal", static runtime => runtime.ManagedConnectorCommandJournal);
        MapCdcCaptureRuntimeDescriptorRoute(engineGroup, "/cdc-capture-runtimes/{executionRuntimeId}/command-journal-durability", "GetCephalonManagedConnectorCommandJournalDurability", static runtime => runtime.ManagedConnectorCommandJournalDurability);
        MapCdcCaptureRuntimeDescriptorRoute(engineGroup, "/cdc-capture-runtimes/{executionRuntimeId}/distributed-retry-lease", "GetCephalonManagedConnectorDistributedRetryLease", static runtime => runtime.ManagedConnectorDistributedRetryLease);
        MapCdcCaptureRuntimeDescriptorRoute(engineGroup, "/cdc-capture-runtimes/{executionRuntimeId}/cross-node-idempotency-hardening", "GetCephalonManagedConnectorCrossNodeIdempotencyHardening", static runtime => runtime.ManagedConnectorCrossNodeIdempotencyHardening);
        MapCdcCaptureRuntimeDescriptorRoute(engineGroup, "/cdc-capture-runtimes/{executionRuntimeId}/distributed-retry-orchestration", "GetCephalonManagedConnectorDistributedRetryOrchestration", static runtime => runtime.ManagedConnectorDistributedRetryOrchestration);
        MapCdcCaptureRuntimeDescriptorRoute(engineGroup, "/cdc-capture-runtimes/{executionRuntimeId}/multi-node-lease-execution", "GetCephalonManagedConnectorMultiNodeLeaseExecution", static runtime => runtime.ManagedConnectorMultiNodeLeaseExecution);
        MapCdcCaptureRuntimeDescriptorRoute(engineGroup, "/cdc-capture-runtimes/{executionRuntimeId}/durable-shared-scheduler-orchestration", "GetCephalonManagedConnectorDurableSharedSchedulerOrchestration", static runtime => runtime.ManagedConnectorDurableSharedSchedulerOrchestration);
        MapCdcCaptureRuntimeDescriptorRoute(engineGroup, "/cdc-capture-runtimes/{executionRuntimeId}/scheduler-recovery-execution-hardening", "GetCephalonManagedConnectorSchedulerRecoveryExecutionHardening", static runtime => runtime.ManagedConnectorSchedulerRecoveryExecutionHardening);
        MapCdcCaptureRuntimeDescriptorRoute(engineGroup, "/cdc-capture-runtimes/{executionRuntimeId}/provider-owned-write-path-execution", "GetCephalonManagedConnectorProviderOwnedWritePathExecution", static runtime => runtime.ManagedConnectorProviderOwnedWritePathExecution);
        MapCdcCaptureRuntimeDescriptorRoute(engineGroup, "/cdc-capture-runtimes/{executionRuntimeId}/provider-execution-orchestration", "GetCephalonManagedConnectorProviderExecutionOrchestration", static runtime => runtime.ManagedConnectorProviderExecutionOrchestration);
        MapCdcCaptureRuntimeDescriptorRoute(engineGroup, "/cdc-capture-runtimes/{executionRuntimeId}/provider-owned-control-plane-ownership", "GetCephalonManagedConnectorProviderOwnedControlPlaneOwnership", static runtime => runtime.ManagedConnectorProviderOwnedControlPlaneOwnership);
        MapCdcCaptureRuntimeDescriptorRoute(engineGroup, "/cdc-capture-runtimes/{executionRuntimeId}/provider-owned-control-plane-mutation-reconcile", "GetCephalonManagedConnectorProviderOwnedControlPlaneMutationReconcile", static runtime => runtime.ManagedConnectorProviderOwnedControlPlaneMutationReconcile);
        MapCdcCaptureRuntimeDescriptorRoute(engineGroup, "/cdc-capture-runtimes/{executionRuntimeId}/provider-owned-control-plane-provisioning", "GetCephalonManagedConnectorProviderOwnedControlPlaneProvisioning", static runtime => runtime.ManagedConnectorProviderOwnedControlPlaneProvisioning);
        MapCdcCaptureRuntimeDescriptorRoute(engineGroup, "/cdc-capture-runtimes/{executionRuntimeId}/provider-owned-control-plane-apply-and-reconcile-execution", "GetCephalonManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecution", static runtime => runtime.ManagedConnectorProviderOwnedControlPlaneApplyAndReconcileExecution);
        MapCdcCaptureRuntimeDescriptorRoute(engineGroup, "/cdc-capture-runtimes/{executionRuntimeId}/provider-owned-control-plane-dependency-aware-apply-and-reconcile-hardening", "GetCephalonManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardening", static runtime => runtime.ManagedConnectorProviderOwnedControlPlaneDependencyAwareApplyAndReconcileHardening);
        MapCdcCaptureRuntimeDescriptorRoute(engineGroup, "/cdc-capture-runtimes/{executionRuntimeId}/provider-owned-control-plane-dependency-aware-provisioning-and-mutation-hardening", "GetCephalonManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardening", static runtime => runtime.ManagedConnectorProviderOwnedControlPlaneDependencyAwareProvisioningAndMutationHardening);
        MapCdcCaptureRuntimeDescriptorRoute(engineGroup, "/cdc-capture-runtimes/{executionRuntimeId}/provider-specific-control-plane-materializer", "GetCephalonManagedConnectorProviderSpecificControlPlaneMaterializer", static runtime => runtime.ManagedConnectorProviderSpecificControlPlaneMaterializer);
        MapCdcCaptureRuntimeDescriptorRoute(engineGroup, "/cdc-capture-runtimes/{executionRuntimeId}/provider-specific-control-plane-dependency-aware-teardown-and-mutation-execution-hardening", "GetCephalonManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardening", static runtime => runtime.ManagedConnectorProviderSpecificControlPlaneDependencyAwareTeardownAndMutationExecutionHardening);
        MapCdcCaptureRuntimeDescriptorRoute(engineGroup, "/cdc-capture-runtimes/{executionRuntimeId}", "GetCephalonCdcCaptureRuntime", static runtime => runtime);
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
        engineGroup.MapGet("/event-dispatches/terminal-failures", (HttpContext httpContext) =>
            {
                var states = httpContext.RequestServices
                    .GetService<IEventDispatchRuntimeCatalog>()?
                    .States
                    .Where(static state => state.TerminalFailure)
                    .ToArray() ?? [];

                return Results.Ok(states);
            })
            .WithName("GetCephalonTerminalEventDispatchFailures");
        engineGroup.MapGet("/event-dispatches/{outboxId}", (string outboxId, HttpContext httpContext) =>
            {
                var state = httpContext.RequestServices
                    .GetService<IEventDispatchRuntimeCatalog>()?
                    .GetByOutboxId(outboxId);

                return state is null ? Results.NotFound() : Results.Ok(state);
            })
            .WithName("GetCephalonEventDispatch");
        engineGroup.MapGet("/event-publications/runtime", (HttpContext httpContext) =>
            {
                var states = httpContext.RequestServices
                    .GetService<IEventPublicationRuntimeCatalog>()?
                    .States ?? [];

                return Results.Ok(states);
            })
            .WithName("GetCephalonEventPublicationRuntimeStates");
        engineGroup.MapGet("/event-publications/runtime/channels/{channelId}", (string channelId, HttpContext httpContext) =>
            {
                var states = httpContext.RequestServices
                    .GetService<IEventPublicationRuntimeCatalog>()?
                    .GetByChannelId(channelId) ?? [];

                return Results.Ok(states);
            })
            .WithName("GetCephalonEventPublicationRuntimeStatesByChannel");
        engineGroup.MapGet("/event-publications/runtime/{publicationId}", (string publicationId, HttpContext httpContext) =>
            {
                var state = httpContext.RequestServices
                    .GetService<IEventPublicationRuntimeCatalog>()?
                    .GetByPublicationId(publicationId);

                return state is null ? Results.NotFound() : Results.Ok(state);
            })
            .WithName("GetCephalonEventPublicationRuntimeState");
        engineGroup.MapPost(
                "/event-publications",
                async (
                    [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] EventPublicationHttpRequest? request,
                    HttpContext httpContext,
                    CancellationToken cancellationToken) =>
                {
                    var dispatcher = httpContext.RequestServices.GetService<IEventPublicationDispatcher>();
                    if (dispatcher is null)
                    {
                        return Results.NotFound(new
                        {
                            error = "Event publication is not available in the active runtime."
                        });
                    }

                    try
                    {
                        var publicationRequest = CreateEventPublicationRequest(request, httpContext);
                        var result = await dispatcher.PublishAsync(publicationRequest, cancellationToken).ConfigureAwait(false);
                        return Results.Ok(result);
                    }
                    catch (ArgumentException exception)
                    {
                        return Results.BadRequest(new { error = exception.Message });
                    }
                    catch (InvalidOperationException exception)
                        when (IsUnregisteredEventChannel(exception))
                    {
                        return Results.NotFound(new { error = exception.Message });
                    }
                    catch (InvalidOperationException exception)
                    {
                        return Results.Problem(
                            title: "Event publication failed.",
                            detail: exception.Message,
                            statusCode: StatusCodes.Status500InternalServerError);
                    }
                })
            .WithName("PublishCephalonEventPublication");
        engineGroup.MapGet("/agent-tool-runs", (HttpContext httpContext) =>
            {
                var runs = httpContext.RequestServices
                    .GetService<IAgentToolRunCatalog>()?
                    .Runs ?? [];

                return Results.Ok(runs);
            })
            .WithName("GetCephalonAgentToolRuns");
        engineGroup.MapGet("/agent-tool-runs/retry-pending", (HttpContext httpContext) =>
            {
                var runs = httpContext.RequestServices
                    .GetService<IAgentToolRunCatalog>()?
                    .Runs
                    .Where(static run => run.RetryPending)
                    .ToArray() ?? [];

                return Results.Ok(runs);
            })
            .WithName("GetCephalonRetryPendingAgentToolRuns");
        engineGroup.MapGet("/agent-tool-runs/idempotency-duplicates", (HttpContext httpContext) =>
            {
                var runs = httpContext.RequestServices
                    .GetService<IAgentToolRunCatalog>()?
                    .Runs
                    .Where(static run => run.DuplicateCompleted)
                    .ToArray() ?? [];

                return Results.Ok(runs);
            })
            .WithName("GetCephalonDuplicateCompletedAgentToolRuns");
        engineGroup.MapGet("/agent-tool-runs/approval-required", (HttpContext httpContext) =>
            {
                var runs = httpContext.RequestServices
                    .GetService<IAgentToolRunCatalog>()?
                    .Runs
                    .Where(static run => run.RequiresApproval)
                    .ToArray() ?? [];

                return Results.Ok(runs);
            })
            .WithName("GetCephalonApprovalRequiredAgentToolRuns");
        engineGroup.MapGet("/agent-tool-runs/terminal-failures", (HttpContext httpContext) =>
            {
                var runs = httpContext.RequestServices
                    .GetService<IAgentToolRunCatalog>()?
                    .Runs
                    .Where(static run => run.TerminalFailure)
                    .ToArray() ?? [];

                return Results.Ok(runs);
            })
            .WithName("GetCephalonTerminalFailureAgentToolRuns");
        engineGroup.MapGet("/agent-tool-runs/{runId}", (string runId, HttpContext httpContext) =>
            {
                var run = httpContext.RequestServices
                    .GetService<IAgentToolRunCatalog>()?
                    .GetByRunId(runId);

                return run is null ? Results.NotFound() : Results.Ok(run);
            })
            .WithName("GetCephalonAgentToolRun");
        engineGroup.MapGet("/agent-tool-runs/by-tool/{toolId}", (string toolId, HttpContext httpContext) =>
            {
                var runs = httpContext.RequestServices
                    .GetService<IAgentToolRunCatalog>()?
                    .GetByToolId(toolId) ?? [];

                return Results.Ok(runs);
            })
            .WithName("GetCephalonAgentToolRunsByTool");
        engineGroup.MapPost(
                "/agent-tools/{toolId}/runs",
                async (
                    string toolId,
                    [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] AgentToolExecutionHttpRequest? request,
                    HttpContext httpContext,
                    CancellationToken cancellationToken) =>
                {
                    var dispatcher = httpContext.RequestServices.GetService<IAgentToolDispatcher>();
                    if (dispatcher is null)
                    {
                        return Results.NotFound(new
                        {
                            error = "Agent-tool execution is not available in the active runtime."
                        });
                    }

                    try
                    {
                        var executionRequest = CreateAgentToolExecutionRequest(toolId, request, httpContext);
                        var result = await dispatcher.ExecuteAsync(executionRequest, cancellationToken).ConfigureAwait(false);
                        return Results.Ok(result);
                    }
                    catch (ArgumentException exception)
                    {
                        return Results.BadRequest(new { error = exception.Message });
                    }
                    catch (InvalidOperationException exception)
                        when (IsUnregisteredAgentTool(exception))
                    {
                        return Results.NotFound(new { error = exception.Message });
                    }
                    catch (InvalidOperationException exception)
                        when (IsMissingAgentToolExecutor(exception))
                    {
                        return Results.Conflict(new { error = exception.Message });
                    }
                    catch (InvalidOperationException exception)
                    {
                        return Results.Problem(
                            title: "Agent-tool execution failed.",
                            detail: exception.Message,
                            statusCode: StatusCodes.Status500InternalServerError);
                    }
                })
            .WithName("RunCephalonAgentTool");
        engineGroup.MapGet("/event-subscription-readiness", (HttpContext httpContext) =>
            {
                var readiness = httpContext.RequestServices
                    .GetService<IEventSubscriptionExecutionReadinessCatalog>()?
                    .Readiness ?? [];

                return Results.Ok(readiness);
            })
            .WithName("GetCephalonEventSubscriptionExecutionReadiness");
        engineGroup.MapGet("/event-subscription-readiness/{subscriptionId}", (string subscriptionId, HttpContext httpContext) =>
            {
                var readiness = httpContext.RequestServices
                    .GetService<IEventSubscriptionExecutionReadinessCatalog>()?
                    .GetBySubscriptionId(subscriptionId);

                return readiness is null ? Results.NotFound() : Results.Ok(readiness);
            })
            .WithName("GetCephalonEventSubscriptionExecutionReadinessBySubscription");
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
        engineGroup.MapGet("/knowledge-indexes", (HttpContext httpContext) =>
            {
                var catalog = httpContext.RequestServices.GetService<IKnowledgeIndexCatalog>();
                IReadOnlyList<KnowledgeIndexState> states = catalog?.States ?? [];

                return TypedResults.Ok(states);
            })
            .WithName("GetCephalonKnowledgeIndexes");
        engineGroup.MapGet("/knowledge-indexes/{collectionId}", (string collectionId, HttpContext httpContext) =>
            {
                var catalog = httpContext.RequestServices.GetService<IKnowledgeIndexCatalog>();
                var state = catalog?.GetByCollectionId(collectionId);

                return state is null ? Results.NotFound() : Results.Ok(state);
            })
            .WithName("GetCephalonKnowledgeIndex");
        engineGroup.MapPost(
                "/knowledge-indexes/{collectionId}/queries",
                async (
                    string collectionId,
                    [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] KnowledgeQueryHttpRequest? request,
                    HttpContext httpContext,
                    CancellationToken cancellationToken) =>
                {
                    var queryEngine = httpContext.RequestServices.GetService<IKnowledgeQueryEngine>();
                    if (queryEngine is null)
                    {
                        return Results.NotFound(new
                        {
                            error = "Knowledge querying is not available in the active runtime."
                        });
                    }

                    try
                    {
                        var queryRequest = CreateKnowledgeQueryRequest(collectionId, request, httpContext);
                        var result = await queryEngine.QueryAsync(queryRequest, cancellationToken).ConfigureAwait(false);
                        return Results.Ok(result);
                    }
                    catch (ArgumentException exception)
                    {
                        return Results.BadRequest(new { error = exception.Message });
                    }
                    catch (InvalidOperationException exception)
                        when (IsUnregisteredKnowledgeCollection(exception))
                    {
                        return Results.NotFound(new { error = exception.Message });
                    }
                    catch (InvalidOperationException exception)
                    {
                        return Results.Problem(
                            title: "Knowledge query failed.",
                            detail: exception.Message,
                            statusCode: StatusCodes.Status500InternalServerError);
                    }
                })
            .WithName("QueryCephalonKnowledgeIndex");
        engineGroup.MapPost(
                "/knowledge-indexes/{collectionId}/reindex",
                async (
                    string collectionId,
                    [FromQuery] string? runId,
                    [FromQuery] string? actorId,
                    [FromQuery] string? correlationId,
                    HttpContext httpContext,
                    CancellationToken cancellationToken) =>
                {
                    var indexer = httpContext.RequestServices.GetService<IKnowledgeIndexer>();
                    if (indexer is null)
                    {
                        return Results.NotFound(new
                        {
                            error = "Knowledge indexing is not available in the active runtime."
                        });
                    }

                    try
                    {
                        var request = new KnowledgeIndexingRequest(
                            collectionId,
                            string.IsNullOrWhiteSpace(runId) ? CreateKnowledgeReindexRunId() : runId,
                            ResolveKnowledgeOperatorActorId(httpContext, actorId),
                            string.IsNullOrWhiteSpace(correlationId) ? httpContext.TraceIdentifier : correlationId,
                            metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                            {
                                ["trigger"] = "aspnetcore-operator-route",
                                ["route"] = "/engine/knowledge-indexes/{collectionId}/reindex"
                            });

                        var result = await indexer.IndexAsync(request, cancellationToken).ConfigureAwait(false);
                        return Results.Ok(result);
                    }
                    catch (ArgumentException exception)
                    {
                        return Results.BadRequest(new { error = exception.Message });
                    }
                    catch (InvalidOperationException exception)
                        when (IsUnregisteredKnowledgeCollection(exception))
                    {
                        return Results.NotFound(new { error = exception.Message });
                    }
                    catch (InvalidOperationException exception)
                    {
                        return Results.Problem(
                            title: "Knowledge indexing failed.",
                            detail: exception.Message,
                            statusCode: StatusCodes.Status500InternalServerError);
                    }
                })
            .WithName("ReindexCephalonKnowledgeIndex");
        MapGetResultRequestDelegate(engineGroup, "/transports", "GetCephalonTransports", static context =>
            TypedResults.Ok(GetRequiredService<RuntimeManifest>(context).AppProfile.Transports));
        MapGetResultRequestDelegate(engineGroup, "/dependencies", "GetCephalonDependencies", static context =>
            TypedResults.Ok(GetRequiredService<RuntimeHealthEvaluator>(context).EvaluateDependencies()));
        MapGetResultRequestDelegate(engineGroup, "/localization", "GetCephalonLocalization", static context =>
            TypedResults.Ok(GetRequiredService<ILocalizedTextCatalog>(context).CreateSnapshot(GetQueryValue(context, "culture"))));
        MapGetResultRequestDelegate(engineGroup, "/reference-docs", "GetCephalonReferenceDocs", context =>
            TypedResults.Ok(referenceDocsSurface));
        MapGetResultRequestDelegate(engineGroup, "/options", "GetCephalonOptions", static context =>
            TypedResults.Ok(GetRequiredService<EngineOptions>(context)));
        MapGetResultRequestDelegate(engineGroup, "/package-policy", "GetCephalonPackagePolicy", static context =>
            TypedResults.Ok(GetRequiredService<PackagePolicy>(context)));
        MapGetResultRequestDelegate(engineGroup, "/failure-policy", "GetCephalonFailurePolicy", static context =>
            TypedResults.Ok(GetRequiredService<FailurePolicy>(context)));
        MapGetResultRequestDelegate(engineGroup, "/trust-policy", "GetCephalonTrustPolicy", static context =>
            TypedResults.Ok(GetRequiredService<CapabilityPolicyEvaluator>(context).Snapshot));
        MapGetResultRequestDelegate(engineGroup, "/status", "GetCephalonStatus", static context =>
            TypedResults.Ok(GetRequiredService<IRuntime>(context).StatusSnapshot));
        MapGetResultRequestDelegate(engineGroup, "/runtime-story", "GetCephalonRuntimeStory", static context =>
            TypedResults.Ok(GetRequiredService<IRuntime>(context).OperationalStory));
        MapGetResultRequestDelegate(engineGroup, "/diagnostics", "GetCephalonDiagnostics", static context =>
            TypedResults.Ok(CreateDiagnosticsSurface(
                GetRequiredService<RuntimeHealthEvaluator>(context),
                GetRequiredService<IRuntimeDiagnosticsCatalog>(context))));
        MapGetResultRequestDelegate(engineGroup, "/diagnostics-conventions", "GetCephalonDiagnosticsConventions", static context =>
            TypedResults.Ok(CreateDiagnosticsConventionsSurface()));
        MapGetResultRequestDelegate(engineGroup, "/modules/{moduleId}", "GetCephalonModule", static context =>
            {
                var moduleId = GetRouteValue(context, "moduleId");
                var manifest = GetRequiredService<RuntimeManifest>(context);
                var module = manifest.Modules.FirstOrDefault(item =>
                    string.Equals(item.Id, moduleId, StringComparison.OrdinalIgnoreCase));

                return module is null ? Results.NotFound() : Results.Ok(module);
            });

        MapCephalonHostInfrastructure(
            app,
            runtime,
            configuration,
            referenceDocsOptions,
            referenceDocsSurface,
            openApiEndpointOptions,
            openApiDocumentNames,
            defaultOpenApiDocumentName,
            openApiToggleScriptRoute,
            scalarFaviconRoute,
            openApiToggleScriptReference,
            scalarFaviconReference,
            restApiSelected);

        return app;
    }

    private static AspNetCoreOperatorSurfaceMode ResolveOperatorSurfaceMode(IConfiguration configuration)
    {
        var configuredValue = configuration[OperatorSurfaceModeConfigurationKey];
        if (string.IsNullOrWhiteSpace(configuredValue))
        {
            return AspNetCoreOperatorSurfaceMode.Full;
        }

        var normalized = configuredValue.Trim();
        if (string.Equals(normalized, "full", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalized, "default", StringComparison.OrdinalIgnoreCase))
        {
            return AspNetCoreOperatorSurfaceMode.Full;
        }

        if (string.Equals(normalized, "core", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalized, "minimal", StringComparison.OrdinalIgnoreCase))
        {
            return AspNetCoreOperatorSurfaceMode.Core;
        }

        throw new InvalidOperationException(
            $"Unsupported Cephalon ASP.NET Core operator surface mode '{configuredValue}'. Configure '{OperatorSurfaceModeConfigurationKey}' as 'full' or 'core'.");
    }

    private static void MapCephalonFullCommonOperatorRoutes(RouteGroupBuilder engineGroup)
    {
        MapGetRequestDelegate(engineGroup, "/", "GetCephalonManifest", static context =>
            WriteOkAsync(context, GetRequiredService<RuntimeManifest>(context)));
        MapGetRequestDelegate(engineGroup, "/manifest", "GetCephalonManifestByPath", static context =>
            WriteOkAsync(context, GetRequiredService<RuntimeManifest>(context)));
        MapGetRequestDelegate(engineGroup, "/snapshot", "GetCephalonSnapshot", static context =>
            WriteOkAsync(context, GetRequiredService<IRuntimeIntrospectionSnapshotProvider>(context).CreateSnapshot()));
        MapGetRequestDelegate(engineGroup, "/app-model", "GetCephalonAppModel", static context =>
            WriteOkAsync(context, GetRequiredService<RuntimeManifest>(context).AppProfile));
        MapGetRequestDelegate(engineGroup, "/resilience", "GetCephalonResilience", static context =>
            WriteOkAsync(context, GetRequiredService<RuntimeManifest>(context).AppProfile.Resilience));
    }

    private static void MapCephalonCoreOperatorRoutes(RouteGroupBuilder engineGroup, ReferenceDocsSurface referenceDocsSurface)
    {
        MapGetRequestDelegate(engineGroup, "/", "GetCephalonManifest", static context =>
            WriteOkAsync(context, GetRequiredService<RuntimeManifest>(context)));
        MapGetRequestDelegate(engineGroup, "/manifest", "GetCephalonManifestByPath", static context =>
            WriteOkAsync(context, GetRequiredService<RuntimeManifest>(context)));
        MapGetRequestDelegate(engineGroup, "/snapshot", "GetCephalonSnapshot", static context =>
            WriteOkAsync(context, GetRequiredService<IRuntimeIntrospectionSnapshotProvider>(context).CreateSnapshot()));
        MapGetRequestDelegate(engineGroup, "/app-model", "GetCephalonAppModel", static context =>
            WriteOkAsync(context, GetRequiredService<RuntimeManifest>(context).AppProfile));
        MapGetRequestDelegate(engineGroup, "/resilience", "GetCephalonResilience", static context =>
            WriteOkAsync(context, GetRequiredService<RuntimeManifest>(context).AppProfile.Resilience));
        MapGetRequestDelegate(engineGroup, "/scaffold", "GetCephalonScaffold", static context =>
        {
            var scaffold = GetRequiredService<RuntimeManifest>(context).AppProfile.Scaffold;
            return scaffold is null
                ? WriteResultAsync(context, Results.NotFound())
                : WriteOkAsync(context, scaffold);
        });
        MapGetRequestDelegate(engineGroup, "/capabilities", "GetCephalonCapabilities", static context =>
            WriteOkAsync(context, GetRequiredService<RuntimeManifest>(context).Capabilities));
        MapGetRequestDelegate(engineGroup, "/modules", "GetCephalonModules", static context =>
            WriteOkAsync(context, GetRequiredService<RuntimeManifest>(context).Modules));
        MapGetRequestDelegate(engineGroup, "/packages", "GetCephalonPackages", static context =>
            WriteOkAsync(context, GetRequiredService<RuntimeManifest>(context).Packages));
        MapGetRequestDelegate(engineGroup, "/patterns", "GetCephalonPatterns", static context =>
            WriteOkAsync(context, GetRequiredService<RuntimeManifest>(context).AppProfile.Patterns));
        MapGetRequestDelegate(engineGroup, "/technologies", "GetCephalonTechnologies", static context =>
            WriteOkAsync(context, GetRequiredService<RuntimeManifest>(context).AppProfile.Technologies));
        MapGetRequestDelegate(engineGroup, "/technology-catalog", "GetCephalonTechnologyCatalog", static context =>
            WriteOkAsync(context, GetRequiredService<TechnologyCatalogSnapshot>(context).Technologies));
        MapGetRequestDelegate(engineGroup, "/technology-surfaces", "GetCephalonTechnologySurfaces", static context =>
            WriteOkAsync(context, GetRequiredService<ITechnologyRuntimeCatalog>(context).Surfaces));
        MapGetRequestDelegate(engineGroup, "/technology-surfaces/{technologyId}", "GetCephalonTechnologySurface", static context =>
            WriteOkAsync(
                context,
                GetRequiredService<ITechnologyRuntimeCatalog>(context).GetByTechnology(GetRouteValue(context, "technologyId"))));
        MapGetRequestDelegate(engineGroup, "/transports", "GetCephalonTransports", static context =>
            WriteOkAsync(context, GetRequiredService<RuntimeManifest>(context).AppProfile.Transports));
        MapGetRequestDelegate(engineGroup, "/dependencies", "GetCephalonDependencies", static context =>
            WriteOkAsync(context, GetRequiredService<RuntimeHealthEvaluator>(context).EvaluateDependencies()));
        MapGetRequestDelegate(engineGroup, "/localization", "GetCephalonLocalization", static context =>
            WriteOkAsync(
                context,
                GetRequiredService<ILocalizedTextCatalog>(context).CreateSnapshot(GetQueryValue(context, "culture"))));
        MapGetRequestDelegate(engineGroup, "/reference-docs", "GetCephalonReferenceDocs", context =>
            WriteOkAsync(context, referenceDocsSurface));
        MapGetRequestDelegate(engineGroup, "/options", "GetCephalonOptions", static context =>
            WriteOkAsync(context, GetRequiredService<EngineOptions>(context)));
        MapGetRequestDelegate(engineGroup, "/package-policy", "GetCephalonPackagePolicy", static context =>
            WriteOkAsync(context, GetRequiredService<PackagePolicy>(context)));
        MapGetRequestDelegate(engineGroup, "/failure-policy", "GetCephalonFailurePolicy", static context =>
            WriteOkAsync(context, GetRequiredService<FailurePolicy>(context)));
        MapGetRequestDelegate(engineGroup, "/trust-policy", "GetCephalonTrustPolicy", static context =>
            WriteOkAsync(context, GetRequiredService<CapabilityPolicyEvaluator>(context).Snapshot));
        MapGetRequestDelegate(engineGroup, "/status", "GetCephalonStatus", static context =>
            WriteOkAsync(context, GetRequiredService<IRuntime>(context).StatusSnapshot));
        MapGetRequestDelegate(engineGroup, "/runtime-story", "GetCephalonRuntimeStory", static context =>
            WriteOkAsync(context, GetRequiredService<IRuntime>(context).OperationalStory));
        MapGetRequestDelegate(engineGroup, "/diagnostics", "GetCephalonDiagnostics", static context =>
            WriteOkAsync(
                context,
                CreateDiagnosticsSurface(
                    GetRequiredService<RuntimeHealthEvaluator>(context),
                    GetRequiredService<IRuntimeDiagnosticsCatalog>(context))));
        MapGetRequestDelegate(engineGroup, "/diagnostics-conventions", "GetCephalonDiagnosticsConventions", static context =>
            WriteOkAsync(context, CreateDiagnosticsConventionsSurface()));
        MapGetRequestDelegate(engineGroup, "/modules/{moduleId}", "GetCephalonModule", static context =>
            {
                var moduleId = GetRouteValue(context, "moduleId");
                var manifest = GetRequiredService<RuntimeManifest>(context);
                var module = manifest.Modules.FirstOrDefault(item =>
                    string.Equals(item.Id, moduleId, StringComparison.OrdinalIgnoreCase));

                return module is null
                    ? WriteResultAsync(context, Results.NotFound())
                    : WriteOkAsync(context, module);
            });
    }

    private static void MapGetRequestDelegate(
        RouteGroupBuilder engineGroup,
        string pattern,
        string endpointName,
        RequestDelegate requestDelegate)
    {
        engineGroup.MapMethods(pattern, [HttpMethods.Get], requestDelegate)
            .WithName(endpointName);
    }

    private static void MapGetResultRequestDelegate(
        RouteGroupBuilder engineGroup,
        string pattern,
        string endpointName,
        Func<HttpContext, IResult> handler)
    {
        MapGetRequestDelegate(engineGroup, pattern, endpointName, context =>
            handler(context).ExecuteAsync(context));
    }

    private static void MapCdcCaptureRuntimeCollectionRoute(
        RouteGroupBuilder engineGroup,
        string pattern,
        string endpointName,
        Func<HttpContext, ICdcCaptureExecutionRuntimeCatalog, IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor>> selector)
    {
        MapGetResultRequestDelegate(engineGroup, pattern, endpointName, context =>
        {
            var catalog = context.RequestServices.GetService<ICdcCaptureExecutionRuntimeCatalog>();
            IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> runtimes = catalog is null ? [] : selector(context, catalog);

            return Results.Ok(runtimes);
        });
    }

    private static void MapCdcCaptureRuntimeDescriptorRoute<TResult>(
        RouteGroupBuilder engineGroup,
        string pattern,
        string endpointName,
        Func<CdcCaptureExecutionRuntimeDescriptor, TResult> selector)
    {
        MapGetResultRequestDelegate(engineGroup, pattern, endpointName, context =>
        {
            var runtimeDescriptor = context.RequestServices
                .GetService<ICdcCaptureExecutionRuntimeCatalog>()?
                .GetById(GetRouteValue(context, "executionRuntimeId"));

            return runtimeDescriptor is null ? Results.NotFound() : Results.Ok(selector(runtimeDescriptor));
        });
    }

    private static IResult GetCdcCaptureRuntimeCommandExecutionHistory(HttpContext context)
    {
        var executionRuntimeId = GetRouteValue(context, "executionRuntimeId");
        var runtimeCatalog = context.RequestServices.GetService<ICdcCaptureExecutionRuntimeCatalog>();
        var runtimeDescriptor = runtimeCatalog?.GetById(executionRuntimeId);
        if (runtimeCatalog is null || runtimeDescriptor is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(runtimeCatalog.GetManagedConnectorCommandExecutionHistory(executionRuntimeId));
    }

    private static TService GetRequiredService<TService>(HttpContext context)
        where TService : notnull
    {
        return context.RequestServices.GetRequiredService<TService>();
    }

    private static string GetRouteValue(HttpContext context, string name)
    {
        var value = context.Request.RouteValues[name]?.ToString();
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value;
    }

    private static bool GetBooleanRouteValue(HttpContext context, string name)
    {
        return bool.TryParse(GetRouteValue(context, name), out var value) && value;
    }

    private static string? GetQueryValue(HttpContext context, string name)
    {
        return context.Request.Query.TryGetValue(name, out var values) && values.Count > 0
            ? values.ToString()
            : null;
    }

    private static Task WriteOkAsync<TValue>(HttpContext context, TValue value)
    {
        return WriteResultAsync(context, Results.Ok(value));
    }

    private static Task WriteResultAsync(HttpContext context, IResult result)
    {
        return result.ExecuteAsync(context);
    }

    private static DiagnosticsSurface CreateDiagnosticsSurface(
        RuntimeHealthEvaluator health,
        IRuntimeDiagnosticsCatalog diagnosticsCatalog)
    {
        return new DiagnosticsSurface(
            MeterName: EngineDiagnostics.MeterName,
            ActivitySourceName: EngineDiagnostics.ActivitySourceName,
            Counters: GetDiagnosticsCounters(),
            Conventions: diagnosticsCatalog.Conventions,
            Liveness: health.EvaluateLiveness(),
            Readiness: health.EvaluateReadiness(),
            SummaryPath: "/health",
            LivenessPath: "/health/live",
            ReadinessPath: "/health/ready");
    }

    private static string[] GetDiagnosticsCounters()
    {
        return
        [
            EngineDiagnostics.EngineBuildCounterName,
            EngineDiagnostics.RuntimeTransitionCounterName,
            EngineDiagnostics.ModuleTransitionCounterName,
            EngineDiagnostics.ExecutionGraphTransitionCounterName,
            EngineDiagnostics.HostedExecutionTransitionCounterName,
            EngineDiagnostics.RuntimeFailureCounterName,
            EngineDiagnostics.ModuleFailureCounterName,
            EngineDiagnostics.RuntimeRestartCounterName
        ];
    }

    private static DiagnosticsConventionsSurface CreateDiagnosticsConventionsSurface()
    {
        return new DiagnosticsConventionsSurface(
            ActivitySources:
            [
                Cephalon.Diagnostics.CephalonActivitySources.Engine,
                Cephalon.Diagnostics.CephalonActivitySources.AspNetCore,
                Cephalon.Diagnostics.CephalonActivitySources.Worker
            ],
            Meters:
            [
                Cephalon.Diagnostics.CephalonMeters.Engine,
                Cephalon.Diagnostics.CephalonMeters.AspNetCore,
                Cephalon.Diagnostics.CephalonMeters.Worker
            ],
            CephalonAttributeKeys:
            [
                Cephalon.Diagnostics.CephalonDiagnosticsAttributeKeys.ModuleId,
                Cephalon.Diagnostics.CephalonDiagnosticsAttributeKeys.BehaviorId,
                Cephalon.Diagnostics.CephalonDiagnosticsAttributeKeys.CellId,
                Cephalon.Diagnostics.CephalonDiagnosticsAttributeKeys.AppBlueprint,
                Cephalon.Diagnostics.CephalonDiagnosticsAttributeKeys.TenantId
            ]);
    }

    [RequiresUnreferencedCode(DynamicRouteMappingRequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(DynamicRouteMappingRequiresDynamicCodeMessage)]
    private static void MapCephalonHostInfrastructure(
        WebApplication app,
        IRuntime runtime,
        IConfiguration configuration,
        ReferenceDocsHostingOptions referenceDocsOptions,
        ReferenceDocsSurface referenceDocsSurface,
        OpenApiEndpointOptions openApiEndpointOptions,
        IReadOnlyList<string> openApiDocumentNames,
        string defaultOpenApiDocumentName,
        string openApiToggleScriptRoute,
        string scalarFaviconRoute,
        string openApiToggleScriptReference,
        string scalarFaviconReference,
        bool restApiSelected)
    {
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
    }

    private enum AspNetCoreOperatorSurfaceMode
    {
        Full,
        Core
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

    [RequiresUnreferencedCode(DynamicRouteMappingRequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(DynamicRouteMappingRequiresDynamicCodeMessage)]
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
            JsonSerializer.Serialize(
                documentNames.ToArray(),
                AspNetCoreJsonSerializerContext.Default.StringArray),
            StringComparison.Ordinal);
        rendered = rendered.Replace(
            ScalarDefaultDocumentNameToken,
            JsonSerializer.Serialize(
                defaultDocumentName,
                AspNetCoreJsonSerializerContext.Default.String),
            StringComparison.Ordinal);

        return rendered;
    }

    [RequiresUnreferencedCode(DynamicRouteMappingRequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(DynamicRouteMappingRequiresDynamicCodeMessage)]
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

    [RequiresUnreferencedCode(DynamicRouteMappingRequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(DynamicRouteMappingRequiresDynamicCodeMessage)]
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

    [RequiresUnreferencedCode(DynamicRouteMappingRequiresUnreferencedCodeMessage)]
    [RequiresDynamicCode(DynamicRouteMappingRequiresDynamicCodeMessage)]
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

    private static EventPublicationRequest CreateEventPublicationRequest(
        EventPublicationHttpRequest? request,
        HttpContext httpContext)
    {
        if (request is null)
        {
            throw new ArgumentException("Event publication request body is required.", nameof(request));
        }

        var metadata = CopyEventPublicationValues(request.Metadata);
        metadata["trigger"] = "aspnetcore-operator-route";
        metadata["route"] = "/engine/event-publications";

        var actorId = ResolveEventPublicationActorId(httpContext, request.ActorId);
        if (!string.IsNullOrWhiteSpace(actorId))
        {
            metadata["actorId"] = actorId;
        }

        return new EventPublicationRequest(
            request.ChannelId ?? string.Empty,
            request.EventType ?? string.Empty,
            SerializeEventPublicationPayload(request.Payload),
            request.Id,
            request.OccurredAtUtc,
            request.ContentType,
            string.IsNullOrWhiteSpace(request.CorrelationId) ? httpContext.TraceIdentifier : request.CorrelationId,
            request.TenantId,
            request.Headers,
            metadata);
    }

    private static string SerializeEventPublicationPayload(JsonElement? payload)
    {
        if (payload is null || payload.Value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            throw new ArgumentException("Event publication payload is required.", nameof(payload));
        }

        return payload.Value.ValueKind == JsonValueKind.String
            ? payload.Value.GetString() ?? string.Empty
            : payload.Value.GetRawText();
    }

    private static string? ResolveEventPublicationActorId(HttpContext httpContext, string? actorId)
    {
        if (!string.IsNullOrWhiteSpace(actorId))
        {
            return actorId.Trim();
        }

        var userName = httpContext.User.Identity?.Name;
        return string.IsNullOrWhiteSpace(userName) ? null : userName.Trim();
    }

    private static bool IsUnregisteredEventChannel(InvalidOperationException exception)
    {
        return exception.Message.Contains(
            "is not registered in the active eventing runtime",
            StringComparison.OrdinalIgnoreCase);
    }

    private static Dictionary<string, string> CopyEventPublicationValues(IReadOnlyDictionary<string, string>? values)
    {
        if (values is null)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        return values
            .Where(static pair => !string.IsNullOrWhiteSpace(pair.Key))
            .ToDictionary(
                static pair => pair.Key.Trim(),
                static pair => pair.Value,
                StringComparer.OrdinalIgnoreCase);
    }

    private static AgentToolExecutionRequest CreateAgentToolExecutionRequest(
        string toolId,
        AgentToolExecutionHttpRequest? request,
        HttpContext httpContext)
    {
        var metadata = CopyAgentToolMetadata(request?.Metadata);
        metadata["trigger"] = "aspnetcore-operator-route";
        metadata["route"] = "/engine/agent-tools/{toolId}/runs";

        return new AgentToolExecutionRequest(
            toolId,
            string.IsNullOrWhiteSpace(request?.RunId) ? CreateAgentToolRunId() : request.RunId,
            request?.Arguments,
            ResolveAgentToolActorId(httpContext, request?.ActorId),
            string.IsNullOrWhiteSpace(request?.CorrelationId) ? httpContext.TraceIdentifier : request.CorrelationId,
            request?.Attempt ?? 1,
            metadata);
    }

    private static string CreateAgentToolRunId()
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"aspnetcore-agent-tool-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}");
    }

    private static string? ResolveAgentToolActorId(HttpContext httpContext, string? actorId)
    {
        if (!string.IsNullOrWhiteSpace(actorId))
        {
            return actorId.Trim();
        }

        var userName = httpContext.User.Identity?.Name;
        return string.IsNullOrWhiteSpace(userName) ? null : userName.Trim();
    }

    private static bool IsUnregisteredAgentTool(InvalidOperationException exception)
    {
        return exception.Message.Contains(
            "is not registered in the active agentics runtime",
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsMissingAgentToolExecutor(InvalidOperationException exception)
    {
        return exception.Message.Contains(
            "No agent tool executor is registered",
            StringComparison.OrdinalIgnoreCase);
    }

    private static Dictionary<string, string> CopyAgentToolMetadata(IReadOnlyDictionary<string, string>? metadata)
    {
        if (metadata is null)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        return metadata
            .Where(static pair => !string.IsNullOrWhiteSpace(pair.Key))
            .ToDictionary(
                static pair => pair.Key.Trim(),
                static pair => pair.Value,
                StringComparer.OrdinalIgnoreCase);
    }

    private static string CreateKnowledgeReindexRunId()
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"aspnetcore-reindex-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}");
    }

    private static KnowledgeQueryRequest CreateKnowledgeQueryRequest(
        string collectionId,
        KnowledgeQueryHttpRequest? request,
        HttpContext httpContext)
    {
        var metadata = CopyKnowledgeQueryMetadata(request?.Metadata);
        metadata["trigger"] = "aspnetcore-operator-route";
        metadata["route"] = "/engine/knowledge-indexes/{collectionId}/queries";

        return new KnowledgeQueryRequest(
            collectionId,
            request?.QueryText ?? string.Empty,
            request?.MaxResults,
            ResolveKnowledgeOperatorActorId(httpContext, request?.ActorId),
            string.IsNullOrWhiteSpace(request?.CorrelationId) ? httpContext.TraceIdentifier : request.CorrelationId,
            metadata);
    }

    private static Dictionary<string, string> CopyKnowledgeQueryMetadata(IReadOnlyDictionary<string, string>? metadata)
    {
        if (metadata is null)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        return metadata
            .Where(static pair => !string.IsNullOrWhiteSpace(pair.Key))
            .ToDictionary(
                static pair => pair.Key.Trim(),
                static pair => pair.Value,
                StringComparer.OrdinalIgnoreCase);
    }

    private static string? ResolveKnowledgeOperatorActorId(HttpContext httpContext, string? actorId)
    {
        if (!string.IsNullOrWhiteSpace(actorId))
        {
            return actorId.Trim();
        }

        var userName = httpContext.User.Identity?.Name;
        return string.IsNullOrWhiteSpace(userName) ? null : userName.Trim();
    }

    private static bool IsUnregisteredKnowledgeCollection(InvalidOperationException exception)
    {
        return exception.Message.Contains(
            "is not registered in the active retrieval runtime",
            StringComparison.OrdinalIgnoreCase);
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
