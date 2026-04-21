using Cephalon.Abstractions.Technologies;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cephalon.Engine.Technologies;

internal sealed class CellTrafficAutomationEdgeMaterializationHostedService(
    ICellTrafficAutomationRuntimeCatalog catalog,
    ICellTrafficAutomationMaterializationReportSink reportSink,
    IEnumerable<ICellTrafficAutomationEdgeMaterializer> materializers,
    ILogger<CellTrafficAutomationEdgeMaterializationHostedService> logger) : IHostedService
{
    private static readonly Action<ILogger, string, string, Exception?> LogMissingEdgeMaterializerMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Warning,
            new EventId(21011, nameof(LogMissingEdgeMaterializerMessage)),
            "Cell traffic automation '{AutomationId}' expected edge materializer '{MaterializerId}', but it was not available when startup reconciliation ran.");

    private static readonly Action<ILogger, string, string, string, Exception?> LogReconcilingEdgeMaterializationMessage =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Information,
            new EventId(21012, nameof(LogReconcilingEdgeMaterializationMessage)),
            "Reconciling edge-managed cell traffic automation '{AutomationId}' for edge nodes '{EdgeNodeIds}' through materializer '{MaterializerId}'.");

    private static readonly Action<ILogger, string, string, Exception?> LogEdgeMaterializationFailedMessage =
        LoggerMessage.Define<string, string>(
            LogLevel.Warning,
            new EventId(21013, nameof(LogEdgeMaterializationFailedMessage)),
            "Edge materializer '{MaterializerId}' failed while reconciling cell traffic automation '{AutomationId}'.");

    private readonly Dictionary<string, ICellTrafficAutomationEdgeMaterializer> materializersById =
        CreateMaterializerIndex(materializers);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var pendingAutomations = catalog.Automations
            .Where(static automation =>
                automation.EdgeNodeIds.Count > 0 &&
                !string.IsNullOrWhiteSpace(automation.EdgeMaterializerId) &&
                string.Equals(
                    automation.EdgeMaterializationState,
                    CellTrafficAutomationMaterializationStates.Pending,
                    StringComparison.OrdinalIgnoreCase))
            .ToArray();
        foreach (var automation in pendingAutomations)
        {
            if (string.IsNullOrWhiteSpace(automation.EdgeMaterializerId))
            {
                continue;
            }

            if (!materializersById.TryGetValue(automation.EdgeMaterializerId, out var materializer))
            {
                LogMissingEdgeMaterializerMessage(
                    logger,
                    automation.Id,
                    automation.EdgeMaterializerId,
                    null);
                await reportSink.ReportEdgeAsync(
                    automation.Id,
                    automation.EdgeMaterializerId,
                    new CellTrafficAutomationMaterializationResult(
                        CellTrafficAutomationMaterializationStates.Unavailable,
                        DateTimeOffset.UtcNow,
                        $"Edge materializer '{automation.EdgeMaterializerId}' was not available when startup reconciliation ran."),
                    cancellationToken).ConfigureAwait(false);
                continue;
            }

            try
            {
                LogReconcilingEdgeMaterializationMessage(
                    logger,
                    automation.Id,
                    string.Join(",", automation.EdgeNodeIds),
                    materializer.MaterializerId,
                    null);
                var result = await materializer.MaterializeAsync(automation, cancellationToken).ConfigureAwait(false);
                await reportSink.ReportEdgeAsync(
                    automation.Id,
                    materializer.MaterializerId,
                    result,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                LogEdgeMaterializationFailedMessage(
                    logger,
                    materializer.MaterializerId,
                    automation.Id,
                    exception);
                await reportSink.ReportEdgeAsync(
                    automation.Id,
                    materializer.MaterializerId,
                    new CellTrafficAutomationMaterializationResult(
                        CellTrafficAutomationMaterializationStates.Failed,
                        DateTimeOffset.UtcNow,
                        exception.Message),
                    cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static Dictionary<string, ICellTrafficAutomationEdgeMaterializer> CreateMaterializerIndex(
        IEnumerable<ICellTrafficAutomationEdgeMaterializer> materializers)
    {
        var index = new Dictionary<string, ICellTrafficAutomationEdgeMaterializer>(StringComparer.OrdinalIgnoreCase);

        foreach (var materializer in materializers)
        {
            ArgumentNullException.ThrowIfNull(materializer);

            if (string.IsNullOrWhiteSpace(materializer.MaterializerId))
            {
                throw new InvalidOperationException(
                    $"Cell traffic automation edge materializer '{materializer.GetType().FullName}' must declare a materializer id.");
            }

            var normalizedMaterializerId = materializer.MaterializerId.Trim();
            if (!index.TryAdd(normalizedMaterializerId, materializer))
            {
                throw new InvalidOperationException(
                    $"Multiple cell traffic automation edge materializers are registered with materializer id '{normalizedMaterializerId}'.");
            }
        }

        return index;
    }
}
