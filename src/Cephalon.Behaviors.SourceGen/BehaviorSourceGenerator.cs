using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Cephalon.Behaviors.SourceGen;

/// <summary>
/// Incremental source generator that validates classes decorated with
/// <c>[AppBehavior]</c> and emits compile-time diagnostics for common authoring mistakes.
/// Diagnostic IDs: ABT-010 through ABT-013.
/// </summary>
[Generator]
public sealed class BehaviorSourceGenerator : IIncrementalGenerator
{
    private const string HelpLink =
        "https://github.com/Cephalon-Labs/CephalonEngine/blob/master/docs/components/behaviors-sourcegen.md#diagnostic-rules";

    // ─────────────────────────────────────────────────────────────────────────
    // Diagnostic descriptors
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>ABT-010: Class with [AppBehavior] must implement IAppBehavior&lt;TIn, TOut&gt;.</summary>
    public static readonly DiagnosticDescriptor Abt010MustImplementIAppBehavior = new(
        id: "ABT0010",
        title: "AppBehavior class must implement IAppBehavior<TIn, TOut>",
        messageFormat: "'{0}' is marked with [AppBehavior] but does not implement IAppBehavior<TIn, TOut>",
        category: "Cephalon.Behaviors",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Every class decorated with [AppBehavior] must implement the IAppBehavior<TIn, TOut> interface so the dispatcher can invoke it.",
        helpLinkUri: HelpLink);

    /// <summary>ABT-011: [AppBehavior] id must not be empty or whitespace.</summary>
    public static readonly DiagnosticDescriptor Abt011EmptyBehaviorId = new(
        id: "ABT0011",
        title: "AppBehavior id must not be empty",
        messageFormat: "'{0}' has an [AppBehavior] attribute with an empty or whitespace id",
        category: "Cephalon.Behaviors",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The behavior id supplied to [AppBehavior] is used for dispatch and configuration lookup; it must be a non-empty string.",
        helpLinkUri: HelpLink);

    /// <summary>ABT-012: [AppBehavior] class must not be abstract.</summary>
    public static readonly DiagnosticDescriptor Abt012MustNotBeAbstract = new(
        id: "ABT0012",
        title: "AppBehavior class must not be abstract",
        messageFormat: "'{0}' is marked with [AppBehavior] but is abstract; behavior classes must be concrete",
        category: "Cephalon.Behaviors",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Abstract classes cannot be instantiated by the dispatcher. Mark the class as concrete or remove [AppBehavior].",
        helpLinkUri: HelpLink);

    /// <summary>ABT-013: [AppBehavior] class must not be static.</summary>
    public static readonly DiagnosticDescriptor Abt013MustNotBeStatic = new(
        id: "ABT0013",
        title: "AppBehavior class must not be static",
        messageFormat: "'{0}' is marked with [AppBehavior] but is static; behavior classes must be instantiable",
        category: "Cephalon.Behaviors",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Static classes cannot be instantiated by the dispatcher. Remove the static modifier or remove [AppBehavior].",
        helpLinkUri: HelpLink);

