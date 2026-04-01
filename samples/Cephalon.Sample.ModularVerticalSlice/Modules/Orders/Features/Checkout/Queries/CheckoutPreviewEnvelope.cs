namespace Cephalon.Sample.ModularVerticalSlice.Modules.Orders.Features.Checkout.Queries;

/// <summary>
/// Represents the checkout preview returned by the modular vertical-slice sample.
/// </summary>
/// <param name="Architecture">
/// The application blueprint represented by the sample.
/// </param>
/// <param name="CustomerId">
/// The customer identifier used to build the preview.
/// </param>
/// <param name="Expedited">
/// Indicates whether the sample classified the request as expedited.
/// </param>
/// <param name="Policy">
/// The policy label selected for the preview.
/// </param>
public sealed record CheckoutPreviewEnvelope(
    string Architecture,
    string CustomerId,
    bool Expedited,
    string Policy);
