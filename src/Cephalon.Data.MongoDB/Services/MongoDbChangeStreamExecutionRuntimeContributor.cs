using Cephalon.Abstractions.Data;
using Cephalon.Data.MongoDB.Configuration;
using Cephalon.Data.Services;

namespace Cephalon.Data.MongoDB.Services;

internal sealed class MongoDbChangeStreamExecutionRuntimeContributor(MongoDbDataOptions options)
    : ICdcCaptureExecutionRuntimeContributor
{
    public void RegisterExecutionRuntimes(ICdcCaptureExecutionRuntimeRegistry executionRuntimes)
    {
        ArgumentNullException.ThrowIfNull(executionRuntimes);

        if (options.ChangeStreamCaptures.Count == 0)
        {
            return;
        }

        executionRuntimes.Add(new CdcCaptureExecutionRuntimeDescriptor(
            id: MongoDbDataRuntimeIds.ChangeStreamExecutionRuntimeId,
            displayName: "MongoDB Change Stream Capture Pump",
            description: "Runs the provider-native MongoDB change-stream background pump that watches configured collections, stages outbox publications, and durably persists resume-token checkpoints.",
            executionOwnership: "host-managed",
            executionTopology: "provider-native",
            acknowledgementMode: "provider-native",
            hostedExecutionId: MongoDbDataRuntimeIds.ChangeStreamHostedExecutionId,
            executionGraphId: MongoDbDataRuntimeIds.ChangeStreamExecutionGraphId,
            metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["pack"] = "Cephalon.Data.MongoDB",
                ["provider"] = MongoDbDataOptions.ProviderId,
                ["surface"] = "change-stream-cdc",
                ["checkpointCollection"] = MongoDbChangeStreamCaptureHostedService.GetCheckpointCollectionName(options.CollectionPrefix)
            },
            cdcCaptureIds: options.ChangeStreamCaptures
                .Where(static capture => !string.IsNullOrWhiteSpace(capture.Id))
                .Select(static capture => capture.Id.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(static id => id, StringComparer.OrdinalIgnoreCase)
                .ToArray()));
    }
}
