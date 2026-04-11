using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cephalon.Sample.Showcase.Infrastructure;

/// <summary>
/// Seeds deterministic commerce reference data after the database topology is ready.
/// </summary>
internal sealed class ShowcaseDatabaseSeedHostedService(IServiceProvider serviceProvider) : IHostedService
{
    private readonly IServiceProvider serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var writeDb = scope.ServiceProvider.GetService<ShowcaseWriteDbContext>();
        var readDb = scope.ServiceProvider.GetService<ShowcaseReadDbContext>();

        if (writeDb is not null)
        {
            await SeedAsync(writeDb, cancellationToken).ConfigureAwait(false);
        }

        if (readDb is not null)
        {
            await SeedAsync(readDb, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private static async Task SeedAsync(
        ShowcaseCommerceDbContextBase db,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(db);

        // Hosted startup seeding can run after migrations on a cold database. Skip the write
        // path entirely once reference rows are already present so repeated host starts stay cheap.
        if (await db.Products.AnyAsync(cancellationToken).ConfigureAwait(false) &&
            await db.InventoryItems.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        ShowcaseDatabaseSeeder.SeedCommerceReferenceData(db);
    }
}
