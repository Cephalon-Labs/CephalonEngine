namespace CephalonTemplateApp.Modules.Orders.Features.Checkout.Queries;

public sealed record CheckoutPreviewEnvelope(
    string Architecture,
    string CustomerId,
    bool Expedited,
    string Policy);
