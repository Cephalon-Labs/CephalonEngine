using System.CodeDom.Compiler;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace Cephalon.ReferenceDocs.Generation;

/// <summary>
/// Generates markdown reference documentation from Cephalon public assemblies and their XML docs.
/// </summary>
public static class ReferenceDocsGenerator
{
    private static readonly JsonSerializerOptions ReferenceManifestJsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly AssemblyMetadata[] AssemblyCatalog =
    [
        new("Cephalon.Abstractions", "Core", "Host-agnostic contracts that module and package authors build against."),
        new("Cephalon.Engine", "Core", "Composition, runtime, policy, manifest, and introspection services."),
        new("Cephalon.AspNetCore", "Hosts", "ASP.NET Core host core, REST surface, docs, health, and runtime endpoints."),
        new("Cephalon.AspNetCore.JsonRpc", "Hosts", "JSON-RPC transport adapter for ASP.NET Core hosts."),
        new("Cephalon.AspNetCore.Grpc", "Hosts", "gRPC transport adapter and contracts for ASP.NET Core hosts."),
        new("Cephalon.Worker", "Hosts", "Generic-host worker adapter for non-HTTP runtime execution."),
        new("Cephalon.Observability", "Hosts", "Operational diagnostics and telemetry conventions for hosts."),
        new("Cephalon.Observability.HttpDependencies", "Hosts", "External HTTP dependency-health integration for Cephalon hosts."),
        new("Cephalon.Observability.OpenTelemetry", "Hosts", "OpenTelemetry OTLP exporter integration for Cephalon hosts."),
        new("Cephalon.Agentics", "Technology Packs", "Agentic workload runtime services and extension points."),
        new("Cephalon.Eventing", "Technology Packs", "Event-driven integration runtime services and extension points."),
        new("Cephalon.Retrieval", "Technology Packs", "Knowledge retrieval runtime services and extension points."),
        new("Cephalon.Edge", "Technology Packs", "Edge-native delivery runtime services and extension points."),
        new("Cephalon.Cli", "Tooling", "Command-line surface for blueprint-aware generation."),
        new("Cephalon.Scaffolding", "Tooling", "Blueprint scaffold generation primitives and filesystem output."),
        new("Cephalon.ReferenceDocs", "Tooling", "Reference-doc generation pipeline for XML comments and public APIs.")
    ];

    private static readonly string[] DefaultAssemblies =
    [
        "Cephalon.Abstractions",
        "Cephalon.Agentics",
        "Cephalon.AspNetCore",
        "Cephalon.AspNetCore.Grpc",
        "Cephalon.AspNetCore.JsonRpc",
        "Cephalon.Cli",
        "Cephalon.Edge",
        "Cephalon.Engine",
        "Cephalon.Eventing",
        "Cephalon.Observability",
        "Cephalon.Observability.HttpDependencies",
        "Cephalon.Observability.OpenTelemetry",
        "Cephalon.ReferenceDocs",
        "Cephalon.Retrieval",
        "Cephalon.Scaffolding",
        "Cephalon.Worker"
    ];

    /// <summary>
    /// Generates rendered markdown reference docs for the supplied request.
    /// </summary>
    /// <param name="request">The generation request.</param>
    /// <returns>The rendered markdown files.</returns>
    public static RenderedReferenceDocs Generate(ReferenceDocsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var assemblyNames = request.Assemblies.Count == 0
            ? DefaultAssemblies
            : request.Assemblies.ToArray();

        using var loadContext = DocumentationLoadContext.Create(request, assemblyNames);
        var pages = assemblyNames
            .Select(assemblyName => GenerateAssemblyPage(request, loadContext, assemblyName))
            .OrderBy(static page => page.AssemblyName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var indexMarkdown = BuildIndex(pages);
        var referenceManifestJson = BuildReferenceManifest(pages);
        var files = new List<ReferenceDocFile>
        {
            new("index.md", indexMarkdown),
            new("README.md", indexMarkdown),
            new("namespaces.md", BuildNamespaceIndex(pages)),
            new("types.md", BuildTypeIndex(pages)),
            new("members.md", BuildMemberIndex(pages)),
            new("reference-manifest.json", referenceManifestJson),
            new("browse.html", ReferenceBrowserRenderer.BuildPage(referenceManifestJson)),
            new("reference-browser.css", ReferenceBrowserRenderer.BuildStyles()),
            new("reference-browser.js", ReferenceBrowserRenderer.BuildScript())
        };

        files.AddRange(pages.Select(static page => new ReferenceDocFile(page.FileName, page.Markdown)));

        return new RenderedReferenceDocs(request, files);
    }

    private static AssemblyPage GenerateAssemblyPage(
        ReferenceDocsRequest request,
        DocumentationLoadContext loadContext,
        string assemblyName)
    {
        var assemblyPath = ResolveAssemblyPath(request, assemblyName);
        if (!File.Exists(assemblyPath))
        {
            throw new InvalidOperationException(
                $"Reference docs assembly '{assemblyName}' was not found at '{assemblyPath}'. Build the repository first.");
        }

        var xmlPath = Path.ChangeExtension(assemblyPath, ".xml");
        if (!File.Exists(xmlPath))
        {
            throw new InvalidOperationException(
                $"XML documentation for assembly '{assemblyName}' was not found at '{xmlPath}'. Build the repository with XML docs enabled first.");
        }

        var assembly = loadContext.LoadAssembly(assemblyPath);
        var comments = LoadComments(xmlPath);
        var types = assembly.GetExportedTypes()
            .Where(static type => !type.IsSpecialName)
            .Where(static type => !IsIgnoredType(type))
            .OrderBy(static type => type.Namespace, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static type => type.Name, StringComparer.OrdinalIgnoreCase)
            .Select(type => CreateTypePage(type, comments))
            .ToArray();

        return new AssemblyPage(
            AssemblyName: assemblyName,
            FileName: $"{ToSlug(assemblyName)}.md",
            NamespaceCount: types.Select(static type => type.Namespace).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            TypeCount: types.Length,
            Types: types,
            Markdown: BuildAssemblyMarkdown(assemblyName, types));
    }

    private static string ResolveAssemblyPath(ReferenceDocsRequest request, string assemblyName)
    {
        return Path.Combine(
            request.RootPath,
            "src",
            assemblyName,
            "bin",
            request.Configuration,
            request.TargetFramework,
            $"{assemblyName}.dll");
    }

    private static Dictionary<string, XElement> LoadComments(string xmlPath)
    {
        var document = XDocument.Load(xmlPath);
        return document.Root?
            .Element("members")?
            .Elements("member")
            .Where(static member => !string.IsNullOrWhiteSpace((string?)member.Attribute("name")))
            .ToDictionary(
                static member => ((string)member.Attribute("name")!).Trim(),
                static member => member,
                StringComparer.Ordinal)
            ?? new Dictionary<string, XElement>(StringComparer.Ordinal);
    }

    private static bool IsIgnoredType(Type type)
    {
        return type.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false);
    }

