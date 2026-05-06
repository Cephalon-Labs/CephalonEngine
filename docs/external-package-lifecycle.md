# External Package Lifecycle

This guide shows the operator-facing baseline for taking a Cephalon module package from a published artifact to a trusted runtime load outside this repository.

Use it together with [Module authoring](module-authoring.md) when you are shipping independently distributed Cephalon packages.

For the repo-native replay of this full out-of-tree path, use `pwsh ./scripts/validate-out-of-tree-package-adoption.ps1`. The scorecard read model validates that replay through `scripts/adoption-smoke-support.json` and emits the package-stage, trust-policy, host-run, and runtime-probe coverage as `AdoptionSmokeEvidence`.
For the matching detached-signature and publisher or signer trust replay, use `pwsh ./scripts/validate-signed-package-governance.ps1`.
For the matching certificate-chain trust replay, use `pwsh ./scripts/validate-signed-package-certificate-chain-governance.ps1`.

## What this proves

- a module can be packed as a published `.nupkg`
- the published artifact can be staged into a loadable package directory with `cephalon package stage`
- a host can load that staged package through `Engine:Discovery:PackageDirectories`
- `Engine:PackagePolicy` and `Engine:Trust` can govern the load
- detached signatures can be required and verified through `Engine:Trust:TrustedSignaturePublicKeys` or `Engine:Trust:TrustedSignatureCertificates` plus `Engine:Trust:TrustedSignatureCertificateAuthorities`
- tampered signed packages can be denied before their module code becomes active
- `/engine/packages`, `/engine/package-policy`, `/engine/trust-policy`, and `/engine/snapshot` expose the truth of what loaded

## 1. Publish the module artifact

Pack the reference module or your own module package:

```powershell
dotnet pack ./samples/Cephalon.ReferenceModule.Operations/Cephalon.ReferenceModule.Operations.csproj `
  -c Release `
  -o ./artifacts/reference-packages
```

The published `.nupkg` is the distribution artifact. It is not yet the directory shape that `Engine:Discovery:PackageDirectories` loads directly.

## 2. Stage the package into a loadable directory

Use the Cephalon CLI to materialize the manifest and selected `lib/<tfm>` files into a package directory:

```powershell
cephalon package stage `
  --package ./artifacts/reference-packages/Cephalon.ReferenceModule.Operations.1.0.0.nupkg `
  --output ./plugins/reference-operations
```

If you are working inside this repository before installing the tool package, run the same command through the project entry point:

```powershell
dotnet run --project ./src/Cephalon.Cli -- package stage `
  --package ./artifacts/reference-packages/Cephalon.ReferenceModule.Operations.1.0.0.nupkg `
  --output ./plugins/reference-operations
```

The staged directory now contains the package manifest and loadable assembly surface:

```text
plugins/reference-operations/
  cephalon.package.json
  Cephalon.ReferenceModule.Operations.dll
  Cephalon.ReferenceModule.Operations.xml
  PACKAGE.md
```

## 3. Load it through package discovery

Point the host at a package directory instead of a repo-local assembly name:

```json
{
  "Engine": {
    "Blueprint": "ModularMonolith",
    "Transports": [ "RestApi" ],
    "Discovery": {
      "PackageDirectories": [
        {
          "Path": "plugins",
          "IncludeSubdirectories": true
        }
      ]
    },
    "PackagePolicy": {
      "AllowAssemblyPathPackages": false,
      "RequireVersion": true,
      "RequireMinimumEngineVersion": true,
      "RequireSupportedTargetFrameworks": true,
      "RequirePublisherId": true
    },
    "Trust": {
      "RequireTrustedPackages": true,
      "TrustedPublishers": [ "cephalon-labs" ]
    }
  }
}
```

This keeps the load manifest-driven, requires the metadata that external operators care about, and lets trust policy stay explicit.

## 4. Run the host and inspect runtime truth

After the host starts, inspect:

