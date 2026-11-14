# Documentation style guide

This guide covers how to write the EncDotNet.ArcGIS documentation: the pages
under `docs/`, the repository's root Markdown files (`README.md`,
`CONTRIBUTING.md` and the others) and project READMEs. It is carried over
from
[EncDotNet.S100's documentation style guide](https://github.com/philliphoff/EncDotNet.S100/blob/main/docs/docs-style.md),
so the two projects read alike. The differences are the names, the UI text
sources and the build check, below.

The [repository README](../README.md) and [CONTRIBUTING.md](../CONTRIBUTING.md)
are the reference examples. When this guide doesn't answer a question, do
what they do, then what EncDotNet.S100's docs do.

## Principles

1. **Write for someone with a task.** Readers arrive wanting to install the
   add-in, add a chart to a map, check a route or build the code. Tell them
   how, in the order they'll do it.
2. **Describe; don't promote.** Say what a feature does and when to use it.
   Don't say how good, fast, easy or complete it is.
3. **Everything you show must work.** Commands run in the order given, code
   compiles against the current API, and UI labels match the add-in.
4. **Say it once.** Each fact has one home. Other pages link to it instead of
   restating it. Facts about EncDotNet.S100 (products, portrayal, the `s100`
   tool) live in its docs; link to them.

## Page types

Every page is one of these types. Choose the type before you write, and use its
structure.

### Quickstart

Gets a new reader from nothing to one working result, such as a chart on a
map in Pro.

1. One or two sentences on what the reader will do.
2. **Prerequisites**: what to install or have first. Omit if there are none.
3. Numbered or titled steps that run in order. Show the expected result after
   the step that produces it.
4. **Next steps**: two to five links, each with a few words on what it covers.

Leave options, variations and background to the guides. A quickstart takes
the shortest path that works.

### How-to guide

Solves one specific task for a reader who already has the basics.

- Title it with the task, as an imperative: "Add an S-101 chart to a map".
- Start with a sentence on what the result is and when you'd want it.
- Prerequisites, then numbered steps, then the result.
- Put alternatives and edge cases at the end, under headings that name them.

### Guide

Explains one area of the add-in in depth.

- Open with a short paragraph on what the area covers.
- Organize sections around what the reader wants to do, such as "Change the
  palette" or "Check a route". Don't organize around the code's structure.
- Each section gives a short explanation, an example, and any caveats.

### Reference

Lists everything in a surface area, for lookup: ribbon commands, Options
settings, geoprocessing tool parameters.

- Make it complete and consistently ordered. Tables suit settings, commands
  and parameters.
- Keep explanation short and link to guides for anything longer.

### Design note

Records why a subsystem works the way it does, for contributors. Lives in
`docs/design/`.

- Cover the problem, the constraints, the decision and the alternatives you
  rejected.
- Design notes may discuss internals; the other page types shouldn't.

### Project README

Describes one project in `src/<project>/README.md`, when it needs more than
the repository README says.

- First paragraph: what the project does.
- Then: how to build or use it, its main entry points, and links to the
  relevant guides.

## Headings

- Use one `#` heading per page.
- Use sentence case: "Add a chart to a map", not "Add A Chart To A Map".
- Make headings describe their content. Task sections use imperatives
  ("Install", "Add a chart"); concept sections use nouns ("Supported
  products").
- Don't skip levels (`##` then `####`).
- Don't use template headings that describe the reader's journey instead of
  the content: "Why it matters", "Quick win", "Deep dive", "Overview" as a
  catch-all, or a closing "Troubleshooting" section that holds general tips.
  Put a real known problem next to the step it affects.
- Heading text becomes the link anchor (`## Running the add-in in Pro`
  becomes `#running-the-add-in-in-pro`). Search for links before you rename a
  heading.

## Voice and tone

- Address the reader as "you". Use present tense and active voice: "The
  add-in starts the chart server", not "The chart server will be started".
- Write steps as imperatives: "Run", "Open", "Choose".
- Use contractions ("don't", "isn't", "you'll"). They read as plain speech.
- Keep sentences short. One idea per sentence; one topic per paragraph.
- Don't use exclamation marks, emoji or badges (the CI status badge in the
  README is the exception).
- Don't claim speed or ease ("in minutes", "a few lines", "simply", "just",
  "easy"). If something is short, the reader will see that.
