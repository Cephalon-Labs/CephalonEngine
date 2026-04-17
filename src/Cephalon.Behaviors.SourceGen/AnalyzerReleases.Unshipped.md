; Unshipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules
Rule ID | Category | Severity | Notes
--------|----------|----------|-------
ABT0010 | Cephalon.Behaviors | Error | AppBehavior class must implement IAppBehavior<TIn, TOut>
ABT0011 | Cephalon.Behaviors | Error | AppBehavior id must not be empty
ABT0012 | Cephalon.Behaviors | Error | AppBehavior class must not be abstract
ABT0013 | Cephalon.Behaviors | Error | AppBehavior class must not be static
ABT0014 | Cephalon.Behaviors | Error | Public REST is module-owned only and must not be declared in behavior topology
ABT0015 | Cephalon.Behaviors | Error | BehaviorRestProfile must select a supported REST method
ABT0016 | Cephalon.Behaviors | Error | BehaviorRestProfile relative pattern must not be empty
ABT0017 | Cephalon.Behaviors | Error | BehaviorRestProfile API version must be greater than zero when specified
ABT0018 | Cephalon.Behaviors | Error | BehaviorRestProfile relative pattern must start with '/'
ABT0019 | Cephalon.Behaviors | Error | BehaviorRestBinding target property name must not be empty
ABT0020 | Cephalon.Behaviors | Error | BehaviorRestBinding source must be supported
ABT0021 | Cephalon.Behaviors | Error | BehaviorRestBinding metadata requires an object input with public properties
ABT0022 | Cephalon.Behaviors | Error | BehaviorRestBinding property must exist on the behavior input
ABT0023 | Cephalon.Behaviors | Error | BehaviorRestBinding property must not be declared more than once
ABT0024 | Cephalon.Behaviors | Error | BehaviorRestBinding body sources require a body-capable REST method
ABT0025 | Cephalon.Behaviors | Error | BehaviorRestBinding route sources must match declared route placeholders
ABT0026 | Cephalon.Behaviors | Error | BehaviorRestProfile relative pattern must use valid route placeholder syntax
ABT0027 | Cephalon.Behaviors | Error | BehaviorRestProfile preserved implicit query fallback requires explicit bindings
