using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Cephalon.Behaviors.SourceGen;

/// <summary>
/// Incremental source generator that validates classes decorated with
/// <c>[AppBehavior]</c>, emits compile-time diagnostics for common authoring mistakes,
/// and generates zero-reflection registration code with pre-built topology descriptors.
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

        // Emit generated code from all valid behaviors.
        var collected = behaviorTypes.Collect();
        context.RegisterSourceOutput(collected, static (spc, infos) =>
        {
            var valid = infos.Where(static i => i!.IsValid).ToImmutableArray();
            if (!valid.IsEmpty)
            {
                spc.AddSource("BehaviorRegistrationHints.g.cs", BuildRegistrationHints(valid));
                spc.AddSource("BehaviorAutoRegistration.g.cs", BuildAutoRegistration(valid));
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

        // Extract topology from static ConfigureTopology method if present
        TopologyInfo? topology = null;
        if (implementsInterface && !isAbstract && !isStatic && ctx.TargetNode is ClassDeclarationSyntax classDecl)
        {
            topology = ExtractTopologyFromConfigureMethod(classDecl);
        }

        return new BehaviorInfo(
            typeName: typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            shortName: typeSymbol.Name,
            behaviorId: id,
            isAbstract: isAbstract,
            isStatic: isStatic,
            implementsInterface: implementsInterface,
            location: location,
            topology: topology);
    }

    private static bool ImplementsIAppBehavior(INamedTypeSymbol typeSymbol)
    {
        return typeSymbol.AllInterfaces.Any(static i =>
            i.OriginalDefinition.ToDisplayString() ==
            "Cephalon.Abstractions.Behaviors.IAppBehavior<TIn, TOut>");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ConfigureTopology static analysis
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Attempts to extract topology declarations from a static <c>ConfigureTopology</c>
    /// method using compile-time syntax analysis of fluent builder calls.
    /// </summary>
    private static TopologyInfo? ExtractTopologyFromConfigureMethod(ClassDeclarationSyntax classDecl)
    {
        // Find the ConfigureTopology method
        var method = classDecl.Members
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(m =>
                m.Identifier.Text == "ConfigureTopology" &&
                m.Modifiers.Any(SyntaxKind.StaticKeyword) &&
                m.Modifiers.Any(SyntaxKind.PublicKeyword));

        if (method?.Body is null && method?.ExpressionBody is null)
            return null;

        // Collect all method call names from the method body
        var invocations = method.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .ToArray();

        if (invocations.Length == 0)
            return null;

        string? pattern = null;
        var transports = new List<string>();
        bool outboxEnabled = false;
        bool inboxEnabled = false;
        bool eventSourcingEnabled = false;
        bool hasComplexLogic = false;

        // Check for any control flow that makes static analysis unreliable
        if (method.DescendantNodes().Any(n =>
            n is IfStatementSyntax ||
            n is ForStatementSyntax ||
            n is ForEachStatementSyntax ||
            n is WhileStatementSyntax ||
            n is SwitchStatementSyntax ||
            n is ConditionalExpressionSyntax))
        {
            hasComplexLogic = true;
        }

        if (hasComplexLogic)
            return null; // Fall back to runtime

        foreach (var invocation in invocations)
        {
            var methodName = GetMethodName(invocation);
            if (methodName is null) continue;

            // Pattern methods
            switch (methodName)
            {
                case "AsDirect": pattern = "direct"; break;
                case "AsCqrs": pattern = "cqrs"; break;
                case "AsEventDriven": pattern = "event-driven"; break;
                case "AsSaga": pattern = "saga-step"; break;
                case "AsProcessManager": pattern = "process-manager"; break;

                // Transport methods
                case "ViaHttpRest": transports.Add("http.rest"); break;
                case "ViaHttpJsonRpc": transports.Add("http.jsonrpc"); break;
                case "ViaHttpGraphQl": transports.Add("http.graphql"); break;
                case "ViaHttpGraphQlSse": transports.Add("http.graphql-sse"); break;
                case "ViaHttpGraphQlWs": transports.Add("http.graphql-ws"); break;
                case "ViaHttpSse": transports.Add("http.sse"); break;
                case "ViaWebSocket": transports.Add("http.ws"); break;
                case "ViaRabbitMq": transports.Add("rabbitmq"); break;
                case "ViaKafka": transports.Add("kafka"); break;
                case "ViaInMemory": transports.Add("in-memory"); break;
                case "ViaGrpc": transports.Add("grpc"); break;

                // WithOptions — analyze the lambda body
                case "WithOptions":
                    ExtractOptionsFromInvocation(invocation, ref outboxEnabled, ref inboxEnabled, ref eventSourcingEnabled);
                    break;
            }
        }

        transports.Sort(System.StringComparer.OrdinalIgnoreCase);

        return new TopologyInfo(
            pattern: pattern ?? "direct",
            transports: transports.Distinct().ToArray(),
            outboxEnabled: outboxEnabled,
            inboxEnabled: inboxEnabled,
            eventSourcingEnabled: eventSourcingEnabled);
    }

    /// <summary>
    /// Extracts the method name from a (possibly chained) invocation expression.
    /// </summary>
    private static string? GetMethodName(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            _ => null
        };
    }

    /// <summary>
    /// Analyzes a <c>WithOptions(opts =&gt; ...)</c> call to extract boolean property assignments.
    /// </summary>
    private static void ExtractOptionsFromInvocation(
        InvocationExpressionSyntax invocation,
        ref bool outboxEnabled,
        ref bool inboxEnabled,
        ref bool eventSourcingEnabled)
    {
        // Find the lambda argument: WithOptions(opts => ...) or WithOptions(opts => { ... })
        var arg = invocation.ArgumentList.Arguments.FirstOrDefault();
        if (arg?.Expression is not (SimpleLambdaExpressionSyntax or ParenthesizedLambdaExpressionSyntax))
            return;

        // Get all assignment expressions inside the lambda
        var assignments = arg.Expression.DescendantNodes()
            .OfType<AssignmentExpressionSyntax>()
            .Where(a => a.IsKind(SyntaxKind.SimpleAssignmentExpression));

        foreach (var assignment in assignments)
        {
            // Match pattern: opts.PropertyName = true/false
            if (assignment.Left is MemberAccessExpressionSyntax propAccess &&
                assignment.Right is LiteralExpressionSyntax literal)
            {
                var propName = propAccess.Name.Identifier.Text;
                var isTrue = literal.IsKind(SyntaxKind.TrueLiteralExpression);

                switch (propName)
                {
                    case "OutboxEnabled": outboxEnabled = isTrue; break;
                    case "InboxEnabled": inboxEnabled = isTrue; break;
                    case "EventSourcingEnabled": eventSourcingEnabled = isTrue; break;
                }
            }
        }
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
    // Code generation — Registration Hints (backward compat)
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

    // ─────────────────────────────────────────────────────────────────────────
    // Code generation — Auto Registration (zero-reflection)
    // ─────────────────────────────────────────────────────────────────────────

    private static SourceText BuildAutoRegistration(ImmutableArray<BehaviorInfo?> infos)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("// Generated by Cephalon.Behaviors.SourceGen — do not edit.");
        sb.AppendLine("// This class enables zero-reflection behavior registration at startup.");
        sb.AppendLine();

        // Assembly-level marker attribute
        sb.AppendLine("[assembly: global::Cephalon.Abstractions.Behaviors.ContainsBehaviors(");
        sb.AppendLine("    typeof(global::Cephalon.Behaviors.Generated.BehaviorAutoRegistration))]");
        sb.AppendLine();
        sb.AppendLine("namespace Cephalon.Behaviors.Generated;");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Source-generated zero-reflection behavior registration.");
        sb.AppendLine("/// Called by the engine at startup instead of scanning assembly types via reflection.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");
        sb.AppendLine("internal static class BehaviorAutoRegistration");
        sb.AppendLine("{");

        // ── Register method ──
        sb.AppendLine("    /// <summary>Registers all behaviors in this assembly into DI and the type registry.</summary>");
        sb.AppendLine("    internal static void Register(");
        sb.AppendLine("        global::Microsoft.Extensions.DependencyInjection.IServiceCollection services,");
        sb.AppendLine("        global::Cephalon.Behaviors.Services.IBehaviorTypeRegistry typeRegistry)");
        sb.AppendLine("    {");

        foreach (var info in infos)
        {
            if (info is not { IsValid: true }) continue;
            var fqn = info.TypeName; // already global:: prefixed from FullyQualifiedFormat
            var id = EscapeString(info.BehaviorId);
            sb.AppendLine();
            sb.AppendLine($"        // {info.ShortName} → \"{id}\"");
            sb.AppendLine($"        if (!typeRegistry.TryGetType(\"{id}\", out _))");
            sb.AppendLine("        {");
            sb.AppendLine($"            global::Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.TryAddTransient(services, typeof({fqn}));");
            sb.AppendLine($"            typeRegistry.Register(\"{id}\", typeof({fqn}));");
            sb.AppendLine("        }");
        }

        sb.AppendLine("    }");
        sb.AppendLine();

        // ── GetTopologyDescriptors method ──
        sb.AppendLine("    /// <summary>Returns pre-built topology descriptors for behaviors with compile-time topology.</summary>");
        sb.AppendLine("    internal static global::System.Collections.Generic.IReadOnlyList<global::Cephalon.Abstractions.Behaviors.BehaviorTopologyDescriptor> GetTopologyDescriptors()");
        sb.AppendLine("    {");
        sb.AppendLine("        return new global::Cephalon.Abstractions.Behaviors.BehaviorTopologyDescriptor[]");
        sb.AppendLine("        {");

        foreach (var info in infos)
        {
            if (info is not { IsValid: true, Topology: not null }) continue;
            var t = info.Topology;
            var id = EscapeString(info.BehaviorId);

            sb.Append($"            new global::Cephalon.Abstractions.Behaviors.BehaviorTopologyDescriptor(");
            sb.Append($"\"{id}\", ");
            sb.Append($"\"{EscapeString(t.Pattern)}\", ");

            // Transport IDs array
            sb.Append("new string[] { ");
            sb.Append(string.Join(", ", t.Transports.Select(tr => $"\"{EscapeString(tr)}\"")));
            sb.Append(" }");

            // Optional named parameters for feature flags
            if (t.InboxEnabled) sb.Append(", inboxEnabled: true");
            if (t.OutboxEnabled) sb.Append(", outboxEnabled: true");
            if (t.EventSourcingEnabled) sb.Append(", eventSourcingEnabled: true");

            sb.AppendLine("),");
        }

        sb.AppendLine("        };");
        sb.AppendLine("    }");

        // ── GetBehaviorIdsWithoutTopology method ──
        // For behaviors that don't have ConfigureTopology or have complex logic,
        // the runtime still needs to invoke their static method via reflection.
        var behaviorsWithoutTopology = infos
            .Where(i => i is { IsValid: true, Topology: null })
            .ToArray();

        sb.AppendLine();
        sb.AppendLine("    /// <summary>Returns behavior IDs that need runtime topology resolution (no compile-time topology).</summary>");
        sb.AppendLine("    internal static global::System.Collections.Generic.IReadOnlyList<(string Id, global::System.Type Type)> GetBehaviorsNeedingRuntimeTopology()");
        sb.AppendLine("    {");

        if (behaviorsWithoutTopology.Length == 0)
        {
            sb.AppendLine("        return global::System.Array.Empty<(string, global::System.Type)>();");
        }
        else
        {
            sb.AppendLine("        return new (string, global::System.Type)[]");
            sb.AppendLine("        {");
            foreach (var info in behaviorsWithoutTopology)
            {
                if (info is null) continue;
                sb.AppendLine($"            (\"{EscapeString(info.BehaviorId)}\", typeof({info.TypeName})),");
            }
            sb.AppendLine("        };");
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return SourceText.From(sb.ToString(), Encoding.UTF8);
    }

    private static string EscapeString(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"");

    // ─────────────────────────────────────────────────────────────────────────
    // Data models
    // ─────────────────────────────────────────────────────────────────────────

    private sealed class TopologyInfo
    {
        public TopologyInfo(string pattern, string[] transports,
            bool outboxEnabled, bool inboxEnabled, bool eventSourcingEnabled)
        {
            Pattern = pattern;
            Transports = transports;
            OutboxEnabled = outboxEnabled;
            InboxEnabled = inboxEnabled;
            EventSourcingEnabled = eventSourcingEnabled;
        }

        public string Pattern { get; }
        public string[] Transports { get; }
        public bool OutboxEnabled { get; }
        public bool InboxEnabled { get; }
        public bool EventSourcingEnabled { get; }
    }

    private sealed class BehaviorInfo
    {
        public BehaviorInfo(
            string typeName,
            string shortName,
            string behaviorId,
            bool isAbstract,
            bool isStatic,
            bool implementsInterface,
            Location location,
            TopologyInfo? topology)
        {
            TypeName = typeName;
            ShortName = shortName;
            BehaviorId = behaviorId;
            IsAbstract = isAbstract;
            IsStatic = isStatic;
            ImplementsInterface = implementsInterface;
            Location = location;
            Topology = topology;
        }

        public string TypeName { get; }
        public string ShortName { get; }
        public string BehaviorId { get; }
        public bool IsAbstract { get; }
        public bool IsStatic { get; }
        public bool ImplementsInterface { get; }
        public Location Location { get; }
        public TopologyInfo? Topology { get; }

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
