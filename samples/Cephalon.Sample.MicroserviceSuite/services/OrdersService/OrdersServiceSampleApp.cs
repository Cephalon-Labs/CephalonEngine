using Cephalon.AspNetCore.Hosting;
using Cephalon.Observability.Hosting;
using Cephalon.Sample.MicroserviceSuite.Foundation.Conventions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Cephalon.Sample.MicroserviceSuite.OrdersService;

/// <summary>
/// Builds the orders-service sample host used by tests, docs, and local exploration.
/// </summary>
public static class OrdersServiceSampleApp
{
    /// <summary>
    /// Builds the orders-service sample application with the default Cephalon host wiring.
    /// </summary>
    /// <param name="args">
    /// Optional command-line arguments for the sample host.
    /// </param>
    /// <param name="configureBuilder">
    /// Optional hook that can customize the application builder before the host is built.
    /// </param>
    /// <returns>
    /// The configured orders-service sample application.
    /// </returns>
    public static WebApplication Build(
        string[]? args = null,
        Action<WebApplicationBuilder>? configureBuilder = null)
    {
        var contentRoot = Path.GetDirectoryName(typeof(OrdersServiceSampleApp).Assembly.Location)
            ?? AppContext.BaseDirectory;
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args ?? [],
            ApplicationName = typeof(OrdersServiceSampleApp).Assembly.FullName,
            ContentRootPath = contentRoot,
            EnvironmentName = Environments.Development
        });

        configureBuilder?.Invoke(builder);
        builder.Configuration.AddJsonFile("orders-service.settings.json", optional: false, reloadOnChange: false);

        builder.AddCephalon();
        builder.Services.AddCephalonObservability(builder.Configuration);

        var app = builder.Build();
        app.UseExceptionHandler();
        app.MapGet("/", () => TypedResults.Ok(new
        {
            sample = "MicroserviceSuite",
            service = CommerceSuiteConventions.OrdersService,
            suite = CommerceSuiteConventions.SuiteName
        })).ExcludeFromDescription();
        app.MapCephalon();

        return app;
    }
}
