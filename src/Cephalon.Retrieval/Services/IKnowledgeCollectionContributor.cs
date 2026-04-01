namespace Cephalon.Retrieval.Services;

/// <summary>
/// Allows a module to contribute knowledge collections into the active retrieval runtime pack.
/// </summary>
public interface IKnowledgeCollectionContributor
{
    /// <summary>
    /// Registers one or more knowledge collection descriptors with the supplied registry.
    /// </summary>
    /// <param name="collections">The registry that collects contributed collection descriptors.</param>
    void RegisterCollections(IKnowledgeCollectionRegistry collections);
}
