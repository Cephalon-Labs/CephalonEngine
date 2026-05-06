using Cephalon.Data.MySql.Configuration;
using Cephalon.Data.MySql.SciSharpReplication.Services;
using Cephalon.Data.MySql.Services;
using Cephalon.Engine.Composition;
using Cephalon.Engine.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cephalon.Data.MySql.SciSharpReplication.Registration;

/// <summary>
/// Registers the SciSharp-backed MySQL binlog transport adapter for <see cref="EngineBuilder" />.
/// </summary>
/// <remarks>
/// <para>
/// This package intentionally isolates the current <c>SciSharp.MySQL.Replication</c> adapter path from
/// the core MySQL data pack so trim, AOT, and single-file governance can reason about the risk at
/// package granularity.
/// </para>
/// </remarks>
public static class MySqlSciSharpReplicationEngineBuilderExtensions
{
    /// <summary>
    /// Adds the SciSharp-backed MySQL binlog transport used by configured MySQL CDC captures.
    /// </summary>
    /// <param name="builder">The engine builder to extend.</param>
    /// <returns>The same engine builder for fluent composition.</returns>
    public static EngineBuilder AddSciSharpMySqlBinlogReplication(this EngineBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddSingleton<IMySqlBinlogTransport>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<MySqlDataOptions>();
            var configuration = serviceProvider.GetService<IConfiguration>();
            var connectionString = ResolveConnectionString(configuration, options);
            var logger = serviceProvider.GetRequiredService<ILogger<SciSharpMySqlBinlogTransport>>();

            return new SciSharpMySqlBinlogTransport(connectionString, options, logger);
        });

        return builder;
    }

    private static string ResolveConnectionString(IConfiguration? configuration, MySqlDataOptions options)
    {
        var connectionString = ConnectionStringResolution.Resolve(
            configuration,
            options.ConnectionString,
            options.ConnectionStringName,
            string.Empty,
            MySqlDataOptions.SectionPath,
            "MySQL");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"{MySqlDataOptions.SectionPath} must configure either ConnectionStringName or ConnectionString before MySQL binlog CDC can start.");
        }

        return connectionString.Trim();
    }
}
