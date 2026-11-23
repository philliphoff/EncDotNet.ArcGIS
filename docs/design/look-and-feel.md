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

The files are `Themes/SoundCharts.Colors.Light.xaml`, `.Dark.xaml` and `.HighContrast.xaml`.
The mockup's inks were adjusted when the theme was built (issue #43) so that each ink meets
4.5:1 on its own 11% fill over Pro's pane (white or `#F7F7F7` in Light, `#2E2E2E` to `#353535`
in Dark); the table shows the shipped values. Check them in Pro before locking them in.

| Key | Light | Dark | High Contrast |
|---|---|---|---|
| `SC_DangerBrush` | `#C0271A` | `#FF9485` | `SystemColors.WindowTextColor` |
| `SC_WarningBrush` (ink) | `#9A5200` | `#FFB938` | `SystemColors.WindowTextColor` |
| `SC_SuccessBrush` | `#217538` | `#58C771` | `SystemColors.WindowTextColor` |
| `SC_InfoBrush` | `#006AA5` (Pro accent, darkened) | `#5BBAF5` | `SystemColors.HotTrackColor` |
| `SC_NeutralBrush` | `#666666` (as `Esri_TextStyleSubduedBrush`) | `#B1B1B1` | `SystemColors.GrayTextColor` |
| `SC_*FillBrush` | tone at 11% (chips) | tone at 11% | Transparent |
| `SC_*TintBrush` | tone at 6% (cards, bars, rows) | tone at 6% | Transparent |
| `SC_*EdgeBrush` | tone at 40% | tone at 40% | `SystemColors.WindowTextColor` |
| `SC_CautionBackgroundBrush` | `#FFF3D1` | `#3A2F12` | `SystemColors.WindowColor` |
| `SC_CautionBorderBrush` | `#D9A21B` | `#9A7A21` | `SystemColors.WindowTextColor` |
| `SC_CautionInkBrush` | `#6A4700` | `#FFD670` | `SystemColors.WindowTextColor` |
| `SC_RuleBrush` | `#D6D6D6` | `#454545` | `SystemColors.WindowTextColor` |
| `SC_RowRuleBrush` | `#EBEBEB` | `#3A3A3A` | Transparent |
| `SC_TagBorderBrush` | `#BDBDBD` | `#5A5A5A` | `SystemColors.WindowTextColor` |

`SC_NeutralBrush`, `SC_RuleBrush`, `SC_RowRuleBrush` and `SC_TagBorderBrush` are add-in values, not aliases of Esri
brushes: a colour dictionary can't refer to a resource that only Pro provides at run time, and
the published style guide has no rule brush (§8).

**Choosing the dictionary.** Read `FrameworkApplication.ApplicationTheme` (Default, Dark,
HighContrast) when a view loads; High Contrast also wins when Windows has a contrast theme on.
Pro 3.7 has no public theme-changed event (§8), so after a theme change, restart Pro. Each view
merges one `<sc:SoundChartsTheme />` (a `ResourceDictionary` subclass whose constructor merges
`SoundCharts.xaml` plus the right Colors file). This replaces the per-view local styles, and
makes `BasedOn="{StaticResource SC_…}"` legal. Views keep the `DesignOnlyResourceDictionary`,
which only loads Pro's styles in the designer.

**High Contrast rule.** In HC, chips, cards and strips draw no fill. They get a 1 px
`WindowText` border, and the glyph carries the severity. Don't use opacity for "subdued": HC
ignores it, so use `Esri_TextStyleSubduedBrush`.

### 4.4 Shared styles (`Themes/SoundCharts.xaml`)

This table is the proposal. What was built, with every key, is in
[§11 Using the theme](#11-using-the-theme), which is the contract for the views.

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
- Tree rows: 24 px, one line; `SC_ProductTag` + name + right-aligned count or edition in Subdued mono;
  "Missing" becomes a warning chip. The row's detail (a dataset's title, a source's location, a
  status) follows the name in the caption size and trims first; the whole of it is the row's
  tooltip. Rows fill the tree's width, so counts and editions share one right edge; each level
  indents 16 px, and rows without children keep the twisty's space, so tags line up. Hover and
  selection fill the whole row.
