# Framework research baseline — September 2026

Verified on **September 16, 2026** for ENG-718. This is a bounded review of primary sources relevant to Cephalon, not a claim to have learned everything on the internet. The decision column is Cephalon's interpretation; cited sources do not certify Cephalon or prove its implementation. Mutable pages must be checked again before implementation or a release decision.

## Sources, decisions and tracked follow-through

| ID | Primary source checked | What it contributes | Cephalon decision / work |
| --- | --- | --- | --- |
| R01 | [.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core) | September 8 table lists .NET 10.0.12, LTS through November 14, 2028; .NET 11 RC1 is a go-live release with a bounded support window. | Keep net10.0; evaluate servicing separately from the current repository SDK 10.0.303. ENG-731. |
| R02 | [.NET 11 downloads](https://dotnet.microsoft.com/en-us/download/dotnet/11.0) | RC1 and SDK 11.0.100-rc.1 are published September 8. | Supersede July Preview 5 as current external truth; RC1 remains Cephalon assessment-only. ENG-731. |
| R03 | [.NET library compatibility rules](https://learn.microsoft.com/en-us/dotnet/core/compatibility/library-change-rules) | Compatibility includes more than compiling new source. | Test binary, source, serialization, configuration and behavior changes against consumers. ENG-731. |
| R04 | [Native AOT](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/) | AOT constrains dynamic loading and runtime code generation. | Keep dynamic plugin and generated/static deployment modes explicit; require package/RID proof. ENG-731. |
| R05 | [AssemblyLoadContext](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.loader.assemblyloadcontext?view=net-10.0) | Assembly loading/isolation is a runtime composition mechanism. | Treat package loading as trusted code execution; use process isolation for untrusted code, not a load-context security claim. ENG-727. |
| R06 | [Kubernetes controllers](https://kubernetes.io/docs/concepts/architecture/controller/) | Controllers reconcile observed state with desired state. | M3 needs a real bounded control loop and effect evidence; a desired/observed snapshot alone is M1. ENG-719–726. |
| R07 | [NIST SSDF 1.1](https://csrc.nist.gov/pubs/sp/800/218/final) | Secure development practices cover organization, software protection, production and vulnerability response. | Map applicable practices to release evidence, ownership and response, without a certification claim. ENG-727/728/737. |
| R08 | [OWASP ASVS](https://github.com/OWASP/ASVS) | Versioned, verifiable application security requirements. | Pin the applicable revision and controls during security implementation; test tenant/operator/identity boundaries. ENG-727. |
| R09 | [OAuth 2.0 security BCP, RFC 9700](https://www.rfc-editor.org/info/rfc9700/) | Current OAuth security guidance for authorization flows and tokens. | Review host/provider integrations for token audience, redirect and replay boundaries; do not invent a core identity protocol. ENG-727/735. |
| R10 | [SLSA 1.2](https://slsa.dev/spec/v1.2/) | Supply-chain assurance separates requirements and evidence. | Map build provenance and verification requirements; declare a level only when independently verified. ENG-728. |
| R11 | [GitHub artifact attestations](https://docs.github.com/en/actions/concepts/security/artifact-attestations) | Attestations tie artifacts to source/build identity; they do not guarantee secure code. | Verify consumer-side identity, digest and policy; tampered artifacts must fail. ENG-728. |
| R12 | [CycloneDX SBOM](https://cyclonedx.org/capabilities/sbom/) | Inventory supports component/dependency transparency. | Version artifact-linked SBOM and license/vulnerability evidence, with response ownership. ENG-728. |
| R13 | [OpenTelemetry semantic conventions](https://opentelemetry.io/docs/specs/semconv/) and [stability](https://opentelemetry.io/docs/specs/otel/versioning-and-stability/) | Telemetry naming and stability affect dashboards and consumers. | Pin adopted stable conventions, migrate deliberately, keep Cephalon.Engine diagnostic names stable and bound cardinality. ENG-729. |
| R14 | [Google SRE: implementing SLOs](https://sre.google/workbook/implementing-slos/) | User-facing indicators, objectives and error budgets guide reliability decisions. | Declare workload-specific SLOs and measure recovery, tail latency and error budgets. ENG-729/730. |
| R15 | [WCAG 2.2](https://www.w3.org/TR/WCAG22/) | Testable web accessibility criteria. | Apply relevant keyboard, focus, contrast and readable-error checks to generated documentation UI. ENG-732. |
| R16 | [CloudEvents](https://cloudevents.io/) | A shared event envelope aids interoperability. | Evaluate envelope/version/schema compatibility in companion integrations; it does not promise delivery or exactly-once effects. ENG-724/733/736. |
| R17 | [Aspire overview](https://aspire.dev/get-started/what-is-aspire/) | App composition, orchestration and observability complement application frameworks. | Compare a thin optional integration against duplicating orchestration in core. ENG-736. |
| R18 | [Dapr concepts](https://docs.dapr.io/concepts/) | Distributed application building blocks offer integration alternatives. | Prototype only an adopter-backed seam and record sidecar/operational costs. ENG-736. |
| R19 | [Wolverine](https://wolverinefx.net/) | Message handling and durable execution are an existing ecosystem alternative. | Preserve the existing Wolverine companion; compare identical workloads before extending shared ownership. ENG-724/736. |
| R20 | [ABP application modules](https://abp.io/docs/latest/modules) | Explicit modules and lifecycle composition provide a comparison point. | Evaluate module-authoring ergonomics; preserve Cephalon's engine identity and independent host adapters. ENG-719/732. |

## Research boundaries and refresh

- Some direct SLSA, OWASP and GitHub deep links failed; reachable official specification/repository pages above were used instead. MCP security pages were not retrievable in this pass. ENG-735 must resolve and pin the applicable protocol revision before implementing an MCP adapter; no MCP version or compliance claim was inferred.
- This pass inspected package/maturity declarations, shared coordination source anchors, SDK/build configuration, validation scripts, GitHub issue/project state and primary standards. It did not rerun every runtime/provider suite or conduct a repository-wide security audit.
- Framework servicing/security sources: monthly and before releases. Protocol/provider support: before integration and quarterly. Architecture/product assumptions: quarterly. Adoption and operator evidence: after any material contract/provider change and before promotion.
- Every future source change becomes an adopt/defer/reject decision with a reason, ENG issue and bounded experiment. Reading a source never advances M0–M4 by itself.

See [completion plan](framework-completion-plan.md), [September review](architecture-review-2026-09.md) and [backlog](engine-backlog.md).

## SDK reproducibility follow-up

On September 16 the official [.NET 10 download catalog](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) lists SDK 10.0.401 with runtime 10.0.12. Microsoft's [global.json guidance](https://learn.microsoft.com/en-us/dotnet/core/tools/global-json) distinguishes SDK selection from target frameworks and recommends exact matching for locked dependency graphs. ENG-739 therefore pins the servicing SDK and refreshes only affected ILLink locks; net10.0 and the separate .NET 11 assessment lane remain unchanged. [Repair evidence](compatibility-repair-2026-09.md) records validation rather than deriving support from upstream availability.

## Executable contract compatibility

Microsoft's [breaking-change guidance](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/breaking-changes) separates source, binary and behavioral compatibility; [package validation](https://learn.microsoft.com/en-us/dotnet/fundamentals/apicompat/package-validation/overview) supports released-package baselines. ENG-743 adds actual unchanged-binary consumers and a bounded rollback negative control for selected Abstractions contracts. Its baseline is an immutable repository checkpoint, not a claimed stable release. The [guide](contract-compatibility.md) lists exclusions and preserves full-surface API validation as separate release work.
