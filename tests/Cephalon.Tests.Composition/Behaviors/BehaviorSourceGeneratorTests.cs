using System.Collections.Immutable;
using System.Reflection;
using Cephalon.Behaviors.SourceGen;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cephalon.Tests.Behaviors;

/// <summary>
/// Exercises <see cref="BehaviorSourceGenerator"/> using the <see cref="CSharpGeneratorDriver"/> API.
/// Gate 22: generator driver tests without the verifier helper package.
/// </summary>
public sealed class BehaviorSourceGeneratorTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Minimal attribute stubs so the generator has types to resolve.</summary>
    private const string AttributeStubs = """
        namespace Cephalon.Abstractions.Behaviors
        {
            [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
            public sealed class AppBehaviorAttribute : System.Attribute
            {
                public AppBehaviorAttribute(string id) { Id = id; }
                public string Id { get; }
            }

            public interface IAppBehavior<in TIn, TOut>
            {
                System.Threading.Tasks.Task<TOut> HandleAsync(
                    TIn input,
                    IBehaviorContext context,
                    System.Threading.CancellationToken cancellationToken = default);
            }

            public interface IBehaviorContext { }

            [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
            public sealed class BehaviorAllowedTransportsAttribute : System.Attribute
            {
                public BehaviorAllowedTransportsAttribute(params string[] transports) { Transports = transports; }
                public string[] Transports { get; }
            }

            public interface IBehaviorTopologyBuilder
            {
                IBehaviorTopologyBuilder ViaHttpJsonRpc();
            }
        }
        """;

    private static (GeneratorDriverRunResult Result, ImmutableArray<Diagnostic> Diagnostics)
        RunGenerator(string source)
    {
        var compilation = CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees:
            [
                CSharpSyntaxTree.ParseText(AttributeStubs),
                CSharpSyntaxTree.ParseText(source)
            ],
            references:
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(
                    Assembly.Load("System.Runtime").Location),
                MetadataReference.CreateFromFile(
                    Assembly.Load("System.Threading.Tasks").Location),
            ],
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new BehaviorSourceGenerator();
        var driver = CSharpGeneratorDriver
            .Create(generator)
            .RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);

        var result = driver.GetRunResult();

        // Diagnostics reported via SourceProductionContext.ReportDiagnostic appear
        // in GeneratorDriverRunResult.Diagnostics (top-level aggregate).
        // We also check each per-result diagnostics and the output compilation.
        var diagnostics = result.Diagnostics
            .Concat(result.Results.SelectMany(r => r.Diagnostics))
            .Concat(outputCompilation.GetDiagnostics())
            .Where(d => d.Id.StartsWith("ABT", StringComparison.Ordinal))
            .ToImmutableArray();

        return (result, diagnostics);
    }

    private static string? GetGeneratedSource(GeneratorDriverRunResult result) =>
        result.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.EndsWith("BehaviorRegistrationHints.g.cs", StringComparison.Ordinal))
            ?.GetText()
            .ToString();

    private static string? GetGeneratedAutoRegistrationSource(GeneratorDriverRunResult result) =>
        result.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.EndsWith("BehaviorAutoRegistration.g.cs", StringComparison.Ordinal))
            ?.GetText()
            .ToString();

    // ─────────────────────────────────────────────────────────────────────────
    // Happy-path: valid behavior
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ValidBehaviorEmitsNoAbtDiagnosticsAndGeneratesHints()
    {
        const string source = """
            using Cephalon.Abstractions.Behaviors;
            using System.Threading;
            using System.Threading.Tasks;

            [AppBehavior("orders.create")]
            public sealed class CreateOrderBehavior : IAppBehavior<string, string>
            {
                public Task<string> HandleAsync(string input, IBehaviorContext ctx, CancellationToken ct = default)
                    => Task.FromResult("ok");
            }
            """;

        var (result, diagnostics) = RunGenerator(source);

        Assert.Empty(diagnostics);

        var generated = GetGeneratedSource(result);
        Assert.NotNull(generated);
        Assert.Contains("\"orders.create\"", generated);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ABT0010: must implement IAppBehavior<TIn, TOut>
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ClassWithoutIAppBehaviorEmitsAbt0010()
    {
        const string source = """
            using Cephalon.Abstractions.Behaviors;

            [AppBehavior("bad.behavior")]
            public sealed class BadBehavior
            {
            }
            """;

        var (_, diagnostics) = RunGenerator(source);

        Assert.Contains(diagnostics, d => d.Id == "ABT0010");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ABT0011: empty behavior id
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void EmptyIdEmitsAbt0011()
    {
        const string source = """
            using Cephalon.Abstractions.Behaviors;
            using System.Threading;
            using System.Threading.Tasks;

            [AppBehavior("")]
            public sealed class EmptyIdBehavior : IAppBehavior<string, string>
            {
                public Task<string> HandleAsync(string input, IBehaviorContext ctx, CancellationToken ct = default)
                    => Task.FromResult("ok");
            }
            """;

        var (_, diagnostics) = RunGenerator(source);

        Assert.Contains(diagnostics, d => d.Id == "ABT0011");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ABT0012: abstract class
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AbstractClassEmitsAbt0012()
    {
        const string source = """
            using Cephalon.Abstractions.Behaviors;
            using System.Threading;
            using System.Threading.Tasks;

            [AppBehavior("abstract.behavior")]
            public abstract class AbstractBehavior : IAppBehavior<string, string>
            {
                public abstract Task<string> HandleAsync(string input, IBehaviorContext ctx, CancellationToken ct = default);
            }
            """;

        var (_, diagnostics) = RunGenerator(source);

        Assert.Contains(diagnostics, d => d.Id == "ABT0012");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ABT0013: static class
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void StaticClassEmitsAbt0013()
    {
        const string source = """
            using Cephalon.Abstractions.Behaviors;

            [AppBehavior("static.behavior")]
            public static class StaticBehavior
            {
            }
            """;

        var (_, diagnostics) = RunGenerator(source);

        Assert.Contains(diagnostics, d => d.Id == "ABT0013");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Diagnostic descriptor metadata
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AllDescriptorsHaveHelpLinkUri()
    {
        var descriptors = new[]
        {
            BehaviorSourceGenerator.Abt010MustImplementIAppBehavior,
            BehaviorSourceGenerator.Abt011EmptyBehaviorId,
            BehaviorSourceGenerator.Abt012MustNotBeAbstract,
            BehaviorSourceGenerator.Abt013MustNotBeStatic,
            BehaviorSourceGenerator.Abt014RestMustBeModuleOwned,
        };

        foreach (var d in descriptors)
        {
            Assert.False(
                string.IsNullOrWhiteSpace(d.HelpLinkUri),
                $"{d.Id} must have a helpLinkUri");
        }
    }

    [Fact]
    public void AllDescriptorsHaveExpectedIds()
    {
        Assert.Equal("ABT0010", BehaviorSourceGenerator.Abt010MustImplementIAppBehavior.Id);
        Assert.Equal("ABT0011", BehaviorSourceGenerator.Abt011EmptyBehaviorId.Id);
        Assert.Equal("ABT0012", BehaviorSourceGenerator.Abt012MustNotBeAbstract.Id);
        Assert.Equal("ABT0013", BehaviorSourceGenerator.Abt013MustNotBeStatic.Id);
        Assert.Equal("ABT0014", BehaviorSourceGenerator.Abt014RestMustBeModuleOwned.Id);
    }

    [Fact]
    public void RestTransportAttributeEmitsAbt0014()
    {
        const string source = """
            using Cephalon.Abstractions.Behaviors;
            using System.Threading;
            using System.Threading.Tasks;

            [AppBehavior("orders.create")]
            [BehaviorAllowedTransports("http.rest")]
            public sealed class CreateOrderBehavior : IAppBehavior<string, string>
            {
                public Task<string> HandleAsync(string input, IBehaviorContext ctx, CancellationToken ct = default)
                    => Task.FromResult("ok");
            }
            """;

        var (_, diagnostics) = RunGenerator(source);

        Assert.Contains(diagnostics, d => d.Id == "ABT0014");
    }

    [Fact]
    public void RestTransportInConfigureTopologyEmitsAbt0014()
    {
        const string source = """
            using Cephalon.Abstractions.Behaviors;
            using System.Threading;
            using System.Threading.Tasks;

            public static class RestExtensions
            {
                public static IBehaviorTopologyBuilder ViaHttpRest(this IBehaviorTopologyBuilder builder)
                {
                    return builder;
                }
            }

            [AppBehavior("orders.lookup")]
            public sealed class LookupOrderBehavior : IAppBehavior<string, string>
            {
                public static void ConfigureTopology(IBehaviorTopologyBuilder builder)
                    => builder.ViaHttpRest();

                public Task<string> HandleAsync(string input, IBehaviorContext ctx, CancellationToken ct = default)
                    => Task.FromResult("ok");
            }
            """;

        var (_, diagnostics) = RunGenerator(source);

        Assert.Contains(diagnostics, d => d.Id == "ABT0014");
    }

    [Fact]
    public void NonRestConfigureTopologyStillGeneratesCompileTimeDescriptor()
    {
        const string source = """
            using Cephalon.Abstractions.Behaviors;
            using System.Threading;
            using System.Threading.Tasks;

            [AppBehavior("orders.lookup")]
            public sealed class LookupOrderBehavior : IAppBehavior<string, string>
            {
                public static void ConfigureTopology(IBehaviorTopologyBuilder builder)
                    => builder.ViaHttpJsonRpc();

                public Task<string> HandleAsync(string input, IBehaviorContext ctx, CancellationToken ct = default)
                    => Task.FromResult("ok");
            }
            """;

        var (result, diagnostics) = RunGenerator(source);

        Assert.Empty(diagnostics);

        var autoRegistration = GetGeneratedAutoRegistrationSource(result);
        Assert.NotNull(autoRegistration);
        Assert.Contains("new global::Cephalon.Abstractions.Behaviors.BehaviorTopologyDescriptor(\"orders.lookup\"", autoRegistration, StringComparison.Ordinal);
        Assert.Contains("\"http.jsonrpc\"", autoRegistration, StringComparison.Ordinal);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Multiple valid behaviors — all ids appear in generated source
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void MultipleValidBehaviorsAllIdsEmitted()
    {
        const string source = """
            using Cephalon.Abstractions.Behaviors;
            using System.Threading;
            using System.Threading.Tasks;

            [AppBehavior("orders.create")]
            public sealed class CreateOrderBehavior : IAppBehavior<string, string>
            {
                public Task<string> HandleAsync(string input, IBehaviorContext ctx, CancellationToken ct = default)
                    => Task.FromResult("ok");
            }

            [AppBehavior("orders.cancel")]
            public sealed class CancelOrderBehavior : IAppBehavior<string, string>
            {
                public Task<string> HandleAsync(string input, IBehaviorContext ctx, CancellationToken ct = default)
                    => Task.FromResult("cancelled");
            }
            """;

        var (result, diagnostics) = RunGenerator(source);

        Assert.Empty(diagnostics);

        var generated = GetGeneratedSource(result);
        Assert.NotNull(generated);
        Assert.Contains("\"orders.create\"", generated);
        Assert.Contains("\"orders.cancel\"", generated);
    }
}