- Details header buttons: Add to Map and Show Footprint with their glyphs and a bordered ≡, all
  24 px (`Esri_Button` at `SC_ControlHeight`).
- Splitter → `SC_Splitter`.
- Details: `SC_ProductTag` + `SC_Title` (mono name), product line, actions, caution strip (only
  if marked), then Product / Validation / Time / Source groups with `SC_SectionHeader` +
  `SC_FieldGrid`. The header, actions and caution strip stay put; only the groups scroll, so the
  buttons never scroll out of reach.
- A selected collection or source shows its name, how many datasets the filter shows under it,
  **Add All to Map**, which adds them all to one chart (asking first above 100 datasets), and
  **Add Datasets in View**, which adds only those whose indexed footprint overlaps the map's
  current extent. The Library ≡ menu has Add Datasets in View across every collection shown. Validation status becomes a tone chip (Valid → success, Not valid → danger,
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
  Failed → error message bar ("Couldn't check the passage" and the reason) in place of the card.
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
- Header: SoundCharts app icon + "SoundCharts for ArcGIS Pro" + a one-line subtitle (open
  question). Built with the ribbon's About icon at 32 px (`SC_AppIcon`, light or dark by Pro's
  theme), not the viewer's `Branding/icon.svg` at 52 px. Versions in `SC_FieldGrid` with mono
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

This section was written before the theme was built. The verification record that follows
it says what the 3.7 SDK confirmed.

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

### 8.1 Verified against the Pro 3.7 SDK (issue #43)

Checked offline in the reference assemblies of the `Esri.ArcGISPro.Extensions30` 3.7.0.1901
package: the type metadata, and the compiled theme dictionaries (BAML) in
`ArcGIS.Desktop.Framework`'s resources (`Resources/BasicResources`, `DarkResources` and
`HCBasicResources`, which `Themes/Default`, `Dark` and `HighContrast` merge). Nothing here was
checked in a running Pro.

- **Resource keys.** Every Esri key the theme uses is defined in all three of Pro's theme
  dictionaries: `Esri_TextStyleDefaultBrush`, `Esri_TextStyleSubduedBrush`,
  `Esri_BackgroundHoverSelectedBrush` and `Esri_BackgroundPressedBrush`. `Esri_Button`,
  `Esri_ButtonBorderless`, `Esri_TextBlockRegular` and `Esri_TextBlockH1`–`H7` are defined
  too. `Esri_Label` is defined nowhere, which confirms finding A4. `Esri_BorderBrush` and
  `Esri_TextErrorBrush` do exist in all three dictionaries, but the published guide doesn't
  list them, so the theme doesn't use them: rules use the add-in's `SC_RuleBrush`, and errors
  use `SC_DangerBrush`.
- **`FrameworkApplication.ApplicationTheme`** exists, with `ApplicationTheme` values `Default`,
  `Dark` and `HighContrast`. There is **no public theme-changed event** (the only theme event
  is `PopupThemeChangedEvent`, for map pop-ups), so `SoundChartsTheme` chooses once, when a view
  loads. Whether Pro itself asks for a restart after a theme change is unverified.
- **`ArcGIS.Desktop.Framework.Controls.CircularAnimationControl`** is public (a `Control`), and
  Pro's resources style it. `SC_BusyState` uses it. Its look at 24 px is unverified.
- **`ArcGIS.Desktop.Framework.Controls.SearchTextBox`** is public (a `TextBox` with `InfoText`
  for the placeholder, `SearchMode` and a `Search` event). The Library can use it (§4.6); its
  behaviour in a pane is unverified, and the fallback is today's `TextBox` with an overlay
  watermark.
- **`ArcGIS.Desktop.Framework.Controls.MessageLabel`** is public (a `Label`), but its
  `MessageType` and `Severity` enums are in `ArcGIS.Desktop.Internal.Framework.Controls`, and
  it draws its own icons and colours. The theme uses its own `SC_MessageBar` instead, so
  message bars share the tone brushes, glyphs and High Contrast rule.
