# Cephalon local package feed

Place local `Cephalon.*.nupkg` artifacts here when validating generated applications before packages are published to NuGet.

`NuGet.config` maps `Cephalon*` packages to this folder through the `cephalon` package source. Seed it with `pwsh ./scripts/publish-package-artifacts.ps1`, or replace the `cephalon` package source with NuGet.org or your private feed when packages are promoted.

The generated Dockerfile and compose path use the same restore configuration automatically.
