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
        (app.Services.GetService(typeof(IRestEndpointRuntimeRegistry)) as IRestEndpointRuntimeRegistry)?.Clear();

        var apiGroup = app.MapGroup(options.RestPrefix)
            .ApplyCephalonRateLimiting(app.Services, TransportId);
        foreach (var module in runtime.Modules.OfType<IRestModule>())
        {
            module.MapRestEndpoints(apiGroup);
        }
    }
}
