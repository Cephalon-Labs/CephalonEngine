using Cephalon.Abstractions.AppModel;
using Cephalon.Engine.AppModel;

namespace Cephalon.Tests.Composition;

public sealed class DatabaseTopologyRoleResolverTests
{
    [Fact]
    public void ResolveReturnsDirectTargetWhenRoleDoesNotUseRoleReference()
    {
        var databases = new DatabaseTopologySelection(
            write: new DatabaseTargetSelection(
                provider: "PostgreSql",
                connectionStringName: "WriteDb",
                runtime: new DatabaseRuntimeSelection(enableRetryOnFailure: false)));

        var resolution = DatabaseTopologyRoleResolver.Resolve(databases, "write");

        Assert.Equal("write", resolution.RequestedRoleId);
        Assert.Equal("write", resolution.ResolvedRoleId);
        Assert.Equal("direct", resolution.ResolutionMode);
        Assert.False(resolution.UsesRoleReference);
        Assert.Equal("PostgreSql", resolution.EffectiveTarget.Provider);
        Assert.Equal("WriteDb", resolution.EffectiveTarget.ConnectionStringName);
        Assert.False(resolution.EffectiveTarget.Runtime.EnableRetryOnFailure);
    }

    [Fact]
    public void ResolveMergesDependentTargetOverridesWhenUseRoleReferencesWrite()
    {
        var databases = new DatabaseTopologySelection(
            write: new DatabaseTargetSelection(
                provider: "PostgreSql",
                connectionStringName: "WriteDb",
                schema: "app",
                runtime: new DatabaseRuntimeSelection(
                    enableRetryOnFailure: false,
                    commandTimeoutSeconds: 30)),
            history: new DatabaseTargetSelection(
                useRole: "write",
                schema: "audit",
                runtime: new DatabaseRuntimeSelection(commandTimeoutSeconds: 120)));

        var resolution = DatabaseTopologyRoleResolver.Resolve(databases, "history");

        Assert.Equal("history", resolution.RequestedRoleId);
        Assert.Equal("write", resolution.ResolvedRoleId);
        Assert.Equal("write", resolution.UseRole);
        Assert.True(resolution.UsesRoleReference);
        Assert.Equal("role-reference", resolution.ResolutionMode);
        Assert.Equal("PostgreSql", resolution.EffectiveTarget.Provider);
        Assert.Equal("WriteDb", resolution.EffectiveTarget.ConnectionStringName);
        Assert.Equal("audit", resolution.EffectiveTarget.Schema);
        Assert.False(resolution.EffectiveTarget.Runtime.EnableRetryOnFailure);
        Assert.Equal(120, resolution.EffectiveTarget.Runtime.CommandTimeoutSeconds);
    }
}
