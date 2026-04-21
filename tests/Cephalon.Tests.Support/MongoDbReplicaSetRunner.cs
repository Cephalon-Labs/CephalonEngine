using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cephalon.Tests.Support;

/// <summary>
/// Starts a disposable single-node MongoDB replica set using the EphemeralMongo runtime binaries copied into the test output.
/// </summary>
public sealed class MongoDbReplicaSetRunner : IAsyncDisposable, IDisposable
{
    private const string ReplicaSetName = "cephalon-rs0";
    private readonly Process process;
    private readonly string dataDirectory;
    private readonly string directConnectionString;
    private readonly List<string> processLogs = [];
    private readonly object processLogsGate = new();

    private MongoDbReplicaSetRunner(Process process, string dataDirectory, string directConnectionString, string connectionString)
    {
        this.process = process;
        this.dataDirectory = dataDirectory;
        this.directConnectionString = directConnectionString;
        ConnectionString = connectionString;
    }

    /// <summary>
    /// Gets the replica-set-aware connection string.
    /// </summary>
    public string ConnectionString { get; }

    /// <summary>
    /// Starts a disposable single-node replica set and waits for a writable primary.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token used while waiting for process and replica-set readiness.</param>
    /// <returns>The started runner.</returns>
    public static async Task<MongoDbReplicaSetRunner> StartAsync(CancellationToken cancellationToken = default)
    {
        var binaryPath = ResolveMongoBinaryPath();
        var port = ReserveTcpPort();
        var dataDirectory = Path.Combine(Path.GetTempPath(), "cephalon-tests-mongodb", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dataDirectory);

        var startInfo = new ProcessStartInfo
        {
            FileName = binaryPath,
            Arguments = $"--dbpath \"{dataDirectory}\" --port {port} --bind_ip 127.0.0.1 --replSet {ReplicaSetName} --quiet",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        var process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true
        };

        var directConnectionString = $"mongodb://127.0.0.1:{port}/?directConnection=true";
        var replicaSetConnectionString = $"mongodb://127.0.0.1:{port}/?replicaSet={ReplicaSetName}&directConnection=true";
        var runner = new MongoDbReplicaSetRunner(process, dataDirectory, directConnectionString, replicaSetConnectionString);

        try
        {
            runner.StartProcess();
            await runner.WaitForServerAsync(cancellationToken).ConfigureAwait(false);
            await runner.InitializeReplicaSetAsync(port, cancellationToken).ConfigureAwait(false);
            return runner;
        }
        catch
        {
            await runner.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        try
        {
            if (!process.HasExited)
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch (InvalidOperationException)
                {
                    // The process exited between the HasExited check and Kill call.
                }

                try
                {
                    await process.WaitForExitAsync().ConfigureAwait(false);
                }
                catch (InvalidOperationException)
                {
                    // The process may already be terminating.
                }
            }
        }
        finally
        {
            process.Dispose();

            try
            {
                if (Directory.Exists(dataDirectory))
                {
                    Directory.Delete(dataDirectory, recursive: true);
                }
            }
            catch (IOException)
            {
                // Best effort cleanup for temporary data directories.
            }
            catch (UnauthorizedAccessException)
            {
                // Best effort cleanup for temporary data directories.
            }
        }
    }

    private static string ResolveMongoBinaryPath()
    {
        var runtimeIdentifier = GetRuntimeIdentifier();
        var executableName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "mongod.exe" : "mongod";
        var candidate = Path.Combine(
            AppContext.BaseDirectory,
            "runtimes",
            runtimeIdentifier,
            "native",
            "mongodb",
            "bin",
            executableName);

        if (File.Exists(candidate))
        {
            return candidate;
        }

        throw new FileNotFoundException(
            $"Could not find the MongoDB runtime binary '{executableName}' under '{candidate}'.");
    }

