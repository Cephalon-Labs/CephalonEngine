using Cephalon.AspNetCore.Hosting;
using Cephalon.Audit.Registration;
using Cephalon.Ids.Sfid.Registration;
using Cephalon.Observability.Hosting;
using Cephalon.Observability.OpenTelemetry.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;

var options = new WebApplicationOptions
{
    Args = args,
    ContentRootPath = WindowsServiceHelpers.IsWindowsService()
        ? AppContext.BaseDirectory
        : default
};

var builder = WebApplication.CreateBuilder(options);
builder.Host.UseWindowsService();

builder.AddCephalon(engine =>
{
    engine.AddSfidIds();
    engine.AddAudit();
});
builder.Services.AddCephalonObservability(builder.Configuration);
builder.AddCephalonOpenTelemetry();

var app = builder.Build();

app.UseExceptionHandler();
app.MapGet("/", () => TypedResults.Ok(new
{
    name = "CephalonTemplateApp",
    blueprint = "Microservice"
})).ExcludeFromDescription();

app.MapCephalon();
app.Run();
