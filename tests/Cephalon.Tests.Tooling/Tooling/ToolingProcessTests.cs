using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Cephalon.Tests.Tooling;

[Collection(ToolingProcessCollectionDefinition.Name)]
public sealed class ToolingProcessTests
{
    [Fact]
    public void DrainsBothPipesWithoutDeadlocking()
    {
        var result = RunScript("for ($i=0; $i -lt 4096; $i++) { [Console]::Out.WriteLine('output-' + $i); [Console]::Error.WriteLine('error-' + $i) }");
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("output-4095", result.Output, StringComparison.Ordinal);
        Assert.Contains("error-4095", result.Error, StringComparison.Ordinal);
    }

    [Fact]
    public void PreservesNonzeroExitAndDiagnostics()
    {
        var result = RunScript("[Console]::Out.WriteLine('before-exit'); [Console]::Error.WriteLine('expected-error'); exit 17");
        Assert.Equal(17, result.ExitCode);
        Assert.Contains("before-exit", result.Output, StringComparison.Ordinal);
        Assert.Contains("expected-error", result.Error, StringComparison.Ordinal);
    }

    [Fact]
    public void TerminatesLiveProcessOnTimeoutAndRetainsPartialOutput()
    {
        var stopwatch = Stopwatch.StartNew();
        var result = RunScript("[Console]::Out.WriteLine($PID); Start-Sleep -Seconds 30", TimeSpan.FromSeconds(2));
        Assert.Equal(-1, result.ExitCode);
        Assert.Contains("timed out", result.Error, StringComparison.Ordinal);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(8));
        var id = int.Parse(result.Output.Trim(), CultureInfo.InvariantCulture);
        Assert.Throws<ArgumentException>(() => Process.GetProcessById(id));
    }

    [Fact]
    public void BoundsPipeDrainAfterRootHasExited()
    {
        var childScript = Convert.ToBase64String(Encoding.Unicode.GetBytes("Start-Sleep -Seconds 30"));
        var script = $$"""
            $start = [Diagnostics.ProcessStartInfo]::new('pwsh', '-NoProfile -EncodedCommand {{childScript}}')
            $start.UseShellExecute = $false
            $start.CreateNoWindow = $true
            $child = [Diagnostics.Process]::Start($start)
            [Console]::Out.WriteLine($child.Id)
            exit 0
            """;
        var stopwatch = Stopwatch.StartNew();
        var result = RunScript(script, TimeSpan.FromSeconds(2));
        var id = int.Parse(result.Output.Trim(), CultureInfo.InvariantCulture);
        try
        {
            Assert.Equal(-1, result.ExitCode);
            Assert.Contains("redirected output timed out", result.Error, StringComparison.Ordinal);
            Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(8));
        }
        finally
        {
            using var child = Process.GetProcessById(id);
            child.Kill(entireProcessTree: true);
            Assert.True(child.WaitForExit(5000));
        }
    }

    private static ToolingProcessResult RunScript(string script, TimeSpan? timeout = null)
        => ToolingProcess.Run("pwsh", "-NoProfile -EncodedCommand "
            + Convert.ToBase64String(Encoding.Unicode.GetBytes(script)),
            Path.GetTempPath(), timeout ?? TimeSpan.FromSeconds(30));
}
