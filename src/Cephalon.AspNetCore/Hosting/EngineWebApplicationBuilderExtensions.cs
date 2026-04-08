using Cephalon.AspNetCore.Documentation;
using Cephalon.AspNetCore.Health;
using Cephalon.AspNetCore.Transports.Rest;
using Cephalon.AspNetCore.Transports.ServerSentEvents;
using Cephalon.AspNetCore.Transports.WebSockets;
using Cephalon.AspNetCore.Transformers;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Cephalon.Engine.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

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
    /// settings such as engine, OpenAPI, CORS, or hosted-doc options do not need to live in one
    /// large <c>appsettings.json</c> file.
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
        builder.AddReferenceDocsHosting();
        builder.Services.AddCephalon(builder.Configuration, configure);

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
