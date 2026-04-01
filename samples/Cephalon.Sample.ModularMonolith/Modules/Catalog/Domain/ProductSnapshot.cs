namespace Cephalon.Sample.ModularMonolith.Modules.Catalog.Domain;

/// <summary>
/// Represents a product listed by the modular monolith catalog sample.
/// </summary>
/// <param name="Sku">
/// The product stock keeping unit.
/// </param>
/// <param name="Name">
/// The product display name.
/// </param>
/// <param name="Posture">
/// The architecture or capability posture highlighted by the sample.
/// </param>
public sealed record ProductSnapshot(string Sku, string Name, string Posture);
