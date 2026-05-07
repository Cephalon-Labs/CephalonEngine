# Cephalon.Observability.Aws

> **Maturity:** `M1` · **Ownership:** `cephalon-managed` (family-covered by maturity audit) — authoritative truth in [`engine-surface-maturity-audit.md`](../engine-surface-maturity-audit.md)

`Cephalon.Observability.Aws` adds AWS-hosted OTLP defaults for Cephalon hosts.

## What it owns

- host-builder registration for OTLP logs, metrics, and traces over the shared `Engine:Observability:Telemetry` contract
- AWS X-Ray-compatible trace IDs and optional AWS X-Ray text-map propagation
- AWS SDK client tracing plus optional Lambda context configuration
- explicit hosted AWS resource defaults and AWS resource detectors for EC2, ECS, EKS, and Elastic Beanstalk
- startup diagnostics that summarize the active AWS export mode without logging secrets
- a sanitized `telemetry-export-aws` runtime surface for `/engine/technology-surfaces` and `/engine/snapshot`

## Main surfaces

- `Configuration/AwsTelemetryExportOptions.cs`
- `Hosting/AwsHostApplicationBuilderExtensions.cs`
- `Hosting/AwsSummaryHostedService.cs`
- `Hosting/AwsTelemetryRuntimeContributor.cs`

## Source structure

- `Configuration`
- `Hosting`

## How it fits

This package stays outside `Cephalon.Engine` and `Cephalon.Observability` on purpose. The engine still owns diagnostics names and lifecycle signals, `Cephalon.Observability` still owns the shared telemetry contract, and this companion package layers AWS-specific propagation, resource detection, and AWS SDK instrumentation on top of the same OTLP baseline.

The AWS slice is intentionally narrower than a generic "all AWS observability products" abstraction. It keeps the shared OTLP contract intact, adds X-Ray-compatible tracing defaults plus hosted AWS resource defaults, and leaves any collector-, account-, or gateway-specific auth path explicit in host configuration. If a deployment sends OTLP data to AWS-managed endpoints directly, prefer an ADOT collector or another SigV4-capable gateway in front of that endpoint instead of pushing AWS auth rules into `Cephalon.Engine`.

When the package is activated, it contributes the `telemetry-export-aws` runtime surface so operators can see the AWS observability pack is active without exposing collector endpoints, headers, or tokens.

## Related docs

- [Cephalon.Observability](observability.md)
- [Cephalon.Observability.OpenTelemetry](observability-opentelemetry.md)
- [Operations](../operations.md)
- [Architecture](../architecture.md)