    private static string GetRuntimeIdentifier()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "win-x64";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return "linux-x64";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return RuntimeInformation.OSArchitecture == Architecture.Arm64 ? "osx-arm64" : "osx-x64";
        }

        throw new PlatformNotSupportedException("MongoDB replica-set test support is only configured for Windows, Linux, and macOS.");
    }

    private static int ReserveTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        try
        {
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }

    private void StartProcess()
    {
        process.OutputDataReceived += (_, args) => AppendLog(args.Data);
        process.ErrorDataReceived += (_, args) => AppendLog(args.Data);

        if (!process.Start())
        {
            throw new InvalidOperationException("Failed to start the MongoDB test process.");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
    }

    private async Task WaitForServerAsync(CancellationToken cancellationToken)
    {
        var database = CreateDirectDatabase();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));

        while (!timeout.IsCancellationRequested)
        {
            ThrowIfProcessExited();

            try
            {
                await database.RunCommandAsync<BsonDocument>(
                    new BsonDocument("ping", 1),
                    cancellationToken: timeout.Token).ConfigureAwait(false);
                return;
            }
            catch (MongoConnectionException)
            {
                // The server is still starting.
            }
            catch (TimeoutException)
            {
                // Retry until the outer timeout expires.
            }

            await Task.Delay(200, timeout.Token).ConfigureAwait(false);
        }

        throw CreateBootstrapException("Timed out while waiting for the MongoDB test server to accept connections.");
    }

    private async Task InitializeReplicaSetAsync(int port, CancellationToken cancellationToken)
    {
        var database = CreateDirectDatabase();
        var configuration = new BsonDocument
        {
            ["_id"] = ReplicaSetName,
            ["members"] = new BsonArray
            {
                new BsonDocument
                {
                    ["_id"] = 0,
                    ["host"] = $"127.0.0.1:{port}"
                }
            }
        };

        try
        {
            await database.RunCommandAsync<BsonDocument>(
                new BsonDocument("replSetInitiate", configuration),
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (MongoCommandException exception) when (exception.CodeName is "AlreadyInitialized" or "InvalidReplicaSetConfig")
        {
            // The server is already configured for replica-set mode.
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));

        while (!timeout.IsCancellationRequested)
        {
            ThrowIfProcessExited();

            try
            {
                var hello = await database.RunCommandAsync<BsonDocument>(
                    new BsonDocument("hello", 1),
                    cancellationToken: timeout.Token).ConfigureAwait(false);

                if ((hello.TryGetValue("isWritablePrimary", out var writablePrimary) && writablePrimary.ToBoolean())
                    || (hello.TryGetValue("ismaster", out var isMaster) && isMaster.ToBoolean()))
                {
                    return;
                }
            }
            catch (MongoCommandException)
            {
                // The replica set may still be electing a primary.
            }
            catch (TimeoutException)
            {
                // Retry until the outer timeout expires.
            }

            await Task.Delay(200, timeout.Token).ConfigureAwait(false);
        }

        throw CreateBootstrapException("Timed out while waiting for the MongoDB replica set primary election.");
    }

    private IMongoDatabase CreateDirectDatabase()
    {
        var settings = MongoClientSettings.FromConnectionString(directConnectionString);
        settings.ServerSelectionTimeout = TimeSpan.FromSeconds(3);
        settings.ConnectTimeout = TimeSpan.FromSeconds(3);
        var client = new MongoClient(settings);
        return client.GetDatabase("admin");
    }

    private void ThrowIfProcessExited()
    {
        if (!process.HasExited)
        {
            return;
        }

        throw CreateBootstrapException($"The MongoDB test process exited unexpectedly with code {process.ExitCode}.");
    }

    private InvalidOperationException CreateBootstrapException(string message)
    {
        var builder = new StringBuilder(message);
        var logs = GetProcessLogs();
        if (logs.Length > 0)
        {
            builder.AppendLine();
            builder.AppendLine("MongoDB process output:");
            foreach (var line in logs)
            {
                builder.AppendLine(line);
            }
        }

        return new InvalidOperationException(builder.ToString());
    }

    private void AppendLog(string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        lock (processLogsGate)
        {
            processLogs.Add(line);
            if (processLogs.Count > 200)
            {
                processLogs.RemoveAt(0);
            }
        }
    }

    private string[] GetProcessLogs()
    {
        lock (processLogsGate)
        {
            return processLogs.ToArray();
        }
    }
}
