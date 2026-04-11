using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cephalon.Abstractions.AppModel;
using Cephalon.Abstractions.AppModel.Scaffolding;

namespace Cephalon.Scaffolding.Generation;

/// <summary>
/// Turns a Cephalon app profile and scaffold request into concrete projects, folders, and files.
/// </summary>
public static class ScaffoldGenerator
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
            new("NuGet.config", BuildNuGetConfig()),
            new("README.md", BuildReadme(appProfile, request))
        };

        var hostProject = projects.FirstOrDefault(project =>
            project.Template is "cephalon-web-host" or "cephalon-service-host");

        if (hostProject is not null)
        {
            files.Add(new(Path.Combine(".cephalon", "packages", "README.md"), BuildLocalPackageFeedReadme()));
            files.Add(new(".dockerignore", BuildDockerIgnore()));
            files.Add(new("Dockerfile", BuildDockerfile(hostProject)));
            files.Add(new("compose.yaml", BuildComposeFile(request)));
            files.Add(new("otel-collector-config.yaml", BuildOtelCollectorConfig()));
            files.Add(new(
                Path.Combine("deploy", "windows-service", "README.md"),
                BuildWindowsServiceReadme(appProfile, request, hostProject.Name)));
            files.Add(new(
                Path.Combine("deploy", "windows-service", "install-service.ps1"),
                BuildWindowsServiceInstallScript(request, hostProject.Name)));
            files.Add(new(
                Path.Combine("deploy", "windows-service", "remove-service.ps1"),
                BuildWindowsServiceRemoveScript(request)));
            files.Add(new(
                Path.Combine("deploy", "iis", "README.md"),
                BuildIisReadme(appProfile, request, hostProject.Name)));
            files.Add(new(
                Path.Combine("deploy", "iis", "install-site.ps1"),
                BuildIisInstallScript(request)));
            files.Add(new(
                Path.Combine("deploy", "iis", "remove-site.ps1"),
                BuildIisRemoveScript(request)));
            files.Add(new(
                Path.Combine("deploy", "azure-app-service", "README.md"),
                BuildAzureAppServiceReadme(appProfile, request, hostProject.Name)));
            files.Add(new(
                Path.Combine("deploy", "azure-app-service", "deploy-zip.ps1"),
                BuildAzureAppServiceDeployScript(request, hostProject.Name)));
            files.Add(new(
                Path.Combine("deploy", "container-image", "README.md"),
                BuildContainerImageReadme(request)));
            files.Add(new(
                Path.Combine("deploy", "container-image", "publish-image.ps1"),
                BuildContainerImagePublishScript(request)));
            files.Add(new(
                Path.Combine("deploy", "azure-container-apps", "README.md"),
                BuildAzureContainerAppsReadme(appProfile, request, hostProject.Name)));
            files.Add(new(
                Path.Combine("deploy", "azure-container-apps", "deploy-up.ps1"),
                BuildAzureContainerAppsDeployScript(request, hostProject.Name)));
            files.Add(new(
                Path.Combine("deploy", "kubernetes", "README.md"),
                BuildKubernetesReadme(appProfile, request)));
            files.Add(new(
                Path.Combine("deploy", "kubernetes", "apply.ps1"),
                BuildKubernetesApplyScript(request)));
            files.Add(new(
                Path.Combine("deploy", "kubernetes", "kustomization.yaml"),
                BuildKubernetesKustomization(request)));
            files.Add(new(
                Path.Combine("deploy", "kubernetes", "namespace.yaml"),
                BuildKubernetesNamespaceManifest(request)));
            files.Add(new(
                Path.Combine("deploy", "kubernetes", "deployment.yaml"),
                BuildKubernetesDeploymentManifest(request)));
            files.Add(new(
                Path.Combine("deploy", "kubernetes", "service.yaml"),
                BuildKubernetesServiceManifest(request)));
            files.Add(new(
                Path.Combine("deploy", "linux", "systemd", "README.md"),
                BuildSystemdReadme(appProfile, request, hostProject.Name)));
            files.Add(new(
                Path.Combine("deploy", "linux", "systemd", $"{request.AppName}.service"),
                BuildSystemdServiceUnit(request, hostProject.Name)));
            files.Add(new(
                Path.Combine("deploy", "linux", "systemd", $"{request.AppName}.env"),
                BuildSystemdEnvironmentFile()));
        }

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
                    files.AddRange(BuildHostConfigurationFiles(project.Path, appProfile, request));
                    files.Add(new(
                        Path.Combine(project.Path, "Properties", "PublishProfiles", "CephalonFolder.pubxml"),
                        BuildPublishProfile()));
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
                    files.Add(new(
                        Path.Combine(project.Path, "Architecture", "CompositionSmokeTests.cs"),
                        BuildCompositionSmokeTest(request)));

                    foreach (var feature in ResolveGeneratedTestFeatures(request))
                    {
                        files.Add(new(
                            Path.Combine(project.Path, "Features", $"{feature.ClassName}BehaviorSpecifications.cs"),
                            BuildBehaviorSpecificationTest(request, feature.DisplayName, feature.ClassName)));
                    }

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
        var hostProjectName = ResolveHostProjectName(appProfile, request);
        var hostProjectPath = $"src/{hostProjectName}/{hostProjectName}.csproj";
        var restDocsLine = appProfile.Transports.Any(transport =>
            string.Equals(transport.Id, "rest", StringComparison.OrdinalIgnoreCase))
            ? "Then inspect `/engine`, `/engine/snapshot`, `/health/ready`, and `/scalar`."
            : "Then inspect `/engine`, `/engine/snapshot`, and `/health/ready`.";
        var restDocsServiceLine = appProfile.Transports.Any(transport =>
            string.Equals(transport.Id, "rest", StringComparison.OrdinalIgnoreCase))
            ? "Then inspect the host URL with `/engine`, `/engine/snapshot`, `/health/ready`, and `/scalar`."
            : "Then inspect the host URL with `/engine`, `/engine/snapshot`, and `/health/ready`.";

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

1. Decide where `Cephalon*` packages should restore from. `NuGet.config` points at `./.cephalon/packages` by default for repo-local package artifacts.
2. Populate `./.cephalon/packages` from the Cephalon repository or replace the `cephalon` source in `NuGet.config` with your published package feed.
3. Adjust package versions in `Directory.Packages.props` if needed.
4. Flesh out module services, capabilities, and transport adapters.
5. Add feature handlers inside the generated folders for each module.
6. Add project-specific languages or replace the localization catalog through `Engine:Localization` and DI.
7. Generate and publish API reference docs before enabling the shipped `ReferenceDocs` host section.
8. Keep Cephalon defaults in `Configurations/Add*.json`, use `appsettings.json` plus `appsettings.[Environment].json` for project-specific overrides, and add `Configurations/[group]/[Environment].json` when you want grouped environment overrides. `Configurations/Observability/Development.json` already seeds a Serilog console example, and `Program.cs` only switches to Serilog when that section exists.
9. Use the shipped `Properties/PublishProfiles/CephalonFolder.pubxml` profile when you want a deterministic published-output path before deployment packaging.
10. Use the shipped `deploy/container-image/README.md` plus the generated image-publish script when you want a provider-neutral build/tag/push baseline from the generated Dockerfile.
11. Use the shipped `deploy/azure-app-service/README.md` plus the generated ZIP deployment script when you want an Azure App Service run-from-package baseline after publish.
12. Use the shipped `deploy/azure-container-apps/README.md` plus the generated source-deploy script when you want an Azure Container Apps baseline from the generated Dockerfile and source root.
13. Use the shipped `deploy/kubernetes/README.md` plus the generated manifest/apply assets when you want a platform-neutral Kubernetes baseline from the generated Dockerfile and source root.
14. Use the shipped `deploy/windows-service/README.md` plus the generated install/remove scripts when you want a self-hosted Windows Service deployment baseline after publish.
15. Use the shipped `deploy/iis/README.md` plus the generated site/app-pool preview scripts when you want a hosted Windows IIS deployment baseline after publish.
16. Use the shipped `deploy/linux/systemd/README.md` plus the generated unit/env files when you want a self-hosted Linux `systemd` deployment baseline after publish.
17. Use `docker compose up --build` for the generated local container + collector smoke path when you want to validate health and `/engine/*` routes under deployment-like boundaries.

## Published output

Generated host projects now also include:

- `Properties/PublishProfiles/CephalonFolder.pubxml`

From the solution root, publish the host with:

```powershell
dotnet publish {hostProjectPath} -p:PublishProfile=CephalonFolder
```

That writes the host to `./artifacts/publish/{hostProjectName}/`.

To run the published output locally:

```powershell
dotnet ./artifacts/publish/{hostProjectName}/{hostProjectName}.dll --urls http://127.0.0.1:18080
```

{restDocsLine}

## Container image path

Generated app scaffolds now also include:

- `deploy/container-image/README.md`
- `deploy/container-image/publish-image.ps1`

Use these files from the solution root when you want a provider-neutral build/tag/push baseline from the generated Dockerfile before you hand the image to Kubernetes or another hosted container platform.

The generated container-image baseline assumes:

- `NuGet.config` points at a reachable Cephalon package source or `./.cephalon/packages` has been seeded before the container image is built
- Docker Desktop or another compatible Docker engine is installed on the machine that performs the build
- `docker login` has already been completed for the target registry when you invoke the script with `-Push`
- the publish script can preview the Docker command contract locally before it performs a real build or push

See `deploy/container-image/README.md` for the preview, build, and push steps.

## Azure App Service path

Generated app scaffolds now also include:

- `deploy/azure-app-service/README.md`
- `deploy/azure-app-service/deploy-zip.ps1`

Use these files after you publish the host into `./artifacts/publish/{hostProjectName}/`.

The generated Azure App Service baseline assumes:

- published output is packaged into `./artifacts/deploy/{hostProjectName}/azure-app-service.zip`
- Azure CLI is installed and authenticated on the machine that performs the deploy
- an Azure App Service web app already exists for the target app name
- the deployment script can preview the Azure CLI contract locally before it performs a live deploy

See `deploy/azure-app-service/README.md` for the package, preview, and deploy steps.

{restDocsServiceLine}

## Azure Container Apps path

Generated app scaffolds now also include:

- `deploy/azure-container-apps/README.md`
- `deploy/azure-container-apps/deploy-up.ps1`

Use these files from the solution root when you want a hosted Azure Container Apps baseline from the generated Dockerfile and source tree instead of the published-output ZIP path.

The generated Azure Container Apps baseline assumes:

- `NuGet.config` points at a reachable Cephalon package source or `./.cephalon/packages` has been seeded before the container image is built
- Azure CLI is installed and authenticated on the machine that performs the deploy
- the generated app root keeps the shipped `Dockerfile`
- the deployment script can preview the Azure CLI contract locally before it performs a live deploy

See `deploy/azure-container-apps/README.md` for the preview and deploy steps.

{restDocsServiceLine}

## Kubernetes path

Generated app scaffolds now also include:

