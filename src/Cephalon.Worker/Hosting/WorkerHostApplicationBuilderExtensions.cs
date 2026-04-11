using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Microsoft.Extensions.Hosting;

namespace Cephalon.Worker.Hosting;

/// <summary>
/// Registers the Cephalon worker host adapter on a <see cref="HostApplicationBuilder" />.
/// </summary>
public static class WorkerHostApplicationBuilderExtensions
{
    /// <summary>
    /// Adds Cephalon's project-configuration conventions to the generic host builder.
    /// </summary>
    /// <param name="builder">The generic host application builder to extend.</param>
    /// <returns>The same builder instance for fluent composition.</returns>
    /// <remarks>
    /// This loads split configuration files from the project's <c>Configurations</c> folder so
    /// engine and host-specific settings can be grouped by concern while preserving the standard
    /// <c>appsettings.json</c> and <c>appsettings.{Environment}.json</c> override path.
    /// </remarks>
    public static HostApplicationBuilder AddCephalonProjectConfigurations(this HostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Configuration.AddCephalonProjectConfigurations(
            builder.Environment.ContentRootPath,
            builder.Environment.EnvironmentName);

        return builder;
    }

    /// <summary>
    /// Adds Cephalon worker hosting using configuration-only engine setup.
    /// </summary>
    /// <param name="builder">The generic host application builder to extend.</param>
    /// <returns>The same builder instance for fluent composition.</returns>
    public static HostApplicationBuilder AddCephalon(this HostApplicationBuilder builder)
    {
        return builder.AddCephalon(static _ => { });
    }

    /// <summary>
    /// Adds Cephalon worker hosting and allows additional code-based engine configuration.
    /// </summary>
    /// <param name="builder">The generic host application builder to extend.</param>
    /// <param name="configure">The callback that configures the underlying engine builder.</param>
    /// <returns>The same builder instance for fluent composition.</returns>
    public static HostApplicationBuilder AddCephalon(
        this HostApplicationBuilder builder,
        Action<EngineBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddCephalonProjectConfigurations();

        builder.Services.AddCephalonWorker(builder.Configuration, configure);

        return builder;
    }
}
