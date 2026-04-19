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
        namespace System.Text.Json.Serialization
        {
            [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
            public sealed class JsonStringEnumMemberNameAttribute : System.Attribute
            {
                public JsonStringEnumMemberNameAttribute(string name) { Name = name; }
                public string Name { get; }
            }
        }

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
                IBehaviorTopologyBuilder AsDurableExecution();
                IBehaviorTopologyBuilder ViaHttpJsonRpc();
            }
        }

        namespace Cephalon.Behaviors.Http.Abstractions
        {
            public enum BehaviorRestBindingSource
            {
                [System.Text.Json.Serialization.JsonStringEnumMemberName("unspecified")]
                Unspecified = 0,
                [System.Text.Json.Serialization.JsonStringEnumMemberName("route")]
                Route = 1,
                [System.Text.Json.Serialization.JsonStringEnumMemberName("query")]
                Query = 2,
                [System.Text.Json.Serialization.JsonStringEnumMemberName("header")]
                Header = 3,
                [System.Text.Json.Serialization.JsonStringEnumMemberName("body")]
                Body = 4
            }

            public enum BehaviorRestMethod
            {
                [System.Text.Json.Serialization.JsonStringEnumMemberName("unspecified")]
                Unspecified = 0,
                [System.Text.Json.Serialization.JsonStringEnumMemberName("get")]
                Get = 1,
                [System.Text.Json.Serialization.JsonStringEnumMemberName("post")]
                Post = 2,
                [System.Text.Json.Serialization.JsonStringEnumMemberName("put")]
                Put = 3,
                [System.Text.Json.Serialization.JsonStringEnumMemberName("patch")]
                Patch = 4,
                [System.Text.Json.Serialization.JsonStringEnumMemberName("delete")]
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
                public bool PreserveImplicitQueryFallback { get; set; }
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
                    System.Collections.Generic.IReadOnlyList<BehaviorRestBindingDescriptor>? bindings = null,
                    bool PreserveImplicitQueryFallback = false)
                {
                    BehaviorId = behaviorId;
                    Method = method;
                    RelativePattern = relativePattern;
                    ApiVersionMajor = apiVersionMajor;
                    Bindings = bindings;
                    this.PreserveImplicitQueryFallback = PreserveImplicitQueryFallback;
                }

                public string BehaviorId { get; }
                public BehaviorRestMethod Method { get; }
                public string RelativePattern { get; }
                public int? ApiVersionMajor { get; }
                public System.Collections.Generic.IReadOnlyList<BehaviorRestBindingDescriptor>? Bindings { get; }
                public bool PreserveImplicitQueryFallback { get; }
            }
        }
        """;

    private static (GeneratorDriverRunResult Result, ImmutableArray<Diagnostic> Diagnostics)
        RunGenerator(string source, string? attributeStubs = null)
    {
        var compilation = CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees:
            [
                CSharpSyntaxTree.ParseText(attributeStubs ?? AttributeStubs),
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
        Assert.Contains("GetRestProfileBehaviorTypes()", autoRegistration, StringComparison.Ordinal);
        Assert.Contains("new global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestProfileDescriptor(\"orders.get\"", autoRegistration, StringComparison.Ordinal);
        Assert.Contains("(\"orders.get\", typeof(global::GetOrderBehavior))", autoRegistration, StringComparison.Ordinal);
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
    public void ValidBehaviorWithRestProfilePreservedImplicitQueryFallbackGeneratesFallbackHint()
    {
        const string source = """
            using Cephalon.Abstractions.Behaviors;
            using Cephalon.Behaviors.Http.Abstractions;
            using System.Threading;
            using System.Threading.Tasks;

            public sealed record LookupOrderInput(string OrderId, int Quantity);

            [AppBehavior("orders.lookup")]
            [BehaviorRestProfile(BehaviorRestMethod.Get, "/lookup/{orderId}", ApiVersionMajor = 3, PreserveImplicitQueryFallback = true)]
            [BehaviorRestBinding(nameof(LookupOrderInput.OrderId), BehaviorRestBindingSource.Route, Name = "orderId")]
            public sealed class LookupOrderBehavior : IAppBehavior<LookupOrderInput, string>
            {
                public Task<string> HandleAsync(LookupOrderInput input, IBehaviorContext ctx, CancellationToken ct = default)
                    => Task.FromResult("ok");
            }
            """;

        var (result, diagnostics) = RunGenerator(source);

        Assert.Empty(diagnostics);

        var autoRegistration = GetGeneratedAutoRegistrationSource(result);
        Assert.NotNull(autoRegistration);
        Assert.Contains("PreserveImplicitQueryFallback: true", autoRegistration, StringComparison.Ordinal);
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
            BehaviorSourceGenerator.Abt019RestBindingPropertyNameMustNotBeEmpty,
            BehaviorSourceGenerator.Abt020RestBindingSourceMustBeSupported,
            BehaviorSourceGenerator.Abt021RestBindingsRequireObjectInput,
            BehaviorSourceGenerator.Abt022RestBindingPropertyMustExistOnInput,
            BehaviorSourceGenerator.Abt023RestBindingPropertyMustNotBeDuplicated,
            BehaviorSourceGenerator.Abt024RestBodyBindingMustUseBodyCapableMethod,
            BehaviorSourceGenerator.Abt025RestRouteBindingMustMatchRoutePlaceholder,
            BehaviorSourceGenerator.Abt026RestProfilePatternMustUseValidPlaceholderSyntax,
            BehaviorSourceGenerator.Abt027RestPreservedImplicitQueryFallbackRequiresExplicitBindings,
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
        Assert.Equal("ABT0019", BehaviorSourceGenerator.Abt019RestBindingPropertyNameMustNotBeEmpty.Id);
        Assert.Equal("ABT0020", BehaviorSourceGenerator.Abt020RestBindingSourceMustBeSupported.Id);
        Assert.Equal("ABT0021", BehaviorSourceGenerator.Abt021RestBindingsRequireObjectInput.Id);
        Assert.Equal("ABT0022", BehaviorSourceGenerator.Abt022RestBindingPropertyMustExistOnInput.Id);
        Assert.Equal("ABT0023", BehaviorSourceGenerator.Abt023RestBindingPropertyMustNotBeDuplicated.Id);
        Assert.Equal("ABT0024", BehaviorSourceGenerator.Abt024RestBodyBindingMustUseBodyCapableMethod.Id);
        Assert.Equal("ABT0025", BehaviorSourceGenerator.Abt025RestRouteBindingMustMatchRoutePlaceholder.Id);
        Assert.Equal("ABT0026", BehaviorSourceGenerator.Abt026RestProfilePatternMustUseValidPlaceholderSyntax.Id);
        Assert.Equal("ABT0027", BehaviorSourceGenerator.Abt027RestPreservedImplicitQueryFallbackRequiresExplicitBindings.Id);
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
    public void RestBindingWithoutPropertyNameEmitsAbt0019()
    {
        const string source = """
            using Cephalon.Abstractions.Behaviors;
            using Cephalon.Behaviors.Http.Abstractions;
            using System.Threading;
            using System.Threading.Tasks;

            public sealed record LookupOrderInput(string OrderId);

            [AppBehavior("orders.lookup")]
            [BehaviorRestProfile(BehaviorRestMethod.Get, "/{orderId}")]
            [BehaviorRestBinding("", BehaviorRestBindingSource.Route, Name = "orderId")]
            public sealed class LookupOrderBehavior : IAppBehavior<LookupOrderInput, string>
            {
                public Task<string> HandleAsync(LookupOrderInput input, IBehaviorContext ctx, CancellationToken ct = default)
                    => Task.FromResult("ok");
            }
            """;

        var (_, diagnostics) = RunGenerator(source);

        Assert.Contains(diagnostics, d => d.Id == "ABT0019");
    }

    [Fact]
    public void RestBindingWithoutSupportedSourceEmitsAbt0020()
    {
        const string source = """
            using Cephalon.Abstractions.Behaviors;
            using Cephalon.Behaviors.Http.Abstractions;
            using System.Threading;
            using System.Threading.Tasks;

            public sealed record LookupOrderInput(string OrderId);

            [AppBehavior("orders.lookup")]
            [BehaviorRestProfile(BehaviorRestMethod.Get, "/{orderId}")]
            [BehaviorRestBinding(nameof(LookupOrderInput.OrderId), (BehaviorRestBindingSource)0, Name = "orderId")]
            public sealed class LookupOrderBehavior : IAppBehavior<LookupOrderInput, string>
            {
                public Task<string> HandleAsync(LookupOrderInput input, IBehaviorContext ctx, CancellationToken ct = default)
                    => Task.FromResult("ok");
            }
            """;

        var (_, diagnostics) = RunGenerator(source);

        Assert.Contains(diagnostics, d => d.Id == "ABT0020");
    }

    [Fact]
    public void RestBindingsOnScalarInputEmitAbt0021()
    {
        const string source = """
            using Cephalon.Abstractions.Behaviors;
            using Cephalon.Behaviors.Http.Abstractions;
            using System.Threading;
            using System.Threading.Tasks;

            [AppBehavior("orders.lookup")]
            [BehaviorRestProfile(BehaviorRestMethod.Get, "/{value}")]
            [BehaviorRestBinding("Value", BehaviorRestBindingSource.Route, Name = "value")]
            public sealed class LookupOrderBehavior : IAppBehavior<string, string>
            {
                public Task<string> HandleAsync(string input, IBehaviorContext ctx, CancellationToken ct = default)
                    => Task.FromResult("ok");
            }
            """;

        var (_, diagnostics) = RunGenerator(source);

        Assert.Contains(diagnostics, d => d.Id == "ABT0021");
    }

    [Fact]
    public void RestBindingsOnInputWithoutPublicPropertiesEmitAbt0021()
    {
        const string source = """
            using Cephalon.Abstractions.Behaviors;
            using Cephalon.Behaviors.Http.Abstractions;
            using System.Threading;
            using System.Threading.Tasks;

            public sealed class LookupOrderInput
            {
            }

            [AppBehavior("orders.lookup")]
            [BehaviorRestProfile(BehaviorRestMethod.Get, "/{orderId}")]
            [BehaviorRestBinding("OrderId", BehaviorRestBindingSource.Route, Name = "orderId")]
            public sealed class LookupOrderBehavior : IAppBehavior<LookupOrderInput, string>
            {
                public Task<string> HandleAsync(LookupOrderInput input, IBehaviorContext ctx, CancellationToken ct = default)
                    => Task.FromResult("ok");
            }
            """;

        var (_, diagnostics) = RunGenerator(source);

        Assert.Contains(diagnostics, d => d.Id == "ABT0021");
    }

    [Fact]
    public void RestBindingForUnknownInputPropertyEmitsAbt0022()
    {
        const string source = """
            using Cephalon.Abstractions.Behaviors;
            using Cephalon.Behaviors.Http.Abstractions;
            using System.Threading;
            using System.Threading.Tasks;

            public sealed record LookupOrderInput(string OrderId);

            [AppBehavior("orders.lookup")]
            [BehaviorRestProfile(BehaviorRestMethod.Get, "/{orderId}")]
            [BehaviorRestBinding("MissingProperty", BehaviorRestBindingSource.Query, Name = "orderId")]
            public sealed class LookupOrderBehavior : IAppBehavior<LookupOrderInput, string>
            {
                public Task<string> HandleAsync(LookupOrderInput input, IBehaviorContext ctx, CancellationToken ct = default)
                    => Task.FromResult("ok");
            }
            """;

        var (_, diagnostics) = RunGenerator(source);

        Assert.Contains(diagnostics, d => d.Id == "ABT0022");
    }

    [Fact]
    public void DuplicateRestBindingsForTheSameInputPropertyEmitAbt0023()
    {
        const string source = """
            using Cephalon.Abstractions.Behaviors;
            using Cephalon.Behaviors.Http.Abstractions;
            using System.Threading;
            using System.Threading.Tasks;

            public sealed record LookupOrderInput(string OrderId);

            [AppBehavior("orders.lookup")]
            [BehaviorRestProfile(BehaviorRestMethod.Get, "/{orderId}")]
            [BehaviorRestBinding(nameof(LookupOrderInput.OrderId), BehaviorRestBindingSource.Route, Name = "orderId")]
            [BehaviorRestBinding(nameof(LookupOrderInput.OrderId), BehaviorRestBindingSource.Query, Name = "orderId")]
            public sealed class LookupOrderBehavior : IAppBehavior<LookupOrderInput, string>
            {
                public Task<string> HandleAsync(LookupOrderInput input, IBehaviorContext ctx, CancellationToken ct = default)
                    => Task.FromResult("ok");
            }
            """;

        var (_, diagnostics) = RunGenerator(source);

        Assert.Contains(diagnostics, d => d.Id == "ABT0023");
    }

    [Fact]
    public void RestBodyBindingForGetEndpointEmitsAbt0024()
    {
        const string source = """
            using Cephalon.Abstractions.Behaviors;
            using Cephalon.Behaviors.Http.Abstractions;
            using System.Threading;
            using System.Threading.Tasks;

            public sealed record LookupOrderInput(string Note);

            [AppBehavior("orders.lookup")]
            [BehaviorRestProfile(BehaviorRestMethod.Get, "/{orderId}")]
            [BehaviorRestBinding(nameof(LookupOrderInput.Note), BehaviorRestBindingSource.Body, Name = "note")]
            public sealed class LookupOrderBehavior : IAppBehavior<LookupOrderInput, string>
            {
                public Task<string> HandleAsync(LookupOrderInput input, IBehaviorContext ctx, CancellationToken ct = default)
                    => Task.FromResult("ok");
            }
            """;

        var (_, diagnostics) = RunGenerator(source);

        Assert.Contains(diagnostics, d => d.Id == "ABT0024");
    }

    [Fact]
    public void RestRouteBindingWithoutMatchingPlaceholderEmitsAbt0025()
    {
        const string source = """
            using Cephalon.Abstractions.Behaviors;
            using Cephalon.Behaviors.Http.Abstractions;
            using System.Threading;
            using System.Threading.Tasks;

            public sealed record LookupOrderInput(string OrderId);

            [AppBehavior("orders.lookup")]
            [BehaviorRestProfile(BehaviorRestMethod.Get, "/{differentOrderId}")]
            [BehaviorRestBinding(nameof(LookupOrderInput.OrderId), BehaviorRestBindingSource.Route, Name = "orderId")]
            public sealed class LookupOrderBehavior : IAppBehavior<LookupOrderInput, string>
            {
                public Task<string> HandleAsync(LookupOrderInput input, IBehaviorContext ctx, CancellationToken ct = default)
                    => Task.FromResult("ok");
            }
            """;

        var (_, diagnostics) = RunGenerator(source);

        Assert.Contains(diagnostics, d => d.Id == "ABT0025");
    }

    [Fact]
    public void RestProfileWithInvalidPlaceholderSyntaxEmitsAbt0026()
    {
        const string source = """
            using Cephalon.Abstractions.Behaviors;
            using Cephalon.Behaviors.Http.Abstractions;
            using System.Threading;
            using System.Threading.Tasks;

            [AppBehavior("orders.lookup")]
            [BehaviorRestProfile(BehaviorRestMethod.Get, "/orders/{")]
            public sealed class LookupOrderBehavior : IAppBehavior<string, string>
            {
                public Task<string> HandleAsync(string input, IBehaviorContext ctx, CancellationToken ct = default)
                    => Task.FromResult("ok");
            }
            """;

        var (_, diagnostics) = RunGenerator(source);

        Assert.Contains(diagnostics, d => d.Id == "ABT0026");
    }

    [Fact]
    public void RestProfilePreservedImplicitQueryFallbackWithoutBindingsEmitsAbt0027()
    {
        const string source = """
            using Cephalon.Abstractions.Behaviors;
            using Cephalon.Behaviors.Http.Abstractions;
            using System.Threading;
            using System.Threading.Tasks;

            public sealed record LookupOrderInput(string OrderId, int Quantity);

            [AppBehavior("orders.lookup")]
            [BehaviorRestProfile(BehaviorRestMethod.Get, "/lookup", PreserveImplicitQueryFallback = true)]
            public sealed class LookupOrderBehavior : IAppBehavior<LookupOrderInput, string>
            {
                public Task<string> HandleAsync(LookupOrderInput input, IBehaviorContext ctx, CancellationToken ct = default)
                    => Task.FromResult("ok");
            }
            """;

        var (_, diagnostics) = RunGenerator(source);

        Assert.Contains(diagnostics, d => d.Id == "ABT0027");
    }

    [Fact]
    public void RestProfileHintsUseResolvedEnumMemberNamesInsteadOfEnumOrdinals()
    {
        const string source = """
            using Cephalon.Abstractions.Behaviors;
            using Cephalon.Behaviors.Http.Abstractions;
            using System.Threading;
            using System.Threading.Tasks;

            public sealed record LookupOrderInput(string OrderId, int Quantity);

            [AppBehavior("orders.lookup")]
            [BehaviorRestProfile(BehaviorRestMethod.Get, "/{orderId}")]
            [BehaviorRestBinding(nameof(LookupOrderInput.OrderId), BehaviorRestBindingSource.Route, Name = "orderId")]
            [BehaviorRestBinding(nameof(LookupOrderInput.Quantity), BehaviorRestBindingSource.Query, Name = "quantity")]
            public sealed class LookupOrderBehavior : IAppBehavior<LookupOrderInput, string>
            {
                public Task<string> HandleAsync(LookupOrderInput input, IBehaviorContext ctx, CancellationToken ct = default)
                    => Task.FromResult("ok");
            }
            """;

        var reorderedStubs = AttributeStubs
            .Replace(
                """
                        public enum BehaviorRestBindingSource
                        {
                            [System.Text.Json.Serialization.JsonStringEnumMemberName("unspecified")]
                            Unspecified = 0,
                            [System.Text.Json.Serialization.JsonStringEnumMemberName("route")]
                            Route = 1,
                            [System.Text.Json.Serialization.JsonStringEnumMemberName("query")]
                            Query = 2,
                            [System.Text.Json.Serialization.JsonStringEnumMemberName("header")]
                            Header = 3,
                            [System.Text.Json.Serialization.JsonStringEnumMemberName("body")]
                            Body = 4
                        }
                """,
                """
                        public enum BehaviorRestBindingSource
                        {
                            [System.Text.Json.Serialization.JsonStringEnumMemberName("unspecified")]
                            Unspecified = 0,
                            [System.Text.Json.Serialization.JsonStringEnumMemberName("header")]
                            Header = 40,
                            [System.Text.Json.Serialization.JsonStringEnumMemberName("body")]
                            Body = 30,
                            [System.Text.Json.Serialization.JsonStringEnumMemberName("query")]
                            Query = 20,
                            [System.Text.Json.Serialization.JsonStringEnumMemberName("route")]
                            Route = 10
                        }
                """,
                StringComparison.Ordinal)
            .Replace(
                """
                        public enum BehaviorRestMethod
                        {
                            [System.Text.Json.Serialization.JsonStringEnumMemberName("unspecified")]
                            Unspecified = 0,
                            [System.Text.Json.Serialization.JsonStringEnumMemberName("get")]
                            Get = 1,
                            [System.Text.Json.Serialization.JsonStringEnumMemberName("post")]
                            Post = 2,
                            [System.Text.Json.Serialization.JsonStringEnumMemberName("put")]
                            Put = 3,
                            [System.Text.Json.Serialization.JsonStringEnumMemberName("patch")]
                            Patch = 4,
                            [System.Text.Json.Serialization.JsonStringEnumMemberName("delete")]
                            Delete = 5
                        }
                """,
                """
                        public enum BehaviorRestMethod
                        {
                            [System.Text.Json.Serialization.JsonStringEnumMemberName("unspecified")]
                            Unspecified = 0,
                            [System.Text.Json.Serialization.JsonStringEnumMemberName("delete")]
                            Delete = 50,
                            [System.Text.Json.Serialization.JsonStringEnumMemberName("patch")]
                            Patch = 40,
                            [System.Text.Json.Serialization.JsonStringEnumMemberName("put")]
                            Put = 30,
                            [System.Text.Json.Serialization.JsonStringEnumMemberName("post")]
                            Post = 20,
                            [System.Text.Json.Serialization.JsonStringEnumMemberName("get")]
                            Get = 10
                        }
                """,
                StringComparison.Ordinal);

        var (result, diagnostics) = RunGenerator(source, reorderedStubs);

        Assert.Empty(diagnostics);

        var autoRegistration = GetGeneratedAutoRegistrationSource(result);
        Assert.NotNull(autoRegistration);
        Assert.Contains("BehaviorRestMethod.Get", autoRegistration, StringComparison.Ordinal);
        Assert.Contains("BehaviorRestBindingSource.Route", autoRegistration, StringComparison.Ordinal);
        Assert.Contains("BehaviorRestBindingSource.Query", autoRegistration, StringComparison.Ordinal);
    }

    [Fact]
    public void RestProfileHintsRespectMethodWireNamesWhenEnumMembersAreRenamed()
    {
        const string source = """
            using Cephalon.Abstractions.Behaviors;
            using Cephalon.Behaviors.Http.Abstractions;
            using System.Threading;
            using System.Threading.Tasks;

            public sealed record LookupOrderInput(string OrderId);

            [AppBehavior("orders.lookup")]
            [BehaviorRestProfile(BehaviorRestMethod.Read, "/{orderId}")]
            public sealed class LookupOrderBehavior : IAppBehavior<LookupOrderInput, string>
            {
                public Task<string> HandleAsync(LookupOrderInput input, IBehaviorContext ctx, CancellationToken ct = default)
                    => Task.FromResult("ok");
            }
            """;

        var renamedMethodStubs = AttributeStubs
            .Replace("Unspecified = 0,", "None = 0,", StringComparison.Ordinal)
            .Replace("Get = 1,", "Read = 10,", StringComparison.Ordinal)
            .Replace("Post = 2,", "Create = 20,", StringComparison.Ordinal)
            .Replace("Put = 3,", "Replace = 30,", StringComparison.Ordinal)
            .Replace("Patch = 4,", "Update = 40,", StringComparison.Ordinal)
            .Replace("Delete = 5", "Remove = 50", StringComparison.Ordinal);

        var (result, diagnostics) = RunGenerator(source, renamedMethodStubs);

        Assert.Empty(diagnostics);

        var autoRegistration = GetGeneratedAutoRegistrationSource(result);
        Assert.NotNull(autoRegistration);
        Assert.Contains("BehaviorRestMethod.Read", autoRegistration, StringComparison.Ordinal);
    }

    [Fact]
    public void RestProfileHintsRespectBindingSourceWireNamesWhenEnumMembersAreRenamed()
    {
        const string source = """
            using Cephalon.Abstractions.Behaviors;
            using Cephalon.Behaviors.Http.Abstractions;
            using System.Threading;
            using System.Threading.Tasks;

            public sealed record LookupOrderInput(string OrderId, int Quantity);

            [AppBehavior("orders.lookup")]
            [BehaviorRestProfile(BehaviorRestMethod.Get, "/{orderId}")]
            [BehaviorRestBinding(nameof(LookupOrderInput.OrderId), BehaviorRestBindingSource.RouteParameter, Name = "orderId")]
            [BehaviorRestBinding(nameof(LookupOrderInput.Quantity), BehaviorRestBindingSource.QueryValue, Name = "quantity")]
            public sealed class LookupOrderBehavior : IAppBehavior<LookupOrderInput, string>
            {
                public Task<string> HandleAsync(LookupOrderInput input, IBehaviorContext ctx, CancellationToken ct = default)
                    => Task.FromResult("ok");
            }
            """;

        var renamedBindingSourceStubs = AttributeStubs
            .Replace("Unspecified = 0,", "None = 0,", StringComparison.Ordinal)
            .Replace("Route = 1,", "RouteParameter = 10,", StringComparison.Ordinal)
            .Replace("Query = 2,", "QueryValue = 20,", StringComparison.Ordinal)
            .Replace("Header = 3,", "RequestHeader = 30,", StringComparison.Ordinal)
            .Replace("Body = 4", "RequestBody = 40", StringComparison.Ordinal);

        var (result, diagnostics) = RunGenerator(source, renamedBindingSourceStubs);

        Assert.Empty(diagnostics);

        var autoRegistration = GetGeneratedAutoRegistrationSource(result);
        Assert.NotNull(autoRegistration);
        Assert.Contains("BehaviorRestBindingSource.RouteParameter", autoRegistration, StringComparison.Ordinal);
        Assert.Contains("BehaviorRestBindingSource.QueryValue", autoRegistration, StringComparison.Ordinal);
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
    public void ConfigureTopologyWithDurableExecutionPatternGeneratesCompileTimeDescriptor()
    {
        const string source = """
            using Cephalon.Abstractions.Behaviors;
            using System.Threading;
            using System.Threading.Tasks;

            [AppBehavior("orders.workflow")]
            public sealed class OrderWorkflowBehavior : IAppBehavior<string, string>
            {
                public static void ConfigureTopology(IBehaviorTopologyBuilder builder)
                    => builder.AsDurableExecution();

                public Task<string> HandleAsync(string input, IBehaviorContext ctx, CancellationToken ct = default)
                    => Task.FromResult("ok");
            }
            """;

        var (result, diagnostics) = RunGenerator(source);

        Assert.Empty(diagnostics);

        var autoRegistration = GetGeneratedAutoRegistrationSource(result);
        Assert.NotNull(autoRegistration);
        Assert.Contains("\"durable-execution\"", autoRegistration, StringComparison.Ordinal);
    }

    [Fact]
    public void ConfigureTopologyWithLiteralRequiredFeatureFlagsGeneratesCompileTimeDescriptor()
    {
        const string source = """
            using Cephalon.Abstractions.Behaviors;
            using System.Threading;
            using System.Threading.Tasks;

            [AppBehavior("orders.preview")]
            public sealed class PreviewOrderBehavior : IAppBehavior<string, string>
            {
                public static void ConfigureTopology(IBehaviorTopologyBuilder builder)
                    => builder
                        .ViaHttpJsonRpc()
                        .RequireFeatureFlag("host.orders-preview")
                        .RequireFeatureFlags("module.orders-rollout");

                public Task<string> HandleAsync(string input, IBehaviorContext ctx, CancellationToken ct = default)
                    => Task.FromResult("ok");
            }
            """;

        const string stubs = """
            namespace System.Text.Json.Serialization
            {
                [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
                public sealed class JsonStringEnumMemberNameAttribute : System.Attribute
                {
                    public JsonStringEnumMemberNameAttribute(string name) { Name = name; }
                    public string Name { get; }
                }
            }

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

                public interface IBehaviorTopologyBuilder
                {
                    IBehaviorTopologyBuilder ViaHttpJsonRpc();
                    IBehaviorTopologyBuilder RequireFeatureFlag(string featureFlagId);
                    IBehaviorTopologyBuilder RequireFeatureFlags(params string[] featureFlagIds);
                }
            }
            """;

        var (result, diagnostics) = RunGenerator(source, stubs);

        Assert.Empty(diagnostics);

        var autoRegistration = GetGeneratedAutoRegistrationSource(result);
        Assert.NotNull(autoRegistration);
        Assert.Contains("requiredFeatureFlagIds: new string[] { \"host.orders-preview\", \"module.orders-rollout\" }", autoRegistration, StringComparison.Ordinal);
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
