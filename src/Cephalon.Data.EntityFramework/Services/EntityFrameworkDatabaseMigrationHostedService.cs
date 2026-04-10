using Cephalon.Abstractions.AppModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cephalon.Data.EntityFramework.Services;

/// <summary>
/// Applies startup schema changes for Entity Framework Core database-role targets selected through <c>Engine:Databases</c>.
/// </summary>
public sealed class EntityFrameworkDatabaseMigrationHostedService(
    IServiceProvider serviceProvider,
    AppProfile appProfile,
    IEnumerable<EntityFrameworkDatabaseMigrationRegistration> registrations) : IHostedService
{
    private readonly IServiceProvider serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly AppProfile appProfile = appProfile ?? throw new ArgumentNullException(nameof(appProfile));
    private readonly EntityFrameworkDatabaseMigrationRegistration[] registrations = registrations?.ToArray()
        ?? throw new ArgumentNullException(nameof(registrations));

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var migrationSelection = appProfile.Databases.Migrations;
        if (migrationSelection.ApplyOnStartup != true)
        {
            return;
        }

        var requestedTargets = ResolveRequestedTargets(migrationSelection, registrations);
        var supportedTargets = registrations
            .SelectMany(static registration => registration.TargetRoleIds)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var unsupportedTargets = requestedTargets
            .Where(target => !supportedTargets.Contains(target))
            .OrderBy(static target => target, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (unsupportedTargets.Length > 0)
        {
            throw new InvalidOperationException(
                $"Entity Framework migration targets [{string.Join(", ", unsupportedTargets)}] are not backed by a registered DbContext. Supported targets: [{string.Join(", ", supportedTargets.OrderBy(static target => target, StringComparer.OrdinalIgnoreCase))}].");
        }

        var selectedRegistrations = registrations
            .Where(registration => registration.TargetRoleIds.Any(requestedTargets.Contains))
            .GroupBy(static registration => registration.DbContextType)
            .Select(static group => group.First())
            .ToArray();

        foreach (var registration in selectedRegistrations)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var dbContext = (DbContext)scope.ServiceProvider.GetRequiredService(registration.DbContextType);
                await ApplySchemaAsync(dbContext, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    $"Entity Framework startup schema apply failed for DbContext '{registration.DbContextType.FullName}'.",
                    exception);
            }
        }

        if (migrationSelection.ExitAfterApply == true)
        {
            serviceProvider.GetService<IHostApplicationLifetime>()?.StopApplication();
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private static async Task ApplySchemaAsync(
        DbContext dbContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        if (dbContext.Database.IsRelational())
        {
            var migrationsAssembly = dbContext.GetService<IMigrationsAssembly>();
            if (migrationsAssembly.Migrations.Count > 0)
            {
                await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
                return;
            }
        }

        await dbContext.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
    }

    private static HashSet<string> ResolveRequestedTargets(
        DatabaseMigrationsSelection migrationSelection,
        IReadOnlyList<EntityFrameworkDatabaseMigrationRegistration> registrations)
    {
        if (migrationSelection.Targets.Count > 0)
        {
            return migrationSelection.Targets
                .Select(static target => target.Trim().ToLowerInvariant())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        return registrations
            .SelectMany(static registration => registration.TargetRoleIds)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
