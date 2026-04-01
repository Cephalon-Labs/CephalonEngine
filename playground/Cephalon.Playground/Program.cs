using Cephalon.Agentics.Registration;
using Cephalon.AspNetCore.Hosting;
using Cephalon.AspNetCore.Grpc.Hosting;
using Cephalon.AspNetCore.JsonRpc.Hosting;
using Cephalon.Edge.Registration;
using Cephalon.Edge.Services;
using Cephalon.Eventing.Registration;
using Cephalon.Eventing.Services;
using Cephalon.Observability.Hosting;
using Cephalon.Retrieval.Registration;
using Cephalon.Agentics.Services;
using Cephalon.Retrieval.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddGrpcTransport();
builder.AddJsonRpcTransport();
builder.AddCephalon(engine =>
{
    engine.AddAgentics(options =>
    {
        options.EnableMemory = true;
        options.Tools.Add(new AgentToolDescriptor(
            id: "discovery.hello",
            displayName: "Discovery Hello",
            description: "Builds a greeting from the discovery module.",
            tags: ["discovery", "greeting"]));
        options.Tools.Add(new AgentToolDescriptor(
            id: "platform.time",
            displayName: "Platform Time",
            description: "Returns deterministic runtime time.",
            tags: ["platform", "clock"]));
    });

    engine.AddRetrieval(options =>
    {
        options.Collections.Add(new KnowledgeCollectionDescriptor(
            id: "principles",
            displayName: "Principles",
            description: "Framework principles and guidance for future-facing workloads.",
            tags: ["docs", "guidance"]));
        options.Collections.Add(new KnowledgeCollectionDescriptor(
            id: "modules",
            displayName: "Modules",
            description: "Operational module and capability knowledge for the playground runtime.",
            tags: ["runtime", "modules"]));
    });

    engine.AddEventing(options =>
    {
        options.Channels.Add(new EventChannelDescriptor(
            id: "orders",
            displayName: "Orders Channel",
            description: "Asynchronous order and fulfillment events for integration-heavy flows.",
            tags: ["orders", "broker"]));
        options.Channels.Add(new EventChannelDescriptor(
            id: "notifications",
            displayName: "Notifications Channel",
            description: "Realtime and asynchronous notification fan-out for edge and client delivery.",
            tags: ["notifications", "fanout"]));
    });

    engine.AddEdge(options =>
    {
        options.Nodes.Add(new EdgeNodeDescriptor(
            id: "bangkok-gateway",
            displayName: "Bangkok Gateway",
            description: "Regional gateway that tolerates partial connectivity and local caching.",
            tags: ["gateway", "apac"]));
        options.Nodes.Add(new EdgeNodeDescriptor(
            id: "field-device",
            displayName: "Field Device",
            description: "Intermittently connected device profile used for delayed synchronization scenarios.",
            tags: ["device", "offline"]));
    });
});
builder.Services.AddCephalonObservability(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();

app.MapGet("/", () => TypedResults.Ok(new
{
    name = "Cephalon",
    mode = "Future-ready engine/framework foundation",
    docs = ProgramLinks.Docs
})).WithName("GetWelcome")
  .ExcludeFromDescription();

app.MapCephalon();

app.Run();

public partial class Program;

internal static class ProgramLinks
{
    public static readonly string[] Docs =
    [
        "/engine",
        "/engine/app-model",
        "/engine/modules",
        "/engine/patterns",
        "/engine/transports",
        "/engine/technology-catalog",
        "/engine/technology-surfaces",
        "/engine/localization",
        "/engine/reference-docs",
        "/engine/options",
        "/engine/status",
        "/engine/capabilities",
        "/reference",
        "/reference/browse.html",
        "/reference/members.md",
        "/reference/reference-manifest.json",
        "/openapi/v1.json",
        "/scalar",
        "/scalar/v1",
        "/scalar/openapi-toggle.js",
        "/scalar/assets/favicon.svg",
        "/api/platform/time",
        "/api/discovery/hello/Codex",
        "/rpc/discovery",
        "/events/discovery/principles",
        "/ws/discovery"
    ];
}