- **`ArcGIS.Desktop.Framework.Controls.BurgerButton`** is public (an Actipro `PopupButton`
  through an internal `Button_Burger`). Its look in a 32 px toolbar is unverified, so the theme
  has its own `SC_BurgerButton`: an icon button that opens its `ContextMenu`, which also works
  in dialogs.

Still to check in Pro 3.7 (phase 6): every style in Light, Dark and High Contrast; the tone
contrast against Pro's real pane colours; the busy spinner; and a theme change while Pro runs.

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

---

## 11. Using the theme

This is the contract for the views: every key the theme defines, and how to use it. The theme is
in `src/EncDotNet.ArcGIS.SoundCharts/Themes/`. Change this section with any change there.

### 11.1 Merge the theme

Each view merges one `sc:SoundChartsTheme`. Keep the `DesignOnlyResourceDictionary` first: it
loads Pro's `Esri_*` styles in the designer only.

```xml
<UserControl x:Class="EncDotNet.SoundCharts.LogPaneView"
             x:ClassModifier="internal"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:extensions="clr-namespace:ArcGIS.Desktop.Extensions;assembly=ArcGIS.Desktop.Extensions"
             xmlns:sc="clr-namespace:EncDotNet.SoundCharts.Themes">
  <UserControl.Resources>
    <ResourceDictionary>
      <ResourceDictionary.MergedDictionaries>
        <extensions:DesignOnlyResourceDictionary Source="pack://application:,,,/ArcGIS.Desktop.Framework;component\Themes\Default.xaml" />
        <sc:SoundChartsTheme />
      </ResourceDictionary.MergedDictionaries>
      <!-- The view's own resources, which may now use BasedOn="{StaticResource SC_…}". -->
    </ResourceDictionary>
  </UserControl.Resources>
```

- Refer to styles, sizes and glyphs with `StaticResource`, and to brushes (`SC_*Brush` and
  `Esri_*`) with `DynamicResource`.
- Use only the `Esri_*` keys in §8.1 and those already in the views. Never `Esri_Label`,
  `Esri_BorderBrush` or `Esri_TextErrorBrush`: use `SC_RuleBrush` and `SC_DangerBrush`.
- Set no hex colour, font family or font size in a view; take them from the keys below.
- Labelled buttons outside toolbars stay `Esri_Button`. Every string still comes from
  `Strings.resx`, and AutomationIds stay where the UI tests expect them.
- `SoundChartsTheme.Variant` says which colour dictionary was merged (`Light`, `Dark` or
  `HighContrast`).

### 11.2 Attached properties and converter

Namespace `xmlns:sc="clr-namespace:EncDotNet.SoundCharts.Themes"`.

| Property | Type | Read by |
|---|---|---|
| `sc:Tone.Value` | `SoundChartsTone`: `Neutral` (default), `Info`, `Success`, `Warning`, `Danger` | `SC_ToneGlyph`, `SC_Chip`, `SC_MonoChip`, `SC_ChipButton`, `SC_VerdictCard`, `SC_MessageBar` (default `Info`), `SC_SectionHeader`, `SC_FoldableSectionHeader` and `SC_ListRow` (glyph colour). It doesn't inherit: set it on each element. |
| `sc:Ui.Glyph` | `Geometry` (an `SC_Glyph…` key) | Toolbar, icon and row buttons, section headers, list rows, chips, the pane footer (default info) and the empty state disc |
| `sc:Ui.Count` | `object` | Section headers: shown after the title, in regular weight |
| `sc:Ui.Trailing` | `object` | `SC_ListRow`: a chip or an `SC_RowAction` at the right end |
| `sc:Ui.Actions` | `object` | `SC_EmptyState`: the buttons under the text |
| `sc:Ui.OpensContextMenu` | `bool` | A button opens its `ContextMenu` below itself on click, with the button's `DataContext`. `SC_ToolbarMenuButton` and `SC_BurgerButton` set it. |
| `sc:Ui.IsDragOver` | `bool` | `SC_DropZone`: true while files are dragged over the view, so the dashed edge takes the accent. The view sets it from its drag events. |
| `SC_ToneConverter` | `ToneConverter` | Binds a view model's tone to `sc:Tone.Value`: a `SoundChartsTone`, a `RouteCheckTone` (`Destructive` is `Danger`) or a name |

