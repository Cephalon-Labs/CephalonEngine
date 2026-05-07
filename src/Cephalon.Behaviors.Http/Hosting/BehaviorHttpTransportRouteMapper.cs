using Cephalon.Abstractions.Behaviors;
using Cephalon.AspNetCore.Hosting;
using Cephalon.Behaviors.Http.Abstractions;
using Cephalon.Behaviors.Services;
using Cephalon.Engine.Runtime;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cephalon.Behaviors.Http.Hosting;

/// <summary>
/// Bridges the <see cref="ITransportRouteMapper" /> system (invoked by <c>MapCephalon()</c>)
/// to the <see cref="IHttpBehaviorBinding" /> system. When the <c>behavior-http</c> transport
/// is selected in the engine manifest, this mapper enumerates every behavior topology descriptor
/// and maps the HTTP bindings for each behavior's configured transports.
/// </summary>
internal sealed partial class BehaviorHttpTransportRouteMapper : ITransportRouteMapper
{
    private readonly IBehaviorCatalog _catalog;
    private readonly IHttpBehaviorBindingRegistry _bindingRegistry;
    private readonly IServiceProvider _services;
    private readonly ILogger<BehaviorHttpTransportRouteMapper>? _logger;

    public BehaviorHttpTransportRouteMapper(
        IBehaviorCatalog catalog,
        IHttpBehaviorBindingRegistry bindingRegistry,
        IServiceProvider services,
        ILogger<BehaviorHttpTransportRouteMapper>? logger = null)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _bindingRegistry = bindingRegistry ?? throw new ArgumentNullException(nameof(bindingRegistry));
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _logger = logger;
    }

    /// <inheritdoc />
    public string TransportId => "behavior-http";

    /// <inheritdoc />
    public void MapRoutes(WebApplication app, IRuntime runtime)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(runtime);

        var descriptors = _catalog.All;
        if (descriptors.Count == 0)
        {
            if (_logger is not null) LogNoBehaviors(_logger);
            return;
        }

        var needsWebSockets = false;
        var mappedCount = 0;
        var dispatcher = _services.GetRequiredService<BehaviorDispatcher>();

        foreach (var descriptor in descriptors)
        {
            foreach (var transportId in descriptor.TransportIds)
            {
                var binding = _bindingRegistry.GetBinding(transportId);
                if (binding is null)
                {
                    continue;
                }

                // Track whether any behavior needs WebSocket middleware
                if (string.Equals(transportId, "http.ws", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(transportId, "http.graphql-ws", StringComparison.OrdinalIgnoreCase))
                {
                    needsWebSockets = true;
                }

                // All current binding MapAsync implementations are synchronous
                // (they only call app.MapGet/MapPost), so blocking is safe here.
                binding.MapAsync(app, descriptor, dispatcher).GetAwaiter().GetResult();
                mappedCount++;
            }
        }

        // Ensure WebSocket middleware is active if any behavior uses WS transport
        if (needsWebSockets)
        {
            app.UseWebSockets();
        }

        if (_logger is not null) LogMappedBindings(_logger, mappedCount, descriptors.Count);
    }

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "No behavior topology descriptors found; skipping behavior HTTP route mapping.")]
    private static partial void LogNoBehaviors(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Mapped {Count} behavior HTTP transport bindings across {BehaviorCount} behaviors.")]
    private static partial void LogMappedBindings(ILogger logger, int count, int behaviorCount);
}
