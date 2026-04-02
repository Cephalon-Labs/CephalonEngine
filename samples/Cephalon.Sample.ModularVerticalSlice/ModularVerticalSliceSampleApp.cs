using Cephalon.AspNetCore.Hosting;
using Cephalon.Observability.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

namespace Cephalon.Sample.ModularVerticalSlice;

/// <summary>
/// Builds the modular vertical-slice sample host used by tests, docs, and local exploration.
/// </summary>
public static class ModularVerticalSliceSampleApp
{
    /// <summary>
    /// Builds the modular vertical-slice sample application with the default Cephalon host wiring.
    /// </summary>
    /// <param name="args">
    /// Optional command-line arguments for the sample host.
    /// </param>
    /// <param name="configureBuilder">
    /// Optional hook that can customize the application builder before the host is built.
    /// </param>
    /// <returns>
    /// The configured modular vertical-slice sample application.
    /// </returns>
    public static WebApplication Build(
        string[]? args = null,
        Action<WebApplicationBuilder>? configureBuilder = null)
    {
        var contentRoot = Path.GetDirectoryName(typeof(ModularVerticalSliceSampleApp).Assembly.Location)
            ?? AppContext.BaseDirectory;
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args ?? [],
            ApplicationName = typeof(ModularVerticalSliceSampleApp).Assembly.FullName,
            ContentRootPath = contentRoot,
            EnvironmentName = Environments.Development
        });

        configureBuilder?.Invoke(builder);
        builder.Configuration.AddJsonFile("modular-vertical-slice.settings.json", optional: false, reloadOnChange: false);

        builder.AddCephalon();
        builder.Services.AddCephalonObservability(builder.Configuration);

        var app = builder.Build();
        app.UseExceptionHandler();
        app.MapGet("/", () => TypedResults.Ok(new
        {
            sample = "ModularVerticalSlice",
            blueprint = "ModularVerticalSlice"
        })).ExcludeFromDescription();
        app.MapCephalon();

        return app;
    }
}