- Avoid marketing and filler words, including: *seamless*, *powerful*,
  *robust*, *blazing*, *best-in-class*, *world-class*, *cutting-edge*,
  *batteries-included*, *out of the box*, *on-ramp*, *leverage*, *unlock*,
  *get to value*, *deep dive*.
- Don't use internal language like issue numbers, slice letters or sprint
  names in reader-facing text. Issue links are fine in design notes and in
  caveats that point to a tracked limitation.

## Names and terms

| Write | Not |
|---|---|
| SoundCharts for ArcGIS (the add-in, in full); SoundCharts on the ribbon and once named | the S-100 add-in, S-100 for ArcGIS |
| ArcGIS Pro, then Pro | ArcMap, ArcGIS Desktop, ArcGIS (alone, for Pro) |
| add-in, `.esriAddinX` | addin, plug-in, extension (Pro uses "extension" for licensed products) |
| SoundCharts (the EncDotNet.S100 desktop app), named as "the SoundCharts app" when both could be meant | the viewer app |
| EncDotNet.S100 (the project and libraries) | EncDotNet, the engine |
| `s100` (the command-line tool) | the CLI app, S100 |
| S-101, S-102, S-100 Part 10b | S101, S 101, part 10B |
| exchange set, dataset, feature catalogue, portrayal catalogue | exchangeset, data set, FC/PC (spell out on first use per page) |
| Windows, Arm64, x64 | win, ARM, AMD64 |

Never call the add-in's ribbon tab, panes or tools "S-100": Pro's own ENC
support uses that name.

## Steps and code

- Use a numbered list for a sequence of actions in one place. Use `###`
  headings for steps that each need a code block or more than a short
  paragraph.
- Use one action per step. After a step with a visible result, say what the
  reader should see.
- Tag every code fence with its language: `powershell`, `bash`, `csharp`,
  `xml`, `json`, `text` for output.
- Use one command per line and no prompt. Keep command output in a separate
  `text` block.
- Use placeholders a reader can recognize and replace: `path\to\dataset.000`,
  `<version>`. Don't use fake values that look real.
- C# examples must compile against the current API. Copy them from the code
  or a test where possible, and keep them as short as the point allows.

## UI text

- Bold UI labels and copy them exactly from the add-in: ribbon captions and
  tooltips from `src/EncDotNet.ArcGIS.SoundCharts/Config.daml`, and window and
  pane text from
  `src/EncDotNet.ArcGIS.SoundCharts/Resources/Strings.resx`, including
  ellipses.
- Write Pro's own labels as Pro shows them, and ribbon paths as
  **SoundCharts** tab > **Help** group > **About SoundCharts**, or in short,
  "On the **SoundCharts** tab, choose **About SoundCharts**".
- Describe the action, not the widget: "Choose **About SoundCharts**", not
  "Click the About SoundCharts button".

## Links

- Use relative links between pages and to repository files, so they resolve
  on GitHub and in a clone: `[CONTRIBUTING.md](../CONTRIBUTING.md)`.
- Use absolute URLs for EncDotNet.S100 pages and files.
- Make link text say where the link goes: "see
  [CONTRIBUTING.md](../CONTRIBUTING.md)", not "see [here](../CONTRIBUTING.md)".
- Link to a page's own topic once, where the reader needs it. Don't repeat the
  same set of links at the top and bottom of a page.

## Alerts, tables and images

- Use GitHub alerts (`> [!NOTE]`, `[!TIP]`, `[!IMPORTANT]`, `[!WARNING]`,
  `[!CAUTION]`) only for information the reader must not miss, placed next to
  the step it affects. Don't open a page with one or use one for decoration.
- Use a table to compare items across the same attributes. Use a list for
  everything else.
- Add a screenshot only when it shows something text can't, such as a chart
  in a map or a pane that is hard to describe. Give it alt text that says what
  it shows.
- Use Mermaid diagrams for flows and relationships. Keep them to one idea.

## Check your page

Before you open a pull request:

1. Preview the page on GitHub (or in an editor that renders GitHub Markdown)
   and read it from top to bottom as a new reader. Check every link and
   anchor.
2. Run each command and code example, or confirm it against the code or a
   test that does.
3. Check UI labels against `Config.daml` and `Strings.resx`, and API names
   against the source.
4. Search the page for the words in [Voice and tone](#voice-and-tone).
