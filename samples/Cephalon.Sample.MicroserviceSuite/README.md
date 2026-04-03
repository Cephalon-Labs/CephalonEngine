# Cephalon Microservice Suite Sample

This sample shows the shipped `MicroserviceSuite` blueprint as a coordinated pair of Cephalon microservices that reuse one shared foundation project.

Layout:

- `shared/Cephalon.Sample.MicroserviceSuite.Shared.Foundation`: shared suite contracts and conventions
- `services/CatalogService`: catalog-focused Cephalon microservice
- `services/OrdersService`: order-coordination Cephalon microservice

Why it exists:

- demonstrate how multiple Cephalon services can stay on the existing `Microservice` host wiring while still belonging to one intentional suite
- keep shared suite conventions in a dedicated shared foundation project instead of hiding them in one host
- stop short of governance packages, gateway hosts, or control-plane guidance so the sample stays within the `#81` scope

Useful endpoints:

- catalog service: `/api/catalog/overview`
- orders service: `/api/orders/coordination/{orderId?}`

This sample intentionally leaves governance packages and optional gateway/control-plane guidance for the later `#82` follow-through.
