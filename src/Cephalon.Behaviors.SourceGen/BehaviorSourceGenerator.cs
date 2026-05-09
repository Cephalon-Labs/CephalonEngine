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
/// Diagnostic IDs: ABT-010 through ABT-026.
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

    /// <summary>ABT-026: REST profile patterns must use valid route placeholder syntax.</summary>
    public static readonly DiagnosticDescriptor Abt026RestProfilePatternMustUseValidPlaceholderSyntax = new(
        id: "ABT0026",
        title: "REST profile route pattern must use valid placeholder syntax",
        messageFormat: "'{0}' declares [BehaviorRestProfile] with relative pattern '{1}', but the route pattern contains invalid placeholder syntax: {2}",
        category: "Cephalon.Behaviors",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Behavior-authored REST profile metadata must use balanced route-parameter placeholder syntax so module-owned shorthand stays build-time safe before runtime route parsing runs.",
        helpLinkUri: HelpLink);

    /// <summary>ABT-027: Preserved implicit query fallback requires at least one explicit binding.</summary>
    public static readonly DiagnosticDescriptor Abt027RestPreservedImplicitQueryFallbackRequiresExplicitBindings = new(
        id: "ABT0027",
        title: "Preserved implicit query fallback requires explicit bindings",
        messageFormat: "'{0}' declares [BehaviorRestProfile(PreserveImplicitQueryFallback = true)] without any [BehaviorRestBinding] metadata",
        category: "Cephalon.Behaviors",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Preserved implicit query fallback only has meaning when a REST profile already declares at least one explicit binding and needs the remaining unbound query-string surface to stay available.",
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
        var durableExecution = ResolveDurableExecutionInfo(typeSymbol);
        var idempotencyMode = ResolveBehaviorIdempotencyMode(typeSymbol);
        var hasRestTransportAttribute = DeclaresRestTransportAttribute(typeSymbol);
        var restProfile = ExtractRestProfile(typeSymbol);

        var location = ctx.TargetNode.GetLocation();

        // Extract topology from static ConfigureTopology method if present, otherwise
        // synthesize attribute-only topology at generation time.
        TopologyInfo? topology = null;
        var hasConfigureTopology = false;
        var hasTopologyDeclarations = false;
        var hasConfigureTopologyRestTransport = false;
        if (implementsInterface && !isAbstract && !isStatic && ctx.TargetNode is ClassDeclarationSyntax classDecl)
        {
            hasConfigureTopology = HasConfigureTopologyMethod(classDecl);
            hasTopologyDeclarations = hasConfigureTopology || DeclaresTopologyAttributes(typeSymbol);
            hasConfigureTopologyRestTransport = DeclaresRestTransportInConfigureMethod(classDecl);
            topology = hasConfigureTopology
                ? ExtractTopologyFromConfigureMethod(classDecl)
                : ExtractTopologyFromAllowlistAttributes(typeSymbol);
        }

        var sagaChoreographyRuntime = ResolveSagaChoreographyRuntimeInfo(
            typeSymbol,
            topology,
            ctx.SemanticModel.Compilation);

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
            durableExecution: durableExecution,
            idempotencyMode: idempotencyMode,
            sagaChoreographyRuntime: sagaChoreographyRuntime,
            restProfile: restProfile,
            hasRestTransportAttribute: hasRestTransportAttribute,
            hasConfigureTopology: hasConfigureTopology,
            hasTopologyDeclarations: hasTopologyDeclarations,
            hasConfigureTopologyRestTransport: hasConfigureTopologyRestTransport);
    }

    private static InputTypeInfo? ResolveBehaviorInputType(INamedTypeSymbol typeSymbol)
    {
        var behaviorInterface = ResolveBehaviorInterface(typeSymbol);
        if (behaviorInterface is null)
        {
            return null;
        }

        var inputType = UnwrapNullable(behaviorInterface.TypeArguments[0]);
        var isSimpleInput = IsSimpleInputType(inputType);
        var publicProperties = isSimpleInput
            ? ImmutableDictionary<string, InputPropertyInfo>.Empty.WithComparers(StringComparer.OrdinalIgnoreCase)
            : inputType
                .GetMembers()
                .OfType<IPropertySymbol>()
                .Where(static property =>
                    !property.IsStatic &&
                    property.GetMethod is not null &&
                    property.DeclaredAccessibility == Accessibility.Public)
                .Select(static property => new InputPropertyInfo(
                    property.Name,
                    ToTypeofTypeName(property.Type)))
                .ToImmutableDictionary(static property => property.Name, static property => property, StringComparer.OrdinalIgnoreCase);

        var outputType = behaviorInterface.TypeArguments[1];
        var returnsStructuredResult = TryResolveStructuredResultPayloadType(outputType, out var responseType);

        return new InputTypeInfo(
            inputType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            behaviorInterface.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            outputType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            ToTypeofTypeName(responseType),
            returnsStructuredResult,
            isSimpleInput,
            publicProperties);
    }

    private static INamedTypeSymbol? ResolveBehaviorInterface(INamedTypeSymbol typeSymbol)
    {
        return typeSymbol.AllInterfaces.FirstOrDefault(static i =>
            i.OriginalDefinition.ToDisplayString() ==
            "Cephalon.Abstractions.Behaviors.IAppBehavior<TIn, TOut>");
    }

    private static bool TryResolveStructuredResultPayloadType(
        ITypeSymbol outputType,
        out ITypeSymbol responseType)
    {
        var effectiveOutputType = UnwrapNullable(outputType);
        responseType = effectiveOutputType;
        if (effectiveOutputType is not INamedTypeSymbol
            {
                IsGenericType: true,
                TypeArguments.Length: 1
            } namedOutputType)
        {
            return false;
        }

        var originalDefinition = namedOutputType.OriginalDefinition.ToDisplayString();
        if (originalDefinition is not
            ("Cephalon.Abstractions.Behaviors.Result<T>" or
             "Cephalon.Abstractions.Behaviors.BehaviorResult<T>"))
        {
            return false;
        }

        responseType = UnwrapNullable(namedOutputType.TypeArguments[0]);
        return true;
    }

    private static string ResolveBehaviorIdempotencyMode(INamedTypeSymbol typeSymbol)
    {
        foreach (var attribute in typeSymbol.GetAttributes())
        {
            if (!string.Equals(
                    attribute.AttributeClass?.ToDisplayString(),
                    "Cephalon.Abstractions.Behaviors.BehaviorIdempotencyAttribute",
                    StringComparison.Ordinal))
            {
                continue;
            }

            if (attribute.ConstructorArguments.Length == 0)
            {
                return "Idempotent";
            }

            var value = attribute.ConstructorArguments[0].Value;
            if (value is int intValue)
            {
                return intValue switch
                {
                    1 => "Idempotent",
                    2 => "NonIdempotent",
                    _ => "Unknown"
                };
            }

            var valueText = value?.ToString();
            return valueText switch
            {
                "Idempotent" => "Idempotent",
                "NonIdempotent" => "NonIdempotent",
                _ => "Unknown"
            };
        }

        return "Unknown";
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

    private static bool DeclaresTopologyAttributes(INamedTypeSymbol typeSymbol)
    {
        foreach (var attribute in typeSymbol.GetAttributes())
        {
            var attributeName = attribute.AttributeClass?.ToDisplayString();
            if (string.Equals(
                    attributeName,
                    "Cephalon.Abstractions.Behaviors.BehaviorAllowedPatternsAttribute",
                    StringComparison.Ordinal) ||
                string.Equals(
                    attributeName,
                    "Cephalon.Abstractions.Behaviors.BehaviorAllowedTransportsAttribute",
                    StringComparison.Ordinal))
            {
                return true;
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

            var method = attribute.ConstructorArguments.Length > 0
                ? ResolveRestMethod(attribute.ConstructorArguments[0])
                : null;
            var relativePattern = attribute.ConstructorArguments.Length > 1
                ? attribute.ConstructorArguments[1].Value as string ?? string.Empty
                : string.Empty;

            var hasApiVersionMajor = false;
            var apiVersionMajor = 0;
            var preserveImplicitQueryFallback = false;
            foreach (var namedArgument in attribute.NamedArguments)
            {
                if (string.Equals(namedArgument.Key, "ApiVersionMajor", StringComparison.Ordinal))
                {
                    hasApiVersionMajor = true;
                    apiVersionMajor = namedArgument.Value.Value is null
                        ? 0
                        : Convert.ToInt32(namedArgument.Value.Value, System.Globalization.CultureInfo.InvariantCulture);
                    continue;
                }

                if (string.Equals(namedArgument.Key, "PreserveImplicitQueryFallback", StringComparison.Ordinal))
                {
                    preserveImplicitQueryFallback = namedArgument.Value.Value is bool preserve && preserve;
                }
            }

            return new RestProfileInfo(
                method?.MemberName,
                method?.WireName,
                relativePattern,
                hasApiVersionMajor,
                apiVersionMajor,
                ExtractRestBindings(typeSymbol),
                preserveImplicitQueryFallback);
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
            var source = attribute.ConstructorArguments.Length > 1
                ? ResolveRestBindingSource(attribute.ConstructorArguments[1])
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

            bindings.Add(new RestBindingInfo(propertyName, source?.MemberName, source?.WireName, name));
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
            .OrderBy(static invocation => invocation.Span.End)
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
        var requiredFeatureFlagIds = new List<string>();
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
            return null;

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
                case "AsSagaChoreography": pattern = "saga-choreography"; break;
                case "AsProcessManager": pattern = "process-manager"; break;
                case "AsDurableExecution": pattern = "durable-execution"; break;

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
                case "RequireFeatureFlag":
                case "RequireFeatureFlags":
                    if (!TryExtractRequiredFeatureFlagIds(invocation, requiredFeatureFlagIds))
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
            apiSurfaceOperationPath: apiSurfaceOperationPath,
            requiredFeatureFlagIds: requiredFeatureFlagIds
                .Distinct(System.StringComparer.OrdinalIgnoreCase)
                .ToArray());
    }

    private static bool HasConfigureTopologyMethod(ClassDeclarationSyntax classDecl)
    {
        return classDecl.Members
            .OfType<MethodDeclarationSyntax>()
            .Any(static m =>
                m.Identifier.Text == "ConfigureTopology" &&
                m.Modifiers.Any(SyntaxKind.StaticKeyword) &&
                m.Modifiers.Any(SyntaxKind.PublicKeyword));
    }

    private static TopologyInfo? ExtractTopologyFromAllowlistAttributes(INamedTypeSymbol typeSymbol)
    {
        var patterns = ExtractAttributeStringValues(
                typeSymbol,
                "Cephalon.Abstractions.Behaviors.BehaviorAllowedPatternsAttribute")
            .Where(static pattern => !string.IsNullOrWhiteSpace(pattern))
            .Select(static pattern => pattern.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static pattern => pattern, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var transports = ExtractAttributeStringValues(
                typeSymbol,
                "Cephalon.Abstractions.Behaviors.BehaviorAllowedTransportsAttribute")
            .Where(static transport => !string.IsNullOrWhiteSpace(transport))
            .Select(static transport => NormalizeTransportId(transport.Trim()))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static transport => transport, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (patterns.Length > 1)
        {
            return null;
        }

        if (patterns.Length == 0 && transports.Length == 0)
        {
            return null;
        }

        return new TopologyInfo(
            pattern: patterns.Length == 1 ? patterns[0] : "direct",
            transports: transports,
            outboxEnabled: false,
            inboxEnabled: false,
            eventSourcingEnabled: false,
            apiSurfaceGroupPath: null,
            apiSurfaceOperationPath: null,
            requiredFeatureFlagIds: []);
    }

    private static IEnumerable<string> ExtractAttributeStringValues(
        INamedTypeSymbol typeSymbol,
        string metadataName)
    {
        foreach (var attribute in typeSymbol.GetAttributes())
        {
            if (!string.Equals(attribute.AttributeClass?.ToDisplayString(), metadataName, StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var argument in attribute.ConstructorArguments)
            {
                if (argument.Kind == TypedConstantKind.Array)
                {
                    foreach (var value in argument.Values)
                    {
                        if (value.Value is string arrayValue)
                        {
                            yield return arrayValue;
                        }
                    }

                    continue;
                }

                if (argument.Value is string scalarValue)
                {
                    yield return scalarValue;
                }
            }
        }
    }

    private static string NormalizeTransportId(string transportId)
    {
        return string.Equals(transportId, "http.grpc", StringComparison.OrdinalIgnoreCase)
            ? "grpc"
            : transportId;
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

    private static bool TryExtractRequiredFeatureFlagIds(
        InvocationExpressionSyntax invocation,
        List<string> requiredFeatureFlagIds)
    {
        if (invocation is null)
        {
            throw new ArgumentNullException(nameof(invocation));
        }

        if (requiredFeatureFlagIds is null)
        {
            throw new ArgumentNullException(nameof(requiredFeatureFlagIds));
        }

        foreach (var argument in invocation.ArgumentList.Arguments)
        {
            if (argument.Expression is not LiteralExpressionSyntax literal ||
                !literal.IsKind(SyntaxKind.StringLiteralExpression))
            {
                return false;
            }

            var value = literal.Token.ValueText;
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            requiredFeatureFlagIds.Add(value.Trim());
        }

        return true;
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

        if (!IsSupportedRestMethodWireName(info.RestProfile.MethodWireName))
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
        else if (!TryValidateRoutePatternSyntax(info.RestProfile.RelativePattern, out var routePatternError))
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                Abt026RestProfilePatternMustUseValidPlaceholderSyntax,
                info.Location,
                info.ShortName,
                info.RestProfile.RelativePattern,
                routePatternError));
        }

        if (info.RestProfile.HasApiVersionMajor && info.RestProfile.ApiVersionMajor <= 0)
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                Abt017RestProfileVersionMustBePositive,
                info.Location,
                info.ShortName,
                info.RestProfile.ApiVersionMajor));
        }

        if (info.RestProfile.PreserveImplicitQueryFallback &&
            info.RestProfile.Bindings.Count == 0)
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                Abt027RestPreservedImplicitQueryFallbackRequiresExplicitBindings,
                info.Location,
                info.ShortName));
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
        var inputTypeNames = infos
            .Select(static info => info is { IsValid: true, InputType: not null }
                ? info.InputType.GenericInputTypeName
                : null)
            .Where(static name => name is not null)
            .Select(static name => name!)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();
        if (inputTypeNames.Length > 0)
        {
            sb.AppendLine("[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");
            sb.AppendLine("internal static class BehaviorJsonTypeInfoProvider");
            sb.AppendLine("{");
            sb.AppendLine("    private static readonly global::System.Text.Json.JsonSerializerOptions Options = new(global::System.Text.Json.JsonSerializerDefaults.Web)");
            sb.AppendLine("    {");
            sb.AppendLine("        TypeInfoResolver = new global::System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()");
            sb.AppendLine("    };");
            sb.AppendLine();
            sb.AppendLine("    internal static global::System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> Get<T>()");
            sb.AppendLine("    {");
            sb.AppendLine("        return (global::System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>)Options.GetTypeInfo(typeof(T));");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            sb.AppendLine();
        }

        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Source-generated zero-reflection behavior registration.");
        sb.AppendLine("/// Called by the engine at startup instead of scanning assembly types via reflection.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");
        sb.AppendLine("internal static class BehaviorAutoRegistration");
        sb.AppendLine("{");
        sb.AppendLine("    [global::System.Runtime.CompilerServices.ModuleInitializer]");
        sb.AppendLine("    internal static void RegisterGeneratedBehaviors()");
        sb.AppendLine("    {");
        sb.AppendLine("        global::Cephalon.Behaviors.Services.BehaviorGeneratedModuleRegistry.Register(");
        sb.AppendLine("            typeof(global::Cephalon.Behaviors.Generated.BehaviorAutoRegistration).Assembly,");
        sb.AppendLine("            new global::Cephalon.Behaviors.Services.BehaviorGeneratedModuleRegistration(");
        sb.AppendLine("                Register,");
        sb.AppendLine("                GetExecutionSlots(),");
        sb.AppendLine("                GetTopologyDescriptors(),");
        sb.AppendLine("                GetBehaviorsNeedingRuntimeTopology()));");
        sb.AppendLine("        global::Cephalon.Behaviors.Services.BehaviorContractRegistry.Register(");
        sb.AppendLine("            typeof(global::Cephalon.Behaviors.Generated.BehaviorAutoRegistration).Assembly,");
        sb.AppendLine("            GetBehaviorContracts());");
        sb.AppendLine("    }");
        sb.AppendLine();

        // ── Register method ──
        sb.AppendLine("    /// <summary>Registers all behaviors in this assembly into DI and implementation descriptors.</summary>");
        sb.AppendLine("    internal static void Register(");
        sb.AppendLine("        global::Microsoft.Extensions.DependencyInjection.IServiceCollection services)");
        sb.AppendLine("    {");

        foreach (var info in infos)
        {
            if (info is not { IsValid: true }) continue;
            var fqn = info.TypeName; // already global:: prefixed from FullyQualifiedFormat
            var id = EscapeString(info.BehaviorId);
            sb.AppendLine();
            sb.AppendLine($"        // {info.ShortName} → \"{id}\"");
            sb.AppendLine("        if (global::Cephalon.Behaviors.Services.BehaviorImplementationRegistration.TryRegister(");
            sb.AppendLine("                services,");
            sb.AppendLine($"                \"{id}\",");
            sb.AppendLine($"                typeof({fqn}),");
            sb.AppendLine($"                global::Cephalon.Abstractions.Behaviors.BehaviorIdempotencyMode.{info.IdempotencyMode}))");
            sb.AppendLine("        {");
            sb.AppendLine($"            global::Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.TryAddTransient(services, typeof({fqn}));");
            if (info.DurableExecution is not null)
            {
                sb.AppendLine("            services.Add(global::Microsoft.Extensions.DependencyInjection.ServiceDescriptor.Singleton(");
                sb.AppendLine("                typeof(global::Cephalon.Behaviors.Patterns.Strategies.DurableExecutionSlot),");
                sb.AppendLine($"                global::Cephalon.Behaviors.Patterns.Strategies.DurableExecutionSlot.For<{fqn}, {info.DurableExecution.InputTypeName}, {info.DurableExecution.StateTypeName}, {info.DurableExecution.OutputTypeName}>()));");
            }
            if (info.SagaChoreographyRuntime is not null && info.InputType is not null)
            {
                var localOutputTypeExpression = info.SagaChoreographyRuntime.LocalOutputTypeName is null
                    ? "null"
                    : $"typeof({info.SagaChoreographyRuntime.LocalOutputTypeName}).FullName";
                sb.AppendLine("            services.Add(global::Microsoft.Extensions.DependencyInjection.ServiceDescriptor.Singleton(");
                sb.AppendLine("                typeof(global::Cephalon.Behaviors.Patterns.Runtime.SagaChoreographyRuntimeSlot),");
                sb.AppendLine($"                global::Cephalon.Behaviors.Patterns.Runtime.SagaChoreographyRuntimeSlot.For<{fqn}, {info.InputType.GenericInputTypeName}, {info.InputType.GenericOutputTypeName}>(\"{EscapeString(info.SagaChoreographyRuntime.AuthoringModel)}\", \"{EscapeString(info.SagaChoreographyRuntime.PublicationResultShape)}\", {localOutputTypeExpression})));");
            }
            sb.AppendLine("        }");
        }

        sb.AppendLine("    }");
        sb.AppendLine();

        // ── GetExecutionSlots method ──
        sb.AppendLine("    /// <summary>Returns pre-built execution slots for behaviors discovered at compile time.</summary>");
        sb.AppendLine("    internal static global::System.Collections.Generic.IReadOnlyList<global::Cephalon.Behaviors.Services.BehaviorGeneratedExecutionSlotDescriptor> GetExecutionSlots()");
        sb.AppendLine("    {");
        sb.AppendLine("        return new global::Cephalon.Behaviors.Services.BehaviorGeneratedExecutionSlotDescriptor[]");
        sb.AppendLine("        {");

        foreach (var info in infos)
        {
            if (info is not { IsValid: true, InputType: not null }) continue;
            var fqn = info.TypeName;
            var id = EscapeString(info.BehaviorId);
            var inputType = info.InputType.GenericInputTypeName;
            var outputType = info.InputType.GenericOutputTypeName;
            sb.AppendLine($"            new global::Cephalon.Behaviors.Services.BehaviorGeneratedExecutionSlotDescriptor(\"{id}\", typeof({fqn}), global::Cephalon.Behaviors.Services.BehaviorExecutionSlot.For<{fqn}, {inputType}, {outputType}>(global::Cephalon.Behaviors.Generated.BehaviorJsonTypeInfoProvider.Get<{inputType}>())),");
        }

        sb.AppendLine("        };");
        sb.AppendLine("    }");
        sb.AppendLine();

        // ── GetBehaviorContracts method ──
        sb.AppendLine("    /// <summary>Returns pre-built behavior contract descriptors discovered at compile time.</summary>");
        sb.AppendLine("    internal static global::System.Collections.Generic.IReadOnlyList<global::Cephalon.Behaviors.Services.BehaviorContractDescriptor> GetBehaviorContracts()");
        sb.AppendLine("    {");
        sb.AppendLine("        return new global::Cephalon.Behaviors.Services.BehaviorContractDescriptor[]");
        sb.AppendLine("        {");

        foreach (var info in infos)
        {
            if (info is not { IsValid: true, InputType: not null }) continue;
            var id = EscapeString(info.BehaviorId);
            var inputType = info.InputType;
            sb.Append("            new global::Cephalon.Behaviors.Services.BehaviorContractDescriptor(");
            sb.Append($"\"{id}\", ");
            sb.Append($"typeof({info.TypeName}), ");
            sb.Append($"typeof({inputType.GenericInputTypeName}), ");
            sb.Append($"typeof({inputType.GenericOutputTypeName}), ");
            sb.Append($"typeof({inputType.ResponseTypeName}), ");
            sb.Append(inputType.ReturnsStructuredResult ? "true" : "false");
            sb.Append(", ");
            sb.Append(inputType.IsSimple ? "true" : "false");
            if (inputType.PublicProperties.Count > 0)
            {
                sb.Append(", new global::Cephalon.Behaviors.Services.BehaviorInputPropertyDescriptor[] { ");
                sb.Append(string.Join(
                    ", ",
                    inputType.PublicProperties.Values
                        .OrderBy(static property => property.Name, StringComparer.Ordinal)
                        .Select(static property =>
                            $"new global::Cephalon.Behaviors.Services.BehaviorInputPropertyDescriptor(\"{EscapeString(property.Name)}\", typeof({property.TypeName}))")));
                sb.Append(" }");
            }

            sb.AppendLine("),");
        }

        sb.AppendLine("        };");
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
            if (t.RequiredFeatureFlagIds.Length > 0)
            {
                sb.Append(", requiredFeatureFlagIds: new string[] { ");
                sb.Append(string.Join(", ", t.RequiredFeatureFlagIds.Select(featureFlagId => $"\"{EscapeString(featureFlagId)}\"")));
                sb.Append(" }");
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
            sb.AppendLine("    [global::System.Runtime.CompilerServices.ModuleInitializer]");
            sb.AppendLine("    internal static void RegisterRestProfiles()");
            sb.AppendLine("    {");
            sb.AppendLine("        global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestGeneratedProfileRegistry.Register(");
            sb.AppendLine("            typeof(global::Cephalon.Behaviors.Generated.BehaviorAutoRegistration).Assembly,");
            sb.AppendLine("            GetRestProfiles(),");
            sb.AppendLine("            GetRestProfileBehaviorTypes());");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("    /// <summary>Returns metadata-only REST profile hints discovered at compile time.</summary>");
            sb.AppendLine("    internal static global::System.Collections.Generic.IReadOnlyList<global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestProfileDescriptor> GetRestProfiles()");
            sb.AppendLine("    {");
            sb.AppendLine("        return new global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestProfileDescriptor[]");
            sb.AppendLine("        {");

            foreach (var info in behaviorsWithRestProfiles)
            {
                if (info?.RestProfile is null ||
                    !IsSupportedRestMethodWireName(info.RestProfile.MethodWireName))
                {
                    continue;
                }

                sb.Append("            new global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestProfileDescriptor(");
                sb.Append($"\"{EscapeString(info.BehaviorId)}\", ");
                sb.Append($"global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestMethod.{info.RestProfile.MethodMemberName}, ");
                sb.Append($"\"{EscapeString(info.RestProfile.RelativePattern.Trim())}\", ");
                sb.Append(info.RestProfile.HasApiVersionMajor
                    ? info.RestProfile.ApiVersionMajor.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    : "null");
                if (info.RestProfile.Bindings.Count > 0 || info.RestProfile.PreserveImplicitQueryFallback)
                {
                    sb.Append(", ");
                    if (info.RestProfile.Bindings.Count > 0)
                    {
                        sb.Append("new global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestBindingDescriptor[] { ");
                        sb.Append(string.Join(
                            ", ",
                            info.RestProfile.Bindings.Select(static binding =>
                                $"new global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestBindingDescriptor(\"{EscapeString(binding.PropertyName)}\", global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestBindingSource.{binding.SourceMemberName}, {(string.IsNullOrWhiteSpace(binding.Name) ? "null" : $"\"{EscapeString(binding.Name!)}\"")})")));
                        sb.Append(" }");
                    }
                    else
                    {
                        sb.Append("null");
                    }

                    if (info.RestProfile.PreserveImplicitQueryFallback)
                    {
                        sb.Append(", PreserveImplicitQueryFallback: true");
                    }
                }
                sb.Append(") { InputContract = ");
                AppendRestInputContractDescriptor(sb, info);
                sb.AppendLine(" },");
            }

            sb.AppendLine("        };");
            sb.AppendLine("    }");

            sb.AppendLine();
            sb.AppendLine("    /// <summary>Returns the behavior types that correspond to generated REST profile hints.</summary>");
            sb.AppendLine("    internal static global::System.Collections.Generic.IReadOnlyList<global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestProfileBehaviorTypeDescriptor> GetRestProfileBehaviorTypes()");
            sb.AppendLine("    {");
            sb.AppendLine("        return new global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestProfileBehaviorTypeDescriptor[]");
            sb.AppendLine("        {");

            foreach (var info in behaviorsWithRestProfiles)
            {
                if (info?.RestProfile is null ||
                    !IsSupportedRestMethodWireName(info.RestProfile.MethodWireName))
                {
                    continue;
                }

                sb.AppendLine($"            new global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestProfileBehaviorTypeDescriptor(\"{EscapeString(info.BehaviorId)}\", typeof({info.TypeName})),");
            }

            sb.AppendLine("        };");
            sb.AppendLine("    }");
        }

        // ── GetBehaviorIdsWithoutTopology method ──
        // Behaviors that declare topology but cannot be reduced to a generated descriptor
        // are surfaced as fail-fast metadata. Runtime ConfigureTopology invocation is no
        // longer used as a fallback path.
        var behaviorsWithoutTopology = infos
            .Where(i => i is { IsValid: true, Topology: null, HasTopologyDeclarations: true })
            .ToArray();

        sb.AppendLine();
        sb.AppendLine("    /// <summary>Returns behavior IDs with topology declarations that were not reduced to generated descriptors.</summary>");
        sb.AppendLine("    internal static global::System.Collections.Generic.IReadOnlyList<global::Cephalon.Behaviors.Services.BehaviorGeneratedRuntimeTopologyDescriptor> GetBehaviorsNeedingRuntimeTopology()");
        sb.AppendLine("    {");

        if (behaviorsWithoutTopology.Length == 0)
        {
            sb.AppendLine("        return global::System.Array.Empty<global::Cephalon.Behaviors.Services.BehaviorGeneratedRuntimeTopologyDescriptor>();");
        }
        else
        {
            sb.AppendLine("        return new global::Cephalon.Behaviors.Services.BehaviorGeneratedRuntimeTopologyDescriptor[]");
            sb.AppendLine("        {");
            foreach (var info in behaviorsWithoutTopology)
            {
                if (info is null) continue;
                sb.AppendLine($"            new global::Cephalon.Behaviors.Services.BehaviorGeneratedRuntimeTopologyDescriptor(\"{EscapeString(info.BehaviorId)}\", typeof({info.TypeName})),");
            }
            sb.AppendLine("        };");
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return SourceText.From(sb.ToString(), Encoding.UTF8);
    }

    private static void AppendRestInputContractDescriptor(StringBuilder sb, BehaviorInfo info)
    {
        if (info.InputType is null)
        {
            sb.Append("null");
            return;
        }

        sb.Append("new global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestInputContractDescriptor(");
        sb.Append($"typeof({info.InputType.GenericInputTypeName}), ");
        sb.Append(info.InputType.IsSimple ? "true" : "false");
        if (info.InputType.PublicProperties.Count > 0)
        {
            sb.Append(", new global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestInputPropertyDescriptor[] { ");
            sb.Append(string.Join(
                ", ",
                info.InputType.PublicProperties.Values
                    .OrderBy(static property => property.Name, StringComparer.Ordinal)
                    .Select(static property =>
                        $"new global::Cephalon.Behaviors.Http.Abstractions.BehaviorRestInputPropertyDescriptor(\"{EscapeString(property.Name)}\", typeof({property.TypeName}))")));
            sb.Append(" }");
        }

        sb.Append(')');
    }

    private static string EscapeString(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static string? ResolveEnumMemberName(TypedConstant constant)
        => ResolveEnumMemberInfo(constant)?.MemberName;

    private static EnumMemberInfo? ResolveEnumMemberInfo(TypedConstant constant)
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
                return new EnumMemberInfo(member.Name, ResolveEnumWireName(member));
            }
        }

        return null;
    }

    private static EnumMemberInfo? ResolveRestBindingSource(TypedConstant constant)
    {
        var member = ResolveEnumMemberInfo(constant);
        if (member is null)
        {
            return null;
        }

        return new EnumMemberInfo(
            member.MemberName,
            NormalizeRestBindingSourceWireName(member.MemberName, member.WireName));
    }

    private static EnumMemberInfo? ResolveRestMethod(TypedConstant constant)
    {
        var member = ResolveEnumMemberInfo(constant);
        if (member is null)
        {
            return null;
        }

        return new EnumMemberInfo(
            member.MemberName,
            NormalizeRestMethodWireName(member.MemberName, member.WireName));
    }

    private static string ResolveEnumWireName(IFieldSymbol member)
    {
        foreach (var attribute in member.GetAttributes())
        {
            if (!string.Equals(
                    attribute.AttributeClass?.ToDisplayString(),
                    "System.Text.Json.Serialization.JsonStringEnumMemberNameAttribute",
                    StringComparison.Ordinal))
            {
                continue;
            }

            if (attribute.ConstructorArguments.Length > 0 &&
                attribute.ConstructorArguments[0].Value is string wireName &&
                !string.IsNullOrWhiteSpace(wireName))
            {
                return wireName.Trim();
            }
        }

        return member.Name;
    }

    private static string NormalizeRestBindingSourceWireName(string memberName, string wireName)
    {
        if (!string.Equals(wireName, memberName, StringComparison.Ordinal))
        {
            return wireName;
        }

        return memberName switch
        {
            "Unspecified" => "unspecified",
            "Route" => "route",
            "Query" => "query",
            "Header" => "header",
            "Body" => "body",
            _ => wireName
        };
    }

    private static string NormalizeRestMethodWireName(string memberName, string wireName)
    {
        if (!string.Equals(wireName, memberName, StringComparison.Ordinal))
        {
            return wireName;
        }

        return memberName switch
        {
            "Unspecified" => "unspecified",
            "Get" => "get",
            "Post" => "post",
            "Put" => "put",
            "Patch" => "patch",
            "Delete" => "delete",
            _ => wireName
        };
    }

    private static bool IsSupportedRestMethodWireName(string? wireName)
    {
        return wireName is "get" or "post" or "put" or "patch" or "delete";
    }

    private static bool MethodAcceptsBodyWireName(string? wireName)
    {
        return wireName is "post" or "put" or "patch";
    }

    private static bool IsSupportedRestBindingSourceWireName(string? wireName)
    {
        return wireName is "route" or "query" or "header" or "body";
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
        var supportsBody = MethodAcceptsBodyWireName(info.RestProfile.MethodWireName);
        var hasSupportedMethod = IsSupportedRestMethodWireName(info.RestProfile.MethodWireName);
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
            var propertyExists = info.InputType.PublicProperties.TryGetValue(propertyName, out var inputProperty);
            var effectivePropertyName = propertyExists ? inputProperty!.Name : propertyName;

            if (!propertyExists)
            {
                issues.Add(new RestBindingValidationIssue(
                    Abt022RestBindingPropertyMustExistOnInput,
                    info.ShortName,
                    propertyName,
                    info.InputType.DisplayName));
            }

            if (propertyExists && !seenProperties.Add(inputProperty!.Name))
            {
                issues.Add(new RestBindingValidationIssue(
                    Abt023RestBindingPropertyMustNotBeDuplicated,
                    info.ShortName,
                    inputProperty.Name));
            }

            if (!IsSupportedRestBindingSourceWireName(binding.SourceWireName))
            {
                issues.Add(new RestBindingValidationIssue(
                    Abt020RestBindingSourceMustBeSupported,
                    info.ShortName,
                    effectivePropertyName));
                continue;
            }

            if (hasSupportedMethod &&
                string.Equals(binding.SourceWireName, "body", StringComparison.Ordinal) &&
                !supportsBody)
            {
                issues.Add(new RestBindingValidationIssue(
                    Abt024RestBodyBindingMustUseBodyCapableMethod,
                    info.ShortName,
                    effectivePropertyName,
                    info.RestProfile.MethodMemberName ?? info.RestProfile.MethodWireName ?? string.Empty));
            }

            if (routeParameters is not null &&
                string.Equals(binding.SourceWireName, "route", StringComparison.Ordinal))
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
               pattern.Trim().StartsWith("/", StringComparison.Ordinal) &&
               TryValidateRoutePatternSyntax(pattern, out _);
    }

    private static bool TryValidateRoutePatternSyntax(string pattern, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return true;
        }

        var current = pattern.Trim();
        var insideParameter = false;
        var parameterStartIndex = -1;
        for (var index = 0; index < current.Length; index++)
        {
            var currentCharacter = current[index];
            if (currentCharacter == '{')
            {
                if (!insideParameter)
                {
                    if (index + 1 < current.Length && current[index + 1] == '{')
                    {
                        index++;
                        continue;
                    }

                    insideParameter = true;
                    parameterStartIndex = index + 1;
                    continue;
                }

                error = "nested '{' characters are not allowed inside a route parameter";
                return false;
            }

            if (currentCharacter != '}')
            {
                continue;
            }

            if (!insideParameter)
            {
                if (index + 1 < current.Length && current[index + 1] == '}')
                {
                    index++;
                    continue;
                }

                error = "encountered '}' without a matching '{'";
                return false;
            }

            var token = current.Substring(parameterStartIndex, index - parameterStartIndex);
            if (string.IsNullOrWhiteSpace(NormalizeRouteParameterToken(token)))
            {
                error = "route parameters must declare a non-empty placeholder name";
                return false;
            }

            insideParameter = false;
            parameterStartIndex = -1;
        }

        if (insideParameter)
        {
            error = "route parameter placeholders must end with '}'";
            return false;
        }

        return true;
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

    private static string ToTypeofTypeName(ITypeSymbol typeSymbol)
    {
        var type = typeSymbol is INamedTypeSymbol
        {
            OriginalDefinition.SpecialType: SpecialType.System_Nullable_T,
            TypeArguments.Length: 1
        }
            ? typeSymbol
            : typeSymbol.WithNullableAnnotation(NullableAnnotation.NotAnnotated);

        return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
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
            string? apiSurfaceOperationPath,
            string[] requiredFeatureFlagIds)
        {
            Pattern = pattern;
            Transports = transports;
            OutboxEnabled = outboxEnabled;
            InboxEnabled = inboxEnabled;
            EventSourcingEnabled = eventSourcingEnabled;
            ApiSurfaceGroupPath = apiSurfaceGroupPath;
            ApiSurfaceOperationPath = apiSurfaceOperationPath;
            RequiredFeatureFlagIds = requiredFeatureFlagIds;
        }

        public string Pattern { get; }
        public string[] Transports { get; }
        public bool OutboxEnabled { get; }
        public bool InboxEnabled { get; }
        public bool EventSourcingEnabled { get; }
        public string? ApiSurfaceGroupPath { get; }
        public string? ApiSurfaceOperationPath { get; }
        public string[] RequiredFeatureFlagIds { get; }
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
            DurableExecutionInfo? durableExecution,
            string idempotencyMode,
            SagaChoreographyRuntimeInfo? sagaChoreographyRuntime,
            RestProfileInfo? restProfile,
            bool hasRestTransportAttribute,
            bool hasConfigureTopology,
            bool hasTopologyDeclarations,
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
            DurableExecution = durableExecution;
            IdempotencyMode = idempotencyMode;
            SagaChoreographyRuntime = sagaChoreographyRuntime;
            RestProfile = restProfile;
            HasRestTransportAttribute = hasRestTransportAttribute;
            HasConfigureTopology = hasConfigureTopology;
            HasTopologyDeclarations = hasTopologyDeclarations;
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
        public DurableExecutionInfo? DurableExecution { get; }
        public string IdempotencyMode { get; }
        public SagaChoreographyRuntimeInfo? SagaChoreographyRuntime { get; }
        public RestProfileInfo? RestProfile { get; }
        public bool HasRestTransportAttribute { get; }
        public bool HasConfigureTopology { get; }
        public bool HasTopologyDeclarations { get; }
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
            IsSupportedRestMethodWireName(RestProfile.MethodWireName) &&
            !string.IsNullOrWhiteSpace(RestProfile.RelativePattern) &&
            RestProfile.RelativePattern.Trim().StartsWith("/", StringComparison.Ordinal) &&
            TryValidateRoutePatternSyntax(RestProfile.RelativePattern, out _) &&
            (!RestProfile.HasApiVersionMajor || RestProfile.ApiVersionMajor > 0) &&
            (!RestProfile.PreserveImplicitQueryFallback || RestProfile.Bindings.Count > 0) &&
            ValidateRestBindingMetadata(this).IsDefaultOrEmpty &&
            !HasRestTransportAttribute &&
            !HasConfigureTopologyRestTransport;
    }

    private sealed class DurableExecutionInfo
    {
        public DurableExecutionInfo(
            string inputTypeName,
            string stateTypeName,
            string outputTypeName)
        {
            InputTypeName = inputTypeName;
            StateTypeName = stateTypeName;
            OutputTypeName = outputTypeName;
        }

        public string InputTypeName { get; }
        public string StateTypeName { get; }
        public string OutputTypeName { get; }
    }

    private sealed class SagaChoreographyRuntimeInfo
    {
        public SagaChoreographyRuntimeInfo(
            string authoringModel,
            string publicationResultShape,
            string? localOutputTypeName)
        {
            AuthoringModel = authoringModel;
            PublicationResultShape = publicationResultShape;
            LocalOutputTypeName = localOutputTypeName;
        }

        public string AuthoringModel { get; }
        public string PublicationResultShape { get; }
        public string? LocalOutputTypeName { get; }
    }

    private sealed class RestProfileInfo
    {
        public RestProfileInfo(
            string? methodMemberName,
            string? methodWireName,
            string relativePattern,
            bool hasApiVersionMajor,
            int apiVersionMajor,
            IReadOnlyList<RestBindingInfo> bindings,
            bool preserveImplicitQueryFallback)
        {
            MethodMemberName = methodMemberName;
            MethodWireName = methodWireName;
            RelativePattern = relativePattern;
            HasApiVersionMajor = hasApiVersionMajor;
            ApiVersionMajor = apiVersionMajor;
            Bindings = bindings;
            PreserveImplicitQueryFallback = preserveImplicitQueryFallback;
        }

        public string? MethodMemberName { get; }
        public string? MethodWireName { get; }
        public string RelativePattern { get; }
        public bool HasApiVersionMajor { get; }
        public int ApiVersionMajor { get; }
        public IReadOnlyList<RestBindingInfo> Bindings { get; }
        public bool PreserveImplicitQueryFallback { get; }
    }

    private sealed class RestBindingInfo
    {
        public RestBindingInfo(string propertyName, string? sourceMemberName, string? sourceWireName, string? name)
        {
            PropertyName = propertyName;
            SourceMemberName = sourceMemberName;
            SourceWireName = sourceWireName;
            Name = name;
        }

        public string PropertyName { get; }
        public string? SourceMemberName { get; }
        public string? SourceWireName { get; }
        public string? Name { get; }
    }

    private sealed class EnumMemberInfo
    {
        public EnumMemberInfo(string memberName, string wireName)
        {
            MemberName = memberName;
            WireName = wireName;
        }

        public string MemberName { get; }
        public string WireName { get; }
    }

    private sealed class InputTypeInfo
    {
        public InputTypeInfo(
            string displayName,
            string genericInputTypeName,
            string genericOutputTypeName,
            string responseTypeName,
            bool returnsStructuredResult,
            bool isSimple,
            ImmutableDictionary<string, InputPropertyInfo> publicProperties)
        {
            DisplayName = displayName;
            GenericInputTypeName = genericInputTypeName;
            GenericOutputTypeName = genericOutputTypeName;
            ResponseTypeName = responseTypeName;
            ReturnsStructuredResult = returnsStructuredResult;
            IsSimple = isSimple;
            PublicProperties = publicProperties;
        }

        public string DisplayName { get; }
        public string GenericInputTypeName { get; }
        public string GenericOutputTypeName { get; }
        public string ResponseTypeName { get; }
        public bool ReturnsStructuredResult { get; }
        public bool IsSimple { get; }
        public ImmutableDictionary<string, InputPropertyInfo> PublicProperties { get; }
    }

    private sealed class InputPropertyInfo
    {
        public InputPropertyInfo(string name, string typeName)
        {
            Name = name;
            TypeName = typeName;
        }

        public string Name { get; }
        public string TypeName { get; }
    }

    private static DurableExecutionInfo? ResolveDurableExecutionInfo(INamedTypeSymbol typeSymbol)
    {
        var durableInterface = typeSymbol.AllInterfaces.FirstOrDefault(static i =>
            i.OriginalDefinition.ToDisplayString() ==
            "Cephalon.Behaviors.Patterns.Abstractions.IDurableExecution<TInput, TState, TOutput>");
        if (durableInterface is null)
        {
            return null;
        }

        return new DurableExecutionInfo(
            durableInterface.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            durableInterface.TypeArguments[1].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            durableInterface.TypeArguments[2].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
    }

    private static SagaChoreographyRuntimeInfo? ResolveSagaChoreographyRuntimeInfo(
        INamedTypeSymbol typeSymbol,
        TopologyInfo? topology,
        Compilation compilation)
    {
        if (!string.Equals(topology?.Pattern, "saga-choreography", StringComparison.OrdinalIgnoreCase) ||
            compilation.GetTypeByMetadataName("Cephalon.Behaviors.Patterns.Runtime.SagaChoreographyRuntimeSlot") is null)
        {
            return null;
        }

        var behaviorInterface = ResolveBehaviorInterface(typeSymbol);
        if (behaviorInterface is null)
        {
            return null;
        }

        var resultType = UnwrapNullable(behaviorInterface.TypeArguments[1]);
        var publicationResultShape = ResolveSagaChoreographyResultShape(resultType, out var localOutputTypeName);
        var authoringModel = typeSymbol.AllInterfaces.Any(static i =>
            i.OriginalDefinition.ToDisplayString() is
                "Cephalon.Behaviors.Patterns.Abstractions.ISagaEventReactor<TEvent>" or
                "Cephalon.Behaviors.Patterns.Abstractions.ISagaEventReactor<TEvent, TOutput>")
            ? "reactor"
            : "behavior";

        return new SagaChoreographyRuntimeInfo(
            authoringModel,
            publicationResultShape,
            localOutputTypeName);
    }

    private static string ResolveSagaChoreographyResultShape(
        ITypeSymbol resultType,
        out string? localOutputTypeName)
    {
        localOutputTypeName = null;

        if (IsSagaPublicationType(resultType))
        {
            return "single-publication";
        }

        if (IsSagaPublicationSequence(resultType))
        {
            return "publication-sequence";
        }

        if (IsNamedType(resultType, "Cephalon.Behaviors.Patterns.Abstractions.SagaChoreographyStepResult"))
        {
            return "step-result";
        }

        if (resultType is INamedTypeSymbol namedResultType &&
            string.Equals(
                namedResultType.OriginalDefinition.ToDisplayString(),
                "Cephalon.Behaviors.Patterns.Abstractions.SagaChoreographyStepResult<TOutput>",
                StringComparison.Ordinal))
        {
            localOutputTypeName = namedResultType.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            return "typed-step-result";
        }

        if (IsNamedType(resultType, "Cephalon.Behaviors.Patterns.Abstractions.ISagaChoreographyStepResult"))
        {
            return "step-result-contract";
        }

        if (ImplementsNamedInterface(resultType, "Cephalon.Behaviors.Patterns.Abstractions.ISagaChoreographyStepResult"))
        {
            return "custom-step-result";
        }

        return "custom-result";
    }

    private static bool IsSagaPublicationSequence(ITypeSymbol resultType)
    {
        if (resultType is IArrayTypeSymbol arrayTypeSymbol)
        {
            return IsSagaPublicationType(arrayTypeSymbol.ElementType);
        }

        return IsGenericEnumerableOfSagaPublication(resultType) ||
               resultType.AllInterfaces.Any(IsGenericEnumerableOfSagaPublication);
    }

    private static bool IsGenericEnumerableOfSagaPublication(ITypeSymbol typeSymbol)
    {
        return typeSymbol is INamedTypeSymbol namedTypeSymbol &&
               string.Equals(
                   namedTypeSymbol.OriginalDefinition.ToDisplayString(),
                   "System.Collections.Generic.IEnumerable<T>",
                   StringComparison.Ordinal) &&
               namedTypeSymbol.TypeArguments.Length == 1 &&
               IsSagaPublicationType(namedTypeSymbol.TypeArguments[0]);
    }

    private static bool IsSagaPublicationType(ITypeSymbol typeSymbol)
    {
        return IsNamedType(typeSymbol, "Cephalon.Behaviors.Patterns.Abstractions.SagaChoreographyPublication");
    }

    private static bool ImplementsNamedInterface(ITypeSymbol typeSymbol, string metadataName)
    {
        return typeSymbol.AllInterfaces.Any(candidate => IsNamedType(candidate, metadataName));
    }

    private static bool IsNamedType(ITypeSymbol typeSymbol, string metadataName)
    {
        return string.Equals(typeSymbol.ToDisplayString(), metadataName, StringComparison.Ordinal);
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
