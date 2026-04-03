# Gateway And Control-Plane Guidance

This sample keeps gateway and control-plane concerns additive to the shipped `MicroserviceSuite` contract.

What that means:

- the suite contract still describes shared projects, shared folders, and repeatable service slots
- each service still boots as a normal Cephalon `Microservice`
- gateway or control-plane hosts are optional adoption layers, not required parts of the suite blueprint

Recommended layering for this sample:

1. keep service logic, modules, and capabilities inside the service hosts
2. keep shared contracts and conventions in `shared/Cephalon.Sample.MicroserviceSuite.Shared.Foundation`
3. keep suite-wide governance and route guidance in `shared/Cephalon.Sample.MicroserviceSuite.Governance`
4. if a team later needs one gateway host, let it aggregate or proxy service routes without becoming a second source of business logic
5. if a team later needs one control-plane host, let it compose existing `/engine/*` runtime answers such as `/engine/snapshot`, `/engine/runtime-story`, `/engine/diagnostics`, and `/engine/packages` instead of inventing a new engine-owned coordinator contract

Suggested additive routes:

- suite gateway root: `/suite/commerce`
- catalog gateway route: `/suite/commerce/catalog`
- orders gateway route: `/suite/commerce/orders`
- suite control-plane path: `/suite/control-plane/runtime`

Guardrails:

- do not move module registration, capability ownership, or workflow logic into the gateway host
- do not add a new engine abstraction just to represent a gateway or control plane
- do not make gateway or control-plane processes required for the suite to boot locally
- prefer the existing runtime introspection surfaces for operator answers before creating new control-plane-specific endpoints
