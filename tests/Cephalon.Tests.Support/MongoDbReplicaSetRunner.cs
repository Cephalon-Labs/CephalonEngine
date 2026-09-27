using System.Diagnostics;
using System.Globalization;
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
    private readonly List<string> processLogs = [];
    private readonly List<string> driverLogs = [];
    private readonly object processLogsGate = new();
    private readonly Stopwatch bootstrapElapsed = Stopwatch.StartNew();

    private MongoDbReplicaSetRunner(Process process, string dataDirectory, string connectionString)
    {
        this.process = process;
        this.dataDirectory = dataDirectory;
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
        cancellationToken.ThrowIfCancellationRequested();
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
        var runner = new MongoDbReplicaSetRunner(process, dataDirectory, replicaSetConnectionString);

        try
        {
            runner.StartProcess();
            using var bootstrap = new MongoDbBootstrapClient(directConnectionString, runner.AppendDriverLog);
            await runner.WaitForServerAsync(bootstrap.Client.GetDatabase("admin"), cancellationToken).ConfigureAwait(false);
            await runner.InitializeReplicaSetAsync(bootstrap.Client, port, cancellationToken).ConfigureAwait(false);
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

    private async Task WaitForServerAsync(IMongoDatabase database, CancellationToken cancellationToken)
    {
        Exception? lastFailure = null;
        await RunBootstrapPhaseAsync(async token =>
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();
                ThrowIfProcessExited();
                try
                {
                    await database.RunCommandAsync<BsonDocument>(
                        new BsonDocument("ping", 1), cancellationToken: token).ConfigureAwait(false);
                    return;
                }
                catch (Exception exception) when (exception is MongoConnectionException or TimeoutException)
                {
                    lastFailure = exception;
                }

                await Task.Delay(200, token).ConfigureAwait(false);
            }
        }, TimeSpan.FromSeconds(20),
            exception => CreateBootstrapException(
                "Timed out while waiting for the MongoDB test server to accept connections.",
                lastFailure ?? exception), cancellationToken).ConfigureAwait(false);
    }

    private async Task InitializeReplicaSetAsync(MongoClient client, int port, CancellationToken cancellationToken)
    {
        var database = client.GetDatabase("admin");
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
            await RunBootstrapPhaseAsync(
                token => database.RunCommandAsync<BsonDocument>(
                    new BsonDocument("replSetInitiate", configuration), cancellationToken: token),
                TimeSpan.FromSeconds(20),
                exception => CreateBootstrapException(
                    "Timed out while initializing the MongoDB test replica set.", exception),
                cancellationToken).ConfigureAwait(false);
        }
        catch (MongoCommandException exception) when (exception.CodeName is "AlreadyInitialized" or "InvalidReplicaSetConfig")
        {
            // The server is already configured for replica-set mode.
        }

        Exception? lastFailure = null;
        await RunBootstrapPhaseAsync(async token =>
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();
                ThrowIfProcessExited();
                try
                {
                    var hello = await database.RunCommandAsync<BsonDocument>(
                        new BsonDocument("hello", 1), cancellationToken: token).ConfigureAwait(false);

                    if ((hello.TryGetValue("isWritablePrimary", out var writablePrimary) && writablePrimary.ToBoolean())
                        || (hello.TryGetValue("ismaster", out var isMaster) && isMaster.ToBoolean()))
                    {
                        return;
                    }
                }
                catch (Exception exception) when (exception is MongoCommandException or TimeoutException)
                {
                    lastFailure = exception;
                }

                await Task.Delay(200, token).ConfigureAwait(false);
            }
        }, TimeSpan.FromSeconds(20),
            exception => CreateBootstrapException(
                "Timed out while waiting for the MongoDB replica set primary election.",
                lastFailure ?? exception), cancellationToken).ConfigureAwait(false);
    }

    internal static async Task RunBootstrapPhaseAsync(
        Func<CancellationToken, Task> operation,
        TimeSpan timeout,
        Func<OperationCanceledException, Exception> createTimeoutException,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(timeout);
        try
        {
            await operation(deadline.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception)
            when (deadline.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            // Both driver commands and the retry delay can expire the deadline. Preserve
            // process/driver diagnostics in either case, while caller cancellation stays cancellation.
            throw createTimeoutException(exception);
        }
    }

    private void ThrowIfProcessExited()
    {
        if (!process.HasExited)
        {
            return;
        }

        throw CreateBootstrapException($"The MongoDB test process exited unexpectedly with code {process.ExitCode}.");
    }

    private InvalidOperationException CreateBootstrapException(string message, Exception? innerException = null)
    {
        var builder = new StringBuilder(message);
        builder.AppendLine();
        builder.AppendLine(CultureInfo.InvariantCulture, $"Bootstrap elapsed: {bootstrapElapsed.Elapsed}; managed threads: {ThreadPool.ThreadCount}; queued work: {ThreadPool.PendingWorkItemCount}.");
        lock (processLogsGate)
        {
            builder.AppendLine("MongoDB bootstrap driver observations:");
            foreach (var line in driverLogs) { builder.AppendLine(line); }
        }
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

        return new InvalidOperationException(builder.ToString(), innerException);
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

    private void AppendDriverLog(string line)
    {
        lock (processLogsGate)
        {
            driverLogs.Add($"{bootstrapElapsed.Elapsed}: {line}");
            if (driverLogs.Count > 100) { driverLogs.RemoveAt(0); }
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
