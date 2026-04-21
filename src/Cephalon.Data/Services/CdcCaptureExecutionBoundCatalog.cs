using Cephalon.Abstractions.Data;

namespace Cephalon.Data.Services;

internal sealed class CdcCaptureExecutionBoundCatalog : ICdcCaptureCatalog
{
    private const string SharedRuntimeId = DataRuntimeIds.CdcExecutionRuntimeId;

    private readonly IReadOnlyList<CdcCaptureDescriptor> cdcCaptures;
    private readonly Dictionary<string, CdcCaptureDescriptor> cdcCapturesById;
    private readonly Dictionary<string, IReadOnlyList<CdcCaptureDescriptor>> cdcCapturesBySourceModule;
    private readonly Dictionary<string, IReadOnlyList<CdcCaptureDescriptor>> cdcCapturesByProvider;
    private readonly Dictionary<string, IReadOnlyList<CdcCaptureDescriptor>> cdcCapturesByOutboxId;
    private readonly Dictionary<string, IReadOnlyList<CdcCaptureDescriptor>> cdcCapturesBySourceId;
    private readonly Dictionary<string, IReadOnlyList<CdcCaptureDescriptor>> cdcCapturesByExecutionRuntimeId;
    private readonly Dictionary<string, IReadOnlyList<CdcCaptureDescriptor>> cdcCapturesByResourceId;

