using System.Reflection;
using Cephalon.Tests.Support;

namespace Cephalon.Tests.Tooling;

public sealed class TestHarnessSurfaceTests
{
    [Fact]
    public void TestAssemblyExportsOnlyXunitTestClasses()
    {
        var exportedTypes = typeof(TestHarnessSurfaceTests).Assembly.GetExportedTypes();
        var allowedFrameworkVisibleTypes = new HashSet<string>(StringComparer.Ordinal)
        {
            typeof(global::Cephalon.Tests.Support.GreetingEnvelope).FullName!,
            typeof(global::Cephalon.Tests.Hosting.IdentityAspNetCoreDocumentsController).FullName!
        };

        Assert.NotEmpty(exportedTypes);
        Assert.All(exportedTypes, type =>
        {
            if (allowedFrameworkVisibleTypes.Contains(type.FullName!))
            {
                return;
            }

            Assert.DoesNotContain(".Support", type.Namespace ?? string.Empty, StringComparison.Ordinal);
            Assert.EndsWith("Tests", type.Name, StringComparison.Ordinal);
            Assert.Contains(
                type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                    .Where(method => method.DeclaringType == type),
                method => method.IsDefined(typeof(FactAttribute), inherit: true) || method.IsDefined(typeof(TheoryAttribute), inherit: true));
        });
    }

    [Fact]
    public void TestProjectKeepsXmlDocumentationGenerationDisabled()
    {
        var projectFile = RepositoryPaths.GetFile("tests", "Cephalon.Tests", "Cephalon.Tests.csproj");
        var projectContents = File.ReadAllText(projectFile);

        Assert.Contains(
            "<GenerateDocumentationFile>false</GenerateDocumentationFile>",
            projectContents,
            StringComparison.Ordinal);
    }
}
