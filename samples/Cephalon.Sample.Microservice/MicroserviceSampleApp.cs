using Cephalon.AspNetCore.Hosting;
using Cephalon.Audit.Registration;
using Cephalon.Ids.Sfid.Registration;
using Cephalon.Observability.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

namespace Cephalon.Sample.Microservice;

/// <summary>
/// Builds the microservice sample host used by tests, docs, and local exploration.
/// </summary>
public static class MicroserviceSampleApp
{
    /// <summary>
    /// Builds the microservice sample application with the default Cephalon host wiring.
    /// </summary>
    /// <param name="args">
    /// Optional command-line arguments for the sample host.
    /// </param>
    /// <param name="configureBuilder">
    /// Optional hook that can customize the application builder before the host is built.
    /// </param>
    /// <returns>
    /// The configured microservice sample application.
    /// </returns>
    public static WebApplication Build(
        string[]? args = null,
        Action<WebApplicationBuilder>? configureBuilder = null)
    {
        var contentRoot = Path.GetDirectoryName(typeof(MicroserviceSampleApp).Assembly.Location)
            ?? AppContext.BaseDirectory;
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args ?? [],
            ApplicationName = typeof(MicroserviceSampleApp).Assembly.FullName,
            ContentRootPath = contentRoot,
            EnvironmentName = Environments.Development
        });

        configureBuilder?.Invoke(builder);
        builder.Configuration.AddJsonFile("microservice.settings.json", optional: false, reloadOnChange: false);

        builder.AddCephalon(engine =>
        {
            engine.AddSfidIds();
            engine.AddAudit();
        });
        builder.Services.AddCephalonObservability(builder.Configuration);

        var app = builder.Build();
        app.UseExceptionHandler();
        app.MapGet("/", () => TypedResults.Ok(new
        {
            sample = "Microservice",
            blueprint = "Microservice"
        })).ExcludeFromDescription();
        app.MapCephalon();

        return app;
    }
}
