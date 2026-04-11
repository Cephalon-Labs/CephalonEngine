using Cephalon.Engine.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Cephalon.Sample.Showcase.Infrastructure;

/// <summary>
/// Creates the write-side showcase DbContext for EF Core design-time tooling.
/// </summary>
public sealed class ShowcaseWriteDbContextFactory : IDesignTimeDbContextFactory<ShowcaseWriteDbContext>
{
    /// <inheritdoc />
    public ShowcaseWriteDbContext CreateDbContext(string[] args)
    {
        var configuration = ShowcaseDesignTimeDatabaseConfiguration.Build(args);
        var optionsBuilder = new DbContextOptionsBuilder<ShowcaseWriteDbContext>();
        ShowcaseDesignTimeDatabaseConfiguration.ConfigureRole(optionsBuilder, configuration, "Write");
        return new ShowcaseWriteDbContext(optionsBuilder.Options);
    }
}

/// <summary>
/// Creates the audit-history showcase DbContext for EF Core design-time tooling.
/// </summary>
public sealed class ShowcaseAuditHistoryDbContextFactory : IDesignTimeDbContextFactory<ShowcaseAuditHistoryDbContext>
{
    /// <inheritdoc />
    public ShowcaseAuditHistoryDbContext CreateDbContext(string[] args)
    {
        var configuration = ShowcaseDesignTimeDatabaseConfiguration.Build(args);
        var optionsBuilder = new DbContextOptionsBuilder<ShowcaseAuditHistoryDbContext>();
        ShowcaseDesignTimeDatabaseConfiguration.ConfigureRole(optionsBuilder, configuration, "History");
        return new ShowcaseAuditHistoryDbContext(optionsBuilder.Options);
    }
}

internal static class ShowcaseDesignTimeDatabaseConfiguration
{
    public static IConfigurationRoot Build(string[]? args)
    {
        var projectDirectory = ResolveProjectDirectory();
        var environmentName = ResolveEnvironmentName(args);

        var configurationBuilder = new ConfigurationBuilder()
            .SetBasePath(projectDirectory)
            .AddCephalonProjectConfigurations(projectDirectory, environmentName)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables("SHOWCASE_");

        return configurationBuilder.Build();
    }

    public static void ConfigureRole(
        DbContextOptionsBuilder optionsBuilder,
        IConfiguration configuration,
        string roleSectionName)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(roleSectionName);

        var normalizedRole = roleSectionName.Trim();
        var connectionString = ResolveConnectionString(configuration, normalizedRole);
        var sharedRuntime = configuration.GetSection("Engine:Databases:Runtime");
        var roleRuntime = configuration.GetSection($"Engine:Databases:{normalizedRole}:Runtime");

        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql =>
            {
                if (TryGetInt(roleRuntime, sharedRuntime, "CommandTimeoutSeconds", out var commandTimeoutSeconds))
                {
                    npgsql.CommandTimeout(commandTimeoutSeconds);
                }

                if (TryGetInt(roleRuntime, sharedRuntime, "MaxBatchSize", out var maxBatchSize))
                {
                    npgsql.MaxBatchSize(maxBatchSize);
                }

                if (TryGetBool(roleRuntime, sharedRuntime, "EnableRetryOnFailure", out var enableRetryOnFailure) &&
                    enableRetryOnFailure)
                {
                    var maxRetryCount = TryGetInt(roleRuntime, sharedRuntime, "MaxRetryCount", out var configuredRetryCount)
                        ? configuredRetryCount
                        : 6;
                    var maxRetryDelay = TryGetInt(roleRuntime, sharedRuntime, "MaxRetryDelaySeconds", out var configuredRetryDelaySeconds)
                        ? TimeSpan.FromSeconds(configuredRetryDelaySeconds)
                        : TimeSpan.FromSeconds(30);

                    npgsql.EnableRetryOnFailure(
                        maxRetryCount: maxRetryCount,
                        maxRetryDelay: maxRetryDelay,
                        errorCodesToAdd: null);
                }
            });
    }

    private static string ResolveProjectDirectory()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            var directProjectPath = Path.Combine(current.FullName, "Cephalon.Sample.Showcase.csproj");
            if (File.Exists(directProjectPath))
            {
                return current.FullName;
            }

            var nestedProjectDirectory = Path.Combine(current.FullName, "samples", "Cephalon.Sample.Showcase");
            var nestedProjectPath = Path.Combine(nestedProjectDirectory, "Cephalon.Sample.Showcase.csproj");
            if (File.Exists(nestedProjectPath))
            {
                return nestedProjectDirectory;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException(
            "Unable to resolve the Cephalon.Sample.Showcase project directory for EF Core design-time tooling.");
    }

    private static string ResolveEnvironmentName(string[]? args)
    {
        var forwardedEnvironment = TryReadForwardedEnvironment(args);
        if (!string.IsNullOrWhiteSpace(forwardedEnvironment))
        {
            return forwardedEnvironment!;
        }

        var environmentName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        return string.IsNullOrWhiteSpace(environmentName)
            ? "Development"
            : environmentName.Trim();
    }

    private static string? TryReadForwardedEnvironment(string[]? args)
    {
        if (args is null || args.Length == 0)
        {
            return null;
        }

        for (var index = 0; index < args.Length; index++)
        {
            var candidate = args[index];
            if (!string.Equals(candidate, "--environment", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(candidate, "--env", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (index + 1 < args.Length && !string.IsNullOrWhiteSpace(args[index + 1]))
            {
                return args[index + 1].Trim();
            }
        }

        return null;
    }

    private static string ResolveConnectionString(
        IConfiguration configuration,
        string roleSectionName)
    {
        var connectionStringName = configuration[$"Engine:Databases:{roleSectionName}:ConnectionStringName"];
        if (!string.IsNullOrWhiteSpace(connectionStringName))
        {
            var namedConnection = configuration.GetConnectionString(connectionStringName.Trim());
            if (!string.IsNullOrWhiteSpace(namedConnection))
            {
                return namedConnection.Trim();
            }
        }

        var inlineConnectionString = configuration[$"Engine:Databases:{roleSectionName}:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(inlineConnectionString))
        {
            return inlineConnectionString.Trim();
        }

        throw new InvalidOperationException(
            $"No connection string could be resolved for Engine:Databases:{roleSectionName}.");
    }

    private static bool TryGetBool(
        IConfiguration roleRuntime,
        IConfiguration sharedRuntime,
        string key,
        out bool value)
    {
        var raw = roleRuntime[key] ?? sharedRuntime[key];
        if (bool.TryParse(raw, out value))
        {
            return true;
        }

        value = default;
        return false;
    }

    private static bool TryGetInt(
        IConfiguration roleRuntime,
        IConfiguration sharedRuntime,
        string key,
        out int value)
    {
        var raw = roleRuntime[key] ?? sharedRuntime[key];
        if (int.TryParse(raw, out value))
        {
            return true;
        }

        value = default;
        return false;
    }
}
