# SoundCharts for ArcGIS Pro: look and feel proposal

Status: **Accepted** (2026-11-16). The owner's decisions on the open questions are in §10;
where they differ from the body or the mockups, §10 wins.
Mockups are in [`images/look-and-feel-mockups.html`](images/look-and-feel-mockups.html). It has a Pro Light / Pro Dark toggle and a
Proposed / Current toggle, and shows every pane and dialog at a docked width of 400 px.

Scope: `LibraryPaneView`, `ObjectInfoPaneView`, `RouteCheckPaneView`, `LogPaneView`,
`ScaleNoteView`, `ApplicationOptionsView`, `ProjectOptionsView`, `AboutWindow`,
`Export/ExportChartWindow` and `Config.daml` (ribbon, panes, images), all in
`src/EncDotNet.ArcGIS.SoundCharts/`. Wording stays bound by the brief
(`docs/design/soundcharts-for-arcgis-brief.md`, §7 and D34–D36). Where this proposal touches a
string, §10 records the decision.

---

## 1. Principles

1. **Pro first, SoundCharts second.** Chrome, controls, fonts, selection and focus come from Pro
   (`Esri_*` styles and brushes). The add-in adds only what Pro doesn't have: semantic tones,
   chips, the caution strip, and the viewer's information design.
2. **One system, written down once.** Every view merges one shared dictionary. No view defines
   its own `OptionalText` or `SectionHeading`, and no view sets a hex colour, font family or
   font size.
3. **Works in Light, Dark and High Contrast.** Every colour is a theme resource with all three
   variants. Severity is always a **shape plus a word** (octagon MUST REVIEW, triangle CHECK,
   info INFO, ⊗ Error). Colour is never the only cue.
4. **The viewer's information design, at Pro's density.** Keep the viewer's verdict card,
   finding groups, NOT FOR NAVIGATION strip, overline section headers and mono identifiers.
   Drop its rounded 8 px cards, shadows and 13–14 px body text. Pro is flatter (2–3 px radii),
   denser (24 px controls) and uses 12 px body text.
5. **Toolbars, not button walls.** A pane has a 32 px toolbar: one labelled primary action on
   the left, icon buttons and a burger (≡) menu on the right. Secondary verbs go in the menu.
6. **Every state is designed.** Empty, working, nothing found, failed and partial-data states
   each get their own visual. None reuses the heading text.
7. **Say "Not for navigation" once per surface, where the brief says to.** Use a footer line in
   the panes. Use the caution strip only when the producer marks the data (§7.1).

---

## 2. Audit: what's inconsistent today

Line numbers are from the merged `main`. Severity: **H** breaks a theme or misleads, **M**
visible inconsistency, **L** polish.

### 2.1 Theming and colour

| # | Sev | Finding | Where |
|---|---|---|---|
| A1 | H | Route check tones are hex literals: `#D62728`, `#F29900`, `#2CA02C`. In Pro Dark the red and green lose contrast against `#2E2E2E`, and in High Contrast they ignore the system palette. Colour bars are also the only severity cue: no glyph, no tone on the chip text. | `RouteCheckPaneView.xaml:305-321`, used at 377 and 471 |
| A2 | M | NOT FOR NAVIGATION is a plain 1 px `Esri_BorderBrush` box with bold text, in both Library and Object information. It looks like any other border and doesn't read as a caution (the viewer uses an amber strip). | `LibraryPaneView.xaml:216-226`, `ObjectInfoPaneView.xaml:124-145` |
| A3 | M | Hard-coded type: `FontFamily="Consolas"` on the badge, `FontSize="11"` on tags, `FontSize="14"` on the verdict, `FontSize="10"` on finding tags. The rest of each view uses Esri styles. | `LibraryPaneView.xaml:49,63`; `RouteCheckPaneView.xaml:382,484` |
| A4 | M | Resource keys that the published Pro style guide doesn't list: `Esri_Label` (Export), `Esri_TextErrorBrush`, `Esri_BorderBrush`. A missing `DynamicResource` fails silently: Export's `Label` falls back to WPF's default template and 5 px padding, so it sits off the TextBox baseline. `Esri_Button` and `Esri_ButtonBorderless` *are* documented. | `Export/ExportChartWindow.xaml:46,67,84`; `LibraryPaneView.xaml:75,195` and others |

### 2.2 Structure and duplication

