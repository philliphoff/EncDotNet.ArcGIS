---
name: docs-writing
description: |
  Writing and reviewing the EncDotNet.ArcGIS documentation — pages under
  docs/, the root Markdown files (README, CONTRIBUTING, SUPPORT, SECURITY)
  and project READMEs — to the repository's documentation style guide
  (docs/docs-style.md): page types and their section structure, plain
  task-focused tone with no marketing language, runnable steps, exact UI
  labels from Config.daml and Strings.resx, and links that work on GitHub.
  USE FOR: adding or rewriting a docs page or README; restructuring
  sections; reviewing a docs change for structure, tone or accuracy.
  DO NOT USE FOR: XML doc comments (follow docs/coding-style.md); UI strings
  themselves (see the arcgis-pro-addin skill); EncDotNet.S100's docs (edit
  them in that repository).
---

# Writing EncDotNet.ArcGIS docs

The normative rules are in
[`docs/docs-style.md`](../../../docs/docs-style.md). Read it in full before
writing. This skill adds the working procedure and a review checklist.

The reference examples are the repository [`README.md`](../../../README.md)
and [`CONTRIBUTING.md`](../../../CONTRIBUTING.md). When unsure, match them,
then EncDotNet.S100's docs.

## Procedure

1. **Pick the page type** (quickstart, how-to guide, guide, reference, design
   note, project README) and use its structure from the style guide. If a page
   mixes types, split it or move material to the page that owns it.
2. **Gather facts from the code, not from other docs.** Other pages may be
   stale.
   - Ribbon captions, tooltips and ids: `src/EncDotNet.ArcGIS.SoundCharts/Config.daml`.
   - Window and pane text: `src/EncDotNet.ArcGIS.SoundCharts/Resources/Strings.resx`.
   - Build and CI steps: `.github/workflows/ci.yml`, `Directory.Build.props`,
     `src/Directory.Build.targets`.
   - Install paths: `tools/deploy-addin.ps1`.
   - `s100` commands and options: `s100 --skill`.
3. **Check inbound links before renaming or removing anything.** Search the
   repository for the file name and `#anchor`, including `README.md`,
   `CONTRIBUTING.md` and `.github/`.
4. **Write**, following the style guide. Prefer linking to an existing page,
   or to EncDotNet.S100's docs, over restating it.
5. **Review** with the checklist below, then preview the page on GitHub.

## Review checklist

- [ ] The page has one `#` title.
- [ ] Sections follow the structure for the page type.
- [ ] No template headings: "Why it matters", "Quick win", "Deep dive", or a
      catch-all "Troubleshooting".
- [ ] No marketing or filler words, emoji, badges or time claims. Quick scan:

      ```bash
      grep -n -i -E "seamless|powerful|robust|blazing|best-in-class|world-class|cutting-edge|batteries-included|out of the box|on-ramp|leverage|unlock|in minutes|simply|easily|deep dive|quick win|why it matters" path/to/page.md
      ```

- [ ] Steps run in the order given. Nothing is used before the step that
      installs or creates it.
- [ ] Code fences have a language tag. Commands have no prompt, and output is
      in a separate block.
- [ ] UI labels are bold and match `Config.daml` or `Strings.resx` exactly.
      The add-in is SoundCharts, never "S-100".
- [ ] Links are relative within the repository and absolute for
      EncDotNet.S100. Link text names the target.
- [ ] Alerts are only used for must-not-miss information, next to the step
      they affect.
- [ ] Renamed headings or removed pages have no remaining inbound links.
