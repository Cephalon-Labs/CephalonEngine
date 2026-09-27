using MongoDB.Driver;
using MongoDB.Driver.Core.Events;
using MongoDB.Driver.Core.Servers;

namespace Cephalon.Tests.Support;

// Owns only the short-lived bootstrap connection. Application/CDC clients keep their normal settings.
internal sealed class MongoDbBootstrapClient : IDisposable
{
    private int disposed;

    public MongoDbBootstrapClient(string connectionString, Action<string> log)
    {
        var settings = MongoClientSettings.FromConnectionString(connectionString);
        // A unique cluster key prevents cleanup from disposing another client's shared cluster.
        settings.ApplicationName = $"Cephalon.Tests.Bootstrap.{Guid.NewGuid():N}";
        settings.ServerSelectionTimeout = TimeSpan.FromSeconds(3);
        settings.ConnectTimeout = TimeSpan.FromSeconds(3);
        settings.ServerMonitoringMode = ServerMonitoringMode.Poll;
        settings.HeartbeatInterval = TimeSpan.FromMilliseconds(500);
        settings.RetryReads = false;
        settings.RetryWrites = false;
        settings.ClusterConfigurator = cluster =>
        {
            cluster.Subscribe<ClusterDescriptionChangedEvent>(change => log($"cluster: {change.NewDescription}"));
            cluster.Subscribe<ConnectionOpenedEvent>(_ => log("connection opened"));
            cluster.Subscribe<CommandStartedEvent>(command => log($"command started: {command.CommandName}"));
            cluster.Subscribe<CommandSucceededEvent>(command => log($"command succeeded: {command.CommandName}"));
            cluster.Subscribe<CommandFailedEvent>(command => log($"command failed: {command.CommandName}: {command.Failure.Message}"));
            cluster.Subscribe<ClusterClosedEvent>(_ => log("cluster closed"));
        };
        Client = new MongoClient(settings);
    }

    public MongoClient Client { get; }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0) { return; }
        var cluster = Client.Cluster;
        try { Client.Dispose(); }
        finally { ClusterRegistry.Instance.UnregisterAndDisposeCluster(cluster); }
    }
}
