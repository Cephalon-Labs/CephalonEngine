using System.Reflection;
using Cephalon.Behaviors.Services;
using Microsoft.Extensions.Configuration;

namespace Cephalon.Behaviors.Configuration;

/// <summary>
/// Top-level behavior options read from the <c>Engine:Behaviors</c> configuration section.
/// </summary>
public sealed class BehaviorOptions
{
    /// <summary>
    /// Gets or sets whether the engine automatically registers source-generated behavior
    /// hints from loaded assemblies. When <see langword="true" />, the engine resolves
    /// candidate assemblies and consumes their generated <c>[AppBehavior]</c> registration
    /// tables without scanning runtime types.
    /// </summary>
    /// <remarks>
    /// Explicit module-owned behavior registration through <c>BehaviorModuleBase</c> and
    /// <c>RestBehaviorModuleBase</c> is now the preferred default path. Use auto-registration as
    /// an opt-in source-generated discovery path for legacy, exploratory, or
    /// convention-driven scenarios that still need configuration-driven assembly selection.
    /// </remarks>
    public bool AutoRegister { get; set; }

    /// <summary>
    /// Gets or sets the list of assembly names to resolve for generated auto-registration hints.
    /// When empty and <see cref="AutoRegister" /> is <see langword="true" />,
    /// the engine considers loaded assemblies that reference <c>Cephalon.Abstractions</c>,
    /// excluding well-known framework prefixes and any entries in
    /// <see cref="AutoRegisterExcludeAssemblyPrefixes" />.
    /// </summary>
    public List<string> AutoRegisterAssemblies { get; set; } = [];

    /// <summary>
    /// Gets or sets additional assembly name prefixes to exclude from generated auto-registration lookup.
    /// Only effective when <see cref="AutoRegisterAssemblies" /> is empty (default lookup mode).
    /// The engine already excludes well-known framework prefixes (<c>System.</c>,
    /// <c>Microsoft.</c>, etc.) — use this property to add project-specific exclusions
    /// such as <c>"MyCompany.Shared."</c> or <c>"ThirdParty."</c>.
    /// </summary>
    public List<string> AutoRegisterExcludeAssemblyPrefixes { get; set; } = [];

    /// <summary>
    /// Creates behavior options from the <c>Engine:Behaviors</c> configuration section without using reflection-based binding.
    /// </summary>
    /// <param name="configuration">The configuration source that contains the engine section.</param>
    /// <returns>The parsed behavior options.</returns>
    public static BehaviorOptions FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var options = new BehaviorOptions();
        options.ApplyConfiguration(configuration.GetSection("Engine").GetSection("Behaviors"));
        return options;
    }

    /// <summary>
    /// Applies values from an <c>Engine:Behaviors</c> configuration section without using reflection-based binding.
    /// </summary>
    /// <param name="section">The behavior configuration section.</param>
    private void ApplyConfiguration(IConfigurationSection section)
    {
        ArgumentNullException.ThrowIfNull(section);

        if (TryGetBoolean(section.GetSection(nameof(AutoRegister)), out var autoRegister))
        {
            AutoRegister = autoRegister;
        }

        AutoRegisterAssemblies = ReadStringList(section.GetSection(nameof(AutoRegisterAssemblies)));
        AutoRegisterExcludeAssemblyPrefixes = ReadStringList(section.GetSection(nameof(AutoRegisterExcludeAssemblyPrefixes)));
    }

    /// <summary>
    /// Resolves the assemblies to inspect for generated auto-registration hints.
    /// </summary>
    /// <returns>The assemblies to inspect for generated hints.</returns>
    internal IReadOnlyList<Assembly> ResolveAutoRegisterAssemblies()
    {
        if (!AutoRegister)
            return [];

        // Explicit list: resolve only the named assemblies.
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

        // Default: use only source-generated module hints that have already registered themselves.
        var excludePrefixes = MergeExcludePrefixes();
        return BehaviorGeneratedModuleRegistry.GetRegisteredAssemblies()
            .Where(a => !a.IsDynamic && !IsExcludedByPrefix(a, excludePrefixes))
            .ToArray();
    }

    /// <summary>
    /// Well-known assembly name prefixes that never contain application behaviors.
    /// Checked before generated module hints are considered.
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

    private static bool TryGetBoolean(IConfigurationSection section, out bool value)
    {
        var rawValue = section.Value;
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            value = false;
            return false;
        }

        return bool.TryParse(rawValue, out value);
    }

    private static List<string> ReadStringList(IConfigurationSection section)
    {
        return section
            .GetChildren()
            .Select(static child => child.Value)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value!.Trim())
            .ToList();
    }
}
