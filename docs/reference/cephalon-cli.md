# Cephalon.Cli

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Cli)
## Namespaces

- `Cephalon.Cli`

<a id="namespace-cephalon-cli"></a>

## Namespace Cephalon.Cli

<a id="type-cephalon-cli-cliapplication"></a>

### `CliApplication`

Hosts the main command-dispatch entry point for the Cephalon CLI.

#### Declaration
```csharp
public static class CliApplication
```

#### Methods

<a id="member-m-cephalon-cli-cliapplication-runasync-system-string-system-io-textwriter-system-io-textwriter-system-threading-cancellationtoken"></a>

##### `RunAsync`

```csharp
Task<int> RunAsync(string[] args, TextWriter output, TextWriter error, CancellationToken cancellationToken)
```

Runs the CLI for the supplied arguments and writers.

Returns: The process exit code.

Parameters:
- `args`: The command-line arguments to execute.
- `output`: The writer used for standard output.
- `error`: The writer used for error output.
- `cancellationToken`: A token that can cancel CLI execution.
