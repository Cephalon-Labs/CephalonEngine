# Cephalon.Scaffolding

Generated from XML comments and the public API surface of the compiled assembly.

[Back to reference index](README.md) | [Namespace index](namespaces.md) | [Type index](types.md) | [Member index](members.md) | [Browser view](browse.html?assembly=Cephalon.Scaffolding)
## Namespaces

- `Cephalon.Scaffolding.Generation`
- `Cephalon.Scaffolding.IO`

<a id="namespace-cephalon-scaffolding-generation"></a>

## Namespace Cephalon.Scaffolding.Generation

<a id="type-cephalon-scaffolding-generation-renderedfile"></a>

### `RenderedFile`

Represents one file produced by scaffold generation.

#### Declaration
```csharp
public sealed class RenderedFile
```

#### Constructors

<a id="member-m-cephalon-scaffolding-generation-renderedfile-ctor-system-string-system-string"></a>

##### `RenderedFile`

```csharp
RenderedFile(string path, string contents)
```

Creates a new rendered file.

Parameters:
- `path`: The relative scaffold path of the file.
- `contents`: The file contents that should be written.

#### Properties

<a id="member-p-cephalon-scaffolding-generation-renderedfile-contents"></a>

##### `Contents`

```csharp
string Contents { get; }
```

Gets the contents that should be written to the file.

<a id="member-p-cephalon-scaffolding-generation-renderedfile-path"></a>

##### `Path`

```csharp
string Path { get; }
```

Gets the relative scaffold path of the file.

<a id="type-cephalon-scaffolding-generation-renderedfolder"></a>

### `RenderedFolder`

Represents one folder produced by scaffold generation.

#### Declaration
```csharp
public sealed class RenderedFolder
```

#### Constructors

<a id="member-m-cephalon-scaffolding-generation-renderedfolder-ctor-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `RenderedFolder`

```csharp
RenderedFolder(string path, string purpose, string scope, string projectKey, IReadOnlyDictionary<string, string> metadata)
```

Creates a new rendered folder.

Parameters:
- `path`: The relative scaffold path of the folder.
- `purpose`: The descriptive purpose of the folder.
- `scope`: The scaffold scope that produced the folder.
- `projectKey`: The owning rendered project key, if the folder belongs to a project.
- `metadata`: Additional metadata associated with the folder.

#### Properties

<a id="member-p-cephalon-scaffolding-generation-renderedfolder-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets additional metadata associated with the folder.

<a id="member-p-cephalon-scaffolding-generation-renderedfolder-path"></a>

##### `Path`

```csharp
string Path { get; }
```

Gets the relative scaffold path of the folder.

<a id="member-p-cephalon-scaffolding-generation-renderedfolder-projectkey"></a>

##### `ProjectKey`

```csharp
string ProjectKey { get; }
```

Gets the owning rendered project key when the folder belongs to a rendered project.

<a id="member-p-cephalon-scaffolding-generation-renderedfolder-purpose"></a>

##### `Purpose`

```csharp
string Purpose { get; }
```

Gets the descriptive purpose of the folder.

<a id="member-p-cephalon-scaffolding-generation-renderedfolder-scope"></a>

##### `Scope`

```csharp
string Scope { get; }
```

Gets the scaffold scope that produced the folder.

<a id="type-cephalon-scaffolding-generation-renderedproject"></a>

### `RenderedProject`

Represents one project produced by scaffold generation.

#### Declaration
```csharp
public sealed class RenderedProject
```

#### Constructors

<a id="member-m-cephalon-scaffolding-generation-renderedproject-ctor-system-string-system-string-system-string-system-string-system-string-system-string-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlydictionary-system-string-system-string"></a>

##### `RenderedProject`

```csharp
RenderedProject(string key, string sourceProjectId, string name, string path, string scope, string role, string template, IReadOnlyList<string> packages, IReadOnlyList<string> projectReferences, IReadOnlyDictionary<string, string> metadata)
```

Creates a new rendered project.

Parameters:
- `key`: The unique key of the rendered project instance.
- `sourceProjectId`: The source scaffold project identifier that produced this instance.
- `name`: The generated project name.
- `path`: The relative scaffold path of the project directory.
- `scope`: The scaffold scope that produced the project.
- `role`: The scaffold role of the project.
- `template`: The scaffold template used to generate the project.
- `packages`: The package references implied by the scaffold plan.
- `projectReferences`: The project references implied by the scaffold plan.
- `metadata`: Additional metadata associated with the project.

