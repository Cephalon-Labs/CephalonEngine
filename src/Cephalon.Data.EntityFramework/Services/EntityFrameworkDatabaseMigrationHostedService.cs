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

    /// <summary>
    /// Applies the configured startup migration policy for every targeted Entity Framework database role.
    /// </summary>
    /// <param name="cancellationToken">The token that cancels startup migration execution.</param>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var migrationSelection = appProfile.Databases.Migrations;
        if (migrationSelection.ApplyOnStartup != true)
        {
            return;
        }

        var databaseRoleRuntimeContributor = serviceProvider.GetService<EntityFrameworkDatabaseRoleRuntimeContributor>();
        var migrationReporter = serviceProvider.GetService<EntityFrameworkDatabaseMigrationCatalog>();
        var timeProvider = serviceProvider.GetService<TimeProvider>() ?? TimeProvider.System;

        var requestedTargets = EntityFrameworkDatabaseMigrationTargetResolver.ResolveRequestedTargets(
            migrationSelection,
            registrations.SelectMany(static registration => registration.TargetRoleIds));
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
            var selectedRoleIds = registration.TargetRoleIds
                .Where(requestedTargets.Contains)
                .ToArray();
            var startedAtUtc = timeProvider.GetUtcNow();

            try
            {
                migrationReporter?.MarkRunning(selectedRoleIds, startedAtUtc);
                databaseRoleRuntimeContributor?.ReportRunning(selectedRoleIds, registration.DbContextType);
                using var scope = serviceProvider.CreateScope();
                var dbContext = (DbContext)scope.ServiceProvider.GetRequiredService(registration.DbContextType);
                var appliedMode = await ApplySchemaAsync(dbContext, cancellationToken).ConfigureAwait(false);
                var completedAtUtc = timeProvider.GetUtcNow();
                migrationReporter?.MarkSucceeded(selectedRoleIds, appliedMode, startedAtUtc, completedAtUtc);
                databaseRoleRuntimeContributor?.ReportSucceeded(selectedRoleIds, registration.DbContextType, appliedMode);
            }
            catch (Exception exception)
            {
                var completedAtUtc = timeProvider.GetUtcNow();
                migrationReporter?.MarkFailed(selectedRoleIds, exception.Message, startedAtUtc, completedAtUtc);
                databaseRoleRuntimeContributor?.ReportFailed(selectedRoleIds, registration.DbContextType, exception);
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

    /// <summary>
    /// Stops the hosted service. Entity Framework startup migration execution is synchronous during startup, so there is no background work to drain.
    /// </summary>
    /// <param name="cancellationToken">The token that cancels shutdown.</param>
    /// <returns>A completed task.</returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private static async Task<string> ApplySchemaAsync(
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
                return "migrate";
            }
        }

        await dbContext.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
        return "ensure-created";
    }
}
