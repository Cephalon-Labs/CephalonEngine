using System.Text.RegularExpressions;
using Cephalon.AspNetCore.Hosting;
using Cephalon.Diagnostics.Redaction;
using Cephalon.Diagnostics.Redaction.Defaults;
using Cephalon.Observability.Hosting;
using Cephalon.Sample.MicroserviceSuite.Foundation.Conventions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        var contentRoot = AppContext.BaseDirectory;
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

        // Canonical redaction recipe — see docs/components/diagnostics.md "Redaction quick start".
        builder.Services.AddSingleton<IRedactionFilter>(new KeyMatchRedactionFilter(
        [
            "http.request.header.authorization",
            "http.request.header.cookie",
            "http.request.header.proxy-authorization",
            "http.response.header.set-cookie",
            "cephalon.tenant.secret",
        ]));
        builder.Services.AddSingleton<IRedactionFilter>(new RegexRedactionFilter(
            new Regex(@"\b(?:\d[ -]*?){13,19}\b", RegexOptions.Compiled)));
        builder.Services.AddSingleton<IRedactionFilter>(new RegexRedactionFilter(
            new Regex(@"Bearer\s+[A-Za-z0-9\-_\.]+", RegexOptions.Compiled),
            replacement: "Bearer [REDACTED]"));

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