- `deploy/kubernetes/README.md`
- `deploy/kubernetes/apply.ps1`
- `deploy/kubernetes/kustomization.yaml`
- `deploy/kubernetes/namespace.yaml`
- `deploy/kubernetes/deployment.yaml`
- `deploy/kubernetes/service.yaml`

Use these files from the solution root when you want a platform-neutral Kubernetes baseline from the generated Dockerfile and source tree instead of the published-output ZIP path.

The generated Kubernetes baseline assumes:

- `NuGet.config` points at a reachable Cephalon package source or `./.cephalon/packages` has been seeded before the container image is built
- `kubectl` with `kustomize` support is installed and already targets the cluster context you want to update
- the deployment script can render the current manifest set locally before it performs a live apply
- the image you pass to the deployment script is pullable by the target cluster

See `deploy/kubernetes/README.md` for the render, preview, and apply steps.

{restDocsServiceLine}

## Windows Service path

Generated app scaffolds now also include:

- `deploy/windows-service/README.md`
- `deploy/windows-service/install-service.ps1`
- `deploy/windows-service/remove-service.ps1`

Use these files after you publish the host into `./artifacts/publish/{hostProjectName}/`.

The generated Windows Service baseline assumes:

- published output is copied to `C:\Services\{request.AppName}\current`
- the install script is run from an elevated PowerShell session on the Windows target

The generated host is already wired for Windows Service lifetime and content-root handling through `Microsoft.Extensions.Hosting.WindowsServices`.

See `deploy/windows-service/README.md` for the preview, install, verify, and removal steps.

{restDocsServiceLine}

## IIS path

Generated app scaffolds now also include:

- `deploy/iis/README.md`
- `deploy/iis/install-site.ps1`
- `deploy/iis/remove-site.ps1`

Use these files after you publish the host into `./artifacts/publish/{hostProjectName}/`.

The generated IIS baseline assumes:

- published output is copied to `C:\inetpub\sites\{request.AppName}\current`
- the install script is run from an elevated PowerShell session on a Windows host with IIS installed
- the publish output keeps the SDK-generated `web.config` that points IIS/ANCM at `dotnet .\{hostProjectName}.dll`

See `deploy/iis/README.md` for the preview, install, verify, and removal steps.

{restDocsServiceLine}

## Linux systemd path

Generated app scaffolds now also include:

- `deploy/linux/systemd/README.md`
- `deploy/linux/systemd/{request.AppName}.service`
- `deploy/linux/systemd/{request.AppName}.env`

Use these files after you publish the host into `./artifacts/publish/{hostProjectName}/`.

The generated Linux service baseline assumes:

- published output is copied to `/opt/{request.AppName}/current`
- the optional environment override file lives at `/etc/cephalon/{request.AppName}.env`

See `deploy/linux/systemd/README.md` for the install, verify, and `systemctl` steps.

{restDocsServiceLine}

## Container runtime

Generated app scaffolds now also include:

- `NuGet.config`
- `.cephalon/packages/README.md`
- `.dockerignore`
- `Dockerfile`
- `compose.yaml`
- `otel-collector-config.yaml`

Populate `./.cephalon/packages` or repoint `NuGet.config` before running the host:

```powershell
dotnet build {request.AppName}.slnx
```

From the solution root, run:

```powershell
docker compose up --build
```

{restDocsLine}
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
            "using Cephalon.Observability.Hosting;",
            "using Cephalon.Observability.OpenTelemetry.Hosting;",
            "using Cephalon.Observability.Serilog.Hosting;",
            "using Microsoft.Extensions.Configuration;",
            "using Microsoft.Extensions.Hosting.WindowsServices;"
        };
        var registrationLines = new List<string>();
        var hostRegistrationLines = new List<string>();
        var engineRegistrationLines = new List<string>();

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

        if (appProfile.Transports.Any(transport => string.Equals(transport.Id, "graphql", StringComparison.OrdinalIgnoreCase)))
        {
            usingLines.Add("using Cephalon.AspNetCore.GraphQL.Hosting;");
            registrationLines.Add("builder.AddGraphQLTransport();");
        }

        if (ShouldGenerateDataPack(appProfile))
        {
            usingLines.Add("using Cephalon.Data.Registration;");
            engineRegistrationLines.Add("    engine.AddData();");
        }

        if (ShouldGenerateSfidPack(appProfile))
        {
            usingLines.Add("using Cephalon.Ids.Sfid.Registration;");
            engineRegistrationLines.Add("    engine.AddSfidIds();");
        }

        if (ShouldGenerateEventingPack(appProfile))
        {
            usingLines.Add("using Cephalon.Eventing.Registration;");
            engineRegistrationLines.Add("    engine.AddEventing();");
        }

        if (ShouldGenerateWolverinePack(appProfile))
        {
            usingLines.Add("using Cephalon.Eventing.Wolverine.Registration;");
            engineRegistrationLines.Add("    engine.AddWolverineEventing();");
        }

        if (ShouldGenerateIdentityPack(appProfile))
        {
            usingLines.Add("using Cephalon.Identity.AspNetCore.Hosting;");
            usingLines.Add("using Cephalon.Identity.Registration;");
            hostRegistrationLines.Add("builder.AddCephalonIdentityAspNetCore();");
            engineRegistrationLines.Add("    engine.AddIdentityAccess();");
        }

        if (ShouldGenerateMultiTenancyPack(appProfile))
        {
            usingLines.Add("using Cephalon.MultiTenancy.Registration;");
            engineRegistrationLines.Add("    engine.AddMultiTenancy();");
        }

        if (ShouldGenerateAuditPack(appProfile))
        {
            usingLines.Add("using Cephalon.Audit.Registration;");
            engineRegistrationLines.Add("    engine.AddAudit();");
        }

        var registrations = registrationLines.Count == 0
            ? string.Empty
            : string.Join(Environment.NewLine, registrationLines) + Environment.NewLine;
        var hostRegistrations = hostRegistrationLines.Count == 0
            ? string.Empty
            : string.Join(Environment.NewLine, hostRegistrationLines) + Environment.NewLine;
        var cephalonRegistration = engineRegistrationLines.Count == 0
            ? "builder.AddCephalon();"
            : "builder.AddCephalon(engine =>" + Environment.NewLine +
              "{" + Environment.NewLine +
              string.Join(Environment.NewLine, engineRegistrationLines) + Environment.NewLine +
              "});";

        return $@"{string.Join(Environment.NewLine, usingLines.Distinct(StringComparer.Ordinal))}

var options = new WebApplicationOptions
{{
    Args = args,
    ContentRootPath = WindowsServiceHelpers.IsWindowsService()
        ? AppContext.BaseDirectory
        : default
}};

