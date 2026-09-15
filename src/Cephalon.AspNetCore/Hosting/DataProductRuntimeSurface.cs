using Cephalon.Abstractions.Data;

namespace Cephalon.AspNetCore.Hosting;

/// <summary>
/// Describes the operator-facing runtime projection for data product descriptors.
/// </summary>
/// <param name="DataProducts">The active data product descriptors visible to the current runtime.</param>
/// <param name="EvaluatedAtUtc">The UTC timestamp when the runtime payload was evaluated.</param>
/// <param name="EvaluationDurationMilliseconds">The time in milliseconds spent evaluating the runtime payload.</param>
public sealed record DataProductRuntimeSurface(
    IReadOnlyList<DataProductDescriptor> DataProducts,
    DateTimeOffset EvaluatedAtUtc = default,
    int EvaluationDurationMilliseconds = 0);