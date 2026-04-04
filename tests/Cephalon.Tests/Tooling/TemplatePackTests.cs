using System.IO.Compression;
using System.Text.Json;
using Cephalon.Tests.Support;

namespace Cephalon.Tests.Tooling;

public sealed class TemplatePackTests
{
    [Fact]
    public void TemplatePackContainsExpectedBlueprintTemplates()
    {
        var templateRoot = FindRepositoryDirectory(Path.Combine("templates", "Cephalon.TemplatePack", "templates"));
        var expectedTemplates = new[]
        {
            ("cephalon-modular-monolith", "Cephalon.Templates.ModularMonolith", "cephalon-monolith", "CephalonTemplateApp"),
            ("cephalon-modular-vertical-slice", "Cephalon.Templates.ModularVerticalSlice", "cephalon-slice", "CephalonTemplateApp"),
            ("cephalon-microservice", "Cephalon.Templates.Microservice", "cephalon-microservice", "CephalonTemplateApp"),
            ("cephalon-module", "Cephalon.Templates.Module", "cephalon-module", "CephalonTemplateModule"),
            ("cephalon-rest-module", "Cephalon.Templates.RestModule", "cephalon-rest-module", "CephalonTemplateModule")
        };

        foreach (var (folderName, identity, shortName, sourceName) in expectedTemplates)
        {
            var templateJsonPath = Path.Combine(templateRoot, folderName, ".template.config", "template.json");
            Assert.True(File.Exists(templateJsonPath), $"Template config '{templateJsonPath}' was not found.");

            using var document = JsonDocument.Parse(File.ReadAllText(templateJsonPath));
            Assert.Equal(identity, document.RootElement.GetProperty("identity").GetString());
            Assert.Equal(shortName, document.RootElement.GetProperty("shortName").GetString());
            Assert.Equal(sourceName, document.RootElement.GetProperty("sourceName").GetString());
        }
    }