    private static TypePage CreateTypePage(Type type, Dictionary<string, XElement> comments)
    {
        var typeComment = comments.TryGetValue(GetTypeDocId(type), out var member) ? member : null;
        var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(static constructor => !constructor.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false))
            .OrderBy(static constructor => constructor.GetParameters().Length)
            .Select(constructor => CreateMemberPage(constructor, comments, "Constructors"))
            .OfType<MemberPage>()
            .ToArray();
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(static field => !field.IsSpecialName)
            .Where(static field => !field.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false))
            .OrderBy(static field => field.Name, StringComparer.OrdinalIgnoreCase)
            .Select(field => CreateMemberPage(field, comments, "Fields"))
            .OfType<MemberPage>()
            .ToArray();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .OrderBy(static property => property.Name, StringComparer.OrdinalIgnoreCase)
            .Select(property => CreateMemberPage(property, comments, "Properties"))
            .OfType<MemberPage>()
            .ToArray();
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(static method => IsDocumentableMethod(method))
            .OrderBy(static method => method.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static method => method.GetParameters().Length)
            .Select(method => CreateMemberPage(method, comments, "Methods"))
            .OfType<MemberPage>()
            .ToArray();

        return new TypePage(
            Namespace: type.Namespace ?? "(global)",
            DisplayName: FormatTypeName(type),
            Declaration: BuildTypeDeclaration(type),
            Summary: RenderElement(typeComment?.Element("summary")),
            Remarks: RenderElement(typeComment?.Element("remarks")),
            Members:
            [
                .. constructors,
                .. fields,
                .. properties,
                .. methods
            ]);
    }

    private static bool IsDocumentableMethod(MethodInfo method)
    {
        if (method.IsSpecialName ||
            method.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false))
        {
            return false;
        }

        return method.Name is not nameof(object.ToString)
            and not nameof(object.GetHashCode)
            and not nameof(object.Equals)
            and not "Deconstruct"
            and not "PrintMembers"
            and not "op_Equality"
            and not "op_Inequality";
    }

    private static MemberPage? CreateMemberPage(
        MemberInfo member,
        Dictionary<string, XElement> comments,
        string category)
    {
        var memberDocId = GetMemberDocId(member);
        var comment = comments.TryGetValue(memberDocId, out var memberComment) ? memberComment : null;
        var summary = RenderElement(comment?.Element("summary"));

        if (ShouldSkipGeneratedMember(member, summary))
        {
            return null;
        }

        return new MemberPage(
            AnchorId: GetMemberAnchorId(memberDocId),
            Category: category,
            DisplayName: GetMemberDisplayName(member),
            Signature: BuildMemberSignature(member),
            Summary: summary,
            Remarks: RenderElement(comment?.Element("remarks")),
            Returns: RenderElement(comment?.Element("returns")),
            Parameters: comment is null
                ? []
                : comment.Elements("param")
                    .Select(static param => CreateNamedDocumentation(param))
                    .OfType<NamedDocumentation>()
                    .ToArray(),
            TypeParameters: comment is null
                ? []
                : comment.Elements("typeparam")
                    .Select(static typeParam => CreateNamedDocumentation(typeParam))
                    .OfType<NamedDocumentation>()
                    .ToArray());
    }

    private static bool ShouldSkipGeneratedMember(MemberInfo member, string? summary)
    {
        return member.IsDefined(typeof(GeneratedCodeAttribute), inherit: false) &&
               string.IsNullOrWhiteSpace(summary);
    }

    private static string BuildIndex(IReadOnlyList<AssemblyPage> pages)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Cephalon Reference Docs");
        builder.AppendLine();
        builder.AppendLine("Generated from repository XML comments and public API metadata.");
        builder.AppendLine();
        builder.AppendLine("This folder is the published navigation layer for Cephalon public APIs. Start here when you want a package-level map before drilling into detailed type and member documentation.");
        builder.AppendLine();
        builder.AppendLine("Quick links:");
        builder.AppendLine();
        builder.AppendLine("- [Browser UI](browse.html)");
        builder.AppendLine("- [Namespace index](namespaces.md)");
        builder.AppendLine("- [Type index](types.md)");
        builder.AppendLine("- [Member index](members.md)");
        builder.AppendLine();
        builder.AppendLine("## Assemblies");
        builder.AppendLine();

        foreach (var category in pages.GroupBy(
                     static page => GetAssemblyMetadata(page.AssemblyName).Category,
                     StringComparer.OrdinalIgnoreCase))
        {
            builder.Append("### ").AppendLine(category.Key);
            builder.AppendLine();

            foreach (var page in category)
            {
                var metadata = GetAssemblyMetadata(page.AssemblyName);
                builder.Append("- [")
                    .Append(page.AssemblyName)
                    .Append("](")
                    .Append(page.FileName)
                    .Append(')')
                    .Append(": ")
                    .Append(metadata.Description)
                    .Append(" Contains ")
                    .Append(page.NamespaceCount)
                    .Append(" namespaces and ")
                    .Append(page.TypeCount)
                    .Append(" public types. ")
                    .Append("[Browse](")
                    .Append(BuildBrowserLink(assemblyName: page.AssemblyName))
                    .AppendLine(")");
            }

            builder.AppendLine();
        }

        builder.AppendLine("## Reading order");
        builder.AppendLine();
        builder.AppendLine("1. Start with `Cephalon.Abstractions` and `Cephalon.Engine` to understand the stable contract and runtime core.");
        builder.AppendLine("2. Move to host packages such as `Cephalon.AspNetCore`, `Cephalon.Worker`, and transport adapters.");
        builder.AppendLine("3. Explore technology packs and tooling once the runtime model is clear.");

        return builder.ToString().TrimEnd() + Environment.NewLine;
    }

    private static string BuildAssemblyMarkdown(string assemblyName, IReadOnlyList<TypePage> types)
    {
        var builder = new StringBuilder();
        builder.Append("# ").AppendLine(assemblyName);
        builder.AppendLine();
        builder.AppendLine("Generated from XML comments and the public API surface of the compiled assembly.");
        builder.AppendLine();
        builder.Append("[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](")
            .Append(BuildBrowserLink(assemblyName: assemblyName))
            .Append(')');
        builder.AppendLine();
        builder.AppendLine("## Namespaces");
        builder.AppendLine();

        foreach (var namespaceName in types
                     .Select(static type => type.Namespace)
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            builder.Append("- `").Append(namespaceName).AppendLine("`");
        }

        foreach (var group in types.GroupBy(static type => type.Namespace, StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine();
            builder.Append("<a id=\"").Append(GetNamespaceAnchorId(group.Key)).AppendLine("\"></a>");
            builder.AppendLine();
            builder.Append("## Namespace ").AppendLine(group.Key);

            foreach (var type in group)
            {
                builder.AppendLine();
                builder.Append("<a id=\"").Append(GetTypeAnchorId(type)).AppendLine("\"></a>");
                builder.AppendLine();
                builder.Append("### ").Append('`').Append(type.DisplayName).AppendLine("`");
                AppendTextBlock(builder, type.Summary);
                AppendTextBlock(builder, type.Remarks, label: "Remarks");

                builder.AppendLine();
                builder.AppendLine("#### Declaration");
                builder.AppendLine("```csharp");
                builder.AppendLine(type.Declaration);
                builder.AppendLine("```");

                foreach (var category in type.Members.GroupBy(static member => member.Category))
                {
                    builder.AppendLine();
                    builder.Append("#### ").AppendLine(category.Key);

                    foreach (var member in category)
                    {
                        builder.AppendLine();
                        builder.Append("<a id=\"").Append(member.AnchorId).AppendLine("\"></a>");
                        builder.AppendLine();
                        builder.Append("##### ").Append('`').Append(member.DisplayName).AppendLine("`");
                        builder.AppendLine();
                        builder.AppendLine("```csharp");
                        builder.AppendLine(member.Signature);
                        builder.AppendLine("```");
                        AppendTextBlock(builder, member.Summary);
                        AppendTextBlock(builder, member.Remarks, label: "Remarks");

                        if (!string.IsNullOrWhiteSpace(member.Returns))
                        {
                            builder.AppendLine();
                            builder.Append("Returns: ").AppendLine(member.Returns);
                        }

                        if (member.TypeParameters.Count > 0)
                        {
                            builder.AppendLine();
                            builder.AppendLine("Type parameters:");
                            foreach (var item in member.TypeParameters)
                            {
                                builder.Append("- `").Append(item.Name).Append("`: ").AppendLine(item.Text);
                            }
                        }

                        if (member.Parameters.Count > 0)
                        {
                            builder.AppendLine();
                            builder.AppendLine("Parameters:");
                            foreach (var item in member.Parameters)
                            {
                                builder.Append("- `").Append(item.Name).Append("`: ").AppendLine(item.Text);
                            }
                        }
                    }
                }
            }
        }

        return builder.ToString().TrimEnd() + Environment.NewLine;
    }

    private static string BuildMemberIndex(IReadOnlyList<AssemblyPage> pages)
    {
        var entries = pages
            .SelectMany(static page => page.Types.SelectMany(type => type.Members.Select(member => new MemberEntry(
                DisplayName: member.DisplayName,
                Category: member.Category,
                Signature: member.Signature,
                Summary: member.Summary,
                DeclaringTypeName: type.DisplayName,
                NamespaceName: type.Namespace,
                AssemblyName: page.AssemblyName,
                FileName: page.FileName,
                AnchorId: member.AnchorId))))
            .OrderBy(static entry => entry.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static entry => entry.DeclaringTypeName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static entry => entry.NamespaceName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static entry => entry.AssemblyName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static entry => entry.Category, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var builder = new StringBuilder();
        builder.AppendLine("# Member Index");
        builder.AppendLine();
        builder.AppendLine("Browse the published API surface by public member.");
        builder.AppendLine();
        builder.AppendLine("[Back to reference index](README.md)");

        foreach (var group in entries.GroupBy(static entry => GetAlphabetBucket(entry.DisplayName)))
        {
            builder.AppendLine();
            builder.Append("## ").AppendLine(group.Key);
            builder.AppendLine();

            foreach (var entry in group)
            {
                builder.Append("- [")
                    .Append(entry.DisplayName)
                    .Append("](")
                    .Append(entry.FileName)
                    .Append('#')
                    .Append(entry.AnchorId)
                    .Append(')')
                    .Append(": `")
                    .Append(entry.Category)
                    .Append("` on `")
                    .Append(entry.DeclaringTypeName)
                    .Append("` in `")
                    .Append(entry.NamespaceName)
                    .Append("` (`")
                    .Append(entry.AssemblyName)
                    .Append("`) ")
                    .Append("[Browse](")
                    .Append(BuildBrowserLink(
                        query: entry.DisplayName,
                        assemblyName: entry.AssemblyName,
                        namespaceName: entry.NamespaceName,
                        scope: "members"))
                    .AppendLine(")");

                if (!string.IsNullOrWhiteSpace(entry.Summary))
                {
                    builder.Append("  - ").AppendLine(entry.Summary);
                }

                builder.Append("  - `").Append(entry.Signature).AppendLine("`");
            }
        }

        return builder.ToString().TrimEnd() + Environment.NewLine;
    }

    private static string BuildNamespaceIndex(IReadOnlyList<AssemblyPage> pages)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Namespace Index");
        builder.AppendLine();
        builder.AppendLine("Browse the published API surface by namespace.");
        builder.AppendLine();
        builder.AppendLine("[Back to reference index](README.md)");

        foreach (var namespaceGroup in pages
                     .SelectMany(static page => page.Types.Select(type => new NamespaceEntry(
                         Namespace: type.Namespace,
                         AssemblyName: page.AssemblyName,
                         FileName: page.FileName,
                         TypeCount: page.Types.Count(candidate => string.Equals(candidate.Namespace, type.Namespace, StringComparison.OrdinalIgnoreCase)))))
                     .Distinct()
                     .OrderBy(static entry => entry.Namespace, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(static entry => entry.AssemblyName, StringComparer.OrdinalIgnoreCase)
                     .GroupBy(static entry => entry.Namespace, StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine();
            builder.Append("## ").Append('`').Append(namespaceGroup.Key).AppendLine("`");
            builder.AppendLine();

            foreach (var entry in namespaceGroup)
            {
                builder.Append("- [")
                    .Append(entry.AssemblyName)
                    .Append("](")
                    .Append(entry.FileName)
                    .Append('#')
                    .Append(GetNamespaceAnchorId(entry.Namespace))
                    .Append(')')
                    .Append(": ")
                    .Append(entry.TypeCount)
                    .Append(" public types ")
                    .Append("[Browse](")
                    .Append(BuildBrowserLink(assemblyName: entry.AssemblyName, namespaceName: entry.Namespace))
                    .AppendLine(")");
            }
        }

        return builder.ToString().TrimEnd() + Environment.NewLine;
    }

    private static string BuildTypeIndex(IReadOnlyList<AssemblyPage> pages)
    {
        var entries = pages
            .SelectMany(static page => page.Types.Select(type => new TypeEntry(
                DisplayName: type.DisplayName,
                Namespace: type.Namespace,
                AssemblyName: page.AssemblyName,
                FileName: page.FileName,
                AnchorId: GetTypeAnchorId(type))))
            .OrderBy(static entry => entry.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static entry => entry.Namespace, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static entry => entry.AssemblyName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var builder = new StringBuilder();
        builder.AppendLine("# Type Index");
        builder.AppendLine();
        builder.AppendLine("Browse the published API surface by public type.");
        builder.AppendLine();
        builder.AppendLine("[Back to reference index](README.md)");

        foreach (var group in entries.GroupBy(static entry => GetAlphabetBucket(entry.DisplayName)))
        {
            builder.AppendLine();
            builder.Append("## ").AppendLine(group.Key);
            builder.AppendLine();

            foreach (var entry in group)
            {
                builder.Append("- [")
                    .Append(entry.DisplayName)
                    .Append("](")
                    .Append(entry.FileName)
                    .Append('#')
                    .Append(entry.AnchorId)
                    .Append(')')
                    .Append(": `")
                    .Append(entry.Namespace)
                    .Append("` in `")
                    .Append(entry.AssemblyName)
                    .Append("` ")
                    .Append("[Browse](")
                    .Append(BuildBrowserLink(
                        query: entry.DisplayName,
                        assemblyName: entry.AssemblyName,
                        namespaceName: entry.Namespace))
                    .AppendLine(")");
            }
        }

        return builder.ToString().TrimEnd() + Environment.NewLine;
    }

    private static string BuildReferenceManifest(IReadOnlyList<AssemblyPage> pages)
    {
        var manifest = new ReferenceManifest(
            SchemaVersion: 2,
            GeneratedAtUtc: DateTimeOffset.UtcNow,
            Assemblies: pages
                .Select(static page =>
                {
                    var metadata = GetAssemblyMetadata(page.AssemblyName);
                    return new AssemblyManifestEntry(
                        AssemblyName: page.AssemblyName,
                        Category: metadata.Category,
                        Description: metadata.Description,
                        FileName: page.FileName,
                        NamespaceCount: page.NamespaceCount,
                        TypeCount: page.TypeCount);
                })
                .ToArray(),
            Namespaces: pages
                .SelectMany(static page => page.Types
                    .GroupBy(static type => type.Namespace, StringComparer.OrdinalIgnoreCase)
                    .Select(group => new NamespaceManifestEntry(
                        NamespaceName: group.Key,
                        AssemblyName: page.AssemblyName,
                        FileName: page.FileName,
                        AnchorId: GetNamespaceAnchorId(group.Key),
                        TypeCount: group.Count())))
                .OrderBy(static entry => entry.NamespaceName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static entry => entry.AssemblyName, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            Types: pages
                .SelectMany(static page => page.Types.Select(type => new TypeManifestEntry(
                    DisplayName: type.DisplayName,
                    NamespaceName: type.Namespace,
                    AssemblyName: page.AssemblyName,
                    FileName: page.FileName,
                    AnchorId: GetTypeAnchorId(type),
                    Declaration: type.Declaration,
                    Summary: type.Summary,
                    MemberCounts: new MemberCategoryCounts(
                        Constructors: CountMembers(type, "Constructors"),
                        Fields: CountMembers(type, "Fields"),
                        Properties: CountMembers(type, "Properties"),
                        Methods: CountMembers(type, "Methods")))))
                .OrderBy(static entry => entry.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static entry => entry.NamespaceName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static entry => entry.AssemblyName, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            Members: pages
                .SelectMany(static page => page.Types.SelectMany(type => type.Members.Select(member => new MemberManifestEntry(
                    DisplayName: member.DisplayName,
                    Category: member.Category,
                    DeclaringTypeName: type.DisplayName,
                    NamespaceName: type.Namespace,
                    AssemblyName: page.AssemblyName,
                    FileName: page.FileName,
                    AnchorId: member.AnchorId,
                    Signature: member.Signature,
                    Summary: member.Summary))))
                .OrderBy(static entry => entry.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static entry => entry.DeclaringTypeName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static entry => entry.NamespaceName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static entry => entry.AssemblyName, StringComparer.OrdinalIgnoreCase)
                .ToArray());

        return JsonSerializer.Serialize(manifest, ReferenceManifestJsonOptions);
    }

    private static void AppendTextBlock(StringBuilder builder, string? text, string? label = null)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        builder.AppendLine();
        if (!string.IsNullOrWhiteSpace(label))
        {
            builder.Append(label).Append(": ");
        }

        builder.AppendLine(text);
    }

    private static string GetTypeDocId(Type type)
    {
        return $"T:{GetDocumentationTypeName(type)}";
    }

    private static string GetMemberDocId(MemberInfo member)
    {
        return member switch
        {
            Type type => GetTypeDocId(type),
            ConstructorInfo constructor => BuildMethodDocId(constructor.DeclaringType!, "#ctor", constructor.GetParameters(), genericArity: 0),
            MethodInfo method => BuildMethodDocId(
                method.DeclaringType!,
                method.Name,
                method.GetParameters(),
                method.IsGenericMethodDefinition ? method.GetGenericArguments().Length : 0),
            PropertyInfo property => $"P:{GetDocumentationTypeName(property.DeclaringType!)}.{property.Name}",
            FieldInfo field => $"F:{GetDocumentationTypeName(field.DeclaringType!)}.{field.Name}",
            _ => throw new InvalidOperationException($"Member '{member.Name}' is not supported by the reference docs generator.")
        };
    }

    private static string BuildMethodDocId(
        Type declaringType,
        string methodName,
        ParameterInfo[] parameters,
        int genericArity)
    {
        var builder = new StringBuilder();
        builder.Append("M:")
            .Append(GetDocumentationTypeName(declaringType))
            .Append('.')
            .Append(methodName);

        if (genericArity > 0)
        {
            builder.Append("``").Append(genericArity);
        }

        if (parameters.Length > 0)
        {
            builder.Append('(')
                .Append(string.Join(",", parameters.Select(static parameter => GetDocumentationTypeName(parameter.ParameterType))))
                .Append(')');
        }

        return builder.ToString();
    }

    private static string GetDocumentationTypeName(Type type)
    {
        if (type.IsByRef)
        {
            return GetDocumentationTypeName(type.GetElementType()!) + "@";
        }

        if (type.IsPointer)
        {
            return GetDocumentationTypeName(type.GetElementType()!) + "*";
        }

        if (type.IsArray)
        {
            var commas = new string(',', type.GetArrayRank() - 1);
            return $"{GetDocumentationTypeName(type.GetElementType()!)}[{commas}]";
        }

        if (type.IsGenericParameter)
        {
            return type.DeclaringMethod is null
                ? $"`{type.GenericParameterPosition}"
                : $"``{type.GenericParameterPosition}";
        }

        if (type.IsGenericType)
        {
            var definition = type.GetGenericTypeDefinition();
            var definitionName = StripGenericArity((definition.FullName ?? definition.Name).Replace('+', '.'));
            var arguments = string.Join(",", type.GetGenericArguments().Select(GetDocumentationTypeName));
            return $"{definitionName}{{{arguments}}}";
        }

        return (type.FullName ?? type.Name).Replace('+', '.');
    }

    private static string GetMemberDisplayName(MemberInfo member)
    {
        return member switch
        {
            ConstructorInfo constructor => constructor.DeclaringType is null
                ? "#ctor"
                : FormatTypeName(constructor.DeclaringType),
            MethodInfo method => method.Name,
            PropertyInfo property => property.Name,
            FieldInfo field => field.Name,
            _ => member.Name
        };
    }

    private static string BuildTypeDeclaration(Type type)
    {
        var modifiers = new List<string> { "public" };
        if (type.IsAbstract && type.IsSealed)
        {
            modifiers.Add("static");
        }
        else
        {
            if (type.IsAbstract && !type.IsInterface)
            {
                modifiers.Add("abstract");
            }

            if (type.IsSealed && !type.IsValueType && !type.IsEnum && !type.IsInterface)
            {
                modifiers.Add("sealed");
            }
        }

        modifiers.Add(type.IsInterface ? "interface" :
            type.IsEnum ? "enum" :
            type.IsValueType && !type.IsPrimitive ? "struct" :
            "class");
        modifiers.Add(FormatTypeName(type));
        return string.Join(" ", modifiers);
    }

    private static string BuildMemberSignature(MemberInfo member)
    {
        return member switch
        {
            ConstructorInfo constructor => BuildConstructorSignature(constructor),
            MethodInfo method => BuildMethodSignature(method),
            PropertyInfo property => BuildPropertySignature(property),
            FieldInfo field => BuildFieldSignature(field),
            _ => member.Name
        };
    }

    private static string BuildConstructorSignature(ConstructorInfo constructor)
    {
        return $"{FormatTypeName(constructor.DeclaringType!)}({string.Join(", ", constructor.GetParameters().Select(static parameter => FormatParameter(parameter)))})";
    }

    private static string BuildMethodSignature(MethodInfo method)
    {
        var builder = new StringBuilder();
        builder.Append(FormatTypeName(method.ReturnType))
            .Append(' ')
            .Append(method.Name);

        if (method.IsGenericMethodDefinition)
        {
            builder.Append('<')
                .Append(string.Join(", ", method.GetGenericArguments().Select(static argument => argument.Name)))
                .Append('>');
        }

        builder.Append('(');
        var parameters = method.GetParameters();
        for (var index = 0; index < parameters.Length; index++)
        {
            if (index > 0)
            {
                builder.Append(", ");
            }

            builder.Append(FormatParameter(parameters[index], index == 0 && method.IsDefined(typeof(ExtensionAttribute), inherit: false)));
        }

        builder.Append(')');
        return builder.ToString();
    }

    private static string BuildPropertySignature(PropertyInfo property)
    {
        var accessors = new List<string>();
        if (property.GetMethod is not null && property.GetMethod.IsPublic)
        {
            accessors.Add("get;");
        }

        if (property.SetMethod is not null && property.SetMethod.IsPublic)
        {
            accessors.Add("set;");
        }

        return $"{FormatTypeName(property.PropertyType)} {property.Name} {{ {string.Join(' ', accessors)} }}";
    }

    private static string BuildFieldSignature(FieldInfo field)
    {
        var modifier = field.IsLiteral && !field.IsInitOnly
            ? "const"
            : field.IsStatic
                ? "static"
                : string.Empty;

        return string.IsNullOrWhiteSpace(modifier)
            ? $"{FormatTypeName(field.FieldType)} {field.Name}"
            : $"{modifier} {FormatTypeName(field.FieldType)} {field.Name}";
    }

    private static string FormatParameter(ParameterInfo parameter, bool isExtensionReceiver = false)
    {
        var prefix = new List<string>();
        if (isExtensionReceiver)
        {
            prefix.Add("this");
        }

        if (parameter.ParameterType.IsByRef)
        {
            prefix.Add(parameter.IsOut ? "out" : "ref");
        }

        var parameterType = parameter.ParameterType.IsByRef
            ? parameter.ParameterType.GetElementType()!
            : parameter.ParameterType;

        var parts = new List<string>(prefix)
        {
            FormatTypeName(parameterType),
            parameter.Name ?? "value"
        };

        return string.Join(" ", parts);
    }

    private static string FormatTypeName(Type type)
    {
        if (type.IsByRef)
        {
            return FormatTypeName(type.GetElementType()!);
        }

        if (type.IsArray)
        {
            return $"{FormatTypeName(type.GetElementType()!)}[]";
        }

        if (type.IsGenericParameter)
        {
            return type.Name;
        }

        if (TryGetAlias(type, out var alias))
        {
            return alias;
        }

        if (IsNullableValueType(type, out var nullableType))
        {
            return $"{FormatTypeName(nullableType!)}?";
        }

        if (type.IsGenericType)
        {
            var name = RemoveGenericSuffix(type.Name);
            var arguments = type.GetGenericArguments();
            return $"{name}<{string.Join(", ", arguments.Select(FormatTypeName))}>";
        }

        return type.Name.Replace('+', '.');
    }

    private static bool TryGetAlias(Type type, out string alias)
    {
        alias = type.FullName switch
        {
            "System.Boolean" => "bool",
            "System.Byte" => "byte",
            "System.Char" => "char",
            "System.Decimal" => "decimal",
            "System.Double" => "double",
            "System.Int16" => "short",
            "System.Int32" => "int",
            "System.Int64" => "long",
            "System.Object" => "object",
            "System.Single" => "float",
            "System.String" => "string",
            "System.Void" => "void",
            _ => string.Empty
        };

        return alias.Length > 0;
    }

    private static bool IsNullableValueType(Type type, out Type? nullableType)
    {
        nullableType = Nullable.GetUnderlyingType(type);
        return nullableType is not null;
    }

    private static string RemoveGenericSuffix(string value)
    {
        var index = value.IndexOf('`');
        return index < 0 ? value : value[..index];
    }

    private static string StripGenericArity(string value)
    {
        var builder = new StringBuilder(value.Length);

        for (var index = 0; index < value.Length; index++)
        {
            if (value[index] == '`')
            {
                index++;
                while (index < value.Length && char.IsDigit(value[index]))
                {
                    index++;
                }

                index--;
                continue;
            }

            builder.Append(value[index]);
        }

        return builder.ToString();
    }

    private static NamedDocumentation? CreateNamedDocumentation(XElement element)
    {
        var name = ((string?)element.Attribute("name"))?.Trim();
        var text = RenderElement(element);
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        return new NamedDocumentation(name, text);
    }

    private static string? RenderElement(XElement? element)
    {
        if (element is null)
        {
            return null;
        }

        var text = RenderNodes(element.Nodes());
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var normalized = text
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');

        var paragraphs = normalized
            .Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(static paragraph => string.Join(
                " ",
                paragraph.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)))
            .Where(static paragraph => !string.IsNullOrWhiteSpace(paragraph))
            .ToArray();

        return paragraphs.Length == 0
            ? null
            : string.Join(Environment.NewLine + Environment.NewLine, paragraphs);
    }

    private static string RenderNodes(IEnumerable<XNode> nodes)
    {
        var builder = new StringBuilder();
        foreach (var node in nodes)
        {
            builder.Append(RenderNode(node));
        }

        return builder.ToString();
    }

    private static string RenderNode(XNode node)
    {
        return node switch
        {
            XText text => text.Value,
            XElement element => RenderElementNode(element),
            _ => string.Empty
        };
    }

    private static string RenderElementNode(XElement element)
    {
        return element.Name.LocalName switch
        {
            "para" => Environment.NewLine + Environment.NewLine + RenderNodes(element.Nodes()).Trim() + Environment.NewLine + Environment.NewLine,
            "see" => RenderSeeElement(element),
            "seealso" => RenderSeeElement(element),
            "paramref" => $"`{(string?)element.Attribute("name") ?? "value"}`",
            "typeparamref" => $"`{(string?)element.Attribute("name") ?? "T"}`",
            "c" => $"`{RenderNodes(element.Nodes()).Trim()}`",
            "code" => Environment.NewLine + "```text" + Environment.NewLine + element.Value.Trim() + Environment.NewLine + "```" + Environment.NewLine,
            _ => RenderNodes(element.Nodes())
        };
    }

    private static string RenderSeeElement(XElement element)
    {
        var langword = (string?)element.Attribute("langword");
        if (!string.IsNullOrWhiteSpace(langword))
        {
            return $"`{langword}`";
        }

        var cref = (string?)element.Attribute("cref");
        if (!string.IsNullOrWhiteSpace(cref))
        {
            return $"`{FormatCref(cref)}`";
        }

        return $"`{RenderNodes(element.Nodes()).Trim()}`";
    }

    private static string FormatCref(string cref)
    {
        var normalized = cref.Trim();
        var colonIndex = normalized.IndexOf(':');
        if (colonIndex >= 0)
        {
            normalized = normalized[(colonIndex + 1)..];
        }

        var parenIndex = normalized.IndexOf('(');
        if (parenIndex >= 0)
        {
            normalized = normalized[..parenIndex];
        }

        var genericIndex = normalized.IndexOf('{');
        if (genericIndex >= 0)
        {
            normalized = normalized[..genericIndex];
        }

        normalized = normalized.Replace("#ctor", "ctor", StringComparison.Ordinal);
        normalized = normalized[(normalized.LastIndexOf('.') + 1)..];
        normalized = normalized.Replace("``1", "<T>", StringComparison.Ordinal);
        normalized = normalized.Replace("``2", "<T1, T2>", StringComparison.Ordinal);
        normalized = normalized.Replace("`1", "<T>", StringComparison.Ordinal);
        normalized = normalized.Replace("`2", "<T1, T2>", StringComparison.Ordinal);
        return normalized;
    }

    private static string ToSlug(string value)
    {
        var builder = new StringBuilder(value.Length);
        var previousWasSeparator = false;

        foreach (var character in value.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                previousWasSeparator = false;
            }
            else if (!previousWasSeparator)
            {
                builder.Append('-');
                previousWasSeparator = true;
            }
        }

        return builder.ToString().Trim('-');
    }

    private static string GetNamespaceAnchorId(string namespaceName)
    {
        return $"namespace-{ToSlug(namespaceName)}";
    }

    private static string GetTypeAnchorId(TypePage type)
    {
        return $"type-{ToSlug($"{type.Namespace}-{type.DisplayName}")}";
    }

    private static string GetMemberAnchorId(string memberDocId)
    {
        return $"member-{ToSlug(memberDocId)}";
    }

    private static string GetAlphabetBucket(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "#";
        }

        var firstCharacter = char.ToUpperInvariant(value[0]);
        return char.IsLetter(firstCharacter) ? firstCharacter.ToString() : "#";
    }

    private static string BuildBrowserLink(
        string? query = null,
        string? category = null,
        string? assemblyName = null,
        string? namespaceName = null,
        string? scope = null)
    {
        var parameters = new List<string>();
        AddBrowserParameter(parameters, "q", query);
        AddBrowserParameter(parameters, "category", category);
        AddBrowserParameter(parameters, "assembly", assemblyName);
        AddBrowserParameter(parameters, "namespace", namespaceName);
        AddBrowserParameter(parameters, "scope", scope);

        return parameters.Count == 0
            ? "browse.html"
            : $"browse.html?{string.Join("&", parameters)}";
    }

    private static void AddBrowserParameter(List<string> parameters, string name, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        parameters.Add($"{name}={Uri.EscapeDataString(value.Trim())}");
    }

    private static int CountMembers(TypePage type, string category)
    {
        return type.Members.Count(member => string.Equals(member.Category, category, StringComparison.Ordinal));
    }

    private static AssemblyMetadata GetAssemblyMetadata(string assemblyName)
    {
        return AssemblyCatalog.FirstOrDefault(
                   metadata => string.Equals(metadata.AssemblyName, assemblyName, StringComparison.OrdinalIgnoreCase))
               ?? new AssemblyMetadata(assemblyName, "Other", "Public API surface.");
    }

    private sealed record AssemblyPage(
        string AssemblyName,
        string FileName,
        int NamespaceCount,
        int TypeCount,
        IReadOnlyList<TypePage> Types,
        string Markdown);

    private sealed record TypePage(
        string Namespace,
        string DisplayName,
        string Declaration,
        string? Summary,
        string? Remarks,
        IReadOnlyList<MemberPage> Members);

    private sealed record MemberPage(
        string AnchorId,
        string Category,
        string DisplayName,
        string Signature,
        string? Summary,
        string? Remarks,
        string? Returns,
        IReadOnlyList<NamedDocumentation> Parameters,
        IReadOnlyList<NamedDocumentation> TypeParameters);

    private sealed record NamedDocumentation(string Name, string Text);

    private sealed record AssemblyMetadata(string AssemblyName, string Category, string Description);

    private sealed record NamespaceEntry(string Namespace, string AssemblyName, string FileName, int TypeCount);

    private sealed record TypeEntry(string DisplayName, string Namespace, string AssemblyName, string FileName, string AnchorId);

    private sealed record MemberEntry(
        string DisplayName,
        string Category,
        string Signature,
        string? Summary,
        string DeclaringTypeName,
        string NamespaceName,
        string AssemblyName,
        string FileName,
        string AnchorId);

    private sealed record ReferenceManifest(
        int SchemaVersion,
        DateTimeOffset GeneratedAtUtc,
        IReadOnlyList<AssemblyManifestEntry> Assemblies,
        IReadOnlyList<NamespaceManifestEntry> Namespaces,
        IReadOnlyList<TypeManifestEntry> Types,
        IReadOnlyList<MemberManifestEntry> Members);

    private sealed record AssemblyManifestEntry(
        string AssemblyName,
        string Category,
        string Description,
        string FileName,
        int NamespaceCount,
        int TypeCount);

    private sealed record NamespaceManifestEntry(
        string NamespaceName,
        string AssemblyName,
        string FileName,
        string AnchorId,
        int TypeCount);

    private sealed record TypeManifestEntry(
        string DisplayName,
        string NamespaceName,
        string AssemblyName,
        string FileName,
        string AnchorId,
        string Declaration,
        string? Summary,
        MemberCategoryCounts MemberCounts);

    private sealed record MemberManifestEntry(
        string DisplayName,
        string Category,
        string DeclaringTypeName,
        string NamespaceName,
        string AssemblyName,
        string FileName,
        string AnchorId,
        string Signature,
        string? Summary);

    private sealed record MemberCategoryCounts(
        int Constructors,
        int Fields,
        int Properties,
        int Methods);
}
