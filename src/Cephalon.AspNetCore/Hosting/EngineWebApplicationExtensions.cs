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
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Cephalon.AspNetCore.Hosting;

/// <summary>
/// Maps the operator-facing HTTP surface exposed by a Cephalon ASP.NET Core host.
/// </summary>
public static class EngineWebApplicationExtensions
{
    private const string DynamicRouteMappingRequiresUnreferencedCodeMessage =
        "Cephalon.AspNetCore still carries an explicit trim boundary for the full adapter endpoint surface. " +
        "Use it only when the remaining non-promoted endpoint surfaces are acceptable.";
    private const string DynamicRouteMappingRequiresDynamicCodeMessage =
        "Cephalon.AspNetCore still carries an explicit Native AOT boundary for the full adapter endpoint surface. " +
        "Use it only when the remaining non-promoted endpoint surfaces are acceptable.";
    private const string OpenApiToggleScriptResourceName = "Cephalon.AspNetCore.Assets.openapi-toggle.js";
    private const string ScalarFaviconResourceName = "Cephalon.AspNetCore.Assets.docs-favicon.svg";
    private const string ScalarRoutePrefixToken = "__CEPHALON_SCALAR_ROUTE_PREFIX__";
    private const string ScalarDocumentNamesToken = "__CEPHALON_SCALAR_DOCUMENT_NAMES__";
    private const string ScalarDefaultDocumentNameToken = "__CEPHALON_SCALAR_DEFAULT_DOCUMENT_NAME__";
    private const string OperatorSurfaceModeConfigurationKey = "Engine:AspNetCore:OperatorSurface:Mode";
    private const string EventDispatchRemediationCommandContinuationTokenMagic = "CEPCMD1";
    private const int EventDispatchRemediationCommandContinuationScopeHashLength = 32;
    private const int EventDispatchRemediationCommandContinuationSignatureLength = 32;
    private const int DefaultEventDispatchRemediationCommandPageSize = 50;
    private const int MaxEventDispatchRemediationCommandPageSize = 500;
    private static readonly byte[] EventDispatchRemediationCommandContinuationSigningKey = RandomNumberGenerator.GetBytes(32);
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
    /// The full operator route surface now avoids direct ASP.NET Core Minimal API delegate binding for the
    /// operator catalog, common operator responses have source-generated JSON metadata, and Cephalon-owned
    /// non-operator documentation endpoints use request delegates; framework health, OpenAPI, and Scalar
    /// endpoints are still audited as an explicit trim and Native AOT boundary until full-adapter support is
    /// promoted deliberately.
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
            MapPostAsyncResultRequestDelegate(engineGroup, "/cdc-capture-runtimes/{executionRuntimeId}/reports", "PostCephalonCdcCaptureRuntimeReports", static async context =>
                {
                    var executionRuntimeId = GetRouteValue(context, "executionRuntimeId");
                    var (observations, bodyError) = await ReadOptionalJsonBodyAsync(
                            context,
                            AspNetCoreJsonSerializerContext.Default.CdcCaptureRuntimeObservationArray)
                        .ConfigureAwait(false);
                    if (bodyError is not null)
                    {
                        return bodyError;
                    }

                    if (observations is null || observations.Length == 0)
                    {
                        return Results.BadRequest("At least one CDC capture runtime observation is required.");
                    }

                    var runtimeCatalog = context.RequestServices.GetService<ICdcCaptureExecutionRuntimeCatalog>();
                    var runtimeDescriptor = runtimeCatalog?.GetById(executionRuntimeId);
                    if (runtimeDescriptor is null)
                    {
                        return Results.NotFound();
                    }

                    var reportSink = GetRequiredService<ICdcCaptureExecutionRuntimeReportSink>(context);

                    try
                    {
                        await reportSink.ReportAsync(executionRuntimeId, observations, context.RequestAborted);
                    }
                    catch (InvalidOperationException exception)
                    {
                        return Results.BadRequest(exception.Message);
                    }

                    return Results.Ok(runtimeCatalog?.GetById(executionRuntimeId) ?? runtimeDescriptor);
                });
        }
        if (app.Services.GetService<ICdcCaptureExecutionRuntimeManagedConnectorCommandExecutor>() is not null)
        {
            MapPostAsyncResultRequestDelegate(engineGroup, "/cdc-capture-runtimes/{executionRuntimeId}/commands/{operationId}", "PostCephalonManagedConnectorCommandExecution", static async context =>
                {
                    var executionRuntimeId = GetRouteValue(context, "executionRuntimeId");
                    var operationId = GetRouteValue(context, "operationId");
                    var (request, bodyError) = await ReadOptionalJsonBodyAsync(
                            context,
                            AspNetCoreJsonSerializerContext.Default.CdcCaptureExecutionRuntimeManagedConnectorCommandExecutionRequest)
                        .ConfigureAwait(false);
                    if (bodyError is not null)
                    {
                        return bodyError;
                    }

                    var runtimeCatalog = context.RequestServices.GetService<ICdcCaptureExecutionRuntimeCatalog>();
                    var runtimeDescriptor = runtimeCatalog?.GetById(executionRuntimeId);
                    if (runtimeDescriptor is null)
                    {
                        return Results.NotFound();
                    }

                    var executor = GetRequiredService<ICdcCaptureExecutionRuntimeManagedConnectorCommandExecutor>(context);

                    try
                    {
                        var result = await executor.ExecuteAsync(
                            executionRuntimeId,
                            operationId,
                            request,
                            context.RequestAborted);

                        return Results.Ok(result);
                    }
                    catch (InvalidOperationException exception)
                    {
                        return Results.BadRequest(exception.Message);
                    }
                });
        }
        MapGetResultRequestDelegate(engineGroup, "/cdc-captures/runtime", "GetCephalonCdcCaptureStates", static context =>
            {
                var catalog = context.RequestServices.GetService<ICdcCaptureRuntimeStateCatalog>();
                return TypedResults.Ok(catalog?.States ?? []);
            });
        MapGetResultRequestDelegate(engineGroup, "/cdc-captures/runtime/{cdcCaptureId}", "GetCephalonCdcCaptureState", static context =>
            {
                var cdcCaptureId = GetRouteValue(context, "cdcCaptureId");
                var catalog = context.RequestServices.GetService<ICdcCaptureRuntimeStateCatalog>();
                var state = catalog?.GetById(cdcCaptureId);

                return state is null ? Results.NotFound() : Results.Ok(state);
            });
        MapGetResultRequestDelegate(engineGroup, "/cdc-captures/runtime/modules/{moduleId}", "GetCephalonCdcCaptureStatesByModule", static context =>
            {
                var moduleId = GetRouteValue(context, "moduleId");
                var catalog = context.RequestServices.GetService<ICdcCaptureRuntimeStateCatalog>();
                return TypedResults.Ok(catalog?.GetBySourceModule(moduleId) ?? []);
            });
        MapGetResultRequestDelegate(engineGroup, "/cdc-captures/runtime/providers/{provider}", "GetCephalonCdcCaptureStatesByProvider", static context =>
            {
                var provider = GetRouteValue(context, "provider");
                var catalog = context.RequestServices.GetService<ICdcCaptureRuntimeStateCatalog>();
                return TypedResults.Ok(catalog?.GetByProvider(provider) ?? []);
            });
        MapGetResultRequestDelegate(engineGroup, "/cdc-captures/runtime/outboxes/{outboxId}", "GetCephalonCdcCaptureStatesByOutbox", static context =>
            {
                var outboxId = GetRouteValue(context, "outboxId");
                var catalog = context.RequestServices.GetService<ICdcCaptureRuntimeStateCatalog>();
                return TypedResults.Ok(catalog?.GetByOutboxId(outboxId) ?? []);
            });
        MapGetResultRequestDelegate(engineGroup, "/cdc-captures/runtime/sources/{sourceId}", "GetCephalonCdcCaptureStatesBySource", static context =>
            {
                var sourceId = GetRouteValue(context, "sourceId");
                var catalog = context.RequestServices.GetService<ICdcCaptureRuntimeStateCatalog>();
                return TypedResults.Ok(catalog?.GetBySourceId(sourceId) ?? []);
            });
        MapGetResultRequestDelegate(engineGroup, "/cdc-captures/runtime/resources/{resourceId}", "GetCephalonCdcCaptureStatesByResource", static context =>
            {
                var resourceId = GetRouteValue(context, "resourceId");
                var catalog = context.RequestServices.GetService<ICdcCaptureRuntimeStateCatalog>();
                return TypedResults.Ok(catalog?.GetByResourceId(resourceId) ?? []);
            });
        MapGetResultRequestDelegate(engineGroup, "/cdc-captures/runtime/execution-runtimes/{executionRuntimeId}", "GetCephalonCdcCaptureStatesByExecutionRuntime", static context =>
            {
                var executionRuntimeId = GetRouteValue(context, "executionRuntimeId");
                var catalog = context.RequestServices.GetService<ICdcCaptureRuntimeStateCatalog>();
                var states = catalog?.GetByExecutionRuntimeId(executionRuntimeId) ?? [];

                return TypedResults.Ok(states);
            });
        MapGetResultRequestDelegate(engineGroup, "/cdc-captures/runtime/reporters/{reporterId}", "GetCephalonCdcCaptureStatesByReporter", static context =>
            {
                var reporterId = GetRouteValue(context, "reporterId");
                var catalog = context.RequestServices.GetService<ICdcCaptureRuntimeStateCatalog>();
                return TypedResults.Ok(catalog?.GetByReporterId(reporterId) ?? []);
            });
        MapGetResultRequestDelegate(engineGroup, "/cdc-captures/runtime/edge-nodes/{edgeNodeId}", "GetCephalonCdcCaptureStatesByEdgeNode", static context =>
            {
                var edgeNodeId = GetRouteValue(context, "edgeNodeId");
                var catalog = context.RequestServices.GetService<ICdcCaptureRuntimeStateCatalog>();
                return TypedResults.Ok(catalog?.GetByEdgeNodeId(edgeNodeId) ?? []);
            });
        MapGetResultRequestDelegate(engineGroup, "/cdc-captures/runtime/reporter-coordination/{coordinationState}", "GetCephalonCdcCaptureStatesByReporterCoordinationState", static context =>
            {
                var coordinationState = GetRouteValue(context, "coordinationState");
                var catalog = context.RequestServices.GetService<ICdcCaptureRuntimeStateCatalog>();
                return TypedResults.Ok(catalog?.GetByReporterCoordinationState(coordinationState) ?? []);
            });
        MapGetResultRequestDelegate(engineGroup, "/cdc-captures/runtime/reporter-coordination/issues/{degradedReason}", "GetCephalonCdcCaptureStatesByReporterCoordinationIssueReason", static context =>
            {
                var degradedReason = GetRouteValue(context, "degradedReason");
                var catalog = context.RequestServices.GetService<ICdcCaptureRuntimeStateCatalog>();
                return TypedResults.Ok(catalog?.GetByReporterCoordinationIssueReason(degradedReason) ?? []);
            });
        MapGetResultRequestDelegate(engineGroup, "/cdc-captures/{cdcCaptureId}", "GetCephalonCdcCapture", static context =>
            {
                var cdcCaptureId = GetRouteValue(context, "cdcCaptureId");
                var catalog = GetRequiredService<ICdcCaptureCatalog>(context);
                var cdcCapture = catalog.GetById(cdcCaptureId);

                return cdcCapture is null ? Results.NotFound() : Results.Ok(cdcCapture);
            });
        MapGetResultRequestDelegate(engineGroup, "/cdc-captures/modules/{moduleId}", "GetCephalonCdcCapturesByModule", static context =>
            TypedResults.Ok(GetRequiredService<ICdcCaptureCatalog>(context).GetBySourceModule(GetRouteValue(context, "moduleId"))));
        MapGetResultRequestDelegate(engineGroup, "/cdc-captures/providers/{provider}", "GetCephalonCdcCapturesByProvider", static context =>
            TypedResults.Ok(GetRequiredService<ICdcCaptureCatalog>(context).GetByProvider(GetRouteValue(context, "provider"))));
        MapGetResultRequestDelegate(engineGroup, "/cdc-captures/outboxes/{outboxId}", "GetCephalonCdcCapturesByOutbox", static context =>
            TypedResults.Ok(GetRequiredService<ICdcCaptureCatalog>(context).GetByOutboxId(GetRouteValue(context, "outboxId"))));
        MapGetResultRequestDelegate(engineGroup, "/cdc-captures/sources/{sourceId}", "GetCephalonCdcCapturesBySource", static context =>
            TypedResults.Ok(GetRequiredService<ICdcCaptureCatalog>(context).GetBySourceId(GetRouteValue(context, "sourceId"))));
        MapGetResultRequestDelegate(engineGroup, "/cdc-captures/resources/{resourceId}", "GetCephalonCdcCapturesByResource", static context =>
            TypedResults.Ok(GetRequiredService<ICdcCaptureCatalog>(context).GetByResourceId(GetRouteValue(context, "resourceId"))));
        MapGetResultRequestDelegate(engineGroup, "/cdc-captures/execution-runtimes/{executionRuntimeId}", "GetCephalonCdcCapturesByExecutionRuntime", static context =>
            TypedResults.Ok(GetRequiredService<ICdcCaptureCatalog>(context).GetByExecutionRuntimeId(GetRouteValue(context, "executionRuntimeId"))));
        MapGetResultRequestDelegate(engineGroup, "/projections", "GetCephalonProjections", static context =>
            TypedResults.Ok(GetRequiredService<IProjectionCatalog>(context).Projections));
        MapGetResultRequestDelegate(engineGroup, "/projections/{projectionId}", "GetCephalonProjection", static context =>
            {
                var projectionId = GetRouteValue(context, "projectionId");
                var catalog = GetRequiredService<IProjectionCatalog>(context);
                var projection = catalog.GetById(projectionId);

                return projection is null ? Results.NotFound() : Results.Ok(projection);
            });
        MapGetResultRequestDelegate(engineGroup, "/outboxes", "GetCephalonOutboxes", static context =>
            TypedResults.Ok(GetRequiredService<IOutboxCatalog>(context).Outboxes));
        MapGetResultRequestDelegate(engineGroup, "/outboxes/{outboxId}", "GetCephalonOutbox", static context =>
            {
                var outboxId = GetRouteValue(context, "outboxId");
                var catalog = GetRequiredService<IOutboxCatalog>(context);
                var outbox = catalog.GetById(outboxId);

                return outbox is null ? Results.NotFound() : Results.Ok(outbox);
            });
        MapGetResultRequestDelegate(engineGroup, "/event-dispatch-runtimes", "GetCephalonEventDispatchRuntimes", static context =>
            {
                var runtimes = context.RequestServices
                    .GetService<IEventDispatchRuntimeDescriptorCatalog>()?
                    .Runtimes ?? [];

                return Results.Ok(runtimes);
            });
        MapGetResultRequestDelegate(engineGroup, "/event-dispatch-runtimes/{dispatchRuntimeId}", "GetCephalonEventDispatchRuntime", static context =>
            {
                var dispatchRuntimeId = GetRouteValue(context, "dispatchRuntimeId");
                var runtimeDescriptor = context.RequestServices
                    .GetService<IEventDispatchRuntimeDescriptorCatalog>()?
                    .GetById(dispatchRuntimeId);

                return runtimeDescriptor is null ? Results.NotFound() : Results.Ok(runtimeDescriptor);
            });
        MapGetResultRequestDelegate(engineGroup, "/event-dispatches", "GetCephalonEventDispatches", static context =>
            {
                var states = context.RequestServices
                    .GetService<IEventDispatchRuntimeCatalog>()?
                    .States ?? [];

                return Results.Ok(states);
            });
        MapGetResultRequestDelegate(engineGroup, "/event-dispatches/terminal-failures", "GetCephalonTerminalEventDispatchFailures", static context =>
            {
                var states = context.RequestServices
                    .GetService<IEventDispatchRuntimeCatalog>()?
                    .States
                    .Where(static state => state.TerminalFailure)
                    .ToArray() ?? [];

                return Results.Ok(states);
            });
        MapGetResultRequestDelegate(engineGroup, "/event-dispatches/{outboxId}", "GetCephalonEventDispatch", static context =>
            {
                var outboxId = GetRouteValue(context, "outboxId");
                var state = context.RequestServices
                    .GetService<IEventDispatchRuntimeCatalog>()?
                    .GetByOutboxId(outboxId);

                return state is null ? Results.NotFound() : Results.Ok(state);
            });
        MapGetResultRequestDelegate(engineGroup, "/event-dispatch-remediation-commands", "GetCephalonEventDispatchRemediationCommands", static context =>
            {
                var states = ResolveEventDispatchRemediationCommandReadModel(context.RequestServices)?.States ?? [];

                return OkLimitedEventDispatchRemediationCommandStates(context, states);
            });
        MapGetResultRequestDelegate(engineGroup, "/event-dispatch-remediation-commands/summary", "GetCephalonEventDispatchRemediationCommandSummary", static context =>
            {
                var summary = ResolveEventDispatchRemediationCommandReadModel(context.RequestServices)?.Summary ??
                    EventDispatchRemediationRuntimeSummary.Empty;

                return Results.Ok(summary);
            });
        MapGetResultRequestDelegate(engineGroup, "/event-dispatch-remediation-commands/latest", "GetCephalonEventDispatchRemediationLatestCommand", static context =>
            {
                var state = ResolveEventDispatchRemediationCommandReadModel(context.RequestServices)?.Latest;

                return state is null ? Results.NotFound() : Results.Ok(state);
            });
        MapGetResultRequestDelegate(engineGroup, "/event-dispatch-remediation-commands/retention", "GetCephalonEventDispatchRemediationCommandRetention", static context =>
            {
                var retention = ResolveEventDispatchRemediationCommandReadModel(context.RequestServices)?.Retention ??
                    EventDispatchRemediationRuntimeRetention.Empty;

                return Results.Ok(retention);
            });
        MapGetResultRequestDelegate(engineGroup, "/event-dispatch-remediation-commands/in-doubt", "GetCephalonEventDispatchRemediationInDoubtCommands", static context =>
            {
                if (!TryGetNullableDateTimeOffsetQueryValue(context, "beforeUtc", out var beforeUtc, out var error))
                {
                    return error;
                }

                var states = ResolveEventDispatchRemediationCommandReadModel(context.RequestServices)?.GetInDoubtBefore(beforeUtc) ?? [];

                return OkLimitedEventDispatchRemediationCommandStates(context, states);
            });
        MapGetResultRequestDelegate(engineGroup, "/event-dispatch-remediation-commands/in-doubt/summary", "GetCephalonEventDispatchRemediationInDoubtCommandSummary", static context =>
            {
                if (!TryGetNullableDateTimeOffsetQueryValue(context, "beforeUtc", out var beforeUtc, out var error))
                {
                    return error;
                }

                var summary = ResolveEventDispatchRemediationCommandReadModel(context.RequestServices)?
                    .GetInDoubtSummaryBefore(beforeUtc) ?? EventDispatchRemediationRuntimeSummary.Empty;

                return Results.Ok(summary);
            });
        MapGetResultRequestDelegate(engineGroup, "/event-dispatch-remediation-commands/in-doubt/oldest", "GetCephalonEventDispatchRemediationOldestInDoubtCommand", static context =>
            {
                if (!TryGetNullableDateTimeOffsetQueryValue(context, "beforeUtc", out var beforeUtc, out var error))
                {
                    return error;
                }

                var state = ResolveEventDispatchRemediationCommandReadModel(context.RequestServices)?
                    .GetOldestInDoubtBefore(beforeUtc);

                return state is null ? Results.NotFound() : Results.Ok(state);
            });
        MapGetResultRequestDelegate(engineGroup, "/event-dispatch-remediation-commands/observations/summary", "GetCephalonEventDispatchRemediationCommandObservationSummary", static context =>
            {
                if (!TryGetNullableDateTimeOffsetQueryValue(context, "fromUtc", out var fromUtc, out var error))
                {
                    return error;
                }

                if (!TryGetNullableDateTimeOffsetQueryValue(context, "toUtc", out var toUtc, out error))
                {
                    return error;
                }

                if (fromUtc is { } from && toUtc is { } to && from > to)
                {
                    return Results.BadRequest("Query parameter 'fromUtc' must be less than or equal to 'toUtc'.");
                }

                var summary = ResolveEventDispatchRemediationCommandReadModel(context.RequestServices)?
                    .GetSummaryByObservedAt(fromUtc, toUtc) ?? EventDispatchRemediationRuntimeSummary.Empty;

                return Results.Ok(summary);
            });
        MapGetResultRequestDelegate(engineGroup, "/event-dispatch-remediation-commands/observations", "GetCephalonEventDispatchRemediationCommandsByObservationWindow", static context =>
            {
                if (!TryGetNullableDateTimeOffsetQueryValue(context, "fromUtc", out var fromUtc, out var error))
                {
                    return error;
                }

                if (!TryGetNullableDateTimeOffsetQueryValue(context, "toUtc", out var toUtc, out error))
                {
                    return error;
                }

                if (fromUtc is { } from && toUtc is { } to && from > to)
                {
                    return Results.BadRequest("Query parameter 'fromUtc' must be less than or equal to 'toUtc'.");
                }

                var states = ResolveEventDispatchRemediationCommandReadModel(context.RequestServices)?
                    .GetByObservedAt(fromUtc, toUtc) ?? [];

                return OkLimitedEventDispatchRemediationCommandStates(context, states);
            });
        MapGetResultRequestDelegate(engineGroup, "/event-dispatch-remediation-commands/outboxes/{outboxId}/summary", "GetCephalonEventDispatchRemediationCommandSummaryByOutbox", static context =>
            {
                var outboxId = GetRouteValue(context, "outboxId");
                var summary = ResolveEventDispatchRemediationCommandReadModel(context.RequestServices)?
                    .GetSummaryByOutboxId(outboxId) ?? EventDispatchRemediationRuntimeSummary.Empty;

                return Results.Ok(summary);
            });
        MapGetResultRequestDelegate(engineGroup, "/event-dispatch-remediation-commands/outboxes/{outboxId}", "GetCephalonEventDispatchRemediationCommandsByOutbox", static context =>
            {
                var outboxId = GetRouteValue(context, "outboxId");
                var states = ResolveEventDispatchRemediationCommandReadModel(context.RequestServices)?
                    .GetByOutboxId(outboxId) ?? [];

                return OkLimitedEventDispatchRemediationCommandStates(context, states);
            });
        MapGetResultRequestDelegate(engineGroup, "/event-dispatch-remediation-commands/messages/{messageId}/summary", "GetCephalonEventDispatchRemediationCommandSummaryByMessage", static context =>
            {
                var messageId = GetRouteValue(context, "messageId");
                var summary = ResolveEventDispatchRemediationCommandReadModel(context.RequestServices)?
                    .GetSummaryByMessageId(messageId) ?? EventDispatchRemediationRuntimeSummary.Empty;

                return Results.Ok(summary);
            });
        MapGetResultRequestDelegate(engineGroup, "/event-dispatch-remediation-commands/messages/{messageId}", "GetCephalonEventDispatchRemediationCommandsByMessage", static context =>
            {
                var messageId = GetRouteValue(context, "messageId");
                var states = ResolveEventDispatchRemediationCommandReadModel(context.RequestServices)?
                    .GetByMessageId(messageId) ?? [];

                return OkLimitedEventDispatchRemediationCommandStates(context, states);
            });
        MapGetResultRequestDelegate(engineGroup, "/event-dispatch-remediation-commands/channels/{channelId}/summary", "GetCephalonEventDispatchRemediationCommandSummaryByChannel", static context =>
            {
                var channelId = GetRouteValue(context, "channelId");
                var summary = ResolveEventDispatchRemediationCommandReadModel(context.RequestServices)?
                    .GetSummaryByChannelId(channelId) ?? EventDispatchRemediationRuntimeSummary.Empty;

                return Results.Ok(summary);
            });
        MapGetResultRequestDelegate(engineGroup, "/event-dispatch-remediation-commands/channels/{channelId}", "GetCephalonEventDispatchRemediationCommandsByChannel", static context =>
            {
                var channelId = GetRouteValue(context, "channelId");
                var states = ResolveEventDispatchRemediationCommandReadModel(context.RequestServices)?
                    .GetByChannelId(channelId) ?? [];

                return OkLimitedEventDispatchRemediationCommandStates(context, states);
            });
        MapGetResultRequestDelegate(engineGroup, "/event-dispatch-remediation-commands/operations/{operationId}/summary", "GetCephalonEventDispatchRemediationCommandSummaryByOperation", static context =>
            {
                var operationId = GetRouteValue(context, "operationId");
                var summary = ResolveEventDispatchRemediationCommandReadModel(context.RequestServices)?
                    .GetSummaryByOperationId(operationId) ?? EventDispatchRemediationRuntimeSummary.Empty;

                return Results.Ok(summary);
            });
        MapGetResultRequestDelegate(engineGroup, "/event-dispatch-remediation-commands/operations/{operationId}", "GetCephalonEventDispatchRemediationCommandsByOperation", static context =>
            {
                var operationId = GetRouteValue(context, "operationId");
                var states = ResolveEventDispatchRemediationCommandReadModel(context.RequestServices)?
                    .GetByOperationId(operationId) ?? [];

                return OkLimitedEventDispatchRemediationCommandStates(context, states);
            });
        MapGetResultRequestDelegate(engineGroup, "/event-dispatch-remediation-commands/actors/{actorId}/summary", "GetCephalonEventDispatchRemediationCommandSummaryByActor", static context =>
            {
                var actorId = GetRouteValue(context, "actorId");
                var summary = ResolveEventDispatchRemediationCommandReadModel(context.RequestServices)?
                    .GetSummaryByActorId(actorId) ?? EventDispatchRemediationRuntimeSummary.Empty;

                return Results.Ok(summary);
            });
        MapGetResultRequestDelegate(engineGroup, "/event-dispatch-remediation-commands/actors/{actorId}", "GetCephalonEventDispatchRemediationCommandsByActor", static context =>
            {
                var actorId = GetRouteValue(context, "actorId");
                var states = ResolveEventDispatchRemediationCommandReadModel(context.RequestServices)?
                    .GetByActorId(actorId) ?? [];

                return OkLimitedEventDispatchRemediationCommandStates(context, states);
            });
        MapGetResultRequestDelegate(engineGroup, "/event-dispatch-remediation-commands/correlations/{correlationId}/summary", "GetCephalonEventDispatchRemediationCommandSummaryByCorrelation", static context =>
            {
                var correlationId = GetRouteValue(context, "correlationId");
                var summary = ResolveEventDispatchRemediationCommandReadModel(context.RequestServices)?
                    .GetSummaryByCorrelationId(correlationId) ?? EventDispatchRemediationRuntimeSummary.Empty;

                return Results.Ok(summary);
            });
        MapGetResultRequestDelegate(engineGroup, "/event-dispatch-remediation-commands/correlations/{correlationId}", "GetCephalonEventDispatchRemediationCommandsByCorrelation", static context =>
            {
                var correlationId = GetRouteValue(context, "correlationId");
                var states = ResolveEventDispatchRemediationCommandReadModel(context.RequestServices)?
                    .GetByCorrelationId(correlationId) ?? [];

                return OkLimitedEventDispatchRemediationCommandStates(context, states);
            });
        MapGetResultRequestDelegate(engineGroup, "/event-dispatch-remediation-commands/reasons/{reason}/summary", "GetCephalonEventDispatchRemediationCommandSummaryByReason", static context =>
            {
                var reason = GetRouteValue(context, "reason");
                var summary = ResolveEventDispatchRemediationCommandReadModel(context.RequestServices)?
                    .GetSummaryByReason(reason) ?? EventDispatchRemediationRuntimeSummary.Empty;

                return Results.Ok(summary);
            });
        MapGetResultRequestDelegate(engineGroup, "/event-dispatch-remediation-commands/reasons/{reason}", "GetCephalonEventDispatchRemediationCommandsByReason", static context =>
            {
                var reason = GetRouteValue(context, "reason");
                var states = ResolveEventDispatchRemediationCommandReadModel(context.RequestServices)?
                    .GetByReason(reason) ?? [];

                return OkLimitedEventDispatchRemediationCommandStates(context, states);
            });
        MapGetResultRequestDelegate(engineGroup, "/event-dispatch-remediation-commands/outcomes/{outcome}/summary", "GetCephalonEventDispatchRemediationCommandSummaryByOutcome", static context =>
            {
                var outcome = GetRouteValue(context, "outcome");
                var summary = ResolveEventDispatchRemediationCommandReadModel(context.RequestServices)?
                    .GetSummaryByOutcome(outcome) ?? EventDispatchRemediationRuntimeSummary.Empty;

                return Results.Ok(summary);
            });
        MapGetResultRequestDelegate(engineGroup, "/event-dispatch-remediation-commands/outcomes/{outcome}", "GetCephalonEventDispatchRemediationCommandsByOutcome", static context =>
            {
                var outcome = GetRouteValue(context, "outcome");
                var states = ResolveEventDispatchRemediationCommandReadModel(context.RequestServices)?
                    .GetByOutcome(outcome) ?? [];

                return OkLimitedEventDispatchRemediationCommandStates(context, states);
            });
        MapGetResultRequestDelegate(engineGroup, "/event-dispatch-remediation-commands/dispatch-outcomes/{dispatchOutcome}/summary", "GetCephalonEventDispatchRemediationCommandSummaryByDispatchOutcome", static context =>
            {
                var dispatchOutcome = GetRouteValue(context, "dispatchOutcome");
                var summary = ResolveEventDispatchRemediationCommandReadModel(context.RequestServices)?
                    .GetSummaryByDispatchOutcome(dispatchOutcome) ?? EventDispatchRemediationRuntimeSummary.Empty;

                return Results.Ok(summary);
            });
        MapGetResultRequestDelegate(engineGroup, "/event-dispatch-remediation-commands/dispatch-outcomes/{dispatchOutcome}", "GetCephalonEventDispatchRemediationCommandsByDispatchOutcome", static context =>
            {
                var dispatchOutcome = GetRouteValue(context, "dispatchOutcome");
                var states = ResolveEventDispatchRemediationCommandReadModel(context.RequestServices)?
                    .GetByDispatchOutcome(dispatchOutcome) ?? [];

                return OkLimitedEventDispatchRemediationCommandStates(context, states);
            });
        MapGetResultRequestDelegate(engineGroup, "/event-dispatch-remediation-commands/{commandId}", "GetCephalonEventDispatchRemediationCommand", static context =>
            {
                var commandId = GetRouteValue(context, "commandId");
                var state = ResolveEventDispatchRemediationCommandReadModel(context.RequestServices)?
                    .GetByCommandId(commandId);

                return state is null ? Results.NotFound() : Results.Ok(state);
            });
        MapPostAsyncResultRequestDelegate(
            engineGroup,
            "/event-dispatches/{outboxId}/commands/{operationId}",
            "RunCephalonEventDispatchRemediationCommand",
            static async context =>
                {
                    var (request, bodyError) = await ReadOptionalJsonBodyAsync(
                            context,
                            AspNetCoreJsonSerializerContext.Default.EventDispatchRemediationHttpRequest)
                        .ConfigureAwait(false);
                    if (bodyError is not null)
                    {
                        return bodyError;
                    }

                    var dispatcher = context.RequestServices.GetService<IEventDispatchRemediationDispatcher>();
                    if (dispatcher is null)
                    {
                        return Results.NotFound(new
                        {
                            error = "Event dispatch remediation is not available in the active runtime."
                        });
                    }

                    try
                    {
                        var remediationRequest = CreateEventDispatchRemediationRequest(
                            GetRouteValue(context, "outboxId"),
                            GetRouteValue(context, "operationId"),
                            request,
                            context);
                        var result = await dispatcher.DispatchAsync(remediationRequest, context.RequestAborted).ConfigureAwait(false);

                        return string.Equals(result.Outcome, EventDispatchRemediationOutcomes.Rejected, StringComparison.OrdinalIgnoreCase)
                            ? Results.Conflict(result)
                            : Results.Ok(result);
                    }
                    catch (ArgumentException exception)
                    {
                        return Results.BadRequest(new { error = exception.Message });
                    }
                    catch (InvalidOperationException exception)
                    {
                        return Results.Problem(
                            title: "Event dispatch remediation command failed.",
                            detail: exception.Message,
                            statusCode: StatusCodes.Status500InternalServerError);
                    }
                });
        MapGetResultRequestDelegate(engineGroup, "/event-publications/runtime", "GetCephalonEventPublicationRuntimeStates", static context =>
            {
                var states = context.RequestServices
                    .GetService<IEventPublicationRuntimeCatalog>()?
                    .States ?? [];

                return Results.Ok(states);
            });
        MapGetResultRequestDelegate(engineGroup, "/event-publications/runtime/channels/{channelId}", "GetCephalonEventPublicationRuntimeStatesByChannel", static context =>
            {
                var channelId = GetRouteValue(context, "channelId");
                var states = context.RequestServices
                    .GetService<IEventPublicationRuntimeCatalog>()?
                    .GetByChannelId(channelId) ?? [];

                return Results.Ok(states);
            });
        MapGetResultRequestDelegate(engineGroup, "/event-publications/runtime/{publicationId}", "GetCephalonEventPublicationRuntimeState", static context =>
            {
                var publicationId = GetRouteValue(context, "publicationId");
                var state = context.RequestServices
                    .GetService<IEventPublicationRuntimeCatalog>()?
                    .GetByPublicationId(publicationId);

                return state is null ? Results.NotFound() : Results.Ok(state);
            });
        MapPostAsyncResultRequestDelegate(
            engineGroup,
            "/event-publications",
            "PublishCephalonEventPublication",
            static async context =>
                {
                    var (request, bodyError) = await ReadOptionalJsonBodyAsync(
                            context,
                            AspNetCoreJsonSerializerContext.Default.EventPublicationHttpRequest)
                        .ConfigureAwait(false);
                    if (bodyError is not null)
                    {
                        return bodyError;
                    }

                    var dispatcher = context.RequestServices.GetService<IEventPublicationDispatcher>();
                    if (dispatcher is null)
                    {
                        return Results.NotFound(new
                        {
                            error = "Event publication is not available in the active runtime."
                        });
                    }

                    try
                    {
                        var publicationRequest = CreateEventPublicationRequest(request, context);
                        var result = await dispatcher.PublishAsync(publicationRequest, context.RequestAborted).ConfigureAwait(false);
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
                });
        MapGetResultRequestDelegate(engineGroup, "/agent-tool-runs", "GetCephalonAgentToolRuns", static context =>
            {
                var runs = context.RequestServices
                    .GetService<IAgentToolRunCatalog>()?
                    .Runs ?? [];

                return Results.Ok(runs);
            });
        MapGetResultRequestDelegate(engineGroup, "/agent-tool-runs/retry-pending", "GetCephalonRetryPendingAgentToolRuns", static context =>
            {
                var runs = context.RequestServices
                    .GetService<IAgentToolRunCatalog>()?
                    .Runs
                    .Where(static run => run.RetryPending)
                    .ToArray() ?? [];

                return Results.Ok(runs);
            });
        MapGetResultRequestDelegate(engineGroup, "/agent-tool-runs/idempotency-duplicates", "GetCephalonDuplicateCompletedAgentToolRuns", static context =>
            {
                var runs = context.RequestServices
                    .GetService<IAgentToolRunCatalog>()?
                    .Runs
                    .Where(static run => run.DuplicateCompleted)
                    .ToArray() ?? [];

                return Results.Ok(runs);
            });
        MapGetResultRequestDelegate(engineGroup, "/agent-tool-runs/approval-required", "GetCephalonApprovalRequiredAgentToolRuns", static context =>
            {
                var runs = context.RequestServices
                    .GetService<IAgentToolRunCatalog>()?
                    .Runs
                    .Where(static run => run.RequiresApproval)
                    .ToArray() ?? [];

                return Results.Ok(runs);
            });
        MapGetResultRequestDelegate(engineGroup, "/agent-tool-runs/terminal-failures", "GetCephalonTerminalFailureAgentToolRuns", static context =>
            {
                var runs = context.RequestServices
                    .GetService<IAgentToolRunCatalog>()?
                    .Runs
                    .Where(static run => run.TerminalFailure)
                    .ToArray() ?? [];

                return Results.Ok(runs);
            });
        MapGetResultRequestDelegate(engineGroup, "/agent-tool-runs/{runId}", "GetCephalonAgentToolRun", static context =>
            {
                var runId = GetRouteValue(context, "runId");
                var run = context.RequestServices
                    .GetService<IAgentToolRunCatalog>()?
                    .GetByRunId(runId);

                return run is null ? Results.NotFound() : Results.Ok(run);
            });
        MapGetResultRequestDelegate(engineGroup, "/agent-tool-runs/by-tool/{toolId}", "GetCephalonAgentToolRunsByTool", static context =>
            {
                var toolId = GetRouteValue(context, "toolId");
                var runs = context.RequestServices
                    .GetService<IAgentToolRunCatalog>()?
                    .GetByToolId(toolId) ?? [];

                return Results.Ok(runs);
            });
        MapPostAsyncResultRequestDelegate(
            engineGroup,
            "/agent-tools/{toolId}/runs",
            "RunCephalonAgentTool",
            static async context =>
                {
                    var toolId = GetRouteValue(context, "toolId");
                    var (request, bodyError) = await ReadOptionalJsonBodyAsync(
                            context,
                            AspNetCoreJsonSerializerContext.Default.AgentToolExecutionHttpRequest)
                        .ConfigureAwait(false);
                    if (bodyError is not null)
                    {
                        return bodyError;
                    }

                    var dispatcher = context.RequestServices.GetService<IAgentToolDispatcher>();
                    if (dispatcher is null)
                    {
                        return Results.NotFound(new
                        {
                            error = "Agent-tool execution is not available in the active runtime."
                        });
                    }

                    try
                    {
                        var executionRequest = CreateAgentToolExecutionRequest(toolId, request, context);
                        var result = await dispatcher.ExecuteAsync(executionRequest, context.RequestAborted).ConfigureAwait(false);
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
                });
        MapGetResultRequestDelegate(engineGroup, "/event-subscription-readiness", "GetCephalonEventSubscriptionExecutionReadiness", static context =>
            {
                var readiness = context.RequestServices
                    .GetService<IEventSubscriptionExecutionReadinessCatalog>()?
                    .Readiness ?? [];

                return Results.Ok(readiness);
            });
        MapGetResultRequestDelegate(engineGroup, "/event-subscription-readiness/{subscriptionId}", "GetCephalonEventSubscriptionExecutionReadinessBySubscription", static context =>
            {
                var subscriptionId = GetRouteValue(context, "subscriptionId");
                var readiness = context.RequestServices
                    .GetService<IEventSubscriptionExecutionReadinessCatalog>()?
                    .GetBySubscriptionId(subscriptionId);

                return readiness is null ? Results.NotFound() : Results.Ok(readiness);
            });
        MapGetResultRequestDelegate(engineGroup, "/inboxes", "GetCephalonInboxes", static context =>
            TypedResults.Ok(GetRequiredService<IInboxCatalog>(context).Inboxes));
        MapGetResultRequestDelegate(engineGroup, "/inboxes/{inboxId}", "GetCephalonInbox", static context =>
            {
                var inboxId = GetRouteValue(context, "inboxId");
                var catalog = GetRequiredService<IInboxCatalog>(context);
                var inbox = catalog.GetById(inboxId);

                return inbox is null ? Results.NotFound() : Results.Ok(inbox);
            });
        MapGetResultRequestDelegate(engineGroup, "/audit-stores", "GetCephalonAuditStores", static context =>
            TypedResults.Ok(GetRequiredService<IAuditStoreCatalog>(context).AuditStores));
        MapGetResultRequestDelegate(engineGroup, "/audit-stores/{auditStoreId}", "GetCephalonAuditStore", static context =>
            {
                var auditStoreId = GetRouteValue(context, "auditStoreId");
                var catalog = GetRequiredService<IAuditStoreCatalog>(context);
                var auditStore = catalog.GetById(auditStoreId);

                return auditStore is null ? Results.NotFound() : Results.Ok(auditStore);
            });
        MapGetResultRequestDelegate(engineGroup, "/features", "GetCephalonFeatures", static context =>
            TypedResults.Ok(GetRequiredService<IFeatureFlagRuntimeCatalog>(context).FeatureFlags));
        MapGetResultRequestDelegate(engineGroup, "/features/enabled", "GetCephalonEnabledFeatures", static context =>
            TypedResults.Ok(GetRequiredService<IFeatureFlagRuntimeCatalog>(context).GetEnabled()));
        MapGetResultRequestDelegate(engineGroup, "/features/disabled", "GetCephalonDisabledFeatures", static context =>
            TypedResults.Ok(GetRequiredService<IFeatureFlagRuntimeCatalog>(context).GetDisabled()));
        MapGetResultRequestDelegate(engineGroup, "/features/modules/{moduleId}", "GetCephalonFeaturesByModule", static context =>
            TypedResults.Ok(GetRequiredService<IFeatureFlagRuntimeCatalog>(context).GetBySourceModule(GetRouteValue(context, "moduleId"))));
        MapGetResultRequestDelegate(engineGroup, "/features/{featureFlagId}/evaluate", "EvaluateCephalonFeature", static context =>
            {
                var featureFlagId = GetRouteValue(context, "featureFlagId");
                var featureToggle = GetRequiredService<IFeatureToggle>(context);
                var evaluationContext = new FeatureFlagEvaluationContext(
                    environmentName: GetQueryValue(context, "environmentName"),
                    moduleId: GetQueryValue(context, "moduleId"),
                    behaviorId: GetQueryValue(context, "behaviorId"),
                    capabilityKey: GetQueryValue(context, "capabilityKey"),
                    transportId: GetQueryValue(context, "transportId"),
                    tenantId: GetQueryValue(context, "tenantId"),
                    subjectId: GetQueryValue(context, "subjectId"),
                    tags: context.Request.Query["tag"]
                        .Select(static value => value?.ToString())
                        .Where(static value => !string.IsNullOrWhiteSpace(value))
                        .Select(static value => value!)
                        .ToArray());

                return TypedResults.Ok(featureToggle.Evaluate(featureFlagId, evaluationContext));
            });
        MapGetResultRequestDelegate(engineGroup, "/features/{featureFlagId}", "GetCephalonFeature", static context =>
            {
                var featureFlagId = GetRouteValue(context, "featureFlagId");
                var catalog = GetRequiredService<IFeatureFlagRuntimeCatalog>(context);
                var featureFlag = catalog.GetById(featureFlagId);

                return featureFlag is null ? Results.NotFound() : Results.Ok(featureFlag);
            });
        MapGetAsyncResultRequestDelegate(engineGroup, "/audit-history", "GetCephalonAuditHistory", static async context =>
            {
                var reader = context.RequestServices.GetService<IAuditHistoryReader>();
                if (reader is null)
                {
                    return Results.NotFound();
                }

                var outcome = GetQueryValue(context, "outcome");
                if (!TryParseAuditOutcome(outcome, out var parsedOutcome))
                {
                    return Results.BadRequest($"Audit outcome '{outcome}' is not supported.");
                }

                if (!TryGetNullableDateTimeOffsetQueryValue(context, "occurredFromUtc", out var occurredFromUtc, out var occurredFromUtcError))
                {
                    return occurredFromUtcError!;
                }

                if (!TryGetNullableDateTimeOffsetQueryValue(context, "occurredToUtc", out var occurredToUtc, out var occurredToUtcError))
                {
                    return occurredToUtcError!;
                }

                if (!TryGetNullableIntQueryValue(context, "offset", out var offset, out var offsetError))
                {
                    return offsetError!;
                }

                if (!TryGetNullableIntQueryValue(context, "limit", out var limit, out var limitError))
                {
                    return limitError!;
                }

                var query = new AuditHistoryQuery(
                    category: GetQueryValue(context, "category"),
                    action: GetQueryValue(context, "action"),
                    subjectType: GetQueryValue(context, "subjectType"),
                    subjectId: GetQueryValue(context, "subjectId"),
                    actorId: GetQueryValue(context, "actorId"),
                    tenantId: GetQueryValue(context, "tenantId"),
                    correlationId: GetQueryValue(context, "correlationId"),
                    outcome: parsedOutcome,
                    occurredFromUtc: occurredFromUtc,
                    occurredToUtc: occurredToUtc,
                    offset: offset ?? 0,
                    limit: limit ?? AuditHistoryQuery.DefaultLimit);

                return Results.Ok(await reader.QueryAsync(query, context.RequestAborted).ConfigureAwait(false));
            });
        MapGetAsyncResultRequestDelegate(engineGroup, "/audit-history/{auditEntryId}", "GetCephalonAuditHistoryEntry", static async context =>
            {
                var auditEntryId = GetRouteValue(context, "auditEntryId");
                var reader = context.RequestServices.GetService<IAuditHistoryReader>();
                if (reader is null)
                {
                    return Results.NotFound();
                }

                var entry = await reader.GetByIdAsync(auditEntryId, context.RequestAborted).ConfigureAwait(false);
                return entry is null ? Results.NotFound() : Results.Ok(entry);
            });
        if (runtime.Manifest.AppProfile.Audit.History.Export.Enabled == true)
        {
            MapGetAsyncResultRequestDelegate(engineGroup, "/audit-history/export", "ExportCephalonAuditHistory", async context =>
                {
                    var exporter = context.RequestServices.GetService<IAuditHistoryExporter>();
                    if (exporter is null)
                    {
                        return Results.NotFound();
                    }

                    var outcome = GetQueryValue(context, "outcome");
                    if (!TryParseAuditOutcome(outcome, out var parsedOutcome))
                    {
                        return Results.BadRequest($"Audit outcome '{outcome}' is not supported.");
                    }

                    if (!TryGetNullableDateTimeOffsetQueryValue(context, "occurredFromUtc", out var occurredFromUtc, out var occurredFromUtcError))
                    {
                        return occurredFromUtcError!;
                    }

                    if (!TryGetNullableDateTimeOffsetQueryValue(context, "occurredToUtc", out var occurredToUtc, out var occurredToUtcError))
                    {
                        return occurredToUtcError!;
                    }

                    if (!TryGetNullableIntQueryValue(context, "maxEntries", out var maxEntries, out var maxEntriesError))
                    {
                        return maxEntriesError!;
                    }

                    var exportLimit = ResolveAuditHistoryExportMaxEntries(runtime.Manifest.AppProfile, maxEntries);
                    var request = new AuditHistoryExportRequest(
                        category: GetQueryValue(context, "category"),
                        action: GetQueryValue(context, "action"),
                        subjectType: GetQueryValue(context, "subjectType"),
                        subjectId: GetQueryValue(context, "subjectId"),
                        actorId: GetQueryValue(context, "actorId"),
                        tenantId: GetQueryValue(context, "tenantId"),
                        correlationId: GetQueryValue(context, "correlationId"),
                        outcome: parsedOutcome,
                        occurredFromUtc: occurredFromUtc,
                        occurredToUtc: occurredToUtc,
                        maxEntries: exportLimit);

                    await context.Response.WriteAuditHistoryNdjsonAsync(
                        exporter,
                        request,
                        fileName: "cephalon-audit-history.ndjson",
                        cancellationToken: context.RequestAborted).ConfigureAwait(false);

                    return Results.Empty;
                });
        }
        MapGetResultRequestDelegate(engineGroup, "/authorization-policies", "GetCephalonAuthorizationPolicies", static context =>
            TypedResults.Ok(GetRequiredService<IAuthorizationPolicyCatalog>(context).Policies));
        MapGetResultRequestDelegate(engineGroup, "/authorization-policies/{policyId}", "GetCephalonAuthorizationPolicy", static context =>
            {
                var policyId = GetRouteValue(context, "policyId");
                var catalog = GetRequiredService<IAuthorizationPolicyCatalog>(context);
                var policy = catalog.GetById(policyId);

                return policy is null ? Results.NotFound() : Results.Ok(policy);
            });
        MapGetResultRequestDelegate(engineGroup, "/strangler-fig", "GetCephalonStranglerFigRoutes", static context =>
            TypedResults.Ok(GetRequiredService<IStranglerFigRuntimeCatalog>(context).Routes));
        MapGetResultRequestDelegate(engineGroup, "/strangler-fig/runtime", "GetCephalonStranglerFigRuntimeRoutes", static context =>
            TypedResults.Ok(GetRequiredService<IStranglerFigMigrationRuntimeCatalog>(context).Routes));
        MapGetResultRequestDelegate(engineGroup, "/strangler-fig/ingress", "GetCephalonStranglerFigIngressRoutes", static context =>
            TypedResults.Ok(GetRequiredService<IStranglerFigIngressRuntimeCatalog>(context).Routes));
        MapGetAsyncResultRequestDelegate(engineGroup, "/strangler-fig/resolve", "ResolveCephalonStranglerFigRoute", static async context =>
            {
                if (!TryGetRequiredQueryValue(context, "path", out var path, out var missingPath))
                {
                    return missingPath!;
                }

                var router = GetRequiredService<IStranglerFigRouter>(context);
                var resolution = await router.ResolveAsync(
                    new StranglerFigRequest(path, GetQueryValue(context, "method") ?? HttpMethods.Get),
                    context.RequestAborted).ConfigureAwait(false);

                return resolution is null
                    ? Results.NotFound()
                    : Results.Ok(resolution);
            });
        MapGetResultRequestDelegate(engineGroup, "/strangler-fig/{routeId}", "GetCephalonStranglerFigRoute", static context =>
            {
                var routeId = GetRouteValue(context, "routeId");
                var catalog = GetRequiredService<IStranglerFigRuntimeCatalog>(context);
                var route = catalog.GetById(routeId);

                return route is null ? Results.NotFound() : Results.Ok(route);
            });
        MapGetResultRequestDelegate(engineGroup, "/strangler-fig/runtime/{routeId}", "GetCephalonStranglerFigRuntimeRoute", static context =>
            {
                var routeId = GetRouteValue(context, "routeId");
                var catalog = GetRequiredService<IStranglerFigMigrationRuntimeCatalog>(context);
                var route = catalog.GetById(routeId);

                return route is null ? Results.NotFound() : Results.Ok(route);
            });
        MapGetResultRequestDelegate(engineGroup, "/strangler-fig/ingress/modules/{moduleId}", "GetCephalonStranglerFigIngressRoutesByModule", static context =>
            {
                var moduleId = GetRouteValue(context, "moduleId");
                var catalog = GetRequiredService<IStranglerFigIngressRuntimeCatalog>(context);
                return Results.Ok(catalog.GetBySourceModule(moduleId));
            });
        MapGetResultRequestDelegate(engineGroup, "/strangler-fig/ingress/{routeId}", "GetCephalonStranglerFigIngressRoute", static context =>
            {
                var routeId = GetRouteValue(context, "routeId");
                var catalog = GetRequiredService<IStranglerFigIngressRuntimeCatalog>(context);
                var route = catalog.GetById(routeId);

                return route is null ? Results.NotFound() : Results.Ok(route);
            });
        MapGetResultRequestDelegate(engineGroup, "/strangler-fig/cutover", "GetCephalonStranglerFigCutoverRoutes", static context =>
            TypedResults.Ok(GetRequiredService<AspNetCoreStranglerFigCutoverCatalog>(context).Routes));
        MapGetAsyncResultRequestDelegate(engineGroup, "/strangler-fig/cutover/resolve", "ResolveCephalonStranglerFigCutover", static async context =>
            {
                if (!TryGetRequiredQueryValue(context, "path", out var path, out var missingPath))
                {
                    return missingPath!;
                }

                var router = GetRequiredService<IStranglerFigRouter>(context);
                var catalog = GetRequiredService<AspNetCoreStranglerFigCutoverCatalog>(context);
                var resolution = await router.ResolveAsync(
                        new StranglerFigRequest(path, GetQueryValue(context, "method") ?? HttpMethods.Get),
                        context.RequestAborted)
                    .ConfigureAwait(false);

                if (resolution is null)
                {
                    return Results.NotFound();
                }

                return Results.Ok(catalog.CreateDecision(
                    resolution,
                    NormalizeOptionalQueryString(GetQueryValue(context, "query"))));
            });
        MapGetResultRequestDelegate(engineGroup, "/strangler-fig/cutover/{routeId}", "GetCephalonStranglerFigCutoverRoute", static context =>
            {
                var routeId = GetRouteValue(context, "routeId");
                var catalog = GetRequiredService<AspNetCoreStranglerFigCutoverCatalog>(context);
                var route = catalog.GetById(routeId);

                return route is null ? Results.NotFound() : Results.Ok(route);
            });
        MapGetResultRequestDelegate(engineGroup, "/backend-for-frontend", "GetCephalonBackendForFrontendBindings", static context =>
            TypedResults.Ok(GetRequiredService<IBackendForFrontendRuntimeCatalog>(context).Bindings));
        MapGetResultRequestDelegate(engineGroup, "/backend-for-frontend/clients/{clientId}", "GetCephalonBackendForFrontendBindingsByClient", static context =>
            TypedResults.Ok(GetRequiredService<IBackendForFrontendRuntimeCatalog>(context).GetByClientId(GetRouteValue(context, "clientId"))));
        MapGetResultRequestDelegate(engineGroup, "/backend-for-frontend/modules/{moduleId}", "GetCephalonBackendForFrontendBindingsByModule", static context =>
            TypedResults.Ok(GetRequiredService<IBackendForFrontendRuntimeCatalog>(context).GetBySourceModule(GetRouteValue(context, "moduleId"))));
        MapGetResultRequestDelegate(engineGroup, "/backend-for-frontend/transports/{transportId}", "GetCephalonBackendForFrontendBindingsByTransport", static context =>
            TypedResults.Ok(GetRequiredService<IBackendForFrontendRuntimeCatalog>(context).GetByTransportId(GetRouteValue(context, "transportId"))));
        MapGetResultRequestDelegate(engineGroup, "/backend-for-frontend/rest-endpoints", "GetCephalonBackendForFrontendRestEndpoints", static context =>
            TypedResults.Ok(GetRequiredService<IBackendForFrontendRestRuntimeCatalog>(context).Endpoints));
        MapGetResultRequestDelegate(engineGroup, "/backend-for-frontend/rest-endpoints/bindings/{bindingId}", "GetCephalonBackendForFrontendRestEndpointsByBinding", static context =>
            TypedResults.Ok(GetRequiredService<IBackendForFrontendRestRuntimeCatalog>(context).GetByBindingId(GetRouteValue(context, "bindingId"))));
        MapGetResultRequestDelegate(engineGroup, "/backend-for-frontend/rest-endpoints/clients/{clientId}", "GetCephalonBackendForFrontendRestEndpointsByClient", static context =>
            TypedResults.Ok(GetRequiredService<IBackendForFrontendRestRuntimeCatalog>(context).GetByClientId(GetRouteValue(context, "clientId"))));
        MapGetResultRequestDelegate(engineGroup, "/backend-for-frontend/rest-endpoints/modules/{moduleId}", "GetCephalonBackendForFrontendRestEndpointsByModule", static context =>
            TypedResults.Ok(GetRequiredService<IBackendForFrontendRestRuntimeCatalog>(context).GetBySourceModule(GetRouteValue(context, "moduleId"))));
        MapGetResultRequestDelegate(engineGroup, "/backend-for-frontend/rest-endpoints/published/{restEndpointId}", "GetCephalonBackendForFrontendRestEndpointsByPublishedEndpoint", static context =>
            TypedResults.Ok(GetRequiredService<IBackendForFrontendRestRuntimeCatalog>(context).GetByRestEndpointId(GetRouteValue(context, "restEndpointId"))));
        MapGetResultRequestDelegate(engineGroup, "/backend-for-frontend/rest-endpoints/{runtimeEndpointId}", "GetCephalonBackendForFrontendRestEndpoint", static context =>
            {
                var runtimeEndpointId = GetRouteValue(context, "runtimeEndpointId");
                var catalog = GetRequiredService<IBackendForFrontendRestRuntimeCatalog>(context);
                var runtimeEndpoint = catalog.GetById(runtimeEndpointId);

                return runtimeEndpoint is null ? Results.NotFound() : Results.Ok(runtimeEndpoint);
            });
        MapGetResultRequestDelegate(engineGroup, "/backend-for-frontend/rest-documents", "GetCephalonBackendForFrontendRestDocuments", static context =>
            TypedResults.Ok(GetRequiredService<IBackendForFrontendRestDocumentRuntimeCatalog>(context).Documents));
        MapGetResultRequestDelegate(engineGroup, "/backend-for-frontend/rest-documents/bindings/{bindingId}", "GetCephalonBackendForFrontendRestDocumentsByBinding", static context =>
            TypedResults.Ok(GetRequiredService<IBackendForFrontendRestDocumentRuntimeCatalog>(context).GetByBindingId(GetRouteValue(context, "bindingId"))));
        MapGetResultRequestDelegate(engineGroup, "/backend-for-frontend/rest-documents/clients/{clientId}", "GetCephalonBackendForFrontendRestDocumentsByClient", static context =>
            TypedResults.Ok(GetRequiredService<IBackendForFrontendRestDocumentRuntimeCatalog>(context).GetByClientId(GetRouteValue(context, "clientId"))));
        MapGetResultRequestDelegate(engineGroup, "/backend-for-frontend/rest-documents/{documentId}", "GetCephalonBackendForFrontendRestDocument", static context =>
            {
                var documentId = GetRouteValue(context, "documentId");
                var catalog = GetRequiredService<IBackendForFrontendRestDocumentRuntimeCatalog>(context);
                var document = catalog.GetById(documentId);

                return document is null ? Results.NotFound() : Results.Ok(document);
            });
        MapGetResultRequestDelegate(engineGroup, "/backend-for-frontend/{bindingId}", "GetCephalonBackendForFrontendBinding", static context =>
            {
                var bindingId = GetRouteValue(context, "bindingId");
                var catalog = GetRequiredService<IBackendForFrontendRuntimeCatalog>(context);
                var binding = catalog.GetById(bindingId);

                return binding is null ? Results.NotFound() : Results.Ok(binding);
            });
        MapGetResultRequestDelegate(engineGroup, "/patterns", "GetCephalonPatterns", static context =>
            TypedResults.Ok(GetRequiredService<RuntimeManifest>(context).AppProfile.Patterns));
        MapGetResultRequestDelegate(engineGroup, "/cells", "GetCephalonCellBoundaries", static context =>
            TypedResults.Ok(GetRequiredService<ICellBoundaryCatalog>(context).CellBoundaries));
        MapGetResultRequestDelegate(engineGroup, "/cells/modules/{moduleId}", "GetCephalonCellBoundariesByModule", static context =>
            TypedResults.Ok(GetRequiredService<ICellBoundaryCatalog>(context).GetByModule(GetRouteValue(context, "moduleId"))));
        MapGetResultRequestDelegate(engineGroup, "/cells/{cellId}", "GetCephalonCellBoundary", static context =>
            {
                var cellId = GetRouteValue(context, "cellId");
                var catalog = GetRequiredService<ICellBoundaryCatalog>(context);
                var cellBoundary = catalog.GetById(cellId);

                return cellBoundary is null ? Results.NotFound() : Results.Ok(cellBoundary);
            });
        MapGetResultRequestDelegate(engineGroup, "/cell-routes", "GetCephalonCellRoutes", static context =>
            TypedResults.Ok(GetRequiredService<ICellRouteCatalog>(context).Routes));
        MapGetResultRequestDelegate(engineGroup, "/cell-routes/modules/{moduleId}", "GetCephalonCellRoutesByModule", static context =>
            TypedResults.Ok(GetRequiredService<ICellRouteCatalog>(context).GetBySourceModule(GetRouteValue(context, "moduleId"))));
        MapGetResultRequestDelegate(engineGroup, "/cell-routes/source-cells/{cellId}", "GetCephalonCellRoutesBySourceCell", static context =>
            TypedResults.Ok(GetRequiredService<ICellRouteCatalog>(context).GetBySourceCellId(GetRouteValue(context, "cellId"))));
        MapGetResultRequestDelegate(engineGroup, "/cell-routes/target-cells/{cellId}", "GetCephalonCellRoutesByTargetCell", static context =>
            TypedResults.Ok(GetRequiredService<ICellRouteCatalog>(context).GetByTargetCellId(GetRouteValue(context, "cellId"))));
        MapGetResultRequestDelegate(engineGroup, "/cell-routes/{routeId}", "GetCephalonCellRoute", static context =>
            {
                var routeId = GetRouteValue(context, "routeId");
                var catalog = GetRequiredService<ICellRouteCatalog>(context);
                var cellRoute = catalog.GetById(routeId);

                return cellRoute is null ? Results.NotFound() : Results.Ok(cellRoute);
            });
        MapGetResultRequestDelegate(engineGroup, "/cell-health-isolations", "GetCephalonCellHealthIsolations", static context =>
            TypedResults.Ok(GetRequiredService<ICellHealthIsolationCatalog>(context).HealthIsolations));
        MapGetResultRequestDelegate(engineGroup, "/cell-health-isolations/modules/{moduleId}", "GetCephalonCellHealthIsolationsByModule", static context =>
            TypedResults.Ok(GetRequiredService<ICellHealthIsolationCatalog>(context).GetBySourceModule(GetRouteValue(context, "moduleId"))));
        MapGetResultRequestDelegate(engineGroup, "/cell-health-isolations/cells/{cellId}", "GetCephalonCellHealthIsolationsByCell", static context =>
            TypedResults.Ok(GetRequiredService<ICellHealthIsolationCatalog>(context).GetByCellId(GetRouteValue(context, "cellId"))));
        MapGetResultRequestDelegate(engineGroup, "/cell-health-isolations/dependencies/{dependencyId}", "GetCephalonCellHealthIsolationsByDependency", static context =>
            TypedResults.Ok(GetRequiredService<ICellHealthIsolationCatalog>(context).GetByDependencyId(GetRouteValue(context, "dependencyId"))));
        MapGetResultRequestDelegate(engineGroup, "/cell-health-isolations/{healthIsolationId}", "GetCephalonCellHealthIsolation", static context =>
            {
                var healthIsolationId = GetRouteValue(context, "healthIsolationId");
                var catalog = GetRequiredService<ICellHealthIsolationCatalog>(context);
                var healthIsolation = catalog.GetById(healthIsolationId);

                return healthIsolation is null ? Results.NotFound() : Results.Ok(healthIsolation);
            });
        MapGetResultRequestDelegate(engineGroup, "/cell-traffic-automations", "GetCephalonCellTrafficAutomations", static context =>
            TypedResults.Ok(GetRequiredService<ICellTrafficAutomationRuntimeCatalog>(context).Automations));
        MapGetResultRequestDelegate(engineGroup, "/cell-traffic-automations/modules/{moduleId}", "GetCephalonCellTrafficAutomationsByModule", static context =>
            TypedResults.Ok(GetRequiredService<ICellTrafficAutomationRuntimeCatalog>(context).GetBySourceModule(GetRouteValue(context, "moduleId"))));
        MapGetResultRequestDelegate(engineGroup, "/cell-traffic-automations/routes/{routeId}", "GetCephalonCellTrafficAutomationByRoute", static context =>
            {
                var routeId = GetRouteValue(context, "routeId");
                var catalog = GetRequiredService<ICellTrafficAutomationRuntimeCatalog>(context);
                var automation = catalog.GetByRouteId(routeId);

                return automation is null ? Results.NotFound() : Results.Ok(automation);
            });
        MapGetResultRequestDelegate(engineGroup, "/cell-traffic-automations/source-cells/{cellId}", "GetCephalonCellTrafficAutomationsBySourceCell", static context =>
            TypedResults.Ok(GetRequiredService<ICellTrafficAutomationRuntimeCatalog>(context).GetBySourceCellId(GetRouteValue(context, "cellId"))));
        MapGetResultRequestDelegate(engineGroup, "/cell-traffic-automations/target-cells/{cellId}", "GetCephalonCellTrafficAutomationsByTargetCell", static context =>
            TypedResults.Ok(GetRequiredService<ICellTrafficAutomationRuntimeCatalog>(context).GetByTargetCellId(GetRouteValue(context, "cellId"))));
        MapGetResultRequestDelegate(engineGroup, "/cell-traffic-automations/providers/{providerId}", "GetCephalonCellTrafficAutomationsByProvider", static context =>
            TypedResults.Ok(GetRequiredService<ICellTrafficAutomationRuntimeCatalog>(context).GetByProvider(GetRouteValue(context, "providerId"))));
        MapGetResultRequestDelegate(engineGroup, "/cell-traffic-automations/edge-nodes/{edgeNodeId}", "GetCephalonCellTrafficAutomationsByEdgeNode", static context =>
            TypedResults.Ok(GetRequiredService<ICellTrafficAutomationRuntimeCatalog>(context).GetByEdgeNodeId(GetRouteValue(context, "edgeNodeId"))));
        MapGetResultRequestDelegate(engineGroup, "/cell-traffic-automations/health-isolations/{healthIsolationId}", "GetCephalonCellTrafficAutomationsByHealthIsolation", static context =>
            TypedResults.Ok(GetRequiredService<ICellTrafficAutomationRuntimeCatalog>(context).GetByHealthIsolationId(GetRouteValue(context, "healthIsolationId"))));
        MapGetResultRequestDelegate(engineGroup, "/cell-traffic-automations/{automationId}", "GetCephalonCellTrafficAutomation", static context =>
            {
                var automationId = GetRouteValue(context, "automationId");
                var catalog = GetRequiredService<ICellTrafficAutomationRuntimeCatalog>(context);
                var automation = catalog.GetById(automationId);

                return automation is null ? Results.NotFound() : Results.Ok(automation);
            });
        MapGetResultRequestDelegate(engineGroup, "/technologies", "GetCephalonTechnologies", static context =>
            TypedResults.Ok(GetRequiredService<RuntimeManifest>(context).AppProfile.Technologies));
        MapGetResultRequestDelegate(engineGroup, "/technology-catalog", "GetCephalonTechnologyCatalog", static context =>
            TypedResults.Ok(GetRequiredService<TechnologyCatalogSnapshot>(context).Technologies));
        MapGetResultRequestDelegate(engineGroup, "/technology-surfaces", "GetCephalonTechnologySurfaces", static context =>
            TypedResults.Ok(GetRequiredService<ITechnologyRuntimeCatalog>(context).Surfaces));
        MapGetResultRequestDelegate(engineGroup, "/technology-surfaces/{technologyId}", "GetCephalonTechnologySurface", static context =>
            TypedResults.Ok(GetRequiredService<ITechnologyRuntimeCatalog>(context).GetByTechnology(GetRouteValue(context, "technologyId"))));
        MapGetResultRequestDelegate(engineGroup, "/knowledge-indexes", "GetCephalonKnowledgeIndexes", static context =>
            {
                var catalog = context.RequestServices.GetService<IKnowledgeIndexCatalog>();
                IReadOnlyList<KnowledgeIndexState> states = catalog?.States ?? [];

                return TypedResults.Ok(states);
            });
        MapGetResultRequestDelegate(engineGroup, "/knowledge-indexes/{collectionId}", "GetCephalonKnowledgeIndex", static context =>
            {
                var collectionId = GetRouteValue(context, "collectionId");
                var catalog = context.RequestServices.GetService<IKnowledgeIndexCatalog>();
                var state = catalog?.GetByCollectionId(collectionId);

                return state is null ? Results.NotFound() : Results.Ok(state);
            });
        MapPostAsyncResultRequestDelegate(
            engineGroup,
            "/knowledge-indexes/{collectionId}/queries",
            "QueryCephalonKnowledgeIndex",
            static async context =>
                {
                    var collectionId = GetRouteValue(context, "collectionId");
                    var (request, bodyError) = await ReadOptionalJsonBodyAsync(
                            context,
                            AspNetCoreJsonSerializerContext.Default.KnowledgeQueryHttpRequest)
                        .ConfigureAwait(false);
                    if (bodyError is not null)
                    {
                        return bodyError;
                    }

                    var queryEngine = context.RequestServices.GetService<IKnowledgeQueryEngine>();
                    if (queryEngine is null)
                    {
                        return Results.NotFound(new
                        {
                            error = "Knowledge querying is not available in the active runtime."
                        });
                    }

                    try
                    {
                        var queryRequest = CreateKnowledgeQueryRequest(collectionId, request, context);
                        var result = await queryEngine.QueryAsync(queryRequest, context.RequestAborted).ConfigureAwait(false);
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
                });
        MapPostAsyncResultRequestDelegate(
            engineGroup,
            "/knowledge-indexes/{collectionId}/reindex",
            "ReindexCephalonKnowledgeIndex",
            static async context =>
                {
                    var collectionId = GetRouteValue(context, "collectionId");
                    var runId = GetQueryValue(context, "runId");
                    var actorId = GetQueryValue(context, "actorId");
                    var correlationId = GetQueryValue(context, "correlationId");
                    var indexer = context.RequestServices.GetService<IKnowledgeIndexer>();
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
                            ResolveKnowledgeOperatorActorId(context, actorId),
                            string.IsNullOrWhiteSpace(correlationId) ? context.TraceIdentifier : correlationId,
                            metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                            {
                                ["trigger"] = "aspnetcore-operator-route",
                                ["route"] = "/engine/knowledge-indexes/{collectionId}/reindex"
                            });

                        var result = await indexer.IndexAsync(request, context.RequestAborted).ConfigureAwait(false);
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
                });
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

    private static void MapPostRequestDelegate(
        RouteGroupBuilder engineGroup,
        string pattern,
        string endpointName,
        RequestDelegate requestDelegate)
    {
        engineGroup.MapMethods(pattern, [HttpMethods.Post], requestDelegate)
            .WithName(endpointName);
    }

    private static IEndpointConventionBuilder MapGetRequestDelegate(
        WebApplication app,
        string pattern,
        RequestDelegate requestDelegate)
    {
        return app.MapMethods(pattern, [HttpMethods.Get], requestDelegate);
    }

    private static IEndpointConventionBuilder MapGetResultRequestDelegate(
        WebApplication app,
        string pattern,
        Func<HttpContext, IResult> handler)
    {
        return MapGetRequestDelegate(app, pattern, context =>
            handler(context).ExecuteAsync(context));
    }

    private static IEndpointConventionBuilder MapGetAsyncResultRequestDelegate(
        WebApplication app,
        string pattern,
        Func<HttpContext, Task<IResult>> handler)
    {
        return MapGetRequestDelegate(app, pattern, async context =>
        {
            var result = await handler(context).ConfigureAwait(false);
            await result.ExecuteAsync(context).ConfigureAwait(false);
        });
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

    private static void MapGetAsyncResultRequestDelegate(
        RouteGroupBuilder engineGroup,
        string pattern,
        string endpointName,
        Func<HttpContext, Task<IResult>> handler)
    {
        MapGetRequestDelegate(engineGroup, pattern, endpointName, async context =>
        {
            var result = await handler(context).ConfigureAwait(false);
            await result.ExecuteAsync(context).ConfigureAwait(false);
        });
    }

    private static void MapPostAsyncResultRequestDelegate(
        RouteGroupBuilder engineGroup,
        string pattern,
        string endpointName,
        Func<HttpContext, Task<IResult>> handler)
    {
        MapPostRequestDelegate(engineGroup, pattern, endpointName, async context =>
        {
            var result = await handler(context).ConfigureAwait(false);
            await result.ExecuteAsync(context).ConfigureAwait(false);
        });
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

    private static bool TryGetRequiredQueryValue(
        HttpContext context,
        string name,
        [NotNullWhen(true)] out string? value,
        [NotNullWhen(false)] out IResult? error)
    {
        value = GetQueryValue(context, name);
        if (!string.IsNullOrWhiteSpace(value))
        {
            error = null;
            return true;
        }

        error = Results.BadRequest($"Query parameter '{name}' is required.");
        return false;
    }

    private static bool TryGetNullableIntQueryValue(
        HttpContext context,
        string name,
        out int? value,
        [NotNullWhen(false)] out IResult? error)
    {
        var rawValue = GetQueryValue(context, name);
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            value = null;
            error = null;
            return true;
        }

        if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            value = parsed;
            error = null;
            return true;
        }

        value = null;
        error = Results.BadRequest($"Query parameter '{name}' must be a valid integer.");
        return false;
    }

    private static bool TryGetNullableDateTimeOffsetQueryValue(
        HttpContext context,
        string name,
        out DateTimeOffset? value,
        [NotNullWhen(false)] out IResult? error)
    {
        var rawValue = GetQueryValue(context, name);
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            value = null;
            error = null;
            return true;
        }

        if (DateTimeOffset.TryParse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
        {
            value = parsed;
            error = null;
            return true;
        }

        value = null;
        error = Results.BadRequest($"Query parameter '{name}' must be a valid date/time offset.");
        return false;
    }

    private static IResult OkLimitedEventDispatchRemediationCommandStates(
        HttpContext context,
        IReadOnlyList<EventDispatchRemediationRuntimeState> states)
    {
        var rawPageSize = GetQueryValue(context, "pageSize");
        var rawContinuationToken = GetQueryValue(context, "continuationToken");
        if (!string.IsNullOrWhiteSpace(rawPageSize) || !string.IsNullOrWhiteSpace(rawContinuationToken))
        {
            return OkPagedEventDispatchRemediationCommandStates(context, states);
        }

        if (!TryGetNullableIntQueryValue(context, "limit", out var limit, out var error))
        {
            return error;
        }

        if (limit.HasValue && limit.Value < 1)
        {
            return Results.BadRequest("Query parameter 'limit' must be greater than or equal to 1.");
        }

        if (limit is null || states.Count <= limit.Value)
        {
            return Results.Ok(states);
        }

        return Results.Ok(states.Take(limit.Value).ToArray());
    }

    private static IEventDispatchRemediationRuntimeCatalog? ResolveEventDispatchRemediationCommandReadModel(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.GetService<IEventDispatchRemediationCommandJournal>() ??
            services.GetService<IEventDispatchRemediationRuntimeCatalog>();
    }

    private static IResult OkPagedEventDispatchRemediationCommandStates(
        HttpContext context,
        IReadOnlyList<EventDispatchRemediationRuntimeState> states)
    {
        if (!string.IsNullOrWhiteSpace(GetQueryValue(context, "limit")))
        {
            return Results.BadRequest("Query parameter 'limit' cannot be combined with 'pageSize' or 'continuationToken'.");
        }

        if (!TryGetNullableIntQueryValue(context, "pageSize", out var pageSize, out var error))
        {
            return error;
        }

        var effectivePageSize = pageSize ?? DefaultEventDispatchRemediationCommandPageSize;
        if (effectivePageSize < 1)
        {
            return Results.BadRequest("Query parameter 'pageSize' must be greater than or equal to 1.");
        }

        if (effectivePageSize > MaxEventDispatchRemediationCommandPageSize)
        {
            return Results.BadRequest($"Query parameter 'pageSize' must be less than or equal to {MaxEventDispatchRemediationCommandPageSize.ToString(CultureInfo.InvariantCulture)}.");
        }

        var continuationScopeHash = CreateEventDispatchRemediationCommandContinuationScopeHash(context);
        var rawContinuationToken = GetQueryValue(context, "continuationToken");
        if (!TryParseEventDispatchRemediationCommandContinuationToken(rawContinuationToken, continuationScopeHash, out var continuationKey, out error))
        {
            return error;
        }

        var orderedStates = states
            .OrderByDescending(static state => state.ObservedAtUtc)
            .ThenBy(static state => state.CommandId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var candidates = continuationKey is null
            ? orderedStates
            : orderedStates
                .Where(state => IsAfterEventDispatchRemediationCommandContinuationToken(state, continuationKey.Value))
                .ToArray();
        var buffer = candidates.Take(effectivePageSize + 1).ToArray();
        var items = buffer.Take(effectivePageSize).ToArray();
        var hasMore = buffer.Length > effectivePageSize;

        return Results.Ok(new EventDispatchRemediationCommandPage
        {
            Items = items,
            PageSize = effectivePageSize,
            ReturnedCount = items.Length,
            TotalRetainedCount = orderedStates.Length,
            ContinuationToken = string.IsNullOrWhiteSpace(rawContinuationToken) ? null : rawContinuationToken,
            NextContinuationToken = hasMore && items.Length > 0
                ? CreateEventDispatchRemediationCommandContinuationToken(items[^1], continuationScopeHash)
                : null,
            HasMore = hasMore
        });
    }

    private static bool TryParseEventDispatchRemediationCommandContinuationToken(
        string? rawValue,
        byte[] continuationScopeHash,
        out EventDispatchRemediationCommandContinuationKey? continuationKey,
        [NotNullWhen(false)] out IResult? error)
    {
        continuationKey = null;
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            error = null;
            return true;
        }

        var payload = TryBase64UrlDecode(rawValue.Trim());
        var magicBytes = Encoding.ASCII.GetBytes(EventDispatchRemediationCommandContinuationTokenMagic);
        var minimumLength = magicBytes.Length
            + EventDispatchRemediationCommandContinuationScopeHashLength
            + sizeof(long)
            + EventDispatchRemediationCommandContinuationSignatureLength;
        if (payload is null || payload.Length <= minimumLength)
        {
            error = Results.BadRequest("Query parameter 'continuationToken' must be an opaque token returned by a previous command-result page.");
            return false;
        }

        var payloadSpan = payload.AsSpan();
        if (!payloadSpan[..magicBytes.Length].SequenceEqual(magicBytes))
        {
            error = Results.BadRequest("Query parameter 'continuationToken' must be an opaque token returned by a previous command-result page.");
            return false;
        }

        var signatureOffset = payload.Length - EventDispatchRemediationCommandContinuationSignatureLength;
        var unsignedPayload = payloadSpan[..signatureOffset];
        var signature = payloadSpan[signatureOffset..];
        var expectedSignature = HMACSHA256.HashData(EventDispatchRemediationCommandContinuationSigningKey, unsignedPayload);
        if (!CryptographicOperations.FixedTimeEquals(signature, expectedSignature))
        {
            error = Results.BadRequest("Query parameter 'continuationToken' must be an opaque token returned by a previous command-result page.");
            return false;
        }

        var scopeHashOffset = magicBytes.Length;
        if (!unsignedPayload.Slice(scopeHashOffset, EventDispatchRemediationCommandContinuationScopeHashLength).SequenceEqual(continuationScopeHash))
        {
            error = Results.BadRequest("Query parameter 'continuationToken' must belong to the current command-result route and filters.");
            return false;
        }

        var ticksOffset = scopeHashOffset + EventDispatchRemediationCommandContinuationScopeHashLength;
        var observedAtUtcTicks = BinaryPrimitives.ReadInt64BigEndian(unsignedPayload.Slice(ticksOffset, sizeof(long)));
        var commandIdOffset = ticksOffset + sizeof(long);
        var commandId = Encoding.UTF8.GetString(payload, commandIdOffset, signatureOffset - commandIdOffset);
        if (observedAtUtcTicks < 0 || string.IsNullOrWhiteSpace(commandId))
        {
            error = Results.BadRequest("Query parameter 'continuationToken' must be an opaque token returned by a previous command-result page.");
            return false;
        }

        continuationKey = new EventDispatchRemediationCommandContinuationKey(observedAtUtcTicks, commandId);
        error = null;
        return true;
    }

    private static bool IsAfterEventDispatchRemediationCommandContinuationToken(
        EventDispatchRemediationRuntimeState state,
        EventDispatchRemediationCommandContinuationKey continuationKey)
    {
        var observedAtUtcTicks = state.ObservedAtUtc.UtcDateTime.Ticks;
        if (observedAtUtcTicks != continuationKey.ObservedAtUtcTicks)
        {
            return observedAtUtcTicks < continuationKey.ObservedAtUtcTicks;
        }

        return StringComparer.Ordinal.Compare(state.CommandId, continuationKey.CommandId) > 0;
    }

    private static string CreateEventDispatchRemediationCommandContinuationToken(
        EventDispatchRemediationRuntimeState state,
        byte[] continuationScopeHash)
    {
        var magicBytes = Encoding.ASCII.GetBytes(EventDispatchRemediationCommandContinuationTokenMagic);
        var commandIdBytes = Encoding.UTF8.GetBytes(state.CommandId);
        var unsignedPayload = new byte[magicBytes.Length + continuationScopeHash.Length + sizeof(long) + commandIdBytes.Length];
        magicBytes.CopyTo(unsignedPayload.AsSpan(0, magicBytes.Length));
        continuationScopeHash.CopyTo(unsignedPayload.AsSpan(magicBytes.Length, continuationScopeHash.Length));
        BinaryPrimitives.WriteInt64BigEndian(
            unsignedPayload.AsSpan(magicBytes.Length + continuationScopeHash.Length, sizeof(long)),
            state.ObservedAtUtc.UtcDateTime.Ticks);
        commandIdBytes.CopyTo(unsignedPayload.AsSpan(magicBytes.Length + continuationScopeHash.Length + sizeof(long)));

        var signature = HMACSHA256.HashData(EventDispatchRemediationCommandContinuationSigningKey, unsignedPayload);
        var payload = new byte[unsignedPayload.Length + signature.Length];
        unsignedPayload.CopyTo(payload.AsSpan(0, unsignedPayload.Length));
        signature.CopyTo(payload.AsSpan(unsignedPayload.Length, signature.Length));

        return Base64UrlEncode(payload);
    }

    private static byte[] CreateEventDispatchRemediationCommandContinuationScopeHash(HttpContext context)
    {
        var builder = new StringBuilder();
        var path = context.Request.Path.Value ?? string.Empty;
        AppendLengthPrefixedValue(builder, path.ToLowerInvariant());

        foreach (var query in context.Request.Query
            .Where(static pair => !IsEventDispatchRemediationCommandContinuationControlQuery(pair.Key))
            .OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            var key = query.Key.ToLowerInvariant();
            foreach (var value in query.Value.OrderBy(static value => value, StringComparer.Ordinal))
            {
                AppendLengthPrefixedValue(builder, key);
                AppendLengthPrefixedValue(builder, value ?? string.Empty);
            }
        }

        return SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
    }

    private static bool IsEventDispatchRemediationCommandContinuationControlQuery(string queryName)
    {
        return string.Equals(queryName, "pageSize", StringComparison.OrdinalIgnoreCase)
            || string.Equals(queryName, "continuationToken", StringComparison.OrdinalIgnoreCase)
            || string.Equals(queryName, "limit", StringComparison.OrdinalIgnoreCase);
    }

    private static void AppendLengthPrefixedValue(StringBuilder builder, string value)
    {
        builder
            .Append(value.Length.ToString(CultureInfo.InvariantCulture))
            .Append(':')
            .Append(value)
            .Append('|');
    }

    private static string Base64UrlEncode(byte[] payload)
    {
        return Convert.ToBase64String(payload)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static byte[]? TryBase64UrlDecode(string value)
    {
        var base64 = value.Replace('-', '+').Replace('_', '/');
        switch (base64.Length % 4)
        {
            case 0:
                break;
            case 2:
                base64 += "==";
                break;
            case 3:
                base64 += "=";
                break;
            default:
                return null;
        }

        try
        {
            return Convert.FromBase64String(base64);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private readonly record struct EventDispatchRemediationCommandContinuationKey(
        long ObservedAtUtcTicks,
        string CommandId);

    private static async Task<(TValue? Value, IResult? Error)> ReadOptionalJsonBodyAsync<TValue>(
        HttpContext context,
        JsonTypeInfo<TValue> jsonTypeInfo)
    {
        if (context.Request.ContentLength == 0)
        {
            return (default, null);
        }

        try
        {
            if (context.Request.ContentLength is null)
            {
                using var buffer = new MemoryStream();
                await context.Request.Body.CopyToAsync(buffer, context.RequestAborted).ConfigureAwait(false);
                if (buffer.Length == 0)
                {
                    return (default, null);
                }

                buffer.Position = 0;
                var bufferedValue = await JsonSerializer.DeserializeAsync(
                        buffer,
                        jsonTypeInfo,
                        context.RequestAborted)
                    .ConfigureAwait(false);

                return (bufferedValue, null);
            }

            var value = await JsonSerializer.DeserializeAsync(
                    context.Request.Body,
                    jsonTypeInfo,
                    context.RequestAborted)
                .ConfigureAwait(false);

            return (value, null);
        }
        catch (JsonException exception)
        {
            return (default, Results.BadRequest($"Invalid JSON request body: {exception.Message}"));
        }
        catch (NotSupportedException exception)
        {
            return (default, Results.BadRequest($"Unsupported JSON request body: {exception.Message}"));
        }
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
            MapGetResultRequestDelegate(
                    app,
                    openApiToggleScriptRoute,
                    _ => Results.Text(
                        RenderOpenApiToggleScript(
                            openApiEndpointOptions.ScalarRoutePrefix,
                            openApiDocumentNames,
                            defaultOpenApiDocumentName),
                        "application/javascript"))
                .DisableRateLimiting()
                .ExcludeFromDescription();
            MapGetResultRequestDelegate(app, scalarFaviconRoute, _ => Results.Text(ScalarFavicon.Value, "image/svg+xml"))
                .DisableRateLimiting()
                .ExcludeFromDescription();
            MapGetResultRequestDelegate(app, "/favicon.ico", _ => Results.Redirect(scalarFaviconReference))
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

        MapGetResultRequestDelegate(app, surface.RoutePrefix, _ => Results.Redirect(surface.DefaultDocumentPath))
            .DisableRateLimiting()
            .ExcludeFromDescription();
        MapGetResultRequestDelegate(app, $"{surface.RoutePrefix}/", _ => Results.Redirect(surface.DefaultDocumentPath))
            .DisableRateLimiting()
            .ExcludeFromDescription();
        MapGetResultRequestDelegate(app, $"{surface.RoutePrefix}/{{**filePath}}", context =>
                ServeReferenceDocsFile(options, GetRouteValue(context, "filePath")))
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

        MapGetAsyncResultRequestDelegate(app, bindingRoutePattern, async context =>
            {
                var publisher = context.RequestServices.GetRequiredService<AspNetCoreBackendForFrontendRestDocumentPublisher>();
                var payload = await publisher
                    .GenerateBindingDocumentAsync(
                        GetRouteValue(context, "bindingId"),
                        GetRouteValue(context, "documentName"),
                        context.RequestAborted)
                    .ConfigureAwait(false);

                return payload is null
                    ? Results.NotFound()
                    : Results.Text(payload, "application/json");
            })
            .DisableRateLimiting()
            .ExcludeFromDescription();

        MapGetAsyncResultRequestDelegate(app, clientRoutePattern, async context =>
            {
                var publisher = context.RequestServices.GetRequiredService<AspNetCoreBackendForFrontendRestDocumentPublisher>();
                var payload = await publisher
                    .GenerateClientDocumentAsync(
                        GetRouteValue(context, "clientId"),
                        GetRouteValue(context, "documentName"),
                        context.RequestAborted)
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

        MapGetResultRequestDelegate(
                app,
                openApiToggleScriptRoute,
                httpContext =>
                {
                    var catalog = httpContext.RequestServices.GetRequiredService<IBackendForFrontendRestDocumentRuntimeCatalog>();
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
        MapGetResultRequestDelegate(app, scalarFaviconRoute, _ => Results.Text(ScalarFavicon.Value, "image/svg+xml"))
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

    private static EventDispatchRemediationRequest CreateEventDispatchRemediationRequest(
        string outboxId,
        string operationId,
        EventDispatchRemediationHttpRequest? request,
        HttpContext httpContext)
    {
        if (request is null)
        {
            throw new ArgumentException("Event dispatch remediation request body is required.", nameof(request));
        }

        var metadata = CopyEventPublicationValues(request.Metadata);
        metadata["trigger"] = "aspnetcore-operator-route";
        metadata["route"] = "/engine/event-dispatches/{outboxId}/commands/{operationId}";

        return new EventDispatchRemediationRequest(
            outboxId: outboxId,
            messageId: request.MessageId ?? string.Empty,
            channelId: request.ChannelId ?? string.Empty,
            operationId: operationId,
            commandId: request.CommandId,
            nextAttemptAtUtc: request.NextAttemptAtUtc,
            reason: request.Reason,
            actorId: ResolveEventPublicationActorId(httpContext, request.ActorId),
            correlationId: string.IsNullOrWhiteSpace(request.CorrelationId) ? httpContext.TraceIdentifier : request.CorrelationId,
            metadata: metadata);
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