    [Fact]
    public void TemplatePackProjectPacksTemplatesIntoNuGetContentFolder()
    {
        var templateProjectPath = FindRepositoryFile(Path.Combine("templates", "Cephalon.TemplatePack", "Cephalon.TemplatePack.csproj"));
        var outputPath = Path.Combine(Path.GetTempPath(), $"cephalon-template-pack-{Guid.NewGuid():N}");

        Directory.CreateDirectory(outputPath);

        try
        {
            var result = RunProcess(
                "dotnet",
                $"pack \"{templateProjectPath}\" -c Debug -o \"{outputPath}\"",
                workingDirectory: Path.GetDirectoryName(templateProjectPath)!);

            Assert.Equal(0, result.ExitCode);

            var packagePath = Directory.GetFiles(outputPath, "*.nupkg", SearchOption.TopDirectoryOnly)
                .Single(path => !path.EndsWith(".symbols.nupkg", StringComparison.OrdinalIgnoreCase));

            using var package = ZipFile.OpenRead(packagePath);
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-modular-monolith/.template.config/template.json", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-modular-vertical-slice/.template.config/template.json", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-microservice/.template.config/template.json", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-module/.template.config/template.json", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-rest-module/.template.config/template.json", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-module/cephalon.package.json", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-rest-module/cephalon.package.json", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-modular-monolith/Dockerfile", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-modular-monolith/compose.yaml", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-modular-monolith/NuGet.config", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-modular-monolith/.cephalon/packages/README.md", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-modular-monolith/Properties/PublishProfiles/CephalonFolder.pubxml", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-modular-monolith/deploy/windows-service/install-service.ps1", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-modular-monolith/deploy/windows-service/remove-service.ps1", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-modular-monolith/deploy/iis/install-site.ps1", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-modular-monolith/deploy/iis/remove-site.ps1", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-modular-monolith/deploy/azure-app-service/deploy-zip.ps1", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-modular-monolith/deploy/azure-app-service/README.md", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-modular-monolith/deploy/container-image/publish-image.ps1", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-modular-monolith/deploy/container-image/README.md", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-modular-monolith/deploy/azure-container-apps/deploy-up.ps1", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-modular-monolith/deploy/azure-container-apps/README.md", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-modular-monolith/deploy/kubernetes/apply.ps1", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-modular-monolith/deploy/kubernetes/kustomization.yaml", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-modular-monolith/deploy/kubernetes/deployment.yaml", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-modular-monolith/deploy/kubernetes/service.yaml", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-modular-monolith/deploy/linux/systemd/CephalonTemplateApp.service", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-modular-monolith/deploy/linux/systemd/CephalonTemplateApp.env", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-microservice/Dockerfile", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-microservice/Properties/PublishProfiles/CephalonFolder.pubxml", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-microservice/deploy/windows-service/install-service.ps1", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-microservice/deploy/windows-service/remove-service.ps1", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-microservice/deploy/iis/install-site.ps1", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-microservice/deploy/iis/remove-site.ps1", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-microservice/deploy/azure-app-service/deploy-zip.ps1", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-microservice/deploy/azure-app-service/README.md", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-microservice/deploy/container-image/publish-image.ps1", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-microservice/deploy/container-image/README.md", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-microservice/deploy/azure-container-apps/deploy-up.ps1", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-microservice/deploy/azure-container-apps/README.md", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-microservice/deploy/kubernetes/apply.ps1", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-microservice/deploy/kubernetes/kustomization.yaml", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-microservice/deploy/kubernetes/deployment.yaml", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-microservice/deploy/kubernetes/service.yaml", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-microservice/deploy/linux/systemd/CephalonTemplateApp.service", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-modular-vertical-slice/Dockerfile", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-modular-vertical-slice/Properties/PublishProfiles/CephalonFolder.pubxml", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-modular-vertical-slice/deploy/windows-service/install-service.ps1", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-modular-vertical-slice/deploy/windows-service/remove-service.ps1", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-modular-vertical-slice/deploy/iis/install-site.ps1", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-modular-vertical-slice/deploy/iis/remove-site.ps1", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-modular-vertical-slice/deploy/azure-app-service/deploy-zip.ps1", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-modular-vertical-slice/deploy/azure-app-service/README.md", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-modular-vertical-slice/deploy/container-image/publish-image.ps1", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-modular-vertical-slice/deploy/container-image/README.md", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-modular-vertical-slice/deploy/azure-container-apps/deploy-up.ps1", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-modular-vertical-slice/deploy/azure-container-apps/README.md", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-modular-vertical-slice/deploy/kubernetes/apply.ps1", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-modular-vertical-slice/deploy/kubernetes/kustomization.yaml", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-modular-vertical-slice/deploy/kubernetes/deployment.yaml", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-modular-vertical-slice/deploy/kubernetes/service.yaml", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("content/templates/cephalon-modular-vertical-slice/deploy/linux/systemd/CephalonTemplateApp.service", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(package.Entries, entry =>
                entry.FullName.EndsWith("PACKAGE.md", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            if (Directory.Exists(outputPath))
            {
                Directory.Delete(outputPath, recursive: true);
            }
        }
    }

    [Fact]
    public void TemplatePackCanBeInstalledAndGenerateAMonolithTemplate()
    {
        var templateProjectPath = FindRepositoryFile(Path.Combine("templates", "Cephalon.TemplatePack", "Cephalon.TemplatePack.csproj"));
        var packageOutputPath = Path.Combine(Path.GetTempPath(), $"cephalon-template-pack-{Guid.NewGuid():N}");
        var customHivePath = Path.Combine(Path.GetTempPath(), $"cephalon-template-hive-{Guid.NewGuid():N}");
        var appOutputPath = Path.Combine(Path.GetTempPath(), $"cephalon-template-app-{Guid.NewGuid():N}");
        var moduleOutputPath = Path.Combine(Path.GetTempPath(), $"cephalon-template-module-{Guid.NewGuid():N}");

        Directory.CreateDirectory(packageOutputPath);
        Directory.CreateDirectory(customHivePath);

        try
        {
            var packResult = RunProcess(
                "dotnet",
                $"pack \"{templateProjectPath}\" -c Debug -o \"{packageOutputPath}\"",
                workingDirectory: Path.GetDirectoryName(templateProjectPath)!);

            Assert.Equal(0, packResult.ExitCode);

            var packagePath = Directory.GetFiles(packageOutputPath, "*.nupkg", SearchOption.TopDirectoryOnly)
                .Single(path => !path.EndsWith(".symbols.nupkg", StringComparison.OrdinalIgnoreCase));

            var installResult = RunProcess(
                "dotnet",
                $"new install \"{packagePath}\" --debug:custom-hive \"{customHivePath}\"",
                workingDirectory: packageOutputPath);

            Assert.Equal(0, installResult.ExitCode);

            var generateResult = RunProcess(
                "dotnet",
                $"new cephalon-monolith -n Acme.Store -o \"{appOutputPath}\" --debug:custom-hive \"{customHivePath}\"",
                workingDirectory: packageOutputPath);

            Assert.Equal(0, generateResult.ExitCode);

            Assert.True(File.Exists(Path.Combine(appOutputPath, "Acme.Store.csproj")));
            Assert.True(File.Exists(Path.Combine(appOutputPath, "NuGet.config")));
            Assert.True(File.Exists(Path.Combine(appOutputPath, ".dockerignore")));
            Assert.True(File.Exists(Path.Combine(appOutputPath, "Dockerfile")));
            Assert.True(File.Exists(Path.Combine(appOutputPath, "compose.yaml")));
            Assert.True(File.Exists(Path.Combine(appOutputPath, "otel-collector-config.yaml")));
            Assert.True(File.Exists(Path.Combine(appOutputPath, ".cephalon", "packages", "README.md")));
            Assert.True(File.Exists(Path.Combine(appOutputPath, "deploy", "windows-service", "README.md")));
            Assert.True(File.Exists(Path.Combine(appOutputPath, "deploy", "windows-service", "install-service.ps1")));
            Assert.True(File.Exists(Path.Combine(appOutputPath, "deploy", "windows-service", "remove-service.ps1")));
            Assert.True(File.Exists(Path.Combine(appOutputPath, "deploy", "iis", "README.md")));
            Assert.True(File.Exists(Path.Combine(appOutputPath, "deploy", "iis", "install-site.ps1")));
            Assert.True(File.Exists(Path.Combine(appOutputPath, "deploy", "iis", "remove-site.ps1")));
            Assert.True(File.Exists(Path.Combine(appOutputPath, "deploy", "azure-app-service", "README.md")));
            Assert.True(File.Exists(Path.Combine(appOutputPath, "deploy", "azure-app-service", "deploy-zip.ps1")));
            Assert.True(File.Exists(Path.Combine(appOutputPath, "deploy", "container-image", "README.md")));
            Assert.True(File.Exists(Path.Combine(appOutputPath, "deploy", "container-image", "publish-image.ps1")));
            Assert.True(File.Exists(Path.Combine(appOutputPath, "deploy", "azure-container-apps", "README.md")));
            Assert.True(File.Exists(Path.Combine(appOutputPath, "deploy", "azure-container-apps", "deploy-up.ps1")));
            Assert.True(File.Exists(Path.Combine(appOutputPath, "deploy", "kubernetes", "README.md")));
            Assert.True(File.Exists(Path.Combine(appOutputPath, "deploy", "kubernetes", "apply.ps1")));
            Assert.True(File.Exists(Path.Combine(appOutputPath, "deploy", "kubernetes", "kustomization.yaml")));
            Assert.True(File.Exists(Path.Combine(appOutputPath, "deploy", "kubernetes", "namespace.yaml")));
            Assert.True(File.Exists(Path.Combine(appOutputPath, "deploy", "kubernetes", "deployment.yaml")));
            Assert.True(File.Exists(Path.Combine(appOutputPath, "deploy", "kubernetes", "service.yaml")));
            Assert.True(File.Exists(Path.Combine(appOutputPath, "deploy", "linux", "systemd", "README.md")));
            Assert.True(File.Exists(Path.Combine(appOutputPath, "deploy", "linux", "systemd", "Acme.Store.service")));
            Assert.True(File.Exists(Path.Combine(appOutputPath, "deploy", "linux", "systemd", "Acme.Store.env")));
            Assert.True(File.Exists(Path.Combine(appOutputPath, "Properties", "PublishProfiles", "CephalonFolder.pubxml")));
            Assert.True(File.Exists(Path.Combine(appOutputPath, "Program.cs")));
            Assert.True(File.Exists(Path.Combine(appOutputPath, "README.md")));
            Assert.True(File.Exists(Path.Combine(appOutputPath, "appsettings.json")));
            Assert.True(Directory.Exists(Path.Combine(appOutputPath, "Modules", "Catalog")));

            var programPath = Path.Combine(appOutputPath, "Program.cs");
            Assert.Contains("Acme.Store", File.ReadAllText(programPath));
            var appProjectPath = Path.Combine(appOutputPath, "Acme.Store.csproj");
            var appProjectContents = File.ReadAllText(appProjectPath);
            Assert.Contains("Configurations\\**\\*.json", appProjectContents, StringComparison.Ordinal);
            Assert.Contains("<CopyToPublishDirectory>PreserveNewest</CopyToPublishDirectory>", appProjectContents, StringComparison.Ordinal);
            Assert.Contains("Cephalon.Observability.OpenTelemetry", appProjectContents, StringComparison.Ordinal);
            Assert.Contains("Microsoft.Extensions.Hosting.WindowsServices", appProjectContents, StringComparison.Ordinal);
            Assert.Contains("builder.AddCephalonOpenTelemetry();", File.ReadAllText(programPath), StringComparison.Ordinal);
            Assert.Contains("WindowsServiceHelpers.IsWindowsService()", File.ReadAllText(programPath), StringComparison.Ordinal);
            Assert.Contains("builder.Host.UseWindowsService();", File.ReadAllText(programPath), StringComparison.Ordinal);
            var appSettingsPath = Path.Combine(appOutputPath, "appsettings.json");
            var appSettingsContents = File.ReadAllText(appSettingsPath);
            Assert.Contains("\"ReferenceDocs\"", appSettingsContents, StringComparison.Ordinal);
            Assert.Contains("\"Enabled\": false", appSettingsContents, StringComparison.Ordinal);
            Assert.Contains("\"DirectoryPath\": \"..\\\\..\\\\docs\\\\reference\"", appSettingsContents, StringComparison.Ordinal);
            Assert.DoesNotContain("http://localhost:4317", appSettingsContents, StringComparison.Ordinal);
            Assert.Contains("\"Protocol\": \"otlp/http\"", appSettingsContents, StringComparison.Ordinal);

            var appReadmePath = Path.Combine(appOutputPath, "README.md");
            var appReadmeContents = File.ReadAllText(appReadmePath);
            Assert.Contains("ReferenceDocs:Enabled", appReadmeContents, StringComparison.Ordinal);
            Assert.Contains("CephalonFolder.pubxml", appReadmeContents, StringComparison.Ordinal);
            Assert.Contains("dotnet publish Acme.Store.csproj -p:PublishProfile=CephalonFolder", appReadmeContents, StringComparison.Ordinal);
            Assert.Contains("./artifacts/publish/Acme.Store/", appReadmeContents, StringComparison.Ordinal);
            Assert.Contains("deploy/windows-service/README.md", appReadmeContents, StringComparison.Ordinal);
            Assert.Contains("deploy/windows-service/install-service.ps1", appReadmeContents, StringComparison.Ordinal);
            Assert.Contains("deploy/iis/README.md", appReadmeContents, StringComparison.Ordinal);
            Assert.Contains("deploy/iis/install-site.ps1", appReadmeContents, StringComparison.Ordinal);
            Assert.Contains("deploy/azure-app-service/README.md", appReadmeContents, StringComparison.Ordinal);
            Assert.Contains("deploy/azure-app-service/deploy-zip.ps1", appReadmeContents, StringComparison.Ordinal);
            Assert.Contains("deploy/container-image/README.md", appReadmeContents, StringComparison.Ordinal);
            Assert.Contains("deploy/container-image/publish-image.ps1", appReadmeContents, StringComparison.Ordinal);
            Assert.Contains("deploy/azure-container-apps/README.md", appReadmeContents, StringComparison.Ordinal);
            Assert.Contains("deploy/azure-container-apps/deploy-up.ps1", appReadmeContents, StringComparison.Ordinal);
            Assert.Contains("deploy/kubernetes/README.md", appReadmeContents, StringComparison.Ordinal);
            Assert.Contains("deploy/kubernetes/apply.ps1", appReadmeContents, StringComparison.Ordinal);
            Assert.Contains("deploy/linux/systemd/README.md", appReadmeContents, StringComparison.Ordinal);
            Assert.Contains("deploy/linux/systemd/Acme.Store.service", appReadmeContents, StringComparison.Ordinal);
            Assert.Contains("docker compose up --build", appReadmeContents, StringComparison.Ordinal);
            Assert.Contains("NuGet.config", appReadmeContents, StringComparison.Ordinal);
            Assert.Contains("./.cephalon/packages", appReadmeContents, StringComparison.Ordinal);

            var templateComposePath = Path.Combine(appOutputPath, "compose.yaml");
            var templateComposeContents = File.ReadAllText(templateComposePath);
            Assert.Contains("http://otel-collector:4318", templateComposeContents, StringComparison.Ordinal);
            Assert.Contains("dockerfile: Dockerfile", templateComposeContents, StringComparison.Ordinal);

            var generatedNuGetConfig = File.ReadAllText(Path.Combine(appOutputPath, "NuGet.config"));
            Assert.Contains("./.cephalon/packages", generatedNuGetConfig, StringComparison.Ordinal);
            Assert.Contains("packageSourceMapping", generatedNuGetConfig, StringComparison.Ordinal);

            var generatedPublishProfile = File.ReadAllText(Path.Combine(appOutputPath, "Properties", "PublishProfiles", "CephalonFolder.pubxml"));
            Assert.Contains("PublishDir", generatedPublishProfile, StringComparison.Ordinal);
            Assert.Contains("UseAppHost>false", generatedPublishProfile, StringComparison.Ordinal);
            Assert.Contains("artifacts/publish", generatedPublishProfile, StringComparison.Ordinal);

            var generatedWindowsInstallScript = File.ReadAllText(Path.Combine(appOutputPath, "deploy", "windows-service", "install-service.ps1"));
            Assert.Contains("sc.exe create", generatedWindowsInstallScript, StringComparison.Ordinal);
            Assert.Contains("--contentRoot", generatedWindowsInstallScript, StringComparison.Ordinal);
            Assert.Contains("Acme.Store.dll", generatedWindowsInstallScript, StringComparison.Ordinal);

            var generatedIisInstallScript = File.ReadAllText(Path.Combine(appOutputPath, "deploy", "iis", "install-site.ps1"));
            Assert.Contains("add apppool", generatedIisInstallScript, StringComparison.Ordinal);
            Assert.Contains("add site", generatedIisInstallScript, StringComparison.Ordinal);
            Assert.Contains("C:\\inetpub\\sites\\Acme.Store\\current", generatedIisInstallScript, StringComparison.Ordinal);

            var generatedAzureDeployScript = File.ReadAllText(Path.Combine(appOutputPath, "deploy", "azure-app-service", "deploy-zip.ps1"));
            Assert.Contains("WEBSITE_RUN_FROM_PACKAGE=1", generatedAzureDeployScript, StringComparison.Ordinal);
            Assert.Contains("az @deployArguments", generatedAzureDeployScript, StringComparison.Ordinal);
            Assert.Contains("azure-app-service.zip", generatedAzureDeployScript, StringComparison.Ordinal);

            var generatedContainerImagePublishScript = File.ReadAllText(Path.Combine(appOutputPath, "deploy", "container-image", "publish-image.ps1"));
            Assert.Contains("docker build", generatedContainerImagePublishScript, StringComparison.Ordinal);
            Assert.Contains("docker push", generatedContainerImagePublishScript, StringComparison.Ordinal);
            Assert.Contains("Container image publishing completed successfully.", generatedContainerImagePublishScript, StringComparison.Ordinal);

            var generatedKubernetesApplyScript = File.ReadAllText(Path.Combine(appOutputPath, "deploy", "kubernetes", "apply.ps1"));
            Assert.Contains("kubectl", generatedKubernetesApplyScript, StringComparison.Ordinal);
            Assert.Contains("kustomize", generatedKubernetesApplyScript, StringComparison.Ordinal);
            Assert.Contains("Kubernetes deployment apply completed successfully.", generatedKubernetesApplyScript, StringComparison.Ordinal);

            var generatedKubernetesDeployment = File.ReadAllText(Path.Combine(appOutputPath, "deploy", "kubernetes", "deployment.yaml"));
            Assert.Contains("replace-with-registry/cephalon-template-app:latest", generatedKubernetesDeployment, StringComparison.Ordinal);
            Assert.Contains("/health/ready", generatedKubernetesDeployment, StringComparison.Ordinal);
            Assert.Contains("/health/live", generatedKubernetesDeployment, StringComparison.Ordinal);

            var generatedSystemdService = File.ReadAllText(Path.Combine(appOutputPath, "deploy", "linux", "systemd", "Acme.Store.service"));
            Assert.Contains("EnvironmentFile=-/etc/cephalon/Acme.Store.env", generatedSystemdService, StringComparison.Ordinal);
            Assert.Contains("ExecStart=/usr/bin/env dotnet /opt/Acme.Store/current/Acme.Store.dll", generatedSystemdService, StringComparison.Ordinal);
            Assert.Contains("DynamicUser=true", generatedSystemdService, StringComparison.Ordinal);

            var generateModuleResult = RunProcess(
                "dotnet",
                $"new cephalon-rest-module -n OperationsKit -o \"{moduleOutputPath}\" --debug:custom-hive \"{customHivePath}\"",
                workingDirectory: packageOutputPath);

            Assert.Equal(0, generateModuleResult.ExitCode);
            Assert.True(File.Exists(Path.Combine(moduleOutputPath, "OperationsKit.csproj")));
            Assert.True(File.Exists(Path.Combine(moduleOutputPath, "Registration", "RestModuleEntry.cs")));
            Assert.True(File.Exists(Path.Combine(moduleOutputPath, "README.md")));
            Assert.True(File.Exists(Path.Combine(moduleOutputPath, "cephalon.package.json")));

            var moduleEntryPath = Path.Combine(moduleOutputPath, "Registration", "RestModuleEntry.cs");
            var moduleEntryContents = File.ReadAllText(moduleEntryPath);
            Assert.Contains("IRestModule", moduleEntryContents, StringComparison.Ordinal);
            Assert.Contains("MapRestEndpoints", moduleEntryContents, StringComparison.Ordinal);

            var moduleProjectPath = Path.Combine(moduleOutputPath, "OperationsKit.csproj");
            var moduleProjectContents = File.ReadAllText(moduleProjectPath);
            Assert.Contains("<Content Include=\"cephalon.package.json\">", moduleProjectContents, StringComparison.Ordinal);

            var packageManifestPath = Path.Combine(moduleOutputPath, "cephalon.package.json");
            var packageManifestContents = File.ReadAllText(packageManifestPath);
            Assert.Contains("\"version\": \"0.1.0-preview\"", packageManifestContents, StringComparison.Ordinal);
            Assert.Contains("\"assembly\": \"OperationsKit.dll\"", packageManifestContents, StringComparison.Ordinal);
            Assert.Contains("\"publisher\"", packageManifestContents, StringComparison.Ordinal);
            Assert.Contains("\"id\": \"operationskit\"", packageManifestContents, StringComparison.Ordinal);
            Assert.Contains("\"displayName\": \"OperationsKit\"", packageManifestContents, StringComparison.Ordinal);
            Assert.Contains("\"minimumEngineVersion\": \"0.1.0-preview\"", packageManifestContents, StringComparison.Ordinal);
            Assert.Contains("\"supportedTargetFrameworks\": [ \"net10.0\" ]", packageManifestContents, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(moduleOutputPath))
            {
                Directory.Delete(moduleOutputPath, recursive: true);
            }

            if (Directory.Exists(appOutputPath))
            {
                Directory.Delete(appOutputPath, recursive: true);
            }

            if (Directory.Exists(customHivePath))
            {
                Directory.Delete(customHivePath, recursive: true);
            }

            if (Directory.Exists(packageOutputPath))
            {
                Directory.Delete(packageOutputPath, recursive: true);
            }
        }
    }

    private static string FindRepositoryDirectory(string relativePath)
    {
        return RepositoryPaths.GetDirectory(relativePath);
    }

    private static string FindRepositoryFile(string relativePath)
    {
        return RepositoryPaths.GetFile(relativePath);
    }

    private static ProcessResult RunProcess(string fileName, string arguments, string workingDirectory)
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = System.Diagnostics.Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Could not start '{fileName}'.");

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return new ProcessResult(process.ExitCode, output, error);
    }

    private sealed record ProcessResult(int ExitCode, string Output, string Error);
}
