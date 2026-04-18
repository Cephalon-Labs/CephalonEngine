using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace Cephalon.Benchmarks.Support;

/// <summary>
/// Uses the shared short benchmark configuration while forcing in-process execution so local
/// smoke runs do not fail when mirrored worktree artifacts contain another benchmark project file.
/// </summary>
public sealed class BenchmarkInProcessShortRunConfig : ManualConfig
{
    /// <summary>
    /// Initializes the shared in-process short-run benchmark config.
    /// </summary>
    public BenchmarkInProcessShortRunConfig()
    {
        AddJob(Job.ShortRun
            .WithToolchain(InProcessEmitToolchain.Instance)
            .WithId("InProcessShortRun"));
    }
}