### 11.3 Brushes

From the colour dictionaries (§4.3). `{Tone}` is `Danger`, `Warning`, `Success`, `Info` or
`Neutral`.

| Key | Intent |
|---|---|
| `SC_{Tone}Brush` | Ink: toned text, glyphs and bars |
| `SC_{Tone}FillBrush` | Ink at 11%: chip fill |
| `SC_{Tone}TintBrush` | Ink at 6%: verdict card, message bar and row tints (the Log's error and warning rows) |
| `SC_{Tone}EdgeBrush` | Ink at 40%: chip, card and message bar edges |
| `SC_CautionBackgroundBrush`, `SC_CautionBorderBrush`, `SC_CautionInkBrush` | NOT FOR NAVIGATION strip |
| `SC_RuleBrush` | 1 px rules: toolbar, footers, splitters; replaces `Esri_BorderBrush` |
| `SC_RowRuleBrush` | Faint 1 px rule between dense list rows (the Log); none in High Contrast |
| `SC_TagBorderBrush` | Product tag borders, the splitter grip, the drop zone's dashed edge |
| `SC_AppIcon` | A `BitmapImage`, not a brush: the app icon (the ribbon's About icon, 64 px for 32 px use), the light rendering in Light and High Contrast and the dark one in Dark. The About header. |

In High Contrast every fill and tint is transparent, and inks, edges and rules are system
colours, so a toned element must also show a glyph and a word.

### 11.4 Sizes

| Key | Type | Value |
|---|---|---|
| `SC_FontSizePageTitle`, `SC_FontSizeTitle`, `SC_FontSizeVerdictTitle`, `SC_FontSizeBody`, `SC_FontSizeCaption`, `SC_FontSizeMono`, `SC_FontSizeTag` | `Double` | 16, 14, 13, 12, 11, 11.5, 10.5 |
| `SC_MonoFontFamily` | `FontFamily` | Cascadia Mono, Consolas |
| `SC_Space2`, `SC_Space4`, `SC_Space8`, `SC_Space12`, `SC_Space16`, `SC_Space24` | `Double` | The spacing scale |
| `SC_ToolbarHeight`, `SC_ControlHeight`, `SC_RowHeight`, `SC_SummaryRowHeight`, `SC_ChipHeight` | `Double` | 32, 24, 24, 22, 18 |
| `SC_GlyphSize`, `SC_GlyphSizeSmall` | `Double` | 16, 12 |
| `SC_DialogButtonMinWidth` | `Double` | 80: dialog and Options buttons |
| `SC_FormInputWidth`, `SC_FormSliderWidth` | `Double` | 96, 170 |
| `SC_EmptyStateTextMaxWidth` | `Double` | 300 |
| `SC_StatusLabelMaxWidth` | `Double` | 220: a status label's widest, as on the Layer tab (the scale note) |
| `SC_PaneGutter` | `Thickness` | 8: a pane's outer margin |
| `SC_DialogPadding` | `Thickness` | 16: dialog body |
| `SC_RowPadding` | `Thickness` | 8,5: list and tree rows |
| `SC_ButtonGap` | `Thickness` | 8,0,0,0: between adjacent buttons |
| `SC_ToolbarItemGap` | `Thickness` | 4,0,0,0: between toolbar items |
| `SC_FieldRowMargin` | `Thickness` | 0,1,0,2: a key/value row |
| `SC_FormRowMargin`, `SC_FormGroupMargin` | `Thickness` | 0,0,0,8 and 0,0,0,16 |
| `SC_FormLabelColumnDialog`, `SC_FormLabelColumnOptions` | `GridLength` | 150 (dialogs), 170 (Options): the one label column per page |
| `SC_SummaryLabelColumn` | `GridLength` | 72: the verdict card's summary row label |

### 11.5 Glyphs

`Geometry` keys on a 16 × 16 grid, outline only, drawn from the mockup's icon set. Show one with
`<Path Style="{StaticResource SC_Glyph}" Data="{StaticResource SC_GlyphCopy}" />`, or pass it to
a style through `sc:Ui.Glyph`.

| Key | Intent |
|---|---|
| `SC_GlyphMustReview` | Octagon with !: MUST REVIEW |
| `SC_GlyphCheck` | Triangle with !: CHECK, warnings, the caution strip |
| `SC_GlyphInfo` | Circle with i: INFO, information, footers |
| `SC_GlyphError` | Circle with ×: errors and failures |
| `SC_GlyphSuccess` | Circle with a tick: meets, valid |
| `SC_GlyphAdd`, `SC_GlyphRefresh`, `SC_GlyphBurger`, `SC_GlyphChevronDown`, `SC_GlyphChevronRight`, `SC_GlyphClose`, `SC_GlyphSearch`, `SC_GlyphFilter`, `SC_GlyphCopy` | Add, refresh or check again, ≡ menu, menu arrow or expanded, collapsed, dismiss, search, filter, copy |
| `SC_GlyphFolder`, `SC_GlyphFolderOpen`, `SC_GlyphLayers`, `SC_GlyphLibrary`, `SC_GlyphChart`, `SC_GlyphFootprint`, `SC_GlyphValidate`, `SC_GlyphZoomTo` | Folder, open folder or Browse..., layers, Library, chart or dataset, footprint, validate, zoom to |
| `SC_GlyphClock`, `SC_GlyphNow`, `SC_GlyphPrevious`, `SC_GlyphNext`, `SC_GlyphNearest` | Time note, Now, previous and next data, nearest data |
| `SC_GlyphObjectInfo`, `SC_GlyphRoute`, `SC_GlyphRouteCheck`, `SC_GlyphDraw`, `SC_GlyphSelectLine`, `SC_GlyphWater` | Object information, route, Route check, draw route, check selected line, water level or clearance |
| `SC_GlyphExport`, `SC_GlyphLog`, `SC_GlyphServer`, `SC_GlyphLaunch`, `SC_GlyphSettings` | Export, Log, chart server, open externally, settings |

| Style | Target | Intent |
|---|---|---|
| `SC_Glyph` | `Path` | 16 px glyph stroked in the inherited `Foreground` (set `TextElement.Foreground` on it or a parent) |
| `SC_GlyphSmall` | `Path` | The same at 12 px |
| `SC_ToneGlyph` | `Path` | Shape and colour from `sc:Tone.Value`: Danger octagon, Warning triangle, Info circle i, Success circle tick, Neutral subdued circle i. Set `Data` to keep the tone colour with another shape. |

### 11.6 Text

All `TextBlock` styles set only foreground, size, weight, wrapping and (mono) font family; the
one-line styles also trim with an ellipsis and centre vertically.

| Key | Intent |
|---|---|
| `SC_PageTitle` | 16 SemiBold: Options page title, dialog heading |
| `SC_Title` | 14 SemiBold: selected feature, dataset, route name |
| `SC_VerdictTitle` | 13 SemiBold: verdict card title, Options expander header |
| `SC_RowTitle` | 12 SemiBold, one line with ellipsis: list row title |
| `SC_Body` | 12 Regular, wraps: everything else |
| `SC_Caption` | 11 subdued, wraps: meta lines, help text |
| `SC_Overline` | 11 SemiBold subdued, one line: section and group headers, pick count. The caps come from the string; WPF has no letter spacing. |
| `SC_Mono`, `SC_MonoSubdued` | 11.5 mono: ids, editions, versions, paths, positions, log rows |
| `SC_OptionalText`, `SC_OptionalCaption`, `SC_OptionalMono` | `SC_Body`, `SC_Caption` and `SC_MonoSubdued` that collapse when `Text` is null or empty. Replaces every local `OptionalText`. |
| `SC_MonoTitle` | `SC_Title` in the mono face: the Library's selected dataset |
| `SC_MonoCell`, `SC_MonoCellSubdued` | `SC_Mono` and `SC_MonoSubdued` on one line with an ellipsis, centred: list cells (the Log's rows) |
| `SC_ColumnHeader` | `SC_Caption` on one line with an ellipsis, centred: column headers over list cells |
| `SC_StatusLabel` | `SC_Body` on one line with an ellipsis, centred: a status label (the scale note). Put the full text in the label's tooltip and cap it at `SC_StatusLabelMaxWidth`. |
| `SC_Hyperlink` | A `Hyperlink` (not a `TextBlock`) in the info ink: a link in running text, such as the Log link in Object information's failure bar |
| `SC_FieldLabel` | Key/value label: subdued body, 12 px right margin |
| `SC_FormLabel` | Form label: body, centred vertically, 12 px right margin |
| `SC_FormHelp` | Help text under a form field: caption, 2 px top margin |

### 11.7 Buttons

| Key | Target | Intent |
|---|---|---|
| `SC_ToolbarButton` | `ButtonBase` | Borderless, 24 px, padding `5,0`: optional `sc:Ui.Glyph` then the content. Hover `Esri_BackgroundHoverSelectedBrush`, pressed `Esri_BackgroundPressedBrush`, disabled at 45% opacity. |
| `SC_ToolbarMenuButton` | `ButtonBase` | `SC_ToolbarButton` with a ▾ that opens its `ContextMenu`: "Add ▾" |
| `SC_IconButton` | `ButtonBase` | 24 × 24, the `sc:Ui.Glyph` only |
| `SC_SmallIconButton` | `ButtonBase` | 20 × 20 with a 12 px glyph: actions inside rows |
| `SC_BurgerButton` | `ButtonBase` | `SC_IconButton` with ≡ that opens its `ContextMenu`: the toolbar's right end |
| `SC_RowAction` | `ButtonBase` | `SC_SmallIconButton`, hidden until its `ListBoxItem` or `ListViewItem` is hovered, selected or has keyboard focus: the finding row's Object information action |
| `SC_SummaryRow` | `Button` | 22 px borderless row with a hover tint; the content lays out label (`SC_SummaryLabelColumn`), text and chip |
| `SC_SummaryRowStatic` | `ContentControl` | `SC_SummaryRow`'s layout for a row that does nothing when clicked: no hover tint, no focus stop (Route check's summary rows) |
| `SC_ChipCloseButton` | `ButtonBase` | 16 × 16 with a 12 px `sc:Ui.Glyph` (`SC_GlyphClose`) inside a chip, in the chip's ink: the Log's source filter chip |
| `SC_ChipButton` | `ButtonBase` | An `SC_Chip` that is a button: `sc:Tone.Value`, `sc:Ui.Glyph` and the content as the chip, a hand cursor and a keyboard stop (the Log's count chips). Give it a `ToolTip` and `AutomationProperties.HelpText` saying what a click does. |

Icon-only buttons need a `ToolTip` and `AutomationProperties.Name` from `Strings.resx`; the styles
can't supply them.

### 11.8 Structure, sections and rows

| Key | Target | Intent |
|---|---|---|
| `SC_PaneToolbar` | `Border` | 32 px, padding `4,0`, 1 px bottom rule. Child: a `DockPanel LastChildFill="False"` with the labelled primary action left and icon buttons and `SC_BurgerButton` right. |
| `SC_PickHeader` | `Border` | A pane's header strip under the toolbar: padding 8, 1 px bottom rule (Object information's pick header) |
| `SC_PaneFooter` | `ContentControl` | 1 px top rule, padding `8,6`, a 12 px info glyph (or `sc:Ui.Glyph`), 11 px subdued wrapped text |
| `SC_OptionalPaneFooter` | `ContentControl` | `SC_PaneFooter` that collapses while `Content` is null or empty |
| `SC_DialogFooter` | `Border` | 1 px top rule, padding `16,12`. Child: a `DockPanel` with Copy-type actions left and a right-aligned `StackPanel` of `Esri_Button`s, default first, `MinWidth` `SC_DialogButtonMinWidth`, `SC_ButtonGap` between them. |
| `SC_Splitter` | `GridSplitter` | Row splitter: 7 px hit area, 1 px rule, centre grip; in its own `Auto` row between the two resizable rows |
| `SC_SectionHeader` | `ContentControl` | Margin `0,12,0,4`: optional 12 px `sc:Ui.Glyph` in the `sc:Tone.Value` colour, overline content, optional `sc:Ui.Count` |
| `SC_FoldableSectionHeader` | `Expander` | The same header with a 12 px chevron, as a keyboard-reachable toggle; `Header` is the text, the content is the section, `IsExpanded` folds it |
| `SC_ListRow` | `HeaderedContentControl` | List row layout for an item template, padding `8,5`: `sc:Ui.Glyph` (in the `sc:Tone.Value` colour), `Header` as the row title, `Content` as a caption line (collapses when null), `sc:Ui.Trailing` at the right. Selection and hover stay Pro's item style. |
| `SC_ListItem` | `ListBoxItem` | A flat row container for `ItemContainerStyle`: no padding or border of its own, full-width hover (`Esri_BackgroundHoverSelectedBrush`) and selection (`Esri_BackgroundSelectedBrush`). Self-contained, so it doesn't need Pro's `ListBoxItem` style at load time. The Log's rows. Use a `ListBox`, not a `ListView`, without a `GridView`: Pro's `ListView` expects one and shows empty rows otherwise. |
| `SC_FieldGrid` | `ItemsControl` | Key/value list: a shared size scope. Item template: a two-column `Grid`, label column `SharedSizeGroup="SC_FieldLabel"`, `SC_FieldLabel` and `SC_FieldValue`, row margin `SC_FieldRowMargin`. |
| `SC_FieldValue`, `SC_FieldValueMono` | `TextBox` | Read-only, chromeless, wrapping, selectable value (mono variant for ids and paths). Give it `AutomationProperties.Name`. |

### 11.9 Chips, cards, messages and states

| Key | Target | Intent |
|---|---|---|
| `SC_Chip` | `ContentControl` | 18 px tone chip: `sc:Tone.Value` fill, edge and ink, optional 12 px `sc:Ui.Glyph`, 11 px text |
| `SC_MonoChip` | `ContentControl` | `SC_Chip` with mono 10.5 px: numbers and counts |
| `SC_ProductTag` | `ContentControl` | Neutral bordered mono tag: `S-101`, `S-104`, `ExS`, `Dir`; min width 40 |
| `SC_OptionalProductTag` | `ContentControl` | `SC_ProductTag` that collapses while `Content` is null: a Library row or dataset with no product |
| `SC_VerdictCard` | `Border` | Tone tint and edge, radius 3, padding `10,9`; put an `SC_ToneGlyph` with the same tone and an `SC_VerdictTitle` inside |
| `SC_WorkingCard` | `ContentControl` | The verdict card's shape while work runs: neutral tint and edge, a 16 px spinner, the content as a 13 px SemiBold title; live region (polite). Route check's Working state. |
| `SC_MessageBar` | `HeaderedContentControl` | 3 px tone bar, the tone's glyph (Danger shows `SC_GlyphError`), optional bold `Header`, wrapped `Content`; live region (polite). `sc:Tone.Value` `Info` (default), `Warning`, `Danger`, `Success` or `Neutral`. |
| `SC_OptionalMessageBar` | `HeaderedContentControl` | `SC_MessageBar` that collapses while `Content` is null or empty: a last action's message, a field's problem |
| `SC_CautionStrip` | `ContentControl` | NOT FOR NAVIGATION: caution colours, warning glyph, 11 px bold content; give it the existing tooltip |
| `SC_EmptyState` | `HeaderedContentControl` | Centred, padding 24: a 40 px tinted disc with `sc:Ui.Glyph`, `Header` as a 14 px SemiBold title, `Content` as subdued text (max 300 px), `sc:Ui.Actions` under it |
| `SC_BusyState` | `ContentControl` | Pro's `CircularAnimationControl` (24 px) above a subdued caption; live region (polite) |
| `SC_DropZone` | `ContentControl` | The content (a caption), centred inside a dashed 1 px `SC_TagBorderBrush` edge that takes `SC_InfoBrush` while `sc:Ui.IsDragOver` is true: the empty Library |

### 11.10 Examples

A pane toolbar:

```xml
<Border Style="{StaticResource SC_PaneToolbar}">
  <DockPanel LastChildFill="False">
    <Button Style="{StaticResource SC_ToolbarMenuButton}"
            sc:Ui.Glyph="{StaticResource SC_GlyphAdd}"
            Content="{x:Static res:Strings.Library_Add}">
      <Button.ContextMenu>
        <ContextMenu>
          <MenuItem Header="{x:Static res:Strings.Library_AddFolder}" Command="{Binding AddFolderCommand}" />
        </ContextMenu>
      </Button.ContextMenu>
    </Button>
    <Button DockPanel.Dock="Right"
            Style="{StaticResource SC_BurgerButton}"
            ToolTip="{x:Static res:Strings.Tooltip_More}"
            AutomationProperties.Name="{x:Static res:Strings.Tooltip_More}" />
    <Button DockPanel.Dock="Right"
            Style="{StaticResource SC_IconButton}"
            sc:Ui.Glyph="{StaticResource SC_GlyphRefresh}"
            Command="{Binding RefreshCommand}"
            ToolTip="{x:Static res:Strings.Tooltip_Refresh}"
            AutomationProperties.Name="{x:Static res:Strings.Tooltip_Refresh}" />
  </DockPanel>
</Border>
```

The string names are placeholders; use the view's own.

A message bar, a toned chip, and a section header with a count:

```xml
<HeaderedContentControl Style="{StaticResource SC_MessageBar}"
                        sc:Tone.Value="Warning"
                        Content="{Binding Problems}" />
<ContentControl Style="{StaticResource SC_MonoChip}"
                sc:Tone.Value="{Binding ChipTone, Converter={StaticResource SC_ToneConverter}}"
                Content="{Binding Chip}" />
<ContentControl Style="{StaticResource SC_SectionHeader}"
                sc:Ui.Glyph="{StaticResource SC_GlyphMustReview}"
                sc:Tone.Value="Danger"
                sc:Ui.Count="{Binding MustReview.Count}"
                Content="{x:Static res:Strings.RouteCheck_MustReview}" />
```

A list row in an item template:

```xml
<DataTemplate>
  <HeaderedContentControl Style="{StaticResource SC_ListRow}"
                          sc:Ui.Glyph="{StaticResource SC_GlyphCheck}"
                          sc:Tone.Value="{Binding Tone, Converter={StaticResource SC_ToneConverter}}"
                          Header="{Binding Title}"
                          Content="{Binding Detail}">
    <sc:Ui.Trailing>
      <Button Style="{StaticResource SC_RowAction}"
              sc:Ui.Glyph="{StaticResource SC_GlyphObjectInfo}"
              Command="{Binding DataContext.ShowObjectInfoCommand, RelativeSource={RelativeSource AncestorType=ListBox}}"
              CommandParameter="{Binding}"
              ToolTip="{x:Static res:Strings.Tooltip_ObjectInfo}"
              AutomationProperties.Name="{x:Static res:Strings.Tooltip_ObjectInfo}" />
    </sc:Ui.Trailing>
  </HeaderedContentControl>
</DataTemplate>
```

### 11.11 Notes for the view changes

- **Pro controls.** Use Pro's `SearchTextBox` for the Library search
  (`xmlns:controls="clr-namespace:ArcGIS.Desktop.Framework.Controls;assembly=ArcGIS.Desktop.Framework"`,
  placeholder in `InfoText`); keep the overlay watermark if it misbehaves in Pro. Use
  `SC_MessageBar` rather than `MessageLabel`, and `SC_BurgerButton` rather than Pro's
  `BurgerButton` (§8.1).
- **Tone doesn't inherit.** A chip inside a danger card is neutral unless it sets its own tone.
  Route check needs a `ChipTone` on its summary rows (§6).
- **Row padding.** `SC_ListRow` pads `8,5` inside Pro's `ListBoxItem`, which has its own small
  padding. Don't replace the item container style to remove it: that loses Pro's selection
  colours.
- **Disabled buttons** in the theme's button styles dim to 45% opacity. High Contrast ignores
  opacity, so check them there (phase 6).
- **Unit suffixes** (`SC_UnitSuffix` in §4.4) aren't in the theme: Options lays out the value
  and a `SC_Caption` unit beside it, or adds the style with that change.
- **Not yet checked in Pro.** The styles compile, but none has run in Pro yet (§8.1). Check
  each view in Light, Dark and High Contrast as it changes.
