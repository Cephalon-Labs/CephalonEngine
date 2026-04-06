using Cephalon.AspNetCore.Documentation;
using Cephalon.AspNetCore.Hosting;
using Cephalon.Engine.Runtime;
using Cephalon.Tests.Support;
using Cephalon.Worker.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cephalon.Tests.Hosting;

public sealed class ProjectConfigurationConventionTests
{
    [Fact]
    public void AddCephalonLoadsSplitProjectConfigurationIntoAspNetCoreHost()
    {
        var rootPath = CreateProjectRoot();

        try
        {
            WriteEnvironmentConfiguration(
                rootPath,
                "Engine",
                Environments.Development,
                """
                {
                  "Engine": {
                    "Blueprint": "ModularVerticalSlice",
                    "Transports": [ "RestApi" ]
                  }
                }
                """);
            WriteEnvironmentConfiguration(
                rootPath,
                "OpenApi",
                Environments.Development,
                """
                {
                  "OpenApi": {
                    "Title": "Convention Driven OpenAPI"
                  }
                }
                """);
            WriteRootConfiguration(
                rootPath,
                "AddReferenceDocs.json",
                """
                {
                  "ReferenceDocs": {
                    "Enabled": true,
                    "RoutePrefix": "/reference",
                    "DirectoryPath": "docs\\reference",
                    "DefaultDocument": "browse.html"
                  }
                }
                """);

            var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions
            {
                ContentRootPath = rootPath,
                EnvironmentName = Environments.Development
            });

            builder.AddCephalon(engine =>
            {
                engine.AddModule(new PlatformTestModule());
                engine.AddModule(new DiscoveryTestModule());
            });

            Assert.Equal("Convention Driven OpenAPI", builder.Configuration["OpenApi:Title"]);

            using var app = builder.Build();
            var runtime = app.Services.GetRequiredService<IRuntime>();
            var referenceDocs = app.Services.GetRequiredService<ReferenceDocsHostingOptions>();

            Assert.Equal("modular-vertical-slice", runtime.Manifest.AppProfile.BlueprintId);
            Assert.True(referenceDocs.Enabled);
            Assert.Equal("/reference", referenceDocs.RoutePrefix);
            Assert.Equal("browse.html", referenceDocs.DefaultDocument);
            Assert.Equal(
                Path.GetFullPath(Path.Combine(rootPath, "docs", "reference")),
                referenceDocs.DirectoryPath);
        }
        finally
        {
            DeleteDirectory(rootPath);
        }
    }

    [Fact]
    public async Task AddCephalonLoadsSplitProjectConfigurationIntoWorkerHost()
    {
        var rootPath = CreateProjectRoot();

        try
        {
            var testAssemblyName = typeof(PlatformTestModule).Assembly.GetName().Name
                ?? throw new InvalidOperationException("Test assembly name was not available.");

            WriteEnvironmentConfiguration(
                rootPath,
                "Engine",
                Environments.Development,
                $$"""
                {
                  "Engine": {
                    "Blueprint": "ModularMonolith",
                    "Discovery": {
                      "Assemblies": [ "{{testAssemblyName}}" ]
                    },
                    "Options": {
                      "Modules": {
                        "lifecycle-platform": { "Enabled": false },
                        "lifecycle-discovery": { "Enabled": false },
                        "failure-platform": { "Enabled": false },
                        "flaky-start": { "Enabled": false },
                        "failing-stop": { "Enabled": false },
                        "stop-observer": { "Enabled": false },
                        "slow-stop": { "Enabled": false },
                        "phase8-runtime-catalogs": { "Enabled": false },
                        "invalid-phase8-projection": { "Enabled": false },
                        "invalid-phase8-outbox": { "Enabled": false },
                        "invalid-phase8-inbox": { "Enabled": false },
                        "invalid-phase8-audit-store": { "Enabled": false },
                        "entity-framework-single-context-tests": { "Enabled": false },
                        "entity-framework-split-context-tests": { "Enabled": false },
                        "entity-framework-outbox-tests": { "Enabled": false },
                        "entity-framework-sfid-tests": { "Enabled": false }
                      }
                    }
                  }
                }
                """);
            WriteRootConfiguration(
                rootPath,
                "AddWorker.json",
                """
                {
                  "Worker": {
                    "HeartbeatSeconds": 7
                  }
                }
                """);

            var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
            {
                ContentRootPath = rootPath,
                EnvironmentName = Environments.Development
            });

            builder.AddCephalon();

            Assert.Equal("7", builder.Configuration["Worker:HeartbeatSeconds"]);

            using var host = builder.Build();
            var runtime = host.Services.GetRequiredService<IRuntime>();

            await host.StartAsync();

            Assert.Equal("modular-monolith", runtime.Manifest.AppProfile.BlueprintId);
            Assert.Contains(runtime.Manifest.Modules, module => module.Id == "platform");
            Assert.Contains(runtime.Manifest.Modules, module => module.Id == "discovery");

            await host.StopAsync();
        }
        finally
        {
            DeleteDirectory(rootPath);
        }
    }

    private static string CreateProjectRoot()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), $"cephalon-project-config-{Guid.NewGuid():N}");
        Directory.CreateDirectory(rootPath);
        return rootPath;
    }

    private static void WriteEnvironmentConfiguration(
        string rootPath,
        string groupName,
        string environmentName,
        string contents)
    {
        var directoryPath = Path.Combine(rootPath, "Configurations", groupName);
        Directory.CreateDirectory(directoryPath);
        File.WriteAllText(Path.Combine(directoryPath, $"{environmentName}.json"), contents);
    }

    private static void WriteRootConfiguration(
        string rootPath,
        string fileName,
        string contents)
    {
        var directoryPath = Path.Combine(rootPath, "Configurations");
        Directory.CreateDirectory(directoryPath);
        File.WriteAllText(Path.Combine(directoryPath, fileName), contents);
    }

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }
}
