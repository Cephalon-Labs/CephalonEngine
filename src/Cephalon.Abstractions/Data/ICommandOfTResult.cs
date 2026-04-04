namespace Cephalon.Abstractions.Data;

/// <summary>
/// Marks a write-side request that returns a value when it completes.
/// </summary>
/// <typeparam name="TResult">The result type returned by the command.</typeparam>
public interface ICommand<TResult> : ICommand
{
}