- `/api/operations/status`
- `/engine/packages`
- `/engine/package-policy`
- `/engine/trust-policy`
- `/engine/snapshot`

What you should see:

- `/api/operations/status` returns the module-owned response
- `/engine/packages` shows `reference-operations` with `kind: directory-manifest`
- `/engine/packages` shows the staged manifest path in `sourcePath` and the staged DLL in `path`
- `/engine/package-policy` shows the stricter metadata requirements that were applied
- `/engine/trust-policy` shows `RequireTrustedPackages` plus the trusted publisher rule
- `/engine/snapshot` shows the trusted package and its active module in the merged runtime view

## 5. Require detached signatures when provenance matters

When a host needs cryptographic trust instead of publisher-only trust, tighten package policy and trust configuration so detached signatures become mandatory:

```json
{
  "Engine": {
    "PackagePolicy": {
      "AllowAssemblyPathPackages": false,
      "RequireVersion": true,
      "RequireMinimumEngineVersion": true,
      "RequireSupportedTargetFrameworks": true,
      "RequirePublisherId": true,
      "RequireSignatureFingerprint": true,
      "RequireSignatureKeyId": true,
      "RequireSignatureValue": true,
      "RequireSignatureVerification": true
    },
    "Trust": {
      "RequireTrustedPackages": true,
      "TrustedSignaturePublicKeys": {
        "cephalon-labs-build": "keys/cephalon-labs-build.public.pem"
      }
    }
  }
}
```

This keeps package governance tied to the exact signing key instead of a broader publisher allow-list. Under that stricter posture:

- `/engine/packages` should surface the publisher, signer, `signatureKeyId`, `signatureFingerprint`, `isSignatureVerified`, `signatureVerificationReason`, and trust reason for the loaded package
- `/engine/trust-policy` should surface the trusted public-key map that made the verification possible
- `/engine/snapshot` should mirror the same verified package truth in the merged runtime view
- a tampered signed package should fail startup when `RequireSignatureVerification` is enabled

If you prefer certificate-chain trust instead of a trusted public-key file, keep the same package-policy settings but swap the trust block to:

```json
{
  "Engine": {
    "Trust": {
      "RequireTrustedPackages": true,
      "TrustedSignatureCertificates": {
        "cephalon-labs-signing-cert": "keys/cephalon-labs-signing-cert.pem"
      },
      "TrustedSignatureCertificateAuthorities": [
        "keys/cephalon-labs-root.pem"
      ]
    }
  }
}
```

Under that certificate-chain posture:

- `/engine/packages` should surface `verificationSource: trusted-certificate-chain` plus `signatureCertificateThumbprint`
- `/engine/trust-policy` should surface the trusted signing certificate map plus the trusted certificate-authority list
- `/engine/snapshot` should mirror the same `trusted-certificate-chain` and certificate-thumbprint truth in the merged runtime view

## Operational notes

- use `cephalon.package.json` as the operator-readable contract for version, compatibility, publisher, provenance, signature, and integrity metadata
- prefer `Engine:PackagePolicy` when hosts must reject ambiguous raw assembly loads
- prefer `Engine:Trust` publisher, signer, certificate, or checksum rules when package governance matters
- prefer `Engine:Trust:TrustedSignaturePublicKeys` plus `RequireSignatureVerification` when detached signing should be enforced through an explicit trusted signer key
- prefer `Engine:Trust:TrustedSignatureCertificates` plus `Engine:Trust:TrustedSignatureCertificateAuthorities` when detached signing should be enforced through a trusted signing certificate chain and surfaced as `trusted-certificate-chain` with a `certificateThumbprint`
- keep package directories explicit and stable so operators can reason about what the host is allowed to load
- if you republish a package and want to replace an existing staged directory, rerun `cephalon package stage` with `--force`

## Related docs

- [Module authoring](module-authoring.md)
- [Package publishing](package-publishing.md)
- [Getting started](getting-started.md)
- [Operations](operations.md)
