using Cephalon.Abstractions.Technologies;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cephalon.Engine.Technologies;

internal sealed class CellTrafficAutomationProviderMaterializationHostedService(
    CellTrafficAutomationRuntimeCatalogSnapshot catalog,
    IEnumerable<ICellTrafficAutomationProviderMaterializer> materializers,
    ILogger<CellTrafficAutomationProviderMaterializationHostedService> logger) : IHostedService
{
    private static readonly Action<ILogger, string, string, Exception?> LogMissingProviderMaterializerMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Warning,
            new EventId(21001, nameof(LogMissingProviderMaterializerMessage)),
            "Cell traffic automation '{AutomationId}' expected provider materializer '{MaterializerId}', but it was not available when startup reconciliation ran.");
    private static readonly Action<ILogger, string, string?, string, Exception?> LogProviderMaterializationStartMessage =
        LoggerMessage.Define<string, string?, string>(
            LogLevel.Information,
            new EventId(21002, nameof(LogProviderMaterializationStartMessage)),
            "Reconciling provider-managed cell traffic automation '{AutomationId}' for provider '{ProviderId}' through materializer '{MaterializerId}'.");
    private static readonly Action<ILogger, string, string, Exception?> LogProviderMaterializationFailureMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Error,
            new EventId(21003, nameof(LogProviderMaterializationFailureMessage)),
            "Provider materializer '{MaterializerId}' failed while reconciling cell traffic automation '{AutomationId}'.");

    private readonly Dictionary<string, ICellTrafficAutomationProviderMaterializer> materializersById =
        CreateMaterializerIndex(materializers);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var pendingAutomations = catalog.GetPendingProviderMaterializations();
        if (pendingAutomations.Count == 0)
        {
            return;
        }

        foreach (var automation in pendingAutomations)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(automation.ProviderMaterializerId))
            {
                continue;
            }

            if (!materializersById.TryGetValue(automation.ProviderMaterializerId, out var materializer))
            {
                LogMissingProviderMaterializerMessage(
                    logger,
                    automation.Id,
                    automation.ProviderMaterializerId,
                    null);
                catalog.ReportProviderMaterialization(
                    automation.Id,
                    automation.ProviderMaterializerId,
                    new CellTrafficAutomationProviderMaterializationResult(
                        CellTrafficAutomationProviderMaterializationStates.Unavailable,
                        DateTimeOffset.UtcNow,
                        $"Provider materializer '{automation.ProviderMaterializerId}' was not available when startup reconciliation ran."));
                continue;
            }

            try
            {
                LogProviderMaterializationStartMessage(
                    logger,
                    automation.Id,
                    automation.ProviderId,
                    materializer.MaterializerId,
                    null);

                var result = await materializer.MaterializeAsync(automation, cancellationToken).ConfigureAwait(false);
                catalog.ReportProviderMaterialization(automation.Id, materializer.MaterializerId, result);
            }
            catch (Exception exception)
            {
                LogProviderMaterializationFailureMessage(
                    logger,
                    materializer.MaterializerId,
                    automation.Id,
                    exception);

                catalog.ReportProviderMaterialization(
                    automation.Id,
                    materializer.MaterializerId,
                    new CellTrafficAutomationProviderMaterializationResult(
                        CellTrafficAutomationProviderMaterializationStates.Failed,
                        DateTimeOffset.UtcNow,
                        exception.Message));
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private static Dictionary<string, ICellTrafficAutomationProviderMaterializer> CreateMaterializerIndex(
        IEnumerable<ICellTrafficAutomationProviderMaterializer> materializers)
    {
        var index = new Dictionary<string, ICellTrafficAutomationProviderMaterializer>(StringComparer.OrdinalIgnoreCase);

        foreach (var materializer in materializers)
        {
            ArgumentNullException.ThrowIfNull(materializer);

            if (string.IsNullOrWhiteSpace(materializer.MaterializerId))
            {
                throw new InvalidOperationException(
                    $"Cell traffic automation provider materializer '{materializer.GetType().FullName}' must declare a materializer id.");
            }

            var normalizedMaterializerId = materializer.MaterializerId.Trim();
            if (!index.TryAdd(normalizedMaterializerId, materializer))
            {
                throw new InvalidOperationException(
                    $"Multiple cell traffic automation provider materializers are registered with materializer id '{normalizedMaterializerId}'.");
            }
        }

        return index;
    }
}