    public CdcCaptureExecutionBoundCatalog(
        IReadOnlyList<CdcCaptureDescriptor> cdcCaptures,
        CdcCaptureExecutionRuntimeDescriptorCatalog runtimeCatalog)
    {
        ArgumentNullException.ThrowIfNull(cdcCaptures);
        ArgumentNullException.ThrowIfNull(runtimeCatalog);

        var runtimes = runtimeCatalog.Runtimes;
        var runtimesById = runtimes.ToDictionary(static runtime => runtime.Id, StringComparer.OrdinalIgnoreCase);
        var claimantsByCaptureId = IndexRuntimeClaims(cdcCaptures, runtimes);

        this.cdcCaptures = cdcCaptures
            .Select(cdcCapture => cdcCapture.WithExecutionBinding(
                ResolveBinding(cdcCapture, runtimesById, claimantsByCaptureId)))
            .ToArray();
        cdcCapturesById = this.cdcCaptures.ToDictionary(static cdcCapture => cdcCapture.Id, StringComparer.OrdinalIgnoreCase);
        cdcCapturesBySourceModule = this.cdcCaptures
            .GroupBy(static cdcCapture => cdcCapture.SourceModuleId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<CdcCaptureDescriptor>)group.ToArray(),
                StringComparer.OrdinalIgnoreCase);
        cdcCapturesByProvider = this.cdcCaptures
            .GroupBy(static cdcCapture => cdcCapture.Provider, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<CdcCaptureDescriptor>)group.ToArray(),
                StringComparer.OrdinalIgnoreCase);
        cdcCapturesByOutboxId = this.cdcCaptures
            .GroupBy(static cdcCapture => cdcCapture.OutboxId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<CdcCaptureDescriptor>)group.ToArray(),
                StringComparer.OrdinalIgnoreCase);
        cdcCapturesBySourceId = this.cdcCaptures
            .GroupBy(static cdcCapture => cdcCapture.SourceId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<CdcCaptureDescriptor>)group.ToArray(),
                StringComparer.OrdinalIgnoreCase);
        cdcCapturesByExecutionRuntimeId = this.cdcCaptures
            .Where(static cdcCapture => !string.IsNullOrWhiteSpace(cdcCapture.ExecutionBinding.EffectiveExecutionRuntimeId))
            .GroupBy(static cdcCapture => cdcCapture.ExecutionBinding.EffectiveExecutionRuntimeId!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<CdcCaptureDescriptor>)group
                    .OrderBy(static cdcCapture => cdcCapture.SourceModuleId, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(static cdcCapture => cdcCapture.Provider, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(static cdcCapture => cdcCapture.Id, StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
                StringComparer.OrdinalIgnoreCase);
        cdcCapturesByResourceId = this.cdcCaptures
            .SelectMany(static cdcCapture => cdcCapture.ResourceIds.Select(resourceId => new KeyValuePair<string, CdcCaptureDescriptor>(resourceId, cdcCapture)))
            .GroupBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<CdcCaptureDescriptor>)group.Select(static pair => pair.Value).ToArray(),
                StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<CdcCaptureDescriptor> CdcCaptures => cdcCaptures;

    public CdcCaptureDescriptor? GetById(string cdcCaptureId)
    {
        if (string.IsNullOrWhiteSpace(cdcCaptureId))
        {
            return null;
        }

        return cdcCapturesById.TryGetValue(cdcCaptureId.Trim(), out var cdcCapture)
            ? cdcCapture
            : null;
    }

    public IReadOnlyList<CdcCaptureDescriptor> GetBySourceModule(string sourceModuleId)
    {
        if (string.IsNullOrWhiteSpace(sourceModuleId))
        {
            return [];
        }

        return cdcCapturesBySourceModule.TryGetValue(sourceModuleId.Trim(), out var matches)
            ? matches
            : [];
    }

    public IReadOnlyList<CdcCaptureDescriptor> GetByProvider(string provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            return [];
        }

        return cdcCapturesByProvider.TryGetValue(provider.Trim(), out var matches)
            ? matches
            : [];
    }

    public IReadOnlyList<CdcCaptureDescriptor> GetByOutboxId(string outboxId)
    {
        if (string.IsNullOrWhiteSpace(outboxId))
        {
            return [];
        }

        return cdcCapturesByOutboxId.TryGetValue(outboxId.Trim(), out var matches)
            ? matches
            : [];
    }

    public IReadOnlyList<CdcCaptureDescriptor> GetBySourceId(string sourceId)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
        {
            return [];
        }

        return cdcCapturesBySourceId.TryGetValue(sourceId.Trim(), out var matches)
            ? matches
            : [];
    }

    public IReadOnlyList<CdcCaptureDescriptor> GetByExecutionRuntimeId(string executionRuntimeId)
    {
        if (string.IsNullOrWhiteSpace(executionRuntimeId))
        {
            return [];
        }

        return cdcCapturesByExecutionRuntimeId.TryGetValue(executionRuntimeId.Trim(), out var matches)
            ? matches
            : [];
    }

    public IReadOnlyList<CdcCaptureDescriptor> GetByResourceId(string resourceId)
    {
        if (string.IsNullOrWhiteSpace(resourceId))
        {
            return [];
        }

        return cdcCapturesByResourceId.TryGetValue(resourceId.Trim(), out var matches)
            ? matches
            : [];
    }

    private static Dictionary<string, IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor>> IndexRuntimeClaims(
        IReadOnlyList<CdcCaptureDescriptor> cdcCaptures,
        IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> runtimes)
    {
        var capturesById = cdcCaptures.ToDictionary(static cdcCapture => cdcCapture.Id, StringComparer.OrdinalIgnoreCase);
        var claims = new Dictionary<string, List<CdcCaptureExecutionRuntimeDescriptor>>(StringComparer.OrdinalIgnoreCase);

        foreach (var runtime in runtimes)
        {
            foreach (var cdcCaptureId in runtime.CdcCaptureIds)
            {
                if (!capturesById.ContainsKey(cdcCaptureId))
                {
                    throw new InvalidOperationException(
                        $"CDC execution runtime '{runtime.Id}' claims capture '{cdcCaptureId}', but that capture is not active in the current runtime.");
                }

                if (!claims.TryGetValue(cdcCaptureId, out var ownedBy))
                {
                    ownedBy = [];
                    claims[cdcCaptureId] = ownedBy;
                }

                ownedBy.Add(runtime);
            }
        }

        return claims.ToDictionary(
            static pair => pair.Key,
            static pair => (IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor>)pair.Value
                .OrderBy(static runtime => runtime.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            StringComparer.OrdinalIgnoreCase);
    }

    private static CdcCaptureExecutionBindingDescriptor ResolveBinding(
        CdcCaptureDescriptor cdcCapture,
        Dictionary<string, CdcCaptureExecutionRuntimeDescriptor> runtimesById,
        Dictionary<string, IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor>> claimantsByCaptureId)
    {
        var authoredRuntimeId = Normalize(cdcCapture.ExecutionBinding.AuthoredExecutionRuntimeId);
        var requestedRuntimeId = Normalize(cdcCapture.ExecutionBinding.RequestedExecutionRuntimeId) ?? authoredRuntimeId;
        claimantsByCaptureId.TryGetValue(cdcCapture.Id, out var claimants);
        claimants ??= [];

        if (!string.IsNullOrWhiteSpace(requestedRuntimeId))
        {
            if (!runtimesById.TryGetValue(requestedRuntimeId, out var requestedRuntime))
            {
                throw new InvalidOperationException(
                    $"CDC capture '{cdcCapture.Id}' requests execution runtime '{requestedRuntimeId}', but that runtime is not active in the current composition.");
            }

            if (claimants.Count > 0 &&
                claimants.Any(runtime => !string.Equals(runtime.Id, requestedRuntimeId, StringComparison.OrdinalIgnoreCase)))
            {
                var competingRuntimeIds = string.Join(
                    ", ",
                    claimants.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.OrdinalIgnoreCase));
                throw new InvalidOperationException(
                    $"CDC capture '{cdcCapture.Id}' requests execution runtime '{requestedRuntimeId}', but competing execution runtimes also claim it: {competingRuntimeIds}.");
            }

            return CreateBinding(
                cdcCapture.Id,
                authoredRuntimeId,
                requestedRuntimeId,
                requestedRuntime,
                resolutionMode: "requested-execution-runtime");
        }

        return claimants.Count switch
        {
            0 when runtimesById.TryGetValue(SharedRuntimeId, out var sharedRuntime) => CreateBinding(
                cdcCapture.Id,
                authoredRuntimeId,
                requestedRuntimeId,
                sharedRuntime,
                resolutionMode: "default-shared-runtime"),
            0 => new CdcCaptureExecutionBindingDescriptor(
                cdcCaptureId: cdcCapture.Id,
                authoredExecutionRuntimeId: authoredRuntimeId,
                requestedExecutionRuntimeId: requestedRuntimeId,
                effectiveExecutionRuntimeId: null,
                executionOwnership: "not-configured",
                resolutionMode: "unbound"),
            1 => CreateBinding(
                cdcCapture.Id,
                authoredRuntimeId,
                requestedRuntimeId,
                claimants[0],
                resolutionMode: "runtime-claim"),
            _ => throw CreateAmbiguousOwnershipException(cdcCapture.Id, claimants)
        };
    }

    private static CdcCaptureExecutionBindingDescriptor CreateBinding(
        string cdcCaptureId,
        string? authoredRuntimeId,
        string? requestedRuntimeId,
        CdcCaptureExecutionRuntimeDescriptor runtime,
        string resolutionMode)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["executionRuntimeDisplayName"] = runtime.DisplayName,
            ["executionTopology"] = runtime.ExecutionTopology
        };

        return new CdcCaptureExecutionBindingDescriptor(
            cdcCaptureId: cdcCaptureId,
            authoredExecutionRuntimeId: authoredRuntimeId,
            requestedExecutionRuntimeId: requestedRuntimeId,
            effectiveExecutionRuntimeId: runtime.Id,
            executionOwnership: runtime.ExecutionOwnership,
            executionTopology: runtime.ExecutionTopology,
            resolutionMode: resolutionMode,
            metadata: metadata);
    }

    private static InvalidOperationException CreateAmbiguousOwnershipException(
        string cdcCaptureId,
        IReadOnlyList<CdcCaptureExecutionRuntimeDescriptor> claimants)
    {
        var runtimeIds = string.Join(
            ", ",
            claimants.Select(static runtime => runtime.Id).OrderBy(static id => id, StringComparer.OrdinalIgnoreCase));
        return new InvalidOperationException(
            $"CDC capture '{cdcCaptureId}' is claimed by multiple execution runtimes: {runtimeIds}.");
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