| # | Sev | Finding | Where |
|---|---|---|---|
| A5 | M | `OptionalText` is defined three times with **different foregrounds** (Subdued in Library, Default in Object information and Route check). `SectionHeading` is defined twice. The comment explains why (BasedOn on an Esri style can't be static), but the result is drift. | `LibraryPaneView.xaml:23-40`; `ObjectInfoPaneView.xaml:23-40`; `RouteCheckPaneView.xaml:292-303` |
| A6 | H | Object information has one presentation for every state. "Finding features...", "Object information didn't finish", "Nothing at this point" and "No feature picked" all render as the same H5 heading at the top left, with no spinner, icon or tone. Route check does the same with `Message`. | `ObjectInfoPaneView.xaml:55-64` with `ObjectInfo/ObjectInfoPresenter.cs:15-96`; `RouteCheckPaneView.xaml:372-375` |
| A7 | M | Problems ("Couldn't read …", "Some data couldn't be read:") are unstyled body text at the bottom of the pane, with no icon or tone. Options validation problems use `Esri_TextErrorBrush` text, far from the fields they're about. | `ObjectInfoPaneView.xaml:258-267`; `RouteCheckPaneView.xaml:509-518`; `ProjectOptionsView.xaml:196-197`; `ApplicationOptionsView.xaml:83-84` |
| A8 | L | "Not for navigation" shows twice in the Library when the data is marked: a box in the details and an always-on footer. | `LibraryPaneView.xaml:216-226, 307-311` |

### 2.3 Spacing, sizing and alignment

| # | Sev | Finding | Where |
|---|---|---|---|
| A9 | M | Button gaps vary: 4 px (Library), 6 px (Route check), 8 px then **0** (Log: Copy all and Open Log Folder touch), and **0** in About, Export and the Options buttons. Dialog buttons have no `MinWidth`. | `LogPaneView.xaml:25-37`; `AboutWindow.xaml:60-73`; `Export/ExportChartWindow.xaml:104-115`; `ProjectOptionsView.xaml:199-208` |
| A10 | M | Pane margins vary: Log uses 4 px, every other pane 8 px. Section heading margins are `0,10,0,4`, card gaps 6 px, toolbar bottom 6 or 8 px. There is no scale. | `LogPaneView.xaml:17`; `LibraryPaneView.xaml:38,97,136`; `RouteCheckPaneView.xaml:334,362` |
| A11 | M | Label columns differ on one Options page: `MinWidth="160"` in two grids and `140` in the Route check grid. Input widths are 60, 80, 100 and a 180 px slider. Units are only in the section title ("Depths (metres)"), not beside the fields. | `ProjectOptionsView.xaml:25,85,175,69,114-139,50,60` |
| A12 | M | Library and Route check put 4–5 full text buttons each in a `WrapPanel`. At a 300–400 px dock they wrap into two or three ragged rows, and the Library details add five more. | `LibraryPaneView.xaml:97-133, 229-265`; `RouteCheckPaneView.xaml:334-359` |
| A13 | M | Every finding row repeats an "Object information" text button, which takes about 110 px of each row and adds noise to the list. | `RouteCheckPaneView.xaml:472-480` |
| A14 | L | Splitters are 5 px bars filled with `Esri_BorderBrush`, heavier than Pro's own pane splitters. | `LibraryPaneView.xaml:192-196`; `ObjectInfoPaneView.xaml:100-104` |
| A15 | M | Log: fixed column widths (140 / 60 / 110 / **800**) force horizontal scrolling at a normal bottom-dock width. Rows use the UI font (the viewer's logs-panel D1 makes them mono). Errors and warnings look the same as Info rows. | `LogPaneView.xaml:56-66` |
| A16 | L | Scale note: a `TextBlock` with `MinWidth 96`, `MaxWidth 160` and wrap. It leaves a blank 96 px gap in the ribbon when there's no note, and wraps to two lines when there is one. | `ScaleNoteView.xaml:15-19` |

### 2.4 Typography hierarchy

| # | Sev | Finding | Where |
|---|---|---|---|
| A17 | M | Heading levels are inconsistent. Options and dialogs use **H3** for page titles. Panes use **H5** for both the pick count ("3 FEATURES AT THIS POINT", which the viewer treats as an overline) and the selected feature's title. Route check's verdict is a hand-set 14 px SemiBold. | `ApplicationOptionsView.xaml:18,21`; `ObjectInfoPaneView.xaml:55,147`; `RouteCheckPaneView.xaml:363,379-383` |
| A18 | L | Italic is used for notes ("Needs a newer chart server…", the restart note). Pro uses italic for nothing in panes. These should be info message bars. | `ApplicationOptionsView.xaml:80-81`; `ProjectOptionsView.xaml:79-81` |
| A19 | M | Application Options: the section called **Palette** (`Settings_AppearanceSection`) also holds Time Format and Depth Unit. | `ApplicationOptionsView.xaml:21-53` |

### 2.5 Ribbon and icons (`Config.daml`)

| # | Sev | Finding | Where |
|---|---|---|---|
| A20 | M | Keytip **A** is used twice on the SoundCharts tab: Add SoundCharts Data and About SoundCharts. | `Config.daml:97,144` |
| A21 | M | Dusk, Night and Follow Pro theme all use `ColorSelector`, and only Day has a sun. Palette state can't be read from the ribbon. | `Config.daml:175-190` |
| A22 | M | Object information uses Pro's `Explore` icon, though D6 says it is explicitly *not* Explore. About and the Scale note share `GenericInformation`. Route check uses a generic check mark. | `Config.daml:136,145,150,161` |
| A23 | L | Mariner Settings... is a small button alone in a fourth column of the Display group. Pro's idiom for "more settings for this group" is the group dialog launcher. | `Config.daml:58-64` |
| A24 | L | Data group order is large, two middle-size, large. Help has two large buttons for low-frequency commands. The brief's Time group and SoundCharts Tools aren't on the main tab yet. | `Config.daml:50-55, 89-92` |
| A25 | L | String typography is mixed: `…` (Library) and `...` (Route check, ribbon); "Remove from **library**" next to "Library" everywhere else; Title Case commands ("Zoom to Data") next to sentence case ("Object information", "Route check"). These are wording questions (§9). | `Resources/Strings.resx` |
| A26 | M | About says only "Not for navigation.", but the brief (§7.1) specifies "SoundCharts for ArcGIS draws S-100 and S-57 data for analysis and planning. It isn't certified for navigation. Not for navigation." | `AboutWindow.xaml:55-58`, `About_NotForNavigation` |

---

## 3. The viewer's visual language and what translates

| Viewer element | Viewer treatment | In Pro |
|---|---|---|
| Palette | ShadUI (shadcn) tokens: `ForegroundColor`, `MutedColor`, `BorderColor`, `CardBackgroundColor`; accent `#007ACC` (user-set) | **Yield.** Use `Esri_TextStyleDefaultBrush`, `Esri_TextStyleSubduedBrush`, Pro borders and Pro's accent. No card backgrounds: Pro panes are one surface. |
| Semantic tones | `DestructiveColor`, `WarningColor`, `SuccessColor`; tint = tone at ~6% fill + 30% border, ink at full tone | **Keep**, as add-in brushes with Light, Dark and HC values (§4.3). Fill 6–11%, border 40%, ink full. |
| Verdict card | Rounded 8 px tinted card, tone glyph, 12.5 SemiBold title, summary rows with chips, confidence line with info glyph, footer | **Keep the anatomy.** Radius 3 px, 13 px title, rows 22 px. |
| Findings | Groups MUST REVIEW / CHECK / INFO as small caps overlines with count; octagon / triangle / waves / info glyphs; INFO folds | **Keep exactly.** Octagon path reused. Group header = SC_Overline + glyph + count. |
| Chips | 18–20 px, mono text for numbers, tone tint | **Keep.** 18 px, radius 2, mono for numeric chips. |
| NOT FOR NAVIGATION | Amber strip with warning glyph (Library, hard-coded `#FFE4B5`); small caps label in pick cards | **Keep, themed.** SC_Caution brushes; 11 px bold caps with 0.05 em tracking. |
| Section headers | 10.5–11.5 px SemiBold, ~55–60% opacity, caps | **Keep** as SC_Overline using `Esri_TextStyleSubduedBrush` (not opacity, which fails HC). |
| Mono | `MonoFontFamily` (SF Mono / Cascadia Mono / Consolas), 11 px | **Keep**: Cascadia Mono, Consolas. Use it for dataset names, ids, editions, versions, paths, log rows and numbers in chips. |
| Library tree | Kind tag (bordered mono), status dot, counts in secondary colour, accent bar on selection | Tag and counts kept. **Selection yields** to Pro's TreeView selection (no accent bar). |
| Segmented controls, pill tabs, toggle switches | ShadUI | **Yield** to Pro combos, check boxes and expanders. No segmented controls in Pro panes. |
| Logs panel (viewer D1–D8) | Mono rows, quiet Info, glyph gutter, error/warn row tints, count chips | **Keep**, at 11 px and with Pro's ListView header. |
| About | App icon, product name, version table, links | **Keep** the icon and name header and the mono versions. |

---

## 4. Design system

### 4.1 Spacing scale

`2 · 4 · 8 · 12 · 16 · 24` px. Nothing else.

- **8** is the pane gutter, the row padding (8 horizontal, 5–6 vertical), the gap between buttons, and the gap between a section's last row and the next header.
- **12** is the label-to-value gap and the top of a section header.
- **16** is dialog body padding and the gap between form groups.
- **4** is icon to text, and the gap between items in a toolbar.

Heights: toolbar 32; button, input and combo 24 (Pro's own); tree and list row 24 (single line);
verdict summary row 22; chip 18; icon 16 (12 inline, 32 on the ribbon).

### 4.2 Type ramp

Segoe UI throughout (Pro's font). Body is 12 px, as Pro's `Esri_TextBlockRegular`.

| Key | Size / weight | Brush | Use | Pro mapping |
|---|---|---|---|---|
| `SC_PageTitle` | 16 SemiBold | Default | Options page title, dialog heading | instead of `Esri_TextBlockH3` |
| `SC_Title` | 14 SemiBold | Default | Selected feature, dataset, route name | instead of `Esri_TextBlockH5` |
| `SC_VerdictTitle` | 13 SemiBold | Default | Verdict card title, Options expander header | — |
| `SC_RowTitle` | 12 SemiBold | Default | List row title | `Esri_TextBlockRegular` + SemiBold |
| `SC_Body` | 12 Regular | Default | Everything else | `Esri_TextBlockRegular` |
| `SC_Caption` | 11 Regular | Subdued | Meta lines, footer, help text | — |
| `SC_Overline` | 11 SemiBold, caps (from the string) | Subdued | Section and group headers, pick count | — |
| `SC_Mono` | 11.5 Regular, Cascadia Mono / Consolas | Default or Subdued | Ids, editions, versions, paths, positions | — |

Why explicit sizes instead of `Esri_TextBlockH*`: Pro's H-styles are sized for Backstage and
dialogs, and a style can't be `BasedOn` an Esri style that is only reachable dynamically. The
SC styles set only Foreground (as `DynamicResource` to Esri brushes), FontSize and FontWeight.

### 4.3 Brushes (theme dictionaries)

Proposed files are `Themes/SoundCharts.Colors.Light.xaml`, `.Dark.xaml` and
`.HighContrast.xaml`. The values below are what the mockup uses; check them in Pro before
locking them in. Ink colours meet 4.5:1 on Pro's pane background.

| Key | Light | Dark | High Contrast |
|---|---|---|---|
| `SC_DangerBrush` | `#C8291C` | `#FF6B57` | `SystemColors.WindowTextBrush` |
| `SC_WarningBrush` (ink) | `#B36200` | `#FFB938` | `SystemColors.WindowTextBrush` |
| `SC_SuccessBrush` | `#23793A` | `#58C771` | `SystemColors.WindowTextBrush` |
| `SC_InfoBrush` | Pro accent (`#007AC2`) | Pro accent (`#2EA8F5`-ish) | `SystemColors.HotTrackBrush` |
| `SC_NeutralBrush` | → `Esri_TextStyleSubduedBrush` | same | `SystemColors.GrayTextBrush` |
| `SC_*FillBrush` | tone at 11% (chips), 6% (cards, bars) | tone at 11% / 6% | Transparent |
| `SC_*EdgeBrush` | tone at 40% | tone at 40% | `SystemColors.WindowTextBrush` |
| `SC_CautionBackgroundBrush` | `#FFF3D1` | `#3A2F12` | `SystemColors.WindowBrush` |
| `SC_CautionBorderBrush` | `#D9A21B` | `#9A7A21` | `SystemColors.WindowTextBrush` |
| `SC_CautionInkBrush` | `#6A4700` | `#FFD670` | `SystemColors.WindowTextBrush` |

**Choosing the dictionary.** Read `FrameworkApplication.ApplicationTheme` (Default, Dark,
HighContrast) when the module loads. Changing Pro's theme needs a Pro restart, as far as I know,
so a load-time choice is enough; verify this in 3.7. Each view merges one
`<sc:SoundChartsTheme />` (a `ResourceDictionary` subclass whose constructor merges
`SoundCharts.xaml` plus the right Colors file). This replaces the per-view
`DesignOnlyResourceDictionary` plus local styles, and makes `BasedOn="{StaticResource SC_…}"`
legal.

**High Contrast rule.** In HC, chips, cards and strips draw no fill. They get a 1 px
`WindowText` border, and the glyph carries the severity. Don't use opacity for "subdued": HC
ignores it, so use `Esri_TextStyleSubduedBrush`.

### 4.4 Shared styles (`Themes/SoundCharts.xaml`)

| Key | Target | Intent |
|---|---|---|
| `SC_PaneToolbar` | `Border`/`DockPanel` | 32 px strip, 1 px bottom border, padding `4,0` |
| `SC_ToolbarButton` | `Button` | Borderless, icon + text, 24 px, padding `5,0`; hover = `Esri_BackgroundHoverSelectedBrush` |
| `SC_IconButton` | `Button` | 24×24, 16 px glyph, borderless; requires `ToolTip` + `AutomationProperties.Name` |
| `SC_BurgerButton` | `Button` + `ContextMenu` | ≡ glyph at the toolbar's right end; Pro's `BurgerButton` control if available in 3.7 |
| `SC_Overline`, `SC_Title`, `SC_RowTitle`, `SC_Body`, `SC_Caption`, `SC_Mono`, `SC_PageTitle` | `TextBlock` | Type ramp (§4.2). `SC_OptionalText` = `SC_Body` + collapse when empty. **One definition.** |
| `SC_SectionHeader` | `ContentControl` | Overline + optional glyph and count, margin `8,12,8,4` |
| `SC_FieldGrid` | `ItemsControl` template | Key/value grid, shared label column, label Subdued, value = read-only selectable TextBox with no chrome (today's trick, one copy) |
| `SC_Chip` + `sc:Tone.Value` attached | `Border` | 18 px, radius 2, tone fill/edge/ink; `Mono` variant |
| `SC_ProductTag` | `Border` | Mono 10.5 px bordered tag (`S-101`, `S-104`, `ExS`, `Dir`), min width 40 |
| `SC_MessageBar` + Tone | `Border` | 3 px left tone bar, tone glyph, text; Info, Warning, Error. Pro's `MessageLabel` if it fits (verify) |
| `SC_CautionStrip` | `Border` | NOT FOR NAVIGATION, with its tooltip |
| `SC_VerdictCard` + Tone | `Border` | Tone 6% fill, 40% edge, radius 3, padding `10,9` |
| `SC_SummaryRow` | `Button` | 22 px row: 72 px label, text, chip; hover tint |
| `SC_FindingRow` | `ListBoxItem` template | Glyph, title + meta, tag chip, Object information icon on hover or selection |
| `SC_EmptyState` | `ContentControl` | 40 px tinted glyph disc, 14 SemiBold title, Subdued body (max 300 px), actions |
| `SC_BusyState` | `ContentControl` | Pro `CircularAnimationControl` (verify) + caption |
| `SC_PaneFooter` | `Border` | 1 px top rule, info glyph, 11 px Subdued text |
| `SC_Splitter` | `GridSplitter` | 7 px hit area, 1 px line, centre grip |
| `SC_DialogFooter` | `Border` | 1 px top rule, padding `16,12`, right-aligned `StackPanel` with 8 px gaps; buttons `MinWidth=80`, default first |
| `SC_FormGrid` | `Grid` | One label column per page (150 px dialogs, 170 px Options), 8 px row gap, help text under the field |
| `SC_UnitSuffix` | `TextBox` template | Value + right-aligned "m" / "MB" inside the box |

### 4.5 Iconography

- Pane and toolbar glyphs: 16 px, Calcite-style 1 px outline, monochrome
  `Esri_TextStyleDefaultBrush`. Use XAML `DrawingImage` (theme-aware via brush binding) rather
  than PNGs, so Dark and HC need no second set.
- Ribbon: 16 and 32 px images. Where Pro has a fitting stock icon, use it (`ZoomFullExtent`,
  `GenericExport`, `DiagnosticsMonitor`, `CatalogShowTree`). Commission five custom ones: Add
  SoundCharts Data (chart + plus), Object information (cursor + i), Route check (route + check),
  and Palette Day, Dusk, Night and Follow Pro theme (sun, horizon, moon, half-disc).
- No two commands share an image. The split button shows the active palette's icon.
- Severity glyphs: octagon-! (MUST REVIEW, Error), triangle-! (CHECK, Warning), circle-i
  (INFO, Info), circle-✓ (meets, valid), circle-✕ (failed).

### 4.6 Component patterns (see the mockup's last section)

- **Pane toolbar:** labelled primary action (with ▾ if it is a menu) on the left; icon buttons and ≡ on the right.
- **Search:** Pro's `SearchTextBox` with placeholder (verify 3.7), replacing the overlay-TextBlock watermark.
- **Section header:** overline caps + count; foldable variant with a 12 px chevron.
- **List row:** glyph, two lines (row title, caption), trailing chip or action; Pro selection colours.
- **Status chip:** tone chip; numeric chips are mono.
- **Message bar:** Info, Warning or Error, inline at the place it concerns.
- **Empty state:** glyph disc, title, body, actions.
- **Footer disclaimer:** "Not for navigation …" with an info glyph.
- **Dialog footer:** right-aligned, 8 px gaps, min 80 px, default first; Copy-type secondary actions at the far left.

---

## 5. Ribbon recommendations (`Config.daml`)

1. **Data:** Add SoundCharts Data becomes a large **split button** (click = files; menu = Add
   Folder, Add as Feature Layers), followed by **Library** (large). The group is then two large
   buttons. Alternative if a split button is too much: keep Add Folder and Add as Feature
   Layers as small buttons stacked *after* Library.
2. **Display:** Palette split button (large, icon = active palette); a stack of Display
   category, Soundings and Text; **Mariner Settings... becomes the group's dialog launcher**
   (`launcherButtonID="SoundCharts_MarinerSettingsButton"` on the group). Its tooltip keeps the
   caption.
3. **Time** (brief §2.1, not yet on the main tab): Now (large), Previous data and Next data (small).
4. **Inquire:** Object information (tool, large, own icon), Route check (large, own icon).
5. **Output:** Export SoundCharts Chart... (large), SoundCharts Tools (small, when A9 lands).
6. **Help:** Log and About SoundCharts as **small** stacked buttons. Fix keytips: About → `O`
   or `B`, so it no longer clashes with Add Data's `A`.
7. **SoundCharts Layer contextual tab:** status labels (scale note, time status, server status)
   use one custom-control template: a 16 px glyph or 8 px status dot in tone + one line of text,
   max 220 px, ellipsis, full text in the tooltip. The scale note's idle state is an open question (§9).

---

## 6. Per-view redesign notes

### Library (`LibraryPaneView.xaml`)
- Replace both WrapPanels with **toolbar** (`Add ▾` menu: Add a folder…, Add an exchange set…,
  Add a collection manifest…; right: Refresh, Remove from library, ≡) and a **details header**
  (Add to Map, Show Footprint, ≡: Add as Feature Layers, Validate, Open Containing Folder).
  Keep every AutomationId, moving the ids onto the menu items.
- Search: `SearchTextBox`, 8 px gutter.
- Tree rows: 24 px; `SC_ProductTag` + name + right-aligned count or edition in Subdued mono;
  "Missing" becomes a warning chip.
- Splitter → `SC_Splitter`.
- Details: `SC_ProductTag` + `SC_Title` (mono name), product line, actions, caution strip (only
  if marked), then Product / Validation / Time / Source groups with `SC_SectionHeader` +
  `SC_FieldGrid`. Validation status becomes a tone chip (Valid → success, Not valid → danger,
  Validating… → spinner). Compatibility gets a tone chip (Supported, Partly supported → warning,
  Not supported → danger).
- Footer: `SC_PaneFooter` "Not for navigation". Remove the duplicate box text.
- Empty: `SC_EmptyState` with the three Add buttons and a dashed drop zone that turns accent on drag-over.

### Object information (`ObjectInfoPaneView.xaml`, `ObjectInfoPaneViewModel.cs`)
- Expose a `State` (Empty, Working, NoLayers, NothingHere, Failed, Results) from the presenter,
  which already distinguishes them, and switch templates on it.
- Header strip: `SC_Overline` count, position in `SC_Mono`, time note with a clock glyph (moved
  up from the footer, because it qualifies the values), copy-position icon, ≡.
- List grouped by dataset: group header = product tag + dataset name. Rows = glyph by kind
  (feature, coverage, warning) + title + caption + data-kind chip (Observed, Predicted,
  Analysis, Hindcast, Forecast per D35; Forecast = info tone, the others neutral) + "In force at
  map time" chip for S-124.
- Details: caution strip first (with the existing tooltip), `SC_Title`, Subdued mono source/id
  line, ATTRIBUTES and REFERENCES as `SC_SectionHeader` + `SC_FieldGrid` (nested attributes
  indent 12 px; colour values get a swatch).
- Problems → warning `SC_MessageBar`. Failed → error message bar.

### Route check (`RouteCheckPaneView.xaml`)
- Toolbar: Check Selected Line, Draw Route and Open Route... as `SC_ToolbarButton` (icon + text);
  Check Again as an icon button at the right. This fits on one line at 400 px; below about
  360 px, move Open Route... into ≡.
- Route name `SC_Title`, parameters `SC_Caption`.
- `SC_VerdictCard` with tone from `Tone` (no hex), a tone glyph, `SC_VerdictTitle`, subtitle,
  `SC_SummaryRow`s with tone chips (the chip tone needs a `ChipTone` on the row model), and the
  confidence line with an info glyph.
- Findings: section headers with glyph + count; INFO folds (keep the existing rule). Rows:
  `SC_FindingRow`; the Object information action becomes an icon button, visible on hover,
  selection and keyboard focus.
- Working: the card shape with a spinner. Message → info `SC_MessageBar`. Problems → warning message bar.
- Footer: `SC_PaneFooter` with the viewer's footer text, word for word.

### Log (`LogPaneView.xaml`)
- Margin 8 → toolbar 32 px: level combo (180 px), optional error and warning count chips, the
  source filter as a dismissible info chip (replacing the "Showing … only" row + Show all), then
  Copy all and Open Log Folder as toolbar buttons on the right.
- Columns: Time 120, Level 70 (glyph + short label), Source 150, Message `*` (a `GridView`
  can't star-size; use a converter on ActualWidth, or a ListView with a Grid row template).
- Rows in `SC_Mono` 11 px; Info level text Subdued; Debug rows at reduced emphasis (Subdued brush,
  not opacity); error and warning rows tinted at 7% and 5%.

### Scale note (`ScaleNoteView.xaml`)
- Glyph + single-line text, max 220 px, ellipsis, tooltip with the full text, `LiveSetting`
  kept. No MinWidth: collapse the text when empty and show the dimmed glyph only.

### Options (`ProjectOptionsView.xaml`, `ApplicationOptionsView.xaml`)
- `SC_PageTitle` + Subdued scope note (one paragraph).
- Sections as Pro-style expanders, all open by default. Project: Display, Depths (metres), Route
  check. Application: Palette, *Time and units* (name TBD), Chart server.
- One `SC_FormGrid` label column (170 px) across the whole page; inputs 96 px with unit suffix;
  sliders 170 px with mono value.
- "Needs a newer chart server" → info message bar at the top of the sections it affects, plus
  the same text as tooltip on each disabled control. Restart note → info message bar in Chart
  server.
- Problems → error message bar directly under the field group it concerns. Highlight the field
  border in danger (`Validation.ErrorTemplate`).
- Reset to Defaults and Use for New Projects 8 px apart; the confirmation note as a success
  caption with a check glyph.
- Add a **Browse...** next to the s100 program path (new; open question).

### Export (`Export/ExportChartWindow.xaml`)
- Width 500; `SC_PageTitle` heading; summary with a layers glyph; `SC_FormGrid` (80 px labels)
  using `TextBlock` + `Target`-less access keys, or a `Label` with an explicit zero-padding
  style; the path in mono; the note as an info message bar; `SC_DialogFooter` (Export default,
  Cancel).

### About (`AboutWindow.xaml`)
- Header: SoundCharts app icon (from the viewer's `Branding/icon.svg`, 52 px) + "SoundCharts for
  ArcGIS Pro" + a one-line subtitle (open question). Versions in `SC_FieldGrid` with mono
  values. The brief's full not-for-navigation sentence in a neutral message bar.
  `SC_DialogFooter` with Copy at the left and Close (default) at the right.

---

## 7. Phased implementation plan

| Phase | What | Files | Risk |
|---|---|---|---|
| **0. Verify and scaffold** (½–1 day) | In Pro 3.7 Light, Dark and HC, check these keys and controls: `Esri_Button`, `Esri_ButtonBorderless`, `Esri_TextErrorBrush`, `Esri_BorderBrush`, `Esri_BackgroundHoverSelectedBrush`, `SearchTextBox`, `MessageLabel`, `CircularAnimationControl`, `BurgerButton`; and whether a theme change needs a restart. Add `Themes/SoundCharts.xaml`, `Themes/SoundCharts.Colors.{Light,Dark,HighContrast}.xaml` and `SoundChartsTheme` (ResourceDictionary subclass). | new `Themes/`, `SoundChartsModule.cs` | Low |
| **1. Consistency sweep** (1–2 days) | Merge `SoundChartsTheme` in every view; delete the local `OptionalText`/`SectionHeading`/`ToneBar`; apply the type ramp; 8 px margins; 8 px button gaps and `MinWidth=80`; replace `Esri_Label`; Route check tones → SC brushes; remove hard-coded fonts and sizes; fix the Log margin; Scale note. No layout restructuring. | all 9 `.xaml` views | Low: visual only, AutomationIds untouched |
| **2. States and messages** (2 days) | `State` on the Object information and Route check view models; `SC_EmptyState`, `SC_BusyState`, `SC_MessageBar`, `SC_CautionStrip`, `SC_PaneFooter`; problems and validation as message bars; About wording (if approved). | `ObjectInfoPaneView*`, `RouteCheckPaneView*`, presenters, Options views, `AboutWindow.xaml` | Medium: VM additions; UI tests on text may need ids |
| **3. Pane structure** (2–3 days) | Toolbars + ≡ menus (Library, Route check), details header actions, `SearchTextBox`, product tags and chips in the Library tree, finding rows with hover action, verdict card, splitters, Log columns, mono rows, tints and filter chip. | `LibraryPaneView.xaml(.cs)`, `RouteCheckPaneView.xaml`, `LogPaneView.xaml`, `Library/*` (tags), `RouteCheck/*` (chip tone) | Medium: AutomationIds must move to menu items; check the UI tests |
| **4. Ribbon and icons** (1–2 days + icon work) | Regroup per §5, dialog launcher, split button, small Help buttons, keytips, the new icons (DrawingImage for panes, PNG 16/32 at 100/150/200% for DAML unless Pro 3.7 takes vector images there; verify). | `Config.daml`, `Images/` | Low–medium: DAML ids unchanged |
| **5. Options and dialogs polish** (1 day) | Expanders, form grid, unit suffixes, Browse..., Export and About layouts. | Options views, `ExportChartWindow.xaml`, `AboutWindow.xaml` | Low |
| **6. HC and accessibility pass** (½–1 day) | Run each pane in High Contrast #1, #2, Black and White; keyboard path through toolbars and menus; Narrator names on icon buttons; tooltips; screenshot set for docs. | — | — |

Do phase 1 first: it removes the dark-theme breakage (A1) and most of the visible drift
(A3, A5, A9–A11) with the least behaviour change.

---

## 8. What I'm unsure of about Pro

- **Exact Esri resource keys.** The public style guide lists `Esri_Button`,
  `Esri_ButtonBorderless`, `Esri_TextBlockH1`–`H7`, `Esri_TextBlockRegular`,
  `Esri_TextStyleDefaultBrush`, `Esri_TextStyleSubduedBrush`,
  `Esri_BackgroundHoverSelectedBrush`, `Esri_BackgroundPressedBrush`,
  `Esri_ControlBackgroundBrush`, `Esri_BorderHoverBrush` and `Esri_Gray145/155/160`. It does
  **not** list `Esri_Label`, `Esri_BorderBrush` or `Esri_TextErrorBrush`, which the add-in uses.
- **Controls.** `MessageLabel`, `SearchTextBox`, `CircularAnimationControl` and `BurgerButton`
  exist in recent Pro SDKs as far as I recall, but I couldn't confirm them for 3.7 offline.
  Each has a plain fallback in §4.4.
- **Theme change.** I believe Pro needs a restart after a theme change; if not, listen to the
  theme-changed event and swap the Colors dictionary.
- **Pro's colours.** The theme colours in the mockup (pane `#FFFFFF` / `#2E2E2E`, accent
  `#007AC2` / `#2EA8F5`) are eyeballed approximations. The real views take them from Esri brushes.

---

## 9. Open questions for the owner

1. **Casing.** The ribbon mixes Title Case ("Zoom to Data") with the viewer's sentence case
   ("Object information", "Route check"). Keep the viewer's words as the brief binds (my
   default), or Title-case ribbon captions only, Pro-style?
2. **Ellipsis.** `…` (Library strings, from the viewer) or `...` (Pro and the rest of the
   add-in)? Pick one for the add-in.
3. **"Remove from library"** → "Remove from Library"? (Library is a proper noun everywhere else.)
4. **Section name** for Time Format and Depth Unit in Application Options: currently under
   "Palette". Proposed "Time and units"; does the viewer have a word for it?
5. **About wording.** Change `About_NotForNavigation` to the brief's full §7.1 sentence? Add a
   product subtitle line?
6. **Overflow menus.** Is it OK for Add as Feature Layers, Validate and Open Containing Folder to
   move into the details ≡ menu, leaving Add to Map and Show Footprint visible?
7. **Icons.** Commission about seven custom ribbon icons (§4.5), or stay with Pro stock icons
   and accept some reuse?
8. **Semantic colours.** Accept add-in-owned tone brushes (§4.3), or limit tones to what Pro
   exposes and use glyphs for severity?
9. **Time group placement.** The brief puts Now, Previous data and Next data on the main tab;
   today Now and Go to Nearest Data are on the Layer tab only. Keep both?
10. **New conveniences** shown in the mockups that the brief doesn't name: Log count chips,
    copy-position in Object information, Browse... for the s100 path, and a "Show the
    SoundCharts Log" link in the Object information failure bar. Take, or drop?
11. **Scale note idle state.** Blank (dimmed glyph) or a quiet "Chart drawn at this scale"?
    The latter is new wording.
12. **Product colour.** The viewer colours Library swatches by availability. I kept product tags
    neutral in Pro. Do you want a per-product hue?

## 10. Decisions (owner review, 2026-11-16)

1. **Casing:** Title Case for ribbon captions and pane titles (e.g. "SoundCharts Object Information"); body text keeps the viewer's sentence case.
2. **Ellipsis:** `...` everywhere; Library strings change from `…`.
3. **"Remove from Library"**, capitalised as elsewhere.
4. **Section name:** "General", as in the viewer's settings categories.
5. **About:** use the brief's §7.1 sentence word for word; no subtitle line.
6. **Overflow:** Add as Feature Layers, Validate and Open Containing Folder move into the details ≡ menu; update the UI tests' automation ids.
7. **Icons:** custom icons (about seven), 16/32 px, light and dark variants.
8. **Semantic colours:** add-in-owned tone brushes per theme, always with a glyph and a word; system colours in High Contrast.
9. **Time group:** on both the main tab and the Layer tab.
10. **Extras:** keep all four: Log count chips, copy position, Browse... for the s100 path, Log link in the failure bar.
11. **Scale note idle:** blank, with a dimmed glyph.
12. **Product tags:** neutral chips, no per-product hue.
