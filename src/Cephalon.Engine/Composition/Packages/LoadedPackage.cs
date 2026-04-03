using System.Reflection;
using Cephalon.Abstractions.Modules;

namespace Cephalon.Engine.Composition.Packages;

internal sealed record LoadedPackage(
    PackageLoadRequest Request,
    Assembly Assembly,
    string LoadContextName,
    IModule[] Modules,
    string ChecksumSha256,
    PackageSignatureVerificationResult SignatureVerification);
