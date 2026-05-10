using Cephalon.Abstractions.Data;

namespace Cephalon.AspNetCore.Hosting;

internal sealed class EventDispatchRemediationCommandPage
{
    public EventDispatchRemediationRuntimeState[] Items { get; init; } = [];

    public int PageSize { get; init; }

    public int ReturnedCount { get; init; }

    public int TotalRetainedCount { get; init; }

    public string? ContinuationToken { get; init; }

    public string? NextContinuationToken { get; init; }

    public bool HasMore { get; init; }
}
