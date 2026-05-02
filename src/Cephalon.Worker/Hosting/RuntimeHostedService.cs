using System.Diagnostics;
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

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var activity = WorkerDiagnostics.ActivitySource.StartActivity(
            WorkerDiagnostics.LifecycleStartActivityName,
            ActivityKind.Internal);

        try
        {
            await runtime.StartAsync(services, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        using var activity = WorkerDiagnostics.ActivitySource.StartActivity(
            WorkerDiagnostics.LifecycleStopActivityName,
            ActivityKind.Internal);

        try
        {
            await runtime.StopAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
            throw;
        }
    }
}
