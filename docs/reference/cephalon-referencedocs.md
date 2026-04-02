# Cephalon.ReferenceDocs

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.ReferenceDocs)
## Namespaces

- `Cephalon.ReferenceDocs`
- `Cephalon.ReferenceDocs.Generation`
- `Cephalon.ReferenceDocs.IO`

<a id="namespace-cephalon-referencedocs"></a>

## Namespace Cephalon.ReferenceDocs

<a id="type-cephalon-referencedocs-referencedocsapplication"></a>

### `ReferenceDocsApplication`

Hosts the main command-dispatch entry point for the Cephalon reference docs generator.

#### Declaration
```csharp
public static class ReferenceDocsApplication
```

#### Methods

<a id="member-m-cephalon-referencedocs-referencedocsapplication-runasync-system-string-system-io-textwriter-system-io-textwriter-system-threading-cancellationtoken"></a>

##### `RunAsync`

```csharp
Task<int> RunAsync(string[] args, TextWriter output, TextWriter error, CancellationToken cancellationToken)
```

Runs the reference docs generator for the supplied arguments and writers.

Returns: The process exit code.

Parameters:
- `args`: The command-line arguments to execute.
- `output`: The writer used for standard output.
- `error`: The writer used for error output.
- `cancellationToken`: A token that can cancel execution.

<a id="namespace-cephalon-referencedocs-generation"></a>

## Namespace Cephalon.ReferenceDocs.Generation

<a id="type-cephalon-referencedocs-generation-referencedocfile"></a>

### `ReferenceDocFile`

Represents one generated reference documentation file.

#### Declaration
```csharp
public sealed class ReferenceDocFile
```

#### Constructors

<a id="member-m-cephalon-referencedocs-generation-referencedocfile-ctor-system-string-system-string"></a>

##### `ReferenceDocFile`

```csharp
ReferenceDocFile(string path, string contents)
```

Creates a new generated reference documentation file.

Parameters:
- `path`: The relative output path of the file.
- `contents`: The markdown contents of the file.

#### Properties

<a id="member-p-cephalon-referencedocs-generation-referencedocfile-contents"></a>

##### `Contents`

```csharp
string Contents { get; }
```

Gets the markdown contents of the file.

<a id="member-p-cephalon-referencedocs-generation-referencedocfile-path"></a>

##### `Path`

```csharp
string Path { get; }
```

Gets the relative output path of the file.

<a id="type-cephalon-referencedocs-generation-referencedocsgenerator"></a>

### `ReferenceDocsGenerator`

Generates markdown reference documentation from Cephalon public assemblies and their XML docs.

#### Declaration
```csharp
public static class ReferenceDocsGenerator
```

#### Methods

<a id="member-m-cephalon-referencedocs-generation-referencedocsgenerator-generate-cephalon-referencedocs-generation-referencedocsrequest"></a>

##### `Generate`

```csharp
RenderedReferenceDocs Generate(ReferenceDocsRequest request)
```

Generates rendered markdown reference docs for the supplied request.

Returns: The rendered markdown files.

Parameters:
- `request`: The generation request.

<a id="type-cephalon-referencedocs-generation-referencedocsrequest"></a>

### `ReferenceDocsRequest`

Describes the input required to generate Cephalon reference documentation.

#### Declaration
```csharp
public sealed class ReferenceDocsRequest
```

#### Constructors

<a id="member-m-cephalon-referencedocs-generation-referencedocsrequest-ctor-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlylist-system-string"></a>

##### `ReferenceDocsRequest`

```csharp
ReferenceDocsRequest(string rootPath, string outputPath, string configuration, string targetFramework, IReadOnlyList<string> assemblies)
```

Creates a new reference docs request.

Parameters:
- `rootPath`: The repository root path.
- `outputPath`: The output directory where reference docs should be written.
- `configuration`: The build configuration to read from.
- `targetFramework`: The target framework to read from.
- `assemblies`: The assemblies to document. When omitted, the generator uses its curated defaults.

#### Properties

<a id="member-p-cephalon-referencedocs-generation-referencedocsrequest-assemblies"></a>

##### `Assemblies`

```csharp
IReadOnlyList<string> Assemblies { get; }
```

Gets the assemblies to document. When empty, the generator uses its curated defaults.

<a id="member-p-cephalon-referencedocs-generation-referencedocsrequest-configuration"></a>

##### `Configuration`

```csharp
string Configuration { get; }
```

Gets the build configuration to read from.

<a id="member-p-cephalon-referencedocs-generation-referencedocsrequest-outputpath"></a>

##### `OutputPath`

```csharp
string OutputPath { get; }
```

Gets the output directory where reference docs should be written.

<a id="member-p-cephalon-referencedocs-generation-referencedocsrequest-rootpath"></a>

##### `RootPath`

```csharp
string RootPath { get; }
```

Gets the repository root path.

<a id="member-p-cephalon-referencedocs-generation-referencedocsrequest-targetframework"></a>

##### `TargetFramework`

```csharp
string TargetFramework { get; }
```

Gets the target framework to read from.

<a id="type-cephalon-referencedocs-generation-renderedreferencedocs"></a>

### `RenderedReferenceDocs`

Represents the full rendered output of a reference documentation generation request.

#### Declaration
```csharp
public sealed class RenderedReferenceDocs
```

#### Constructors

<a id="member-m-cephalon-referencedocs-generation-renderedreferencedocs-ctor-cephalon-referencedocs-generation-referencedocsrequest-system-collections-generic-ireadonlylist-cephalon-referencedocs-generation-referencedocfile"></a>

##### `RenderedReferenceDocs`

```csharp
RenderedReferenceDocs(ReferenceDocsRequest request, IReadOnlyList<ReferenceDocFile> files)
```

Creates a new rendered reference docs result.

Parameters:
- `request`: The original generation request.
- `files`: The generated markdown files.

#### Properties

<a id="member-p-cephalon-referencedocs-generation-renderedreferencedocs-files"></a>

##### `Files`

```csharp
IReadOnlyList<ReferenceDocFile> Files { get; }
```

Gets the generated markdown files.

<a id="member-p-cephalon-referencedocs-generation-renderedreferencedocs-request"></a>

##### `Request`

```csharp
ReferenceDocsRequest Request { get; }
```

Gets the original generation request.

<a id="namespace-cephalon-referencedocs-io"></a>

## Namespace Cephalon.ReferenceDocs.IO

<a id="type-cephalon-referencedocs-io-referencedocswriter"></a>

### `ReferenceDocsWriter`

Writes rendered reference documentation to the local file system.

#### Declaration
```csharp
public static class ReferenceDocsWriter
```

#### Methods

<a id="member-m-cephalon-referencedocs-io-referencedocswriter-writeasync-cephalon-referencedocs-generation-renderedreferencedocs-system-boolean-system-threading-cancellationtoken"></a>

##### `WriteAsync`

```csharp
Task WriteAsync(RenderedReferenceDocs rendered, bool overwrite, CancellationToken cancellationToken)
```

Writes the supplied reference docs output to disk.

Returns: A task that completes when all files have been written.

Parameters:
- `rendered`: The rendered reference docs to write.
- `overwrite`: `true` to overwrite existing files; otherwise the write fails when a target file exists.
- `cancellationToken`: A token that can cancel the write operation.
