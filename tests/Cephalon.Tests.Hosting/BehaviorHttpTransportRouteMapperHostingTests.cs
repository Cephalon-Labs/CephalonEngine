using Cephalon.Abstractions.Behaviors;
using Cephalon.Abstractions.Modules;
using Cephalon.AspNetCore.Hosting;
using Cephalon.Behaviors.Hosting;
using Cephalon.Behaviors.Http.Hosting;
using Cephalon.Behaviors.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;

namespace Cephalon.Tests.Hosting;

public sealed class BehaviorHttpTransportRouteMapperHostingTests
{
    [Fact]
    public async Task MapCephalonDoesNotResolveBehaviorDispatcherWhenBehaviorHttpTransportIsNotSelected()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Configuration["Engine:Blueprint"] = "ModularMonolith";
        builder.Configuration["Engine:Transports:0"] = "RestApi";
        builder.AddCephalon(engine =>
        {
            engine.AddModule(new UntypedBehaviorHttpOnlyModule());
            engine.AddBehaviors(options => options.AutoRegister = false, behaviors =>
            {
                behaviors.AddHttpBehaviorBindings();
            });
        });

        await using var app = builder.Build();
        app.MapCephalon();
    }

    private sealed class UntypedBehaviorHttpOnlyModule : BehaviorModuleBase
    {
        public override ModuleDescriptor Descriptor { get; } = new(
            id: "tests.behavior-http.mapper-lazy",
            displayName: "Behavior HTTP Mapper Lazy Resolution",
            description: "Owns an untyped behavior so unselected behavior-http mapper resolution would fail if it eagerly resolved the dispatcher.",
            version: "1.0.0");

        public override void ConfigureBehaviors(IBehaviorModuleBuilder behaviors)
        {
            behaviors.Add<UntypedBehaviorHttpOnlyBehavior>(topology => topology
                .AsDirect()
                .ViaHttpJsonRpc());
        }
    }

    [AppBehavior("tests.behavior-http.mapper-lazy.probe")]
    private sealed class UntypedBehaviorHttpOnlyBehavior : IAppBehavior<string, string>
    {
        public Task<string> HandleAsync(
            string input,
            IBehaviorContext context,
            CancellationToken cancellationToken = default)
            => Task.FromResult(input);
    }
}
