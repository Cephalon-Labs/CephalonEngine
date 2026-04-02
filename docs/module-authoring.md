# Cephalon Module Authoring

Cephalon now ships a first-class module authoring path alongside app scaffolding.

## Starting points

Choose the starter that matches the package you want to author:

- `dotnet new cephalon-module`
  - host-agnostic module package
  - capability registration
  - lifecycle hooks
  - package-owned localization resources
  - generated `cephalon.package.json` copied to the output folder
- `dotnet new cephalon-rest-module`
  - everything in `cephalon-module`
  - plus `IRestModule` and a localized REST endpoint
  - generated `cephalon.package.json` copied to the output folder

For a concrete reference implementation, use:

- `samples/Cephalon.ReferenceModule.Operations`

## Recommended package shape

Keep module packages small and explicit:

- `Application/`
  - package-owned services and state
- `Contracts/`
  - payloads or envelopes exposed by the package
- `Registration/`
  - the module entry point and transport contribution interfaces
- `cephalon.package.json`
  - package manifest used by `Engine:Discovery:Packages:ManifestPath` and `Engine:Discovery:PackageDirectories`
  - version, compatibility, and optional integrity metadata for independently shipped modules

That keeps the authoring path close to the same module-first ideas used by Cephalon apps.

## Authoring checklist

1. Define a `ModuleDescriptor` with a stable id, display name, description, tags, and version.
2. Register package-owned services in `ConfigureServices(...)`.
3. Register explicit capabilities in `RegisterCapabilities(...)`.
4. Implement lifecycle hooks only when the package owns startup/runtime behavior.
5. Use `ILocalizedResourceContributor` for package-owned text instead of hardcoding strings in hosts.
6. Use `ITechnologyContributor` when the package introduces a future-tech profile, workload convention, or package hint that the host should be able to select through `Engine:Technologies`.
7. Use `ITechnologyServiceContributor` or `ITechnologyCapabilityContributor` when package services or capabilities should only activate for specific technology profiles.
8. If the package extends a shipped technology pack, register the pack-specific contributor service in `ConfigureServices(...)` such as `IAgentToolContributor`, `IKnowledgeCollectionContributor`, `IEventChannelContributor`, or `IEdgeNodeContributor`.
9. Use `ITechnologyRuntimeContributor` when the package or pack needs to expose an operator-facing runtime snapshot through `/engine/technology-surfaces`.
10. Add transport contribution interfaces only when the package really owns an external surface.

## Package manifest contract

`cephalon.package.json` is now the recommended place to describe both where the package assembly lives and what runtime it expects.

Baseline example:

```json
{
  "id": "operations",
  "version": "1.0.0",
  "assembly": "Cephalon.ReferenceModule.Operations.dll",
  "publisher": {
    "id": "cephalon-labs",
    "displayName": "Cephalon Labs",
    "website": "https://example.invalid/cephalon-labs"
  },
  "signature": {
    "type": "detached-signature",
    "signer": "Cephalon Labs Build",
    "keyId": "cephalon-labs-build",
    "fingerprint": "sha256:cephalon-labs-reference-operations",
    "algorithm": "RSA-SHA256",
    "value": "<base64-detached-signature>"
  },
  "compatibility": {
    "minimumEngineVersion": "1.0.0",
    "maximumEngineVersion": "2.0.0",
    "supportedTargetFrameworks": ["net10.0"]
  },
  "integrity": {
    "sha256": "sha256:3e5d5b9fd0dfb7c60e441d013d7d2a60f41c7b0a0a4fb2d2ad5a9f88d6e7c123"
  }
}
```

For a single signer, use the legacy `signature` object shown above. When a package needs more than one signer or attestation, it can declare `signatures` instead. The engine supports both shapes and exposes per-signer verification results through runtime introspection.

Current behavior:

- `id` should stay stable across releases so trust policy and operators have a predictable handle
- `version` is surfaced through `/engine/packages` and should match the distributed package version
- `compatibility.minimumEngineVersion` blocks older engine versions from loading the package
- `compatibility.maximumEngineVersion` is optional but useful when a package intentionally caps support
- `compatibility.supportedTargetFrameworks` blocks mismatched runtime target frameworks
- `publisher.id` should stay stable across releases if operators or trust policy use publisher-level allow-lists
- `signature.keyId` or `signatures[].keyId` should stay stable across releases if hosts map trusted public keys by signing identity
- `signature.fingerprint` or `signatures[].fingerprint` identifies the signing key and is surfaced through diagnostics and trust snapshots
- `signature.value` or `signatures[].value` is optional until a host requires cryptographic verification, but when present it should be a detached signature over the resolved assembly SHA-256 hash
- `integrity.sha256` is optional, but when present the resolved assembly must match it exactly

For scaffolded modules and `dotnet new` starters, Cephalon now emits this manifest automatically with the current package version and target framework baseline.

## Compatibility checklist

Keep authored packages aligned with the broader Cephalon compatibility contract:

- `version` and `compatibility.minimumEngineVersion` should reflect the Cephalon package version you intend to support
- `compatibility.supportedTargetFrameworks` should reflect the actual target framework of the compiled module assembly
- if you intentionally cap support, set `compatibility.maximumEngineVersion` explicitly instead of relying on undocumented assumptions
- when you change package version or target framework expectations, update the project file, `cephalon.package.json`, packaging docs, and any starter/template copies together
- use `docs/compatibility.md` as the repository-wide matrix for what else must stay aligned across scaffolding, CLI, templates, and docs

