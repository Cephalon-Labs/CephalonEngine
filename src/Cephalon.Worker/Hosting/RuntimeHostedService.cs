using System.Diagnostics;
using Cephalon.Diagnostics.Redaction;
using Cephalon.Engine.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cephalon.Worker.Hosting;

internal sealed class RuntimeHostedService : IHostedService
{
    private readonly IRuntime runtime;
    private readonly IServiceProvider services;
    private RedactionPipeline? redactionPipeline;

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
        TagLifecycleActivity(activity, WorkerDiagnostics.LifecyclePhaseStart);

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
        TagLifecycleActivity(activity, WorkerDiagnostics.LifecyclePhaseStop);

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

    private void TagLifecycleActivity(Activity? activity, string phase)
    {
        if (activity is null)
        {
            return;
        }

        SetTag(activity, WorkerDiagnostics.LifecyclePhaseTag, phase);
        SetTag(activity, WorkerDiagnostics.BlueprintTag, runtime.Manifest.AppProfile.BlueprintId);
        SetTag(activity, WorkerDiagnostics.ModuleCountTag, runtime.Modules.Count);
    }

    /// <summary>
    /// Sets a tag on <paramref name="activity"/> after routing the value through the consumer-
    /// registered <see cref="RedactionPipeline"/>. The pipeline is empty by default when no
    /// consumer registered any <see cref="IRedactionFilter"/>; in that case (and when DI did not
    /// supply a pipeline at all) this method short-circuits to passthrough so worker lifecycle
    /// emission stays cheap.
    /// </summary>
    private void SetTag(Activity activity, string attributeKey, object? value)
    {
        activity.SetTag(attributeKey, Redact(activity, attributeKey, value));
    }

    private object? Redact(Activity? activity, string attributeKey, object? value)
    {
        redactionPipeline ??= services.GetService<RedactionPipeline>();
        if (redactionPipeline is null)
        {
            return value;
        }

        var context = new RedactionContext(
            ActivitySourceName: activity?.Source.Name,
            MeterName: null,
            AttributeKey: attributeKey,
            LoggerCategory: null);
        return redactionPipeline.Filter(context, value);
    }
}
