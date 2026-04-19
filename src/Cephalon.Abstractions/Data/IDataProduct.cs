namespace Cephalon.Abstractions.Data;

/// <summary>
/// Exposes a module-owned queryable data product.
/// </summary>
/// <typeparam name="T">The result shape returned by the data product.</typeparam>
public interface IDataProduct<T>
{
    /// <summary>
    /// Queries the current value of the data product.
    /// </summary>
    /// <param name="cancellationToken">The token that cancels the operation.</param>
    /// <returns>A task that completes with the current data product value.</returns>
    ValueTask<T> QueryAsync(CancellationToken cancellationToken = default);
}
