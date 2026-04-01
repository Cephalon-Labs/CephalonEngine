namespace Cephalon.Retrieval.Services;

/// <summary>
/// Collects knowledge collection descriptors contributed to the active retrieval runtime pack.
/// </summary>
public interface IKnowledgeCollectionRegistry
{
    /// <summary>
    /// Adds a knowledge collection descriptor to the registry.
    /// </summary>
    /// <param name="collection">The collection descriptor to contribute.</param>
    void Add(KnowledgeCollectionDescriptor collection);
}