var builder = WebApplication.CreateBuilder(options);
builder.AddCephalonProjectConfigurations();
builder.Host.UseWindowsService();
{registrations}{hostRegistrations}{cephalonRegistration}
builder.Services.AddCephalonObservability(builder.Configuration);
if (builder.Configuration.GetSection(""Serilog"").Exists())
{{
    builder.Logging.ClearProviders();
}}
builder.AddCephalonSerilog();
builder.AddCephalonOpenTelemetry();

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

    private static RenderedFile[] BuildHostConfigurationFiles(
        string projectPath,
        AppProfile appProfile,
        ScaffoldRequest request)
    {
        return
        [
            new(Path.Combine(projectPath, "Configurations", "README.md"), BuildHostConfigurationsReadme()),
            new(Path.Combine(projectPath, "Configurations", "AddEngine.AppModel.json"), BuildHostAppModelSettings(appProfile, request)),
            new(Path.Combine(projectPath, "Configurations", "AddEngine.Data.json"), BuildHostDataSettings(appProfile)),
            new(Path.Combine(projectPath, "Configurations", "AddEngine.Identity.json"), BuildHostIdentitySettings(appProfile)),
            new(Path.Combine(projectPath, "Configurations", "AddEngine.Tenancy.json"), BuildHostTenancySettings(appProfile)),
            new(Path.Combine(projectPath, "Configurations", "AddEngine.Audit.json"), BuildHostAuditSettings(appProfile)),
            new(Path.Combine(projectPath, "Configurations", "AddEngine.Messaging.json"), BuildHostMessagingSettings(appProfile)),
            new(Path.Combine(projectPath, "Configurations", "AddEngine.Observability.json"), BuildHostObservabilitySettings()),
            new(Path.Combine(projectPath, "Configurations", "AddEngine.Localization.json"), BuildHostLocalizationSettings(request)),
            new(
                Path.Combine(projectPath, "Configurations", "Observability", "Development.json"),
                BuildDevelopmentSerilogSettings(ResolveHostProjectName(appProfile, request))),
            new(Path.Combine(projectPath, "Configurations", "AddOpenApi.json"), BuildOpenApiSettings(request)),
            new(Path.Combine(projectPath, "Configurations", "AddReferenceDocs.json"), BuildReferenceDocsSettings("..\\..\\docs\\reference")),
            new(Path.Combine(projectPath, "appsettings.json"), BuildProjectAppSettings()),
            new(Path.Combine(projectPath, "appsettings.Development.json"), BuildProjectAppSettings())
        ];
    }

    private static string BuildHostConfigurationsReadme()
    {
        return """
# Host Configuration

Generated Cephalon hosts load configuration in three layers:

- shared Cephalon defaults from `Configurations/Add*.json`
- optional grouped environment overrides from `Configurations/{group}/{Environment}.json`
- standard project overrides from `appsettings.json` and `appsettings.{Environment}.json`

The generated `Configurations/Observability/Development.json` already includes a Serilog console
sample. `Program.cs` switches cleanly to Serilog only when a top-level `Serilog` section exists, so
the starter stays optional for other environments instead of forcing a provider decision globally.

This starter keeps the shipped baseline in root `Add*.json` files so the host stays deterministic in
`Development`, `Local`, `Production`, or any other environment name without requiring duplicate files.

When you need per-environment differences later, add overrides such as:

- `Configurations/OpenApi/Development.json`
- `Configurations/Engine/Observability/Production.json`

`Program.cs` already loads this convention through `AddCephalonProjectConfigurations()`. The engine inserts
split-config defaults ahead of standard host overrides, so `appsettings.json`, user secrets, environment
variables, and command-line arguments continue to win the same way developers expect in ASP.NET Core.
""";
    }

    private static string BuildProjectAppSettings()
    {
        return BuildJsonContents(new JsonObject());
    }

    private static string BuildDevelopmentSerilogSettings(string applicationName)
    {
        return BuildJsonContents(new JsonObject
        {
            ["Serilog"] = new JsonObject
            {
                ["Using"] = CreateJsonArray(["Serilog.Sinks.Console"]),
                ["MinimumLevel"] = new JsonObject
                {
                    ["Default"] = "Information",
                    ["Override"] = new JsonObject
                    {
                        ["Microsoft"] = "Warning",
                        ["Microsoft.Hosting.Lifetime"] = "Information",
                        ["System"] = "Warning"
                    }
                },
                ["WriteTo"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["Name"] = "Console"
                    }
                },
                ["Properties"] = new JsonObject
                {
                    ["Application"] = applicationName
                }
            }
        });
    }

    private static string BuildHostAppModelSettings(AppProfile appProfile, ScaffoldRequest request)
    {
        return BuildJsonContents(new JsonObject
        {
            ["Engine"] = new JsonObject
            {
                ["Blueprint"] = appProfile.BlueprintId,
                ["Discovery"] = new JsonObject
                {
                    ["Assemblies"] = CreateJsonArray(
                        request.Modules.Select(moduleName => request.AppName + ".Modules." + moduleName))
                },
                ["Patterns"] = CreateJsonArray(appProfile.Patterns.Select(pattern => pattern.Id)),
                ["Technologies"] = CreateJsonArray(appProfile.Technologies.Select(technology => technology.Id)),
                ["Transports"] = CreateJsonArray(appProfile.Transports.Select(transport => transport.Id))
            },
        });
    }

    private static string BuildHostDataSettings(AppProfile appProfile)
    {
        return BuildJsonContents(new JsonObject
        {
            ["Engine"] = new JsonObject
            {
                ["Data"] = BuildGeneratedDataSettings(appProfile)
            }
        });
    }

    private static string BuildHostIdentitySettings(AppProfile appProfile)
    {
        return BuildJsonContents(new JsonObject
        {
            ["Engine"] = new JsonObject
            {
                ["Identity"] = BuildGeneratedIdentitySettings(appProfile)
            }
        });
    }

    private static string BuildHostTenancySettings(AppProfile appProfile)
    {
        return BuildJsonContents(new JsonObject
        {
            ["Engine"] = new JsonObject
            {
                ["Tenancy"] = BuildGeneratedTenancySettings(appProfile)
            }
        });
    }

    private static string BuildHostAuditSettings(AppProfile appProfile)
    {
        return BuildJsonContents(new JsonObject
        {
            ["Engine"] = new JsonObject
            {
                ["Audit"] = new JsonObject
                {
                    ["Enabled"] = ShouldGenerateAuditPack(appProfile)
                }
            }
        });
    }

    private static string BuildHostMessagingSettings(AppProfile appProfile)
    {
        return BuildJsonContents(new JsonObject
        {
            ["Engine"] = new JsonObject
            {
                ["Messaging"] = BuildGeneratedMessagingSettings(appProfile)
            }
        });
    }

    private static string BuildHostObservabilitySettings()
    {
        return BuildJsonContents(new JsonObject
        {
            ["Engine"] = new JsonObject
            {
                ["Observability"] = new JsonObject
                {
                    ["LogManifestSummary"] = true,
                    ["LogModuleSummary"] = true,
                    ["LogCapabilitySummary"] = true,
                    ["Telemetry"] = new JsonObject
                    {
                        ["Provider"] = "OpenTelemetry",
                        ["Protocol"] = "otlp/http",
                        ["ExportLogs"] = true,
                        ["ExportMetrics"] = true,
                        ["ExportTraces"] = true
                    }
                }
            }
        });
    }

    private static string BuildHostLocalizationSettings(ScaffoldRequest request)
    {
        return BuildJsonContents(new JsonObject
        {
            ["Engine"] = new JsonObject
            {
                ["Localization"] = new JsonObject
                {
                    ["DefaultCulture"] = "en",
                    ["SupportedCultures"] = CreateJsonArray(["en", "th"]),
                    ["Resources"] = new JsonObject
                    {
                        ["th"] = new JsonObject
                        {
                            ["engine.docs.rest.title"] = request.AppName + " REST API ภาษาไทย",
                            ["engine.docs.rest.description"] = "พื้นผิว REST ที่ " + request.AppName + " host เปิดให้ใช้งาน"
                        }
                    }
                }
            }
        });
    }

    private static string BuildOpenApiSettings(ScaffoldRequest request)
    {
        return BuildJsonContents(new JsonObject
        {
            ["OpenApi"] = new JsonObject
            {
                ["Title"] = request.AppName + " API"
            }
        });
    }

    private static string BuildReferenceDocsSettings(string directoryPath)
    {
        return BuildJsonContents(new JsonObject
        {
            ["ReferenceDocs"] = new JsonObject
            {
                ["Enabled"] = false,
                ["RoutePrefix"] = "/reference",
                ["DirectoryPath"] = directoryPath,
                ["DefaultDocument"] = "browse.html"
            }
        });
    }

    private static string BuildJsonContents(JsonObject root)
    {
        return root.ToJsonString(new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    private static JsonObject BuildGeneratedDataSettings(AppProfile appProfile)
    {
        var dataSettings = new JsonObject
        {
            ["ReadWriteSplit"] = appProfile.Data.ReadWriteSplit ?? HasPattern(appProfile, "cqrs"),
            ["Outbox"] = new JsonObject
            {
                ["Enabled"] = appProfile.Data.OutboxEnabled ?? HasPattern(appProfile, "outbox")
            },
            ["Ids"] = new JsonObject()
        };

        if (!string.IsNullOrWhiteSpace(appProfile.Data.Provider))
        {
            dataSettings["Provider"] = appProfile.Data.Provider;
        }

        var idGenerator = ResolveGeneratedIdGenerator(appProfile);
        if (!string.IsNullOrWhiteSpace(idGenerator))
        {
            ((JsonObject)dataSettings["Ids"]!).Add("Generator", idGenerator);
        }

        return dataSettings;
    }

    private static JsonObject BuildGeneratedIdentitySettings(AppProfile appProfile)
    {
        var authorizationModes = ResolveGeneratedAuthorizationModes(appProfile);
        return new JsonObject
        {
            ["Enabled"] = ShouldGenerateIdentityPack(appProfile),
            ["AuthorizationModes"] = CreateJsonArray(authorizationModes)
        };
    }

    private static JsonObject BuildGeneratedTenancySettings(AppProfile appProfile)
    {
        var settings = new JsonObject
        {
            ["Enabled"] = ShouldGenerateMultiTenancyPack(appProfile)
        };

        var mode = ResolveGeneratedTenancyMode(appProfile);
        if (!string.IsNullOrWhiteSpace(mode))
        {
            settings["Mode"] = mode;
        }

        return settings;
    }

    private static JsonObject BuildGeneratedMessagingSettings(AppProfile appProfile)
    {
        var settings = new JsonObject();
        var provider = ResolveGeneratedMessagingProvider(appProfile);
        if (!string.IsNullOrWhiteSpace(provider))
        {
            settings["Provider"] = provider;
        }

        return settings;
    }

    private static JsonArray CreateJsonArray(IEnumerable<string> values)
    {
        var array = new JsonArray();

        foreach (var value in values)
        {
            array.Add(value);
        }

        return array;
    }

    private static bool ShouldGenerateDataPack(AppProfile appProfile)
    {
        return appProfile.Data.HasValues ||
            HasPattern(appProfile, "cqrs") ||
            HasPattern(appProfile, "outbox");
    }

    private static bool ShouldGenerateSfidPack(AppProfile appProfile)
    {
        return !string.IsNullOrWhiteSpace(ResolveGeneratedIdGenerator(appProfile));
    }

    private static string? ResolveGeneratedIdGenerator(AppProfile appProfile)
    {
        if (!string.IsNullOrWhiteSpace(appProfile.Data.IdGenerator))
        {
            return appProfile.Data.IdGenerator;
        }

        return ShouldGenerateDataPack(appProfile)
            ? "Sfid"
            : null;
    }

    private static bool ShouldGenerateIdentityPack(AppProfile appProfile)
    {
        return appProfile.Identity.HasValues ||
            HasTechnology(appProfile, "identity-access");
    }

    private static IReadOnlyList<string> ResolveGeneratedAuthorizationModes(AppProfile appProfile)
    {
        if (appProfile.Identity.AuthorizationModes.Count > 0)
        {
            return appProfile.Identity.AuthorizationModes;
        }

        return ShouldGenerateIdentityPack(appProfile)
            ? ["RBAC", "ABAC", "Policy"]
            : [];
    }

    private static bool ShouldGenerateMultiTenancyPack(AppProfile appProfile)
    {
        return appProfile.Tenancy.HasValues ||
            HasTechnology(appProfile, "multi-tenancy");
    }

    private static string? ResolveGeneratedTenancyMode(AppProfile appProfile)
    {
        if (!string.IsNullOrWhiteSpace(appProfile.Tenancy.Mode))
        {
            return appProfile.Tenancy.Mode;
        }

        return ShouldGenerateMultiTenancyPack(appProfile)
            ? "SharedDatabase"
            : null;
    }

    private static bool ShouldGenerateAuditPack(AppProfile appProfile)
    {
        return appProfile.Audit.Enabled == true;
    }

    private static bool ShouldGenerateEventingPack(AppProfile appProfile)
    {
        return appProfile.Messaging.HasValues ||
            HasTechnology(appProfile, "event-driven-integration");
    }

    private static bool ShouldGenerateWolverinePack(AppProfile appProfile)
    {
        return string.Equals(
            ResolveGeneratedMessagingProvider(appProfile),
            "Wolverine",
            StringComparison.OrdinalIgnoreCase);
    }

    private static string? ResolveGeneratedMessagingProvider(AppProfile appProfile)
    {
        if (!string.IsNullOrWhiteSpace(appProfile.Messaging.Provider))
        {
            return appProfile.Messaging.Provider;
        }

        return ShouldGenerateEventingPack(appProfile)
            ? "Wolverine"
            : null;
    }

    private static bool HasPattern(AppProfile appProfile, string patternId)
    {
        return appProfile.Patterns.Any(pattern =>
            string.Equals(pattern.Id, patternId, StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasTechnology(AppProfile appProfile, string technologyId)
    {
        return appProfile.Technologies.Any(technology =>
            string.Equals(technology.Id, technologyId, StringComparison.OrdinalIgnoreCase));
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

    private static string BuildCompositionSmokeTest(ScaffoldRequest request)
    {
        return $@"namespace {request.RootNamespace}.Tests.Architecture;

public sealed class CompositionSmokeTests
{{
    [Fact]
    public void Generated_scaffold_has_a_test_harness_ready_for_real_composition_checks()
    {{
        Assert.True(true);
    }}
}}
";
    }

    private static string BuildBehaviorSpecificationTest(
        ScaffoldRequest request,
        string featureName,
        string className)
    {
        var featureSlug = ScaffoldRequest.ToSlug(featureName, "feature")
            .Replace("-", "_", StringComparison.Ordinal);

        return $@"namespace {request.RootNamespace}.Tests.Features;

public sealed class {className}BehaviorSpecifications
{{
    [Fact]
    public void Given_{featureSlug}_behavior_when_you_start_tdd_then_replace_this_placeholder_with_the_first_failing_specification()
    {{
        // Replace this starter assertion with the first business rule you want to drive through TDD.
        Assert.True(true);
    }}
}}
";
    }

    private static GeneratedTestFeature[] ResolveGeneratedTestFeatures(ScaffoldRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var features = request.Features.Count == 0
            ? ["Core"]
            : request.Features;

        return features
            .Select(feature => new GeneratedTestFeature(
                feature,
                $"{ScaffoldRequest.ToIdentifier(feature, "Feature")}"))
            .ToArray();
    }

    private sealed record GeneratedTestFeature(string DisplayName, string ClassName);

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

    private static string BuildDockerIgnore()
    {
        return """
.git
**/bin
**/obj
TestResults
artifacts
""";
    }

    private static string BuildPublishProfile()
    {
        return """
<Project>
  <PropertyGroup>
    <Configuration>Release</Configuration>
    <PublishDir Condition="'$(PublishDir)' == '' and Exists('$(MSBuildProjectDirectory)/../../Directory.Build.props')">$(MSBuildProjectDirectory)/../../artifacts/publish/$(MSBuildProjectName)/</PublishDir>
    <PublishDir Condition="'$(PublishDir)' == ''">$(MSBuildProjectDirectory)/artifacts/publish/$(MSBuildProjectName)/</PublishDir>
    <UseAppHost>false</UseAppHost>
    <DeleteExistingFiles>false</DeleteExistingFiles>
  </PropertyGroup>
</Project>
""";
    }

    private static string BuildNuGetConfig()
    {
        return """
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="cephalon" value="./.cephalon/packages" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="cephalon">
      <package pattern="Cephalon*" />
    </packageSource>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
""";
    }

    private static string BuildLocalPackageFeedReadme()
    {
        return """
# Cephalon local package feed

`NuGet.config` points `Cephalon*` package restore at this directory by default so generated apps can build before you publish Cephalon packages to a shared feed.

Populate this directory from the Cephalon repository with:

```powershell
pwsh <path-to-cephalon-repo>/scripts/publish-package-artifacts.ps1 -OutputPath <absolute-path-to-this-folder>
```

If your team already publishes Cephalon packages to a shared source, replace the `cephalon` package source in `NuGet.config` instead. The Dockerfile and compose path use the same restore configuration automatically.
""";
    }

    private static string BuildWindowsServiceReadme(
        AppProfile appProfile,
        ScaffoldRequest request,
        string hostProjectName)
    {
        var restDocsLine = appProfile.Transports.Any(transport =>
            string.Equals(transport.Id, "rest", StringComparison.OrdinalIgnoreCase))
            ? "Then inspect the running host with `/engine`, `/engine/snapshot`, `/health/ready`, and `/scalar`."
            : "Then inspect the running host with `/engine`, `/engine/snapshot`, and `/health/ready`.";

        return $"""
# Windows Service deployment

These assets provide the self-hosted Windows Service baseline for `{request.AppName}` after the host is published through `CephalonFolder.pubxml`.

The generated host already includes Windows Service-aware startup wiring through `Microsoft.Extensions.Hosting.WindowsServices` so the service lifetime and content root stay aligned when the Service Control Manager launches the process.

The generated install script assumes:

- published output lives at `C:\Services\{request.AppName}\current`
- `dotnet` is available on the Windows target
- the real install step is run from an elevated PowerShell session

Files in this folder:

- `install-service.ps1`
- `remove-service.ps1`

From the solution root, publish the host with:

```powershell
dotnet publish src/{hostProjectName}/{hostProjectName}.csproj -p:PublishProfile=CephalonFolder
```

On the Windows target, copy the published output and preview the install contract with:

```powershell
New-Item -ItemType Directory -Path 'C:\Services\{request.AppName}\current' -Force | Out-Null
Copy-Item -Path .\artifacts\publish\{hostProjectName}\* -Destination 'C:\Services\{request.AppName}\current' -Recurse -Force
pwsh ./deploy/windows-service/install-service.ps1 -PublishRoot 'C:\Services\{request.AppName}\current' -Preview
```

When you are ready to install the service for real, rerun the same command from an elevated PowerShell session without `-Preview`:

```powershell
pwsh ./deploy/windows-service/install-service.ps1 -PublishRoot 'C:\Services\{request.AppName}\current'
Start-Service -Name '{request.AppName}'
Get-Service -Name '{request.AppName}'
sc.exe qc '{request.AppName}'
```

To remove the service later:

```powershell
pwsh ./deploy/windows-service/remove-service.ps1
```

{restDocsLine}
""";
    }

    private static string BuildWindowsServiceInstallScript(
        ScaffoldRequest request,
        string hostProjectName)
    {
        return $$"""
param(
    [string]$ServiceName = "{{EscapeString(request.AppName)}}",
    [string]$DisplayName = "{{EscapeString(request.AppName)}}",
    [string]$Description = "Cephalon host for {{EscapeString(request.AppName)}}",
    [string]$PublishRoot = "C:\Services\{{EscapeString(request.AppName)}}\current",
    [string]$DotnetPath = "dotnet",
    [string]$EnvironmentName = "Production",
    [string]$Urls = "http://127.0.0.1:8080",
    [ValidateSet("auto", "demand", "disabled")]
    [string]$StartupType = "auto",
    [switch]$Preview
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Test-IsAdministrator {
    $principal = [Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

$dllPath = Join-Path $PublishRoot "{{EscapeString(hostProjectName)}}.dll"

if (-not (Test-Path -LiteralPath $dllPath)) {
    throw "Expected published host at '$dllPath'."
}

$resolvedDotnetPath = (Get-Command $DotnetPath -ErrorAction Stop).Source
$binaryPath = "`"$resolvedDotnetPath`" `"$dllPath`" --contentRoot `"$PublishRoot`" --environment `"$EnvironmentName`" --urls `"$Urls`""

$createArguments = @(
    "create",
    $ServiceName,
    "binPath=",
    $binaryPath,
    "start=",
    $StartupType,
    "displayname=",
    $DisplayName)
$descriptionArguments = @("description", $ServiceName, $Description)
$failureArguments = @("failure", $ServiceName, "reset=", "0", "actions=", "restart/60000/restart/60000/restart/60000")

if ($Preview) {
    Write-Host "Preview only. The following commands would run:" -ForegroundColor Yellow
    Write-Host "sc.exe $($createArguments -join ' ')" -ForegroundColor Cyan
    Write-Host "sc.exe $($descriptionArguments -join ' ')" -ForegroundColor Cyan
    Write-Host "sc.exe $($failureArguments -join ' ')" -ForegroundColor Cyan
    return
}

if (-not (Test-IsAdministrator)) {
    throw "Installing a Windows Service requires an elevated PowerShell session."
}

$existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($null -ne $existingService) {
    throw "Service '$ServiceName' already exists. Remove it first or choose another -ServiceName."
}

& sc.exe @createArguments
if ($LASTEXITCODE -ne 0) {
    throw "sc.exe create failed for '$ServiceName'."
}

& sc.exe @descriptionArguments
if ($LASTEXITCODE -ne 0) {
    throw "sc.exe description failed for '$ServiceName'."
}

& sc.exe @failureArguments
if ($LASTEXITCODE -ne 0) {
    throw "sc.exe failure failed for '$ServiceName'."
}

Write-Host ""
Write-Host "Windows Service '$ServiceName' created successfully." -ForegroundColor Green
Write-Host "Start it with: Start-Service -Name '$ServiceName'" -ForegroundColor Cyan
""";
    }

    private static string BuildWindowsServiceRemoveScript(ScaffoldRequest request)
    {
        return $$"""
param(
    [string]$ServiceName = "{{EscapeString(request.AppName)}}",
    [switch]$Preview
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Test-IsAdministrator {
    $principal = [Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

if ($Preview) {
    Write-Host "sc.exe delete $ServiceName" -ForegroundColor Cyan
    return
}

if (-not (Test-IsAdministrator)) {
    throw "Removing a Windows Service requires an elevated PowerShell session."
}

$existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($null -eq $existingService) {
    Write-Host "Windows Service '$ServiceName' is not installed." -ForegroundColor Yellow
    return
}

if ($existingService.Status -ne [System.ServiceProcess.ServiceControllerStatus]::Stopped) {
    Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
}

& sc.exe delete $ServiceName
if ($LASTEXITCODE -ne 0) {
    throw "sc.exe delete failed for '$ServiceName'."
}

Write-Host "Windows Service '$ServiceName' deleted successfully." -ForegroundColor Green
""";
    }

    private static string BuildIisReadme(
        AppProfile appProfile,
        ScaffoldRequest request,
        string hostProjectName)
    {
        var restDocsLine = appProfile.Transports.Any(transport =>
            string.Equals(transport.Id, "rest", StringComparison.OrdinalIgnoreCase))
            ? "Then inspect the running host with `/engine`, `/engine/snapshot`, `/health/ready`, and `/scalar`."
            : "Then inspect the running host with `/engine`, `/engine/snapshot`, and `/health/ready`.";

        return $"""
# IIS deployment

These assets provide the hosted Windows IIS baseline for `{request.AppName}` after the host is published through `CephalonFolder.pubxml`.

The generated publish output already includes the SDK-generated `web.config` for ASP.NET Core Module V2 (`AspNetCoreModuleV2`), so IIS can proxy the app through `dotnet .\{hostProjectName}.dll` without extra host-specific code.

The generated install script assumes:

- published output lives at `C:\inetpub\sites\{request.AppName}\current`
- IIS plus the ASP.NET Core Hosting Bundle are installed on the Windows target
- the real install step is run from an elevated PowerShell session

Files in this folder:

- `install-site.ps1`
- `remove-site.ps1`

From the solution root, publish the host with:

```powershell
dotnet publish src/{hostProjectName}/{hostProjectName}.csproj -p:PublishProfile=CephalonFolder
```

On the Windows target, copy the published output and preview the IIS install contract with:

```powershell
New-Item -ItemType Directory -Path 'C:\inetpub\sites\{request.AppName}\current' -Force | Out-Null
Copy-Item -Path .\artifacts\publish\{hostProjectName}\* -Destination 'C:\inetpub\sites\{request.AppName}\current' -Recurse -Force
pwsh ./deploy/iis/install-site.ps1 -PhysicalPath 'C:\inetpub\sites\{request.AppName}\current' -Preview
```

When you are ready to install the site for real, rerun the same command from an elevated PowerShell session without `-Preview`:

```powershell
pwsh ./deploy/iis/install-site.ps1 -PhysicalPath 'C:\inetpub\sites\{request.AppName}\current'
```

That creates:

- an application pool named `{request.AppName}` with `No Managed Code`
- a site named `{request.AppName}` bound by default to `http/*:8080:`
- the site's root application mapped to the generated app pool

To remove the site later:

```powershell
pwsh ./deploy/iis/remove-site.ps1
```

{restDocsLine}
""";
    }

    private static string BuildIisInstallScript(ScaffoldRequest request)
    {
        return $$"""
param(
    [string]$SiteName = "{{EscapeString(request.AppName)}}",
    [string]$AppPoolName = "{{EscapeString(request.AppName)}}",
    [string]$PhysicalPath = "C:\inetpub\sites\{{EscapeString(request.AppName)}}\current",
    [string]$BindingInformation = "*:8080:",
    [string]$AppCmdPath = "$env:WinDir\System32\inetsrv\appcmd.exe",
    [switch]$Preview
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Test-IsAdministrator {
    $principal = [Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

$webConfigPath = Join-Path $PhysicalPath "web.config"
if (-not (Test-Path -LiteralPath $webConfigPath)) {
    throw "Expected published IIS host assets at '$webConfigPath'."
}

$addAppPoolArguments = @("add", "apppool", "/name:$AppPoolName", "/managedRuntimeVersion:", "/managedPipelineMode:Integrated")
$setAppPoolArguments = @("set", "apppool", "/apppool.name:$AppPoolName", "/processModel.idleTimeout:00:00:00", "/startMode:AlwaysRunning")
$addSiteArguments = @("add", "site", "/name:$SiteName", "/bindings:http/$BindingInformation", "/physicalPath:$PhysicalPath")
$setAppArguments = @("set", "app", "/app.name:$SiteName/", "/applicationPool:$AppPoolName")
$startSiteArguments = @("start", "site", "/site.name:$SiteName")

if ($Preview) {
    Write-Host "Preview only. The following commands would run:" -ForegroundColor Yellow
    Write-Host "$AppCmdPath $($addAppPoolArguments -join ' ')" -ForegroundColor Cyan
    Write-Host "$AppCmdPath $($setAppPoolArguments -join ' ')" -ForegroundColor Cyan
    Write-Host "$AppCmdPath $($addSiteArguments -join ' ')" -ForegroundColor Cyan
    Write-Host "$AppCmdPath $($setAppArguments -join ' ')" -ForegroundColor Cyan
    Write-Host "$AppCmdPath $($startSiteArguments -join ' ')" -ForegroundColor Cyan
    return
}

if (-not (Test-IsAdministrator)) {
    throw "Installing an IIS site requires an elevated PowerShell session."
}

if (-not (Test-Path -LiteralPath $AppCmdPath)) {
    throw "Could not find appcmd.exe at '$AppCmdPath'. Install IIS and the management tools first."
}

& $AppCmdPath @addAppPoolArguments
if ($LASTEXITCODE -ne 0) {
    throw "appcmd add apppool failed for '$AppPoolName'."
}

& $AppCmdPath @setAppPoolArguments
if ($LASTEXITCODE -ne 0) {
    throw "appcmd set apppool failed for '$AppPoolName'."
}

& $AppCmdPath @addSiteArguments
if ($LASTEXITCODE -ne 0) {
    throw "appcmd add site failed for '$SiteName'."
}

& $AppCmdPath @setAppArguments
if ($LASTEXITCODE -ne 0) {
    throw "appcmd set app failed for '$SiteName/'."
}

& $AppCmdPath @startSiteArguments
if ($LASTEXITCODE -ne 0) {
    throw "appcmd start site failed for '$SiteName'."
}

Write-Host ""
Write-Host "IIS site '$SiteName' created successfully." -ForegroundColor Green
Write-Host "App pool: $AppPoolName" -ForegroundColor Cyan
Write-Host "Physical path: $PhysicalPath" -ForegroundColor Cyan
""";
    }

    private static string BuildIisRemoveScript(ScaffoldRequest request)
    {
        return $$"""
param(
    [string]$SiteName = "{{EscapeString(request.AppName)}}",
    [string]$AppPoolName = "{{EscapeString(request.AppName)}}",
    [string]$AppCmdPath = "$env:WinDir\System32\inetsrv\appcmd.exe",
    [switch]$Preview
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Test-IsAdministrator {
    $principal = [Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

$stopSiteArguments = @("stop", "site", "/site.name:$SiteName")
$deleteSiteArguments = @("delete", "site", "/site.name:$SiteName")
$deleteAppPoolArguments = @("delete", "apppool", "/apppool.name:$AppPoolName")

if ($Preview) {
    Write-Host "Preview only. The following commands would run:" -ForegroundColor Yellow
    Write-Host "$AppCmdPath $($stopSiteArguments -join ' ')" -ForegroundColor Cyan
    Write-Host "$AppCmdPath $($deleteSiteArguments -join ' ')" -ForegroundColor Cyan
    Write-Host "$AppCmdPath $($deleteAppPoolArguments -join ' ')" -ForegroundColor Cyan
    return
}

if (-not (Test-IsAdministrator)) {
    throw "Removing an IIS site requires an elevated PowerShell session."
}

if (-not (Test-Path -LiteralPath $AppCmdPath)) {
    throw "Could not find appcmd.exe at '$AppCmdPath'. Install IIS and the management tools first."
}

& $AppCmdPath @stopSiteArguments
& $AppCmdPath @deleteSiteArguments
if ($LASTEXITCODE -ne 0) {
    throw "appcmd delete site failed for '$SiteName'."
}

& $AppCmdPath @deleteAppPoolArguments
if ($LASTEXITCODE -ne 0) {
    throw "appcmd delete apppool failed for '$AppPoolName'."
}

Write-Host "IIS site '$SiteName' and app pool '$AppPoolName' deleted successfully." -ForegroundColor Green
""";
    }

    private static string BuildAzureAppServiceReadme(
        AppProfile appProfile,
        ScaffoldRequest request,
        string hostProjectName)
    {
        var restDocsLine = appProfile.Transports.Any(transport =>
            string.Equals(transport.Id, "rest", StringComparison.OrdinalIgnoreCase))
            ? "Then inspect the running host with `/engine`, `/engine/snapshot`, `/health/ready`, and `/scalar`."
            : "Then inspect the running host with `/engine`, `/engine/snapshot`, and `/health/ready`.";

        return $"""
# Azure App Service deployment

These assets provide the Azure App Service run-from-package baseline for `{request.AppName}` after the host is published through `CephalonFolder.pubxml`.

The generated deployment script packages the published output into `azure-app-service.zip`, sets `WEBSITE_RUN_FROM_PACKAGE=1`, and deploys the ZIP artifact through `az webapp deploy`.

The generated deploy script assumes:

- published output lives at `./artifacts/publish/{hostProjectName}/`
- Azure CLI is installed on the deployment machine
- `az login` has already been completed for the target subscription
- the target App Service app already exists

Files in this folder:

- `deploy-zip.ps1`

From the solution root, publish the host with:

```powershell
dotnet publish src/{hostProjectName}/{hostProjectName}.csproj -p:PublishProfile=CephalonFolder
```

Preview the Azure deployment contract locally with:

```powershell
pwsh ./deploy/azure-app-service/deploy-zip.ps1 -ResourceGroupName my-resource-group -AppName my-cephalon-app -Preview
```

When you are ready to deploy for real:

```powershell
az login
pwsh ./deploy/azure-app-service/deploy-zip.ps1 -ResourceGroupName my-resource-group -AppName my-cephalon-app
```

The generated ZIP package is written to `./artifacts/deploy/{hostProjectName}/azure-app-service.zip`.

{restDocsLine}
""";
    }

    private static string BuildAzureAppServiceDeployScript(
        ScaffoldRequest request,
        string hostProjectName)
    {
        return $$"""
param(
    [string]$ResourceGroupName = "replace-with-resource-group",
    [string]$AppName = "replace-with-app-service-name",
    [string]$SlotName = "",
    [string]$PublishRoot = (Join-Path (Join-Path $PSScriptRoot "..\..") "artifacts\publish\{{EscapeString(hostProjectName)}}"),
    [string]$PackagePath = (Join-Path (Join-Path (Join-Path $PSScriptRoot "..\..") "artifacts\deploy\{{EscapeString(hostProjectName)}}") "azure-app-service.zip"),
    [switch]$Preview
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Test-PlaceholderValue {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    return $Value.StartsWith("replace-with-", [System.StringComparison]::Ordinal)
}

function Get-AzCommandArguments {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    if ([string]::IsNullOrWhiteSpace($SlotName)) {
        return $Arguments
    }

    return $Arguments + @("--slot", $SlotName)
}

function Format-Command {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Command,
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    $parts = @($Command) + $Arguments

    return ($parts | ForEach-Object {
        if ($_ -match '\s') {
            '"' + $_ + '"'
        }
        else {
            $_
        }
    }) -join ' '
}

if (-not (Test-Path -LiteralPath $PublishRoot)) {
    throw "Expected published output at '$PublishRoot'. Run dotnet publish with CephalonFolder first."
}

$publishDllPath = Join-Path $PublishRoot "{{EscapeString(hostProjectName)}}.dll"
$webConfigPath = Join-Path $PublishRoot "web.config"

foreach ($path in @($publishDllPath, $webConfigPath)) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Expected published asset at '$path'."
    }
}

$packageDirectory = Split-Path -Parent $PackagePath
if (-not [string]::IsNullOrWhiteSpace($packageDirectory)) {
    New-Item -ItemType Directory -Path $packageDirectory -Force | Out-Null
}

if (Test-Path -LiteralPath $PackagePath) {
    Remove-Item -LiteralPath $PackagePath -Force
}

Compress-Archive -Path (Join-Path $PublishRoot '*') -DestinationPath $PackagePath -Force

$setRunFromPackageArguments = Get-AzCommandArguments -Arguments @(
    "webapp", "config", "appsettings", "set",
    "--resource-group", $ResourceGroupName,
    "--name", $AppName,
    "--settings", "WEBSITE_RUN_FROM_PACKAGE=1")
$deployArguments = Get-AzCommandArguments -Arguments @(
    "webapp", "deploy",
    "--resource-group", $ResourceGroupName,
    "--name", $AppName,
    "--src-path", $PackagePath,
    "--type", "zip",
    "--clean", "true")

if ($Preview) {
    Write-Host "Created Azure App Service ZIP package: $PackagePath" -ForegroundColor Green
    Write-Host "Preview only. The following commands would run:" -ForegroundColor Yellow
    Write-Host (Format-Command -Command "az" -Arguments $setRunFromPackageArguments) -ForegroundColor Cyan
    Write-Host (Format-Command -Command "az" -Arguments $deployArguments) -ForegroundColor Cyan
    return
}

if (Test-PlaceholderValue -Value $ResourceGroupName -or Test-PlaceholderValue -Value $AppName) {
    throw "Set both -ResourceGroupName and -AppName before running a live Azure App Service deployment."
}

$null = Get-Command az -ErrorAction Stop

& az @setRunFromPackageArguments
if ($LASTEXITCODE -ne 0) {
    throw "Failed to set WEBSITE_RUN_FROM_PACKAGE for '$AppName'."
}

& az @deployArguments
if ($LASTEXITCODE -ne 0) {
    throw "Failed to deploy ZIP package to '$AppName'."
}

Write-Host ""
Write-Host "Azure App Service deployment completed successfully." -ForegroundColor Green
Write-Host "Package: $PackagePath" -ForegroundColor Cyan
Write-Host "App: $AppName" -ForegroundColor Cyan
""";
    }

    private static string BuildContainerImageReadme(ScaffoldRequest request)
    {
        var imagePlaceholder = BuildContainerImagePlaceholder(request);

        return $"""
# Container image publishing

These assets provide the provider-neutral container image publishing baseline for `{request.AppName}` from the solution root and Dockerfile.

The generated publish script validates the generated app root, builds the shipped Dockerfile with one or more image tags, and can optionally push those tags to the registry you choose without inventing provider-specific packaging first.

The generated publish script assumes:

- the generated app root keeps the shipped `Dockerfile` and `NuGet.config`
- `NuGet.config` points at a reachable Cephalon package source or `./.cephalon/packages` has been seeded before the container image is built
- Docker Desktop or another compatible Docker engine is installed on the build machine
- `docker login` has already been completed for the target registry before you use `-Push`

Files in this folder:

- `publish-image.ps1`

Preview the Docker build and push contract locally with:

```powershell
pwsh ./deploy/container-image/publish-image.ps1 -Image {imagePlaceholder} -Push -Preview
```

Build the generated image locally without pushing it yet:

```powershell
pwsh ./deploy/container-image/publish-image.ps1 -Image {imagePlaceholder}
```

When you are ready to publish the image to the registry:

```powershell
docker login ghcr.io
pwsh ./deploy/container-image/publish-image.ps1 -Image {imagePlaceholder} -AdditionalTags replace-with-registry/{BuildKubernetesResourceName(request)}:stable -Push
```

If you need to target a specific runtime platform during the build, also pass `-Platform linux/amd64` or another Docker-compatible platform string.

After the push completes, reuse the same image tag with `deploy/kubernetes/apply.ps1` or another hosted container deployment surface.
""";
    }

    private static string BuildContainerImagePublishScript(ScaffoldRequest request)
    {
        var imagePlaceholder = BuildContainerImagePlaceholder(request);

        return $$"""
param(
    [string]$Image = "{{EscapeString(imagePlaceholder)}}",
    [string[]]$AdditionalTags = @(),
    [string]$SourceRoot = (Join-Path (Join-Path $PSScriptRoot "..\..") "."),
    [string]$DockerfilePath = (Join-Path (Join-Path $PSScriptRoot "..\..") "Dockerfile"),
    [string]$Platform = "",
    [switch]$Pull,
    [switch]$Push,
    [switch]$Preview
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Test-PlaceholderValue {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    return $Value.StartsWith("replace-with-", [System.StringComparison]::Ordinal)
}

function Format-Command {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Command,
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    $parts = @($Command) + $Arguments

    return ($parts | ForEach-Object {
        if ($_ -match '\s') {
            '"' + $_ + '"'
        }
        else {
            $_
        }
    }) -join ' '
}

function Invoke-Docker {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,
        [Parameter(Mandatory = $true)]
        [string]$WorkingDirectory
    )

    Push-Location $WorkingDirectory
    try {
        & docker @Arguments
        if ($LASTEXITCODE -ne 0) {
            throw "docker command failed: docker $($Arguments -join ' ')"
        }
    }
    finally {
        Pop-Location
    }
}

function Get-ImageTags {
    $seen = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    $tags = [System.Collections.Generic.List[string]]::new()

    foreach ($tag in @($Image) + $AdditionalTags) {
        if ([string]::IsNullOrWhiteSpace($tag)) {
            continue
        }

        if ($seen.Add($tag)) {
            $tags.Add($tag)
        }
    }

    return $tags.ToArray()
}

function Get-DockerBuildArguments {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResolvedSourceRoot,
        [Parameter(Mandatory = $true)]
        [string]$ResolvedDockerfilePath,
        [Parameter(Mandatory = $true)]
        [string[]]$ImageTags
    )

    $arguments = @("build", "-f", $ResolvedDockerfilePath)

    foreach ($tag in $ImageTags) {
        $arguments += @("-t", $tag)
    }

    if (-not [string]::IsNullOrWhiteSpace($Platform)) {
        $arguments += @("--platform", $Platform)
    }

    if ($Pull) {
        $arguments += "--pull"
    }

    $arguments += $ResolvedSourceRoot

    return $arguments
}

if (-not (Test-Path -LiteralPath $SourceRoot)) {
    throw "Expected generated app root at '$SourceRoot'."
}

if (-not (Test-Path -LiteralPath $DockerfilePath)) {
    throw "Expected generated Dockerfile at '$DockerfilePath'."
}

$resolvedSourceRoot = (Resolve-Path -LiteralPath $SourceRoot).Path
$resolvedDockerfilePath = (Resolve-Path -LiteralPath $DockerfilePath).Path
$nuGetConfigPath = Join-Path $resolvedSourceRoot "NuGet.config"

foreach ($path in @($resolvedDockerfilePath, $nuGetConfigPath)) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Expected generated asset at '$path'."
    }
}

$imageTags = Get-ImageTags
if ($imageTags.Count -eq 0) {
    throw "Provide at least one -Image or -AdditionalTags value."
}

$buildArguments = Get-DockerBuildArguments -ResolvedSourceRoot $resolvedSourceRoot -ResolvedDockerfilePath $resolvedDockerfilePath -ImageTags $imageTags

if ($Preview) {
    Write-Host "Validated generated app root: $resolvedSourceRoot" -ForegroundColor Green
    Write-Host "Validated Dockerfile: $resolvedDockerfilePath" -ForegroundColor Green
    Write-Host "Preview only. The following commands would run:" -ForegroundColor Yellow
    Write-Host (Format-Command -Command "docker" -Arguments $buildArguments) -ForegroundColor Cyan

    if ($Push) {
        foreach ($tag in $imageTags) {
            Write-Host (Format-Command -Command "docker" -Arguments @("push", $tag)) -ForegroundColor Cyan
        }
    }
    else {
        Write-Host "Push skipped. Re-run with -Push when the target registry is ready." -ForegroundColor Yellow
    }

    return
}

foreach ($tag in $imageTags) {
    if (Test-PlaceholderValue -Value $tag) {
        throw "Set -Image and -AdditionalTags to real container-image references before running a live publish."
    }
}

$null = Get-Command docker -ErrorAction Stop

Invoke-Docker -WorkingDirectory $resolvedSourceRoot -Arguments $buildArguments

if ($Push) {
    foreach ($tag in $imageTags) {
        Invoke-Docker -WorkingDirectory $resolvedSourceRoot -Arguments @("push", $tag)
    }
}

Write-Host ""
Write-Host "Container image publishing completed successfully." -ForegroundColor Green
Write-Host "Source root: $resolvedSourceRoot" -ForegroundColor Cyan
Write-Host "Image tags: $($imageTags -join ', ')" -ForegroundColor Cyan

if ($Push) {
    Write-Host "The published image tags are ready for Kubernetes or other hosted container deployment surfaces." -ForegroundColor Cyan
}
else {
    Write-Host "Push skipped. Re-run with -Push when you want to publish the image tags to a registry." -ForegroundColor Yellow
}
""";
    }

    private static string BuildAzureContainerAppsReadme(
        AppProfile appProfile,
        ScaffoldRequest request,
        string hostProjectName)
    {
        var restDocsLine = appProfile.Transports.Any(transport =>
            string.Equals(transport.Id, "rest", StringComparison.OrdinalIgnoreCase))
            ? "Then inspect the running host with `/engine`, `/engine/snapshot`, `/health/ready`, and `/scalar`."
            : "Then inspect the running host with `/engine`, `/engine/snapshot`, and `/health/ready`.";

        return $"""
# Azure Container Apps deployment

These assets provide the Azure Container Apps source-deployment baseline for `{request.AppName}` from the generated app root and Dockerfile.

The generated deployment script uses `az containerapp up --source` so the generated host can move from scaffolded source into a hosted Azure Container Apps baseline without inventing a separate image-packaging workflow first. If you want a provider-neutral registry build/tag/push step before the Azure deploy, use `deploy/container-image/publish-image.ps1` first.

The generated deploy script assumes:

- the generated app root keeps the shipped `Dockerfile` and `NuGet.config`
- `NuGet.config` points at a reachable Cephalon package source or `./.cephalon/packages` has been seeded before the container image is built
- Azure CLI is installed on the deployment machine
- `az login` has already been completed for the target subscription

Files in this folder:

- `deploy-up.ps1`

Preview the Azure Container Apps deployment contract locally with:

```powershell
pwsh ./deploy/azure-container-apps/deploy-up.ps1 -ResourceGroupName my-resource-group -Location eastus -AppName my-cephalon-app -Preview
```

If you want to pin the deployment to an existing Container Apps environment, also pass `-ContainerAppEnvironment my-container-apps-env`.

When you are ready to deploy for real:

```powershell
az login
pwsh ./deploy/azure-container-apps/deploy-up.ps1 -ResourceGroupName my-resource-group -Location eastus -AppName my-cephalon-app -ContainerAppEnvironment my-container-apps-env
```

The generated script deploys from the app root, keeps ingress external by default, targets port `8080`, and passes `ASPNETCORE_HTTP_PORTS=8080` plus `DOTNET_ENVIRONMENT=Production` into the hosted container app.

If you need to extend the runtime environment contract, pass extra `-EnvironmentVariables key=value` entries when you invoke the script.

{restDocsLine}
""";
    }

    private static string BuildAzureContainerAppsDeployScript(
        ScaffoldRequest request,
        string hostProjectName)
    {
        var defaultAppName = BuildAzureContainerAppName(request);

        return $$"""
param(
    [string]$ResourceGroupName = "replace-with-resource-group",
    [string]$AppName = "{{EscapeString(defaultAppName)}}",
    [string]$Location = "replace-with-azure-region",
    [string]$ContainerAppEnvironment = "",
    [string]$SourceRoot = (Join-Path (Join-Path $PSScriptRoot "..\..") "."),
    [ValidateSet("external", "internal")]
    [string]$Ingress = "external",
    [ValidateRange(1, 65535)]
    [int]$TargetPort = 8080,
    [string[]]$EnvironmentVariables = @(
        "ASPNETCORE_HTTP_PORTS=8080",
        "DOTNET_ENVIRONMENT=Production"),
    [switch]$Preview
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Test-PlaceholderValue {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    return $Value.StartsWith("replace-with-", [System.StringComparison]::Ordinal)
}

function Format-Command {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Command,
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    $parts = @($Command) + $Arguments

    return ($parts | ForEach-Object {
        if ($_ -match '\s') {
            '"' + $_ + '"'
        }
        else {
            $_
        }
    }) -join ' '
}

function Get-AzCommandArguments {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResolvedSourceRoot
    )

    $arguments = @(
        "containerapp", "up",
        "--name", $AppName,
        "--resource-group", $ResourceGroupName,
        "--location", $Location,
        "--source", $ResolvedSourceRoot,
        "--ingress", $Ingress,
        "--target-port", $TargetPort.ToString([System.Globalization.CultureInfo]::InvariantCulture))

    if (-not [string]::IsNullOrWhiteSpace($ContainerAppEnvironment)) {
        $arguments += @("--environment", $ContainerAppEnvironment)
    }

    if ($EnvironmentVariables.Count -gt 0) {
        $arguments += @("--env-vars") + $EnvironmentVariables
    }

    return $arguments
}

if (-not (Test-Path -LiteralPath $SourceRoot)) {
    throw "Expected generated app root at '$SourceRoot'."
}

$resolvedSourceRoot = (Resolve-Path -LiteralPath $SourceRoot).Path
$dockerfilePath = Join-Path $resolvedSourceRoot "Dockerfile"
$nuGetConfigPath = Join-Path $resolvedSourceRoot "NuGet.config"
$hostProjectPath = Join-Path $resolvedSourceRoot "src\{{EscapeString(hostProjectName)}}\{{EscapeString(hostProjectName)}}.csproj"

foreach ($path in @($dockerfilePath, $nuGetConfigPath)) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Expected generated asset at '$path'."
    }
}

$upArguments = Get-AzCommandArguments -ResolvedSourceRoot $resolvedSourceRoot

if ($Preview) {
    Write-Host "Validated Azure Container Apps source root: $resolvedSourceRoot" -ForegroundColor Green
    Write-Host "Detected generated host project: $hostProjectPath" -ForegroundColor Green
    Write-Host "Preview only. The following command would run:" -ForegroundColor Yellow
    Write-Host (Format-Command -Command "az" -Arguments $upArguments) -ForegroundColor Cyan
    return
}

if (Test-PlaceholderValue -Value $ResourceGroupName -or Test-PlaceholderValue -Value $Location) {
    throw "Set both -ResourceGroupName and -Location before running a live Azure Container Apps deployment."
}

if ($AppName -notmatch '^[a-z](?:[a-z0-9-]{0,29}[a-z0-9])?$' -or $AppName.Contains("--", [System.StringComparison]::Ordinal)) {
    throw "Container App names must use lower-case letters, digits, or '-', start with a letter, end with a letter or digit, avoid '--', and stay under 32 characters."
}

$null = Get-Command az -ErrorAction Stop

& az @upArguments
if ($LASTEXITCODE -ne 0) {
    throw "Failed to deploy source root '$resolvedSourceRoot' to Container App '$AppName'."
}

Write-Host ""
Write-Host "Azure Container Apps deployment completed successfully." -ForegroundColor Green
Write-Host "Source root: $resolvedSourceRoot" -ForegroundColor Cyan
Write-Host "App: $AppName" -ForegroundColor Cyan
""";
    }

    private static string BuildKubernetesReadme(
        AppProfile appProfile,
        ScaffoldRequest request)
    {
        var restDocsLine = appProfile.Transports.Any(transport =>
            string.Equals(transport.Id, "rest", StringComparison.OrdinalIgnoreCase))
            ? "Then inspect the running host with `/engine`, `/engine/snapshot`, `/health/ready`, and `/scalar`."
            : "Then inspect the running host with `/engine`, `/engine/snapshot`, and `/health/ready`.";
        var resourceName = BuildKubernetesResourceName(request);

        return $"""
# Kubernetes deployment

These assets provide the platform-neutral Kubernetes baseline for `{request.AppName}` from the generated app root and Dockerfile.

The generated deployment script renders the shipped `deploy/kubernetes/*` manifests through `kubectl kustomize` so the generated host can move from scaffolded source into a generic Kubernetes baseline without inventing a Helm chart or provider-specific packaging workflow first. If you want a provider-neutral registry build/tag/push step before you apply the manifest set, use `deploy/container-image/publish-image.ps1` first.

The generated deploy script assumes:

- the generated app root keeps the shipped `Dockerfile` and `NuGet.config`
- `NuGet.config` points at a reachable Cephalon package source or `./.cephalon/packages` has been seeded before the container image is built
- `kubectl` with `kustomize` support is installed on the deployment machine
- the target cluster can pull the image you pass to the deployment script
- your current `kubectl` context already targets the cluster you want to update

Files in this folder:

- `apply.ps1`
- `kustomization.yaml`
- `namespace.yaml`
- `deployment.yaml`
- `service.yaml`

Preview the Kubernetes deployment contract locally with:

```powershell
pwsh ./deploy/kubernetes/apply.ps1 -Image ghcr.io/example/{resourceName}:latest -Preview
```

By default the generated manifests target namespace `{resourceName}` and expose the host through a `ClusterIP` service on port `80` to container port `8080`. Pass `-Namespace my-namespace` when you need a different namespace.

When you are ready to apply the rendered manifest set to the current cluster context:

```powershell
kubectl config current-context
pwsh ./deploy/kubernetes/apply.ps1 -Image ghcr.io/example/{resourceName}:latest -Namespace {resourceName}
```

After the workload is ready, port-forward the generated service to inspect the host locally:

```powershell
kubectl -n {resourceName} port-forward service/{resourceName} 18080:80
```

The generated manifests keep `ASPNETCORE_HTTP_PORTS=8080`, `DOTNET_ENVIRONMENT=Production`, `readinessProbe` on `/health/ready`, `livenessProbe` on `/health/live`, and a `startupProbe` on `/health/ready`.

{restDocsLine}
""";
    }

    private static string BuildKubernetesApplyScript(ScaffoldRequest request)
    {
        var resourceName = BuildKubernetesResourceName(request);
        var imagePlaceholder = BuildContainerImagePlaceholder(request);

        return $$"""
param(
    [string]$Image = "{{EscapeString(imagePlaceholder)}}",
    [string]$Namespace = "{{EscapeString(resourceName)}}",
    [string]$SourceRoot = (Join-Path (Join-Path $PSScriptRoot "..\..") "."),
    [string]$ManifestRoot = $PSScriptRoot,
    [switch]$Preview
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Test-PlaceholderValue {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    return $Value.StartsWith("replace-with-", [System.StringComparison]::Ordinal)
}

function Test-KubernetesName {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    return $Value -match '^[a-z0-9](?:[-a-z0-9]{0,61}[a-z0-9])?$'
}

function Write-Utf8File {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$Contents
    )

    [System.IO.File]::WriteAllText($Path, $Contents, [System.Text.UTF8Encoding]::new($false))
}

function Invoke-Kubectl {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,
        [Parameter(Mandatory = $true)]
        [string]$WorkingDirectory
    )

    Push-Location $WorkingDirectory
    try {
        $output = & kubectl @Arguments 2>&1
        if ($LASTEXITCODE -ne 0) {
            throw "kubectl command failed: kubectl $($Arguments -join ' ')"
        }

        return ($output | Out-String)
    }
    finally {
        Pop-Location
    }
}

function New-RenderedManifestSet {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResolvedManifestRoot
    )

    $renderRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("cephalon-generated-kubernetes-" + [Guid]::NewGuid().ToString("N"))
    New-Item -ItemType Directory -Path $renderRoot -Force | Out-Null

    Copy-Item -Path (Join-Path $ResolvedManifestRoot '*') -Destination $renderRoot -Recurse -Force

    $kustomizationPath = Join-Path $renderRoot "kustomization.yaml"
    $namespacePath = Join-Path $renderRoot "namespace.yaml"
    $deploymentPath = Join-Path $renderRoot "deployment.yaml"

    $kustomizationContents = (Get-Content -LiteralPath $kustomizationPath -Raw).Replace("namespace: {{EscapeString(resourceName)}}", "namespace: $Namespace")
    $namespaceContents = (Get-Content -LiteralPath $namespacePath -Raw).Replace("name: {{EscapeString(resourceName)}}", "name: $Namespace")
    $deploymentContents = (Get-Content -LiteralPath $deploymentPath -Raw).Replace("image: {{EscapeString(imagePlaceholder)}}", "image: $Image")

    Write-Utf8File -Path $kustomizationPath -Contents $kustomizationContents
    Write-Utf8File -Path $namespacePath -Contents $namespaceContents
    Write-Utf8File -Path $deploymentPath -Contents $deploymentContents

    $manifest = Invoke-Kubectl -WorkingDirectory $renderRoot -Arguments @("kustomize", ".")
    $manifestPath = Join-Path $renderRoot "rendered-manifest.yaml"
    Write-Utf8File -Path $manifestPath -Contents $manifest

    return @{
        RenderRoot = $renderRoot
        Manifest = $manifest
        ManifestPath = $manifestPath
    }
}

if (-not (Test-Path -LiteralPath $SourceRoot)) {
    throw "Expected generated app root at '$SourceRoot'."
}

if (-not (Test-Path -LiteralPath $ManifestRoot)) {
    throw "Expected generated Kubernetes manifest root at '$ManifestRoot'."
}

$resolvedSourceRoot = (Resolve-Path -LiteralPath $SourceRoot).Path
$resolvedManifestRoot = (Resolve-Path -LiteralPath $ManifestRoot).Path
$dockerfilePath = Join-Path $resolvedSourceRoot "Dockerfile"
$nuGetConfigPath = Join-Path $resolvedSourceRoot "NuGet.config"

foreach ($path in @(
    $dockerfilePath,
    $nuGetConfigPath,
    (Join-Path $resolvedManifestRoot "kustomization.yaml"),
    (Join-Path $resolvedManifestRoot "namespace.yaml"),
    (Join-Path $resolvedManifestRoot "deployment.yaml"),
    (Join-Path $resolvedManifestRoot "service.yaml")))
{
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Expected generated asset at '$path'."
    }
}

if (-not (Test-KubernetesName -Value $Namespace)) {
    throw "Kubernetes namespace names must use lower-case letters, digits, or '-', start and end with a letter or digit, and stay under 64 characters."
}

$null = Get-Command kubectl -ErrorAction Stop

$renderedManifest = $null

try {
    $renderedManifest = New-RenderedManifestSet -ResolvedManifestRoot $resolvedManifestRoot

    if ($Preview) {
        Write-Host "Validated generated app root: $resolvedSourceRoot" -ForegroundColor Green
        Write-Host "Validated Kubernetes manifest root: $resolvedManifestRoot" -ForegroundColor Green
        Write-Host "Preview only. The rendered manifest follows:" -ForegroundColor Yellow
        Write-Host $renderedManifest.Manifest -ForegroundColor Cyan
        Write-Host "Use the same command without -Preview to apply this manifest to the current kubectl context." -ForegroundColor Yellow
        return
    }

    if (Test-PlaceholderValue -Value $Image) {
        throw "Set -Image to a pullable container image before running a live Kubernetes apply."
    }

    $null = Invoke-Kubectl -WorkingDirectory $renderedManifest.RenderRoot -Arguments @("apply", "-f", $renderedManifest.ManifestPath)

    Write-Host ""
    Write-Host "Kubernetes deployment apply completed successfully." -ForegroundColor Green
    Write-Host "Source root: $resolvedSourceRoot" -ForegroundColor Cyan
    Write-Host "Namespace: $Namespace" -ForegroundColor Cyan
    Write-Host "Image: $Image" -ForegroundColor Cyan
}
finally {
    if ($renderedManifest -and (Test-Path -LiteralPath $renderedManifest.RenderRoot)) {
        Remove-Item -LiteralPath $renderedManifest.RenderRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}
""";
    }

    private static string BuildKubernetesKustomization(ScaffoldRequest request)
    {
        var resourceName = BuildKubernetesResourceName(request);

        return $"""
apiVersion: kustomize.config.k8s.io/v1beta1
kind: Kustomization
namespace: {resourceName}
resources:
- namespace.yaml
- deployment.yaml
- service.yaml
""";
    }

    private static string BuildKubernetesNamespaceManifest(ScaffoldRequest request)
    {
        var resourceName = BuildKubernetesResourceName(request);

        return $"""
apiVersion: v1
kind: Namespace
metadata:
  name: {resourceName}
  labels:
    app.kubernetes.io/name: {resourceName}
    app.kubernetes.io/part-of: cephalon
""";
    }

    private static string BuildKubernetesDeploymentManifest(ScaffoldRequest request)
    {
        var resourceName = BuildKubernetesResourceName(request);
        var imagePlaceholder = BuildContainerImagePlaceholder(request);

        return $"""
apiVersion: apps/v1
kind: Deployment
metadata:
  name: {resourceName}
  labels:
    app.kubernetes.io/name: {resourceName}
    app.kubernetes.io/part-of: cephalon
    app.kubernetes.io/component: host
spec:
  replicas: 1
  selector:
    matchLabels:
      app.kubernetes.io/name: {resourceName}
      app.kubernetes.io/component: host
  template:
    metadata:
      labels:
        app.kubernetes.io/name: {resourceName}
        app.kubernetes.io/part-of: cephalon
        app.kubernetes.io/component: host
    spec:
      containers:
      - name: {resourceName}
        image: {imagePlaceholder}
        imagePullPolicy: IfNotPresent
        ports:
        - containerPort: 8080
          name: http
        env:
        - name: ASPNETCORE_HTTP_PORTS
          value: "8080"
        - name: DOTNET_ENVIRONMENT
          value: Production
        readinessProbe:
          httpGet:
            path: /health/ready
            port: http
          initialDelaySeconds: 5
          periodSeconds: 10
          timeoutSeconds: 5
          failureThreshold: 3
        livenessProbe:
          httpGet:
            path: /health/live
            port: http
          initialDelaySeconds: 15
          periodSeconds: 20
          timeoutSeconds: 5
          failureThreshold: 3
        startupProbe:
          httpGet:
            path: /health/ready
            port: http
          periodSeconds: 5
          timeoutSeconds: 5
          failureThreshold: 24
        resources:
          requests:
            cpu: 100m
            memory: 128Mi
          limits:
            cpu: 500m
            memory: 512Mi
""";
    }

    private static string BuildKubernetesServiceManifest(ScaffoldRequest request)
    {
        var resourceName = BuildKubernetesResourceName(request);

        return $"""
apiVersion: v1
kind: Service
metadata:
  name: {resourceName}
  labels:
    app.kubernetes.io/name: {resourceName}
    app.kubernetes.io/part-of: cephalon
    app.kubernetes.io/component: host
spec:
  type: ClusterIP
  selector:
    app.kubernetes.io/name: {resourceName}
    app.kubernetes.io/component: host
  ports:
  - name: http
    port: 80
    targetPort: http
""";
    }

    private static string BuildSystemdReadme(
        AppProfile appProfile,
        ScaffoldRequest request,
        string hostProjectName)
    {
        var restDocsLine = appProfile.Transports.Any(transport =>
            string.Equals(transport.Id, "rest", StringComparison.OrdinalIgnoreCase))
            ? "Then inspect the running host with `/engine`, `/engine/snapshot`, `/health/ready`, and `/scalar`."
            : "Then inspect the running host with `/engine`, `/engine/snapshot`, and `/health/ready`.";

        return $"""
# Linux systemd deployment

These assets provide the self-hosted Linux baseline for `{request.AppName}` after the host is published through `CephalonFolder.pubxml`.

The generated service files assume:

- published output lives at `/opt/{request.AppName}/current`
- the optional environment override file lives at `/etc/cephalon/{request.AppName}.env`

Files in this folder:

- `{request.AppName}.service`
- `{request.AppName}.env`

From the solution root, publish the host with:

```powershell
dotnet publish src/{hostProjectName}/{hostProjectName}.csproj -p:PublishProfile=CephalonFolder
```

On the Linux target, install the published output and service assets with:

```bash
sudo install -d /opt/{request.AppName}/current
sudo cp -R ./artifacts/publish/{hostProjectName}/. /opt/{request.AppName}/current/
sudo install -d /etc/cephalon
sudo install -m 0644 ./deploy/linux/systemd/{request.AppName}.env /etc/cephalon/{request.AppName}.env
sudo install -m 0644 ./deploy/linux/systemd/{request.AppName}.service /etc/systemd/system/{request.AppName}.service
sudo systemd-analyze verify /etc/systemd/system/{request.AppName}.service
sudo systemctl daemon-reload
sudo systemctl enable --now {request.AppName}.service
```

To inspect startup logs:

```bash
sudo journalctl -u {request.AppName}.service -f
```

{restDocsLine}
""";
    }

    private static string BuildSystemdServiceUnit(
        ScaffoldRequest request,
        string hostProjectName)
    {
        return $"""
[Unit]
Description={request.AppName} Cephalon host
Wants=network-online.target
After=network-online.target

[Service]
Type=simple
WorkingDirectory=/opt/{request.AppName}/current
EnvironmentFile=-/etc/cephalon/{request.AppName}.env
Environment=DOTNET_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://0.0.0.0:8080
ExecStart=/usr/bin/env dotnet /opt/{request.AppName}/current/{hostProjectName}.dll
Restart=on-failure
RestartSec=5
KillSignal=SIGINT
SyslogIdentifier={request.AppName}
DynamicUser=true
NoNewPrivileges=true
PrivateTmp=true
ProtectSystem=full
ProtectHome=true

[Install]
WantedBy=multi-user.target
""";
    }

    private static string BuildSystemdEnvironmentFile()
    {
        return """
# Override environment variables for the generated Cephalon host.
DOTNET_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:8080
# Engine__Observability__Telemetry__Provider=OpenTelemetry
# Engine__Observability__Telemetry__UseSelfHostedDefaults=true
# Engine__Observability__Telemetry__Protocol=otlp/http
# Engine__Observability__Telemetry__Endpoint=http://localhost:4318
# Engine__Observability__Telemetry__ExportLogs=true
# Engine__Observability__Telemetry__ExportMetrics=true
# Engine__Observability__Telemetry__ExportTraces=true
""";
    }

    private static string BuildAzureContainerAppName(ScaffoldRequest request)
    {
        var slug = ScaffoldRequest.ToSlug(request.AppName, "cephalon-app");

        if (!char.IsLetter(slug[0]))
        {
            slug = $"c-{slug}";
        }

        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        slug = slug.Trim('-');

        if (slug.Length > 31)
        {
            slug = slug[..31].TrimEnd('-');
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            return "cephalon-app";
        }

        if (!char.IsLetterOrDigit(slug[^1]))
        {
            slug = slug.TrimEnd('-');
        }

        return string.IsNullOrWhiteSpace(slug) ? "cephalon-app" : slug;
    }

    private static string BuildKubernetesResourceName(ScaffoldRequest request)
    {
        var slug = ScaffoldRequest.ToSlug(request.AppName, "cephalon-app");

        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        slug = slug.Trim('-');

        if (slug.Length > 63)
        {
            slug = slug[..63].TrimEnd('-');
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            return "cephalon-app";
        }

        if (!char.IsLetterOrDigit(slug[0]))
        {
            slug = $"app-{slug}";
        }

        if (slug.Length > 63)
        {
            slug = slug[..63].TrimEnd('-');
        }

        if (!char.IsLetterOrDigit(slug[^1]))
        {
            slug = slug.TrimEnd('-');
        }

        return string.IsNullOrWhiteSpace(slug) ? "cephalon-app" : slug;
    }

    private static string BuildContainerImagePlaceholder(ScaffoldRequest request)
    {
        return $"replace-with-registry/{BuildKubernetesResourceName(request)}:latest";
    }

    private static string ResolveHostProjectName(AppProfile appProfile, ScaffoldRequest request)
    {
        return string.Equals(appProfile.BlueprintId, "microservice", StringComparison.OrdinalIgnoreCase)
            ? $"{request.AppName}.Service"
            : $"{request.AppName}.Host";
    }

    private static string BuildDockerfile(RenderedProject hostProject)
    {
        return $"""
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish {hostProject.Path}/{hostProject.Name}.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "{hostProject.Name}.dll"]
""";
    }

    private static string BuildComposeFile(ScaffoldRequest request)
    {
        var serviceName = ScaffoldRequest.ToSlug(request.AppName, "cephalon-app");

        return $"""
services:
  {serviceName}:
    build:
      context: .
      dockerfile: Dockerfile
    environment:
      ASPNETCORE_HTTP_PORTS: 8080
      DOTNET_ENVIRONMENT: Container
      Engine__Observability__Telemetry__Provider: OpenTelemetry
      Engine__Observability__Telemetry__Protocol: otlp/http
      Engine__Observability__Telemetry__Endpoint: http://otel-collector:4318
      Engine__Observability__Telemetry__ExportLogs: "true"
      Engine__Observability__Telemetry__ExportMetrics: "true"
      Engine__Observability__Telemetry__ExportTraces: "true"
    ports:
      - "8080:8080"
    depends_on:
      - otel-collector

  otel-collector:
    image: otel/opentelemetry-collector-contrib:0.149.0
    command:
      - "--config=/etc/otelcol-contrib/config.yaml"
    volumes:
      - ./otel-collector-config.yaml:/etc/otelcol-contrib/config.yaml:ro
    ports:
      - "13133:13133"
""";
    }

    private static string BuildOtelCollectorConfig()
    {
        return """
extensions:
  health_check:
    endpoint: 0.0.0.0:13133

receivers:
  otlp:
    protocols:
      http:
        endpoint: 0.0.0.0:4318

processors:
  batch: {}

exporters:
  debug:
    verbosity: normal

service:
  extensions:
    - health_check
  pipelines:
    logs:
      receivers:
        - otlp
      processors:
        - batch
      exporters:
        - debug
    metrics:
      receivers:
        - otlp
      processors:
        - batch
      exporters:
        - debug
    traces:
      receivers:
        - otlp
      processors:
        - batch
      exporters:
        - debug
""";
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
                packages: GetEffectiveProjectPackages(Source.Template, Source.Packages),
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

    private static string[] GetEffectiveProjectPackages(
        string template,
        IReadOnlyList<string> packages)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(template);
        ArgumentNullException.ThrowIfNull(packages);

        var effectivePackages = packages.ToList();

        if (template is "cephalon-web-host" or "cephalon-service-host")
        {
            effectivePackages.Add("Cephalon.Observability.Serilog");
            effectivePackages.Add("Serilog.Sinks.Console");
        }

        return effectivePackages
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
