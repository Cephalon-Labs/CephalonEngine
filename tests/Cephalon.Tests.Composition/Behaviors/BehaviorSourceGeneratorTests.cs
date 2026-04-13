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

        namespace Cephalon.Behaviors.Http.Abstractions
        {
            public enum BehaviorRestBindingSource
            {
                Unspecified = 0,
                Route = 1,
                Query = 2,
                Header = 3,
                Body = 4
            }

            public enum BehaviorRestMethod
            {
                Unspecified = 0,
                Get = 1,
                Post = 2,
                Put = 3,
                Patch = 4,
                Delete = 5
            }

            [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
            public sealed class BehaviorRestProfileAttribute : System.Attribute
            {
                public BehaviorRestProfileAttribute(BehaviorRestMethod method, string relativePattern)
                {
                    Method = method;
                    RelativePattern = relativePattern;
                }

                public BehaviorRestMethod Method { get; }
                public string RelativePattern { get; }
                public int ApiVersionMajor { get; set; }
            }

            [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
            public sealed class BehaviorRestBindingAttribute : System.Attribute
            {
                public BehaviorRestBindingAttribute(string propertyName, BehaviorRestBindingSource source)
                {
                    PropertyName = propertyName;
                    Source = source;
                }

                public string PropertyName { get; }
                public BehaviorRestBindingSource Source { get; }
                public string? Name { get; set; }
            }

            public sealed class BehaviorRestBindingDescriptor
            {
                public BehaviorRestBindingDescriptor(string propertyName, BehaviorRestBindingSource source, string? name = null)
                {
                    PropertyName = propertyName;
                    Source = source;
                    Name = name;
                }

                public string PropertyName { get; }
                public BehaviorRestBindingSource Source { get; }
                public string? Name { get; }
            }

            public sealed class BehaviorRestProfileDescriptor
            {
                public BehaviorRestProfileDescriptor(
                    string behaviorId,
                    BehaviorRestMethod method,
                    string relativePattern,
                    int? apiVersionMajor,
                    System.Collections.Generic.IReadOnlyList<BehaviorRestBindingDescriptor>? bindings = null)
                {
                    BehaviorId = behaviorId;
                    Method = method;
                    RelativePattern = relativePattern;
                    ApiVersionMajor = apiVersionMajor;
                    Bindings = bindings;
                }

                public string BehaviorId { get; }
                public BehaviorRestMethod Method { get; }
                public string RelativePattern { get; }
                public int? ApiVersionMajor { get; }
                public System.Collections.Generic.IReadOnlyList<BehaviorRestBindingDescriptor>? Bindings { get; }
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

    [Fact]
    public void ValidBehaviorWithRestProfileEmitsNoAbtDiagnosticsAndGeneratesRestProfileHints()
    {
        const string source = """
            using Cephalon.Abstractions.Behaviors;
            using Cephalon.Behaviors.Http.Abstractions;
            using System.Threading;
            using System.Threading.Tasks;

            [AppBehavior("orders.get")]
            [BehaviorRestProfile(BehaviorRestMethod.Get, "/{orderId}", ApiVersionMajor = 2)]
            public sealed class GetOrderBehavior : IAppBehavior<string, string>
            {
                public Task<string> HandleAsync(string input, IBehaviorContext ctx, CancellationToken ct = default)
                    => Task.FromResult("ok");
            }
            """;

        var (result, diagnostics) = RunGenerator(source);

        Assert.Empty(diagnostics);

        var autoRegistration = GetGeneratedAutoRegistrationSource(result);
        Assert.NotNull(autoRegistration);
        Assert.Contains("GetRestProfiles()", autoRegistration, StringComparison.Ordinal);
        Assert.Contains("new global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestProfileDescriptor(\"orders.get\"", autoRegistration, StringComparison.Ordinal);
        Assert.Contains("BehaviorRestMethod.Get", autoRegistration, StringComparison.Ordinal);
        Assert.Contains("\"/{orderId}\"", autoRegistration, StringComparison.Ordinal);
        Assert.Contains(", 2)", autoRegistration, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidBehaviorWithRestProfileBindingsGeneratesBindingHints()
    {
        const string source = """
            using Cephalon.Abstractions.Behaviors;
            using Cephalon.Behaviors.Http.Abstractions;
            using System.Threading;
            using System.Threading.Tasks;

            public sealed record CreateOrderInput(string OrderId, int Quantity, string? CorrelationId, string? Note);

            [AppBehavior("orders.create")]
            [BehaviorRestProfile(BehaviorRestMethod.Post, "/{orderId}", ApiVersionMajor = 6)]
            [BehaviorRestBinding(nameof(CreateOrderInput.OrderId), BehaviorRestBindingSource.Route, Name = "orderId")]
            [BehaviorRestBinding(nameof(CreateOrderInput.Quantity), BehaviorRestBindingSource.Query, Name = "quantity")]
            [BehaviorRestBinding(nameof(CreateOrderInput.CorrelationId), BehaviorRestBindingSource.Header, Name = "X-Correlation-Id")]
            [BehaviorRestBinding(nameof(CreateOrderInput.Note), BehaviorRestBindingSource.Body, Name = "note")]
            public sealed class CreateOrderBehavior : IAppBehavior<CreateOrderInput, string>
            {
                public Task<string> HandleAsync(CreateOrderInput input, IBehaviorContext ctx, CancellationToken ct = default)
                    => Task.FromResult("ok");
            }
            """;

        var (result, diagnostics) = RunGenerator(source);

        Assert.Empty(diagnostics);

        var autoRegistration = GetGeneratedAutoRegistrationSource(result);
        Assert.NotNull(autoRegistration);
        Assert.Contains("BehaviorRestBindingDescriptor", autoRegistration, StringComparison.Ordinal);
        Assert.Contains("\"OrderId\"", autoRegistration, StringComparison.Ordinal);
        Assert.Contains("BehaviorRestBindingSource.Route", autoRegistration, StringComparison.Ordinal);
        Assert.Contains("\"X-Correlation-Id\"", autoRegistration, StringComparison.Ordinal);
    }

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
            BehaviorSourceGenerator.Abt015RestProfileMethodMustBeSpecified,
            BehaviorSourceGenerator.Abt016RestProfilePatternMustNotBeEmpty,
            BehaviorSourceGenerator.Abt017RestProfileVersionMustBePositive,
            BehaviorSourceGenerator.Abt018RestProfilePatternMustStartWithSlash,
        };

        foreach (var descriptor in descriptors)
        {
            Assert.False(
                string.IsNullOrWhiteSpace(descriptor.HelpLinkUri),
                $"{descriptor.Id} must have a helpLinkUri");
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
        Assert.Equal("ABT0015", BehaviorSourceGenerator.Abt015RestProfileMethodMustBeSpecified.Id);
        Assert.Equal("ABT0016", BehaviorSourceGenerator.Abt016RestProfilePatternMustNotBeEmpty.Id);
        Assert.Equal("ABT0017", BehaviorSourceGenerator.Abt017RestProfileVersionMustBePositive.Id);
        Assert.Equal("ABT0018", BehaviorSourceGenerator.Abt018RestProfilePatternMustStartWithSlash.Id);
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
    public void RestProfileWithoutMethodEmitsAbt0015()
    {
        const string source = """
            using Cephalon.Abstractions.Behaviors;
            using Cephalon.Behaviors.Http.Abstractions;
            using System.Threading;
            using System.Threading.Tasks;

            [AppBehavior("orders.get")]
            [BehaviorRestProfile((BehaviorRestMethod)0, "/{orderId}")]
            public sealed class GetOrderBehavior : IAppBehavior<string, string>
            {
                public Task<string> HandleAsync(string input, IBehaviorContext ctx, CancellationToken ct = default)
                    => Task.FromResult("ok");
            }
            """;

        var (_, diagnostics) = RunGenerator(source);

        Assert.Contains(diagnostics, d => d.Id == "ABT0015");
    }

    [Fact]
    public void RestProfileWithEmptyPatternEmitsAbt0016()
    {
        const string source = """
            using Cephalon.Abstractions.Behaviors;
            using Cephalon.Behaviors.Http.Abstractions;
            using System.Threading;
            using System.Threading.Tasks;

            [AppBehavior("orders.get")]
            [BehaviorRestProfile(BehaviorRestMethod.Get, "")]
            public sealed class GetOrderBehavior : IAppBehavior<string, string>
            {
                public Task<string> HandleAsync(string input, IBehaviorContext ctx, CancellationToken ct = default)
                    => Task.FromResult("ok");
            }
            """;

        var (_, diagnostics) = RunGenerator(source);

        Assert.Contains(diagnostics, d => d.Id == "ABT0016");
    }

    [Fact]
    public void RestProfileWithNonPositiveVersionEmitsAbt0017()
    {
        const string source = """
            using Cephalon.Abstractions.Behaviors;
            using Cephalon.Behaviors.Http.Abstractions;
            using System.Threading;
            using System.Threading.Tasks;

            [AppBehavior("orders.get")]
            [BehaviorRestProfile(BehaviorRestMethod.Get, "/{orderId}", ApiVersionMajor = 0)]
            public sealed class GetOrderBehavior : IAppBehavior<string, string>
            {
                public Task<string> HandleAsync(string input, IBehaviorContext ctx, CancellationToken ct = default)
                    => Task.FromResult("ok");
            }
            """;

        var (_, diagnostics) = RunGenerator(source);

        Assert.Contains(diagnostics, d => d.Id == "ABT0017");
    }

    [Fact]
    public void RestProfileWithoutLeadingSlashEmitsAbt0018()
    {
        const string source = """
            using Cephalon.Abstractions.Behaviors;
            using Cephalon.Behaviors.Http.Abstractions;
            using System.Threading;
            using System.Threading.Tasks;

            [AppBehavior("orders.get")]
            [BehaviorRestProfile(BehaviorRestMethod.Get, "{orderId}")]
            public sealed class GetOrderBehavior : IAppBehavior<string, string>
            {
                public Task<string> HandleAsync(string input, IBehaviorContext ctx, CancellationToken ct = default)
                    => Task.FromResult("ok");
            }
            """;

        var (_, diagnostics) = RunGenerator(source);

        Assert.Contains(diagnostics, d => d.Id == "ABT0018");
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
