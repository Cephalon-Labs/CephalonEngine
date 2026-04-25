using Cephalon.AspNetCore.Hosting;
using Cephalon.Audit.Registration;
using Cephalon.Behaviors.Hosting;
using Cephalon.Behaviors.Http.Hosting;
using Cephalon.Ids.Sfid.Registration;
using Cephalon.Observability.Hosting;
using Cephalon.Observability.OpenTelemetry.Hosting;
using Cephalon.Observability.Serilog.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting.WindowsServices;

var options = new WebApplicationOptions
{
    Args = args,
    ContentRootPath = WindowsServiceHelpers.IsWindowsService()
        ? AppContext.BaseDirectory
        : default
};

var builder = WebApplication.CreateBuilder(options);
builder.AddCephalonProjectConfigurations();
builder.Host.UseWindowsService();

builder.AddCephalon(engine =>
{
    engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
    {
        behaviors.AddHttpBehaviorBindings();
    });
    engine.AddSfidIds();
    engine.AddAudit();
});
builder.Services.AddCephalonObservability(builder.Configuration);
if (builder.Configuration.GetSection("Serilog").Exists())
{
    builder.Logging.ClearProviders();
}
builder.AddCephalonSerilog();
builder.AddCephalonOpenTelemetry();

var app = builder.Build();

app.UseExceptionHandler();
app.MapGet("/", () => TypedResults.Ok(new
{
    name = "CephalonTemplateApp",
    blueprint = "ModularVerticalSlice"
})).ExcludeFromDescription();

app.MapCephalon();
app.Run();
