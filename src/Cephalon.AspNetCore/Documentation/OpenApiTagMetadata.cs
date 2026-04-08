namespace Cephalon.AspNetCore.Documentation;

/// <summary>
/// Describes OpenAPI tag metadata projected from ASP.NET Core route groups or endpoints.
/// </summary>
/// <param name="Name">The public tag name shown in OpenAPI and Scalar.</param>
/// <param name="Description">The optional tag description shown in OpenAPI and Scalar.</param>
public sealed record OpenApiTagMetadata(string Name, string? Description);
