using Cephalon.Engine.Runtime;
using Microsoft.Extensions.Hosting;

namespace Cephalon.Worker.Hosting;

internal sealed class RuntimeHostedService : IHostedService
{
    private readonly IRuntime runtime;
    private readonly IServiceProvider services;

    public RuntimeHostedService(IRuntime runtime, IServiceProvider services)
    {
        this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        this.services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return runtime.StartAsync(services, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return runtime.StopAsync(cancellationToken);
    }
}
