using Cephalon.Observability.Hosting;
using Cephalon.Worker.Hosting;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.AddCephalon();
builder.Services.AddCephalonObservability(builder.Configuration);

var host = builder.Build();
await host.RunAsync();
