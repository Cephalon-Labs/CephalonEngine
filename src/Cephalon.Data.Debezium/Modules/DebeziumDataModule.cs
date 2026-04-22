using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Modules;
using Cephalon.Data.Debezium.Configuration;
using Cephalon.Data.Debezium.Services;
using Cephalon.Data.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cephalon.Data.Debezium.Modules;

internal sealed class DebeziumDataModule(DebeziumDataOptions options)
    : ModuleBase,
        ICdcCaptureContributor
{
    private static readonly ModuleDescriptor DescriptorInstance = new(
        id: "debezium-data",
        displayName: "Debezium Data",
        description: "Debezium-managed external CDC registration for Cephalon data workloads.",
        tags: ["data", "debezium", "external-managed"],
        version: "1.0.0",
        metadata: new Dictionary<string, string>
        {
            ["layer"] = "companion-pack",
            ["surface"] = "debezium-data"
        });

    /// <inheritdoc />
    public override ModuleDescriptor Descriptor => DescriptorInstance;

    /// <inheritdoc />
    public override void ConfigureServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(options);

        if (options.Connectors.Count > 0)
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<ICdcCaptureExecutionRuntimeContributor, DebeziumExecutionRuntimeContributor>());
            services.TryAddSingleton<ICdcCaptureExecutionRuntimeReportSink, DebeziumExecutionRuntimeReportSink>();
        }
    }

    /// <inheritdoc />
    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {
        ArgumentNullException.ThrowIfNull(capabilities);

        capabilities.Add(new Capability(
            key: "data.debezium",
            displayName: "Debezium Data Provider",
            description: "Registers Debezium-managed external CDC runtimes and capture descriptors for Cephalon data workloads.",
            metadata: new Dictionary<string, string>
            {
                ["pack"] = "Cephalon.Data.Debezium",
                ["provider"] = DebeziumDataOptions.ProviderId,
                ["connectorCount"] = options.Connectors.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)
            }));

        if (options.Connectors.Count > 0)
        {
            var captureCount = options.Connectors.Sum(static connector => connector.CdcCaptures.Count);
            capabilities.Add(new Capability(
                key: "data.cdc.debezium",
                displayName: "Debezium Managed CDC",
                description: "Projects Debezium-managed connector ownership, runtime reporting, and capture topology through the shared Cephalon CDC runtime surfaces.",
                metadata: new Dictionary<string, string>
                {
                    ["pack"] = "Cephalon.Data.Debezium",
                    ["provider"] = DebeziumDataOptions.ProviderId,
                    ["connectorCount"] = options.Connectors.Count.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["cdcCaptureCount"] = captureCount.ToString(System.Globalization.CultureInfo.InvariantCulture)
                }));
        }
    }

    public void RegisterCdcCaptures(ICdcCaptureRegistry cdcCaptures)
    {
        ArgumentNullException.ThrowIfNull(cdcCaptures);

        foreach (var connector in options.Connectors)
        {
            if (connector.CdcCaptures.Count == 0)
            {
                continue;
            }

            var runtimeId = NormalizeConnectorId(connector);
            var declaredTaskIds = connector.TaskIds
                .Where(static value => !string.IsNullOrWhiteSpace(value))
                .Select(static value => value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var expectedTaskCount = connector.ExpectedTaskCount ?? (declaredTaskIds.Length > 0 ? declaredTaskIds.Length : null);
            foreach (var capture in connector.CdcCaptures)
            {
                var captureId = NormalizeCaptureId(capture);
                var topicName = NormalizeOptional(capture.TopicName);
                var sourceId = string.IsNullOrWhiteSpace(capture.SourceId)
                    ? BuildSourceId(runtimeId, topicName, captureId)
                    : capture.SourceId.Trim();
                var metadata = new Dictionary<string, string>(capture.Metadata, StringComparer.OrdinalIgnoreCase)
                {
                    ["provider"] = DebeziumDataOptions.ProviderId,
                    ["connectorId"] = runtimeId,
                    ["publicationMode"] = "external-managed",
                    ["reportingMode"] = "external-runtime-reporting",
                    ["snapshotMode"] = NormalizeOptional(capture.SnapshotMode) ?? "connector-default",
                    ["executionRuntimeId"] = runtimeId,
                    ["contributorModuleId"] = Descriptor.Id,
                    ["debeziumManagementMode"] = NormalizeRequired(
                        connector.ManagementMode,
                        $"{nameof(DebeziumConnectorOptions.ManagementMode)} is required for Debezium connector '{runtimeId}'.")
                };

                AddIfPresent(metadata, "connectClusterId", connector.ConnectClusterId);
                AddIfPresent(metadata, "connectorClass", connector.ConnectorClass);
                AddIfPresent(metadata, "sourceProviderId", connector.SourceProviderId);
                AddIfPresent(metadata, "topicPrefix", connector.TopicPrefix);
                AddIfPresent(metadata, "topicName", topicName);
                AddIfPresent(metadata, "acknowledgementMode", connector.AcknowledgementMode);
                AddIfPresent(metadata, "debeziumExpectedTaskCount", expectedTaskCount?.ToString(System.Globalization.CultureInfo.InvariantCulture));

                if (declaredTaskIds.Length > 0)
                {
                    metadata["taskIds"] = string.Join(",", declaredTaskIds);
                    metadata["debeziumDeclaredTaskIds"] = string.Join(",", declaredTaskIds);
                }

                if (connector.EdgeNodeIds.Count > 0)
                {
                    metadata["edgeNodeIds"] = string.Join(",", connector.EdgeNodeIds
                        .Where(static value => !string.IsNullOrWhiteSpace(value))
                        .Select(static value => value.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase));
                }

                var resourceIds = capture.ResourceIds.Count == 0
                    ? topicName is null
                        ? Array.Empty<string>()
                        : [topicName]
                    : capture.ResourceIds
                        .Where(static value => !string.IsNullOrWhiteSpace(value))
                        .Select(static value => value.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToArray();

                cdcCaptures.Add(new CdcCaptureDescriptor(
                    id: captureId,
                    displayName: string.IsNullOrWhiteSpace(capture.DisplayName) ? captureId : capture.DisplayName.Trim(),
                    description: string.IsNullOrWhiteSpace(capture.Description)
                        ? $"Projects Debezium-managed connector '{runtimeId}' capture truth into outbox '{capture.OutboxId.Trim()}' without inventing a second CDC registry."
                        : capture.Description.Trim(),
                    sourceModuleId: NormalizeRequired(capture.SourceModuleId, $"{nameof(DebeziumCaptureOptions.SourceModuleId)} for capture '{captureId}'"),
                    provider: DebeziumDataOptions.ProviderId,
                    sourceId: sourceId,
                    outboxId: NormalizeRequired(capture.OutboxId, $"{nameof(DebeziumCaptureOptions.OutboxId)} for capture '{captureId}'"),
                    executionBinding: new CdcCaptureExecutionBindingDescriptor(
                        cdcCaptureId: captureId,
                        authoredExecutionRuntimeId: runtimeId,
                        requestedExecutionRuntimeId: runtimeId),
                    mode: NormalizeRequired(capture.Mode, $"{nameof(DebeziumCaptureOptions.Mode)} for capture '{captureId}'"),
                    eventFormat: NormalizeRequired(capture.EventFormat, $"{nameof(DebeziumCaptureOptions.EventFormat)} for capture '{captureId}'"),
                    resourceIds: resourceIds,
                    tags: capture.Tags.Count == 0
                        ? ["cdc", "debezium", "external-managed"]
                        : capture.Tags
                            .Where(static value => !string.IsNullOrWhiteSpace(value))
                            .Select(static value => value.Trim())
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .ToArray(),
                    metadata: metadata));
            }
        }
    }

    private static string NormalizeConnectorId(DebeziumConnectorOptions connector)
    {
        ArgumentNullException.ThrowIfNull(connector);
        return NormalizeRequired(connector.Id, $"{nameof(DebeziumConnectorOptions.Id)} is required for Debezium connector registration.");
    }

    private static string NormalizeCaptureId(DebeziumCaptureOptions capture)
    {
        ArgumentNullException.ThrowIfNull(capture);
        return NormalizeRequired(capture.Id, $"{nameof(DebeziumCaptureOptions.Id)} is required for Debezium capture registration.");
    }

    private static string BuildSourceId(string runtimeId, string? topicName, string captureId)
    {
        return topicName is null
            ? $"{DebeziumDataOptions.ProviderId}:{runtimeId}:{captureId}"
            : $"{DebeziumDataOptions.ProviderId}:{runtimeId}:{topicName}";
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

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
