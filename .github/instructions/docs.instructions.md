---
applyTo: "docs/**/*.md,src/**/README.md,*.md"
---

# Documentation editing rules

When adding or editing documentation, follow
[`docs/docs-style.md`](../../docs/docs-style.md). For a new page or a
substantial rewrite, load the `docs-writing` skill. In particular:

- Choose the page type first (quickstart, how-to guide, guide, reference,
  design note or project README) and use its section structure.
- Describe what things do. Don't promote them: no marketing words, emoji,
  badges or claims about speed or ease.
- Don't use the "Why it matters / Quick win / Deep dive / Troubleshooting /
  Next step" template headings.
- Every command and code example must work in the order shown. Copy UI
  labels exactly from `src/EncDotNet.ArcGIS.SoundCharts/Config.daml` and
  `src/EncDotNet.ArcGIS.SoundCharts/Resources/Strings.resx`.
- Call the add-in SoundCharts, never "S-100", in UI references.
