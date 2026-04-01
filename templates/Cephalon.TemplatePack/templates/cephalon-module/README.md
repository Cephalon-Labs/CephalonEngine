# CephalonTemplateModule

Generated from the Cephalon module package template.

## Included shape

- Host-agnostic class library
- `ModuleBase` entry point with lifecycle hooks
- starter capability registration
- starter localization resource contribution
- state service for module lifecycle examples

## Next steps

1. Rename `ModuleEntry` and update the descriptor values to match your domain.
2. Replace the starter capability keys with package-specific ones.
3. Register the package through assembly discovery or `engine.AddModule(new ModuleEntry())`.
4. Add transport-specific contribution interfaces only when the package really owns an external surface.
