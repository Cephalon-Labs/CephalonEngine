# CephalonTemplateModule

Generated from the Cephalon behavior-backed REST module package template.

## Included shape

- class library package with `RestBehaviorModuleBase`
- starter behavior with metadata-only REST profile plus module-owned `MapProfile<TBehavior>()`
- lifecycle-aware state service
- starter capability registration
- localized status response using `ILocalizedTextCatalog`

## Next steps

1. Rename `RestBehaviorModuleEntry`, `GetModuleStatusBehavior`, and the contract types to match your domain.
2. Update the module descriptor, behavior id, capability key, and route values to your bounded context.
3. Replace the starter localization keys and status service with your package-owned behavior logic.
4. Keep public REST in `ConfigureRestBehaviors(...)` and keep REST out of behavior transport allowlists.
5. Use `MapGeneratedProfiles(...)` or `MapGeneratedProfileGroups(...)` later if your module grows into a larger generated-profile surface.
