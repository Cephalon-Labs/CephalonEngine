using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SfidNet.Abstractions;
using SfidNet.EntityFramework;

namespace Cephalon.Data.EntityFramework.Services;

internal sealed class SfidKeyAssigningSaveChangesInterceptor(ISfidGenerator generator) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        AssignKeys(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        AssignKeys(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void AssignKeys(DbContext? dbContext)
    {
        if (dbContext is null)
        {
            return;
        }

        dbContext.AssignSnowfakeKeys(generator);
    }
}
