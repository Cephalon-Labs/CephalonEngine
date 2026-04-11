# Host Configuration

Generated Cephalon hosts load configuration in three layers:

- shared Cephalon defaults from `Configurations/Add*.json`
- optional grouped environment overrides from `Configurations/{group}/{Environment}.json`
- standard project overrides from `appsettings.json` and `appsettings.{Environment}.json`

The generated `Configurations/Observability/Development.json` already includes a Serilog console
sample. `Program.cs` switches cleanly to Serilog only when a top-level `Serilog` section exists, so
the starter stays optional for other environments instead of forcing a provider decision globally.

This starter keeps the shipped baseline in root `Add*.json` files so the host stays deterministic in
`Development`, `Local`, `Production`, or any other environment name without requiring duplicate files.

When you need per-environment differences later, add overrides such as:

- `Configurations/OpenApi/Development.json`
- `Configurations/Engine/Observability/Production.json`

`Program.cs` already loads this convention through `AddCephalonProjectConfigurations()`. The engine inserts
split-config defaults ahead of standard host overrides, so `appsettings.json`, user secrets, environment
variables, and command-line arguments continue to win the same way developers expect in ASP.NET Core.
