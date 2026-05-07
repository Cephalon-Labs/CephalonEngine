namespace Cephalon.Tests.Hosting;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class SerilogHostingCollectionDefinition
{
    public const string Name = "Serilog hosting";
}
