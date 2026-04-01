# CephalonTemplateModule

Generated from the Cephalon REST module package template.

## Included shape

- class library package with `IRestModule`
- lifecycle-aware state service
- starter capability registration
- localized REST response using `ILocalizedTextCatalog`

## Next steps

1. Rename `RestModuleEntry` and update the descriptor and route values to match your domain.
2. Replace the starter capability keys with your package capabilities.
3. Add the package assembly to `Engine:Discovery:Assemblies` or register the module manually.
4. Keep transport-specific behavior isolated in this package instead of leaking it into host startup.
