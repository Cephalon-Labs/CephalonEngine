using Cephalon.AspNetCore.Hosting;
using Cephalon.Abstractions.Transports;
using Cephalon.Engine.Runtime;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace Cephalon.AspNetCore.Transports.Rest;

internal sealed class RestTransportRouteMapper : ITransportRouteMapper
{
    private readonly ApiRoutesOptions options;

    public RestTransportRouteMapper(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        options = ApiRoutesOptions.FromConfiguration(configuration);
    }

    public string TransportId => "rest-api";

    public void MapRoutes(WebApplication app, IRuntime runtime)
    {
        var candidateRegistry = app.Services.GetService(typeof(IRestEndpointCandidateRuntimeRegistry)) as IRestEndpointCandidateRuntimeRegistry;
        candidateRegistry?.Clear();

        var registry = app.Services.GetService(typeof(IRestEndpointRuntimeRegistry)) as IRestEndpointRuntimeRegistry;
        registry?.Clear();

        var apiGroup = app.MapGroup(options.RestPrefix)
            .ApplyCephalonRateLimiting(app.Services, TransportId);
        foreach (var module in runtime.Modules.OfType<IRestModule>())
        {
            var moduleGroup = apiGroup.MapGroup(string.Empty);
            moduleGroup.WithMetadata(RestEndpointRuntimeMaterializer.CreateModuleMetadata(module));
            module.MapRestEndpoints(moduleGroup);
        }

        RestEndpointRuntimeMaterializer.RegisterModuleOwnedEndpoints(app, options, registry);
    }
}
