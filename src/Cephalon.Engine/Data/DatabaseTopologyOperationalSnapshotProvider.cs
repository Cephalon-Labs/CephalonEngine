using Cephalon.Abstractions.Data;
using Cephalon.Abstractions.Health;

namespace Cephalon.Engine.Data;

internal sealed class DatabaseTopologyOperationalSnapshotProvider(
    IDatabaseRoleCatalog databaseRoleCatalog,
    IDatabaseMigrationCatalog databaseMigrationCatalog) : IDatabaseTopologyOperationalSnapshotProvider
{
    private const string EngineDatabasesPath = "/engine/databases";
    private const string EngineDatabaseRolesPath = "/engine/database-roles";
    private const string EngineDatabaseMigrationsPath = "/engine/database-migrations";
    private const string EngineRuntimeSnapshotPath = "/engine/snapshot";

    public DatabaseTopologyOperationalSnapshot CreateSnapshot()
    {
        var generatedAtUtc = DateTimeOffset.UtcNow;
        var roles = databaseRoleCatalog.DatabaseRoles;
        var migrations = databaseMigrationCatalog.DatabaseMigrations;
        var advisories = BuildAdvisories(roles, migrations);
        var summary = BuildSummary(roles, migrations);
        var actionPlan = BuildActionPlan(generatedAtUtc, roles, migrations);

        return new DatabaseTopologyOperationalSnapshot(
            generatedAtUtc: generatedAtUtc,
            summary: summary,
            advisories: advisories,
            actionPlan: actionPlan);
    }

    private static DatabaseTopologyOperationalSummary BuildSummary(
        IReadOnlyList<DatabaseRoleDescriptor> roles,
        IReadOnlyList<DatabaseMigrationDescriptor> migrations)
    {
        ArgumentNullException.ThrowIfNull(roles);
        ArgumentNullException.ThrowIfNull(migrations);

        var healthyRoleCount = roles.Count(static role => role.HealthState == HealthState.Healthy);
        var degradedRoleCount = roles.Count(static role => role.HealthState == HealthState.Degraded);
        var unhealthyRoleCount = roles.Count(static role => role.HealthState == HealthState.Unhealthy);
        var succeededMigrationTargetCount = migrations.Count(static migration => migration.Status == DatabaseMigrationStatus.Succeeded);
        var failedMigrationTargetCount = migrations.Count(static migration => migration.Status == DatabaseMigrationStatus.Failed);
        var pendingMigrationTargetCount = migrations.Count(static migration => migration.Status != DatabaseMigrationStatus.Succeeded);
        var productionReadyMigrationTargetCount = migrations.Count(static migration =>
            migration.Commands.Any(static command => command.RecommendedForProduction));

        if (roles.Count == 0)
        {
            return new DatabaseTopologyOperationalSummary(
                status: "Ready",
                headline: "No database topology is configured",
                detail: "The current runtime does not configure any engine-owned database roles, so there is no active database topology posture to evaluate.",
                actionLabel: "Open databases",
                actionPath: EngineDatabasesPath,
                roleCount: 0,
                healthyRoleCount: 0,
                degradedRoleCount: 0,
                unhealthyRoleCount: 0,
                migrationTargetCount: migrations.Count,
                succeededMigrationTargetCount: succeededMigrationTargetCount,
                failedMigrationTargetCount: failedMigrationTargetCount,
                pendingMigrationTargetCount: pendingMigrationTargetCount,
                productionReadyMigrationTargetCount: productionReadyMigrationTargetCount);
        }

        if (unhealthyRoleCount > 0)
        {
            return new DatabaseTopologyOperationalSummary(
                status: "Blocked",
                headline: "Database topology is blocked",
                detail: "At least one resolved database role is unhealthy, so operators should recover connectivity and probe health before relying on the environment.",
                actionLabel: "Open database roles",
                actionPath: EngineDatabaseRolesPath,
                roleCount: roles.Count,
                healthyRoleCount: healthyRoleCount,
                degradedRoleCount: degradedRoleCount,
                unhealthyRoleCount: unhealthyRoleCount,
                migrationTargetCount: migrations.Count,
                succeededMigrationTargetCount: succeededMigrationTargetCount,
                failedMigrationTargetCount: failedMigrationTargetCount,
                pendingMigrationTargetCount: pendingMigrationTargetCount,
                productionReadyMigrationTargetCount: productionReadyMigrationTargetCount);
        }

        if (failedMigrationTargetCount > 0)
        {
            return new DatabaseTopologyOperationalSummary(
                status: "Blocked",
                headline: "Migration targets are blocked",
                detail: "One or more logical migration targets failed, so the topology needs intervention before it should be treated as current.",
                actionLabel: "Open migration targets",
                actionPath: EngineDatabaseMigrationsPath,
                roleCount: roles.Count,
                healthyRoleCount: healthyRoleCount,
                degradedRoleCount: degradedRoleCount,
                unhealthyRoleCount: unhealthyRoleCount,
                migrationTargetCount: migrations.Count,
                succeededMigrationTargetCount: succeededMigrationTargetCount,
                failedMigrationTargetCount: failedMigrationTargetCount,
                pendingMigrationTargetCount: pendingMigrationTargetCount,
                productionReadyMigrationTargetCount: productionReadyMigrationTargetCount);
        }

        if (degradedRoleCount > 0)
        {
            return new DatabaseTopologyOperationalSummary(
                status: "Attention",
                headline: "Database topology needs attention",
                detail: "At least one resolved database role is degraded, so operators should inspect runtime health and probe freshness before treating the topology as stable.",
                actionLabel: "Open database roles",
                actionPath: EngineDatabaseRolesPath,
                roleCount: roles.Count,
                healthyRoleCount: healthyRoleCount,
                degradedRoleCount: degradedRoleCount,
                unhealthyRoleCount: unhealthyRoleCount,
                migrationTargetCount: migrations.Count,
                succeededMigrationTargetCount: succeededMigrationTargetCount,
                failedMigrationTargetCount: failedMigrationTargetCount,
                pendingMigrationTargetCount: pendingMigrationTargetCount,
                productionReadyMigrationTargetCount: productionReadyMigrationTargetCount);
        }

        if (pendingMigrationTargetCount > 0)
        {
            return new DatabaseTopologyOperationalSummary(
                status: "Attention",
                headline: "Migration work is still pending",
                detail: "Some logical migration targets are not yet succeeded, so operators should finish the published migration path before promoting the topology.",
                actionLabel: "Open migration targets",
                actionPath: EngineDatabaseMigrationsPath,
                roleCount: roles.Count,
                healthyRoleCount: healthyRoleCount,
                degradedRoleCount: degradedRoleCount,
                unhealthyRoleCount: unhealthyRoleCount,
                migrationTargetCount: migrations.Count,
                succeededMigrationTargetCount: succeededMigrationTargetCount,
                failedMigrationTargetCount: failedMigrationTargetCount,
                pendingMigrationTargetCount: pendingMigrationTargetCount,
                productionReadyMigrationTargetCount: productionReadyMigrationTargetCount);
        }

        if (migrations.Count > 0 &&
            productionReadyMigrationTargetCount < migrations.Count)
        {
            return new DatabaseTopologyOperationalSummary(
                status: "Attention",
                headline: "Production migration guidance is incomplete",
                detail: "The topology is currently healthy, but not every migration target publishes a production-recommended bundle or script path yet.",
                actionLabel: "Open migration targets",
                actionPath: EngineDatabaseMigrationsPath,
                roleCount: roles.Count,
                healthyRoleCount: healthyRoleCount,
                degradedRoleCount: degradedRoleCount,
                unhealthyRoleCount: unhealthyRoleCount,
                migrationTargetCount: migrations.Count,
                succeededMigrationTargetCount: succeededMigrationTargetCount,
                failedMigrationTargetCount: failedMigrationTargetCount,
                pendingMigrationTargetCount: pendingMigrationTargetCount,
                productionReadyMigrationTargetCount: productionReadyMigrationTargetCount);
        }

        return new DatabaseTopologyOperationalSummary(
            status: "Ready",
            headline: "Database topology is ready",
            detail: "Resolved roles are healthy, migration targets succeeded, and the engine-owned operator guidance is complete enough to validate the topology directly from runtime introspection.",
            actionLabel: "Open runtime snapshot",
            actionPath: EngineRuntimeSnapshotPath,
            roleCount: roles.Count,
            healthyRoleCount: healthyRoleCount,
            degradedRoleCount: degradedRoleCount,
            unhealthyRoleCount: unhealthyRoleCount,
            migrationTargetCount: migrations.Count,
            succeededMigrationTargetCount: succeededMigrationTargetCount,
            failedMigrationTargetCount: failedMigrationTargetCount,
            pendingMigrationTargetCount: pendingMigrationTargetCount,
            productionReadyMigrationTargetCount: productionReadyMigrationTargetCount);
    }

    private static DatabaseTopologyOperationalAdvisory[] BuildAdvisories(
        IReadOnlyList<DatabaseRoleDescriptor> roles,
        IReadOnlyList<DatabaseMigrationDescriptor> migrations)
    {
        ArgumentNullException.ThrowIfNull(roles);
        ArgumentNullException.ThrowIfNull(migrations);

        var advisories = new List<DatabaseTopologyOperationalAdvisory>();

        var rolesNeedingAttention = roles
            .Where(role => role.HealthState is HealthState.Degraded or HealthState.Unhealthy)
            .ToArray();
        if (rolesNeedingAttention.Length > 0)
        {
            var tone = rolesNeedingAttention.Any(static role => role.HealthState == HealthState.Unhealthy)
                ? "Error"
                : "Warning";
            var roleIds = rolesNeedingAttention.Select(static role => role.Id).ToArray();

            advisories.Add(new DatabaseTopologyOperationalAdvisory(
                id: "role-health-attention",
                tone: tone,
                title: "Database role health needs attention",
                detail: $"{rolesNeedingAttention.Length} role(s) are not healthy: {string.Join(", ", roleIds)}. Review role runtime state and probe freshness before trusting the topology.",
                actionLabel: "Open database roles",
                actionPath: EngineDatabaseRolesPath,
                sourceRoleIds: roleIds));
        }

        var migrationsNeedingAttention = migrations
            .Where(static migration => migration.Status != DatabaseMigrationStatus.Succeeded)
            .ToArray();
        if (migrationsNeedingAttention.Length > 0)
        {
            var tone = migrationsNeedingAttention.Any(static migration => migration.Status == DatabaseMigrationStatus.Failed)
                ? "Error"
                : "Warning";
            var migrationIds = migrationsNeedingAttention.Select(static migration => migration.Id).ToArray();

            advisories.Add(new DatabaseTopologyOperationalAdvisory(
                id: "migration-attention",
                tone: tone,
                title: tone == "Error"
                    ? "Migration targets failed"
                    : "Migration targets still need attention",
                detail: $"{migrationsNeedingAttention.Length} migration target(s) are not yet succeeded: {string.Join(", ", migrationIds)}. Review execution mode and published guidance before promoting the topology.",
                actionLabel: "Open migration targets",
                actionPath: EngineDatabaseMigrationsPath,
                sourceMigrationIds: migrationIds));
        }

        if (migrations.Count > 0)
        {
            var productionReadyTargets = migrations
                .Where(static migration => migration.Commands.Any(static command => command.RecommendedForProduction))
                .Select(static migration => migration.Id)
                .ToArray();

            if (productionReadyTargets.Length == migrations.Count)
            {
                advisories.Add(new DatabaseTopologyOperationalAdvisory(
                    id: "migration-production-guidance",
                    tone: "Success",
                    title: "Production migration guidance published",
                    detail: $"All {migrations.Count} migration target(s) publish production-recommended bundle or script guidance in addition to the local direct-update path.",
                    actionLabel: "Open migration targets",
                    actionPath: EngineDatabaseMigrationsPath,
                    sourceMigrationIds: productionReadyTargets));
            }
            else if (productionReadyTargets.Length == 0)
            {
                advisories.Add(new DatabaseTopologyOperationalAdvisory(
                    id: "migration-production-guidance-missing",
                    tone: "Warning",
                    title: "Production migration guidance missing",
                    detail: "No migration targets currently publish production-recommended bundle or script guidance, so startup apply remains the only visible path.",
                    actionLabel: "Open migration targets",
                    actionPath: EngineDatabaseMigrationsPath));
            }
            else
            {
                advisories.Add(new DatabaseTopologyOperationalAdvisory(
                    id: "migration-production-guidance-partial",
                    tone: "Warning",
                    title: "Production migration guidance is partial",
                    detail: $"{productionReadyTargets.Length} of {migrations.Count} migration target(s) publish production-recommended commands. Review the remaining targets before treating startup apply as the only deploy-time path.",
                    actionLabel: "Open migration targets",
                    actionPath: EngineDatabaseMigrationsPath,
                    sourceMigrationIds: productionReadyTargets));
            }
        }

        if (roles.Count > 0 &&
            advisories.All(static advisory =>
                string.Equals(advisory.Tone, "Success", StringComparison.OrdinalIgnoreCase)))
        {
            advisories.Insert(0, new DatabaseTopologyOperationalAdvisory(
                id: "topology-aligned",
                tone: "Success",
                title: "Topology aligned",
                detail: "All resolved database roles are healthy and the current migration targets report success.",
                actionLabel: "Open runtime snapshot",
                actionPath: EngineRuntimeSnapshotPath,
                sourceRoleIds: roles.Select(static role => role.Id).ToArray(),
                sourceMigrationIds: migrations.Select(static migration => migration.Id).ToArray()));
        }

        return advisories.ToArray();
    }

    private static DatabaseTopologyOperationalActionPlan BuildActionPlan(
        DateTimeOffset generatedAtUtc,
        IReadOnlyList<DatabaseRoleDescriptor> roles,
        IReadOnlyList<DatabaseMigrationDescriptor> migrations)
    {
        ArgumentNullException.ThrowIfNull(roles);
        ArgumentNullException.ThrowIfNull(migrations);

        var actions = new List<DatabaseTopologyOperationalAction>();

        if (roles.Count == 0)
        {
            actions.Add(new DatabaseTopologyOperationalAction(
                id: "no-database-topology-configured",
                category: "topology-posture",
                tone: "Success",
                title: "No database topology remediation is required",
                detail: "The current runtime does not configure any engine-owned database roles, so there is no active database topology that needs operator remediation.",
                completionSignal: "No additional topology action is required unless database roles are configured later.",
                actionLabel: "Open databases",
                actionPath: EngineDatabasesPath));

            return new DatabaseTopologyOperationalActionPlan(generatedAtUtc, actions);
        }

        var unhealthyRoles = roles
            .Where(static role => role.HealthState == HealthState.Unhealthy)
            .Select(static role => role.Id)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static roleId => roleId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (unhealthyRoles.Length > 0)
        {
            actions.Add(new DatabaseTopologyOperationalAction(
                id: "restore-unhealthy-roles",
                category: "role-health",
                tone: "Error",
                title: "Restore unhealthy database roles",
                detail: $"Resolved role(s) {string.Join(", ", unhealthyRoles)} currently report Unhealthy. Recover connectivity and probe health before relying on migrations or topology validation.",
                completionSignal: "Every resolved database role reports Healthy.",
                actionLabel: "Open database roles",
                actionPath: EngineDatabaseRolesPath,
                sourceRoleIds: unhealthyRoles));
        }
        else
        {
            var degradedRoles = roles
                .Where(static role => role.HealthState == HealthState.Degraded)
                .Select(static role => role.Id)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(static roleId => roleId, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (degradedRoles.Length > 0)
            {
                actions.Add(new DatabaseTopologyOperationalAction(
                    id: "inspect-degraded-roles",
                    category: "role-health",
                    tone: "Warning",
                    title: "Inspect degraded role probes",
                    detail: $"Resolved role(s) {string.Join(", ", degradedRoles)} are degraded. Review runtime health and probe freshness before treating the environment as stable.",
                    completionSignal: "Every resolved database role reports Healthy.",
                    actionLabel: "Open database roles",
                    actionPath: EngineDatabaseRolesPath,
                    sourceRoleIds: degradedRoles));
            }
        }

        var failedMigrations = migrations
            .Where(static migration => migration.Status == DatabaseMigrationStatus.Failed)
            .Select(static migration => migration.Id)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static migrationId => migrationId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (failedMigrations.Length > 0)
        {
            actions.Add(new DatabaseTopologyOperationalAction(
                id: "repair-failed-migrations",
                category: "migration-state",
                tone: "Error",
                title: "Repair failed migration targets",
                detail: $"Migration target(s) {string.Join(", ", failedMigrations)} failed. Resolve the failing target and re-run the published guidance before promoting the topology.",
                completionSignal: "Every migration target reports Succeeded.",
                actionLabel: "Open migration targets",
                actionPath: EngineDatabaseMigrationsPath,
                sourceMigrationIds: failedMigrations));
        }
        else
        {
            var pendingMigrations = migrations
                .Where(static migration => migration.Status != DatabaseMigrationStatus.Succeeded)
                .Select(static migration => migration.Id)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(static migrationId => migrationId, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (pendingMigrations.Length > 0)
            {
                actions.Add(new DatabaseTopologyOperationalAction(
                    id: "finish-pending-migrations",
                    category: "migration-state",
                    tone: "Warning",
                    title: "Finish pending migration targets",
                    detail: $"Migration target(s) {string.Join(", ", pendingMigrations)} are not yet succeeded. Use the published guidance to bring the topology fully current.",
                    completionSignal: "Every migration target reports Succeeded.",
                    actionLabel: "Open migration targets",
                    actionPath: EngineDatabaseMigrationsPath,
                    sourceMigrationIds: pendingMigrations));
            }
        }

        var hasMigrationStateAttention = failedMigrations.Length > 0 ||
            migrations.Any(static migration => migration.Status != DatabaseMigrationStatus.Succeeded);
        if (!hasMigrationStateAttention && migrations.Count > 0)
        {
            var productionReadyTargets = migrations
                .Where(static migration => migration.Commands.Any(static command => command.RecommendedForProduction))
                .Select(static migration => migration.Id)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(static migrationId => migrationId, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (productionReadyTargets.Length < migrations.Count)
            {
                actions.Add(new DatabaseTopologyOperationalAction(
                    id: "review-manual-migration-paths",
                    category: "migration-guidance",
                    tone: "Warning",
                    title: "Review manual-only migration paths",
                    detail: $"{migrations.Count - productionReadyTargets.Length} migration target(s) still do not publish a production-ready bundle or script path. Review the published operator guidance before treating the topology as deploy-ready.",
                    completionSignal: "Every migration target publishes production-recommended guidance.",
                    actionLabel: "Open migration targets",
                    actionPath: EngineDatabaseMigrationsPath,
                    sourceMigrationIds: productionReadyTargets));
            }
        }

        if (actions.Count == 0)
        {
            actions.Add(new DatabaseTopologyOperationalAction(
                id: "topology-ready-for-validation",
                category: "topology-posture",
                tone: "Success",
                title: "Topology is ready for operator validation",
                detail: "Resolved roles are healthy, migration targets succeeded, and the engine-owned operator guidance is complete enough to use as the current topology hand-off.",
                completionSignal: "No remediation is required unless the topology or deployment state changes.",
                actionLabel: "Open runtime snapshot",
                actionPath: EngineRuntimeSnapshotPath,
                sourceRoleIds: roles.Select(static role => role.Id).ToArray(),
                sourceMigrationIds: migrations.Select(static migration => migration.Id).ToArray()));
        }

        return new DatabaseTopologyOperationalActionPlan(generatedAtUtc, actions);
    }
}
