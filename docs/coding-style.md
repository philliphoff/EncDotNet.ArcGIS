# C# coding style guide

This is the **normative** coding style guide for the C#/.NET code in
EncDotNet.ArcGIS. It is carried over from
[EncDotNet.S100](https://github.com/philliphoff/EncDotNet.S100/blob/main/docs/coding-style.md),
so code reads the same in both repositories; the differences are the add-in
rules in §10 and §11. It exists so that style is *explicit and documented*
rather than inferred from surrounding code. New code must follow it.

> **Scope.** This document covers *language, formatting, and naming* style.
> For *API-shape* decisions (collection return types, `class` vs `record`,
> quantity types) follow EncDotNet.S100's [API design
> conventions](https://github.com/philliphoff/EncDotNet.S100/blob/main/docs/design/api-conventions.md).
> For test conventions see
> [`CONTRIBUTING.md`](https://github.com/philliphoff/EncDotNet.ArcGIS/blob/main/CONTRIBUTING.md).

> **Enforcement.** The mechanically-checkable rules below are encoded in the
> repository [`.editorconfig`](https://github.com/philliphoff/EncDotNet.ArcGIS/blob/main/.editorconfig) and can be checked/applied with
> `dotnet format`. CI's *Format check* job uses one targeted invocation to
> verify **whitespace/formatting**, **using directives** (IDE0005: System-first
> ordering, placement outside the file-scoped namespace, and removal of
> unnecessary directives), and **naming** (IDE1006). IDE1006 is verification
> only because `dotnet format` cannot Fix-All naming; naming failures must be
> corrected manually. Other style diagnostics remain ungated because the
> solution-wide style fix-all is unreliable.
> `EnforceCodeStyleInBuild` is left off so `dotnet build` stays fast and does
> not turn style suggestions into build errors. When this document and the
> `.editorconfig` disagree, the encoded rule wins and this document should be
> corrected.

## 1. Guiding principles

1. **Consistency over personal preference.** Match the established style even
   if you would personally choose differently. A uniform codebase is easier to
   read, review, and refactor mechanically.
2. **The compiler is the first reviewer.** The build sets
   `TreatWarningsAsErrors=true` (see `Directory.Build.props`); a warning is a
   failure. Do not suppress warnings to make the build pass — fix the cause.
3. **Nullability is part of the type.** Nullable reference types are enabled
   solution-wide. Model presence/absence honestly instead of defeating the
   analyzer.
4. **Prefer the standard tooling.** Formatting is whatever `dotnet format`
   produces from the shared `.editorconfig`; do not hand-format against it.

## 2. Files, namespaces, and usings

- **One top-level type per file**, and the file name matches the type
  (`SpecRef.cs` contains `SpecRef`). Small tightly-coupled helpers (a private
  nested type, a partial file) are the exception.
- **File-scoped namespaces** only:

  ```csharp
  namespace EncDotNet.SoundCharts;
  ```

  Block-scoped (`namespace X { ... }`) namespaces are not used anywhere in the
  codebase and must not be introduced.
- **`ImplicitUsings` is enabled**, so do not add `using` directives for the
  implicit set (`System`, `System.Linq`, `System.Collections.Generic`, etc.).
  Add only the usings the implicit set does not cover. WPF projects leave
  `System.IO` out of the implicit set (it clashes with
  `System.Windows.Shapes.Path`), so add `using System.IO;` where needed.
- Place `using` directives at the top of the file, `System.*` first, then
  other namespaces, each group alphabetically ordered. Remove unused usings
  (they are warnings, and warnings fail the build).
- Use **UTF-8**, LF or platform-native line endings per `.editorconfig`, a
  trailing newline, and no trailing whitespace.

## 3. Formatting

- **4 spaces** per indent level. No tabs.
- **Allman braces** — the opening brace goes on its own line for types,
  methods, properties, and control-flow blocks:

  ```csharp
  public SpecRef(string name, SpecVersion edition)
  {
      Name = SpecName.Normalize(name);
      Edition = edition;
  }
  ```

- **Always brace multi-line blocks.** A single-statement `if` whose body is on
  the *same line* is acceptable for terse guard clauses
  (`if (firstDot <= 0) return false;`), but once the body wraps to the next
  line it must be braced. Do not mix a braced and unbraced arm in the same
  `if`/`else`.
- One statement per line; one declaration per line.
- Keep lines reasonably short (roughly 100–120 columns). Prose in doc comments
  in this codebase wraps around 72–76 columns — match the surrounding file.
- Use a single blank line to separate members and logical groups; never stack
  multiple consecutive blank lines.
- Use **expression-bodied members** for one-liners where they read well
  (`public override string ToString() => $"{Name}/{Edition}";`). Use a block
  body when the logic spans multiple statements.

## 4. Naming

| Element | Convention | Example |
|---|---|---|
| Namespace, type, method, property, event, enum member | `PascalCase` | `CoveragePipeline`, `TryParse` |
| Interface | `PascalCase` prefixed with `I` | `IAssetSource` |
| Type parameter | `PascalCase` prefixed with `T` | `TModel`, `TKey` |
| Local variable, local constant, parameter | `camelCase` | `versionPart`, `tolerance`, `edition` |
| Private / internal field (instance or mutable static) | `_camelCase` | `_assetSource`, `_tileWorkerCount` |
| Constant, `static readonly` | `PascalCase` | `MaxDepthBands`, `Sampling` |
| Async method | `PascalCase` suffixed `Async` | `OpenFeatureCatalogueAsync` |

- The `s_`/`t_` prefixes (the dotnet/runtime convention for static and
  thread-static fields) are **not** used here: a private field is `_camelCase`
  regardless of whether it is instance or static, and a `static readonly` /
  `const` field is `PascalCase`. When a `static readonly` field backs a
  same-named property, method, or type, give the field a distinct descriptive
  `PascalCase` name (for example, `LazyDefault` backing `Default`).
- Do **not** prefix with `this.` to disambiguate fields — the `_` field prefix
  already makes fields visually distinct, and `this.` is effectively unused in
  the codebase.
- Prefer descriptive names over abbreviations, except well-known domain terms
  (`Fc`, `Pc`, `Crs`, `Utm`, S-100 attribute acronyms). Spec-derived
  identifiers should match the spec's casing where practical and cite the
  section (see §8).

## 5. Language features and idioms

- **`var`** is the default for local declarations — it is used pervasively.
  Reach for an explicit type only when it materially improves readability or
  the right-hand side does not make the type obvious.
- Use **target-typed `new`** where the type is already stated
  (`SpecRef value = new(name, edition);` / `= []` for an empty collection).
- Prefer **collection expressions** (`[]`, `[a, b, c]`) and range/index
  operators (`s[..sep]`, `afterPrefix[(firstDot + 1)..]`) — both are used
  throughout.
- Prefer **pattern matching** and switch expressions over long `if`/`else if`
  ladders and `switch` statements where they read more clearly.
- Prefer **string interpolation** (`$"..."`) over concatenation. Pass an
  explicit `StringComparison` to string comparisons/`StartsWith`/`IndexOf`
  where culture matters (`StringComparison.OrdinalIgnoreCase` for
  identifiers/tokens).
- Use `is null` / `is not null` for reference equality checks.

## 6. Nullability and argument validation

- Never use the null-forgiving operator `!` to silence the analyzer. If a value
  is genuinely never null, express that in the type; if it can be null, handle
  it. `!` is reserved for the rare, commented case where an invariant cannot be
  encoded.
- Validate public-entry-point arguments up front:

  ```csharp
  ArgumentNullException.ThrowIfNull(source);
  ArgumentException.ThrowIfNullOrWhiteSpace(name);
  ```

- Follow the `Parse` / `TryParse` pair pattern for parsing: `TryParse` returns
  `bool` and never throws on malformed input; `Parse` delegates to it and
  throws `FormatException` (see `SpecRef` in EncDotNet.S100).
- Throw the most specific standard exception (`ArgumentException`,
  `ArgumentOutOfRangeException`, `InvalidOperationException`,
  `FormatException`, `NotSupportedException`) with a message that names the
  offending value.

## 7. Documentation comments

- **Every public and protected member carries XML doc comments** — at minimum
  `<summary>`, plus `<param>`, `<returns>`, and `<exception>` where
  applicable. This is required, not optional.
- Use `<see cref="..."/>` for cross-references, `<c>...</c>` for inline code,
  and `<remarks>`/`<para>` for rationale. Explain *why*, not just *what*, for
  non-obvious design choices (see `SpecRef` in EncDotNet.S100 for the house
  style).
- Internal/private members are documented when the intent is non-obvious.
- **Implementation comments are for clarification only.** Do not narrate code
  that speaks for itself. Prefer a comment that captures a non-obvious
  invariant, a spec reference, or a "do not change this without …" warning.

## 8. Spec-derived code

- S-100 product semantics (feature catalogues, portrayal, encodings) belong
  in EncDotNet.S100, not in the add-in. When the add-in needs a constant,
  attribute name or rule from an IHO S-100 product specification, prefer
  the EncDotNet.S100 API that exposes it. If you must define it here, cite
  the spec and section in the XML doc comment (e.g. `S-104 §10.2.3
  WaterLevel attribute names`).
- Consult EncDotNet.S100's per-product skills before writing spec-semantic
  code.

## 9. Async

- Suffix async methods with `Async` and return `Task`/`Task<T>`/`ValueTask<T>`.
- Accept and honor a `CancellationToken` on async APIs that do I/O; flow it
  through to the calls you make.
- Do not expose `async void` except for event handlers.
- Avoid `.Result` / `.Wait()` / `.GetAwaiter().GetResult()` — they invite
  deadlocks. Make the call chain async instead.

## 10. Dependencies and build

- NuGet versions are managed centrally in `Directory.Packages.props` via
  Central Package Management. Do **not** add `Version` attributes to individual
  `.csproj` files.
- Do not add `#pragma warning disable` or `<NoWarn>` to work around analyzer
  findings without a comment justifying the specific, local reason.
- The add-in targets `net10.0-windows` and `win-x64` only, because ArcGIS Pro
  is a Windows x64 process. Windows APIs are fine in the add-in; keep logic
  you want to test free of the Pro API (see §11).

## 11. ArcGIS Pro add-in code

- **Namespaces start `EncDotNet.SoundCharts`**, not
  `EncDotNet.ArcGIS.SoundCharts`. Inside a namespace with an `ArcGIS`
  segment, `ArcGIS.Desktop.*` and `ArcGIS.Core.*` resolve against that
  segment and stop compiling, and the WPF code generator doesn't qualify
  them with `global::`.
- **Pro-facing classes are thin.** Modules, buttons, tools, panes and their
  view models call into plain classes that don't use the Pro API, so the
  logic is testable without Pro.
- Classes that Pro creates from `Config.daml` (modules, buttons, panes) are
  `internal sealed`.
- Run Pro API calls that touch maps, layers, geometry or the geodatabase on
  the Main CIM Thread with `QueuedTask.Run`, and await it; never block the UI
  thread on it.
- Every UI string is localizable: window and pane text lives in
  `Resources/Strings.resx` with a matching `Strings` property, and ribbon
  text in `Config.daml`.
