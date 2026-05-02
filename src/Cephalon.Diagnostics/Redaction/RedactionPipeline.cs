namespace Cephalon.Diagnostics.Redaction;

/// <summary>
/// Composes an ordered sequence of <see cref="IRedactionFilter"/> instances into a single
/// pipeline that consumer apps and engine emission sites can apply at the engine boundary.
/// </summary>
/// <remarks>
/// <para>
/// The pipeline implements the orchestration discipline declared in <see cref="IRedactionFilter"/>'s
/// XML remarks: filters run in registration order, each filter sees the previous filter's output
/// as its input, and the final value is what the call site emits to the exporter / log sink.
/// </para>
/// <para>
/// The pipeline is itself an <see cref="IRedactionFilter"/>, so consumer apps can register a
/// composed pipeline as a single filter when that matches their DI shape (e.g. a per-emission-site
/// pipeline assembled from a global filter set plus a site-specific overlay). It is read-only
/// after construction; mutation is not supported because the engine is allowed to cache the
/// resolved pipeline per call site.
/// </para>
/// <para>
/// Engine emission sites that route values through registered <see cref="IRedactionFilter"/>
/// instances should resolve <see cref="RedactionPipeline"/> from DI rather than re-implementing
/// the pipe-through loop; doing so keeps the pipeline contract (ordering, no-mutation,
/// no-throwing) authoritative in one place.
/// </para>
/// </remarks>
public sealed class RedactionPipeline : IRedactionFilter
{
    private readonly IRedactionFilter[] filters;

    /// <summary>
    /// Creates a pipeline that applies <paramref name="filters"/> in iteration order.
    /// </summary>
    /// <param name="filters">
    /// The ordered filter sequence. The pipeline materialises the sequence at construction time;
    /// later mutations to the source collection do not affect the pipeline.
    /// </param>
    public RedactionPipeline(IEnumerable<IRedactionFilter> filters)
    {
        ArgumentNullException.ThrowIfNull(filters);

        this.filters = filters.ToArray();

        for (int i = 0; i < this.filters.Length; i++)
        {
            if (this.filters[i] is null)
            {
                throw new ArgumentException(
                    $"Filter at index {i} is null; redaction pipelines do not allow null filters.",
                    nameof(filters));
            }
        }
    }

    /// <summary>
    /// The number of filters in this pipeline. An empty pipeline returns input values unchanged.
    /// </summary>
    public int Count => filters.Length;

    /// <inheritdoc />
    public object? Filter(RedactionContext context, object? value)
    {
        var current = value;

        for (int i = 0; i < filters.Length; i++)
        {
            current = filters[i].Filter(context, current);
        }

        return current;
    }
}
