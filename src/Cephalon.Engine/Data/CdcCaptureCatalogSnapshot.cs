using Cephalon.Abstractions.Data;

namespace Cephalon.Engine.Data;

internal sealed class CdcCaptureCatalogSnapshot : ICdcCaptureCatalog
{
    private readonly IReadOnlyList<CdcCaptureDescriptor> cdcCaptures;
    private readonly Dictionary<string, CdcCaptureDescriptor> cdcCapturesById;
    private readonly Dictionary<string, IReadOnlyList<CdcCaptureDescriptor>> cdcCapturesBySourceModule;
    private readonly Dictionary<string, IReadOnlyList<CdcCaptureDescriptor>> cdcCapturesByProvider;
    private readonly Dictionary<string, IReadOnlyList<CdcCaptureDescriptor>> cdcCapturesByOutboxId;
    private readonly Dictionary<string, IReadOnlyList<CdcCaptureDescriptor>> cdcCapturesBySourceId;
    private readonly Dictionary<string, IReadOnlyList<CdcCaptureDescriptor>> cdcCapturesByResourceId;

    public CdcCaptureCatalogSnapshot(IEnumerable<CdcCaptureDescriptor> cdcCaptures)
    {
        ArgumentNullException.ThrowIfNull(cdcCaptures);

        this.cdcCaptures = cdcCaptures.ToArray();
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
}
