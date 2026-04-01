using Cephalon.AspNetCore.Hosting;
using Cephalon.Observability.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.AddCephalon();
builder.Services.AddCephalonObservability(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.MapGet("/", () => TypedResults.Ok(new
{
    name = "CephalonTemplateApp",
    blueprint = "ModularVerticalSlice"
})).ExcludeFromDescription();

app.MapCephalon();
app.Run();
