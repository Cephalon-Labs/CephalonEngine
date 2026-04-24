using Cephalon.Abstractions.Data;
using Cephalon.Data.Debezium.Configuration;
using Cephalon.Data.Services;

namespace Cephalon.Data.Debezium.Services;

internal sealed class DebeziumExecutionRuntimeContributor(DebeziumDataOptions options)
    : ICdcCaptureExecutionRuntimeContributor
{
    public void RegisterExecutionRuntimes(ICdcCaptureExecutionRuntimeRegistry executionRuntimes)
    {
        ArgumentNullException.ThrowIfNull(executionRuntimes);

        foreach (var connector in options.Connectors)
        {
            var captureIds = connector.CdcCaptures
                .Where(static capture => !string.IsNullOrWhiteSpace(capture.Id))
                .Select(static capture => capture.Id.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(static id => id, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (captureIds.Length == 0)
            {
                continue;
            }

            var runtimeId = NormalizeRequired(
                connector.Id,
                $"{nameof(DebeziumConnectorOptions.Id)} is required for Debezium connector registration.");
            var metadata = new Dictionary<string, string>(connector.Metadata, StringComparer.OrdinalIgnoreCase)
            {
                ["pack"] = "Cephalon.Data.Debezium",
                ["provider"] = DebeziumDataOptions.ProviderId,
                ["surface"] = "debezium-cdc",
                ["connectorId"] = runtimeId,
                ["managedConnectorProviderSpecificControlPlaneProviderId"] = DebeziumDataOptions.ProviderId,
                ["managedConnectorProviderSpecificControlPlaneMaterializerId"] = CdcCaptureExecutionRuntimeManagedConnectorExecutionAdapterIds.DebeziumKafkaConnectRest,
                ["managedConnectorProviderSpecificControlPlaneTransportKind"] = "http-rest",
                ["managedConnectorProviderSpecificControlPlaneSurfaceId"] = "debezium-kafka-connect-rest",
                ["managedConnectorProviderSpecificControlPlaneConnectorId"] = runtimeId,
                ["managedConnectorManagementMode"] = NormalizeRequired(
                    connector.ManagementMode,
                    $"{nameof(DebeziumConnectorOptions.ManagementMode)} is required for Debezium connector '{runtimeId}'."),
                ["debeziumManagementMode"] = NormalizeRequired(
                    connector.ManagementMode,
                    $"{nameof(DebeziumConnectorOptions.ManagementMode)} is required for Debezium connector '{runtimeId}'.")
            };
            var declaredTaskIds = connector.TaskIds
                .Where(static value => !string.IsNullOrWhiteSpace(value))
                .Select(static value => value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var expectedTaskCount = connector.ExpectedTaskCount ?? (declaredTaskIds.Length > 0 ? declaredTaskIds.Length : null);

            AddIfPresent(metadata, "connectClusterId", connector.ConnectClusterId);
            AddIfPresent(metadata, "connectorClass", connector.ConnectorClass);
            AddIfPresent(metadata, "sourceProviderId", connector.SourceProviderId);
            AddIfPresent(metadata, "managedConnectorDeclaredConnectClusterId", connector.ConnectClusterId);
            AddIfPresent(metadata, "managedConnectorDeclaredConnectorClass", connector.ConnectorClass);
            AddIfPresent(metadata, "managedConnectorDeclaredSourceProviderId", connector.SourceProviderId);
            AddIfPresent(metadata, "debeziumDeclaredConnectClusterId", connector.ConnectClusterId);
            AddIfPresent(metadata, "debeziumDeclaredConnectorClass", connector.ConnectorClass);
            AddIfPresent(metadata, "debeziumDeclaredSourceProviderId", connector.SourceProviderId);
            AddIfPresent(metadata, "topicPrefix", connector.TopicPrefix);
            AddIfPresent(metadata, "managedConnectorExpectedTaskCount", expectedTaskCount?.ToString(System.Globalization.CultureInfo.InvariantCulture));
            AddIfPresent(metadata, "debeziumExpectedTaskCount", expectedTaskCount?.ToString(System.Globalization.CultureInfo.InvariantCulture));

            if (declaredTaskIds.Length > 0)
            {
                metadata["taskIds"] = string.Join(",", declaredTaskIds);
                metadata["managedConnectorDeclaredTaskIds"] = string.Join(",", declaredTaskIds);
                metadata["debeziumDeclaredTaskIds"] = string.Join(",", declaredTaskIds);
            }

            executionRuntimes.Add(new CdcCaptureExecutionRuntimeDescriptor(
                id: runtimeId,
                displayName: string.IsNullOrWhiteSpace(connector.DisplayName) ? runtimeId : connector.DisplayName.Trim(),
                description: string.IsNullOrWhiteSpace(connector.Description)
                    ? $"Represents Debezium-managed connector '{runtimeId}' and projects its external runtime observations into the shared Cephalon CDC runtime catalog."
                    : connector.Description.Trim(),
                executionOwnership: NormalizeRequired(
                    connector.ExecutionOwnership,
                    $"{nameof(DebeziumConnectorOptions.ExecutionOwnership)} is required for Debezium connector '{runtimeId}'."),
                executionTopology: NormalizeRequired(
                    connector.ExecutionTopology,
                    $"{nameof(DebeziumConnectorOptions.ExecutionTopology)} is required for Debezium connector '{runtimeId}'."),
                acknowledgementMode: NormalizeRequired(
                    connector.AcknowledgementMode,
                    $"{nameof(DebeziumConnectorOptions.AcknowledgementMode)} is required for Debezium connector '{runtimeId}'."),
                observationStaleAfterSeconds: connector.ObservationStaleAfterSeconds,
                rejectOutOfOrderReports: connector.RejectOutOfOrderReports,
                metadata: metadata,
                cdcCaptureIds: captureIds,
                reporterLeaseSeconds: connector.ReporterLeaseSeconds,
                rejectConflictingReporterIds: connector.RejectConflictingReporterIds,
                edgeNodeIds: connector.EdgeNodeIds
                    .Where(static value => !string.IsNullOrWhiteSpace(value))
                    .Select(static value => value.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
                    .ToArray()));
        }
    }

    private static void AddIfPresent(Dictionary<string, string> metadata, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            metadata[key] = value.Trim();
        }
    }

    private static string NormalizeRequired(string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(message);
        }

        return value.Trim();
    }
}