## Loading a package

Package-path discovery:

```json
{
  "Engine": {
    "Discovery": {
      "Packages": [
        {
          "Id": "operations",
          "Path": "plugins/Cephalon.ReferenceModule.Operations.dll"
        }
      ]
    },
    "Transports": [ "RestApi" ]
  }
}
```

Package-manifest discovery:

```json
{
  "Engine": {
    "Discovery": {
      "Packages": [
        {
          "ManifestPath": "plugins/reference-operations/cephalon.package.json"
        }
      ]
    },
    "Transports": [ "RestApi" ]
  }
}
```

Package-directory discovery:

```json
{
  "Engine": {
    "Discovery": {
      "PackageDirectories": [
        {
          "Path": "plugins",
          "IncludeSubdirectories": true
        }
      ]
    },
    "Transports": [ "RestApi" ]
  }
}
```

Assembly-name discovery is still available when the module assembly is already part of the app load context:

```json
{
  "Engine": {
    "Discovery": {
      "Assemblies": [ "Cephalon.ReferenceModule.Operations" ]
    },
    "Transports": [ "RestApi" ]
  }
}
```

Code-driven package path:

```csharp
builder.AddCephalon(engine =>
{
    engine.AddPackageAssembly("plugins/Cephalon.ReferenceModule.Operations.dll", id: "operations");
    engine.AddPackageManifest("plugins/reference-operations/cephalon.package.json");
    engine.AddPackageDirectory("plugins");
});
```

Manual module registration path:

```csharp
builder.AddCephalon(engine =>
{
    engine.AddModule(new OperationsModule());
});
```

`/engine/packages` exposes the package-loading snapshot the runtime resolved, including the package `kind`, resolved assembly `path`, original `sourcePath`, declared `version`, compatibility fields, publisher/signature provenance metadata, the top-level signature summary fields kept for backward compatibility, the per-signer `signatures` collection, cryptographic verification status, computed `checksumSha256`, and the current `trustReason`. `/engine/modules` continues to show the active module set after policy and ordering have been applied. `/engine/technology-catalog` shows the technology profiles available after built-in, package, and project contributions have been merged. `/engine/technology-surfaces` shows the active runtime surfaces exposed by installed technology packs after host options and module contributors have both been applied, while `/engine/technology-surfaces/{technologyId}` narrows that view to a single selected technology profile. In code, the same merged surface set is available through `ITechnologyRuntimeCatalog`, and the broader operator-facing runtime snapshot is available through `IRuntimeIntrospectionSnapshotProvider` or `GET /engine/snapshot`.

## What the reference package demonstrates

`Cephalon.ReferenceModule.Operations` shows:

- package-owned service registration
- capability registration visible in `/engine/capabilities`
- lifecycle-driven state transitions
- package localization merged into the runtime catalog
- package-contributed technology profiles through `ITechnologyContributor`
- technology-aware service and capability activation through `ITechnologyServiceContributor` and `ITechnologyCapabilityContributor`
- REST endpoint contribution through `IRestModule`
- a distributable `cephalon.package.json` manifest copied to the package output

That makes it the baseline package to copy when authoring a new module package for Cephalon. The scaffold generator and `dotnet new` starters now emit the same manifest convention automatically.

## Trust and capability policy

Module packages should assume capability access may be governed by `Engine:Trust`.

Current baseline:

- package-loaded assemblies may be required to come from manifest-driven discovery through `Engine:PackagePolicy`
- package manifests may be required to declare `version`, compatibility fields, publisher ids, signer fingerprints, signature key ids, signature values, cryptographic signature verification, or `integrity.sha256` through `Engine:PackagePolicy`
- package-loaded assemblies can be blocked unless the package is trusted
- package trust can come from package id, assembly name, publisher id, signer fingerprint, checksum allow-list, or any declared signature that verifies successfully under the active trust policy
- capability metadata can be allowed, trusted-only, or denied
- REST endpoints can opt into request-time enforcement with `RequireCapability("capability.key")`

For independently distributed packages, trust can combine signing keys and checksums:

```json
{
  "Engine": {
    "Trust": {
      "TrustedPublishers": [ "cephalon-labs" ],
      "TrustedSignerFingerprints": [
        "sha256:cephalon-labs-reference-operations"
      ],
      "TrustedSignaturePublicKeys": {
        "cephalon-labs-build": "keys/cephalon-labs-build.public.pem"
      },
      "AllowedPackageChecksums": {
        "operations": [
          "sha256:3e5d5b9fd0dfb7c60e441d013d7d2a60f41c7b0a0a4fb2d2ad5a9f88d6e7c123"
        ]
      }
    }
  }
}
```

That means package authors should give capabilities stable keys and use those keys consistently when binding runtime policies.

## Companion packs vs modules

If a package is mainly about a future-tech workload and provides reusable runtime primitives across many apps, prefer the technology-pack pattern documented in `docs/technology-packs.md`.

Use a module package when the package primarily owns domain behavior.
Use a technology pack when the package primarily owns reusable workload services or capability activation for a technology profile.
If a domain module only needs to add descriptors into an existing technology pack, prefer the pack's contributor services instead of creating a new companion package.
