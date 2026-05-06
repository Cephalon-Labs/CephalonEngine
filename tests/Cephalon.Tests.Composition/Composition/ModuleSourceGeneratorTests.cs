using System.Collections.Immutable;
using System.Reflection;
using Cephalon.Engine.SourceGen;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cephalon.Tests.Composition;

/// <summary>
/// Exercises <see cref="ModuleSourceGenerator"/> with the Roslyn generator driver.
/// </summary>
public sealed class ModuleSourceGeneratorTests
{
    private const string ModuleStubs = """
        namespace Cephalon.Abstractions.Modules
        {
            public interface IModule
            {
            }

            public sealed class ModuleDiscoveryDescriptor
            {
                public ModuleDiscoveryDescriptor(System.Type moduleType, System.Func<IModule> moduleFactory)
                {
                }
            }

            public static class ModuleDiscoveryRegistry
            {
                public static void Register(
                    System.Reflection.Assembly assembly,
                    System.Collections.Generic.IEnumerable<ModuleDiscoveryDescriptor> descriptors)
                {
                }
            }
        }
        """;

    [Fact]
    public void ConcreteModuleEmitsGeneratedDescriptorRegistration()
    {
        const string source = """
            namespace Sample;

            internal sealed class OrdersModule : Cephalon.Abstractions.Modules.IModule
            {
            }
            """;

        var result = RunGenerator(source);
        var generated = GetGeneratedSource(result);

        Assert.NotNull(generated);
        Assert.Contains("internal static class CephalonGeneratedModuleDiscovery_GeneratedModuleTests", generated, StringComparison.Ordinal);
        Assert.Contains("ModuleDiscoveryRegistry.Register", generated, StringComparison.Ordinal);
        Assert.Contains("typeof(global::Sample.OrdersModule)", generated, StringComparison.Ordinal);
        Assert.Contains("static () => new global::Sample.OrdersModule()", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void ModuleWithoutAccessibleParameterlessConstructorDoesNotEmitDescriptor()
    {
        const string source = """
            namespace Sample;

            internal sealed class FactoryOnlyModule : Cephalon.Abstractions.Modules.IModule
            {
                private FactoryOnlyModule()
                {
                }
            }
            """;

        var result = RunGenerator(source);

        Assert.Null(GetGeneratedSource(result));
    }

    private static GeneratorDriverRunResult RunGenerator(string source)
    {
        var compilation = CSharpCompilation.Create(
            assemblyName: "GeneratedModuleTests",
            syntaxTrees:
            [
                CSharpSyntaxTree.ParseText(ModuleStubs),
                CSharpSyntaxTree.ParseText(source)
            ],
            references:
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Collections").Location),
            ],
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new ModuleSourceGenerator();
        var driver = CSharpGeneratorDriver
            .Create(generator)
            .RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);

        var diagnostics = outputCompilation
            .GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToImmutableArray();
        Assert.Empty(diagnostics);

        return driver.GetRunResult();
    }

    private static string? GetGeneratedSource(GeneratorDriverRunResult result) =>
        result.GeneratedTrees
            .FirstOrDefault(tree => tree.FilePath.EndsWith("CephalonGeneratedModuleDiscovery_GeneratedModuleTests.g.cs", StringComparison.Ordinal))
            ?.GetText()
            .ToString();
}
