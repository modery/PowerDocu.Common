# CLAUDE.md — PowerDocu.Common

AI assistant guidance for the PowerDocu.Common repository.

## Project Overview

PowerDocu.Common is a .NET 10 class library providing shared parsing and documentation-generation infrastructure for [PowerDocu](https://github.com/modery/PowerDocu). It ingests Microsoft Power Platform artifacts (Power Apps `.msapp`, Power Automate flows, Agents, Model-Driven Apps, Business Process Flows, Desktop Flows, and solution packages) and produces Word, Markdown, and HTML documentation.

**Namespace:** `PowerDocu.Common` (all classes)  
**Target Framework:** `net10.0`, `LangVersion: latest`  
**License:** MIT

---

## Repository Layout

```
PowerDocu.Common/
├── PowerDocu.Common/           # Single class library project
│   ├── *.cs                    # ~59 source files
│   ├── PowerDocu.Common.csproj
│   └── Resources/
│       ├── styles.xml          # Default CSS for generated HTML docs
│       ├── ConnectorIcons/
│       │   ├── connectors.json           # Connector metadata & icon data (336 KB)
│       │   └── GenerateConnectorMapping.ps1
│       └── DefaultSettings/
│               ├── AppDefaultSetting.json
│               ├── ControlDefaultSetting.json
│               └── ScreenDefaultSetting.json
├── README.md
├── LICENSE
└── CLAUDE.md                   # This file
```

No test project lives in this repo. Tests exist in the consumer PowerDocu project.

---

## Architecture: Entity–Parser–Builder

The codebase follows three distinct tiers:

### 1. Entity Classes (Data Models)

Pure data containers. Prefer simple public properties and lists. Avoid logic beyond light helper methods that look up or filter the entity's own data.

| Entity | Source artifact |
|--------|----------------|
| `SolutionEntity` | solution.xml from solution ZIP |
| `CustomizationsEntity` | customizations.xml (lazy-parsed via XPath) |
| `AppEntity` | `.msapp` Canvas App package |
| `FlowEntity` | `definition.json` in flow ZIP |
| `AgentEntity` | Agent YAML/JSON configuration |
| `BPFEntity` | Business Process Flow XAML |
| `DesktopFlowEntity` | Desktop Flow definition |
| `TableEntity` | Dataverse table definition |
| `ConnectionReference` | Connection references embedded in flows/apps |
| `Expression` | Parsed Power FX expression tree |

Notable sub-structures:
- `AppEntity.ControlEntity` — recursive UI control tree with Properties and Rules
- `AppEntity.DataSource` — name, type, and property bag; `IsSampleData`, `IsAuxiliary`, `IsStandard`
- `ActionGraph.ActionNode` — flow action with Neighbours (sequential), Subactions (nested), and Elseactions (conditional)
- `FlowEntity.FlowType` enum: `CloudFlow | DesktopFlow | BusinessProcessFlow | Unknown`
- `FlowEntity.ModernFlowType` enum: `CloudFlow=0 | AgentFlow=1 | M365CopilotAgentFlow=2`

### 2. Parser Classes

Parse source artifacts into entity objects. Live in `*Parser.cs` files.

| Parser | Input | Output |
|--------|-------|--------|
| `SolutionParser` | `.zip` solution package | `SolutionEntity` |
| `CustomizationsParser` | `customizations.xml` | `CustomizationsEntity` |
| `FlowParser` | Flow ZIP (`PackageType`: FlowPackage/LogicAppsTemplate/SolutionPackage) | `FlowEntity` |
| `AppParser` | `.msapp` archive | `AppEntity` |
| `AgentParser` | Agent definition files | `AgentEntity` |
| `AppActionParser` | `appaction.xml` | `AppActionEntity` |
| `BPFXamlParser` | XAML definition | `BPFEntity` |
| `RobinScriptParser` | ROBIN script | `DesktopFlowEntity` |
| `EnvironmentVariableParser` | XML node | `EnvironmentVariableEntity` |
| `SettingDefinitionParser` | XML node | `SettingDefinitionEntity` |

Key patterns in parsers:
- Use `ZipHelper` to extract files from ZIP archives before parsing
- Wrap parse logic in `try-catch` and emit via `NotificationHelper` on errors
- Rely on `Newtonsoft.Json` for JSON, `XmlDocument`/XPath for XML, `YamlDotNet` for YAML

### 3. Builder Classes (Documentation Output)

Abstract base classes; concrete implementations live in the consuming PowerDocu project.

| Base Class | Output Format | Key Members |
|------------|--------------|-------------|
| `WordDocBuilder` | `.docx` / `.docm` | `InitializeWordDocument()`, `AddStylesPartToPackage()`, page dimension constants |
| `MarkdownBuilder` | `.md` files | `AddExpressionDetails(List<Expression>)` — renders expression trees as HTML tables |
| `HtmlBuilder` | `.html` files | `WrapInHtmlPage(title, body, nav, css)`, `Encode(text)`, `Heading()`, `Link()`, collapsible sidebar JS |

Word page dimensions (for layout calculations):
- `PageWidth` = 11906, `PageHeight` = 16838, Margins = 1000 each
- `DocumentSizePerPixel` = 15, `EmuPerPixel` = 9525

---

## Central Registry: `DocumentationContext`

The `DocumentationContext` is the single source of truth passed through the documentation pipeline. It holds all parsed entities and exposes cross-reference resolution helpers.

```csharp
// Key properties
SolutionEntity Solution
CustomizationsEntity Customizations
List<FlowEntity> Flows
List<AppEntity> Apps
List<AgentEntity> Agents
List<AppModuleEntity> AppModules
List<BPFEntity> BusinessProcessFlows
List<DesktopFlowEntity> DesktopFlows
List<TableEntity> Tables
List<RoleEntity> Roles
ConfigHelper Config
string OutputPath
bool FullDocumentation
string SourceZipPath
ProgressTracker ProgressTracker

// Cross-reference helpers
string GetFlowNameById(string flowId)
FlowEntity GetFlowById(string flowId)
string GetAppNameBySchemaName(string schemaName)
AppEntity GetAppByName(string name)
string GetAgentNameBySchemaName(string schemaName)
```

Always add new entity types to `DocumentationContext` and provide lookup helpers when the entity is referenceable from other entities.

---

## Helper & Utility Classes (Static)

| Class | Purpose |
|-------|---------|
| `ConnectorHelper` | Loads `connectors.json`; resolves display names and icon paths. Uses `ConcurrentDictionary` for thread safety. |
| `ZipHelper` | `getWorkflowFilesFromZip()`, `getFilesInPathFromZip()`, `getFileFromZip()` |
| `NotificationHelper` | Observer-pattern notifications. Call `SendNotification()`, `SendStatusUpdate()`, `SendPhaseUpdate()`. Register receivers via `AddNotificationReceiver()`. |
| `ConfigHelper` | Persists settings to `%APPDATA%\PowerDocu\powerdocu.config.json`. |
| `ImageHelper` | SVG→PNG conversion with caching; concurrent access safe. |
| `DefaultChangeHelper` | Identifies canvas app properties that differ from defaults. |
| `TableDefinitionHelper` | Parses Power FX table-definition expressions into `TableDefinitionInfo`. |
| `CrossDocLinkHelper` | Generates cross-document navigation links. |
| `SolutionComponentHelper` | Builds solution component dependency graphs. |
| `OutputFormatHelper` | Constants: `All`, `Word`, `Markdown`, `Html`. |
| `FlowActionSortOrderHelper` | Constants: `ByName`, `ByOrder`. |
| `AssemblyHelper` | `GetApplicationName()`, `GetExecutablePath()`. |
| `PowerDocuReleaseHelper` | `CheckForUpdates()` — network call to check for new releases. |
| `ColourHelper` | Hex/RGB conversion utilities. |
| `CharsetHelper` | Charset validation and conversion. |

---

## Progress Tracking

`ProgressTracker` provides thread-safe progress reporting (uses `lock`):

```csharp
tracker.Register("Flows", totalCount);
tracker.Increment("Flows");
tracker.Complete("Flows");
string display = tracker.BuildString(); // "3/10 Flows"
```

---

## Configuration Defaults (`ConfigHelper`)

Default settings reflect common use-cases; do not change defaults without considering downstream impact:

| Setting | Default |
|---------|---------|
| `outputFormat` | `All` |
| `documentChangesOnlyCanvasApps` | `true` |
| `documentDefaultValuesCanvasApps` | `true` |
| `flowActionSortOrder` | `ByName` |
| `wordTemplate` | `null` |
| `documentSolution/Flows/Agents/Apps/etc.` | `true` |
| `documentDefaultColumns` | `false` |
| `addTableOfContents` | `false` |
| `showAllComponentsInGraph` | `true` |
| `checkForUpdatesOnLaunch` | `true` |

---

## NuGet Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `DocumentFormat.OpenXml` | 3.5.1 | Word document generation |
| `Grynwald.MarkdownGenerator` | 3.0.106 | Markdown generation |
| `Newtonsoft.Json` | 13.0.4 | JSON parsing |
| `Svg` | 3.4.7 | SVG to PNG conversion |
| `Rubjerg.Graphviz` | 3.0.4 | Graph visualization |
| `HtmlAgilityPack` | 1.12.4 | HTML parsing |
| `System.Drawing.Common` | 10.0.5 | Drawing utilities |
| `Microsoft.PowerFx.Core` | 1.8.1 | Power FX formula evaluation |
| `YamlDotNet` | 16.3.0 | YAML parsing |

All `Resources/` subdirectories are configured with `CopyToOutputDirectory: Always` and `ExcludeFromSingleFile: true`.

---

## Coding Conventions

### Naming
- **Classes/Properties/Methods:** PascalCase (`AppEntity`, `GetFlowById`, `SourceZipPath`)
- **Private fields:** camelCase, optionally prefixed with `_` (`_iconFilePaths`)
- **Constants:** UPPER_SNAKE_CASE for low-level values; PascalCase constants in static helper classes
- **Enums:** PascalCase members (`FlowType.CloudFlow`)

### Collections
- Prefer `List<T>` for ordered sequences
- Use `Dictionary<K,V>` for keyed lookup
- Use `ConcurrentDictionary<K,V>` when accessed from multiple threads
- Use `HashSet<T>` for deduplication

### XML/JSON/YAML Parsing
- **XML:** `XmlDocument` with XPath queries; avoid LINQ-to-XML unless already present in file
- **JSON:** `Newtonsoft.Json` (`JObject`, `JArray`, `JsonConvert`)
- **YAML:** `YamlDotNet`

### Null Safety
- Use `string.IsNullOrEmpty()` for string guards
- Use `??` null coalescing where appropriate
- Avoid throwing on null; return empty string/list as sentinel values in entity lookups

### Error Handling
- Parsers catch exceptions and emit via `NotificationHelper.SendNotification()`
- Do not swallow exceptions silently; always notify

### Thread Safety
- New shared state accessed concurrently must use `ConcurrentDictionary` or `lock`
- `ProgressTracker` already handles thread safety; use it for progress reporting

### Output Builders
- HTML output: use `HtmlBuilder` helpers (`Heading()`, `Paragraph()`, `Link()`, `Image()`, `Encode()`) — never concatenate raw HTML strings without encoding user-controlled content
- Word output: use `WordDocBuilder` constants for dimensions; do not hard-code pixel values
- Always call `AddExpressionDetails()` for rendering `Expression` trees rather than writing custom table renderers

---

## Development Workflow

### Build

```bash
cd PowerDocu.Common
dotnet build
```

No test project in this repo — integration tests are in the consuming PowerDocu solution.

### Branch Strategy
- Main branch: `main`
- Feature branches: `feature/<description>` or `claude/<description>`
- Commit directly to feature branch; merge to `main` via PR

### Updating NuGet Packages
After updating package versions in `PowerDocu.Common.csproj`, run:
```bash
dotnet restore
dotnet build
```

### Adding a New Power Platform Component Type

1. Create `<Type>Entity.cs` — data model
2. Create `<Type>Parser.cs` — parser that populates the entity
3. Add entity list and lookup helpers to `DocumentationContext`
4. Update `SolutionParser` to invoke the new parser when the component type is detected in a solution ZIP
5. Builder implementations (in the consuming project) extend `WordDocBuilder`/`MarkdownBuilder`/`HtmlBuilder`

### Adding a New Configuration Option

1. Add property to `ConfigHelper` with a sensible default
2. Ensure it round-trips through `LoadConfigurationFromFile()` / `SaveConfigurationToFile()`
3. Expose on `DocumentationContext` via the `ConfigHelper` reference

---

## Key Files Quick Reference

| File | Purpose |
|------|---------|
| `DocumentationContext.cs` | Central registry — start here to understand data flow |
| `FlowEntity.cs` | Flow data model with `FlowType`/`ModernFlowType` enums |
| `AppEntity.cs` | Canvas App data model including `ControlEntity` tree |
| `AgentEntity.cs` | Agent data model with topic/knowledge/tool accessors |
| `SolutionParser.cs` | Entry point for processing solution ZIP packages |
| `FlowParser.cs` | Entry point for flow definition parsing |
| `WordDocBuilder.cs` | Abstract base for Word doc generation |
| `HtmlBuilder.cs` | Abstract base for HTML generation |
| `MarkdownBuilder.cs` | Abstract base for Markdown generation |
| `ConnectorHelper.cs` | Connector metadata, loaded from `connectors.json` |
| `NotificationHelper.cs` | Observer-pattern notification plumbing |
| `ConfigHelper.cs` | Persistent user configuration |
| `ProgressTracker.cs` | Thread-safe progress reporting |
