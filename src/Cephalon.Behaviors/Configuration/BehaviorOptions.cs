using System.Reflection;

namespace Cephalon.Behaviors.Configuration;

/// <summary>
/// Top-level behavior options read from the <c>Engine:Behaviors</c> configuration section.
/// </summary>
public sealed class BehaviorOptions
{
    /// <summary>
    /// Gets or sets whether the engine automatically discovers and registers behaviors
    /// from loaded assemblies. When <see langword="true" />, the engine
    /// scans assemblies for concrete types decorated with
    /// <c>[AppBehavior]</c> and implementing <c>IAppBehavior&lt;TIn, TOut&gt;</c>,
    /// registering any that have not already been registered manually.
    /// </summary>
    /// <remarks>
    /// Explicit module-owned behavior registration through <c>BehaviorModuleBase</c> and
    /// <c>RestBehaviorModuleBase</c> is now the preferred default path. Use auto-registration as
    /// an opt-in fallback for legacy, exploratory, or convention-driven scenarios that still rely
    /// on assembly scanning.
    /// </remarks>
    public bool AutoRegister { get; set; }

    /// <summary>
    /// Gets or sets the list of assembly names to scan for auto-registration.
    /// When empty and <see cref="AutoRegister" /> is <see langword="true" />,
    /// the engine scans all loaded assemblies that reference <c>Cephalon.Abstractions</c>,
    /// excluding well-known framework prefixes and any entries in
    /// <see cref="AutoRegisterExcludeAssemblyPrefixes" />.
    /// </summary>
    public List<string> AutoRegisterAssemblies { get; set; } = [];

    /// <summary>
    /// Gets or sets additional assembly name prefixes to exclude from auto-registration scanning.
    /// Only effective when <see cref="AutoRegisterAssemblies" /> is empty (default scan mode).
    /// The engine already excludes well-known framework prefixes (<c>System.</c>,
    /// <c>Microsoft.</c>, etc.) — use this property to add project-specific exclusions
    /// such as <c>"MyCompany.Shared."</c> or <c>"ThirdParty."</c>.
    /// </summary>
    public List<string> AutoRegisterExcludeAssemblyPrefixes { get; set; } = [];

    /// <summary>
    /// Resolves the assemblies to scan for auto-registration.
    /// </summary>
    /// <returns>The assemblies to scan.</returns>
    internal IReadOnlyList<Assembly> ResolveAutoRegisterAssemblies()
    {
        if (!AutoRegister)
            return [];

        // Explicit list: scan only the named assemblies
        if (AutoRegisterAssemblies.Count > 0)
        {
            return AutoRegisterAssemblies
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(LoadAssembly)
                .Where(a => a is not null)
                .Cast<Assembly>()
                .ToArray();
        }

        // Default: scan all loaded assemblies that reference Cephalon.Abstractions,
        // excluding well-known framework assemblies that can never contain behaviors.
        var abstractionsName = typeof(Abstractions.Behaviors.AppBehaviorAttribute).Assembly.GetName().Name!;

        // Merge built-in + user-configured exclusion prefixes
        var excludePrefixes = MergeExcludePrefixes();

        return AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic
                        && !IsExcludedByPrefix(a, excludePrefixes)
                        && ReferencesAssembly(a, abstractionsName))
            .ToArray();
    }

    /// <summary>
    /// Well-known assembly name prefixes that never contain application behaviors.
    /// Checked before the more expensive <see cref="ReferencesAssembly" /> call
    /// to eliminate 90%+ of loaded assemblies with a cheap string comparison.
    /// </summary>
    private static readonly string[] BuiltInExcludePrefixes =
    [
        // .NET runtime & BCL
        "System.",
        "System,",
        "netstandard",
        "mscorlib",

        // Microsoft frameworks & extensions
        "Microsoft.",

        // Common NuGet infrastructure
        "NuGet.",
        "Newtonsoft.",

        // Testing frameworks
        "xunit.",
        "nunit.",
        "Moq.",
        "FluentAssertions.",
        "coverlet.",
        "BenchmarkDotNet.",

        // Common data drivers (driver assemblies don't define behaviors)
        "Npgsql.",
        "MongoDB.",
        "StackExchange.",
        "MySqlConnector.",
        "Oracle.",

        // OpenTelemetry instrumentation
        "OpenTelemetry.",

        // Swagger / API docs
        "Swashbuckle.",
        "NSwag.",

        // gRPC & Protobuf
        "Grpc.",
        "Google.Protobuf",

        // Cephalon engine internals (contracts & engine core never contain behaviors)
        "Cephalon.Abstractions",
        "Cephalon.Engine",
    ];

    /// <summary>
    /// Merges the built-in exclusion prefixes with any user-configured ones.
    /// </summary>
    private string[] MergeExcludePrefixes()
    {
        if (AutoRegisterExcludeAssemblyPrefixes.Count == 0)
            return BuiltInExcludePrefixes;

        var merged = new HashSet<string>(BuiltInExcludePrefixes, StringComparer.OrdinalIgnoreCase);
        foreach (var prefix in AutoRegisterExcludeAssemblyPrefixes)
        {
            if (!string.IsNullOrWhiteSpace(prefix))
                merged.Add(prefix.Trim());
        }

        return [.. merged];
    }

    /// <summary>
    /// Returns <see langword="true" /> when the assembly name starts with any excluded prefix.
    /// This is a cheap O(n) string check that avoids the more expensive
    /// <see cref="Assembly.GetReferencedAssemblies" /> call.
    /// </summary>
    private static bool IsExcludedByPrefix(Assembly assembly, string[] prefixes)
    {
        var name = assembly.GetName().Name;
        if (string.IsNullOrEmpty(name))
            return true; // unnamed assemblies are never behavior sources

        foreach (var prefix in prefixes)
        {
            if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static Assembly? LoadAssembly(string name)
    {
        var loaded = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a =>
                string.Equals(a.GetName().Name, name, StringComparison.OrdinalIgnoreCase));
        if (loaded is not null) return loaded;

        try
        {
            return Assembly.Load(new AssemblyName(name));
        }
        catch
        {
            return null;
        }
    }

    private static bool ReferencesAssembly(Assembly assembly, string referenceName)
    {
        if (string.Equals(assembly.GetName().Name, referenceName, StringComparison.OrdinalIgnoreCase))
            return true;

        return assembly.GetReferencedAssemblies()
            .Any(r => string.Equals(r.Name, referenceName, StringComparison.OrdinalIgnoreCase));
    }
}
