using Cephalon.AspNetCore.Hosting;
using Cephalon.Observability.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

namespace Cephalon.Sample.Microservice;

public static class MicroserviceSampleApp
{
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

        builder.AddCephalon();
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