#### Properties

<a id="member-p-cephalon-scaffolding-generation-renderedproject-key"></a>

##### `Key`

```csharp
string Key { get; }
```

Gets the unique key of the rendered project instance.

<a id="member-p-cephalon-scaffolding-generation-renderedproject-metadata"></a>

##### `Metadata`

```csharp
IReadOnlyDictionary<string, string> Metadata { get; }
```

Gets additional metadata associated with the project.

<a id="member-p-cephalon-scaffolding-generation-renderedproject-name"></a>

##### `Name`

```csharp
string Name { get; }
```

Gets the generated project name.

<a id="member-p-cephalon-scaffolding-generation-renderedproject-packages"></a>

##### `Packages`

```csharp
IReadOnlyList<string> Packages { get; }
```

Gets the package references implied by the scaffold plan.

<a id="member-p-cephalon-scaffolding-generation-renderedproject-path"></a>

##### `Path`

```csharp
string Path { get; }
```

Gets the relative scaffold path of the project directory.

<a id="member-p-cephalon-scaffolding-generation-renderedproject-projectreferences"></a>

##### `ProjectReferences`

```csharp
IReadOnlyList<string> ProjectReferences { get; }
```

Gets the project references implied by the scaffold plan.

<a id="member-p-cephalon-scaffolding-generation-renderedproject-role"></a>

##### `Role`

```csharp
string Role { get; }
```

Gets the scaffold role of the project.

<a id="member-p-cephalon-scaffolding-generation-renderedproject-scope"></a>

##### `Scope`

```csharp
string Scope { get; }
```

Gets the scaffold scope that produced the project.

<a id="member-p-cephalon-scaffolding-generation-renderedproject-sourceprojectid"></a>

##### `SourceProjectId`

```csharp
string SourceProjectId { get; }
```

Gets the source scaffold project identifier that produced this instance.

<a id="member-p-cephalon-scaffolding-generation-renderedproject-template"></a>

##### `Template`

```csharp
string Template { get; }
```

Gets the scaffold template that was used to generate the project.

<a id="type-cephalon-scaffolding-generation-renderedscaffold"></a>

### `RenderedScaffold`

Represents the fully rendered output of a scaffold generation request.

#### Declaration
```csharp
public sealed class RenderedScaffold
```

#### Constructors

<a id="member-m-cephalon-scaffolding-generation-renderedscaffold-ctor-cephalon-abstractions-appmodel-appprofile-cephalon-scaffolding-generation-scaffoldrequest-system-collections-generic-ireadonlylist-cephalon-scaffolding-generation-renderedproject-system-collections-generic-ireadonlylist-cephalon-scaffolding-generation-renderedfolder-system-collections-generic-ireadonlylist-cephalon-scaffolding-generation-renderedfile"></a>

##### `RenderedScaffold`

```csharp
RenderedScaffold(AppProfile appProfile, ScaffoldRequest request, IReadOnlyList<RenderedProject> projects, IReadOnlyList<RenderedFolder> folders, IReadOnlyList<RenderedFile> files)
```

Creates a new rendered scaffold.

Parameters:
- `appProfile`: The application profile used to drive generation.
- `request`: The original scaffold request.
- `projects`: The rendered projects.
- `folders`: The rendered folders.
- `files`: The rendered files.

#### Properties

<a id="member-p-cephalon-scaffolding-generation-renderedscaffold-appprofile"></a>

##### `AppProfile`

```csharp
AppProfile AppProfile { get; }
```

Gets the application profile used to drive generation.

<a id="member-p-cephalon-scaffolding-generation-renderedscaffold-files"></a>

##### `Files`

```csharp
IReadOnlyList<RenderedFile> Files { get; }
```

Gets the rendered files.

<a id="member-p-cephalon-scaffolding-generation-renderedscaffold-folders"></a>

##### `Folders`

```csharp
IReadOnlyList<RenderedFolder> Folders { get; }
```

Gets the rendered folders.

<a id="member-p-cephalon-scaffolding-generation-renderedscaffold-projects"></a>

##### `Projects`

```csharp
IReadOnlyList<RenderedProject> Projects { get; }
```

Gets the rendered projects.

<a id="member-p-cephalon-scaffolding-generation-renderedscaffold-request"></a>

##### `Request`

```csharp
ScaffoldRequest Request { get; }
```

