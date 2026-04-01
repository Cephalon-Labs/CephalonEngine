using System.Text;
using Cephalon.Abstractions.AppModel;
using Cephalon.Abstractions.AppModel.Scaffolding;

namespace Cephalon.Scaffolding.Generation;

/// <summary>
/// Turns a Cephalon app profile and scaffold request into concrete projects, folders, and files.
/// </summary>
public sealed class ScaffoldGenerator
{
    private const string PackageManifestFileName = "cephalon.package.json";

    /// <summary>
    /// Generates a rendered scaffold from the supplied application profile and request.
    /// </summary>
    /// <param name="appProfile">The application profile that carries scaffold guidance.</param>
    /// <param name="request">The concrete naming and framework request for generation.</param>
    /// <returns>The rendered scaffold output.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the supplied app profile does not carry a scaffold plan or when required scoped
    /// values such as modules or features are missing for the selected blueprint.
    /// </exception>
    public static RenderedScaffold Generate(AppProfile appProfile, ScaffoldRequest request)
    {
        ArgumentNullException.ThrowIfNull(appProfile);
        ArgumentNullException.ThrowIfNull(request);

        if (appProfile.Scaffold is null)
        {
            throw new InvalidOperationException(
                $"App profile '{appProfile.BlueprintId}' does not carry a scaffold plan.");
        }

        var projectsBySourceId = new Dictionary<string, List<ProjectInstance>>(StringComparer.OrdinalIgnoreCase);

        foreach (var project in appProfile.Scaffold.Projects)
        {
            projectsBySourceId[project.Id] = ExpandProject(project, request).ToList();
        }

        var renderedProjects = projectsBySourceId.Values
            .SelectMany(projects => projects)
            .Select(instance => instance.ToRenderedProject(projectsBySourceId))
            .OrderBy(project => project.Path, StringComparer.OrdinalIgnoreCase)
            .ThenBy(project => project.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var renderedFolders = ExpandFolders(appProfile.Scaffold, request, projectsBySourceId)
            .OrderBy(folder => folder.Path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var renderedFiles = BuildFiles(appProfile, request, renderedProjects, renderedFolders)
            .OrderBy(file => file.Path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new RenderedScaffold(appProfile, request, renderedProjects, renderedFolders, renderedFiles);
    }

    private static IEnumerable<ProjectInstance> ExpandProject(
        ScaffoldProject project,
        ScaffoldRequest request)
    {
        return project.Scope switch
        {
            ScaffoldScopes.Solution => [CreateProjectInstance(project, request)],
            ScaffoldScopes.Module => request.Modules.Count == 0
                ? throw new InvalidOperationException(
                    $"Scaffold project '{project.Id}' requires at least one module name.")
                : request.Modules.Select(moduleName => CreateProjectInstance(project, request, moduleName: moduleName)),
            ScaffoldScopes.Feature => request.Features.Count == 0
                ? throw new InvalidOperationException(
                    $"Scaffold project '{project.Id}' requires at least one feature name.")
                : request.Features.Select(featureName => CreateProjectInstance(project, request, featureName: featureName)),
            _ => throw new InvalidOperationException(
                $"Scaffold scope '{project.Scope}' is not supported by the generator.")
        };
    }

    private static ProjectInstance CreateProjectInstance(
        ScaffoldProject project,
        ScaffoldRequest request,
        string? moduleName = null,
        string? featureName = null)
    {
        var tokens = CreateTokens(request, moduleName, featureName);
        var name = ReplaceTokens(project.NameTemplate, tokens);
        var path = ReplaceTokens(project.PathTemplate, tokens);

        return new ProjectInstance(
            source: project,
            key: BuildKey(project.Id, moduleName, featureName),
            name: name,
            path: NormalizePath(path),
            projectFilePath: NormalizePath(Path.Combine(path, $"{name}.csproj")),
            moduleName: moduleName,
            featureName: featureName,
            tokens: tokens);
    }

    private static IEnumerable<RenderedFolder> ExpandFolders(
        ScaffoldPlan plan,
        ScaffoldRequest request,
        IReadOnlyDictionary<string, List<ProjectInstance>> projectsBySourceId)
    {
        foreach (var folder in plan.Folders)
        {
            if (folder.ProjectId is null ||
                !projectsBySourceId.TryGetValue(folder.ProjectId, out var owners))
            {
                continue;
            }

            foreach (var owner in owners)
            {
                if (folder.Scope == ScaffoldScopes.Feature)
                {
                    foreach (var featureName in request.Features)
                    {
                        var featureTokens = CreateTokens(request, owner.ModuleName, featureName);
                        yield return new RenderedFolder(
                            path: NormalizePath(Path.Combine(owner.Path, ReplaceTokens(folder.PathTemplate, featureTokens))),
                            purpose: folder.Purpose,
                            scope: folder.Scope,
                            projectKey: owner.Key,
                            metadata: folder.Metadata);
                    }

                    continue;
                }

                yield return new RenderedFolder(
                    path: NormalizePath(Path.Combine(owner.Path, ReplaceTokens(folder.PathTemplate, owner.Tokens))),
                    purpose: folder.Purpose,
                    scope: folder.Scope,
                    projectKey: owner.Key,
                    metadata: folder.Metadata);
            }
        }
    }

    private static RenderedFile[] BuildFiles(
        AppProfile appProfile,
        ScaffoldRequest request,
        IReadOnlyList<RenderedProject> projects,
        IReadOnlyList<RenderedFolder> folders)
    {
        var files = new List<RenderedFile>
        {
            new($"{request.AppName}.slnx", BuildSolutionFile(projects)),
            new("Directory.Build.props", BuildDirectoryBuildProps()),
            new("Directory.Packages.props", BuildDirectoryPackagesProps(projects, request)),
            new("README.md", BuildReadme(appProfile, request))
        };

        foreach (var project in projects)
        {
            files.Add(new(
                Path.Combine(project.Path, $"{project.Name}.csproj"),
                BuildProjectFile(project, request)));

            switch (project.Template)
            {
                case "cephalon-web-host":
                case "cephalon-service-host":
                    files.Add(new(Path.Combine(project.Path, "Program.cs"), BuildHostProgram(appProfile, request)));
                    files.Add(new(Path.Combine(project.Path, "appsettings.json"), BuildHostSettings(appProfile, request)));
                    break;
                case "cephalon-foundation":
                    files.Add(new(Path.Combine(project.Path, "AssemblyMarker.cs"), BuildFoundationMarker(request)));
                    break;
                case "cephalon-contracts":
                    files.Add(new(Path.Combine(project.Path, "Contracts", "GreetingContract.cs"), BuildContractFile(request)));
                    break;
                case "cephalon-module":
                    files.Add(new(
                        Path.Combine(project.Path, $"{ResolveModuleTypeName(project)}Module.cs"),
                        BuildModuleFile(appProfile, project, request)));
                    files.Add(new(
                        Path.Combine(project.Path, PackageManifestFileName),
                        BuildPackageManifest(project, request)));
                    break;
                case "cephalon-tests":
                    files.Add(new(Path.Combine(project.Path, "SmokeTests.cs"), BuildSmokeTest(request)));
                    break;
            }
        }

        foreach (var folder in folders.Where(folder => folder.Scope == ScaffoldScopes.Feature))
        {
            files.Add(new(Path.Combine(folder.Path, ".gitkeep"), string.Empty));
        }

        return files
            .Select(file => new RenderedFile(NormalizePath(file.Path), file.Contents))
            .ToArray();
    }

    private static string BuildSolutionFile(IReadOnlyList<RenderedProject> projects)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<Solution>");

        foreach (var folder in projects
                     .Select(project => project.Path.Split('/', StringSplitOptions.RemoveEmptyEntries)[0])
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .OrderBy(folder => folder, StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine("  <Folder Name=\"/" + folder + "/\">");

            foreach (var project in projects
                         .Where(project => project.Path.StartsWith($"{folder}/", StringComparison.OrdinalIgnoreCase))
                         .OrderBy(project => project.Path, StringComparer.OrdinalIgnoreCase))
            {
                builder.AppendLine("    <Project Path=\"" + project.Path + "/" + project.Name + ".csproj\" />");
            }

            builder.AppendLine("  </Folder>");
        }

        builder.AppendLine("</Solution>");
        return builder.ToString();
    }

    private static string BuildDirectoryBuildProps()
    {
        return """
<Project>
  <PropertyGroup>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);CS1591</NoWarn>
  </PropertyGroup>
</Project>
""";
    }

    private static string BuildDirectoryPackagesProps(
        IReadOnlyList<RenderedProject> projects,
        ScaffoldRequest request)
    {
        var packages = projects
            .SelectMany(project => project.Packages)
            .Concat(PackageVersionCatalog.GetTestInfrastructurePackages())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(package => package, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var builder = new StringBuilder();
        builder.AppendLine("<Project>");
        builder.AppendLine("  <PropertyGroup>");
        builder.AppendLine("    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>");
        builder.AppendLine("  </PropertyGroup>");
        builder.AppendLine();
        builder.AppendLine("  <ItemGroup>");

        foreach (var package in packages)
        {
            builder.AppendLine(
                "    <PackageVersion Include=\"" + package + "\" Version=\"" + PackageVersionCatalog.Resolve(package, request.CephalonPackageVersion) + "\" />");
        }

        builder.AppendLine("  </ItemGroup>");
        builder.AppendLine("</Project>");
        return builder.ToString();
    }

    private static string BuildReadme(AppProfile appProfile, ScaffoldRequest request)
    {
        var transports = appProfile.Transports.Count == 0
            ? "None"
            : string.Join(", ", appProfile.Transports.Select(transport => transport.DisplayName));
        var technologies = appProfile.Technologies.Count == 0
            ? "None"
            : string.Join(", ", appProfile.Technologies.Select(technology => $"`{technology.DisplayName}`"));
        var modules = request.Modules.Count == 0
            ? "None"
            : string.Join(", ", request.Modules.Select(module => $"`{module}`"));

        return $"""
# {request.AppName}

Generated from the Cephalon `{appProfile.BlueprintDisplayName}` blueprint.

## Included shape

- Blueprint: `{appProfile.BlueprintDisplayName}`
- Patterns: {string.Join(", ", appProfile.Patterns.Select(pattern => $"`{pattern.DisplayName}`"))}
- Technologies: {technologies}
- Transports: {transports}
- Modules: {modules}

## Next steps

1. Adjust package versions in `Directory.Packages.props` if needed.
2. Flesh out module services, capabilities, and transport adapters.
3. Add feature handlers inside the generated folders for each module.
4. Add project-specific languages or replace the localization catalog through `Engine:Localization` and DI.
5. Generate and publish API reference docs before enabling the shipped `ReferenceDocs` host section.
6. Split host settings into `Configurations/Add*.json` and `Configurations/[group]/[Environment].json` when appsettings starts getting too large.
""";
    }

    private static string BuildProjectFile(RenderedProject project, ScaffoldRequest request)
    {
        var sdk = project.Template is "cephalon-web-host" or "cephalon-service-host"
            ? "Microsoft.NET.Sdk.Web"
            : "Microsoft.NET.Sdk";
        var builder = new StringBuilder();
        builder.AppendLine("<Project Sdk=\"" + sdk + "\">");
        builder.AppendLine("  <PropertyGroup>");
        builder.AppendLine("    <TargetFramework>" + request.TargetFramework + "</TargetFramework>");

        if (project.Template == "cephalon-tests")
        {
            builder.AppendLine("    <IsPackable>false</IsPackable>");
        }

        builder.AppendLine("  </PropertyGroup>");

        if (project.Template is "cephalon-web-host" or "cephalon-service-host")
        {
            builder.AppendLine();
            builder.AppendLine("  <ItemGroup>");
            builder.AppendLine("    <FrameworkReference Include=\"Microsoft.AspNetCore.App\" />");
            builder.AppendLine("  </ItemGroup>");
            builder.AppendLine();
            builder.AppendLine("  <ItemGroup>");
            builder.AppendLine("    <Content Include=\"Configurations\\**\\*.json\">");
            builder.AppendLine("      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>");
            builder.AppendLine("      <CopyToPublishDirectory>PreserveNewest</CopyToPublishDirectory>");
            builder.AppendLine("    </Content>");
            builder.AppendLine("  </ItemGroup>");
        }

        var packageReferences = project.Template == "cephalon-tests"
            ? project.Packages
                .Concat(PackageVersionCatalog.GetTestInfrastructurePackages())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(package => package, StringComparer.OrdinalIgnoreCase)
                .ToArray()
            : project.Packages
                .OrderBy(package => package, StringComparer.OrdinalIgnoreCase)
                .ToArray();

        if (packageReferences.Length > 0)
        {
            builder.AppendLine();
            builder.AppendLine("  <ItemGroup>");

            foreach (var package in packageReferences)
            {
                if (project.Template == "cephalon-tests" && package == "coverlet.collector")
                {
                    builder.AppendLine("    <PackageReference Include=\"coverlet.collector\">");
                    builder.AppendLine("      <PrivateAssets>all</PrivateAssets>");
                    builder.AppendLine("      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>");
                    builder.AppendLine("    </PackageReference>");
                    continue;
                }

                if (project.Template == "cephalon-tests" && package == "xunit.runner.visualstudio")
                {
                    builder.AppendLine("    <PackageReference Include=\"xunit.runner.visualstudio\">");
                    builder.AppendLine("      <PrivateAssets>all</PrivateAssets>");
                    builder.AppendLine("      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>");
                    builder.AppendLine("    </PackageReference>");
                    continue;
                }

                builder.AppendLine("    <PackageReference Include=\"" + package + "\" />");
            }

            builder.AppendLine("  </ItemGroup>");
        }

        if (project.Template == "cephalon-tests")
        {
            builder.AppendLine();
            builder.AppendLine("  <ItemGroup>");
            builder.AppendLine("    <Using Include=\"Xunit\" />");
            builder.AppendLine("  </ItemGroup>");
        }

        if (project.ProjectReferences.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("  <ItemGroup>");
            foreach (var reference in project.ProjectReferences.OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                builder.AppendLine("    <ProjectReference Include=\"" + NormalizePath(reference) + "\" />");
            }

            builder.AppendLine("  </ItemGroup>");
        }

        if (project.Template == "cephalon-module")
        {
            builder.AppendLine();
            builder.AppendLine("  <ItemGroup>");
            builder.AppendLine("    <Content Include=\"" + PackageManifestFileName + "\">");
            builder.AppendLine("      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>");
            builder.AppendLine("    </Content>");
            builder.AppendLine("  </ItemGroup>");
        }

        builder.AppendLine("</Project>");
        return builder.ToString();
    }

    private static string BuildHostProgram(AppProfile appProfile, ScaffoldRequest request)
    {
        var usingLines = new List<string>
        {
            "using Cephalon.AspNetCore.Hosting;",
            "using Cephalon.Observability.Hosting;"
        };
        var registrationLines = new List<string>();

        if (appProfile.Transports.Any(transport => string.Equals(transport.Id, "json-rpc", StringComparison.OrdinalIgnoreCase)))
        {
            usingLines.Add("using Cephalon.AspNetCore.JsonRpc.Hosting;");
            registrationLines.Add("builder.AddJsonRpcTransport();");
        }

        if (appProfile.Transports.Any(transport => string.Equals(transport.Id, "grpc", StringComparison.OrdinalIgnoreCase)))
        {
            usingLines.Add("using Cephalon.AspNetCore.Grpc.Hosting;");
            registrationLines.Add("builder.AddGrpcTransport();");
        }

        var registrations = registrationLines.Count == 0
            ? string.Empty
            : string.Join(Environment.NewLine, registrationLines) + Environment.NewLine;

        return $@"{string.Join(Environment.NewLine, usingLines.Distinct(StringComparer.Ordinal))}

var builder = WebApplication.CreateBuilder(args);
{registrations}builder.AddCephalon();
builder.Services.AddCephalonObservability(builder.Configuration);

var app = builder.Build();

app.MapGet(""/"", () => Results.Ok(new
{{
    Name = ""{EscapeString(request.AppName)}"",
    Blueprint = ""{EscapeString(appProfile.BlueprintDisplayName)}""
}}))
   .ExcludeFromDescription();

app.MapCephalon();
app.Run();
";
    }

    private static string BuildHostSettings(AppProfile appProfile, ScaffoldRequest request)
    {
        var builder = new StringBuilder();
        builder.AppendLine("{");
        builder.AppendLine("  \"Engine\": {");
        builder.AppendLine("    \"Blueprint\": \"" + EscapeJson(appProfile.BlueprintDisplayName) + "\",");
        builder.AppendLine("    \"Discovery\": {");
        builder.AppendLine("      \"Assemblies\": [");

        for (var index = 0; index < request.Modules.Count; index++)
        {
            var moduleName = request.Modules[index];
            var suffix = index == request.Modules.Count - 1 ? string.Empty : ",";
            builder.AppendLine("        \"" + EscapeJson(request.AppName + ".Modules." + moduleName) + "\"" + suffix);
        }

        builder.AppendLine("      ]");
        builder.AppendLine("    },");
        builder.AppendLine("    \"Patterns\": [");

        for (var index = 0; index < appProfile.Patterns.Count; index++)
        {
            var pattern = appProfile.Patterns[index];
            var suffix = index == appProfile.Patterns.Count - 1 ? string.Empty : ",";
            builder.AppendLine("      \"" + EscapeJson(pattern.DisplayName) + "\"" + suffix);
        }

        builder.AppendLine("    ],");
        builder.AppendLine("    \"Technologies\": [");

        for (var index = 0; index < appProfile.Technologies.Count; index++)
        {
            var technology = appProfile.Technologies[index];
            var suffix = index == appProfile.Technologies.Count - 1 ? string.Empty : ",";
            builder.AppendLine("      \"" + EscapeJson(technology.DisplayName) + "\"" + suffix);
        }

        builder.AppendLine("    ],");
        builder.AppendLine("    \"Observability\": {");
        builder.AppendLine("      \"LogManifestSummary\": true,");
        builder.AppendLine("      \"LogModuleSummary\": true,");
        builder.AppendLine("      \"LogCapabilitySummary\": true,");
        builder.AppendLine("      \"Telemetry\": {");
        builder.AppendLine("        \"Provider\": \"OpenTelemetry\",");
        builder.AppendLine("        \"Protocol\": \"otlp\",");
        builder.AppendLine("        \"Endpoint\": \"http://localhost:4317\",");
        builder.AppendLine("        \"ExportLogs\": true,");
        builder.AppendLine("        \"ExportMetrics\": true,");
        builder.AppendLine("        \"ExportTraces\": true");
        builder.AppendLine("      }");
        builder.AppendLine("    },");
        builder.AppendLine("    \"Localization\": {");
        builder.AppendLine("      \"DefaultCulture\": \"en\",");
        builder.AppendLine("      \"SupportedCultures\": [");
        builder.AppendLine("        \"en\",");
        builder.AppendLine("        \"th\"");
        builder.AppendLine("      ],");
        builder.AppendLine("      \"Resources\": {");
        builder.AppendLine("        \"th\": {");
        builder.AppendLine("          \"engine.docs.rest.title\": \"" + EscapeJson(request.AppName + " REST API ภาษาไทย") + "\",");
        builder.AppendLine("          \"engine.docs.rest.description\": \"" + EscapeJson("พื้นผิว REST ที่ " + request.AppName + " host เปิดให้ใช้งาน") + "\"");
        builder.AppendLine("        }");
        builder.AppendLine("      }");
        builder.AppendLine("    },");
        builder.AppendLine("    \"Transports\": [");

        for (var index = 0; index < appProfile.Transports.Count; index++)
        {
            var transport = appProfile.Transports[index];
            var suffix = index == appProfile.Transports.Count - 1 ? string.Empty : ",";
            builder.AppendLine("      \"" + EscapeJson(transport.DisplayName) + "\"" + suffix);
        }

        builder.AppendLine("    ]");
        builder.AppendLine("  },");
        builder.AppendLine("  \"ReferenceDocs\": {");
        builder.AppendLine("    \"Enabled\": false,");
        builder.AppendLine("    \"RoutePrefix\": \"/reference\",");
        builder.AppendLine("    \"DirectoryPath\": \"..\\\\..\\\\docs\\\\reference\",");
        builder.AppendLine("    \"DefaultDocument\": \"browse.html\"");
        builder.AppendLine("  }");
        builder.AppendLine("}");
        return builder.ToString();
    }

    private static string BuildFoundationMarker(ScaffoldRequest request)
    {
        return $@"namespace {request.RootNamespace}.Foundation;

public static class AssemblyMarker
{{
}}
";
    }

    private static string BuildContractFile(ScaffoldRequest request)
    {
        return $@"namespace {request.RootNamespace}.Contracts;

public sealed record GreetingContract(string Message, DateTimeOffset CreatedAtUtc);
";
    }

    private static string BuildModuleFile(
        AppProfile appProfile,
        RenderedProject project,
        ScaffoldRequest request)
    {
        var moduleName = ResolveModuleName(project);
        var moduleTypeName = ResolveModuleTypeName(project);
        var moduleId = ScaffoldRequest.ToSlug(moduleName, "module");

        return $@"using Cephalon.Abstractions.Capabilities;
using Cephalon.Abstractions.Modules;

namespace {request.RootNamespace}.Modules.{ScaffoldRequest.ToIdentifier(moduleName, "Module")};

public sealed class {moduleTypeName}Module : ModuleBase
{{
    public override ModuleDescriptor Descriptor {{ get; }} = new(
        id: ""{moduleId}"",
        displayName: ""{EscapeString(moduleName)}"",
        description: ""Generated {EscapeString(moduleName)} module for the {EscapeString(appProfile.BlueprintDisplayName)} blueprint."",
        tags: [""generated"", ""{appProfile.BlueprintId}""]);

    public override void RegisterCapabilities(ICapabilityRegistry capabilities)
    {{
        capabilities.Add(new Capability(
            key: ""{moduleId}.health"",
            displayName: ""{EscapeString(moduleName)} health"",
            description: ""Generated health capability for the {EscapeString(moduleName)} module.""));
    }}
}}
";
    }

    private static string BuildPackageManifest(RenderedProject project, ScaffoldRequest request)
    {
        var moduleId = ResolveModuleId(project);

        return $$"""
{
  "id": "{{EscapeJson(moduleId)}}",
  "version": "{{EscapeJson(request.CephalonPackageVersion)}}",
  "assembly": "{{EscapeJson(project.Name)}}.dll",
  "publisher": {
    "id": "{{EscapeJson(request.RootNamespace.ToLowerInvariant())}}",
    "displayName": "{{EscapeJson(request.RootNamespace)}}"
  },
  "compatibility": {
    "minimumEngineVersion": "{{EscapeJson(request.CephalonPackageVersion)}}",
    "supportedTargetFrameworks": [ "{{EscapeJson(request.TargetFramework)}}" ]
  }
}
""";
    }

    private static string BuildSmokeTest(ScaffoldRequest request)
    {
        return $@"namespace {request.RootNamespace}.Tests;

public sealed class SmokeTests
{{
    [Fact]
    public void GeneratedScaffoldCompilesIntoATestableSolution()
    {{
        Assert.True(true);
    }}
}}
";
    }

    private static string ResolveModuleName(RenderedProject project)
    {
        if (project.Metadata.TryGetValue("moduleName", out var moduleName))
        {
            return moduleName;
        }

        throw new InvalidOperationException(
            $"Rendered project '{project.Name}' did not contain module metadata.");
    }

    private static string ResolveModuleTypeName(RenderedProject project)
    {
        return ScaffoldRequest.ToIdentifier(ResolveModuleName(project), "Module");
    }

    private static string ResolveModuleId(RenderedProject project)
    {
        if (project.Metadata.TryGetValue("moduleName", out var moduleName))
        {
            return ScaffoldRequest.ToSlug(moduleName, "module");
        }

        throw new InvalidOperationException(
            $"Rendered project '{project.Name}' did not contain module metadata for package manifest generation.");
    }

    private static string EscapeJson(string value)
    {
        return value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
    }

    private static string EscapeString(string value)
    {
        return value.Replace("\"", string.Empty, StringComparison.Ordinal);
    }

    private static string BuildKey(string projectId, string? moduleName, string? featureName)
    {
        var suffix = string.Join(
            ':',
            new[] { moduleName, featureName }
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!));

        return string.IsNullOrWhiteSpace(suffix) ? projectId : $"{projectId}:{suffix}";
    }

    private static Dictionary<string, string> CreateTokens(
        ScaffoldRequest request,
        string? moduleName = null,
        string? featureName = null)
    {
        var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["AppName"] = request.AppName,
            ["AppNamespace"] = request.RootNamespace
        };

        if (!string.IsNullOrWhiteSpace(moduleName))
        {
            tokens["ModuleName"] = moduleName;
            tokens["ModuleId"] = ScaffoldRequest.ToSlug(moduleName, "module");
            tokens["ModuleTypeName"] = ScaffoldRequest.ToIdentifier(moduleName, "Module");
        }

        if (!string.IsNullOrWhiteSpace(featureName))
        {
            tokens["FeatureName"] = featureName;
            tokens["FeatureTypeName"] = ScaffoldRequest.ToIdentifier(featureName, "Feature");
        }

        return tokens;
    }

    private static string ReplaceTokens(string template, IReadOnlyDictionary<string, string> tokens)
    {
        var result = template;

        foreach (var token in tokens)
        {
            result = result.Replace($"{{{token.Key}}}", token.Value, StringComparison.Ordinal);
        }

        return result;
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/');
    }

    private sealed class ProjectInstance
    {
        public ProjectInstance(
            ScaffoldProject source,
            string key,
            string name,
            string path,
            string projectFilePath,
            string? moduleName,
            string? featureName,
            IReadOnlyDictionary<string, string> tokens)
        {
            Source = source;
            Key = key;
            Name = name;
            Path = path;
            ProjectFilePath = projectFilePath;
            ModuleName = moduleName;
            FeatureName = featureName;
            Tokens = tokens;
        }

        public ScaffoldProject Source { get; }

        public string Key { get; }

        public string Name { get; }

        public string Path { get; }

        public string ProjectFilePath { get; }

        public string? ModuleName { get; }

        public string? FeatureName { get; }

        public IReadOnlyDictionary<string, string> Tokens { get; }

        public RenderedProject ToRenderedProject(
            IReadOnlyDictionary<string, List<ProjectInstance>> projectsBySourceId)
        {
            var dependencies = Source.DependsOn
                .SelectMany((string projectId) =>
                {
                    if (projectsBySourceId.TryGetValue(projectId, out var items))
                    {
                        return (IEnumerable<ProjectInstance>)items;
                    }

                    return Enumerable.Empty<ProjectInstance>();
                })
                .ToArray();

            var projectReferences = dependencies
                .Select(project => GetRelativeProjectPath(ProjectFilePath, project.ProjectFilePath))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var metadata = new Dictionary<string, string>(Source.Metadata, StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(ModuleName))
            {
                metadata["moduleName"] = ModuleName;
            }

            if (!string.IsNullOrWhiteSpace(FeatureName))
            {
                metadata["featureName"] = FeatureName;
            }

            return new RenderedProject(
                key: Key,
                sourceProjectId: Source.Id,
                name: Name,
                path: Path,
                scope: Source.Scope,
                role: Source.Role,
                template: Source.Template,
                packages: Source.Packages,
                projectReferences: projectReferences,
                metadata: metadata);
        }

        private static string GetRelativeProjectPath(string sourceProjectFilePath, string targetProjectFilePath)
        {
            var sourceDirectory = System.IO.Path.GetDirectoryName(sourceProjectFilePath)
                ?? throw new InvalidOperationException("Source project directory was not available.");
            var relative = System.IO.Path.GetRelativePath(sourceDirectory, targetProjectFilePath);
            return NormalizePath(relative);
        }
    }
}
