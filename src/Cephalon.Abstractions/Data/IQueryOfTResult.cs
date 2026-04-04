namespace Cephalon.Abstractions.Data;

/// <summary>
/// Marks a request that should execute on the read side of a Cephalon application.
/// </summary>
/// <typeparam name="TResult">The result type returned by the query.</typeparam>
public interface IQuery<TResult>
{
}