Gets the original scaffold request.

<a id="type-cephalon-scaffolding-generation-scaffoldgenerator"></a>

### `ScaffoldGenerator`

Turns a Cephalon app profile and scaffold request into concrete projects, folders, and files.

#### Declaration
```csharp
public static class ScaffoldGenerator
```

#### Methods

<a id="member-m-cephalon-scaffolding-generation-scaffoldgenerator-generate-cephalon-abstractions-appmodel-appprofile-cephalon-scaffolding-generation-scaffoldrequest"></a>

##### `Generate`

```csharp
RenderedScaffold Generate(AppProfile appProfile, ScaffoldRequest request)
```

Generates a rendered scaffold from the supplied application profile and request.

Returns: The rendered scaffold output.

Parameters:
- `appProfile`: The application profile that carries scaffold guidance.
- `request`: The concrete naming and framework request for generation.

<a id="type-cephalon-scaffolding-generation-scaffoldrequest"></a>

### `ScaffoldRequest`

Describes the user input required to turn an app profile into a concrete scaffold.

#### Declaration
```csharp
public sealed class ScaffoldRequest
```

#### Constructors

<a id="member-m-cephalon-scaffolding-generation-scaffoldrequest-ctor-system-string-system-collections-generic-ireadonlylist-system-string-system-collections-generic-ireadonlylist-system-string-system-string-system-string"></a>

##### `ScaffoldRequest`

```csharp
ScaffoldRequest(string appName, IReadOnlyList<string> modules, IReadOnlyList<string> features, string targetFramework, string cephalonPackageVersion)
```

Creates a new scaffold request.

Parameters:
- `appName`: The application name to scaffold.
- `modules`: The module names to materialize in the scaffold.
- `features`: The feature or slice names to materialize in the scaffold.
- `targetFramework`: The target framework for generated projects.
- `cephalonPackageVersion`: The Cephalon package version to write into the scaffold.

#### Properties

<a id="member-p-cephalon-scaffolding-generation-scaffoldrequest-appname"></a>

##### `AppName`

```csharp
string AppName { get; }
```

Gets the application name to scaffold.

<a id="member-p-cephalon-scaffolding-generation-scaffoldrequest-cephalonpackageversion"></a>

##### `CephalonPackageVersion`

```csharp
string CephalonPackageVersion { get; }
```

Gets the Cephalon package version written into the scaffold.

<a id="member-p-cephalon-scaffolding-generation-scaffoldrequest-features"></a>

##### `Features`

```csharp
IReadOnlyList<string> Features { get; }
```

Gets the feature or slice names to materialize in the scaffold.

<a id="member-p-cephalon-scaffolding-generation-scaffoldrequest-modules"></a>

##### `Modules`

```csharp
IReadOnlyList<string> Modules { get; }
```

Gets the module names to materialize in the scaffold.

<a id="member-p-cephalon-scaffolding-generation-scaffoldrequest-rootnamespace"></a>

##### `RootNamespace`

```csharp
string RootNamespace { get; }
```

Gets the root namespace derived from `AppName`.

<a id="member-p-cephalon-scaffolding-generation-scaffoldrequest-targetframework"></a>

##### `TargetFramework`

```csharp
string TargetFramework { get; }
```

Gets the target framework for generated projects.

<a id="namespace-cephalon-scaffolding-io"></a>

## Namespace Cephalon.Scaffolding.IO

<a id="type-cephalon-scaffolding-io-filesystemscaffoldwriter"></a>

### `FileSystemScaffoldWriter`

Writes a rendered scaffold to the local file system.

#### Declaration
```csharp
public static class FileSystemScaffoldWriter
```

#### Methods

<a id="member-m-cephalon-scaffolding-io-filesystemscaffoldwriter-writeasync-system-string-cephalon-scaffolding-generation-renderedscaffold-system-boolean-system-threading-cancellationtoken"></a>

##### `WriteAsync`

```csharp
Task WriteAsync(string rootPath, RenderedScaffold scaffold, bool overwrite, CancellationToken cancellationToken)
```

Writes the supplied scaffold to disk.

Returns: A task that completes when all folders and files have been written.

Parameters:
- `rootPath`: The target root directory.
- `scaffold`: The rendered scaffold to write.
- `overwrite`: `true` to overwrite existing files; otherwise the write fails when a target file exists.
- `cancellationToken`: A token that can cancel the write operation.