    // ─────────────────────────────────────────────────────────────────────────
    // Initialization
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Gate 15: use ForAttributeWithMetadataName — only visits classes carrying the exact attribute.
        var behaviorTypes = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "Cephalon.Abstractions.Behaviors.AppBehaviorAttribute",
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, ct) => GetBehaviorInfo(ctx, ct))
            .Where(static info => info is not null);

        // Report diagnostics per-item (incremental: runs once per changed class).
        context.RegisterSourceOutput(behaviorTypes, static (spc, info) =>
        {
            ReportDiagnostics(spc, info!);
        });

        // Emit generated registration hint from all valid behaviors.
        var collected = behaviorTypes.Collect();
        context.RegisterSourceOutput(collected, static (spc, infos) =>
        {
            var valid = infos.Where(static i => i!.IsValid).ToImmutableArray();
            if (!valid.IsEmpty)
            {
                spc.AddSource("BehaviorRegistrationHints.g.cs", BuildRegistrationHints(valid));
            }
        });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Semantic model extraction
    // ─────────────────────────────────────────────────────────────────────────

    private static BehaviorInfo? GetBehaviorInfo(
        GeneratorAttributeSyntaxContext ctx,
        System.Threading.CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (ctx.TargetSymbol is not INamedTypeSymbol typeSymbol)
            return null;

        // Extract the id from the attribute constructor argument.
        var attribute = ctx.Attributes.FirstOrDefault();
        var id = attribute?.ConstructorArguments.FirstOrDefault().Value as string ?? string.Empty;

        var isAbstract = typeSymbol.IsAbstract;
        var isStatic = typeSymbol.IsStatic;

        var implementsInterface = ImplementsIAppBehavior(typeSymbol);

        var location = ctx.TargetNode.GetLocation();

        return new BehaviorInfo(
            typeName: typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            shortName: typeSymbol.Name,
            behaviorId: id,
            isAbstract: isAbstract,
            isStatic: isStatic,
            implementsInterface: implementsInterface,
            location: location);
    }

    private static bool ImplementsIAppBehavior(INamedTypeSymbol typeSymbol)
    {
        return typeSymbol.AllInterfaces.Any(static i =>
            i.OriginalDefinition.ToDisplayString() ==
            "Cephalon.Abstractions.Behaviors.IAppBehavior<TIn, TOut>");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Diagnostics
    // ─────────────────────────────────────────────────────────────────────────

    private static void ReportDiagnostics(SourceProductionContext spc, BehaviorInfo info)
    {
        if (string.IsNullOrWhiteSpace(info.BehaviorId))
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                Abt011EmptyBehaviorId,
                info.Location,
                info.ShortName));
        }

        if (info.IsAbstract)
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                Abt012MustNotBeAbstract,
                info.Location,
                info.ShortName));
        }

        if (info.IsStatic)
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                Abt013MustNotBeStatic,
                info.Location,
                info.ShortName));
        }

        if (!info.ImplementsInterface && !info.IsStatic)
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                Abt010MustImplementIAppBehavior,
                info.Location,
                info.ShortName));
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Code generation
    // ─────────────────────────────────────────────────────────────────────────

    private static SourceText BuildRegistrationHints(ImmutableArray<BehaviorInfo?> infos)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("// Generated by Cephalon.Behaviors.SourceGen — do not edit.");
        sb.AppendLine();
        sb.AppendLine("namespace Cephalon.Behaviors.Generated;");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Auto-generated catalog of behavior identifiers discovered at compile time.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("internal static class BehaviorRegistrationHints");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>All behavior ids discovered via [AppBehavior] at compile time.</summary>");
        sb.AppendLine("    internal static readonly System.Collections.Generic.IReadOnlyList<string> BehaviorIds =");
        sb.AppendLine("    [");

        foreach (var info in infos)
        {
            if (info is { IsValid: true })
            {
                sb.AppendLine($"        \"{EscapeString(info.BehaviorId)}\",");
            }
        }

        sb.AppendLine("    ];");
        sb.AppendLine("}");

        return SourceText.From(sb.ToString(), Encoding.UTF8);
    }

    private static string EscapeString(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"");

    // ─────────────────────────────────────────────────────────────────────────
    // Data model
    // ─────────────────────────────────────────────────────────────────────────

    private sealed class BehaviorInfo
    {
        public BehaviorInfo(
            string typeName,
            string shortName,
            string behaviorId,
            bool isAbstract,
            bool isStatic,
            bool implementsInterface,
            Location location)
        {
            TypeName = typeName;
            ShortName = shortName;
            BehaviorId = behaviorId;
            IsAbstract = isAbstract;
            IsStatic = isStatic;
            ImplementsInterface = implementsInterface;
            Location = location;
        }

        public string TypeName { get; }
        public string ShortName { get; }
        public string BehaviorId { get; }
        public bool IsAbstract { get; }
        public bool IsStatic { get; }
        public bool ImplementsInterface { get; }
        public Location Location { get; }

        /// <summary>
        /// True when the behavior class passes all validation checks and should be included in generated output.
        /// </summary>
        public bool IsValid =>
            !string.IsNullOrWhiteSpace(BehaviorId) &&
            !IsAbstract &&
            !IsStatic &&
            ImplementsInterface;
    }
}
