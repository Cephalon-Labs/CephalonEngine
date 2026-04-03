# Cephalon Microservice Suite Sample

This sample shows the shipped `MicroserviceSuite` blueprint as a coordinated pair of Cephalon microservices that reuse one shared foundation project plus one shared governance package.

Layout:

- `shared/Cephalon.Sample.MicroserviceSuite.Shared.Foundation`: shared suite contracts and conventions
- `shared/Cephalon.Sample.MicroserviceSuite.Governance`: shared governance profile plus additive gateway/control-plane guidance
- `services/CatalogService`: catalog-focused Cephalon microservice
- `services/OrdersService`: order-coordination Cephalon microservice

Why it exists:

- demonstrate how multiple Cephalon services can stay on the existing `Microservice` host wiring while still belonging to one intentional suite
- keep shared suite conventions in a dedicated shared foundation project instead of hiding them in one host
- keep shared governance conventions and optional gateway/control-plane guidance in a dedicated package instead of stretching the suite contract or engine core
- show that optional gateway or control-plane layering stays additive and can consume the existing `/engine/*` runtime surfaces rather than replacing them

Useful endpoints:

- catalog service: `/api/catalog/overview`
- catalog governance: `/api/catalog/governance`
- orders service: `/api/orders/coordination/{orderId?}`
- orders governance: `/api/orders/governance`

Guidance:

- see [gateway-and-control-plane-guidance.md](gateway-and-control-plane-guidance.md) for the sample's recommended additive gateway and control-plane layering
