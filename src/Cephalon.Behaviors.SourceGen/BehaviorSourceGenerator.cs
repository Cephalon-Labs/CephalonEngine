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
/// Diagnostic IDs: ABT-010 through ABT-025.
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

    /// <summary>ABT-014: REST is module-owned only and must not be declared in behavior topology.</summary>
    public static readonly DiagnosticDescriptor Abt014RestMustBeModuleOwned = new(
        id: "ABT0014",
        title: "REST must be mapped by a module",
        messageFormat: "'{0}' declares REST in behavior topology; remove 'http.rest' or ViaHttpRest(...) and map REST in a module with RestBehaviorModuleBase.ConfigureRestBehaviors(...) or, for advanced manual routes, MapAdditionalEndpoints(...) plus MapBehaviorRestGroup(...)",
        category: "Cephalon.Behaviors",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Cephalon keeps public REST module-owned. Behaviors must not declare http.rest in [BehaviorAllowedTransports] or ConfigureTopology(...). Map REST endpoints through a module's ConfigureRestBehaviors(...) DSL, or through manual module-owned helper routes only when deliberately using the advanced escape hatch.",
        helpLinkUri: HelpLink);

    /// <summary>ABT-015: [BehaviorRestProfile] must select a supported REST method.</summary>
    public static readonly DiagnosticDescriptor Abt015RestProfileMethodMustBeSpecified = new(
        id: "ABT0015",
        title: "REST profile method must be specified",
        messageFormat: "'{0}' declares [BehaviorRestProfile] without a supported REST method",
        category: "Cephalon.Behaviors",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Behavior-authored REST profile metadata must select one supported candidate REST method so future generated or descriptor-backed module projections stay deterministic.",
        helpLinkUri: HelpLink);

    /// <summary>ABT-016: [BehaviorRestProfile] relative pattern must not be empty.</summary>
    public static readonly DiagnosticDescriptor Abt016RestProfilePatternMustNotBeEmpty = new(
        id: "ABT0016",
        title: "REST profile relative pattern must not be empty",
        messageFormat: "'{0}' declares [BehaviorRestProfile] with an empty relative pattern",
        category: "Cephalon.Behaviors",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Behavior-authored REST profile metadata must describe a non-empty route pattern relative to a future owning REST group.",
        helpLinkUri: HelpLink);

    /// <summary>ABT-017: [BehaviorRestProfile] API version must be positive when specified.</summary>
    public static readonly DiagnosticDescriptor Abt017RestProfileVersionMustBePositive = new(
        id: "ABT0017",
        title: "REST profile API version must be greater than zero",
        messageFormat: "'{0}' declares [BehaviorRestProfile] with ApiVersionMajor '{1}', but candidate REST profile versions must be greater than zero",
        category: "Cephalon.Behaviors",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Behavior-authored REST profile metadata may omit the candidate API version, but when specified it must be a positive major version.",
        helpLinkUri: HelpLink);

    /// <summary>ABT-018: [BehaviorRestProfile] relative pattern must start with '/'.</summary>
    public static readonly DiagnosticDescriptor Abt018RestProfilePatternMustStartWithSlash = new(
        id: "ABT0018",
        title: "REST profile relative pattern must start with '/'",
        messageFormat: "'{0}' declares [BehaviorRestProfile] with relative pattern '{1}', but REST profile patterns must start with '/'",
        category: "Cephalon.Behaviors",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Behavior-authored REST profile metadata should use the same leading-slash relative pattern shape as the module-owned REST DSL so future projection material stays unambiguous.",
        helpLinkUri: HelpLink);

    /// <summary>ABT-019: [BehaviorRestBinding] target property name must not be empty.</summary>
    public static readonly DiagnosticDescriptor Abt019RestBindingPropertyNameMustNotBeEmpty = new(
        id: "ABT0019",
        title: "REST binding target property name must not be empty",
        messageFormat: "'{0}' declares [BehaviorRestBinding] without a target input property name",
        category: "Cephalon.Behaviors",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Behavior-authored REST binding metadata must identify the input property that the owning module-owned REST projection should populate.",
        helpLinkUri: HelpLink);

    /// <summary>ABT-020: [BehaviorRestBinding] source must select a supported binding source.</summary>
    public static readonly DiagnosticDescriptor Abt020RestBindingSourceMustBeSupported = new(
        id: "ABT0020",
        title: "REST binding source must be supported",
        messageFormat: "'{0}' declares [BehaviorRestBinding] for input property '{1}' without a supported binding source",
        category: "Cephalon.Behaviors",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Behavior-authored REST binding metadata must use one supported source such as route, query, header, or body.",
        helpLinkUri: HelpLink);

    /// <summary>ABT-021: Explicit REST bindings require an object input with public properties.</summary>
    public static readonly DiagnosticDescriptor Abt021RestBindingsRequireObjectInput = new(
        id: "ABT0021",
        title: "REST bindings require an object input with public properties",
        messageFormat: "'{0}' declares [BehaviorRestBinding] metadata, but input type '{1}' must expose public properties for explicit REST binding",
        category: "Cephalon.Behaviors",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Explicit REST binding metadata can only target object inputs that expose public readable properties.",
        helpLinkUri: HelpLink);

    /// <summary>ABT-022: [BehaviorRestBinding] property must exist on the behavior input.</summary>
    public static readonly DiagnosticDescriptor Abt022RestBindingPropertyMustExistOnInput = new(
        id: "ABT0022",
        title: "REST binding property must exist on the behavior input",
        messageFormat: "'{0}' declares [BehaviorRestBinding] for input property '{1}', but '{2}' does not expose a matching public property",
        category: "Cephalon.Behaviors",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Behavior-authored REST binding metadata must target a real public input property so module-owned REST projections remain deterministic.",
        helpLinkUri: HelpLink);

    /// <summary>ABT-023: [BehaviorRestBinding] must not target the same property twice.</summary>
    public static readonly DiagnosticDescriptor Abt023RestBindingPropertyMustNotBeDuplicated = new(
        id: "ABT0023",
        title: "REST binding property must not be declared more than once",
        messageFormat: "'{0}' declares more than one [BehaviorRestBinding] for input property '{1}'",
        category: "Cephalon.Behaviors",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Each behavior input property may have at most one explicit REST binding descriptor.",
        helpLinkUri: HelpLink);

    /// <summary>ABT-024: GET/DELETE REST profiles must not bind from the request body.</summary>
    public static readonly DiagnosticDescriptor Abt024RestBodyBindingMustUseBodyCapableMethod = new(
        id: "ABT0024",
        title: "REST body bindings require a body-capable method",
        messageFormat: "'{0}' declares a body [BehaviorRestBinding] for input property '{1}', but REST method '{2}' does not accept a request body",
        category: "Cephalon.Behaviors",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Body bindings are only valid for body-capable REST methods so the generated profile metadata matches the module-owned REST runtime contract.",
        helpLinkUri: HelpLink);

    /// <summary>ABT-025: Route bindings must target placeholders declared in the profile pattern.</summary>
    public static readonly DiagnosticDescriptor Abt025RestRouteBindingMustMatchRoutePlaceholder = new(
        id: "ABT0025",
        title: "REST route bindings must match declared route placeholders",
        messageFormat: "'{0}' declares a route [BehaviorRestBinding] for input property '{1}' using placeholder '{2}', but route pattern '{3}' does not declare that placeholder",
        category: "Cephalon.Behaviors",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Explicit route bindings must name placeholders that are actually present in the profile route pattern so module-owned projections do not carry unreachable route intent.",
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

        var inputType = ResolveBehaviorInputType(typeSymbol);
        var implementsInterface = inputType is not null;
        var hasRestTransportAttribute = DeclaresRestTransportAttribute(typeSymbol);
        var restProfile = ExtractRestProfile(typeSymbol);

        var location = ctx.TargetNode.GetLocation();

        // Extract topology from static ConfigureTopology method if present
        TopologyInfo? topology = null;
        var hasConfigureTopologyRestTransport = false;
        if (implementsInterface && !isAbstract && !isStatic && ctx.TargetNode is ClassDeclarationSyntax classDecl)
        {
            hasConfigureTopologyRestTransport = DeclaresRestTransportInConfigureMethod(classDecl);
            topology = ExtractTopologyFromConfigureMethod(classDecl);
        }

        return new BehaviorInfo(
            typeName: typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            shortName: typeSymbol.Name,
            behaviorId: id,
            isAbstract: isAbstract,
            isStatic: isStatic,
            implementsInterface: implementsInterface,
            inputType: inputType,
            location: location,
            topology: topology,
            restProfile: restProfile,
            hasRestTransportAttribute: hasRestTransportAttribute,
            hasConfigureTopologyRestTransport: hasConfigureTopologyRestTransport);
    }

    private static InputTypeInfo? ResolveBehaviorInputType(INamedTypeSymbol typeSymbol)
    {
        var behaviorInterface = typeSymbol.AllInterfaces.FirstOrDefault(static i =>
            i.OriginalDefinition.ToDisplayString() ==
            "Cephalon.Abstractions.Behaviors.IAppBehavior<TIn, TOut>");
        if (behaviorInterface is null)
        {
            return null;
        }

        var inputType = UnwrapNullable(behaviorInterface.TypeArguments[0]);
        var publicProperties = inputType
            .GetMembers()
            .OfType<IPropertySymbol>()
            .Where(static property =>
                !property.IsStatic &&
                property.GetMethod is not null &&
                property.DeclaredAccessibility == Accessibility.Public)
            .Select(static property => property.Name)
            .ToImmutableDictionary(static property => property, static property => property, StringComparer.OrdinalIgnoreCase);

        return new InputTypeInfo(
            inputType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            IsSimpleInputType(inputType),
            publicProperties);
    }

    private static bool DeclaresRestTransportAttribute(INamedTypeSymbol typeSymbol)
    {
        foreach (var attribute in typeSymbol.GetAttributes())
        {
            if (!string.Equals(
                    attribute.AttributeClass?.ToDisplayString(),
                    "Cephalon.Abstractions.Behaviors.BehaviorAllowedTransportsAttribute",
                    StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var argument in attribute.ConstructorArguments)
            {
                if (argument.Kind == TypedConstantKind.Array)
                {
                    foreach (var value in argument.Values)
                    {
                        if (string.Equals(value.Value as string, "http.rest", StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }

                    continue;
                }

                if (string.Equals(argument.Value as string, "http.rest", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static RestProfileInfo? ExtractRestProfile(INamedTypeSymbol typeSymbol)
    {
        foreach (var attribute in typeSymbol.GetAttributes())
        {
            if (!string.Equals(
                    attribute.AttributeClass?.ToDisplayString(),
                    "Cephalon.Behaviors.Http.Abstractions.BehaviorRestProfileAttribute",
                    StringComparison.Ordinal))
            {
                continue;
            }

            var methodName = attribute.ConstructorArguments.Length > 0
                ? ResolveEnumMemberName(attribute.ConstructorArguments[0])
                : null;
            var relativePattern = attribute.ConstructorArguments.Length > 1
                ? attribute.ConstructorArguments[1].Value as string ?? string.Empty
                : string.Empty;

            var hasApiVersionMajor = false;
            var apiVersionMajor = 0;
            foreach (var namedArgument in attribute.NamedArguments)
            {
                if (!string.Equals(namedArgument.Key, "ApiVersionMajor", StringComparison.Ordinal))
                {
                    continue;
                }

                hasApiVersionMajor = true;
                apiVersionMajor = namedArgument.Value.Value is null
                    ? 0
                    : Convert.ToInt32(namedArgument.Value.Value, System.Globalization.CultureInfo.InvariantCulture);
                break;
            }

            return new RestProfileInfo(
                methodName,
                relativePattern,
                hasApiVersionMajor,
                apiVersionMajor,
                ExtractRestBindings(typeSymbol));
        }

        return null;
    }

    private static List<RestBindingInfo> ExtractRestBindings(INamedTypeSymbol typeSymbol)
    {
        var bindings = new List<RestBindingInfo>();

        foreach (var attribute in typeSymbol.GetAttributes())
        {
            if (!string.Equals(
                    attribute.AttributeClass?.ToDisplayString(),
                    "Cephalon.Behaviors.Http.Abstractions.BehaviorRestBindingAttribute",
                    StringComparison.Ordinal))
            {
                continue;
            }

            var propertyName = attribute.ConstructorArguments.Length > 0
                ? attribute.ConstructorArguments[0].Value as string ?? string.Empty
                : string.Empty;
            var sourceName = attribute.ConstructorArguments.Length > 1
                ? ResolveEnumMemberName(attribute.ConstructorArguments[1])
                : null;
            string? name = null;

            foreach (var namedArgument in attribute.NamedArguments)
            {
                if (!string.Equals(namedArgument.Key, "Name", StringComparison.Ordinal))
                {
                    continue;
                }

                name = namedArgument.Value.Value as string;
                break;
            }

            bindings.Add(new RestBindingInfo(propertyName, sourceName, name));
        }

        return bindings;
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
        string? apiSurfaceGroupPath = null;
        string? apiSurfaceOperationPath = null;
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

            if (string.Equals(methodName, "ViaHttpRest", StringComparison.Ordinal))
            {
                return null;
            }

            // Pattern methods
            switch (methodName)
            {
                case "AsDirect": pattern = "direct"; break;
                case "AsCqrs": pattern = "cqrs"; break;
                case "AsEventDriven": pattern = "event-driven"; break;
                case "AsSaga": pattern = "saga-step"; break;
                case "AsProcessManager": pattern = "process-manager"; break;

                // Transport methods
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
                case "WithApiSurface":
                    if (!TryExtractApiSurfaceFromInvocation(invocation, out apiSurfaceGroupPath, out apiSurfaceOperationPath))
                    {
                        return null;
                    }
                    break;

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
            eventSourcingEnabled: eventSourcingEnabled,
            apiSurfaceGroupPath: apiSurfaceGroupPath,
            apiSurfaceOperationPath: apiSurfaceOperationPath);
    }

    private static bool DeclaresRestTransportInConfigureMethod(ClassDeclarationSyntax classDecl)
    {
        var method = classDecl.Members
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(m =>
                m.Identifier.Text == "ConfigureTopology" &&
                m.Modifiers.Any(SyntaxKind.StaticKeyword) &&
                m.Modifiers.Any(SyntaxKind.PublicKeyword));

        if (method is null)
        {
            return false;
        }

        return method.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Select(GetMethodName)
            .Any(static methodName => string.Equals(methodName, "ViaHttpRest", StringComparison.Ordinal));
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

    private static bool TryExtractApiSurfaceFromInvocation(
        InvocationExpressionSyntax invocation,
        out string? groupPath,
        out string? operationPath)
    {
        groupPath = null;
        operationPath = null;

        if (invocation.ArgumentList.Arguments.Count < 2)
        {
            return false;
        }

        if (!TryGetStringLiteral(invocation.ArgumentList.Arguments[0].Expression, out groupPath) ||
            !TryGetStringLiteral(invocation.ArgumentList.Arguments[1].Expression, out operationPath))
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(operationPath);
    }

    private static bool TryGetStringLiteral(ExpressionSyntax expression, out string? value)
    {
        value = expression switch
        {
            LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.StringLiteralExpression)
                => literal.Token.ValueText,
            _ => null
        };

        return value is not null;
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

        if (info.HasRestTransportAttribute || info.HasConfigureTopologyRestTransport)
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                Abt014RestMustBeModuleOwned,
                info.Location,
                info.ShortName));
        }

        if (info.RestProfile is null)
        {
            return;
        }

        if (!IsSupportedRestMethod(info.RestProfile.MethodName))
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                Abt015RestProfileMethodMustBeSpecified,
                info.Location,
                info.ShortName));
        }

        if (string.IsNullOrWhiteSpace(info.RestProfile.RelativePattern))
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                Abt016RestProfilePatternMustNotBeEmpty,
                info.Location,
                info.ShortName));
        }
        else if (!info.RestProfile.RelativePattern.Trim().StartsWith("/", StringComparison.Ordinal))
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                Abt018RestProfilePatternMustStartWithSlash,
                info.Location,
                info.ShortName,
                info.RestProfile.RelativePattern));
        }

        if (info.RestProfile.HasApiVersionMajor && info.RestProfile.ApiVersionMajor <= 0)
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                Abt017RestProfileVersionMustBePositive,
                info.Location,
                info.ShortName,
                info.RestProfile.ApiVersionMajor));
        }

        foreach (var issue in ValidateRestBindingMetadata(info))
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                issue.Descriptor,
                info.Location,
                issue.MessageArguments));
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
            var apiSurfaceOperationPath = t.ApiSurfaceOperationPath;
            if (!string.IsNullOrWhiteSpace(apiSurfaceOperationPath))
            {
                var apiSurfaceGroupPath = t.ApiSurfaceGroupPath ?? string.Empty;
                sb.Append(", apiSurface: new global::Cephalon.Abstractions.Behaviors.BehaviorApiSurfaceDescriptor(");
                sb.Append($"\"{EscapeString(apiSurfaceGroupPath)}\", ");
                sb.Append($"\"{EscapeString(apiSurfaceOperationPath!)}\")");
            }

            sb.AppendLine("),");
        }

        sb.AppendLine("        };");
        sb.AppendLine("    }");

        var behaviorsWithRestProfiles = infos
            .Where(static info => info is { IsValid: true, HasValidRestProfile: true })
            .ToArray();

        if (behaviorsWithRestProfiles.Length > 0)
        {
            sb.AppendLine();
            sb.AppendLine("    /// <summary>Returns metadata-only REST profile hints discovered at compile time.</summary>");
            sb.AppendLine("    internal static global::System.Collections.Generic.IReadOnlyList<global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestProfileDescriptor> GetRestProfiles()");
            sb.AppendLine("    {");
            sb.AppendLine("        return new global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestProfileDescriptor[]");
            sb.AppendLine("        {");

            foreach (var info in behaviorsWithRestProfiles)
            {
                if (info?.RestProfile is null ||
                    !IsSupportedRestMethod(info.RestProfile.MethodName))
                {
                    continue;
                }

                sb.Append("            new global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestProfileDescriptor(");
                sb.Append($"\"{EscapeString(info.BehaviorId)}\", ");
                sb.Append($"global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestMethod.{info.RestProfile.MethodName}, ");
                sb.Append($"\"{EscapeString(info.RestProfile.RelativePattern.Trim())}\", ");
                sb.Append(info.RestProfile.HasApiVersionMajor
                    ? info.RestProfile.ApiVersionMajor.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    : "null");
                if (info.RestProfile.Bindings.Count > 0)
                {
                    sb.Append(", new global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestBindingDescriptor[] { ");
                    sb.Append(string.Join(
                        ", ",
                        info.RestProfile.Bindings.Select(static binding =>
                            $"new global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestBindingDescriptor(\"{EscapeString(binding.PropertyName)}\", global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestBindingSource.{binding.SourceName}, {(string.IsNullOrWhiteSpace(binding.Name) ? "null" : $"\"{EscapeString(binding.Name!)}\"")})")));
                    sb.Append(" }");
                }
                sb.AppendLine("),");
            }

            sb.AppendLine("        };");
            sb.AppendLine("    }");
        }

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

    private static string? ResolveEnumMemberName(TypedConstant constant)
    {
        if (constant.Type is not INamedTypeSymbol { TypeKind: TypeKind.Enum } enumType ||
            constant.Value is null)
        {
            return null;
        }

        foreach (var member in enumType.GetMembers().OfType<IFieldSymbol>())
        {
            if (!member.HasConstantValue || member.ConstantValue is null)
            {
                continue;
            }

            if (Equals(member.ConstantValue, constant.Value))
            {
                return member.Name;
            }
        }

        return null;
    }

    private static bool IsSupportedRestMethod(string? methodName)
    {
        return methodName is "Get" or "Post" or "Put" or "Patch" or "Delete";
    }

    private static bool MethodAcceptsBody(string? methodName)
    {
        return methodName is "Post" or "Put" or "Patch";
    }

    private static bool IsSupportedRestBindingSource(string? sourceName)
    {
        return sourceName is "Route" or "Query" or "Header" or "Body";
    }

    private static ImmutableArray<RestBindingValidationIssue> ValidateRestBindingMetadata(BehaviorInfo info)
    {
        if (info.RestProfile is null || info.RestProfile.Bindings.Count == 0)
        {
            return [];
        }

        if (info.InputType is null)
        {
            return [];
        }

        if (info.InputType.IsSimple || info.InputType.PublicProperties.Count == 0)
        {
            return
            [
                new RestBindingValidationIssue(
                    Abt021RestBindingsRequireObjectInput,
                    info.ShortName,
                    info.InputType.DisplayName)
            ];
        }

        var issues = ImmutableArray.CreateBuilder<RestBindingValidationIssue>();
        var routeParameters = HasValidRoutePattern(info.RestProfile.RelativePattern)
            ? ExtractRouteParameterNames(info.RestProfile.RelativePattern)
            : null;
        var supportsBody = MethodAcceptsBody(info.RestProfile.MethodName);
        var hasSupportedMethod = IsSupportedRestMethod(info.RestProfile.MethodName);
        var seenProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var binding in info.RestProfile.Bindings)
        {
            if (string.IsNullOrWhiteSpace(binding.PropertyName))
            {
                issues.Add(new RestBindingValidationIssue(
                    Abt019RestBindingPropertyNameMustNotBeEmpty,
                    info.ShortName));
                continue;
            }

            var propertyName = binding.PropertyName.Trim();
            var propertyExists = info.InputType.PublicProperties.TryGetValue(propertyName, out var canonicalPropertyName);
            var effectivePropertyName = propertyExists ? canonicalPropertyName! : propertyName;

            if (!propertyExists)
            {
                issues.Add(new RestBindingValidationIssue(
                    Abt022RestBindingPropertyMustExistOnInput,
                    info.ShortName,
                    propertyName,
                    info.InputType.DisplayName));
            }

            if (propertyExists && !seenProperties.Add(canonicalPropertyName!))
            {
                issues.Add(new RestBindingValidationIssue(
                    Abt023RestBindingPropertyMustNotBeDuplicated,
                    info.ShortName,
                    canonicalPropertyName!));
            }

            if (!IsSupportedRestBindingSource(binding.SourceName))
            {
                issues.Add(new RestBindingValidationIssue(
                    Abt020RestBindingSourceMustBeSupported,
                    info.ShortName,
                    effectivePropertyName));
                continue;
            }

            if (hasSupportedMethod &&
                string.Equals(binding.SourceName, "Body", StringComparison.Ordinal) &&
                !supportsBody)
            {
                issues.Add(new RestBindingValidationIssue(
                    Abt024RestBodyBindingMustUseBodyCapableMethod,
                    info.ShortName,
                    effectivePropertyName,
                    info.RestProfile.MethodName!));
            }

            if (routeParameters is not null &&
                string.Equals(binding.SourceName, "Route", StringComparison.Ordinal))
            {
                var placeholderName = string.IsNullOrWhiteSpace(binding.Name)
                    ? effectivePropertyName
                    : binding.Name!.Trim();
                if (!routeParameters.Contains(placeholderName))
                {
                    issues.Add(new RestBindingValidationIssue(
                        Abt025RestRouteBindingMustMatchRoutePlaceholder,
                        info.ShortName,
                        effectivePropertyName,
                        placeholderName,
                        info.RestProfile.RelativePattern.Trim()));
                }
            }
        }

        return issues.ToImmutable();
    }

    private static bool HasValidRoutePattern(string pattern)
    {
        return !string.IsNullOrWhiteSpace(pattern) &&
               pattern.Trim().StartsWith("/", StringComparison.Ordinal);
    }

    private static HashSet<string> ExtractRouteParameterNames(string pattern)
    {
        var parameters = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return parameters;
        }

        var current = pattern.Trim();
        var index = 0;
        while (index < current.Length)
        {
            var openBrace = current.IndexOf('{', index);
            if (openBrace < 0)
            {
                break;
            }

            var closeBrace = current.IndexOf('}', openBrace + 1);
            if (closeBrace < 0)
            {
                break;
            }

            var token = current.Substring(openBrace + 1, closeBrace - openBrace - 1);
            var parameterName = NormalizeRouteParameterToken(token);
            if (!string.IsNullOrWhiteSpace(parameterName))
            {
                parameters.Add(parameterName!);
            }

            index = closeBrace + 1;
        }

        return parameters;
    }

    private static string? NormalizeRouteParameterToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var normalized = token.Trim();
        if (normalized.StartsWith("**", StringComparison.Ordinal))
        {
            normalized = normalized.Substring(2);
        }
        else if (normalized.StartsWith("*", StringComparison.Ordinal))
        {
            normalized = normalized.Substring(1);
        }

        var separatorIndex = normalized.IndexOfAny([':', '=', '?']);
        if (separatorIndex >= 0)
        {
            normalized = normalized.Substring(0, separatorIndex);
        }

        normalized = normalized.Trim();
        return normalized.Length == 0 ? null : normalized;
    }

    private static ITypeSymbol UnwrapNullable(ITypeSymbol typeSymbol)
    {
        return typeSymbol is INamedTypeSymbol
        {
            OriginalDefinition.SpecialType: SpecialType.System_Nullable_T,
            TypeArguments.Length: 1
        } namedTypeSymbol
            ? namedTypeSymbol.TypeArguments[0]
            : typeSymbol;
    }

    private static bool IsSimpleInputType(ITypeSymbol inputType)
    {
        var type = UnwrapNullable(inputType);
        return type.TypeKind == TypeKind.Enum ||
               type.SpecialType is SpecialType.System_Boolean or
                   SpecialType.System_Byte or
                   SpecialType.System_SByte or
                   SpecialType.System_Int16 or
                   SpecialType.System_UInt16 or
                   SpecialType.System_Int32 or
                   SpecialType.System_UInt32 or
                   SpecialType.System_Int64 or
                   SpecialType.System_UInt64 or
                   SpecialType.System_Single or
                   SpecialType.System_Double or
                   SpecialType.System_Char or
                   SpecialType.System_String or
                   SpecialType.System_Decimal or
                   SpecialType.System_DateTime ||
               string.Equals(type.ToDisplayString(), "System.Guid", StringComparison.Ordinal) ||
               string.Equals(type.ToDisplayString(), "System.DateTimeOffset", StringComparison.Ordinal) ||
               string.Equals(type.ToDisplayString(), "System.DateOnly", StringComparison.Ordinal) ||
               string.Equals(type.ToDisplayString(), "System.TimeOnly", StringComparison.Ordinal);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Data models
    // ─────────────────────────────────────────────────────────────────────────

    private sealed class TopologyInfo
    {
        public TopologyInfo(
            string pattern,
            string[] transports,
            bool outboxEnabled,
            bool inboxEnabled,
            bool eventSourcingEnabled,
            string? apiSurfaceGroupPath,
            string? apiSurfaceOperationPath)
        {
            Pattern = pattern;
            Transports = transports;
            OutboxEnabled = outboxEnabled;
            InboxEnabled = inboxEnabled;
            EventSourcingEnabled = eventSourcingEnabled;
            ApiSurfaceGroupPath = apiSurfaceGroupPath;
            ApiSurfaceOperationPath = apiSurfaceOperationPath;
        }

        public string Pattern { get; }
        public string[] Transports { get; }
        public bool OutboxEnabled { get; }
        public bool InboxEnabled { get; }
        public bool EventSourcingEnabled { get; }
        public string? ApiSurfaceGroupPath { get; }
        public string? ApiSurfaceOperationPath { get; }
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
            InputTypeInfo? inputType,
            Location location,
            TopologyInfo? topology,
            RestProfileInfo? restProfile,
            bool hasRestTransportAttribute,
            bool hasConfigureTopologyRestTransport)
        {
            TypeName = typeName;
            ShortName = shortName;
            BehaviorId = behaviorId;
            IsAbstract = isAbstract;
            IsStatic = isStatic;
            ImplementsInterface = implementsInterface;
            InputType = inputType;
            Location = location;
            Topology = topology;
            RestProfile = restProfile;
            HasRestTransportAttribute = hasRestTransportAttribute;
            HasConfigureTopologyRestTransport = hasConfigureTopologyRestTransport;
        }

        public string TypeName { get; }
        public string ShortName { get; }
        public string BehaviorId { get; }
        public bool IsAbstract { get; }
        public bool IsStatic { get; }
        public bool ImplementsInterface { get; }
        public InputTypeInfo? InputType { get; }
        public Location Location { get; }
        public TopologyInfo? Topology { get; }
        public RestProfileInfo? RestProfile { get; }
        public bool HasRestTransportAttribute { get; }
        public bool HasConfigureTopologyRestTransport { get; }

        /// <summary>
        /// True when the behavior class passes all validation checks and should be included in generated output.
        /// </summary>
        public bool IsValid =>
            !string.IsNullOrWhiteSpace(BehaviorId) &&
            !IsAbstract &&
            !IsStatic &&
            ImplementsInterface;

        public bool HasValidRestProfile =>
            IsValid &&
            RestProfile is not null &&
            IsSupportedRestMethod(RestProfile.MethodName) &&
            !string.IsNullOrWhiteSpace(RestProfile.RelativePattern) &&
            RestProfile.RelativePattern.Trim().StartsWith("/", StringComparison.Ordinal) &&
            (!RestProfile.HasApiVersionMajor || RestProfile.ApiVersionMajor > 0) &&
            ValidateRestBindingMetadata(this).IsDefaultOrEmpty &&
            !HasRestTransportAttribute &&
            !HasConfigureTopologyRestTransport;
    }

    private sealed class RestProfileInfo
    {
        public RestProfileInfo(
            string? methodName,
            string relativePattern,
            bool hasApiVersionMajor,
            int apiVersionMajor,
            IReadOnlyList<RestBindingInfo> bindings)
        {
            MethodName = methodName;
            RelativePattern = relativePattern;
            HasApiVersionMajor = hasApiVersionMajor;
            ApiVersionMajor = apiVersionMajor;
            Bindings = bindings;
        }

        public string? MethodName { get; }
        public string RelativePattern { get; }
        public bool HasApiVersionMajor { get; }
        public int ApiVersionMajor { get; }
        public IReadOnlyList<RestBindingInfo> Bindings { get; }
    }

    private sealed class RestBindingInfo
    {
        public RestBindingInfo(string propertyName, string? sourceName, string? name)
        {
            PropertyName = propertyName;
            SourceName = sourceName;
            Name = name;
        }

        public string PropertyName { get; }
        public string? SourceName { get; }
        public string? Name { get; }
    }

    private sealed class InputTypeInfo
    {
        public InputTypeInfo(
            string displayName,
            bool isSimple,
            ImmutableDictionary<string, string> publicProperties)
        {
            DisplayName = displayName;
            IsSimple = isSimple;
            PublicProperties = publicProperties;
        }

        public string DisplayName { get; }
        public bool IsSimple { get; }
        public ImmutableDictionary<string, string> PublicProperties { get; }
    }

    private sealed class RestBindingValidationIssue
    {
        public RestBindingValidationIssue(DiagnosticDescriptor descriptor, params object?[] messageArguments)
        {
            Descriptor = descriptor;
            MessageArguments = messageArguments;
        }

        public DiagnosticDescriptor Descriptor { get; }
        public object?[] MessageArguments { get; }
    }
}
