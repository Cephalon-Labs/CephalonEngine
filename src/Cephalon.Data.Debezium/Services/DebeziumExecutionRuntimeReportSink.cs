using Cephalon.Abstractions.Data;

namespace Cephalon.Data.Debezium.Services;

internal sealed class DebeziumExecutionRuntimeReportSink(IServiceProvider serviceProvider)
    : ICdcCaptureExecutionRuntimeReportSink
{
    private static readonly Type? RuntimeStateCatalogType =
        typeof(Cephalon.Data.Registration.DataEngineBuilderExtensions).Assembly
            .GetType("Cephalon.Data.Services.CdcCaptureRuntimeStateCatalog", throwOnError: false, ignoreCase: false);

    public ValueTask ReportAsync(
        string executionRuntimeId,
        IReadOnlyList<CdcCaptureRuntimeObservation> observations,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executionRuntimeId);
        ArgumentNullException.ThrowIfNull(observations);

        if (RuntimeStateCatalogType is null)
        {
            throw new InvalidOperationException(
                "Cephalon.Data.Debezium could not locate the shared Cephalon.Data runtime-state catalog required for external CDC reporting.");
        }

        var runtimeStateCatalog = serviceProvider.GetService(RuntimeStateCatalogType) as ICdcCaptureExecutionRuntimeReportSink;
        if (runtimeStateCatalog is null)
        {
            throw new InvalidOperationException(
                "Cephalon.Data.Debezium requires engine.AddData(...) so the shared Cephalon.Data runtime-state catalog can accept external Debezium runtime reports.");
        }

        return runtimeStateCatalog.ReportAsync(executionRuntimeId, observations, cancellationToken);
    }
}
